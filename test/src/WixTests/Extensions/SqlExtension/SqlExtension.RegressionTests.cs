// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Sql extension tests
    /// </summary>
    public class SqlExtensionTests : WixTests
    {
        [NamedFact]
        [Priority(3)]
        public void SqlExtensionCustomActionTest01()
        {
            string testDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\SqlExtension\RegressionTests\CustomActions");
            string msi = Builder.BuildPackage(testDirectory, "product.wxs", Path.Combine(this.TestContext.TestDirectory, "product.msi"), " -ext WixSqlExtension", " -cultures:en-us -ext WixSqlExtension");

            string query = "SELECT `Source` FROM `CustomAction` WHERE `Action` = 'InstallSqlData'";
            Assert.True("ScaSchedule2".Equals(Verifier.Query(msi, query)), String.Format(@"Unexpected value in {0} returned by ""{1}""", msi, query));

            query = "SELECT `Source` FROM `CustomAction` WHERE `Action` = 'UninstallSqlData'";
            Assert.True("ScaSchedule2".Equals(Verifier.Query(msi, query)), String.Format(@"Unexpected value in {0} returned by ""{1}""", msi, query));
        }
    }
}
