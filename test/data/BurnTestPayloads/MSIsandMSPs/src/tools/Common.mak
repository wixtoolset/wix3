.SUFFIXES : .wxs .wixobj

FLAGS = -nologo -v
CFLAGS = $(FLAGS)
LFLAGS = $(FLAGS) -sval
TFLAGS = $(FLAGS) -xi -xo -t patch -p
PFLAGS = $(FLAGS) -sw1079 -sw1110
WIXTOOLSPATH = %WIX_ROOT%\build\debug\x86

.wxs.wixobj:
	$(WIXTOOLSPATH)\candle.exe $(CFLAGS) $< -out $@

.wixobj.msi:
	$(WIXTOOLSPATH)\light.exe $(LFLAGS) $** -out $@

.wixobj.wixmsp:
	$(WIXTOOLSPATH)\light.exe $(LFLAGS) $** -out $@

clean_intermediate:
	-del /s *.wixobj > nul 2>&1
	-del /s *.wixpdb > nul 2>&1
	-del /s *.wixmst > nul 2>&1
	-del /s *.wixmsp > nul 2>&1

clean: clean_intermediate
	-del /s *.msi > nul 2>&1
	-del /s *.msp > nul 2>&1
	-del /s *.cab > nul 2>&1
	-rmdir /q /s test\ > nul 2>&1

$(CABINET): all clean_intermediate
	cabarc -m LZX:21 -p -r N $(CABINET) * ..\tools\*
