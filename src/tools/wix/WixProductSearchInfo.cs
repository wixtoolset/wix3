// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Xml;

    /// <summary>
    /// Utility class for all WixProductSearches.
    /// </summary>
    internal class WixProductSearchInfo : WixSearchInfo
    {
        public WixProductSearchInfo(Row row)
            : this((string)row[0], (string)row[1], (int)row[2])
        {
        }

        public WixProductSearchInfo(string id, string guid, int attributes)
            : base(id)
        {
            this.Guid = guid;
            this.Attributes = (WixProductSearchAttributes)attributes;
        }

        public string Guid { get; private set; }
        public WixProductSearchAttributes Attributes { get; private set; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup for a product search.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("MsiProductSearch");
            this.WriteWixSearchAttributes(writer);

            if (0 != (this.Attributes & WixProductSearchAttributes.UpgradeCode))
            {
                writer.WriteAttributeString("UpgradeCode", this.Guid);
            }
            else
            {
                writer.WriteAttributeString("ProductCode", this.Guid);
            }

            if (0 != (this.Attributes & WixProductSearchAttributes.Version))
            {
                writer.WriteAttributeString("Type", "version");
            }
            else if (0 != (this.Attributes & WixProductSearchAttributes.Language))
            {
                writer.WriteAttributeString("Type", "language");
            }
            else if (0 != (this.Attributes & WixProductSearchAttributes.State))
            {
                writer.WriteAttributeString("Type", "state");
            }
            else if (0 != (this.Attributes & WixProductSearchAttributes.Assignment))
            {
                writer.WriteAttributeString("Type", "assignment");
            }

            writer.WriteEndElement();
        }
    }
}
