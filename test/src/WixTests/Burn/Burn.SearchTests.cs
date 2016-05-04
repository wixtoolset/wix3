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

    public class SearchTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Tests all possible results for a ComponentSearch.")]
        [RuntimeTest]
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
            Assert.True(MsiVerifier.IsPackageInstalled(packageA));
            Assert.True(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall the main bundle and add-on.
            installerA.Uninstall();

            this.Complete();
        }
    }
}
