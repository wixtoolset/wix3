//-----------------------------------------------------------------------
// <copyright file="UtilExtension.WixCloseApplicationTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension WixCloseApplication tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    
    using System.Diagnostics;
   
    /// <summary>
    /// Util extension WixCloseApplication element tests
    /// </summary>
    [TestClass]
    public class WixCloseApplicationTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\WixCloseApplicationTests");

        [TestMethod]
        [Description("Verify that the (WixCloseApplication and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void WixCloseApplication_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(WixCloseApplicationTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("WixCloseApplications", 65, "WixCA", "WixCloseApplications"),
                new CustomActionTableData("WixCloseApplicationsDeferred", 3137, "WixCA", "WixCloseApplicationsDeferred"),
                new CustomActionTableData("WixCheckRebootRequired", 65, "WixCA", "WixCheckRebootRequired"));

            // Verify WixCloseApplication table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.WixCloseApplication,
                new TableRow(WixCloseApplicationColumns.WixCloseApplication.ToString(), "CloseNotepad"),
                new TableRow(WixCloseApplicationColumns.Target.ToString(), "notepad.exe"),
                new TableRow(WixCloseApplicationColumns.Description.ToString(), "Please close notepad before continuing."),
                new TableRow(WixCloseApplicationColumns.Condition.ToString(), string.Empty),
                new TableRow(WixCloseApplicationColumns.Attributes.ToString(), "5", false),
                new TableRow(WixCloseApplicationColumns.Sequence.ToString(), string.Empty, false),
                new TableRow(WixCloseApplicationColumns.Property.ToString(), string.Empty));
        }

        [TestMethod]
        [Description("Verify that the the application is closed when the msi is installed.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void WixCloseApplication_Install()
        {
            string sourceFile = Path.Combine(WixCloseApplicationTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            // Start Notepad process
            Process notepadProcess = Process.Start("notepad.exe");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(notepadProcess.HasExited, "Notepad process was NOT closed. It was expected to.");

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [TestMethod]
        [Description("Verify that the closeapplication does not fail when the application was not found.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void WixCloseApplication_ApplicationDoesNotExisit()
        {
            string sourceFile = Path.Combine(WixCloseApplicationTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
