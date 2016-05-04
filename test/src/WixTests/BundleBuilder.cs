// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests
{
    /// <summary>
    /// Provides methods for building a Bundle.
    /// </summary>
    public class BundleBuilder : WixTest.BundleBuilder
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BundleBuilder"/> class.
        /// </summary>
        /// <param name="test">The container <see cref="WixTests"/> class.</param>
        /// <param name="name">The name of the bundle to build.</param>
        public BundleBuilder(WixTests test, string name)
            : base(test.TestContext.TestName, name, test.TestContext.TestDataDirectory, test.TestArtifacts)
        {
        }
    }
}
