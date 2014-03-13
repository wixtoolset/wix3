//-------------------------------------------------------------------------------------------------
// <copyright file="heatfile.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Build task to execute the file harvester extension of the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using Microsoft.Build.Framework;

    public sealed class HeatFile : HeatTask
    {
        private string file;
        private bool suppressCom;
        private bool suppressRegistry;
        private bool suppressRootDirectory;
        private string template;
        private string componentGroupName;
        private string directoryRefId;
        private string preprocessorVariable;

        public string ComponentGroupName
        {
            get { return this.componentGroupName; }
            set { this.componentGroupName = value; }
        }

        public string DirectoryRefId
        {
            get { return this.directoryRefId; }
            set { this.directoryRefId = value; }
        }

        [Required]
        public string File
        {
            get { return this.file; }
            set { this.file = value; }
        }

        public string PreprocessorVariable
        {
            get { return this.preprocessorVariable; }
            set { this.preprocessorVariable = value; }
        }

        public bool SuppressCom
        {
            get { return this.suppressCom; }
            set { this.suppressCom = value; }
        }

        public bool SuppressRegistry
        {
            get { return this.suppressRegistry; }
            set { this.suppressRegistry = value; }
        }

        public bool SuppressRootDirectory
        {
            get { return this.suppressRootDirectory; }
            set { this.suppressRootDirectory = value; }
        }

        public string Template
        {
            get { return this.template; }
            set { this.template = value; }
        }

        protected override string OperationName
        {
            get { return "file"; }
        }

        /// <summary>
        /// Generate the command line arguments to write to the response file from the properties.
        /// </summary>
        /// <returns>Command line string.</returns>
        protected override string GenerateResponseFileCommands()
        {
            WixCommandLineBuilder commandLineBuilder = new WixCommandLineBuilder();

            commandLineBuilder.AppendSwitch(this.OperationName);
            commandLineBuilder.AppendFileNameIfNotNull(this.File);

            commandLineBuilder.AppendSwitchIfNotNull("-cg ", this.ComponentGroupName);
            commandLineBuilder.AppendSwitchIfNotNull("-dr ", this.DirectoryRefId);
            commandLineBuilder.AppendIfTrue("-scom", this.SuppressCom);
            commandLineBuilder.AppendIfTrue("-srd", this.SuppressRootDirectory);
            commandLineBuilder.AppendIfTrue("-sreg", this.SuppressRegistry);
            commandLineBuilder.AppendSwitchIfNotNull("-template ", this.Template);
            commandLineBuilder.AppendSwitchIfNotNull("-var ", this.PreprocessorVariable);

            base.BuildCommandLine(commandLineBuilder);
            return commandLineBuilder.ToString();
        }
    }
}
