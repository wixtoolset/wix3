// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for running FlattenXml.vbs, which flattens an XML file by removing excess whitespace.
    /// </summary>
    public class FlattenXml : ToolTask
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private ITaskItem[] flattenedFiles;
        private ITaskItem[] sourceFiles;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="FlattenXml"/> class.
        /// </summary>
        public FlattenXml()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the path to the target (flattened) files.
        /// </summary>
        [Output]
        [Required]
        public ITaskItem[] FlattenedFiles
        {
            get { return this.flattenedFiles; }
            set { this.flattenedFiles = value; }
        }

        /// <summary>
        /// Gets the source files to flatten.
        /// </summary>
        [Required]
        public ITaskItem[] SourceFiles
        {
            get { return this.sourceFiles; }
            set { this.sourceFiles = value; }
        }

        /// <summary>
        /// Gets the name of the executable file to run.
        /// </summary>
        protected override string ToolName
        {
            get { return "FlattenXml.exe"; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Returns a string value containing the command line arguments to pass directly to the executable file.
        /// </summary>
        /// <returns>A string value containing the command line arguments to pass directly to the executable file.</returns>
        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder commandLine = new CommandLineBuilder();

            commandLine.AppendSwitch("-nologo");

            for (int i = 0; i < this.SourceFiles.Length; i++)
            {
                commandLine.AppendFileNameIfNotNull(this.SourceFiles[i].ItemSpec);
                commandLine.AppendFileNameIfNotNull(this.FlattenedFiles[i].ItemSpec);
            }

            return commandLine.ToString();
        }

        /// <summary>
        /// Returns the fully qualified path to the executable file.
        /// </summary>
        /// <returns>The fully qualified path to the executable file.</returns>
        protected override string GenerateFullPathToTool()
        {
            // the base class will combine ToolPath with ToolName if ToolPath is not null or empty,
            // and since we ensure that ToolPath is not empty in ValidateParameters, then we're
            // fine returning just the tool name
            return this.ToolName;
        }

        /// <summary>
        /// Indicates whether all task parameters are valid.
        /// </summary>
        /// <returns>true if all task parameters are valid; otherwise, false.</returns>
        protected override bool ValidateParameters()
        {
            // verify that ToolPath has been set
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                this.Log.LogError("The ToolPath has not been set.");
                return false;
            }

            // the source and target arrays should have the same length
            if (this.SourceFiles.Length != this.FlattenedFiles.Length)
            {
                this.Log.LogError("The SourceFiles and FlattenedFiles arrays do not have the same length.");
                return false;
            }

            // make sure the source files exists
            foreach (ITaskItem sourceItem in this.SourceFiles)
            {
                if (!File.Exists(sourceItem.ItemSpec))
                {
                    this.Log.LogError("The source file '{0}' does not exist.", sourceItem.ItemSpec);
                    return false;
                }
            }

            return base.ValidateParameters();
        }
    }
}
