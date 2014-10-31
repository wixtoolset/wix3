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
    using Microsoft.Win32;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// NetFX extension VSSetup element tests
    /// </summary>
    public class VSExtensionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\VSExtension\VSExtensionTests");
        private static readonly string DevenvRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS\";
        private static readonly string DevenvRegistryValueName = @"EnvironmentPath";
        private static string OutputFileName;
        private static string DevenvOriginalLocation;

        protected override void ClassInitialize()
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

        [NamedFact]
        [Description("Verify that the propject templates are installed to the correct folder on install")]
        [Priority(2)]
        [RuntimeTest]
        public void VS90InstallVSTemplates_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90InstallVSTemplates.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(VSExtensionTests.OutputFileName), "devenv.exe was not called");
            string acctualParamters = File.ReadAllText(VSExtensionTests.OutputFileName).Trim();
            string expectedParamters = "/InstallVSTemplates";
            Assert.True(acctualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), String.Format("devenv.exe was not called with the expected paramters. Acctual: '{0}'. Expected '{1}'.", acctualParamters, expectedParamters));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [NamedFact]
        [Description("Verify that the propject templates are installed to the correct folder on install")]
        [Priority(2)]
        [RuntimeTest]
        public void VSSetup_Install()
        {
            string sourceFile = Path.Combine(VSExtensionTests.TestDataDirectory, @"VS90Setup.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixVSExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.True(File.Exists(VSExtensionTests.OutputFileName), "devenv.exe was not called");
            string acctualParamters = File.ReadAllText(VSExtensionTests.OutputFileName).Trim();
            string expectedParamters = "/setup";
            Assert.True(acctualParamters.ToLowerInvariant().Equals(expectedParamters.ToLowerInvariant()), String.Format("devenv.exe was not called with the expected paramters. Acctual: '{0}'. Expected '{1}'.", acctualParamters, expectedParamters));

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }

        protected override void TestUninitialize()
        {
            base.TestUninitialize();
            File.Delete(VSExtensionTests.OutputFileName);
        }

        protected override void ClassUninitialize()
        {
            base.ClassUninitialize();

            // replace the devenv.exe registry key  with the original file
            Registry.SetValue(VSExtensionTests.DevenvRegistryKey, VSExtensionTests.DevenvRegistryValueName, VSExtensionTests.DevenvOriginalLocation);
        }
    }
}
