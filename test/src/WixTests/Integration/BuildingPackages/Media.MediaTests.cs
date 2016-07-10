// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Media
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for the Media element
    /// </summary>
    public class MediaTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Media\MediaTests");

        [NamedFact]
        [Description("Verify that files can be assigned to different media")]
        [Priority(1)]
        public void SimpleMedia()
        {
            string sourceFile = Path.Combine(MediaTests.TestDataDirectory, @"SimpleMedia\product.wxs");
            string msi = Builder.BuildPackage(sourceFile);

            Verifier.VerifyResults(Path.Combine(MediaTests.TestDataDirectory, @"SimpleMedia\expected.msi"), msi, "File", "Media");
        }
    }
}
