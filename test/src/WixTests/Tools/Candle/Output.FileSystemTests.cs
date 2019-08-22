// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.Output
{
    using System;
    using System.IO;
    using WixTest;
    using Xunit;
    
    /// <summary>
    /// Test Candle to verify that it interacts appropriately with the file system to produce output files.
    /// </summary>
    public class FileSystemTests : WixTests
    {
        [NamedFact(Skip="Ignore")]
        [Description("Verify that Candle fails gracefully in case of creating a output file on a network share with no permissions.")]
        [Priority(2)]
        public void NetworkShareNoPermissions()
        {
        }

        [NamedFact]
        [Description("Verify that Candle can output nonalphanumeric characters in the filename")]
        [Priority(2)]
        public void NonAlphaNumericCharactersInFileName()
        {
            Candle candle = new Candle();
            candle.OutputFile = Path.Combine(Utilities.FileUtilities.GetUniqueFileName(), "#@%+BasicProduct.wixobj");
            candle.SourceFiles.Add(Path.Combine(Tests.WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.Run();
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle can output files to a read only share")]
        [Priority(2)]
        public void ReadOnlyShare()
        {
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle can output files to a network share")]
        [Priority(2)]
        public void FileToNetworkShare()
        {
        }

        [NamedFact]
        [Description("Verify that Candle can output file names with single quotes")]
        [Priority(3)]
        public void FileNameWithSingleQuotes()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(Tests.WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.OutputFile = Path.Combine(Utilities.FileUtilities.GetUniqueFileName(), "Basic'Product'.wixobj");
            candle.Run();
        }
               
        [NamedFact]
        [Description("Verify that Candle can output a file with space in its name.")]
        [Priority(3)]
        public void FileNameWithSpace()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(Tests.WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.OutputFile = Path.Combine(Utilities.FileUtilities.GetUniqueFileName(), "Pro  duct.wixobj");
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that Candle output to a file path that is more than 256 characters.")]
        [Priority(3)]
        public void LongFilePath()
        {
            //the max length of a path is 247 chars, the filepath is 259 chars

            string OutputPath = Utilities.FileUtilities.GetUniqueFileName();
            //add initial 170 chars
            OutputPath = Path.Combine(OutputPath, @"FilePathNewfolder11(20chars)\Newfolder12(20chars)Newfolder12(20chars)Newfolder13(20chars)Newfolder13(20chars)\Newfolder14(20chars)Newfolder15(20chars)Newfolder16(20chars)");

            int i = 245 - OutputPath.Length;
            while (i > 0)
            {
                OutputPath = Path.Combine(OutputPath, @"pt");
                i = 245 - OutputPath.Length;
            }
            if (OutputPath.Length < 246)
            {
                OutputPath = Path.Combine(OutputPath, "T");
            }

            Assert.True(OutputPath.Length  <248, "The output path is not less than 248 chars"); 
            Directory.CreateDirectory(OutputPath);

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(Tests.WixTests.SharedAuthoringDirectory, "BasicProduct.wxs"));
            candle.OutputFile = Path.Combine(OutputPath, "Prod.wixobj");

            Assert.True((candle.OutputFile.Length > 256) && (candle.OutputFile.Length < 260), String.Format("The output filepath{0} is not between 256 and 260 chars",candle .OutputFile .Length));
           
            candle.Run();
        }

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that Candle can output a file to a URI path.")]
        [Priority(3)]
        public void URI()
        {
        }
    }
}
