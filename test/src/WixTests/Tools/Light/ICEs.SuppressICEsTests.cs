// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
