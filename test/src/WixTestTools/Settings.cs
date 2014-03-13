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
    /// Contains settings such as default directories
    /// </summary>
    public static class Settings
    {

        /// <summary>
        /// The build flavor to run tests against
        /// </summary>
        private static string flavor = String.Empty;

        /// <summary>
        /// The default location for MSBuild
        /// </summary>
        private static string msBuildDirectory = String.Empty;

        /// <summary>
        /// The location of the wix.targets file
        /// </summary>
        private static string wixTargetsPath = String.Empty;

        /// <summary>
        /// The location of the WixTasks.dll file
        /// </summary>
        private static string wixTasksPath = String.Empty;

        /// <summary>
        /// The default location for the WiX tools
        /// </summary>
        private static string wixToolDirectory = String.Empty;

        /// <summary>
        /// Folder for extracted files
        /// </summary>
        public const string ExtractedFilesFolder = "extracted";

        /// <summary>
        /// Folder for .msi file types
        /// </summary>
        public const string MSIFolder = "msis";

        /// <summary>
        /// Folder for .msm file types
        /// </summary>
        public const string MSMFolder = "msms";

        /// <summary>
        /// Folder for .msp file types
        /// </summary>
        public const string MSPFolder = "msps";

        /// <summary>
        /// Folder for .mst file types
        /// </summary>
        public const string MSTFolder = "msts";

        /// <summary>
        /// Folder for .wixobj file types
        /// </summary>
        public const string WixobjFolder = "wixobjs";

        /// <summary>
        /// Folder for .wixout file types
        /// </summary>
        public const string WixoutFolder = "wixouts";

        /// <summary>
        /// The build flavor to run tests against
        /// </summary>
        public static string Flavor
        {
            get
            {
                return Settings.flavor;
            }

            set
            {
                Settings.flavor = value;
            }
        }

        /// <summary>
        /// The default location for MSBuild
        /// </summary>
        public static string MSBuildDirectory
        {
            get
            {
                return Settings.msBuildDirectory;
            }

            set
            {
                Settings.msBuildDirectory = value;
            }
        }

        /// <summary>
        /// The location for the WiX build output
        /// </summary>
        public static string WixBuildDirectory { get; set; }

        /// <summary>
        /// The location of the wix.targets file
        /// </summary>
        public static string WixTargetsPath
        {
            get
            {
                return Settings.wixTargetsPath;
            }

            set
            {
                Settings.wixTargetsPath = value;
            }
        }

        /// <summary>
        /// The location of the WixTasks.dll file
        /// </summary>
        public static string WixTasksPath
        {
            get
            {
                return Settings.wixTasksPath;
            }

            set
            {
                Settings.wixTasksPath = value;
            }
        }

        /// <summary>
        /// The location for the WiX tools
        /// </summary>
        public static string WixToolDirectory
        {
            get
            {
                return Settings.wixToolDirectory;
            }

            set
            {
                Settings.wixToolDirectory = value;
            }
        }
    }
}
