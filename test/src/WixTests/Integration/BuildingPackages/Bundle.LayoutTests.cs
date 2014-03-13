//-----------------------------------------------------------------------
// <copyright file="Bundle.LayoutTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Bundle Layout elements (LayoutDirectory, LayoutFile, LayoutDirectoryRef)
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for Bundle Layout elements (LayoutDirectory, LayoutFile, LayoutDirectoryRef)
    /// </summary>
    [TestClass]
    public class LayoutTests : BundleTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\LayoutTests");

        /* LayoutDirectory tests */

        [TestMethod]
        [Description("@Name is required.")]
        [Priority(3)]
        public void LayoutDirectoryNameMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutDirectoryNameMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The LayoutDirectory/@Name attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the LayoutDirectory Name attribute has an invalid directory name")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2980722&group_id=105970&atid=642714")]
        public void LayoutDirectoryInvalidName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutDirectoryInvalidName\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(346, "The LayoutDirectory/@Name attribute's value, 'Output?*|Directory', is not a valid relative long name because it contains illegal characters.  Legal relative long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 346;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the LayoutDirecotory is redifined.")]
        [Priority(3)]
        public void DuplicateLayoutDirectory()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"DuplicateLayoutDirectory\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(91, "Duplicate symbol 'WixLayoutDirectory:LayoutDirectory1' found.", Message.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, "Location of symbol related to previous error.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        /* LayoutFile tests */

        [TestMethod]
        [Description("@SourceFile can be defined as relative to the current working directory")]
        [Priority(2)]
        public void LayoutFileRelativeSourceFilePath()
        {
            string sourceFile = Path.Combine(LayoutTests.TestDataDirectory, @"LayoutFileRelativeSourceFilePath\Product.wxs");
            string testFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"LayoutDirectory\RootOfLayoutDirectory.txt");
            string outputDirectory = this.TestDirectory;

            // Copy a file to the current directory. This file is used to verify relative paths in source files.
            File.Copy(testFile, Path.Combine(outputDirectory, "RootOfLayoutDirectory.txt"), true);

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify layout was created
            LayoutTests.VerifyLayoutDirectory(Path.Combine(outputDirectory, @"OutputDirectory"), null, new string[] { "RootOfLayoutDirectory.txt" });
        }

        [TestMethod]
        [Description("@SourceFile is required.")]
        [Priority(3)]
        public void LayoutFileSourceFileMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutFileSourceFileMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The LayoutFile/@SourceFile attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [TestMethod]
        [Description("@SourceFile can be defined as a UNC path.")]
        [Priority(3)]
        [Ignore]
        public void LayoutFileUNCSourceFile()
        {
        }

        [TestMethod]
        [Description("Verify that there is an error if the Layout @Name attribute has an invalid file name")]
        [Priority(3)]
        public void LayoutFileInvalidName()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutFileInvalidName\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(27, "The LayoutFile/@Name attribute's value, 'File?*|Name.txt', is not a valid long name because it contains illegal characters.  Legal long names contain no more than 260 characters and must contain at least one non-period character.  Any character except for the follow may be used: \\ ? | > < : / * \".", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 27;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the Layout @SourceFile attribute has an invalid file name")]
        [Priority(3)]
        [TestProperty("Bug Link","https://sourceforge.net/tracker/index.php?func=detail&aid=2980803&group_id=105970&atid=642714")]
        public void LayoutFileInvalidSourceFile()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutFileInvalidSourceFile\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(300, "Illegal characters in path 'FileSource|*?.txt'. Ensure you provided a valid path to the file.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 300;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the Layout @SourceFile attribute points to a file that does not exist.")]
        [Priority(3)]
        public void LayoutFileNonexistentSourceFile()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutFileNonexistentSourceFile\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(103, "The system cannot find the file 'NonexistingFile' with type ''.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 103;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the LayoutFile is redifined.")]
        [Priority(3)]
        public void DuplicateLayoutFile()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"DuplicateLayoutFile\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'LayoutDirectory1/RootOfLayoutDirectory.txt' is duplicated in table 'WixLayoutFile'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 130;
            light.Run();
        }

        /* LayoutDirectoryRef tests */

        [TestMethod]
        [Description("@Id is required.")]
        [Priority(3)]
        public void LayoutDirectoryRefIdMissing()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutDirectoryRefIdMissing\Product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The LayoutDirectoryRef/@Id attribute was not found; it is required.", Message.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if @Id points to a nonexisting LayoutDirectory.")]
        [Priority(3)]
        public void LayoutDirectoryRefNonexistingId()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"LayoutDirectoryRefNonexistingId\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(94, Message.MessageTypeEnum.Error)); //Unresolved reference to symbol error
            light.ExpectedExitCode = 94;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if @Id points to a nonexisting LayoutDirectory.")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2981229&group_id=105970&atid=642714")]
        public void RecursiveLayoutDirectoryRef()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"RecursiveLayoutDirectoryRef\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(332, @"A circular reference of layout directories was detected. The infinite loop includes: OutputDirectory1 and OutputDirectory2. layout directories references must form a directed acyclic graph.", Message.MessageTypeEnum.Error));
            light.ExpectedExitCode = 332;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the same LayoutDirectory is referenced twice.")]
        [Priority(3)]
        public void DuplicateLayoutDirectoryRef()
        {
            string candleOutput = Candle.Compile(Path.Combine(LayoutTests.TestDataDirectory, @"DuplicateLayoutDirectoryRef\Product.wxs"));

            Light light = new Light();
            light.ObjectFiles.Add(candleOutput);
            light.OutputFile = "setup.exe";
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'LayoutDirectory1/LayoutDirectory2' is duplicated in table 'WixLayoutDirRef'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", Message.MessageTypeEnum.Error)); //Unresolved reference to symbol error
            light.ExpectedExitCode = 130;
            light.Run();
        }

        /* General Layout tests */

        [TestMethod]
        [Description("Nested LayoutDirectories are allowed")]
        [Priority(2)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2981263&group_id=105970&atid=642714")]
        public void NestedLayoutDirectories()
        {
            string sourceFile = Path.Combine(LayoutTests.TestDataDirectory, @"NestedLayoutDirectories\Product.wxs");
            string outputDirectory = this.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify layout was created correctely
            string rootDirectory = Path.Combine(outputDirectory, @"LayoutRoot");
            LayoutTests.VerifyLayoutDirectory(rootDirectory, new string[] { "SubDir" }, new string[] { "autoexec.exe" });

            string subDirectory = Path.Combine(rootDirectory, @"SubDir");
            LayoutTests.VerifyLayoutDirectory(subDirectory, new string[] { "LayoutDirectoryFromRef" }, new string[] { "InSubdirectory.txt" });

            string layoutDirectoryFromRef = Path.Combine(subDirectory, @"LayoutDirectoryFromRef");
            LayoutTests.VerifyLayoutDirectory(layoutDirectoryFromRef, new string[] { "1" }, null);

            string layoutDirInFragment = Path.Combine(layoutDirectoryFromRef, @"1\2\3");
            LayoutTests.VerifyLayoutDirectory(layoutDirInFragment, new string[] { "2" }, new string[] { "InLayoutDirectoryInFragment.txt" });

            string layoutDirInFragment2 = Path.Combine(layoutDirInFragment, @"2\3");
            LayoutTests.VerifyLayoutDirectory(layoutDirInFragment2, null, new string[] { "Renamed.txt" });
        }

        [TestMethod]
        [Description("Empty LayoutDirectory is created correctelly.")]
        [Priority(2)]
        [TestProperty("Bug Link","https://sourceforge.net/tracker/?func=detail&aid=2980757&group_id=105970&atid=642714")]
        public void EmptyLayoutDirectory()
        {
            string sourceFile = Path.Combine(LayoutTests.TestDataDirectory, @"EmptyLayoutDirectory\Product.wxs");
            string outputDirectory = this.TestDirectory;

            // build the bootstrapper
            string bootstrapper = Builder.BuildBundlePackage(outputDirectory, sourceFile);

            // verify layout was created correctely
            string emptyLayoutDirectory = Path.Combine(outputDirectory, @"EmptyLayoutDirectory");
            LayoutTests.VerifyLayoutDirectory(emptyLayoutDirectory, null, null);
        }

        #region Verification Methods

        /// <summary>
        /// Verify that a LayoutDirectory was created correctely with the expected structure
        /// </summary>
        /// <param name="directoryPath">Expected layout directory path.</param>
        /// <param name="expectedSubdirectories">List of expected subdirectories of the layout directory.</param>
        /// <param name="expectedFiles">List of expected files of the layout directory.</param>
        public static void VerifyLayoutDirectory(string directoryPath, string[] expectedSubdirectories, string[] expectedFiles)
        {
            Assert.IsTrue(Directory.Exists(directoryPath), string.Format(@"Layout Directory '{0}' was not created as expected.", directoryPath));

            int expectedSubdirectoriesCount = null == expectedSubdirectories ? 0 : expectedSubdirectories.Length;
            int expectedFilesCount = null == expectedFiles ? 0 : expectedFiles.Length;

            Assert.AreEqual(Directory.GetDirectories(directoryPath).Length, expectedSubdirectoriesCount, string.Format(@"Layout Directory '{0}' has more sub directories than expected.", directoryPath));
            if (null != expectedSubdirectories)
            {
                foreach (string subdirectoryName in expectedSubdirectories)
                {
                    Assert.IsTrue(Directory.Exists(Path.Combine(directoryPath, subdirectoryName)), string.Format(@"Layout Subdirectory '{0}\{1}' was not created as expected.", directoryPath, subdirectoryName));
                }
            }

            Assert.AreEqual(Directory.GetFiles(directoryPath).Length, expectedFilesCount, string.Format(@"Layout Directory '{0}' has more files than expected.", directoryPath));
            if (null != expectedFiles)
            {
                foreach (string fileName in expectedFiles)
                {
                    Assert.IsTrue(File.Exists(Path.Combine(directoryPath, fileName)), string.Format(@"Layout File '{0}\{1}' was not created as expected.", directoryPath, fileName));
                }
            }
        }

        #endregion
    }
}