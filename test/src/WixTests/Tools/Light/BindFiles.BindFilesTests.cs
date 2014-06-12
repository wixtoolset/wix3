//-----------------------------------------------------------------------
// <copyright file="BindFiles.BindFilesTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for binding files into wixouts
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.BindFiles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for binding files into wixouts
    /// </summary>
    public class BindFilesTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\BindFiles\BindFilesTests");

        [NamedFact]
        [Description("Verify that Light can bind files into a wixout")]
        [Priority(1)]
        public void SimpleBindFiles()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(BindFilesTests.TestDataDirectory, @"SimpleBindFiles\product.wxs"));
            candle.Run();

            Light light1 = new Light(candle, true);
            light1.BindFiles = true;
            light1.Run();

            // Verify that TextFile1 was not created in the layout location
            string textFile1 = Path.Combine(Path.GetDirectoryName(light1.OutputFile), @"PFiles\WixTestFolder\TextFile1.txt");
            Assert.False(File.Exists(textFile1), String.Format("{0} exists but it should not", textFile1));

            // Build the wixout into an msi and verify it
            Light light2 = new Light();
            light2.ObjectFiles.Add(light1.OutputFile);
            light2.Run();

            // Verify that TextFile1 was created in the layout location
            Verifier.VerifyResults(Path.Combine(BindFilesTests.TestDataDirectory, @"SimpleBindFiles\expected.msi"), light2.OutputFile);
            textFile1 = Path.Combine(Path.GetDirectoryName(light2.OutputFile), @"PFiles\WixTestFolder\TextFile1.txt");
            Assert.True(File.Exists(textFile1), String.Format("{0} does not exist", textFile1));
        }
    }
}