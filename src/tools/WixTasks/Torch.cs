//-------------------------------------------------------------------------------------------------
// <copyright file="Torch.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to execute the transform generator of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    public sealed class Torch : WixToolTask
    {
        private const string TorchToolName = "Torch.exe";

        private bool adminImage;
        private ITaskItem baselineFile;
        private string binaryExtractionPath;
        private bool inputIsXml;
        private bool leaveTemporaryFiles;
        private bool outputAsXml;
        private ITaskItem outputFile;
        private bool preserveUnmodifiedContent;
        private string suppressTransformErrorFlags;
        private string transformValidationFlags;
        private string transformValidationType;
        private ITaskItem updateFile;

        public bool AdminImage
        {
            get { return this.adminImage; }
            set { this.adminImage = value; }
        }


        [Required]
        public ITaskItem BaselineFile
        {
            get { return this.baselineFile; }
            set { this.baselineFile = value; }
        }

        public string BinaryExtractionPath
        {
            get { return this.binaryExtractionPath; }
            set { this.binaryExtractionPath = value; }
        }

        public bool LeaveTemporaryFiles
        {
            get { return this.leaveTemporaryFiles; }
            set { this.leaveTemporaryFiles = value; }
        }

        public bool InputIsXml
        {
            get { return this.inputIsXml; }
            set { this.inputIsXml = value; }
        }

        public bool OutputAsXml
        {
            get { return this.outputAsXml; }
            set { this.outputAsXml = value; }
        }

        public bool PreserveUnmodifiedContent
        {
            get { return this.preserveUnmodifiedContent; }
            set { this.preserveUnmodifiedContent = value; }
        }

        [Required]
        [Output]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        public string SuppressTransformErrorFlags
        {
            get { return this.suppressTransformErrorFlags; }
            set { this.suppressTransformErrorFlags = value; }
        }

        public string TransformValidationType
        {
            get { return this.transformValidationType; }
            set { this.transformValidationType = value; }
        }

        public string TransformValidationFlags
        {
            get { return this.transformValidationFlags; }
            set { this.transformValidationFlags = value; }
        }

        [Required]
        public ITaskItem UpdateFile
        {
            get { return this.updateFile; }
            set { this.updateFile = value; }
        }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of torch.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return TorchToolName; }
        }

        /// <summary>
        /// Get the path to the executable. 
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply torch.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return TorchToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), TorchToolName);
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendIfTrue("-notidy", this.LeaveTemporaryFiles);
            commandLineBuilder.AppendIfTrue("-xo", this.OutputAsXml);
            commandLineBuilder.AppendIfTrue("-xi", this.InputIsXml);
            commandLineBuilder.AppendIfTrue("-p", this.PreserveUnmodifiedContent);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
            commandLineBuilder.AppendFileNameIfNotNull(this.BaselineFile);
            commandLineBuilder.AppendFileNameIfNotNull(this.UpdateFile);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendIfTrue("-a", this.adminImage);
            commandLineBuilder.AppendSwitchIfNotNull("-x ", this.BinaryExtractionPath);
            commandLineBuilder.AppendSwitchIfNotNull("-serr ", this.SuppressTransformErrorFlags);
            commandLineBuilder.AppendSwitchIfNotNull("-t ", this.TransformValidationType);
            commandLineBuilder.AppendSwitchIfNotNull("-val ", this.TransformValidationFlags);
        }
    }
}
