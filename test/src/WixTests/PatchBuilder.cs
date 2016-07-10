// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests
{
    /// <summary>
    /// Provides methods for building an MSP.
    /// </summary>
    public class PatchBuilder : WixTest.PatchBuilder
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PatchBuilder"/> class.
        /// </summary>
        /// <param name="test">The container <see cref="WixTests"/> class.</param>
        /// <param name="name">The name of the patch to build.</param>
        public PatchBuilder(WixTests test, string name)
            : base(test.TestContext.TestName, name, test.TestContext.TestDataDirectory, test.TestArtifacts)
        {
        }
    }
}
