//-----------------------------------------------------------------------
// <copyright file="Settings.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Contains settings such as default directories</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;

    /// <summary>
    /// Contains settings such as default directories.
    /// </summary>
    public static class Settings
    {
        static Settings()
        {
            Settings.Flavor = String.Empty;
            Settings.MSBuildDirectory = String.Empty;
            Settings.WixTargetsPath = String.Empty;
            Settings.WixTasksPath = String.Empty;
            Settings.WixToolsDirectory = String.Empty;
        }

        /// <summary>
        /// Folder for extracted files
        /// </summary>
        public static readonly string ExtractedFilesFolder = "extracted";

        /// <summary>
        /// Folder for .msi file types
        /// </summary>
        public static readonly string MsiFolder = "msis";

        /// <summary>
        /// Folder for .msm file types
        /// </summary>
        public static readonly string MsmFolder = "msms";

        /// <summary>
        /// Folder for .msp file types
        /// </summary>
        public static readonly string MspFolder = "msps";

        /// <summary>
        /// Folder for .mst file types
        /// </summary>
        public static readonly string MstFolder = "msts";

        /// <summary>
        /// Folder for .wixobj file types
        /// </summary>
        public static readonly string WixobjFolder = "wixobjs";

        /// <summary>
        /// Folder for .wixout file types
        /// </summary>
        public static readonly string WixoutFolder = "wixouts";

        /// <summary>
        /// Gets the unique seed for the current test run.
        /// </summary>
        public static string Seed { get; internal set; }

        /// <summary>
        /// The build flavor for test runs.
        /// </summary>
        public static string Flavor {get; set; }

        /// <summary>
        /// The default location for MSBuild.
        /// </summary>
        public static string MSBuildDirectory { get; internal set; }

        /// <summary>
        /// The location for the WiX build output.
        /// </summary>
        public static string WixBuildDirectory { get; internal set; }

        /// <summary>
        /// The location of the wix.targets file.
        /// </summary>
        public static string WixTargetsPath { get; internal set; }

        /// <summary>
        /// The location of the WixTasks.dll file.
        /// </summary>
        public static string WixTasksPath { get; internal set; }

        /// <summary>
        /// The location for the WiX tools directory.
        /// </summary>
        public static string WixToolsDirectory { get; internal set; }
    }
}
