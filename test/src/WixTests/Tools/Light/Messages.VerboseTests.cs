//-----------------------------------------------------------------------
// <copyright file="Messages.VerboseTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for verbose output
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Messages
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for verbose output
    /// </summary>
    [TestClass]
    public class VerboseTests : WixTests
    {
        [TestMethod]
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