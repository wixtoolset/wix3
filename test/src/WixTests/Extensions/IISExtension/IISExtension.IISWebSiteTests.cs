//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISWebSiteTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension IISWebSite tests</summary>
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
    /// IIS extension IISWebSite element tests
    /// </summary>
    [TestClass]
    public class IISWebSiteTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebSiteTests");

        [TestMethod]
        [Description("Verify that the (IISWebSite,CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void IISWebSite_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISWebSiteTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));
            
            Verifier.VerifyTableData(msiFile, MSITables.IIsWebSite,
                new TableRow(IIsWebSiteColumns.Web.ToString(), "Test"),
                new TableRow(IIsWebSiteColumns.Component_.ToString(), "TestWebSiteProductComponent"),
                new TableRow(IIsWebSiteColumns.Description.ToString(), "Test web server"),
                new TableRow(IIsWebSiteColumns.ConnectionTimeout.ToString(), string.Empty, false),
                new TableRow(IIsWebSiteColumns.Directory_.ToString(), "TestWebSiteProductDirectory"),
                new TableRow(IIsWebSiteColumns.State.ToString(), "2", false),
                new TableRow(IIsWebSiteColumns.Attributes.ToString(), "2", false),
                new TableRow(IIsWebSiteColumns.KeyAddress_.ToString(), "TestAddress"),
                new TableRow(IIsWebSiteColumns.DirProperties_.ToString(), "ReadAndExecute"),
                new TableRow(IIsWebSiteColumns.Application_.ToString(), string.Empty),
                new TableRow(IIsWebSiteColumns.Sequence.ToString(), string.Empty, false),
                new TableRow(IIsWebSiteColumns.Log_.ToString(), "log"));
        }
        
        [TestMethod]
        [Description("Install the MSI. Verify that the website was created and was started.Uninstall the product. Verify that the website was removed.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebSite_Install()
        {
            string sourceFile = Path.Combine(IISWebSiteTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the website was created and was started
            Assert.IsTrue(IISVerifier.WebSiteExists("Test web server"), "WebSite '{0}' was not created on Install", "Test web server");
            Assert.IsTrue(IISVerifier.WebSiteStarted("Test web server"), "WebSite '{0}' was not started on Install", "Test web server");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the website was removed
            Assert.IsFalse(IISVerifier.WebSiteExists("Test web server"), "WebSite '{0}' was not removed on Uninstall", "Test web server");
        }

        [TestMethod]
        [Description("Verify that the expected Candle error is shown when WebSite element has @AutoStart specified without a component parent.")]
        [Priority(3)]
        public void IISWebSite_AutoStartSpecified()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISWebSiteTests.TestDataDirectory, @"AutoStartSpecified.wxs"));
            candle.Extensions.Add("WixIIsExtension");

            candle.ExpectedWixMessages.Add(new WixMessage(5151, "The iis:WebSite/@AutoStart attribute cannot be specified unless the element has a Component as an ancestor. A iis:WebSite that does not have a Component ancestor is not installed.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5151;
            candle.Run();
        }
    }
}
