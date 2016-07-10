// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Conditions
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for conditions as they apply to features
    /// </summary>
    public class FeatureConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\FeatureConditionTests");

        [NamedFact]
        [Description("Verify that a condition for a feature can be specified")]
        [Priority(1)]
        public void SimpleCondition()
        {
            string msi = Builder.BuildPackage(Path.Combine(FeatureConditionTests.TestDataDirectory, @"SimpleCondition\product.wxs"));

            string query = "SELECT `Condition` FROM `Condition` WHERE `Feature_` = 'Feature1'";
            Verifier.VerifyQuery(msi, query, "Property1=\"A\"");

            query = "SELECT `Condition` FROM `Condition` WHERE `Feature_` = 'Feature2'";
            Verifier.VerifyQuery(msi, query, "1=1");
        }
    }
}
