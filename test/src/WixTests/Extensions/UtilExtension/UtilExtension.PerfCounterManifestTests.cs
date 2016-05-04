// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Microsoft.Win32;
    using Xunit;

    /// <summary>
    /// Util extension PerfCounterManifest element tests
    /// </summary>
    public class PerfCounterManifestTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\PerfCounterManifestTests");

        [NamedFact]
        [Description("Verify that the (PerfCounterManifest and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void PerfCounterManifest_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(PerfCounterManifestTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigurePerfmonManifestRegister", 1, "ScaSchedule", "ConfigurePerfmonManifestRegister"),
                new CustomActionTableData("ConfigurePerfmonManifestUnregister", 1, "ScaSchedule", "ConfigurePerfmonManifestUnregister"),
                new CustomActionTableData("RegisterPerfmonManifest", 3073, "WixCA", "CAQuietExec"),
                new CustomActionTableData("UnregisterPerfmonManifest", 3137, "WixCA", "CAQuietExec"),
                new CustomActionTableData("RollbackRegisterPerfmonManifest", 3393, "WixCA", "CAQuietExec"),
                new CustomActionTableData("RollbackUnregisterPerfmonManifest", 3329, "WixCA", "CAQuietExec")
                );

            Verifier.VerifyTableData(msiFile, MSITables.PerfmonManifest,
                new TableRow(PerfmonManifestColumns.Component_.ToString(), "TestPerfmonManifestProductComponent"),
                new TableRow(PerfmonManifestColumns.File.ToString(), "[#SymFile]"),
                new TableRow(PerfmonManifestColumns.ResourceFileDirectory.ToString(), "[TestPerfmonProductDirectory]"));
        }

        [NamedFact]
        [Description("Verify that the Performance counter manifest is created upon install.")]
        [Priority(2)]
        [RuntimeTest]
        public void PerfCounterManifest_Install()
        {
            string sourceFile = Path.Combine(PerfCounterManifestTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string keyName = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\_V2Providers\{42aaeb49-78e7-4d78-81a0-6f35bfde65bc}";
            string valueName = "ProviderName";
            string expectedValue = "ApServerPerfmon";

            RegistryVerifier.VerifyRegistryKeyValue(RegistryHive.LocalMachine, keyName, valueName, expectedValue);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, keyName), String.Format("Registry Key '{0}' was not removed on uninstall.", keyName));
        }

        [NamedFact]
        [Description("Verify that the Performance counter manifest was removed upon rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void PerfCounterManifest_InstallFailure()
        {
            string sourceFile = Path.Combine(PerfCounterManifestTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            string keyName = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\_V2Providers\{42aaeb49-78e7-4d78-81a0-6f35bfde65bc}";

            // Verify that the file was removed
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, keyName), String.Format("Registry Key '{0}' was not removed on uninstall.", keyName));
        }
    }
}
