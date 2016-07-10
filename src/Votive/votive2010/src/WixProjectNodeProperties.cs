// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio.Package;
    using Utilities = Microsoft.VisualStudio.Package.Utilities;
    using VSConstants = Microsoft.VisualStudio.VSConstants;

    /// <summary>
    /// Represents the root node of a WiX project within a Solution Explorer hierarchy.
    /// </summary>
    [CLSCompliant(false), ComVisible(true)]
    [Guid("CC565F35-2526-4426-BF53-35620AAB1DCD")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    public class WixProjectNodeProperties : ProjectNodeProperties
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixProjectNodeProperties"/> class.
        /// </summary>
        /// <param name="node">The <see cref="WixProjectNode"/> from which the properties are read.</param>
        public WixProjectNodeProperties(WixProjectNode node)
            : base(node)
        {
        }

        /// <summary>
        /// AdditionalCub property
        /// </summary>
        /// <value>Additional cub</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string AdditionalCub
        {
            get { return this.GetPropertyString(WixProjectFileConstants.AdditionalCub); }
            set { this.SetPropertyString(WixProjectFileConstants.AdditionalCub, value); }
        }

        /// <summary>
        /// AllowIdenticalRows property
        /// </summary>
        /// <value>Allow identical rows for the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool AllowIdenticalRows
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.AllowIdenticalRows, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.AllowIdenticalRows, value); }
        }

        /// <summary>
        /// BackwardsCompatibleGuidGeneration property
        /// </summary>
        /// <value>Generate backwards compatible guids</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool BackwardsCompatibleGuidGeneration
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.BackwardsCompatibleGuidGeneration, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.BackwardsCompatibleGuidGeneration, value); }
        }

        /// <summary>
        /// BaseInputPaths property
        /// </summary>
        /// <value>Linker reference paths</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string BaseInputPaths
        {
            get { return this.GetPropertyString(WixProjectFileConstants.BaseInputPaths); }
            set { this.SetPropertyString(WixProjectFileConstants.BaseInputPaths, value); }
        }

        /// <summary>
        /// CabinetCachePath property
        /// </summary>
        /// <value>Path to cabinet cache</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string CabinetCachePath
        {
            get { return this.GetPropertyString(WixProjectFileConstants.CabinetCachePath); }
            set { this.SetPropertyString(WixProjectFileConstants.CabinetCachePath, value); }
        }

        /// <summary>
        /// CabinetCreationThreadCount property
        /// </summary>
        /// <value>Number of threads to be used for creating cabinets</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public int CabinetCreationThreadCount
        {
            get { return this.GetPropertyInt32(WixProjectFileConstants.CabinetCreationThreadCount, 0); }
            set { this.SetPropertyInt32(WixProjectFileConstants.CabinetCreationThreadCount, value); }
        }

        /// <summary>
        /// Additional switches for the compiler
        /// </summary>
        /// <value>The switches passed as a string</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string CompilerAdditionalOptions
        {
            get { return this.GetPropertyString(WixProjectFileConstants.CompilerAdditionalOptions); }
            set { this.SetPropertyString(WixProjectFileConstants.CompilerAdditionalOptions, value); }
        }

        /// <summary>
        /// Additional switches for the compiler
        /// </summary>
        /// <value>Additional switches as set by the user on the property page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string CompilerAdditionalOptionsLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.CompilerAdditionalOptions, false); }
        }

        /// <summary>
        /// Cultures property
        /// </summary>
        /// <value>Cultures switch for the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string Cultures
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.Cultures);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.Cultures, value);
            }
        }

        /// <summary>
        /// DefineConstans
        /// </summary>
        /// <value>Constants for the compiles</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string DefineConstants
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.DefineConstants);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.DefineConstants, value);
            }
        }

        /// <summary>
        /// DefineConstans
        /// </summary>
        /// <value>Constants for the compiles</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string DefineConstantsLiteral
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.DefineConstants, false);
            }
        }

        /// <summary>
        /// DropUnrealTables property
        /// </summary>
        /// <value>Drop unreal tables</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool DropUnrealTables
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.DropUnrealTables, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.DropUnrealTables, value); }
        }

        /// <summary>
        /// IncludeSearchPaths property
        /// </summary>
        /// <value>Folder to search includes by the compiler</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string IncludeSearchPaths
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.IncludeSearchPaths);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.IncludeSearchPaths, value);
            }
        }

        /// <summary>
        /// InstallerPlatform property
        /// </summary>
        /// <value>The installer platform</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string InstallerPlatform
        {
            get { return this.GetPropertyString(WixProjectFileConstants.InstallerPlatform); }
            set { this.SetPropertyString(WixProjectFileConstants.InstallerPlatform, value); }
        }

        /// <summary>
        /// IntermediateOutputPath property
        /// </summary>
        /// <value>Output path for the compiler</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string IntermediateOutputPath
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.IntermediateOutputPath); 
            }
            
            set
            {
                this.SetPropertyString(WixProjectFileConstants.IntermediateOutputPath, value);
            }
        }

        /// <summary>
        /// Unexpanded IntermediateOutputPath property
        /// </summary>
        /// <value>Intermediate output path as set by the user on the property page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string IntermediateOutputPathLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.IntermediateOutputPath, false); }
        }

        /// <summary>
        /// LeaveTemporaryFiles property
        /// </summary>
        /// <value>Do not delete any temporary files when done</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LeaveTemporaryFiles
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LeaveTemporaryFiles, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LeaveTemporaryFiles, value); }
        }

        /// <summary>
        /// LibAdditionalOptions property
        /// </summary>
        /// <value>Additional switched to be passed to the librarian</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LibAdditionalOptions
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LibAdditionalOptions); }
            set { this.SetPropertyString(WixProjectFileConstants.LibAdditionalOptions, value); }
        }

        /// <summary>
        /// Additional switches for the librarian
        /// </summary>
        /// <value>Additional switches as set by the user on the property page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LibAdditionalOptionsLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LibAdditionalOptions, false); }
        }

        /// <summary>
        /// LibBindFiles property
        /// </summary>
        /// <value>Bind files into the library file</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LibBindFiles
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LibBindFiles, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LibBindFiles, value); }
        }

        /// <summary>
        /// LibSuppressIntermediateFileVersionMatching property
        /// </summary>
        /// <value>Suppress intermediate file version mismatch checking</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LibSuppressIntermediateFileVersionMatching
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LibSuppressIntermediateFileVersionMatching, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LibSuppressIntermediateFileVersionMatching, value); }
        }

        /// <summary>
        /// LibSuppressSchemaValidation property
        /// </summary>
        /// <value>Suppress schema validation by the librarian</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LibSuppressSchemaValidation
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LibSuppressSchemaValidation, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LibSuppressSchemaValidation, value); }
        }

        /// <summary>
        /// LibSuppressSpecificWarnings property
        /// </summary>
        /// <value>Suppress specific warnings by the librarian</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LibSuppressSpecificWarnings
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LibSuppressSpecificWarnings); }
            set { this.SetPropertyString(WixProjectFileConstants.LibSuppressSpecificWarnings, value); }
        }

        /// <summary>
        /// LibTreatWarningsAsErrors property
        /// </summary>
        /// <value>Librarian treats warnings as errors</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LibTreatWarningsAsErrors
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LibTreatWarningsAsErrors, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LibTreatWarningsAsErrors, value); }
        }

        /// <summary>
        /// LibVerboseOutput property
        /// </summary>
        /// <value>Enable verbose output by the librarian</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LibVerboseOutput
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LibVerboseOutput, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LibVerboseOutput, value); }
        }

        /// <summary>
        /// LinkerAdditionalOptions property
        /// </summary>
        /// <value>Additional switches to be passed to the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LinkerAdditionalOptions
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LinkerAdditionalOptions); }
            set { this.SetPropertyString(WixProjectFileConstants.LinkerAdditionalOptions, value); }
        }

        /// <summary>
        /// Additional switches for the linker
        /// </summary>
        /// <value>Additional switches as set by the user on the property page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LinkerAdditionalOptionsLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LinkerAdditionalOptions, false); }
        }

        /// <summary>
        /// LinkerPedantic property
        /// </summary>
        /// <value>Linker shows pedantic messages</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LinkerPedantic
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LinkerPedantic, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LinkerPedantic, value); }
        }

        /// <summary>
        /// LinkerSuppressIntermediateFileVersionMatching property
        /// </summary>
        /// <value>Suppress intermediate file version matching checking</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LinkerSuppressIntermediateFileVersionMatching
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LinkerSuppressIntermediateFileVersionMatching, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LinkerSuppressIntermediateFileVersionMatching, value); }
        }

        /// <summary>
        /// LinkerSuppressSchemaValidation property
        /// </summary>
        /// <value>Suppress schema validation (performance boost)</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LinkerSuppressSchemaValidation
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LinkerSuppressSchemaValidation, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LinkerSuppressSchemaValidation, value); }
        }

        /// <summary>
        /// LinkerSuppressSpecificWarnings property
        /// </summary>
        /// <value>Suppress specific warnings from the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string LinkerSuppressSpecificWarnings
        {
            get { return this.GetPropertyString(WixProjectFileConstants.LinkerSuppressSpecificWarnings); }
            set { this.SetPropertyString(WixProjectFileConstants.LinkerSuppressSpecificWarnings, value); }
        }

        /// <summary>
        /// LinkerTreatWarningsAsErrors property
        /// </summary>
        /// <value>Should the linker treat warnings as errors</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LinkerTreatWarningsAsErrors
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LinkerTreatWarningsAsErrors, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LinkerTreatWarningsAsErrors, value); }
        }

        /// <summary>
        /// LinkerVerboseOutput property
        /// </summary>
        /// <value>Enable verbose output from the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool LinkerVerboseOutput
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.LinkerVerboseOutput, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.LinkerVerboseOutput, value); }
        }

        /// <summary>
        /// OnlyValidateDocuments
        /// </summary>
        /// <value>OnlyValidateDocuments from the build task</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool OnlyValidateDocuments
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.OnlyValidateDocuments, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.OnlyValidateDocuments, value); }
        }

        /// <summary>
        /// OutputFileName property
        /// </summary>
        /// <value>OutputFileName</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string OutputFileName
        {
            get
            {
                string outputFileName = this.OutputName;
                if (!String.IsNullOrEmpty(outputFileName))
                {
                    string outputType = this.OutputType;
                    if (String.Equals(outputType, WixOutputType.Package.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        outputFileName = String.Concat(outputFileName, ".msi");
                    }
                    else if (String.Equals(outputType, WixOutputType.Module.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        outputFileName = String.Concat(outputFileName, ".msm");
                    }
                    else if (String.Equals(outputType, WixOutputType.Library.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        outputFileName = String.Concat(outputFileName, ".wixlib");
                    }
                    else if (String.Equals(outputType, WixOutputType.Bundle.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        outputFileName = String.Concat(outputFileName, ".exe");
                    }
                }

                return outputFileName;
            }

            set
            {
                string outputName = Path.GetFileNameWithoutExtension(value);

                string outputExtension = Path.GetExtension(value);
                if (String.Equals(outputExtension, ".msi", StringComparison.OrdinalIgnoreCase))
                {
                    this.OutputType = WixOutputType.Package.ToString();
                }
                else if (String.Equals(outputExtension, ".msm", StringComparison.OrdinalIgnoreCase))
                {
                    this.OutputType = WixOutputType.Module.ToString();
                }
                else if (String.Equals(outputExtension, ".wixlib", StringComparison.OrdinalIgnoreCase))
                {
                    this.OutputType = WixOutputType.Library.ToString();
                }
                else if (String.Equals(outputExtension, ".exe", StringComparison.OrdinalIgnoreCase))
                {
                    this.OutputType = WixOutputType.Bundle.ToString();
                }
                else
                {
                    outputName = value;
                }

                this.OutputName = outputName;
            }
        }

        /// <summary>
        /// OutputName property
        /// </summary>
        /// <value>OutputName</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string OutputName
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.OutputName);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.OutputName, value);
            }
        }

        /// <summary>
        /// OutputPath property
        /// </summary>
        /// <value>Output path for the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string OutputPath
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.OutputPath);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.OutputPath, value);
            }
        }

        /// <summary>
        /// Unexpanded OutputPath property
        /// </summary>
        /// <value>Output path as set by the user on the property page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string OutputPathLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.OutputPath, false); }
        }

        /// <summary>
        /// OutputType property
        /// </summary>
        /// <value>OutputType</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string OutputType
        {
            get
            {
                return this.ProjectNode.OutputType.ToString();
            }

            set
            {
                WixOutputType currentOutputType = this.ProjectNode.OutputType;
                WixOutputType newOutputType = (WixOutputType)Enum.Parse(typeof(WixOutputType), value, true);
                if (newOutputType != currentOutputType)
                {
                    this.SetPropertyString(WixProjectFileConstants.OutputType, newOutputType.ToString());
                    ((WixProjectNode)this.Node.ProjectMgr).OnOutputTypeChanged();
                }
            }
        }

        /// <summary>
        /// PdbOutputFils
        /// </summary>
        /// <value>Name of the output pdb file</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string PdbOutputFile
        {
            get { return this.GetPropertyString(WixProjectFileConstants.PdbOutputFile); }
            set { this.SetPropertyString(WixProjectFileConstants.PdbOutputFile, value); }
        }

        /// <summary>
        /// Pedantic property
        /// </summary>
        /// <value>Show pedantic messages for the compiler</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool Pedantic
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.Pedantic, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.Pedantic, value); }
        }

        /// <summary>
        /// PostBuildEvent property
        /// </summary>
        /// <value>Command line executed after the build is finished</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string PostBuildEvent
        {
            get { return this.GetPropertyString(WixProjectFileConstants.PostBuildEvent); }
            set { this.SetPropertyString(WixProjectFileConstants.PostBuildEvent, value); }
        }

        /// <summary>
        /// PostBuildEvent literal property.
        /// </summary>
        /// <value>Command line executed after the build is finished as entered by the user.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string PostBuildEventLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.PostBuildEvent, false); }
        }

        /// <summary>
        /// PreBuildEvent property
        /// </summary>
        /// <value>Command line executed before build</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string PreBuildEvent
        {
            get { return this.GetPropertyString(WixProjectFileConstants.PreBuildEvent); }
            set { this.SetPropertyString(WixProjectFileConstants.PreBuildEvent, value); }
        }

        /// <summary>
        /// PreBuildEvent literal property.
        /// </summary>
        /// <value>Command line executed before build as specified by the user.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string PreBuildEventLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.PreBuildEvent, false); }
        }

        /// <summary>
        /// ReuseCabinetCache property
        /// </summary>
        /// <value>Reuse cabinets from the cabinet cache</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool ReuseCabinetCache
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.ReuseCabinetCache, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.ReuseCabinetCache, value); }
        }

        /// <summary>
        /// RunPostBuildEvent property
        /// </summary>
        /// <value>Determines when the PostBuildEvent is run</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string RunPostBuildEvent
        {
            get { return this.GetPropertyString(WixProjectFileConstants.RunPostBuildEvent); }
            set { this.SetPropertyString(WixProjectFileConstants.RunPostBuildEvent, value); }
        }

        /// <summary>
        /// SetMsiAssemblyNameFileVersion property
        /// </summary>
        /// <value>Add a fileVersion entry to the MsiAssemblyName table</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SetMsiAssemblyNameFileVersion
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SetMsiAssemblyNameFileVersion, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SetMsiAssemblyNameFileVersion, value); }
        }

        /// <summary>
        /// ShowSourceTrace property
        /// </summary>
        /// <value>Show source trace for errors, etc.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool ShowSourceTrace
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.ShowSourceTrace, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.ShowSourceTrace, value); }
        }

        /// <summary>
        /// SuppressAclReset property
        /// </summary>
        /// <value>Suppress restting ACLs</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressAclReset
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressAclReset, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressAclReset, value); }
        }

        /// <summary>
        /// SuppressAllWarnings property
        /// </summary>
        /// <value>Suppress all compiler warnings</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressAllWarnings
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressAllWarnings, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressAllWarnings, value); }
        }

        /// <summary>
        /// SuppressAssemblies property
        /// </summary>
        /// <value>Suppress assemblies</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressAssemblies
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressAssemblies, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressAssemblies, value); }
        }

        /// <summary>
        /// SuppressDefaultAdminSequenceActions property
        /// </summary>
        /// <value>Suppress default admin sequence actions</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressDefaultAdminSequenceActions
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressDefaultAdminSequenceActions, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressDefaultAdminSequenceActions, value); }
        }

        /// <summary>
        /// SuppressDefaultAdvSequenceActions property
        /// </summary>
        /// <value>Suppress default adv sequence actions</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressDefaultAdvSequenceActions
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressDefaultAdvSequenceActions, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressDefaultAdvSequenceActions, value); }
        }

        /// <summary>
        /// SuppressDefaultUISequenceActions property
        /// </summary>
        /// <value>Suppress default UI sequence actions</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressDefaultUISequenceActions
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressDefaultUISequenceActions, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressDefaultUISequenceActions, value); }
        }

        /// <summary>
        /// SuppressFileHashAndInfo property
        /// </summary>
        /// <value>Suppress file info (do not get hash, version, language, etc.)</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressFileHashAndInfo
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressFileHashAndInfo, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressFileHashAndInfo, value); }
        }

        /// <summary>
        /// SuppressFiles property
        /// </summary>
        /// <value>Suppress files</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressFiles
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressFiles, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressFiles, value); }
        }

        /// <summary>
        /// SuppressIces property
        /// </summary>
        /// <value>Suppress internal consistency evaluators</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string SuppressIces
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.SuppressIces);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.SuppressIces, value);
            }
        }

        /// <summary>
        /// SuppressLayout property
        /// </summary>
        /// <value>Suppress layout</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressLayout
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressLayout, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressLayout, value); }
        }

        /// <summary>
        /// SuppressMsiAssemblyTableProcessing property
        /// </summary>
        /// <value>Suppress processing the data in MsiAssembly table</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressMsiAssemblyTableProcessing
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressMsiAssemblyTableProcessing, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressMsiAssemblyTableProcessing, value); }
        }

        /// <summary>
        /// SuppressOutputtingPdb property
        /// </summary>
        /// <value>Controlls if a wixpdb file will be generated</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressPdbOutput
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressPdbOutput, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressPdbOutput, value); }
        }

        /// <summary>
        /// SuppressSchemaValidation property
        /// </summary>
        /// <value>Supress schema validation when compiling</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressSchemaValidation
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressSchemaValidation, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressSchemaValidation, value); }
        }

        /// <summary>
        /// SuppressSpecificWarnings property
        /// </summary>
        /// <value>Compiler warnings to suppress</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string SuppressSpecificWarnings
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.SuppressSpecificWarnings);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.SuppressSpecificWarnings, value);
            }
        }

        /// <summary>
        /// SuppressTagSectionIdAttributeOnTuples
        /// </summary>
        /// <value>Suppresses tag section ID attribute on tuples</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressTagSectionIdAttributeOnTuples
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressTagSectionIdAttributeOnTuples, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressTagSectionIdAttributeOnTuples, value); }
        }

        /// <summary>
        /// SuppressValidation property
        /// </summary>
        /// <value>Suppress MSI/MSM validation</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool SuppressValidation
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.SuppressValidation, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.SuppressValidation, value); }
        }

        /// <summary>
        /// TreatWarningsAsErrors property
        /// </summary>
        /// <value>Should the compiler treat the warnings as errors</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool TreatWarningsAsErrors
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.TreatWarningsAsErrors, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.TreatWarningsAsErrors, value); }
        }

        /// <summary>
        /// VerboseOutput property
        /// </summary>
        /// <value>Show verbose output when compiling</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public bool VerboseOutput
        {
            get { return this.GetPropertyBoolean(WixProjectFileConstants.VerboseOutput, false); }
            set { this.SetPropertyBoolean(WixProjectFileConstants.VerboseOutput, value); }
        }

        /// <summary>
        /// WixVariables property
        /// </summary>
        /// <value>WiX variables for the linker</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string WixVariables
        {
            get
            {
                return this.GetPropertyString(WixProjectFileConstants.WixVariables);
            }

            set
            {
                this.SetPropertyString(WixProjectFileConstants.WixVariables, value);
            }
        }

        /// <summary>
        /// WixVariables property
        /// </summary>
        /// <value>WiX variables for the linker as set by the user in the properties page.</value>
        [AutomationBrowsable(true)]
        [Browsable(false)]
        public string WixVariablesLiteral
        {
            get { return this.GetPropertyString(WixProjectFileConstants.WixVariables, false); }
        }

        private WixProjectNode ProjectNode
        {
            get { return (WixProjectNode)this.Node; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Creates a custom property descriptor for the node properties, which affects the behavior
        /// of the property grid.
        /// </summary>
        /// <param name="propertyDescriptor">The <see cref="PropertyDescriptor"/> to wrap.</param>
        /// <returns>A custom <see cref="PropertyDescriptor"/> object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called p and in the 2008 SDK it's propertyDescriptor")]
        public override DesignPropertyDescriptor CreateDesignPropertyDescriptor(PropertyDescriptor propertyDescriptor)
        {
            return new WixDesignPropertyDescriptor(propertyDescriptor);
        }

        /// <summary>
        /// Return a boolean property
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="defaultValue">Value to be returned if property cannot be parsed as bool</param>
        /// <returns>Value of a property as a boolean</returns>
        private bool GetPropertyBoolean(string propertyName, bool defaultValue)
        {
            bool propertyValue;
            if (Boolean.TryParse(this.GetPropertyString(propertyName), out propertyValue))
            {
                return propertyValue;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Return an integer property
        /// </summary>
        /// <param name="propertyName">Name of the project property</param>
        /// <param name="defaultValue">Value to be returned if property cannot be parsed as a valid integer</param>
        /// <returns>Value of the property as an integer</returns>
        private int GetPropertyInt32(string propertyName, int defaultValue)
        {
            int propertyValue;
            if (Int32.TryParse(this.GetPropertyString(propertyName), out propertyValue))
            {
                return propertyValue;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Return a string property
        /// </summary>
        /// <param name="propertyName">Name of the project property</param>
        /// <returns>Value of the property as a non-null string, with all variables evaluated.</returns>
        private string GetPropertyString(string propertyName)
        {
            return this.GetPropertyString(propertyName, true);
        }

        /// <summary>
        /// Return a string property
        /// </summary>
        /// <param name="propertyName">Name of the project property</param>
        /// <param name="finalValue">Whether to evaluate variables in the value.</param>
        /// <returns>Value of the property as a non-null string</returns>
        private string GetPropertyString(string propertyName, bool finalValue)
        {
            ProjectProperty property = new ProjectProperty(this.ProjectNode, propertyName);
            string propertyValue = property.GetValue(finalValue);

            if (propertyValue == null)
            {
                return String.Empty;
            }

            return propertyValue;
        }

        /// <summary>
        /// Set a project property
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        private void SetPropertyBoolean(string propertyName, bool value)
        {
            this.SetPropertyString(propertyName, value.ToString());
        }

        /// <summary>
        /// Set an integer project property
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        private void SetPropertyInt32(string propertyName, int value)
        {
            this.SetPropertyString(propertyName, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Set a string project property
        /// </summary>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        private void SetPropertyString(string propertyName, string value)
        {
            PropertyValidator.ValidateProperty(propertyName, value);

            if (!this.Node.ProjectMgr.QueryEditProjectFile(true))
            {
                throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
            }

            ProjectProperty property = new ProjectProperty(this.ProjectNode, propertyName);
            property.SetValue(value);
        }
    }
}
