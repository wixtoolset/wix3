//-----------------------------------------------------------------------
// <copyright file="Authoring.IdentifierTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Identifiers (Ids)
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Authoring
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for Identifiers (Ids)
    /// </summary>
    public class IdentifierTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Authoring\IdentifierTests");

        [NamedFact]
        [Description("Verify that Identifiers with long names can be defined and referenced.")]
        [Priority(1)]
        [Trait("Bug Link", "https://sourceforge.net/tracker/index.php?func=detail&aid=1867881&group_id=105970&atid=642714")]
        public void LongIdentifiers()
        {
            string longDirectoryName = "Directory_01234567890123456789012345678901234567890123456789012345678901234567890123456789";
            string longComponentName = "Component_01234567890123456789012345678901234567890123456789012345678901234567890123456789";
            string longFileName = "Test_txt_01234567890123456789012345678901234567890123456789012345678901234567890123456789";

            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IdentifierTests.TestDataDirectory, @"LongIdentifiers\product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(1026, string.Format("The Directory/@Id attribute's value, '{0}', is too long for an identifier.  Standard identifiers are 72 characters long or less.", longDirectoryName),WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1026, string.Format("The Component/@Id attribute's value, '{0}', is too long for an identifier.  Standard identifiers are 72 characters long or less.", longComponentName), WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1026, string.Format("The File/@Id attribute's value, '{0}', is too long for an identifier.  Standard identifiers are 72 characters long or less.", longFileName), WixMessage.MessageTypeEnum.Warning));
            candle.ExpectedWixMessages.Add(new WixMessage(1026, string.Format("The ComponentRef/@Id attribute's value, '{0}', is too long for an identifier.  Standard identifiers are 72 characters long or less.", longComponentName), WixMessage.MessageTypeEnum.Warning));
            candle.Run();

            Light light = new Light(candle);
            light.SuppressedICEs.Add("ICE03");
            light.Run();

            // verify long names in the resulting msi
            string query = string.Format("SELECT `Directory` FROM `Directory` WHERE `Directory` = '{0}'", longDirectoryName);
            string queryResult = Verifier.Query(light.OutputFile, query);
            Assert.Equal(longDirectoryName, queryResult);

            query = string.Format("SELECT `Component` FROM `Component` WHERE `Component` = '{0}'", longComponentName);
            queryResult = Verifier.Query(light.OutputFile, query);
            Assert.Equal(longComponentName, queryResult);

            query = string.Format("SELECT `File` FROM `File` WHERE `File` = '{0}'", longFileName);
            queryResult = Verifier.Query(light.OutputFile, query);
            Assert.Equal(longFileName, queryResult);

            query = string.Format("SELECT `Component_` FROM `FeatureComponents` WHERE `Component_` = '{0}'", longComponentName);
            queryResult = Verifier.Query(light.OutputFile, query);
            Assert.Equal(longComponentName, queryResult);
        }
    }
}
