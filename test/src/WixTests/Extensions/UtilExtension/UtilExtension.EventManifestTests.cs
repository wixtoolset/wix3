//-----------------------------------------------------------------------
// <copyright file="UtilExtension.EventManifestTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension EventManifest tests</summary>
//-----------------------------------------------------------------------

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
    /// Util extension EventManifest element tests
    /// </summary>
    public class EventManifestTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\EventManifestTests");

        [NamedFact]
        [Description("Verify that the (EventManifest and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void EventManifest_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(EventManifestTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureEventManifestRegister", 1, "ScaSchedule", "ConfigureEventManifestRegister"),
                new CustomActionTableData("ConfigureEventManifestUnregister", 1, "ScaSchedule", "ConfigureEventManifestUnregister"),
                new CustomActionTableData("RegisterEventManifest", 3073, "WixCA", "CAQuietExec"),
                new CustomActionTableData("UnregisterEventManifest", 3137, "WixCA", "CAQuietExec"),
                new CustomActionTableData("RollbackRegisterEventManifest", 3393, "WixCA", "CAQuietExec"),
                new CustomActionTableData("RollbackUnregisterEventManifest", 3329, "WixCA", "CAQuietExec"));

            Verifier.VerifyTableData(msiFile, MSITables.EventManifest,
                new TableRow(EventManifestColumns.Component_.ToString(), "Component1"),
                new TableRow(EventManifestColumns.File.ToString(), "[#event]"));
        }

        [NamedFact]
        [Description("Verify that the Event Manifest is created upon install.")]
        [Priority(2)]
        [RuntimeTest]
        public void EventManifest_Install()
        {
            string sourceFile = Path.Combine(EventManifestTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            string registryKey = @"Software\Microsoft\Windows\CurrentVersion\WINEVT\Publishers\{1db28f2e-8f80-4027-8c5a-a11f7f10f62d}";
            Assert.True(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not created on install.", registryKey));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the key was removed
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not removed on uninstall.", registryKey));
        }

        [NamedFact]
        [Description("Verify that the Event Manifest was removed upon rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void EventManifest_InstallFailure()
        {
            string sourceFile = Path.Combine(EventManifestTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            string registryKey = @"Software\Microsoft\Windows\CurrentVersion\WINEVT\Publishers\{1db28f2e-8f80-4027-8c5a-a11f7f10f62d}";
            
            // Verify that the file was not created
            Assert.False(RegistryVerifier.RegistryKeyExists(RegistryHive.LocalMachine, registryKey), String.Format("Registry Key '{0}' was not removed on Rollback.", registryKey));
        }
    }
}
