//-----------------------------------------------------------------------
// <copyright file="SuppressWarningsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Tests how Candle handles the suppress warning switch.</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle.SuppressWarnings
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the suppress warning switch.
    /// </summary>
    public class SuppressWarningsTests : WixTests
    {
        private static string testFile = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\SuppressWarningsTests\Product.wxs");

        [NamedFact]
        [Description("Verify that Candle honors the sw switch when specified.")]
        [Priority(2)]
        public void SuppressSpecificWarnings()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.SuppressWarnings.Add(1075);
            candle.ExpectedWixMessages.Add(new WixMessage(1096, WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle does not suppress warnings when the sw switch is not specified.")]
        [Priority(2)]
        public void NoSuppressWarningsSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.ExpectedWixMessages.Add(new WixMessage(1096, WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1075, WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that all warnings are suppressed with the sw switch.")]
        [Priority(2)]
        public void SuppressAllWarningsSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.SuppressAllWarnings = true;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle honors the swall switch when specified and displays that the switch is deprecated.")]
        [Priority(2)]
        public void VerifyDeprecatedSwitch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.OtherArguments = "-swall";
            candle.ExpectedWixMessages.Add(new WixMessage(1108, "The command line switch 'swall' is deprecated. Please use 'sw' instead.", WixMessage.MessageTypeEnum.Warning));
            candle.Run();
        }
    }
}