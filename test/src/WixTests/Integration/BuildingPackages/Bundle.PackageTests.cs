// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using Microsoft.Deployment.Compression.Cab;
    using WixTest.Verifiers;
    using Xunit;

    /// <summary>
    /// Tests for Bundle *Package elements
    /// </summary>
    public class PackageTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\PackageTests");

        [NamedFact]
        [Description("Package SourceFile is required.")]
        [Priority(3)]
        // bug https://sourceforge.net/tracker/?func=detail&aid=2981298&group_id=105970&atid=642714
        public void PackageSourceFileMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageSourceFileMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The MsiPackage/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The MspPackage/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The MsuPackage/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The ExePackage/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("Package SourceFile is empty.")]
        [Priority(3)]
        public void PackageEmptySourceFile()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageEmptySourceFile\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The MsiPackage/@SourceFile attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The MspPackage/@SourceFile attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The MsuPackage/@SourceFile attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The ExePackage/@SourceFile attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 6;
            candle.Run();
        }

        [NamedFact]
        [Description("Package @SourceFile contains an invalid path.")]
        [Priority(3)]
        public void PackageInvalidSourceFile()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"PackageInvalidSourceFile\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(300, @"Illegal characters in path 'MsiPackage|*?.msi'. Ensure you provided a valid path to the file.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 300;
            light.Run();
        }

        [NamedFact]
        [Description("Package @SourceFile target does not exist on disk.")]
        [Priority(3)]
        [Trait("Bug Link", @"https://sourceforge.net/tracker/?func=detail&aid=2980318&group_id=105970&atid=642714")]
        public void PackageNonexistingSourceFile()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"PackageNonexistingSourceFile\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(352, string.Format(@"Unable to read package '{0}'. This installation package could not be opened. Verify that the package exists and that you can access it, or contact the application vendor to verify that this is a valid Windows Installer package.", Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\NonExisitingMsiPackage.msi")), Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(352, string.Format(@"Unable to read package '{0}'. This installation package could not be opened. Verify that the package exists and that you can access it, or contact the application vendor to verify that this is a valid Windows Installer package.", Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\NonExisitingMspPackage.msp")), Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(352, string.Format(@"Unable to read package '{0}'. Could not find file '{0}'.", Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\NonExisitingMsuPackage.msu")), Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(352, string.Format(@"Unable to read package '{0}'. Could not find file '{0}'.", Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\NonExisitingExePackage.exe")), Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 352;
            light.Run();
        }

        [NamedFact]
        [Description("Package @Name contains an invalid path.")]
        [Priority(3)]
        public void PackageInvalidName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageInvalidName\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The MsiPackage/@Name attribute's value, 'MsiPackage|*?.msi', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The MspPackage/@Name attribute's value, 'MspPackage|*?.msp', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The MsuPackage/@Name attribute's value, 'MsuPackage|*?.msu', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The ExePackage/@Name attribute's value, 'ExePackage|*?.exe', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 346;
            candle.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a missing Package.")]
        [Priority(3)]
        public void PackageAfterUndefinedPackage()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"PackageAfterUndefinedPackage\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(344, "An expected identifier ('UndefinedMsiPackage', of type 'Package') was not found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(344, "An expected identifier ('UndefinedMspPackage', of type 'Package') was not found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(344, "An expected identifier ('UndefinedMsuPackage', of type 'Package') was not found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(344, "An expected identifier ('UndefinedExePackage', of type 'Package') was not found.", Message.MessageTypeEnum.Error));
            light.IgnoreWixMessageOrder = true;
            light.ExpectedExitCode = 344;
            light.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a Package after this Package.")]
        [Priority(3)]
        public void PackageRecursiveAfter()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"PackageRecursiveAfter\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(343, "A circular reference of ordering dependencies was detected. The infinite loop includes: Package:MsuPackage -> Package:MspPackage -> Package:MsuPackage. Ordering dependency references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(343, "A circular reference of ordering dependencies was detected. The infinite loop includes: Package:MspPackage -> Package:ExePackage -> Package:MspPackage. Ordering dependency references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(343, "A circular reference of ordering dependencies was detected. The infinite loop includes: Package:MspPackage -> Package:MsiPackage -> Package:MspPackage. Ordering dependency references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(343, "A circular reference of ordering dependencies was detected. The infinite loop includes: Package:MsiPackage -> Package:MsuPackage -> Package:MsiPackage. Ordering dependency references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(343, Message.MessageTypeEnum.Error));
            light.IgnoreWixMessageOrder = true;
            light.ExpectedExitCode = 343;
            light.Run();
        }

        [NamedFact]
        [Description("Package @Vital contains an invalid value.")]
        [Priority(3)]
        public void PackageInvalidVital()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageInvalidVital\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MsiPackage/@Vital attribute's value, 'true', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MspPackage/@Vital attribute's value, 'false', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MsuPackage/@Vital attribute's value, 'vital', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The ExePackage/@Vital attribute's value, 'TRUE', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 15;
            candle.Run();
        }

        [NamedFact]
        [Description("Package @Cache contains an invalid value.")]
        [Priority(3)]
        public void PackageInvalidCache()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageInvalidCache\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MsiPackage/@Cache attribute's value, 'true', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MspPackage/@Cache attribute's value, 'false', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The MsuPackage/@Cache attribute's value, 'Cache', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(15, "The ExePackage/@Cache attribute's value, 'TRUE', is not a legal yes/no value.  The only legal values are 'no' and 'yes'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 15;
            candle.Run();
        }

        [NamedFact]
        [Description("Valid Package.")]
        [Priority(2)]
        // bug# https://sourceforge.net/tracker/?func=detail&aid=2980325&group_id=105970&atid=642714
        public void ValidPackage()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"ValidPackage\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the burnManifest has the correct information 
            PackageTests.VerifyMspPackageInformation(outputDirectory, "MspPackage1.msp", "MspPackage1", null, BundleTests.MspPackagePatchCode, true, false, "MspPackage1CacheId", null, BundleTests.MspPackageFile);
            PackageTests.VerifyMsuPackageInformation(outputDirectory, "MsuPackage2.msu", "MsuPackage2", null, true, true, @"ftp://192.168.0.1/testPayload.exe", BundleTests.MsuPackageFile);
            PackageTests.VerifyMsiPackageInformation(outputDirectory, "MsiPackage3.msi", "MsiPackage3", null, BundleTests.MsiPackageProductCode, false, true, string.Format("{0}v0.1.0.0", BundleTests.MsiPackageProductCode), @"http://go.microsoft.com/fwlink/?linkid=164202", BundleTests.MsiPackageFile);
            PackageTests.VerifyExePackageInformation(outputDirectory, "ExePackage5.exe", "ExePackage5", "x!=y", true, true, @"\q", @"\q \r \t", @"\anotherargument -t", @"\\wixbuild\releases\wix\", BundleTests.MsiPackageFile); // using the msi package for an exe

            PackageTests.VerifyMsiPackageOrder(outputDirectory, "ExePackage5.exe", "MspPackage1.msp", "MsuPackage2.msi", "MsiPackage3.msi");
        }

        [NamedFact]
        [Description("Package can have payload children.")]
        [Priority(2)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2987928&group_id=105970&atid=642714")]
        public void PackagePayloads()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"PackagePayloads\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;
            string PayloadFile1 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile1.txt");
            string PayloadFile2 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile2.txt");
            string PayloadFile3 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile3.txt");
            string PayloadFile4 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile4.txt");
            string PayloadFile5 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile5.txt");

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the burnManifest has the correct information 
            PackageTests.VerifyMsiPackageInformation(outputDirectory, "MsiPackage1.msi", "MsiPackage1", null, BundleTests.MsiPackageProductCode, false, false, string.Format("{0}v0.1.0.0", BundleTests.MsiPackageProductCode), null, BundleTests.MsiPackageFile);
            PackageTests.VerifyMsuPackageInformation(outputDirectory, "MsuPackage2.msu", "MsuPackage2", null, false, false, null, BundleTests.MsuPackageFile);
            PackageTests.VerifyMspPackageInformation(outputDirectory, "MspPackage3.msp", "MspPackage3", null, BundleTests.MspPackagePatchCode, false, false, BundleTests.MspPackagePatchCode, null, BundleTests.MspPackageFile);
            PackageTests.VerifyExePackageInformation(outputDirectory, "ExePackage4.exe", "ExePackage4", null, false, false, string.Empty, string.Empty, string.Empty, @"file://wixbuild/releases/wix/", BundleTests.ExePackageFile);

            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSI, "MsiPackage1.msi", "MsiPackage1", "PayloadFile1.txt", null, PayloadFile1);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSU, "MsuPackage2.msu", "MsuPackage2", "PayloadFile2.txt", null, PayloadFile2);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSP, "MspPackage3.msp", "MspPackage3", "PayloadFile3.txt", null, PayloadFile3);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSP, "MspPackage3.msp", "MspPackage3", "PayloadFile4.txt", "http://go.microsoft.com/fwlink/?linkid=164202", PayloadFile4);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.EXE, "ExePackage4.exe", "ExePackage4", "PayloadFile5.txt", null, PayloadFile5);
        }

        /* MsiPackage specific tests */

        [NamedFact]
        [Description("MsiPackage @SourceFile target is not a valid .msi file.")]
        [Priority(3)]
        public void MsiPackageInvalidMsi()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"MsiPackageInvalidMsi\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(352, string.Format(@"Unable to read package '{0}'. This installation package could not be opened. Contact the application vendor to verify that this is a valid Windows Installer package.", BundleTests.ExePackageFile), Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 352;
            light.Run();
        }

        [NamedFact]
        [Description("MsiPackage @SourceFile target is in use by another application.")]
        [Priority(3)]
        public void MsiPackageInuse()
        {
            // acquire a loc on the file
            string outputDirectory = this.TestContext.TestDirectory;
            string testFileName = Path.Combine(outputDirectory, "MsiPackage.msi");
            File.Copy(BundleTests.MsiPackageFile, testFileName);
            FileStream msiPackage = File.Open(testFileName, FileMode.Open, FileAccess.Read);

            string errorMessage = string.Format(@"Unable to read package '{0}'. This installation package could not be opened. Contact the application vendor to verify that this is a valid Windows Installer package.", testFileName);
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"MsiPackageInuse\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light(outputDirectory);
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(352, errorMessage, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 352;
            light.Run();

            // release lock
            msiPackage.Close();
        }

        [NamedFact]
        [Description("MsiProperty Name is required.")]
        [Priority(3)]
        public void MsiPropertyNameMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"MsiPropertyNameMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The MsiProperty/@Name attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("MsiProperty Value is required.")]
        [Priority(3)]
        public void MsiPropertyValueMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"MsiPropertyValueMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The MsiProperty/@Value attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("MsiProperty can not be redefined.")]
        [Priority(3)]
        public void DuplicateMsiProperty()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"DuplicateMsiProperty\Product.wxs");
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error)); // Duplicate symbol error
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Valid MsiPackage MsiProperty child elements.")]
        [Priority(2)]
        [Trait("Bug Link", @"https://sourceforge.net/tracker/?func=detail&aid=2987866&group_id=105970&atid=642714")]
        public void ValidMsiProperty()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"ValidMsiProperty\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the burnManifest has the correct information 
            PackageTests.VerifyMsiPropertyInformation(outputDirectory, "MsiPackage1", "MsiProperty1", "x!=y | z==y");
            PackageTests.VerifyMsiPropertyInformation(outputDirectory, "MsiPackage1", "MsiProperty2", "http://www.microsoft.com");
            PackageTests.VerifyMsiPropertyInformation(outputDirectory, "MsiPackage1", "MsiProperty3", "23.00");
            PackageTests.VerifyMsiPropertyInformation(outputDirectory, "MsiPackage2", "MsiProperty1", "x!=y | z==y");
        }

        /* MspPackage specific tests */

        [NamedFact]
        [Description("MspPackage @SourceFile target is not a valid .msi file.")]
        [Priority(3)]
        public void MspPackageInvalidMsp()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"MspPackageInvalidMsp\Product.wxs");
            string errorMessage = string.Format(@"Unable to read package '{0}'. This installation package could not be opened. Contact the application vendor to verify that this is a valid Windows Installer package.", BundleTests.ExePackageFile);
            string candleOutput = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(352, errorMessage, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 352;
            light.Run();
        }

        #region Verification Methods

        /// <summary>
        /// Supported package types
        /// </summary>
        public enum PackageType
        {
            /// <summary>
            /// Msi Package
            /// </summary>
            MSI,

            /// <summary>
            /// Msp Package
            /// </summary>
            MSP,

            /// <summary>
            /// Msu Package
            /// </summary>
            MSU,

            /// <summary>
            /// Exe Package
            /// </summary>
            EXE,
        };

        /// <summary>
        /// Verifies MsiPackage information in Burn-Manifest.xml and Burn-UxManifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param> 
        /// <param name="expectedPackageName">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedId">Expected Package @Id value.</param>
        /// <param name="expectedInstallCondition">Expected Package @InstallCondition value.</param>
        /// <param name="expectedProductCode">Expected Package @ProductCode value.</param>
        /// <param name="expecteVital">Package is viatal or not.</param>
        /// <param name="expectedPermanent">Expected Package @Permanent value.</param>
        /// <param name="expectedCacheId">Expected Package @CacheId value.</param>
        /// <param name="acctualFilePath">Path to the acctual file for comparison.</param>
        /// <param name="expectedDownloadURL">Expected Package @DownloadURL value.</param>
        public static void VerifyMsiPackageInformation(string embededResourcesDirectoryPath, string expectedPackageName, string expectedId, string expectedInstallCondition, string expectedProductCode, bool expecteVital, bool expectedPermanent, string expectedCacheId, string expectedDownloadURL, string acctualFilePath)
        {
            VerifyPackageInformation(embededResourcesDirectoryPath, expectedPackageName, PackageType.MSI, expectedId, expectedInstallCondition, expectedProductCode, expecteVital, expectedPermanent, expectedCacheId, null, null, null, expectedDownloadURL, acctualFilePath);
        }

        /// <summary>
        /// Verifies MspPackage information in Burn-Manifest.xml and Burn-UxManifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param> 
        /// <param name="expectedPackageName">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedId">Expected Package @Id value.</param>
        /// <param name="expectedInstallCondition">Expected Package @InstallCondition value.</param>
        /// <param name="expectedPatchCode">Expected Package @PatchCode value.</param>
        /// <param name="expecteVital">Package is viatal or not.</param>
        /// <param name="expectedPermanent">Expected Package @Permanent value.</param>
        /// <param name="expectedCacheId">Expected Package @CacheId value.</param>
        /// <param name="acctualFilePath">Path to the acctual file for comparison.</param>
        /// <param name="expectedDownloadURL">Expected Package @DownloadURL value.</param>
        public static void VerifyMspPackageInformation(string embededResourcesDirectoryPath, string expectedPackageName, string expectedId, string expectedInstallCondition, string expectedPatchCode, bool expecteVital, bool expectedPermanent, string expectedCacheId, string expectedDownloadURL, string acctualFilePath)
        {
            VerifyPackageInformation(embededResourcesDirectoryPath, expectedPackageName, PackageType.MSP, expectedId, expectedInstallCondition, expectedPatchCode, expecteVital, expectedPermanent, expectedCacheId, null, null, null, expectedDownloadURL, acctualFilePath);
        }

        /// <summary>
        /// Verifies MsuPackage information in Burn-Manifest.xml and Burn-UxManifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param> 
        /// <param name="expectedPackageName">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedId">Expected Package @Id value.</param>
        /// <param name="expectedInstallCondition">Expected Package @InstallCondition value.</param>
        /// <param name="expecteVital">Package is viatal or not.</param>
        /// <param name="expectedPermanent">Expected Package @Permanent value.</param>
        /// <param name="acctualFilePath">Path to the acctual file for comparison.</param>
        /// <param name="expectedDownloadURL">Expected Package @DownloadURL value.</param>
        public static void VerifyMsuPackageInformation(string embededResourcesDirectoryPath, string expectedPackageName, string expectedId, string expectedInstallCondition, bool expecteVital, bool expectedPermanent, string expectedDownloadURL, string acctualFilePath)
        {
            VerifyPackageInformation(embededResourcesDirectoryPath, expectedPackageName, PackageType.MSU, expectedId, expectedInstallCondition, null, expecteVital, expectedPermanent, null, null, null, null, expectedDownloadURL, acctualFilePath);
        }

        /// <summary>
        /// Verifies ExePackage information in Burn-Manifest.xml and Burn-UxManifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param> 
        /// <param name="expectedPackageName">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedId">Expected Package @Id value.</param>
        /// <param name="expectedInstallCondition">Expected Package @InstallCondition value.</param>
        /// <param name="expecteVital">Package is viatal or not.</param>
        /// <param name="expectedPermanent">Expected Package @Permanent value.</param>
        /// <param name="acctualFilePath">Path to the acctual file for comparison.</param>
        /// <param name="expectedInstallCommmand">Expected @InstallCommand value.</param>
        /// <param name="expectedUninstallCommmand">Expected @UninstallCommand value.</param>
        /// <param name="expectedRepairCommand">Expected @RepairCommand value.</param>
        /// <param name="expectedDownloadURL">Expected Package @DownloadURL value.</param>
        public static void VerifyExePackageInformation(string embededResourcesDirectoryPath, string expectedPackageName, string expectedId, string expectedInstallCondition, bool expecteVital, bool expectedPermanent, string expectedInstallCommmand, string expectedUninstallCommmand, string expectedRepairCommand, string expectedDownloadURL, string acctualFilePath)
        {
            VerifyPackageInformation(embededResourcesDirectoryPath, expectedPackageName, PackageType.EXE, expectedId, expectedInstallCondition, null, expecteVital, expectedPermanent, null, expectedInstallCommmand, expectedUninstallCommmand, expectedRepairCommand, expectedDownloadURL, acctualFilePath);
        }

        /// <summary>
        /// Verifies Package information in Burn-Manifest.xml and Burn-UxManifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedPackageName">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedPackageType">Package type MSI, MSP, MSU or EXE.</param>
        /// <param name="expectedId">Expected Package @Id value.</param>
        /// <param name="expectedInstallCondition">Expected Package @InstallCondition value.</param>
        /// <param name="expectedProductCode">Expected Package @ProductCode value.</param>
        /// <param name="expecteVital">Package is viatal or not.</param>
        /// <param name="expectedPermanent">Expected Package @Permanent value.</param>
        /// <param name="expectedCacheId">Expected Package @CacheId value.</param>
        /// <param name="expectedInstallCommmand">Expected @InstallCommand value.</param>
        /// <param name="expectedUninstallCommmand">Expected @UninstallCommand value.</param>
        /// <param name="expectedRepairCommand">Expected @RepairCommand value.</param>
        /// <param name="expectedDownloadURL">Expected Package @DownloadURL value.</param>
        /// <param name="acctualFilePath">Path to the acctual file for comparison.</param>
        private static void VerifyPackageInformation(string embededResourcesDirectoryPath, string expectedPackageName, PackageType expectedPackageType, string expectedId,
                                                     string expectedInstallCondition, string expectedProductCode, bool expecteVital, bool expectedPermanent, string expectedCacheId,
                                                     string expectedInstallCommmand, string expectedUninstallCommmand, string expectedRepairCommand, string expectedDownloadURL,
                                                     string acctualFilePath)
        {
            string expectedFileSize = new FileInfo(acctualFilePath).Length.ToString();
            string expectedHash = FileVerifier.ComputeFileSHA1Hash(acctualFilePath);
            string expectedProductSize = expectedFileSize;

            // verify the Burn_Manifest has the correct information 
            string burnManifestXPath = string.Format(@"//burn:{0}[@Id='{1}']", GetPackageElementName(expectedPackageType), expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No MsiPackage with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "InstallCondition", expectedInstallCondition);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Permanent", expectedPermanent ? "yes" : "no");
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Vital", expecteVital ? "yes" : "no");

            if (expectedPackageType == PackageType.MSI)
            {
                BundleTests.VerifyAttributeValue(burnManifestNodes[0], "ProductCode", expectedProductCode);
            }

            if (expectedPackageType == PackageType.EXE)
            {
                BundleTests.VerifyAttributeValue(burnManifestNodes[0], "InstallArguments", expectedInstallCommmand);
                BundleTests.VerifyAttributeValue(burnManifestNodes[0], "UninstallArguments", expectedUninstallCommmand);
                BundleTests.VerifyAttributeValue(burnManifestNodes[0], "RepairArguments", expectedRepairCommand);
            }

            if (!String.IsNullOrEmpty(expectedCacheId))
            {
                BundleTests.VerifyAttributeValue(burnManifestNodes[0], "CacheId", expectedCacheId);
            }

            // verify payload information
            PackageTests.VerifyPackagePayloadInformation(embededResourcesDirectoryPath, expectedPackageType, expectedPackageName, expectedId, expectedPackageName, expectedDownloadURL, acctualFilePath);

            // verify the Burn-UxManifest has the correct information
            string expectedProductName = null;
            string expectedDiscription = null;

            if (expectedPackageType == PackageType.EXE)
            {
                FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(acctualFilePath);
                expectedProductName = string.IsNullOrEmpty(fileInfo.ProductName) ? null : fileInfo.ProductName;
                expectedDiscription = string.IsNullOrEmpty(fileInfo.FileDescription) ? null : fileInfo.FileDescription;
            }
            else if (expectedPackageType == PackageType.MSI)
            {
                string subject = Verifier.GetMsiSummaryInformationProperty(acctualFilePath, Verifier.MsiSummaryInformationProperty.Subject);
                expectedProductName = string.IsNullOrEmpty(subject) ? null : subject;
            }

            string burnUxManifestXPath = string.Format(@"//burnUx:WixPackageProperties[@Package='{0}']", expectedId);
            XmlNodeList burnUxManifestNodes = BundleTests.QueryBurnUxManifest(embededResourcesDirectoryPath, burnUxManifestXPath);
            Assert.True(1 == burnUxManifestNodes.Count, String.Format("No WixPackageProperties for Package: '{0}' was found in Burn-UxManifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "Vital", expecteVital ? "yes" : "no");
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "DownloadSize", expectedFileSize);
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "PackageSize", expectedFileSize);
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "InstalledSize", expectedFileSize);
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "DisplayName", expectedProductName);
            BundleTests.VerifyAttributeValue(burnUxManifestNodes[0], "Description", expectedDiscription);
        }

        /// <summary>
        /// Verify MsiPackage elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="packageNames">Names of the MsiPackage elements in order.</param>
        public static void VerifyMsiPackageOrder(string embededResourcesDirectoryPath, params string[] packageNames)
        {
            string burnManifestXPath = @"//burn:MsiPackage |//burn:MspPackage |//burn:MsuPackage |//burn:ExePackage";
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            BundleTests.VerifyElementOrder(burnManifestNodes, "FileName", packageNames);
        }

        /// <summary>
        /// Verifies MsiProperty information Burn_Manifest.xml
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="msiPackageId">Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedPropertyName">Expected MsiProperty @Name value.</param>
        /// <param name="expectedPropertyValue">Expected MsiProperty @Value value.</param>
        public static void VerifyMsiPropertyInformation(string embededResourcesDirectoryPath, string msiPackageId, string expectedPropertyName, string expectedPropertyValue)
        {
            string burnManifestXPath = string.Format(@"//burn:{0}[@Id='{1}']/burn:MsiProperty[@Id='{2}'] ", GetPackageElementName(PackageType.MSI), msiPackageId, expectedPropertyName);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No MsiProperty with the Id: '{0}' was found under MsiPackage: '{1}' in Burn_Manifest.xml.", expectedPropertyName, msiPackageId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Value", expectedPropertyValue);
        }

        /// <summary>
        /// Verify the pacakge Payload information in Burn_Manifest.xml
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedParentPackageType">Parent Package type.</param>
        /// <param name="expectedParentPackageName">Parent Package name; this is the attribute used to locate the package.</param>
        /// <param name="expectedFileName">Payload name; this is the attribute used to locate the payload.</param>
        /// <param name="expectedDownloadURL">@DownloadURL expected value.</param>
        /// <param name="acctualFilePath">Path to the acctual file to compate against file in cab.</param>
        public static void VerifyPackagePayloadInformation(string embededResourcesDirectoryPath, PackageTests.PackageType expectedParentPackageType, string expectedParentPackageName,string expectedParentPackageId, string expectedFileName, string expectedDownloadURL, string acctualFilePath)
        {
            string expectedFileSize = new FileInfo(acctualFilePath).Length.ToString();
            string expectedHash = FileVerifier.ComputeFileSHA1Hash(acctualFilePath);

            // find the Payload element 
            string payloadXPath = string.Format(@"//burn:Payload[@FilePath='{0}']", expectedFileName);
            XmlNodeList payloadNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, payloadXPath);
            Assert.True(1 == payloadNodes.Count, String.Format("No Package payload with the name: '{0}' was found in Burn_Manifest.xml.", expectedFileName));
            BundleTests.VerifyAttributeValue(payloadNodes[0], "FileSize", expectedFileSize);
            BundleTests.VerifyAttributeValue(payloadNodes[0], "Sha1Hash", expectedHash);
            BundleTests.VerifyAttributeValue(payloadNodes[0], "DownloadUrl", expectedDownloadURL);

            // make sure the payload is added to the package
            string payloadId = payloadNodes[0].Attributes["Id"].Value;
            string packagePayloadRefXPath = string.Format(@"//burn:{0}[@Id='{1}']/burn:PayloadRef[@Id='{2}']", GetPackageElementName(expectedParentPackageType), expectedParentPackageId, payloadId);
            XmlNodeList packagePayloadRefNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, packagePayloadRefXPath);
            Assert.True(1 == packagePayloadRefNodes.Count, String.Format("Package payload with the name: '{0}' was found under Package '{1}'.", expectedFileName, expectedParentPackageId));

            // verify the correct file is added to the attached container
            if (null == expectedDownloadURL)
            {
                string extractedFilePath = Path.Combine(Builder.AttachedContainerFolderName, expectedFileName);
                FileVerifier.VerifyFilesAreIdentical(acctualFilePath, extractedFilePath);
            }
        }

        /// <summary>
        /// Return the expected element name in burnManifest
        /// </summary>
        /// <param name="type">The type of the element</param>
        /// <param name="fileName">parameterInfo or burnManifest</param>
        /// <returns>Element name in the specified file.</returns>
        private static string GetPackageElementName(PackageType type)
        {
            switch (type)
            {
                case PackageType.MSI:
                    return "MsiPackage";
                case PackageType.MSP:
                    return "MspPackage";
                case PackageType.MSU:
                    return "MsuPackage";
                case PackageType.EXE:
                    return "ExePackage";
                default:
                    throw new ArgumentException(string.Format("Undefined PacakgeType : {0}", type.ToString()));
            };
        }

        #endregion
    }
}
