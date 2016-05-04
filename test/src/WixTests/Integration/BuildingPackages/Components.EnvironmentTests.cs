// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for defining Environment Variables for a component
    /// </summary>
    public class EnvironmentTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\EnvironmentTests");

        [NamedFact]
        [Description("Verify that an environment variable can be created")]
        [Priority(1)]
        public void CreateEnvironment()
        {
            QuickTest .BuildMsiTest (Path.Combine(EnvironmentTests.TestDataDirectory, @"CreateEnvironment\product.wxs"),Path.Combine(EnvironmentTests.TestDataDirectory, @"CreateEnvironment\expected.msi")); 
        }

        [NamedFact]
        [Description("Verify that an environment variable can be changed")]
        [Priority(1)]
        public void ChangeEnvironment()
        {
            QuickTest.BuildMsiTest(Path.Combine(EnvironmentTests.TestDataDirectory, @"ChangeEnvironment\product.wxs"), Path.Combine(EnvironmentTests.TestDataDirectory, @"ChangeEnvironment\expected.msi"));
        }

        [NamedFact]
        [Description("Verify that an environment variable can be removed")]
        [Priority(1)]
        public void RemoveEnvironment()
        {
            QuickTest.BuildMsiTest(Path.Combine(EnvironmentTests.TestDataDirectory, @"RemoveEnvironment\product.wxs"), Path.Combine(EnvironmentTests.TestDataDirectory, @"RemoveEnvironment\expected.msi"));
        }
    }
}
