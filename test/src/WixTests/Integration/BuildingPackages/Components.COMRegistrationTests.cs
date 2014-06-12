//-----------------------------------------------------------------------
// <copyright file="Components.COMRegistrationTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for COM registration
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for COM registration
    /// </summary>
    public class COMRegistrationTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\COMRegistrationTests");

        [NamedFact]
        [Description("Verify that unadvertised class registration is handled correctly.")]
        [Priority(2)]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1660163&group_id=105970&atid=642714")]
        public void ValidUnadvertisedClass()
        {
            string msi = Builder.BuildPackage(Path.Combine(COMRegistrationTests.TestDataDirectory, @"ValidUnadvertisedClass\product.wxs"));

            string query = @"SELECT `Value` FROM `Registry` WHERE `Key` = 'CLSID\{00000000-0000-0000-0000-000000000001}\InprocServer32'";
            Verifier.VerifyQuery(msi, query, "[#test.txt]");
        }
    }
}
