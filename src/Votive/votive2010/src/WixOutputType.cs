// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Enumeration for the various output types for a Wix project.
    /// </summary>
    public enum WixOutputType
    {
        /// <summary>
        /// Wix project that builds an MSI file.
        /// </summary>
        Package,

        /// <summary>
        /// Wix project that builds an MSM file.
        /// </summary>
        Module,

        /// <summary>
        /// Wix project that builds a wixlib file.
        /// </summary>
        Library,

        /// <summary>
        /// Wix project that builds a exe file.
        /// </summary>
        Bundle,
    }
}
