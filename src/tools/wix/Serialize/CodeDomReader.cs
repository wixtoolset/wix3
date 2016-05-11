// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Serialize
{
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// Class used for reading XML files in to the CodeDom.
    /// </summary>
    public class CodeDomReader
    {
        private Assembly[] assemblies;

        /// <summary>
        /// Creates a new CodeDomReader, using the current assembly.
        /// </summary>
        public CodeDomReader()
        {
            this.assemblies = new Assembly[] { Assembly.GetExecutingAssembly() };
        }

        /// <summary>
        /// Creates a new CodeDomReader, and takes in a list of assemblies in which to
        /// look for elements.
        /// </summary>
        /// <param name="assemblies">Assemblies in which to look for types that correspond 
        /// to elements.</param>
        public CodeDomReader(Assembly[] assemblies)
        {
            this.assemblies = assemblies;
        }

        /// <summary>
        /// Loads an XML file into a strongly-typed code dom.
        /// </summary>
        /// <param name="filePath">File to load into the code dom.</param>
        /// <returns>The strongly-typed object at the root of the tree.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public ISchemaElement Load(string filePath)
        {
            XmlDocument document = new XmlDocument();
            document.Load(filePath);
            ISchemaElement schemaElement = null;

            foreach (XmlNode node in document.ChildNodes)
            {
                XmlElement element = node as XmlElement;
                if (element != null)
                {
                    if (schemaElement != null)
                    {
                        throw new InvalidOperationException(WixStrings.EXP_MultipleRootElementsFoundInFile);
                    }

                    schemaElement = this.CreateObjectFromElement(element);
                    this.ParseObjectFromElement(schemaElement, element);
                }
            }
            return schemaElement;
        }

        /// <summary>
        /// Sets an attribute on an ISchemaElement.
        /// </summary>
        /// <param name="schemaElement">Schema element to set attribute on.</param>
        /// <param name="name">Name of the attribute to set.</param>
        /// <param name="value">Value to set on the attribute.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        private static void SetAttributeOnObject(ISchemaElement schemaElement, string name, string value)
        {
            ISetAttributes setAttributes = schemaElement as ISetAttributes;
            if (setAttributes == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_ISchemaElementDoesnotImplementISetAttribute, schemaElement.GetType().FullName));
            }
            else
            {
                setAttributes.SetAttribute(name, value);
            }
        }

        /// <summary>
        /// Parses an ISchemaElement from the XmlElement.
        /// </summary>
        /// <param name="schemaElement">ISchemaElement to fill in.</param>
        /// <param name="element">XmlElement to parse from.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        private void ParseObjectFromElement(ISchemaElement schemaElement, XmlElement element)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                SetAttributeOnObject(schemaElement, attribute.LocalName, attribute.Value);
            }

            foreach (XmlNode node in element.ChildNodes)
            {
                XmlElement childElement = node as XmlElement;
                if (childElement != null)
                {
                    ISchemaElement childSchemaElement = null;
                    ICreateChildren createChildren = schemaElement as ICreateChildren;
                    if (createChildren == null)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_ISchemaElementDoesnotImplementICreateChildren, element.LocalName));
                    }
                    else
                    {
                        childSchemaElement = createChildren.CreateChild(childElement.LocalName);
                    }

                    if (childSchemaElement == null)
                    {
                        childSchemaElement = this.CreateObjectFromElement(childElement);
                        if (childSchemaElement == null)
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_XmlElementDoesnotHaveISchemaElement, childElement.LocalName));
                        }
                    }

                    this.ParseObjectFromElement(childSchemaElement, childElement);
                    IParentElement parentElement = (IParentElement)schemaElement;
                    parentElement.AddChild(childSchemaElement);
                }
                else
                {
                    XmlText childText = node as XmlText;
                    if (childText != null)
                    {
                        SetAttributeOnObject(schemaElement, "Content", childText.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Creates an object from an XML element by digging through the assembly list.
        /// </summary>
        /// <param name="element">XML Element to create an ISchemaElement from.</param>
        /// <returns>A constructed ISchemaElement.</returns>
        private ISchemaElement CreateObjectFromElement(XmlElement element)
        {
            ISchemaElement schemaElement = null;
            foreach (Assembly assembly in this.assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.FullName.EndsWith(element.LocalName, StringComparison.Ordinal) 
                        && typeof(ISchemaElement).IsAssignableFrom(type))
                    {
                        schemaElement = (ISchemaElement)Activator.CreateInstance(type);
                    }
                }
            }
            return schemaElement;
        }
    }
}
