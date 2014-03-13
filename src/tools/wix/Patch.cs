//-------------------------------------------------------------------------------------------------
// <copyright file="Patch.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains output tables and logic for building an MSP package.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Values for the OptimizeCA MsiPatchMetdata property, which indicates whether custom actions can be skipped when applying the patch.
    /// </summary>
    [Flags]
    internal enum OptimizeCA
    {
        /// <summary>
        /// No custom actions are skipped.
        /// </summary>
        None = 0,

        /// <summary>
        /// Skip property (type 51) and directory (type 35) assignment custom actions.
        /// </summary>
        SkipAssignment = 1,

        /// <summary>
        /// Skip immediate custom actions that are not property or directory assignment custom actions.
        /// </summary>
        SkipImmediate = 2,

        /// <summary>
        /// Skip custom actions that run within the script.
        /// </summary>
        SkipDeferred = 4,
    }

    /// <summary>
    /// Contains output tables and logic for building an MSP package.
    /// </summary>
    public class Patch
    {
        private List<InspectorExtension> inspectorExtensions;
        private Output patch;
        private TableDefinitionCollection tableDefinitions;
        public event MessageEventHandler Message;

        public Output PatchOutput
        {
            get { return this.patch; }
        }

        public Patch()
        {
            this.inspectorExtensions = new List<InspectorExtension>();
            this.tableDefinitions = Installer.GetTableDefinitions();
        }

        /// <summary>
        /// Adds an extension.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        public void AddExtension(WixExtension extension)
        {
            if (null != extension.InspectorExtension)
            {
                this.inspectorExtensions.Add(extension.InspectorExtension);
            }
        }

        public void Load(string patchPath)
        {
            this.patch = Output.Load(patchPath, false, false);
        }

        /// <summary>
        /// Include transforms in a patch.
        /// </summary>
        /// <param name="transforms">List of transforms to attach.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public void AttachTransforms(ArrayList transforms)
        {
            InspectorCore inspectorCore = new InspectorCore(this.Message);

            // Track if at least one transform gets attached.
            bool attachedTransform = false;

            if (transforms == null || transforms.Count == 0)
            {
                throw new WixException(WixErrors.PatchWithoutTransforms());
            }

            // Get the patch id from the WixPatchId table.
            string patchId = null;
            string clientPatchId = null;
            Table wixPatchIdTable = this.patch.Tables["WixPatchId"];
            if (null != wixPatchIdTable && 0 < wixPatchIdTable.Rows.Count)
            {
                Row patchIdRow = wixPatchIdTable.Rows[0];
                if (null != patchIdRow)
                {
                    patchId = patchIdRow[0].ToString();
                    clientPatchId = patchIdRow[1].ToString();
                }
            }

            if (null == patchId)
            {
                throw new WixException(WixErrors.ExpectedPatchIdInWixMsp());
            }
            if (null == clientPatchId)
            {
                throw new WixException(WixErrors.ExpectedClientPatchIdInWixMsp());
            }

            // enumerate patch.Media to map diskId to Media row
            Table patchMediaTable = patch.Tables["Media"];

            if (null == patchMediaTable || patchMediaTable.Rows.Count == 0)
            {
                throw new WixException(WixErrors.ExpectedMediaRowsInWixMsp());
            }

            Hashtable mediaRows = new Hashtable(patchMediaTable.Rows.Count);
            foreach (MediaRow row in patchMediaTable.Rows)
            {
                int media = row.DiskId;
                mediaRows[media] = row;
            }

            // enumerate patch.WixPatchBaseline to map baseline to diskId
            Table patchBaselineTable = patch.Tables["WixPatchBaseline"];

            int numPatchBaselineRows = (null != patchBaselineTable) ? patchBaselineTable.Rows.Count : 0;

            Hashtable baselineMedia = new Hashtable(numPatchBaselineRows);
            if (patchBaselineTable != null)
            {
                foreach (Row row in patchBaselineTable.Rows)
                {
                    string baseline = (string)row[0];
                    int media = (int)row[1];
                    int validationFlags = (int)row[2];
                    if (baselineMedia.Contains(baseline))
                    {
                        this.OnMessage(WixErrors.SamePatchBaselineId(row.SourceLineNumbers, baseline));
                    }
                    baselineMedia[baseline] = new int[] { media, validationFlags };
                }
            }

            // populate MSP summary information
            Table patchSummaryInfo = patch.EnsureTable(this.tableDefinitions["_SummaryInformation"]);

            // Remove properties that will be calculated or are reserved.
            for (int i = patchSummaryInfo.Rows.Count - 1; i >= 0; i--)
            {
                Row row = patchSummaryInfo.Rows[i];
                switch ((SummaryInformation.Patch)row[0])
                {
                    case SummaryInformation.Patch.ProductCodes:
                    case SummaryInformation.Patch.TransformNames:
                    case SummaryInformation.Patch.PatchCode:
                    case SummaryInformation.Patch.InstallerRequirement:
                    case SummaryInformation.Patch.Reserved11:
                    case SummaryInformation.Patch.Reserved14:
                    case SummaryInformation.Patch.Reserved16:
                        patchSummaryInfo.Rows.RemoveAt(i);
                        break;
                }
            }

            // Index remaining summary properties.
            SummaryInfoRowCollection summaryInfo = new SummaryInfoRowCollection(patchSummaryInfo);

            // PID_CODEPAGE
            if (!summaryInfo.Contains((int)SummaryInformation.Patch.CodePage))
            {
                // set the code page by default to the same code page for the
                // string pool in the database.
                Row codePage = patchSummaryInfo.CreateRow(null);
                codePage[0] = (int)SummaryInformation.Patch.CodePage;
                codePage[1] = this.patch.Codepage.ToString(CultureInfo.InvariantCulture);
            }

            // GUID patch code for the patch. 
            Row revisionRow = patchSummaryInfo.CreateRow(null);
            revisionRow[0] = (int)SummaryInformation.Patch.PatchCode;
            revisionRow[1] = patchId;

            // Indicates the minimum Windows Installer version that is required to install the patch. 
            Row wordsRow = patchSummaryInfo.CreateRow(null);
            wordsRow[0] = (int)SummaryInformation.Patch.InstallerRequirement;
            wordsRow[1] = ((int)SummaryInformation.InstallerRequirement.Version31).ToString(CultureInfo.InvariantCulture);

            if (!summaryInfo.Contains((int)SummaryInformation.Patch.Security))
            {
                Row security = patchSummaryInfo.CreateRow(null);
                security[0] = (int)SummaryInformation.Patch.Security;
                security[1] = "4"; // Read-only enforced
            }

            // use authored comments or default to DisplayName (required)
            string comments = null;

            Table msiPatchMetadataTable = patch.Tables["MsiPatchMetadata"];
            Hashtable metadataTable = new Hashtable();
            if (null != msiPatchMetadataTable)
            {
                foreach (Row row in msiPatchMetadataTable.Rows)
                {
                    metadataTable.Add(row.Fields[1].Data.ToString(), row.Fields[2].Data.ToString());
                }

                if (!summaryInfo.Contains((int)SummaryInformation.Patch.Title) && metadataTable.Contains("DisplayName"))
                {
                    string displayName = (string)metadataTable["DisplayName"];

                    Row title = patchSummaryInfo.CreateRow(null);
                    title[0] = (int)SummaryInformation.Patch.Title;
                    title[1] = displayName;

                    // default comments use DisplayName as-is (no loc)
                    comments = displayName;
                }

                if (!summaryInfo.Contains((int)SummaryInformation.Patch.CodePage) && metadataTable.Contains("CodePage"))
                {
                    Row codePage = patchSummaryInfo.CreateRow(null);
                    codePage[0] = (int)SummaryInformation.Patch.CodePage;
                    codePage[1] = metadataTable["CodePage"];
                }

                if (!summaryInfo.Contains((int)SummaryInformation.Patch.PackageName) && metadataTable.Contains("Description"))
                {
                    Row subject = patchSummaryInfo.CreateRow(null);
                    subject[0] = (int)SummaryInformation.Patch.PackageName;
                    subject[1] = metadataTable["Description"];
                }

                if (!summaryInfo.Contains((int)SummaryInformation.Patch.Manufacturer) && metadataTable.Contains("ManufacturerName"))
                {
                    Row author = patchSummaryInfo.CreateRow(null);
                    author[0] = (int)SummaryInformation.Patch.Manufacturer;
                    author[1] = metadataTable["ManufacturerName"];
                }
            }

            // special metadata marshalled through the build
            Table wixPatchMetadataTable = patch.Tables["WixPatchMetadata"];
            Hashtable wixMetadataTable = new Hashtable();
            if (null != wixPatchMetadataTable)
            {
                foreach (Row row in wixPatchMetadataTable.Rows)
                {
                    wixMetadataTable.Add(row.Fields[0].Data.ToString(), row.Fields[1].Data.ToString());
                }

                if (wixMetadataTable.Contains("Comments"))
                {
                    comments = (string)wixMetadataTable["Comments"];
                }
            }

            // write the package comments to summary info
            if (!summaryInfo.Contains((int)SummaryInformation.Patch.Comments) && null != comments)
            {
                Row commentsRow = patchSummaryInfo.CreateRow(null);
                commentsRow[0] = (int)SummaryInformation.Patch.Comments;
                commentsRow[1] = comments;
            }

            // enumerate transforms
            Dictionary<string, object> productCodes = new Dictionary<string, object>();
            ArrayList transformNames = new ArrayList();
            ArrayList validTransform = new ArrayList();
            int transformCount = 0;
            foreach (PatchTransform mainTransform in transforms)
            {
                string baseline = null;
                int media = -1;
                int validationFlags = 0;

                if (baselineMedia.Contains(mainTransform.Baseline))
                {
                    int[] baselineData = (int[])baselineMedia[mainTransform.Baseline];
                    int newMedia = baselineData[0];
                    if (media != -1 && media != newMedia)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_TransformAuthoredIntoMultipleMedia, media, newMedia));
                    }
                    baseline = mainTransform.Baseline;
                    media = newMedia;
                    validationFlags = baselineData[1];
                }

                if (media == -1)
                {
                    // transform's baseline not attached to any Media
                    continue;
                }

                Table patchRefTable = patch.Tables["WixPatchRef"];
                if (patchRefTable != null && patchRefTable.Rows.Count > 0)
                {
                    if (!Patch.ReduceTransform(mainTransform.Transform, patchRefTable))
                    {
                        // transform has none of the content authored into this patch
                        continue;
                    }
                }

                // Validate the transform doesn't break any patch specific rules.
                mainTransform.Validate();

                // ensure consistent File.Sequence within each Media
                MediaRow mediaRow = (MediaRow)mediaRows[media];

                // Ensure that files are sequenced after the last file in any transform.
                Table transformMediaTable = mainTransform.Transform.Tables["Media"];
                if (null != transformMediaTable && 0 < transformMediaTable.Rows.Count)
                {
                    foreach (MediaRow transformMediaRow in transformMediaTable.Rows)
                    {
                        if (mediaRow.LastSequence < transformMediaRow.LastSequence)
                        {
                            // The Binder will pre-increment the sequence.
                            mediaRow.LastSequence = transformMediaRow.LastSequence;
                        }
                    }
                }
                
                // Use the Media/@DiskId if greater for backward compatibility.
                if (mediaRow.LastSequence < mediaRow.DiskId)
                {
                    mediaRow.LastSequence = mediaRow.DiskId;
                }

                // ignore media table from transform.
                mainTransform.Transform.Tables.Remove("Media");
                mainTransform.Transform.Tables.Remove("WixMedia");
                mainTransform.Transform.Tables.Remove("MsiDigitalSignature");

                string productCode;
                Output pairedTransform = this.BuildPairedTransform(patchId, clientPatchId, mainTransform.Transform, mediaRow, validationFlags, out productCode);
                productCodes[productCode] = null;
                DictionaryEntry entry = new DictionaryEntry();
                entry.Key = productCode;
                entry.Value = mainTransform.Transform;
                validTransform.Add(entry);

                // attach these transforms to the patch object
                // TODO: is this an acceptable way to auto-generate transform stream names?
                string transformName = baseline + "." + (++transformCount).ToString(CultureInfo.InvariantCulture);
                patch.SubStorages.Add(new SubStorage(transformName, mainTransform.Transform));
                patch.SubStorages.Add(new SubStorage("#" + transformName, pairedTransform));
                transformNames.Add(":" + transformName);
                transformNames.Add(":#" + transformName);
                attachedTransform = true;
            }

            if (!attachedTransform)
            {
                throw new WixException(WixErrors.PatchWithoutValidTransforms());
            }

            // Validate that a patch authored as removable is actually removable
            if (metadataTable.Contains("AllowRemoval"))
            {
                if ("1" == metadataTable["AllowRemoval"].ToString())
                {
                    ArrayList tables = Patch.GetPatchUninstallBreakingTables();
                    bool result = true;
                    foreach (DictionaryEntry entry in validTransform)
                    {
                        result &= this.CheckUninstallableTransform(entry.Key.ToString(), (Output)entry.Value, tables);
                    }

                    if (!result)
                    {
                        throw new WixException(WixErrors.PatchNotRemovable());
                    }
                }
            }

            // Finish filling tables with transform-dependent data.
            // Semicolon delimited list of the product codes that can accept the patch.
            Table wixPatchTargetTable = patch.Tables["WixPatchTarget"];
            if (null != wixPatchTargetTable)
            {
                Dictionary<string, object> targets = new Dictionary<string, object>();
                bool replace = true;
                foreach (Row wixPatchTargetRow in wixPatchTargetTable.Rows)
                {
                    string target = wixPatchTargetRow[0].ToString();
                    if (0 == String.CompareOrdinal("*", target))
                    {
                        replace = false;
                    }
                    else
                    {
                        targets[target] = null;
                    }
                }

                // Replace the target ProductCodes with the authored list.
                if (replace)
                {
                    productCodes = targets;
                }
                else
                {
                    // Copy the authored target ProductCodes into the list.
                    foreach (string target in targets.Keys)
                    {
                        productCodes[target] = null;
                    }
                }
            }

            string[] uniqueProductCodes = new string[productCodes.Keys.Count];
            productCodes.Keys.CopyTo(uniqueProductCodes, 0);

            Row templateRow = patchSummaryInfo.CreateRow(null);
            templateRow[0] = (int)SummaryInformation.Patch.ProductCodes;
            templateRow[1] = String.Join(";", uniqueProductCodes);

            // Semicolon delimited list of transform substorage names in the order they are applied.
            Row savedbyRow = patchSummaryInfo.CreateRow(null);
            savedbyRow[0] = (int)SummaryInformation.Patch.TransformNames;
            savedbyRow[1] = String.Join(";", (string[])transformNames.ToArray(typeof(string)));

            // inspect the patch and filtered transforms
            foreach (InspectorExtension inspectorExtension in this.inspectorExtensions)
            {
                inspectorExtension.Core = inspectorCore;
                inspectorExtension.InspectPatch(this.patch);

                // reset
                inspectorExtension.Core = null;
            }
        }

        /// <summary>
        /// Ensure transform is uninstallable.
        /// </summary>
        /// <param name="productCode">Product code in transform.</param>
        /// <param name="transform">Transform generated by torch.</param>
        /// <param name="tables">Tables to be checked</param>
        /// <returns>True if the transform is uninstallable</returns>
        private bool CheckUninstallableTransform(string productCode, Output transform, ArrayList tables)
        {
            bool ret = true;
            foreach (string table in tables)
            {
                Table wixTable = transform.Tables[table];
                if (null != wixTable)
                {
                    foreach (Row row in wixTable.Rows)
                    {
                        if (row.Operation == RowOperation.Add)
                        {
                            ret = false;
                            string primaryKey = row.GetPrimaryKey('/');
                            if (null == primaryKey)
                            {
                                primaryKey = string.Empty;
                            }
                            this.OnMessage(WixErrors.NewRowAddedInTable(row.SourceLineNumbers, productCode, wixTable.Name, primaryKey));
                        }
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Tables affect patch uninstall.
        /// </summary>
        /// <returns>list of tables to be checked</returns>
        private static ArrayList GetPatchUninstallBreakingTables()
        {
            ArrayList tables = new ArrayList();
            tables.Add("BindImage");
            tables.Add("Class");
            tables.Add("Complus");
            tables.Add("CreateFolder");
            tables.Add("DuplicateFile");
            tables.Add("Environment");
            tables.Add("Extension");
            tables.Add("Font");
            tables.Add("IniFile");
            tables.Add("IsolatedComponent");
            tables.Add("LockPermissions");
            tables.Add("MIME");
            tables.Add("MoveFile");
            tables.Add("ODBCAttribute");
            tables.Add("ODBCDataSource");
            tables.Add("ODBCDriver");
            tables.Add("ODBCSourceAttribute");
            tables.Add("ODBCTranslator");
            tables.Add("ProgId");
            tables.Add("PublishComponent");
            tables.Add("RemoveIniFile");
            tables.Add("SelfReg");
            tables.Add("ServiceControl");
            tables.Add("ServiceInstall");
            tables.Add("TypeLib");
            tables.Add("Verb");

            return tables;
        }

        /// <summary>
        /// Reduce the transform according to the patch references.
        /// </summary>
        /// <param name="transform">transform generated by torch.</param>
        /// <param name="patchRefTable">Table contains patch family filter.</param>
        /// <returns>true if the transform is not empty</returns>
        public static bool ReduceTransform(Output transform, Table patchRefTable)
        {
            // identify sections to keep
            Hashtable oldSections = new Hashtable(patchRefTable.Rows.Count);
            Hashtable newSections = new Hashtable(patchRefTable.Rows.Count);
            Hashtable tableKeyRows = new Hashtable();
            ArrayList sequenceList = new ArrayList();
            Hashtable componentFeatureAddsIndex = new Hashtable();
            Hashtable customActionTable = new Hashtable();
            Hashtable directoryTableAdds = new Hashtable();
            Hashtable featureTableAdds = new Hashtable();
            Hashtable keptComponents = new Hashtable();
            Hashtable keptDirectories = new Hashtable();
            Hashtable keptFeatures = new Hashtable();
            Hashtable keptLockPermissions = new Hashtable();
            Hashtable keptMsiLockPermissionExs = new Hashtable();
            
            Dictionary<string, List<string>> componentCreateFolderIndex = new Dictionary<string,List<string>>();
            Dictionary<string, List<Row>> directoryLockPermissionsIndex = new Dictionary<string,List<Row>>();
            Dictionary<string, List<Row>> directoryMsiLockPermissionsExIndex = new Dictionary<string,List<Row>>();

            foreach (Row patchRefRow in patchRefTable.Rows)
            {
                string tableName = (string)patchRefRow[0];
                string key = (string)patchRefRow[1];

                Table table = transform.Tables[tableName];
                if (table == null)
                {
                    // table not found
                    continue;
                }

                // index this table
                if (!tableKeyRows.Contains(tableName))
                {
                    Hashtable newKeyRows = new Hashtable();
                    foreach (Row newRow in table.Rows)
                    {
                        newKeyRows[newRow.GetPrimaryKey('/')] = newRow;
                    }
                    tableKeyRows[tableName] = newKeyRows;
                }
                Hashtable keyRows = (Hashtable)tableKeyRows[tableName];

                Row row = (Row)keyRows[key];
                if (row == null)
                {
                    // row not found
                    continue;
                }

                // Differ.sectionDelimiter
                string[] sections = row.SectionId.Split('/');
                oldSections[sections[0]] = row;
                newSections[sections[1]] = row;
            }

            // throw away sections not referenced
            int keptRows = 0;
            Table directoryTable = null;
            Table featureTable = null;;
            Table lockPermissionsTable = null;
            Table msiLockPermissionsTable = null;

            foreach (Table table in transform.Tables)
            {
                if ("_SummaryInformation" == table.Name)
                {
                    continue;
                }

                if (table.Name == "AdminExecuteSequence"
                    || table.Name == "AdminUISequence"
                    || table.Name == "AdvtExecuteSequence"
                    || table.Name == "InstallUISequence"
                    || table.Name == "InstallExecuteSequence")
                {
                    sequenceList.Add(table);
                    continue;
                }

                for (int i = 0; i < table.Rows.Count; i++)
                {
                    Row row = table.Rows[i];

                    if (table.Name == "CreateFolder")
                    {
                        string createFolderComponentId = (string)row[1];

                        List<string> directoryList;
                        if (!componentCreateFolderIndex.TryGetValue(createFolderComponentId, out directoryList))
                        {
                            directoryList = new List<string>();
                            componentCreateFolderIndex.Add(createFolderComponentId, directoryList);
                        }

                        directoryList.Add((string)row[0]);
                    }

                    if (table.Name == "CustomAction")
                    {
                        customActionTable.Add(row[0], row);
                    }

                    if (table.Name == "Directory")
                    {
                        directoryTable = table;
                        if (RowOperation.Add == row.Operation)
                        {
                            directoryTableAdds.Add(row[0], row);
                        }
                    }

                    if (table.Name == "Feature")
                    {
                        featureTable = table;
                        if (RowOperation.Add == row.Operation)
                        {
                            featureTableAdds.Add(row[0], row);
                        }
                    }

                    if (table.Name == "FeatureComponents")
                    {
                        if (RowOperation.Add == row.Operation)
                        {
                            string featureId = (string)row[0];
                            string componentId = (string)row[1];

                            if (componentFeatureAddsIndex.ContainsKey(componentId))
                            {
                                ArrayList featureList = (ArrayList)componentFeatureAddsIndex[componentId];
                                featureList.Add(featureId);
                            }
                            else
                            {
                                ArrayList featureList = new ArrayList();
                                componentFeatureAddsIndex.Add(componentId, featureList);
                                featureList.Add(featureId);
                            }
                        }
                    }

                    if (table.Name == "LockPermissions")
                    {
                        lockPermissionsTable = table;
                        if ("CreateFolder" == (string)row[1])
                        {
                            string directoryId = (string)row[0];

                            List<Row> rowList;
                            if (!directoryLockPermissionsIndex.TryGetValue(directoryId, out rowList))
                            {
                                rowList = new List<Row>();
                                directoryLockPermissionsIndex.Add(directoryId, rowList);
                            }

                            rowList.Add(row);
                        }
                    }

                    if (table.Name == "MsiLockPermissionsEx")
                    {
                        msiLockPermissionsTable = table;
                        if ("CreateFolder" == (string)row[1])
                        {
                            string directoryId = (string)row[0];

                            List<Row> rowList;
                            if (!directoryMsiLockPermissionsExIndex.TryGetValue(directoryId, out rowList))
                            {
                                rowList = new List<Row>();
                                directoryMsiLockPermissionsExIndex.Add(directoryId, rowList);
                            }

                            rowList.Add(row);
                        }
                    }

                    if (null == row.SectionId)
                    {
                        table.Rows.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        string[] sections = row.SectionId.Split('/');
                        // ignore the row without section id.
                        if (0 == sections[0].Length && 0 == sections[1].Length)
                        {
                            table.Rows.RemoveAt(i);
                            i--;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            if ("Component" == table.Name)
                            {
                                keptComponents.Add((string)row[0], row);
                            }

                            if ("Directory" == table.Name)
                            {
                                keptDirectories.Add(row[0], row);
                            }

                            if ("Feature" == table.Name)
                            {
                                keptFeatures.Add(row[0], row);
                            }

                            keptRows++;
                        }
                        else
                        {
                            table.Rows.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            keptRows += ReduceTransformSequenceTable(sequenceList, oldSections, newSections, customActionTable);

            if (null != directoryTable)
            {
                foreach (Row componentRow in keptComponents.Values)
                {
                    string componentId = (string)componentRow[0];

                    if (RowOperation.Add == componentRow.Operation)
                    {
                        // make sure each added component has its required directory and feature heirarchy.
                        string directoryId = (string)componentRow[2];
                        while (null != directoryId && directoryTableAdds.ContainsKey(directoryId))
                        {
                            Row directoryRow = (Row)directoryTableAdds[directoryId];

                            if (!keptDirectories.ContainsKey(directoryId))
                            {
                                directoryTable.Rows.Add(directoryRow);
                                keptDirectories.Add(directoryRow[0], null);
                                keptRows++;
                            }

                            directoryId = (string)directoryRow[1];
                        }

                        if (componentFeatureAddsIndex.ContainsKey(componentId))
                        {
                            foreach (string featureId in (ArrayList)componentFeatureAddsIndex[componentId])
                            {
                                string currentFeatureId = featureId;
                                while (null != currentFeatureId && featureTableAdds.ContainsKey(currentFeatureId))
                                {
                                    Row featureRow = (Row)featureTableAdds[currentFeatureId];

                                    if (!keptFeatures.ContainsKey(currentFeatureId))
                                    {
                                        featureTable.Rows.Add(featureRow);
                                        keptFeatures.Add(featureRow[0], null);
                                        keptRows++;
                                    }

                                    currentFeatureId = (string)featureRow[1];
                                }
                            }
                        }
                    }

                    // Hook in changes LockPermissions and MsiLockPermissions for folders for each component that has been kept.
                    foreach (string keptComponentId in keptComponents.Keys)
                    {
                        List<string> directoryList;
                        if (componentCreateFolderIndex.TryGetValue(keptComponentId, out directoryList))
                        {
                            foreach (string directoryId in directoryList)
                            {
                                List<Row> lockPermissionsRowList;
                                if (directoryLockPermissionsIndex.TryGetValue(directoryId, out lockPermissionsRowList))
                                {
                                    foreach (Row lockPermissionsRow in lockPermissionsRowList)
                                    {
                                        string key = lockPermissionsRow.GetPrimaryKey('/');
                                        if (!keptLockPermissions.ContainsKey(key))
                                        {
                                            lockPermissionsTable.Rows.Add(lockPermissionsRow);
                                            keptLockPermissions.Add(key, null);
                                            keptRows++;
                                        }
                                    }
                                }

                                List<Row> msiLockPermissionsExRowList;
                                if (directoryMsiLockPermissionsExIndex.TryGetValue(directoryId, out msiLockPermissionsExRowList))
                                {
                                    foreach (Row msiLockPermissionsExRow in msiLockPermissionsExRowList)
                                    {
                                        string key = msiLockPermissionsExRow.GetPrimaryKey('/');
                                        if (!keptMsiLockPermissionExs.ContainsKey(key))
                                        {
                                            msiLockPermissionsTable.Rows.Add(msiLockPermissionsExRow);
                                            keptMsiLockPermissionExs.Add(key, null);
                                            keptRows++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            keptRows += ReduceTransformSequenceTable(sequenceList, oldSections, newSections, customActionTable);

            // Delete tables that are empty.
            ArrayList tablesToDelete = new ArrayList();
            foreach (Table table in transform.Tables)
            {
                if (0 == table.Rows.Count)
                {
                    tablesToDelete.Add(table.Name);
                }
            }

            // delete separately to avoid messing up enumeration
            foreach (string tableName in tablesToDelete)
            {
                transform.Tables.Remove(tableName);
            }

            return keptRows > 0;
        }

        /// <summary>
        /// Check if the section is in a PatchFamily.
        /// </summary>
        /// <param name="oldSection">Section id in target wixout</param>
        /// <param name="newSection">Section id in upgrade wixout</param>
        /// <param name="oldSections">Hashtable contains section id should be kept in the baseline wixout.</param>
        /// <param name="newSections">Hashtable contains section id should be kept in the upgrade wixout.</param>
        /// <returns>true if section in patch family</returns>
        private static bool IsInPatchFamily(string oldSection, string newSection, Hashtable oldSections, Hashtable newSections)
        {
            bool result = false;

            if ((String.IsNullOrEmpty(oldSection) && newSections.Contains(newSection)) || (String.IsNullOrEmpty(newSection) && oldSections.Contains(oldSection)))
            {
                result = true;
            }
            else if (!String.IsNullOrEmpty(oldSection) && !String.IsNullOrEmpty(newSection) && (oldSections.Contains(oldSection) || newSections.Contains(newSection)))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Reduce the transform sequence tables.
        /// </summary>
        /// <param name="sequenceList">ArrayList of tables to be reduced</param>
        /// <param name="oldSections">Hashtable contains section id should be kept in the baseline wixout.</param>
        /// <param name="newSections">Hashtable contains section id should be kept in the target wixout.</param>
        /// <param name="customAction">Hashtable contains all the rows in the CustomAction table.</param>
        /// <returns>Number of rows left</returns>
        private static int ReduceTransformSequenceTable(ArrayList sequenceList, Hashtable oldSections, Hashtable newSections, Hashtable customAction)
        {
            int keptRows = 0;

            foreach (Table currentTable in sequenceList)
            {
                for (int i = 0; i < currentTable.Rows.Count; i++)
                {
                    Row row = currentTable.Rows[i];
                    string actionName = row.Fields[0].Data.ToString();
                    string[] sections = row.SectionId.Split('/');
                    bool isSectionIdEmpty = (sections[0].Length == 0 && sections[1].Length == 0);

                    if (row.Operation == RowOperation.None)
                    {
                        // ignore the rows without section id.
                        if (isSectionIdEmpty)
                        {
                            currentTable.Rows.RemoveAt(i);
                            i--;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            currentTable.Rows.RemoveAt(i);
                            i--;
                        }
                    }
                    else if (row.Operation == RowOperation.Modify)
                    {
                        bool sequenceChanged = row.Fields[2].Modified;
                        bool conditionChanged = row.Fields[1].Modified;

                        if (sequenceChanged && !conditionChanged)
                        {
                            keptRows++;
                        }
                        else if (!sequenceChanged && conditionChanged)
                        {
                            if (isSectionIdEmpty)
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                            {
                                keptRows++;
                            }
                            else
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                        }
                        else if (sequenceChanged && conditionChanged)
                        {
                            if (isSectionIdEmpty)
                            {
                                row.Fields[1].Modified = false;
                                keptRows++;
                            }
                            else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                            {
                                keptRows++;
                            }
                            else
                            {
                                row.Fields[1].Modified = false;
                                keptRows++;
                            }
                        }
                    }
                    else if (row.Operation == RowOperation.Delete)
                    {
                        if (isSectionIdEmpty)
                        {
                            // it is a stardard action which is added by wix, we should keep this action.
                            row.Operation = RowOperation.None;
                            keptRows++;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            if (customAction.ContainsKey(actionName))
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                // it is a stardard action, we should keep this action.
                                row.Operation = RowOperation.None;
                                keptRows++;
                            }
                        }
                    }
                    else if (row.Operation == RowOperation.Add)
                    {
                        if (isSectionIdEmpty)
                        {
                            keptRows++;
                        }
                        else if (IsInPatchFamily(sections[0], sections[1], oldSections, newSections))
                        {
                            keptRows++;
                        }
                        else
                        {
                            if (customAction.ContainsKey(actionName))
                            {
                                currentTable.Rows.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                keptRows++;
                            }
                        }
                    }
                }
            }

            return keptRows;
        }

        /// <summary>
        /// Create the #transform for the given main transform.
        /// </summary>
        /// <param name="patchId">Patch GUID from patch authoring.</param>
        /// <param name="clientPatchId">Easily referenced identity for this patch.</param>
        /// <param name="mainTransform">Transform generated by torch.</param>
        /// <param name="mediaRow">Media authored into patch.</param>
        /// <param name="validationFlags">Transform validation flags for the summary information stream.</param>
        /// <param name="productCode">Output string to receive ProductCode.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public Output BuildPairedTransform(string patchId, string clientPatchId, Output mainTransform, MediaRow mediaRow, int validationFlags, out string productCode)
        {
            productCode = null;
            Output pairedTransform = new Output(null);
            pairedTransform.Type = OutputType.Transform;
            pairedTransform.Codepage = mainTransform.Codepage;

            // lookup productVersion property to correct summaryInformation
            string newProductVersion = null;
            Table mainPropertyTable = mainTransform.Tables["Property"];
            if (null != mainPropertyTable)
            {
                foreach (Row row in mainPropertyTable.Rows)
                {
                    if ("ProductVersion" == (string)row[0])
                    {
                        newProductVersion = (string)row[1];
                    }
                }
            }

            // TODO: build class for manipulating SummaryInformation table
            Table mainSummaryTable = mainTransform.Tables["_SummaryInformation"];
            // add required properties
            Hashtable mainSummaryRows = new Hashtable();
            foreach (Row mainSummaryRow in mainSummaryTable.Rows)
            {
                mainSummaryRows[mainSummaryRow[0]] = mainSummaryRow;
            }
            if (!mainSummaryRows.Contains((int)SummaryInformation.Transform.ValidationFlags))
            {
                Row mainSummaryRow = mainSummaryTable.CreateRow(null);
                mainSummaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                mainSummaryRow[1] = validationFlags.ToString(CultureInfo.InvariantCulture);
            }

            // copy summary information from core transform
            Table pairedSummaryTable = pairedTransform.EnsureTable(this.tableDefinitions["_SummaryInformation"]);
            foreach (Row mainSummaryRow in mainSummaryTable.Rows)
            {
                string value = (string)mainSummaryRow[1];
                switch ((SummaryInformation.Transform)mainSummaryRow[0])
                {
                    case SummaryInformation.Transform.ProductCodes:
                        string[] propertyData = value.Split(';');
                        string oldProductVersion = propertyData[0].Substring(38);
                        string upgradeCode = propertyData[2];
                        productCode = propertyData[0].Substring(0, 38);
                        if (newProductVersion == null)
                        {
                            newProductVersion = oldProductVersion;
                        }

                        // force mainTranform to old;new;upgrade and pairedTransform to new;new;upgrade
                        mainSummaryRow[1] = String.Concat(productCode, oldProductVersion, ';', productCode, newProductVersion, ';', upgradeCode);
                        value = String.Concat(productCode, newProductVersion, ';', productCode, newProductVersion, ';', upgradeCode);
                        break;
                    case SummaryInformation.Transform.ValidationFlags:
                        // use validation flags authored into the patch XML
                        mainSummaryRow[1] = value = validationFlags.ToString(CultureInfo.InvariantCulture);
                        break;
                }
                Row pairedSummaryRow = pairedSummaryTable.CreateRow(null);
                pairedSummaryRow[0] = mainSummaryRow[0];
                pairedSummaryRow[1] = value;
            }

            if (productCode == null)
            {
                throw new InvalidOperationException(WixStrings.EXP_CouldnotDetermineProductCodeFromTransformSummaryInfo);
            }

            // copy File table
            Table mainFileTable = mainTransform.Tables["File"];
            if (null != mainFileTable && 0 < mainFileTable.Rows.Count)
            {
                // We require file source information.
                Table mainWixFileTable = mainTransform.Tables["WixFile"];
                if (null == mainWixFileTable)
                {
                    throw new WixException(WixErrors.AdminImageRequired(productCode));
                }

                FileRowCollection mainFileRows = new FileRowCollection();
                mainFileRows.AddRange(mainFileTable.Rows);

                Table pairedFileTable = pairedTransform.EnsureTable(mainFileTable.Definition);
                foreach (WixFileRow mainWixFileRow in mainWixFileTable.Rows)
                {
                    FileRow mainFileRow = mainFileRows[mainWixFileRow.File];

                    // set File.Sequence to non null to satisfy transform bind
                    mainFileRow.Sequence = 1;

                    // delete's don't need rows in the paired transform
                    if (mainFileRow.Operation == RowOperation.Delete)
                    {
                        continue;
                    }

                    FileRow pairedFileRow = (FileRow)pairedFileTable.CreateRow(null);
                    pairedFileRow.Operation = RowOperation.Modify;
                    for (int i = 0; i < mainFileRow.Fields.Length; i++)
                    {
                        pairedFileRow[i] = mainFileRow[i];
                    }

                    // override authored media for patch bind
                    // TODO: consider using File/@DiskId for patch media
                    mainFileRow.DiskId = mediaRow.DiskId;
                    mainWixFileRow.DiskId = mediaRow.DiskId;
                    // suppress any change to File.Sequence to avoid bloat
                    mainFileRow.Fields[7].Modified = false;

                    // force File row to appear in the transform
                    switch (mainFileRow.Operation)
                    {
                        case RowOperation.Modify:
                        case RowOperation.Add:
                            // set msidbFileAttributesPatchAdded
                            pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                            pairedFileRow.Fields[6].Modified = true;
                            pairedFileRow.Operation = mainFileRow.Operation;
                            break;
                        default:
                            pairedFileRow.Fields[6].Modified = false;
                            break;
                    }
                }
            }

            // add Media row to pairedTransform
            Table pairedMediaTable = pairedTransform.EnsureTable(this.tableDefinitions["Media"]);
            Row pairedMediaRow = pairedMediaTable.CreateRow(null);
            pairedMediaRow.Operation = RowOperation.Add;
            for (int i = 0; i < mediaRow.Fields.Length; i++)
            {
                pairedMediaRow[i] = mediaRow[i];
            }

            // add PatchPackage for this Media
            Table pairedPackageTable = pairedTransform.EnsureTable(this.tableDefinitions["PatchPackage"]);
            pairedPackageTable.Operation = TableOperation.Add;
            Row pairedPackageRow = pairedPackageTable.CreateRow(null);
            pairedPackageRow.Operation = RowOperation.Add;
            pairedPackageRow[0] = patchId;
            pairedPackageRow[1] = mediaRow.DiskId;

            // add property to both identify client patches and whether those patches are removable or not
            int allowRemoval = 0;
            Table msiPatchMetadataTable = this.patch.Tables["MsiPatchMetadata"];
            if (null != msiPatchMetadataTable)
            {
                foreach (Row msiPatchMetadataRow in msiPatchMetadataTable.Rows)
                {
                    // get the value of the standard AllowRemoval property, if present
                    string company = (string)msiPatchMetadataRow[0];
                    if ((null == company || 0 == company.Length) && "AllowRemoval" == (string)msiPatchMetadataRow[1])
                    {
                        allowRemoval = Int32.Parse((string)msiPatchMetadataRow[2], CultureInfo.InvariantCulture);
                    }
                }
            }

            // add the property to the patch transform's Property table
            Table pairedPropertyTable = pairedTransform.EnsureTable(this.tableDefinitions["Property"]);
            pairedPropertyTable.Operation = TableOperation.Add;
            Row pairedPropertyRow = pairedPropertyTable.CreateRow(null);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = string.Concat(clientPatchId, ".AllowRemoval");
            pairedPropertyRow[1] = allowRemoval.ToString(CultureInfo.InvariantCulture);

            // add this patch code GUID to the patch transform to identify
            // which patches are installed, including in multi-patch
            // installations.
            pairedPropertyRow = pairedPropertyTable.CreateRow(null);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = string.Concat(clientPatchId, ".PatchCode");
            pairedPropertyRow[1] = patchId;

            // add PATCHNEWPACKAGECODE to apply to admin layouts
            pairedPropertyRow = pairedPropertyTable.CreateRow(null);
            pairedPropertyRow.Operation = RowOperation.Add;
            pairedPropertyRow[0] = "PATCHNEWPACKAGECODE";
            pairedPropertyRow[1] = patchId;

            // add PATCHNEWSUMMARYCOMMENTS and PATCHNEWSUMMARYSUBJECT to apply to admin layouts
            Table _summaryInformationTable = this.patch.Tables["_SummaryInformation"];
            if (null != _summaryInformationTable)
            {
                foreach (Row row in _summaryInformationTable.Rows)
                {
                    if (3 == (int)row[0]) // PID_SUBJECT
                    {
                        pairedPropertyRow = pairedPropertyTable.CreateRow(null);
                        pairedPropertyRow.Operation = RowOperation.Add;
                        pairedPropertyRow[0] = "PATCHNEWSUMMARYSUBJECT";
                        pairedPropertyRow[1] = row[1];
                    }
                    else if (6 == (int)row[0]) // PID_COMMENTS
                    {
                        pairedPropertyRow = pairedPropertyTable.CreateRow(null);
                        pairedPropertyRow.Operation = RowOperation.Add;
                        pairedPropertyRow[0] = "PATCHNEWSUMMARYCOMMENTS";
                        pairedPropertyRow[1] = row[1];
                    }
                }
            }

            return pairedTransform;
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            WixErrorEventArgs errorEventArgs = mea as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, mea);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
