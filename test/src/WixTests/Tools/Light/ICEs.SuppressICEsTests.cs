//-----------------------------------------------------------------------
// <copyright file="ICEs.SuppressICEsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for suppressing ICEs
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
    /// Tests for suppressing ICEs
    /// </summary>
    public class SuppressICEsTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\ICEs\SuppressICEsTests");

        [NamedFact]
        [Description("Verify that an ICE can be suppressed")]
        [Priority(1)]
        public void SimpleSuppressICE()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(SuppressICEsTests.TestDataDirectory, @"SimpleSuppressICE\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressedICEs.Add("ICE18");
            light.Run();
        }
    }
}