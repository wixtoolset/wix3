//-------------------------------------------------------------------------------------------------
// <copyright file="SqlDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset SQL Server Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using Sql = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Sql;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset SQL Server Extension.
    /// </summary>
    public sealed class SqlDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "SqlDatabase":
                    this.DecompileSqlDatabaseTable(table);
                    break;
                case "SqlFileSpec":
                    // handled in FinalizeSqlFileSpecTable
                    break;
                case "SqlScript":
                    this.DecompileSqlScriptTable(table);
                    break;
                case "SqlString":
                    this.DecompileSqlStringTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void FinalizeDecompile(TableCollection tables)
        {
            this.FinalizeSqlFileSpecTable(tables);
            this.FinalizeSqlScriptAndSqlStringTables(tables);
        }

        /// <summary>
        /// Decompile the SqlDatabase table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSqlDatabaseTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Sql.SqlDatabase sqlDatabase = new Sql.SqlDatabase();

                sqlDatabase.Id = (string)row[0];

                if (null != row[1])
                {
                    sqlDatabase.Server = (string)row[1];
                }

                if (null != row[2])
                {
                    sqlDatabase.Instance = (string)row[2];
                }

                sqlDatabase.Database = (string)row[3];

                if (null != row[5])
                {
                    sqlDatabase.User = (string)row[5];
                }

                // the FileSpec_ and FileSpec_Log columns will be handled in FinalizeSqlFileSpecTable

                if (null != row[8])
                {
                    int attributes = (int)row[8];

                    if (SqlCompiler.DbCreateOnInstall == (attributes & SqlCompiler.DbCreateOnInstall))
                    {
                        sqlDatabase.CreateOnInstall = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbDropOnUninstall == (attributes & SqlCompiler.DbDropOnUninstall))
                    {
                        sqlDatabase.DropOnUninstall = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbContinueOnError == (attributes & SqlCompiler.DbContinueOnError))
                    {
                        sqlDatabase.ContinueOnError = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbDropOnInstall == (attributes & SqlCompiler.DbDropOnInstall))
                    {
                        sqlDatabase.DropOnInstall = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbCreateOnUninstall == (attributes & SqlCompiler.DbCreateOnUninstall))
                    {
                        sqlDatabase.CreateOnUninstall = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbConfirmOverwrite == (attributes & SqlCompiler.DbConfirmOverwrite))
                    {
                        sqlDatabase.ConfirmOverwrite = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbCreateOnReinstall == (attributes & SqlCompiler.DbCreateOnReinstall))
                    {
                        sqlDatabase.CreateOnReinstall = Sql.YesNoType.yes;
                    }

                    if (SqlCompiler.DbDropOnReinstall == (attributes & SqlCompiler.DbDropOnReinstall))
                    {
                        sqlDatabase.DropOnReinstall = Sql.YesNoType.yes;
                    }
                }

                if (null != row[4])
                {
                    Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[4]);

                    if (null != component)
                    {
                        component.AddChild(sqlDatabase);
                    }
                    else
                    {
                        this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[4], "Component"));
                    }
                }
                else
                {
                    this.Core.RootElement.AddChild(sqlDatabase);
                }
                this.Core.IndexElement(row, sqlDatabase);
            }
        }

        /// <summary>
        /// Decompile the SqlScript table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSqlScriptTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Sql.SqlScript sqlScript = new Sql.SqlScript();

                sqlScript.Id = (string)row[0];

                // the Db_ and Component_ columns are handled in FinalizeSqlScriptAndSqlStringTables

                sqlScript.BinaryKey = (string)row[3];

                if (null != row[4])
                {
                    sqlScript.User = (string)row[4];
                }

                int attributes = (int)row[5];

                if (SqlCompiler.SqlContinueOnError == (attributes & SqlCompiler.SqlContinueOnError))
                {
                    sqlScript.ContinueOnError = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnInstall == (attributes & SqlCompiler.SqlExecuteOnInstall))
                {
                    sqlScript.ExecuteOnInstall = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnReinstall == (attributes & SqlCompiler.SqlExecuteOnReinstall))
                {
                    sqlScript.ExecuteOnReinstall = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnUninstall == (attributes & SqlCompiler.SqlExecuteOnUninstall))
                {
                    sqlScript.ExecuteOnUninstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnInstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnInstall)))
                {
                    sqlScript.RollbackOnInstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnReinstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnReinstall)))
                {
                    sqlScript.RollbackOnReinstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnUninstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnUninstall)))
                {
                    sqlScript.RollbackOnUninstall = Sql.YesNoType.yes;
                }

                if (null != row[6])
                {
                    sqlScript.Sequence = (int)row[6];
                }

                this.Core.IndexElement(row, sqlScript);
            }
        }

        /// <summary>
        /// Decompile the SqlString table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSqlStringTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Sql.SqlString sqlString = new Sql.SqlString();

                sqlString.Id = (string)row[0];

                // the Db_ and Component_ columns are handled in FinalizeSqlScriptAndSqlStringTables

                sqlString.SQL = (string)row[3];

                if (null != row[4])
                {
                    sqlString.User = (string)row[4];
                }

                int attributes = (int)row[5];

                if (SqlCompiler.SqlContinueOnError == (attributes & SqlCompiler.SqlContinueOnError))
                {
                    sqlString.ContinueOnError = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnInstall == (attributes & SqlCompiler.SqlExecuteOnInstall))
                {
                    sqlString.ExecuteOnInstall = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnReinstall == (attributes & SqlCompiler.SqlExecuteOnReinstall))
                {
                    sqlString.ExecuteOnReinstall = Sql.YesNoType.yes;
                }

                if (SqlCompiler.SqlExecuteOnUninstall == (attributes & SqlCompiler.SqlExecuteOnUninstall))
                {
                    sqlString.ExecuteOnUninstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnInstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnInstall)))
                {
                    sqlString.RollbackOnInstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnReinstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnReinstall)))
                {
                    sqlString.RollbackOnReinstall = Sql.YesNoType.yes;
                }

                if ((SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnUninstall) == (attributes & (SqlCompiler.SqlRollback | SqlCompiler.SqlExecuteOnUninstall)))
                {
                    sqlString.RollbackOnUninstall = Sql.YesNoType.yes;
                }

                if (null != row[6])
                {
                    sqlString.Sequence = (int)row[6];
                }

                this.Core.IndexElement(row, sqlString);
            }
        }

        /// <summary>
        /// Finalize the SqlFileSpec table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// Since rows of the SqlFileSpec table are represented by either
        /// the SqlFileSpec or SqlLogFileSpec depending upon the context in
        /// which they are used in the SqlDatabase table, decompilation of this
        /// table must occur after the SqlDatbase parents are decompiled.
        /// </remarks>
        private void FinalizeSqlFileSpecTable(TableCollection tables)
        {
            Table sqlDatabaseTable = tables["SqlDatabase"];
            Table sqlFileSpecTable = tables["SqlFileSpec"];

            if (null != sqlDatabaseTable && null != sqlFileSpecTable)
            {
                Hashtable sqlFileSpecRows = new Hashtable();

                // index each SqlFileSpec row by its primary key
                foreach (Row row in sqlFileSpecTable.Rows)
                {
                    sqlFileSpecRows.Add(row[0], row);
                }

                // create the necessary SqlFileSpec and SqlLogFileSpec elements for each row
                foreach (Row row in sqlDatabaseTable.Rows)
                {
                    Sql.SqlDatabase sqlDatabase = (Sql.SqlDatabase)this.Core.GetIndexedElement(row);

                    if (null != row[6])
                    {
                        Row sqlFileSpecRow = (Row)sqlFileSpecRows[row[6]];

                        if (null != sqlFileSpecRow)
                        {
                            Sql.SqlFileSpec sqlFileSpec = new Sql.SqlFileSpec();

                            sqlFileSpec.Id = (string)sqlFileSpecRow[0];

                            if (null != sqlFileSpecRow[1])
                            {
                                sqlFileSpec.Name = (string)sqlFileSpecRow[1];
                            }

                            sqlFileSpec.Filename = (string)sqlFileSpecRow[2];

                            if (null != sqlFileSpecRow[3])
                            {
                                sqlFileSpec.Size = (string)sqlFileSpecRow[3];
                            }

                            if (null != sqlFileSpecRow[4])
                            {
                                sqlFileSpec.MaxSize = (string)sqlFileSpecRow[4];
                            }

                            if (null != sqlFileSpecRow[5])
                            {
                                sqlFileSpec.GrowthSize = (string)sqlFileSpecRow[5];
                            }

                            sqlDatabase.AddChild(sqlFileSpec);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, sqlDatabaseTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "FileSpec_", (string)row[6], "SqlFileSpec"));
                        }
                    }

                    if (null != row[7])
                    {
                        Row sqlFileSpecRow = (Row)sqlFileSpecRows[row[7]];

                        if (null != sqlFileSpecRow)
                        {
                            Sql.SqlLogFileSpec sqlLogFileSpec = new Sql.SqlLogFileSpec();

                            sqlLogFileSpec.Id = (string)sqlFileSpecRow[0];

                            if (null != sqlFileSpecRow[1])
                            {
                                sqlLogFileSpec.Name = (string)sqlFileSpecRow[1];
                            }

                            sqlLogFileSpec.Filename = (string)sqlFileSpecRow[2];

                            if (null != sqlFileSpecRow[3])
                            {
                                sqlLogFileSpec.Size = (string)sqlFileSpecRow[3];
                            }

                            if (null != sqlFileSpecRow[4])
                            {
                                sqlLogFileSpec.MaxSize = (string)sqlFileSpecRow[4];
                            }

                            if (null != sqlFileSpecRow[5])
                            {
                                sqlLogFileSpec.GrowthSize = (string)sqlFileSpecRow[5];
                            }

                            sqlDatabase.AddChild(sqlLogFileSpec);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, sqlDatabaseTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "FileSpec_Log", (string)row[7], "SqlFileSpec"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finalize the SqlScript table.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        /// <remarks>
        /// The SqlScript and SqlString tables contain a foreign key into the SqlDatabase
        /// and Component tables.  Depending upon the parent of the SqlDatabase
        /// element, the SqlScript and SqlString elements are nested under either the
        /// SqlDatabase or the Component element.
        /// </remarks>
        private void FinalizeSqlScriptAndSqlStringTables(TableCollection tables)
        {
            Table sqlDatabaseTable = tables["SqlDatabase"];
            Table sqlScriptTable = tables["SqlScript"];
            Table sqlStringTable = tables["SqlString"];

            Hashtable sqlDatabaseRows = new Hashtable();

            // index each SqlDatabase row by its primary key
            if (null != sqlDatabaseTable)
            {
                foreach (Row row in sqlDatabaseTable.Rows)
                {
                    sqlDatabaseRows.Add(row[0], row);
                }
            }

            if (null != sqlScriptTable)
            {
                foreach (Row row in sqlScriptTable.Rows)
                {
                    Sql.SqlScript sqlScript = (Sql.SqlScript)this.Core.GetIndexedElement(row);

                    Row sqlDatabaseRow = (Row)sqlDatabaseRows[row[1]];
                    string databaseComponent = (string)sqlDatabaseRow[4];

                    // determine if the SqlScript element should be nested under the database or another component
                    if (null != databaseComponent && databaseComponent == (string)row[2])
                    {
                        Sql.SqlDatabase sqlDatabase = (Sql.SqlDatabase)this.Core.GetIndexedElement(sqlDatabaseRow);

                        sqlDatabase.AddChild(sqlScript);
                    }
                    else // nest under the component of the SqlDatabase row
                    {
                        Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);

                        // set the Database value
                        sqlScript.SqlDb = (string)row[1];

                        if (null != component)
                        {
                            component.AddChild(sqlScript);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, sqlScriptTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                        }
                    }
                }
            }

            if (null != sqlStringTable)
            {
                foreach (Row row in sqlStringTable.Rows)
                {
                    Sql.SqlString sqlString = (Sql.SqlString)this.Core.GetIndexedElement(row);

                    Row sqlDatabaseRow = (Row)sqlDatabaseRows[row[1]];
                    string databaseComponent = (string)sqlDatabaseRow[4];

                    // determine if the SqlScript element should be nested under the database or another component
                    if (null != databaseComponent && databaseComponent == (string)row[2])
                    {
                        Sql.SqlDatabase sqlDatabase = (Sql.SqlDatabase)this.Core.GetIndexedElement(sqlDatabaseRow);

                        sqlDatabase.AddChild(sqlString);
                    }
                    else // nest under the component of the SqlDatabase row
                    {
                        Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[2]);

                        // set the Database value
                        sqlString.SqlDb = (string)row[1];

                        if (null != component)
                        {
                            component.AddChild(sqlString);
                        }
                        else
                        {
                            this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, sqlStringTable.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                        }
                    }
                }
            }
        }
    }
}
