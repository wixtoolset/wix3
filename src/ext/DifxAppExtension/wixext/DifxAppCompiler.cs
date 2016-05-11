// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppCompiler : CompilerExtension
    {
        private XmlSchema schema;
        private Hashtable components;

        /// <summary>
        /// Instantiate a new DifxAppCompiler.
        /// </summary>
        public DifxAppCompiler()
        {
            this.components = new Hashtable();
        }

        /// <summary>
        /// Gets the schema for this extension.
        /// </summary>
        /// <value>Schema for this extension.</value>
        public override XmlSchema Schema
        {
            get
            {
                if (null == this.schema)
                {
                    this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.difxapp.xsd");
                }

                return this.schema;
            }
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
                case "Component":
                    string componentId = contextValues[0];
                    string directoryId = contextValues[1];

                    switch (element.LocalName)
                    {
                        case "Driver":
                            this.ParseDriverElement(element, componentId);
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
        /// Parses a Driver element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseDriverElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int attributes = 0;
            int sequence = CompilerCore.IntegerNotSet;

            // check the number of times a Driver element has been nested under this Component element
            if (null != componentId)
            {
                if (this.components.Contains(componentId))
                {
                    this.Core.OnMessage(WixErrors.TooManyElements(sourceLineNumbers, "Component", node.Name, 1));
                }
                else
                {
                    this.components.Add(componentId, null);
                }
            }

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "AddRemovePrograms":
                        if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x4;
                        }
                        break;
                    case "DeleteFiles":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x10;
                        }
                        break;
                    case "ForceInstall":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x1;
                        }
                        break;
                    case "Legacy":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x8;
                        }
                        break;
                    case "PlugAndPlayPrompt":
                        if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= 0x2;
                        }
                        break;
                    case "Sequence":
                        sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "MsiDriverPackages");
                row[0] = componentId;
                row[1] = attributes;
                if (CompilerCore.IntegerNotSet != sequence)
                {
                    row[2] = sequence;
                }

                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "MsiProcessDrivers");
            }
        }
    }
}
