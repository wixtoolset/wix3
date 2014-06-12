//-----------------------------------------------------------------------
// <copyright file="Extensions.ExtensionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Test how Dark handles the -ext switch</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Dark.Extensions
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    /// Test how Dark handles the -ext switch.
    /// </summary>
    public class ExtensionTests : WixTests
    {
        [NamedFact]
        [Description("Verify that Dark generates the correct error for a missing extension after -ext switch.")]
        [Priority(2)]
        public void MissingExtension()
        {
            Dark dark = new Dark();
            dark.InputFile = Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi");
            dark.Extensions.Add(string.Empty);
            dark.ExpectedWixMessages.Add(new WixMessage(113, "The parameter '-ext' must be followed by the extension's type specification.  The type specification should be a fully qualified class and assembly identity, for example: \"MyNamespace.MyClass,myextension.dll\".", WixMessage.MessageTypeEnum.Error));
            dark.ExpectedExitCode = 113;
            dark.Run();
        }

        [NamedFact]
        [Description("Verify that Dark generates the correct error for an invalid extension file after -ext switch.")]
        [Priority(2)]
        public void InvalidExtension()
        {
            Dark dark = new Dark();
            dark.InputFile = Path.Combine(WixTests.SharedBaselinesDirectory, @"MSIs\BasicProduct.msi");
            dark.Extensions.Add(WixTests.BasicProductWxs);
            dark.ExpectedWixMessages.Add(new WixMessage(144, string.Format("The extension '{0}' could not be loaded because of the following reason: Could not load file or assembly 'file:///{1}\\test\\data\\SharedData\\Authoring\\BasicProduct.wxs' or one of its dependencies. The module was expected to contain an assembly manifest.", WixTests.BasicProductWxs,Environment .GetEnvironmentVariable("WIX_ROOT")), WixMessage.MessageTypeEnum.Error));
            dark.ExpectedExitCode = 144;
            dark.Run();
        }
    }
}