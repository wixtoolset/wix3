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

    public class ParentTests : BurnTests
    {
        const string V2 = "2.0.0.0";

        private WixTest.PackageBuilder packageA;
        private WixTest.PackageBuilder packageAv2;
        private WixTest.PackageBuilder packageD;
        private WixTest.BundleBuilder bundleA;
        private WixTest.BundleBuilder bundleAv2;
        private WixTest.BundleBuilder bundleD;

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A with a parent then uninstalls without parent then uninstalls with parent.")]
        [RuntimeTest]
        public void Burn_ParentInstallUninstallParentUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle without a parent and ensure it is still installed.
            installerA.Uninstall();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle with the parent and ensure it is removed.
            installerA.Uninstall(arguments: "-parent Foo");
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A with a parent then install with a second parent then uninstalls twice for both parents to clean up.")]
        [RuntimeTest]
        public void Burn_ParentInstallTwiceThenUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerAFoo = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Install the bundle with a second parent, and ensure it is installed.
            BundleInstaller installerABar = new BundleInstaller(this, bundleA).Install(arguments: "-parent Bar");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle with first parent and ensure it is still installed.
            installerAFoo.Uninstall(arguments: "-parent Foo");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle with the second parent and ensure it is removed.
            installerABar.Uninstall(arguments: "-parent Bar");
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A with a self dependency parent then uninstalls using parent:none then successfully uninstalls.")]
        [RuntimeTest]
        public void Burn_ParentSelfInstallNoneUninstallSelfUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;

            // Install the bundle, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Attempt to uninstall bundle using parent:none and ensure it is still installed.
            installerA.Uninstall(arguments: "-parent:none");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Uninstall bundle and ensure it is removed.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A with a parent then upgrades then uninstalls without parent and expects it was removed.")]
        [RuntimeTest]
        public void Burn_ParentMajorUpgrade()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;
            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the bundle with a parent, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(arguments: "-parent Foo");
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            // Upgrade without a parent reference.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            // Attempt to uninstall v2 bundle and ensure it is was removed.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A then installs bundle D with a parent as addon then uninstalls bundle A which leaves bundle D.")]
        [RuntimeTest]
        public void Burn_ParentInstallAddonUninstall()
        {
            // Build.
            string packageA = this.GetPackageA().Output;
            string bundleA = this.GetBundleA().Output;
            string packageD = this.GetPackageD().Output;
            string bundleD = this.GetBundleD().Output;

            // Install the base bundle, and ensure it is installed.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));

            // Install the addon bundle, and ensure it is installed.
            BundleInstaller installerD = new BundleInstaller(this, bundleD).Install(arguments: "-parent Foo");
            Assert.True(MsiVerifier.IsPackageInstalled(packageD));

            // Uninstall the base bundle and ensure it is removed but addon is still present.
            installerA.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageD));

            // Uninstall addon bundle with the parent and ensure everything is removed.
            installerD.Uninstall(arguments: "-parent Foo");
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageD));

            this.Complete();
        }

        private WixTest.PackageBuilder GetPackageA()
        {
            if (null == this.packageA)
            {
                this.packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build();
            }

            return this.packageA;
        }

        private WixTest.PackageBuilder GetPackageAv2()
        {
            if (null == this.packageAv2)
            {
                this.packageAv2 = new PackageBuilder(this, "A") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
            }

            return this.packageAv2;
        }

        private WixTest.BundleBuilder GetBundleA(Dictionary<string, string> bindPaths = null)
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

        private WixTest.BundleBuilder GetBundleAv2(Dictionary<string, string> bindPaths = null)
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

        private WixTest.PackageBuilder GetPackageD()
        {
            if (null == this.packageD)
            {
                this.packageD = new PackageBuilder(this, "D") { Extensions = Extensions }.Build();
            }

            return this.packageD;
        }

        private WixTest.BundleBuilder GetBundleD(Dictionary<string, string> bindPaths = null)
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
