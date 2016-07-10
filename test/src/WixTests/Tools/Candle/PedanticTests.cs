// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.Pedantic
{
    using System;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the Pedantic switch.
    /// </summary>
    public class PedanticTests : WixTests
    {
        private static readonly string TestFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\PedanticTests\Product.wxs");

        [NamedFact]
        [Description("Verify that Candle honors the pedantic switch when specified.")]
        [Priority(2)]
        public void PedanticSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(PedanticTests.TestFile);
            string guid = "aaaffB15-DF17-43b8-9971-DddC3D4F3490"; 
            candle.Pedantic = true;
            string expectedOutput = String.Format("The Product/@Id attribute's value, '{0}', is a mixed-case guid.  All letters in a guid value should be uppercase.", guid);
            candle.ExpectedWixMessages.Add(new WixMessage(87, expectedOutput, WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 87;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle does not print pedantic messages when Pedantic switch is not specified.")]
        [Priority(2)]
        public void NoPedanticSwitch()
        {
            Candle candle = new Candle();
            candle.Pedantic = false;
            candle.SourceFiles.Add(PedanticTests.TestFile);
            candle.Run();
        }
    }
}
