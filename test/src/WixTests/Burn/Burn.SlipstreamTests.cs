//-----------------------------------------------------------------------
// <copyright file="Burn.SlipstreamTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods test Burn slipstreaming.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.Win32;
    using WixTest.Verifiers;
    using Xunit;

    public class SlipstreamTests : BurnTests
    {
        private const string V101 = "1.0.1.0";

        private WixTest.PackageBuilder packageA;
        private WixTest.PackageBuilder packageAv101;
        private WixTest.PatchBuilder patchA;
        private WixTest.PackageBuilder packageB;

        private WixTest.BundleBuilder bundleA;
        private WixTest.BundleBuilder bundleOnlyA;
        private WixTest.BundleBuilder bundleOnlyPatchA;
        private WixTest.BundleBuilder bundleAReverse;
        private WixTest.BundleBuilder bundleB;

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream then removes it.")]
        [RuntimeTest]
        public void Burn_SlipstreamInstallUninstall()
        {
            const string patchedVersion = V101;

            // Build the bundle.
            string bundleA = this.GetBundleA().Output;
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            install.Uninstall();

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream then installs a bundle with only the patch then patch only and slipstream should stay.")]
        [RuntimeTest]
        public void Burn_SlipstreamInstallReferenceCountUninstall()
        {
            const string patchedVersion = V101;

            // Build the bundles.
            string bundleA = this.GetBundleA().Output;
            string bundlePatchA = this.GetBundleOnlyPatchA().Output;

            // Install the bundle with slipstreamed patch.
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Install the bundle with only the patch. This is basically a no-op.
            BundleInstaller installPatch = new BundleInstaller(this, bundlePatchA).Install();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Uninstall the bundle with only the patch. This should also basically be a no-op.
            installPatch.Uninstall();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Finally uninstall the original bundle and that should clean everything off.
            install.Uninstall();

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream then repairs it.")]
        [RuntimeTest]
        public void Burn_SlipstreamRepair()
        {
            const string patchedVersion = V101;

            string bundleA = this.GetBundleA().Output;
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Delete the installed file and registry key.
            File.Delete(packageSourceCodeInstalled);
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                root.DeleteValue("A");
            }

            // Repair and verify the repair fixed everything.
            install.Repair();

            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload repaired at: ", packageSourceCodeInstalled));

            // Clean up.
            install.Uninstall();

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream patch chained before the MSI then repairs.")]
        [RuntimeTest]
        public void Burn_SlipstreamReverseRepair()
        {
            const string patchedVersion = V101;

            string bundleA = this.GetBundleAReverse().Output;

            // Ensure the patch is not installed when the bundle installs.
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.True(patchedVersion == actualVersion, "Patch A should not have been installed.");
            }

            // Repair the bundle and send the patch along for the ride.
            File.Delete(packageSourceCodeInstalled);
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                root.DeleteValue("A");
            }

            install.Repair();

            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload *still* installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.True(patchedVersion.Equals(actualVersion), "Patch A should have been installed during the repair.");
            }

            install.Uninstall(); // uninstall just to make sure no error occur removing the package without the patch.

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with package then installs bundle with slipstream forcing repair.")]
        [RuntimeTest]
        public void Burn_SlipstreamRepairOnlyPatch()
        {
            const string unpatchedVersion = "1.0.0.0";
            const string patchedVersion = V101;

            // Create the bundle with only teh package and a bundle that slipstreams the package and bundle.
            string bundleOnlyA = this.GetBundleOnlyA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the package.
            BundleInstaller installOnlyPackage = new BundleInstaller(this, bundleOnlyA).Install();

            // Verify the package is installed correctly.
            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(unpatchedVersion, actualVersion);
            }

            // Delete the installed file and registry key so we have something to repair.
            File.Delete(packageSourceCodeInstalled);
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                root.DeleteValue("A");
            }

            // "Install" the bundle but force everything to be a repair.
            this.SetPackageRequestedState("packageA", RequestState.Repair);
            this.SetPackageRequestedState("patchA", RequestState.Repair);
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Clean up.
            this.ResetPackageStates("packageA");
            this.ResetPackageStates("patchA");
            install.Uninstall();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(unpatchedVersion, actualVersion);
            }

            installOnlyPackage.Uninstall();

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with package then installs bundle with slipstream before the package forcing repair.")]
        [RuntimeTest]
        public void Burn_SlipstreamReverseRepairOnlyPatch()
        {
            const string unpatchedVersion = "1.0.0.0";
            const string patchedVersion = V101;

            // Create the bundle with only teh package and a bundle that slipstreams the package and bundle.
            string bundleOnlyA = this.GetBundleOnlyA().Output;
            string bundleA = this.GetBundleAReverse().Output;

            // Install the package.
            BundleInstaller installOnlyPackage = new BundleInstaller(this, bundleOnlyA).Install();

            // Verify the package is installed correctly.
            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(unpatchedVersion, actualVersion);
            }

            // Delete the installed file and registry key so we have something to repair.
            File.Delete(packageSourceCodeInstalled);
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                root.DeleteValue("A");
            }

            // "Install" the bundle but force everything to be a repair.
            this.SetPackageRequestedState("packageA", RequestState.Repair);
            this.SetPackageRequestedState("patchA", RequestState.Repair);
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Clean up.
            this.ResetPackageStates("packageA");
            this.ResetPackageStates("patchA");
            install.Uninstall();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(unpatchedVersion, actualVersion);
            }

            installOnlyPackage.Uninstall();

            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream then removes it.")]
        [RuntimeTest]
        public void Burn_SlipstreamRemovePatchAlone()
        {
            const string patchedVersion = V101;

            string bundleA = this.GetBundleA().Output;
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Remove only the slipstream patch and ensure the version is back to default.
            this.SetPackageRequestedState("patchA", RequestState.Absent);
            install.Modify();

            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload *still* installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.True("1.0.0.0".Equals(actualVersion), "Patch A should have been removed and so the registry key would go back to default version.");
            }

            install.Uninstall(); // uninstall just to make sure no error occur removing the package without the patch.

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstreamed package A and package B then removes both package A and patch A at same time.")]
        [RuntimeTest]
        public void Burn_SlipstreamRemovePackageAndPatch()
        {
            const string patchedVersion = V101;

            // Create bundle and install everything.
            string bundleB = this.GetBundleB().Output;
            BundleInstaller install = new BundleInstaller(this, bundleB).Install();

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"B\B.wxs");
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package B payload installed at: ", packageSourceCodeInstalled));

            // Remove package A and its patch should go with it.
            this.SetPackageRequestedState("packageA", RequestState.Absent);
            this.SetPackageRequestedState("patchA", RequestState.Absent);
            install.Modify();

            this.ResetPackageStates("packageA");
            this.ResetPackageStates("patchA");

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("After modify, should *not* have found Package A payload installed at: ", packageSourceCodeInstalled));

            // Remove.
            install.Uninstall();

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"B\B.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("After uninstall bundle, should *not* have found Package B payload installed at: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstreamed package A and package B and trigger error rollback.")]
        [RuntimeTest]
        public void Burn_SlipstreamFailureRollback()
        {
            // Create a folder with same name as the file to be installed in package B, this will trigger error in B and rollback A
            string errorTriggeringFolder = this.GetTestInstallFolder(@"B\B.wxs");
            if (!Directory.Exists(errorTriggeringFolder))
            {
                Directory.CreateDirectory(errorTriggeringFolder);
            }

            // Create bundle and install everything.
            string bundleB = this.GetBundleB().Output;
            BundleInstaller install = new BundleInstaller(this, bundleB).Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Nothing should exist after the rollback
            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should NOT have found Package A payload installed at: ", packageSourceCodeInstalled));

            packageSourceCodeInstalled = this.GetTestInstallFolder(@"B\B.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should NOT have found Package B payload installed at: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should NOT exist after rollback.");
            
            // Delete the directory
            Directory.Delete(errorTriggeringFolder);

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle using automatic slipstreaming then removes it.")]
        [RuntimeTest]
        public void Burn_AutomaticSlipstreamInstallUninstall()
        {
            const string originalVersion = "1.0.0.0";
            const string patchedVersion = "1.0.1.0";

            // Build the packages.
            string packageA = this.GetPackageA().Output;
            string packageAUpdate = this.GetPackageAv101().Output;
            string packageB = this.GetPackageB().Output;
            string packageBUpdate = new PackageBuilder(this, "B") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion} }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, TargetPaths = new string[] { packageA, packageB }, UpgradePaths = new string[] { packageAUpdate, packageBUpdate } }.Build().Output;
            string patchB = new PatchBuilder(this, "PatchB") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, TargetPaths = new string[] { packageA, packageB }, UpgradePaths = new string[] { packageAUpdate, packageBUpdate } }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);
            bindPaths.Add("patchA", patchA);
            bindPaths.Add("patchB", patchB);

            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            BundleInstaller install = new BundleInstaller(this, bundleC).Install();

            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                // Product A should've slipstreamed both patches.
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.Equal(patchedVersion, actualVersion);

                // Product B should've only slipstreamed patch B.
                actualVersion = root.GetValue("B") as string;
                Assert.Equal(originalVersion, actualVersion);

                actualVersion = root.GetValue("B2") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            install.Uninstall();

            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with package that is doing a major upgrade and has a patch slipstreamed to it.")]
        [RuntimeTest]
        public void Burn_MajorUpgradeWithSlipstream()
        {
            const string originalVersion = "1.0.0.0";
            const string upgradeVersion = "2.0.0.0";
            const string patchedVersion = "2.0.1.0";

            // Build the packages.
            string originalPackageA = this.GetPackageA().Output;
            string upgradePackageA = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", upgradeVersion } }, }.Build().Output;
            string packageAUpdate = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, TargetPaths = new string[] { upgradePackageA }, UpgradePaths = new string[] { packageAUpdate } }.Build().Output;

            // Create the named bind paths to the packages in the bundle.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", upgradePackageA);
            bindPaths.Add("patchA", patchA);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the original MSI.
            MSIExec.InstallProduct(originalPackageA, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(MsiVerifier.IsPackageInstalled(originalPackageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                // Original Product A should be present.
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(originalVersion, actualVersion);
            }

            // Now install the bundle that should upgrade the MSI and apply the patch.
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            Assert.False(MsiVerifier.IsPackageInstalled(originalPackageA));
            Assert.True(MsiVerifier.IsPackageInstalled(upgradePackageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                // Product A should've slipstreamed with its patch.
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            install.Uninstall();

            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        private WixTest.PackageBuilder GetPackageA()
        {
            return (null != this.packageA) ? this.packageA : this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
        }

        private WixTest.PackageBuilder GetPackageAv101()
        {
            return (null != this.packageAv101) ? this.packageAv101 : this.packageAv101 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", V101 } }, NeverGetsInstalled = true }.Build();
        }

        private WixTest.PatchBuilder GetPatchA()
        {
            if (null == this.patchA)
            {
                string packageA = this.GetPackageA().Output;
                string packageAUpdate = this.GetPackageAv101().Output;

                this.patchA = new PatchBuilder(this, "PatchA") { TargetPath = packageA, UpgradePath = packageAUpdate }.Build();
            }

            return this.patchA;
        }

        private WixTest.PackageBuilder GetPackageB()
        {
            return (null != this.packageB) ? this.packageB : this.packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build();
        }

        private WixTest.BundleBuilder GetBundleA()
        {
            if (null == this.bundleA)
            {
                string packageAPath = this.GetPackageA().Output;
                string patchAPath = this.GetPatchA().Output;
                Dictionary<string, string> bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath }, { "patchA", patchAPath } };

                this.bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleA;
        }

        private WixTest.BundleBuilder GetBundleOnlyA()
        {
            if (null == this.bundleOnlyA)
            {
                string packageAPath = this.GetPackageA().Output;
                Dictionary<string, string> bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };

                this.bundleOnlyA = new BundleBuilder(this, "BundleOnlyA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleOnlyA;
        }

        private WixTest.BundleBuilder GetBundleOnlyPatchA()
        {
            if (null == this.bundleOnlyPatchA)
            {
                string patchAPath = this.GetPatchA().Output;
                Dictionary<string, string> bindPaths = new Dictionary<string, string>() { { "patchA", patchAPath } };

                this.bundleOnlyPatchA = new BundleBuilder(this, "BundleOnlyPatchA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleOnlyPatchA;
        }

        private WixTest.BundleBuilder GetBundleAReverse()
        {
            if (null == this.bundleAReverse)
            {
                string packageAPath = this.GetPackageA().Output;
                string patchAPath = this.GetPatchA().Output;
                Dictionary<string, string> bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath }, { "patchA", patchAPath } };

                this.bundleAReverse = new BundleBuilder(this, "BundleAReverse") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleAReverse;
        }

        private WixTest.BundleBuilder GetBundleB()
        {
            if (null == this.bundleB)
            {
                string packageAPath = this.GetPackageA().Output;
                string patchAPath = this.GetPatchA().Output;
                string packageBPath = this.GetPackageB().Output;
                Dictionary<string, string> bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath }, { "patchA", patchAPath }, { "packageB", packageBPath } };

                this.bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleB;
        }
    }
}
