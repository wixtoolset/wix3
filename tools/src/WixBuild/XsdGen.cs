//-------------------------------------------------------------------------------------------------
// <copyright file="XsdGen.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the XsdGen class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for running XsdGen.exe, to create a .cs file and .resources file from an XML file.
    /// </summary>
    public class XsdGen : ToolTask
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private string commonNamespace;
        private string @namespace;
        private ITaskItem outputFile;
        private ITaskItem sourceFile;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="XsdGen"/> class.
        /// </summary>
        public XsdGen()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the common namespace for the generated .cs file.
        /// </summary>
        public string CommonNamespace
        {
            get { return this.commonNamespace; }
            set { this.commonNamespace = value; }
        }

        /// <summary>
        /// Gets or sets the namespace for the generated .cs file.
        /// </summary>
        [Required]
        public string Namespace
        {
            get { return this.@namespace; }
            set { this.@namespace = value; }
        }

        /// <summary>
        /// Gets or sets the generated class (.cs) file.
        /// </summary>
        [Output]
        [Required]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        /// <summary>
        /// Gets or sets the source XML file.
        /// </summary>
        [Required]
        public ITaskItem SourceFile
        {
            get { return this.sourceFile; }
            set { this.sourceFile = value; }
        }

        /// <summary>
        /// Gets the name of the executable file to run.
        /// </summary>
        protected override string ToolName
        {
            get { return "XsdGen.exe"; }
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

            commandLine.AppendFileNameIfNotNull(this.SourceFile);
            commandLine.AppendFileNameIfNotNull(this.OutputFile);
            commandLine.AppendFileNameIfNotNull(this.Namespace);
            commandLine.AppendFileNameIfNotNull(this.CommonNamespace);

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

            if (String.IsNullOrEmpty(this.SourceFile.ItemSpec))
            {
                this.Log.LogError("The SourceFile path is blank.");
                return false;
            }

            return base.ValidateParameters();
        }
    }
}
