// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class that uses MSBuild to build a .wixproj
    /// </summary>
    public partial class WixprojMSBuild : MSBuild
    {
        /// <summary>
        /// Constructor that uses the default location for MSBuild.
        /// </summary>
        public WixprojMSBuild()
            : base()
        {
            this.Initialize();
        }

        /// <summary>
        /// Constructor that uses the default location for MSBuild.
        /// </summary>
        /// <param name="outputRootDirectory">The MSBuild output location</param>
        public WixprojMSBuild(string outputRootDirectory)
            : this()
        {
            this.OutputRootDirectory = outputRootDirectory;
        }

        /// <summary>
        /// Constructor that accepts a path to the MSBuild location.
        /// </summary>
        /// <param name="toolDirectory">The directory of MSBuild.exe.</param>
        /// <param name="outputRootDirectory">The MSBuild output location</param>
        public WixprojMSBuild(string toolDirectory, string outputRootDirectory)
            : base(toolDirectory)
        {
            this.Initialize();
            this.OutputRootDirectory = outputRootDirectory;
        }

        /// <summary>
        /// Searches for a task in the build output
        /// </summary>
        /// <returns>Match object holding representing the result from searching for task</returns>
        /// <remarks>The task output is stored in group "taskOutput"</remarks>
        protected override Match SearchTask(string task)
        {
            Regex regex = new Regex(String.Format("Task \"{0}\"\\s*Command:(?<taskOutput>.*)Done executing task \"{0}\"\\.", task), RegexOptions.Singleline);
            return regex.Match(this.Result.StandardOutput);
        }

        /// <summary>
        /// Perform some initialization for this class
        /// </summary>
        private void Initialize()
        {
            this.ExpectedExitCode = 0;
            this.Properties.Add("DefineSolutionProperties", "false");
            this.Properties.Add("WixToolPath", Settings.WixToolsDirectory);

            if (!String.IsNullOrEmpty(Settings.WixTargetsPath))
            {
                this.Properties.Add("WixTargetsPath", Settings.WixTargetsPath);
            }

            if (!String.IsNullOrEmpty(Settings.WixTasksPath))
            {
                this.Properties.Add("WixTasksPath", Settings.WixTasksPath);
                // this.Properties.Add("WixTasksPath", Path.Combine(Settings.WixToolDirectory, "WixTasks.dll"));
            }
        }
    }
}
