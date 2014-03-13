---
title: How To: Install the Visual C++ Redistributable with your installer
layout: documentation
---
# How To: Install the Visual C++ Redistributable with your installer
If your application depends on the Visual C++ runtimes you can include them as part of your installer to simplify the installation experience for your end users. This how to describes including the Visual C++ runtime merge modules into your installer and explains the expected ICE warnings you will see.

## Step 1: Obtain the correct Visual C++ runtime merge modules
The Visual C++ runtime merge modules are installed with Visual Studio and are located in **\Program Files\Common Files\Merge Modules**. The Visual C++ 8.0 runtime file is **Microsoft\_VC80\_CRT\_x86.msm**. This same MSM is used for the Visual C++ 8.0 SP1 runtime, however it is updated in place by the Visual Studio 2005 SP1 installer. The Visual Studio 9.0 runtime file is **Microsoft\_VC90\_CRT\_x86.msm**. There is generally no need to include the policy MSMs as part of the installation.

## Step 2: Include the merge module in your installer
To include the merge module in your installer use the [&lt;Merge&gt;](~/xsd/wix/merge.html) and [&lt;MergeRef&gt;](~/xsd/wix/mergeref.html) elements. The following example illustrates how these elements are used.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"TARGETDIR"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Merge</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">VCRedist</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">SourceFile</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2">MySourceFiles</font><font size="2" color="#0000FF">\Microsoft_VC80_CRT_x86.msm</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">DiskId</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Language</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">0</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
&lt;/</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF">&gt;</font>
</pre>

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">VCRedist</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Title</font><font size="2" color="#0000FF">=</font><font size="2">"Visual C++ 8.0 Runtime"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">AllowAdvertise</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">no</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Display</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">hidden</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Level</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">MergeRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">VCRedist</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
&lt;/</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The Merge element ensures the merge module is included in the final Windows Installer package. A unique id is assigned using the Id attribute. The SourceFile attribute points to the location of the merge module on your machine. The DiskId attribute should match the DiskId specified in your project&apos;s Media element. The Language attribute should always be 0.

The MergeRef element is used within a Feature element to actually install the merge module. In the example above a feature specific to the runtime is created and marked as hidden to prevent it from displaying in any UI your installer may use. The MergeRef refers to the merge module by its unique id.

## A note about ICE warnings
Including the Visual C++ Runtime merge module in your installer will result in the following ICE warnings:

    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.762.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.100.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.101.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.103.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.104.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Component, Column: KeyPath, Key(s): downlevel_manifest.8.0.50727.193.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.100.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.101.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.103.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.104.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.193.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE03: String overflow (greater than length permitted in column); Table: Registry, Column: Registry, Key(s): reg_downlevel_manifest.8.0.50727.762.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E
    light.exe(0,0): warning LGHT1076: ICE25: Possible dependency failure as we do not find CRT.Policy.63E949F6_03BC_5C40_FF1F_C8B3B9A1E18E@0 v in ModuleSignature table
    light.exe(0,0): warning LGHT1076: ICE82: This action SystemFolder.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E has duplicate sequence number 1 in the table InstallExecuteSequence
    light.exe(0,0): warning LGHT1076: ICE82: This action SystemFolder.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E has duplicate sequence number 1 in the table InstallUISequence
    light.exe(0,0): warning LGHT1076: ICE82: This action SystemFolder.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E has duplicate sequence number 1 in the table AdminExecuteSequence
    light.exe(0,0): warning LGHT1076: ICE82: This action SystemFolder.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E has duplicate sequence number 1 in the table AdminUISequence
    light.exe(0,0): warning LGHT1076: ICE82: This action SystemFolder.98CB24AD_52FB_DB5F_FF1F_C8B3B9A1E18E has duplicate sequence number 1 in the table AdvtExecuteSequence

These warnings are expected and are due to how the Visual C++ merge modules were authored. For more details see <a href="http://blogs.msdn.com/astebner/archive/2007/02/13/building-an-msi-using-wix-v3-0-that-includes-the-vc-8-0-runtime-merge-modules.aspx" target="_blank">Aaron Stebner&apos;s blog entry</a>.
