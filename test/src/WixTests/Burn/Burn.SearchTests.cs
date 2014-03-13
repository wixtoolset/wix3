//-----------------------------------------------------------------------
// <copyright file="Burn.SearchTests.cs" company="Outercurve Foundation">
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
    public class SearchTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Tests all possible results for a ComponentSearch.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_ComponentSearchResults()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Install the bundles.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // PackageB will be installed if all ComponentSearches worked.
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall the main bundle and add-on.
            installerA.Uninstall();

            this.CleanTestArtifacts = true;
        }
    }
}
