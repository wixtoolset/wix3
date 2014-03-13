//-------------------------------------------------------------------------------------------------
// <copyright file="FileRow.cs" company="Outercurve Foundation">
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
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Every file row has an assembly type.
    /// </summary>
    public enum FileAssemblyType
    {
        /// <summary>File is not an assembly.</summary>
        NotAnAssembly = -1,

        /// <summary>File is a Common Language Runtime Assembly.</summary>
        DotNetAssembly = 0,

        /// <summary>File is Win32 SxS assembly.</summary>
        Win32Assembly = 1,
    }

    /// <summary>
    /// Specialization of a row for the file table.
    /// </summary>
    public sealed class FileRow : Row, IComparable
    {
        private string assemblyApplication;
        private string assemblyManifest;
        private FileAssemblyType assemblyType;
        private string directory;
        private int diskId;
        private bool fromModule;
        private bool isGeneratedShortFileName;
        private int patchGroup;
        private string processorArchitecture;
        private string source;
        private Row hashRow;
        private RowCollection assemblyNameRows;
        private string[] previousSource;
        private string symbols;
        private string[] previousSymbols;
        private PatchAttributeType patchAttributes;
        private string retainOffsets;
        private string retainLengths;
        private string ignoreOffsets;
        private string ignoreLengths;
        private string[] previousRetainOffsets;
        private string[] previousRetainLengths;
        private string[] previousIgnoreOffsets;
        private string[] previousIgnoreLengths;
        private string patch;

        /// <summary>
        /// Creates a File row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this File row belongs to and should get its column definitions from.</param>
        public FileRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
            this.assemblyType = FileAssemblyType.NotAnAssembly;
            this.previousSource = new string[1];
            this.previousSymbols = new string[1];
            this.previousRetainOffsets = new string[1];
            this.previousRetainLengths = new string[1];
            this.previousIgnoreOffsets = new string[1];
            this.previousIgnoreLengths = new string[1];
        }

        /// <summary>
        /// Creates a File row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDefinition">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public FileRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDefinition)
            : base(sourceLineNumbers, tableDefinition)
        {
            this.assemblyType = FileAssemblyType.NotAnAssembly;
            this.previousSource = new string[1];
            this.previousSymbols = new string[1];
            this.previousRetainOffsets = new string[1];
            this.previousRetainLengths = new string[1];
            this.previousIgnoreOffsets = new string[1];
            this.previousIgnoreLengths = new string[1];
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
        /// Gets or sets the component this file row belongs to.
        /// </summary>
        /// <value>Component this file row belongs to.</value>
        public string Component
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>Name of the file.</value>
        public string FileName
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the real filesystem name of the file (without a pipe). This is typically the long name of the file.
        /// However, if no long name is available, falls back to the short name.
        /// </summary>
        /// <value>Long Name of the file - or if no long name is available, falls back to the short name.</value>
        public string LongFileName
        {
            get
            {
                string fileName = this.FileName;
                int index = fileName.IndexOf('|');

                // If it doesn't contain a pipe, just return the whole string
                if (-1 == index)
                {
                    return fileName;
                }
                else // otherwise, extract the part of the string after the pipe
                {
                    return fileName.Substring(index + 1);
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the file.
        /// </summary>
        /// <value>Size of the file.</value>
        public int FileSize
        {
            get { return (int)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets or sets the version of the file.
        /// </summary>
        /// <value>Version of the file.</value>
        public string Version
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the LCID of the file.
        /// </summary>
        /// <value>LCID of the file.</value>
        public string Language
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the attributes on a file.
        /// </summary>
        /// <value>Attributes on a file.</value>
        public int Attributes
        {
            get { return Convert.ToInt32(this.Fields[6].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[6].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether this file should be compressed.
        /// </summary>
        /// <value>Whether this file should be compressed.</value>
        public YesNoType Compressed
        {
            get
            {
                bool compressedFlag = (0 < (this.Attributes & MsiInterop.MsidbFileAttributesCompressed));
                bool noncompressedFlag = (0 < (this.Attributes & MsiInterop.MsidbFileAttributesNoncompressed));

                if (compressedFlag && noncompressedFlag)
                {
                    throw new WixException(WixErrors.IllegalFileCompressionAttributes(this.SourceLineNumbers));
                }
                else if (compressedFlag)
                {
                    return YesNoType.Yes;
                }
                else if (noncompressedFlag)
                {
                    return YesNoType.No;
                }
                else
                {
                    return YesNoType.NotSet;
                }
            }

            set
            {
                if (YesNoType.Yes == value)
                {
                    // these are mutually exclusive
                    this.Attributes |= MsiInterop.MsidbFileAttributesCompressed;
                    this.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                }
                else if (YesNoType.No == value)
                {
                    // these are mutually exclusive
                    this.Attributes |= MsiInterop.MsidbFileAttributesNoncompressed;
                    this.Attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                }
                else // not specified
                {
                    Debug.Assert(YesNoType.NotSet == value);

                    // clear any compression bits
                    this.Attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                    this.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                }
            }
        }

        /// <summary>
        /// Gets or sets the sequence of the file row.
        /// </summary>
        /// <value>Sequence of the file row.</value>
        public int Sequence
        {
            get { return (int)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        /// <summary>
        /// Gets or sets the type of assembly of file row.
        /// </summary>
        /// <value>Assembly type for file row.</value>
        public FileAssemblyType AssemblyType
        {
            get { return this.assemblyType; }
            set { this.assemblyType = value; }
        }

        /// <summary>
        /// Gets or sets the identifier for the assembly application.
        /// </summary>
        /// <value>Identifier for the assembly application.</value>
        public string AssemblyApplication
        {
            get { return this.assemblyApplication; }
            set { this.assemblyApplication = value; }
        }

        /// <summary>
        /// Gets or sets the identifier for the assembly manifest.
        /// </summary>
        /// <value>Identifier for the assembly manifest.</value>
        public string AssemblyManifest
        {
            get { return this.assemblyManifest; }
            set { this.assemblyManifest = value; }
        }

        /// <summary>
        /// Gets or sets the directory of the file.
        /// </summary>
        /// <value>Directory of the file.</value>
        public string Directory
        {
            get { return this.directory; }
            set { this.directory = value; }
        }

        /// <summary>
        /// Gets or sets the disk id for this file.
        /// </summary>
        /// <value>Disk id for the file.</value>
        public int DiskId
        {
            get { return this.diskId; }
            set { this.diskId = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file.
        /// </summary>
        /// <value>Source location to the file.</value>
        public string Source
        {
            get { return this.source; }
            set { this.source = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the previous file.
        /// </summary>
        /// <value>Source location to the previous file.</value>
        public string PreviousSource
        {
            get { return this.previousSource[0]; }
            set { this.previousSource[0] = value; }
        }

        /// <summary>
        /// Gets the source location to the previous files.
        /// </summary>
        /// <value>Source location to the previous files.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousSourceArray
        {
            get { return this.previousSource; }
        }

        /// <summary>
        /// Gets or sets the architecture the file executes on.
        /// </summary>
        /// <value>Architecture the file executes on.</value>
        public string ProcessorArchitecture
        {
            get { return this.processorArchitecture; }
            set { this.processorArchitecture = value; }
        }

        /// <summary>
        /// Gets of sets the patch group of a patch-added file.
        /// </summary>
        /// <value>The patch group of a patch-added file.</value>
        public int PatchGroup
        {
            get { return this.patchGroup; }
            set { this.patchGroup = value; }
        }

        /// <summary>
        /// Gets or sets the patch header of the file.
        /// </summary>
        /// <value>Patch header of the file.</value>
        public string Patch
        {
            get { return this.patch; }
            set { this.patch = value; }
        }

        /// <summary>
        /// Gets or sets the locations to find the file's symbols.
        /// </summary>
        /// <value>Symbol paths for the file.</value>
        public string Symbols
        {
            get { return this.symbols; }
            set { this.symbols = value; }
        }

        /// <summary>
        /// Gets or sets the locations to find the file's previous symbols.
        /// </summary>
        /// <value>Symbol paths for the previous file.</value>
        public string PreviousSymbols
        {
            get { return this.previousSymbols[0]; }
            set { this.previousSymbols[0] = value; }
        }

        /// <summary>
        /// Gets the locations to find the files' previous symbols.
        /// </summary>
        /// <value>Symbol paths for the previous files.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousSymbolsArray
        {
            get { return this.previousSymbols; }
        }

        /// <summary>
        /// Gets or sets the generated short file name attribute.
        /// </summary>
        /// <value>The generated short file name attribute.</value>
        public bool IsGeneratedShortFileName
        {
            get { return this.isGeneratedShortFileName; }

            set { this.isGeneratedShortFileName = value; }
        }

        /// <summary>
        /// Gets or sets whether this row came from a merge module.
        /// </summary>
        /// <value>Whether this row came from a merge module.</value>
        public bool FromModule
        {
            get { return this.fromModule; }
            set { this.fromModule = value; }
        }

        /// <summary>
        /// Gets or sets the MsiFileHash row created for this FileRow.
        /// </summary>
        /// <value>Row for MsiFileHash table.</value>
        public Row HashRow
        {
            get { return this.hashRow; }
            set { this.hashRow = value; }
        }

        /// <summary>
        /// Gets or sets the set of MsiAssemblyName rows created for this FileRow.
        /// </summary>
        /// <value>RowCollection of MsiAssemblyName table.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public RowCollection AssemblyNameRows
        {
            get { return this.assemblyNameRows; }
            set { this.assemblyNameRows = value; }
        }

        /// <summary>
        /// Gets or sets the patching attributes to the file.
        /// </summary>
        /// <value>Patching attributes of the file.</value>
        public PatchAttributeType PatchAttributes
        {
            get { return this.patchAttributes; }
            set { this.patchAttributes = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-length list for the file.
        /// </summary>
        /// <value>RetainLength list for the file.</value>
        public string RetainLengths
        {
            get { return this.retainLengths; }
            set { this.retainLengths = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-offset list for the file.
        /// </summary>
        /// <value>IgnoreOffset list for the file.</value>
        public string IgnoreOffsets
        {
            get { return this.ignoreOffsets; }
            set { this.ignoreOffsets = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-length list for the file.
        /// </summary>
        /// <value>IgnoreLength list for the file.</value>
        public string IgnoreLengths
        {
            get { return this.ignoreLengths; }
            set { this.ignoreLengths = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-offset list for the file.
        /// </summary>
        /// <value>RetainOffset list for the file.</value>
        public string RetainOffsets
        {
            get { return this.retainOffsets; }
            set { this.retainOffsets = value; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-length list for the previous file.
        /// </summary>
        /// <value>RetainLength list for the previous file.</value>
        public string PreviousRetainLengths
        {
            get { return this.previousRetainLengths[0]; }
            set { this.previousRetainLengths[0] = value; }
        }

        /// <summary>
        /// Gets the delta patch retain-length list for the previous files.
        /// </summary>
        /// <value>RetainLength list for the previous files.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousRetainLengthsArray
        {
            get { return this.previousRetainLengths; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-offset list for the previous file.
        /// </summary>
        /// <value>IgnoreOffset list for the previous file.</value>
        public string PreviousIgnoreOffsets
        {
            get { return this.previousIgnoreOffsets[0]; }
            set { this.previousIgnoreOffsets[0] = value; }
        }

        /// <summary>
        /// Gets the delta patch ignore-offset list for the previous files.
        /// </summary>
        /// <value>IgnoreOffset list for the previous files.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousIgnoreOffsetsArray
        {
            get { return this.previousIgnoreOffsets; }
        }

        /// <summary>
        /// Gets or sets the delta patch ignore-length list for the previous file.
        /// </summary>
        /// <value>IgnoreLength list for the previous file.</value>
        public string PreviousIgnoreLengths
        {
            get { return this.previousIgnoreLengths[0]; }
            set { this.previousIgnoreLengths[0] = value; }
        }

        /// <summary>
        /// Gets the delta patch ignore-length list for the previous files.
        /// </summary>
        /// <value>IgnoreLength list for the previous files.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousIgnoreLengthsArray
        {
            get { return this.previousIgnoreLengths; }
        }

        /// <summary>
        /// Gets or sets the delta patch retain-offset list for the previous file.
        /// </summary>
        /// <value>RetainOffset list for the previous file.</value>
        public string PreviousRetainOffsets
        {
            get { return this.previousRetainOffsets[0]; }
            set { this.previousRetainOffsets[0] = value; }
        }

        /// <summary>
        /// Gets the delta patch retain-offset list for the previous files.
        /// </summary>
        /// <value>RetainOffset list for the previous files.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] PreviousRetainOffsetsArray
        {
            get { return this.previousRetainOffsets; }
        }

        /// <summary>
        /// Compares the current FileRow with another object of the same type.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>An integer that indicates the relative order of the comparands.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        [SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison")]
        public int CompareTo(object obj)
        {
            if (this == obj)
            {
                return 0;
            }

            FileRow fileRow = obj as FileRow;
            if (null == fileRow)
            {
                throw new ArgumentException(WixStrings.EXP_OtherObjectIsNotFileRow);
            }

            int compared = this.DiskId - fileRow.DiskId;
            if (0 == compared)
            {
                compared = this.patchGroup - fileRow.patchGroup;

                if (0 == compared)
                {
                    compared = String.Compare(this.File, fileRow.File, StringComparison.InvariantCulture);
                }
            }

            return compared;
        }

        /// <summary>
        /// Copies data from another FileRow object.
        /// </summary>
        /// <param name="src">An row to get data from.</param>
        public void CopyFrom(FileRow src)
        {
            for (int i = 0; i < src.Fields.Length; i++)
            {
                this[i] = src[i];
            }
            this.assemblyManifest = src.assemblyManifest;
            this.assemblyType = src.assemblyType;
            this.directory = src.directory;
            this.diskId = src.diskId;
            this.fromModule = src.fromModule;
            this.isGeneratedShortFileName = src.isGeneratedShortFileName;
            this.patchGroup = src.patchGroup;
            this.processorArchitecture = src.processorArchitecture;
            this.source = src.source;
            this.PreviousSource = src.PreviousSource;
            this.Operation = src.Operation;
            this.symbols = src.symbols;
            this.PreviousSymbols = src.PreviousSymbols;
            this.patchAttributes = src.patchAttributes;
            this.retainOffsets = src.retainOffsets;
            this.retainLengths = src.retainLengths;
            this.ignoreOffsets = src.ignoreOffsets;
            this.ignoreLengths = src.ignoreLengths;
            this.PreviousRetainOffsets = src.PreviousRetainOffsets;
            this.PreviousRetainLengths = src.PreviousRetainLengths;
            this.PreviousIgnoreOffsets = src.PreviousIgnoreOffsets;
            this.PreviousIgnoreLengths = src.PreviousIgnoreLengths;
        }

        /// <summary>
        /// Appends previous data from another FileRow object.
        /// </summary>
        /// <param name="src">An row to get data from.</param>
        public void AppendPreviousDataFrom(FileRow src)
        {
            AppendStringToArray(ref this.previousSource, src.previousSource[0]);
            AppendStringToArray(ref this.previousSymbols, src.previousSymbols[0]);
            AppendStringToArray(ref this.previousRetainOffsets, src.previousRetainOffsets[0]);
            AppendStringToArray(ref this.previousRetainLengths, src.previousRetainLengths[0]);
            AppendStringToArray(ref this.previousIgnoreOffsets, src.previousIgnoreOffsets[0]);
            AppendStringToArray(ref this.previousIgnoreLengths, src.previousIgnoreLengths[0]);
        }

        /// <summary>
        /// Helper method for AppendPreviousDataFrom.
        /// </summary>
        /// <param name="source">Destination array.</param>
        /// <param name="destination">Source string.</param>
        private static void AppendStringToArray(ref string[] destination, string source)
        {
            string[] result = new string[destination.Length + 1];
            destination.CopyTo(result, 0);
            result[destination.Length] = source;
            destination = result;
        }
    }
}
