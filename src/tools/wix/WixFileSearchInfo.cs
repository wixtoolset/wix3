//-------------------------------------------------------------------------------------------------
// <copyright file="WixFileSearchInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for all WixFileSearches (file and directory searches).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Xml;

    /// <summary>
    /// Utility class for all WixFileSearches (file and directory searches).
    /// </summary>
    internal class WixFileSearchInfo : WixSearchInfo
    {
        public WixFileSearchInfo(Row row)
            : this((string)row[0], (string)row[1], (int)row[9])
        {
        }

        public WixFileSearchInfo(string id, string path, int attributes)
            : base(id)
        {
            this.Path = path;
            this.Attributes = (WixFileSearchAttributes)attributes;
        }

        public string Path { get; private set; }
        public WixFileSearchAttributes Attributes { get; private set; }

        /// <summary>
        /// Generates Burn manifest and ParameterInfo-style markup for a file/directory search.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement((0 == (this.Attributes & WixFileSearchAttributes.IsDirectory)) ? "FileSearch" : "DirectorySearch");
            this.WriteWixSearchAttributes(writer);
            writer.WriteAttributeString("Path", this.Path);
            if (WixFileSearchAttributes.WantExists == (this.Attributes & WixFileSearchAttributes.WantExists))
            {
                writer.WriteAttributeString("Type", "exists");
            }
            else if (WixFileSearchAttributes.WantVersion == (this.Attributes & WixFileSearchAttributes.WantVersion))
            {
                // Can never get here for DirectorySearch.
                writer.WriteAttributeString("Type", "version");
            }
            else
            {
                writer.WriteAttributeString("Type", "path");
            }
            writer.WriteEndElement();
        }
    }
}
