//-----------------------------------------------------------------------
// <copyright file="SqlExtension.RegressionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Sql extension tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Sql extension tests
    /// </summary>
    [TestClass]
    public class SqlExtensionTests : WixTests
    {
        [TestMethod]
        [Priority(3)]
        public void SqlExtensionCustomActionTest01()
        {
            string testDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\SqlExtension\RegressionTests\CustomActions");
            string msi = Builder.BuildPackage(testDirectory, "product.wxs", "product.msi", " -ext WixSqlExtension", " -cultures:en-us -ext WixSqlExtension");

            string query = "SELECT `Source` FROM `CustomAction` WHERE `Action` = 'InstallSqlData'";
            Assert.AreEqual("ScaSchedule2", Verifier.Query(msi, query), @"Unexpected value in {0} returned by ""{1}""", msi, query);

            query = "SELECT `Source` FROM `CustomAction` WHERE `Action` = 'UninstallSqlData'";
            Assert.AreEqual("ScaSchedule2", Verifier.Query(msi, query), @"Unexpected value in {0} returned by ""{1}""", msi, query);
        }
    }
}
