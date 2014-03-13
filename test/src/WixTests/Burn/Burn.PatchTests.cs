//-----------------------------------------------------------------------
// <copyright file="Burn.PatchTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Contains methods test Burn slipstreaming.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Burn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class PatchTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle with slipstream then removes it.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_PatchInstallUninstall()
        {
            string originalVersion = "1.0.0.0";
            string patchedVersion = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions }.Build().Output;
            string packageAUpdate = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { Extensions = WixTests.Extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("patchA", patchA);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = WixTests.Extensions }.Build().Output;
            string bundleAPatch = new BundleBuilder(this, "PatchBundleA") { BindPaths = bindPaths, Extensions = WixTests.Extensions }.Build().Output;

            // Install the unpatched bundle.
            BundleInstaller installA = new BundleInstaller(this, bundleA).Install();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(originalVersion, actualVersion);
            }

            // Install the patch bundle.
            BundleInstaller installAPatch = new BundleInstaller(this, bundleAPatch).Install();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(patchedVersion, actualVersion);
            }

            // Uninstall the patch bundle.
            installAPatch.Uninstall();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(originalVersion, actualVersion);
            }

            installA.Uninstall();
            Assert.IsNull(this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs package then installs a bundle with two patches that target the package and removes it all.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_PatchOnePackageTwoPatches()
        {
            string originalVersion = "1.0.0.0";
            string patchedVersion = "1.0.1.0";

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions }.Build().Output;
            string packageAUpdate = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { Extensions = WixTests.Extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;
            string patchA2 = new PatchBuilder(this, "PatchA2") { Extensions = WixTests.Extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("patchA", patchA);
            bindPaths.Add("patchA2", patchA2);

            string bundlePatch = new BundleBuilder(this, "PatchBundleA2") { BindPaths = bindPaths, Extensions = WixTests.Extensions }.Build().Output;

            // Install the original MSI and ensure the registry keys that get patched are as expected.
            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(originalVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.AreEqual(originalVersion, actualVersion);
            }

            // Install the bundle of patches and ensure all the registry keys are updated.
            BundleInstaller installPatches = new BundleInstaller(this, bundlePatch).Install();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(patchedVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.AreEqual(patchedVersion, actualVersion);
            }

            // Uninstall the patch bundle and verify the keys go back to original values.
            installPatches.Uninstall();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.AreEqual(originalVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.AreEqual(originalVersion, actualVersion);
            }

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs patch bundle with repeated Detect phases.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_PatchRedetect()
        {
            this.SetRedetectCount(1);
            this.Burn_PatchInstallUninstall();
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle with SWID tag that is patched.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_PatchTag()
        {
            string originalVersion = "1.0.0.0";
            string patchedVersion = "1.0.1.0";
            string actualVersion = null;

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions }.Build().Output;
            string packageAUpdate = new PackageBuilder(this, "A") { Extensions = WixTests.Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { Extensions = WixTests.Extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("patchA", patchA);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = WixTests.Extensions }.Build().Output;
            string bundleAPatch = new BundleBuilder(this, "PatchBundleA") { BindPaths = bindPaths, Extensions = WixTests.Extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } } }.Build().Output;

            // Install the unpatched bundle.
            BundleInstaller installA = new BundleInstaller(this, bundleA).Install();
            actualVersion = GetTagVersion("~Burn_PatchTag - Bundle A");
            Assert.AreEqual(originalVersion, actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.AreEqual(originalVersion, actualVersion);

            // Install the patch bundle.
            BundleInstaller installAPatch = new BundleInstaller(this, bundleAPatch).Install();
            actualVersion = GetTagVersion("~Burn_PatchTag - Patch Bundle A");
            Assert.AreEqual(patchedVersion, actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.AreEqual(patchedVersion, actualVersion);

            // Uninstall the patch bundle.
            installAPatch.Uninstall();
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.AreEqual(originalVersion, actualVersion);

            // Uninstall the original bundle and ensure all the tags are gone.
            installA.Uninstall();
            actualVersion = GetTagVersion("~Burn_PatchTag - Bundle A");
            Assert.IsNull(actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.IsNull(actualVersion);

            this.CleanTestArtifacts = true;
        }

        private static string GetTagVersion(string tagName)
        {
            string regidFolder = System.Environment.ExpandEnvironmentVariables(@"%ProgramData%\regid.1995-08.com.example");
            string tagPath = Path.Combine(regidFolder, "regid.1995-08.com.example " + tagName + ".swidtag");
            string version = null;

            if (File.Exists(tagPath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(tagPath);

                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("s", "http://standards.iso.org/iso/19770/-2/2009/schema.xsd");

                XmlNode versionNode = doc.SelectSingleNode("/s:software_identification_tag/s:product_version/s:name", ns);
                version = (null  == versionNode) ? String.Empty : versionNode.InnerText;
            }

            return version;
        }
    }
}
