//-----------------------------------------------------------------------
// <copyright file="DependencyExtension.DependencyExtensionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Dependency extension functional tests.</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.DependencyExtension
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using WixTest.Tests;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Microsoft.Win32;
    using Xunit;

    public sealed class DependencyExtensionTests : WixTests
    {
        [NamedFact]
        [Priority(2)]
        [Description("Install products A then B, and uninstall in reverse order as normal.")]
        [RuntimeTest]
        public void DependencyExtension_Install()
        {
            string packageA = BuildPackage("A", null);
            string packageB = BuildPackage("B", null);

            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);

            // Uninstall in reverse order.
            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.UninstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install product B without dependency A installed and fail.")]
        [RuntimeTest]
        public void DependencyExtension_MissingDependency()
        {
            string packageB = BuildPackage("B", null);

            string logB = MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            Assert.True(LogVerifier.MessageInLogFile(logB, @"WixDependencyRequire:  The dependency ""Microsoft.WiX.DependencyExtension_MissingDependency.A,v1.0"" is missing or is not the required version."));
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install product B without dependency A installed and override.")]
        [RuntimeTest]
        public void DependencyExtension_MissingDependencyOverride()
        {
            string packageB = BuildPackage("B", null);

            string logB = InstallProductWithProperties(packageB, MSIExec.MSIExecReturnCode.SUCCESS, "DISABLEDEPENDENCYCHECK=1");
            Assert.True(LogVerifier.MessageInLogFile(logB, @"Skipping action: WixDependencyRequire (condition is false)"));

            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install products A then B, and uninstall A while B is still present and appear to succeed.")]
        [RuntimeTest]
        public void DependencyExtension_UninstallDependency()
        {
            string packageA = BuildPackage("A", null);
            string packageB = BuildPackage("B", null);

            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);

            // Now attempt the uninstall of dependency package A.
            string logA = MSIExec.UninstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            Assert.True(LogVerifier.MessageInLogFile(logA, @"WixDependencyCheck:  Found dependent"));

            // Uninstall in reverse order.
            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.UninstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install products A then B, and uninstall A while B is still present and override.")]
        [RuntimeTest]
        public void DependencyExtension_UninstallDependencyOverrideAll()
        {
            string packageA = BuildPackage("A", null);
            string packageB = BuildPackage("B", null);

            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);

            // Now attempt the uninstall of dependency package A.
            string logA = UninstallProductWithProperties(packageA, MSIExec.MSIExecReturnCode.SUCCESS, "IGNOREDEPENDENCIES=ALL");
            Assert.True(LogVerifier.MessageInLogFile(logA, @"Skipping action: WixDependencyCheck (condition is false)"));

            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install products A then B, and uninstall A while B is still present and override.")]
        [RuntimeTest]
        public void DependencyExtension_UninstallDependencyOverrideSpecific()
        {
            string packageA = BuildPackage("A", null);
            string packageB = BuildPackage("B", null);

            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);

            // Now attempt the uninstall of dependency package A.
            string productCode = MsiUtils.GetMSIProductCode(packageB);
            string ignoreDependencies = String.Concat("IGNOREDEPENDENCIES=", productCode);

            UninstallProductWithProperties(packageA, MSIExec.MSIExecReturnCode.SUCCESS, ignoreDependencies);

            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Priority(2)]
        [Description("Install products A then B, upgrades A, then attempts to uninstall A while B is still present.")]
        [RuntimeTest]
        public void DependencyExtension_UninstallUpgradedDependency()
        {
            string packageA = BuildPackage("A", null);
            string packageB = BuildPackage("B", null);
            string packageA1 = BuildPackage("A", "1.0.1.0");

            MSIExec.InstallProduct(packageA, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.InstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);

            // Build the upgraded dependency A1 and make sure A was removed.
            MSIExec.InstallProduct(packageA1, MSIExec.MSIExecReturnCode.SUCCESS);
            Assert.False(IsPackageInstalled(packageA));

            // Now attempt the uninstall of upgraded dependency package A1.
            string logA = MSIExec.UninstallProduct(packageA1, MSIExec.MSIExecReturnCode.SUCCESS);
            Assert.True(LogVerifier.MessageInLogFile(logA, @"WixDependencyCheck:  Found dependent"));

            // Uninstall in reverse order.
            MSIExec.UninstallProduct(packageB, MSIExec.MSIExecReturnCode.SUCCESS);
            MSIExec.UninstallProduct(packageA1, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        #region Utilities
        /// <summary>
        /// Passes in per-test data to avoid collisions with failed tests when installing dependencies.
        /// </summary>
        /// <param name="name">The name of the source file (sans extension) to build.</param>
        /// <param name="version">The optional version to pass to the compiler.</param>
        /// <returns>The path to the build MSI package.</returns>
        private string BuildPackage(string name, string version)
        {
            PackageBuilder builder = new PackageBuilder(this, name) { Extensions = DependencyExtensionTests.Extensions };
            if (!String.IsNullOrEmpty(version))
            {
                builder.PreprocessorVariables.Add("Version", version);
            }

            return builder.Build().Output;
        }

        /// <summary>
        /// Builds a patch using given paths for the target and upgrade packages.
        /// </summary>
        /// <param name="targetPath">The path to the target MSI.</param>
        /// <param name="upgradePath">The path to the upgrade MSI.</param>
        /// <param name="name">The name of the patch to build.</param>
        /// <param name="version">Optional version for the bundle.</param>
        /// <returns>The path to the patch.</returns>
        private string BuildPatch(string targetPath, string upgradePath, string name, string version)
        {
            // Get the name of the calling method.
            StackTrace stack = new StackTrace();
            string caller = stack.GetFrame(1).GetMethod().Name;

            // Create paths.
            string source = Path.Combine(this.TestContext.TestDataDirectory, String.Concat(name, ".wxs"));
            string rootDirectory = FileUtilities.GetUniqueFileName();
            string objDirectory = Path.Combine(rootDirectory, Settings.WixobjFolder);
            string msiDirectory = Path.Combine(rootDirectory, Settings.MsiFolder);
            string wixmst = Path.Combine(objDirectory, String.Concat(name, ".wixmst"));
            string wixmsp = Path.Combine(objDirectory, String.Concat(name, ".wixmsp"));
            string package = Path.Combine(msiDirectory, String.Concat(name, ".msp"));

            // Add the root directory to be cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(rootDirectory));

            // Compile.
            Candle candle = new Candle();
            candle.Extensions.AddRange(DependencyExtensionTests.Extensions);
            candle.OtherArguments = String.Concat("-dTestName=", caller);
            if (!String.IsNullOrEmpty(version))
            {
                candle.OtherArguments = String.Concat(candle.OtherArguments, " -dVersion=", version);
            }
            candle.OutputFile = String.Concat(objDirectory, @"\");
            candle.SourceFiles.Add(source);
            candle.WorkingDirectory = this.TestContext.TestDataDirectory;
            candle.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(objDirectory));

            // Link.
            Light light = new Light();
            light.Extensions.AddRange(DependencyExtensionTests.Extensions);
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.OutputFile = wixmsp;
            light.SuppressMSIAndMSMValidation = true;
            light.WorkingDirectory = this.TestContext.TestDataDirectory;
            light.Run();

            // Make sure the output directory is cleaned up.
            this.TestArtifacts.Add(new DirectoryInfo(msiDirectory));

            // Torch.
            Torch torch = new Torch();
            torch.TargetInput = Path.ChangeExtension(targetPath, "wixpdb");
            torch.UpdatedInput = Path.ChangeExtension(upgradePath, "wixpdb");
            torch.PreserveUnmodified = true;
            torch.XmlInput = true;
            torch.OutputFile = wixmst;
            torch.WorkingDirectory = this.TestContext.TestDataDirectory;
            torch.Run();

            // Pyro.
            Pyro pyro = new Pyro();
            pyro.Baselines.Add(torch.OutputFile, name);
            pyro.InputFile = light.OutputFile;
            pyro.OutputFile = package;
            pyro.WorkingDirectory = this.TestContext.TestDataDirectory;
            pyro.SuppressWarnings.Add("1079");
            pyro.Run();

            return pyro.OutputFile;
        }

        /// <summary>
        /// Gets whether the product defined by the package <paramref name="path"/> is installed.
        /// </summary>
        /// <param name="path">The path to the package to test.</param>
        /// <returns>True if the package is installed; otherwise, false.</returns>
        private bool IsPackageInstalled(string path)
        {
            string productCode = MsiUtils.GetMSIProductCode(path);
            return MsiUtils.IsProductInstalled(productCode);
        }

        /// <summary>
        /// Gets whether the two values are equal.
        /// </summary>
        /// <param name="name">The name of the registry value to check.</param>
        /// <param name="value">The value to check. Pass null to check if the registry value is missing.</param>
        /// <returns>True if the values are equal; otherwise, false.</returns>
        private bool IsRegistryValueEqual(string name, string value)
        {
            // Get the name of the calling method.
            StackTrace stack = new StackTrace();
            string caller = stack.GetFrame(1).GetMethod().Name;

            string key = String.Format(@"Software\WiX\Tests\{0}", caller);
            string data = null;

            using (RegistryKey reg = Registry.LocalMachine.OpenSubKey(key))
            {
                if (null != reg)
                {
                    object o = reg.GetValue(name);
                    if (null != o)
                    {
                        data = o.ToString();
                    }
                }
            }

            return String.Equals(data, value);
        }

        /// <summary>
        /// Installs the product and passes zero or more property settings.
        /// </summary>
        /// <param name="path">The path to the MSI to install.</param>
        /// <param name="expectedExitCode">The expected exit code.</param>
        /// <param name="properties">Zero or more properties to pass to the install.</param>
        /// <returns>The path to the log file.</returns>
        private string InstallProductWithProperties(string path, MSIExec.MSIExecReturnCode expectedExitCode, params string[] properties)
        {
            return RunMSIWithProperties(path, expectedExitCode, MSIExec.MSIExecMode.Install, properties);
        }

        /// <summary>
        /// Uninstalls the product and passes zero or more property settings.
        /// </summary>
        /// <param name="path">The path to the MSI to install.</param>
        /// <param name="expectedExitCode">The expected exit code.</param>
        /// <param name="properties">Zero or more properties to pass to the install.</param>
        /// <returns>The path to the log file.</returns>
        private string UninstallProductWithProperties(string path, MSIExec.MSIExecReturnCode expectedExitCode, params string[] properties)
        {
            return RunMSIWithProperties(path, expectedExitCode, MSIExec.MSIExecMode.Uninstall, properties);
        }

        /// <summary>
        /// Runs msiexec on the given <paramref name="path"/> with zero or more property settings.
        /// </summary>
        /// <param name="path">The path to the MSI to run.</param>
        /// <param name="expectedExitCode">The expected exit code.</param>
        /// <param name="mode">The installation mode for the MSI.</param>
        /// <param name="properties">Zero or more properties to pass to the install.</param>
        /// <returns>The path to the log file.</returns>
        private string RunMSIWithProperties(string path, MSIExec.MSIExecReturnCode expectedExitCode, MSIExec.MSIExecMode mode, params string[] properties)
        {
            MSIExec exec = new MSIExec();
            exec.ExecutionMode = mode;
            exec.ExpectedExitCode = expectedExitCode;
            exec.OtherArguments = null != properties ? String.Join(" ", properties) : null;
            exec.Product = path;

            // Get the name of the calling method.
            StackTrace stack = new StackTrace();
            string caller = stack.GetFrame(2).GetMethod().Name;

            // Generate the log file name.
            string logFile = String.Format("{0}_{1:yyyyMMddhhmmss}_{3}_{2}.log", caller, DateTime.UtcNow, Path.GetFileNameWithoutExtension(path), mode);
            exec.LogFile = Path.Combine(Path.GetTempPath(), logFile);

            exec.Run();

            return exec.LogFile;
        }
        #endregion
    }
}
