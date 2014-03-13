@echo off
set CURRENTDIR=%~dp0

rem This file will run the Wix Tests

REM -- attempt to find MSTEST ---
rem Use MSTest from VS 2010

if %PROCESSOR_ARCHITECTURE% == AMD64 set MSTESTPFILES=%ProgramFiles(x86)%
if %PROCESSOR_ARCHITECTURE% == IA64 set MSTESTPFILES=%ProgramFiles(x86)%
if %PROCESSOR_ARCHITECTURE% == x86 set MSTESTPFILES=%ProgramFiles%

pushd %MSTESTPFILES%
if "%MSTEST%" == "" if exist "Microsoft Visual Studio 10.0\Common7\IDE\mstest.exe" set MSTEST=%MSTESTPFILES%\Microsoft Visual Studio 10.0\Common7\IDE\mstest.exe
popd

REM -- Find MSTest.exe ---
if "%MSTEST%" == "" goto :NoMSTest

setlocal
set EXITCODE=0
set FLAVOR=debug
set ENABLERUNTIMETESTS=false
set MEASURECODECOVERAGE=false
set RESULTSFILE=
set RUNCONFIGFILE=
set TESTS=
set TESTSLIST=
set VERBOSE=false

REM -- parse the arguments ---

:Parse_Args
if /i [%1]==[] goto :End_Parse_Args
if /i [%1]==[debug] set FLAVOR=%1& shift& goto :Parse_Args
if /i [%1]==[ship] set FLAVOR=%1& shift& goto :Parse_Args
if /i [%1]==[-enableruntimetests] set ENABLERUNTIMETESTS=true& shift& goto :Parse_Args
if /i [%1]==[/enableruntimetests] set ENABLERUNTIMETESTS=true& shift& goto :Parse_Args
if /i [%1]==[-measurecodecoverage] set MEASURECODECOVERAGE=true& shift& goto :Parse_Args
if /i [%1]==[/measurecodecoverage] set MEASURECODECOVERAGE=true& shift& goto :Parse_Args
if /i [%1]==[-all] set TESTS=%TESTS% /test:*& shift& goto :Parse_Args
if /i [%1]==[/all] set TESTS=%TESTS% /test:*& shift& goto :Parse_Args
if /i [%1]==[all] set TESTS=%TESTS% /test:*& shift& goto :Parse_Args
if /i [%1]==[-resultsfile] set RESULTSFILE=/resultsfile:"%2" & shift& shift& goto :Parse_Args
if /i [%1]==[/resultsfile] set RESULTSFILE=/resultsfile:"%2" & shift& shift& goto :Parse_Args
if /i [%1]==[-smoke] set TESTS=/test:WixTest.Tests.QTests& shift& goto :Parse_Args
if /i [%1]==[/smoke] set TESTS=/test:WixTest.Tests.QTests& shift& goto :Parse_Args
if /i [%1]==[smoke] set TESTS=/test:WixTest.Tests.QTests& shift& goto :Parse_Args
if /i [%1]==[-testlist] set TESTLIST=%2& shift& shift& goto :Parse_Args
if /i [%1]==[/testlist] set TESTLIST=%2& shift& shift& goto :Parse_Args
if /i [%1]==[-test] set TESTS=%TESTS% /test:%2 & shift& shift& goto :Parse_Args
if /i [%1]==[/test] set TESTS=%TESTS% /test:%2 & shift& shift& goto :Parse_Args
if /i [%1]==[-area] set TESTS=%TESTS% /test:WixTest.Tests.%2 & shift& shift& goto :Parse_Args
if /i [%1]==[/area] set TESTS=%TESTS% /test:WixTest.Tests.%2 & shift& shift& goto :Parse_Args
if /i [%1]==[-areas] goto :Areas
if /i [%1]==[/areas] goto :Areas
if /i [%1]==[-v] set VERBOSE=true & shift& goto :Parse_Args
if /i [%1]==[/v] set VERBOSE=true & shift& goto :Parse_Args
if /i [%1]==[-?] goto :Help
if /i [%1]==[/?] goto :Help
if /i [%1]==[-help] goto :Help
if /i [%1]==[/help] goto :Help

echo.
echo.Invalid argument: '%1'
goto :Help

:End_Parse_Args
REM -- Resolve WixToolPath ---
if "%WIX_ROOT%" == "" set WIX_ROOT=%CURRENTDIR%..
if "%WixTargetsPath%" == "" set WixTargetsPath=%WIX_ROOT%\build\%FLAVOR%\x86\wix.targets
if "%WixTasksPath%" == "" set WixTasksPath=%WIX_ROOT%\build\%FLAVOR%\x86\WixTasks.dll
if "%WixTestMSBuildDirectory%" == "" set WixTestMSBuildDirectory=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319

set WIXTOOLSPATH=%WIX_ROOT%\build\%FLAVOR%\x86

REM -- Attempt to find the tests ---
set WIXTESTS=%WIXTOOLSPATH%\WixTests.dll
set BURNUNITTESTS=%WIXTOOLSPATH%\BurnUnitTest.dll
if not exist %WIXTESTS% goto :NoWixTests

REM -- Set test container ---
set TESTCONTAINERS=/testcontainer:"%WIXTESTS%"
if exist %BURNUNITTESTS% set TESTCONTAINERS= %TESTCONTAINERS% /testcontainer:"%BURNUNITTESTS%"


REM -- Select run.config ---
if %MEASURECODECOVERAGE%==true ( 
  set RUNCONFIGFILE=/runconfig:"%WIX_ROOT%\test\src\WixTestsCodeCoverage.testrunconfig"
) else (
  set RUNCONFIGFILE=/runconfig:"%WIX_ROOT%\test\src\WixTests.testrunconfig"
)

REM -- Enable/disable Runtime tests ---
if %ENABLERUNTIMETESTS%==true (
  set RuntimeTestsEnabled=true
) else (
  set RuntimeTestsEnabled=false
)

REM -- Load Test Lists --
setlocal ENABLEDELAYEDEXPANSION
if not "%TESTLIST%" == "" (
  if not exist %TESTLIST% ( goto :NoTESTLIST )
  
  REM -- Open the file and read the tests --
  for /F "usebackq" %%i in (%TESTLIST%) do ( 
    set TESTS=!TESTS! /test:%%i 
  )
)


REM -- Set Details --
if %VERBOSE% == true (
  set DETAIL=/detail:errormessage /detail:errorstacktrace /detail:stderr /detail:stdout
) 

REM -- change directory to wix tests ---
pushd %WIX_ROOT%\test

REM -- Display test settings ---
echo.
echo Setting the test environment
echo   Flavor         :   %flavor%
echo   Wix Tools Path :   %WIXTOOLSPATH%
echo   MSTest Location:   %MSTEST%
echo   Runconfig File :   %RUNCONFIGFILE%
echo   Code Coverage  :   %MEASURECODECOVERAGE%
echo   Runtime Tests  :   %RuntimeTestsEnabled%
echo   Test List File :   %TESTLIST%
echo   Verbose Output :   %VERBOSE%

if %VERBOSE% == true (
  echo   MSTest Command :   %RUNCONFIGFILE% 
  echo                      %TESTCONTAINERS% 
  echo                      %TESTS%
  echo                      %DETAIL%
  echo                      %RESULTSFILE%
)

REM -- call mstest and capture the output ---
echo.
echo Backing up dependency registration to Dependencies.hiv
reg.exe save HKLM\Software\Classes\Installer\Dependencies Dependencies.hiv /y > nul 2>&1

echo.
echo Running tests

@echo on
call "%MSTEST%" %RUNCONFIGFILE% %TESTCONTAINERS% %TESTS% %DETAIL% %RESULTSFILE%
@echo off

REM -- findout the appropriate exit code ---
REM -- if "Test Run Failed." is displayed then it failed ---
find "Test Run Failed." mstestoutput.txt > nul
if NOT ERRORLEVEL 1 goto :TestRunFailed

REM -- else if "Test Run Completed." is not displayed, e.g. the test run was not complete then it failed ---
find "Test Run Completed." mstestoutput.txt > nul
if NOT ERRORLEVEL 1 goto :TestRunPassed
goto :TestRunFailed


REM -- Test Run Failed ---
:TestRunFailed
echo.
echo -------------------------------
echo   Test Run Result:   Failed
echo -------------------------------
echo.
set EXITCODE=2
goto :Cleanup

REM -- Test Run Completed Successfully ---
:TestRunPassed
echo.
echo Restoring dependency registration
if exist "Dependencies.hiv" reg.exe restore HKLM\Software\Classes\Installer\Dependencies Dependencies.hiv > nul 2>&1
if errorlevel 0 del /q Dependencies.hiv

echo.
echo -------------------------------
echo   Test Run Result:   Passed
echo -------------------------------
echo.
set EXITCODE=0
goto :Cleanup

REM -- MSTest was not found ---
:NoMSTest
echo.
echo MSTest.exe version 10.0 could not be found. To run the tests, you must manually set the MSTEST environment variable to the full path of MSTest.exe.
set EXITCODE=1
goto :Cleanup

REM -- The test binaries were not found ---
:NoWixTests
echo.
echo The test binaries at %WIXTESTS% were not found. They must be built with the WiX binaries and require the Visual Studio Test Tools to be installed on your machine.
set EXITCODE=1
goto :Cleanup

REM -- The test list provided does not exist ---
:NoTESTLIST
echo.
echo The test list specified '%TESTLIST% does not exist. Please specify a valid test list.
set EXITCODE=1
goto :Cleanup

REM -- Help text ---
:Help
echo.
echo test.bat - Runs tests against the WiX core toolset
echo.
echo Usage: test.bat [debug^|ship] [options] [test1 test2 ...]
echo.
echo Options:
echo   flavor                Sets the flavor to either debug (default) or ship
echo   -all                  Runs all of the tests
echo   -smoke                Runs the smoke tests
echo   -area ^<area name^>     Specifies the area to run tests for. 
echo                         Examples of possible values are: 
echo                           burn, wixproj, examples
echo                           tools.candle, tools.dark, tools.light
echo                           tools.canlde.input, tools.light.cabs
echo                           integration, qtests
echo   -areas                For list of possible test areas. 
echo   -test ^<test name^>     Runs a specific test
echo   -enableruntimetests   Enable runnning tests that can change the machine state
echo                         e.g. installing an msi
echo   -measurecodecoverage  Measure code coverage for tests
echo   -testlist ^<testlist^>  Run a specified test list
echo   -resultsfile ^<file^>   Specifies the location of the results file
echo   -v                    Displays verbose output
echo   -?                    Shows this help
echo.
goto :Cleanup

REM -- Test Areas text ---
:Areas
echo.
echo List of test areas defined in the WiX test solution: 
echo.
echo    Burn 
echo    Burn.BurnServicingBurn 
echo    Burn.CommonTestFixture 
echo    Burn.Condition 
echo    Burn.Downloader 
echo    Burn.EndToEnd 
echo    Burn.ExternalFiles 
echo    Burn.Manifest 
echo    Burn.MSIOption 
echo    Burn.RebootResume 
echo    Burn.Searches 
echo    Burn.Variables 
echo    Examples 
echo    Extensions
echo    Extensions.DependencyExtension
echo    Extensions.IISExtension 
echo    Extensions.NetFXExtension 
echo    Extensions.SqlExtension 
echo    Extensions.UIExtension 
echo    Extensions.UtilExtension 
echo    Extensions.VSExtension 
echo    Integration.BuildingPackages 
echo    Integration.BuildingPackages.Authoring 
echo    Integration.BuildingPackages.Binaries 
echo    Integration.BuildingPackages.Bundle 
echo    Integration.BuildingPackages.Components 
echo    Integration.BuildingPackages.Conditions 
echo    Integration.BuildingPackages.CustomActions 
echo    Integration.BuildingPackages.CustomTables 
echo    Integration.BuildingPackages.Directories 
echo    Integration.BuildingPackages.Features 
echo    Integration.BuildingPackages.Files 
echo    Integration.BuildingPackages.InstallPackages 
echo    Integration.BuildingPackages.InstanceTransforms 
echo    Integration.BuildingPackages.Media 
echo    Integration.BuildingPackages.Permissions 
echo    Integration.BuildingPackages.Sequencing 
echo    Integration.BuildingPackages.Shortcuts 
echo    Integration.BuildingPackages.SymbolPaths 
echo    Integration.BuildingPackages.UI 
echo    QTests
echo    QTests.Heat
echo    QTests.Localize
echo    QTests.Merge
echo    QTests.MSBuild
echo    QTests.Patch
echo    QTests.Smoke
echo    QTests.UI
echo    QTests.VS
echo    Tools.Candle 
echo    Tools.Common
echo    Tools.Dark 
echo    Tools.Light 
echo    Tools.Lit
echo    Wixproj 
echo.
goto :Cleanup

REM -- Cleanup at the end of the script ---
:Cleanup
REM -- revert directory ---
popd

endlocal & Exit /B %EXITCODE%
