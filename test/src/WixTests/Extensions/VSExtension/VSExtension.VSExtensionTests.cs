//-----------------------------------------------------------------------
// <copyright file="VSExtension.VSExtensionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>VS Extension VSSetup tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.VSExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Win32;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;

    /// <summary>
    /// NetFX extension VSSetup element tests
    /// </summary>
    [TestClass]
    public class VSExtensionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\VSExtension\VSExtensionTests");
        private static readonly string DevenvRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS\";
        private static readonly string DevenvRegistryValueName = @"EnvironmentPath";
        private static string OutputFileName;
        private static string DevenvOriginalLocation;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // create a new command file
            string commandFileName = Path.Combine(Path.GetTempPath(), "stubdevenv.cmd");
            VSExtensionTests.OutputFileName = Utilities.FileUtilities.GetUniqueFileName();
            File.WriteAllText(commandFileName, string.Format("echo %* > {0}", VSExtensionTests.OutputFileName));

            // backup the original devenv.exe registry key first
            VSExtensionTests.DevenvOriginalLocation = (string)Registry.GetValue(VSExtensionTests.DevenvRegistryKey, VSExtensionTests.DevenvRegistryValueName, string.Empty);

            // replace the devenv.exe registry key  with the new command file
            Registry.SetValue(VSExtensionTests.DevenvRegistryKey, VSExtensionTests.DevenvRegistryValueName, commandFileName);
        }

        [TestMethod]
        [Description("Verify that the propject templates are installed to the correct folder on install")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void VS90InstallVSTemplates_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90InstallVSTemplates.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(File.Exists(VSExtensionTests.OutputFileName), "devenv.exe was not called");
            string acctualParamters = File.ReadAllText(VSExtensionTests.OutputFileName).Trim();
            string expectedParamters = "/InstallVSTemplates";
            Assert.IsTrue(acctualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), "devenv.exe was not called with the expected paramters. Acctual: '{0}'. Expected '{1}'.", acctualParamters, expectedParamters);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [TestMethod]
        [Description("Verify that the propject templates are installed to the correct folder on install")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void VSSetup_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90Setup.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.IsTrue(File.Exists(VSExtensionTests.OutputFileName), "devenv.exe was not called");
            string acctualParamters = File.ReadAllText(VSExtensionTests.OutputFileName).Trim();
            string expectedParamters = "/setup";
            Assert.IsTrue(acctualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), "devenv.exe was not called with the expected paramters. Acctual: '{0}'. Expected '{1}'.", acctualParamters, expectedParamters);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [TestCleanup]
        public override void CleanUp()
        {
            File.Delete(VSExtensionTests.OutputFileName);
            // make sure to call the base class cleanup method
            base.CleanUp();
        }

        [ClassCleanup]
        public static void ClassCleanUp()
        {
            // replace the devenv.exe registry key  with the original file
            Registry.SetValue(VSExtensionTests.DevenvRegistryKey, VSExtensionTests.DevenvRegistryValueName, VSExtensionTests.DevenvOriginalLocation);
        }
    }
}
