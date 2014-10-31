//-----------------------------------------------------------------------
// <copyright file="Output.OutputTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Output 
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Output
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Test for Output
    /// </summary>
    public class OutputTests : WixTests
    {
        [NamedFact]
        [Description("Verify that locking light output file results in the expected error message.")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1835329&group_id=105970&atid=642714")]
        public void LockedOutputFile()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(WixTests.BasicProductWxs);
            candle.Run();

            string outputDirectory = Utilities.FileUtilities.GetUniqueFileName();
            System.IO.Directory.CreateDirectory(outputDirectory);
            string outputFile = Path.Combine(outputDirectory, "test.msi");
            System.IO.File.OpenWrite(outputFile);

            Light light = new Light(candle);
            light.OutputFile = outputFile;
            string expectedErrorMessage = string.Format("The process can not access the file '{0}' because it is being used by another process.", outputFile);
            light.ExpectedWixMessages.Add(new WixMessage(128, expectedErrorMessage, WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 128;
            light.Run();
        }
    }
}