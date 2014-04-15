﻿//-----------------------------------------------------------------------
// <copyright file="Burn.UpdateBundleTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods to test update bundles in Burn.
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class UpdateBundleTests : BurnTests
    {
        private const string V2 = "2.0.0.0";

        private PackageBuilder packageA;
        private PackageBuilder packageAv2;
        private BundleBuilder bundleA;
        private BundleBuilder bundleAv2;

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.")]
        [TestProperty("IsRuntimeTest", "true")]
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
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2.
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.IsNull(this.GetTestRegistryRoot());

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle Av1.0 then does an update to bundle Av2.0 during modify.")]
        [TestProperty("IsRuntimeTest", "true")]
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
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA2));

            // Run the v1 bundle providing an update bundle.
            installerA1.Modify(arguments: String.Concat("\"", "-updatebundle:", bundleA2, "\""));

            // Test that only v2 packages is installed.
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall v2.
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2).Uninstall();

            // Test all packages are uninstalled.
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.IsNull(this.GetTestRegistryRoot());

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0 and verifies arguments are passed through the whole way.")]
        [TestProperty("IsRuntimeTest", "true")]
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
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA2));

            // Attempt to uninstall bundleA2 without the verify arguments passed and expect failure code.
            installerA2.Uninstall(expectedExitCode:-1);

            // Remove the required arguments and uninstall again.
            this.SetBurnTestValue(BurnTests.TestValueVerifyArguments, null);
            installerA2.Uninstall();

            // Test all packages are uninstalled.
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA2));
            Assert.IsNull(this.GetTestRegistryRoot());

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle Av1.0 that is updated bundle Av2.0.  Verifies the OptionalUpdateRegistration Element is correct for both installs")]
        [TestProperty("IsRuntimeTest", "true")]
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
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageAv1));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.AreEqual("Y", key.GetValue("ThisVersionInstalled"));
                Assert.AreEqual("1.0.0.0", key.GetValue("PackageVersion"));
            }

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the OptionalUpdateRegistration exists.
            // SOFTWARE\[Manufacturer]\Updates\[ProductFamily]\[Name]
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.AreEqual("Y", key.GetValue("ThisVersionInstalled"));
                Assert.AreEqual("2.0.0.0", key.GetValue("PackageVersion"));
            }

            // Uninstall the second bundle and everything should be gone.
            installerAv2.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageAv2));

            // Make sure the key is removed.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_InstallUpdatedBundleOptionalUpdateRegistration - Bundle A"))
            {
                Assert.IsNull(key);
            }

            this.CleanTestArtifacts = false;
        }

        private PackageBuilder GetPackageA()
        {
            return (null != this.packageA) ? this.packageA : this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
        }

        private PackageBuilder GetPackageAv2()
        {
            return (null != this.packageAv2) ? this.packageAv2 : this.packageAv2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
        }

        private BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageA().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            return (null != this.bundleA) ? this.bundleA : this.bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
        }

        private BundleBuilder GetBundleAv2(Dictionary<string, string> bindPaths = null)
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
