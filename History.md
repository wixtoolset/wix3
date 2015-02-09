* SeanHall: WIXBUG:4647 - Format ConfirmCancelMessage in WixStdBA.

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
