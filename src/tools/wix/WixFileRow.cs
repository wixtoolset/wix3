//-------------------------------------------------------------------------------------------------
// <copyright file="WixFileRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Specialization of a row for the file table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// PatchAttribute values
    /// </summary>
    [Flags]
    public enum PatchAttributeType
    {
        None = 0,

        /// <summary>Prevents the updating of the file that is in fact changed in the upgraded image relative to the target images.</summary>
        Ignore = 1,

        /// <summary>Set if the entire file should be installed rather than creating a binary patch.</summary>
        IncludeWholeFile = 2,

        /// <summary>Set to indicate that the patch is non-vital.</summary>
        AllowIgnoreOnError = 4,

        /// <summary>Allowed bits.</summary>
        Defined = Ignore | IncludeWholeFile | AllowIgnoreOnError
    }

    /// <summary>
    /// Specialization of a row for the WixFile table.
    /// </summary>
    public sealed class WixFileRow : Row
    {
        /// <summary>
        /// Creates a WixFile row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixFileRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixFile row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this File row belongs to and should get its column definitions from.</param>
        public WixFileRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the application for the assembly.
        /// </summary>
        /// <value>Application for the assembly.</value>
        public string AssemblyApplication
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the assembly attributes of the file row.
        /// </summary>
        /// <value>Assembly attributes of the file row.</value>
        public int AssemblyAttributes
        {
            get { return Convert.ToInt32(this.Fields[1].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the identifier for the assembly manifest.
        /// </summary>
        /// <value>Identifier for the assembly manifest.</value>
        public string AssemblyManifest
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the attributes on a file.
        /// </summary>
        /// <value>Attributes on a file.</value>
        public int Attributes
        {
            get { return Convert.ToInt32(this.Fields[9].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[9].Data = value; }
        }

        /// <summary>
        /// Gets or sets the directory of the file.
        /// </summary>
        /// <value>Directory of the file.</value>
        public string Directory
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the disk id for this file.
        /// </summary>
        /// <value>Disk id for the file.</value>
        public int DiskId
        {
            get { return Convert.ToInt32(this.Fields[5].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the primary key of the file row.
        /// </summary>
        /// <value>Primary key of the file row.</value>
        public string File
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets of sets the patch group of a patch-added file.
        /// </summary>
        /// <value>The patch group of a patch-added file.</value>
        public int PatchGroup
        {
            get { return Convert.ToInt32(this.Fields[8].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[8].Data = value; }
        }

        /// <summary>
        /// Gets or sets the architecture the file executes on.
        /// </summary>
        /// <value>Architecture the file executes on.</value>
        public string ProcessorArchitecture
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string Source
        {
            get { return (string)this.Fields[6].Data; }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string PreviousSource
        {
            get { return (string)this.Fields[6].PreviousData; }
            set { this.Fields[6].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the patching attributes to the file.
        /// </summary>
        /// <value>Patching attributes of the file.</value>
        public PatchAttributeType PatchAttributes
        {
            get { return (PatchAttributeType) Convert.ToInt32(this.Fields[10].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[10].Data = (int) value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-length list for the file.
        /// </summary>
        /// <value>RetainLength list for the file.</value>
        public string RetainLengths
        {
            get { return (string)this.Fields[11].Data; }
            set { this.Fields[11].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch retain-length list for the file.
        /// </summary>
        /// <value>Previous RetainLength list for the file.</value>
        public string PreviousRetainLengths
        {
            get { return this.Fields[11].PreviousData; }
            set { this.Fields[11].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-offset list for the file.
        /// </summary>
        /// <value>IgnoreOffset list for the file.</value>
        public string IgnoreOffsets
        {
            get { return (string)this.Fields[12].Data; }
            set { this.Fields[12].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch ignore-offset list for the file.
        /// </summary>
        /// <value>Previous IgnoreOffset list for the file.</value>
        public string PreviousIgnoreOffsets
        {
            get { return this.Fields[12].PreviousData; }
            set { this.Fields[12].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-length list for the file.
        /// </summary>
        /// <value>IgnoreLength list for the file.</value>
        public string IgnoreLengths
        {
            get { return (string)this.Fields[13].Data; }
            set { this.Fields[13].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch ignore-length list for the file.
        /// </summary>
        /// <value>Previous IgnoreLength list for the file.</value>
        public string PreviousIgnoreLengths
        {
            get { return this.Fields[13].PreviousData; }
            set { this.Fields[13].PreviousData = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-offset list for the file.
        /// </summary>
        /// <value>RetainOffset list for the file.</value>
        public string RetainOffsets
        {
            get { return (string)this.Fields[14].Data; }
            set { this.Fields[14].Data = value; }
        }

        /// <summary>
        /// Gets or sets the previous delta patch retain-offset list for the file.
        /// </summary>
        /// <value>PreviousRetainOffset list for the file.</value>
        public string PreviousRetainOffsets
        {
            get { return this.Fields[14].PreviousData; }
            set { this.Fields[14].PreviousData = value; }
        }
    }
}
