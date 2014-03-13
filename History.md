## WixBuild: Version 3.8.1128.0

* RobMen: RTM WiX Toolset v3.8.

* AVishnyakov, BMurri: WIXFeature:4144 - Icons for WiX Visual Studio 2012/2013 templates.

## WixBuild: Version 3.8.1125.0

* BobArnson: WIXBUG:4201 - Correct the list of themes that support showing the bundle version.

* BobArnson: WIXBUG:4198 - Use the Release value for .NET 4.5.1 on Windows 8.1 rather than that of the downlevel redistributable.

* BobArnson: WIXBUG:4197 - Add VS2013 detection properties and custom actions to the doc. Add missing VS2012 entries.

## WixBuild: Version 3.8.1114.0

* BobArnson: WIXBUG:4185 - Correct VSIXInstaller.exe FileSearch name for VS2013.

* BobArnson: WIXBUG:4180 - Add .NET Framework 4.5.1 package groups.

* RjvdBoon: WIXBUG:4111;4170 - Generate URLs in the manual with forward slashes instead of backward slashes.

* BobArnson: WIXBUG:4175 - Add detection of VS/VWD 2013 for VsixPackage.

* BobArnson: WIXBUG:3872 - Add a block in the WiX BA when .NET 3.5.1 isn't present.

## WixBuild: Version 3.8.1031.0

* BobArnson: WIXBUG:4160 - Add DTF assemblies Melt.exe needs for "pdb melting" to the toolset bin directory.

* BMurri: WIXBUG:4061 - Call RtlGetVersion out of ntdll.dll to get OS version

* BMurri: WIXBUG:4032 - Enable heat to accomodate COM relative paths and foreign servers.

* BobArnson: WIXBUG:3807 - Add AssemblyFoldersEx key.

* BobArnson: WIXBUG:3737 - Block with specific message attempt to use RemotePayload outside ExePackage.

* BobArnson: WIXBUG:3830 - Remove obsolete PreProcExampleExtension.

* BobArnson: WIXBUG:4158 - Add error message rather than throwing an exception.

* BobArnson: WIXBUG:3870 - Clarify Directory/@ComponentGuidGenerationSeed documentation.

* BobArnson: WIXBUG:4142 - Add message for WixLocalization namespace problems.

## WixBuild: Version 3.8.1021.0

* BobArnson: Fix WIXBUG:3839 and WIXBUG:4093.
  * Add projects to build version-specific templates for VS2012 and VS2013.
  * Correct paths to native SDK libraries.

* BobArnson: WIXBUG:4146 - Fill in missing searches and verify GUIDs.

* RobMen: WIXFEAT:4091 - build and setup libs for VS2013.

* BobArnson: WIXBUG:4146 - Fill in missing searches and verify GUIDs.

* BobArnson: WIXBUG:4152 - Remove WixPdbPath property and -sbuildinfo switch.

* BobArnson: WIXBUG:3165 - Add x64 package to write x64 registry values.

## WixBuild: Version 3.8.1014.0

* BobArnson: Handle the case when you have SQLCE v4.0 installed without matching SDK but do have SQLCE v3.5 and its SDK installed.

## WixBuild: Version 3.8.1007.0

* BMurri: WIXBUG:4104 - Wrong language ID for Slovenian specified on site

* RobMen: Reorganize source code to better match reality.

* RobMen: Fix parallel build when building wix.proj.

* BobArnson: WIXBUG:4073 - write v3.8 value under HKLM\SOFTWARE\Microsoft\Windows Installer XML\3.x with installation directory.

* BobArnson: WIXBUG:4120 - Remove SetupBld.

* BobArnson: WIXBUG:4128 - Add Win8/8.1 compatibility GUIDs to stub.manifest.

* RobMen: WIXBUG:4103 - use [#FileId] in Shortcut how-tos.

* BMurri: WIXBUG:4062 - Heat harvesting is URL Encoding parenthesis in “Content” filenames resulting in LGHT0103 errors.

* BMurri: WIXBUG:4072 - NullReferenceException in pyro.exe.

## WixBuild: Version 3.8.826.0

* RobMen: WIXBUG:4083 - Website manual cannot be navigated.

## WixBuild: Version 3.8.819.0

* BobArnson: SFBUG:2474 - Allow bind-time variables in Publish/@Property.

* BobArnson: SFBUG:3352 - Throw a better error message for unexpected attributes.

## WixBuild: Version 3.8.722.0

* BobArnson:
  * Add Visual Studio 2012 Express for Windows Desktop detection properties to WixVSExtension.
  * Add Visual Studio 2013 Preview detection properties to WixVSExtension.

* BMurri: Bug in delta patching (large file optimize was incorrect)

## WixBuild: Version 3.8.715.0

* BMurri: SFBUG:3324 - Fix issues caused when different files have differing numbers of old versions.

* BMurri: Describe new help system and how to add new topics.

* BMurri: SFBUG:3342 - Fix unhandled exception when unbinding transforms containing tables with null primary keys.

* BMurri: SFBUG:3329 - xmlns attributes may have either the name or the prefix set to xmlns.

* BMurri: Fix links and cleanup documentation formating.

## WixBuild: Version 3.8.708.0

* BobArnson: SFBUG:1747 - Handle missing tables and system tables that exist only when SQL queries are being run when determining row counts.

* BobArnson: SFBUG:2235 - Use overload of WaitHandle.WaitOne that's available on .NET 2.0.

* BobArnson: SFBUG:1677 - Update conditions for Firewall custom actions to support Windows 2003 Server SP1.

## WixBuild: Version 3.8.628.0

* RobMen: Updated release system.

* MiCarls: SFBUG:3288 - Fix unhandled exception when harvesting unsupported registry types from .reg files.

* Corfe83: SFBUG:3283 - Allow wix tools to load remote wix extensions / other assemblies

* BobArnson: SFBUG:3116 - Add @HideTarget to custom actions and @Hidden to custom action data properties.

* RobMen: New help build system.

## WixBuild: Version 3.8.611.0

* BorisDachev: SFBUG:3066 - harvest project links in MSBuild 4.0 correctly.

* BMurri: SFBUG:3285 - Explicitly set enable32BitAppOnWin64 in IIS7 AppPools.

* NeilSleighthold: Add probing for default sub language of the detected primary language.

* BarryNolte: SFBUG:3296 - quote NETFX4.5 log path.

* BarryNolte: SFBUG:3288 - improve heat.exe error message for unhandled registry types.

* PatOShea - SFBUG:2485;3119;3020;3223 - fixed links in html documentation.

## WixBuild: Version 3.8.520.0

* StefanAgner: SFBUG:3074 - fix harvesting to correctly handle WinForms.resx by changing current directory.

* BobArnson: SFBUG:3262 - Implement MediaTemplate/@CompressionLevel.

* BobArnson: SFBUG:3266 - reword error message to avoid implying required values can be omitted.

* BobArnson: SFBUG:3256 - be a bit more lax about accepting binder variables anywhere in version strings.

* BobArnson: SFBUG:3248 - alias UI and Control elements in .wxl files for LocUtil.

* BobArnson: SFBUG:3233 - verify bundle version elements are <=UInt16 once all binder variables are resolved.

* JCHoover: SFFEAT:727 - Added support for ActionData messages (ACTIONSTART) within WixStdBA.

* JCHoover: SFEAT:629 - skip shortcut scheduling if the CA is unable to create CLSID_InternetShortcut or CLSID_ShellLink.

* Corfe83: SFBUG:3251 - Fix heat to no longer hang with ProgIds whose CurVer is their own ProgId.

## WixBuild: Version 3.8.514.0

* JCHoover: SFFEAT:636;668 - support wildcard ProductCode and configuring UpgradeCode in InstanceTransforms.

* JHennessey: Add assemblyPublicKeyTokenPreservedCase and assemblyFullNamePreservedCase binder variables.

* RobMen: Add support for integration tests on xUnit.net and convert one Burn test.

* NeilSleightholm: Add HyperlinkSidebar, HyperlinkLarge and RtfLarge themes to wixstdba.

## WixBuild: Version 3.8.506.0

* NeilSleightholm: SFBUG:3289 - fix 'heat reg' output to not generate candle CNDL1138 warnings.

* RobMen: Add support for xUnit.net based tests.

## WixBuild: Version 3.8.422.0

* JoshuaKwan: Added strings in Error table for errors 1610,1611,1651 but not localized yet.

## WixBuild: Version 3.8.415.0

* RobMen: SFBUG:3263 - correctly register bundle when present in cache but pending removal on restart.

## WixBuild: Version 3.8.408.0

* RobMen: SFBUG:3178 - make NETFX v3.5 default base for WiX toolset.

* RobMen: SFBUG:3222 - fix Manufacturer in WiX installs.

* NeilSleightholm: Add support for BA functions to wixstdba.

* RobMen: Allow WebApplication to be defined under WebDir element.

## WixBuild: Version 3.8.401.0

* RobMen: Add arguments and ability to hide launch target in wixstdba.

* NeilSleightholm: Add support for additional controls on the install and options pages.

* NeilSleightholm: Add support for an auto generated version number to the preprocessor.

* NeilSleightholm: SFBUG:3191 - document NETFX v4.5 properties.

* NeilSleightholm: SFBUG:3221 - use correct function to get user language in LocProbeForFile().

## WixBuild: Version 3.8.326.0

* BruceCran: Use v110_xp instead of v110 for VS2012 to retain compatibility with XP.

* RobMen: SFBUG:3160 - pass "files in use" message to BA correctly.

* BobArnson: SFBUG:3236 - Enable Lux in binaries.zip and setup.

* RobMen: Enhance wixstdba to display user and warning MSI messages.

## WixBuild: Version 3.8.305.0

* RobMen: Add PromptToContinue attribute to CloseApplications.

* RobMen: Enhance wixstdba progress text for related bundles.

## WixBuild: Version 3.8.225.0

* RobMen: Add EndSession, TerminateProcess and Timeout attributes to CloseApplications.

* RobMen: Support transparent .pngs for Images in thmutil.

* RobMen: SFBUG:3216 - fix build to handle multiple platform SDK include paths.

* RobMen: WiX v3.8

## WixBuild: Version 3.8.0.0
