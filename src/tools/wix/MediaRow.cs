//-------------------------------------------------------------------------------------------------
// <copyright file="MediaRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the Media table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Specialization of a row for the Media table.
    /// </summary>
    public sealed class MediaRow : Row
    {
        private Cab.CompressionLevel compressionLevel;
        private bool hasExplicitCompressionLevel;
        private string layout;

        /// <summary>
        /// Creates a Media row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public MediaRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
            // default the compression level to mszip
            this.compressionLevel = Cab.CompressionLevel.Mszip;
        }

        /// <summary>
        /// Gets or sets the disk id for this media row.
        /// </summary>
        /// <value>Disk id.</value>
        public int DiskId
        {
            get { return (int)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the last sequence number for this media row.
        /// </summary>
        /// <value>Last sequence number.</value>
        public int LastSequence
        {
            get { return (int)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the disk prompt for this media row.
        /// </summary>
        /// <value>Disk prompt.</value>
        public string DiskPrompt
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the cabinet name for this media row.
        /// </summary>
        /// <value>Cabinet name.</value>
        public string Cabinet
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the volume label for this media row.
        /// </summary>
        /// <value>Volume label.</value>
        public string VolumeLabel
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source for this media row.
        /// </summary>
        /// <value>Source.</value>
        public string Source
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the compression level for this media row.
        /// </summary>
        /// <value>Compression level.</value>
        public Cab.CompressionLevel CompressionLevel
        {
            get { return this.compressionLevel; }
            set
            {
                this.compressionLevel = value;
                this.hasExplicitCompressionLevel = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the compression level for this media row has been set.
        /// </summary>
        /// <value>Compression level.</value>
        public bool HasExplicitCompressionLevel
        {
            get { return this.hasExplicitCompressionLevel; }
        }

        /// <summary>
        /// Gets or sets the layout location for this media row.
        /// </summary>
        /// <value>Layout location to the root of the media.</value>
        public string Layout
        {
            get { return this.layout; }
            set { this.layout = value; }
        }
    }
}
