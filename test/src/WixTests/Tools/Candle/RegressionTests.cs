//-----------------------------------------------------------------------
// <copyright file="RegressionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Regresssion tests for Candle</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Regresssion tests for Candle
    /// </summary>
    public class RegressionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Candle\RegressionTests");

        [NamedFact]
        [Description("Verify that the proper error when TARGETDIR has Name='SOURCEDIR'")]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1667625&group_id=105970&atid=642714")]
        [Priority(3)]
        public void SourceDirTest()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"SourceDirTest\product.wxs"));

            candle.ExpectedWixMessages.Add(new WixMessage(206, "The 'TARGETDIR' directory has an illegal DefaultDir value of 'tqepgrb4|SOURCEDIR'.  The DefaultDir value is created from the *Name attributes of the Directory element.  The TARGETDIR directory is a special directory which must have its Name attribute set to 'SourceDir'.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 206;

            candle.Run();
        }

        [NamedFact(Skip = "Ignored because of a bug")]
        [Description("Verify that there is no exception from Candle when the Product element is not populated completely")]
        [Priority(2)]
        public void ProductElementNotPopulated()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"ProductElementNotPopulated\IncompleteProductElementBug.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Id attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Language attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Manufacturer attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Name attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Version attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact(Skip = "Ignored because of a bug")]
        [Description("Verify that there is only one error message from Candle, when the version attribute in the Product element is not populated")]
        [Priority(3)]
        public void ProductVersionAttributeNotPopulated()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"ProductVersionAttributeNotPopulated\ProductVersionAttributeMissing.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Product/@Version attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 10;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that there is no exception from Candle, when there is no Directory set for a shortcut")]
        [Priority(1)]
        public void ShortcutDirectoryNotSet()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory, @"ShortcutDirectoryNotSet\ShortcutProduct.wxs"));
            candle.ExpectedExitCode = 0;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify MinSize in FileSearch element does not generate a Candle error")]
        [Priority(1)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1648088&group_id=105970&atid=642714")]
        public void NoErrorOnSpecifyingMinSizeInFileSearch()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"NoErrorOnSpecifyingMinSizeInFileSearch\FileSearch.wxs"));
            candle.ExpectedExitCode = 0;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that the EmbedCab element cannot be specified without the Cabient attribute")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1690710&group_id=105970&atid=642714")]
        public void EmbedCabAttrWithoutCabinetAttr()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RegressionTests.TestDataDirectory,@"EmbedCabAttrWithoutCabinetAttr\EmbedCabProduct.wxs"));
            candle.ExpectedExitCode = 10;
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The Media/@Cabinet attribute was not found; it is required when attribute EmbedCab has a value of 'yes'.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }
    }
}
