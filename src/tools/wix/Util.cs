//-------------------------------------------------------------------------------------------------
// <copyright file="Util.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Common Wix utility methods and types.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Common Wix utility methods and types.
    /// </summary>
    public sealed class Util
    {
        private static readonly object lockObject = new object();

        private static Hashtable standardActionsHash;
        private static Hashtable standardDirectories;
        private static Hashtable standardProperties;

        private static bool runningInMsBuild = false;

        /// <summary>
        /// Set by WixToolTasks to indicate WIX is running inside MSBuild
        /// </summary>
        public static bool RunningInMsBuild
        {
            get { return runningInMsBuild; }
            set { runningInMsBuild = value; }
        }

        /// <summary>
        /// Gets (and loads if not yet loaded) the list of standard MSI directories.
        /// </summary>
        /// <value>The list of standard MSI directories.</value>
        public static Hashtable StandardDirectories
        {
            get
            {
                if (null == standardDirectories)
                {
                    LoadStandardDirectories();
                }
                return standardDirectories;
            }
        }

        /// <summary>
        /// Find out if an action is a standard action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>true if the action is standard, false otherwise.</returns>
        public static bool IsStandardAction(string actionName)
        {
            lock (lockObject)
            {
                if (null == standardActionsHash)
                {
                    standardActionsHash = new Hashtable();
                    standardActionsHash.Add("AllocateRegistrySpace", null);
                    standardActionsHash.Add("AppSearch", null);
                    standardActionsHash.Add("BindImage", null);
                    standardActionsHash.Add("CCPSearch", null);
                    standardActionsHash.Add("CostFinalize", null);
                    standardActionsHash.Add("CostInitialize", null);
                    standardActionsHash.Add("CreateFolders", null);
                    standardActionsHash.Add("CreateShortcuts", null);
                    standardActionsHash.Add("DeleteServices", null);
                    standardActionsHash.Add("DisableRollback", null);
                    standardActionsHash.Add("DuplicateFiles", null);
                    standardActionsHash.Add("ExecuteAction", null);
                    standardActionsHash.Add("FileCost", null);
                    standardActionsHash.Add("FindRelatedProducts", null);
                    standardActionsHash.Add("ForceReboot", null);
                    standardActionsHash.Add("InstallAdminPackage", null);
                    standardActionsHash.Add("InstallExecute", null);
                    standardActionsHash.Add("InstallExecuteAgain", null);
                    standardActionsHash.Add("InstallFiles", null);
                    standardActionsHash.Add("InstallFinalize", null);
                    standardActionsHash.Add("InstallInitialize", null);
                    standardActionsHash.Add("InstallODBC", null);
                    standardActionsHash.Add("InstallServices", null);
                    standardActionsHash.Add("InstallSFPCatalogFile", null);
                    standardActionsHash.Add("InstallValidate", null);
                    standardActionsHash.Add("IsolateComponents", null);
                    standardActionsHash.Add("LaunchConditions", null);
                    standardActionsHash.Add("MigrateFeatureStates", null);
                    standardActionsHash.Add("MoveFiles", null);
                    standardActionsHash.Add("MsiConfigureServices", null);
                    standardActionsHash.Add("MsiPublishAssemblies", null);
                    standardActionsHash.Add("MsiUnpublishAssemblies", null);
                    standardActionsHash.Add("PatchFiles", null);
                    standardActionsHash.Add("ProcessComponents", null);
                    standardActionsHash.Add("PublishComponents", null);
                    standardActionsHash.Add("PublishFeatures", null);
                    standardActionsHash.Add("PublishProduct", null);
                    standardActionsHash.Add("RegisterClassInfo", null);
                    standardActionsHash.Add("RegisterComPlus", null);
                    standardActionsHash.Add("RegisterExtensionInfo", null);
                    standardActionsHash.Add("RegisterFonts", null);
                    standardActionsHash.Add("RegisterMIMEInfo", null);
                    standardActionsHash.Add("RegisterProduct", null);
                    standardActionsHash.Add("RegisterProgIdInfo", null);
                    standardActionsHash.Add("RegisterTypeLibraries", null);
                    standardActionsHash.Add("RegisterUser", null);
                    standardActionsHash.Add("RemoveDuplicateFiles", null);
                    standardActionsHash.Add("RemoveEnvironmentStrings", null);
                    standardActionsHash.Add("RemoveExistingProducts", null);
                    standardActionsHash.Add("RemoveFiles", null);
                    standardActionsHash.Add("RemoveFolders", null);
                    standardActionsHash.Add("RemoveIniValues", null);
                    standardActionsHash.Add("RemoveODBC", null);
                    standardActionsHash.Add("RemoveRegistryValues", null);
                    standardActionsHash.Add("RemoveShortcuts", null);
                    standardActionsHash.Add("ResolveSource", null);
                    standardActionsHash.Add("RMCCPSearch", null);
                    standardActionsHash.Add("ScheduleReboot", null);
                    standardActionsHash.Add("SelfRegModules", null);
                    standardActionsHash.Add("SelfUnregModules", null);
                    standardActionsHash.Add("SetODBCFolders", null);
                    standardActionsHash.Add("StartServices", null);
                    standardActionsHash.Add("StopServices", null);
                    standardActionsHash.Add("UnpublishComponents", null);
                    standardActionsHash.Add("UnpublishFeatures", null);
                    standardActionsHash.Add("UnregisterClassInfo", null);
                    standardActionsHash.Add("UnregisterComPlus", null);
                    standardActionsHash.Add("UnregisterExtensionInfo", null);
                    standardActionsHash.Add("UnregisterFonts", null);
                    standardActionsHash.Add("UnregisterMIMEInfo", null);
                    standardActionsHash.Add("UnregisterProgIdInfo", null);
                    standardActionsHash.Add("UnregisterTypeLibraries", null);
                    standardActionsHash.Add("ValidateProductID", null);
                    standardActionsHash.Add("WriteEnvironmentStrings", null);
                    standardActionsHash.Add("WriteIniValues", null);
                    standardActionsHash.Add("WriteRegistryValues", null);
                }
            }

            return standardActionsHash.ContainsKey(actionName);
        }

        /// <summary>
        /// Find out if a directory is a standard directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>true if the directory is standard, false otherwise.</returns>
        public static bool IsStandardDirectory(string directoryName)
        {
            if (null == standardDirectories)
            {
                LoadStandardDirectories();
            }

            return standardDirectories.ContainsKey(directoryName);
        }

        /// <summary>
        /// Find out if a property is a standard property.
        /// References: 
        /// Title:   Property Reference [Windows Installer]: 
        /// URL:     http://msdn.microsoft.com/library/en-us/msi/setup/property_reference.asp
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>true if a property is standard, false otherwise.</returns>
        public static bool IsStandardProperty(string propertyName)
        {
            lock (lockObject)
            {
                if (null == standardProperties)
                {
                    standardProperties = new Hashtable();
                    standardProperties.Add("~", null); // REG_MULTI_SZ/NULL marker
                    standardProperties.Add("ACTION", null);
                    standardProperties.Add("ADDDEFAULT", null);
                    standardProperties.Add("ADDLOCAL", null);
                    standardProperties.Add("ADDDSOURCE", null);
                    standardProperties.Add("AdminProperties", null);
                    standardProperties.Add("AdminUser", null);
                    standardProperties.Add("ADVERTISE", null);
                    standardProperties.Add("AFTERREBOOT", null);
                    standardProperties.Add("AllowProductCodeMismatches", null);
                    standardProperties.Add("AllowProductVersionMajorMismatches", null);
                    standardProperties.Add("ALLUSERS", null);
                    standardProperties.Add("Alpha", null);
                    standardProperties.Add("ApiPatchingSymbolFlags", null);
                    standardProperties.Add("ARPAUTHORIZEDCDFPREFIX", null);
                    standardProperties.Add("ARPCOMMENTS", null);
                    standardProperties.Add("ARPCONTACT", null);
                    standardProperties.Add("ARPHELPLINK", null);
                    standardProperties.Add("ARPHELPTELEPHONE", null);
                    standardProperties.Add("ARPINSTALLLOCATION", null);
                    standardProperties.Add("ARPNOMODIFY", null);
                    standardProperties.Add("ARPNOREMOVE", null);
                    standardProperties.Add("ARPNOREPAIR", null);
                    standardProperties.Add("ARPPRODUCTIONICON", null);
                    standardProperties.Add("ARPREADME", null);
                    standardProperties.Add("ARPSIZE", null);
                    standardProperties.Add("ARPSYSTEMCOMPONENT", null);
                    standardProperties.Add("ARPULRINFOABOUT", null);
                    standardProperties.Add("ARPURLUPDATEINFO", null);
                    standardProperties.Add("AVAILABLEFREEREG", null);
                    standardProperties.Add("BorderSize", null);
                    standardProperties.Add("BorderTop", null);
                    standardProperties.Add("CaptionHeight", null);
                    standardProperties.Add("CCP_DRIVE", null);
                    standardProperties.Add("ColorBits", null);
                    standardProperties.Add("COMPADDLOCAL", null);
                    standardProperties.Add("COMPADDSOURCE", null);
                    standardProperties.Add("COMPANYNAME", null);
                    standardProperties.Add("ComputerName", null);
                    standardProperties.Add("CostingComplete", null);
                    standardProperties.Add("Date", null);
                    standardProperties.Add("DefaultUIFont", null);
                    standardProperties.Add("DISABLEADVTSHORTCUTS", null);
                    standardProperties.Add("DISABLEMEDIA", null);
                    standardProperties.Add("DISABLEROLLBACK", null);
                    standardProperties.Add("DiskPrompt", null);
                    standardProperties.Add("DontRemoveTempFolderWhenFinished", null);
                    standardProperties.Add("EnableUserControl", null);
                    standardProperties.Add("EXECUTEACTION", null);
                    standardProperties.Add("EXECUTEMODE", null);
                    standardProperties.Add("FASTOEM", null);
                    standardProperties.Add("FILEADDDEFAULT", null);
                    standardProperties.Add("FILEADDLOCAL", null);
                    standardProperties.Add("FILEADDSOURCE", null);
                    standardProperties.Add("IncludeWholeFilesOnly", null);
                    standardProperties.Add("Installed", null);
                    standardProperties.Add("INSTALLLEVEL", null);
                    standardProperties.Add("Intel", null);
                    standardProperties.Add("Intel64", null);
                    standardProperties.Add("IsAdminPackage", null);
                    standardProperties.Add("LeftUnit", null);
                    standardProperties.Add("LIMITUI", null);
                    standardProperties.Add("ListOfPatchGUIDsToReplace", null);
                    standardProperties.Add("ListOfTargetProductCode", null);
                    standardProperties.Add("LOGACTION", null);
                    standardProperties.Add("LogonUser", null);
                    standardProperties.Add("Manufacturer", null);
                    standardProperties.Add("MEDIAPACKAGEPATH", null);
                    standardProperties.Add("MediaSourceDir", null);
                    standardProperties.Add("MinimumRequiredMsiVersion", null);
                    standardProperties.Add("MsiAMD64", null);
                    standardProperties.Add("MSIAPRSETTINGSIDENTIFIER", null);
                    standardProperties.Add("MSICHECKCRCS", null);
                    standardProperties.Add("MSIDISABLERMRESTART", null);
                    standardProperties.Add("MSIENFORCEUPGRADECOMPONENTRULES", null);
                    standardProperties.Add("MSIFASTINSTALL", null);
                    standardProperties.Add("MsiFileToUseToCreatePatchTables", null);
                    standardProperties.Add("MsiHiddenProperties", null);
                    standardProperties.Add("MSIINSTALLPERUSER", null);
                    standardProperties.Add("MSIINSTANCEGUID", null);
                    standardProperties.Add("MsiLogFileLocation", null);
                    standardProperties.Add("MsiLogging", null);
                    standardProperties.Add("MsiNetAssemblySupport", null);
                    standardProperties.Add("MSINEWINSTANCE", null);
                    standardProperties.Add("MSINODISABLEMEDIA", null);
                    standardProperties.Add("MsiNTProductType", null);
                    standardProperties.Add("MsiNTSuiteBackOffice", null);
                    standardProperties.Add("MsiNTSuiteDataCenter", null);
                    standardProperties.Add("MsiNTSuiteEnterprise", null);
                    standardProperties.Add("MsiNTSuiteSmallBusiness", null);
                    standardProperties.Add("MsiNTSuiteSmallBusinessRestricted", null);
                    standardProperties.Add("MsiNTSuiteWebServer", null);
                    standardProperties.Add("MsiNTSuitePersonal", null);
                    standardProperties.Add("MsiPatchRemovalList", null);
                    standardProperties.Add("MSIPATCHREMOVE", null);
                    standardProperties.Add("MSIRESTARTMANAGERCONTROL", null);
                    standardProperties.Add("MsiRestartManagerSessionKey", null);
                    standardProperties.Add("MSIRMSHUTDOWN", null);
                    standardProperties.Add("MsiRunningElevated", null);
                    standardProperties.Add("MsiUIHideCancel", null);
                    standardProperties.Add("MsiUIProgressOnly", null);
                    standardProperties.Add("MsiUISourceResOnly", null);
                    standardProperties.Add("MsiSystemRebootPending", null);
                    standardProperties.Add("MsiWin32AssemblySupport", null);
                    standardProperties.Add("NOCOMPANYNAME", null);
                    standardProperties.Add("NOUSERNAME", null);
                    standardProperties.Add("OLEAdvtSupport", null);
                    standardProperties.Add("OptimizePatchSizeForLargeFiles", null);
                    standardProperties.Add("OriginalDatabase", null);
                    standardProperties.Add("OutOfDiskSpace", null);
                    standardProperties.Add("OutOfNoRbDiskSpace", null);
                    standardProperties.Add("ParentOriginalDatabase", null);
                    standardProperties.Add("ParentProductCode", null);
                    standardProperties.Add("PATCH", null);
                    standardProperties.Add("PATCH_CACHE_DIR", null);
                    standardProperties.Add("PATCH_CACHE_ENABLED", null);
                    standardProperties.Add("PatchGUID", null);
                    standardProperties.Add("PATCHNEWPACKAGECODE", null);
                    standardProperties.Add("PATCHNEWSUMMARYCOMMENTS", null);
                    standardProperties.Add("PATCHNEWSUMMARYSUBJECT", null);
                    standardProperties.Add("PatchOutputPath", null);
                    standardProperties.Add("PatchSourceList", null);
                    standardProperties.Add("PhysicalMemory", null);
                    standardProperties.Add("PIDKEY", null);
                    standardProperties.Add("PIDTemplate", null);
                    standardProperties.Add("Preselected", null);
                    standardProperties.Add("PRIMARYFOLDER", null);
                    standardProperties.Add("PrimaryVolumePath", null);
                    standardProperties.Add("PrimaryVolumeSpaceAvailable", null);
                    standardProperties.Add("PrimaryVolumeSpaceRemaining", null);
                    standardProperties.Add("PrimaryVolumeSpaceRequired", null);
                    standardProperties.Add("Privileged", null);
                    standardProperties.Add("ProductCode", null);
                    standardProperties.Add("ProductID", null);
                    standardProperties.Add("ProductLanguage", null);
                    standardProperties.Add("ProductName", null);
                    standardProperties.Add("ProductState", null);
                    standardProperties.Add("ProductVersion", null);
                    standardProperties.Add("PROMPTROLLBACKCOST", null);
                    standardProperties.Add("REBOOT", null);
                    standardProperties.Add("REBOOTPROMPT", null);
                    standardProperties.Add("RedirectedDllSupport", null);
                    standardProperties.Add("REINSTALL", null);
                    standardProperties.Add("REINSTALLMODE", null);
                    standardProperties.Add("RemoveAdminTS", null);
                    standardProperties.Add("REMOVE", null);
                    standardProperties.Add("ReplacedInUseFiles", null);
                    standardProperties.Add("RestrictedUserControl", null);
                    standardProperties.Add("RESUME", null);
                    standardProperties.Add("RollbackDisabled", null);
                    standardProperties.Add("ROOTDRIVE", null);
                    standardProperties.Add("ScreenX", null);
                    standardProperties.Add("ScreenY", null);
                    standardProperties.Add("SecureCustomProperties", null);
                    standardProperties.Add("ServicePackLevel", null);
                    standardProperties.Add("ServicePackLevelMinor", null);
                    standardProperties.Add("SEQUENCE", null);
                    standardProperties.Add("SharedWindows", null);
                    standardProperties.Add("ShellAdvtSupport", null);
                    standardProperties.Add("SHORTFILENAMES", null);
                    standardProperties.Add("SourceDir", null);
                    standardProperties.Add("SOURCELIST", null);
                    standardProperties.Add("SystemLanguageID", null);
                    standardProperties.Add("TARGETDIR", null);
                    standardProperties.Add("TerminalServer", null);
                    standardProperties.Add("TextHeight", null);
                    standardProperties.Add("Time", null);
                    standardProperties.Add("TRANSFORMS", null);
                    standardProperties.Add("TRANSFORMSATSOURCE", null);
                    standardProperties.Add("TRANSFORMSSECURE", null);
                    standardProperties.Add("TTCSupport", null);
                    standardProperties.Add("UILevel", null);
                    standardProperties.Add("UpdateStarted", null);
                    standardProperties.Add("UpgradeCode", null);
                    standardProperties.Add("UPGRADINGPRODUCTCODE", null);
                    standardProperties.Add("UserLanguageID", null);
                    standardProperties.Add("USERNAME", null);
                    standardProperties.Add("UserSID", null);
                    standardProperties.Add("Version9X", null);
                    standardProperties.Add("VersionDatabase", null);
                    standardProperties.Add("VersionMsi", null);
                    standardProperties.Add("VersionNT", null);
                    standardProperties.Add("VersionNT64", null);
                    standardProperties.Add("VirtualMemory", null);
                    standardProperties.Add("WindowsBuild", null);
                    standardProperties.Add("WindowsVolume", null);
                }
            }

            return standardProperties.ContainsKey(propertyName);
        }


        /// <summary>
        /// Sets up a hashtable with the set of standard MSI directories
        /// </summary>
        private static void LoadStandardDirectories()
        {
            lock (lockObject)
            {
                if (null == standardDirectories)
                {
                    standardDirectories = new Hashtable();
                    standardDirectories.Add("TARGETDIR", null);
                    standardDirectories.Add("AdminToolsFolder", null);
                    standardDirectories.Add("AppDataFolder", null);
                    standardDirectories.Add("CommonAppDataFolder", null);
                    standardDirectories.Add("CommonFilesFolder", null);
                    standardDirectories.Add("DesktopFolder", null);
                    standardDirectories.Add("FavoritesFolder", null);
                    standardDirectories.Add("FontsFolder", null);
                    standardDirectories.Add("LocalAppDataFolder", null);
                    standardDirectories.Add("MyPicturesFolder", null);
                    standardDirectories.Add("PersonalFolder", null);
                    standardDirectories.Add("ProgramFilesFolder", null);
                    standardDirectories.Add("ProgramMenuFolder", null);
                    standardDirectories.Add("SendToFolder", null);
                    standardDirectories.Add("StartMenuFolder", null);
                    standardDirectories.Add("StartupFolder", null);
                    standardDirectories.Add("System16Folder", null);
                    standardDirectories.Add("SystemFolder", null);
                    standardDirectories.Add("TempFolder", null);
                    standardDirectories.Add("TemplateFolder", null);
                    standardDirectories.Add("WindowsFolder", null);
                    standardDirectories.Add("CommonFiles64Folder", null);
                    standardDirectories.Add("ProgramFiles64Folder", null);
                    standardDirectories.Add("System64Folder", null);
                    standardDirectories.Add("NetHoodFolder", null);
                    standardDirectories.Add("PrintHoodFolder", null);
                    standardDirectories.Add("RecentFolder", null);
                    standardDirectories.Add("WindowsVolume", null);
                }
            }
        }
    }
}