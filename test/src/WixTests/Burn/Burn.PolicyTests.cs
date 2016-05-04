// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Burn
{
    using System.Collections.Generic;
    using System.IO;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.Win32;
    using Xunit;

    public class PolicyTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle A using default settings, changes the package cache, and installs bundle B.")]
        [RuntimeTest]
        public void Burn_RedirectPackageCache()
        {
            const string PolicyName = "PackageCache";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundles.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;
            string bundleB = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            RegistryKey policy = Registry.LocalMachine.CreateSubKey(@"Software\Policies\WiX\Burn");
            string currentPackageCache = null;

            try
            {
                currentPackageCache = policy.GetValue(PolicyName) as string;

                // Install the first bundle using the default package cache.
                policy.DeleteValue(PolicyName);

                BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();
                Assert.True(MsiVerifier.IsPackageInstalled(packageA));

                // Install the second bundle which has a shared package using the redirected package cache.
                string path = Path.Combine(Path.GetTempPath(), "Package Cache");
                policy.SetValue(PolicyName, path);

                BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();
                Assert.True(MsiVerifier.IsPackageInstalled(packageA));
                Assert.True(MsiVerifier.IsPackageInstalled(packageB));

                // The first bundle should leave package A behind.
                installerA.Uninstall();

                // Now make sure that the second bundle removes packages from either cache directory.
                installerB.Uninstall();

                this.Complete();
            }
            finally
            {
                if (!string.IsNullOrEmpty(currentPackageCache))
                {
                    policy.SetValue(PolicyName, currentPackageCache);
                }
                else
                {
                    policy.DeleteValue(PolicyName);
                }

                policy.Dispose();
            }
        }
    }
}
