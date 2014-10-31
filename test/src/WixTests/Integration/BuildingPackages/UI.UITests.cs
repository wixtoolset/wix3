//-----------------------------------------------------------------------
// <copyright file="UI.UITests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     General tests for UI
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.UI
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// General tests for UI
    /// </summary>
    public class UITests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\UI\UITests");

        [NamedFact]
        [Description("Verify that a simple UI can be defined")]
        [Priority(1)]
        public void SimpleUI()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(UITests.TestDataDirectory, @"SimpleUI\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressedICEs.Add("ICE20");
            light.SuppressedICEs.Add("ICE31");
            light.Run();

            string expectedMsi = Path.Combine(UITests.TestDataDirectory, @"SimpleUI\expected.msi");
            Verifier.VerifyResults(expectedMsi, light.OutputFile);
        }
    }
}