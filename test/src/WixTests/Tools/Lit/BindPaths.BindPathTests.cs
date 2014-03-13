//-----------------------------------------------------------------------
// <copyright file="BindPaths.BindPathTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test for how Lit handles -b switch
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Lit.BindPaths
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    ///  Test for how Lit handles -b switch
    /// </summary>
    [TestClass]
    public class BindPathTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Lit\BindPaths\BindPathTests");

        [TestMethod]
        [Description("Verify that Lit can use a binder path")]
        [Priority(1)]
        public void SimpleBindPath()
        {
            // Create a temp text file to bind into the wix library
            DirectoryInfo tempDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());
            string testFileName = Path.Combine(tempDirectory.FullName, "TextFile1.txt");
            StreamWriter outputFile =  File.CreateText(testFileName);
            outputFile.Write("abc");
            outputFile.Close();

            // Build the library
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(Path.Combine(BindPathTests.TestDataDirectory, @"SimpleBindPath\Product.wxs")));
            lit.BindPath = tempDirectory.FullName;
            lit.BindFiles = true;
            lit.Run();

            // Delete the source file
            File.Delete(testFileName);

            // Link the library and verify files are in the resulting msi layout
            Light light = new Light(lit);
            light.Run();

            string outputFileName = Path.Combine(Path.GetDirectoryName(lit.OutputFile), @"PFiles\WixTestFolder\TextFile1.txt");
            Assert.IsTrue(File.Exists(outputFileName), "File was not created in msi layout as expected.");
            Assert.IsTrue(File.ReadAllText(outputFileName).Equals("abc"), "File contents do not match expected.");
        }

        [TestMethod]
        [Description("Verify that Lit can use named binder paths")]
        [Priority(1)]
        public void NamedBindPath()
        {
            // Create a temp text file to bind into the wix library
            DirectoryInfo tempDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());
            string testFileName = Path.Combine(tempDirectory.FullName, "TextFile1.txt");
            StreamWriter outputFile =  File.CreateText(testFileName);
            outputFile.Write("abc");
            outputFile.Close();

            // Build the library
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(Path.Combine(BindPathTests.TestDataDirectory, @"NamedBindPath\Product.wxs")));
            lit.BindPath = String.Concat("Test=", tempDirectory.FullName);
            lit.BindFiles = true;
            lit.Run();

            // Delete the source file
            File.Delete(testFileName);

            // Link the library and verify files are in the resulting msi layout
            Light light = new Light(lit);
            light.Run();

            string outputFileName = Path.Combine(Path.GetDirectoryName(lit.OutputFile), @"PFiles\WixTestFolder\TextFile1.txt");
            Assert.IsTrue(File.Exists(outputFileName), "File was not created in msi layout as expected.");
            Assert.IsTrue(File.ReadAllText(outputFileName).Equals("abc"), "File contents do not match expected.");
        }
    }
}
