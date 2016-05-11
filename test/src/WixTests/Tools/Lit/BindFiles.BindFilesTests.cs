// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Lit.BindFiles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for how Lit handles -bf switch
    /// </summary>
    public class BindFilesTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Lit\BindFiles\BindFilesTests");

        [NamedFact]
        [Description("Verify that Lit can bind files into a wix library")]
        [Priority(1)]
        public void SimpleBindFiles()
        {
            // Create a temp text file to bind into the wix library
            DirectoryInfo tempDirectory = Directory.CreateDirectory(Utilities.FileUtilities.GetUniqueFileName());
            string testFileName = Path.Combine(tempDirectory.FullName, "TextFile1.txt");
            StreamWriter outputFile = File.CreateText(testFileName);
            outputFile.Write("abc");
            outputFile.Close();

            // Build the library
            Lit lit = new Lit();
            lit.WorkingDirectory = tempDirectory.FullName;
            lit.ObjectFiles.Add(Candle.Compile(Path.Combine(BindFilesTests.TestDataDirectory, @"SimpleBindFiles\Product.wxs")));
            lit.BindFiles = true;
            lit.Run();

            // Delete the source file
            File.Delete(testFileName);

            // Link the library and verify files are in the resulting msi layout
            Light light = new Light(lit);
            light.Run();

            string outputFileName = Path.Combine(Path.GetDirectoryName(lit.OutputFile), @"PFiles\WixTestFolder\TextFile1.txt");
            Assert.True(File.Exists(outputFileName), "File was not created in msi layout as expected.");
            Assert.True(File.ReadAllText(outputFileName).Equals("abc"), "File contents do not match expected.");
        }
    }
}
