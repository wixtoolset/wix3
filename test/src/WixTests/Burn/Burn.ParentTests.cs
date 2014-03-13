//-----------------------------------------------------------------------
// <copyright file="Burn.ParentTests.cs" company="Outercurve Foundation">
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class ParentTests : BurnTests
    {
        const string V2 = "2.0.0.0";

        private PackageBuilder packageA;
        private PackageBuilder packageAv2;
        private PackageBuilder packageD;
        private BundleBuilder bundleA;
        private BundleBuilder bundleAv2;
        private BundleBuilder bundleD;

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A with a parent then uninstalls without parent then uninstalls with parent.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ParentInstallUninstallParentUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle without a parent and ensure it is still installed.
            installerA.Uninstall();
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle with the parent and ensure it is removed.
            installerA.Uninstall(arguments: "-parent Foo");
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A with a parent then install with a second parent then uninstalls twice for both parents to clean up.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ParentInstallTwiceThenUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerAFoo = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Install the bundle with a second parent, and ensure it is installed.
            BundleInstaller installerABar = new BundleInstaller(this, bundleA).Install(arguments: "-parent Bar");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle with first parent and ensure it is still installed.
            installerAFoo.Uninstall(arguments: "-parent Foo");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle with the second parent and ensure it is removed.
            installerABar.Uninstall(arguments: "-parent Bar");
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A with a self dependency parent then uninstalls using parent:none then successfully uninstalls.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ParentSelfInstallNoneUninstallSelfUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle using parent:none and ensure it is still installed.
            installerA.Uninstall(arguments: "-parent:none");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle and ensure it is removed.
            installerA.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A with a parent then upgrades then uninstalls without parent and expects it was removed.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ParentMajorUpgrade()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;
            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageAv2));

            // Upgrade without a parent reference.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageAv2));

            // Attempt to uninstall v2 bundle and ensure it is was removed.
            installerAv2.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageAv2));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle A then installs bundle D with a parent as addon then uninstalls bundle A which leaves bundle D.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ParentInstallAddonUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;
            string packageD = this.GetPackageD().Output;
            string bundleD = this.GetBundleD().Output;

            // Install the base bundle, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));

            // Install the addon bundle, and ensure it is installed.
            BundleInstaller installerD = new BundleInstaller(this, bundleD).Install(arguments: "-parent Foo");
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageD));

            // Uninstall the base bundle and ensure it is removed but addon is still present.
            installerA.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageD));

            // Uninstall addon bundle with the parent and ensure everything is removed.
            installerD.Uninstall(arguments: "-parent Foo");
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageD));

            this.CleanTestArtifacts = true;
        }

        private PackageBuilder GetPackageA()
        {
            if (null == this.packageA)
            {
                this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
            }

            return this.packageA;
        }

        private PackageBuilder GetPackageAv2()
        {
            if (null == this.packageAv2)
            {
                this.packageAv2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
            }

            return this.packageAv2;
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
                this.bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleA;
        }

        private BundleBuilder GetBundleAv2(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageAPath = this.GetPackageAv2().Output;
                bindPaths = new Dictionary<string, string>() { { "packageA", packageAPath } };
            }

            if (null == this.bundleAv2)
            {
                this.bundleAv2 = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
            }

            return this.bundleAv2;
        }

        private PackageBuilder GetPackageD()
        {
            if (null == this.packageD)
            {
                this.packageD = new PackageBuilder(this, "D") { Extensions = Extensions }.Build();
            }

            return this.packageD;
        }

        private BundleBuilder GetBundleD(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageDPath = this.GetPackageD().Output;
                bindPaths = new Dictionary<string, string>() { { "packageD", packageDPath } };
            }

            if (null == this.bundleD)
            {
                this.bundleD = new BundleBuilder(this, "BundleD") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleD;
        }
    }
}
