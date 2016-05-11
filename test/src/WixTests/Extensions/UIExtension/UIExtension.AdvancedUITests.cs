// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UIExtension
{
    using System;
    using System.IO;
    using WixTest;
    using WixTest.Verifiers;
    using Xunit;

    /// <summary>
    /// NetFX extension AdvancedUI element tests
    /// </summary>
    public class AdvancedUITests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UIExtension\AdvancedUITests");

        [NamedFact]
        [Description("Verify that the CustomAction Table is created in the MSI and has the expected data.")]
        [Priority(1)]
        public void AdvancedUI_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(AdvancedUITests.TestDataDirectory, @"InstallforAllUser.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUIExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("WixUIValidatePath", 65, "WixUIWixca", "ValidatePath"),
                new CustomActionTableData("WixUIPrintEula", 65, "WixUIWixca", "PrintEula"),
                new CustomActionTableData("WixSetDefaultPerUserFolder", 51, "WixPerUserFolder", @"[LocalAppDataFolder]Apps\[ApplicationFolderName]"),
                new CustomActionTableData("WixSetDefaultPerMachineFolder", 51, "WixPerMachineFolder", "[ProgramFilesFolder][ApplicationFolderName]"),
                new CustomActionTableData("WixSetPerUserFolder", 51, "APPLICATIONFOLDER", "[WixPerUserFolder]"),
                new CustomActionTableData("WixSetPerMachineFolder", 51, "APPLICATIONFOLDER", "[WixPerMachineFolder]"));
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify using the msilog that the correct actions was executed.")]
        [Priority(2)]
        [Trait("RuntimeTest", "false")]
        public void AdvancedUI_InstallforAllUser()
        {
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify using the msilog that the correct actions was executed.")]
        [Priority(2)]
        [Trait("RuntimeTest", "false")]
        public void AdvancedUI_InstallJustForYou()
        {
        }
     }
}
