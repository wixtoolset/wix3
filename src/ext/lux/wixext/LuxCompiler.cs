// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Lux operators.
    /// </summary>
    public enum Operator
    {
        /// <summary>No value has been set (defaults to Equal).</summary>
        NotSet,

        /// <summary>Case-sensitive equality.</summary>
        Equal,

        /// <summary>Case-sensitive inequality.</summary>
        NotEqual,

        /// <summary>Case-insensitive equality.</summary>
        CaseInsensitiveEqual,

        /// <summary>Case-insensitive inequality.</summary>
        CaseInsensitiveNotEqual,
    }

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Lux Extension.
    /// </summary>
    public sealed class LuxCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Initializes a new instance of the LuxCompiler class.
        /// </summary>
        public LuxCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.Lux.xsd");
        }

        /// <summary>
        /// Gets the schema for this extension.
        /// </summary>
        /// <value>Schema for this extension.</value>
        public override XmlSchema Schema
        {
            get { return this.schema; }
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, params string[] contextValues)
        {
            switch (parentElement.LocalName)
            {
                case "Product":
                    switch (element.LocalName)
                    {
                        case "UnitTestRef":
                            this.ParseUnitTestRefElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                    switch (element.LocalName)
                    {
                        case "Mutation":
                            this.ParseMutationElement(element);
                            break;
                        case "UnitTest":
                            this.ParseUnitTestElement(element, null);
                            break;
                        case "UnitTestRef":
                            this.ParseUnitTestRefElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a Mutation element to create Lux unit test mutationss.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseMutationElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string mutation = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            mutation = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            if (String.IsNullOrEmpty(mutation))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "UnitTest":
                                this.ParseUnitTestElement(child, mutation);
                                break;
                            default:
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

        }

        /// <summary>
        /// Parses a UnitTest element to create Lux unit tests.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="args">Used while parsing multi-value property tests to pass values from the parent element.</param>
        private void ParseUnitTestElement(XmlNode node, string mutation, params string[] args)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            bool multiValue = 0 < args.Length;
            string id = null;
            string action = multiValue ? args[0] : null;
            string property = multiValue ? args[1] : null;
            string op = null;
            Operator oper = Operator.NotSet;
            string value = null;
            string expression = null;
            string valueSep = multiValue ? args[2] : null;
            string nameValueSep = multiValue ? args[3] : null;
            string condition = null;
            string index = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "CustomAction":
                        case "Property":
                        case "Expression":
                        case "ValueSeparator":
                        case "NameValueSeparator":
                        if (multiValue)
                        {
                            this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.LocalName, attrib.LocalName));
                        }
                        break;
                    }

                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "CustomAction":
                            action = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Operator":
                            op = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < op.Length)
                            {
                                switch (op)
                                {
                                    case "equal":
                                        oper = Operator.Equal;
                                        break;
                                    case "notEqual":
                                        oper = Operator.NotEqual;
                                        break;
                                    case "caseInsensitiveEqual":
                                        oper = Operator.CaseInsensitiveEqual;
                                        break;
                                    case "caseInsensitiveNotEqual":
                                        oper = Operator.CaseInsensitiveNotEqual;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.LocalName, attrib.LocalName, op, "equal", "notEqual", "caseInsensitiveEqual", "caseInsensitiveNotEqual"));
                                        break;
                                }
                            }
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ValueSeparator":
                            valueSep = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "NameValueSeparator":
                            nameValueSep = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            if (!multiValue)
                            {
                                this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNotNested(sourceLineNumbers, node.LocalName, attrib.LocalName));
                            }
                            index = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            bool isParent = false;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "Condition":
                                // the condition should not be empty
                                condition = CompilerCore.GetConditionInnerText(child);
                                if (null == condition || 0 == condition.Length)
                                {
                                    condition = null;
                                    this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, child.Name));
                                }
                                break;
                            case "Expression":
                                // the expression should not be empty
                                expression = CompilerCore.GetConditionInnerText(child);
                                if (null == expression || 0 == expression.Length)
                                {
                                    expression = null;
                                    this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, child.Name));
                                }
                                break;
                            case "UnitTest":
                                if (multiValue)
                                {
                                    SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
                                    this.Core.OnMessage(LuxErrors.ElementTooDeep(childSourceLineNumbers, child.LocalName, node.LocalName));
                                }

                                this.ParseUnitTestElement(child, mutation, action, property, valueSep, nameValueSep);
                                isParent = true;
                                break;
                            default:
                                this.Core.UnexpectedElement(node, child);
                                break;
                        }
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            if (isParent)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    this.Core.OnMessage(LuxErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.LocalName, "Value"));
                }
            }
            else
            {
                // the children generate multi-value unit test rows; the parent doesn't generate anything

                if (!String.IsNullOrEmpty(property) && String.IsNullOrEmpty(value))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.LocalName, "Property", "Value"));
                }

                if (!String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(expression))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.LocalName, "Property", "Expression"));
                }

                if (multiValue && String.IsNullOrEmpty(valueSep) && String.IsNullOrEmpty(nameValueSep))
                {
                    this.Core.OnMessage(LuxErrors.MissingRequiredParentAttribute(sourceLineNumbers, node.LocalName, "ValueSeparator", "NameValueSeparator"));
                }

                if (!String.IsNullOrEmpty(valueSep) && !String.IsNullOrEmpty(nameValueSep))
                {
                    this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.LocalName, "ValueSeparator", "NameValueSeparator"));
                }

                if (!this.Core.EncounteredError)
                {
                    if (String.IsNullOrEmpty(id))
                    {
                        id = this.Core.GenerateIdentifier("lux", action, property, index, condition, mutation);
                    }

                    if (Operator.NotSet == oper)
                    {
                        oper = Operator.Equal;
                    }

                    Row row = this.Core.CreateRow(sourceLineNumbers, "WixUnitTest");
                    row[0] = id;
                    row[1] = action;
                    row[2] = property;
                    row[3] = (int)oper;
                    row[4] = value;
                    row[5] = expression;
                    row[6] = condition;
                    row[7] = valueSep;
                    row[8] = nameValueSep;
                    row[9] = index;
                    if (!string.IsNullOrEmpty(mutation))
                    {
                        row[10] = mutation;
                    }

                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", action);
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixRunImmediateUnitTests");
                }
            }
        }

        /// <summary>
        /// Parses a UnitTestRef element to reference Lux unit tests.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseUnitTestRefElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            // find unexpected child elements
            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            if (String.IsNullOrEmpty(id))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "WixUnitTest", id);
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixRunImmediateUnitTests");
            }
        }
    }
}
