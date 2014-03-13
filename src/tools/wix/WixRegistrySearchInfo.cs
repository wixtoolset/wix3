//-------------------------------------------------------------------------------------------------
// <copyright file="WixRegistrySearchInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for all WixRegistrySearches.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Xml;

    /// <summary>
    /// Utility class for all WixRegistrySearches.
    /// </summary>
    internal class WixRegistrySearchInfo : WixSearchInfo
    {
        public WixRegistrySearchInfo(Row row)
            : this((string)row[0], (int)row[1], (string)row[2], (string)row[3], (int)row[4])
        {
        }

        public WixRegistrySearchInfo(string id, int root, string key, string value, int attributes)
            : base(id)
        {
            this.Root = root;
            this.Key = key;
            this.Value = value;
            this.Attributes = (WixRegistrySearchAttributes)attributes;
        }

        public int Root { get; private set; }
        public string Key { get; private set; }
        public string Value { get; private set; }
        public WixRegistrySearchAttributes Attributes { get; private set; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup for a registry search.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("RegistrySearch");
            this.WriteWixSearchAttributes(writer);

            switch (this.Root)
            {
                case Msi.Interop.MsiInterop.MsidbRegistryRootClassesRoot:
                    writer.WriteAttributeString("Root", "HKCR");
                    break;
                case Msi.Interop.MsiInterop.MsidbRegistryRootCurrentUser:
                    writer.WriteAttributeString("Root", "HKCU");
                    break;
                case Msi.Interop.MsiInterop.MsidbRegistryRootLocalMachine:
                    writer.WriteAttributeString("Root", "HKLM");
                    break;
                case Msi.Interop.MsiInterop.MsidbRegistryRootUsers:
                    writer.WriteAttributeString("Root", "HKU");
                    break;
            }

            writer.WriteAttributeString("Key", this.Key);

            if (!String.IsNullOrEmpty(this.Value))
            {
                writer.WriteAttributeString("Value", this.Value);
            }

            bool existenceOnly = 0 != (this.Attributes & WixRegistrySearchAttributes.WantExists);

            writer.WriteAttributeString("Type", existenceOnly ? "exists" : "value");

            if (0 != (this.Attributes & WixRegistrySearchAttributes.Win64))
            {
                writer.WriteAttributeString("Win64", "yes");
            }

            if (!existenceOnly)
            {
                if (0 != (this.Attributes & WixRegistrySearchAttributes.ExpandEnvironmentVariables))
                {
                    writer.WriteAttributeString("ExpandEnvironment", "yes");
                }

                // We *always* say this is VariableType="string". If we end up
                // needing to be more specific, we will have to expand the "Format"
                // attribute to allow "number" and "version".

                writer.WriteAttributeString("VariableType", "string");
            }

            writer.WriteEndElement();
        }
    }

}
