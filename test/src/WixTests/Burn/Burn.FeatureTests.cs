//-----------------------------------------------------------------------
// <copyright file="Burn.FeatureTests.cs" company="Outercurve Foundation">
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

    public class FeatureTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs a bundle and controls the feature state for install/uninstall.")]
        [RuntimeTest]
        public void Burn_FeatureInstallUninstall()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            BundleInstaller install = new BundleInstaller(this, bundleA).Install();

            // Source file should *not* be installed, main registry key should be present.
            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should not have found Package A payload installed at: ", packageSourceCodeInstalled));
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal("1.0.0.0", actualVersion);
            }

            // Now turn on the feature.
            this.SetPackageRequestedState("PackageA", RequestState.Present);
            this.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            install.Modify();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A payload installed at: ", packageSourceCodeInstalled));
            this.ResetPackageStates("PackageA");

            // Turn the feature back off.
            this.SetPackageRequestedState("PackageA", RequestState.Present);
            this.SetPackageFeatureState("PackageA", "Test", FeatureState.Absent);
            install.Modify();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Should have removed Package A payload from: ", packageSourceCodeInstalled));
            this.ResetPackageStates("PackageA");

            // Uninstall everything.
            install.Uninstall();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with a feature then repairs it.")]
        [RuntimeTest]
        public void Burn_FeatureRepair()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundle with the optional feature present
            this.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            BundleInstaller install = new BundleInstaller(this, bundleA).Install();
            this.ResetPackageStates("PackageA");

            string packageSourceCodeInstalled = this.GetTestInstallFolder(@"A\A.wxs");
            string packageNotKeyPathFile = this.GetTestInstallFolder(@"A\notkeypath.file");

            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A non-keyfile installed at: ", packageSourceCodeInstalled));

            // Delete the non-keypath source file.
            File.Delete(packageNotKeyPathFile);

            // Now repair without repairing the feature to verify the non-keyfile doesn't come back.
            install.Repair();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.False(File.Exists(packageNotKeyPathFile), String.Concat("Should have not found Package A non-keyfile installed at: ", packageNotKeyPathFile));

            // Now repair and include the feature this time.
            this.SetPackageFeatureState("PackageA", "Test", FeatureState.Local);
            install.Repair();
            Assert.True(File.Exists(packageSourceCodeInstalled), String.Concat("Should have found Package A keyfile installed at: ", packageSourceCodeInstalled));
            Assert.True(File.Exists(packageNotKeyPathFile), String.Concat("Should have repaired Package A non-keyfile installed at: ", packageNotKeyPathFile));
            this.ResetPackageStates("PackageA");

            // Uninstall everything.
            install.Uninstall();
            Assert.False(File.Exists(packageSourceCodeInstalled), String.Concat("Package A payload should have been removed by uninstall from: ", packageSourceCodeInstalled));
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }
    }
}
