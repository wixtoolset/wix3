//-----------------------------------------------------------------------
// <copyright file="Input.WixobjTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Test for giving wixobj files as input to Light</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Input
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Tests;

    /// <summary>
    /// Test for giving wixobj files as input to Light
    /// </summary>
    public class WixobjTests : WixTests
    {
        /// <summary>
        /// This authoring will be used by many tests
        /// </summary>
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Input");
        private static readonly string ModuleWxs = Path.Combine(WixTests.SharedAuthoringDirectory, "BasicModule.wxs");

        #region wixobjs

        [NamedFact]
        [Description("Verify that Light accepts a single wixobj as input")]
        [Priority(1)]
        public void SingleWixobj()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that Light accepts multiple wixobjs as input")]
        [Priority(1)]
        public void MultipleWixobjs()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\product.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\features.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component1.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that Light accepts multiple wixobjs where at least one wixobj is not referenced")]
        [Priority(2)]
        public void UnreferencedWixobj()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\product.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\features.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component1.wxs"));
            // Component2 in component2.wxs is not referenced anywhere
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component2.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.Run();
        }

        [NamedFact]
        [Description("Verify a Light error for a single wixobj with no entry section")]
        [Priority(2)]
        public void SingleWixobjWithNoEntrySection()
        {
            Candle candle = new Candle();
            // component1.wxs does not have an entry section
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component1.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.ExpectedWixMessages.Add(new WixMessage(93, "Could not find entry section in provided list of intermediates. Expected section of type 'Product'.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 93;
            light.Run();
        }

        [NamedFact]
        [Description("Verify a Light error for a multiple wixobjs with no entry section")]
        [Priority(3)]
        public void MultipleWixobjsWithNoEntrySection()
        {
            Candle candle = new Candle();
            // These files do not have entry sections
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\features.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component1.wxs"));
            candle.SourceFiles.Add(Path.Combine(WixobjTests.TestDataDirectory, @"WixobjTests\component2.wxs"));
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.ExpectedWixMessages.Add(new WixMessage(93, "Could not find entry section in provided list of intermediates. Expected section of type 'Product'.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 93;
            light.Run();
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify a Light error for a multiple wixobjs with multiple entry sections. This test is disabled until a spec issue is resolved.")]
        [Priority(2)]
        public void MultipleWixobjEntrySections()
        {
            Candle candle = new Candle();
            // These files both have entry sections
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.SourceFiles.Add(WixobjTests.ModuleWxs);
            candle.Run();

            Light light = new Light();
            light.ObjectFiles = candle.ExpectedOutputFiles;
            light.ExpectedWixMessages.Add(new WixMessage(89, "Multiple entry sections '{12345678-1234-1234-1234-123456789012}' and 'Module1' found.  Only one entry section may be present in a single target.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 89;
            light.Run(); ;
        }

        #endregion
    }
}