//-----------------------------------------------------------------------
// <copyright file="BurnTestBase.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest.BurnIntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Win32;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Xunit;

    /// <summary>
    /// Basic burn tests.
    /// </summary>
    public class BasicTests : BurnTestBase
    {
        private PackageBuilder packageA;
        private BundleBuilder bundleA;

        [NamedFact]
        [RuntimeTest]
        public void Burn_InstallUninstall()
        {
            // Build the packages.
            string packageA = this.CreatePackage("A").Output;
            string packageB = this.CreatePackage("B").Output;

            // Create the named bind paths to the packages.
            var bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = this.CreateBundle("BundleA", bindPaths).Output;
            string bundleB = this.CreateBundle("BundleB", bindPaths).Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall bundleB now.
            installerB.Uninstall();

            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));

            this.Complete();
        }

        [NamedFact]
        [RuntimeTest]
        public void Burn_InstallLockUninstallInstallUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;
            BundleRegistration registration = null;

            // Install.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(this.TryGetBundleRegistration("5802E2D0-AC39-4486-86FF-D4B7AD012EB5", out registration));
            Assert.Equal("~Burn_InstallLockUninstallInstallUninstall - Bundle A", registration.DisplayName);
            Assert.Equal(1, registration.Installed);
            Assert.Equal("1.0.0.0", registration.Version);

            // Uninstall while the file is locked.
            BundleInstaller uninstallerA = new BundleInstaller(this, registration.UninstallCommand);
            using (FileStream lockBundle = File.Open(registration.UninstallCommand, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                uninstallerA.Uninstall();
                Assert.False(MsiVerifier.IsPackageInstalled(packageA));
                Assert.False(this.TryGetBundleRegistration("5802E2D0-AC39-4486-86FF-D4B7AD012EB5", out registration));
                Assert.Null(registration);
            }

            // Install again.
            installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(this.TryGetBundleRegistration("5802E2D0-AC39-4486-86FF-D4B7AD012EB5", out registration));
            Assert.Equal("~Burn_InstallLockUninstallInstallUninstall - Bundle A", registration.DisplayName);
            Assert.Equal(1, registration.Installed);
            Assert.Equal("1.0.0.0", registration.Version);

            // Uninstall again.
            uninstallerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(this.TryGetBundleRegistration("5802E2D0-AC39-4486-86FF-D4B7AD012EB5", out registration));
            Assert.Null(registration);

            this.Complete();
        }

        [NamedFact]
        [RuntimeTest]
        public void Burn_MajorUpgrade()
        {
            string v2Version = "2.0.0.0";
            Dictionary<string, string> pv = new Dictionary<string, string>() { { "Version", v2Version } };

            // Build the packages.
            string packageAv1 = this.GetPackageA().Output;
            string packageAv2 = this.CreatePackage("A", preprocessorVariables : pv).Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsv1 = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsv2 = new Dictionary<string, string>() { { "packageA", packageAv2 } };

            // Build the bundles.
            string bundleAv1 = this.CreateBundle("BundleA", bindPathsv1).Output;
            string bundleAv2 = this.CreateBundle("BundleA", bindPathsv2, preprocessorVariables: pv).Output;

            // Initialize with first bundle.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleAv1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            // Uninstall the second bundle and everything should be gone.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            this.Complete();
        }

        [NamedFact]
        [RuntimeTest]
        public void Burn_MajorUpgradeUsingModify()
        {
            string v2Version = "2.0.0.0";

            // Build the packages.
            string packageAv1 = this.GetPackageA().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsv1 = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsv2 = new Dictionary<string, string>() { { "packageA", packageAv1 } };

            // Build the bundles.
            string bundleAv1 = this.CreateBundle("BundleA", bindPathsv1).Output;
            string bundleAv2 = this.CreateBundle("BundleA", bindPathsv2, preprocessorVariables : new Dictionary<string, string>() { { "Version", v2Version } }).Output;

            // Initialize with first bundle.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleAv1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Modify();
            //Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Uninstall the second bundle and everything should be gone.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            //Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            this.Complete();
        }

        //[NamedFact(Skip="This test is not supported yet.")]
        //[RuntimeTest]
        //public void Burn_MajorUpgradeSameVersion()
        //{
        //    // Build the packages.
        //    string packageA1 = this.GetPackageA().Output;
        //    string packageA2 = this.CreatePackage("A").Output;

        //    // Create the named bind paths to the packages.
        //    Dictionary<string, string> bindPaths1 = new Dictionary<string, string>() { { "packageA", packageA1 } };
        //    Dictionary<string, string> bindPaths2 = new Dictionary<string, string>() { { "packageA", packageA2 } };

        //    // Build the bundles.
        //    string bundleA1 = this.CreateBundle("BundleA", bindPaths1).Output;
        //    string bundleA2 = this.CreateBundle("BundleA", bindPaths2).Build().Output;

        //    // Initialize with first bundle.
        //    BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install();
        //    Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
        //    Assert.False(MsiVerifier.IsPackageInstalled(packageA2));

        //    // Install second bundle which will major upgrade away A1 (since they have the same version).
        //    BundleInstaller installerA2 = new BundleInstaller(this, bundleA2).Install();
        //    Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
        //    Assert.True(MsiVerifier.IsPackageInstalled(packageA2));

        //    // Uninstall the second bundle and everything should be gone.
        //    installerA2.Uninstall();
        //    Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
        //    Assert.False(MsiVerifier.IsPackageInstalled(packageA2));

        //    this.Completed();
        //}

        [NamedFact]
        [RuntimeTest]
        public void Burn_SharedMinorUpgrade()
        {
            string productCode = Guid.NewGuid().ToString("B").ToUpperInvariant();
            string originalVersion = "1.0.0.0";
            string v11Version = "1.0.1.0";

            var processorVariables = new Dictionary<string, string>() { { "ProductCode", productCode } };
            var processorVariablesV11 = new Dictionary<string, string>() { { "ProductCode", productCode }, { "Version", v11Version } };

            // Build the packages.
            string packageAv1 = this.CreatePackage("A" ,preprocessorVariables : processorVariables).Output;
            string packageAv11 = this.CreatePackage("A", preprocessorVariables: processorVariablesV11).Output;
            string packageB = this.CreatePackage("B").Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsA = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsB = new Dictionary<string, string>() { { "packageA", packageAv11 }, { "packageB", packageB } };

            // Build the bundles.
            string bundleA = this.CreateBundle("BundleA", bindPathsA).Output;
            string bundleB = this.CreateBundle("BundleB", bindPathsB).Output;

            // Initialize with first bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsProductInstalled(productCode));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(originalVersion, actualVersion);
            }

            // Install second bundle which will minor upgrade .
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            Assert.True(MsiVerifier.IsProductInstalled(productCode));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(v11Version, actualVersion);
            }

            // Uninstall the second bundle and only the minor upgrade MSI should be left.
            installerB.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.True(MsiVerifier.IsProductInstalled(productCode));
            using (var root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(v11Version, actualVersion);
            }

            // Now everything should be gone.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsProductInstalled(productCode));

            this.Complete();
        }

        [NamedFact]
        [RuntimeTest]
        public void Burn_MajorUpgradeRemovesPackageFixedByRepair()
        {
            string v2Version = "2.0.0.0";
            var pv2 = new Dictionary<string, string>() { { "Version", v2Version } };

            // Build the packages.
            string packageAv1 = this.GetPackageA().Output;
            string packageAv2 = this.CreatePackage("A", preprocessorVariables : pv2).Output;
            string packageB = this.CreatePackage("B").Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPathsv1 = new Dictionary<string, string>() { { "packageA", packageAv1 } };
            Dictionary<string, string> bindPathsv2 = new Dictionary<string, string>() { { "packageA", packageAv2 }, { "packageB", packageB } };

            // Build the bundles.
            string bundleAv1 = this.CreateBundle("BundleA", bindPathsv1).Output;
            string bundleB = this.CreateBundle("BundleB", bindPathsv2).Output;

            // Initialize with first bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleAv1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));

            // Install second bundle which will major upgrade away v1.
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            // Uninstall second bundle which will remove all packages
            installerB.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            // Repair first bundle to get v1 back on the machine.
            installerA.Repair();
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            // Uninstall first bundle and everything should be gone.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            this.Complete();
        }

        [NamedFact]
        [RuntimeTest]
        public void Burn_ValidateMultipleSourcePaths()
        {
            // Build the package.
            string packageA = this.GetPackageA().Output;
            string packageA_Directory = Path.GetDirectoryName(packageA);
            string packageA_ProductCode = MsiUtils.GetMSIProductCode(packageA);

            // Build the bundle.
            string bundleA = this.GetBundleA().Output;

            // Install the bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Copy the package using the bundle package name.
            ProductInstallation product = new ProductInstallation(packageA_ProductCode, null, UserContexts.Machine);
            string packageA_Copy = Path.Combine(packageA_Directory, product.AdvertisedPackageName);
            File.Copy(packageA, packageA_Copy);
            this.TestArtifacts.Add(new FileInfo(packageA_Copy));

            // Repair and recache the MSI.
            MSIExec.InstallProduct(packageA_Copy, MSIExec.MSIExecReturnCode.SUCCESS, "REINSTALL=ALL REINSTALLMODE=vomus");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Check that the source contains both the original and burn cached paths.
            SourceList sources = product.SourceList;
            Assert.Equal(2, sources.Count);

            // Attempt to uninstall bundleA.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));

            this.Complete();
        }

        private PackageBuilder GetPackageA()
        {
            if (null == this.packageA)
            {
                this.packageA = this.CreatePackage("A");
            }

            return this.packageA;
        }

        private BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageA().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            if (null == this.bundleA)
            {
                this.bundleA = this.CreateBundle("BundleA", bindPaths);
            }

            return this.bundleA;
        }
    }
}
