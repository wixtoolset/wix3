//-----------------------------------------------------------------------
// <copyright file="Burn.PermanentTests.cs" company="Outercurve Foundation">
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Deployment.WindowsInstaller;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Microsoft.Win32;
    using Xunit;

    public class PermanentTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then removes it and ensures MSIs are still present.")]
        [RuntimeTest]
        public void Burn_PermanentInstallUninstall()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install Bundle A.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall bundleA.
            installerA.Uninstall();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then force uninstalls it.")]
        [RuntimeTest]
        public void Burn_PermanentInstallForceUninstall()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install Bundle A.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Force Uninstall Bundle A.
            this.SetPackageRequestedState("PackageA", Microsoft.Tools.WindowsInstallerXml.Bootstrapper.RequestState.ForceAbsent);
            this.SetPackageRequestedState("PackageB", Microsoft.Tools.WindowsInstallerXml.Bootstrapper.RequestState.ForceAbsent);
            installerA.Uninstall();

            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then uninstalls it then force uninstalls it.")]
        [RuntimeTest]
        public void Burn_PermanentInstallUninstallForceUninstall()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install Bundle A.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall Bundle A.
            installerA.Uninstall();

            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));

            // Force Uninstall Bundle A.
            this.SetPackageRequestedState("PackageA", Microsoft.Tools.WindowsInstallerXml.Bootstrapper.RequestState.ForceAbsent);
            this.SetPackageRequestedState("PackageB", Microsoft.Tools.WindowsInstallerXml.Bootstrapper.RequestState.ForceAbsent);
            installerA.Uninstall();

            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB));

            this.Complete();
        }
    }
}
