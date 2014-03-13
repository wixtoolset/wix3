//-----------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Smoke tests for Candle/Light Integration</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Smoke tests for Candle/Light Integration
    /// </summary>
    [TestClass]
    public class SmokeTests : WixTests
    {
        [TestMethod]
        [Description("A small, typical case scenario for using Candle and Light")]
        [Priority(1)]
        public void Scenario01()
        {
            string sourceFile = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            
            Verifier.VerifyResults(Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi"), msi);
        }

        [TestMethod]
        [Description("A scenario for using Candle and Light that exercises several features")]
        [Priority(1)]
        public void Scenario02()
        {
            string testDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\SmokeTests\Scenario02");

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(testDirectory, "customactions.wxs"));
            candle.SourceFiles.Add(Path.Combine(testDirectory, "components.wxs"));
            candle.SourceFiles.Add(Path.Combine(testDirectory, "features.wxs"));
            candle.SourceFiles.Add(Path.Combine(testDirectory, "product.wxs"));
            candle.SourceFiles.Add(Path.Combine(testDirectory, "properties.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.SuppressedICEs.Add("ICE49");
            light.Run();

            // Verify
            Verifier.VerifyResults(Path.Combine(testDirectory, @"expected.msi"), light.OutputFile);
        }
    }
}
