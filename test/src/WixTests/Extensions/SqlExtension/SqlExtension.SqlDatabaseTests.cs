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
    /// Sql extension SqlDatabase element tests
    /// </summary>
    public class SqlDatabaseTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\SqlExtension\SqlDatabaseTests");
        private static readonly string SQLServerHostName = Environment.ExpandEnvironmentVariables("%SQLServerHostName%");
        private static readonly string SQLServerInstanceName = Environment.ExpandEnvironmentVariables("%SQLServerInstanceName%");

        [NamedFact]
        [Description("Verify that the (SqlDatabase, SqlFileSpec, SqlString, and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void SqlDatabase_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(SqlDatabaseTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("InstallSqlData", 1, "ScaSchedule2", "InstallSqlData"),
                new CustomActionTableData("UninstallSqlData", 1, "ScaSchedule2", "UninstallSqlData"),
                new CustomActionTableData("CreateDatabase", 25601, "ScaExecute2", "CreateDatabase"),
                new CustomActionTableData("DropDatabase", 25601, "ScaExecute2", "DropDatabase"),
                new CustomActionTableData("ExecuteSqlStrings", 25601, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackExecuteSqlStrings", 25857, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackCreateDatabase", 25857, "ScaExecute2", "DropDatabase"));

            // Verify SqlDatabase table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.SqlDatabase,
                new TableRow(SqlDatabaseColumns.SqlDb.ToString(), "TestDB1"),
                new TableRow(SqlDatabaseColumns.Server.ToString(), SqlDatabaseTests.SQLServerHostName),
                new TableRow(SqlDatabaseColumns.Instance.ToString(), SqlDatabaseTests.SQLServerInstanceName),
                new TableRow(SqlDatabaseColumns.Database.ToString(), "BlankDB"),
                new TableRow(SqlDatabaseColumns.Component_.ToString(), "TestSqlScriptProductComponent1"),
                new TableRow(SqlDatabaseColumns.User_.ToString(), string.Empty),
                new TableRow(SqlDatabaseColumns.FileSpec_.ToString(), "TestFileSpec"),
                new TableRow(SqlDatabaseColumns.FileSpec_Log.ToString(), "TestLogFileSpec"),
                new TableRow(SqlDatabaseColumns.Attributes.ToString(), "39", false)
                );

            // Verify SqlFileSpec table contains the right data");
            Verifier.VerifyTableData(msiFile, MSITables.SqlFileSpec,
                new TableRow(SqlFileSpecColumns.FileSpec.ToString(), "TestFileSpec"),
                new TableRow(SqlFileSpecColumns.Name.ToString(), "foo"),
                new TableRow(SqlFileSpecColumns.Filename.ToString(), "DBFILE.mdf"),
                new TableRow(SqlFileSpecColumns.Size.ToString(), "[DBFILESIZE]"),
                new TableRow(SqlFileSpecColumns.MaxSize.ToString(), "[DBFILEMAXSIZE]"),
                new TableRow(SqlFileSpecColumns.GrowthSize.ToString(), "[DBFILEGROWTHSIZE]")
                );

            // Verify SqlFileSpec table contains the right data");
            Verifier.VerifyTableData(msiFile, MSITables.SqlString,
                new TableRow(SqlStringColumns.String.ToString(), "TestString1"),
                new TableRow(SqlStringColumns.SqlDb_.ToString(), "TestDB1"),
                new TableRow(SqlStringColumns.Component_.ToString(), "TestSqlScriptProductComponent1"),
                new TableRow(SqlStringColumns.SQL.ToString(), "CREATE TABLE TestTable1(name varchar(20), value varchar(20))"),
                new TableRow(SqlStringColumns.User_.ToString(), string.Empty),
                new TableRow(SqlStringColumns.Attributes.ToString(), "1", false),
                new TableRow(SqlStringColumns.Sequence.ToString(), string.Empty, false)
                );
        }
       
        [NamedFact]
        [Description("Verify that the database was installed correctelly.")]
        [Priority(2)]
        [RuntimeTest]
        public void SqlDatabase_Install()
        {
            string sourceFile = Path.Combine(SqlDatabaseTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "BlankDB"), String.Format("Database '{0}' was not created on Install", "BlankDB"));
            Assert.True(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "Blank[Db11"), String.Format("Database '{0}' was not created on Install", "Blank[Db11"));
         
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.False(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "BlankDB"), String.Format("Database '{0}' was not dropped on Uninstall", "BlankDB"));
            Assert.False(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "Blank[Db11"), String.Format("Database '{0}' was not dropped on Uninstall", "Blank[Db11"));
        }

        [NamedFact]
        [Description("Verify that the databases created were dropped on rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void SqlDatabase_InstallFailure()
        {
            string sourceFile = Path.Combine(SqlDatabaseTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            Assert.False(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "BlankDB"), String.Format("Database '{0}' was not dropped on Rollback", "BlankDB"));
            Assert.False(SqlVerifier.DatabaseExists(SqlDatabaseTests.SQLServerHostName, SqlDatabaseTests.SQLServerInstanceName, "Blank[Db11"), String.Format("Database '{0}' was not dropped on Rollback", "Blank[Db11"));
        }

        [NamedFact]
        [Description("Verify that the expected error message is shown if the SqlDatabase element does not have a matching parent component element.")]
        [Priority(3)]
        public void SqlDatabase_MissingParentComponent()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlDatabaseTests.TestDataDirectory, @"MissingParentComponent.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(5100, @"The sql:SqlDatabase/@CreateOnInstall attribute cannot be specified unless the element has a Component as an ancestor. A sql:SqlDatabase that does not have a Component ancestor is not installed.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5100;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that the expected error message is shown if the SqlDatabase element does not define any of the CreateOnInstall, CreateOnUninstall, DropOnInstall or DropOnUninstall attributes")]
        [Priority(3)]
        public void SqlDatabase_MissingAttributes()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlDatabaseTests.TestDataDirectory, @"MissingAttributes.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(5102, @"When nested under a Component, the sql:SqlDatabase element must have one of the following attributes specified: CreateOnInstall, CreateOnUninstall, DropOnInstall or DropOnUninstall.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5102;
            candle.Run();
        }
    }
}
