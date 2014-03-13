//-----------------------------------------------------------------------
// <copyright file="Input.WixlibTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test for giving wixlib files as input to Light
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Input
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Test for giving wixlib files as input to Light
    /// </summary>
    [TestClass]
    public class WixlibTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Input\WixlibTests");

        [TestMethod]
        [Description("Verify that Light can link a wixlib")]
        [Priority(1)]
        public void SingleWixlib()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.Run();

            //Building the expected msi directly from wixobj
            Light light1 = new Light();
            light1.ObjectFiles.Add(candle.OutputFile);
            light1.Run();
            String expectedMSI = light1.OutputFile;

            // Build a wixlib
            Lit lit = new Lit(candle);
            lit.Run();

            Light light2 = new Light();
            light2.ObjectFiles.Add(lit.ExpectedOutputFile);
            light2.Run();

            Verifier.VerifyResults(expectedMSI, light2.OutputFile);
        }

        [TestMethod]
        [Description("Verify that Light can link multiple wixlibs")]
        [Priority(2)]
        public void MultipleWixlibs()
        {
            // Create Temp Directory
            string outputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Utilities.FileUtilities.CreateOutputDirectory(outputDirectory);

            // Build first Wix object
            Candle candle1 = new Candle();
            candle1.SourceFiles.Add(Path.Combine(WixlibTests.TestDataDirectory, "MultipleWixlibs\\Product.wxs"));
            candle1.OutputFile = Path.Combine(outputDirectory,"Product.wixobj");
            // candle1.OutputToTemp = false;
            candle1.Run();
            String firstObjectFile = candle1.OutputFile;

            // Build first wixlib
            Lit lit1 = new Lit(candle1);
            lit1.OutputFile = Path.Combine(outputDirectory, "Product.wixlib");
            lit1.Run();
            String firstWixlib = lit1.ExpectedOutputFile;

            // Build Second Wix object
            Candle candle2 = new Candle();
            candle2.SourceFiles.Add(Path.Combine(WixlibTests.TestDataDirectory, "MultipleWixlibs\\Component.wxs"));
            candle2.OutputFile = Path.Combine(outputDirectory,"Component.wixobj");
            // candle2.OutputToTemp = false;
            candle2.Run();
            String secondObjectFile = candle2.OutputFile;

            // Build second wixlib
            Lit lit2 = new Lit(candle2);
            lit2.OutputFile = Path.Combine(outputDirectory, "Component.wixlib");
            lit2.Run();
            String secondWixlib = lit2.ExpectedOutputFile;

            // Generate Expected msi directly from wix objects
            Light light1 = new Light();
            light1.ObjectFiles.Add(firstObjectFile);
            light1.ObjectFiles.Add(secondObjectFile);
            light1.OutputFile = Path.Combine(outputDirectory, "ExpectedProduct.msi");
            light1.Run();
            String expectedMSI = light1.OutputFile;

            // Generate msi from the lib objects
            Light light2 = new Light();
            light2.ObjectFiles.Add(firstWixlib);
            light2.ObjectFiles.Add(secondWixlib);
            light2.OutputFile = Path.Combine(outputDirectory, "actual.msi");
            light2.Run();

            Verifier.VerifyResults(expectedMSI, light2.OutputFile);
        }

    }
}
