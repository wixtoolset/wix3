//-------------------------------------------------------------------------------------------------
// <copyright file="WixProjectFileConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixProjectFileConstants class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;

    /// <summary>
    /// Contains constants for MSBuild .wixproj and .user files.
    /// </summary>
    //// Keep these in alphabetical order for easy referencing.
    internal static class WixProjectFileConstants
    {
        public const int UnspecifiedValue = -1;

        public const string AdditionalCub = "AdditionalCub";
        public const string AllowIdenticalRows = "AllowIdenticalRows";
        public const string AllowUnresolvedReferences = "AllowUnresolvedReferences";
        public const string BackwardsCompatibleGuidGeneration = "BackwardsCompatibleGuidGeneration";
        public const string BaseInputPaths = "BaseInputPaths";
        public const string CabinetCachePath = "CabinetCachePath";
        public const string CabinetCreationThreadCount = "CabinetCreationThreadCount";
        public const string CompilerAdditionalOptions = "CompilerAdditionalOptions";
        public const string Cultures = "Cultures";
        public const string DefineDebugConstant = "DefineDebugConstant";
        public const string DefineConstants = "DefineConstants";
        public const string DevEnvDir = "DevEnvDir";
        public const string DoNotHarvest = "DoNotHarvest";
        public const string DropUnrealTables = "DropUnrealTables";
        public const string IncludeSearchPaths = "IncludeSearchPaths";
        public const string InstallerPlatform = "InstallerPlatform";
        public const string IntermediateOutputPath = "IntermediateOutputPath";
        public const string LeaveTemporaryFiles = "LeaveTemporaryFiles";
        public const string LibAdditionalOptions = "LibAdditionalOptions";
        public const string LibBindFiles = "LibBindFiles";
        public const string LibSuppressIntermediateFileVersionMatching = "LibSuppressIntermediateFileVersionMatching";
        public const string LibSuppressSchemaValidation = "LibSuppressSchemaValidation";
        public const string LibSuppressSpecificWarnings = "LibSuppressSpecificWarnings";
        public const string LibTreatWarningsAsErrors = "LibTreatWarningsAsErrors";
        public const string LibVerboseOutput = "LibVerboseOutput";
        public const string LinkerAdditionalOptions = "LinkerAdditionalOptions";
        public const string LinkerBaseInputPaths = "LinkerBaseInputPaths";
        public const string LinkerBindFiles = "LinkerBindFiles";
        public const string LinkerPedantic = "LinkerPedantic";
        public const string LinkerSuppressIntermediateFileVersionMatching = "LinkerSuppressIntermediateFileVersionMatching";
        public const string LinkerSuppressSchemaValidation = "LinkerSuppressSchemaValidation";
        public const string LinkerSuppressSpecificWarnings = "LinkerSuppressSpecificWarnings";
        public const string LinkerTreatWarningsAsErrors = "LinkerTreatWarningsAsErrors";
        public const string LinkerVerboseOutput = "LinkerVerboseOutput";
        public const string OnlyValidateDocuments = "OnlyValidateDocuments";
        public const string OutputAsXml = "OutputAsXml";
        public const string OutputName = "OutputName";
        public const string OutputPath = "OutputPath";
        public const string OutputType = "OutputType";
        public const string PdbOutputFile = "PdbOutputFile";
        public const string Pedantic = "Pedantic";
        public const string PostBuildEvent = "PostBuildEvent";
        public const string ProjectFiles = "ProjectFiles";
        public const string ProjectView = "ProjectView";
        public const string PropertyGroup = "PropertyGroup";
        public const string PreBuildEvent = "PreBuildEvent";
        public const string ReferencePaths = "ReferencePaths";
        public const string ReuseCabinetCache = "ReuseCabinetCache";
        public const string RunPostBuildEvent = "RunPostBuildEvent";
        public const string SccProjectName = "SccProjectName";
        public const string SccLocalPath = "SccLocalPath";
        public const string SccAuxPath = "SccAuxPath";
        public const string SccProvider = "SccProvider";
        public const string SetMsiAssemblyNameFileVersion = "SetMsiAssemblyNameFileVersion";
        public const string ShowAllFiles = "ShowAllFiles";
        public const string ShowSourceTrace = "ShowSourceTrace";
        public const string SolutionDir = "SolutionDir";
        public const string SolutionExt = "SolutionExt";
        public const string SolutionName = "SolutionName";
        public const string SolutionFileName = "SolutionFileName";
        public const string SolutionPath = "SolutionPath";
        public const string SuppressAclReset = "SuppressAclReset";
        public const string SuppressAllWarnings = "SuppressAllWarnings";
        public const string SuppressAssemblies = "SuppressAssemblies";
        public const string SuppressDefaultAdminSequenceActions = "SuppressDefaultAdminSequenceActions";
        public const string SuppressDefaultAdvSequenceActions = "SuppressDefaultAdvSequenceActions";
        public const string SuppressDefaultUISequenceActions = "SuppressDefaultUISequenceActions";
        public const string SuppressFileHashAndInfo = "SuppressFileHashAndInfo";
        public const string SuppressFiles = "SuppressFiles";
        public const string SuppressIces = "SuppressIces";
        public const string SuppressLayout = "SuppressLayout";
        public const string SuppressMsiAssemblyTableProcessing = "SuppressMsiAssemblyTableProcessing";
        public const string SuppressPdbOutput = "SuppressPdbOutput";
        public const string SuppressSchemaValidation = "SuppressSchemaValidation";
        public const string SuppressSpecificWarnings = "SuppressSpecificWarnings";
        public const string SuppressTagSectionIdAttributeOnTuples = "SuppressTagSectionIdAttributeOnTuples";
        public const string SuppressValidation = "SuppressValidation";
        public const string TreatWarningsAsErrors = "TreatWarningsAsErrors";
        public const string VerboseOutput = "VerboseOutput";
        public const string WarningLevel = "WarningLevel";
        public const string WixExtDir = "WixExtDir";
        public const string WixExtension = "WixExtension";
        public const string WixLibrary = "WixLibrary";
        public const string WixTargetsPath = "WixTargetsPath";
        public const string WixToolPath = "WixToolPath";
        public const string WixVariables = "WixVariables";

        /// <summary>
        /// Target names for the Wix.targets MSBuild file.
        /// </summary>
        public static class MsBuildTarget
        {
            public const string GetTargetPath = "GetTargetPath";
            public const string ResolveWixLibraryReferences = "ResolveWixLibraryReferences";
        }
    }
}