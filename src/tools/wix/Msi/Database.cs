// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Enum of predefined persist modes used when opening a database.
    /// </summary>
    public enum OpenDatabase
    {
        /// <summary>
        /// Open a database read-only, no persistent changes.
        /// </summary>
        ReadOnly = MsiInterop.MSIDBOPENREADONLY,

        /// <summary>
        /// Open a database read/write in transaction mode.
        /// </summary>
        Transact = MsiInterop.MSIDBOPENTRANSACT,

        /// <summary>
        /// Open a database direct read/write without transaction.
        /// </summary>
        Direct = MsiInterop.MSIDBOPENDIRECT,

        /// <summary>
        /// Create a new database, transact mode read/write.
        /// </summary>
        Create = MsiInterop.MSIDBOPENCREATE,

        /// <summary>
        /// Create a new database, direct mode read/write.
        /// </summary>
        CreateDirect = MsiInterop.MSIDBOPENCREATEDIRECT,

        /// <summary>
        /// Indicates a patch file is being opened.
        /// </summary>
        OpenPatchFile = MsiInterop.MSIDBOPENPATCHFILE
    }

    /// <summary>
    /// The errors to suppress when applying a transform.
    /// </summary>
    [Flags]
    public enum TransformErrorConditions
    {
        /// <summary>
        /// None of the following conditions.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Suppress error when adding a row that exists.
        /// </summary>
        AddExistingRow = 0x1,

        /// <summary>
        /// Suppress error when deleting a row that does not exist.
        /// </summary>
        DeleteMissingRow = 0x2,

        /// <summary>
        /// Suppress error when adding a table that exists.
        /// </summary>
        AddExistingTable = 0x4,

        /// <summary>
        /// Suppress error when deleting a table that does not exist.
        /// </summary>
        DeleteMissingTable = 0x8,

        /// <summary>
        /// Suppress error when updating a row that does not exist.
        /// </summary>
        UpdateMissingRow = 0x10,

        /// <summary>
        /// Suppress error when transform and database code pages do not match, and their code pages are neutral.
        /// </summary>
        ChangeCodepage = 0x20,

        /// <summary>
        /// Create the temporary _TransformView table when applying a transform.
        /// </summary>
        ViewTransform = 0x100,

        /// <summary>
        /// Suppress all errors but the option to create the temporary _TransformView table.
        /// </summary>
        All = 0x3F
    }

    /// <summary>
    /// The validation to run while applying a transform.
    /// </summary>
    [Flags]
    public enum TransformValidations
    {
        /// <summary>
        /// Do not validate properties.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Default language must match base database.
        /// </summary>
        Language = 0x1,

        /// <summary>
        /// Product must match base database.
        /// </summary>
        Product = 0x2,

        /// <summary>
        /// Check major version only.
        /// </summary>
        MajorVersion = 0x8,

        /// <summary>
        /// Check major and minor versions only.
        /// </summary>
        MinorVersion = 0x10,

        /// <summary>
        /// Check major, minor, and update versions.
        /// </summary>
        UpdateVersion = 0x20,

        /// <summary>
        /// Installed version &lt; base version.
        /// </summary>
        NewLessBaseVersion = 0x40,

        /// <summary>
        /// Installed version &lt;= base version.
        /// </summary>
        NewLessEqualBaseVersion = 0x80,

        /// <summary>
        /// Installed version = base version.
        /// </summary>
        NewEqualBaseVersion = 0x100,

        /// <summary>
        /// Installed version &gt;= base version.
        /// </summary>
        NewGreaterEqualBaseVersion = 0x200,

        /// <summary>
        /// Installed version &gt; base version.
        /// </summary>
        NewGreaterBaseVersion = 0x400,

        /// <summary>
        /// UpgradeCode must match base database.
        /// </summary>
        UpgradeCode = 0x800
    }

    /// <summary>
    /// Wrapper class for managing MSI API database handles.
    /// </summary>
    public sealed class Database : MsiHandle
    {
        private const int STG_E_LOCKVIOLATION = unchecked((int)0x80030021);

        /// <summary>
        /// Constructor that opens an MSI database.
        /// </summary>
        /// <param name="path">Path to the database to be opened.</param>
        /// <param name="type">Persist mode to use when opening the database.</param>
        public Database(string path, OpenDatabase type)
        {
            uint handle = 0;
            int error = MsiInterop.MsiOpenDatabase(path, new IntPtr((int)type), out handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
            this.Handle = handle;
        }

        public void ApplyTransform(string transformFile)
        {
            // get the curret validation bits
            TransformErrorConditions conditions = TransformErrorConditions.None;
            using (SummaryInformation summaryInfo = new SummaryInformation(transformFile))
            {
                string value = summaryInfo.GetProperty((int)SummaryInformation.Transform.ValidationFlags);
                try
                {
                    int validationFlags = Int32.Parse(value, CultureInfo.InvariantCulture);
                    conditions = (TransformErrorConditions)(validationFlags & 0xffff);
                }
                catch (FormatException)
                {
                    // fallback to default of None
                }
            }

            this.ApplyTransform(transformFile, conditions);
        }

        /// <summary>
        /// Applies a transform to this database.
        /// </summary>
        /// <param name="transformFile">Path to the transform file being applied.</param>
        /// <param name="errorConditions">Specifies the error conditions that are to be suppressed.</param>
        public void ApplyTransform(string transformFile, TransformErrorConditions errorConditions)
        {
            int error = MsiInterop.MsiDatabaseApplyTransform(this.Handle, transformFile, errorConditions);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Commits changes made to the database.
        /// </summary>
        public void Commit()
        {
            // Retry this call 3 times to deal with an MSI internal locking problem.
            const int retryWait = 300;
            const int retryLimit = 3;
            int error = 0;

            for (int i = 1; i <= retryLimit; ++i)
            {
                error = MsiInterop.MsiDatabaseCommit(this.Handle);

                if (0 == error)
                {
                    return;
                }
                else
                {
                    MsiException exception = new MsiException(error);

                    // We need to see if the error code is contained in any of the strings in ErrorInfo.
                    // Join the array together and search for the error code to cover the string array.
                    if (!String.Join(", ", exception.ErrorInfo).Contains(STG_E_LOCKVIOLATION.ToString()))
                    {
                        break;
                    }

                    Console.WriteLine(String.Format("Failed to create the database. Info: {0}. Retrying ({1} of {2})", String.Join(", ", exception.ErrorInfo), i, retryLimit));
                    Thread.Sleep(retryWait);
                }
            }

            throw new MsiException(error);
        }

        /// <summary>
        /// Creates and populates the summary information stream of an existing transform file.
        /// </summary>
        /// <param name="referenceDatabase">Required database that does not include the changes.</param>
        /// <param name="transformFile">The name of the generated transform file.</param>
        /// <param name="errorConditions">Required error conditions that should be suppressed when the transform is applied.</param>
        /// <param name="validations">Required when the transform is applied to a database;
        /// shows which properties should be validated to verify that this transform can be applied to the database.</param>
        public void CreateTransformSummaryInfo(Database referenceDatabase, string transformFile, TransformErrorConditions errorConditions, TransformValidations validations)
        {
            int error = MsiInterop.MsiCreateTransformSummaryInfo(this.Handle, referenceDatabase.Handle, transformFile, errorConditions, validations);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Imports an installer text archive table (idt file) into an open database.
        /// </summary>
        /// <param name="folderPath">Specifies the path to the folder containing archive files.</param>
        /// <param name="fileName">Specifies the name of the file to import.</param>
        /// <exception cref="WixInvalidIdtException">Attempted to import an IDT file with an invalid format or unsupported data.</exception>
        /// <exception cref="MsiException">Another error occured while importing the IDT file.</exception>
        public void Import(string folderPath, string fileName)
        {
            int error = MsiInterop.MsiDatabaseImport(this.Handle, folderPath, fileName);
            if (1627 == error) // ERROR_FUNCTION_FAILED
            {
                string path = Path.Combine(folderPath, fileName);
                throw new WixInvalidIdtException(path);
            }
            else if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Exports an installer table from an open database to a text archive file (idt file).
        /// </summary>
        /// <param name="tableName">Specifies the name of the table to export.</param>
        /// <param name="folderPath">Specifies the name of the folder that contains archive files. If null or empty string, uses current directory.</param>
        /// <param name="fileName">Specifies the name of the exported table archive file.</param>
        public void Export(string tableName, string folderPath, string fileName)
        {
            if (null == folderPath || 0 == folderPath.Length)
            {
                folderPath = System.Environment.CurrentDirectory;
            }

            int error = MsiInterop.MsiDatabaseExport(this.Handle, tableName, folderPath, fileName);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Creates a transform that, when applied to the reference database, results in this database.
        /// </summary>
        /// <param name="referenceDatabase">Required database that does not include the changes.</param>
        /// <param name="transformFile">The name of the generated transform file. This is optional.</param>
        /// <returns>true if a transform is generated; false if a transform is not generated because
        /// there are no differences between the two databases.</returns>
        public bool GenerateTransform(Database referenceDatabase, string transformFile)
        {
            int error = MsiInterop.MsiDatabaseGenerateTransform(this.Handle, referenceDatabase.Handle, transformFile, 0, 0);
            if (0 != error && 0xE8 != error) // ERROR_NO_DATA(0xE8) means no differences were found
            {
                throw new MsiException(error);
            }

            return (0xE8 != error);
        }

        /// <summary>
        /// Merges two databases together.
        /// </summary>
        /// <param name="mergeDatabase">The database to merge into the base database.</param>
        /// <param name="tableName">The name of the table to receive merge conflict information.</param>
        public void Merge(Database mergeDatabase, string tableName)
        {
            int error = MsiInterop.MsiDatabaseMerge(this.Handle, mergeDatabase.Handle, tableName);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Prepares a database query and creates a <see cref="View">View</see> object.
        /// </summary>
        /// <param name="query">Specifies a SQL query string for querying the database.</param>
        /// <returns>A view object is returned if the query was successful.</returns>
        public View OpenView(string query)
        {
            return new View(this, query);
        }

        /// <summary>
        /// Prepares and executes a database query and creates a <see cref="View">View</see> object.
        /// </summary>
        /// <param name="query">Specifies a SQL query string for querying the database.</param>
        /// <returns>A view object is returned if the query was successful.</returns>
        public View OpenExecuteView(string query)
        {
            View view = new View(this, query);

            view.Execute();
            return view;
        }

        /// <summary>
        /// Verifies the existence or absence of a table.
        /// </summary>
        /// <param name="tableName">Table name to to verify the existence of.</param>
        /// <returns>Returns true if the table exists, false if it does not.</returns>
        public bool TableExists(string tableName)
        {
            int result = MsiInterop.MsiDatabaseIsTablePersistent(this.Handle, tableName);
            return MsiInterop.MSICONDITIONTRUE == result;
        }

        /// <summary>
        /// Returns a <see cref="Record">Record</see> containing the names of all the primary 
        /// key columns for a specified table.
        /// </summary>
        /// <param name="tableName">Specifies the name of the table from which to obtain 
        /// primary key names.</param>
        /// <returns>Returns a <see cref="Record">Record</see> containing the names of all the 
        /// primary key columns for a specified table.</returns>
        public Record PrimaryKeys(string tableName)
        {
            uint recordHandle;
            int error = MsiInterop.MsiDatabaseGetPrimaryKeys(this.Handle, tableName, out recordHandle);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            return new Record(recordHandle);
        }

        /// <summary>
        /// Imports a table into the database.
        /// </summary>
        /// <param name="codepage">Codepage of the database to import table to.</param>
        /// <param name="messageHandler">Message handler.</param>
        /// <param name="table">Table to import into database.</param>
        /// <param name="baseDirectory">The base directory where intermediate files are created.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        public void ImportTable(int codepage, IMessageHandler messageHandler, Table table, string baseDirectory, bool keepAddedColumns)
        {
            // write out the table to an IDT file
            string idtPath = Path.Combine(baseDirectory, String.Concat(table.Name, ".idt"));
            StreamWriter idtWriter = null;

            try
            {
                Encoding encoding;

                // If UTF8 encoding, use the UTF8-specific constructor to avoid writing
                // the byte order mark at the beginning of the file
                if (Encoding.UTF8.CodePage == codepage)
                {
                    encoding = new UTF8Encoding(false, true);
                }
                else
                {
                    if (0 == codepage)
                    {
                        codepage = Encoding.ASCII.CodePage;
                    }

                    encoding = Encoding.GetEncoding(codepage, new EncoderExceptionFallback(), new DecoderExceptionFallback());
                }

                idtWriter = new StreamWriter(idtPath, false, encoding);

                table.ToIdtDefinition(idtWriter, messageHandler, keepAddedColumns);
            }
            finally
            {
                if (null != idtWriter)
                {
                    idtWriter.Close();
                }
            }

            // try to import the table into the MSI
            try
            {
                this.Import(Path.GetDirectoryName(idtPath), Path.GetFileName(idtPath));
            }
            catch (WixInvalidIdtException)
            {
                table.ValidateRows();

                // if ValidateRows finds anything it doesn't like, it throws
                // otherwise throw WixInvalidIdtException (which is caught in light and turns tidy mode off)
                throw new WixInvalidIdtException(idtPath, table.Name);
            }
        }
     }
}
