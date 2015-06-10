* JacobHoover: WIXBUG:4512 - fix WiX BA, prevent multiple install clicks

## WixBuild: Version 3.10.0.1726

* creativbox: WIXFEAT:4382 - Added files-in-use UI to WixStdBA

* SeanHall: WIXBUG:4597 - Fix project harvesting in heat for Tools version 12.0 and 14.0.

* HeathS: WIXBUG:3060 - Do not redownload package payloads when /layout is restarted.

* SeanHall: WIXFEAT:4763 - Add literal flag to Burn variables to indicate that their values shouldn't be formatted.

* BobArnson: WIXFEAT:4772 - Replace hyperlink ShelExec with ShelExecUnelevated.

* BobArnson: WIXBUG:4716 - If a .wxl file is missing strings added in v3.10, duplicate the generic string from v3.9. Add support for doing so to locutil.

## WixBuild: Version 3.10.0.1719

* SeanHall: WIXBUG:4761 - Use the package's exit code to tell if the prereq was installed.

* BobArnson: WIXBUG:4734 - Rewrote type-51 CAs using SetProperty.

* BobArnson: WIXFEAT:4720 - Added bind-time variables for .NET Framework package groups detect condition, install condition, and package directories.

* HeathS: Add VSIX property for VS2015 and fix searches for previous versions.

* BobArnson: Add libs_minimal.proj with just the libraries needed for tools/ tree build. This prevents the build from backing up behind a full libs/ tree build, which gets more painful the more versions of Visual Studio that are installed.

* BobArnson: WIXBUG:4750 - Add a note about binary (in)compatibility.

* RobMen: WIXBUG:4732 - fix documentation links to MsiServiceConfig and MsiServiceConfigFailureActions.

* BobArnson: WIXFEAT:4719 - Implement ExePackage/CommandLine:
  * Add WixBundleExecutePackageAction variable: Set to the BOOTSTRAPPER_ACTION_STATE of the package as it's about to executed.
  * Add ExePackage/CommandLine to compiler and binder.
  * Update Burn to parse CommandLine table in manifest and apply it during ExePackage execution.

* BobArnson: WIXBUG:4725 - Scrub the WixStdBA license doc and add a blurb about a missing WixStdbaLicenseUrl variable.

* BobArnson: WIXBUG:4721 - Tweak RepairCommand doc.

* SeanHall: WIXFEAT:4619 - Include WixUI dialogs and wxl files in core MSI.

* SeanHall: WIXFEAT:4618 - Include WixStdBA and mbapreq themes and wxl files in core MSI.

* JacobHoover: WIXBUG:4482 - Temp file for update feed isn't deleted when download fails

* SeanHall: WIXBUG:4731 - Obscure hidden variable values in the logged command line.

* SeanHall: WIXBUG:4630 - Serialize all variables to the elevated Burn process.

* SeanHall: WIXFEAT:3933 - Make WixBundleManufacturer variable writable.

* BobArnson: WIXBUG:4700 - Added blurb about SequenceType.first.

* BobArnson: Project reference tweaks: 
  - Removed unnecessary reference to setupicons from x64msi.
  - Move BuildInParallel=false from global to just project that needs it

## WixBuild: Version 3.10.0.1519

* BobArnson: WIXBUG:4520 - Added blurb about using a PayloadGroup to get offline capability for .NET redist.

* BobArnson: WIXBUG:4545 - Resized button for de-DE.

* BobArnson: Add WixStdBALanguageId language and documentation.

* BobArnson: Add project output message in minimal MSBuild logging verbosity.

* BobArnson: WIXBUG:4654 - Add VS14 properties and custom actions. And, as it's a long topic, added anchors and links.

* BobArnson: WIXBUG:4617 - Added 4.5.2 package group information to doc. Also mentioned that some properties are new to WiX v3.10.

* BobArnson: WixBroadcastSettingChange and WixBroadcastEnvironmentChange custom actions to WixUtilExtension.

* SeanHall: WIXBUG:4393 - Fix BOOTSTRAPPER_REQUEST_STATE_CACHE.

* thfabba: WIXBUG:4681 - Corrected return type on the lone WOW64 redirection function that returns a BOOLEAN instead of BOOL.

* SeanHall: WIXBUG:4689 - Fix hidden numeric and version variables.

## WixBuild: Version 3.10.0.1502

* BobArnson: WIXBUG:3872 - Added note that CLR v2.0 is required in WiX v3.10 and that CLR v4.0 will be the minimum required in WiX v3.11.

* SeanHall: WIXBUG:4685 - Fix bug in mbahost where it didn't bind as the LegacyV2Runtime when necessary.

* SeanHall: WIXBUG:4669 - Fix bug in mbahost where it assumed that the CLRCreateInstance function was implemented when it exists.

* SeanHall: WIXBUG:4646 - Allow sharing the whole drive with util:FileShare.

* SeanHall: WIXBUG:4647 - Format ConfirmCancelMessage in WixStdBA.

* SeanHall: WIXFEAT:4496 - Make WixStdBA load the text for named editboxes.

* SeanHall: WIXBUG:4480 - Remove non-standard and unnecessary regex contructs from wix.xsd.

## WixBuild: Version 3.10.0.1403

* BobArnson: WIXBUG:4662 - Add WIX_IS_NETFRAMEWORK_4XX_OR_LATER_INSTALLED SetProperty custom actions to WixNetfxExtension.

* BobArnson: WIXBUG:4611 - Eliminate mysteriously doubled .pkgdef content.

* BobArnson: WIXBUG:4610 - Write RegisterPerfmonManifest CustomActionData correctly.

* BobArnson: WIXBUG:4589 - Catch exceptions from Version when passing in arbitrary strings. For bundles, try to recover a three-part version number.

* STunney: WIXBUG:4187 - Melt doesn't export Binary or Icon tables

* BobArnson: WIXBUG:4553 - Fix Lux generator to exclude any files with non-fragment sections. Fix Lux custom actions to have proper config.

* PhillHogland: WIXBUG:4592 - Register named process, in another user's context with Restart Manager.  If Access Denied, continue install rather than fail.

* BobArnson: **BREAKING CHANGE** Changed bundle version to Major.Minor.0.BuildNumber. This allows us to publish updates as Major.Minor.(GreaterThanZero).BuildNumber. MSI product version numbers remain Major.Minor.BuildNumber so major upgrades continue to work. This bundle will not upgrade from build v3.10.1124.0. If you've installed v3.10.1124.0, you must uninstall before installing a later bundle.

* BMurri: WIXBUG:3750 - Add LaunchWorkingFolder to wixstdba to facilitate processes that require a different working folder.

* SeanHall: WIXBUG:4609 - Fix incorrect use of BVariantCopy by creating the new method BVariantSetValue.

* SeanHall: WIXBUG:4608 - Fix bug in mbapreq where it wouldn't reload the bootstrapper if there was a mix of installed and uninstalled prerequisites.

## WixBuild: Version 3.10.1124.0

* SeanHall: WIXBUG:4598 - Fix thmutil documentation.  Also backport some thmutil features/fixes from wix4.

* BobArnson: WIXBUG:4580 - Check bit mask appropriately for Burn system variables.

* SamuelS: WIXFEAT:4543 - Allow Pyro to exclude empty patch transforms.

* HeathS: WIXBUG:4542 - Pad package sequence number log file names for proper sorting

* HeathS: Add logging for hash verification and registration issues.

* HeathS: Redefine Exit\* macros as variadic macros

* SeanHall: WIXFEAT:4505 - WixHttpExtension for URL reservations.

* BobArnson: WIXBUG:4569 - Add SWAPRUN for CD(!) and NET back to the Burn stub.

* BobArnson: Add support for registering Votive into Visual Studio 2015 Preview.

* BobArnson: WiX v3.10

## WixBuild: Version 3.10.0.0
