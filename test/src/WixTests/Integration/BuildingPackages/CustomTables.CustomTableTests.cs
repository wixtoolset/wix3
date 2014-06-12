//-----------------------------------------------------------------------
// <copyright file="CustomTables.CustomTableTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for custom tables
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.CustomTables
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for custom tables
    /// </summary>
    public class CustomTableTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\CustomTables\CustomTableTests");

        [NamedFact]
        [Description("Verify that a custom table can be created")]
        [Priority(1)]
        public void SimpleCustomTable()
        {
            string msi = Builder.BuildPackage(Path.Combine(CustomTableTests.TestDataDirectory, @"SimpleCustomTable\product.wxs"));
            Verifier.VerifyResults(Path.Combine(CustomTableTests.TestDataDirectory, @"SimpleCustomTable\expected.msi"), msi, "CustomTable1");
        }

        [NamedFact]
        [Description("Verify that a null values in a custom table triggers an error if Nullable='yes' is not specified")]
        [Priority(1)]
        public void NullValues()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CustomTableTests.TestDataDirectory, @"NullValues\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(53, "There is no data for column 'Column2' in a contained row of custom table 'CustomTable1'.  A non-null value must be supplied for this column.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(53, "There is no data for column 'Column2' in a contained row of custom table 'CustomTable1'.  A non-null value must be supplied for this column.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 53;
            light.Run();
        }
        [NamedFact]
        [Description("Verify that invalid type in a custom table triggers an error if wrong type is specified")]
        [Priority(1)]
        public void InvalidType()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CustomTableTests.TestDataDirectory, @"InvalidType\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(8, "The Column2/@CustomTable1 attribute's value, 'C', is not a legal integer value.  Legal integer values are from -2,147,483,648 to 2,147,483,647.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(53, "There is no data for column 'Column2' in a contained row of custom table 'CustomTable1'.  A non-null value must be supplied for this column.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 53;
            light.Run();
        }

    }
}