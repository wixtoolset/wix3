// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// An MSBuild task to run the WiX linker.
    /// </summary>
    public sealed class Light : WixToolTask
    {
        private const string LightToolName = "Light.exe";

        private string additionalCub;
        private bool allowIdenticalRows;
        private bool allowDuplicateDirectoryIds;
        private bool allowUnresolvedReferences;
        private string[] baseInputPaths;
        private ITaskItem[] bindInputPaths;
        private bool backwardsCompatibleGuidGeneration;
        private bool bindFiles;
        private ITaskItem builtOutputsFile;
        private string cabinetCachePath;
        private int cabinetCreationThreadCount = WixCommandLineBuilder.Unspecified;
        private ITaskItem contentsFile;
        private string cultures;
        private string customBinder;
        private string defaultCompressionLevel;
        private ITaskItem[] extensions;
        private string[] ices;
        private bool leaveTemporaryFiles;
        private ITaskItem[] localizationFiles;
        private ITaskItem[] objectFiles;
        private bool outputAsXml;
        private ITaskItem outputsFile;
        private ITaskItem outputFile;
        private ITaskItem pdbOutputFile;
        private ITaskItem wixProjectFile;
        private bool pedantic;
        private bool reuseCabinetCache;
        private bool setMsiAssemblyNameFileVersion;
        private bool suppressAclReset;
        private bool suppressAssemblies;
        private bool suppressDefaultAdminSequenceActions;
        private bool suppressDefaultAdvSequenceActions;
        private bool suppressDefaultUISequenceActions;
        private bool dropUnrealTables;
        private bool exactAssemblyVersions;
        private bool suppressFileHashAndInfo;
        private bool suppressFiles;
        private bool suppressIntermediateFileVersionMatching;
        private string[] suppressIces;
        private bool suppressLayout;
        private bool suppressLocalization;
        private bool suppressMsiAssemblyTableProcessing;
        private bool suppressPatchSequenceData;
        private bool suppressPdbOutput;
        private bool suppressSchemaValidation;
        private bool suppressValidation;
        private bool suppressTagSectionIdAttributeOnTuples;
        private ITaskItem unreferencedSymbolsFile;
        private string[] wixVariables;
        private string extensionDirectory;
        private string[] referencePaths;

        /// <summary>
        /// Creates a new light task.
        /// </summary>
        /// <remarks>
        /// Defaults to running the task as a separate process, instead of in-proc
        /// which is the default for WixToolTasks. This allows the Win32 manifest file
        /// embedded in light.exe to enable reg-free COM interop with mergemod.dll.
        /// </remarks>
        public Light()
        {
        }

        public string AdditionalCub
        {
            get { return this.additionalCub; }
            set { this.additionalCub = value; }
        }

        public bool AllowIdenticalRows
        {
            get { return this.allowIdenticalRows; }
            set { this.allowIdenticalRows = value; }
        }

        public bool AllowDuplicateDirectoryIds
        {
            get { return this.allowDuplicateDirectoryIds; }
            set { this.allowDuplicateDirectoryIds = value; }
        }

        public bool AllowUnresolvedReferences
        {
            get { return this.allowUnresolvedReferences; }
            set { this.allowUnresolvedReferences = value; }
        }

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

        public bool BackwardsCompatibleGuidGeneration
        {
            get { return this.backwardsCompatibleGuidGeneration; }
            set { this.backwardsCompatibleGuidGeneration = value; }
        }

        public bool BindFiles
        {
            get { return this.bindFiles; }
            set { this.bindFiles = value; }
        }

        public string CabinetCachePath
        {
            get { return this.cabinetCachePath; }
            set { this.cabinetCachePath = value; }
        }

        public int CabinetCreationThreadCount
        {
            get { return this.cabinetCreationThreadCount; }
            set { this.cabinetCreationThreadCount = value; }
        }

        public ITaskItem BindBuiltOutputsFile
        {
            get { return this.builtOutputsFile; }
            set { this.builtOutputsFile = value; }
        }

        public ITaskItem BindContentsFile
        {
            get { return this.contentsFile; }
            set { this.contentsFile = value; }
        }

        public ITaskItem BindOutputsFile
        {
            get { return this.outputsFile; }
            set { this.outputsFile = value; }
        }

        public string Cultures
        {
            get { return this.cultures; }
            set { this.cultures = value; }
        }

        public string CustomBinder
        {
            get { return this.customBinder; }
            set { this.customBinder = value; }
        }

        public string DefaultCompressionLevel
        {
            get { return this.defaultCompressionLevel; }
            set { this.defaultCompressionLevel = value; }
        }

        public bool DropUnrealTables
        {
            get { return this.dropUnrealTables; }
            set { this.dropUnrealTables = value; }
        }

        public bool ExactAssemblyVersions
        {
            get { return this.exactAssemblyVersions; }
            set { this.exactAssemblyVersions = value; }
        }

        public ITaskItem[] Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        public string[] Ices
        {
            get { return this.ices; }
            set { this.ices = value; }
        }

        public bool LeaveTemporaryFiles
        {
            get { return this.leaveTemporaryFiles; }
            set { this.leaveTemporaryFiles = value; }
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

        public bool OutputAsXml
        {
            get { return this.outputAsXml; }
            set { this.outputAsXml = value; }
        }

        [Required]
        [Output]
        public ITaskItem OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
        }

        public bool SuppressPatchSequenceData
        {
            get { return this.suppressPatchSequenceData; }
            set { this.suppressPatchSequenceData = value; }
        }

        [Output]
        public ITaskItem PdbOutputFile
        {
            get { return this.pdbOutputFile; }
            set { this.pdbOutputFile = value; }
        }

        public bool Pedantic
        {
            get { return this.pedantic; }
            set { this.pedantic = value; }
        }

        public bool ReuseCabinetCache
        {
            get { return this.reuseCabinetCache; }
            set { this.reuseCabinetCache = value; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public bool SetMsiAssemblyNameFileVersion
        {
            get { return this.setMsiAssemblyNameFileVersion; }
            set { this.setMsiAssemblyNameFileVersion = value; }
        }

        public bool SuppressAclReset
        {
            get { return this.suppressAclReset; }
            set { this.suppressAclReset = value; }
        }

        public bool SuppressAssemblies
        {
            get { return this.suppressAssemblies; }
            set { this.suppressAssemblies = value; }
        }

        public bool SuppressDefaultAdminSequenceActions
        {
            get { return this.suppressDefaultAdminSequenceActions; }
            set { this.suppressDefaultAdminSequenceActions = value; }
        }

        public bool SuppressDefaultAdvSequenceActions
        {
            get { return this.suppressDefaultAdvSequenceActions; }
            set { this.suppressDefaultAdvSequenceActions = value; }
        }

        public bool SuppressDefaultUISequenceActions
        {
            get { return this.suppressDefaultUISequenceActions; }
            set { this.suppressDefaultUISequenceActions = value; }
        }

        public bool SuppressFileHashAndInfo
        {
            get { return this.suppressFileHashAndInfo; }
            set { this.suppressFileHashAndInfo = value; }
        }

        public bool SuppressFiles
        {
            get { return this.suppressFiles; }
            set { this.suppressFiles = value; }
        }

        public bool SuppressIntermediateFileVersionMatching
        {
            get { return this.suppressIntermediateFileVersionMatching; }
            set { this.suppressIntermediateFileVersionMatching = value; }
        }

        public string[] SuppressIces
        {
            get { return this.suppressIces; }
            set { this.suppressIces = value; }
        }

        public bool SuppressLayout
        {
            get { return this.suppressLayout; }
            set { this.suppressLayout = value; }
        }

        public bool SuppressLocalization
        {
            get { return this.suppressLocalization; }
            set { this.suppressLocalization = value; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public bool SuppressMsiAssemblyTableProcessing
        {
            get { return this.suppressMsiAssemblyTableProcessing; }
            set { this.suppressMsiAssemblyTableProcessing = value; }
        }

        public bool SuppressPdbOutput
        {
            get { return this.suppressPdbOutput; }
            set { this.suppressPdbOutput = value; }
        }

        public bool SuppressSchemaValidation
        {
            get { return this.suppressSchemaValidation; }
            set { this.suppressSchemaValidation = value; }
        }

        public bool SuppressValidation
        {
            get { return this.suppressValidation; }
            set { this.suppressValidation = value; }
        }

        public bool SuppressTagSectionIdAttributeOnTuples
        {
            get { return this.suppressTagSectionIdAttributeOnTuples; }
            set { this.suppressTagSectionIdAttributeOnTuples = value; }
        }

        [Output]
        public ITaskItem UnreferencedSymbolsFile
        {
            get { return this.unreferencedSymbolsFile; }
            set { this.unreferencedSymbolsFile = value; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public ITaskItem WixProjectFile
        {
            get { return this.wixProjectFile; }
            set { this.wixProjectFile = value; }
        }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public string[] WixVariables
        {
            get { return this.wixVariables; }
            set { this.wixVariables = value; }
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
        /// <remarks>The ToolName is used with the ToolPath to get the location of light.exe.</remarks>
        /// <value>The name of the executable.</value>
        protected override string ToolName
        {
            get { return LightToolName; }
        }

        /// <summary>
        /// Get the path to the executable. 
        /// </summary>
        /// <remarks>GetFullPathToTool is only called when the ToolPath property is not set (see the ToolName remarks above).</remarks>
        /// <returns>The full path to the executable or simply light.exe if it's expected to be in the system path.</returns>
        protected override string GenerateFullPathToTool()
        {
            // If there's not a ToolPath specified, it has to be in the system path.
            if (String.IsNullOrEmpty(this.ToolPath))
            {
                return LightToolName;
            }

            return Path.Combine(Path.GetFullPath(this.ToolPath), LightToolName);
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

            commandLineBuilder.AppendIfTrue("-ai", this.AllowIdenticalRows);
            commandLineBuilder.AppendIfTrue("-ad", this.AllowDuplicateDirectoryIds);
            commandLineBuilder.AppendIfTrue("-au", this.AllowUnresolvedReferences);
            commandLineBuilder.AppendArrayIfNotNull("-b ", this.baseInputPaths);

            if (null != this.BindInputPaths)
            {
                Queue<String> formattedBindInputPaths = new Queue<String>();
                foreach (ITaskItem item in this.BindInputPaths)
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
                commandLineBuilder.AppendArrayIfNotNull("-b ", formattedBindInputPaths.ToArray());
            }

            commandLineBuilder.AppendIfTrue("-bcgg", this.BackwardsCompatibleGuidGeneration);
            commandLineBuilder.AppendIfTrue("-bf", this.BindFiles);
            commandLineBuilder.AppendSwitchIfNotNull("-cc ", this.CabinetCachePath);
            commandLineBuilder.AppendIfSpecified("-ct ", this.CabinetCreationThreadCount);
            commandLineBuilder.AppendSwitchIfNotNull("-cub ", this.AdditionalCub);
            commandLineBuilder.AppendSwitchIfNotNull("-cultures:", this.Cultures);
            commandLineBuilder.AppendSwitchIfNotNull("-binder ", this.CustomBinder);
            commandLineBuilder.AppendArrayIfNotNull("-d", this.WixVariables);
            commandLineBuilder.AppendSwitchIfNotNull("-dcl:", this.DefaultCompressionLevel);
            commandLineBuilder.AppendIfTrue("-dut", this.DropUnrealTables);
            commandLineBuilder.AppendIfTrue("-eav", this.ExactAssemblyVersions);
            commandLineBuilder.AppendExtensions(this.Extensions, this.ExtensionDirectory, this.referencePaths);
            commandLineBuilder.AppendIfTrue("-fv", this.SetMsiAssemblyNameFileVersion);
            commandLineBuilder.AppendArrayIfNotNull("-ice:", this.Ices);
            commandLineBuilder.AppendArrayIfNotNull("-loc ", this.LocalizationFiles);
            commandLineBuilder.AppendIfTrue("-notidy", this.LeaveTemporaryFiles);
            commandLineBuilder.AppendIfTrue("-pedantic", this.Pedantic);
            commandLineBuilder.AppendIfTrue("-reusecab", this.ReuseCabinetCache);
            commandLineBuilder.AppendIfTrue("-sa", this.SuppressAssemblies);
            commandLineBuilder.AppendIfTrue("-sacl", this.SuppressAclReset);
            commandLineBuilder.AppendIfTrue("-sadmin", this.SuppressDefaultAdminSequenceActions);
            commandLineBuilder.AppendIfTrue("-sadv", this.SuppressDefaultAdvSequenceActions);
            commandLineBuilder.AppendArrayIfNotNull("-sice:", this.SuppressIces);
            commandLineBuilder.AppendIfTrue("-sma", this.SuppressMsiAssemblyTableProcessing);
            commandLineBuilder.AppendIfTrue("-sf", this.SuppressFiles);
            commandLineBuilder.AppendIfTrue("-sh", this.SuppressFileHashAndInfo);
            commandLineBuilder.AppendIfTrue("-sl", this.SuppressLayout);
            commandLineBuilder.AppendIfTrue("-sloc", this.SuppressLocalization);
            commandLineBuilder.AppendIfTrue("-spdb", this.SuppressPdbOutput);
            commandLineBuilder.AppendIfTrue("-spsd", this.SuppressPatchSequenceData);
            commandLineBuilder.AppendIfTrue("-ss", this.SuppressSchemaValidation);
            commandLineBuilder.AppendIfTrue("-sts", this.SuppressTagSectionIdAttributeOnTuples);
            commandLineBuilder.AppendIfTrue("-sui", this.SuppressDefaultUISequenceActions);
            commandLineBuilder.AppendIfTrue("-sv", this.SuppressIntermediateFileVersionMatching);
            commandLineBuilder.AppendIfTrue("-sval", this.SuppressValidation);
            commandLineBuilder.AppendSwitchIfNotNull("-usf ", this.UnreferencedSymbolsFile);
            commandLineBuilder.AppendIfTrue("-xo", this.OutputAsXml);
            commandLineBuilder.AppendSwitchIfNotNull("-contentsfile ", this.BindContentsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-outputsfile ", this.BindOutputsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-builtoutputsfile ", this.BindBuiltOutputsFile);
            commandLineBuilder.AppendSwitchIfNotNull("-wixprojectfile ", this.WixProjectFile);
            commandLineBuilder.AppendTextIfNotNull(this.AdditionalOptions);

            List<string> objectFilePaths = AdjustFilePaths(this.objectFiles, this.ReferencePaths);
            commandLineBuilder.AppendFileNamesIfNotNull(objectFilePaths.ToArray(), " ");
        }
    }
}
