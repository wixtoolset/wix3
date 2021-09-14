---
title: Burn Built-in Variables
layout: documentation
after: authoring_bundle_package_manifest
---
# Burn Built-in Variables

The Burn engine offers a set of commonly-used variables so you can use them without defining your own. Here is the list of the built-in variable names:

* AdminToolsFolder - gets the well-known folder for CSIDL\_ADMINTOOLS.
* AppDataFolder - gets the well-known folder for CSIDL\_APPDATA.
* CommonAppDataFolder - gets the well-known folder for CSIDL\_COMMON\_APPDATA.
* CommonFilesFolder - gets the well-known folder for CSIDL\_PROGRAM\_FILES\_COMMONX86.
* CommonFiles64Folder - gets the well-known folder for CSIDL\_PROGRAM\_FILES\_COMMON.
* CommonFiles6432Folder - gets the well-known folder for CSIDL\_PROGRAM\_FILES\_COMMON on 64-bit Windows and CSIDL\_PROGRAM\_FILES\_COMMONX86 on 32-bit Windows.
* CompatibilityMode - non-zero if the operating system launched the bootstrapper in compatibility mode.
* ComputerName - name of the computer as returned by GetComputerName() function.
* Date - gets the current date using the short date format of the current user locale.
* DesktopFolder - gets the well-known folder for CSIDL\_DESKTOP.
* FavoritesFolder - gets the well-known folder for CSIDL\_FAVORITES.
* FontsFolder - gets the well-known folder for CSIDL\_FONTS.
* InstallerName - gets the name of the installer engine (&quot;WiX Burn&quot;).
* InstallerVersion - gets the version of the installer engine.
* LocalAppDataFolder - gets the well-known folder for CSIDL\_LOCAL\_APPDATA.
* LogonUser - gets the current user name.
* MyPicturesFolder - gets the well-known folder for CSIDL\_MYPICTURES.
* NativeMachine - gets the [Image File Machine value](https://docs.microsoft.com/en-us/windows/win32/sysinfo/image-file-machine-constants) representing the native architecture of the machine. This variable was added in v3.14 and is only set on Windows 10, version 1511 (TH2) and higher.
* NTProductType - numeric product type from OS version information.
* NTSuiteBackOffice - non-zero if OS version suite is Back Office.
* NTSuiteDataCenter - non-zero if OS version suite is Datacenter.
* NTSuiteEnterprise - non-zero if OS version suite is Enterprise.
* NTSuitePersonal - non-zero if OS version suite is Personal.
* NTSuiteSmallBusiness - non-zero if OS version suite is Small Business.
* NTSuiteSmallBusinessRestricted - non-zero if OS version suite is Restricted Small Business.
* NTSuiteWebServer - non-zero if OS version suite is Web Server.
* PersonalFolder - gets the well-known folder for CSIDL\_PERSONAL.
* ProcessorArchitecture - gets the value of [SYSTEM_INFO.wProcessorArchitecture](http://msdn.microsoft.com/en-us/library/windows/desktop/ms724958%28v=vs.85%29.aspx). If NativeMachine is available then it should be used instead because this value is not accurate when running in WOW.
* Privileged - non-zero if the process could run elevated (on Vista+) or is running as an Administrator (on WinXP).
* ProgramFilesFolder - gets the well-known folder for CSIDL\_PROGRAM\_FILESX86.
* ProgramFiles64Folder - gets the well-known folder for CSIDL\_PROGRAM\_FILES.
* ProgramFiles6432Folder - gets the well-known folder for CSIDL\_PROGRAM\_FILES on 64-bit Windows and CSIDL\_PROGRAM\_FILESX86 on 32-bit Windows.
* ProgramMenuFolder - gets the well-known folder for CSIDL\_PROGRAMS.
* RebootPending - non-zero if the system requires a reboot. Note that this value will reflect the reboot status of the system when the variable is first requested.
* SendToFolder - gets the well-known folder for CSIDL\_SENDTO.
* ServicePackLevel - numeric value representing the installed OS service pack.
* StartMenuFolder - gets the well-known folder for CSIDL\_STARTMENU.
* StartupFolder - gets the well-known folder for CSIDL\_STARTUP.
* SystemFolder - gets the well-known folder for CSIDL\_SYSTEMX86 on 64-bit Windows and CSIDL\_SYSTEM on 32-bit Windows.
* System64Folder - gets the well-known folder for CSIDL\_SYSTEM on 64-bit Windows and undefined on 32-bit Windows.
* SystemLanguageID - gets the language ID for the system locale.
* TempFolder - gets the well-known folder for temp location.
* TemplateFolder - gets the well-known folder for CSIDL\_TEMPLATES.
* TerminalServer - non-zero if the system is running in application server mode of Remote Desktop Services.
* UserUILanguageID - gets the selection language ID for the current user locale.
* UserLanguageID - gets the formatting language ID for the current user locale.
* VersionMsi - version value representing the Windows Installer engine version.
* VersionNT - version value representing the OS version. The result is a version variable (v#.#.#.#) which differs from the MSI Property &apos;VersionNT&apos; which is an integer. For example, to use this variable in a Bundle condition try: &quot;VersionNT &gt; v6.1&quot;.
* VersionNT64 - version value representing the OS version if 64-bit. Undefined if running a 32-bit operating system. The result is a version variable (v#.#.#.#) which differs from the MSI Property &apos;VersionNT64&apos; which is an integer. For example, to use this variable in a Bundle condition try: &quot;VersionNT64 &gt; v6.1&quot;.
* WindowsFolder - gets the well-known folder for CSIDL\_WINDOWS.
* WindowsVolume - gets the well-known folder for the windows volume.
* WixBundleAction - set to the numeric value of BOOTSTRAPPER\_ACTION from the command-line and updated during the call to IBootstrapperEngine::Plan().
* WixBundleDirectoryLayout - set to the folder provided to the -layout switch (default is the directory containing the bundle executable). This variable can also be set by the bootstrapper application to modify where files will be laid out.
* WixBundleElevated - gets whether the bundle was launched elevated and will be set to 1 once the bundle is elevated. For example, use this variable to show or hide the elevation shield in the bootstrapper application UI.
* WixBundleExecutePackageCacheFolder - gets the absolute path to the currently executing package&apos;s cache folder.  This variable is only available while the package is executing.
* WixBundleForcedRestartPackage - gets the ID of the package that caused a force restart during apply. This value is reset on the next call to apply.
* WixBundleInstalled - gets whether the bundle was already installed. This value is only set when the engine initializes.
* WixBundleLastUsedSource - gets the path of the last successful source resolution for a container or payload.
* WixBundleName - gets the name of the bundle (from Bundle/@Name). This variable can also be set by the bootstrapper application to modify the bundle name at runtime.
* WixBundleManufacturer - gets the manufacturer of the bundle (from Bundle/@Manufacturer).
* WixBundleOriginalSource - gets the source path from where the bundle originally ran.
* WixBundleOriginalSourceFolder - gets the folder from where the bundle originally ran.
* WixBundleSourceProcessPath - gets the source path of the bundle where originally executed. Will only be set when bundle is executing in the clean room.
* WixBundleSourceProcessFolder - gets the source folder of the bundle where originally executed. Will only be set when bundle is executing in the clean room.
* WixBundleProviderKey - gets the bundle dependency provider key.
* WixBundleTag - gets the developer-defined tag string for this bundle (from Bundle/@Tag).
* WixBundleUILevel - gets the level of the user interface (the value BOOTSTRAPPER\_DISPLAY enum).
* WixBundleVersion - gets the version for this bundle (from Bundle/@Version).
