// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Conditions
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for conditions as they apply to Components
    /// </summary>
    public class ComponentConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\ComponentConditionTests");

        [NamedFact]
        [Description("Verify that a condition for a component can be specified")]
        [Priority(1)]
        public void SimpleCondition()
        {
            string msi = Builder.BuildPackage(Path.Combine(ComponentConditionTests.TestDataDirectory, @"SimpleCondition\product.wxs"));
            
            string query = "SELECT `Condition` FROM `Component` WHERE `Component` = 'Component1'";
            Verifier.VerifyQuery(msi, query, "1 < 2");

            query = "SELECT `Condition` FROM `Component` WHERE `Component` = 'Component2'";
            Verifier.VerifyQuery(msi, query, "%MyEnvironmentVariable~=\"A\"");
        }
    }
}
