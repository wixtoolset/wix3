//-------------------------------------------------------------------------------------------------
// <copyright file="WixWarningLevel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixWarningLevel enum.
// </summary>
//-------------------------------------------------------------------------------------------------

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
