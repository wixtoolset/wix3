//-------------------------------------------------------------------------------------------------
// <copyright file="NetFxCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the Windows Installer XML Toolset .NET Framework Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetFxCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new NetFxCompiler.
        /// </summary>
        public NetFxCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.netfx.xsd");
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
                case "File":
                    string fileId = contextValues[0];

                    switch (element.LocalName)
                    {
                        case "NativeImage":
                            this.ParseNativeImageElement(element, fileId);
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
        /// Parses a NativeImage element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="fileId">The file identifier of the parent element.</param>
        private void ParseNativeImageElement(XmlNode node, string fileId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string appBaseDirectory = null;
            string assemblyApplication = null;
            int attributes = 0x8; // 32bit is on by default
            int priority = 3;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AppBaseDirectory":
                            appBaseDirectory = this.Core.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == appBaseDirectory.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Directory", appBaseDirectory);
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.Core.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == assemblyApplication.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", assemblyApplication);
                            }
                            break;
                        case "Debug":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        case "Dependencies":
                            if (YesNoType.No == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x2;
                            }
                            break;
                        case "Platform":
                            string platformValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < platformValue.Length)
                            {
                                switch (platformValue)
                                {
                                    case "32bit":
                                        // 0x8 is already on by default
                                        break;
                                    case "64bit":
                                        attributes &= ~0x8;
                                        attributes |= 0x10;
                                        break;
                                    case "all":
                                        attributes |= 0x10;
                                        break;
                                }
                            }
                            break;
                        case "Priority":
                            priority = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3);
                            break;
                        case "Profile":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x4;
                            }
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

            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
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

            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "NetFxScheduleNativeImage");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "NetFxNativeImage");
                row[0] = id;
                row[1] = fileId;
                row[2] = priority;
                row[3] = attributes;
                row[4] = assemblyApplication;
                row[5] = appBaseDirectory;
            }
        }
    }
}
