setlocal
set _FLAVOR=%1
if "%_FLAVOR%" == "" set _FLAVOR=debug
set _ROOT=%WIX_ROOT%\build\%_FLAVOR%\x86

%WINDIR%\Microsoft.NET\Framework\v3.5\MSBuild.exe %2 %3 %4 %5 %6 %7 %8 %9 /consoleloggerparameters:Verbosity=minimal /fileLogger /fileloggerparameters:Verbosity=diagnostic /t:Test /p:WixToolPath=%_ROOT% /p:WixTargetsPath=%_ROOT%\wix.targets /p:WixTasksPath=%_ROOT%\WixTasks.dll /p:LuxTargetsPath=%_ROOT%\lux.targets /p:LuxTasksPath=%_ROOT%\LuxTasks.dll 

endlocal
