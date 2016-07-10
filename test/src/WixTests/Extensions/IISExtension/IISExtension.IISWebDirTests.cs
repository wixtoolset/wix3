// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// IIS extension IISWebDir element tests
    /// </summary>
    public class IISWebDirTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebDirTests");

        [NamedFact]
        [Description("Verify that the (IIsWebDir,CustomAction) Tables are created in the MSI and have expected data")]
        [Priority(1)]
        public void IISWebDir_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebDirTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));

            Verifier.VerifyTableData(msiFile, MSITables.IIsWebDir,
                new TableRow(IISWebDirColumns.WebDir.ToString(), "testwebdir"),
                new TableRow(IISWebDirColumns.Component_.ToString(), "TestWebSiteProductComponent"),
                new TableRow(IISWebDirColumns.Web_.ToString(), "Test"),
                new TableRow(IISWebDirColumns.Path.ToString(), "webdir"),
                new TableRow(IISWebDirColumns.DirProperties_.ToString(), "ReadAndExecute"),
                new TableRow(IISWebDirColumns.Application_.ToString(), string.Empty)
                );
        }

        [NamedFact]
        [Description("Install the MSI. Verify that the web directory was created.Uninstall MSI . Verify that the web directory was removed ")]
        [Priority(2)]
        [RuntimeTest]
        [Trait("OSquery", "124674")]     //Vista and Above 
        public void IISWebDir_Install()
        {
            string sourceFile = Path.Combine(IISWebDirTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was created
            Assert.True(IISVerifier.WebDirExist("webdir", "Test web server"), String.Format("WebDir '{0}' in site '{1}' was not created on Install", "webdir", "Test web server"));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            Assert.False(IISVerifier.WebDirExist("webdir", "Test web server"), String.Format("WebDir '{0}' in site '{1}' was not removed on Uninstall", "webdir", "Test web server"));
        }

        [NamedFact]
        [Description("Install the MSI to a 64-bit specific loaction. Verify that the web directory was created. Uninstall MSI . Verify that the web directory was removed ")]
        [Priority(2)]
        [RuntimeTest]
        [Is64BitSpecificTest]
        public void IISWebDir_Install_64bit()
        {
            string sourceFile = Path.Combine(IISWebDirTests.TestDataDirectory, @"product_64.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was created
            Assert.True(IISVerifier.WebDirExist("webdir", "Test web server"), String.Format("WebDir '{0}' in site '{1}' was not created on Install", "webdir", "Test web server"));

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify WebDir was removed
            Assert.False(IISVerifier.WebDirExist("webdir", "Test web server"), String.Format("WebDir '{0}' in site '{1}' was not removed on Uninstall", "webdir", "Test web server"));
        }

        [NamedFact]
        [Description("Cancel install of  MSI. Verify that the web directory was not created.")]
        [Priority(2)]
        [RuntimeTest]
        public void IISWebDir_InstallFailure()
        {
            string sourceFile = Path.Combine(IISWebDirTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
           
            // Verify WebDir was not created
            Assert.False(IISVerifier.WebDirExist("webdir", "Test web server"), String.Format("WebDir '{0}' in site '{1}' was created on failed install", "webdir", "Test web server"));
        }
    }
}
