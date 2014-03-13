//-------------------------------------------------------------------------------------------------
// <copyright file="Rc.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the Rc class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for reading a value from the registry.
    /// </summary>
    public class Rc : ToolTask
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private string[] includePaths;
        private string[] preprocessorDefinitions;
        private ITaskItem[] sourceFiles;
        private ITaskItem resFile;
        private bool verbose;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="Rc"/> class.
        /// </summary>
        public Rc()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the paths to add to the include searches.
        /// </summary>
        public string[] IncludePaths
        {
            get { return this.includePaths; }
            set { this.includePaths = value; }
        }

        /// <summary>
        /// Gets or sets the preprocessor definitions.
        /// </summary>
        public string[] PreprocessorDefinitions
        {
            get { return this.preprocessorDefinitions; }
            set { this.preprocessorDefinitions = value; }
        }

        /// <summary>
        /// Gets or sets the compiled .res file.
        /// </summary>
        [Output]
        public ITaskItem ResFile
        {
            get { return this.resFile; }
            set { this.resFile = value; }
        }

        /// <summary>
        /// Gets or sets the resource files to compile. Only the first item is used.
        /// </summary>
        [Required]
        public ITaskItem[] SourceFiles
        {
            get { return this.sourceFiles; }
            set { this.sourceFiles = value; }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the compiler prints progress messages.
        /// </summary>
        public bool Verbose
        {
            get { return this.verbose; }
            set { this.verbose = value; }
        }

        /// <summary>
        /// Gets the name of the executable file to run.
        /// </summary>
        protected override string ToolName
        {
            get { return "rc.exe"; }
        }

        /// <summary>
        /// Gets the first source file from the <see cref="SourceFiles"/> array.
        /// </summary>
        private ITaskItem RcFile
        {
            get
            {
                if (this.SourceFiles != null && this.SourceFiles.Length > 0)
                {
                    return this.SourceFiles[0];
                }

                return null;
            }
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

            // make sure we've set the res file path
            this.EnsureResFilePath();

            // add the /fo switch, which renames the output file
            commandLine.AppendSwitchIfNotNull("/fo ", this.ResFile);

            // verbosity
            if (this.Verbose)
            {
                commandLine.AppendSwitch("/v");
            }

            // include paths
            if (this.IncludePaths != null)
            {
                foreach (string includePath in this.IncludePaths)
                {
                    commandLine.AppendSwitchIfNotNull("/i ", includePath);
                }
            }

            // preprocessor definitions
            if (this.PreprocessorDefinitions != null)
            {
                foreach (string preprocessorDefinition in this.PreprocessorDefinitions)
                {
                    commandLine.AppendSwitchIfNotNull("/d ", preprocessorDefinition);
                }
            }

            // source file must come last
            commandLine.AppendFileNameIfNotNull(this.RcFile);

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
            // verify that there is only one source file
            if (this.SourceFiles == null || this.SourceFiles.Length != 1)
            {
                this.Log.LogError("The SourceFiles array should have only one element.");
                return false;
            }

            return base.ValidateParameters();
        }

        /// <summary>
        /// Generates the path to the output .res file if it wasn't specified.
        /// </summary>
        private void EnsureResFilePath()
        {
            if (String.IsNullOrEmpty(this.ResFile.ItemSpec))
            {
                // create the path to the .res file
                string sourcePath = this.RcFile.ItemSpec;
                string resPath = Path.Combine(Path.GetDirectoryName(sourcePath), Path.GetFileNameWithoutExtension(sourcePath) + ".res");
                this.ResFile = new TaskItem(resPath);
            }
        }
    }
}
