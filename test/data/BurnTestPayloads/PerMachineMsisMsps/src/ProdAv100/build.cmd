call %WIX_ROOT%\build\debug\x86\candle.exe -nologo -v product.wxs -out product.wixobj
call %WIX_ROOT%\build\debug\x86\light.exe -nologo -v -sval product.wixobj -out prodAv100.msi