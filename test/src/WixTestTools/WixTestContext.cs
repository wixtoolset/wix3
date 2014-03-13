//-----------------------------------------------------------------------
// <copyright file="WixTestContext.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Contains the context for the test case.
    /// </summary>
    public class WixTestContext
    {
        public string Seed { get; internal set; }

        public string TestName { get; internal set; }

        public string TestDirectory { get; internal set; }

        public string TestDataDirectory { get; internal set; }

        public List<FileSystemInfo> TestArtifacts { get; internal set; }
    }
}
