//-----------------------------------------------------------------------
// <copyright file="Burn.RelatedBundleTests.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Contains methods to test related bundles in Burn.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Deployment.WindowsInstaller;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Microsoft.Win32;
    using Xunit;

    public class MultiplePatchesTests: BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, patch bundle B, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallPatchBundle()
        {
            const string patchVersion = "1.0.1.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A").Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, NeverGetsInstalled = true }.Build().Output;

            string packageB1 = new PackageBuilder(this, "B").Build().Output;
            string packageB2 = new PackageBuilder(this, "B") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, NeverGetsInstalled = true }.Build().Output;

            string patchB = new PatchBuilder(this, "PatchB") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, TargetPath = packageB1, UpgradePath = packageB2 }.Build().Output;

            PatchBuilder patchBuilderAB = new PatchBuilder(this, "PatchAB") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } } };
            patchBuilderAB.TargetPaths = new string[] { packageA1, packageB1 };
            patchBuilderAB.UpgradePaths = new string[] { packageA2, packageB2 };
            string patchAB = patchBuilderAB.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("patchAB", patchAB);
            bindPaths.Add("packageB", packageB1);
            bindPaths.Add("patchB", patchB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the msi bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            // Test both packages are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1)); 
            
            
            // Install the patch bundle.
            //     Slow the caching of patchB to ensure that patchAB finishes caching and needs to wait for patchB to be cached.
            this.SetPackageSlowCache("patchB", 10000);
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A2") as string;
                Assert.Equal(patchVersion, actualVersion);

                actualVersion = root.GetValue("B") as string;
                Assert.Equal(patchVersion, actualVersion);

                actualVersion = root.GetValue("B2") as string;
                Assert.Equal(patchVersion, actualVersion);
            }

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that uninstalling bundle A detected and would remove bundle B.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.1\.0, operation: Remove"));

            // Test both packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }
    }
}
