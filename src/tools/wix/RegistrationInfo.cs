// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
