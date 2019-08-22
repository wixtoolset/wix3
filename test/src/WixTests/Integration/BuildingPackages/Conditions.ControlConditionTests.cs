// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Conditions
{
    using System;
    using System.IO;
    using System.Text;
    using WixTest;

    /// <summary>
    /// Tests for conditions as they apply to controls
    /// </summary>
    public class ControlConditionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Conditions\ControlConditionTests");

        [NamedFact(Skip = "Ignore")]
        [Description("Verify that a condition for a control can be specified")]
        [Priority(1)]
        public void SimpleCondition()
        {
        }
    }
}
