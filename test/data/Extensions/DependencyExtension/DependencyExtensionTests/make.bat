@setlocal enableextensions

set PATH=%WIX_ROOT%\build\debug\x86;%PATH%

candle.exe A.wxs -out %~dp0Packages\A.wixobj -ext WixDependencyExtension
if "%errorlevel%" neq "0" exit /b 1

light.exe %~dp0Packages\A.wixobj -out %~dp0Packages\A.msi -ext WixDependencyExtension -sval
if "%errorlevel%" neq "0" exit /b 1

candle.exe B.wxs -out %~dp0Packages\B.wixobj -ext WixDependencyExtension
if "%errorlevel%" neq "0" exit /b 1

light.exe %~dp0Packages\B.wixobj -out %~dp0Packages\B.msi -ext WixDependencyExtension -sval
if "%errorlevel%" neq "0" exit /b 1
