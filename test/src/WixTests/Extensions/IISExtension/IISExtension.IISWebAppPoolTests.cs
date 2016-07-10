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
    /// IIS extension IISWebAppPool element tests
    /// </summary>
    public class IISWebAppPoolTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISWebAppPoolTests");

        [NamedFact]
        [Description("Install the MSI. Verify that the website was created and was started.Uninstall the product. Verify that the website was removed.")]
        [Priority(2)]
        [RuntimeTest]
        public void IISWebAppPool_Install()
        {
            string sourceFile = Path.Combine(IISWebAppPoolTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", new string[] { "WixIIsExtension", "WixUtilExtension" });

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the AppPool was created and verify its properties
            Assert.True(IISVerifier.AppPoolExists("App Pool 1"), String.Format("AppPool '{0}' was not created on Install", "App Pool 1"));
            long acctualApppoolQueueLength = IISVerifier.AppPoolQueueLength("App Pool 1");
            Assert.True(acctualApppoolQueueLength == 4444, String.Format("AppPool '{0}' was not created on Install", "App Pool 1", acctualApppoolQueueLength, 4444));
            string acctualProcessIdentity = IISVerifier.AppPoolProcessIdentity("App Pool 1");
            Assert.True(acctualProcessIdentity == "SpecificUser", String.Format("AppPool '{0}' ProcessingIdentity does not match expected. Acctual: '{1}'. Expected: '{2}'.", "App Pool 1", acctualProcessIdentity, "SpecificUser"));

            // Uninstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the app pool was removed
            Assert.False(IISVerifier.AppPoolExists("App Pool 1"), String.Format("AppPool '{0}' was not removed on Uninstall", "App Pool 1"));
        }

        [NamedFact]
        [Description("Cancel install of  MSI. Verify that the AppPool was not created.")]
        [Priority(2)]
        [RuntimeTest]
        public void IISWebAppPool_InstallFailure()
        {
            string sourceFile = Path.Combine(IISWebAppPoolTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", new string[] { "WixIIsExtension", "WixUtilExtension" });

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that the app pool was removed
            Assert.False(IISVerifier.AppPoolExists("App Pool 1"), String.Format("AppPool '{0}' was not removed on Rollback", "App Pool 1"));
        }

        [NamedFact]
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
