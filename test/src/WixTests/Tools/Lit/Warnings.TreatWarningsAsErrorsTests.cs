//-----------------------------------------------------------------------
// <copyright file="Warnings.TreatWarningsAsErrorsTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests how Lit handles the wx switch.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Lit.Warnings
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    /// Test how Lit handles the wx switch.
    /// </summary>
    public class TreatWarningsAsErrorsTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Lit honors the wx switch when specified.")]
        [Priority(2)]
        public void TreatAllWarningsAsErrorSwitch()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = " -abc";
            lit.TreatAllWarningsAsErrors = true;
            lit.ExpectedWixMessages.Add(new WixMessage(1098, "'abc' is not a valid command line argument.", WixMessage.MessageTypeEnum.Error));
            lit.ExpectedExitCode = 1098;
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit honors the wx[N] switch when specified.")]
        [Priority(2)]
        public void TreatSpecificWarningsAsErrorSwitch()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = " -abc";
            lit.TreatWarningsAsErrors.Add(1098);
            lit.ExpectedWixMessages.Add(new WixMessage(1098, "'abc' is not a valid command line argument.", WixMessage.MessageTypeEnum.Error));
            lit.ExpectedExitCode = 1098;
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit does not treat warnings as errors when wx switch is not specified.")]
        [Priority(2)]
        public void NoTreatWarningsAsErrorSwitch()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = " -abc";
            lit.ExpectedWixMessages.Add(new WixMessage(1098, "'abc' is not a valid command line argument.", WixMessage.MessageTypeEnum.Warning));
            lit.ExpectedExitCode = 0;
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit handles the wxall switch and displays a message that the switch is deprecated.")]
        [Priority(2)]
        public void VerifyDeprecatedSwitch()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.OtherArguments = "-wxall";
            lit.ExpectedWixMessages.Add(new WixMessage(1108, "The command line switch 'wxall' is deprecated. Please use 'wx' instead.", WixMessage.MessageTypeEnum.Warning));
            lit.Run();
        }
    }
}