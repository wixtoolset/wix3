//-------------------------------------------------------------------------------------------------
// <copyright file="RegistrationInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Add/Remove Programs registration for the bundle.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Add/Remove Programs registration for the bundle.
    /// </summary>
    internal class RegistrationInfo
    {
        public string Name { get; set; }
        public string Publisher { get; set; }
        public string HelpLink { get; set; }
        public string HelpTelephone { get; set; }
        public string AboutUrl { get; set; }
        public string UpdateUrl { get; set; }
        public string ParentName { get; set; }
        public int DisableModify { get; set; }
        public bool DisableRemove { get; set; }
    }
}
