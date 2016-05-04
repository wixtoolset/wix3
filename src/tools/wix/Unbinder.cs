// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;

    using Microsoft.Tools.WindowsInstallerXml.Cab;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;
    using Microsoft.Tools.WindowsInstallerXml.Ole32;

    /// <summary>
    /// Unbinder core of the Windows Installer Xml toolset.
    /// </summary>
    public sealed class Unbinder : IMessageHandler
    {
        private string emptyFile;
        private bool isAdminImage;
        private int sectionCount;
        private bool suppressDemodularization;
        private bool suppressExtractCabinets;
        private TableDefinitionCollection tableDefinitions;
        private ArrayList unbinderExtensions;
        private TempFileCollection tempFiles;

        /// <summary>
        /// Creates a new unbinder object with a default set of table definitions.
        /// </summary>
        public Unbinder()
        {
            this.tableDefinitions = Installer.GetTableDefinitions();
            this.unbinderExtensions = new ArrayList();
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets or sets whether the input msi is an admin image.
        /// </summary>
        /// <value>Set to true if the input msi is part of an admin image.</value>
        public bool IsAdminImage
        {
            get { return this.isAdminImage; }
            set { this.isAdminImage = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress demodularizing values.
        /// </summary>
        /// <value>The option to suppress demodularizing values.</value>
        public bool SuppressDemodularization
        {
            get { return this.suppressDemodularization; }
            set { this.suppressDemodularization = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress extracting cabinets.
        /// </summary>
        /// <value>The option to suppress extracting cabinets.</value>
        public bool SuppressExtractCabinets
        {
            get { return this.suppressExtractCabinets; }
            set { this.suppressExtractCabinets = value; }
        }

        /// <summary>
        /// Gets or sets the temporary path for the Binder.  If left null, the binder
        /// will use %TEMP% environment variable.
        /// </summary>
        /// <value>Path to temp files.</value>
        public string TempFilesLocation
        {
            get
            {
                return null == this.tempFiles ? String.Empty : this.tempFiles.BasePath;
            }

            set
            {
                if (null == value)
                {
                    this.tempFiles = new TempFileCollection();
                }
                else
                {
                    this.tempFiles = new TempFileCollection(value);
                }
            }
        }

        /// <summary>
        /// Adds an extension.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        public void AddExtension(WixExtension extension)
        {
            if (null != extension.TableDefinitions)
            {
                foreach (TableDefinition tableDefinition in extension.TableDefinitions)
                {
                    if (!this.tableDefinitions.Contains(tableDefinition.Name))
                    {
                        this.tableDefinitions.Add(tableDefinition);
                    }
                    else
                    {
                        throw new WixException(WixErrors.DuplicateExtensionTable(extension.GetType().ToString(), tableDefinition.Name));
                    }
                }
            }

            if (null != extension.UnbinderExtension)
            {
                this.unbinderExtensions.Add(extension.UnbinderExtension);
            }
        }

        /// <summary>
        /// Unbind a Windows Installer file.
        /// </summary>
        /// <param name="file">The Windows Installer file.</param>
        /// <param name="outputType">The type of output to create.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The output representing the database.</returns>
        public Output Unbind(string file, OutputType outputType, string exportBasePath)
        {
            if (!File.Exists(file))
            {
                if (OutputType.Transform == outputType)
                {
                    throw new WixException(WixErrors.FileNotFound(null, file, "Transform"));
                }
                else
                {
                    throw new WixException(WixErrors.FileNotFound(null, file, "Database"));
                }
            }

            // if we don't have the temporary files object yet, get one
            if (null == this.tempFiles)
            {
                this.TempFilesLocation = null;
            }
            Directory.CreateDirectory(this.tempFiles.BasePath); // ensure the base path is there

            if (OutputType.Patch == outputType)
            {
                return this.UnbindPatch(file, exportBasePath);
            }
            else if (OutputType.Transform == outputType)
            {
                return this.UnbindTransform(file, exportBasePath);
            }
            else if (OutputType.Bundle == outputType)
            {
                return this.UnbindBundle(file, exportBasePath);
            }
            else // other database types
            {
                return this.UnbindDatabase(file, outputType, exportBasePath);
            }
        }

        /// <summary>
        /// Cleans up the temp files used by the Decompiler.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        /// <remarks>
        /// This should be called after every call to Decompile to ensure there
        /// are no conflicts between each decompiled database.
        /// </remarks>
        public bool DeleteTempFiles()
        {
            if (null == this.tempFiles)
            {
                return true; // no work to do
            }
            else
            {
                bool deleted = Common.DeleteTempFiles(this.tempFiles.BasePath, this);

                if (deleted)
                {
                    this.tempFiles = null; // temp files have been deleted, no need to remember this now
                }

                return deleted;
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, e);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }

        /// <summary>
        /// Unbind an MSI database file.
        /// </summary>
        /// <param name="databaseFile">The database file.</param>
        /// <param name="outputType">The output type.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The unbound database.</returns>
        private Output UnbindDatabase(string databaseFile, OutputType outputType, string exportBasePath)
        {
            Output output;

            try
            {
                using (Database database = new Database(databaseFile, OpenDatabase.ReadOnly))
                {
                    output = this.UnbindDatabase(databaseFile, database, outputType, exportBasePath, false);

                    // extract the files from the cabinets
                    if (null != exportBasePath && !this.suppressExtractCabinets)
                    {
                        this.ExtractCabinets(output, database, databaseFile, exportBasePath);
                    }
                }
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    throw new WixException(WixErrors.OpenDatabaseFailed(databaseFile));
                }

                throw;
            }

            return output;
        }

        /// <summary>
        /// Unbind an MSI database file.
        /// </summary>
        /// <param name="databaseFile">The database file.</param>
        /// <param name="database">The opened database.</param>
        /// <param name="outputType">The type of output to create.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <param name="skipSummaryInfo">Option to skip unbinding the _SummaryInformation table.</param>
        /// <returns>The output representing the database.</returns>
        private Output UnbindDatabase(string databaseFile, Database database, OutputType outputType, string exportBasePath, bool skipSummaryInfo)
        {
            string modularizationGuid = null;
            Output output = new Output(SourceLineNumberCollection.FromFileName(databaseFile));
            View validationView = null;

            // set the output type
            output.Type = outputType;

            // get the codepage
            database.Export("_ForceCodepage", this.TempFilesLocation, "_ForceCodepage.idt");
            using (StreamReader sr = File.OpenText(Path.Combine(this.TempFilesLocation, "_ForceCodepage.idt")))
            {
                string line;

                while (null != (line = sr.ReadLine()))
                {
                    string[] data = line.Split('\t');

                    if (2 == data.Length)
                    {
                        output.Codepage = Convert.ToInt32(data[0], CultureInfo.InvariantCulture);
                    }
                }
            }

            // get the summary information table if it exists; it won't if unbinding a transform
            if (!skipSummaryInfo)
            {
                using (SummaryInformation summaryInformation = new SummaryInformation(database))
                {
                    Table table = new Table(null, this.tableDefinitions["_SummaryInformation"]);

                    for (int i = 1; 19 >= i; i++)
                    {
                        string value = summaryInformation.GetProperty(i);

                        if (0 < value.Length)
                        {
                            Row row = table.CreateRow(output.SourceLineNumbers);
                            row[0] = i;
                            row[1] = value;
                        }
                    }

                    output.Tables.Add(table);
                }
            }

            try
            {
                // open a view on the validation table if it exists
                if (database.TableExists("_Validation"))
                {
                    validationView = database.OpenView("SELECT * FROM `_Validation` WHERE `Table` = ? AND `Column` = ?");
                }

                // get the normal tables
                using (View tablesView = database.OpenExecuteView("SELECT * FROM _Tables"))
                {
                    while (true)
                    {
                        using (Record tableRecord = tablesView.Fetch())
                        {
                            if (null == tableRecord)
                            {
                                break;
                            }

                            string tableName = tableRecord.GetString(1);

                            using (View tableView = database.OpenExecuteView(String.Format(CultureInfo.InvariantCulture, "SELECT * FROM `{0}`", tableName)))
                            {
                                TableDefinition tableDefinition = new TableDefinition(tableName, false, false);
                                Hashtable tablePrimaryKeys = new Hashtable();

                                using (Record columnNameRecord = tableView.GetColumnInfo(MsiInterop.MSICOLINFONAMES),
                                              columnTypeRecord = tableView.GetColumnInfo(MsiInterop.MSICOLINFOTYPES))
                                {
                                    int columnCount = columnNameRecord.GetFieldCount();

                                    // index the primary keys
                                    using (Record primaryKeysRecord = database.PrimaryKeys(tableName))
                                    {
                                        int primaryKeysFieldCount = primaryKeysRecord.GetFieldCount();

                                        for (int i = 1; i <= primaryKeysFieldCount; i++)
                                        {
                                            tablePrimaryKeys[primaryKeysRecord.GetString(i)] = null;
                                        }
                                    }

                                    for (int i = 1; i <= columnCount; i++)
                                    {
                                        string columnName = columnNameRecord.GetString(i);
                                        string idtType = columnTypeRecord.GetString(i);

                                        ColumnType columnType;
                                        int length;
                                        bool nullable;

                                        ColumnCategory columnCategory = ColumnCategory.Unknown;
                                        ColumnModularizeType columnModularizeType = ColumnModularizeType.None;
                                        bool primary = tablePrimaryKeys.Contains(columnName);
                                        bool minValueSet = false;
                                        int minValue = -1;
                                        bool maxValueSet = false;
                                        int maxValue = -1;
                                        string keyTable = null;
                                        bool keyColumnSet = false;
                                        int keyColumn = -1;
                                        string category = null;
                                        string set = null;
                                        string description = null;

                                        // get the column type, length, and whether its nullable
                                        switch (Char.ToLower(idtType[0], CultureInfo.InvariantCulture))
                                        {
                                            case 'i':
                                                columnType = ColumnType.Number;
                                                break;
                                            case 'l':
                                                columnType = ColumnType.Localized;
                                                break;
                                            case 's':
                                                columnType = ColumnType.String;
                                                break;
                                            case 'v':
                                                columnType = ColumnType.Object;
                                                break;
                                            default:
                                                // TODO: error
                                                columnType = ColumnType.Unknown;
                                                break;
                                        }
                                        length = Convert.ToInt32(idtType.Substring(1), CultureInfo.InvariantCulture);
                                        nullable = Char.IsUpper(idtType[0]);

                                        // try to get validation information
                                        if (null != validationView)
                                        {
                                            using (Record validationRecord = new Record(2))
                                            {
                                                validationRecord.SetString(1, tableName);
                                                validationRecord.SetString(2, columnName);

                                                validationView.Execute(validationRecord);
                                            }

                                            using (Record validationRecord = validationView.Fetch())
                                            {
                                                if (null != validationRecord)
                                                {
                                                    string validationNullable = validationRecord.GetString(3);
                                                    minValueSet = !validationRecord.IsNull(4);
                                                    minValue = (minValueSet ? validationRecord.GetInteger(4) : -1);
                                                    maxValueSet = !validationRecord.IsNull(5);
                                                    maxValue = (maxValueSet ? validationRecord.GetInteger(5) : -1);
                                                    keyTable = (!validationRecord.IsNull(6) ? validationRecord.GetString(6) : null);
                                                    keyColumnSet = !validationRecord.IsNull(7);
                                                    keyColumn = (keyColumnSet ? validationRecord.GetInteger(7) : -1);
                                                    category = (!validationRecord.IsNull(8) ? validationRecord.GetString(8) : null);
                                                    set = (!validationRecord.IsNull(9) ? validationRecord.GetString(9) : null);
                                                    description = (!validationRecord.IsNull(10) ? validationRecord.GetString(10) : null);

                                                    // check the validation nullable value against the column definition
                                                    if (null == validationNullable)
                                                    {
                                                        // TODO: warn for illegal validation nullable column
                                                    }
                                                    else if ((nullable && "Y" != validationNullable) || (!nullable && "N" != validationNullable))
                                                    {
                                                        // TODO: warn for mismatch between column definition and validation nullable
                                                    }

                                                    // convert category to ColumnCategory
                                                    if (null != category)
                                                    {
                                                        try
                                                        {
                                                            columnCategory = (ColumnCategory)Enum.Parse(typeof(ColumnCategory), category, true);
                                                        }
                                                        catch (ArgumentException)
                                                        {
                                                            columnCategory = ColumnCategory.Unknown;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // TODO: warn about no validation information
                                                }
                                            }
                                        }

                                        // guess the modularization type
                                        if ("Icon" == keyTable && 1 == keyColumn)
                                        {
                                            columnModularizeType = ColumnModularizeType.Icon;
                                        }
                                        else if ("Condition" == columnName)
                                        {
                                            columnModularizeType = ColumnModularizeType.Condition;
                                        }
                                        else if (ColumnCategory.Formatted == columnCategory || ColumnCategory.FormattedSDDLText == columnCategory)
                                        {
                                            columnModularizeType = ColumnModularizeType.Property;
                                        }
                                        else if (ColumnCategory.Identifier == columnCategory)
                                        {
                                            columnModularizeType = ColumnModularizeType.Column;
                                        }

                                        tableDefinition.Columns.Add(new ColumnDefinition(columnName, columnType, length, primary, nullable, columnModularizeType, (ColumnType.Localized == columnType), minValueSet, minValue, maxValueSet, maxValue, keyTable, keyColumnSet, keyColumn, columnCategory, set, description, true, true));
                                    }
                                }
                                // use our table definitions if core properties are the same; this allows us to take advantage
                                // of wix concepts like localizable columns which current code assumes
                                if (this.tableDefinitions.Contains(tableName) && 0 == tableDefinition.CompareTo(this.tableDefinitions[tableName]))
                                {
                                    tableDefinition = this.tableDefinitions[tableName];
                                }
                                Table table = new Table(null, tableDefinition);

                                while (true)
                                {
                                    using (Record rowRecord = tableView.Fetch())
                                    {
                                        if (null == rowRecord)
                                        {
                                            break;
                                        }

                                        int recordCount = rowRecord.GetFieldCount();
                                        Row row = table.CreateRow(output.SourceLineNumbers);

                                        for (int i = 0; recordCount > i && row.Fields.Length > i; i++)
                                        {
                                            if (rowRecord.IsNull(i + 1))
                                            {
                                                if (!row.Fields[i].Column.IsNullable)
                                                {
                                                    // TODO: display an error for a null value in a non-nullable field OR
                                                    // display a warning and put an empty string in the value to let the compiler handle it
                                                    // (the second option is risky because the later code may make certain assumptions about
                                                    // the contents of a row value)
                                                }
                                            }
                                            else
                                            {
                                                switch (row.Fields[i].Column.Type)
                                                {
                                                    case ColumnType.Number:
                                                        bool success = false;
                                                        int intValue = rowRecord.GetInteger(i + 1);
                                                        if (row.Fields[i].Column.IsLocalizable)
                                                        {
                                                            success = row.BestEffortSetField(i, Convert.ToString(intValue, CultureInfo.InvariantCulture));
                                                        }
                                                        else
                                                        {
                                                            success = row.BestEffortSetField(i, intValue);
                                                        }

                                                        if (!success)
                                                        {
                                                            this.OnMessage(WixWarnings.BadColumnDataIgnored(row.SourceLineNumbers, Convert.ToString(intValue, CultureInfo.InvariantCulture), tableName, row.Fields[i].Column.Name));
                                                        }
                                                        break;
                                                    case ColumnType.Object:
                                                        string sourceFile = "FILE NOT EXPORTED, USE THE dark.exe -x OPTION TO EXPORT BINARIES";

                                                        if (null != exportBasePath)
                                                        {
                                                            string relativeSourceFile = Path.Combine(tableName, row.GetPrimaryKey('.'));
                                                            sourceFile = Path.Combine(exportBasePath, relativeSourceFile);

                                                            // ensure the parent directory exists
                                                            System.IO.Directory.CreateDirectory(Path.Combine(exportBasePath, tableName));

                                                            using (FileStream fs = System.IO.File.Create(sourceFile))
                                                            {
                                                                int bytesRead;
                                                                byte[] buffer = new byte[512];

                                                                while (0 != (bytesRead = rowRecord.GetStream(i + 1, buffer, buffer.Length)))
                                                                {
                                                                    fs.Write(buffer, 0, bytesRead);
                                                                }
                                                            }
                                                        }

                                                        row[i] = sourceFile;
                                                        break;
                                                    default:
                                                        string value = rowRecord.GetString(i + 1);

                                                        switch (row.Fields[i].Column.Category)
                                                        {
                                                            case ColumnCategory.Guid:
                                                                value = value.ToUpper(CultureInfo.InvariantCulture);
                                                                break;
                                                        }

                                                        // de-modularize
                                                        if (!this.suppressDemodularization && OutputType.Module == output.Type && ColumnModularizeType.None != row.Fields[i].Column.ModularizeType)
                                                        {
                                                            Regex modularization = new Regex(@"\.[0-9A-Fa-f]{8}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{4}_[0-9A-Fa-f]{12}");

                                                            if (null == modularizationGuid)
                                                            {
                                                                Match match = modularization.Match(value);
                                                                if (match.Success)
                                                                {
                                                                    modularizationGuid = String.Concat('{', match.Value.Substring(1).Replace('_', '-'), '}');
                                                                }
                                                            }

                                                            value = modularization.Replace(value, String.Empty);
                                                        }

                                                        // escape "$(" for the preprocessor
                                                        value = value.Replace("$(", "$$(");

                                                        // escape things that look like wix variables
                                                        MatchCollection matches = Common.WixVariableRegex.Matches(value);
                                                        for (int j = matches.Count - 1; 0 <= j; j--)
                                                        {
                                                            value = value.Insert(matches[j].Index, "!");
                                                        }

                                                        row[i] = value;
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }

                                output.Tables.Add(table);
                            }

                        }
                    }
                }
            }
            finally
            {
                if (null != validationView)
                {
                    validationView.Close();
                }
            }

            // set the modularization guid as the PackageCode
            if (null != modularizationGuid)
            {
                Table table = output.Tables["_SummaryInformation"];

                foreach (Row row in table.Rows)
                {
                    if (9 == (int)row[0]) // PID_REVNUMBER
                    {
                        row[1] = modularizationGuid;
                    }
                }
            }

            if (this.isAdminImage)
            {
                GenerateWixFileTable(databaseFile, output);
                GenerateSectionIds(output);
            }

            return output;
        }

        /// <summary>
        /// Creates section ids on rows which form logical groupings of resources.
        /// </summary>
        /// <param name="output">The Output that represents the msi database.</param>
        private void GenerateSectionIds(Output output)
        {
            // First assign and index section ids for the tables that are in their own sections.
            AssignSectionIdsToTable(output.Tables["Binary"], 0);
            Hashtable componentSectionIdIndex = AssignSectionIdsToTable(output.Tables["Component"], 0);
            Hashtable customActionSectionIdIndex = AssignSectionIdsToTable(output.Tables["CustomAction"], 0);
            AssignSectionIdsToTable(output.Tables["Directory"], 0);
            Hashtable featureSectionIdIndex = AssignSectionIdsToTable(output.Tables["Feature"], 0);
            AssignSectionIdsToTable(output.Tables["Icon"], 0);
            Hashtable digitalCertificateSectionIdIndex = AssignSectionIdsToTable(output.Tables["MsiDigitalCertificate"], 0);
            AssignSectionIdsToTable(output.Tables["Property"], 0);

            // Now handle all the tables that rely on the first set of indexes but also produce their own indexes. Order matters here.
            Hashtable fileSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["File"], componentSectionIdIndex, 1, 0);
            Hashtable appIdSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["Class"], componentSectionIdIndex, 2, 5);
            Hashtable odbcDataSourceSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ODBCDataSource"], componentSectionIdIndex, 1, 0);
            Hashtable odbcDriverSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ODBCDriver"], componentSectionIdIndex, 1, 0);
            Hashtable registrySectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["Registry"], componentSectionIdIndex, 5, 0);
            Hashtable serviceInstallSectionIdIndex = ConnectTableToSectionAndIndex(output.Tables["ServiceInstall"], componentSectionIdIndex, 11, 0);

            // Now handle all the tables which only rely on previous indexes and order does not matter.
            foreach (Table table in output.Tables)
            {
                switch (table.Name)
                {
                    case "WixFile":
                    case "MsiFileHash":
                        ConnectTableToSection(table, fileSectionIdIndex, 0);
                        break;
                    case "MsiAssembly":
                    case "MsiAssemblyName":
                        ConnectTableToSection(table, componentSectionIdIndex, 0);
                        break;
                    case "MsiPackageCertificate":
                    case "MsiPatchCertificate":
                        ConnectTableToSection(table, digitalCertificateSectionIdIndex, 1);
                        break;
                    case "CreateFolder":
                    case "FeatureComponents":
                    case "MoveFile":
                    case "ReserveCost":
                    case "ODBCTranslator":
                        ConnectTableToSection(table, componentSectionIdIndex, 1);
                        break;
                    case "TypeLib":
                        ConnectTableToSection(table, componentSectionIdIndex, 2);
                        break;
                    case "Shortcut":
                    case "Environment":
                        ConnectTableToSection(table, componentSectionIdIndex, 3);
                        break;
                    case "RemoveRegistry":
                        ConnectTableToSection(table, componentSectionIdIndex, 4);
                        break;
                    case "ServiceControl":
                        ConnectTableToSection(table, componentSectionIdIndex, 5);
                        break;
                    case "IniFile":
                    case "RemoveIniFile":
                        ConnectTableToSection(table, componentSectionIdIndex, 7);
                        break;
                    case "AppId":
                        ConnectTableToSection(table, appIdSectionIdIndex, 0);
                        break;
                    case "Condition":
                        ConnectTableToSection(table, featureSectionIdIndex, 0);
                        break;
                    case "ODBCSourceAttribute":
                        ConnectTableToSection(table, odbcDataSourceSectionIdIndex, 0);
                        break;
                    case "ODBCAttribute":
                        ConnectTableToSection(table, odbcDriverSectionIdIndex, 0);
                        break;
                    case "AdminExecuteSequence":
                    case "AdminUISequence":
                    case "AdvtExecuteSequence":
                    case "AdvtUISequence":
                    case "InstallExecuteSequence":
                    case "InstallUISequence":
                        ConnectTableToSection(table, customActionSectionIdIndex, 0);
                        break;
                    case "LockPermissions":
                    case "MsiLockPermissions":
                        foreach (Row row in table.Rows)
                        {
                            string lockObject = (string)row[0];
                            string tableName = (string)row[1];
                            switch (tableName)
                            {
                                case "File":
                                    row.SectionId = (string)fileSectionIdIndex[lockObject];
                                    break;
                                case "Registry":
                                    row.SectionId = (string)registrySectionIdIndex[lockObject];
                                    break;
                                case "ServiceInstall":
                                    row.SectionId = (string)serviceInstallSectionIdIndex[lockObject];
                                    break;
                            }
                        }
                        break;
                }
            }

            // Now pass the output to each unbinder extension to allow them to analyze the output and determine thier proper section ids.
            foreach (UnbinderExtension extension in this.unbinderExtensions)
            {
                extension.GenerateSectionIds(output);
            }
        }

        /// <summary>
        /// Creates new section ids on all the rows in a table.
        /// </summary>
        /// <param name="table">The table to add sections to.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A Hashtable containing the tables key for each row paired with its assigned section id.</returns>
        private Hashtable AssignSectionIdsToTable(Table table, int rowPrimaryKeyIndex)
        {
            Hashtable hashtable = new Hashtable();
            if (null != table)
            {
                foreach (Row row in table.Rows)
                {
                    row.SectionId = GetNewSectionId();
                    hashtable.Add(row[rowPrimaryKeyIndex], row.SectionId);
                }
            }
            return hashtable;
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        private static void ConnectTableToSection(Table table, Hashtable sectionIdIndex, int rowIndex)
        {
            if (null != table)
            {
                foreach (Row row in table.Rows)
                {
                    if (sectionIdIndex.ContainsKey(row[rowIndex]))
                    {
                        row.SectionId = (string)sectionIdIndex[row[rowIndex]];
                    }
                }
            }
        }

        /// <summary>
        /// Connects a table's rows to an already sectioned table and produces an index for other tables to connect to it.
        /// </summary>
        /// <param name="table">The table containing rows that need to be connected to sections.</param>
        /// <param name="sectionIdIndex">A hashtable containing keys to map table to its section.</param>
        /// <param name="rowIndex">The index of the column which is used as the foreign key in to the sectionIdIndex.</param>
        /// <param name="rowPrimaryKeyIndex">The index of the column which is used by other tables to reference this table.</param>
        /// <returns>A Hashtable containing the tables key for each row paired with its assigned section id.</returns>
        private static Hashtable ConnectTableToSectionAndIndex(Table table, Hashtable sectionIdIndex, int rowIndex, int rowPrimaryKeyIndex)
        {
            Hashtable newHashTable = new Hashtable();
            if (null != table)
            {
                foreach (Row row in table.Rows)
                {
                    if (!sectionIdIndex.ContainsKey(row[rowIndex]))
                    {
                        continue;
                    }

                    row.SectionId = (string)sectionIdIndex[row[rowIndex]];
                    if (null != row[rowPrimaryKeyIndex])
                    {
                        newHashTable.Add(row[rowPrimaryKeyIndex], row.SectionId);
                    }
                }
            }
            return newHashTable;
        }

        /// <summary>
        /// Creates a new section identifier to be used when adding a section to an output.
        /// </summary>
        /// <returns>A string representing a new section id.</returns>
        private string GetNewSectionId()
        {
            this.sectionCount++;
            return "wix.section." + this.sectionCount.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Generates the WixFile table based on a path to an admin image msi and an Output.
        /// </summary>
        /// <param name="databaseFile">The path to the msi database file in an admin image.</param>
        /// <param name="output">The Output that represents the msi database.</param>
        private void GenerateWixFileTable(string databaseFile, Output output)
        {
            string adminRootPath = Path.GetDirectoryName(databaseFile);

            Hashtable componentDirectoryIndex = new Hashtable();
            Table componentTable = output.Tables["Component"];
            foreach (Row row in componentTable.Rows)
            {
                componentDirectoryIndex.Add(row[0], row[2]);
            }

            // Index full source paths for all directories
            Hashtable directoryDirectoryParentIndex = new Hashtable();
            Hashtable directoryFullPathIndex = new Hashtable();
            Hashtable directorySourceNameIndex = new Hashtable();
            Table directoryTable = output.Tables["Directory"];
            foreach (Row row in directoryTable.Rows)
            {
                directoryDirectoryParentIndex.Add(row[0], row[1]);
                if (null == row[1])
                {
                    directoryFullPathIndex.Add(row[0], adminRootPath);
                }
                else
                {
                    directorySourceNameIndex.Add(row[0], GetAdminSourceName((string)row[2]));
                }
            }

            foreach (DictionaryEntry directoryEntry in directoryDirectoryParentIndex)
            {
                if (!directoryFullPathIndex.ContainsKey(directoryEntry.Key))
                {
                    GetAdminFullPath((string)directoryEntry.Key, directoryDirectoryParentIndex, directorySourceNameIndex, directoryFullPathIndex);
                }
            }

            Table fileTable = output.Tables["File"];
            Table wixFileTable = output.Tables.EnsureTable(null, this.tableDefinitions["WixFile"]);
            foreach (Row row in fileTable.Rows)
            {
                WixFileRow wixFileRow = new WixFileRow(null, this.tableDefinitions["WixFile"]);
                wixFileRow.File = (string)row[0];
                wixFileRow.Directory = (string)componentDirectoryIndex[(string)row[1]];
                wixFileRow.Source = Path.Combine((string)directoryFullPathIndex[wixFileRow.Directory], GetAdminSourceName((string)row[2]));

                if (!File.Exists(wixFileRow.Source))
                {
                    throw new WixException(WixErrors.WixFileNotFound(wixFileRow.Source));
                }

                wixFileTable.Rows.Add(wixFileRow);
            }
        }

        /// <summary>
        /// Gets the full path of a directory. Populates the full path index with the directory's full path and all of its parent directorie's full paths.
        /// </summary>
        /// <param name="directory">The directory identifier.</param>
        /// <param name="directoryDirectoryParentIndex">The Hashtable containing all the directory to directory parent mapping.</param>
        /// <param name="directorySourceNameIndex">The Hashtable containing all the directory to source name mapping.</param>
        /// <param name="directoryFullPathIndex">The Hashtable containing a mapping between all of the directories and their previously calculated full paths.</param>
        /// <returns>The full path to the directory.</returns>
        private string GetAdminFullPath(string directory, Hashtable directoryDirectoryParentIndex, Hashtable directorySourceNameIndex, Hashtable directoryFullPathIndex)
        {
            string parent = (string)directoryDirectoryParentIndex[directory];
            string sourceName = (string)directorySourceNameIndex[directory];

            string parentFullPath;
            if (directoryFullPathIndex.ContainsKey(parent))
            {
                parentFullPath = (string)directoryFullPathIndex[parent];
            }
            else
            {
                parentFullPath = GetAdminFullPath(parent, directoryDirectoryParentIndex, directorySourceNameIndex, directoryFullPathIndex);
            }

            if (null == sourceName)
            {
                sourceName = String.Empty;
            }

            string fullPath = Path.Combine(parentFullPath, sourceName);
            directoryFullPathIndex.Add(directory, fullPath);

            return fullPath;
        }

        /// <summary>
        /// Get the source name in an admin image.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <returns>The source name of the directory in an admin image.</returns>
        private static string GetAdminSourceName(string value)
        {
            string name = null;
            string[] names;
            string shortname = null;
            string shortsourcename = null;
            string sourcename = null;

            names = Installer.GetNames(value);

            if (null != names[0] && "." != names[0])
            {
                if (null != names[1])
                {
                    shortname = names[0];
                }
                else
                {
                    name = names[0];
                }
            }

            if (null != names[1])
            {
                name = names[1];
            }

            if (null != names[2])
            {
                if (null != names[3])
                {
                    shortsourcename = names[2];
                }
                else
                {
                    sourcename = names[2];
                }
            }

            if (null != names[3])
            {
                sourcename = names[3];
            }

            if (null != sourcename)
            {
                return sourcename;
            }
            else if (null != shortsourcename)
            {
                return shortsourcename;
            }
            else if (null != name)
            {
                return name;
            }
            else
            {
                return shortname;
            }
        }

        /// <summary>
        /// Unbind an MSP patch file.
        /// </summary>
        /// <param name="patchFile">The patch file.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The unbound patch.</returns>
        private Output UnbindPatch(string patchFile, string exportBasePath)
        {
            Output patch;

            // patch files are essentially database files (use a special flag to let the API know its a patch file)
            try
            {
                using (Database database = new Database(patchFile, OpenDatabase.ReadOnly | OpenDatabase.OpenPatchFile))
                {
                    patch = this.UnbindDatabase(patchFile, database, OutputType.Patch, exportBasePath, false);
                }
            }
            catch (Win32Exception e)
            {
                if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                {
                    throw new WixException(WixErrors.OpenDatabaseFailed(patchFile));
                }

                throw;
            }

            // retrieve the transforms (they are in substorages)
            using (Storage storage = Storage.Open(patchFile, StorageMode.Read | StorageMode.ShareDenyWrite))
            {
                Table summaryInformationTable = patch.Tables["_SummaryInformation"];
                foreach (Row row in summaryInformationTable.Rows)
                {
                    if (8 == (int)row[0]) // PID_LASTAUTHOR
                    {
                        string value = (string)row[1];

                        foreach (string decoratedSubStorageName in value.Split(';'))
                        {
                            string subStorageName = decoratedSubStorageName.Substring(1);
                            string transformFile = Path.Combine(this.TempFilesLocation, String.Concat("Transform", Path.DirectorySeparatorChar, subStorageName, ".mst"));

                            // ensure the parent directory exists
                            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(transformFile));

                            // copy the substorage to a new storage for the transform file
                            using (Storage subStorage = storage.OpenStorage(subStorageName))
                            {
                                using (Storage transformStorage = Storage.CreateDocFile(transformFile, StorageMode.ReadWrite | StorageMode.ShareExclusive | StorageMode.Create))
                                {
                                    subStorage.CopyTo(transformStorage);
                                }
                            }

                            // unbind the transform
                            Output transform = this.UnbindTransform(transformFile, (null == exportBasePath ? null : Path.Combine(exportBasePath, subStorageName)));
                            patch.SubStorages.Add(new SubStorage(subStorageName, transform));
                        }

                        break;
                    }
                }
            }

            // extract the files from the cabinets
            // TODO: use per-transform export paths for support of multi-product patches
            if (null != exportBasePath && !this.suppressExtractCabinets)
            {
                using (Database database = new Database(patchFile, OpenDatabase.ReadOnly | OpenDatabase.OpenPatchFile))
                {
                    foreach (SubStorage subStorage in patch.SubStorages)
                    {
                        // only patch transforms should carry files
                        if (subStorage.Name.StartsWith("#", StringComparison.Ordinal))
                        {
                            this.ExtractCabinets(subStorage.Data, database, patchFile, exportBasePath);
                        }
                    }
                }
            }

            return patch;
        }

        /// <summary>
        /// Unbind an MSI transform file.
        /// </summary>
        /// <param name="transformFile">The transform file.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The unbound transform.</returns>
        private Output UnbindTransform(string transformFile, string exportBasePath)
        {
            Output transform = new Output(SourceLineNumberCollection.FromFileName(transformFile));
            transform.Type = OutputType.Transform;

            // get the summary information table
            using (SummaryInformation summaryInformation = new SummaryInformation(transformFile))
            {
                Table table = transform.Tables.EnsureTable(null, this.tableDefinitions["_SummaryInformation"]);

                for (int i = 1; 19 >= i; i++)
                {
                    string value = summaryInformation.GetProperty(i);

                    if (0 < value.Length)
                    {
                        Row row = table.CreateRow(transform.SourceLineNumbers);
                        row[0] = i;
                        row[1] = value;
                    }
                }
            }

            // create a schema msi which hopefully matches the table schemas in the transform
            Output schemaOutput = new Output(null);
            string msiDatabaseFile = Path.Combine(this.tempFiles.BasePath, "schema.msi");
            foreach (TableDefinition tableDefinition in this.tableDefinitions)
            {
                // skip unreal tables and the Patch table
                if (!tableDefinition.IsUnreal && "Patch" != tableDefinition.Name)
                {
                    schemaOutput.EnsureTable(tableDefinition);
                }
            }

            Hashtable addedRows = new Hashtable();
            Table transformViewTable;

            // bind the schema msi
            using (Binder binder = new Binder())
            {
                binder.SuppressAddingValidationRows = true;
                binder.WixVariableResolver = new WixVariableResolver();
                binder.GenerateDatabase(schemaOutput, msiDatabaseFile, true, false);

                // apply the transform to the database and retrieve the modifications
                using (Database msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
                {
                    // apply the transform with the ViewTransform option to collect all the modifications
                    msiDatabase.ApplyTransform(transformFile, TransformErrorConditions.All | TransformErrorConditions.ViewTransform);

                    // unbind the database
                    Output transformViewOutput = this.UnbindDatabase(msiDatabaseFile, msiDatabase, OutputType.Product, exportBasePath, true);

                    // index the added and possibly modified rows (added rows may also appears as modified rows)
                    transformViewTable = transformViewOutput.Tables["_TransformView"];
                    Hashtable modifiedRows = new Hashtable();
                    foreach (Row row in transformViewTable.Rows)
                    {
                        string tableName = (string) row[0];
                        string columnName = (string) row[1];
                        string primaryKeys = (string) row[2];

                        if ("INSERT" == columnName)
                        {
                            string index = String.Concat(tableName, ':', primaryKeys);

                            addedRows.Add(index, null);
                        }
                        else if ("CREATE" != columnName && "DELETE" != columnName && "DROP" != columnName && null != primaryKeys) // modified row
                        {
                            string index = String.Concat(tableName, ':', primaryKeys);

                            modifiedRows[index] = row;
                        }
                    }

                    // create placeholder rows for modified rows to make the transform insert the updated values when its applied
                    foreach (Row row in modifiedRows.Values)
                    {
                        string tableName = (string) row[0];
                        string columnName = (string) row[1];
                        string primaryKeys = (string) row[2];

                        string index = String.Concat(tableName, ':', primaryKeys);

                        // ignore information for added rows
                        if (!addedRows.Contains(index))
                        {
                            Table table = schemaOutput.Tables[tableName];
                            this.CreateRow(table, primaryKeys, true);
                        }
                    }
                }

                // re-bind the schema output with the placeholder rows
                binder.GenerateDatabase(schemaOutput, msiDatabaseFile, true, false);
            }

            // apply the transform to the database and retrieve the modifications
            using (Database msiDatabase = new Database(msiDatabaseFile, OpenDatabase.Transact))
            {
                try
                {
                    // apply the transform
                    msiDatabase.ApplyTransform(transformFile, TransformErrorConditions.All);

                    // commit the database to guard against weird errors with streams
                    msiDatabase.Commit();
                }
                catch (Win32Exception ex)
                {
                    if (0x65B == ex.NativeErrorCode)
                    {
                        // this commonly happens when the transform was built
                        // against a database schema different from the internal
                        // table definitions
                        throw new WixException(WixErrors.TransformSchemaMismatch());
                    }
                }

                // unbind the database
                Output output = this.UnbindDatabase(msiDatabaseFile, msiDatabase, OutputType.Product, exportBasePath, true);

                // index all the rows to easily find modified rows
                Hashtable rows = new Hashtable();
                foreach (Table table in output.Tables)
                {
                    foreach (Row row in table.Rows)
                    {
                        rows.Add(String.Concat(table.Name, ':', row.GetPrimaryKey('\t', " ")), row);
                    }
                }

                // process the _TransformView rows into transform rows
                foreach (Row row in transformViewTable.Rows)
                {
                    string tableName = (string)row[0];
                    string columnName = (string)row[1];
                    string primaryKeys = (string)row[2];

                    Table table = transform.Tables.EnsureTable(null, this.tableDefinitions[tableName]);

                    if ("CREATE" == columnName) // added table
                    {
                        table.Operation = TableOperation.Add;
                    }
                    else if ("DELETE" == columnName) // deleted row
                    {
                        Row deletedRow = this.CreateRow(table, primaryKeys, false);
                        deletedRow.Operation = RowOperation.Delete;
                    }
                    else if ("DROP" == columnName) // dropped table
                    {
                        table.Operation = TableOperation.Drop;
                    }
                    else if ("INSERT" == columnName) // added row
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);
                        Row addedRow = (Row)rows[index];
                        addedRow.Operation = RowOperation.Add;
                        table.Rows.Add(addedRow);
                    }
                    else if (null != primaryKeys) // modified row
                    {
                        string index = String.Concat(tableName, ':', primaryKeys);

                        // the _TransformView table includes information for added rows
                        // that looks like modified rows so it sometimes needs to be ignored
                        if (!addedRows.Contains(index))
                        {
                            Row modifiedRow = (Row)rows[index];

                            // mark the field as modified
                            int indexOfModifiedValue = modifiedRow.TableDefinition.Columns.IndexOf(columnName);
                            modifiedRow.Fields[indexOfModifiedValue].Modified = true;

                            // move the modified row into the transform the first time its encountered
                            if (RowOperation.None == modifiedRow.Operation)
                            {
                                modifiedRow.Operation = RowOperation.Modify;
                                table.Rows.Add(modifiedRow);
                            }
                        }
                    }
                    else // added column
                    {
                        table.Definition.Columns[columnName].Added = true;
                    }
                }
            }

            return transform;
        }

        /// <summary>
        /// Unbind a bundle.
        /// </summary>
        /// <param name="bundleFile">The bundle file.</param>
        /// <param name="exportBasePath">The path where files should be exported.</param>
        /// <returns>The unbound bundle.</returns>
        private Output UnbindBundle(string bundleFile, string exportBasePath)
        {
            string uxExtractPath = Path.Combine(exportBasePath, "UX");
            string acExtractPath = Path.Combine(exportBasePath, "AttachedContainer");

            using (BurnReader reader = BurnReader.Open(bundleFile, this))
            {
                reader.ExtractUXContainer(uxExtractPath, this.tempFiles.BasePath);
                reader.ExtractAttachedContainer(acExtractPath, this.tempFiles.BasePath);
            }

            return null;
        }

        /// <summary>
        /// Create a deleted or modified row.
        /// </summary>
        /// <param name="table">The table containing the row.</param>
        /// <param name="primaryKeys">The primary keys of the row.</param>
        /// <param name="setRequiredFields">Option to set all required fields with placeholder values.</param>
        /// <returns>The new row.</returns>
        private Row CreateRow(Table table, string primaryKeys, bool setRequiredFields)
        {
            Row row = table.CreateRow(null);

            string[] primaryKeyParts = primaryKeys.Split('\t');
            int primaryKeyPartIndex = 0;

            for (int i = 0; i < table.Definition.Columns.Count; i++)
            {
                ColumnDefinition columnDefinition = table.Definition.Columns[i];

                if (columnDefinition.IsPrimaryKey)
                {
                    if (ColumnType.Number == columnDefinition.Type && !columnDefinition.IsLocalizable)
                    {
                        row[i] = Convert.ToInt32(primaryKeyParts[primaryKeyPartIndex++], CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        row[i] = primaryKeyParts[primaryKeyPartIndex++];
                    }
                }
                else if (setRequiredFields)
                {
                    if (ColumnType.Number == columnDefinition.Type && !columnDefinition.IsLocalizable)
                    {
                        row[i] = 1;
                    }
                    else if (ColumnType.Object == columnDefinition.Type)
                    {
                        if (null == this.emptyFile)
                        {
                            this.emptyFile = this.tempFiles.AddExtension("empty");
                            using (FileStream fileStream = File.Create(this.emptyFile))
                            {
                            }
                        }

                        row[i] = this.emptyFile;
                    }
                    else
                    {
                        row[i] = "1";
                    }
                }
            }

            return row;
        }

        /// <summary>
        /// Extract the cabinets from a database.
        /// </summary>
        /// <param name="output">The output to use when finding cabinets.</param>
        /// <param name="database">The database containing the cabinets.</param>
        /// <param name="databaseFile">The location of the database file.</param>
        /// <param name="exportBasePath">The path where the files should be exported.</param>
        private void ExtractCabinets(Output output, Database database, string databaseFile, string exportBasePath)
        {
            string databaseBasePath = Path.GetDirectoryName(databaseFile);
            StringCollection cabinetFiles = new StringCollection();
            SortedList embeddedCabinets = new SortedList();

            // index all of the cabinet files
            if (OutputType.Module == output.Type)
            {
                embeddedCabinets.Add(0, "MergeModule.CABinet");
            }
            else if (null != output.Tables["Media"])
            {
                foreach (MediaRow mediaRow in output.Tables["Media"].Rows)
                {
                    if (null != mediaRow.Cabinet)
                    {
                        if (OutputType.Product == output.Type ||
                            (OutputType.Transform == output.Type && RowOperation.Add == mediaRow.Operation))
                        {
                            if (mediaRow.Cabinet.StartsWith("#", StringComparison.Ordinal))
                            {
                                embeddedCabinets.Add(mediaRow.DiskId, mediaRow.Cabinet.Substring(1));
                            }
                            else
                            {
                                cabinetFiles.Add(Path.Combine(databaseBasePath, mediaRow.Cabinet));
                            }
                        }
                    }
                }
            }

            // extract the embedded cabinet files from the database
            if (0 < embeddedCabinets.Count)
            {
                using (View streamsView = database.OpenView("SELECT `Data` FROM `_Streams` WHERE `Name` = ?"))
                {
                    foreach (int diskId in embeddedCabinets.Keys)
                    {
                        using(Record record = new Record(1))
                        {
                            record.SetString(1, (string)embeddedCabinets[diskId]);
                            streamsView.Execute(record);
                        }

                        using (Record record = streamsView.Fetch())
                        {
                            if (null != record)
                            {
                                // since the cabinets are stored in case-sensitive streams inside the msi, but the file system is not case-sensitive,
                                // embedded cabinets must be extracted to a canonical file name (like their diskid) to ensure extraction will always work
                                string cabinetFile = Path.Combine(this.TempFilesLocation, String.Concat("Media", Path.DirectorySeparatorChar, diskId.ToString(CultureInfo.InvariantCulture), ".cab"));

                                // ensure the parent directory exists
                                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(cabinetFile));

                                using (FileStream fs = System.IO.File.Create(cabinetFile))
                                {
                                    int bytesRead;
                                    byte[] buffer = new byte[512];

                                    while (0 != (bytesRead = record.GetStream(1, buffer, buffer.Length)))
                                    {
                                        fs.Write(buffer, 0, bytesRead);
                                    }
                                }

                                cabinetFiles.Add(cabinetFile);
                            }
                            else
                            {
                                // TODO: warning about missing embedded cabinet
                            }
                        }
                    }
                }
            }

            // extract the cabinet files
            if (0 < cabinetFiles.Count)
            {
                string fileDirectory = Path.Combine(exportBasePath, "File");

                // delete the directory and its files to prevent cab extraction due to an existing file
                if (Directory.Exists(fileDirectory))
                {
                    Directory.Delete(fileDirectory, true);
                }

                // ensure the directory exists or extraction will fail
                Directory.CreateDirectory(fileDirectory);

                foreach (string cabinetFile in cabinetFiles)
                {
                    using (WixExtractCab extractCab = new WixExtractCab())
                    {
                        try
                        {
                            extractCab.Extract(cabinetFile, fileDirectory);
                        }
                        catch (FileNotFoundException)
                        {
                            throw new WixException(WixErrors.FileNotFound(SourceLineNumberCollection.FromFileName(databaseFile), cabinetFile));
                        }
                    }
                }
            }
        }
    }
}
