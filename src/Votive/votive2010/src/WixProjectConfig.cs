//-------------------------------------------------------------------------------------------------
// <copyright file="WixProjectConfig.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixProjectConfig class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Build.BuildEngine;
    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Package.Automation;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Allows getting and setting configuration-dependent properties for a WiX project.
    /// </summary>
    [CLSCompliant(false)]
    public class WixProjectConfig : ProjectConfig
    {
        internal const string X86Platform = "x86";
        internal const string X64Platform = "x64";
        internal const string IA64Platform = "ia64";

        internal const string DebugConfiguration = "Debug";
        internal const string ReleaseConfiguration = "Release";

        internal const string ConfigConditionString = " '$(Configuration)' == '{0}' ";
        internal const string PlatformConditionString = " '$(Platform)' == '{0}' ";
        internal const string ConfigAndPlatformConditionString = " '$(Configuration)|$(Platform)' == '{0}|{1}' ";

        /// <summary>
        /// Creates a new project config instance.
        /// </summary>
        /// <param name="project">Parent project node.</param>
        /// <param name="configName">Configuration name such as "Debug".</param>
        /// <param name="platformName">Platform name such as "x86".</param>
        public WixProjectConfig(WixProjectNode project, string configName, string platformName)
            : base(project, new ConfigCanonicalName(configName, platformName))
        {
        }

        /// <summary>
        /// Gets the conditional expression for the PropertyGroup corresponding to the project config.
        /// </summary>
        public override string Condition
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, this.ConfigCanonicalName.ConfigName, this.ConfigCanonicalName.MSBuildPlatform);
            }
        }
    }
}
