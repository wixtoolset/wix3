SET BUILD_TYPE=Release
if "%1" == "Debug" set BUILD_TYPE=Debug

REM Determine whether we are on an 32 or 64 bit machine
if "%PROCESSOR_ARCHITECTURE%"=="x86" if "%PROCESSOR_ARCHITEW6432%"=="" goto x86

set ProgramFilesPath=%ProgramFiles(x86)%

goto startInstall

:x86

set ProgramFilesPath=%ProgramFiles%

:startInstall

pushd "%~dp0"

@REM use the local build of wix
SET WIX_BUILD_LOCATION=%WIX_ROOT%\build\debug\x86
@REM if WIX_ROOT wasn't set, use the installed build of wix
if "%WIX_BUILD_LOCATION%" == "\build\debug\x86" SET WIX_BUILD_LOCATION=%ProgramFilesPath%\Windows Installer XML v3.5\bin

SET OUTPUTNAME=hello_world.msi

REM Cleanup leftover intermediate files

del /f /q "*.wixobj"
del /f /q "*.wixpdb"
del /f /q "%OUTPUTNAME%"

REM Build the MSI for the setup package

"%WIX_BUILD_LOCATION%\candle.exe" hello_world.wxs -out "hello_world.wixobj"
"%WIX_BUILD_LOCATION%\light.exe" "hello_world.wixobj" -sice:ICE91 -sice:ICE39 -cultures:en-US -ext "%WIX_BUILD_LOCATION%\WixUIExtension.dll" -ext "%WIX_BUILD_LOCATION%\WixUtilExtension.dll" -loc hello_world-en-us.wxl -out "%OUTPUTNAME%"

popd

