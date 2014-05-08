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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class OriginalSourceBundleTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A, bundle A has embedded bundle B, then uninstalls bundle A.")]
        [TestProperty("IsRuntimeTest", "true")]
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
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageB1));

            // Attempt to uninstall bundleA.
            installerA.Uninstall();

            // Test package is uninstalled.
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA1));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB1));

            this.CleanTestArtifacts = true;
        }
    }
}
