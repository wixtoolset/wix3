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

    public class OriginalSourceBundleTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A, bundle A has embedded bundle B, then uninstalls bundle A.")]
        [RuntimeTest]
        public void Burn_InstallUninstallBundleWithEmbeddedBundle()
        {
            // Build the packages.
            string packageA1 = new PackageBuilder(this, "A").Build().Output;
            string packageB1 = new PackageBuilder(this, "B").Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA1);
            bindPaths.Add("packageB", packageB1);

            // Build the embedded bundle.
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Build the parent bundle
            bindPaths.Add("bundleB", bundleB);
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;


            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            // Test package is installed.
            Assert.True(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB1));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test package is uninstalled.
            Assert.False(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.False(MsiVerifier.IsPackageInstalled(packageB1));

            this.Complete();
        }
    }
}
