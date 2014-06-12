//-----------------------------------------------------------------------
// <copyright file="Cabs.CabCachingTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for cab caching
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Cabs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for cab caching
    /// </summary>
    public class CabCachingTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Cabs\CabCachingTests");

        [NamedFact(Skip = "Ignored because of a bug")]
        [Description("Verify that Light can bind files into a wixout")]
        [Priority(1)]
        public void SimpleCabCaching()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CabCachingTests.TestDataDirectory, @"SimpleCabCaching\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.CachedCabsPath = Path.Combine(Path.GetDirectoryName(light.OutputFile), "CabCacheDirectory");
            light.ReuseCab = false;
            light.Run();

            string cachedCab = Path.Combine(light.CachedCabsPath, "product.cab");
            Assert.True(File.Exists(cachedCab), String.Format("The cabinet file was not cached in {0}", cachedCab));
        }

        [NamedFact]
        [Description("Verify that passing an existing file path instead of a directory path as a cab cache path results in the expected error message")]
        [Priority(2)]
        public void InvalidCabCache()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add((Path.Combine(CabCachingTests.TestDataDirectory,@"InvalidCabCache\product.wxs")));
            candle.Run();

            string invalidCabCachePath = Path.Combine(Path.GetDirectoryName(candle.OutputFile), "testCabCache.cab");
            System.IO.File.Create(invalidCabCachePath);
            string expectedErrorMessage = string.Format("The -cc option requires a directory, but the provided path is a file: {0}", invalidCabCachePath);

            Light light = new Light(candle);
            light.CachedCabsPath = invalidCabCachePath;
            light.ExpectedWixMessages.Add(new WixMessage(280, expectedErrorMessage, WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 280;
            light.Run();
        }

    }
}