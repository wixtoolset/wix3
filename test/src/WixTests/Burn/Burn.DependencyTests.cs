//-----------------------------------------------------------------------
// <copyright file="Burn.DependencyTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods test Burn.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System.Collections.Generic;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.Win32;
    using Xunit;

    public class DependencyTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then bundle B, and removes them in reverse order.")]
        [RuntimeTest]
        public void Burn_InstallBundles()
        {
            const string expectedVersion = "1.0.0.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // Make sure the MSIs are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Uninstall in reverse order.
            installerB.Uninstall();
            installerA.Uninstall();

            // Make sure the MSIs are not installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then bundle B, attempts to remove bundle A, then removes them in reverse order.")]
        [RuntimeTest]
        public void Burn_UninstallBundleWithDependent()
        {
            const string expectedVersion = "1.0.0.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // Make sure the MSIs and EXE are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Verify packageA and ExeA are still installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Uninstall bundleB now.
            installerB.Uninstall();

            // Make sure the MSIs are installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install bundle A then B, upgrade A, then attempt to uninstall A while B is still present.")]
        [RuntimeTest]
        public void Burn_UninstallUpgradedBundle()
        {
            const string expectedVersion1 = "1.0.0.0";
            const string expectedVersion2 = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;
            string packageA1 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } } }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Override the path for A to A1.
            bindPaths["packageA"] = packageA1;
            string bundleA1 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } } }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // Make sure the MSIs and EXE are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion1, actualVersion);
            }

            // Attempt to upgrade bundleA.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install();

            // Verify packageA1 was installed and packageA was uninstalled.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion2, actualVersion);
            }

            // Uninstall bundleA1 and verify that packageA1 is still installed.
            installerA1.Uninstall();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));

            // Uninstall bundleB now.
            // // SFBUG:3307315, or (new) 2555 - Detect for upgraded package also.
            installerB.Uninstall();

            // Make sure the MSIs are not installed.
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install bundle A, then upgrade it with a slipstream of package A and patch A.")]
        [RuntimeTest]
        public void Burn_InstallUpgradeSlipstreamBundle()
        {
            const string expectedVersion1 = "1.0.0.0";
            const string expectedVersion2 = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageA1 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } }, TargetPath = packageA, UpgradePath = packageA1 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("patchA", patchA);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } } }.Build().Output;

            // Install the base bundle and make sure it's installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion1, actualVersion);
            }

            // Install the upgrade bundle with a slipstreamed patch and make sure the patch is installed.
            // SFBUG:3387046 - Uninstalling bundle registers a dependency on a package
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();
            Assert.True(MsiUtils.IsPatchInstalled(patchA));

            // BundleC doesn't carry the EXE, so make sure it's removed.
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                Assert.Null(root.GetValue("Version"));
            }

            // Repair the upgrade bundle to make sure it does not prompt for source.
            // SFBUG:3386927 - MSIs get removed from cache during upgrade
            installerC.Repair();

            // Uninstall the slipstream bundle and make sure both packages are uninstalled.
            installerC.Uninstall();
            Assert.False(MsiUtils.IsPatchInstalled(patchA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install bundle A, then install upgrade bundle D which will fail and roll back.")]
        [RuntimeTest]
        public void Burn_RollbackUpgradeBundle()
        {
            const string expectedVersion1 = "1.0.0.0";
            const string expectedVersion2 = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleD = new BundleBuilder(this, "BundleD") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } } }.Build().Output;

            // Install the base bundle and make sure it's installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion1, actualVersion);
            }

            // Install the upgrade bundle that will fail and rollback. Make sure packageA is still present.
            // SFBUG:3405221 - pkg dependecy not removed in rollback if pkg already present
            BundleInstaller installerD = new BundleInstaller(this, bundleD).Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion1, actualVersion);
            }

            // Uninstall the first bundle and make sure packageA is uninstalled.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs an MSI then fails a non-vital package to test that the bundle still installs successfully.")]
        [RuntimeTest]
        public void Burn_FailNonVitalPackage()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageC = new PackageBuilder(this, "C") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageC);

            // Build the bundle.
            string bundleE = new BundleBuilder(this, "BundleE") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundle and make sure packageA is installed.
            // SFBUG:3435047 - Make sure during install we don't fail for non-vital packages.
            BundleInstaller installerE = new BundleInstaller(this, bundleE).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                Assert.Null(root.GetValue("Version"));
            }

            // Repair the bundle.
            // SFBUG:3435047 - Make sure during repair we don't fail for the same reason in a different code path.
            installerE.Repair();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                Assert.Null(root.GetValue("Version"));
            }

            // Uninstall the bundle and make sure packageA is uninstalled.
            installerE.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle, then an addon bundle, and uninstalls the main bundle.")]
        [RuntimeTest]
        public void Burn_UninstallAddonBundle()
        {
            const string expectedVersion = "1.0.0.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA1 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleA2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleF = new BundleBuilder(this, "BundleF") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the base bundle and make sure all packages are installed.
            BundleInstaller installerF = new BundleInstaller(this, bundleF).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Install an addon bundle and make sure all packages are installed.
            BundleInstaller installerA1 = new BundleInstaller(this, bundleA1).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Install a second addon bundle and make sure all packages are installed.
            BundleInstaller installerA2 = new BundleInstaller(this, bundleA2).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Uninstall the base bundle and make sure all packages are uninstalled.
            installerF.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle, then a patch bundle, and uninstalls the main bundle.")]
        [RuntimeTest]
        public void Burn_InstallPatchBundle()
        {
            const string expectedVersion = "1.0.1.0";

            // Build the packages with explicit provider keys.
            string packageA1 = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string packageB1 = new PackageBuilder(this, "E") { Extensions = Extensions }.Build().Output;
            string packageB2 = new PackageBuilder(this, "E") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } } }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } }, TargetPath = packageA1, UpgradePath = packageA2 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageB", packageB1);
            bindPaths.Add("patchA", patchA);

            // Build the base bundle.
            string bundleF = new BundleBuilder(this, "BundleF") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Build the patch bundle.
            bindPaths["packageB"] = packageB2;
            string bundleJ = new BundleBuilder(this, "BundleJ") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } } }.Build().Output;

            // Install the base bundle and make sure all packages are installed.
            BundleInstaller installerF = new BundleInstaller(this, bundleF).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));

            // Install patch bundle and make sure all packages are installed.
            BundleInstaller installerJ = new BundleInstaller(this, bundleJ).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.True(MsiUtils.IsPatchInstalled(patchA));

            // Uninstall the base bundle and make sure all packages are uninstalled.
            installerF.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.False(MsiUtils.IsPatchInstalled(patchA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle, then a patch bundle, uninstalls the patch bundle and makes sure the base bundle is restored.")]
        [RuntimeTest]
        public void Burn_UninstallPatchBundle()
        {
            const string expectedVersion = "1.0.1.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string packageB1 = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;
            string packageB2 = new PackageBuilder(this, "B") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } } }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } }, TargetPath = packageA1, UpgradePath = packageA2 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageB", packageB1);
            bindPaths.Add("patchA", patchA);

            // Build the base bundle.
            string bundleF = new BundleBuilder(this, "BundleF") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Build the patch bundle.
            bindPaths["packageB"] = packageB2;
            string bundleJ = new BundleBuilder(this, "BundleJ") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion } } }.Build().Output;

            // Install the base bundle and make sure all packages are installed.
            BundleInstaller installerF = new BundleInstaller(this, bundleF).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));

            // Install patch bundle and make sure all packages are installed.
            BundleInstaller installerJ = new BundleInstaller(this, bundleJ).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.True(MsiUtils.IsPatchInstalled(patchA));

            // Uninstall the patch bundle and make sure the base bundle is restored.
            installerJ.Uninstall();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.False(MsiUtils.IsPatchInstalled(patchA));

            // Uninstall the base bundle and make sure all packages are uninstalled.
            installerF.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB2));
            Assert.False(MsiUtils.IsPatchInstalled(patchA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs two bundles with one bundle not requesting a shared package be installed, then uninstalls.")]
        [RuntimeTest]
        public void Burn_DifferentPackageRequestStates()
        {
            const string expectedVersion = "1.0.0.0";

            // Build the package.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the base bundle and make sure it's installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // SFBUG:3469206 - install a bundle without installing the shared package, which should not be ref-counted.
            this.SetPackageRequestedState("PackageA", RequestState.None);

            // Also don't install packageB since it has an authored dependency on packageA and would fail this test case.
            this.SetPackageRequestedState("PackageB", RequestState.None);

            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Uninstall the first bundle and make sure packageA is uninstalled.
            this.ResetPackageStates("PackageA");
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("Version") as string;
                Assert.Equal(expectedVersion, actualVersion);
            }

            // Uninstall the second bundle and make sure all packages are uninstalled.
            installerB.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                Assert.Null(root.GetValue("Version"));
            }

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installed bundle v1 with per-user and per-machine packages, then upgrades only the per-user package.")]
        [RuntimeTest]
        public void Burn_MixedScopeUpgradedBundle()
        {
            const string upgradeVersion = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageD1 = new PackageBuilder(this, "D") { Extensions = Extensions }.Build().Output;
            string packageD2 = new PackageBuilder(this, "D") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", upgradeVersion } } }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageD", packageD1);

            // Build the base bundle.
            string bundleH1 = new BundleBuilder(this, "BundleH") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Override the path for D1 to D2 and build the upgrade bundle.
            bindPaths["packageD"] = packageD2;
            string bundleH2 = new BundleBuilder(this, "BundleH") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", upgradeVersion } } }.Build().Output;

            // Install the base bundle.
            BundleInstaller installerH1 = new BundleInstaller(this, bundleH1).Install();

            // Make sure the MSIs are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageD1));

            Assert.True(LogVerifier.MessageInLogFileRegex(installerH1.LastLogFile, @"Skipping cross-scope dependency registration on package: PackageA, bundle scope: PerUser, package scope: PerMachine"));

            // Install the upgrade bundle. Verify the base bundle was removed.
            BundleInstaller installerH2 = new BundleInstaller(this, bundleH2).Install();

            // Verify packageD2 was installed and packageD1 was uninstalled.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageD2));

            Assert.True(LogVerifier.MessageInLogFileRegex(installerH2.LastLogFile, @"Skipping cross-scope dependency registration on package: PackageA, bundle scope: PerUser, package scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerH2.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerUser, version: 1\.0\.0\.0, operation: MajorUpgrade"));

            // Uninstall the upgrade bundle now.
            installerH2.Uninstall();

            // Verify that permanent packageA is still installed and then remove.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            MSIExec.UninstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);

            // Make sure the MSIs were uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD2));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs an upgrade bundle that contains the same patch as a previous bundle version and an additional patch.")]
        [RuntimeTest]
        public void Burn_InstallPatchBundleUpgrade()
        {
            const string expectedVersion1 = "1.0.1.0";
            const string expectedVersion2 = "1.0.2.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion1 } }, NeverGetsInstalled = true }.Build().Output;
            string packageA3 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } }, NeverGetsInstalled = true }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion1 } }, TargetPath = packageA1, UpgradePath = packageA2 }.Build().Output;
            string patchB = new PatchBuilder(this, "PatchB") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } }, TargetPath = packageA1, UpgradePath = packageA3 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageB", packageB);
            bindPaths.Add("patchA", patchA);
            bindPaths.Add("patchB", patchB);

            // Build the bundles.
            string bundleF = new BundleBuilder(this, "BundleF") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleG = new BundleBuilder(this, "BundleG") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion1 } } }.Build().Output;
            string bundleI = new BundleBuilder(this, "BundleI") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", expectedVersion2 } } }.Build().Output;

            // Install the base bundle and make sure all packages are installed.
            BundleInstaller installerF = new BundleInstaller(this, bundleF).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Install patch bundle and make sure all packages are installed.
            BundleInstaller installerG = new BundleInstaller(this, bundleG).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            Assert.True(MsiUtils.IsPatchInstalled(patchA));

            // Install patch bundle upgrade and make sure all packages are installed.
            BundleInstaller installerI = new BundleInstaller(this, bundleI).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            Assert.True(MsiUtils.IsPatchInstalled(patchA));
            Assert.True(MsiUtils.IsPatchInstalled(patchB));

            // Uninstall the base bundle and make sure all packages are uninstalled.
            installerF.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.False(MsiUtils.IsPatchInstalled(patchA));
            Assert.False(MsiUtils.IsPatchInstalled(patchB));

            this.Complete();
        }
    }
}
