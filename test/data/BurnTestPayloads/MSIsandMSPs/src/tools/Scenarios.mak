!ifndef PRODUCTCODE
!error PRODUCTCODE is not defined
!endif

MSIFLAGS = /qn DISABLEROLLBACK=1
CPYFLAGS = /njh /njs /xo
SCRFLAGS = //nologo

test: all

uninstall: 
	-mkdir test > nul 2>&1
	-msiexec.exe $(MSIFLAGS) /x "$(PRODUCTCODE)" /l*vx test\uninstall.log

# Apply the GDR and then the SP to supersede the GDR
test: scenario1
scenario1: rtm\product.msi gdr1\gdr1.msp sp1\sp1.msp
	@echo ---Scenario1---
	-mkdir test\scenario1 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario1\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\gdr1\gdr1.msp" /l*vx "test\scenario1\2.gdr1.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\sp1\sp1.msp" /l*vx "test\scenario1\3.sp1.log"

# Apply the GDR then the Baseliner to specific product to switch branches
test: scenario2
scenario2: rtm\product.msi gdr1\gdr1.msp rtmldr\rtmldr.msp
	@echo ---Scenario2---
	-mkdir test\scenario2 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario2\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\gdr1\gdr1.msp" /l*vx "test\scenario2\2.gdr1.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\rtmldr\rtmldr.msp" /l*vx "test\scenario2\3.rtmldr.log"

# Apply the GDR and LDR after the Baseliner is applied to specific product
test: scenario3
scenario3: rtm\product.msi rtmldr\rtmldr.msp gdr1\gdr1.msp ldr2\ldr2.msp
	@echo ---Scenario3---
	-mkdir test\scenario3 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario3\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\rtmldr\rtmldr.msp" /l*vx "test\scenario3\2.rtmldr.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\gdr1\gdr1.msp" /l*vx "test\scenario3\3.gdr1.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\ldr2\ldr2.msp" /l*vx "test\scenario3\4.ldr2.log"

# Apply the Baseliner and GDR to specific product
test: scenario4
scenario4: rtm\product.msi rtmldr\rtmldr.msp gdr1\gdr1.msp
	@echo ---Scenario4---
	-mkdir test\scenario4 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario4\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\rtmldr\rtmldr.msp;$(MAKEDIR)\gdr1\gdr1.msp" /l*vx "test\scenario4\2.rtmldr+gdr1.log"

# Slipstream the product, Baseliner, and GDR
test: scenario5
scenario5: rtm\product.msi rtmldr\rtmldr.msp gdr1\gdr1.msp 
	@echo ---Scenario5---
	-mkdir test\scenario5 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /update "$(MAKEDIR)\rtmldr\rtmldr.msp;$(MAKEDIR)\gdr1\gdr1.msp" /l*vx "test\scenario5\1.slipstream.log"

# Apply the Baseliner and LDR to specific product
test: scenario6
scenario6: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp
	@echo ---Scenario6---
	-mkdir test\scenario6 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario6\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" /update "$(MAKEDIR)\rtmldr\rtmldr.msp;$(MAKEDIR)\ldr2\ldr2.msp" /l*vx "test\scenario6\2.rtmldr+ldr2.log"

# Apply the patches to an admin images
test: scenario7
scenario7: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp gdr1\gdr1.msp
	@echo ---Scenario7---
	-mkdir test\scenario7 > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /a "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario7\1.admin.log" TARGETDIR="$(MAKEDIR)\test\scenario7"
	msiexec.exe $(MSIFLAGS) /a "$(MAKEDIR)\test\scenario7\product.msi" /update "$(MAKEDIR)\rtmldr\rtmldr.msp;$(MAKEDIR)\ldr2\ldr2.msp;$(GDR)" /l*vx "test\scenario7\2.adminpatch.log"

# Apply the Baseliner (no target ProductCodes) and LDR by enumerating target products using hotiron
test: scenario8
scenario8: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp
	@echo ---Scenario8---
	-mkdir test\scenario8 > nul 2>&1
	-3 robocopy.exe $(CPYFLAGS) rtmldr\ test\scenario8\ rtmldr.msp
	cscript.exe $(SCRFLAGS) tools\wisuminfo.vbs test\scenario8\rtmldr.msp 7 ""
	-3 robocopy.exe $(CPYFLAGS) ldr2 test\scenario8 ldr2.msp
	-3 robocopy.exe $(CPYFLAGS) /s tools\hotiron test\scenario8
	copy /y tools\scenarios\scenario8.xml test\scenario8\ParameterInfo.xml
	-del test\scenario8\2.patch.html > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario8\1.product.log"
	test\scenario8\hotfixinstaller.exe /q /norestart /log "$(MAKEDIR)\test\scenario8\2.patch.html"

# Apply the Baseliner (no target ProductCodes) and LDR by enumerating target products using ironspigot
test: scenario9
scenario9: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp
	@echo ---Scenario9---
	-mkdir test\scenario9 > nul 2>&1
	-3 robocopy.exe $(CPYFLAGS) rtmldr\ test\scenario9\ rtmldr.msp
	cscript.exe $(SCRFLAGS) tools\wisuminfo.vbs test\scenario9\rtmldr.msp 7 ""
	-3 robocopy.exe $(CPYFLAGS) ldr2 test\scenario9 ldr2.msp
	-3 robocopy.exe $(CPYFLAGS) /s tools\ironspigot test\scenario9
	copy /y tools\scenarios\Scenario9.xml test\scenario9\ParameterInfo.xml
	-del test\scenario9\2.patch.html > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario9\1.product.log"
	test\scenario9\spinstaller.exe /q /norestart /log "$(MAKEDIR)\test\scenario9\2.patch.html"

# Apply the Baseliner (fake target ProductCode) and LDR by enumerating target products using ironspigot
test: scenario10
scenario10: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp
	@echo ---Scenario10---
	-mkdir test\scenario10 > nul 2>&1
	-3 robocopy.exe $(CPYFLAGS) rtmldr\ test\scenario10\ rtmldr.msp
	cscript.exe $(SCRFLAGS) tools\wisuminfo.vbs test\scenario10\rtmldr.msp 7 "{35E540AE-E82B-4CEC-969A-A1F38F0DC6C0}"
	-3 robocopy.exe $(CPYFLAGS) ldr2 test\scenario10 ldr2.msp
	-3 robocopy.exe $(CPYFLAGS) /s tools\ironspigot test\scenario10
	copy /y tools\scenarios\Scenario10.xml test\scenario10\ParameterInfo.xml
	-del test\scenario10\2.patch.html > nul 2>&1
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario10\1.product.log"
	test\scenario10\spinstaller.exe /q /norestart /log "$(MAKEDIR)\test\scenario10\2.patch.html"

# Apply the Baseliner (fake target ProductCode) and LDR to specific product using PATCH=
test: scenario11
scenario11: rtm\product.msi rtmldr\rtmldr.msp ldr2\ldr2.msp
	@echo ---Scenario11---
	-mkdir test\scenario11 > nul 2>&1
	-3 robocopy.exe $(CPYFLAGS) rtmldr\ test\scenario11\ rtmldr.msp
	cscript.exe $(SCRFLAGS) tools\wisuminfo.vbs test\scenario11\rtmldr.msp 7 "{35E540AE-E82B-4CEC-969A-A1F38F0DC6C0}"
	@$(MAKE) /nologo uninstall
	msiexec.exe $(MSIFLAGS) /i "$(MAKEDIR)\rtm\product.msi" /l*vx "test\scenario11\1.product.log"
	msiexec.exe $(MSIFLAGS) /i "$(PRODUCTCODE)" PATCH="$(MAKEDIR)\test\scenario11\rtmldr.msp;$(MAKEDIR)\ldr2\ldr2.msp" /l*vx "test\scenario11\2.rtmldr+ldr2.log"
