//-----------------------------------------------------------------------
// <copyright file="PackageBuilder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Provides methods for building an MSI.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests
{
    /// <summary>
    /// Provides methods for building an MSI.
    /// </summary>
    public class PackageBuilder : WixTest.PackageBuilder
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PackageBuilder"/> class.
        /// </summary>
        /// <param name="test">The container <see cref="WixTests"/> class.</param>
        /// <param name="name">The name of the package to build.</param>
        public PackageBuilder(WixTests test, string name)
            : base(test.TestContext.TestName, name, test.TestContext.TestDataDirectory, test.TestArtifacts)
        {
        }
    }
}
