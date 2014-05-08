//-----------------------------------------------------------------------
// <copyright file="Burn.BasicTests.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class EmbeddedTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A which installs bundle B as an embedded bundle, then removes A, which removes B.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_InstallUninstall()
        {
            string v2Version = "2.0.0.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageC = new PackageBuilder(this, "C") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageC", packageC);
            // Add the bindpath for the cab for C to enable to add it as a payload for BundleA
            bindPaths.Add("packageCcab", Path.Combine(Path.GetDirectoryName(packageC), "cab1.cab"));

            // Build the embedded bundle.
            string bundleBv2 = new BundleBuilder(this, "BundleBv2") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Build the parent bundle.
            bindPaths.Add("bundleBv2", bundleBv2);
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the parent bundle that will install the embedded bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageC));

            // Attempt to uninstall bundleA, which will uninstall bundleB since it is a patch related bundle.
            installerA.Uninstall();

            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageC));


            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle Bv1, then installs BundleA which installs bundle Bv2 as an embedded bundle which does a major upgrade of Bv1.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_InstallUninstallMajorUpgrade()
        {
            string v2Version = "2.0.0.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;
            string packageC = new PackageBuilder(this, "C") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);
            bindPaths.Add("packageC", packageC);
            // Add the bindpath for the cab for C to enable to add it as a payload for BundleA
            bindPaths.Add("packageCcab", Path.Combine(Path.GetDirectoryName(packageC), "cab1.cab"));

            // Build the embedded bundle and the earlier version of the bundle.
            string bundleBv1 = new BundleBuilder(this, "BundleBv1") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleBv2 = new BundleBuilder(this, "BundleBv2") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Build the parent bundle.
            bindPaths.Add("bundleBv2", bundleBv2);
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", v2Version } } }.Build().Output;

            // Install Bv1
            BundleInstaller installerBv1 = new BundleInstaller(this, bundleBv1).Install();
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageB));

            // Install the bundle containing the upgraded embedded bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageC));

            // Attempt to uninstall bundleA, which will uninstall bundleBv2 since it is a patch related bundle.
            installerA.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageC));

            this.CleanTestArtifacts = true;
        }
    }
}
