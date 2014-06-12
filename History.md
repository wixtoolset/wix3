* BobArnson: WIXBUG:3838 - Since the compiler coerces null values to empty strings, check for those in ColumnDefinition.

* FireGiant: WIXBUG:4319 - Support full range of ExePackage exit code values.

* BobArnson: WIXBUG:4442 - Add missing tables.

* SeanHall: WIXBUG:4416 - Fail fast when loading an MBA on Win7 RTM with .NET 4.5.2 or greater installed.

* SeanHall: Rename IBootstrapperApplication::OnApplyNumberOfPhases to IBootstrapperApplication::OnApplyPhaseCount.

* HeathS: WIXFEAT:4278 - Support redirectable package cache via policy.

* RobMen: WIXBUG:4243 - use hashes to verify bundled packages by default.

* SeanHall: WIXFEAT:3736 - Add WixBundleExecutePackageCacheFolder variable.

* SeanHall: WIXBUG:3890 - put Burn command line arguments first when launching unelevated parent process since malformed user arguments created an infinite loop.

* SeanHall: WIXBUG:3951 - Document limitations of VersionVariables and create NormalizeVersion method.

* RobMen: WIXBUG:4288 - do not mask error code when testing file size of payload in cache.

* RobMen: Point to new page for linker error 217 to fix WIXBUG:4208.

## WixBuild: Version 3.9.526.0

* BobArnson: WIXBUG:3701 - Add a check for file size when verifying cab cache.

* BobArnson: WIXBUG:4134 - Add <UI> overrides to WixUI localization for pt-BR.

* BobArnson: WIXBUG:4192 - Guard against a null.

* BobArnson: WIXBUG:4125 - Clarify doc.

* STunney: WIXBUG:4434 - Assert maximum patch baseline id length (because of MSI limitation in transform substorage ids).

* BobArnson: WIXFEAT:2742 - Add ProcessorArchitecture Burn variable.

* BobArnson: WIXFEAT:4378 - Add WixBundleOriginalSourceFolder variable (WixBundleOriginalSource minus file name).

* BobArnson: WIXBUG:4418 - Release strings and remove dead code.

* SeanHall: WIXFEAT:4161 - Add the PrereqSupportPackage attribute to all package types so that more than one package can be installed by the Prereq BA, and the MbaPrereqPackage can be conditionally installed.

* SeanHall: WIXFEAT:4413 - Add IBootstrapperApplication::OnApplyNumberOfPhases.

* BobArnson: WIXBUG:4215 - Clarify all the elements that switch bitness based on -arch/InstallerPlatform.

* SeanHall: WIXBUG:3835 - Fix progress bug when extracting multiple packages from a container.

* BobArnson: WIXBUG:4410 - Fix MediaTemplate/@CompressionLevel and ensure that when it's not specified, the default compression level takes effect.

* BobArnson: WIXBUG:4394 - Enforce a maximum include nesting depth of 1024 to avoid stack overflows when files include themselves.

* johnbuuck: WIXBUG:4279 - Add support for MSBuild version 12.0.

* mavxg: WIXFEAT:4373 - Add LogonAsBatchJob to WixUtilExtension.User

* SeanHall: WIXFEAT:4329 - Change the type of the Cache attribute to the new YesNoAlwaysType, and make the engine always cache a package if Cache is set to always.

* SeanHall: WIXBUG:3978 - Add InstallCondition to BootstrapperApplicationData.xml.

* HeathS: Don't fail uninstall when planned package was removed by related bundle. Don't repair dependent bundles when upgrading a patch or addon bundle.

* AndySt: Update Registration key was being deleted during a bundle to bundle upgrade. Added version check so that only if the version was the same would the key be deleted.

* AndySt: Skip the repair of the related bundle if it has the same provider key as an embedded bundle that is being installed, so only V2 of the patch bundle is on the machine at the end.

* AndySt: Add /DisableSystemRestore switch and /OriginalSource switch /OriginalSource is used when authoring embedded bundles so it can look to the parent's original location instead of the package cached location that it is run from.

* HeathS: Make sure enough memory is allocated for compatible packages.

* HeathS: Uninstall compatible orphaned MSI packages.

* HeathS: Allow package downgrades for related bundles with proper ref-counting. Make sure packages register their identities in the provider registry.

## WixBuild: Version 3.9.421.0

* steveOgilvie/BobArnson: Add to WixStdBA the WixBundleFileVersion variable that's set to the file version of the bundle .exe.

* SeanHall: WIXBUG:4163 - For references to Microsoft.Deployment.WindowsInstaller.dll in managed Custom Action projects, set Private=True in case it's installed in the GAC.

* BobArnson: WIXBUG:4361 - Clarify error message.

* BobArnson: WIXBUG:4384 - Add MsuPackage back as supporting RemotePayload.

* STunney/BobArnson: WIXFEAT:4239 - Add option to not extract the .msi when melting .wixpdbs. Don't leave temporary cabinet files behind (unless -notidy is in effect).

* BobArnson: WIXBUG:4331 - guard against null registry keys

* BobArnson: WIXBUG:4301 - don't cross the HRESULTs and Win32 error codes; it would be bad.

* SeanHall: WIXBUG:3914 - !(bind.packageVersion.PackageID) isn't expanded in bundle.

* RobMen: WIXBUG:4228 - send TRUE to WM_ENDSESSION to correctly inform applications to close.

* RobMen: WIXBUG:4285 - Fix typo in documentation.

* RobMen: WIXFEAT:4234 - Remove ClickThrough.

* SeanHall: WIXFEAT:4233 - Add ProductCode, UpgradeCode, and Version to BootstrapperApplicationData.xml.

* WIXBUG:3883 - Retry on IIS ERROR_TRANSACTIONAL_CONFLICT too

* SeanHall: WIXFEAT:4292 - Don't assume downgrade if already detected major upgrade.

## WixBuild: Version 3.9.120.0

* BobArnson: WIXBUG:4271 - Warn when using a RemotePayload package that isn't explicitly set @Compressed="no".

* BobArnson: WIXBUG:4263 - Add WixMsmqExtension.dll back to binaries .zip.

* BobArnson: WIXBUG:4272 - Omit custom (and therefore un-Google-able) HRESULT for failed launch conditions on the failed page of WixStdBA.

* BobArnson: WIXBUG:4077 - Add log entry for the bal:Condition message itself.

## WixBuild: Version 3.9.16.0

* rjvdboon: WIXBUG:4089 - Remove SimpleTypes from help table of contents.

* jchoover: WIXFEAT:4194 - Move DownloadUrl functionality from engine to dutil.

* jhennessey: WIXFEAT:3169 - Add support for upgrade code-based product search.

* jchoover: WIXFEAT:4193 - Added searching for bundles via upgrade code and querying bundle properties via provider code.

## WixBuild: Version 3.9.10.0

* BMurri: WIXBUG:4225 - DTF: InstallPathMap didn't accept identifiers differing only by case.

## WixBuild: Version 3.9.02.0

* BMurri: WIXBUG:4174 - Prevent unneeded build errors when generating delta patches with long file ids.

* BMurri: WIXBUG:3326 - project harvester now unescapes linked files with spaces.

* BobArnson: Support building on VS2013 only. Make SQL CE optional.

* RobMen: WiX v3.9

## WixBuild: Version 3.9.0.0
