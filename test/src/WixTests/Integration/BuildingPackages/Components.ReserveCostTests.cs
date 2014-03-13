//-----------------------------------------------------------------------
// <copyright file="Components.ReserveCostTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the ReserveCost element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for the ReserveCost element
    /// </summary>
    [TestClass]
    public class ReserveCostTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\ReserveCostTests");

        [TestMethod]
        [Description("Verify that a simple use of the ReserveCost element adds the correct entry to the ReserveCost table")]
        [Priority(1)]
        public void SimpleReserveCost()
        {
            string sourceFile = Path.Combine(ReserveCostTests.TestDataDirectory, @"SimpleReserveCost\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `ReserveKey` FROM `ReserveCost` WHERE `ReserveKey` = 'Cost1'";
            Verifier.VerifyQuery(msi, query, "Cost1");
        }

        [TestMethod]
        [Description("Verify that ReserveCost can reserve disk cost for a specified directory and not just the parent Component's directory")]
        [Priority(1)]
        public void ReserveCostDirectory()
        {
            string sourceFile = Path.Combine(ReserveCostTests.TestDataDirectory, @"ReserveCostDirectory\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `ReserveFolder` FROM `ReserveCost` WHERE `ReserveKey` = 'Cost1'";
            Verifier.VerifyQuery(msi, query, "TARGETDIR");
        }

        [TestMethod]
        [Description("Verify that there is an error if the required attributes RunFromSource and RunLocal are missing")]
        [Priority(3)]
        public void InvalidReserveCost()
        {
            string sourceFile = Path.Combine(ReserveCostTests.TestDataDirectory, @"InvalidReserveCost\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 10;
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The ReserveCost/@RunFromSource attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The ReserveCost/@RunLocal attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the value of RunFromSource is not an integer")]
        [Priority(3)]
        public void InvalidRunFromSourceType()
        {
            string sourceFile = Path.Combine(ReserveCostTests.TestDataDirectory, @"InvalidRunFromSourceType\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 8;
            candle.ExpectedWixMessages.Add(new WixMessage(8, "The ReserveCost/@RunFromSource attribute's value, '12abc', is not a legal integer value.  Legal integer values are from -2,147,483,648 to 2,147,483,647.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the value of RunLocal is not an integer")]
        [Priority(3)]
        public void InvalidRunLocalType()
        {
            string sourceFile = Path.Combine(ReserveCostTests.TestDataDirectory, @"InvalidRunLocalType\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 8;
            candle.ExpectedWixMessages.Add(new WixMessage(8, "The ReserveCost/@RunLocal attribute's value, '12abc', is not a legal integer value.  Legal integer values are from -2,147,483,648 to 2,147,483,647.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }
    }
}
