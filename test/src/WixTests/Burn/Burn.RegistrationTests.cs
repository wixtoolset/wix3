//-----------------------------------------------------------------------
// <copyright file="Burn.RegistrationTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Validations registration for various bundles.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Win32;
    using WixTest.Verifiers;
    using Xunit;

    public class RegistrationTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Minimal authoring for AdditionalRegistration")]
        [RuntimeTest]
        public void Burn_MimimalAdditionalRegistration()
        {
            // Build the bundle.
            string bundleA = new BundleBuilder(this, "BundleA") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_MimimalAdditionalRegistration - Bundle A"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.Equal("Update", key.GetValue("ReleaseType"));
            }

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Minimal authoring for AdditionalRegistration with ProductFamily inherited.")]
        [RuntimeTest]
        public void Burn_MinimalAdditionalRegistrationWithProductFamily()
        {
            // Build the bundle.
            string bundleB = new BundleBuilder(this, "BundleB") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\Visual Studio 11\~Burn_MinimalAdditionalRegistrationWithProductFamily - Bundle B"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.Equal("Update", key.GetValue("ReleaseType"));
            }

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("All attributes authored for AdditionalRegistration")]
        [RuntimeTest]
        public void Burn_MaximumAdditionalRegistration()
        {
            // Build the bundle.
            string bundleC = new BundleBuilder(this, "BundleC") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Updates\Visual Studio 11\KB1234567"))
            {
                Assert.Equal("Y", key.GetValue("ThisVersionInstalled"));
                Assert.Equal("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.Equal("Developer Division", key.GetValue("PublishingGroup"));
                Assert.Equal("Service Pack", key.GetValue("ReleaseType"));
            }

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("No attributes are authored and required attributes not inherited.")]
        [RuntimeTest]
        public void Burn_MissingAttributesForAddditionalRegistration()
        {
            Assert.Throws<TestException>(() =>
                {
                    // Build the bundle.
                    string bundleD = new BundleBuilder(this, "BundleD") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;
                });

            this.Complete();
        }

        private string[] AdditionalSourceFiles
        {
            get
            {
                return new string[]
                {
                    Path.Combine(this.TestContext.TestDataDirectory, "TestExe.wxs"),
                };
            }
        }
    }
}
