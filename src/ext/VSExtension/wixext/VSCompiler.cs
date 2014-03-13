//-------------------------------------------------------------------------------------------------
// <copyright file="VSCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the Windows Installer XML Toolset Visual Studio Extension.
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
    /// The compiler for the Windows Installer XML Toolset Visual Studio Extension.
    /// </summary>
    public sealed class VSCompiler : CompilerExtension
    {
        internal const int MsidbCustomActionTypeExe              = 0x00000002;  // Target = command line args
        internal const int MsidbCustomActionTypeProperty         = 0x00000030;  // Source = full path to executable
        internal const int MsidbCustomActionTypeContinue         = 0x00000040;  // ignore action return status; continue running
        internal const int MsidbCustomActionTypeRollback         = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        internal const int MsidbCustomActionTypeInScript         = 0x00000400;  // queue for execution within script
        internal const int MsidbCustomActionTypeNoImpersonate    = 0x00000800;  // queue for not impersonating

        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new HelpCompiler.
        /// </summary>
        public VSCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.vs.xsd");
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
                    switch (element.LocalName)
                    {
                        case "VsixPackage":
                            this.ParseVsixPackageElement(element, contextValues[0], null);
                            break;
                        default:
                            base.ParseElement(sourceLineNumbers, parentElement, element, contextValues);
                            break;
                    }
                    break;
                case "File":
                    switch (element.LocalName)
                    {
                        case "HelpCollection":
                            this.ParseHelpCollectionElement(element, contextValues[0]);
                            break;
                        case "HelpFile":
                            this.ParseHelpFileElement(element, contextValues[0]);
                            break;
                        case "VsixPackage":
                            this.ParseVsixPackageElement(element, contextValues[1], contextValues[0]);
                            break;
                        default:
                            base.ParseElement(sourceLineNumbers, parentElement, element, contextValues);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.LocalName)
                    {
                        case "HelpCollectionRef":
                            this.ParseHelpCollectionRefElement(element);
                            break;
                        case "HelpFilter":
                            this.ParseHelpFilterElement(element);
                            break;
                        default:
                            base.ParseElement(sourceLineNumbers, parentElement, element, contextValues);
                            break;
                    }
                    break;
                default:
                    base.ParseElement(sourceLineNumbers, parentElement, element, contextValues);
                    break;
            }
        }

        /// <summary>
        /// Parses a HelpCollectionRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        private void ParseHelpCollectionRefElement(XmlNode node)
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
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "HelpNamespace", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(sourceLineNumbers, (XmlElement)node, attrib);
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
                        switch (child.LocalName)
                        {
                            case "HelpFileRef":
                                this.ParseHelpFileRefElement(child, id);
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
        /// Parses a HelpCollection element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="fileId">Identifier of the parent File element.</param>
        private void ParseHelpCollectionElement(XmlNode node, string fileId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string description = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

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
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Name"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "HelpFileRef":
                                this.ParseHelpFileRefElement(child, id);
                                break;
                            case "HelpFilterRef":
                                this.ParseHelpFilterRefElement(child, id);
                                break;
                            case "PlugCollectionInto":
                                this.ParsePlugCollectionIntoElement(child, id);
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
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpNamespace");
                row[0] = id;
                row[1] = name;
                row[2] = fileId;
                row[3] = description;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFile element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="fileId">Identifier of the parent file element.</param>
        private void ParseHelpFileElement(XmlNode node, string fileId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string name = null;
            int language = CompilerCore.IntegerNotSet;
            string hxi = null;
            string hxq = null;
            string hxr = null;
            string samples = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "AttributeIndex":
                            hxr = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", hxr);
                            break;
                        case "Index":
                            hxi = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", hxi);
                            break;
                        case "Language":
                            language = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SampleLocation":
                            samples = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", samples);
                            break;
                        case "Search":
                            hxq = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", hxq);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            //uninstall will always fail silently, leaving file registered, if Language is not set
            if (CompilerCore.IntegerNotSet == language)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Language"));
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFile");
                row[0] = id;
                row[1] = name;
                row[2] = language;
                row[3] = fileId;
                row[4] = hxi;
                row[5] = hxq;
                row[6] = hxr;
                row[7] = samples;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFileRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="collectionId">Identifier of the parent help collection.</param>
        private void ParseHelpFileRefElement(XmlNode node, string collectionId)
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
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "HelpFile", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(sourceLineNumbers, (XmlElement)node, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFileToNamespace");
                row[0] = id;
                row[1] = collectionId;
            }
        }

        /// <summary>
        /// Parses a HelpFilter element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        private void ParseHelpFilterElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string filterDefinition = null;
            string name = null;
            YesNoType suppressCAs = YesNoType.No;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "FilterDefinition":
                            filterDefinition = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressCustomActions":
                            suppressCAs = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilter");
                row[0] = id;
                row[1] = name;
                row[2] = filterDefinition;

                if (YesNoType.No == suppressCAs)
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "CA_RegisterMicrosoftHelp.3643236F_FC70_11D3_A536_0090278A1BB8");
                }
            }
        }

        /// <summary>
        /// Parses a HelpFilterRef element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="collectionId">Identifier of the parent help collection.</param>
        private void ParseHelpFilterRefElement(XmlNode node, string collectionId)
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
                            this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "HelpFilter", id);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(sourceLineNumbers, (XmlElement)node, attrib);
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpFilterToNamespace");
                row[0] = id;
                row[1] = collectionId;
            }
        }

        /// <summary>
        /// Parses a PlugCollectionInto element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="parentId">Identifier of the parent help collection.</param>
        private void ParsePlugCollectionIntoElement(XmlNode node, string parentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string hxa = null;
            string hxt = null;
            string hxtParent = null;
            string namespaceParent = null;
            string feature = null;
            YesNoType suppressExternalNamespaces = YesNoType.No;
            bool pluginVS05 = false;
            bool pluginVS08 = false;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Attributes":
                            hxa = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TableOfContents":
                            hxt = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetCollection":
                            namespaceParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetTableOfContents":
                            hxtParent = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "TargetFeature":
                            feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressExternalNamespaces":
                            suppressExternalNamespaces = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            pluginVS05 = namespaceParent.Equals("MS_VSIPCC_v80", StringComparison.Ordinal);
            pluginVS08 = namespaceParent.Equals("MS.VSIPCC.v90", StringComparison.Ordinal);

            if (null == namespaceParent)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TargetCollection"));
            }

            if (null == feature && (pluginVS05 || pluginVS08) && YesNoType.No == suppressExternalNamespaces)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TargetFeature"));
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

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "HelpPlugin");
                row[0] = parentId;
                row[1] = namespaceParent;
                row[2] = hxt;
                row[3] = hxa;
                row[4] = hxtParent;

                if (pluginVS05)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2005
                        this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2005_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction",
                            "CA_HxMerge_VSIPCC_VSCC");
                    }
                }
                else if (pluginVS08)
                {
                    if (YesNoType.No == suppressExternalNamespaces)
                    {
                        // Bring in the help 2 base namespace components for VS 2008
                        this.Core.CreateComplexReference(sourceLineNumbers, ComplexReferenceParentType.Feature, feature, String.Empty,
                            ComplexReferenceChildType.ComponentGroup, "Help2_VS2008_Namespace_Components", false);
                        // Reference CustomAction since nothing will happen without it
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction",
                            "CA_ScheduleExtHelpPlugin_VSCC_VSIPCC");
                    }
                }
                else
                {
                    // Reference the parent namespace to enforce the foreign key relationship
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "HelpNamespace",
                        namespaceParent);
                }
            }
        }

        /// <summary>
        /// Parses a VsixPackage element.
        /// </summary>
        /// <param name="node">Element to process.</param>
        /// <param name="componentId">Identifier of the parent Component element.</param>
        /// <param name="fileId">Identifier of the parent File element.</param>
        private void ParseVsixPackageElement(XmlNode node, string componentId, string fileId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string propertyId = "VS_VSIX_INSTALLER_PATH";
            string packageId = null;
            YesNoType permanent = YesNoType.NotSet;
            string target = null;
            string targetVersion = null;
            YesNoType vital = YesNoType.NotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "File":
                            if (String.IsNullOrEmpty(fileId))
                            {
                                fileId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            else
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, "File", "File"));
                            }
                            break;
                        case "PackageId":
                            packageId = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Permanent":
                            permanent = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Target":
                            target = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (target.ToLowerInvariant())
                            {
                            case "integrated":
                            case "integratedshell":
                                target = "IntegratedShell";
                                break;
                            case "professional":
                                target = "Pro";
                                break;
                            case "premium":
                                target = "Premium";
                                break;
                            case "ultimate":
                                target = "Ultimate";
                                break;
                            case "vbexpress":
                                target = "VBExpress";
                                break;
                            case "vcexpress":
                                target = "VCExpress";
                                break;
                            case "vcsexpress":
                                target = "VCSExpress";
                                break;
                            case "vwdexpress":
                                target = "VWDExpress";
                                break;
                            }
                            break;
                        case "TargetVersion":
                            targetVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Vital":
                            vital = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "VsixInstallerPathProperty":
                            propertyId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(sourceLineNumbers, (XmlElement)node, attrib);
                }
            }

            if (String.IsNullOrEmpty(fileId))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "File"));
            }

            if (String.IsNullOrEmpty(packageId))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "PackageId"));
            }

            if (!String.IsNullOrEmpty(target) && String.IsNullOrEmpty(targetVersion))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TargetVersion", "Target"));
            }
            else if (String.IsNullOrEmpty(target) && !String.IsNullOrEmpty(targetVersion))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Target", "TargetVersion"));
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

            if (!this.Core.EncounteredError)
            {
                // Ensure there is a reference to the AppSearch Property that will find the VsixInstaller.exe.
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Property", propertyId);

                // Ensure there is a reference to the package file (even if we are a child under it).
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "File", fileId);

                string cmdlinePrefix = "/q ";

                if (!String.IsNullOrEmpty(target))
                {
                    cmdlinePrefix = String.Format("{0} /skuName:{1} /skuVersion:{2}", cmdlinePrefix, target, targetVersion);
                }

                string installAfter = "WriteRegistryValues"; // by default, come after the registry key registration.
                int installExtraBits = VSCompiler.MsidbCustomActionTypeInScript;

                // If the package is not vital, mark the install action as continue.
                if (vital == YesNoType.No)
                {
                    installExtraBits |= VSCompiler.MsidbCustomActionTypeContinue;
                }
                else // the package is vital so ensure there is a rollback action scheduled.
                {
                    string rollbackNamePerUser = this.Core.GenerateIdentifier("vru", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    string rollbackNamePerMachine = this.Core.GenerateIdentifier("vrm", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string rollbackCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:", packageId);
                    string rollbackCmdLinePerMachine = String.Concat(rollbackCmdLinePerUser, " /admin");
                    int rollbackExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeRollback | VSCompiler.MsidbCustomActionTypeInScript;
                    int rollbackExtraBitsPerMachine = rollbackExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string rollbackConditionPerUser = String.Format("NOT ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.
                    string rollbackConditionPerMachine = String.Format("ALLUSERS AND NOT Installed AND ${0}=2 AND ?{0}>2", componentId); // NOT Installed && Component being installed but not installed already.

                    this.SchedulePropertyExeAction(sourceLineNumbers, rollbackNamePerUser, propertyId, rollbackCmdLinePerUser, rollbackExtraBitsPerUser, rollbackConditionPerUser, null, installAfter);
                    this.SchedulePropertyExeAction(sourceLineNumbers, rollbackNamePerMachine, propertyId, rollbackCmdLinePerMachine, rollbackExtraBitsPerMachine, rollbackConditionPerMachine, null, rollbackNamePerUser);

                    installAfter = rollbackNamePerMachine;
                }

                string installNamePerUser = this.Core.GenerateIdentifier("viu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                string installNamePerMachine = this.Core.GenerateIdentifier("vim", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                string installCmdLinePerUser = String.Format("{0} \"[#{1}]\"", cmdlinePrefix, fileId);
                string installCmdLinePerMachine = String.Concat(installCmdLinePerUser, " /admin");
                string installConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.
                string installConditionPerMachine = String.Format("ALLUSERS AND ${0}=3", componentId); // only execute if the Component being installed.

                this.SchedulePropertyExeAction(sourceLineNumbers, installNamePerUser, propertyId, installCmdLinePerUser, installExtraBits, installConditionPerUser, null, installAfter);
                this.SchedulePropertyExeAction(sourceLineNumbers, installNamePerMachine, propertyId, installCmdLinePerMachine, installExtraBits | VSCompiler.MsidbCustomActionTypeNoImpersonate, installConditionPerMachine, null, installNamePerUser);

                // If not permanent, schedule the uninstall custom action.
                if (permanent != YesNoType.Yes)
                {
                    string uninstallNamePerUser = this.Core.GenerateIdentifier("vuu", componentId, fileId, "per-user", target ?? String.Empty, targetVersion ?? String.Empty);
                    string uninstallNamePerMachine = this.Core.GenerateIdentifier("vum", componentId, fileId, "per-machine", target ?? String.Empty, targetVersion ?? String.Empty);
                    string uninstallCmdLinePerUser = String.Concat(cmdlinePrefix, " /u:", packageId);
                    string uninstallCmdLinePerMachine = String.Concat(uninstallCmdLinePerUser, " /admin");
                    int uninstallExtraBitsPerUser = VSCompiler.MsidbCustomActionTypeContinue | VSCompiler.MsidbCustomActionTypeInScript;
                    int uninstallExtraBitsPerMachine = uninstallExtraBitsPerUser | VSCompiler.MsidbCustomActionTypeNoImpersonate;
                    string uninstallConditionPerUser = String.Format("NOT ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.
                    string uninstallConditionPerMachine = String.Format("ALLUSERS AND ${0}=2 AND ?{0}>2", componentId); // Only execute if component is being uninstalled.

                    this.SchedulePropertyExeAction(sourceLineNumbers, uninstallNamePerUser, propertyId, uninstallCmdLinePerUser, uninstallExtraBitsPerUser, uninstallConditionPerUser, "InstallFinalize", null);
                    this.SchedulePropertyExeAction(sourceLineNumbers, uninstallNamePerMachine, propertyId, uninstallCmdLinePerMachine, uninstallExtraBitsPerMachine, uninstallConditionPerMachine, "InstallFinalize", null);
                }
            }
        }

        private void SchedulePropertyExeAction(SourceLineNumberCollection sourceLineNumbers, string name, string source, string cmdline, int extraBits, string condition, string beforeAction, string afterAction)
        {
            const string sequence = "InstallExecuteSequence";

            Row actionRow = this.Core.CreateRow(sourceLineNumbers, "CustomAction");
            actionRow[0] = name;
            actionRow[1] = VSCompiler.MsidbCustomActionTypeProperty | VSCompiler.MsidbCustomActionTypeExe | extraBits;
            actionRow[2] = source;
            actionRow[3] = cmdline;

            Row sequenceRow = this.Core.CreateRow(sourceLineNumbers, "WixAction");
            sequenceRow[0] = sequence;
            sequenceRow[1] = name;
            sequenceRow[2] = condition;
            // no explicit sequence
            sequenceRow[4] = beforeAction;
            sequenceRow[5] = afterAction;
            sequenceRow[6] = 0; // not overridable

            if (null != beforeAction)
            {
                if (Util.IsStandardAction(beforeAction))
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "WixAction", sequence, beforeAction);
                }
                else
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", beforeAction);
                }
            }

            if (null != afterAction)
            {
                if (Util.IsStandardAction(afterAction))
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "WixAction", sequence, afterAction);
                }
                else
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", afterAction);
                }
            }
        }
    }
}
