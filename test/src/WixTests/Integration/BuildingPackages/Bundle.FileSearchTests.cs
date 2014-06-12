//-----------------------------------------------------------------------
// <copyright file="Bundle.FileSearchTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle FileSearch element
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
    /// Tests for Bundle FileSearch element
    /// </summary>
    public class FileSearchTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\FileSearchTests");

        [NamedFact]
        [Description("FileSearch Variable is required.")]
        [Priority(3)]
        public void FileSearchVariableMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchVariableMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:FileSearch/@Variable attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("FileSearch Path is required.")]
        [Priority(3)]
        public void FileSearchPathMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchPathMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The util:FileSearch/@Path attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("FileSearch @Path contains an invalid  Path.")]
        [Priority(3)]
        [Trait("Bug Link", @"https://sourceforge.net/tracker/?func=detail&aid=2980327&group_id=105970&atid=642714")]
        public void FileSearchInvalidPath()
        {
            string sourceFile = Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchInvalidPath\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The util:FileSearch/@Path attribute's value, '%windir%\\System|*32\\mscoree.dll', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 346;
            candle.Run();
        }

        [NamedFact]
        [Description("FileSearch @Variable should not be a predefined variable.")]
        [Priority(3)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2980329&group_id=105970")]
        public void FileSearchPredefinedVariable()
        {
            string expectedErrorMessage = @"The util:FileSearch/@Variable attribute's value, 'AdminToolsFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFilesFolder', 'CompatibilityMode', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'LocalAppDataFolder', 'MyPicturesFolder', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProgramFilesFolder', 'ProgramMenuFolder', 'SendToFolder', 'StartMenuFolder', 'StartupFolder', 'SystemFolder', 'TempFolder', 'TemplateFolder', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsFolder', or 'WindowsVolume'.";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchPredefinedVariable\Product.wxs"));
            candle.OutputFile = "Setup.exe";
            candle.Extensions.Add("WixUtilExtension");
            candle.ExpectedWixMessages.Add(new WixMessage(348, expectedErrorMessage, Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 348;
            candle.Run();
        }

        [NamedFact]
        [Description("FileSearch @Result contains invalid value (something other than Exists)")]
        [Priority(3)]
        public void FileSearchInvalidResultValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchInvalidResultValue\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The util:FileSearch/@Result attribute's value, 'NotExists', is not one of the legal options: 'Exists', or 'Version'.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 21;
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();
        }

        [NamedFact]
        [Description("Cannot have dupplicate FileSearch with the same id.")]
        [Priority(3)]
        public void DuplicateFileSearch()
        {
            string sourceFile = Path.Combine(FileSearchTests.TestDataDirectory, @"DuplicateFileSearch\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceFile }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(91, Message.MessageTypeEnum.Error));  //  duplicate symbol error
            light.ExpectedWixMessages.Add(new WixMessage(92, Message.MessageTypeEnum.Error));  //  Location of symbol related to previous error.
            light.ExpectedExitCode = 92;
            light.IgnoreWixMessageOrder = true;
            light.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a missing search.")]
        [Priority(3)]
        public void FileSearchAfterUndefinedSearch()
        {
            string sourceFile = Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchAfterUndefinedSearch\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceFile }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(94, Message.MessageTypeEnum.Error));  //  Unresolved reference to symbol 'WixSearch:UndefinedSearch'
            light.ExpectedExitCode = 94;
            light.Run();
        }

        [NamedFact]
        [Description("After contains an Id of a search after this search.")]
        [Priority(3)]
        public void FileSearchRecursiveAfter()
        {
            string sourceFile = Path.Combine(FileSearchTests.TestDataDirectory, @"FileSearchRecursiveAfter\Product.wxs");
            string[] candleOutputs = Candle.Compile(sourceFiles: new string[] { sourceFile }, extensions: new string[] { "WixUtilExtension" });

            Light light = new Light();
            light.ObjectFiles.AddRange(candleOutputs);
            light.OutputFile = "Setup.exe";
            light.Extensions.Add("WixUtilExtension");
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: FileSearch1 -> FileSearch2 -> FileSearch1. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error)); 
            light.ExpectedWixMessages.Add(new WixMessage(5060, "A circular reference of search ordering constraints was detected: FileSearch2 -> FileSearch1 -> FileSearch2. Search ordering references must form a directed acyclic graph.", Message.MessageTypeEnum.Error)); 
            light.ExpectedExitCode = 5060;
            light.Run();
        }

        [NamedFact]
        [Description("Valid FileSearch.")]
        [Priority(2)]
        public void ValidFileSearch()
        {
            string sourceFile = Path.Combine(FileSearchTests.TestDataDirectory, @"ValidFileSearch\Product.wxs");
            string outputDirectory = this.TestContext.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile, new string[] { "WixUtilExtension" });

            // verify the ParameterInfo and burnManifest has the correct information 
            FileSearchTests.VerifyFileSearchInformation(outputDirectory, "FileSearch1", @"%windir%\System\mscoree.dll", "variable1", "true", "version");
            FileSearchTests.VerifyFileSearchInformation(outputDirectory, "FileSearch2", @"%windir%\System32\mscoree.dll", "variable2", null, "exists");
            FileSearchTests.VerifyFileSearchInformation(outputDirectory, "FileSearch3", @"%windir%\SysWOW64\mscoree.dll", "variable3", "variable1=0.0.0.0", "exists");

            FileSearchTests.VerifyFileSearchOrder(outputDirectory, "FileSearch1", "FileSearch3", "FileSearch2");
        }

        #region Verification Methods

        /// <summary>
        /// Verifies FileSearch information in Burn_Manifest.xml.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="expectedId">Expected FileSearch @Id; this attribute is used to search for the element.</param>
        /// <param name="expectedPath">Expected FileSearch @Path value.</param>
        /// <param name="expectedVariableName">Expected FileSearch @Variable value.</param>
        /// <param name="expectedCondition">Expected FileSearch @Condition value.</param>
        /// <param name="expectedResult">Expected FileSearch @Attribute value.</param>
        public static void VerifyFileSearchInformation(string embededResourcesDirectoryPath, string expectedId, string expectedPath, string expectedVariableName, string expectedCondition, string expectedResult)
        {
            string burnManifestXPath = string.Format(@"//burn:FileSearch[@Id='{0}']", expectedId);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);
            Assert.True(1 == burnManifestNodes.Count, String.Format("No FileSearch with the Id: '{0}' was found in Burn_Manifest.xml.", expectedId));
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Path", expectedPath);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Variable", expectedVariableName);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Type", expectedResult);
            BundleTests.VerifyAttributeValue(burnManifestNodes[0], "Condition", expectedCondition);
        }

        /// <summary>
        /// Verify FileSearch elements appear in a specific order
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are.</param>
        /// <param name="fileSearchIds">Ids of the FileSearch elements in order.</param>
        public static void VerifyFileSearchOrder(string embededResourcesDirectoryPath, params string[] fileSearchIds)
        {
            BundleTests.VerifyBurnManifestElementOrder(embededResourcesDirectoryPath, "FileSearch", "Id", fileSearchIds);
        }

        #endregion
    }
}
