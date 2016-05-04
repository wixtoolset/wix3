// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.CodeDom;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Type containing static Generate method, which fills in a compile unit from a 
    /// given schema.
    /// </summary>
    internal class StronglyTypedClasses
    {
        private static string outputXmlComment = "Processes this element and all child elements into an XmlWriter.";
        private static Hashtable simpleTypeNamesToClrTypeNames;
        private static Dictionary<string, EnumDeclaration> typeNamesToEnumDeclarations;
        private static Dictionary<EnumDeclaration, CodeTypeDeclaration> enumsToParseMethodClasses;
        private static Regex multiUppercaseNameRegex = new Regex("[A-Z][A-Z][A-Z]", RegexOptions.Compiled);
        private static Dictionary<string, XmlSchemaAttributeGroup> refToAttributeGroups;
        private static CodeTypeDeclaration enumHelperClass;

        /// <summary>
        /// Private constructor for static class.
        /// </summary>
        private StronglyTypedClasses()
        {
        }

        /// <summary>
        /// Generates strongly typed serialization classes for the given schema document
        /// under the given namespace and generates a code compile unit.
        /// </summary>
        /// <param name="xmlSchema">Schema document to generate classes for.</param>
        /// <param name="generateNamespace">Namespace to be used for the generated code.</param>
        /// <param name="commonNamespace">Namespace in which to find common classes and interfaces, 
        /// like ISchemaElement.</param>
        /// <returns>A fully populated CodeCompileUnit, which can be serialized in the language of choice.</returns>
        public static CodeCompileUnit Generate(XmlSchema xmlSchema, string generateNamespace, string commonNamespace)
        {
            if (xmlSchema == null)
            {
                throw new ArgumentNullException("xmlSchema");
            }
            if (generateNamespace == null)
            {
                throw new ArgumentNullException("generateNamespace");
            }

            simpleTypeNamesToClrTypeNames = new Hashtable();
            typeNamesToEnumDeclarations = new Dictionary<string,EnumDeclaration>();
            refToAttributeGroups = new Dictionary<string, XmlSchemaAttributeGroup>();
            enumsToParseMethodClasses = new Dictionary<EnumDeclaration, CodeTypeDeclaration>();

            CodeCompileUnit codeCompileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace(generateNamespace);
            codeCompileUnit.Namespaces.Add(codeNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.CodeDom.Compiler")); // for GeneratedCodeAttribute
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Diagnostics.CodeAnalysis"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Globalization"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("System.Xml"));
            if (commonNamespace != null)
            {
                codeNamespace.Imports.Add(new CodeNamespaceImport(commonNamespace));
            }

            // NOTE: This hash table serves double duty so be sure to have the XSD
            //       type name mapped to the CLR type name *and* the CLR type name
            //       mapped to the same CLR type name.  Look at long and bool for
            //       examples below (and before you ask, no I don't know why DateTime
            //       just works).
            simpleTypeNamesToClrTypeNames.Add("dateTime", "DateTime");
            simpleTypeNamesToClrTypeNames.Add("integer", "int");
            simpleTypeNamesToClrTypeNames.Add("int", "int");
            simpleTypeNamesToClrTypeNames.Add("NMTOKEN", "string");
            simpleTypeNamesToClrTypeNames.Add("string", "string");
            simpleTypeNamesToClrTypeNames.Add("nonNegativeInteger", "long");
            simpleTypeNamesToClrTypeNames.Add("long", "long");
            simpleTypeNamesToClrTypeNames.Add("boolean", "bool");
            simpleTypeNamesToClrTypeNames.Add("bool", "bool");

            xmlSchema.Compile(null);

            foreach (XmlSchemaAttributeGroup schemaAttributeGroup in xmlSchema.AttributeGroups.Values)
            {
                refToAttributeGroups.Add(schemaAttributeGroup.Name, schemaAttributeGroup);
            }

            foreach (XmlSchemaObject schemaObject in xmlSchema.SchemaTypes.Values)
            {
                XmlSchemaSimpleType schemaSimpleType = schemaObject as XmlSchemaSimpleType;
                if (schemaSimpleType != null)
                {
                    ProcessSimpleType(schemaSimpleType, codeNamespace);
                }
            }

            foreach (XmlSchemaObject schemaObject in xmlSchema.SchemaTypes.Values)
            {
                XmlSchemaComplexType schemaComplexType = schemaObject as XmlSchemaComplexType;
                if (schemaComplexType != null)
                {
                    ProcessComplexType(schemaComplexType, codeNamespace);
                }
            }

            foreach (XmlSchemaObject schemaObject in xmlSchema.Elements.Values)
            {
                XmlSchemaElement schemaElement = schemaObject as XmlSchemaElement;
                if (schemaElement != null)
                {
                    ProcessElement(schemaElement, codeNamespace);
                }
            }

            return codeCompileUnit;
        }

        /// <summary>
        /// Processes an XmlSchemaElement into corresponding types.
        /// </summary>
        /// <param name="schemaElement">XmlSchemaElement to be processed.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code.</param>
        private static void ProcessElement(XmlSchemaElement schemaElement, CodeNamespace codeNamespace)
        {
            string elementType = schemaElement.SchemaTypeName.Name;
            string elementNamespace = schemaElement.QualifiedName.Namespace;
            string elementDocumentation = GetDocumentation(schemaElement.Annotation);

            if ((elementType == null || elementType.Length == 0) && schemaElement.SchemaType != null)
            {
                ProcessComplexType(schemaElement.Name, elementNamespace, (XmlSchemaComplexType)schemaElement.SchemaType, elementDocumentation, codeNamespace);
            }
            else
            {
                if (elementType == null || elementType.Length == 0)
                {
                    elementType = "string";
                }

                CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(schemaElement.Name);
                typeDeclaration.CustomAttributes.Add(GetGeneratedCodeAttribute());
                typeDeclaration.Attributes = MemberAttributes.Public;
                typeDeclaration.IsClass = true;

                if (elementDocumentation != null)
                {
                    GenerateSummaryComment(typeDeclaration.Comments, elementDocumentation);
                }

                CodeMemberMethod outputXmlMethod = new CodeMemberMethod();
                outputXmlMethod.Attributes = MemberAttributes.Public;
                outputXmlMethod.ImplementationTypes.Add("ISchemaElement");
                outputXmlMethod.Name = "OutputXml";
                outputXmlMethod.Parameters.Add(new CodeParameterDeclarationExpression("XmlWriter", "writer"));
                outputXmlMethod.Statements.Add(GetArgumentNullCheckStatement("writer", false));
                outputXmlMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteStartElement", new CodeSnippetExpression(String.Concat("\"", schemaElement.Name, "\"")), new CodeSnippetExpression(String.Concat("\"", elementNamespace, "\""))));
                GenerateSummaryComment(outputXmlMethod.Comments, outputXmlComment);

                if (simpleTypeNamesToClrTypeNames.ContainsKey(elementType))
                {
                    CodeMemberField parentField = new CodeMemberField("ISchemaElement", "parentElement");
                    typeDeclaration.Members.Add(parentField);

                    CodeMemberProperty parentProperty = new CodeMemberProperty();
                    parentProperty.Attributes = MemberAttributes.Public;
                    parentProperty.ImplementationTypes.Add("ISchemaElement");
                    parentProperty.Name = "ParentElement";
                    parentProperty.Type = new CodeTypeReference("ISchemaElement");
                    parentProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement")));
                    parentProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement"), new CodeVariableReferenceExpression("value")));
                    typeDeclaration.Members.Add(parentProperty);

                    CodeMemberMethod setAttributeMethod = new CodeMemberMethod();
                    setAttributeMethod.Attributes = MemberAttributes.Public;
                    setAttributeMethod.ImplementationTypes.Add("ISetAttributes");
                    setAttributeMethod.Name = "SetAttribute";
                    setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
                    setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
                    setAttributeMethod.PrivateImplementationType = new CodeTypeReference("ISetAttributes");
                    setAttributeMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"));
                    setAttributeMethod.Statements.Add(GetArgumentNullCheckStatement("name", true));

                    GenerateFieldAndProperty("Content", (string)simpleTypeNamesToClrTypeNames[elementType], typeDeclaration, outputXmlMethod, setAttributeMethod, null, elementDocumentation, true, false);

                    typeDeclaration.Members.Add(setAttributeMethod);
                    typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISetAttributes"));
                }
                else
                {
                    typeDeclaration.BaseTypes.Add(elementType);
                    outputXmlMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeBaseReferenceExpression(), "OutputXml", new CodeVariableReferenceExpression("writer")));
                    outputXmlMethod.Attributes |= MemberAttributes.Override;
                }

                outputXmlMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteEndElement"));

                typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISchemaElement"));
                typeDeclaration.Members.Add(outputXmlMethod);
                codeNamespace.Types.Add(typeDeclaration);
            }
        }

        /// <summary>
        /// Processes an XmlSchemaComplexType into corresponding types.
        /// </summary>
        /// <param name="complexType">XmlSchemaComplexType to be processed.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code.</param>
        private static void ProcessComplexType(XmlSchemaComplexType complexType, CodeNamespace codeNamespace)
        {
            CodeMemberMethod outputXmlMethod = new CodeMemberMethod();
            outputXmlMethod.Attributes = MemberAttributes.Public;
            outputXmlMethod.ImplementationTypes.Add("ISchemaElement");
            outputXmlMethod.Name = "OutputXml";
            outputXmlMethod.Parameters.Add(new CodeParameterDeclarationExpression("XmlWriter", "writer"));
            outputXmlMethod.Statements.Add(GetArgumentNullCheckStatement("writer", false));
            GenerateSummaryComment(outputXmlMethod.Comments, outputXmlComment);

            CodeMemberMethod setAttributeMethod = new CodeMemberMethod();
            setAttributeMethod.Attributes = MemberAttributes.Public;
            setAttributeMethod.ImplementationTypes.Add("ISetAttributes");
            setAttributeMethod.Name = "SetAttribute";
            setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
            setAttributeMethod.PrivateImplementationType = new CodeTypeReference("ISetAttributes");
            setAttributeMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"));
            setAttributeMethod.Statements.Add(GetArgumentNullCheckStatement("name", true));

            string documentation = GetDocumentation(complexType.Annotation);

            ProcessSimpleContent(complexType.Name, (XmlSchemaSimpleContentExtension)complexType.ContentModel.Content, documentation, codeNamespace, outputXmlMethod, setAttributeMethod, true);
        }

        /// <summary>
        /// Processes an XmlSchemaComplexType into corresponding types.
        /// </summary>
        /// <param name="typeName">Name to use for the type being output.</param>
        /// <param name="elementNamespace">Namespace of the xml element.</param>
        /// <param name="complexType">XmlSchemaComplexType to be processed.</param>
        /// <param name="documentation">Documentation for the element.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code.</param>
        private static void ProcessComplexType(string typeName, string elementNamespace, XmlSchemaComplexType complexType, string documentation, CodeNamespace codeNamespace)
        {
            CodeMemberMethod outputXmlMethod = new CodeMemberMethod();
            outputXmlMethod.Attributes = MemberAttributes.Public;
            outputXmlMethod.ImplementationTypes.Add("ISchemaElement");
            outputXmlMethod.Name = "OutputXml";
            outputXmlMethod.Parameters.Add(new CodeParameterDeclarationExpression("XmlWriter", "writer"));
            outputXmlMethod.Statements.Add(GetArgumentNullCheckStatement("writer", false));
            outputXmlMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteStartElement", new CodeSnippetExpression(String.Concat("\"", typeName, "\"")), new CodeSnippetExpression(String.Concat("\"", elementNamespace, "\""))));
            GenerateSummaryComment(outputXmlMethod.Comments, outputXmlComment);

            CodeMemberMethod setAttributeMethod = new CodeMemberMethod();
            setAttributeMethod.Attributes = MemberAttributes.Public;
            setAttributeMethod.ImplementationTypes.Add("ISetAttributes");
            setAttributeMethod.Name = "SetAttribute";
            setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "name"));
            setAttributeMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "value"));
            setAttributeMethod.PrivateImplementationType = new CodeTypeReference("ISetAttributes");
            setAttributeMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"));
            setAttributeMethod.Statements.Add(GetArgumentNullCheckStatement("name", true));

            if (complexType.ContentModel == null)
            {
                CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(typeName);
                typeDeclaration.CustomAttributes.Add(GetGeneratedCodeAttribute());
                typeDeclaration.Attributes = MemberAttributes.Public;
                typeDeclaration.IsClass = true;
                CodeIterationStatement childEnumStatement = null;

                if (documentation != null)
                {
                    GenerateSummaryComment(typeDeclaration.Comments, documentation);
                }

                if (complexType.Particle != null)
                {
                    CodeMemberField childrenField = new CodeMemberField("ElementCollection", "children");
                    typeDeclaration.Members.Add(childrenField);

                    CodeMemberProperty childrenProperty = new CodeMemberProperty();
                    childrenProperty.Attributes = MemberAttributes.Public;
                    childrenProperty.ImplementationTypes.Add("IParentElement");
                    childrenProperty.Name = "Children";
                    childrenProperty.Type = new CodeTypeReference("IEnumerable");
                    childrenProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children")));
                    typeDeclaration.Members.Add(childrenProperty);

                    CodeMemberProperty filterChildrenProperty = new CodeMemberProperty();
                    filterChildrenProperty.Attributes = MemberAttributes.Public;
                    filterChildrenProperty.ImplementationTypes.Add("IParentElement");
                    filterChildrenProperty.Name = "Item";
                    filterChildrenProperty.Parameters.Add(new CodeParameterDeclarationExpression(typeof(Type), "childType"));
                    filterChildrenProperty.Type = new CodeTypeReference("IEnumerable");
                    filterChildrenProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children"), "Filter", new CodeVariableReferenceExpression("childType"))));
                    filterChildrenProperty.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers"));
                    typeDeclaration.Members.Add(filterChildrenProperty);

                    CodeMemberMethod addChildMethod = new CodeMemberMethod();
                    addChildMethod.Attributes = MemberAttributes.Public;
                    addChildMethod.ImplementationTypes.Add("IParentElement");
                    addChildMethod.Name = "AddChild";
                    addChildMethod.Parameters.Add(new CodeParameterDeclarationExpression("ISchemaElement", "child"));
                    addChildMethod.Statements.Add(GetArgumentNullCheckStatement("child", false));
                    CodeExpressionStatement addChildStatement = new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children"), "AddElement", new CodeVariableReferenceExpression("child")));
                    addChildMethod.Statements.Add(addChildStatement);
                    CodeAssignStatement setParentStatement = new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("child"), "ParentElement"), new CodeThisReferenceExpression());
                    addChildMethod.Statements.Add(setParentStatement);
                    typeDeclaration.Members.Add(addChildMethod);

                    CodeMemberMethod removeChildMethod = new CodeMemberMethod();
                    removeChildMethod.Attributes = MemberAttributes.Public;
                    removeChildMethod.ImplementationTypes.Add("IParentElement");
                    removeChildMethod.Name = "RemoveChild";
                    removeChildMethod.Parameters.Add(new CodeParameterDeclarationExpression("ISchemaElement", "child"));
                    removeChildMethod.Statements.Add(GetArgumentNullCheckStatement("child", false));
                    CodeExpressionStatement removeChildStatement = new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children"), "RemoveElement", new CodeVariableReferenceExpression("child")));
                    removeChildMethod.Statements.Add(removeChildStatement);
                    CodeAssignStatement nullParentStatement = new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("child"), "ParentElement"), new CodePrimitiveExpression(null));
                    removeChildMethod.Statements.Add(nullParentStatement);
                    typeDeclaration.Members.Add(removeChildMethod);

                    CodeMemberMethod createChildMethod = new CodeMemberMethod();
                    createChildMethod.Attributes = MemberAttributes.Public;
                    createChildMethod.ImplementationTypes.Add("ICreateChildren");
                    createChildMethod.Name = "CreateChild";
                    createChildMethod.Parameters.Add(new CodeParameterDeclarationExpression(typeof(string), "childName"));
                    createChildMethod.PrivateImplementationType = new CodeTypeReference("ICreateChildren");
                    createChildMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes"));
                    createChildMethod.ReturnType = new CodeTypeReference("ISchemaElement");
                    createChildMethod.Statements.Add(GetArgumentNullCheckStatement("childName", true));
                    createChildMethod.Statements.Add(new CodeVariableDeclarationStatement("ISchemaElement", "childValue", new CodePrimitiveExpression(null)));

                    CodeConstructor typeConstructor = new CodeConstructor();
                    typeConstructor.Attributes = MemberAttributes.Public;

                    CodeVariableReferenceExpression collectionVariable = null;

                    XmlSchemaChoice schemaChoice = complexType.Particle as XmlSchemaChoice;
                    if (schemaChoice != null)
                    {
                        collectionVariable = ProcessSchemaGroup(schemaChoice, typeConstructor, createChildMethod);
                    }
                    else
                    {
                        XmlSchemaSequence schemaSequence = complexType.Particle as XmlSchemaSequence;
                        if (schemaSequence != null)
                        {
                            collectionVariable = ProcessSchemaGroup(schemaSequence, typeConstructor, createChildMethod);
                        }
                    }

                    typeConstructor.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children"), collectionVariable));
                    typeDeclaration.Members.Add(typeConstructor);

                    CodeConditionStatement childNameNotFound = new CodeConditionStatement();
                    childNameNotFound.Condition = new CodeBinaryOperatorExpression(new CodePrimitiveExpression(null), CodeBinaryOperatorType.ValueEquality, new CodeVariableReferenceExpression("childValue"));
                    childNameNotFound.TrueStatements.Add(new CodeThrowExceptionStatement(new CodeObjectCreateExpression("InvalidOperationException", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("String"), "Concat", new CodeVariableReferenceExpression("childName"), new CodeSnippetExpression("\" is not a valid child name.\"")))));
                    createChildMethod.Statements.Add(childNameNotFound);

                    if (createChildMethod.Statements.Count > 8)
                    {
                        createChildMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"));
                    }

                    createChildMethod.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("childValue")));
                    typeDeclaration.Members.Add(createChildMethod);

                    childEnumStatement = new CodeIterationStatement();
                    childEnumStatement.InitStatement = new CodeVariableDeclarationStatement("IEnumerator", "enumerator", new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "children"), "GetEnumerator"));
                    childEnumStatement.TestExpression = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("enumerator"), "MoveNext");
                    childEnumStatement.Statements.Add(new CodeVariableDeclarationStatement("ISchemaElement", "childElement", new CodeCastExpression("ISchemaElement", new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("enumerator"), "Current"))));
                    childEnumStatement.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("childElement"), "OutputXml", new CodeVariableReferenceExpression("writer")));
                    childEnumStatement.IncrementStatement = new CodeExpressionStatement(new CodeSnippetExpression(""));

                    typeDeclaration.BaseTypes.Add(new CodeTypeReference("IParentElement"));
                    typeDeclaration.BaseTypes.Add(new CodeTypeReference("ICreateChildren"));
                }

                // TODO: Handle xs:anyAttribute.
                ProcessAttributes(complexType.Attributes, typeDeclaration, outputXmlMethod, setAttributeMethod);

                if (childEnumStatement != null)
                {
                    outputXmlMethod.Statements.Add(childEnumStatement);
                }

                typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISchemaElement"));
                typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISetAttributes"));

                CodeMemberField parentField = new CodeMemberField("ISchemaElement", "parentElement");
                typeDeclaration.Members.Add(parentField);

                CodeMemberProperty parentProperty = new CodeMemberProperty();
                parentProperty.Attributes = MemberAttributes.Public;
                parentProperty.ImplementationTypes.Add("ISchemaElement");
                parentProperty.Name = "ParentElement";
                parentProperty.Type = new CodeTypeReference("ISchemaElement");
                parentProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement")));
                parentProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement"), new CodeVariableReferenceExpression("value")));
                typeDeclaration.Members.Add(parentProperty);

                if (outputXmlMethod.Statements.Count > 8)
                {
                    outputXmlMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"));
                }

                if (setAttributeMethod.Statements.Count > 8)
                {
                    setAttributeMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"));
                }

                typeDeclaration.Members.Add(outputXmlMethod);
                typeDeclaration.Members.Add(setAttributeMethod);
                codeNamespace.Types.Add(typeDeclaration);
            }
            else
            {
                ProcessSimpleContent(typeName, (XmlSchemaSimpleContentExtension)complexType.ContentModel.Content, documentation, codeNamespace, outputXmlMethod, setAttributeMethod, false);
            }

            outputXmlMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteEndElement"));
        }

        /// <summary>
        /// Processes a collection of attributes, generating the required fields and properties.
        /// </summary>
        /// <param name="attributes">List of attribute or attributeGroupRef elements being processed.</param>
        /// <param name="typeDeclaration">CodeTypeDeclaration to be used when outputting code.</param>
        /// <param name="outputXmlMethod">Member method for the OutputXml method.</param>
        /// <param name="setAttributeMethod">Member method for the SetAttribute method.</param>
        private static void ProcessAttributes(XmlSchemaObjectCollection attributes, CodeTypeDeclaration typeDeclaration, CodeMemberMethod outputXmlMethod, CodeMemberMethod setAttributeMethod)
        {
            foreach (XmlSchemaObject schemaObject in attributes)
            {
                XmlSchemaAttribute schemaAttribute = schemaObject as XmlSchemaAttribute;
                if (schemaAttribute != null)
                {
                    ProcessAttribute(schemaAttribute, typeDeclaration, outputXmlMethod, setAttributeMethod);
                }
                else
                {
                    XmlSchemaAttributeGroupRef schemaAttributeGroupRef = schemaObject as XmlSchemaAttributeGroupRef;
                    if (schemaAttributeGroupRef != null)
                    {
                        XmlSchemaAttributeGroup schemaAttributeGroup = refToAttributeGroups[schemaAttributeGroupRef.RefName.Name];
                        // recurse!
                        ProcessAttributes(schemaAttributeGroup.Attributes, typeDeclaration, outputXmlMethod, setAttributeMethod);
                    }
                }
            }
        }

        /// <summary>
        /// Processes an XmlSchemaGroupBase element.
        /// </summary>
        /// <param name="schemaGroup">Element group to process.</param>
        /// <param name="constructor">Constructor to which statements should be added.</param>
        /// <param name="createChildMethod">Method used for creating children on read-in.</param>
        /// <returns>A reference to the local variable containing the collection.</returns>
        private static CodeVariableReferenceExpression ProcessSchemaGroup(XmlSchemaGroupBase schemaGroup, CodeConstructor constructor, CodeMemberMethod createChildMethod)
        {
            return ProcessSchemaGroup(schemaGroup, constructor, createChildMethod, 0);
        }

        /// <summary>
        /// Processes an XmlSchemaGroupBase element.
        /// </summary>
        /// <param name="schemaGroup">Element group to process.</param>
        /// <param name="constructor">Constructor to which statements should be added.</param>
        /// <param name="createChildMethod">Method used for creating children on read-in.</param>
        /// <param name="depth">Depth to which this collection is nested.</param>
        /// <returns>A reference to the local variable containing the collection.</returns>
        private static CodeVariableReferenceExpression ProcessSchemaGroup(XmlSchemaGroupBase schemaGroup, CodeConstructor constructor, CodeMemberMethod createChildMethod, int depth)
        {
            string collectionName = String.Format("childCollection{0}", depth);
            CodeVariableReferenceExpression collectionVariableReference = new CodeVariableReferenceExpression(collectionName);
            CodeVariableDeclarationStatement collectionStatement = new CodeVariableDeclarationStatement("ElementCollection", collectionName);
            if (schemaGroup is XmlSchemaChoice)
            {
                collectionStatement.InitExpression = new CodeObjectCreateExpression("ElementCollection", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("ElementCollection.CollectionType"), "Choice"));
            }
            else
            {
                collectionStatement.InitExpression = new CodeObjectCreateExpression("ElementCollection", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("ElementCollection.CollectionType"), "Sequence"));
            }
            constructor.Statements.Add(collectionStatement);

            foreach (XmlSchemaObject obj in schemaGroup.Items)
            {
                XmlSchemaElement schemaElement = obj as XmlSchemaElement;
                if (schemaElement != null)
                {
                    if (schemaGroup is XmlSchemaChoice)
                    {
                        CodeMethodInvokeExpression addItemInvoke = new CodeMethodInvokeExpression(collectionVariableReference, "AddItem", new CodeObjectCreateExpression("ElementCollection.ChoiceItem", new CodeTypeOfExpression(schemaElement.RefName.Name)));
                        constructor.Statements.Add(addItemInvoke);
                    }
                    else
                    {
                        CodeMethodInvokeExpression addItemInvoke = new CodeMethodInvokeExpression(collectionVariableReference, "AddItem", new CodeObjectCreateExpression("ElementCollection.SequenceItem", new CodeTypeOfExpression(schemaElement.RefName.Name)));
                        constructor.Statements.Add(addItemInvoke);
                    }

                    CodeConditionStatement createChildIf = new CodeConditionStatement();
                    createChildIf.Condition = new CodeBinaryOperatorExpression(new CodeSnippetExpression(String.Concat("\"", schemaElement.RefName.Name, "\"")), CodeBinaryOperatorType.ValueEquality, new CodeVariableReferenceExpression("childName"));
                    createChildIf.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("childValue"), new CodeObjectCreateExpression(schemaElement.RefName.Name)));
                    createChildMethod.Statements.Add(createChildIf);

                    continue;
                }

                XmlSchemaAny schemaAny = obj as XmlSchemaAny;
                if (schemaAny != null)
                {
                    if (schemaGroup is XmlSchemaChoice)
                    {
                        CodeMethodInvokeExpression addItemInvoke = new CodeMethodInvokeExpression(collectionVariableReference, "AddItem", new CodeObjectCreateExpression("ElementCollection.ChoiceItem", new CodeTypeOfExpression("ISchemaElement")));
                        constructor.Statements.Add(addItemInvoke);
                    }
                    else
                    {
                        CodeMethodInvokeExpression addItemInvoke = new CodeMethodInvokeExpression(collectionVariableReference, "AddItem", new CodeObjectCreateExpression("ElementCollection.SequenceItem", new CodeTypeOfExpression("ISchemaElement"), new CodeSnippetExpression("0"), new CodeSnippetExpression("-1")));
                        constructor.Statements.Add(addItemInvoke);
                    }

                    continue;
                }

                XmlSchemaGroupBase schemaGroupBase = obj as XmlSchemaGroupBase;
                if (schemaGroupBase != null)
                {
                    CodeVariableReferenceExpression nestedCollectionReference = ProcessSchemaGroup(schemaGroupBase, constructor, createChildMethod, depth + 1);
                    CodeMethodInvokeExpression addCollectionInvoke = new CodeMethodInvokeExpression(collectionVariableReference, "AddCollection", nestedCollectionReference);
                    constructor.Statements.Add(addCollectionInvoke);

                    continue;
                }
            }

            return collectionVariableReference;
        }

        /// <summary>
        /// Processes an XmlSchemaSimpleContentExtension into corresponding types.
        /// </summary>
        /// <param name="typeName">Name of the type being generated.</param>
        /// <param name="simpleContent">XmlSchemaSimpleContentExtension being processed.</param>
        /// <param name="documentation">Documentation for the simple content.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code.</param>
        /// <param name="outputXmlMethod">Method to use when outputting Xml.</param>
        /// <param name="setAttributeMethod">Method to use when setting an attribute.</param>
        /// <param name="abstractClass">If true, generate an abstract class.</param>
        private static void ProcessSimpleContent(string typeName, XmlSchemaSimpleContentExtension simpleContent, string documentation, CodeNamespace codeNamespace, CodeMemberMethod outputXmlMethod, CodeMemberMethod setAttributeMethod, bool abstractClass)
        {
            CodeTypeDeclaration typeDeclaration = new CodeTypeDeclaration(typeName);
            typeDeclaration.CustomAttributes.Add(GetGeneratedCodeAttribute());
            typeDeclaration.Attributes = MemberAttributes.Public;
            typeDeclaration.IsClass = true;

            if (documentation != null)
            {
                GenerateSummaryComment(typeDeclaration.Comments, documentation);
            }

            if (abstractClass)
            {
                typeDeclaration.TypeAttributes = System.Reflection.TypeAttributes.Abstract | System.Reflection.TypeAttributes.Public;
            }

            // TODO: Handle xs:anyAttribute here.
            foreach (XmlSchemaAttribute schemaAttribute in simpleContent.Attributes)
            {
                ProcessAttribute(schemaAttribute, typeDeclaration, outputXmlMethod, setAttributeMethod);
            }

            // This needs to come last, so that the generation code generates the inner content after the attributes.
            string contentDocumentation = GetDocumentation(simpleContent.Annotation);
            GenerateFieldAndProperty("Content", (string)simpleTypeNamesToClrTypeNames[simpleContent.BaseTypeName.Name], typeDeclaration, outputXmlMethod, setAttributeMethod, null, contentDocumentation, true, false);

            typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISchemaElement"));
            typeDeclaration.BaseTypes.Add(new CodeTypeReference("ISetAttributes"));

            CodeMemberField parentField = new CodeMemberField("ISchemaElement", "parentElement");
            typeDeclaration.Members.Add(parentField);

            CodeMemberProperty parentProperty = new CodeMemberProperty();
            parentProperty.Attributes = MemberAttributes.Public;
            parentProperty.ImplementationTypes.Add("ISchemaElement");
            parentProperty.Name = "ParentElement";
            parentProperty.Type = new CodeTypeReference("ISchemaElement");
            parentProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement")));
            parentProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "parentElement"), new CodeVariableReferenceExpression("value")));
            typeDeclaration.Members.Add(parentProperty);

            if (outputXmlMethod.Statements.Count > 8)
            {
                outputXmlMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"));
            }

            if (setAttributeMethod.Statements.Count > 8)
            {
                setAttributeMethod.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"));
            }

            typeDeclaration.Members.Add(outputXmlMethod);
            typeDeclaration.Members.Add(setAttributeMethod);
            codeNamespace.Types.Add(typeDeclaration);
        }

        /// <summary>
        /// Processes an attribute, generating the required field and property. Potentially generates
        /// an enum for an attribute restriction.
        /// </summary>
        /// <param name="attribute">Attribute element being processed.</param>
        /// <param name="typeDeclaration">CodeTypeDeclaration to be used when outputting code.</param>
        /// <param name="outputXmlMethod">Member method for the OutputXml method.</param>
        /// <param name="setAttributeMethod">Member method for the SetAttribute method.</param>
        private static void ProcessAttribute(XmlSchemaAttribute attribute, CodeTypeDeclaration typeDeclaration, CodeMemberMethod outputXmlMethod, CodeMemberMethod setAttributeMethod)
        {
            string attributeName = attribute.QualifiedName.Name;
            string rawAttributeType = attribute.AttributeSchemaType.QualifiedName.Name;
            string attributeType = null;
            EnumDeclaration enumDeclaration = null;
            if (rawAttributeType == null || rawAttributeType.Length == 0)
            {
                ProcessSimpleType(attributeName, attribute.AttributeSchemaType, true, out enumDeclaration, out attributeType);
                
                if (enumDeclaration != null)
                {
                    typeDeclaration.Members.Add(enumDeclaration.TypeDeclaration);
                    AddEnumHelperMethods(enumDeclaration, typeDeclaration);
                }
            }
            else
            {
                attributeType = (string)simpleTypeNamesToClrTypeNames[rawAttributeType];
            }

            string documentation = GetDocumentation(attribute.Annotation);

            // TODO: Handle required fields.
            GenerateFieldAndProperty(attributeName, attributeType, typeDeclaration, outputXmlMethod, setAttributeMethod, enumDeclaration, documentation, false, false);
        }

        /// <summary>
        /// Gets the first sentence of a documentation element and returns it as a string.
        /// </summary>
        /// <param name="annotation">The annotation in which to look for a documentation element.</param>
        /// <returns>The string representing the first sentence, or null if none found.</returns>
        private static string GetDocumentation(XmlSchemaAnnotation annotation)
        {
            string documentation = null;
            
            if (annotation != null && annotation.Items != null)
            {
                foreach (XmlSchemaObject obj in annotation.Items)
                {
                    XmlSchemaDocumentation schemaDocumentation = obj as XmlSchemaDocumentation;
                    if (schemaDocumentation != null)
                    {
                        if (schemaDocumentation.Markup.Length > 0)
                        {
                            XmlText text = schemaDocumentation.Markup[0] as XmlText;
                            if (text != null)
                            {
                                documentation = text.Value;
                            }
                        }
                        break;
                    }
                }
            }

            if (documentation != null)
            {
                documentation = documentation.Trim();
            }
            return documentation;
        }

        /// <summary>
        /// Makes a valid enum value out of the passed in value. May remove spaces, add 'Item' to the
        /// start if it begins with an integer, or strip out punctuation.
        /// </summary>
        /// <param name="enumValue">Enum value to be processed.</param>
        /// <returns>Enum value with invalid characters removed.</returns>
        private static string MakeEnumValue(string enumValue)
        {
            if (Char.IsDigit(enumValue[0]))
            {
                enumValue = String.Concat("Item", enumValue);
            }

            StringBuilder newValue = new StringBuilder();
            for (int i = 0; i < enumValue.Length; ++i)
            {
                if (!Char.IsPunctuation(enumValue[i]) && !Char.IsSymbol(enumValue[i]) && !Char.IsWhiteSpace(enumValue[i]))
                {
                    newValue.Append(enumValue[i]);
                }
            }

            return newValue.ToString();
        }

        /// <summary>
        /// Generates the private field and public property for a piece of data.
        /// </summary>
        /// <param name="propertyName">Name of the property being generated.</param>
        /// <param name="typeName">Name of the type for the property.</param>
        /// <param name="typeDeclaration">Type declaration into which the field and property should be placed.</param>
        /// <param name="outputXmlMethod">Member method for the OutputXml method.</param>
        /// <param name="setAttributeMethod">Member method for the SetAttribute method.</param>
        /// <param name="enumDeclaration">EnumDeclaration, which is null unless called from a locally defined enum attribute.</param>
        /// <param name="documentation">Comment string to be placed on the property.</param>
        /// <param name="nestedContent">If true, the field will be placed in nested content when outputting to XML.</param>
        /// <param name="requiredField">If true, the generated serialization code will throw if the field is not set.</param>
        private static void GenerateFieldAndProperty(string propertyName, string typeName, CodeTypeDeclaration typeDeclaration, CodeMemberMethod outputXmlMethod, CodeMemberMethod setAttributeMethod, EnumDeclaration enumDeclaration, string documentation, bool nestedContent, bool requiredField)
        {
            string fieldName = String.Concat(propertyName.Substring(0, 1).ToLower(), propertyName.Substring(1), "Field");
            string fieldNameSet = String.Concat(fieldName, "Set");
            Type type = GetClrTypeByXmlName(typeName);
            CodeMemberField fieldMember;
            if (type == null)
            {
                fieldMember = new CodeMemberField(typeName, fieldName);
            }
            else
            {
                fieldMember = new CodeMemberField(type, fieldName);
            }
            fieldMember.Attributes = MemberAttributes.Private;
            typeDeclaration.Members.Add(fieldMember);
            typeDeclaration.Members.Add(new CodeMemberField(typeof(bool), fieldNameSet));

            CodeMemberProperty propertyMember = new CodeMemberProperty();
            propertyMember.Attributes = MemberAttributes.Public | MemberAttributes.Final;
            if (documentation != null)
            {
                GenerateSummaryComment(propertyMember.Comments, documentation);
            }
            propertyMember.Name = propertyName;
            if (type == null)
            {
                propertyMember.Type = new CodeTypeReference(typeName);
            }
            else
            {
                propertyMember.Type = new CodeTypeReference(type);
            }

            if (propertyMember.Name.StartsWith("src"))
            {
                propertyMember.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly"));
            }
            else if (StronglyTypedClasses.multiUppercaseNameRegex.Match(propertyMember.Name).Success)
            {
                propertyMember.CustomAttributes.Add(GetCodeAnalysisSuppressionAttribute("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased"));
            }

            CodeMethodReturnStatement returnStatement = new CodeMethodReturnStatement();
            returnStatement.Expression = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
            propertyMember.GetStatements.Add(returnStatement);

            CodeAssignStatement assignmentStatement = new CodeAssignStatement();
            propertyMember.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldNameSet), new CodePrimitiveExpression(true)));
            assignmentStatement.Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
            assignmentStatement.Right = new CodePropertySetValueReferenceExpression();
            propertyMember.SetStatements.Add(assignmentStatement);

            CodeConditionStatement fieldSetStatement = new CodeConditionStatement();
            fieldSetStatement.Condition = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldNameSet);

            CodeAssignStatement fieldSetAttrStatement = new CodeAssignStatement();
            fieldSetAttrStatement.Left = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldNameSet);
            fieldSetAttrStatement.Right = new CodePrimitiveExpression(true);

            CodeConditionStatement attributeNameMatchStatement = new CodeConditionStatement();
            attributeNameMatchStatement.Condition = new CodeBinaryOperatorExpression(new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), CodeBinaryOperatorType.IdentityEquality, new CodeVariableReferenceExpression("name"));

            string clrTypeName = (string)simpleTypeNamesToClrTypeNames[typeName];
            switch (clrTypeName)
            {
                case "string":
                    if (nestedContent)
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteString", new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                        attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeVariableReferenceExpression("value")));
                    }
                    else
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName)));
                        attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeVariableReferenceExpression("value")));
                    }
                    break;
                case "bool":
                    if (nestedContent)
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteString", new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    }
                    else
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    }
                    attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Convert"), "ToBoolean", new CodeVariableReferenceExpression("value"), new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    break;
                case "int":
                case "long":
                    if (nestedContent)
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteString", new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    }
                    else
                    {
                        fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    }
                    attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Convert"), "ToInt32", new CodeVariableReferenceExpression("value"), new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                    break;
                default:
                    if (typeName == "DateTime")
                    {
                        if (nestedContent)
                        {
                            fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteString", new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                        }
                        else
                        {
                            fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeMethodInvokeExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), "ToString", new CodePrimitiveExpression("yyyy-MM-ddTHH:mm:ss"), new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"), "DateTimeFormat"))));
                        }
                        attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Convert"), "ToDateTime", new CodeVariableReferenceExpression("value"), new CodePropertyReferenceExpression(new CodeTypeReferenceExpression("CultureInfo"), "InvariantCulture"))));
                        break;
                    }

                    if (enumDeclaration == null)
                    {
                        GenerateOutputForEnum(fieldSetStatement, attributeNameMatchStatement, typeNamesToEnumDeclarations[typeName], fieldName, propertyName);
                    }
                    else
                    {
                        GenerateOutputForEnum(fieldSetStatement, attributeNameMatchStatement, enumDeclaration, fieldName, propertyName);
                    }
                    break;
            }

            attributeNameMatchStatement.TrueStatements.Add(fieldSetAttrStatement);

            // TODO: Add throw to falseStatements if required field not set.
            outputXmlMethod.Statements.Add(fieldSetStatement);
            setAttributeMethod.Statements.Add(attributeNameMatchStatement);

            typeDeclaration.Members.Add(propertyMember);
        }

        /// <summary>
        /// Generates output for an enum type. Will generate a switch statement for normal enums, and if statements
        /// for a flags enum.
        /// </summary>
        /// <param name="fieldSetStatement">If statement to add statements to.</param>
        /// <param name="attributeNameMatchStatement">If statement to add statements to.</param>
        /// <param name="enumDeclaration">Enum declaration for this field. Could be locally defined enum or global.</param>
        /// <param name="fieldName">Name of the private field.</param>
        /// <param name="propertyName">Name of the property (and XML attribute).</param>
        private static void GenerateOutputForEnum(CodeConditionStatement fieldSetStatement, CodeConditionStatement attributeNameMatchStatement, EnumDeclaration enumDeclaration, string fieldName, string propertyName)
        {
            CodeTypeDeclaration enumParent = enumsToParseMethodClasses[enumDeclaration];

            if (enumDeclaration.Flags)
            {
                CodeVariableDeclarationStatement outputValueVariable = new CodeVariableDeclarationStatement(typeof(string), "outputValue", new CodeSnippetExpression("\"\""));
                fieldSetStatement.TrueStatements.Add(outputValueVariable);

                foreach (string key in enumDeclaration.Values)
                {
                    CodeConditionStatement enumIfStatement = new CodeConditionStatement();
                    enumIfStatement.Condition = new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), CodeBinaryOperatorType.BitwiseAnd, new CodePropertyReferenceExpression(new CodeSnippetExpression(enumDeclaration.Name), MakeEnumValue(key))), CodeBinaryOperatorType.IdentityInequality, new CodeSnippetExpression("0"));
                    CodeConditionStatement lengthIfStatement = new CodeConditionStatement();
                    lengthIfStatement.Condition = new CodeBinaryOperatorExpression(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("outputValue"), "Length"), CodeBinaryOperatorType.IdentityInequality, new CodeSnippetExpression("0"));
                    lengthIfStatement.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("outputValue"), new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("outputValue"), CodeBinaryOperatorType.Add, new CodeSnippetExpression("\" \""))));
                    enumIfStatement.TrueStatements.Add(lengthIfStatement);
                    enumIfStatement.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("outputValue"), new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression("outputValue"), CodeBinaryOperatorType.Add, new CodeSnippetExpression(String.Concat("\"", key, "\"")))));
                    fieldSetStatement.TrueStatements.Add(enumIfStatement);
                }

                attributeNameMatchStatement.TrueStatements.Add(new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(enumParent.Name),
                    String.Concat("TryParse", enumDeclaration.Name),
                    new CodeVariableReferenceExpression("value"),
                    new CodeDirectionExpression(FieldDirection.Out, new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName))));

                fieldSetStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeSnippetExpression(String.Concat("outputValue"))));
            }
            else
            {
                foreach (string key in enumDeclaration.Values)
                {
                    CodeConditionStatement enumOutStatement = new CodeConditionStatement();
                    enumOutStatement.Condition = new CodeBinaryOperatorExpression(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName), CodeBinaryOperatorType.ValueEquality, new CodePropertyReferenceExpression(new CodeSnippetExpression(enumDeclaration.Name), MakeEnumValue(key)));
                    enumOutStatement.TrueStatements.Add(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("writer"), "WriteAttributeString", new CodeSnippetExpression(String.Concat("\"", propertyName, "\"")), new CodeSnippetExpression(String.Concat("\"", key, "\""))));
                    fieldSetStatement.TrueStatements.Add(enumOutStatement);
                }

                attributeNameMatchStatement.TrueStatements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName),
                    new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(enumParent.Name),
                        String.Concat("Parse", enumDeclaration.Name),
                        new CodeVariableReferenceExpression("value"))));
            }
        }

        /// <summary>
        /// Generates a summary comment.
        /// </summary>
        /// <param name="comments">Comments collection to add the comments to.</param>
        /// <param name="content">Content of the comment.</param>
        private static void GenerateSummaryComment(CodeCommentStatementCollection comments, string content)
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlTextWriter writer = null;

                // create the comment as xml to ensure proper escaping of special xml characters
                try
                {
                    writer = new XmlTextWriter(sw);
                    writer.Indentation = 0;

                    writer.WriteStartElement("summary");
                    writer.WriteString(Environment.NewLine);

                    string nextComment;
                    int newlineIndex = content.IndexOf(Environment.NewLine);
                    int offset = 0;
                    while (newlineIndex != -1)
                    {
                        nextComment = content.Substring(offset, newlineIndex - offset).Trim();
                        writer.WriteString(nextComment);
                        writer.WriteString(Environment.NewLine);
                        offset = newlineIndex + Environment.NewLine.Length;
                        newlineIndex = content.IndexOf(Environment.NewLine, offset);
                    }
                    nextComment = content.Substring(offset).Trim();
                    writer.WriteString(nextComment);
                    writer.WriteString(Environment.NewLine);

                    writer.WriteEndElement();
                }
                finally
                {
                    if (null != writer)
                    {
                        writer.Close();
                    }
                }

                // create the comment statements (one per line of xml)
                using (StringReader sr = new StringReader(sw.ToString()))
                {
                    string line;

                    while (null != (line = sr.ReadLine()))
                    {
                        comments.Add(new CodeCommentStatement(line, true));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the CLR type for simple XML type.
        /// </summary>
        /// <param name="typeName">Plain text name of type.</param>
        /// <returns>Type corresponding to parameter.</returns>
        private static Type GetClrTypeByXmlName(string typeName)
        {
            switch (typeName)
            {
                case "bool":
                    return typeof(bool);
                case "int":
                    return typeof(int);
                case "long":
                    return typeof(long);
                case "string":
                    return typeof(string);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Processes an XmlSchemaSimpleType into corresponding types.
        /// </summary>
        /// <param name="simpleType">XmlSchemaSimpleType to be processed.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code.</param>
        private static void ProcessSimpleType(XmlSchemaSimpleType simpleType, CodeNamespace codeNamespace)
        {
            EnumDeclaration enumDeclaration;
            string simpleTypeName = simpleType.Name;
            string baseTypeName;

            ProcessSimpleType(simpleTypeName, simpleType, false, out enumDeclaration, out baseTypeName);

            simpleTypeNamesToClrTypeNames.Add(simpleTypeName, baseTypeName);

            if (enumDeclaration != null)
            {
                codeNamespace.Types.Add(enumDeclaration.TypeDeclaration);
                typeNamesToEnumDeclarations.Add(simpleTypeName, enumDeclaration);
                AddEnumHelperMethods(enumDeclaration, codeNamespace);
            }
        }

        /// <summary>
        /// Processes an XmlSchemaSimpleType into corresponding code.
        /// </summary>
        /// <param name="simpleTypeName">Name for the type.</param>
        /// <param name="simpleType">XmlSchemaSimpleType to be processed.</param>
        /// <param name="codeNamespace">CodeNamespace to be used when outputting code for global types.</param>
        /// <param name="parentTypeDeclaration">CodeTypeDeclaration to be used when outputting code for nested types.</param>
        /// <param name="outputXmlMethod">Member method for the OutputXml method for nested types.</param>
        /// <param name="setAttributeMethod">Member method for the SetAttribute method for nested types.</param>
        private static void ProcessSimpleType(string simpleTypeName, XmlSchemaSimpleType simpleType, bool nestedType, out EnumDeclaration enumDeclaration, out string baseTypeName)
        {
            enumDeclaration = null;
            baseTypeName = null;

            // XSD supports simpleTypes derived by union, list, or restriction; restrictions can have any
            // combination of pattern, enumeration, length, and more; lists can contain any other simpleType.
            // XsdGen, in contrast, only supports a limited set of values...
            // Unions are weakly supported by just using the first member type
            // restrictions must either be all enumeration or a single pattern, a list must be of a
            // single simpleType which itself is only a restriction of enumeration.
            if (simpleType.Content is XmlSchemaSimpleTypeUnion)
            {
                XmlSchemaSimpleTypeUnion union = simpleType.Content as XmlSchemaSimpleTypeUnion;
                if (union.MemberTypes.Length > 0)
                {
                    baseTypeName = union.MemberTypes[0].Name;
                    return;
                }
                else
                {
                    baseTypeName = "string";
                    return;
                }
            }

            bool listType = false; // XSD lists become [Flag]enums in C#...
            XmlSchemaSimpleTypeList simpleTypeList = simpleType.Content as XmlSchemaSimpleTypeList;
            XmlSchemaSimpleTypeRestriction simpleTypeRestriction = simpleType.Content as XmlSchemaSimpleTypeRestriction;

            if (simpleTypeList != null)
            {
                baseTypeName = simpleTypeList.ItemTypeName.Name;

                if (String.IsNullOrEmpty(baseTypeName))
                {
                    simpleTypeRestriction = simpleTypeList.ItemType.Content as XmlSchemaSimpleTypeRestriction;
                    if (simpleTypeRestriction == null)
                    {
                        string appName = typeof(XsdGen).Assembly.GetName().Name;
                        throw new NotImplementedException(string.Format("{0} does not support a <list> that does not contain a <simpleType>/<restriction>.", appName));
                    }

                    listType = true;
                }
                else
                {
                    // We expect to find an existing enum already declared!
                    EnumDeclaration existingEnumDeclaration = typeNamesToEnumDeclarations[baseTypeName];
                    // TODO: do we need to further alter the Flags setter code because of the helper stuff?
                    // As far as I can tell, this code is never exercised by our existing XSDs!
                    existingEnumDeclaration.SetFlags();
                }
            }

            if (simpleTypeRestriction == null)
            {
                string appName = typeof(XsdGen).Assembly.GetName().Name;
                throw new NotImplementedException(string.Format("{0} does not understand this simpleType!", appName));
            }

            bool foundPattern = false;
            foreach (XmlSchemaFacet facet in simpleTypeRestriction.Facets)
            {
                XmlSchemaEnumerationFacet enumFacet = facet as XmlSchemaEnumerationFacet;
                XmlSchemaPatternFacet patternFacet = facet as XmlSchemaPatternFacet;

                if (enumFacet != null)
                {
                    if (foundPattern)
                    {
                        string appName = typeof(XsdGen).Assembly.GetName().Name;
                        throw new NotImplementedException(string.Format("{0} does not support restrictions containing both <pattern> and <enumeration>.", appName));
                    }

                    if (enumDeclaration == null)
                    {
                        // For nested types, the simple name comes from the attribute name, with "Type" appended
                        // to prevent name collision with the attribute member itself.
                        if (nestedType)
                        {
                            simpleTypeName = String.Concat(simpleTypeName, "Type");
                        }
                        baseTypeName = simpleTypeName;

                        string typeDocumentation = GetDocumentation(simpleType.Annotation);
                        enumDeclaration = new EnumDeclaration(simpleTypeName, typeDocumentation);
                    }

                    string documentation = GetDocumentation(enumFacet.Annotation);
                    enumDeclaration.AddValue(enumFacet.Value, documentation);
                }

                if (patternFacet != null)
                {
                    if (enumDeclaration != null)
                    {
                        string appName = typeof(XsdGen).Assembly.GetName().Name;
                        throw new NotImplementedException(string.Format("{0} does not support restrictions containing both <pattern> and <enumeration>.", appName));
                    }

                    if (foundPattern)
                    {
                        string appName = typeof(XsdGen).Assembly.GetName().Name;
                        throw new NotImplementedException(string.Format("{0} does not support restrictions multiple <pattern> elements.", appName));
                    }

                    foundPattern = true;
                }
            }

            if (enumDeclaration != null && listType)
            {
                enumDeclaration.SetFlags();
            }

            if (String.IsNullOrEmpty(baseTypeName))
            {
                baseTypeName = (string)simpleTypeNamesToClrTypeNames[simpleTypeRestriction.BaseTypeName.Name];
            }
        }

        /// <summary>
        /// Creates an attribute declaration indicating generated code including the tool name and version.
        /// </summary>
        /// <returns>GeneratedCodeAttribute declearation.</returns>
        private static CodeAttributeDeclaration GetGeneratedCodeAttribute()
        {
            AssemblyName generatorAssemblyName = typeof(XsdGen).Assembly.GetName();
            return new CodeAttributeDeclaration("GeneratedCode",
                new CodeAttributeArgument(new CodePrimitiveExpression(generatorAssemblyName.Name)),
                new CodeAttributeArgument(new CodePrimitiveExpression(generatorAssemblyName.Version.ToString())));
        }

        /// <summary>
        /// Creates a code statement to throw an exception if an argument is null.
        /// </summary>
        /// <param name="argumentName">Name of the argument to check.</param>
        /// <param name="nullOrEmpty">True to check for null-or-empty instead of just null</param>
        /// <returns>Code condition statement.</returns>
        private static CodeConditionStatement GetArgumentNullCheckStatement(string argumentName, bool nullOrEmpty)
        {
            CodeConditionStatement conditionStatement = new CodeConditionStatement();
            if (nullOrEmpty)
            {
                conditionStatement.Condition = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("String"), "IsNullOrEmpty"), new CodeVariableReferenceExpression(argumentName));
            }
            else
            {
                conditionStatement.Condition = new CodeBinaryOperatorExpression(new CodePrimitiveExpression(null), CodeBinaryOperatorType.ValueEquality, new CodeVariableReferenceExpression(argumentName));
            }

            conditionStatement.TrueStatements.Add(new CodeThrowExceptionStatement(new CodeObjectCreateExpression("ArgumentNullException", new CodeSnippetExpression(String.Concat("\"", argumentName, "\"")))));
            return conditionStatement;
        }

        /// <summary>
        /// Creates an attribute declaration to suppress a particular code-analysis message.
        /// </summary>
        /// <param name="category">Code analysis category, such as "Microsoft.Design"</param>
        /// <param name="checkId">Code analysis ID number.</param>
        /// <returns>SuppressMessageAttribute declaration.</returns>
        private static CodeAttributeDeclaration GetCodeAnalysisSuppressionAttribute(string category, string checkId)
        {
            return new CodeAttributeDeclaration("SuppressMessage",
                new CodeAttributeArgument(new CodePrimitiveExpression(category)),
                new CodeAttributeArgument(new CodePrimitiveExpression(checkId)));
        }

        /// <summary>
        /// Class representing an enum declaration.
        /// </summary>
        internal class EnumDeclaration
        {
            private string enumTypeName;
            private CodeTypeDeclaration declaration;
            private bool flags;
            private StringCollection enumValues;

            /// <summary>
            /// Creates a new enum declaration with the given name.
            /// </summary>
            /// <param name="enumTypeName">Name of the type for the enum.</param>
            /// <param name="documentation">Documentation for the enum type.</param>
            public EnumDeclaration(string enumTypeName, string documentation)
            {
                this.enumTypeName = enumTypeName;

                this.declaration = new CodeTypeDeclaration(enumTypeName);
                this.declaration.CustomAttributes.Add(GetGeneratedCodeAttribute());
                this.declaration.Attributes = MemberAttributes.Public;
                this.declaration.IsEnum = true;

                if (documentation != null)
                {
                    GenerateSummaryComment(this.declaration.Comments, documentation);
                }

                this.enumValues = new StringCollection();
            }

            public CodeTypeDeclaration TypeDeclaration
            {
                get { return this.declaration; }
            }

            /// <summary>
            /// Gets the enumeration values.
            /// </summary>
            /// <value>The enumeration values.</value>
            public ICollection Values
            {
                get { return this.enumValues; }
            }

            /// <summary>
            /// Gets the enumeration name.
            /// </summary>
            /// <value>The enumeration name.</value>
            public string Name
            {
                get { return this.enumTypeName; }
            }

            /// <summary>
            /// Gets the enumeration flags property.
            /// </summary>
            /// <value>Whether the enumeration is a [Flags] type.</value>
            public bool Flags
            {
                get { return this.flags; }
            }

            /// <summary>
            /// Sets the [Flags] property on the enumeration. Once set, this cannot be undone.
            /// </summary>
            public void SetFlags()
            {
                if (this.flags)
                {
                    return;
                }

                this.flags = true;

                this.declaration.CustomAttributes.Add(new CodeAttributeDeclaration("Flags"));
                SwitchToNoneValue();

                int enumValue = 0;
                foreach (CodeMemberField enumField in this.declaration.Members)
                {
                    enumField.InitExpression = new CodeSnippetExpression(enumValue.ToString());
                    if (enumValue == 0)
                    {
                        enumValue = 1;
                    }
                    else
                    {
                        enumValue *= 2;
                    }
                }
            }

            private void InjectIllegalAndNotSetValues()
            {
                CodeMemberField memberIllegal = new CodeMemberField(typeof(int), "IllegalValue");
                CodeMemberField memberNotSet = new CodeMemberField(typeof(int), "NotSet");

                memberIllegal.InitExpression = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(int)), "MaxValue");
                // Using "-1" for "NotSet" ensure that the next value is zero, which is consistent
                // with older (3.0) behavior.
                memberNotSet.InitExpression = new CodePrimitiveExpression(-1);

                this.declaration.Members.Insert(0, memberIllegal);
                this.declaration.Members.Insert(1, memberNotSet);
            }

            private void SwitchToNoneValue()
            {
                if (this.enumValues.Count > 0)
                {
                    // Remove the "IllegalValue" and "NotSet" values first.
                    this.declaration.Members.RemoveAt(0);
                    this.declaration.Members.RemoveAt(0);

                    CodeMemberField memberNone = new CodeMemberField(typeof(int), "None");
                    memberNone.InitExpression = new CodePrimitiveExpression(0);

                    this.declaration.Members.Insert(0, memberNone);
                }
            }

            /// <summary>
            /// Add a value to the enumeration.
            /// </summary>
            /// <param name="enumValue">The value to add.</param>
            public void AddValue(string enumValue, string documentation)
            {
                if (this.enumValues.Count == 0)
                {
                    InjectIllegalAndNotSetValues();
                }

                this.enumValues.Add(enumValue);
                CodeMemberField memberField = new CodeMemberField(typeof(int), MakeEnumValue(enumValue));
                //memberField.Attributes
                this.declaration.Members.Add(memberField);
                if (documentation != null)
                {
                    GenerateSummaryComment(memberField.Comments, documentation);
                }
            }
        }

        private static void AddEnumHelperMethods(EnumDeclaration enumDeclaration, CodeNamespace codeNamespace)
        {
            if (enumHelperClass == null)
            {
                enumHelperClass = new CodeTypeDeclaration("Enums");
                enumHelperClass.CustomAttributes.Add(GetGeneratedCodeAttribute());
                // The static and final attributes don't seem to get applied, but we'd prefer if they were.
                enumHelperClass.Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final;
                codeNamespace.Types.Add(enumHelperClass);
            }

            AddEnumHelperMethods(enumDeclaration, enumHelperClass);
        }

        private static void AddEnumHelperMethods(EnumDeclaration enumDeclaration, CodeTypeDeclaration parentType)
        {
            CodeTypeReference stringType = new CodeTypeReference(typeof(string));
            CodeTypeReference boolType = new CodeTypeReference(typeof(bool));
            CodeTypeReference enumType = new CodeTypeReference(typeof(Enum));
            CodeTypeReference newEnumType = new CodeTypeReference(enumDeclaration.Name);

            CodePrimitiveExpression falseValue = new CodePrimitiveExpression(false);
            CodePrimitiveExpression trueValue = new CodePrimitiveExpression(true);
            CodeMethodReturnStatement returnFalse = new CodeMethodReturnStatement(falseValue);
            CodeMethodReturnStatement returnTrue = new CodeMethodReturnStatement(trueValue);

            string parseMethodName = String.Concat("Parse", enumDeclaration.Name);
            string tryParseMethodName = String.Concat("TryParse", enumDeclaration.Name);

            CodeFieldReferenceExpression defaultEnumValue = null;
            CodeFieldReferenceExpression illegalEnumValue = null;
            bool addParse = true;
            if (enumDeclaration.Flags)
            {
                defaultEnumValue = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newEnumType), "None");
                illegalEnumValue = defaultEnumValue;
                // Because there's no "IllegalValue" for [Flags] enums, we can't create the Parse()
                // method.  We can still create the TryParse() method, though!
                addParse = false;
            }
            else
            {
                defaultEnumValue = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newEnumType), "NotSet");
                illegalEnumValue = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newEnumType), "IllegalValue");
            }

            if (addParse)
            {
                CodeMemberMethod parseNewEnum = new CodeMemberMethod();
                GenerateSummaryComment(parseNewEnum.Comments, String.Format("Parses a {0} from a string.", enumDeclaration.Name));
                parseNewEnum.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                parseNewEnum.Name = parseMethodName;
                parseNewEnum.ReturnType = newEnumType;
                parseNewEnum.Parameters.Add(new CodeParameterDeclarationExpression(stringType, "value"));

                parseNewEnum.Statements.Add(new CodeVariableDeclarationStatement(newEnumType, "parsedValue"));

                // Just delegate to the TryParse version...
                parseNewEnum.Statements.Add(new CodeMethodInvokeExpression(
                    new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(parentType.Name), tryParseMethodName),
                    new CodeArgumentReferenceExpression("value"),
                    new CodeDirectionExpression(FieldDirection.Out, new CodeVariableReferenceExpression("parsedValue"))));

                parseNewEnum.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("parsedValue")));
                parentType.Members.Add(parseNewEnum);
            }

            CodeMemberMethod tryParseNewEnum = new CodeMemberMethod();
            GenerateSummaryComment(tryParseNewEnum.Comments, String.Format("Tries to parse a {0} from a string.", enumDeclaration.Name));
            tryParseNewEnum.Attributes = MemberAttributes.Public | MemberAttributes.Static;
            tryParseNewEnum.Name = tryParseMethodName;
            tryParseNewEnum.ReturnType = boolType;
            CodeParameterDeclarationExpression valueDeclaration = new CodeParameterDeclarationExpression(stringType, "value");
            CodeParameterDeclarationExpression parsedValueDeclaration = new CodeParameterDeclarationExpression(newEnumType, "parsedValue");
            parsedValueDeclaration.Direction = FieldDirection.Out;
            tryParseNewEnum.Parameters.Add(valueDeclaration);
            tryParseNewEnum.Parameters.Add(parsedValueDeclaration);

            CodeArgumentReferenceExpression value = new CodeArgumentReferenceExpression(valueDeclaration.Name);
            CodeArgumentReferenceExpression parsedValue = new CodeArgumentReferenceExpression(parsedValueDeclaration.Name);

            tryParseNewEnum.Statements.Add(new CodeAssignStatement(parsedValue, defaultEnumValue));

            tryParseNewEnum.Statements.Add(new CodeConditionStatement(
                new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(stringType), "IsNullOrEmpty"), value),
                returnFalse));

            // The structure is similar, but distinct, for regular and flag-style enums. In particular,
            // for a flags-style enum we have to be able to parse multiple values, separated by
            // spaces, and each value is bitwise-OR'd together.
            CodeStatementCollection nestedIfParent = tryParseNewEnum.Statements;
            CodeExpression valueToTest = value;

            // For Flags-style enums, we need to loop over the space-separated values...
            if (enumDeclaration.Flags)
            {
                CodeVariableDeclarationStatement split = new CodeVariableDeclarationStatement(typeof(string[]), "splitValue",
                    new CodeMethodInvokeExpression(value, "Split",
                        new CodeMethodInvokeExpression(new CodePrimitiveExpression(" \t\r\n"), "ToCharArray"),
                        new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(StringSplitOptions)), "RemoveEmptyEntries")));
                tryParseNewEnum.Statements.Add(split);

                CodeIterationStatement flagLoop = new CodeIterationStatement(
                    new CodeVariableDeclarationStatement(typeof(IEnumerator), "enumerator",
                        new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(split.Name), "GetEnumerator")),
                    new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("enumerator"), "MoveNext"),
                    new CodeSnippetStatement(""));
                tryParseNewEnum.Statements.Add(flagLoop);

                CodeVariableDeclarationStatement currentValue = new CodeVariableDeclarationStatement(typeof(string), "currentValue",
                    new CodeCastExpression(stringType,
                        new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("enumerator"), "Current")));
                flagLoop.Statements.Add(currentValue);
                valueToTest = new CodeVariableReferenceExpression(currentValue.Name);

                nestedIfParent = flagLoop.Statements;
            }

            // We can't just Enum.Parse, because some values are also keywords (like 'string', 'int', 'default'),
            // and these get generated as '@'-prefixed values.  Instead, we 'switch' on the value and do it manually.
            // Actually, we if/else, because CodeDom doesn't support 'switch'!  Also, we nest the successive 'if's
            // in order to short-circuit the parsing as soon as there's a match.
            foreach (string enumValue in enumDeclaration.Values)
            {
                CodeFieldReferenceExpression enumValueReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(newEnumType), MakeEnumValue(enumValue));
                CodeConditionStatement ifStatement = new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(new CodePrimitiveExpression(enumValue), CodeBinaryOperatorType.ValueEquality, valueToTest));
                if (enumDeclaration.Flags)
                {
                    ifStatement.TrueStatements.Add(new CodeAssignStatement(parsedValue,
                        new CodeBinaryOperatorExpression(parsedValue, CodeBinaryOperatorType.BitwiseOr, enumValueReference)));
                }
                else
                {
                    ifStatement.TrueStatements.Add(new CodeAssignStatement(parsedValue, enumValueReference));
                }
                nestedIfParent.Add(ifStatement);
                nestedIfParent = ifStatement.FalseStatements;
            }

            // Finally, if we didn't find a match, it's illegal (or none, for flags)!
            nestedIfParent.Add(new CodeAssignStatement(parsedValue, illegalEnumValue));
            nestedIfParent.Add(returnFalse);

            tryParseNewEnum.Statements.Add(returnTrue);

            parentType.Members.Add(tryParseNewEnum);

            enumsToParseMethodClasses.Add(enumDeclaration, parentType);
        }
    }
}
