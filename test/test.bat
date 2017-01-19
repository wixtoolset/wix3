@echo off
setlocal enableextensions enabledelayedexpansion

REM Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

set COMMAND=
set ENABLERUNTIMETESTS=
set EXITCODE=0
set FLAVOR=debug
set INTEGRATIONTESTS=
set WIX_TEST_ROOT=%~dp0
set WIX_ROOT=%WIX_TEST_ROOT:~0,-6%

:Parse_Args
if /i "%1"=="" goto :End_Parse_Args
if /i "%1"=="debug" set FLAVOR=debug& shift& goto :Parse_Args
if /i "%1"=="-debug" set FLAVOR=debug& shift& goto :Parse_Args
if /i "%1"=="/debug" set FLAVOR=debug& shift& goto :Parse_Args
if /i "%1"=="ship" set FLAVOR=release& shift& goto :Parse_Args
if /i "%1"=="-ship" set FLAVOR=release& shift& goto :Parse_Args
if /i "%1"=="/ship" set FLAVOR=release& shift& goto :Parse_Args
if /i "%1"=="-enableruntimetests" set ENABLERUNTIMETESTS=true& shift& goto :Parse_Args
if /i "%1"=="/enableruntimetests" set ENABLERUNTIMETESTS=true& shift& goto :Parse_Args
if /i "%1"=="-integrationtests" set INTEGRATIONTESTS=true& shift& goto :Parse_Args
if /i "%1"=="/integrationtests" set INTEGRATIONTESTS=true& shift& goto :Parse_Args
if /i "%1"=="-?" goto :Help
if /i "%1"=="/?" goto :Help
if /i "%1"=="-help" goto :Help
if /i "%1"=="/help" goto :Help

echo.
echo.Invalid argument: %1
goto :Help

:End_Parse_Args
set WIX_BUILD_X86=%WIX_ROOT%\build\%FLAVOR%\x86

REM Specify the test assemblies.
set TESTASSEMBLIES=!TESTASSEMBLIES! "%WIX_BUILD_X86%\WixTest.BurnIntegrationTests.dll"
set TESTASSEMBLIES=!TESTASSEMBLIES! "%WIX_BUILD_X86%\WixTest.MsbuildIntegrationTests.dll"

if not "%INTEGRATIONTESTS%"=="true" (
    set TESTASSEMBLIES=!TESTASSEMBLIES! "%WIX_BUILD_X86%\BurnUnitTest.dll"
    set TESTASSEMBLIES=!TESTASSEMBLIES! "%WIX_BUILD_X86%\DUtilUnitTest.dll"
    set TESTASSEMBLIES=!TESTASSEMBLIES! "%WIX_BUILD_X86%\WixTests.dll"
)

REM Enable runtime tests
if "%ENABLERUNTIMETESTS%"=="true" (
    set RuntimeTestsEnabled=true
)

for /f "usebackq" %%i in (`where /f xunit.console.clr4.*`) do (
    set COMMAND=%%i
)

if not "!COMMAND!"=="" (
    for %%i in (!TESTASSEMBLIES!) do (
        call !COMMAND! %%i
        if not "!EXITCODE!"=="0" set EXITCODE=%ERRORLEVEL%
    )
) else (
    call msbuild "%WIX_TEST_ROOT%test\All.testproj" /p:Configuration=%FLAVOR%
    if not "!EXITCODE!"=="0" set EXITCODE=%ERRORLEVEL%
)

goto :End

:Help
echo.
echo test.bat - Runs tests against the WiX core toolset
echo.
echo Usage: test.bat [debug^|ship] [options]
echo.
echo Options:
echo   flavor                Sets the flavor to either debug (default) or ship
echo   enableruntimetests    Enable runnning tests that can change the machine state
echo   integrationtests      Run only integration tests
echo   ?^|help               Shows this help
echo.
goto :End

:End
endlocal
exit /b !EXITCODE!
