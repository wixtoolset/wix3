// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for running DocCompiler.exe, to create HTML Help files.
    /// </summary>
    public class DocCompiler : ToolTask
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private string workingDirectory;
        private string helpCompiler;
        private ITaskItem helpFile;
        private ITaskItem tableOfContents;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="DocCompiler"/> class.
        /// </summary>
        public DocCompiler()
        {
            this.workingDirectory = null;
            this.helpCompiler = null;
            this.helpFile = null;
            this.tableOfContents = null;
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the generated help file.
        /// </summary>
        [Output]
        [Required]
        public ITaskItem HelpFile
        {
            get { return this.helpFile; }
            set { this.helpFile = value; }
        }

        /// <summary>
        /// Gets or sets the table of contents XML file.
        /// </summary>
        [Required]
        public ITaskItem TableOfContents
        {
            get { return this.tableOfContents; }
            set { this.tableOfContents = value; }
        }

        /// <summary>
        /// Gets or sets the working directory.
        /// </summary>
        [Required]
        public string WorkingDirectory
        {
            get { return this.workingDirectory; }
            set { this.workingDirectory = value; }
        }

        /// <summary>
        /// Gets or sets the path to the help compiler.
        /// </summary>
        [Required]
        public string HelpCompiler 
        {
            get { return this.helpCompiler; }
            set { this.helpCompiler = value; }
        }

        /// <summary>
        /// Gets the name of the executable file to run.
        /// </summary>
        protected override string ToolName
        {
            get { return "DocCompiler.exe"; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Logs an informational message and executes the tool.
        /// </summary>
        /// <returns>true if the tool executed successfully; otherwise, false.</returns>
        public override bool Execute()
        {
            this.Log.LogMessage(MessageImportance.Normal, "Compiling \"{0}\" from directory \"{1}\"", this.HelpFile.ItemSpec, this.WorkingDirectory);
            return base.Execute();
        }

        /// <summary>
        /// Returns the working directory which contains the help files in the HTML sub-directory.
        /// </summary>
        /// <returns>A string that is the working directory.</returns>
        protected override string GetWorkingDirectory()
        {
            return this.workingDirectory;
        }

        /// <summary>
        /// Returns a string value containing the command line arguments to pass directly to the executable file.
        /// </summary>
        /// <returns>A string value containing the command line arguments to pass directly to the executable file.</returns>
        protected override string GenerateCommandLineCommands()
        {
            CommandLineBuilder commandLine = new CommandLineBuilder();

            commandLine.AppendSwitchIfNotNull("-c:", this.HelpCompiler);
            commandLine.AppendFileNameIfNotNull(this.TableOfContents);
            commandLine.AppendFileNameIfNotNull(this.HelpFile);

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

            if (String.IsNullOrEmpty(this.WorkingDirectory))
            {
                this.Log.LogError("The WorkingDirectory is not set.");
                return false;
            }

            if (String.IsNullOrEmpty(this.HelpCompiler))
            {
                this.Log.LogError("The HelpCompiler is not set.");
                return false;
            }

            if (String.IsNullOrEmpty(this.HelpFile.ItemSpec))
            {
                this.Log.LogError("The HelpFile path is blank.");
                return false;
            }

            if (String.IsNullOrEmpty(this.TableOfContents.ItemSpec))
            {
                this.Log.LogError("The TableOfContents path is blank.");
                return false;
            }

            return base.ValidateParameters();
        }
    }
}
