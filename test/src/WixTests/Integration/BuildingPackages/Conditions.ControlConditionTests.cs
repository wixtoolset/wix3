//-----------------------------------------------------------------------
// <copyright file="Conditions.ControlConditionTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for conditions as they apply to controls
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
    /// Tests for conditions as they apply to controls
    /// </summary>
    [TestClass]
    public class ControlConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\ControlConditionTests");

        [TestMethod]
        [Description("Verify that a condition for a control can be specified")]
        [Priority(1)]
        [Ignore]
        public void SimpleCondition()
        {
        }
    }
}