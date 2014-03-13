//-----------------------------------------------------------------------
// <copyright file="Conditions.ComponentConditionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for conditions as they apply to components
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Conditions
{
    using System;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Tests for conditions as they apply to Components
    /// </summary>
    [TestClass]
    public class ComponentConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\ComponentConditionTests");

        [TestMethod]
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
