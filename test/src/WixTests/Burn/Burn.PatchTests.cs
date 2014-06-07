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
    using System.Linq;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.Win32;
    using Xunit;

    public class PatchTests : BurnTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with slipstream then removes it.")]
        [RuntimeTest]
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
                Assert.Equal(originalVersion, actualVersion);
            }

            // Install the patch bundle.
            BundleInstaller installAPatch = new BundleInstaller(this, bundleAPatch).Install();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Uninstall the patch bundle.
            installAPatch.Uninstall();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(originalVersion, actualVersion);
            }

            installA.Uninstall();
            Assert.True(null == this.GetTestRegistryRoot(), "Test registry key should have been removed during uninstall.");

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs package then installs a bundle with two patches that target the package and removes it all.")]
        [RuntimeTest]
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
                Assert.Equal(originalVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.Equal(originalVersion, actualVersion);
            }

            // Install the bundle of patches and ensure all the registry keys are updated.
            BundleInstaller installPatches = new BundleInstaller(this, bundlePatch).Install();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(patchedVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.Equal(patchedVersion, actualVersion);
            }

            // Uninstall the patch bundle and verify the keys go back to original values.
            installPatches.Uninstall();
            using (RegistryKey root = this.GetTestRegistryRoot())
            {
                string actualVersion = root.GetValue("A") as string;
                Assert.Equal(originalVersion, actualVersion);

                actualVersion = root.GetValue("A2") as string;
                Assert.Equal(originalVersion, actualVersion);
            }

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs patch bundle with repeated Detect phases.")]
        [RuntimeTest]
        public void Burn_PatchRedetect()
        {
            this.SetRedetectCount(1);
            this.Burn_PatchInstallUninstall();
        }

        [NamedFact]
        [Priority(2)]
        [Description("Installs bundle with SWID tag that is patched.")]
        [RuntimeTest]
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
            Assert.Equal(originalVersion, actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.Equal(originalVersion, actualVersion);

            // Install the patch bundle.
            BundleInstaller installAPatch = new BundleInstaller(this, bundleAPatch).Install();
            actualVersion = GetTagVersion("~Burn_PatchTag - Patch Bundle A");
            Assert.Equal(patchedVersion, actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.Equal(patchedVersion, actualVersion);

            // Uninstall the patch bundle.
            installAPatch.Uninstall();
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.Equal(originalVersion, actualVersion);

            // Uninstall the original bundle and ensure all the tags are gone.
            installA.Uninstall();
            actualVersion = GetTagVersion("~Burn_PatchTag - Bundle A");
            Assert.Null(actualVersion);
            actualVersion = GetTagVersion("~Burn_PatchTag - A");
            Assert.Null(actualVersion);

            this.Complete();
        }

        [NamedFact]
        [Priority(2)]
        public void Burn_BuildNonSpecificPatches()
        {
            string patchedVersion = "1.0.1.0";
            string[] extensions = new string[] { "WixBalExtension", "WixTagExtension", };

            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = extensions }.Build().Output;
            string packageAUpdate = new PackageBuilder(this, "A") { Extensions = extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } }, NeverGetsInstalled = true }.Build().Output;
            string patchA = new PatchBuilder(this, "PatchA") { Extensions = extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;
            string patchB = new PatchBuilder(this, "PatchB") { Extensions = extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;
            string patchC = new PatchBuilder(this, "PatchC") { Extensions = extensions, TargetPath = packageA, UpgradePath = packageAUpdate }.Build().Output;

            // Build the bundles.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("patchA", patchA);
            bindPaths.Add("patchB", patchB);
            bindPaths.Add("patchC", patchC);

            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = extensions }.Build().Output;
            BundleBuilder bundleAPatch = new BundleBuilder(this, "PatchBundleA") { BindPaths = bindPaths, Extensions = extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } } }.Build();
            BundleBuilder bundleBPatch = new BundleBuilder(this, "PatchBundleB") { BindPaths = bindPaths, Extensions = extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } } }.Build();
            BundleBuilder bundleCPatch = new BundleBuilder(this, "PatchBundleC") { BindPaths = bindPaths, Extensions = extensions, PreprocessorVariables = new Dictionary<string, string>() { { "Version", patchedVersion } } }.Build();

            // Disassemble the patch bundles and check for PatchTargetCode elements.
            XmlNodeList nodes = PatchTests.GetPatchTargetCodes(bundleAPatch);
            Assert.Equal(1, nodes.Count);
            Assert.True(nodes.OfType<XmlElement>().Any(elem => elem.HasAttribute("Product") && "yes".Equals(elem.Attributes["Product"].Value)));

            nodes = PatchTests.GetPatchTargetCodes(bundleBPatch);
            Assert.Equal(2, nodes.Count);
            Assert.True(nodes.OfType<XmlElement>().Any(elem => elem.HasAttribute("Product") && "yes".Equals(elem.Attributes["Product"].Value)));
            Assert.True(nodes.OfType<XmlElement>().Any(elem => elem.HasAttribute("Product") && "no".Equals(elem.Attributes["Product"].Value)));

            nodes = PatchTests.GetPatchTargetCodes(bundleCPatch);
            Assert.Equal(0, nodes.Count);

            this.Complete();
        }

        private static XmlNodeList GetPatchTargetCodes(BundleBuilder bundle)
        {
            string path = Path.Combine(bundle.Disassemble(), @"UX\manifest.xml");

            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("w", "http://schemas.microsoft.com/wix/2008/Burn");

            return doc.SelectNodes("/w:BurnManifest/w:PatchTargetCode", nsmgr);
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
