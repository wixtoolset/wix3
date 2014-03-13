//-----------------------------------------------------------------------
// <copyright file="SqlExtension.SqlScriptTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Sql Extension SqlScript tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.SqlExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;

    /// <summary>
    /// Sql extension SqlScript element tests
    /// </summary>
    [TestClass]
    public class SqlScriptTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\SqlExtension\SqlScriptTests");
        private static readonly string SQLServerHostName = Environment.ExpandEnvironmentVariables("%SQLServerHostName%");
        private static readonly string SQLServerInstanceName = Environment.ExpandEnvironmentVariables("%SQLServerInstanceName%");

        [TestMethod]
        [Description("Verify that the (SqlScript and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void SqlScript_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(SqlScriptTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("InstallSqlData", 1, "ScaSchedule2", "InstallSqlData"),
                new CustomActionTableData("UninstallSqlData", 1, "ScaSchedule2", "UninstallSqlData"),
                new CustomActionTableData("CreateDatabase", 25601, "ScaExecute2", "CreateDatabase"),
                new CustomActionTableData("DropDatabase", 25601, "ScaExecute2", "DropDatabase"),
                new CustomActionTableData("ExecuteSqlStrings", 25601, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackExecuteSqlStrings", 25857, "ScaExecute2", "ExecuteSqlStrings"),
                new CustomActionTableData("RollbackCreateDatabase", 25857, "ScaExecute2", "DropDatabase"));


            // Verify SqlScript table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.SqlScript,
                new TableRow(SqlScriptColumns.Script.ToString(), "SqlScript1"),
                new TableRow(SqlScriptColumns.SqlDb_.ToString(), "TestDB"),
                new TableRow(SqlScriptColumns.Component_.ToString(), "TestSqlScriptProductComponent1"),
                new TableRow(SqlScriptColumns.ScriptBinary_.ToString(), "SqlScript1"),
                new TableRow(SqlScriptColumns.User_.ToString(), string.Empty),
                new TableRow(SqlScriptColumns.Attributes.ToString(), "17", false),
                new TableRow(SqlScriptColumns.Sequence.ToString(), "1", false));
        }

        [TestMethod]
        [Description("Verify that the msi installs and the database was created.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void SqlScript_Install()
        {
            string sourceFile = Path.Combine(SqlScriptTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(SqlVerifier.DatabaseExists(SqlScriptTests.SQLServerHostName, SqlScriptTests.SQLServerInstanceName, "BlankDB12"), "Database '{0}' was not created on Install", "BlankDB12");
            Assert.IsTrue(SqlVerifier.TableExists(SqlScriptTests.SQLServerHostName, SqlScriptTests.SQLServerInstanceName, "BlankDB12", "TestTable2"), "Table '{0}:{1}' was not created on Install", "BlankDB12", "TestTable2");

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsFalse(SqlVerifier.DatabaseExists(SqlScriptTests.SQLServerHostName, SqlScriptTests.SQLServerInstanceName, "BlankDB12"), "Database '{0}' was not dropped on Uninstall", "BlankDB12");
        }

        [TestMethod]
        [Description("Verify that the Rollback actions are executed correctelly when install fails.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void SqlScript_InstallFailure()
        {
            string sourceFile = Path.Combine(SqlScriptTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            Assert.IsFalse(SqlVerifier.DatabaseExists(SqlScriptTests.SQLServerHostName, SqlScriptTests.SQLServerInstanceName, "BlankDB12"), "Database '{0}' was not dropped on Rollback", "BlankDB12");
        }

        [TestMethod]
        [Description("Verify that the installtion fails, and created objects are droped if the SqlScript is invalid.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void SqlScript_InvalidSqlScript()
        {
            string sourceFile = Path.Combine(SqlScriptTests.TestDataDirectory, @"InvalidSqlScript.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixSqlExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            Assert.IsFalse(SqlVerifier.DatabaseExists(SqlScriptTests.SQLServerHostName, SqlScriptTests.SQLServerInstanceName, "BlankDB12"), "Database '{0}' was not dropped on Rollback", "BlankDB12");
        }

        [TestMethod]
        [Description("Verify that the correct error message is displayed when the wrong attributes are defined.")]
        [Priority(3)]
        public void SqlScript_InvaildAttribute()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlScriptTests.TestDataDirectory, @"InvaildAttribute.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlScript/@RollbackOnInstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlScript/@RollbackOnReinstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(36, @"The sql:SqlScript/@RollbackOnUninstall attribute cannot be specified when attribute ExecuteOnInstall, ExecuteOnReinstall, or ExecuteOnUninstall is also present.", WixMessage.MessageTypeEnum.Error)); 
            candle.ExpectedExitCode = 36;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the expected error message is shown if the SqlScript element does not have a matching parent component element.")]
        [Priority(3)]
        public void SqlScript_MissingParentComponent()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlScriptTests.TestDataDirectory, @"MissingParentComponent.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(5101, @"The sql:SqlScript element cannot be specified unless the element has a Component as an ancestor. A sql:SqlScript that does not have a Component ancestor is not installed.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5101;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the expected error message is shown if the SqlScript element defined the deprecated child element Binary.")]
        [Priority(3)]
        public void SqlScript_DeprecatedBinaryElement()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SqlScriptTests.TestDataDirectory, @"DeprecatedBinaryElement.wxs"));
            candle.Extensions.Add("WixSqlExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(5103, @"The sql:SqlScript element contains a deprecated child Binary element.  Please move the Binary element under a Fragment, Module, or Product element and set the sql:SqlScript/@BinaryKey attribute to the value of the Binary/@Id attribute.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5103;
            candle.Run();
        }
    }
}
