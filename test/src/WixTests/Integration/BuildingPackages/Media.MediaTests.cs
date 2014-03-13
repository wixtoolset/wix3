//-----------------------------------------------------------------------
// <copyright file="Media.MediaTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the Media element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Media
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for the Media element
    /// </summary>
    [TestClass]
    public class MediaTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Media\MediaTests");

        [TestMethod]
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