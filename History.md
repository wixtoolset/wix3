* JozefIzso: WIXBUG:3920 - Make themes DPI aware

* HeathS: WIXBUG:4541 - Add support for high DPI to the Burn engine

## WixBuild: Version 3.11.0.705

* @barnson: WIXBUG:5306 - Warn against ServiceConfig and ServiceConfigFailureActions.

* @barnson: Fix WIXBUG:5294 - Move MsiProperty check from ChainPackageInfo,
  where it was verifying MSI property names too early, to the compiler for the
  earliest feedback.

* @barnson for @firegiantco: Update FileVersionFromStringEx to handle "vVersion" syntax like FileVersionFromString.

* @barnson: Enable the WiX native SDK when the new Visual C++ Build Tools bundle is installed. Fixes WIXBUG:5279.

* SeanHall: WIXBUG:4810 - Fix bug where mbapreq tried to do something other than HELP or INSTALL.

* SeanHall: WIXBUG:5308 - Make embedded bundles launch a clean room process so the BA runs in a consistent environment.

* SeanHall: WIXBUG:5301 - Fix bug where file handles weren't being passed to the clean room process.

* SeanHall: WIXBUG:5302 - Fix bug where the command line for burn exe packages had the executable path in the middle.

* FRichter: WIXBUG:5277 - burn engine: Always use the bundle source path for all purposes. The original source path is needed in all cases: for copying the bundle to the layout directory as well as for checking whether we're layouting to the bundle location.

* @barnson: Fix WIXBUG:5293 - Document illegal MsiProperty names.

RobMen: WIXBUG:5282 - reduce clean room security to successfully load BA's dependent on GDI+ (including WinForms).

* FabienL: WIXBUG:4976 - Add support for .net framework 4.6.1 in netfxExtension

## WixBuild: Version 3.11.0.504

* SeanHall: WIXBUG:5238 - Get the engine's file handle as soon as possible.  Also, when launching Burn processes, pass a file handle of the exe to the process on the command line.

* SeanHall: WIXBUG:5234 - Make Burn grab a file handle to the original bundle so it can still access compressed payloads even if the original bundle was deleted after initialization (e.g. when a bundle uses Burn's built-in update mechanism). Also, when launching the clean room Burn process, pass a file handle of the original exe to the process on the command line. Use this file handle when using the attached container.

## WixBuild: Version 3.11.0.321

* HeathS: WIXFEAT:5230 - Support adding users to Performance Log users group

* HeathS: WIXBUG:5229 - Allow processes to terminate with 0 exit code

* HeathS: WIXBUG:5227 - Allow for merging of directories from different wixlibs and sources

* HeathS: WIXBUG:4894 - Allow lit binder extensions to parse command line arguments

## WixBuild: Version 3.11.0.226

* HeathS: WIXBUG:4880 - Add detection support for VS15

* @barnson for @firegiantco: Fix wixtoolset/issues#5221.
  * Use better logic to determine when to send the Burn ancestors switches.

* @barnson: Write VersionMajor/VersionMinor Uninstall values.
  * Partial fix for wixtoolset/issues#5171. (Does not write InstallLocation.)

* @barnson: Fix up bad/old links in Learning WiX topic.

* @barnson: WIXBUG:4802 - Clarify QtExec names by version.

* @barnson: Correct paths in votive .pkgdef for v3.11.

* BMurri: WIXBUG:5132 - fix incomplete usage of kbKeyBitness parameter of RegDelete() function in DUtil's RegUtil.

* @barnson: WIXBUG:5185 - correct LPWSTR to LPCWSTR

* @barnson: Correct file name of local name for Web package per wixtoolset/issues#4904

## WixBuild: Version 3.11.0.129

* jchoover: WIXBUG:5193 - Fix /layout default directory with clean room:
  * Attempt to use WixBundleSourceProcessPath / WixBundleSourceProcessFolder before defaulting to PathForCurrentProcess
  * Prevent trying to layout the origional bundle exe on top of the existing exe, if the path provided to layout is the same as the bundles working directory.

* BMurri: WIXBUG:5186 - Fix build warning MSB3277:
  * Starting with VSIP 10 SP1 the Assemblies directory was split into two subdirectories: v2.0 & v4.0. That change wasn't properly reflected in the code, rendering many of the various HintPaths ineffective.
  * MSBuild was picking a different version of Microsoft.VisualStudio.CommonIDE than the one most likely intended (and that particular assembly never was in VSIP 2010 SP1).
  * It's possible that the build warning could possibly mask some unexpected behaviors introduced after support for some VS post-2010 was added do to the different version of the assembly selected from the original code's assumption.

* @barnson: Make CBalBaseBootstrapperApplication::PromptCancel usefully overrideable.
  * Make cancellation related members protected rather than private so PromptCancel can be overridden to provide custom cancellation prompt UI.

* BobArnson: Change Burn's behavior to, instead of skipping all related bundles when the current bundle is embedded, skip only dependent bundles when the current bundle is a related bundle. (Burn supports embedded mode in cases other than when being executed as a related bundle.)

* BobArnson: Have Burn rewrite ARP DisplayName during modify so changes to WixBundleName are reflected in ARP.

* MikeGC: Add StrAllocConcatFormatted to concatenate a formatted string to an existing string.

* MikeGC: Add simple combo box support to ThmUtil.

* BobArnson: Add `DisableVS201x` properties to skip versions of VS during the build.
  This is useful to save build time during debugging and to diagnose codegen problems in different versions of VS.

* SeanHall: WIXBUG:4857 - Fix DTF custom actions in Win10 Apps and Features.

jmcooper8654: WIXFEAT:4437 - Modify Wix.CA.targets to add PDB files to CA Package when /p:Configuration=Debug.

* DavidFlamme: WIXBUG:4785 - Fixing memory leak in InstallPackage.cs

MikeGC: WIXBUG:4878 - fix iniutil memory leak

* Himem: WIXBUG:4737 - fixed condition of showing InvalidDirDlg from BrowseDlg

* BMurri: WIXBUG:3902 - Fix ability to find config files in certain circumstances.

## WixBuild: Version 3.11.0.0
