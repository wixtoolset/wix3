// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
    /// The compiler for the Windows Installer XML Toolset Bal Extension.
    /// </summary>
    public sealed class BalCompiler : CompilerExtension
    {
        private SourceLineNumberCollection addedConditionLineNumber;
        private XmlSchema schema;

        /// <summary>
        /// Instantiate a new BalCompiler.
        /// </summary>
        public BalCompiler()
        {
            this.addedConditionLineNumber = null;
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.bal.xsd");
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
                case "Bundle":
                case "Fragment":
                    switch (element.LocalName)
                    {
                        case "Condition":
                            this.ParseConditionElement(element);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "BootstrapperApplicationRef":
                    switch (element.LocalName)
                    {
                        case "WixStandardBootstrapperApplication":
                            this.ParseWixStandardBootstrapperApplicationElement(element);
                            break;
                        case "WixManagedBootstrapperApplicationHost":
                            this.ParseWixManagedBootstrapperApplicationHostElement(element);
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
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="attribute">Attribute to process.</param>
        public override void ParseAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute)
        {
            switch (parentElement.LocalName)
            {
                case "BootstrapperApplication":
                case "BootstrapperApplicationRef":
                    switch (attribute.LocalName)
                    {
                        case "UseUILanguages":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                            {
                                Row row = this.Core.CreateRow(sourceLineNumbers, "WixStdbaSettings");
                                row[0] = "UseUILanguages";
                                row[1] = "1";
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                    break;
            }
        }

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="attribute">Attribute to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute, Dictionary<string, string> contextValues)
        {
            switch (parentElement.LocalName)
            {
                case "ExePackage":
                case "MsiPackage":
                case "MspPackage":
                case "MsuPackage":
                    string packageId;
                    if (!contextValues.TryGetValue("PackageId", out packageId) || String.IsNullOrEmpty(packageId))
                    {
                        this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, parentElement.LocalName, "Id", attribute.LocalName));
                    }
                    else
                    {
                        switch (attribute.LocalName)
                        {
                            case "PrereqSupportPackage":
                                if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    Row row = this.Core.CreateRow(sourceLineNumbers, "MbaPrerequisiteSupportPackage");
                                    row[0] = packageId;
                                }
                                break;
                            default:
                                this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                                break;
                        }
                    }
                    break;
                case "Variable":
                    // at the time the extension attribute is parsed, the compiler might not yet have
                    // parsed the Name attribute, so we need to get it directly from the parent element.
                    string variableName = parentElement.GetAttribute("Name");
                    if (String.IsNullOrEmpty(variableName))
                    {
                        this.Core.OnMessage(WixErrors.ExpectedParentWithAttribute(sourceLineNumbers, "Variable", "Overridable", "Name"));
                    }
                    else
                    {
                        switch (attribute.LocalName)
                        {
                            case "Overridable":
                                if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attribute))
                                {
                                    Row row = this.Core.CreateRow(sourceLineNumbers, "WixStdbaOverridableVariable");
                                    row[0] = variableName;
                                }
                                break;
                            default:
                                this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                                break;
                        }
                    }
                    break;
                default:
                    this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                    break;
            }
        }

        /// <summary>
        /// Parses a Condition element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseConditionElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string condition = CompilerCore.GetConditionInnerText(node); // condition is the inner text of the element.
            string message = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Message":
                            message = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
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

            // Error check the values.
            if (String.IsNullOrEmpty(condition))
            {
                this.Core.OnMessage(WixErrors.ConditionExpected(sourceLineNumbers, node.Name));
            }

            if (null == message)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "Message"));
            }

            if (!this.Core.EncounteredError)
            {
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixBalCondition");
                row[0] = condition;
                row[1] = message;

                if (null == this.addedConditionLineNumber)
                {
                    this.addedConditionLineNumber = sourceLineNumbers;
                }
            }
        }

        /// <summary>
        /// Parses a WixStandardBootstrapperApplication element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixStandardBootstrapperApplicationElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string launchTarget = null;
            string launchTargetElevatedId = null;
            string launchArguments = null;
            YesNoType launchHidden = YesNoType.NotSet;
            string launchWorkingDir = null;
            string licenseFile = null;
            string licenseUrl = null;
            string logoFile = null;
            string logoSideFile = null;
            string themeFile = null;
            string localizationFile = null;
            YesNoType suppressOptionsUI = YesNoType.NotSet;
            YesNoType suppressDowngradeFailure = YesNoType.NotSet;
            YesNoType suppressRepair = YesNoType.NotSet;
            YesNoType showVersion = YesNoType.NotSet;
            YesNoType supportCacheOnly = YesNoType.NotSet;
            YesNoType showFilesInUse = YesNoType.NotSet;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "LaunchTarget":
                            launchTarget = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LaunchTargetElevatedId":
                            launchTargetElevatedId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchArguments":
                            launchArguments = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LaunchHidden":
                            launchHidden = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LaunchWorkingFolder":
                            launchWorkingDir = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LicenseFile":
                            licenseFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LicenseUrl":
                            licenseUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib, true);
                            break;
                        case "LogoFile":
                            logoFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LogoSideFile":
                            logoSideFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ThemeFile":
                            themeFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "SuppressOptionsUI":
                            suppressOptionsUI = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressDowngradeFailure":
                            suppressDowngradeFailure = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SuppressRepair":
                            suppressRepair = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ShowVersion":
                            showVersion = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "SupportCacheOnly":
                            supportCacheOnly = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "ShowFilesInUse":
                            showFilesInUse = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (String.IsNullOrEmpty(licenseFile) && null == licenseUrl)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(launchTarget))
                {
                    this.Core.CreateVariableRow(sourceLineNumbers, "LaunchTarget", launchTarget, "string", false, false);
                }

                if (!String.IsNullOrEmpty(launchTargetElevatedId))
                {
                    this.Core.CreateVariableRow(sourceLineNumbers, "LaunchTargetElevatedId", launchTargetElevatedId, "string", false, false);
                }

                if (!String.IsNullOrEmpty(launchArguments))
                {
                    this.Core.CreateVariableRow(sourceLineNumbers, "LaunchArguments", launchArguments, "string", false, false);
                }

                if (YesNoType.Yes == launchHidden)
                {
                    this.Core.CreateVariableRow(sourceLineNumbers, "LaunchHidden", "yes", "string", false, false);
                }

                if (!String.IsNullOrEmpty(launchWorkingDir))
                {
                    this.Core.CreateVariableRow(sourceLineNumbers, "LaunchWorkingFolder", launchWorkingDir, "string", false, false);
                }

                if (!String.IsNullOrEmpty(licenseFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaLicenseRtf", licenseFile, false);
                }

                if (null != licenseUrl)
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaLicenseUrl", licenseUrl, false);
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaLogo", logoFile, false);
                }

                if (!String.IsNullOrEmpty(logoSideFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaLogoSide", logoSideFile, false);
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaThemeXml", themeFile, false);
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixStdbaThemeWxl", localizationFile, false);
                }

                if (YesNoType.Yes == suppressOptionsUI || YesNoType.Yes == suppressDowngradeFailure || YesNoType.Yes == suppressRepair || YesNoType.Yes == showVersion || YesNoType.Yes == supportCacheOnly || YesNoType.Yes == showFilesInUse)
                {
                    Row row = this.Core.CreateRow(sourceLineNumbers, "WixStdbaOptions");
                    if (YesNoType.Yes == suppressOptionsUI)
                    {
                        row[0] = 1;
                    }

                    if (YesNoType.Yes == suppressDowngradeFailure)
                    {
                        row[1] = 1;
                    }

                    if (YesNoType.Yes == suppressRepair)
                    {
                        row[2] = 1;
                    }

                    if (YesNoType.Yes == showVersion)
                    {
                        row[3] = 1;
                    }

                    if (YesNoType.Yes == showFilesInUse)
                    {
                        row[4] = 1;
                    }

                    if (YesNoType.Yes == supportCacheOnly)
                    {
                        row[5] = 1;
                    }
                }
            }
        }

        /// <summary>
        /// Parses a WixManagedBootstrapperApplicationHost element for Bundles.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        private void ParseWixManagedBootstrapperApplicationHostElement(XmlNode node)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string licenseFile = null;
            string licenseUrl = null;
            string logoFile = null;
            string themeFile = null;
            string localizationFile = null;
            string netFxPackageId = null;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "LicenseFile":
                            licenseFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LicenseUrl":
                            licenseUrl = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LogoFile":
                            logoFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "ThemeFile":
                            themeFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "LocalizationFile":
                            localizationFile = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
                            break;
                        case "NetFxPackageId":
                            netFxPackageId = this.Core.GetAttributeValue(sourceLineNumbers, attrib, false);
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

            if (String.IsNullOrEmpty(licenseFile) == String.IsNullOrEmpty(licenseUrl))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name, "LicenseFile", "LicenseUrl", true));
            }

            if (!this.Core.EncounteredError)
            {
                if (!String.IsNullOrEmpty(licenseFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixMbaPrereqLicenseRtf", licenseFile, false);
                }

                if (!String.IsNullOrEmpty(licenseUrl))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixMbaPrereqLicenseUrl", licenseUrl, false);
                }

                if (!String.IsNullOrEmpty(logoFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "PreqbaLogo", logoFile, false);
                }

                if (!String.IsNullOrEmpty(themeFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "PreqbaThemeXml", themeFile, false);
                }

                if (!String.IsNullOrEmpty(localizationFile))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "PreqbaThemeWxl", localizationFile, false);
                }

                if (!String.IsNullOrEmpty(netFxPackageId))
                {
                    this.Core.CreateWixVariableRow(sourceLineNumbers, "WixMbaPrereqPackageId", netFxPackageId, false);
                }
            }
        }
    }
}
