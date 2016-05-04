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

    public class RelatedBundleTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, patch bundle D, upgrades patch bundle D, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallPatchRelatedBundle()
        {
            const string patchVersion1 = "1.0.1.0";
            const string patchVersion2 = "1.0.2.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A").Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, NeverGetsInstalled = true }.Build().Output;
            string packageA3 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, NeverGetsInstalled = true }.Build().Output;
            string patchA1 = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, TargetPath = packageA1, UpgradePath = packageA2 }.Build().Output;
            string patchA2 = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, TargetPath = packageA1, UpgradePath = packageA3 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("patchA", patchA1);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleD1 = new BundleBuilder(this, "BundleD") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            bindPaths["patchA"] = patchA2;
            string bundleD2 = new BundleBuilder(this, "BundleD") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerD1 = new BundleInstaller(this, bundleD1).Install();

            // Test both packages are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchVersion1, actualVersion);
            }

            // Install the patch upgrade bundle.
            BundleInstaller installerD2 = new BundleInstaller(this, bundleD2).Install();

            // Test the package is upgraded but that bundle A is not repaired.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerD2.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Dependent, scope: PerMachine, version: 1\.0\.0\.0, operation: None"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerD2.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerMachine, version: 1\.0\.1\.0, operation: MajorUpgrade"));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchVersion2, actualVersion);
            }

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that uninstalling bundle A detected and would remove bundle D.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.2\.0, operation: Remove"));

            // Test both packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, patch bundle D, bundle B which should reapply bundle D, then uninstalls bundles A and B.")]
        [RuntimeTest]
        public void Burn_InstallUninstallStickyPatchRelatedBundle()
        {
            const string patchVersion = "1.0.1.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A").Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, NeverGetsInstalled = true }.Build().Output;
            string packageB1 = new PackageBuilder(this, "B").Build().Output;
            string packageB2 = new PackageBuilder(this, "B") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA")
            {
                PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } },
                TargetPaths = new string[] { packageA1, packageB1 },
                UpgradePaths = new string[] { packageA2, packageB2 }
            }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageB", packageB1);
            bindPaths.Add("patchA", patchA);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleD = new BundleBuilder(this, "BundleD") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerD = new BundleInstaller(this, bundleD).Install();

            // Make sure that bundle D detected dependent bundle A.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerD.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Dependent, scope: PerMachine, version: 1\.0\.0\.0, operation: None"));

            // Test that packageA1 and patchA are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchVersion, actualVersion);
            }

            // Install bundle B (tests sticky patching).
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.1\.0, operation: Install"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Detect, scope: PerMachine, version: 1\.0\.0\.0, operation: None"));

            // Test that packageB and patchA are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("B") as string;
                Assert.Equal(patchVersion, actualVersion);
            }

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that packageA is still installed (ref-counted).
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));

            // Test that uninstalling bundle A detected bundle D (ref-counted).
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.1\.0, operation: Remove"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Will not uninstall package: \{[0-9A-Za-z\-]{36}\}, found dependents: 1"));

            // Attempt to uninstall bundleB.
            installerB.Uninstall();

            // Test that all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));
            Assert.Null(this.GetTestRegistryRoot());

            // Test that uninstalling bundle B detected and removed bundle D (ref-counted).
            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.1\.0, operation: Remove"));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, addon bundle C, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallAddonRelatedBundle()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageC = new PackageBuilder(this, "C").Build().Output;
            string packageD = new PackageBuilder(this, "D").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageC", packageC);
            bindPaths.Add("packageD", packageD);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Test that packages A and C but not D are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that uninstalling bundle A detected and would remove bundle C.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Remove"));

            // Test that all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, addon bundle C, bundle B which should reapply bundle C, then uninstalls bundles A and B.")]
        [RuntimeTest]
        public void Burn_InstallUninstallStickyAddonRelatedBundle()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageB = new PackageBuilder(this, "B").Build().Output;
            string packageC = new PackageBuilder(this, "C").Build().Output;
            string packageD = new PackageBuilder(this, "D").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);
            bindPaths.Add("packageC", packageC);
            bindPaths.Add("packageD", packageD);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Make sure that bundle C detected dependent bundle A.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerC.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Dependent, scope: PerMachine, version: 1\.0\.0\.0, operation: None"));

            // Test that packages A and C but not D are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Install bundle B (tests sticky addons).
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Install"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Detect, scope: PerMachine, version: 1\.0\.0\.0, operation: None"));

            // Test that all packages are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.True(MsiVerifier.IsPackageInstalled(packageD));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that packageA is still installed (ref-counted).
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Test that uninstalling bundle A detected bundle C (ref-counted).
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Remove"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Will not uninstall package: \{[0-9A-Za-z\-]{36}\}, found dependents: 1"));

            // Attempt to uninstall bundleB.
            installerB.Uninstall();

            // Test that all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Test that uninstalling bundle B detected and removed bundle C (ref-counted).
            Assert.True(LogVerifier.MessageInLogFileRegex(installerB.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Remove"));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, patch bundle D, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallUpgradePatchRelatedBundleWithAddon()
        {
            const string patchVersion1 = "1.0.1.0";
            const string patchVersion2 = "1.0.2.0";

            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A").Build().Output;
            string packageA2 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, NeverGetsInstalled = true }.Build().Output;
            string packageA3 = new PackageBuilder(this, "A") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, NeverGetsInstalled = true }.Build().Output;
            string packageC = new PackageBuilder(this, "C").Build().Output;
            string packageD = new PackageBuilder(this, "D").Build().Output;
            string patchA1 = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, TargetPath = packageA1, UpgradePath = packageA2 }.Build().Output;
            string patchA2 = new PatchBuilder(this, "PatchA") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, TargetPath = packageA1, UpgradePath = packageA3 }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageC", packageC);
            bindPaths.Add("packageD", packageD);
            bindPaths.Add("patchA", patchA1);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleD1 = new BundleBuilder(this, "BundleD") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion1 } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Build the v2 patch bundle.
            bindPaths["patchA"] = patchA2;
            string bundleD2 = new BundleBuilder(this, "BundleD") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchVersion2 } }, BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerD1 = new BundleInstaller(this, bundleD1).Install();

            // Test both packages are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchVersion1, actualVersion);
            }

            // Install the addon bundle.
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Test that package C but not D is installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Install the v2 patch bundles.
            BundleInstaller installerD2 = new BundleInstaller(this, bundleD2).Install();

            // Test that all packages but D are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchVersion2, actualVersion);
            }

            // Test that installing D2 upgrades D1.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerD2.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Upgrade, scope: PerMachine, version: 1\.0\.1\.0, operation: MajorUpgrade"));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that uninstalling bundle A detected and would remove bundles C and D.
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Remove"));
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Patch, scope: PerMachine, version: 1\.0\.2\.0, operation: Remove"));

            // Test all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));
            Assert.Null(this.GetTestRegistryRoot());

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, addon bundle C, patches bundle C with bundle E, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallAddonPatchRelatedBundle()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;
            string packageC1 = new PackageBuilder(this, "C").Build().Output;
            string packageC2 = new PackageBuilder(this, "C") { PreprocessorVariables = new Dictionary<string, string>() { { "Version", "1.0.1.0" } } }.Build().Output;
            string packageD = new PackageBuilder(this, "D").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageC", packageC1);
            bindPaths.Add("packageD", packageD);

            // Build the base and addon bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Update path to C2 and build the addon patch bundle.
            bindPaths["packageC"] = packageC2;
            string bundleE = new BundleBuilder(this, "BundleE") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", "1.0.1.0" } } }.Build().Output;

            // Install the base and addon bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Test that packages A and C1 but not D are installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC2));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Install the patch to the addon.
            BundleInstaller installerE = new BundleInstaller(this, bundleE).Install();

            // Test that packages A and C2 but not D are installed, and that C1 was upgraded.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageC2));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test that uninstalling bundle A detected and removed bundle C, which removed bundle E (can't easily reference log).
            Assert.True(LogVerifier.MessageInLogFileRegex(installerA.LastLogFile, @"Detected related bundle: \{[0-9A-Za-z\-]{36}\}, type: Addon, scope: PerMachine, version: 1\.0\.0\.0, operation: Remove"));

            // Test that all packages are uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageC2));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            this.Complete();
        }
    }
}
