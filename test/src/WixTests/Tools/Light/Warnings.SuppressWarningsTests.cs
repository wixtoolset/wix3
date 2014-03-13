//-----------------------------------------------------------------------
// <copyright file="Warnings.SuppressWarningsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for suppressing warnings
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Warnings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for suppressing warnings
    /// </summary>
    [TestClass]
    public class SuppressWarningsTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Warnings");

        [TestMethod]
        [Description("Verify that specific warnings can be suppressed")]
        [Priority(1)]
        public void SimpleSuppressWarnings()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Environment.ExpandEnvironmentVariables(Path.Combine(SuppressWarningsTests.TestDataDirectory, @"Shared\Warning1079.wxs")));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressWarnings.Add("1079");
            light.Run();
        }
    }
}