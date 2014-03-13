# Dual-branch Servicing

CABINET = Single.cab
PRODUCTCODE = {FB94421B-7FA3-4495-A9D7-212099C19147}

all: rtm\product.msi rtmldr\rtmldr.msp gdr1\gdr1.msp ldr2\ldr2.msp ldr3\ldr3.msp sp1\sp1.msp

!include <tools\Common.mak>
!include <tools\Scenarios.mak>

# RTM
rtm\product.wixobj: rtm\product.wxs
rtm\product.msi: rtm\product.wixobj

# LDR Baseline
rtmldr\product.wixobj: rtmldr\product.wxs
rtmldr\product.msi: rtmldr\product.wixobj
rtmldr\rtmldr.wixmst: rtm\product.msi rtmldr\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtm\product.wixpdb rtmldr\product.wixpdb -out rtmldr\rtmldr.wixmst
rtmldr\patch.wixobj: rtmldr\patch.wxs
rtmldr\patch.wixmsp: rtmldr\patch.wixobj
rtmldr\rtmldr.msp: rtmldr\rtmldr.wixmst rtmldr\patch.wixmsp
	$(WIXTOOLSPATH)\pyro.exe $(PFLAGS) rtmldr\patch.wixmsp -out rtmldr\rtmldr.msp -t rtm rtmldr\rtmldr.wixmst

# LDR1
ldr1\product.wixobj: ldr1\product.wxs
ldr1\product.msi: ldr1\product.wixobj
ldr1\ldr1.wixmst: rtmldr\product.msi ldr1\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtmldr\product.wixpdb ldr1\product.wixpdb -out ldr1\ldr1.wixmst

# GDR1
gdr1\product.wixobj: gdr1\product.wxs
gdr1\product.msi: gdr1\product.wixobj
gdr1\gdr1.wixmst: rtm\product.msi gdr1\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtm\product.wixpdb gdr1\product.wixpdb -out gdr1\gdr1.wixmst
gdr1\patch.wixobj: gdr1\patch.wxs
gdr1\patch.wixmsp: gdr1\patch.wixobj
gdr1\gdr1.msp: ldr1\ldr1.wixmst gdr1\gdr1.wixmst gdr1\patch.wixmsp
	$(WIXTOOLSPATH)\pyro.exe $(PFLAGS) gdr1\patch.wixmsp -out gdr1\gdr1.msp -t rtmldr ldr1\ldr1.wixmst -t rtm gdr1\gdr1.wixmst

# LDR2
ldr2\product.wixobj: ldr2\product.wxs
ldr2\product.msi: ldr2\product.wixobj
ldr2\ldr2.wixmst: rtmldr\product.msi ldr2\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtmldr\product.wixpdb ldr2\product.wixpdb -out ldr2\ldr2.wixmst
ldr2\patch.wixobj: ldr2\patch.wxs
ldr2\patch.wixmsp: ldr2\patch.wixobj
ldr2\ldr2.msp: ldr2\ldr2.wixmst ldr2\patch.wixmsp
	$(WIXTOOLSPATH)\pyro.exe $(PFLAGS) ldr2\patch.wixmsp -out ldr2\ldr2.msp -t rtmldr ldr2\ldr2.wixmst

# LDR3
ldr3\product.wixobj: ldr3\product.wxs
ldr3\product.msi: ldr3\product.wixobj
ldr3\ldr3.wixmst: rtmldr\product.msi ldr3\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtmldr\product.wixpdb ldr3\product.wixpdb -out ldr3\ldr3.wixmst
ldr3\patch.wixobj: ldr3\patch.wxs
ldr3\patch.wixmsp: ldr3\patch.wixobj
ldr3\ldr3.msp: ldr3\ldr3.wixmst ldr3\patch.wixmsp
	$(WIXTOOLSPATH)\pyro.exe $(PFLAGS) ldr3\patch.wixmsp -out ldr3\ldr3.msp -t rtmldr ldr3\ldr3.wixmst

# SP1
sp1\product.wixobj: sp1\product.wxs
sp1\product.msi: sp1\product.wixobj
sp1\sp1.wixmst: rtm\product.msi sp1\product.msi
	$(WIXTOOLSPATH)\torch.exe $(TFLAGS) rtm\product.wixpdb sp1\product.wixpdb -out sp1\sp1.wixmst
sp1\patch.wixobj: sp1\patch.wxs
sp1\patch.wixmsp: sp1\patch.wixobj
sp1\sp1.msp: sp1\sp1.wixmst sp1\patch.wixmsp
	$(WIXTOOLSPATH)\pyro.exe $(PFLAGS) sp1\patch.wixmsp -out sp1\sp1.msp -t rtm sp1\sp1.wixmst
