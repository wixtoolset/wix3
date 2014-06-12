//-----------------------------------------------------------------------
// <copyright file="Bundle.PayloadTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle Payload element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// Tests for Bundle Payload element
    /// </summary>
    public class PayloadTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\PayloadTests");

        [NamedFact]
        [Description("Name is optional and will default to the SourceFile file name")]
        [Priority(2)]
        public void PayloadNameNotSpecified()
        {
            string sourceFile = Path.Combine(PayloadTests.TestDataDirectory, @"PayloadNameNotSpecified\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Bootstrapper.exe");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            UXTests.VerifyUXPayloadInformation(outputDirectory, testFile, "Bootstrapper.exe");
        }

        [NamedFact]
        [Description("Name can be explicitly defined.")]
        [Priority(2)]
        public void PayloadNameSpecified()
        {
            string sourceFile = Path.Combine(PayloadTests.TestDataDirectory, @"PayloadNameSpecified\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Bootstrapper.exe");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            UXTests.VerifyUXPayloadInformation(outputDirectory, testFile, "Setup.exe");
        }

        [NamedFact]
        [Description("Verify that there is an error if the Payload SourceFile attribute has an invalid file name")]
        [Priority(3)]
        public void PayloadInvalidSourceFile()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadInvalidSourceFile\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(300, "Illegal characters in path 'Setup|*.exe'. Ensure you provided a valid path to the file.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 300;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that there is an error if the Payload SourceFile attribute has an invalid file name")]
        [Priority(3)]
        [Trait("Bug Link", @"https://sourceforge.net/tracker/?func=detail&aid=2980332&group_id=105970&atid=642714")]
        public void PayloadInvalidName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadInvalidName\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The Payload/@Name attribute's value, 'Setup|*.exe', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 346;
            candle.Run();
        }

        [NamedFact]
        [Description("SourceFile can be defined as relative to the current working directory")]
        [Priority(2)]
        public void PayloadRelativeSourceFilePath()
        {
            string sourceFile = Path.Combine(PayloadTests.TestDataDirectory, @"PayloadRelativeSourceFilePath\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Bootstrapper.exe");
            string outputDirectory = this.TestContext.TestDirectory;

            // Copy a file to the current directory. This file is used to verify relative paths in source files.
            File.Copy(testFile, Path.Combine(outputDirectory, "Bootstrapper.exe"), true);

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            UXTests.VerifyUXPayloadInformation(outputDirectory, testFile, "Bootstrapper.exe");
        }

        [NamedFact(Skip = "Ignore")]
        [Description("SourceFile can be defined as a UNC path.")]
        [Priority(3)]
        public void PayloadUNCSourceFile()
        {
        }

        [NamedFact]
        [Description("Nonexistent SourceFile produces an error.")]
        [Priority(3)]
        public void PayloadNonexistentSourceFilePath()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadNonexistentSourceFilePath\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(103,"The system cannot find the file 'NonExistentFileBootstrapper.exe' with type ''.",Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 103;
            light.Run();
        }

        [NamedFact]
        [Description("SourceFile attribute is not defined.")]
        [Priority(3)]
        // bug# https://sourceforge.net/tracker/?func=detail&aid=2980338&group_id=105970&atid=642714
        public void PayloadMissingSourceFileAttribute()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadMissingSourceFileAttribute\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Payload/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("SourceFile attribute can not be empty.")]
        [Priority(3)]
        public void PayloadEmptySourceFileAttribute()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadEmptySourceFileAttribute\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(6, "The Payload/@SourceFile attribute's value cannot be an empty string.  If you want the value to be null or empty, simply remove the entire attribute.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 6;
            candle.Run();
        }

        [NamedFact]
        [Description("PayloadGroup Id attribute is required.")]
        [Priority(3)]
        public void PayloadGroupMissingId()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupMissingId\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The PayloadGroup/@Id attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("PayloadGroup Id attribute is required.")]
        [Priority(3)]
        public void PayloadGroupDuplicateId()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupDuplicateId\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "Setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, "Duplicate symbol 'PayloadGroup:PayloadGroup1' found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, "Location of symbol related to previous error.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if two child Payloads are the same file")]
        [Priority(3)]
        public void PayloadGroupDuplicatePayloads()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupDuplicatePayloads\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if two child PayloadGroups contain the same file")]
        [Priority(3)]
        public void PayloadGroupDuplicatePayloadInPayloadGroups()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupDuplicatePayloadInPayloadGroups\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if two child Payloads are the same file")]
        [Priority(3)]
        public void PayloadGroupDuplicatePayloadGroupRefs()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupDuplicatePayloadGroupRefs\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(343, Message.MessageTypeEnum.Error)); // Circular reference error
            light.ExpectedExitCode = 343;
            light.IgnoreWixMessageOrder = true;
            light.Run();
        }

        [NamedFact]
        [Description("Cannot have recursive ref to same payload group")]
        [Priority(3)]
        public void PayloadGroupRecursiveRefs()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupRecursiveRefs\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(86, @"A circular reference of groups was detected. The infinite loop includes: PayloadGroup:PayloadGroup1 -> PayloadGroup:PayloadGroup2 -> PayloadGroup:PayloadGroup1. Group references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 86;
            light.IgnoreWixMessageOrder = true;
            light.Run();
        }

        [NamedFact]
        [Description("Nested PayloadGroups are allowed.")]
        [Priority(2)]
        public void NestedPayloadGroups()
        {
            string sourceFile = Path.Combine(PayloadTests.TestDataDirectory, @"NestedPayloadGroups\Product.wxs");
            string PayloadFile1 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile1.txt");
            string PayloadFile2 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile2.txt");
            string PayloadFile3 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile3.txt");
            string PayloadFile4 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile4.txt");
            string PayloadFile5 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile5.txt");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            UXTests.VerifyUXPayloadInformation(outputDirectory, PayloadFile1, "PayloadFile1.txt", false);
            UXTests.VerifyUXPayloadInformation(outputDirectory, PayloadFile2, "PayloadFile2.txt", false);
            UXTests.VerifyUXPayloadInformation(outputDirectory, PayloadFile3, "PayloadFile3.txt", false);
            UXTests.VerifyUXPayloadInformation(outputDirectory, PayloadFile4, "PayloadFile4.txt", false);
            UXTests.VerifyUXPayloadInformation(outputDirectory, PayloadFile5, "PayloadFile5.txt", false);
        }

        [NamedFact]
        [Description("Verify that build fails if a PayloadGroupRef element is missing an Id")]
        [Priority(3)]
        public void PayloadGroupRefMissingId()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PayloadTests.TestDataDirectory, @"PayloadGroupRefMissingId\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, @"The PayloadGroupRef/@Id attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that build fails if a PayloadGroupRef is pointing to a missing Payload")]
        [Priority(3)]
        public void MissingPayloadGroupRef()
        {
            string candleOutput = Candle.Compile(Path.Combine(PayloadTests.TestDataDirectory, @"MissingPayloadGroupRef\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(94, Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 94;
            light.Run();
        }

        [NamedFact]
        [Description("Payload can have @DownloadURL.")]
        [Priority(2)]
        public void ValidPayloadDownloadURL()
        {
            string sourceFile = Path.Combine(PayloadTests.TestDataDirectory, @"ValidPayloadDownloadURL\Product.wxs");
            string PayloadFile1 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile1.txt");
            string PayloadFile2 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile2.txt");
            string PayloadFile3 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile3.txt");
            string PayloadFile4 = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"UXPayload\PayloadFile4.txt");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify the ParameterInfo and burnManifest has the correct information 
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSI, "MsiPackage.msi", "MsiPackage", "PayloadFile1.txt", @"http://go.microsoft.com/fwlink/?linkid=164202", PayloadFile1);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSI, "MsiPackage.msi", "MsiPackage", "PayloadFile2.txt", @"http://localhost/testPayload", PayloadFile2);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSI, "MsiPackage.msi", "MsiPackage", "PayloadFile3.txt", @"ftp://192.168.0.1/testPayload.exe", PayloadFile3);
            PackageTests.VerifyPackagePayloadInformation(outputDirectory, PackageTests.PackageType.MSI, "MsiPackage.msi", "MsiPackage", "PayloadFile4.txt", @"file://wixbuild/releases/wix/", PayloadFile4);
        }
    }
}
