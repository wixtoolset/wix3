@REM Build v1.0.1.0 of Product A

call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v product.wxs -out product.wixobj
call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval product.wixobj -out prodAv101.msi

@REM Build v1.0.1.0 patch that targets Product A

call %WIX_ROOT%\build\debug\x86\torch.exe -nologo -v -xi -xo -t patch -p ..\ProdAv100\ProdAv100.wixpdb ..\ProdAv101\ProdAv101.wixpdb -out PatchAv101.wixmst
call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v patch.wxs -out patch.wixobj

call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval patch.wixobj -out patch.wixmsp
call %WIX_ROOT%\build\debug\x86\pyro.exe -nologo -v -sw1079 -sw1110 patch.wixmsp -out PatchAv101.msp -t rtmA PatchAv101.wixmst
