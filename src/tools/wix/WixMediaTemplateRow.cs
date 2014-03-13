//-------------------------------------------------------------------------------------------------
// <copyright file="WixMediaTemplateRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the MediaTeplate table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Specialization of a row for the MediaTemplate table.
    /// </summary>
    public sealed class WixMediaTemplateRow : Row
    {
        /// <summary>
        /// Creates a MediaTemplate row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this MediaTeplate row belongs to and should get its column definitions from.</param>
        public WixMediaTemplateRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the cabinet template name for this media template row.
        /// </summary>
        /// <value>Cabinet name.</value>
        public string CabinetTemplate
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the compression level for this media template row.
        /// </summary>
        /// <value>Compression level.</value>
        public string CompressionLevel
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the disk prompt for this media template row.
        /// </summary>
        /// <value>Disk prompt.</value>
        public string DiskPrompt
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }


        /// <summary>
        /// Gets or sets the volume label for this media template row.
        /// </summary>
        /// <value>Volume label.</value>
        public string VolumeLabel
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the maximum uncompressed media size for this media template row.
        /// </summary>
        /// <value>Disk id.</value>
        public int MaximumUncompressedMediaSize
        {
            get { return (int)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Maximum Cabinet Size For Large File Splitting for this media template row.
        /// </summary>
        /// <value>Disk id.</value>
        public int MaximumCabinetSizeForLargeFileSplitting
        {
            get { return (int)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }
    }
}
