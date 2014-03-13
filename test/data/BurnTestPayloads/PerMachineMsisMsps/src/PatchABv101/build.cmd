@REM  Create a patch with 2 transforms.  
@REM  One installs PatchAv101 on Prod A
@REM  One installs PatchBv101 on Prod B
@REM  Call this single msp  PatchABv101.msp
@REM 
@REM Note, you must build these first:
@REM   ProdAv100
@REM   ProdAv101
@REM   ProdBv100
@REM   ProdBv101

call %WIX_ROOT%\build\debug\x86\torch.exe -nologo -v -xi -xo -t patch -p ..\ProdAv100\ProdAv100.wixpdb ..\ProdAv101\ProdAv101.wixpdb -out PatchABv101.wixmst
call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v patch.wxs -out patch.wixobj

call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval patch.wixobj -out patch.wixmsp
call %WIX_ROOT%\build\debug\x86\pyro.exe -nologo -v -sw1079 -sw1110 patch.wixmsp -out PatchABv101.msp -t rtmA ..\ProdAv101\PatchAv101.wixmst -t rtmB ..\ProdBv101\PatchBv101.wixmst
