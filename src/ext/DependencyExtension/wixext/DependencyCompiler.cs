// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Dependency;

    using Wix = Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The compiler for the Windows Installer XML toolset dependency extension.
    /// </summary>
    public sealed class DependencyCompiler : CompilerExtension
    {
        /// <summary>
        /// Package type when parsing the Provides element.
        /// </summary>
        private enum PackageType
        {
            None,
            ExePackage,
            MsiPackage,
            MspPackage,
            MsuPackage
        }

        private XmlSchema schema;

        public DependencyCompiler()
        {
            this.schema = LoadXmlSchemaHelper(Assembly.GetExecutingAssembly(), "Microsoft.Tools.WindowsInstallerXml.Extensions.Xsd.Dependency.xsd");
        }

        /// <summary>
        /// Gets the schema for this extension.
        /// </summary>
        public override XmlSchema Schema
        {
            get { return this.schema; }
        }

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        public override void ParseAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute)
        {
            switch (parentElement.LocalName)
            {
                case "Bundle":
                    switch (attribute.LocalName)
                    {
                        case "ProviderKey":
                            this.ParseProviderKeyAttribute(sourceLineNumbers, parentElement, attribute);
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
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, params string[] contextValues)
        {
            PackageType packageType = PackageType.None;

            switch (parentElement.LocalName)
            {
                case "Bundle":
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(element, null, false);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ExePackage":
                    packageType = PackageType.ExePackage;
                    break;
                case "MsiPackage":
                    packageType = PackageType.MsiPackage;
                    break;
                case "MspPackage":
                    packageType = PackageType.MspPackage;
                    break;
                case "MsuPackage":
                    packageType = PackageType.MsuPackage;
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }

            if (PackageType.None != packageType)
            {
                string packageId = contextValues[0];

                switch (element.LocalName)
                {
                    case "Provides":
                        string keyPath = null;
                        this.ParseProvidesElement(element, packageType, ref keyPath, packageId);
                        break;
                    default:
                        this.Core.UnexpectedElement(parentElement, element);
                        break;
                }
            }
        }

        /// <summary>
        /// Processes a child element of a Component for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="keyPath">Explicit key path.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        /// <returns>The component key path type if set.</returns>
        public override CompilerExtension.ComponentKeypathType ParseElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, ref string keyPath, params string[] contextValues)
        {
            CompilerExtension.ComponentKeypathType keyPathType = CompilerExtension.ComponentKeypathType.None;

            switch (parentElement.LocalName)
            {
                case "Component":
                    string componentId = contextValues[0];

                    // 64-bit components may cause issues downlevel.
                    bool win64 = false;
                    Boolean.TryParse(contextValues[2], out win64);

                    switch (element.LocalName)
                    {
                        case "Provides":
                            if (win64)
                            {
                                this.Core.OnMessage(DependencyWarnings.Win64Component(sourceLineNumbers, componentId));
                            }

                            keyPathType = this.ParseProvidesElement(element, PackageType.None, ref keyPath, componentId);
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

            return keyPathType;
        }

        /// <summary>
        /// Processes the ProviderKey bundle attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">The XML attribute for the ProviderKey attribute.</param>
        private void ParseProviderKeyAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlAttribute attribute)
        {
            string id = null;
            string providerKey = null;
            int illegalChar = -1;

            switch (attribute.LocalName)
            {
                case "ProviderKey":
                    providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attribute);
                    break;
                default:
                    this.Core.UnexpectedAttribute(sourceLineNumbers, attribute);
                    break;
            }

            // Make sure the key does not contain any illegal characters or values.
            if (String.IsNullOrEmpty(providerKey))
            {
                this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, parentElement.LocalName, attribute.LocalName));
            }
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }
            else if ("ALL" == providerKey)
            {
                this.Core.OnMessage(DependencyErrors.ReservedValue(sourceLineNumbers, parentElement.LocalName, "ProviderKey", providerKey));
            }

            // Generate the primary key for the row.
            id = this.Core.GenerateIdentifier("dep", attribute.LocalName, providerKey);

            if (!this.Core.EncounteredError)
            {
                // Create the provider row for the bundle. The Component_ field is required
                // in the table definition but unused for bundles, so just set it to the valid ID.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyProvider");
                row[0] = id;
                row[1] = id;
                row[2] = providerKey;
                row[5] = DependencyCommon.ProvidesAttributesBundle;
            }
        }

        /// <summary>
        /// Processes the Provides element.
        /// </summary>
        /// <param name="node">The XML node for the Provides element.</param>
        /// <param name="packageType">The type of the package being chained into a bundle, or "None" if building an MSI package.</param>
        /// <param name="keyPath">Explicit key path.</param>
        /// <param name="parentId">The identifier of the parent component or package.</param>
        /// <returns>The type of key path if set.</returns>
        private CompilerExtension.ComponentKeypathType ParseProvidesElement(XmlNode node, PackageType packageType, ref string keyPath, string parentId)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            CompilerExtension.ComponentKeypathType keyPathType = CompilerExtension.ComponentKeypathType.None;
            string id = null;
            string key = null;
            string version = null;
            string displayName = null;
            int attributes = 0;
            int illegalChar = -1;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib, true);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // Make sure the key is valid. The key will default to the ProductCode for MSI packages
            // and the package code for MSP packages in the binder if not specified.
            if (!String.IsNullOrEmpty(key))
            {
                // Make sure the key does not contain any illegal characters or values.
                if (0 <= (illegalChar = key.IndexOfAny(DependencyCommon.InvalidCharacters)))
                {
                    StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                    Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                    this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "Key", key[illegalChar], sb.ToString()));
                }
                else if ("ALL" == key)
                {
                    this.Core.OnMessage(DependencyErrors.ReservedValue(sourceLineNumbers, node.LocalName, "Key", key));
                }
            }
            else if (PackageType.ExePackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the provider key when authored for a package.
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.LocalName, "Key"));
            }
            else if (PackageType.None == packageType)
            {
                // Make sure the ProductCode is authored and set the key.
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "Property", "ProductCode");
                key = "!(bind.property.ProductCode)";
            }

            // The Version attribute should not be authored in or for an MSI package.
            if (!String.IsNullOrEmpty(version))
            {
                switch (packageType)
                {
                    case PackageType.None:
                        this.Core.OnMessage(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers));
                        break;
                    case PackageType.MsiPackage:
                        this.Core.OnMessage(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers, parentId));
                        break;
                }
            }
            else if (PackageType.MspPackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the Version when authored for packages that do not contain a version.
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.LocalName, "Version"));
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (String.IsNullOrEmpty(id))
            {
                id = this.Core.GenerateIdentifier("dep", node.LocalName, parentId, key);
            }

            foreach (XmlNode child in node.ChildNodes)
            {
                if (XmlNodeType.Element == child.NodeType)
                {
                    if (child.NamespaceURI == this.schema.TargetNamespace)
                    {
                        switch (child.LocalName)
                        {
                            case "Requires":
                                this.ParseRequiresElement(child, id, PackageType.None == packageType);
                                break;
                            case "RequiresRef":
                                this.ParseRequiresRefElement(child, id, PackageType.None == packageType);
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
                // Create the row in the provider table.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyProvider");
                row[0] = id;
                row[1] = parentId;
                row[2] = key;

                if (!String.IsNullOrEmpty(version))
                {
                    row[3] = version;
                }

                if (!String.IsNullOrEmpty(displayName))
                {
                    row[4] = displayName;
                }

                if (0 != attributes)
                {
                    row[5] = attributes;
                }

                if (PackageType.None == packageType)
                {
                    // Reference the Check custom action to check for dependencies on the current provider.
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyCheck_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyCheck");
                    }

                    // Generate registry rows for the provider using binder properties.
                    string keyProvides = String.Concat(DependencyCommon.RegistryRoot, key);

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry");
                    row[0] = this.Core.GenerateIdentifier("reg", id, "(Default)");
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = null;
                    row[4] = "[ProductCode]";
                    row[5] = parentId;

                    // Use the Version registry value as the key path if not already set.
                    string idVersion = this.Core.GenerateIdentifier("reg", id, "Version");
                    if (String.IsNullOrEmpty(keyPath))
                    {
                        keyPath = idVersion;
                        keyPathType = CompilerExtension.ComponentKeypathType.Registry;
                    }

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry");
                    row[0] = idVersion;
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = "Version";
                    row[4] = !String.IsNullOrEmpty(version) ? version : "[ProductVersion]";
                    row[5] = parentId;

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry");
                    row[0] = this.Core.GenerateIdentifier("reg", id, "DisplayName");
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = "DisplayName";
                    row[4] = !String.IsNullOrEmpty(displayName) ? displayName : "[ProductName]";
                    row[5] = parentId;

                    if (0 != attributes)
                    {
                        row = this.Core.CreateRow(sourceLineNumbers, "Registry");
                        row[0] = this.Core.GenerateIdentifier("reg", id, "Attributes");
                        row[1] = -1;
                        row[2] = keyProvides;
                        row[3] = "Attributes";
                        row[4] = String.Concat("#", attributes.ToString(CultureInfo.InvariantCulture.NumberFormat));
                        row[5] = parentId;
                    }
                }
            }

            return keyPathType;
        }

        /// <summary>
        /// Processes the Requires element.
        /// </summary>
        /// <param name="node">The XML node for the Requires element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresElement(XmlNode node, string providerId, bool requiresAction)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;
            string providerKey = null;
            string minVersion = null;
            string maxVersion = null;
            int attributes = 0;
            int illegalChar = -1;

            foreach (XmlAttribute attrib in node.Attributes)
            {
                if (0 == attrib.NamespaceURI.Length || attrib.NamespaceURI == this.schema.TargetNamespace)
                {
                    switch (attrib.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ProviderKey":
                            providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib, true);
                            break;
                        case "Maximum":
                            maxVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib, true);
                            break;
                        case "IncludeMinimum":
                            if (Wix.YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMinVersionInclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (Wix.YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMaxVersionInclusive;
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
                // Generate an ID only if this element is authored under a Provides element; otherwise, a RequiresRef
                // element will be necessary and the Id attribute will be required.
                if (!String.IsNullOrEmpty(providerId))
                {
                    id = this.Core.GenerateIdentifier("dep", node.LocalName, providerKey);
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.LocalName, "Id", "Provides"));
                }
            }

            if (String.IsNullOrEmpty(providerKey))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.LocalName, "ProviderKey"));
            }
            // Make sure the key does not contain any illegal characters.
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }


            if (!this.Core.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyRequire_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyRequire");
                    }
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependency");
                row[0] = id;
                row[1] = providerKey;
                row[2] = minVersion;
                row[3] = maxVersion;

                if (0 != attributes)
                {
                    row[4] = attributes;
                }

                // Create the relationship between this WixDependency row and the WixDependencyProvider row.
                if (!String.IsNullOrEmpty(providerId))
                {
                    // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                    row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyRef");
                    row[0] = providerId;
                    row[1] = id;
                }
            }
        }

        /// <summary>
        /// Processes the RequiresRef element.
        /// </summary>
        /// <param name="node">The XML node for the RequiresRef element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresRefElement(XmlNode node, string providerId, bool requiresAction)
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
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.LocalName, "Id"));
            }

            if (!this.Core.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyRequire_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "CustomAction", "WixDependencyRequire");
                    }
                }

                // Create a link dependency on the row that contains information we'll need during bind.
                this.Core.CreateWixSimpleReferenceRow(sourceLineNumbers, "WixDependency", id);

                // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyRef");
                row[0] = providerId;
                row[1] = id;
            }
        }
    }
}
