// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.VisualStudio.Package
{
	using System;
	using System.Reflection;
	using System.Globalization;
	using System.Resources;
	using System.Text;
	using System.Threading;
	using System.ComponentModel;
	using System.Security.Permissions;

	[AttributeUsage(AttributeTargets.All)]
	internal sealed class SRDescriptionAttribute : DescriptionAttribute
	{

		private bool replaced = false;

		public SRDescriptionAttribute(string description)
			: base(description)
		{
		}

		public override string Description
		{
			get
			{
				if (!replaced)
				{
					replaced = true;
					DescriptionValue = SR.GetString(base.Description, CultureInfo.CurrentUICulture);
				}
				return base.Description;
			}
		}
	}

	[AttributeUsage(AttributeTargets.All)]
	internal sealed class SRCategoryAttribute : CategoryAttribute
	{

		public SRCategoryAttribute(string category)
			: base(category)
		{
		}

		protected override string GetLocalizedString(string value)
		{
			return SR.GetString(value, CultureInfo.CurrentUICulture);
		}
	}
	internal sealed class SR
	{
		internal const string AddReferenceDialogTitle = "AddReferenceDialogTitle";
		internal const string AddToNullProjectError = "AddToNullProjectError";
		internal const string Advanced = "Advanced";
		internal const string AssemblyReferenceAlreadyExists = "AssemblyReferenceAlreadyExists";
		internal const string AttributeLoad = "AttributeLoad";
		internal const string BuildAction = "BuildAction";
		internal const string BuildActionDescription = "BuildActionDescription";
		internal const string BuildCaption = "BuildCaption";
		internal const string BuildVerbosity = "BuildVerbosity";
		internal const string BuildVerbosityDescription = "BuildVerbosityDescription";
		internal const string BuildEventError = "BuildEventError";
		internal const string CancelQueryEdit = "CancelQueryEdit";
		internal const string CannotAddFileThatIsOpenInEditor = "CannotAddFileThatIsOpenInEditor";
		internal const string CannotAddItemToProjectWithWildcards = "CannotAddItemToProjectWithWildcards";
		internal const string CannotBuildWhenBuildInProgress = "CannotBuildWhenBuildInProgress";
		internal const string CanNotSaveFileNotOpeneInEditor = "CanNotSaveFileNotOpeneInEditor";
		internal const string CannotStartLibraries = "CannotStartLibraries";
		internal const string cli1 = "cli1";
		internal const string Compile = "Compile";
		internal const string ConfirmExtensionChange = "ConfirmExtensionChange";
		internal const string Content = "Content";
		internal const string CopyNOfFile = "CopyNOfFile";
		internal const string CopyOfFile = "CopyOfFile";
		internal const string CopyAlways = "CopyAlways";
		internal const string CopyIfNewer = "CopyIfNewer";
		internal const string CopyToLocal = "CopyToLocal";
		internal const string CopyToLocalDescription = "CopyToLocalDescription";
		internal const string CopyToOutputDirectory = "CopyToOutputDirectory";
		internal const string CopyToOutputDirectoryDescription = "CopyToOutputDirectoryDescription";
		internal const string CustomTool = "CustomTool";
		internal const string CustomToolDescription = "CustomToolDescription";
		internal const string CustomToolNamespace = "CustomToolNamespace";
		internal const string CustomToolNamespaceDescription = "CustomToolNamespaceDescription";
		internal const string DestinationFolderAlreadyExists = "DestinationFolderAlreadyExists";
		internal const string DestinationPathEqualsSourcePath = "DestinationPathEqualsSourcePath";
		internal const string DestinationPathSubfolderOfSourcePath = "DestinationPathSubfolderOfSourcePath";
		internal const string DetailsImport = "DetailsImport";
		internal const string DetailsUserImport = "DetailsUserImport";
		internal const string DetailsItem = "DetailsItem";
		internal const string DetailsItemLocation = "DetailsItemLocation";
		internal const string DetailsProperty = "DetailsProperty";
		internal const string DetailsTarget = "DetailsTarget";
		internal const string DetailsUsingTask = "DetailsUsingTask";
		internal const string Detailed = "Detailed";
		internal const string Diagnostic = "Diagnostic";
		internal const string DirectoryExistError = "DirectoryExistError";
		internal const string DoNotCopy = "DoNotCopy";
		internal const string EditorViewError = "EditorViewError";
		internal const string EmbeddedResource = "EmbeddedResource";
		internal const string Error = "Error";
		internal const string ErrorInvalidFileName = "ErrorInvalidFileName";
		internal const string ErrorInvalidProjectName = "ErrorInvalidProjectName";
		internal const string ErrorReferenceCouldNotBeAdded = "ErrorReferenceCouldNotBeAdded";
		internal const string ErrorMsBuildRegistration = "ErrorMsBuildRegistration";
		internal const string ErrorSaving = "ErrorSaving";
		internal const string Exe = "Exe";
		internal const string ExpectedObjectOfType = "ExpectedObjectOfType";
		internal const string FailedToGetService = "FailedToGetService";
		internal const string FailedToRetrieveProperties = "FailedToRetrieveProperties";
		internal const string FileNameCannotContainALeadingPeriod = "FileNameCannotContainALeadingPeriod";
		internal const string FileCannotBeRenamedToAnExistingFile = "FileCannotBeRenamedToAnExistingFile";
		internal const string FileAlreadyExistsAndCannotBeRenamed = "FileAlreadyExistsAndCannotBeRenamed";
		internal const string FileAlreadyExists = "FileAlreadyExists";
		internal const string FileAlreadyExistsCaption = "FileAlreadyExistsCaption";
		internal const string FileAlreadyInProject = "FileAlreadyInProject";
		internal const string FileAlreadyInProjectCaption = "FileAlreadyInProjectCaption";
		internal const string FileCopyError = "FileCopyError";
		internal const string FileName = "FileName";
		internal const string FileNameDescription = "FileNameDescription";
		internal const string FileOrFolderAlreadyExists = "FileOrFolderAlreadyExists";
		internal const string FileOrFolderCannotBeFound = "FileOrFolderCannotBeFound";
		internal const string FileProperties = "FileProperties";
		internal const string FolderName = "FolderName";
		internal const string FolderNameDescription = "FolderNameDescription";
		internal const string FolderProperties = "FolderProperties";
		internal const string FullPath = "FullPath";
		internal const string FullPathDescription = "FullPathDescription";
		internal const string ItemDoesNotExistInProjectDirectory = "ItemDoesNotExistInProjectDirectory";
		internal const string InvalidAutomationObject = "InvalidAutomationObject";
		internal const string InvalidLoggerType = "InvalidLoggerType";
		internal const string InvalidParameter = "InvalidParameter";
		internal const string Library = "Library";
		internal const string LinkedItemsAreNotSupported = "LinkedItemsAreNotSupported";
		internal const string LinkAlreadyExistsInProject = "LinkAlreadyExistsInProject";
		internal const string LinkedFileAlreadyExists = "LinkedFileAlreadyExists";
		internal const string Minimal = "Minimal";
		internal const string Misc = "Misc";
		internal const string None = "None";
		internal const string NoWildcardsInProject = "NoWildcardsInProject";
		internal const string NoZeroImpactProjects = "NoZeroImpactProjects";
		internal const string Normal = "Normal";
		internal const string NestedProjectFailedToReload = "NestedProjectFailedToReload";
		internal const string OutputPath = "OutputPath";
		internal const string OutputPathDescription = "OutputPathDescription";
		internal const string PasteFailed = "PasteFailed";
		internal const string ParameterMustBeAValidGuid = "ParameterMustBeAValidGuid";
		internal const string ParameterMustBeAValidItemId = "ParameterMustBeAValidItemId";
		internal const string ParameterCannotBeNullOrEmpty = "ParameterCannotBeNullOrEmpty";
		internal const string PathTooLong = "PathTooLong";
		internal const string ProjectContainsCircularReferences = "ProjectContainsCircularReferences";
		internal const string Program = "Program";
		internal const string Project = "Project";
		internal const string ProjectFile = "ProjectFile";
		internal const string ProjectFileDescription = "ProjectFileDescription";
		internal const string ProjectFolder = "ProjectFolder";
		internal const string ProjectFolderDescription = "ProjectFolderDescription";
		internal const string ProjectLocationNotTrusted = "ProjectLocationNotTrusted";
		internal const string ProjectProperties = "ProjectProperties";
		internal const string Quiet = "Quiet";
		internal const string QueryReloadNestedProject = "QueryReloadNestedProject";
		internal const string ReferenceAlreadyExists = "ReferenceAlreadyExists";
		internal const string ReferencesNodeName = "ReferencesNodeName";
		internal const string ReferenceProperties = "ReferenceProperties";
		internal const string RefName = "RefName";
		internal const string RefNameDescription = "RefNameDescription";
		internal const string RenameFolder = "RenameFolder";
		internal const string RTL = "RTL";
		internal const string SaveCaption = "SaveCaption";
		internal const string SaveModifiedDocuments = "SaveModifiedDocuments";
		internal const string SaveOfProjectFileOutsideCurrentDirectory = "SaveOfProjectFileOutsideCurrentDirectory";
		internal const string SpecificVersion = "SpecificVersion";
		internal const string SpecificVersionDescription = "SpecificVersionDescription";
		internal const string StandardEditorViewError = "StandardEditorViewError";
		internal const string UpgradeCannotOpenProjectFileForEdit = "UpgradeCannotOpenProjectFileForEdit";
		internal const string UpgradeNoNeedToUpgradeAfterCheckout = "UpgradeNoNeedToUpgradeAfterCheckout";
		internal const string UpgradeSuccessful = "UpgradeSuccessful";
		internal const string URL = "URL";
		internal const string UseOfDeletedItemError = "UseOfDeletedItemError";
		internal const string v1 = "v1";
		internal const string v11 = "v11";
		internal const string v2 = "v2";
		internal const string Warning = "Warning";
		internal const string WinExe = "WinExe";

		static SR loader = null;
		ResourceManager resources;

		private static Object s_InternalSyncObject;
		private static Object InternalSyncObject
		{
			get
			{
				if (s_InternalSyncObject == null)
				{
					Object o = new Object();
					Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
				}
				return s_InternalSyncObject;
			}
		}

		internal SR()
		{
			resources = new System.Resources.ResourceManager("Microsoft.VisualStudio.Package.Project", this.GetType().Assembly);
		}

		private static SR GetLoader()
		{
			if (loader == null)
			{
				lock (InternalSyncObject)
				{
					if (loader == null)
					{
						loader = new SR();
					}
				}
			}

			return loader;
		}

		private static CultureInfo Culture
		{
			get { return null/*use ResourceManager default, CultureInfo.CurrentUICulture*/; }
		}

		public static ResourceManager Resources
		{
			get
			{
				return GetLoader().resources;
			}
		}

		public static string GetString(string name, params object[] args)
		{
			SR sys = GetLoader();
			if (sys == null)
				return null;
			string res = sys.resources.GetString(name, SR.Culture);

			if (args != null && args.Length > 0)
			{
				return String.Format(CultureInfo.CurrentCulture, res, args);
			}
			else
			{
				return res;
			}
		}

		public static string GetString(string name)
		{
			SR sys = GetLoader();
			if (sys == null)
				return null;
			return sys.resources.GetString(name, SR.Culture);
		}

		public static string GetString(string name, CultureInfo culture)
		{
			SR sys = GetLoader();
			if (sys == null)
				return null;
			return sys.resources.GetString(name, culture);
		}

		public static string GetStringWithCR(string name)
		{
			var s = GetString(name);
			return s.Replace(@"\n", Environment.NewLine);
		}

		public static object GetObject(string name)
		{
			SR sys = GetLoader();
			if (sys == null)
				return null;
			return sys.resources.GetObject(name, SR.Culture);
		}
	}
}
