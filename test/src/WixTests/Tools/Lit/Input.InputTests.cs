//-----------------------------------------------------------------------
// <copyright file="Input.InputTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test how Lit handles different input files
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Lit.Input
{
    using System;
    using System.IO;
    using WixTest;
    
    /// <summary>
    ///  Test how Lit handles different input files
    /// </summary>
    public class InputTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Lit\Input\InputTests");

        [NamedFact]
        [Description("Verify that Lit accepts a single Windows Installer XML source (wxs) file as input")]
        [Priority(0)]
        public void SingleWixObjFile()
        {
            string testFile = Candle.Compile(WixTests.PropertyFragmentWxs);

            Lit lit = new Lit();
            lit.ObjectFiles.Add(testFile);
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit accepts multiple Windows Installer XML object (.wix) files as input")]
        [Priority(0)]
        public void MultipleWixObjFiles()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.ObjectFiles.Add(Candle.Compile(Path.Combine(InputTests.TestDataDirectory, @"MultipleWixObjFiles\ComponentFragment.wxs")));
            lit.ObjectFiles.Add(Candle.Compile(Path.Combine(InputTests.TestDataDirectory, @"MultipleWixObjFiles\PropertyFragment.wxs")));
            lit.Run();
        }
               
        [NamedFact]
        [Description("Verify that Lit can accept a WixObj file without the .wixobj extension as input")]
        [Priority(2)]
        public void NoWixObjExtension()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.OutputFile = "Library";
            candle.Run();

            Lit lit = new Lit(candle);
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit can accept a file with .foo as extension")]
        [Priority(2)]
        public void ValidFileWithUnknownExtension()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.PropertyFragmentWxs);
            candle.OutputFile = "Library.foo";
            candle.Run();

            Lit lit = new Lit(candle);
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit can handle invalid WixObj, a non wix text file with a wixobj extension")]
        [Priority(1)]
        public void InvalidWixObjFile()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"InvalidWixObjFile\Library.wixobj"));
            lit.ExpectedWixMessages.Add(new WixMessage(104, "Not a valid object file; detail: Data at the root level is invalid. Line 1, position 1.", WixMessage.MessageTypeEnum.Error));
            lit.ExpectedExitCode = 104;
            lit.Run();
        }

        [NamedFact]
        [Description("Verify that Lit can handle invalid WixObj, a non wix text file with a wixobj extension")]
        [Priority(1)]
        public void WildcardInput()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"WildcardInput\PropertyFragment1.wxs"));
            candle.SourceFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"WildcardInput\PropertyFragment2.wxs"));
            candle.SourceFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"WildcardInput\TestPropertyFragment3.wxs"));
            candle.SourceFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"WildcardInput\TestPropertyFragment4.wxs"));
            candle.Run();

            Lit lit = new Lit();
            lit.ObjectFiles.Add(Path.Combine(Path.GetDirectoryName(candle.OutputFile),@"PropertyFragment?.wixobj"));
            lit.ObjectFiles.Add(Path.Combine(Path.GetDirectoryName(candle.OutputFile),@"Test*.wixobj"));
            lit.Run();

            Verifier.VerifyWixLibProperty(lit.OutputFile, "Property1", "Property1_Value");
            Verifier.VerifyWixLibProperty(lit.OutputFile, "Property2", "Property2_Value");
            Verifier.VerifyWixLibProperty(lit.OutputFile, "Property3", "Property3_Value");
            Verifier.VerifyWixLibProperty(lit.OutputFile, "Property4", "Property4_Value");
        }

        [NamedFact]
        [Description("Verify that Lit can handle response file")]
        [Priority(3)]
        public void ResponseFile()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Candle.Compile(WixTests.PropertyFragmentWxs));
            lit.ResponseFile = Path.Combine(InputTests.TestDataDirectory, @"ResponseFile\ResponseFile.txt");
            lit.Run();

            // verify the loc file added by the @ResponseFile is read and added to the library
            Verifier.VerifyWixLibLocString(lit.OutputFile, "en-us", "String1", "String1(en-us)");
        }

        [NamedFact]
        [Description("Verify that Lit can handle empty wixobj file")]
        [Priority(3)]
        public void EmptyWixObjFile()
        {
            Lit lit = new Lit();
            lit.ObjectFiles.Add(Path.Combine(InputTests.TestDataDirectory, @"EmptyWixObjFile\EmptyFile.wixobj"));
            lit.ExpectedWixMessages.Add(new WixMessage(104, "Not a valid object file; detail: Root element is missing.", WixMessage.MessageTypeEnum.Error));
            lit.ExpectedExitCode = 104;
            lit.Run();
        }
    }
}