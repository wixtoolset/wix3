// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
