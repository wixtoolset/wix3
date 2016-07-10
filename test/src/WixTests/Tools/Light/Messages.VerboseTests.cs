// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Light.Messages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for verbose output
    /// </summary>
    public class VerboseTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Light prints verbose output")]
        [Priority(1)]
        public void SimpleVerbose()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.Verbose = true;
            light.ExpectedOutputStrings.Add("Updating file information.");
            light.ExpectedOutputStrings.Add("Creating cabinet files.");
            light.ExpectedOutputStrings.Add("Generating database.");
            light.ExpectedOutputStrings.Add("Merging modules.");
            light.ExpectedOutputStrings.Add("Validating database.");
            light.ExpectedOutputStrings.Add("Laying out media.");
            light.ExpectedOutputStrings.Add("Moving file");
            
            light.Run();
        }
    }
}
