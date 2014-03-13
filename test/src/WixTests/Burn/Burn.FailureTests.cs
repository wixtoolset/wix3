//-----------------------------------------------------------------------
// <copyright file="Burn.FailureTests.cs" company="Outercurve Foundation">
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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    [TestClass]
    public class FailureTests : BurnTests
    {
        [TestMethod]
        [Priority(2)]
        [Description("Cancels Package B very, very early.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_CancelVeryEarly()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundle.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Cancel package B right away.
            this.SetPackageCancelExecuteAtProgress("PackageB", 1);

            // Install the bundle and hopefully it fails.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(expectedExitCode: ErrorCodes.ERROR_INSTALL_USEREXIT);
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Cancels Package B very, very late.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_CancelVeryLate()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundle.
            string bundleA = new BundleBuilder(this, "BundleA") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Cancel package B at the last moment possible..
            this.SetPackageCancelExecuteAtProgress("PackageB", 100);

            // Install the bundle and hopefully it fails.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(expectedExitCode: ErrorCodes.ERROR_INSTALL_USEREXIT);
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Cancels Package A being executed while Package B still being cached.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_CancelExecuteWhileCaching()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundle.
            string bundleA = new BundleBuilder(this, "BundleB") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Slow the caching of package B to ensure that package A starts installing and cancels.
            this.SetPackageCancelExecuteAtProgress("PackageA", 50);
            this.SetPackageSlowCache("PackageB", 1000);

            // Install the bundle and hopefully it fails.
            BundleInstaller installerA = new BundleInstaller(this, bundleA).Install(expectedExitCode: ErrorCodes.ERROR_INSTALL_USEREXIT);
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB));

            this.CleanTestArtifacts = true;
        }

        [TestMethod]
        [Priority(2)]
        [Description("Installs bundle C with missing non-vital package successfully.")]
        [TestProperty("IsRuntimeTest", "true")]
        public void Burn_MissingNonVitalPackage()
        {
            // Build the packages.
            string packageA = new PackageBuilder(this, "A") { Extensions = Extensions }.Build().Output;
            string packageB = new PackageBuilder(this, "B") { Extensions = Extensions }.Build().Output;

            // Create the named bind paths to the packages.
            Dictionary<string, string> bindPaths = new Dictionary<string, string>();
            bindPaths.Add("packageA", packageA);
            bindPaths.Add("packageB", packageB);

            // Build the bundle.
            string bundleC = new BundleBuilder(this, "BundleC") { BindPaths = bindPaths, Extensions = Extensions }.Build().Output;

            // Delete the non-vital package to force the ignorable failure.
            File.Delete(Path.Combine(Path.GetDirectoryName(bundleC), "~Burn_MissingNonVitalPackage_PackageA.msi"));

            // Install the bundle.
            BundleInstaller installerC = new BundleInstaller(this, bundleC).Install();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsTrue(LogVerifier.MessageInLogFileRegex(installerC.LastLogFile, "Skipping apply of package: PackageA due to cache error: 0x80070002. Continuing..."));
            Assert.IsTrue(MsiVerifier.IsPackageInstalled(packageB));

            // Uninstall bundle.
            installerC.Uninstall();
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageA));
            Assert.IsFalse(MsiVerifier.IsPackageInstalled(packageB));

            this.CleanTestArtifacts = true;
        }

    }
}
