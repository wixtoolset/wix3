// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Smoke tests for Candle/Light Integration
    /// </summary>
    public class SmokeTests : WixTests
    {
        [NamedFact]
        [Description("A small, typical case scenario for using Candle and Light")]
        [Priority(1)]
        public void Scenario01()
        {
            string sourceFile = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            
            Verifier.VerifyResults(Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi"), msi);
        }

        [NamedFact]
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
