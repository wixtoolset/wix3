//-----------------------------------------------------------------------
// <copyright file="UtilExtension.RegressionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util extension tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Sql extension tests
    /// </summary>
    public class RegressionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\RegressionTests");

        [NamedFact]
        [Description("Verify that using util:ServiceConfig without the -out paramter does not cause a light error.")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1951034&group_id=105970&atid=642714")]
        public void ValidServiceConfig()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"ValidServiceConfig\product.wxs"));
            candle.Extensions.Add("WixUtilExtension");
            candle.Run();

            Light light = new Light(candle);
            light.Extensions.Add("WixUtilExtension");
            light.OutputFile = string.Empty;
            light.Run();
        }
    }
}
