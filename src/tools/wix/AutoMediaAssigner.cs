//-------------------------------------------------------------------------------------------------
// <copyright file="AutoMediaAssigner.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// AutoMediaAssigner assigns files to cabs depending on Media or MediaTemplate.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.IO;
    using System.Globalization;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.Tools.WindowsInstallerXml.Cab;

    /// <summary>
    /// AutoMediaAssigner assigns files to cabs depending on Media or MediaTemplate.
    /// </summary>
    public class AutoMediaAssigner
    {
        private BinderCore core;
        private bool filesCompressed;
        private string cabinetNameTemplate;

        private Hashtable cabinets;
        private MediaRowCollection mediaRows;
        private FileRowCollection uncompressedFileRows;

        private Output output;

        /// <summary>
        /// Gets cabinets 
        /// </summary>
        public virtual Hashtable Cabinets
        {
            get { return cabinets; }
        }

        /// <summary>
        /// Get media rows
        /// </summary>
        /// <value>MediaRowCollection</value>
        public virtual MediaRowCollection MediaRows
        {
            get { return mediaRows; }
        }

        /// <summary>
        /// Get uncompressed file rows. This will contain file rows of File elements that are marked with compression=no.
        /// This contains all the files when Package element is marked with compression=no
        /// </summary>
        /// <value>FileRowCollection</value>
        public FileRowCollection UncompressedFileRows
        {
            get { return uncompressedFileRows; }
            set { uncompressedFileRows = value; }
        }

        /// <summary>
        /// Constructor for auto media assigner.
        /// </summary>
        /// <param name="output">Output</param>
        /// <param name="core">Binder core.</param>
        /// <param name="filesCompressed">True if files are compressed by default. </param>
        public AutoMediaAssigner(Output output, BinderCore core, bool filesCompressed)
        {
            this.output = output;
            this.core = core;
            this.filesCompressed = filesCompressed;
            this.cabinetNameTemplate = "Cab{0}.cab";

            uncompressedFileRows = new FileRowCollection();
            mediaRows = new MediaRowCollection();
            cabinets = new Hashtable();
        }

        /// <summary>
        /// Assigns the file rows to media rows based on Media or MediaTemplate authoring. Updates uncompressed files collection.
        /// </summary>
        /// <param name="fileRows">FileRowCollection</param>
        public void AssignFiles(FileRowCollection fileRows)
        {
            bool autoAssign = false;
            MediaRow mergeModuleMediaRow = null;
            Table mediaTable = this.output.Tables["Media"];
            Table mediaTemplateTable = this.output.Tables["WixMediaTemplate"];

            // If both tables are authored, it is an error.
            if ((mediaTemplateTable != null && mediaTemplateTable.Rows.Count > 0) &&( mediaTable != null && mediaTable.Rows.Count > 1))
            {
                throw new WixException(WixErrors.MediaTableCollision(null));
            }

            autoAssign = mediaTemplateTable != null && OutputType.Module != this.output.Type ? true : false;

            // When building merge module, all the files go to "#MergeModule.CABinet"
            if (OutputType.Module == this.output.Type)
            {
                Table mergeModuleMediaTable = new Table(null, this.core.TableDefinitions["Media"]);
                mergeModuleMediaRow = (MediaRow)mergeModuleMediaTable.CreateRow(null);
                mergeModuleMediaRow.Cabinet = "#MergeModule.CABinet";

                this.cabinets.Add(mergeModuleMediaRow, new FileRowCollection());
            }

            if (autoAssign)
            {
                this.AutoAssignFiles(mediaTable, fileRows);
            }
            else
            {
                this.ManuallyAssignFiles(mediaTable, mergeModuleMediaRow, fileRows);
            }
        }

        /// <summary>
        /// Assign files to cabinets based on MediaTemplate authoring.
        /// </summary>
        /// <param name="fileRows">FileRowCollection</param>
        private void AutoAssignFiles(Table mediaTable, FileRowCollection fileRows)
        {
            const int MaxCabIndex = 999;

            ulong currentPreCabSize = 0;
            ulong maxPreCabSizeInBytes;
            int maxPreCabSizeInMB = 0;
            int currentCabIndex = 0;

            MediaRow currentMediaRow = null;

            Table mediaTemplateTable = this.output.Tables["WixMediaTemplate"];

            // Auto assign files to cabinets based on maximum uncompressed media size
            mediaTable.Rows.Clear();
            WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)mediaTemplateTable.Rows[0];

            if (!String.IsNullOrEmpty(mediaTemplateRow.CabinetTemplate))
            {
                this.cabinetNameTemplate = mediaTemplateRow.CabinetTemplate;
            }

            string mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");

            try
            {
                // Override authored mums value if environment variable is authored.
                if (!String.IsNullOrEmpty(mumsString))
                {
                    maxPreCabSizeInMB = Int32.Parse(mumsString);
                }
                else
                {
                    maxPreCabSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize;
                }

                maxPreCabSizeInBytes = (ulong)maxPreCabSizeInMB * 1024 * 1024;
            }
            catch (FormatException)
            {
                throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
            }
            catch (OverflowException)
            {
                throw new WixException(WixErrors.MaximumUncompressedMediaSizeTooLarge(null, maxPreCabSizeInMB));
            }

            foreach (FileRow fileRow in fileRows)
            {
                // When building a product, if the current file is not to be compressed or if 
                // the package set not to be compressed, don't cab it.
                if (OutputType.Product == output.Type &&
                    (YesNoType.No == fileRow.Compressed ||
                    (YesNoType.NotSet == fileRow.Compressed && !this.filesCompressed)))
                {
                    uncompressedFileRows.Add(fileRow);
                    continue;
                }

                FileInfo fileInfo = null;

                // Get the file size
                try
                {
                    fileInfo = new FileInfo(fileRow.Source);
                }
                catch (ArgumentException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }
                catch (PathTooLongException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }
                catch (NotSupportedException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                }

                if (fileInfo.Exists)
                {
                    if (fileInfo.Length > Int32.MaxValue)
                    {
                        throw new WixException(WixErrors.FileTooLarge(fileRow.SourceLineNumbers, fileRow.Source));
                    }

                    fileRow.FileSize = Convert.ToInt32(fileInfo.Length, CultureInfo.InvariantCulture);
                }

                if (currentCabIndex == MaxCabIndex)
                {
                    // Associate current file with last cab (irrespective of the size) and cab index is not incremented anymore.
                    FileRowCollection cabinetFileRow = (FileRowCollection)this.cabinets[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRow.Add(fileRow);
                    continue;
                }

                // Update current cab size.
                currentPreCabSize += (ulong)fileRow.FileSize;

                if (currentPreCabSize > maxPreCabSizeInBytes)
                {
                    // Overflow due to current file
                    currentMediaRow = this.AddMediaRow(mediaTable, ++currentCabIndex, mediaTemplateRow.CompressionLevel);

                    FileRowCollection cabinetFileRow = (FileRowCollection)this.cabinets[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRow.Add(fileRow);
                    // Now files larger than MaxUncompressedMediaSize will be the only file in its cabinet so as to respect MaxUncompressedMediaSize
                    currentPreCabSize = (ulong)fileRow.FileSize;
                }
                else
                {
                    // File fits in the current cab.
                    if (currentMediaRow == null)
                    {
                        // Create new cab and MediaRow
                        currentMediaRow = this.AddMediaRow(mediaTable, ++currentCabIndex, mediaTemplateRow.CompressionLevel);
                    }

                    // Associate current file with current cab.
                    FileRowCollection cabinetFileRow = (FileRowCollection)this.cabinets[currentMediaRow];
                    fileRow.DiskId = currentCabIndex;
                    cabinetFileRow.Add(fileRow);
                }
            }

            // If there are uncompressed files and no MediaRow, create a default one.
            if (uncompressedFileRows.Count > 0 && mediaTable.Rows.Count == 0 )
            {
                 MediaRow defaultMediaRow = (MediaRow)mediaTable.CreateRow(null);
                 defaultMediaRow.DiskId = 1;
                 mediaRows.Add(defaultMediaRow);
            }
        }

        /// <summary>
        /// Assign files to cabinets based on Media authoring.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="mergeModuleMediaRow"></param>
        /// <param name="fileRows"></param>
        private void ManuallyAssignFiles(Table mediaTable, MediaRow mergeModuleMediaRow, FileRowCollection fileRows)
        {
            if (OutputType.Module != this.output.Type)
            {
                if (null != mediaTable)
                {
                    Dictionary<string, MediaRow> cabinetMediaRows = new Dictionary<string, MediaRow>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (MediaRow mediaRow in mediaTable.Rows)
                    {
                        // If the Media row has a cabinet, make sure it is unique across all Media rows.
                        if (!String.IsNullOrEmpty(mediaRow.Cabinet))
                        {
                            MediaRow existingRow;
                            if (cabinetMediaRows.TryGetValue(mediaRow.Cabinet, out existingRow))
                            {
                                this.core.OnMessage(WixErrors.DuplicateCabinetName(mediaRow.SourceLineNumbers, mediaRow.Cabinet));
                                this.core.OnMessage(WixErrors.DuplicateCabinetName2(existingRow.SourceLineNumbers, existingRow.Cabinet));
                            }
                            else
                            {
                                cabinetMediaRows.Add(mediaRow.Cabinet, mediaRow);
                            }
                        }

                        this.mediaRows.Add(mediaRow);
                    }
                }

                foreach (MediaRow mediaRow in this.mediaRows)
                {
                    if (null != mediaRow.Cabinet)
                    {
                        this.cabinets.Add(mediaRow, new FileRowCollection());
                    }
                }
            }

            foreach (FileRow fileRow in fileRows)
            {
                if (OutputType.Module == output.Type)
                {
                    ((FileRowCollection)this.cabinets[mergeModuleMediaRow]).Add(fileRow);
                }
                else
                {
                    MediaRow mediaRow = this.mediaRows[fileRow.DiskId];

                    if (null == mediaRow)
                    {
                        this.core.OnMessage(WixErrors.MissingMedia(fileRow.SourceLineNumbers, fileRow.DiskId));
                        continue;
                    }

                    // When building a product, if the current file is not to be compressed or if 
                    // the package set not to be compressed, don't cab it.
                    if (OutputType.Product == output.Type &&
                        (YesNoType.No == fileRow.Compressed ||
                        (YesNoType.NotSet == fileRow.Compressed && !this.filesCompressed)))
                    {
                        uncompressedFileRows.Add(fileRow);
                    }
                    else // file in a Module or marked compressed
                    {
                        FileRowCollection cabinetFileRow = (FileRowCollection)this.cabinets[mediaRow];

                        if (null != cabinetFileRow)
                        {
                            cabinetFileRow.Add(fileRow);
                        }
                        else
                        {
                            this.core.OnMessage(WixErrors.ExpectedMediaCabinet(fileRow.SourceLineNumbers, fileRow.File, fileRow.DiskId));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a row to the media table with cab name template filled in.
        /// </summary>
        /// <param name="mediaTable"></param>
        /// <param name="cabIndex"></param>
        /// <returns></returns>
        private MediaRow AddMediaRow(Table mediaTable, int cabIndex, string compressionLevel)
        {
            MediaRow currentMediaRow = (MediaRow)mediaTable.CreateRow(null);
            currentMediaRow.DiskId = cabIndex;
            currentMediaRow.Cabinet = String.Format(this.cabinetNameTemplate, cabIndex);

            if (!String.IsNullOrEmpty(compressionLevel))
            {
                currentMediaRow.CompressionLevel = WixCreateCab.CompressionLevelFromString(compressionLevel);
            }

            this.mediaRows.Add(currentMediaRow);
            this.cabinets.Add(currentMediaRow, new FileRowCollection());

            Table wixMediaTable = this.output.EnsureTable(this.core.TableDefinitions["WixMedia"]);
            Row row = wixMediaTable.CreateRow(null);
            row[0] = cabIndex;
            row[1] = compressionLevel;

            return currentMediaRow;
        }
    }
}
