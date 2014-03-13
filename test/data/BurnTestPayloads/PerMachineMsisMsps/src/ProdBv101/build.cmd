@REM Build v1.0.1.0 of Product B

call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v product.wxs -out product.wixobj
call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval product.wixobj -out prodBv101.msi

@REM Build v1.0.1.0 patch that targets Product B

call %WIX_ROOT%\build\debug\x86\torch.exe -nologo -v -xi -xo -t patch -p ..\prodBv100\prodBv100.wixpdb ..\prodBv101\prodBv101.wixpdb -out PatchBv101.wixmst
call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v patch.wxs -out patch.wixobj

call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval patch.wixobj -out patch.wixmsp
call %WIX_ROOT%\build\debug\x86\pyro.exe -nologo -v -sw1079 -sw1110 patch.wixmsp -out PatchBv101.msp -t rtmB PatchBv101.wixmst
