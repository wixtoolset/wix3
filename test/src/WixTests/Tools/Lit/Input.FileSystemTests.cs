//-----------------------------------------------------------------------
// <copyright file="Input.FileSystemTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//   Test how Lit handles different types of files
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Lit.Input
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;
    
    /// <summary>
    /// Test how Lit handles different types of files.
    /// </summary>
    [TestClass]
    public class FileSystemTests : WixTests
    {
        [TestMethod]
        [Description("Verify that Lit accepts input file path given as relative path.")]
        [Priority(1)]
        public void RelativePath()
        {
            string temporaryDirectory = Utilities.FileUtilities.GetUniqueFileName();
            DirectoryInfo workingDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, @"RelativePath\WorkingDirectory"));
            DirectoryInfo objectFileDirectory = Directory.CreateDirectory(Path.Combine(temporaryDirectory, @"RelativePath\ObjectFileDirectory"));

            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.OutputFile = Path.Combine(objectFileDirectory.FullName, "Fragment.wixobj");
            candle.Run();

            Lit lit = new Lit();
            lit.WorkingDirectory = workingDirectory.FullName;
            lit.ObjectFiles.Add(@"..\ObjectFileDirectory\Fragment.wixobj");
            lit.Run();
        }
       
        [TestMethod]
        [Description("Verify that Lit can handle a non existing input file")]
        [Priority(2)]
        public void NonExistingInputFile()
        {
            string testFile = Path.Combine(Utilities.FileUtilities.GetUniqueFileName(), "foo.wixobj");

            Lit lit = new Lit();
            lit.ObjectFiles.Add(testFile);
            string outputString = String.Format("The system cannot find the file '{0}' with type 'Source'.", testFile);
            lit.ExpectedWixMessages.Add(new WixMessage(103, outputString, WixMessage.MessageTypeEnum.Error));
            lit.ExpectedExitCode = 103;
            lit.Run();
        }

        [TestMethod]
        [Description("Verify that Lit can accept non-alphanumeric characters in the filename")]
        [Priority(2)]
        public void NonAlphaNumericCharactersInFileName()
        {
            DirectoryInfo temporaryDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());

            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.OutputFile = Path.Combine(temporaryDirectory.FullName, "~!@#$%^&()_-=+,.wixobj");
            candle.Run();

            Lit lit = new Lit(candle);
            lit.Run();
        }

        [TestMethod]
        [Description("Verify that Lit can accept read only files as input")]
        [Priority(2)]
        public void ReadOnlyInputFile()
        {
            string testFile = Candle.Compile(WixTests.PropertyFragmentWxs);

            // Set the file to readonly
            File.SetAttributes(testFile, FileAttributes.ReadOnly);

            Lit lit = new Lit();
            lit.ObjectFiles.Add(testFile);
            lit.Run();
        }

        [TestMethod]
        [Description("Verify that Lit can accept files from a network share")]
        [Priority(2)]
        [Ignore]
        public void MultipleFilesFromNetworkShare()
        {
        }

        [TestMethod]
        [Description("Verify that Lit accepts s large size input file.")]
        [Priority(2)]
        [Ignore]
        public void LargeSizeInputFile()
        {
        }

        [TestMethod]
        [Description("Verify that Lit can accept file names with single quotes")]
        [Priority(3)]
        public void FileNameWithSingleQuotes()
        {
            DirectoryInfo temporaryDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());

            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.OutputFile = Path.Combine(temporaryDirectory.FullName, "'BasicProduct'.wixobj");
            candle.Run();

            Lit lit = new Lit(candle);
            lit.Run();
        }

        [TestMethod]
        [Description("Verify that Lit can accepts an input file with space in its name")]
        [Priority(3)]
        public void FileNameWithSpace()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.Run();

            string testdirectoryName = Utilities.FileUtilities.GetUniqueFileName();
            Directory.CreateDirectory(testdirectoryName);
            string testFile = Path.Combine(testdirectoryName, "  Simple Fragment                           .wixobj");
            File.Copy(candle.OutputFile, testFile);

            Lit lit = new Lit();
            lit.ObjectFiles.Add(testFile);
            lit.Run();
        }

        [TestMethod]
        [Description("Verify that Lit fails gracefully in case of input file on a network share with no permissions.")]
        [Priority(3)]
        [Ignore]
        public void NetworkShareNoPermissions()
        {
        }
 
        [TestMethod]
        [Description("Verify that Lit can accept an input file from a URI path")]
        [Priority(3)]
        [Ignore]
        public void URI()
        {
        }
    }
}