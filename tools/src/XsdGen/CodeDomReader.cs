//-------------------------------------------------------------------------------------------------
// <copyright file="CodeDomReader.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Interface for generated schema elements.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Serialize
{
    using System;
    using System.Collections;
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
                        throw new InvalidOperationException("Multiple root elements found in file.");
                    }

                    schemaElement = this.CreateObjectFromElement(element);
                    this.ParseObjectFromElement(schemaElement, element);
                }
            }
            return schemaElement;
        }

        /// <summary>
        /// Parses an ISchemaElement from the XmlElement.
        /// </summary>
        /// <param name="schemaElement">ISchemaElement to fill in.</param>
        /// <param name="element">XmlElement to parse from.</param>
        private void ParseObjectFromElement(ISchemaElement schemaElement, XmlElement element)
        {
            foreach (XmlAttribute attribute in element.Attributes)
            {
                this.SetAttributeOnObject(schemaElement, attribute.LocalName, attribute.Value);
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
                        throw new InvalidOperationException("ISchemaElement with name " + element.LocalName + " does not implement ICreateChildren.");
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
                            throw new InvalidOperationException("XmlElement with name " + childElement.LocalName + " does not have a corresponding ISchemaElement.");
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
                        this.SetAttributeOnObject(schemaElement, "Content", childText.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Sets an attribute on an ISchemaElement.
        /// </summary>
        /// <param name="schemaElement">Schema element to set attribute on.</param>
        /// <param name="name">Name of the attribute to set.</param>
        /// <param name="value">Value to set on the attribute.</param>
        private void SetAttributeOnObject(ISchemaElement schemaElement, string name, string value)
        {
            ISetAttributes setAttributes = schemaElement as ISetAttributes;
            if (setAttributes == null)
            {
                throw new InvalidOperationException("ISchemaElement with name " 
                    + schemaElement.GetType().FullName.ToString() 
                    + " does not implement ISetAttributes.");
            }
            else
            {
                setAttributes.SetAttribute(name, value);
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
                    if (type.FullName.EndsWith(element.LocalName) 
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
