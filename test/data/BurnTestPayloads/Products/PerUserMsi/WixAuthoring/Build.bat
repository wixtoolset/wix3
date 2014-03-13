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

SET WIX_BUILD_LOCATION=%ProgramFilesPath%\Windows Installer XML v3.5\bin
SET OUTPUTNAME=hello_world_non_admin.msi

REM Cleanup leftover intermediate files

del /f /q "*.wixobj"
del /f /q "%OUTPUTNAME%"

REM Build the MSI for the setup package

"%WIX_BUILD_LOCATION%\candle.exe" hello_world_non_admin.wxs -out "hello_world_non_admin.wixobj"
"%WIX_BUILD_LOCATION%\light.exe" "hello_world_non_admin.wixobj" -sice:ICE91 -sice:ICE39 -cultures:en-US -ext "%ProgramFilesPath%\Windows Installer XML v3.5\bin\WixUIExtension.dll" -ext "%ProgramFilesPath%\Windows Installer XML v3.5\bin\WixUtilExtension.dll" -loc hello_world_non_admin-en-us.wxl -out "%OUTPUTNAME%"

popd

