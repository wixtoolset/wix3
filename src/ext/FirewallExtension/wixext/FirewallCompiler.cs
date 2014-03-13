//-------------------------------------------------------------------------------------------------
// <copyright file="FirewallCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the Windows Installer XML Toolset Firewall Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new FirewallCompiler.
        /// </summary>
        public FirewallCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.firewall.xsd");
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
                    string fileComponentId = contextValues[1];

                    switch (element.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(element, fileComponentId, fileId);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    string componentId = contextValues[0];

                    switch (element.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(element, componentId, null);
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
        /// Parses a FirewallException element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this firewall exception.</param>
        /// <param name="fileId">The file identifier of the parent element (null if nested under Component).</param>
        private void ParseFirewallExceptionElement(XmlNode node, string componentId, string fileId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            int attributes = 0;
            string file = null;
            string program = null;
            string port = null;
            string protocolValue = null;
            int protocol = CompilerCore.IntegerNotSet;
            string profileValue = null;
            int profile = CompilerCore.IntegerNotSet;
            string scope = null;
            string remoteAddresses = null;
            string description = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            if (null != fileId)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.LocalName, "File", "File"));
                            }
                            else
                            {
                                file = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "IgnoreFailure":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1; // feaIgnoreFailures
                            }
                            break;
                        case "Program":
                            if (null != fileId)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.LocalName, "Program", "File"));
                            }
                            else
                            {
                                program = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "Port":
                            port = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Protocol":
                            protocolValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (protocolValue)
                            {
                                case "tcp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                                    break;
                                case "udp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_UDP;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.LocalName, "Protocol", protocolValue, "tcp", "udp"));
                                    break;
                            }
                            break;
                        case "Scope":
                            scope = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (scope)
                            {
                                case "any":
                                    remoteAddresses = "*";
                                    break;
                                case "localSubnet":
                                    remoteAddresses = "LocalSubnet";
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.LocalName, "Scope", scope, "any", "localSubnet"));
                                    break;
                            }
                            break;
                        case "Profile":
                            profileValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (profileValue)
                            {
                                case "domain":
                                    profile = FirewallConstants.NET_FW_PROFILE2_DOMAIN;
                                    break;
                                case "private":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PRIVATE;
                                    break;
                                case "public":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PUBLIC;
                                    break;
                                case "all":
                                    profile = FirewallConstants.NET_FW_PROFILE2_ALL;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.LocalName, "Profile", profileValue, "domain", "private", "public", "all"));
                                    break;
                            }
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // parse RemoteAddress children
            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.Schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "RemoteAddress":
                                if (null != scope)
                                {
                                    this.Core.OnMessage(FirewallErrors.IllegalRemoteAddressWithScopeAttribute(sourceLineNumbers));
                                }
                                else
                                {
                                    this.ParseRemoteAddressElement(child, ref remoteAddresses);
                                }
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

            // Id and Name are required
            if (null == id)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Id"));
            }

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            // Scope or child RemoteAddress(es) are required
            if (null == remoteAddresses)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributeOrElement(sourceLineNumbers, node.Name, "Scope", "RemoteAddress"));
            }

            // can't have both Program and File
            if (null != program && null != file)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.LocalName, "File", "Program"));
            }

            // must be nested under File, have File or Program attributes, or have Port attribute
            if (String.IsNullOrEmpty(fileId) && String.IsNullOrEmpty(file) && String.IsNullOrEmpty(program) && String.IsNullOrEmpty(port))
            {
                this.Core.OnMessage(FirewallErrors.NoExceptionSpecified(sourceLineNumbers));
            }

            if (!this.Core.EncounteredError)
            {
                // at this point, File attribute and File parent element are treated the same
                if (null != file)
                {
                    fileId = file;
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixFirewallException");
                row[0] = id;
                row[1] = name;
                row[2] = remoteAddresses;

                if (!String.IsNullOrEmpty(port))
                {
                    row[3] = port;
                    
                    if (CompilerCore.IntegerNotSet == protocol)
                    {
                        // default protocol is "TCP"
                        protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                    }
                }

                if (CompilerCore.IntegerNotSet != protocol)
                {
                    row[4] =  protocol;
                }

                if (!String.IsNullOrEmpty(fileId))
                {
                    row[5] = String.Format(CultureInfo.InvariantCulture, "[#{0}]", fileId);
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", fileId);
                }
                else if (!String.IsNullOrEmpty(program))
                {
                    row[5] = program;
                }

                if (CompilerCore.IntegerNotSet != attributes)
                {
                    row[6] = attributes;
                }

                // Default is "all"
                row[7] = CompilerCore.IntegerNotSet == profile ? FirewallConstants.NET_FW_PROFILE2_ALL : profile;

                row[8] = componentId;

                row[9] = description;

                if (this.Core.CurrentPlatform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsInstall_ARM");
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsUninstall_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsInstall");
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixSchedFirewallExceptionsUninstall");
                }
            }
        }

        /// <summary>
        /// Parses a RemoteAddress element
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseRemoteAddressElement(XmlNode node, ref string remoteAddresses)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // no attributes
            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                }
                else
                {
                    this.Core.UnsupportedExtensionAttribute(sourceLineNumbers, attrib);
                }
            }

            // no children
            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.Schema.TargetNamespace)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }
                    else
                    {
                        this.Core.UnsupportedExtensionElement(node, child);
                    }
                }
            }

            string address = CompilerCore.GetTrimmedInnerText(node);
            if (String.IsNullOrEmpty(address))
            {
                this.Core.OnMessage(FirewallErrors.IllegalEmptyRemoteAddress(sourceLineNumbers));
            }
            else
            {
                if (String.IsNullOrEmpty(remoteAddresses))
                {
                    remoteAddresses = address;
                }
                else
                {
                    remoteAddresses = String.Concat(remoteAddresses, ",", address);
                }
            }
        }

    }
}
