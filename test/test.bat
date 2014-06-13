@echo off
setlocal enableextensions enabledelayedexpansion

REM -----------------------------------------------------------------------
REM  <copyright file="test.bat" company="Outercurve Foundation">
REM    Copyright (c) 2004, Outercurve Foundation.
REM    This software is released under Microsoft Reciprocal License (MS-RL).
REM    The license and further copyright text can be found in the file
REM    LICENSE.TXT at the root directory of the distribution.
REM  </copyright>
REM -----------------------------------------------------------------------

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

for /f "usebackq" %%i in (`where /f xunit.console.clr4.*`) do (
    set COMMAND=%%i
)

if not "!COMMAND!"=="" (
    for %%i in (!TESTASSEMBLIES!) do (
        call !COMMAND! %%i
        if not "!EXITCODE!"=="0" set EXITCODE=%ERRORLEVEL%
    )
) else (
    call msbuild "%~dp0\test\All.testproj" /p:Configuration=%FLAVOR%
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
