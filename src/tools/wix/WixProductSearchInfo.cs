//-------------------------------------------------------------------------------------------------
// <copyright file="WixProductSearchInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for all WixProductSearches.
// </summary>
//-------------------------------------------------------------------------------------------------

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
