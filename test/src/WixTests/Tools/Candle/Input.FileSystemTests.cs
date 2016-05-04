// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.Input
{
    using System;
    using System.IO;
    using WixTest;
    using Xunit;
    
    /// <summary>
    /// Test how Candle handles different types of files.
    /// </summary>
    public class FileSystemTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\Input\FileSystemTests");

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle fails gracefully in case of input file on a network share with no permissions.")]
        [Priority(3)]
        public void NetworkShareNoPermissions()
        {
        }

        [NamedFact]
        [Description("Verify that Candle accepts input file path given as relative path.")]
        [Priority(1)]
        public void RelativePath()
        {
            string temporaryDirectory = Utilities.FileUtilities.GetUniqueFileName();

            DirectoryInfo workingDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, @"RelativePath\WorkingDirectory"));
            DirectoryInfo sourceFileDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, @"RelativePath\SourceFileDirectory"));

            string sourceFile = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs");
            string destinationFile = Path.Combine(sourceFileDirectory.FullName, "BasicProduct.wxs");
            File.Copy(sourceFile, destinationFile);

            Candle candle = new Candle();
            candle.WorkingDirectory = workingDirectory.FullName;
            candle.SourceFiles.Add(@"..\SourceFileDirectory\BasicProduct.wxs");
            candle.Run();
        }
       
        [NamedFact]
        [Description("Verify that Candle can handle a non existing input file")]
        [Priority(2)]
        public void NonExistingInputFile()
        {
            // Retrieving the path to a temporary directory
            string temporaryDirectory = Utilities.FileUtilities.GetUniqueFileName();
            string testFile = Path.Combine(temporaryDirectory, "foo.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            string outputString = String.Format("The system cannot find the file '{0}' with type 'Source'.", testFile);
            candle.ExpectedWixMessages.Add(new WixMessage(103, outputString, WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 103;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle can accept alphanumeric characters in the filename")]
        [Priority(2)]
        public void NonAlphaNumericCharactersInFileName()
        {
            DirectoryInfo temporaryDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());
            string sourceFile = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs");
            string destinationFile = Path.Combine(temporaryDirectory.FullName, "#@%+BasicProduct.wxs");
            File.Copy(sourceFile, destinationFile, true);
            Candle.Compile(destinationFile);
        }

        [NamedFact]
        [Description("Verify that Candle can accept read only files as input")]
        [Priority(2)]
        public void ReadOnlyInputFile()
        {
            // Retrieving the path to a temporary directory
            string testFile = Path.Combine(Path.GetTempPath(), String.Concat(Path.GetTempFileName(),".wxs"));
            string sourceFile = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs");
            File.Copy(sourceFile, testFile, true);

            // Set the file to readonly
            File.SetAttributes(testFile, FileAttributes.ReadOnly);

            Candle.Compile(testFile);
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle can accept files from a network share")]
        [Priority(2)]
        public void MultipleFilesFromNetworkShare()
        {
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle accepts s large size input file.")]
        [Priority(2)]
        public void LargeSizeInputFile()
        {
        }

        [NamedFact]
        [Description("Verify that Candle can accept file names with single quotes")]
        [Priority(3)]
        public void FileNameWithSingleQuotes()
        {

            string testFile = Path.Combine(FileSystemTests.TestDataDirectory, @"FileNameWithSingleQuotes\'Product'.wxs");
            Candle.Compile(testFile);
        }
               
        [NamedFact]
        [Description("Verify that Candle can accepts a small size input file")]
        [Priority(3)]
        public void SmallSizeInputFile()
        {
            string testFile = Path.Combine(FileSystemTests.TestDataDirectory, @"SmallSizeInputFile\Product.wxs");
            Candle.Compile(testFile);
        }

        [NamedFact]
        [Description("Verify that Candle can accepts an input file with space in its name")]
        [Priority(3)]
        public void FileNameWithSpace()
        {
            string testFile = Path.Combine(FileSystemTests.TestDataDirectory, @"FileNameWithSpace\Pro  duct.wxs");
            Candle.Compile(testFile);
        }

        [NamedFact]
        [Description("Verify that Candle accepts an input file path that is more than 256 characters")]
        [Priority(3)]
        public void LongFilePath()
        {
            //the max length of a path is 247 chars, the filepath is 259 chars

            string InputPath = Utilities.FileUtilities.GetUniqueFileName();
            //add initial 170 chars
            InputPath = Path.Combine(InputPath, @"FilePathNewfolder11(20chars)\Newfolder12(20chars)Newfolder12(20chars)Newfolder13(20chars)Newfolder13(20chars)\Newfolder14(20chars)Newfolder15(20chars)Newfolder16(20chars)");

            int i = 245 - InputPath.Length;
            while (i > 0)
            {
                InputPath = Path.Combine(InputPath, @"pt");
                i = 245 - InputPath.Length;
            }
            if (InputPath.Length < 246)
            {
                InputPath = Path.Combine(InputPath, "T");
            }

            Assert.True(InputPath.Length < 248, "The output path is not less than 248 chars");
            Directory.CreateDirectory(InputPath);

            //copy wxs file to target input path
            File.Copy(Path.Combine(FileSystemTests.TestDataDirectory, @"LongFilePath\Product.wxs"), Path.Combine(InputPath,"Product.wxs"));

            //make sure the input file path is between 256 and 260;
            string testFile = Path.Combine(InputPath, "Product.wxs");
            Assert.True((testFile.Length > 256) && (testFile.Length < 260), String.Format("The intput filepath length {0} is not between 256 and 260 chars",testFile .Length ));

            Candle.Compile(testFile);
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle can accept an input file from a URI path")]
        [Priority(3)]
        public void URI()
        {
        }
    }
}
