// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// An MSBuild task to run the WiX lib tool.
    /// </summary>
    public sealed class Lit : WixToolTask
    {
        private const string LitToolName = "lit.exe";

        private string[] baseInputPaths;
        private ITaskItem[] bindInputPaths;
        private bool bindFiles;
        private ITaskItem[] extensions;
        private ITaskItem[] localizationFiles;
        private ITaskItem[] objectFiles;
        private ITaskItem outputFile;
        private bool pedantic;
        private bool suppressIntermediateFileVersionMatching;
        private bool suppressSchemaValidation;
        private string extensionDirectory;
        private string[] referencePaths;

        // TODO: remove this property entirely in v4.0
        [Obsolete("Use BindInputPaths instead of BaseInputPaths.")]
        public string[] BaseInputPaths
        {
            get { return this.baseInputPaths; }
            set { this.baseInputPaths = value; }
        }

        public ITaskItem[] BindInputPaths
        {
            get { return this.bindInputPaths; }
            set { this.bindInputPaths = value; }
        }

        public bool BindFiles
        {
            get { return this.bindFiles; }
            set { this.bindFiles = value; }
        }

        public ITaskItem[] Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        public ITaskItem[] LocalizationFiles
        {
            get { return this.localizationFiles; }
            set { this.localizationFiles = value; }
        }

        [Required]
        public ITaskItem[] ObjectFiles
        {
            get { return this.objectFiles; }
            set { this.objectFiles = value; }
        }

        [Required]
        [Output]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        public bool Pedantic
        {
            get { return this.pedantic; }
            set { this.pedantic = value; }
        }

        public bool SuppressIntermediateFileVersionMatching
        {
            get { return this.suppressIntermediateFileVersionMatching; }
            set { this.suppressIntermediateFileVersionMatching = value; }
        }

        public bool SuppressSchemaValidation
        {
            get { return this.suppressSchemaValidation; }
            set { this.suppressSchemaValidation = value; }
        }

        public string ExtensionDirectory
        {
            get { return this.extensionDirectory; }
            set { this.extensionDirectory = value; }
        }

        public string[] ReferencePaths
        {
            get { return this.referencePaths; }
            set { this.referencePaths = value; }
        }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of lit.exe</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return LitToolName; }
        }

        /// <summary>
        /// Get the path to the executable. 
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply lit.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return LitToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), LitToolName);
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendArrayIfNotNull("-b ", this.baseInputPaths);
            if (null != this.BindInputPaths)
            {
                Queue<String> formattedBindInputPaths = new Queue<String>();
                foreach (ITaskItem item in this.BindInputPaths)
                {
                    String formattedPath = string.Empty;
                    String bindName = item.GetMetadata("BindName");
                    if (!String.IsNullOrEmpty(item.GetMetadata("BindName")))
                    {
                        formattedPath = String.Concat(bindName, "=", item.GetMetadata("FullPath"));
                    }
                    else
                    {
                        formattedPath = item.GetMetadata("FullPath");
                    }
                    formattedBindInputPaths.Enqueue(formattedPath);
                }
                commandLineBuilder.AppendArrayIfNotNull("-b ", formattedBindInputPaths.ToArray());
            }
            commandLineBuilder.AppendIfTrue("-bf", this.BindFiles);
            commandLineBuilder.AppendExtensions(this.extensions, this.ExtensionDirectory, this.referencePaths);
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendIfTrue("-pedantic", this.Pedantic);
            commandLineBuilder.AppendIfTrue("-ss", this.SuppressSchemaValidation);
            commandLineBuilder.AppendIfTrue("-sv", this.SuppressIntermediateFileVersionMatching);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);

            List<string> objectFilePaths = AdjustFilePaths(this.objectFiles, this.ReferencePaths);
            commandLineBuilder.AppendFileNamesIfNotNull(objectFilePaths.ToArray(), " ");
        }
    }
}
