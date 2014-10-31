//-----------------------------------------------------------------------
// <copyright file="Components.EnvironmentTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for defining Environment Variables for a component
// </summary>
//-----------------------------------------------------------------------

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