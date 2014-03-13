//-----------------------------------------------------------------------
// <copyright file="Cabinets.CabinetTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for cabinets
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for cabinets
    /// </summary>
    [TestClass]
    public class CabinetTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Cabinets\CabinetTests");

        [TestMethod]
        [Description("Verify that binding fails with duplicate cabinet names")]
        [TestProperty("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1793251&group_id=105970&atid=642714")]
        [Priority(2)]
        public void DuplicateCabinetNames()
        {
            // Run Candle
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CabinetTests.TestDataDirectory, @"DuplicateCabinetNames\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(290, "Duplicate cabinet name '#dupe.cab' found.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(291, "Duplicate cabinet name '#dupe.cab' error related to previous error.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 291;
            light.Run();
        }
    }
}