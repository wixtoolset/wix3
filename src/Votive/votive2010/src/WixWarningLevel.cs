// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Enumeration for the warning level used during build time
    /// </summary>
    public enum WixWarningLevel
    {
        /// <summary>
        /// No warnings at all.
        /// </summary>
        None,

        /// <summary>
        /// Only the more important warnings are shown.
        /// </summary>
        Normal,

        /// <summary>
        /// All possible warnings.
        /// </summary>
        Pedantic,
    }
}
