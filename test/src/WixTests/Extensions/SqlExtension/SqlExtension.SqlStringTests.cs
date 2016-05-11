// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.SqlExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// Sql extension SqlString element tests
    /// </summary>
    public class SqlStringTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\SqlExtension\SqlStringTests");
        private static readonly string SQLServerHostName = Environment.ExpandEnvironmentVariables("%SQLServerHostName%");
        private static readonly string SQLServerInstanceName = Environment.ExpandEnvironmentVariables("%SQLServerInstanceName%");

        [NamedFact]
        [Description("Verify that the (SqlString and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void SqlString_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(SqlStringTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("InstallSqlData", 1, "ScaSchedule2", "InstallSqlData"),
                new CustomActionTableData("UninstallSqlData", 1, "ScaSchedule2", "UninstallSqlData"),
                new CustomActionTableData("CreateDatabase", 25601, "ScaExecute2", "CreateDatabase"),
                new CustomActionTableData("DropDatabase", 25601, "ScaExecute2", "DropDatabase"),
                new CustomActionTableData("ExecuteSqlStrings", 25601, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackExecuteSqlStrings", 25857, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackCreateDatabase", 25857, "ScaExecute2", "DropDatabase"));

            // Verify SqlString table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.SqlString,
                new TableRow(SqlStringColumns.String.ToString(), "TestString1"),
                new TableRow(SqlStringColumns.SqlDb_.ToString(), "TestDB1"),
                new TableRow(SqlStringColumns.Component_.ToString(), "TestSqlStringProductComponent1"),
                new TableRow(SqlStringColumns.SQL.ToString(), "CREATE TABLE TestTable1(name varchar(20), value varchar(20))"),
                new TableRow(SqlStringColumns.User_.ToString(), string.Empty),
                new TableRow(SqlStringColumns.Attributes.ToString(), "5", false),
                new TableRow(SqlStringColumns.Sequence.ToString(), "2", false)
                );

            Verifier.VerifyTableData(msiFile, MSITables.SqlString,
                new TableRow(SqlStringColumns.String.ToString(), "TestString2"),
                new TableRow(SqlStringColumns.SqlDb_.ToString(), "TestDB1"),
                new TableRow(SqlStringColumns.Component_.ToString(), "TestSqlStringProductComponent2"),
                new TableRow(SqlStringColumns.SQL.ToString(), "CREATE TABLE TestTable2(name varchar(20), value varchar(20))"),
                new TableRow(SqlStringColumns.User_.ToString(), string.Empty),
                new TableRow(SqlStringColumns.Attributes.ToString(), "1", false),
                new TableRow(SqlStringColumns.Sequence.ToString(), string.Empty, false)
                );

            Verifier.VerifyTableData(msiFile, MSITables.SqlString,
                new TableRow(SqlStringColumns.String.ToString(), "TestString4"),
                new TableRow(SqlStringColumns.SqlDb_.ToString(), "TestDB3"),
                new TableRow(SqlStringColumns.Component_.ToString(), "TestSqlStringProductComponent4"),
                new TableRow(SqlStringColumns.SQL.ToString(), "CREATE TABLE TestTable1(name varchar(20), value varchar(20))"),
                new TableRow(SqlStringColumns.User_.ToString(), string.Empty),
                new TableRow(SqlStringColumns.Attributes.ToString(), "1", false),
                new TableRow(SqlStringColumns.Sequence.ToString(), string.Empty, false)
                );
        }
       
        [NamedFact]
        [Description("Verify that the msi installs and the database was created.")]
        [Priority(2)]
        [RuntimeTest]
        public void SqlString_Install()
        {
            string sourceFile = Path.Combine(SqlStringTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB10"), String.Format("Database '{0}' was not created on Install", "BlankDB10"));
            Assert.True(SqlVerifier.TableExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "ScottDB22", "TestTable1"), String.Format("Table '{0}:{1}' was not created on Install", "ScottDB22", "TestTable1"));
            Assert.True(SqlVerifier.TableExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB10", "TestTable2"), String.Format("Table '{0}:{1}' was not created on Install", "BlankDB10", "TestTable2"));
            string sqlQuery = "Select * from TestTable2 where name ='FIRST' and value ='Kurtzeborn'";
            Assert.True(SqlVerifier.SqlObjectExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB10", sqlQuery), String.Format("Query '{0}' Results were not created on Install", sqlQuery));


            // Verify The Database BlankDB4 was not created
            Assert.False(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB44"), String.Format("Database '{0}' was created on Install", "BlankDB44"));

            MSIExec.RepairProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify The Database was added
            Assert.True(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB44"), String.Format("Database '{0}' was Not created on Repair", "BlankDB44"));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB10"), String.Format("Database '{0}' was dropped on Uninstall", "BlankDB10"));

            Assert.False(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB44"), String.Format("Database '{0}' was not dropped on Uninstall", "BlankDB44"));

            SqlStringTests.DropDatabase("BlankDB10");
        }

        [NamedFact]
        [Description("Verify that the new sqlstring was rolledback correctelly on install failure.")]
        [Priority(2)]
        [RuntimeTest]
        public void SqlString_InstallFailure()
        {
            string sourceFile = Path.Combine(SqlStringTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            Assert.False(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB110"), String.Format("Database '{0}' was Not dropped on Rollback", "BlankDB110"));
        }

        [NamedFact]
        [Description("Verify that the OnReinstall actions are executed correctelly.")]
        [Priority(2)]
        [RuntimeTest]
        public void SqlString_ReInstall()
        {
            string sourceFile = Path.Combine(SqlStringTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(SqlVerifier.DatabaseExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB15"), String.Format("Database '{0}' was not created on Install", "BlankDB15"));
            Assert.True(SqlVerifier.TableExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB15", "TestTable1"), String.Format("Table '{0}:{1}' was not created on Install", "BlankDB15", "TestTable1"));
            
            // insert a record in the new database
            string sqlInsertString = "INSERT INTO TestTable1(name, value) Values('test', 'nooverwrite')";
            SqlVerifier.ExecuteSQlCommand(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB15", sqlInsertString);

            string sqlqueryString = "Select * from TestTable1 where name ='test' and value ='nooverwrite'";
            Assert.True(SqlVerifier.SqlObjectExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB15", sqlqueryString), String.Format("Query '{0}' Results were not created on Install", sqlqueryString));

            MSIExec.RepairProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // verify the change is still in the database
            Assert.True(SqlVerifier.SqlObjectExists(SqlStringTests.SQLServerHostName, SqlStringTests.SQLServerInstanceName, "BlankDB15", sqlqueryString), String.Format("Query '{0}' Results were not created on Install", sqlqueryString));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            SqlStringTests.DropDatabase("BlankDB10");
        }

        [NamedFact]
        [Description("Verify that the correct error message is displayed when the rollback attributes are not defined correctelly.")]
        [Priority(3)]
        public void SqlString_InvaildAttribute()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlStringTests.TestDataDirectory, @"InvaildAttribute.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlString/@RollbackOnInstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlString/@RollbackOnReinstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlString/@RollbackOnUninstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error)); 
            candle.ExpectedExitCode = 36;
            candle.Run();
        }
        
        private static void DropDatabase(string databaseName)
        {
            string sqlString = "drop database " + databaseName + "";
            SqlVerifier.ExecuteSQlCommand(SqlStringTests.SQLServerHostName,SqlStringTests.SQLServerInstanceName,"master", sqlString);
        }
    }
}
