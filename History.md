## WixBuild: Version 3.14.0.4118

* SeanHall: WIXBUG:4342 - Fix WixStdBA hang with DisplayInternalUI when pressing the BA cancel button.

* SeanHall: WIXBUG:6158 - Fix WixStdBA hang with DisplayInternalUI in certain cases.

* JasonStephenson: WIXBUG:4875 - Explicitly request RW permissions for iis:Certificate to avoid install errors due to ACCESS_DENIED.

## WixBuild: Version 3.14.0.3910

* BobArnson: WIXBUG:6141: Add ARM64 to a few more enumerations (code and doc).

## WixBuild: Version 3.14.0.3909

* BobArnson: WIXBUG:6137
  - Force Package/@InstallerVersion to at least `500` for ARM*.
  - Include ARM64-aware .cubs.

* BobArnson: WIXBUG:6136
  - Make ARM64 CAs more 64-bit-aware.
  - Fix unsuffixed reference that was pulling in 32-bit CAs in ARM64 package.

## WixBuild: Version 3.14.0.3827

* BobArnson: WIXFEATURE:5558 - Implement ARM64 support in core toolset

* RobMen: WIXBUG:4688 - Support really long paths in MakeSfxCA.exe by using a response file

* RobMen: WIXBUG:6089 - Do not sign SfxCA stub as it prevents signing actual CA dll

* ipetrovic11,stukselbax,SeanHall: WIXFEAT:5658 - Retry launching the elevated bundle once if we think it failed due to antivirus interference.

## WixBuild: Version 3.14.0.3316

* RobMen: WIXBUG:6075 - Fix "Zip Slip" vulnerability in DTF.

* jchoover: WIXBUG:6071 - Enable HTTP to HTTPS redirects for burn downloads.

## WixBuild: Version 3.14.0.3205

## WixBuild: Version 3.14.0.2927

## WixBuild: Version 3.14.0.2812

* HeathS: Add support for .NET Foundation signing service

* SeanHall: WIXBUG:5711 - Remove last remaining reference to .NET 2.0 assembly

## WixBuild: Version 3.14.0.1703

* RobMen - WIXBUG:5486 - Fix potential deadlock with VSTS logger

* RSeanHall - WIXBUG:5803 - Fix dependency bug where custom keys were skipped

* DRISHTI271110 - WIXBUG:5543 - Support TLS 1.2 in SqlExtension

## WixBuild: Version 3.14.0.1118

* RobMen - WIXBUG:5724 - fix DLL hijack of clean room when bundle launched elevated.

## WixBuild: Version 3.14.0.712

* HeathS: WIXBUG:5597 - Check VS2017 product IDs against supported SKUs

## WixBuild: Version 3.14.0
