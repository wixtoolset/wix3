// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Warnings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for the -wx switch
    /// </summary>
    public class WarningsAsErrorsTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Warnings");

        [NamedFact]
        [Description("Verify that warnings are treated as errors")]
        [Priority(1)]
        public void SimpleWarningsAsErrors()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(WarningsAsErrorsTests.TestDataDirectory, @"Shared\Warning1079.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.TreatAllWarningsAsErrors = true;
            light.ExpectedWixMessages.Add(new WixMessage(1079, WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 1079;
            light.Run();

        }
    }
}
