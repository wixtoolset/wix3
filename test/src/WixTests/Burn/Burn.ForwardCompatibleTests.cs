//-----------------------------------------------------------------------
// <copyright file="Burn.ForwardCompatibleTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods test Burn failure scenarios.
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

    public class ForwardCompatibleTests : BurnTests
    {
        const string V2 = "2.0.0.0";

        private WixTest.PackageBuilder packageA;
        private WixTest.PackageBuilder packageAv2;
        private WixTest.PackageBuilder packageB;
        private WixTest.PackageBuilder packageC;
        private WixTest.PackageBuilder packageCv2;
        private WixTest.BundleBuilder bundleA;
        private WixTest.BundleBuilder bundleAv2;
        private WixTest.BundleBuilder bundleB;
        private WixTest.BundleBuilder bundleC;
        private WixTest.BundleBuilder bundleCv2;

        [NamedFact]
        [Priority(2)]
        [Description("Installs v2 of a bundle then does a passthrough install and uninstall of v1 with parent.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleInstallV1UninstallV1()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v2 bundle.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);

            // Install the v1 bundle with a parent which should passthrough to v2.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with the same parent which should passthrough to v2 and remove parent.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and all should be removed.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v2 of a bundle then does a passthrough install v1 with parent then uninstall of v2.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleInstallV1UninstallV2()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v2 bundle.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);

            // Install the v1 bundle with a parent which should passthrough to v2.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));


            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v1 of a bundle with a parent then upgrades it then uninstalls without parent then actually uninstalls with parent.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleMajorUpgrade()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v1 bundle with a parent.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v1 of a bundle with two parents then upgrades it then uninstalls twice to actually uninstall.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleParentTwiceMajorUpgrade()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parent2 = "~BundleAv1_Parent2";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);

            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v1 bundle with a parent.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Install the v1 bundle with a second parent.
            installerAv1.Install(arguments: parent2Switch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            installerAv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent2));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            installerAv1.Uninstall(arguments: parent2Switch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v1 of a bundle with two parents then upgrades it with a third parent then uninstalls thrice to actually uninstall.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleParentThriceMajorUpgrade()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parent2 = "~BundleAv1_Parent2";
            string parent3 = "~BundleAv1_Parent3";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);
            string parent3Switch = String.Concat("-parent ", parent3);

            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v1 bundle with a parent.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));
            Assert.False(this.DependencyDependentExists(providerId, parent2));
            Assert.False(this.DependencyDependentExists(providerId, parent3));

            // Install the v1 bundle with a second parent.
            installerAv1.Install(arguments: parent2Switch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));
            Assert.True(this.DependencyDependentExists(providerId, parent2));
            Assert.False(this.DependencyDependentExists(providerId, parent3));

            // Upgrade with the v2 bundle.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install(arguments: parent3Switch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));
            Assert.True(this.DependencyDependentExists(providerId, parent2));
            Assert.True(this.DependencyDependentExists(providerId, parent3));

            // Uninstall the v2 bundle and nothing should happen because there are still two other parents.
            installerAv2.Uninstall(arguments: parent3Switch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));
            Assert.True(this.DependencyDependentExists(providerId, parent2));
            Assert.False(this.DependencyDependentExists(providerId, parent3));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.False(this.DependencyDependentExists(providerId, parent));
            Assert.True(this.DependencyDependentExists(providerId, parent2));
            Assert.False(this.DependencyDependentExists(providerId, parent3));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            installerAv1.Uninstall(arguments: parent2Switch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v1 of a per-user bundle with two parents then upgrades it then uninstalls twice to actually uninstall.")]
        [RuntimeTest]
        public void Burn_ForwardCompatiblePerUserParentTwiceMajorUpgrade()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleC");
            string parent = "~BundleCv1";
            string parent2 = "~BundleCv1_Parent2";
            string parentSwitch = String.Concat("-parent ", parent);
            string parent2Switch = String.Concat("-parent ", parent2);

            // Build.
            string packageC = this.GetPackageC().Output;
            string packageCv2 = this.GetPackageCv2().Output;

            string bundleC = this.GetBundleC().Output;
            string bundleCv2 = this.GetBundleCv2().Output;

            // Install the v1 bundle with a parent.
            BundleInstaller installerCv1 = new BundleInstaller(this, bundleC).Install(arguments: parentSwitch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageCv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Install the v1 bundle with a second parent.
            installerCv1.Install(arguments: parent2Switch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageCv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle.
            BundleInstaller installerCv2 = new BundleInstaller(this, bundleCv2).Install();
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.True(MsiVerifier.IsPackageInstalled(packageCv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v2 bundle and nothing should happen because there is still a parent.
            installerCv2.Uninstall();
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.True(MsiVerifier.IsPackageInstalled(packageCv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall one parent of the v1 bundle and nothing should happen because there is still a parent.
            installerCv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.True(MsiVerifier.IsPackageInstalled(packageCv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent2));

            // Uninstall the v1 bundle with passthrough with second parent and all should be removed.
            installerCv1.Uninstall(arguments: parent2Switch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageC));
            Assert.False(MsiVerifier.IsPackageInstalled(packageCv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs v1 of a bundle with a parent then upgrades with parent:none then successfully uninstalls with A's parent.")]
        [RuntimeTest]
        public void Burn_ForwardCompatibleMajorUpgradeParentNone()
        {
            string providerId = String.Concat("~", this.TestContext.TestName, "_BundleA");
            string parent = "~BundleAv1";
            string parentSwitch = String.Concat("-parent ", parent);

            // Build.
            string packageA = this.GetPackageA().Output;
            string packageAv2 = this.GetPackageAv2().Output;

            string bundleA = this.GetBundleA().Output;
            string bundleAv2 = this.GetBundleAv2().Output;

            // Install the v1 bundle with a parent.
            BundleInstaller installerAv1 = new BundleInstaller(this, bundleA).Install(arguments: parentSwitch);
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            string actualProviderVersion;
            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal("1.0.0.0", actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Upgrade with the v2 bundle but prevent self parent being registered.
            BundleInstaller installerAv2 = new BundleInstaller(this, bundleAv2).Install(arguments: "-parent:none");
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.True(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));
            Assert.Equal(V2, actualProviderVersion);
            Assert.True(this.DependencyDependentExists(providerId, parent));

            // Uninstall the v1 bundle with passthrough and all should be removed.
            installerAv1.Uninstall(arguments: parentSwitch);
            Assert.False(MsiVerifier.IsPackageInstalled(packageA));
            Assert.False(MsiVerifier.IsPackageInstalled(packageAv2));

            Assert.False(this.TryGetDependencyProviderValue(providerId, "Version", out actualProviderVersion));

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

        private WixTest.PackageBuilder GetPackageB()
        {
            if (null == this.packageB)
            {
                this.packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build();
            }

            return this.packageB;
        }

        private WixTest.BundleBuilder GetBundleB(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageBPath = this.GetPackageB().Output;
                bindPaths = new Dictionary<string, string>() { { "packageB", packageBPath } };
            }

            if (null == this.bundleB)
            {
                this.bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleB;
        }

        private WixTest.PackageBuilder GetPackageC()
        {
            if (null == this.packageC)
            {
                this.packageC = new PackageBuilder(this, "C") { Extensions = Extensions }.Build();
            }

            return this.packageC;
        }

        private WixTest.PackageBuilder GetPackageCv2()
        {
            if (null == this.packageCv2)
            {
                this.packageCv2 = new PackageBuilder(this, "C") { Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
            }

            return this.packageCv2;
        }

        private WixTest.BundleBuilder GetBundleC(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageCPath = this.GetPackageC().Output;
                bindPaths = new Dictionary<string, string>() { { "packageC", packageCPath } };
            }

            if (null == this.bundleC)
            {
                this.bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build();
            }

            return this.bundleC;
        }

        private WixTest.BundleBuilder GetBundleCv2(Dictionary<string, string> bindPaths = null)
        {
            if (null == bindPaths)
            {
                string packageCPath = this.GetPackageCv2().Output;
                bindPaths = new Dictionary<string, string>() { { "packageC", packageCPath } };
            }

            if (null == this.bundleCv2)
            {
                this.bundleCv2 = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", V2 } } }.Build();
            }

            return this.bundleCv2;
        }
    }
}
