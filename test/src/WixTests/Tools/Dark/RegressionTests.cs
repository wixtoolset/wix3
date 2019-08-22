// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Dark
{
    using System;
    using System.IO;
    using WixTest;

    /// <summary>
    /// Regresssion tests for Dark
    /// </summary>
    public class RegressionTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Dark generates the proper warning for invalid command arguments.")]
        [Priority(3)]
        public void InvalidArgument()
        {
            Dark dark = new Dark();
            dark.InputFile = Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi");
            dark.OtherArguments = " -abc ";
            dark.ExpectedWixMessages.Add(new WixMessage(1098, "'abc' is not a valid command line argument.", WixMessage.MessageTypeEnum.Warning));
            dark.Run();

            // uppercase version of valid command arrguments
            dark = new Dark();
            dark.InputFile = Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi");
            dark.OtherArguments = " -SW1098 ";
            dark.ExpectedWixMessages.Add(new WixMessage(1098, "'SW1098' is not a valid command line argument.", WixMessage.MessageTypeEnum.Warning));
            dark.Run();
        }
    }
}
