//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISFilterTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension IISFilter tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers.Extensions;

    /// <summary>
    /// IIS extension IISFilter element tests
    /// </summary>
    [TestClass]
    public class IISFilterTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISFilterTests");

        [TestMethod]
        [Description("Verify that the (IIsFilter,CustomAction) Tables are created in the MSI and have defined data")]
        [Priority(1)]
        public void IISFilter_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISFilterTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsFilter,
                new TableRow(IISFilterColumns.Filter.ToString(), "TestFilter1"),
                new TableRow(IISFilterColumns.Name.ToString(), "Test Filter"),
                new TableRow(IISFilterColumns.Component_.ToString(), "TestWebFilterProductComponent"),
                new TableRow(IISFilterColumns.Path.ToString(), "[#FILEID1]"),
                new TableRow(IISFilterColumns.Web_.ToString(), "Test"),
                new TableRow(IISFilterColumns.Description.ToString(), string.Empty),
                new TableRow(IISFilterColumns.Flags.ToString(), "0", false),
                new TableRow(IISFilterColumns.LoadOrder.ToString(), "-1", false));

            Verifier.VerifyTableData(msiFile, MSITables.IIsFilter,
                new TableRow(IISFilterColumns.Filter.ToString(), "TestGlobalFilter"),
                new TableRow(IISFilterColumns.Name.ToString(), "Global Filter"),
                new TableRow(IISFilterColumns.Component_.ToString(), "TestWebFilterProductComponent2"),
                new TableRow(IISFilterColumns.Path.ToString(), "[#FILEID1]"),
                new TableRow(IISFilterColumns.Web_.ToString(), string.Empty),
                new TableRow(IISFilterColumns.Description.ToString(), string.Empty),
                new TableRow(IISFilterColumns.Flags.ToString(), "0", false),
                new TableRow(IISFilterColumns.LoadOrder.ToString(), "-1", false));
        }

        [TestMethod]
        [Description("Install the MSI.Verify that “TestFilter” Was added for website 'Test'.Verify that 'Global Filter' was added as a global filter.Uninstall the MSi Verify that filters are removed")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISFilter_Install()
        {
            string sourceFile = Path.Combine(IISFilterTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebFilters were created
            Assert.IsTrue(IISVerifier.FilterExists("Test Filter", "Test web server", false), "Filter '{0}' in site '{1}' was not created on Install", "Test Filter", "Test web server");
            Assert.IsTrue(IISVerifier.FilterExists("Global Filter", string.Empty, true), "Global Filter '{0}' was not created on Install", "Global Filter");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebFilters were removed
            Assert.IsFalse(IISVerifier.FilterExists("Test Filter", "Test web server", false), "Filter '{0}' in site '{1}' was not removed on Uninstall", "Test Filter", "Test web server");
            Assert.IsFalse(IISVerifier.FilterExists("Global Filter", string.Empty, true), "Global Filter '{0}' was not removed on Uninstall", "Global Filter"); 
        }
    }
}
