// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixTest;

    /// <summary>
    /// Tests for Bundle Chain element
    /// </summary>
    public class ChainTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\ChainTests");

        [NamedFact]
        [Description("Verify output of a Chain element with 1 Package child elements")]
        [Priority(2)]
        public void ChainPackageChild()
        {
            string sourceFile = Path.Combine(ChainTests.TestDataDirectory, @"ChainPackageChild\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            PackageTests.VerifyMsiPackageInformation(outputDirectory, "MsiPackage.msi", "MsiPackage", null, BundleTests.MsiPackageProductCode, false, false, string.Format("{0}v0.1.0.0", BundleTests.MsiPackageProductCode), null, BundleTests.MsiPackageFile);
            PackageTests.VerifyMspPackageInformation(outputDirectory, "MspPackage.msp", "MspPackage", null, BundleTests.MspPackagePatchCode, false, false, BundleTests.MspPackagePatchCode, null, BundleTests.MspPackageFile);
            PackageTests.VerifyMsuPackageInformation(outputDirectory, "MsuPackage.msu", "MsuPackage", null, false, false, null, BundleTests.MsuPackageFile);
            PackageTests.VerifyExePackageInformation(outputDirectory, "ExePackage.exe", "ExePackage", null, false, false, string.Empty, string.Empty, string.Empty, null, BundleTests.ExePackageFile);
        }

        [NamedFact]
        [Description("Verify output of a Chain element with 1 PackageGroupRef child elements")]
        [Priority(2)]
        public void ChainPackageGroupRefChild()
        {
            string sourceFile = Path.Combine(ChainTests.TestDataDirectory, @"ChainPackageGroupRefChild\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            PackageTests.VerifyMsiPackageInformation(outputDirectory, "MsiPackage.msi", "MsiPackage", null, BundleTests.MsiPackageProductCode, false, false, string.Format("{0}v0.1.0.0", BundleTests.MsiPackageProductCode), null, BundleTests.MsiPackageFile);
            PackageTests.VerifyMspPackageInformation(outputDirectory, "MspPackage.msp", "MspPackage", null, BundleTests.MspPackagePatchCode, false, false, BundleTests.MspPackagePatchCode, null, BundleTests.MspPackageFile);
            PackageTests.VerifyMsuPackageInformation(outputDirectory, "MsuPackage.msu", "MsuPackage", null, false, false, null, BundleTests.MsuPackageFile);
            PackageTests.VerifyExePackageInformation(outputDirectory, "ExePackage.exe", "ExePackage", null, false, false, string.Empty, string.Empty, string.Empty, null, BundleTests.ExePackageFile);
        }

        [NamedFact(Skip = "Ignore")]
        [Description("50 Package child elements.")]
        [Priority(3)]
        public void ChainMultiplePackageChildren()
        {
        }

        [NamedFact]
        [Description("Verify that build fails if two sibling Packages are the same Id")]
        [Priority(3)]
        public void ChainDuplicatePackages()
        {
            string candleOutput = Candle.Compile(Path.Combine(ChainTests.TestDataDirectory, @"ChainDuplicatePackages\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, "Duplicate symbol 'ChainPackage:Package1' found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, "Location of symbol related to previous error.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if two sibling PackageGroups contain the same Id")]
        [Priority(3)]
        public void ChainDuplicatePackageInPackageGroups()
        {
            string candleOutput = Candle.Compile(Path.Combine(ChainTests.TestDataDirectory, @"ChainDuplicatePackageInPackageGroups\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error)); // Duplicate symbol error
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error)); // Location of symbol related to previous error
            light.IgnoreWixMessageOrder = true;
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if two sibling PackageGroupRefs reference the same PackageGroup")]
        [Priority(3)]
        public void ChainDuplicatePackageGroupRefs()
        {
            string candleOutput = Candle.Compile(Path.Combine(ChainTests.TestDataDirectory, @"ChainDuplicatePackageGroupRefs\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(343, Message.MessageTypeEnum.Error)); //A circular reference of ordering dependencies was detected.
            light.IgnoreWixMessageOrder = true;
            light.ExpectedExitCode = 343;
            light.Run();
        }
    }
}
