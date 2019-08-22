// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Attributes available for a bundle package.
    /// </summary>
    [Flags]
    internal enum BundlePackageAttributes : int
    {
        None = 0x0,
        Permanent = 0x1,
        Visible = 0x2,
        Slipstream = 0x4,
    }
}
