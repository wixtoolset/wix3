// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Microsoft.Tools.WindowsInstallerXml.Build.Tasks;

    /// <summary>
    /// An MSBuild task to run the WiX patch builder.
    /// </summary>
    public sealed class Pyro : WixToolTask
    {
        private const string PyroToolName = "pyro.exe";

        public bool BinaryDeltaPatch { get; set; }
        public string CabinetCachePath { get; set; }
        public string ExtensionDirectory { get; set; }
        public ITaskItem[] Extensions { get; set; }
        public bool LeaveTemporaryFiles { get; set; }
        public string[] ReferencePaths { get; set; }
        public bool ReuseCabinetCache { get; set; }
        public bool SetMsiAssemblyNameFileVersion { get; set; }
        public bool SuppressAssemblies { get; set; }
        public bool SuppressFiles { get; set; }
        public bool SuppressFileHashAndInfo { get; set; }
        public bool SuppressPdbOutput { get; set; }

        [Required]
        public string DefaultBaselineId { get; set; }

        public ITaskItem[] BindInputPathsForTarget { get; set; }
        public ITaskItem[] BindInputPathsForUpdated { get; set; }

        [Required]
        public ITaskItem InputFile { get; set; }

        [Required]
        [Output]
        public ITaskItem OutputFile { get; set; }

        [Output]
        public ITaskItem PdbOutputFile { get; set; }

        [Required]
        public ITaskItem[] Transforms { get; set; }

        /// <summary>
        /// Get the name of the executable.
        /// </summary>
        /// <remarks>The ToolName is used with the ToolPath to get the location of pyro.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return PyroToolName; }
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
                return PyroToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), PyroToolName);
        }

        /// <summary>
        /// Builds a command line for bind-input paths (-bt and -bu switches).
        /// </summary>
        private void AppendBindInputPaths(WixCommandLineBuilder commandLineBuilder, IEnumerable<ITaskItem> bindInputPaths, string switchName)
        {
            if (null != bindInputPaths)
            {
                Queue<String> formattedBindInputPaths = new Queue<String>();
                foreach (ITaskItem item in bindInputPaths)
                {
                    String formattedPath = string.Empty;
                    String bindName = item.GetMetadata("BindName");
                    if (!String.IsNullOrEmpty(bindName))
                    {
                        formattedPath = String.Concat(bindName, "=", item.GetMetadata("FullPath"));
                    }
                    else
                    {
                        formattedPath = item.GetMetadata("FullPath");
                    }
                    formattedBindInputPaths.Enqueue(formattedPath);
                }

                commandLineBuilder.AppendArrayIfNotNull(switchName, formattedBindInputPaths.ToArray());
            }
        }

        /// <summary>
        /// Builds a command line from options in this task.
        /// </summary>
        protected override void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            // Always put the output first so it is easy to find in the log.
            commandLineBuilder.AppendSwitchIfNotNull("-out ", this.OutputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-pdbout ", this.PdbOutputFile);

            base.BuildCommandLine(commandLineBuilder);

            this.AppendBindInputPaths(commandLineBuilder, this.BindInputPathsForTarget, "-bt ");
            this.AppendBindInputPaths(commandLineBuilder, this.BindInputPathsForUpdated, "-bu ");

            commandLineBuilder.AppendFileNameIfNotNull(this.InputFile);
            commandLineBuilder.AppendSwitchIfNotNull("-cc ", this.CabinetCachePath);
            commandLineBuilder.AppendIfTrue("-delta", this.BinaryDeltaPatch);
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.ReferencePaths);
            commandLineBuilder.AppendIfTrue("-fv", this.SetMsiAssemblyNameFileVersion);
            commandLineBuilder.AppendIfTrue("-notidy", this.LeaveTemporaryFiles);
            commandLineBuilder.AppendIfTrue("-reusecab", this.ReuseCabinetCache);
            commandLineBuilder.AppendIfTrue("-sa", this.SuppressAssemblies);
            commandLineBuilder.AppendIfTrue("-sf", this.SuppressFiles);
            commandLineBuilder.AppendIfTrue("-sh", this.SuppressFileHashAndInfo);
            commandLineBuilder.AppendIfTrue("-spdb", this.SuppressPdbOutput);
            foreach (ITaskItem transform in this.Transforms)
            {
                string transformPath = transform.ItemSpec;
                string baselineId = transform.GetMetadata("OverrideBaselineId");
                if (String.IsNullOrEmpty(baselineId))
                {
                    baselineId = this.DefaultBaselineId;
                }

                commandLineBuilder.AppendTextIfNotNull(String.Format("-t {0} {1}", baselineId, transformPath));
            }

            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);
        }
    }
}
