//-------------------------------------------------------------------------------------------------
// <copyright file="Candle.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to execute the compiler of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

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
    /// An MSBuild task to run the WiX compiler.
    /// </summary>
    public sealed class Candle : WixToolTask
    {
        private const string CandleToolName = "candle.exe";

        private string[] defineConstants;
        private ITaskItem[] extensions;
        private bool suppressFilesVitalByDefault;
        private string[] includeSearchPaths;
        private bool onlyValidateDocuments;
        private ITaskItem outputFile;
        private bool pedantic;
        private string installerPlatform;
        private string preprocessToFile;
        private bool preprocessToStdOut;
        private bool showSourceTrace;
        private ITaskItem[] sourceFiles;
        private bool suppressSchemaValidation;
        private string extensionDirectory;
        private string[] referencePaths;
        private bool fipsCompliant;

        public string[] DefineConstants
        {
            get { return this.defineConstants; }
            set { this.defineConstants = value; }
        }

        public ITaskItem[] Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        public bool SuppressFilesVitalByDefault
        {
            get { return this.suppressFilesVitalByDefault; }
            set { this.suppressFilesVitalByDefault = value; }
        }

        public string[] IncludeSearchPaths
        {
            get { return this.includeSearchPaths; }
            set { this.includeSearchPaths = value; }
        }

        public string InstallerPlatform
        {
            get { return this.installerPlatform; }
            set { this.installerPlatform = value; }
        }

        public bool OnlyValidateDocuments
        {
            get { return this.onlyValidateDocuments; }
            set { this.onlyValidateDocuments = value; }
        }

        [Output]
        [Required]
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

        public string PreprocessToFile
        {
            get { return this.preprocessToFile; }
            set { this.preprocessToFile = value; }
        }

        public bool PreprocessToStdOut
        {
            get { return this.preprocessToStdOut; }
            set { this.preprocessToStdOut = value; }
        }

        public bool ShowSourceTrace
        {
            get { return this.showSourceTrace; }
            set { this.showSourceTrace = value; }
        }

        [Required]
        public ITaskItem[] SourceFiles
        {
            get { return this.sourceFiles; }
            set { this.sourceFiles = value; }
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

        public bool FipsCompliant
        {
            get { return this.fipsCompliant; }
            set { this.fipsCompliant = value; }
        }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of candle.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return CandleToolName; }
        }

        /// <summary>
        /// Get the path to the executable. 
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply candle.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return CandleToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), CandleToolName);
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            base.BuildCommandLine(commandLineBuilder);

            commandLineBuilder.AppendArrayIfNotNull("-d", this.DefineConstants);
            commandLineBuilder.AppendIfTrue("-p", this.PreprocessToStdOut);
            commandLineBuilder.AppendSwitchIfNotNull("-p", this.PreprocessToFile);
            commandLineBuilder.AppendIfTrue("-sfdvital", this.suppressFilesVitalByDefault);
            commandLineBuilder.AppendArrayIfNotNull("-I", this.IncludeSearchPaths);
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendIfTrue("-pedantic", this.Pedantic);
            commandLineBuilder.AppendSwitchIfNotNull("-arch ", this.InstallerPlatform);
            commandLineBuilder.AppendIfTrue("-ss", this.SuppressSchemaValidation);
            commandLineBuilder.AppendIfTrue("-trace", this.ShowSourceTrace);
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.referencePaths);
            commandLineBuilder.AppendIfTrue("-zs", this.OnlyValidateDocuments);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
            commandLineBuilder.AppendIfTrue("-fips", this.fipsCompliant);

            // Support per-source-file output by looking at the SourceFiles items to
            // see if there is any "CandleOutput" metadata.  If there is, we do our own
            // appending, otherwise we fall back to the built-in "append file names" code.
            // Note also that the wix.targets "Compile" target does *not* automagically
            // fix the "@(CompileObjOutput)" list to include these new output names.
            // If you really want to use this, you're going to have to clone the target
            // in your own .targets file and create the output list yourself.
            bool usePerSourceOutput = false;
            if (this.SourceFiles != null)
            {
                foreach (ITaskItem item in this.SourceFiles)
                {
                    if (!String.IsNullOrEmpty(item.GetMetadata("CandleOutput")))
                    {
                        usePerSourceOutput = true;
                        break;
                    }
                }
            }

            if (usePerSourceOutput)
            {
                string[] newSourceNames = new string[this.SourceFiles.Length];
                for (int iSource = 0; iSource < this.SourceFiles.Length; ++iSource)
                {
                    ITaskItem item = this.SourceFiles[iSource];
                    if (null == item)
                    {
                        newSourceNames[iSource] = null;
                    }
                    else
                    {
                        string output = item.GetMetadata("CandleOutput");

                        if (!String.IsNullOrEmpty(output))
                        {
                            newSourceNames[iSource] = String.Concat(item.ItemSpec, ";", output);
                        }
                        else
                        {
                            newSourceNames[iSource] = item.ItemSpec;
                        }
                    }
                }

                commandLineBuilder.AppendFileNamesIfNotNull(newSourceNames, " ");
            }
            else
            {
                commandLineBuilder.AppendFileNamesIfNotNull(this.SourceFiles, " ");
            }
        }
    }
}
