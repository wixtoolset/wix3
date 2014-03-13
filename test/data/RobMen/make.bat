
@echo off
setlocal

set _THIS=%~dp0
pushd %_THIS%

if ""=="%WIX_BUILDROOT%" set WIX_BUILDROOT=%WIX_ROOT%\build\

set _B=build\
set _BP=build\obj\packages\
set _BM=build\obj\manifests\
set _M=manifests\
set _P=packages\
set _R=..\..\..\build\debug\x86\StduxResources
set _S=..\..\..\build\debug\x86\WixstdbaResources
set _T=..\..\..\build\debug\x86\
set _BASEPATHS=-b %_BP% -b %_M% -b %_R% -b %_T% -b build\Package1 -b build\Package3
rem set _NOTIDY=-notidy

IF /I "bundles"=="%1" (
   rd /s/q %_B% 2> nul
   )

IF /I "clean"=="%1" (
   rd /s/q %_B% 2> nul
   goto :EOF
   )


IF NOT EXIST %_BP%*.msi (
   md %_BP%
   %_T%candle.exe %_P%*.wxs -o %_BP%
   %_T%light.exe %_BP%Package4.wixobj -ext WixUtilExtension -sval -o %_BP%Package4.msi
   )

rem IF NOT EXIST %_B%external_nocache\b.exe (
rem    md %_B%external_nocache
rem    %_T%candle.exe %_M%external_nocache.wxs -o %_BM%
rem    %_T%light.exe -b %_P% -b %_M% -b %_T% -o %_B%external_nocache\b.exe %_BM%external_nocache.wixobj
rem    )

IF NOT EXIST %_B%source_res\b.exe (
   md %_B%source_res\data
   %_T%candle.exe %_M%source_res.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% -ext WixBalExtension -b %_BP% -b %_M% -b %_S% -b %_T% -o %_B%source_res\b.exe %_BM%source_res.wixobj
   move %_B%source_res\Package3.msi %_B%source_res\data
   move %_B%source_res\pkg3.cab %_B%source_res\data
   )

IF NOT EXIST %_B%one_embedded\b.exe (
   md %_B%one_embedded
   %_T%candle.exe %_M%one_embedded.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%one_embedded\b.exe %_BM%one_embedded.wixobj
   )

IF NOT EXIST %_B%one_external\b.exe (
   md %_B%one_external
   %_T%candle.exe %_M%one_external.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%one_external\b.exe %_BM%one_external.wixobj
   )

IF NOT EXIST %_B%detached_container\b.exe (
   md %_B%detached_container
   %_T%candle.exe %_M%detached_container.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%detached_container\b.exe %_BM%detached_container.wixobj
   )

IF NOT EXIST %_B%3_ext\b.exe (
   md %_B%3_ext
   %_T%candle.exe %_M%3_ext.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%3_ext\b.exe %_BM%3_ext.wixobj
   )

IF NOT EXIST %_B%versions\v1addon2.exe (
   md %_B%versions
   %_T%candle.exe %_M%v1addon2.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%versions\v1addon2.exe %_BM%v1addon2.wixobj
   )

IF NOT EXIST %_B%burninburn\b.exe (
   md %_B%burninburn
   %_T%candle.exe %_M%child.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%burninburn\child.exe %_BM%child.wixobj
   %_T%candle.exe -ext WixUtilExtension %_M%burninburn.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -b %_B%burninburn -ext WixUtilExtension -o %_B%burninburn\b.exe %_BM%burninburn.wixobj
   )

:Web
IF NOT EXIST %_B%one_web\b.exe (
   md %_B%one_web\data
   %_T%candle.exe %_M%one_web.wxs -o %_BM%
   %_T%light.exe %_NOTIDY% %_BASEPATHS% -o %_B%one_web\b.exe %_BM%one_web.wixobj
   move %_B%one_web\Package3.msi %_B%one_web\data
   move %_B%one_web\pkg3.cab %_B%one_web\data
   )

:End
popd
endlocal