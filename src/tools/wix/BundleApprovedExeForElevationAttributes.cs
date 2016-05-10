// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Attributes available for an ApprovedExeForElevation.
    /// </summary>
    [Flags]
    public enum BundleApprovedExeForElevationAttributes : int
    {
        None = 0x0,
        Win64 = 0x1,
    }
}
