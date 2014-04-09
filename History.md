* AndySt: Add /DisableSystemRestore switch and /OriginalSource switch /OriginalSource is used when authoring embedded bundles so it can look to the parent's original location instead of the package cached location that it is run from.

* HeathS: Make sure enough memory is allocated for compatible packages.

* HeathS: Uninstall compatible orphaned MSI packages.

* HeathS: Allow package downgrades for related bundles with proper ref-counting. Make sure packages register their identities in the provider registry.

* STunney/BobArnson: WIXFEAT:4239 - Add option to not extract the .msi when melting .wixpdbs. Don't leave temporary cabinet files behind (unless -notidy is in effect).

* BobArnson: WIXBUG:4331 - guard against null registry keys

* BobArnson: WIXBUG:4301 - don't cross the HRESULTs and Win32 error codes; it would be bad.

* RobMen: WIXBUG:4228 - send TRUE to WM_ENDSESSION to correctly inform applications to close.

* RobMen: WIXBUG:4285 - Fix typo in documentation.

* RobMen: WIXFEAT:4234 - Remove ClickThrough.

* SeanHall: WIXFEAT:4292 - Add ProductCode, UpgradeCode, and Version to BootstrapperApplicationData.xml.

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
