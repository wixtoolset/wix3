//-----------------------------------------------------------------------
// <copyright file="InstallPackages.PackageTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the Package element as it applies to the Product element.
//     Summary Information and Compression are the main areas to test.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.InstallPackages
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for the Package element as it applies to the Product element
    /// </summary>
    [TestClass]
    public class PackageTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\InstallPackages\PackageTests");

        [TestMethod]
        [Description("Verify that a simple MSI can be built and that the expected default values are set")]
        [Priority(1)]
        public void SimplePackage()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"SimplePackage\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string packageDesc = Verifier.GetMsiSummaryInformationProperty(msi, Verifier.MsiSummaryInformationProperty.Subject);
            string packageInstallVer = Verifier.GetMsiSummaryInformationProperty (msi, Verifier.MsiSummaryInformationProperty.Schema);

            Assert.IsTrue("This package is used for testing purposes" == packageDesc, packageDesc + "didn't match the expected string in wix source file");
            Assert.IsTrue("201" == packageInstallVer, packageInstallVer + "didn't match the expected string in wix source file");
        }

        [TestMethod]
        [Description("Verify that a package can compress its files in a cab")]
        [Priority(1)]
        public void CompressedPackage()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"CompressedPackage\product.wxs"));
            candle.Run();
            
            Light light = new Light(candle);
            light.Run();

            Verifier.VerifyResults(Path.Combine(PackageTests.TestDataDirectory, @"CompressedPackage\expected.msi"), light.OutputFile);

            string expectedCab = Path.Combine(Path.GetDirectoryName(light.OutputFile), "product.cab");
            Assert.IsTrue(File.Exists(expectedCab), "The expected cab file {0} does not exist", expectedCab);
        }

        [TestMethod]
        [Description("Verify that a package Id can be static or auto-generated")]
        [Priority(2)]
        public void PackageIds()
        {
            // These are the valid package Ids that will be tested
            Dictionary<string, Regex> ids = new Dictionary<string, Regex>();
            ids.Add("{E3B6D482-3AB1-4246-BA97-4D75CF4F55F1}", new Regex("^{E3B6D482-3AB1-4246-BA97-4D75CF4F55F1}$"));
            ids.Add("E3B6D482-3AB1-4246-BA97-4D75CF4F55F1", new Regex("^{E3B6D482-3AB1-4246-BA97-4D75CF4F55F1}$"));
            ids.Add("aaaaaaaa-bbbb-cccc-dddd-eeeeeeffffff", new Regex("^{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEFFFFFF}$"));
            ids.Add("*", new Regex("^{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}}$"));

            foreach (string id in ids.Keys)
            {
                Candle candle = new Candle();
                candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"PackageIds\product.wxs"));
                
                // Set a preprocessor variable that defines the package code
                candle.PreProcessorParams.Add("PackageId", id);

                candle.IgnoreExtraWixMessages = true;
                candle.Run();

                Light light = new Light(candle);
                light.Run();

                // Verify that the package code was set properly
                string packageId = Verifier.GetMsiSummaryInformationProperty (light.OutputFile, Verifier.MsiSummaryInformationProperty.PackageCode);
                Assert.IsTrue(ids[id].IsMatch(packageId), "The Summary Info property {0} in {1} with a value of {2} does not match the regular expression {3}", (int)Verifier.MsiSummaryInformationProperty.PackageCode , light.OutputFile, packageId, ids[id].ToString()); 
            }
        }

        [TestMethod]
        [Description("Verify that a package can support any of the three platforms intel, intel64 and x64")]
        [Priority(2)]
        public void Platforms()
        {
            List<string> platform = new List<string>();
            platform.AddRange(new string[] { "x86", "ia64", "x64", "intel", "intel64"});
            string platformValue;
          
            foreach (string value in platform)
            {
                Candle candle = new Candle();
                candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"Platforms\product.wxs"));

                // Set a preprocessor variable that defines the CodepageValue
                candle.PreProcessorParams.Add("Platform", value);
                candle.IgnoreExtraWixMessages = true;
                candle.Run();

                Light light = new Light(candle);
                light.Run();
                platformValue = Verifier.GetMsiSummaryInformationProperty(light.OutputFile, Verifier.MsiSummaryInformationProperty.TargetPlatformAndLanguage);
                Assert.IsTrue(platformValue.ToLower().Contains("intel") || platformValue.ToLower ().Contains("intel64") || platformValue.ToLower ().Contains("x64"), "platform Value didn't match.expected:{0},actual:{1}.", value, platformValue);
            }
        }

        [TestMethod]
        [Description("Verify that there is an error if an invalid platform is specified")]
        [Priority(3)]
        public void InvalidPlatform()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"InvalidPlatform\product.wxs"));
            candle.ExpectedExitCode = 265;
            candle.ExpectedWixMessages.Add(new WixMessage(265, "The Platform attribute has an invalid value abc.  Possible values are x86, x64, or ia64.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that install privileges can be specified on a package")]
        [Priority(3)]
        public void InstallPrivileges()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"InstallPrivileges\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string wordcount = Verifier.GetMsiSummaryInformationProperty(msi, Verifier.MsiSummaryInformationProperty.WordCount);
            byte installPrivileges;
            if (byte.TryParse(wordcount, out installPrivileges))
            {
                if (0 != installPrivileges)
                {
                    Assert.Fail("Setting install Privileges failed.");
                }
            }
            else
            {
                Assert.Fail("Failed to fetch wordcount from msi");
            }
        }

        [TestMethod]
        [Description("Verify that the source can be an admin image")]
        [Priority(2)]
        public void AdminImage()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"AdminImage\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.IgnoreExtraWixMessages = true;
            light.Run();
            string wordcount = Verifier.GetMsiSummaryInformationProperty(light .OutputFile , Verifier.MsiSummaryInformationProperty.WordCount);
            byte adminImage;
            if (byte.TryParse(wordcount, out adminImage))
            {
                if (4 != adminImage)
                {
                    Assert.Fail("Setting Admin Image failed.");
                }
            }
            else
            {
                Assert.Fail("Failed to fetch wordcount from msi");
            }
        }

        [TestMethod]
        [Timeout(3600000)]
        [Description("Verify that installer version can be set to any valid version")]
        [Priority(2)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2990011&group_id=105970&atid=642714")]
        public void InstallerVersion()
        {
            Random random = new Random(WixTests.Seed);
            for (int i = 1; i < 10; i++)
            {
                string version = random.Next (100,500).ToString ();
                Candle candle = new Candle();
                candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"InstallerVersion\product.wxs"));

                // Set a preprocessor variable that defines the CodepageValue
                candle.PreProcessorParams.Add("InstallerVersion", version);

                candle.IgnoreExtraWixMessages = true;
                candle.Run();

                Light light = new Light(candle);
                light.Run();
                string packageInstallVer = Verifier.GetMsiSummaryInformationProperty(light.OutputFile, Verifier.MsiSummaryInformationProperty.Schema);
                Assert.IsTrue(version == packageInstallVer, packageInstallVer + "didn't match the expected installer version");
            }
        }

        [TestMethod]
        [Description("Verify that an invalid installer version cannot be set")]
        [Priority(3)]
        public void InvalidInstallerVersion()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(PackageTests.TestDataDirectory, @"InvalidInstallerVersion\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 216;
            light.ExpectedWixMessages.Add(new WixMessage(216, "An unexpected Win32 exception with error code 0x64D occurred: This installation package cannot be installed by the Windows Installer service. You must install a Windows service pack that contains a newer version of the Windows Installer service", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }

        [TestMethod]
        [Description("Verify that short filenames can be in the source")]
        [Priority(3)]
        public void ShortNames()
        {
            string sourceFile = Path.Combine(PackageTests.TestDataDirectory, @"ShortNames\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string wordcount = Verifier.GetMsiSummaryInformationProperty(msi, Verifier.MsiSummaryInformationProperty.WordCount);
            byte shortname;
            if (byte.TryParse(wordcount, out shortname))
            {
                if (1 != shortname)
                {
                    Assert.Fail("Setting short file name failed.");
                }
            }
            else
            {
                Assert.Fail("Failed to fetch wordcount from msi");
            }
        }
    }
}