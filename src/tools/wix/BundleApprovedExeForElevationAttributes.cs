//-------------------------------------------------------------------------------------------------
// <copyright file="BundleApprovedExeForElevationAttributes.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Bit flags for an ApprovedExeForElevation in the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

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
