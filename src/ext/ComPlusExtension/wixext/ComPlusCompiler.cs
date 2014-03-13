//-------------------------------------------------------------------------------------------------
// <copyright file="ComPlusCompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The compiler for the Windows Installer XML Toolset COM+ Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    public sealed class ComPlusCompiler : CompilerExtension
    {
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new ComPlusCompiler.
        /// </summary>
        public ComPlusCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.complus.xsd");
        }

        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum CpiAssemblyAttributes
        {
            EventClass       = (1 << 0),
            DotNetAssembly   = (1 << 1),
            DllPathFromGAC   = (1 << 2),
            RegisterInCommit = (1 << 3)
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
                    bool win64 = Boolean.Parse(contextValues[2]);

                    switch (element.LocalName)
                    {
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(element, componentId, win64);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(element, componentId, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(element, componentId, win64, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(element, componentId, win64, null);
                            break;
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(element, componentId, null);
                            break;
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(element, componentId, null);
                            break;
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(element, componentId, null);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(element, componentId, null);
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
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(element, null, false);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(element, null, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(element, null, false, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(element, null, null);
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

        ///	<summary>
        ///	Parses a COM+ partition element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionElement(XmlNode node, string componentKey, bool win64)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string id = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "PartitionId":
                        id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Changeable":
                        this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name, attrib.Name));
                        break;
                    case "Deleteable":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Deleteable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "Description":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null != componentKey && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name, "Id", "Name"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(child, componentKey, key);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(child, componentKey, win64, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartition");
            row[0] = key;
            row[1] = componentKey;
            row[2] = id;
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }

            if (componentKey != null)
            {
                if (win64)
                {
                    if (this.Core.CurrentPlatform == Platform.IA64)
                    {
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.LocalName));
                    }
                    else
                    {
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                    }
                }
                else
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
                }
            }
        }

        ///	<summary>
        ///	Parses a COM+ partition role element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusPartitionRoleElement(XmlNode node, string componentKey, string partitionKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string name = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Partition":
                        if (null != partitionKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusPartition", partitionKey);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == partitionKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Partition"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            // add table row
            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionRole");
            row[0] = key;
            row[1] = partitionKey;
            row[3] = name;
        }

        ///	<summary>
        ///	Parses a COM+ partition role user element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInPartitionRoleElement(XmlNode node, string componentKey, string partitionRoleKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "PartitionRole":
                        if (null != partitionRoleKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        partitionRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusPartitionRole", partitionRoleKey);
                        break;
                    case "User":
                        user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == partitionRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "PartitionRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusUserInPartitionRole");
            row[0] = key;
            row[1] = partitionRoleKey;
            row[2] = componentKey;
            row[3] = user;
        }

        ///	<summary>
        ///	Parses a COM+ partition role user element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInPartitionRoleElement(XmlNode node, string componentKey, string partitionRoleKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string group = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "PartitionRole":
                        if (null != partitionRoleKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        partitionRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusPartitionRole", partitionRoleKey);
                        break;
                    case "Group":
                        group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Group", group);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == partitionRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "PartitionRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusGroupInPartitionRole");
            row[0] = key;
            row[1] = partitionRoleKey;
            row[2] = componentKey;
            row[3] = group;
        }

        ///	<summary>
        ///	Parses a COM+ partition element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionUserElement(XmlNode node, string componentKey, string partitionKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Partition":
                        if (null != partitionKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusPartition", partitionKey);
                        break;
                    case "User":
                        user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == partitionKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Partition"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionUser");
            row[0] = key;
            row[1] = partitionKey;
            row[2] = componentKey;
            row[3] = user;
        }

        ///	<summary>
        ///	Parses a COM+ application element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="partitionKey">Optional identifier of parent partition.</param>
        private void ParseComPlusApplicationElement(XmlNode node, string componentKey, bool win64, string partitionKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string id = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Partition":
                        if (null != partitionKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusPartition", partitionKey);
                        break;
                    case "ApplicationId":
                        id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ThreeGigSupportEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["3GigSupportEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "AccessChecksLevel":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string accessChecksLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (accessChecksLevelValue)
                        {
                            case "applicationLevel":
                                properties["AccessChecksLevel"] = "0";
                                break;
                            case "applicationComponentLevel":
                                properties["AccessChecksLevel"] = "1";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AccessChecksLevel", accessChecksLevelValue, "applicationLevel", "applicationComponentLevel"));
                                break;
                        }
                        break;
                    case "Activation":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string activationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (activationValue)
                        {
                            case "inproc":
                                properties["Activation"] = "Inproc";
                                break;
                            case "local":
                                properties["Activation"] = "Local";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Activation", activationValue, "inproc", "local"));
                                break;
                        }
                        break;
                    case "ApplicationAccessChecksEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["ApplicationAccessChecksEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ApplicationDirectory":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["ApplicationDirectory"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Authentication":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string authenticationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (authenticationValue)
                        {
                            case "default":
                                properties["Authentication"] = "0";
                                break;
                            case "none":
                                properties["Authentication"] = "1";
                                break;
                            case "connect":
                                properties["Authentication"] = "2";
                                break;
                            case "call":
                                properties["Authentication"] = "3";
                                break;
                            case "packet":
                                properties["Authentication"] = "4";
                                break;
                            case "integrity":
                                properties["Authentication"] = "5";
                                break;
                            case "privacy":
                                properties["Authentication"] = "6";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Authentication", authenticationValue, "default", "none", "connect", "call", "packet", "integrity", "privacy"));
                                break;
                        }
                        break;
                    case "AuthenticationCapability":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string authenticationCapabilityValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (authenticationCapabilityValue)
                        {
                            case "none":
                                properties["AuthenticationCapability"] = "0";
                                break;
                            case "secureReference":
                                properties["AuthenticationCapability"] = "2";
                                break;
                            case "staticCloaking":
                                properties["AuthenticationCapability"] = "32";
                                break;
                            case "dynamicCloaking":
                                properties["AuthenticationCapability"] = "64";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AuthenticationCapability", authenticationCapabilityValue, "none", "secureReference", "staticCloaking", "dynamicCloaking"));
                                break;
                        }
                        break;
                    case "Changeable":
                        this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name, attrib.Name));
                        break;
                    case "CommandLine":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["CommandLine"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ConcurrentApps":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["ConcurrentApps"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CreatedBy":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["CreatedBy"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CRMEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["CRMEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "CRMLogFile":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["CRMLogFile"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Deleteable":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Deleteable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "Description":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DumpEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["DumpEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "DumpOnException":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["DumpOnException"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "DumpOnFailfast":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["DumpOnFailfast"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "DumpPath":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["DumpPath"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "EventsEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["EventsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "Identity":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Identity"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ImpersonationLevel":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string impersonationLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (impersonationLevelValue)
                        {
                            case "anonymous":
                                properties["ImpersonationLevel"] = "1";
                                break;
                            case "identify":
                                properties["ImpersonationLevel"] = "2";
                                break;
                            case "impersonate":
                                properties["ImpersonationLevel"] = "3";
                                break;
                            case "delegate":
                                properties["ImpersonationLevel"] = "4";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "ImpersonationLevel", impersonationLevelValue, "anonymous", "identify", "impersonate", "delegate"));
                                break;
                        }
                        break;
                    case "IsEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["IsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "MaxDumpCount":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["MaxDumpCount"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Password":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Password"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "QCAuthenticateMsgs":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string qcAuthenticateMsgsValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (qcAuthenticateMsgsValue)
                        {
                            case "secureApps":
                                properties["QCAuthenticateMsgs"] = "0";
                                break;
                            case "off":
                                properties["QCAuthenticateMsgs"] = "1";
                                break;
                            case "on":
                                properties["QCAuthenticateMsgs"] = "2";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "QCAuthenticateMsgs", qcAuthenticateMsgsValue, "secureApps", "off", "on"));
                                break;
                        }
                        break;
                    case "QCListenerMaxThreads":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["QCListenerMaxThreads"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "QueueListenerEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["QueueListenerEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "QueuingEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["QueuingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "RecycleActivationLimit":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RecycleActivationLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "RecycleCallLimit":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RecycleCallLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "RecycleExpirationTimeout":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RecycleExpirationTimeout"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "RecycleLifetimeLimit":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RecycleLifetimeLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "RecycleMemoryLimit":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RecycleMemoryLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Replicable":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Replicable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "RunForever":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["RunForever"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ShutdownAfter":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["ShutdownAfter"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SoapActivated":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["SoapActivated"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "SoapBaseUrl":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["SoapBaseUrl"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SoapMailTo":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["SoapMailTo"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SoapVRoot":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["SoapVRoot"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SRPEnabled":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["SRPEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "SRPTrustLevel":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        string srpTrustLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (srpTrustLevelValue)
                        {
                            case "disallowed":
                                properties["SRPTrustLevel"] = "0";
                                break;
                            case "fullyTrusted":
                                properties["SRPTrustLevel"] = "262144";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "SRPTrustLevel", srpTrustLevelValue, "disallowed", "fullyTrusted"));
                                break;
                        }
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null != componentKey && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name, "Id", "Name"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(child, componentKey, win64, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplication");
            row[0] = key;
            row[1] = partitionKey;
            row[2] = componentKey;
            row[3] = id;
            row[4] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }

            if (componentKey != null)
            {
                if (win64)
                {
                    if (this.Core.CurrentPlatform == Platform.IA64)
                    {
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.LocalName));
                    }
                    else
                    {
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                    }
                }
                else
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
                }
            }
        }

        ///	<summary>
        ///	Parses a COM+ application role element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusApplicationRoleElement(XmlNode node, string componentKey, string applicationKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Application":
                        if (null != applicationKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        applicationKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusApplication", applicationKey);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        if (null == componentKey)
                        {
                            this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name, attrib.Name));
                        }
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == applicationKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Application"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationRole");
            row[0] = key;
            row[1] = applicationKey;
            row[2] = componentKey;
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationRoleProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        ///	<summary>
        ///	Parses a COM+ application role user element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInApplicationRoleElement(XmlNode node, string componentKey, string applicationRoleKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string user = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "ApplicationRole":
                        if (null != applicationRoleKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusApplicationRole", applicationRoleKey);
                        break;
                    case "User":
                        user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "User", user);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == applicationRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ApplicationRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusUserInApplicationRole");
            row[0] = key;
            row[1] = applicationRoleKey;
            row[2] = componentKey;
            row[3] = user;
        }

        ///	<summary>
        ///	Parses a COM+ application role group element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInApplicationRoleElement(XmlNode node, string componentKey, string applicationRoleKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string group = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "ApplicationRole":
                        if (null != applicationRoleKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusApplicationRole", applicationRoleKey);
                        break;
                    case "Group":
                        group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Group", group);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == applicationRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "ApplicationRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusGroupInApplicationRole");
            row[0] = key;
            row[1] = applicationRoleKey;
            row[2] = componentKey;
            row[3] = group;
        }

        ///	<summary>
        ///	Parses a COM+ assembly element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusAssemblyElement(XmlNode node, string componentKey, bool win64, string applicationKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string assemblyName = null;
            string dllPath = null;
            string tlbPath = null;
            string psDllPath = null;
            int attributes = 0;

            bool hasComponents = false;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Application":
                        if (null != applicationKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        applicationKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusApplication", applicationKey);
                        break;
                    case "AssemblyName":
                        assemblyName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DllPath":
                        dllPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "TlbPath":
                        tlbPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "PSDllPath":
                        psDllPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Type":
                        string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (typeValue)
                        {
                            case ".net":
                                attributes |= (int)CpiAssemblyAttributes.DotNetAssembly;
                                break;
                            case "native":
                                attributes &= ~(int)CpiAssemblyAttributes.DotNetAssembly;
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusAssembly", "Type", typeValue, ".net", "native"));
                                break;
                        }
                        break;
                    case "EventClass":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)CpiAssemblyAttributes.EventClass;
                        }
                        else
                        {
                            attributes &= ~(int)CpiAssemblyAttributes.EventClass;
                        }
                        break;
                    case "DllPathFromGAC":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)CpiAssemblyAttributes.DllPathFromGAC;
                        }
                        else
                        {
                            attributes &= ~(int)CpiAssemblyAttributes.DllPathFromGAC;
                        }
                        break;
                    case "RegisterInCommit":
                        if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                        {
                            attributes |= (int)CpiAssemblyAttributes.RegisterInCommit;
                        }
                        else
                        {
                            attributes &= ~(int)CpiAssemblyAttributes.RegisterInCommit;
                        }
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == applicationKey && 0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Application", "Type", "native"));
            }
            if (null != assemblyName && 0 == (attributes & (int)CpiAssemblyAttributes.DllPathFromGAC))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithoutOtherValue(sourceLineNumbers, node.Name, "AssemblyName", "DllPathFromGAC", "no"));
            }
            if (null == tlbPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "TlbPath", "Type", ".net"));
            }
            if (null != psDllPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name, "PSDllPath", "Type", ".net"));
            }
            if (0 != (attributes & (int)CpiAssemblyAttributes.EventClass) && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name, "EventClass", "yes", "Type", ".net"));
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusAssemblyDependency":
                            this.ParseComPlusAssemblyDependencyElement(child, key);
                            break;
                        case "ComPlusComponent":
                            this.ParseComPlusComponentElement(child, componentKey, key);
                            hasComponents = true;
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            if (0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly) && !hasComponents)
            {
                this.Core.OnMessage(ComPlusWarnings.MissingComponents(sourceLineNumbers));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusAssembly");
            row[0] = key;
            row[1] = applicationKey;
            row[2] = componentKey;
            row[3] = assemblyName;
            row[4] = dllPath;
            row[5] = tlbPath;
            row[6] = psDllPath;
            row[7] = attributes;

            if (win64)
            {
                if (this.Core.CurrentPlatform == Platform.IA64)
                {
                    this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.LocalName));
                }
                else
                {
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                    this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                }
            }
            else
            {
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
            }
        }

        ///	<summary>
        ///	Parses a COM+ assembly dependency element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusAssemblyDependencyElement(XmlNode node, string assemblyKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string requiredAssemblyKey = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "RequiredAssembly":
                        requiredAssemblyKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusAssemblyDependency");
            row[0] = assemblyKey;
            row[1] = requiredAssemblyKey;
        }

        ///	<summary>
        ///	Parses a COM+ component element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusComponentElement(XmlNode node, string componentKey, string assemblyKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string clsid = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "CLSID":
                        clsid = "{" + this.Core.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                        break;
                    case "AllowInprocSubscribers":
                        properties["AllowInprocSubscribers"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ComponentAccessChecksEnabled":
                        properties["ComponentAccessChecksEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ComponentTransactionTimeout":
                        properties["ComponentTransactionTimeout"] = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3600).ToString();
                        break;
                    case "ComponentTransactionTimeoutEnabled":
                        properties["ComponentTransactionTimeoutEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "COMTIIntrinsics":
                        properties["COMTIIntrinsics"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ConstructionEnabled":
                        properties["ConstructionEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ConstructorString":
                        properties["ConstructorString"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CreationTimeout":
                        properties["CreationTimeout"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "EventTrackingEnabled":
                        properties["EventTrackingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ExceptionClass":
                        properties["ExceptionClass"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "FireInParallel":
                        properties["FireInParallel"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "IISIntrinsics":
                        properties["IISIntrinsics"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "InitializesServerApplication":
                        properties["InitializesServerApplication"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "IsEnabled":
                        properties["IsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "IsPrivateComponent":
                        properties["IsPrivateComponent"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "JustInTimeActivation":
                        properties["JustInTimeActivation"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "LoadBalancingSupported":
                        properties["LoadBalancingSupported"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "MaxPoolSize":
                        properties["MaxPoolSize"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MinPoolSize":
                        properties["MinPoolSize"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MultiInterfacePublisherFilterCLSID":
                        properties["MultiInterfacePublisherFilterCLSID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MustRunInClientContext":
                        properties["MustRunInClientContext"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "MustRunInDefaultContext":
                        properties["MustRunInDefaultContext"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "ObjectPoolingEnabled":
                        properties["ObjectPoolingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "PublisherID":
                        properties["PublisherID"] = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "SoapAssemblyName":
                        properties["SoapAssemblyName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "SoapTypeName":
                        properties["SoapTypeName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Synchronization":
                        string synchronizationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (synchronizationValue)
                        {
                            case "ignored":
                                properties["Synchronization"] = "0";
                                break;
                            case "none":
                                properties["Synchronization"] = "1";
                                break;
                            case "supported":
                                properties["Synchronization"] = "2";
                                break;
                            case "required":
                                properties["Synchronization"] = "3";
                                break;
                            case "requiresNew":
                                properties["Synchronization"] = "4";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Synchronization", synchronizationValue, "ignored", "none", "supported", "required", "requiresNew"));
                                break;
                        }
                        break;
                    case "Transaction":
                        string transactionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (transactionValue)
                        {
                            case "ignored":
                                properties["Transaction"] = "0";
                                break;
                            case "none":
                                properties["Transaction"] = "1";
                                break;
                            case "supported":
                                properties["Transaction"] = "2";
                                break;
                            case "required":
                                properties["Transaction"] = "3";
                                break;
                            case "requiresNew":
                                properties["Transaction"] = "4";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Transaction", transactionValue, "ignored", "none", "supported", "required", "requiresNew"));
                                break;
                        }
                        break;
                    case "TxIsolationLevel":
                        string txIsolationLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (txIsolationLevelValue)
                        {
                            case "any":
                                properties["TxIsolationLevel"] = "0";
                                break;
                            case "readUnCommitted":
                                properties["TxIsolationLevel"] = "1";
                                break;
                            case "readCommitted":
                                properties["TxIsolationLevel"] = "2";
                                break;
                            case "repeatableRead":
                                properties["TxIsolationLevel"] = "3";
                                break;
                            case "serializable":
                                properties["TxIsolationLevel"] = "4";
                                break;
                            default:
                                this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "TxIsolationLevel", txIsolationLevelValue, "any", "readUnCommitted", "readCommitted", "repeatableRead", "serializable"));
                                break;
                        }
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(child, componentKey, key);
                            break;
                        case "ComPlusInterface":
                            this.ParseComPlusInterfaceElement(child, componentKey, key);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusComponent");
            row[0] = key;
            row[1] = assemblyKey;
            row[2] = clsid;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusComponentProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        ///	<summary>
        ///	Parses a COM+ application role for component element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusRoleForComponentElement(XmlNode node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string applicationRoleKey = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Component":
                        if (null != cpcomponentKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        cpcomponentKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusComponent", cpcomponentKey);
                        break;
                    case "ApplicationRole":
                        applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == cpcomponentKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Component"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForComponent");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        ///	<summary>
        ///	Parses a COM+ interface element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusInterfaceElement(XmlNode node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            // parse attributes
            string key = null;
            string iid = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "IID":
                        iid = "{" + this.Core.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                        break;
                    case "Description":
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "QueuingEnabled":
                        properties["QueuingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(child, componentKey, key);
                            break;
                        case "ComPlusMethod":
                            this.ParseComPlusMethodElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusInterface");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = iid;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusInterfaceProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        ///	<summary>
        ///	Parses a COM+ application role for interface element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusRoleForInterfaceElement(XmlNode node, string componentKey, string interfaceKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string applicationRoleKey = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Interface":
                        if (null != interfaceKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        interfaceKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusInterface", interfaceKey);
                        break;
                    case "ApplicationRole":
                        applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == interfaceKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Interface"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForInterface");
            row[0] = key;
            row[1] = interfaceKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        ///	<summary>
        ///	Parses a COM+ method element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusMethodElement(XmlNode node, string componentKey, string interfaceKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            int index = CompilerCore.IntegerNotSet;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Index":
                        index = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "AutoComplete":
                        properties["AutoComplete"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "Description":
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    switch (child.LocalName)
                    {
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
            }

            if (CompilerCore.IntegerNotSet == index && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttribute(sourceLineNumbers, node.Name, "Index", "Name"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusMethod");
            row[0] = key;
            row[1] = interfaceKey;
            if (CompilerCore.IntegerNotSet != index)
            {
                row[2] = index;
            }
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusMethodProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        ///	<summary>
        ///	Parses a COM+ application role for method element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="methodKey">Identifier of parent method.</param>
        private void ParseComPlusRoleForMethodElement(XmlNode node, string componentKey, string methodKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string applicationRoleKey = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Method":
                        if (null != methodKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        methodKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusMethod", methodKey);
                        break;
                    case "ApplicationRole":
                        applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == methodKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Method"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForMethod");
            row[0] = key;
            row[1] = methodKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        ///	<summary>
        ///	Parses a COM+ event subscription element.
        ///	</summary>
        ///	<param name="node">Element to parse.</param>
        ///	<param name="componentKey">Identifier of parent component.</param>
        ///	<param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusSubscriptionElement(XmlNode node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);;

            string key = null;
            string id = null;
            string name = null;
            string eventCLSID = null;
            string publisherID = null;

            Hashtable properties = new Hashtable();

            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.LocalName)
                {
                    case "Id":
                        key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Component":
                        if (null != cpcomponentKey)
                        {
                            this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name, attrib.Name, node.ParentNode.Name));
                        }
                        cpcomponentKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "ComPlusComponent", cpcomponentKey);
                        break;
                    case "SubscriptionId":
                        id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "Name":
                        name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "EventCLSID":
                        eventCLSID = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "PublisherID":
                        publisherID = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "Description":
                        properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Enabled":
                        properties["Enabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "EventClassPartitionID":
                        properties["EventClassPartitionID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "FilterCriteria":
                        properties["FilterCriteria"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "InterfaceID":
                        properties["InterfaceID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MachineName":
                        properties["MachineName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "MethodName":
                        properties["MethodName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "PerUser":
                        properties["PerUser"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "Queued":
                        properties["Queued"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                        break;
                    case "SubscriberMoniker":
                        properties["SubscriberMoniker"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "UserName":
                        properties["UserName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(sourceLineNumbers, attrib);
                        break;
                }
            }

            if (null == cpcomponentKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Component"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusSubscription");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = componentKey;
            row[3] = id;
            row[4] = name;
            row[5] = eventCLSID;
            row[6] = publisherID;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusSubscriptionProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Attempts to parse the input value as a GUID, and in case the value is a valid
        /// GUID returnes it in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}".
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string TryFormatGuidValue(string val)
        {
            try
            {
                Guid guid = new Guid(val);
                return guid.ToString("B").ToUpper();
            }
            catch (FormatException)
            {
                return val;
            }
            catch (OverflowException)
            {
                return val;
            }
        }
    }
}
