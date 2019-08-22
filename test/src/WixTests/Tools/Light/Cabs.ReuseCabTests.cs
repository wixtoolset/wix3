// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Cabs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for reusing cabs
    /// </summary>
    public class ReuseCabTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Cabs\ReuseCabTests");

        [NamedFact(Skip = "Ignored because of a bug")]
        [Description("Verify that cabs can be reused")]
        [Priority(1)]
        public void SimpleReuseCab()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ReuseCabTests.TestDataDirectory, @"SimpleReuseCab\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ReuseCab = true;
            light.CachedCabsPath = Path.Combine(ReuseCabTests.TestDataDirectory, "SimpleReuseCab");
            light.Run();
        }
    }
}
