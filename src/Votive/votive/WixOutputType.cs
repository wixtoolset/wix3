//-------------------------------------------------------------------------------------------------
// <copyright file="WixOutputType.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixOutputType enum.
// </summary>
//-------------------------------------------------------------------------------------------------

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
