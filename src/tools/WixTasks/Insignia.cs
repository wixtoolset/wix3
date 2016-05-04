// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// An MSBuild task to run the WiX transform generator.
    /// </summary>
    public sealed class Insignia : WixToolTask
    {
        private const string InsigniaToolName = "insignia.exe";

        /// <summary>
        /// Gets or sets the path to the database to inscribe.
        /// </summary>
        public ITaskItem DatabaseFile { get; set; }

        /// <summary>
        /// Gets or sets the path to the bundle to inscribe.
        /// </summary>
        public ITaskItem BundleFile { get; set; }

        /// <summary>
        /// Gets or sets the path to the original bundle that contains the attached container.
        /// </summary>
        public ITaskItem OriginalBundleFile { get; set; }

        /// <summary>
        /// Gets or sets the path to output the inscribed result.
        /// </summary>
        [Required]
        public ITaskItem OutputFile { get; set; }

        /// <summary>
        /// Gets or sets the output. Only set if insignia does work.
        /// </summary>
        [Output]
        public ITaskItem Output { get; set; }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of Insignia.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return InsigniaToolName; }
        }

        /// <summary>
        /// Get the path to the executable. 
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply Insignia.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return InsigniaToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), InsigniaToolName);
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendSwitchIfNotNull("-im ", this.DatabaseFile);
            if (null != this.OriginalBundleFile)
            {
                commandLineBuilder.AppendSwitchIfNotNull("-ab ", this.BundleFile);
                commandLineBuilder.AppendFileNameIfNotNull(this.OriginalBundleFile);
            }
            else
            {
                commandLineBuilder.AppendSwitchIfNotNull("-ib ", this.BundleFile);
            }

            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
        }

        /// <summary>
        /// Executes a tool in-process by loading the tool assembly and invoking its entrypoint.
        /// </summary>
        /// <param name="pathToTool">Path to the tool to be executed; must be a managed executable.</param>
        /// <param name="responseFileCommands">Commands to be written to a response file.</param>
        /// <param name="commandLineCommands">Commands to be passed directly on the command-line.</param>
        /// <returns>The tool exit code.</returns>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int returnCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            if (0 == returnCode) // successfully did work.
            {
                this.Output = this.OutputFile;
            }
            else if (-1 == returnCode) // no work done.
            {
                returnCode = 0;
            }

            return returnCode;
        }
    }
}
