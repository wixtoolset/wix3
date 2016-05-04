// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.WixBuild
{
    using System;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// MSBuild task for running MsgGen.exe, to create a .cs file and .resources file from an XML file.
    /// </summary>
    public class MsgGen : ToolTask
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private ITaskItem classFile;
        private ITaskItem resourcesFile;
        private ITaskItem sourceFile;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="MsgGen"/> class.
        /// </summary>
        public MsgGen()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets the generated class (.cs) file.
        /// </summary>
        [Output]
        [Required]
        public ITaskItem ClassFile
        {
            get { return this.classFile; }
            set { this.classFile = value; }
        }

        /// <summary>
        /// Gets or sets the generated .resources file.
        /// </summary>
        [Output]
        public ITaskItem ResourcesFile
        {
            get { return this.resourcesFile; }
            set { this.resourcesFile = value; }
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
            get { return "MsgGen.exe"; }
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
            commandLine.AppendFileNameIfNotNull(this.ClassFile);
            commandLine.AppendFileNameIfNotNull(this.ResourcesFile);

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

            if (String.IsNullOrEmpty(this.ClassFile.ItemSpec))
            {
                this.Log.LogError("The ClassFile path is blank.");
                return false;
            }

            return base.ValidateParameters();
        }
    }
}
