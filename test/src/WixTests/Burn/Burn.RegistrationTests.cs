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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;
    using WixTest.Verifiers;

    [TestClass]
    public class RegistrationTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Minimal authoring for AdditionalRegistration")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_MimimalAdditionalRegistration()
        {
            // Build the bundle.
            string bundleA = new BundleBuilder(this, "BundleA") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\~Burn_MimimalAdditionalRegistration - Bundle A"))
            {
                Assert.AreEqual("Y", key.GetValue("ThisVersionInstalled"));
                Assert.AreEqual("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.AreEqual("Update", key.GetValue("ReleaseType"));
            }

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Minimal authoring for AdditionalRegistration with ProductFamily inherited.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_MinimalAdditionalRegistrationWithProductFamily()
        {
            // Build the bundle.
            string bundleB = new BundleBuilder(this, "BundleB") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerB = new BundleInstaller(this, bundleB).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft Corporation\Updates\Visual Studio 11\~Burn_MinimalAdditionalRegistrationWithProductFamily - Bundle B"))
            {
                Assert.AreEqual("Y", key.GetValue("ThisVersionInstalled"));
                Assert.AreEqual("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.AreEqual("Update", key.GetValue("ReleaseType"));
            }

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("All attributes authored for AdditionalRegistration")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_MaximumAdditionalRegistration()
        {
            // Build the bundle.
            string bundleC = new BundleBuilder(this, "BundleC") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            // Install the bundle.
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();

            // Make sure the registry exists.
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Updates\Visual Studio 11\KB1234567"))
            {
                Assert.AreEqual("Y", key.GetValue("ThisVersionInstalled"));
                Assert.AreEqual("Microsoft Corporation", key.GetValue("Publisher"));
                Assert.AreEqual("Developer Division", key.GetValue("PublishingGroup"));
                Assert.AreEqual("Service Pack", key.GetValue("ReleaseType"));
            }

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("No attributes are authored and required attributes not inherited.")]
        [TestProperty("IsRuntimeTest", "true")]
        [ExpectedException(typeof(TestException))]
        public void Burn_MissingAttributesForAddditionalRegistration()
        {
            // Build the bundle.
            string bundleD = new BundleBuilder(this, "BundleD") { Extensions = Extensions, AdditionalSourceFiles = this.AdditionalSourceFiles }.Build().Output;

            this.CleanTestArtifacts = true;
        }

        private string[] AdditionalSourceFiles
        {
            get
            {
                return new string[]
                {
                    Path.Combine(this.TestDataDirectory2, "TestExe.wxs"),
                };
            }
        }
    }
}
