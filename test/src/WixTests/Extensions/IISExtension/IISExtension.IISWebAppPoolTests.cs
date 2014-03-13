//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISWebAppPoolTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension IISWebAppPool tests</summary>
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
    /// IIS extension IISWebAppPool element tests
    /// </summary>
    [TestClass]
    public class IISWebAppPoolTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebAppPoolTests");

        [TestMethod]
        [Description("Install the MSI. Verify that the website was created and was started.Uninstall the product. Verify that the website was removed.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebAppPool_Install()
        {
            string sourceFile = Path.Combine(IISWebAppPoolTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", new string[] { "WixIIsExtension", "WixUtilExtension" });

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the AppPool was created and verify its properties
            Assert.IsTrue(IISVerifier.AppPoolExists("App Pool 1"), "AppPool '{0}' was not created on Install", "App Pool 1");
            long acctualApppoolQueueLength = IISVerifier.AppPoolQueueLength("App Pool 1");
            Assert.IsTrue(acctualApppoolQueueLength == 4444, "AppPool '{0}' was not created on Install", "App Pool 1", acctualApppoolQueueLength, 4444);
            string acctualProcessIdentity = IISVerifier.AppPoolProcessIdentity("App Pool 1");
            Assert.IsTrue(acctualProcessIdentity == "SpecificUser", "AppPool '{0}' ProcessingIdentity does not match expected. Acctual: '{1}'. Expected: '{2}'.", "App Pool 1", acctualProcessIdentity, "SpecificUser");

            // Uninstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the app pool was removed
            Assert.IsFalse(IISVerifier.AppPoolExists("App Pool 1"), "AppPool '{0}' was not removed on Uninstall", "App Pool 1");
        }

        [TestMethod]
        [Description("Cancel install of  MSI. Verify that the AppPool was not created.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISWebAppPool_InstallFailure()
        {
            string sourceFile = Path.Combine(IISWebAppPoolTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", new string[] { "WixIIsExtension", "WixUtilExtension" });

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that the app pool was removed
            Assert.IsFalse(IISVerifier.AppPoolExists("App Pool 1"), "AppPool '{0}' was not removed on Rollback", "App Pool 1");
        }

        [TestMethod]
        [Description("Verify that the expected Candle error is shown when WebAppPool element is missing its parent component.")]
        [Priority(3)]
        public void IISWebAppPool_MissingComponentAncestor()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISWebAppPoolTests.TestDataDirectory, @"MissingComponentAncestor.wxs"));
            candle.Extensions.Add("WixIIsExtension");

            candle.ExpectedWixMessages.Add(new WixMessage(5152, "The iis:WebAppPool element cannot be specified unless the element has a Component as an ancestor. A iis:WebAppPool that does not have a Component ancestor is not installed.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5152;
            candle.Run();
        }
    }
}
