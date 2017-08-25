// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// The compiler for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new IIsCompiler.
        /// </summary>
        public IIsCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.iis.xsd");
        }

        /// <summary>
        /// Types of objects that custom HTTP Headers can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eHttpHeaderParentType in scahttpheader.h.</remarks>
        private enum HttpHeaderParentType
        {
            /// <summary>Custom HTTP Header is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,
            /// <summary>Custom HTTP Header is to be applied to a Web Site.</summary>
            WebSite = 2,
        }

        /// <summary>
        /// Types of objects that MimeMaps can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eMimeMapParentType in scamimemap.h.</remarks>
        private enum MimeMapParentType
        {
            /// <summary>MimeMap is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,
            WebSite = 2,
        }

        /// <summary>
        /// Types of objects that custom WebErrors can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eWebErrorParentType in scaweberror.h.</remarks>
        private enum WebErrorParentType
        {
            /// <summary>Custom WebError is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,

            /// <summary>Custom WebError is to be applied to a Web Site.</summary>
            WebSite = 2,
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
                case "Component":
                    string componentId = contextValues[0];
                    string directoryId = contextValues[1];

                    switch (element.LocalName)
                    {
                        case "Certificate":
                            this.ParseCertificateElement(element, componentId);
                            break;
                        case "WebAppPool":
                            this.ParseWebAppPoolElement(element, componentId);
                            break;
                        case "WebDir":
                            this.ParseWebDirElement(element, componentId, null);
                            break;
                        case "WebFilter":
                            this.ParseWebFilterElement(element, componentId, null);
                            break;
                        case "WebProperty":
                            this.ParseWebPropertyElement(element, componentId);
                            break;
                        case "WebServiceExtension":
                            this.ParseWebServiceExtensionElement(element, componentId);
                            break;
                        case "WebSite":
                            this.ParseWebSiteElement(element, componentId);
                            break;
                        case "WebVirtualDir":
                            this.ParseWebVirtualDirElement(element, componentId, null, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.LocalName)
                    {
                        case "WebApplication":
                            this.ParseWebApplicationElement(element);
                            break;
                        case "WebAppPool":
                            this.ParseWebAppPoolElement(element, null);
                            break;
                        case "WebDirProperties":
                            this.ParseWebDirPropertiesElement(element);
                            break;
                        case "WebLog":
                            this.ParseWebLogElement(element);
                            break;
                        case "WebSite":
                            this.ParseWebSiteElement(element, null);
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
        /// Parses a certificate element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseCertificateElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            string binaryKey = null;
            string certificatePath = null;
            string name = null;
            string pfxPassword = null;
            int storeLocation = 0;
            string storeName = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "BinaryKey":
                            attributes |= 2; // SCA_CERT_ATTRIBUTE_BINARYDATA
                            binaryKey = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Binary", binaryKey);
                            break;
                        case "CertificatePath":
                            certificatePath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Overwrite":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4; // SCA_CERT_ATTRIBUTE_OVERWRITE
                            }
                            else
                            {
                                attributes &= ~4; // SCA_CERT_ATTRIBUTE_OVERWRITE
                            }
                            break;
                        case "PFXPassword":
                            pfxPassword = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Request":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1; // SCA_CERT_ATTRIBUTE_REQUEST
                            }
                            else
                            {
                                attributes &= ~1; // SCA_CERT_ATTRIBUTE_REQUEST
                            }
                            break;
                        case "StoreLocation":
                            string storeLocationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < storeLocationValue.Length)
                            {
                                switch (storeLocationValue)
                                {
                                    case "currentUser":
                                        storeLocation = 1; // SCA_CERTSYSTEMSTORE_CURRENTUSER
                                        break;
                                    case "localMachine":
                                        storeLocation = 2; // SCA_CERTSYSTEMSTORE_LOCALMACHINE
                                        break;
                                    default:
                                        storeLocation = -1;
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "StoreLocation", storeLocationValue, "currentUser", "localMachine"));
                                        break;
                                }
                            }
                            break;
                        case "StoreName":
                            string storeNameValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < storeNameValue.Length)
                            {
                                switch (storeNameValue)
                                {
                                    case "ca":
                                        storeName = "CA";
                                        break;
                                    case "my":
                                    case "personal":
                                        storeName = "MY";
                                        break;
                                    case "request":
                                        storeName = "REQUEST";
                                        break;
                                    case "root":
                                        storeName = "Root";
                                        break;
                                    case "otherPeople":
                                        storeName = "AddressBook";
                                        break;
                                    case "trustedPeople":
                                        storeName = "TrustedPeople";
                                        break;
                                    case "trustedPublisher":
                                        storeName = "TrustedPublisher";
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "StoreName", storeNameValue, "ca", "my", "request", "root", "otherPeople", "trustedPeople", "trustedPublisher"));
                                        break;
                                }
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            if (0 == storeLocation)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "StoreLocation"));
            }

            if (null == storeName)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "StoreName"));
            }

            if (null != binaryKey && null != certificatePath)
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "BinaryKey", "CertificatePath", certificatePath));
            }
            else if (null == binaryKey && null == certificatePath)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttributes(sourceLineNumbers, node.Name, "BinaryKey", "CertificatePath"));
            }

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

            // Reference InstallCertificates and UninstallCertificates since nothing will happen without them
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "InstallCertificates");
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "UninstallCertificates");
            this.Core.EnsureTable(sourceLineNumbers, "CertificateHash"); // Certificate CustomActions require the CertificateHash table

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "Certificate");
                row[0] = id;
                row[1] = componentId;
                row[2] = name;
                row[3] = storeLocation;
                row[4] = storeName;
                row[5] = attributes;
                row[6] = binaryKey;
                row[7] = certificatePath;
                row[8] = pfxPassword;
            }
        }

        /// <summary>
        /// Parses a CertificateRef extension element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="webId">Identifier for parent web site.</param>
        private void ParseCertificateRefElement(XmlNode node, string webId)
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
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Certificate", id);
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

            if (!this.Core.EncounteredError)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Certificate", id);

                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebSiteCertificates");
                row[0] = webId;
                row[1] = id;
            }
        }

        /// <summary>
        /// Parses a mime map element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentId">Identifier for parent symbol.</param>
        /// <param name="parentType">Type that parentId refers to.</param>
        private void ParseMimeMapElement(XmlNode node, string parentId, MimeMapParentType parentType)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string extension = null;
            string type = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            extension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == extension)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Extension"));
            }
            else if (0 < extension.Length)
            {
                if (!extension.StartsWith(".", StringComparison.Ordinal))
                {
                    this.Core.OnMessage(IIsErrors.MimeMapExtensionMissingPeriod(sourceLineNumbers, node.Name, "Extension", extension));
                }
            }

            if (null == type)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
            }

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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsMimeMap");
                row[0] = id;
                row[1] = (int)parentType;
                row[2] = parentId;
                row[3] = type;
                row[4] = extension;
            }
        }

        /// <summary>
        /// Parses a recycle time element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Recycle time value.</returns>
        private string ParseRecycleTimeElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string value = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == value)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value"));
            }

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

            return value;
        }

        /// <summary>
        /// Parses a web address element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentWeb">Identifier of parent web site.</param>
        /// <returns>Identifier for web address.</returns>
        private string ParseWebAddressElement(XmlNode node, string parentWeb)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string header = null;
            string ip = null;
            string port = null;
            bool secure = false;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Header":
                            header = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IP":
                            ip = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Port":
                            port = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Secure":
                            secure = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == port)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Port"));
            }

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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebAddress");
                row[0] = id;
                row[1] = parentWeb;
                row[2] = ip;
                row[3] = port;
                row[4] = header;
                row[5] = secure ? 1 : 0;
            }

            return id;
        }

        /// <summary>
        /// Parses a web application element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>Identifier for web application.</returns>
        private string ParseWebApplicationElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            YesNoDefaultType allowSessions = YesNoDefaultType.Default;
            string appPool = null;
            YesNoDefaultType buffer = YesNoDefaultType.Default;
            YesNoDefaultType clientDebugging = YesNoDefaultType.Default;
            string defaultScript = null;
            int isolation = 0;
            string name = null;
            YesNoDefaultType parentPaths = YesNoDefaultType.Default;
            int scriptTimeout = CompilerCore.IntegerNotSet;
            int sessionTimeout = CompilerCore.IntegerNotSet;
            YesNoDefaultType serverDebugging = YesNoDefaultType.Default;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AllowSessions":
                            allowSessions = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Buffer":
                            buffer = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ClientDebugging":
                            clientDebugging = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultScript":
                            defaultScript = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < defaultScript.Length)
                            {
                                switch (defaultScript)
                                {
                                    case "JScript":
                                    case "VBScript":
                                        // these are valid values
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, defaultScript, "JScript", "VBScript"));
                                        break;
                                }
                            }
                            break;
                        case "Isolation":
                            string isolationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < isolationValue.Length)
                            {
                                switch (isolationValue)
                                {
                                    case "low":
                                        isolation = 0;
                                        break;
                                    case "medium":
                                        isolation = 2;
                                        break;
                                    case "high":
                                        isolation = 1;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, isolationValue, "low", "medium", "high"));
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParentPaths":
                            parentPaths = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ScriptTimeout":
                            scriptTimeout = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ServerDebugging":
                            serverDebugging = this.Core.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "SessionTimeout":
                            sessionTimeout = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "WebAppPool":
                            appPool = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsAppPool", appPool);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }
            else if (-1 != name.IndexOf("\\", StringComparison.Ordinal))
            {
                this.Core.OnMessage(IIsErrors.IllegalCharacterInAttributeValue(sourceLineNumbers, node.Name, "Name", name, '\\'));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "WebApplicationExtension":
                                this.ParseWebApplicationExtensionElement(child, id);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebApplication");
                row[0] = id;
                row[1] = name;
                row[2] = isolation;
                if (YesNoDefaultType.Default != allowSessions)
                {
                    row[3] = YesNoDefaultType.Yes == allowSessions ? 1 : 0;
                }

                if (CompilerCore.IntegerNotSet != sessionTimeout)
                {
                    row[4] = sessionTimeout;
                }

                if (YesNoDefaultType.Default != buffer)
                {
                    row[5] = YesNoDefaultType.Yes == buffer ? 1 : 0;
                }

                if (YesNoDefaultType.Default != parentPaths)
                {
                    row[6] = YesNoDefaultType.Yes == parentPaths ? 1 : 0;
                }
                row[7] = defaultScript;
                if (CompilerCore.IntegerNotSet != scriptTimeout)
                {
                    row[8] = scriptTimeout;
                }

                if (YesNoDefaultType.Default != serverDebugging)
                {
                    row[9] = YesNoDefaultType.Yes == serverDebugging ? 1 : 0;
                }

                if (YesNoDefaultType.Default != clientDebugging)
                {
                    row[10] = YesNoDefaultType.Yes == clientDebugging ? 1 : 0;
                }
                row[11] = appPool;
            }

            return id;
        }

        /// <summary>
        /// Parses a web application extension element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="application">Identifier for parent web application.</param>
        private void ParseWebApplicationExtensionElement(XmlNode node, string application)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int attributes = 0;
            string executable = null;
            string extension = null;
            string verbs = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "CheckPath":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4;
                            }
                            else
                            {
                                attributes &= ~4;
                            }
                            break;
                        case "Executable":
                            executable = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            extension = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Script":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1;
                            }
                            else
                            {
                                attributes &= ~1;
                            }
                            break;
                        case "Verbs":
                            verbs = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebApplicationExtension");
                row[0] = application;
                row[1] = extension;
                row[2] = verbs;
                row[3] = executable;
                if (0 < attributes)
                {
                    row[4] = attributes;
                }
            }
        }

        /// <summary>
        /// Parses web application pool element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseWebAppPoolElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            int cpuAction = CompilerCore.IntegerNotSet;
            string cpuMon = null;
            int idleTimeout = CompilerCore.IntegerNotSet;
            int maxCpuUsage = 0;
            int maxWorkerProcs = CompilerCore.IntegerNotSet;
            string managedRuntimeVersion = null;
            string managedPipelineMode = null;
            string name = null;
            int privateMemory = CompilerCore.IntegerNotSet;
            int queueLimit = CompilerCore.IntegerNotSet;
            int recycleMinutes = CompilerCore.IntegerNotSet;
            int recycleRequests = CompilerCore.IntegerNotSet;
            string recycleTimes = null;
            int refreshCpu = CompilerCore.IntegerNotSet;
            string user = null;
            int virtualMemory = CompilerCore.IntegerNotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "CpuAction":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            string cpuActionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < cpuActionValue.Length)
                            {
                                switch (cpuActionValue)
                                {
                                    case "shutdown":
                                        cpuAction = 1;
                                        break;
                                    case "none":
                                        cpuAction = 0;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, cpuActionValue, "shutdown", "none"));
                                        break;
                                }
                            }
                            break;
                        case "Identity":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            string identityValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < identityValue.Length)
                            {
                                switch (identityValue)
                                {
                                    case "networkService":
                                        attributes |= 1;
                                        break;
                                    case "localService":
                                        attributes |= 2;
                                        break;
                                    case "localSystem":
                                        attributes |= 4;
                                        break;
                                    case "other":
                                        attributes |= 8;
                                        break;
                                    case "applicationPoolIdentity":
                                        attributes |= 0x10;
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, identityValue, "networkService", "localService", "localSystem", "other", "applicationPoolIdentity"));
                                        break;
                                }
                            }
                            break;
                        case "IdleTimeout":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            idleTimeout = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ManagedPipelineMode":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            managedPipelineMode = this.Core.GetAttributeValue(sourceLineNumbers, attrib);


                            if (!String.IsNullOrEmpty(managedPipelineMode))
                            {
                                switch (managedPipelineMode)
                                {
                                    // In 3.5 we allowed lower case values (per camel case enum style), we now use formatted fields, 
                                    // so the value needs to match exactly what we pass in to IIS which uses pascal case.
                                    case "classic":
                                        managedPipelineMode = "Classic";
                                        break;
                                    case "integrated":
                                        managedPipelineMode = "Integrated";
                                        break;
                                    case "Classic":
                                        break;
                                    case "Integrated":
                                        break;
                                    default:
                                        if (!CompilerCore.ContainsProperty(managedPipelineMode))
                                        {
                                            this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, managedPipelineMode, "Classic", "Integrated"));
                                        }
                                        break;
                                }
                            }

                            break;
                        case "ManagedRuntimeVersion":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            managedRuntimeVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);

                            switch (managedRuntimeVersion)
                            {
                                case "v2.0":
                                    break;

                                case "v4.0":
                                    break;

                                case "No Managed Code":
                                    break;

                                default:
                                    if (!CompilerCore.ContainsProperty(managedRuntimeVersion))
                                    {
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, attrib.Name, managedRuntimeVersion, "v2.0", "v4.0", "No Managed Code"));
                                    }
                                    break;
                            }
                            
                            break;
                        case "MaxCpuUsage":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            maxCpuUsage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;
                        case "MaxWorkerProcesses":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            maxWorkerProcs = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PrivateMemory":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            privateMemory = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 4294967);
                            break;
                        case "QueueLimit":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            queueLimit = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RecycleMinutes":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            recycleMinutes = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RecycleRequests":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            recycleRequests = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RefreshCpu":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            refreshCpu = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "User":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            user = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                            break;
                        case "VirtualMemory":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            virtualMemory = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 4294967);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            if (null == user && 8 == (attributes & 0x1F))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "User", "Identity", "other"));
            }

            if (null != user && 8 != (attributes & 0x1F))
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, node.Name, "User", user, "Identity", "other"));
            }

            cpuMon = maxCpuUsage.ToString(CultureInfo.InvariantCulture.NumberFormat);
            if (CompilerCore.IntegerNotSet != refreshCpu)
            {
                cpuMon = String.Concat(cpuMon, ",", refreshCpu.ToString(CultureInfo.InvariantCulture.NumberFormat));
                if (CompilerCore.IntegerNotSet != cpuAction)
                {
                    cpuMon = String.Concat(cpuMon, ",", cpuAction.ToString(CultureInfo.InvariantCulture.NumberFormat));
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "RecycleTime":
                                if (null == componentId)
                                {
                                    SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, node.Name));
                                }

                                if (null == recycleTimes)
                                {
                                    recycleTimes = this.ParseRecycleTimeElement(child);
                                }
                                else
                                {
                                    recycleTimes = String.Concat(recycleTimes, ",", this.ParseRecycleTimeElement(child));
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

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsAppPool");
                row[0] = id;
                row[1] = name;
                row[2] = componentId;
                row[3] = attributes;
                row[4] = user;
                if (CompilerCore.IntegerNotSet != recycleMinutes)
                {
                    row[5] = recycleMinutes;
                }

                if (CompilerCore.IntegerNotSet != recycleRequests)
                {
                    row[6] = recycleRequests;
                }
                row[7] = recycleTimes;
                if (CompilerCore.IntegerNotSet != idleTimeout)
                {
                    row[8] = idleTimeout;
                }

                if (CompilerCore.IntegerNotSet != queueLimit)
                {
                    row[9] = queueLimit;
                }
                row[10] = cpuMon;
                if (CompilerCore.IntegerNotSet != maxWorkerProcs)
                {
                    row[11] = maxWorkerProcs;
                }

                if (CompilerCore.IntegerNotSet != virtualMemory)
                {
                    row[12] = virtualMemory;
                }

                if (CompilerCore.IntegerNotSet != privateMemory)
                {
                    row[13] = privateMemory;
                }
                row[14] = managedRuntimeVersion;
                row[15] = managedPipelineMode;
            }
        }

        /// <summary>
        /// Parses a web directory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="parentWeb">Optional identifier for parent web site.</param>
        private void ParseWebDirElement(XmlNode node, string componentId, string parentWeb)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string dirProperties = null;
            string path = null;
            string application = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "DirProperties":
                            dirProperties = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebApplication":
                            application = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Core.OnMessage(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
                            }

                            parentWeb = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebSite", parentWeb);
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

            if (null == path)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Path"));
            }

            if (null == parentWeb)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "WebSite"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);

                        switch (child.LocalName)
                        {
                            case "WebApplication":
                                if (null != application)
                                {
                                    this.Core.OnMessage(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, node.Name));
                                }

                                application = this.ParseWebApplicationElement(child);
                                break;
                            case "WebDirProperties":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                string childWebDirProperties = this.ParseWebDirPropertiesElement(child);
                                if (null == dirProperties)
                                {
                                    dirProperties = childWebDirProperties;
                                }
                                else
                                {
                                    this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, child.LocalName, "DirProperties", node.LocalName));
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

            if (null == dirProperties)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "DirProperties"));
            }

            if (null != application)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebApplication", application);
            }

            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebDirProperties", dirProperties);

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebDir");
                row[0] = id;
                row[1] = componentId;
                row[2] = parentWeb;
                row[3] = path;
                row[4] = dirProperties;
                row[5] = application;
            }
        }

        /// <summary>
        /// Parses a web directory properties element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <returns>The identifier for this WebDirProperties.</returns>
        private string ParseWebDirPropertiesElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int access = 0;
            bool accessSet = false;
            int accessSSLFlags = 0;
            bool accessSSLFlagsSet = false;
            string anonymousUser = null;
            YesNoType aspDetailedError = YesNoType.NotSet;
            string authenticationProviders = null;
            int authorization = 0;
            bool authorizationSet = false;
            string cacheControlCustom = null;
            long cacheControlMaxAge = CompilerCore.LongNotSet;
            string defaultDocuments = null;
            string httpExpires = null;
            bool iisControlledPassword = false;
            YesNoType index = YesNoType.NotSet;
            YesNoType logVisits = YesNoType.NotSet;
            YesNoType notCustomError = YesNoType.NotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AnonymousUser":
                            anonymousUser = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", anonymousUser);
                            break;
                        case "AspDetailedError":
                            aspDetailedError = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AuthenticationProviders":
                            authenticationProviders = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheControlCustom":
                            cacheControlCustom = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheControlMaxAge":
                            cacheControlMaxAge = this.Core.GetAttributeLongValue(sourceLineNumbers, attrib, 0, uint.MaxValue); // 4294967295 (uint.MaxValue) represents unlimited
                            break;
                        case "ClearCustomError":
                            notCustomError = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultDocuments":
                            defaultDocuments = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HttpExpires":
                            httpExpires = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IIsControlledPassword":
                            iisControlledPassword = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            index = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LogVisits":
                            logVisits = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;

                        // Access attributes
                        case "Execute":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 4;
                            }
                            else
                            {
                                access &= ~4;
                            }
                            accessSet = true;
                            break;
                        case "Read":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 1;
                            }
                            else
                            {
                                access &= ~1;
                            }
                            accessSet = true;
                            break;
                        case "Script":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 512;
                            }
                            else
                            {
                                access &= ~512;
                            }
                            accessSet = true;
                            break;
                        case "Write":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 2;
                            }
                            else
                            {
                                access &= ~2;
                            }
                            accessSet = true;
                            break;

                        // AccessSSL Attributes
                        case "AccessSSL":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 8;
                            }
                            else
                            {
                                accessSSLFlags &= ~8;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSL128":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 256;
                            }
                            else
                            {
                                accessSSLFlags &= ~256;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLMapCert":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 128;
                            }
                            else
                            {
                                accessSSLFlags &= ~128;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLNegotiateCert":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 32;
                            }
                            else
                            {
                                accessSSLFlags &= ~32;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLRequireCert":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 64;
                            }
                            else
                            {
                                accessSSLFlags &= ~64;
                            }
                            accessSSLFlagsSet = true;
                            break;

                        // Authorization attributes
                        case "AnonymousAccess":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 1;
                            }
                            else
                            {
                                authorization &= ~1;
                            }
                            authorizationSet = true;
                            break;
                        case "BasicAuthentication":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 2;
                            }
                            else
                            {
                                authorization &= ~2;
                            }
                            authorizationSet = true;
                            break;
                        case "DigestAuthentication":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 16;
                            }
                            else
                            {
                                authorization &= ~16;
                            }
                            authorizationSet = true;
                            break;
                        case "PassportAuthentication":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 64;
                            }
                            else
                            {
                                authorization &= ~64;
                            }
                            authorizationSet = true;
                            break;
                        case "WindowsAuthentication":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 4;
                            }
                            else
                            {
                                authorization &= ~4;
                            }
                            authorizationSet = true;
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebDirProperties");
                row[0] = id;
                if (accessSet)
                {
                    row[1] = access;
                }

                if (authorizationSet)
                {
                    row[2] = authorization;
                }
                row[3] = anonymousUser;
                row[4] = iisControlledPassword ? 1 : 0;
                if (YesNoType.NotSet != logVisits)
                {
                    row[5] = YesNoType.Yes == logVisits ? 1 : 0;
                }

                if (YesNoType.NotSet != index)
                {
                    row[6] = YesNoType.Yes == index ? 1 : 0;
                }
                row[7] = defaultDocuments;
                if (YesNoType.NotSet != aspDetailedError)
                {
                    row[8] = YesNoType.Yes == aspDetailedError ? 1 : 0;
                }
                row[9] = httpExpires;
                if (CompilerCore.LongNotSet != cacheControlMaxAge)
                {
                    row[10] = unchecked((int)cacheControlMaxAge);
                }
                row[11] = cacheControlCustom;
                if (YesNoType.NotSet != notCustomError)
                {
                    row[12] = YesNoType.Yes == notCustomError ? 1 : 0;
                }

                if (accessSSLFlagsSet)
                {
                    row[13] = accessSSLFlags;
                }

                if (null != authenticationProviders)
                {
                    row[14] = authenticationProviders;
                }
            }

            return id;
        }

        /// <summary>
        /// Parses a web error element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="parent">Id of the parent.</param>
        private void ParseWebErrorElement(XmlNode node, WebErrorParentType parentType, string parent)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            int errorCode = CompilerCore.IntegerNotSet;
            string file = null;
            string url = null;
            int subCode = CompilerCore.IntegerNotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "ErrorCode":
                            errorCode = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 400, 599);
                            break;
                        case "File":
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SubCode":
                            subCode = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "URL":
                            url = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (CompilerCore.IntegerNotSet == errorCode)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ErrorCode"));
                errorCode = CompilerCore.IllegalInteger;
            }

            if (CompilerCore.IntegerNotSet == subCode)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "SubCode"));
                subCode = CompilerCore.IllegalInteger;
            }

            if (String.IsNullOrEmpty(file) && String.IsNullOrEmpty(url))
            {
                this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "File", "URL"));
            }

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

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebError");
                row[0] = errorCode;
                row[1] = subCode;
                row[2] = (int)parentType;
                row[3] = parent;
                row[4] = file;
                row[5] = url;
            }
        }

        /// <summary>
        /// Parses a web filter element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentWeb">Optional identifier of parent web site.</param>
        private void ParseWebFilterElement(XmlNode node, string componentId, string parentWeb)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string description = null;
            int flags = 0;
            int loadOrder = CompilerCore.IntegerNotSet;
            string name = null;
            string path = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Flags":
                            flags = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "LoadOrder":
                            string loadOrderValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < loadOrderValue.Length)
                            {
                                switch (loadOrderValue)
                                {
                                    case "first":
                                        loadOrder = 0;
                                        break;
                                    case "last":
                                        loadOrder = -1;
                                        break;
                                    default:
                                        loadOrder = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Core.OnMessage(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
                            }

                            parentWeb = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebSite", parentWeb);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            if (null == path)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Path"));
            }

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

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsFilter");
                row[0] = id;
                row[1] = name;
                row[2] = componentId;
                row[3] = path;
                row[4] = parentWeb;
                row[5] = description;
                row[6] = flags;
                if (CompilerCore.IntegerNotSet != loadOrder)
                {
                    row[7] = loadOrder;
                }
            }
        }

        /// <summary>
        /// Parses web log element.
        /// </summary>
        /// <param name="node">Node to be parsed.</param>
        private void ParseWebLogElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string type = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                switch (typeValue)
                                {
                                    case "IIS":
                                        type = "Microsoft IIS Log File Format";
                                        break;
                                    case "NCSA":
                                        type = "NCSA Common Log File Format";
                                        break;
                                    case "none":
                                        type = "none";
                                        break;
                                    case "ODBC":
                                        type = "ODBC Logging";
                                        break;
                                    case "W3C":
                                        type = "W3C Extended Log File Format";
                                        break;
                                    default:
                                        this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Type", typeValue, "IIS", "NCSA", "none", "ODBC", "W3C"));
                                        break;
                                }
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

            if (null == type)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Type"));
            }

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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebLog");
                row[0] = id;
                row[1] = type;
            }
        }

        /// <summary>
        /// Parses a web property element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebPropertyElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string value = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            switch (id)
            {
                case "ETagChangeNumber":
                case "MaxGlobalBandwidth":
                    // Must specify a value for these
                    if (null == value)
                    {
                        this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Value", "Id", id));
                    }
                    break;
                case "IIs5IsolationMode":
                case "LogInUTF8":
                    // Can't specify a value for these
                    if (null != value)
                    {
                        this.Core.OnMessage(WixErrors.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name, "Value", "Id", id));
                    }
                    break;
                default:
                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, node.Name, "Id", id, "ETagChangeNumber", "IIs5IsolationMode", "LogInUTF8", "MaxGlobalBandwidth"));
                    break;
            }

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

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsProperty");
                row[0] = id;
                row[1] = componentId;
                row[2] = 0;
                row[3] = value;
            }
        }

        /// <summary>
        /// Parses a web service extension element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebServiceExtensionElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            int attributes = 0;
            string description = null;
            string file = null;
            string group = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Allow":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1;
                            }
                            else
                            {
                                attributes &= ~1;
                            }
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Group":
                            group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UIDeletable":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 2;
                            }
                            else
                            {
                                attributes &= ~2;
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

            if (null == file)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
            }

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

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebServiceExtension");
                row[0] = id;
                row[1] = componentId;
                row[2] = file;
                row[3] = description;
                row[4] = group;
                row[5] = attributes;
            }
        }

        /// <summary>
        /// Parses a web site element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseWebSiteElement(XmlNode node, string componentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string application = null;
            int attributes = 0;
            int connectionTimeout = CompilerCore.IntegerNotSet;
            string description = null;
            string directory = null;
            string dirProperties = null;
            string keyAddress = null;
            string log = null;
            string siteId = null;
            int sequence = CompilerCore.IntegerNotSet;
            int state = CompilerCore.IntegerNotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AutoStart":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                state = 2;
                            }
                            else if (state != 1)
                            {
                                state = 0;
                            }
                            break;
                        case "ConfigureIfExists":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes &= ~2;
                            }
                            else
                            {
                                attributes |= 2;
                            }
                            break;
                        case "ConnectionTimeout":
                            connectionTimeout = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Description":
                            description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Directory", directory);
                            break;
                        case "DirProperties":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            dirProperties = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SiteId":
                            siteId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("*" == siteId)
                            {
                                siteId = "-1";
                            }
                            break;
                        case "Sequence":
                            sequence = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "StartOnInstall":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            // when state is set to 2 it implies 1, so don't set it to 1
                            if (2 != state && YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                state = 1;
                            }
                            else if (2 != state)
                            {
                                state = 0;
                            }
                            break;
                        case "WebApplication":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            application = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebLog":
                            if (null == componentId)
                            {
                                this.Core.OnMessage(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                            }

                            log = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebLog", log);
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

            if (null == description)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Description"));
            }

            if (null == directory && null != componentId)
            {
                this.Core.OnMessage(IIsErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name, "Directory"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                        switch (child.LocalName)
                        {
                            case "CertificateRef":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseCertificateRefElement(child, id);
                                break;
                            case "HttpHeader":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseHttpHeaderElement(child, HttpHeaderParentType.WebSite, id);
                                break;
                            case "WebAddress":
                                string address = this.ParseWebAddressElement(child, id);
                                if (null == keyAddress)
                                {
                                    keyAddress = address;
                                }
                                break;
                            case "WebApplication":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                if (null != application)
                                {
                                    this.Core.OnMessage(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, node.Name));
                                }

                                application = this.ParseWebApplicationElement(child);
                                break;
                            case "WebDir":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseWebDirElement(child, componentId, id);
                                break;
                            case "WebDirProperties":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                string childWebDirProperties = this.ParseWebDirPropertiesElement(child);
                                if (null == dirProperties)
                                {
                                    dirProperties = childWebDirProperties;
                                }
                                else
                                {
                                    this.Core.OnMessage(WixErrors.IllegalParentAttributeWhenNested(sourceLineNumbers, "WebSite", "DirProperties", child.LocalName));
                                }
                                break;
                            case "WebError":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseWebErrorElement(child, WebErrorParentType.WebSite, id);
                                break;
                            case "WebFilter":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseWebFilterElement(child, componentId, id);
                                break;
                            case "WebVirtualDir":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                this.ParseWebVirtualDirElement(child, componentId, id, null);
                                break;
                            case "MimeMap":
                                this.ParseMimeMapElement(child, id, MimeMapParentType.WebSite);
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

            if (null == keyAddress)
            {
                this.Core.OnMessage(WixErrors.ExpectedElement(sourceLineNumbers, node.Name, "WebAddress"));
            }

            if (null != application)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebApplication", application);
            }

            if (null != dirProperties)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebDirProperties", dirProperties);
            }

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebSite");
                row[0] = id;
                row[1] = componentId;
                row[2] = description;
                if (CompilerCore.IntegerNotSet != connectionTimeout)
                {
                    row[3] = connectionTimeout;
                }
                row[4] = directory;
                if (CompilerCore.IntegerNotSet != state)
                {
                    row[5] = state;
                }

                if (0 != attributes)
                {
                    row[6] = attributes;
                }
                row[7] = keyAddress;
                row[8] = dirProperties;
                row[9] = application;
                if (CompilerCore.IntegerNotSet != sequence)
                {
                    row[10] = sequence;
                }
                row[11] = log;
                row[12] = siteId;
            }
        }

        /// <summary>
        /// Parses a HTTP Header element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="parent">Id of the parent.</param>
        private void ParseHttpHeaderElement(XmlNode node, HttpHeaderParentType parentType, string parent)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string headerName = null;
            string headerValue = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Name":
                        headerName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        headerValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == headerName)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }
            else if (null == id)
            {
                id = CompilerCore.GetIdentifierFromName(headerName);
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    this.Core.UnexpectedElement(node, child);
                }
            }

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            Row row = this.Core.CreateRow(sourceLineNumbers, "IIsHttpHeader");
            row[0] = id;
            row[1] = (int)parentType;
            row[2] = parent;
            row[3] = headerName;
            row[4] = headerValue;
            row[5] = 0;
            row[6] = null;
        }

        /// <summary>
        /// Parses a virtual directory element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentWeb">Identifier of parent web site.</param>
        /// <param name="parentAlias">Alias of the parent web site.</param>
        private void ParseWebVirtualDirElement(XmlNode node, string componentId, string parentWeb, string parentAlias)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string alias = null;
            string application = null;
            string directory = null;
            string dirProperties = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Alias":
                            alias = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Directory", directory);
                            break;
                        case "DirProperties":
                            dirProperties = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebApplication":
                            application = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Core.OnMessage(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, node.Name));
                            }

                            parentWeb = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebSite", parentWeb);
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

            if (null == alias)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Alias"));
            }
            else if (-1 != alias.IndexOf("\\", StringComparison.Ordinal))
            {
                this.Core.OnMessage(IIsErrors.IllegalCharacterInAttributeValue(sourceLineNumbers, node.Name, "Alias", alias, '\\'));
            }

            if (null == directory)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Directory"));
            }

            if (null == parentWeb)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "WebSite"));
            }

            if (null == componentId)
            {
                this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(sourceLineNumbers, node.Name));
            }

            if (null != parentAlias)
            {
                alias = String.Concat(parentAlias, "/", alias);
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                        switch (child.LocalName)
                        {
                            case "WebApplication":
                                if (null != application)
                                {
                                    this.Core.OnMessage(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, node.Name));
                                }

                                application = this.ParseWebApplicationElement(child);
                                break;
                            case "WebDirProperties":
                                if (null == componentId)
                                {
                                    this.Core.OnMessage(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name));
                                }

                                string childWebDirProperties = this.ParseWebDirPropertiesElement(child);
                                if (null == dirProperties)
                                {
                                    dirProperties = childWebDirProperties;
                                }
                                else
                                {
                                    this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, child.LocalName, "DirProperties", node.LocalName));
                                }
                                break;

                            case "WebError":
                                this.ParseWebErrorElement(child, WebErrorParentType.WebVirtualDir, id);
                                break;
                            case "WebVirtualDir":
                                this.ParseWebVirtualDirElement(child, componentId, parentWeb, alias);
                                break;
                            case "HttpHeader":
                                this.ParseHttpHeaderElement(child, HttpHeaderParentType.WebVirtualDir, id);
                                break;
                            case "MimeMap":
                                this.ParseMimeMapElement(child, id, MimeMapParentType.WebVirtualDir);
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

            if (null != dirProperties)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebDirProperties", dirProperties);
            }

            if (null != application)
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "IIsWebApplication", application);
            }

            // Reference ConfigureIIs since nothing will happen without it
            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "IIsWebVirtualDir");
                row[0] = id;
                row[1] = componentId;
                row[2] = parentWeb;
                row[3] = alias;
                row[4] = directory;
                row[5] = dirProperties;
                row[6] = application;
            }
        }
    }
}
