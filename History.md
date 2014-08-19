* RobMen: WIXBUG:4501 - Add file size to upload metadata for use in releases feed.

* RobMen: WIXBUG:4497 - Undeprecate "-spdb" command-line switch.

* SeanHall: WIXBUG:4491 - Make sure the BA DLL is the first payload in the UX container.

## WixBuild: Version 3.9.805.0

* jchoover: WIXBUG:4481 - Better handle update detection when the feed isn't available or is malformed and when uninstalling.

* BobArnson: WIXBUG:4486 - When writing to the Run/RunOnce key to handle in-chain reboots, include the cached bundle stub.

## WixBuild: Version 3.9.721.0

## WixBuild: Version 3.9.720.0

* BobArnson: WIXBUG:4474 - Add missing headers; add missing file and correct typo to point to deputil directory.

* jchoover: Switch WixBA over to using engine updates.

* BobArnson: Install native SDK packages when VS Express SKUs (VC++ Express v10 or Windows Desktop Express v11/v12), in addition to Professional and later.

* BobArnson: WIXBUG:4456 - Look at different things on opposite sides of an expression.

* jchoover: Fixed some memory leaks in the engine.

* BobArnson: WIXBUG:4466 - Open icons with read-sharing in DTF.

* BobArnson: WIXBUG:4476 - Add x64 deputil.lib to NativeSdkMsi.

* BobArnson: Use MediaTemplate in WiX setup. Include native SDK packages when the corresponding compiler is present, not just when the corresponding SDK is present. (The SDK is needed only to create the C++ custom action templates.)

* BobArnson: WIXBUG:4460 - Switch license from HTML to plain text.

* BobArnson: WIXBUG:4471 - Add warning about late RemoveExistingProducts scheduling with PerfCounterManifest.

* RobMen: WIXBUG:4468 - fix missed suppression of suppress signature verification of MSI packages.

* BobArnson: WIXBUG:4473 - Remove Wui.csproj from Wix.sln.

* SeanHall: WIXBUG:4472 - Try to clean the downloaded update bundle from the cache.

* SeanHall: WIXBUG:4467 - Create path2utl for path functions that require shlwapi.lib.

* SeanHall: WIXBUG:4470 - Check whether the LaunchArguments are null before trying to format them.

* flcdrg: WIXBUG:4437 - Adds CopyLocal COM reference assemblies to the list of assemblies to be included in managed CA.

* champloo: WIXBUG:4097 - Fixes uncaught UnauthorizedAccessException in RecursiveFileAttributes.

* RobMen: WIXFEAT:4188 - deprecate switches removed in WiX v4.0

## WixBuild: Version 3.9.702.0

* HeathS: WIXBUG:4457 - Add support for temporary object columns in DTF.

* SeanHall: WIXBUG:4459 - Make Burn decrypt encrypted payloads after moving/copying to the package cache.

* SeanHall: WIXBUG:4458 - Make DTF set the current directory for a managed custom action to the AppDomain's BaseDirectory.

* jchoover: WIXFEAT:4190 - Added support for self updating bundles.

* SeanHall: WIXFEAT:3249 - Allow BA to run elevated async process through the engine.

## WixBuild: Version 3.9.616.0

* HeathS: WIXBUG:4422 - Ref-count superseded products when provider already exists.

* HeathS: WIXBUG:4431 - Fix objects schema to allow unbounded columns.

* ErnestT: WIXBUG:4430 - Do not repair dependent bundles if no packages are executed.

* ErnestT: WIXBUG:4429 - Fix responsiveness problems when cancelling during a BITS download that is not transferring (in a transient error state for example).

* HeathS: WIXBUG:4428 - Add detection for Visual Studio 14.0.

* ErnestT: WIXBUG:4427 - Update resume command handling to support more than 260 characters.

* HeathS: WIXBUG:4425 - Prevent embedded bundles from starting simultaneously after reboot.

* HeathS: WIXBUG:4424 - Allow user custom actions to be independently non-vital.

* HeathS: WIXBUG:4420 - Suppress patch sequence data to optimize patch performance

* HeathS: WIXBUG:4366 - Correctly enum all products for non-specific patches

* BobArnson: WIXBUG:4443 - Ensure that MsiPackages in a Bundle have ProductVersions that fit in a QWORD, how Burn represents a four-field version number with each field a WORD.

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
