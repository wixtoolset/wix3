// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Compression
{
using System;
using System.Collections.Generic;
using System.Text;

    /// <summary>
    /// Specifies the compression level ranging from minimum compresion to
    /// maximum compression, or no compression at all.
    /// </summary>
    /// <remarks>
    /// Although only four values are enumerated, any integral value between
    /// <see cref="CompressionLevel.Min"/> and <see cref="CompressionLevel.Max"/> can also be used.
    /// </remarks>
    public enum CompressionLevel
    {
        /// <summary>Do not compress files, only store.</summary>
        None = 0,

        /// <summary>Minimum compression; fastest.</summary>
        Min = 1,

        /// <summary>A compromize between speed and compression efficiency.</summary>
        Normal = 6,

        /// <summary>Maximum compression; slowest.</summary>
        Max = 10
    }
}
