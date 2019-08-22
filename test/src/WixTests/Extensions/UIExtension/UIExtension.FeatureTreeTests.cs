// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Extensions.UIExtension
{
    using System;
    using System.IO;
    using WixTest;
    using WixTest.Verifiers;
    using Xunit;

    /// <summary>
    /// NetFX extension FeatureTree element tests
    /// </summary>
    public class FeatureTreeTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UIExtension\FeatureTreeTests");

        [NamedFact]
        [Description("Verify that the CustomAction Table is created in the MSI and has the expected data.")]
        [Priority(1)]
        public void FeatureTree_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(FeatureTreeTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUIExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("WixUIPrintEula", 65, "WixUIWixca", "PrintEula"));
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify using the msilog that the correct actions was executed.")]
        [Priority(2)]
        [Trait("RuntimeTest", "false")]
        public void FeatureTree_PrintEULA()
        {
        }
    }
}
