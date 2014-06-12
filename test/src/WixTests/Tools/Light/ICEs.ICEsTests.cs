//-----------------------------------------------------------------------
// <copyright file="ICEs.ICEsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for running only the specified ICEs
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.ICEs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for running only the specified ICEs
    /// </summary>
    public class ICEsTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\ICEs\ICEsTests");

        [NamedFact]
        [Description("Verify that Light will only run the ICE specified by the -ice switch")]
        [Priority(1)]
        public void SimpleICE()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ICEsTests.TestDataDirectory, @"SimpleICE\product.wxs"));
            candle.Run();

            Light light = new Light(candle);

            // The product violates ICE16 and ICE18, but ICE18 should not get run because we are using the -ice switch
            light.ICEs.Add("ICE16");
            
            light.ExpectedWixMessages.Add(new WixMessage(204, "ICE16: ProductName: '1234567890123456789012345678901234567890123456789012345678901234567890' is greater than 63 characters in length. Current length: 70", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 204;
            light.Run();
        }
    }
}