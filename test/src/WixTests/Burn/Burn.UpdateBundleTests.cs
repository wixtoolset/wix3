// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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

    public class UpdateBundleTests : BurnTests
    {
        private const string V2 = "2.0.0.0";

        private WixTest.PackageBuilder packageA;
        private WixTest.PackageBuilder packageAv2;
        private WixTest.BundleBuilder bundleA;
        private WixTest.BundleBuilder bundleAv2;

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundle()
        {
            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\""));
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2);

            // Test that only the newest packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2.
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 then does an update to bundle Av2.0 during modify.")]
        [RuntimeTest]
        public void Burn_UpdateInstalledBundle()
        {
            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install();

            // Test that v1 was correctly installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));

            // Run the v1 bundle providing an update bundle.
            installerA1.Modify(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\""));

            // Test that only v2 packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall v2.
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2).Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0 and verifies arguments are passed through the whole way.")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundleVerifyArguments()
        {
            const string verifyArguments = "these arguments should exist";

            // Build the packages.
            string packageA1 = this.GetPackageA().Output;
            string packageA2 = this.GetPackageAv2().Output;

            // Build the bundles.
            string bundleA1 = this.GetBundleA().Output;
            string bundleA2 = this.GetBundleAv2().Output;

            this.SetBurnTestValue(BurnTests.TestValueVerifyArguments, verifyArguments);

            // Install the v1 bundle.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\" ", verifyArguments));
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2);

            // Test that only the newest packages is installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2 without the verify arguments passed and expect failure code.
            installerA2.Uninstall(expectedExitCode:-1);

            // Remove the required arguments and uninstall again.
            this.SetBurnTestValue(BurnTests.TestValueVerifyArguments, null);
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.  Verifies the OptionalUpdateRegistration Element is correct for both installs")]
        [RuntimeTest]
        public void Burn_InstallUpdatedBundleOptionalUpdateRegistration()
        {
            string v2Version = "2.0.0.0";

            // Build the packages.
            string packageAv1 = new PackageBuilder(this, "A").Build().Output;
            string packageAv2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsv1 = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsv2 = new Dictionary<string, string>() { { "packageA", packageAv2 } };

            // Build the bundles.
            string bundleAv1 = new BundleBuilder(this, "BundleA") { BindPaths = bindPathsv1, Extensions = Extensions }.Build().Output;
            string bundleAv2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPathsv2, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Initialize with first bundle.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleAv1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("1.0.0.0", key.GetValue("PackageVersion"));
            }

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("2.0.0.0", key.GetValue("PackageVersion"));
            }

            // Uninstall the second bundle and everything should be gone.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the key is removed.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.Null(key);
            }
        }

        private WixTest.PackageBuilder GetPackageA()
        {
            return (null != this.packageA) ? this.packageA : this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
        }

        private WixTest.PackageBuilder GetPackageAv2()
        {
            return (null != this.packageAv2) ? this.packageAv2 : this.packageAv2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }

        private WixTest.BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageA().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            return (null != this.bundleA) ? this.bundleA : this.bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
        }

        private WixTest.BundleBuilder GetBundleAv2(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageAv2().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            return (null != this.bundleAv2) ? this.bundleAv2 : this.bundleAv2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }
    }
}
