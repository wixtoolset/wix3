//-----------------------------------------------------------------------
// <copyright file="Cabs.ReuseCabTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for reusing cabs
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Cabs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for reusing cabs
    /// </summary>
    [TestClass]
    public class ReuseCabTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Cabs\ReuseCabTests");

        [TestMethod]
        [Description("Verify that cabs can be reused")]
        [Priority(1)]
        [Ignore] // Bug
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