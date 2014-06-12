//-----------------------------------------------------------------------
// <copyright file="Warnings.SuppressWarningsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests how Lit handles the suppress warning switch.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Lit.Warnings
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    /// Test how Lit handles the suppress warning switch.
    /// </summary>
    public class SuppressWarningsTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Lit honors the sw switch when specified.")]
        [Priority(2)]
        public void SuppressSpecificWarnings()
        {
            Lit lit = new Lit();
            lit.OtherArguments = " -abc";
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.SuppressWarnings.Add(1098);
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit does not suppress warnings when the sw switch is not specified.")]
        [Priority(2)]
        public void NoSuppressWarningsSwitch()
        {
            Lit lit = new Lit();
            lit.OtherArguments = " -abc";
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.ExpectedWixMessages.Add(new WixMessage(1098, "'abc' is not a valid command line argument.", WixMessage.MessageTypeEnum.Warning));
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that all warnings are suppressed with the sw switch.")]
        [Priority(2)]
        public void SuppressAllWarningsSwitch()
        {
            Lit lit = new Lit();
            lit.OtherArguments = " -abc";
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.SuppressAllWarnings = true;
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit honors the swall switch when specified and displays that the switch is deprecated.")]
        [Priority(2)]
        public void VerifyDeprecatedSwitch()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = "-swall";
            lit.ExpectedWixMessages.Add(new WixMessage(1108, "The command line switch 'swall' is deprecated. Please use 'sw' instead.", WixMessage.MessageTypeEnum.Warning));
            lit.Run();
        }
    }
}