//-----------------------------------------------------------------------
// <copyright file="PreProcessor.ErrorsAndWarningsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Test how Candle handles preprocessing for errors and warnings.</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle.PreProcessor
{
    using System;
    using System.IO;
    using WixTest;

    /// <summary>
    /// Test how Candle handles preprocessing for errors and warnings.
    /// </summary>
    public class ErrorsAndWarningsTests : WixTests
    {
        private static readonly string TestDataDirectory = @"%WIX_ROOT%\test\data\Tools\Candle\PreProcessor\ErrorsAndWarningsTests";

        [NamedFact]
        [Description("Verify that Candle can preprocess errors.")]
        [Priority(2)]
        public void Error()
        {
            string testFile = Environment.ExpandEnvironmentVariables(Path.Combine(ErrorsAndWarningsTests.TestDataDirectory, @"Error\Product.wxs"));
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.ExpectedWixMessages.Add(new WixMessage(250, "Preprocessor error", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 250;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess warnings and continue.")]
        [Priority(2)]
        public void Warning()
        {
            string testFile = Environment.ExpandEnvironmentVariables(Path.Combine(ErrorsAndWarningsTests.TestDataDirectory, @"Warning\Product.wxs"));
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.ExpectedWixMessages.Add(new WixMessage(1096, "Preprocessor warning", WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }
    }
}