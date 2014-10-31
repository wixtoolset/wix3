---
title: How To: Install DirectX 9.0 With Your Installer
layout: documentation
---
# How To: Install DirectX 9.0 With Your Installer
Applications that require components from DirectX 9.0 can benefit from including the DirectX 9.0 Redistributable inside their installer. This simplifies the installation process for end users and ensures the required components for your application are always available on the target user&apos;s machine.

DirectX 9.0 can be re-distributed in several different ways, each of which is outlined in MSDN&apos;s <a href="http://msdn.microsoft.com/library/bb174600.aspx#DirectX_Redistribution" target="_blank">Installing DirectX with DirectSetup</a> article. This how to describes using the dxsetup.exe application to install DirectX 9.0 on a Vista machine assuming the application being installed only depends on a specific DirectX component.

Prior to redistributing the DirectX binaries you should read and understand the license agreement for the redistributable files. The license agreement can be found in the **Documentation\License Agreements\DirectX Redist.txt** file in your DirectX SDK installation.

## Step 1: Add the installer files to your WiX project
Adding the files to the WiX project follows the same process as described in [How To: Add a file to your installer](~/howtos/files_and_registry/add_a_file.html). The following example illustrates a typical fragment that includes the necessary files:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"APPLICATIONROOTDIRECTORY"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"DirectXRedistDirectory"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DirectX9.0c</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DirectXRedist</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"<a href="~/howtos/general/generate_guids.html">PUT-GUID-HERE</a>"</font><font size="2" color="#0000FF">&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DXSETUPEXE</font><font size="2">"</font>
<font size="2" color="#FF0000">           Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MySourceFiles\DirectXMinInstall\dxsetup.exe</font><font size="2">"
                  </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font>
<font size="2" color="#FF0000">           Checksum</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">dxupdate.cab</font><font size="2">"</font>
<font size="2" color="#FF0000">           Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2">MySourceFiles</font><font size="2" color="#0000FF">\DirectXMinInstall\dxupdate.cab</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">dxdllreg_x86.cab</font><font size="2">"</font>
<font size="2" color="#FF0000">           Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2">MySourceFiles</font><font size="2" color="#0000FF">\DirectXMinInstall\dxdllreg_x86.cab</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">dsetup32.dll</font><font size="2">"</font>
<font size="2" color="#FF0000">           Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2">MySourceFiles</font><font size="2" color="#0000FF">\DirectXMinInstall\dsetup32.dll</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">dsetup.dll"
                  </font><font size="2" color="#FF0000">Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MySourceFiles\DirectXMinInstall\dsetup.dll</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DEC2006_d3dx9_32_x86.cab</font><font size="2">"</font>
<font size="2" color="#FF0000">           Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MySourceFiles\DirectXMinInstall\DEC2006_d3dx9_32_x86.cab</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;/</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF">&gt;
    &lt;/</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF">&gt;

&lt;</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DirectXRedist</font><font size="2">"
         </font><font size="2" color="#FF0000">Title</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">!(loc.FeatureDirectX)</font><font size="2">"
         </font><font size="2" color="#FF0000">AllowAdvertise</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">no</font><font size="2">"</font>
<font size="2" color="#FF0000">  Display</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">hidden</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Level</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">ComponentRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DirectXRedist</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
&lt;/</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The files included are <a href="http://msdn.microsoft.com/library/bb219742.aspx" target="_blank">the minimal set of files</a> required by the DirectX 9.0 install process, as described in the MSDN documentation. The last file in the list, DEC2006\_d3dx9\_32\_x86.cab contains the specific DirectX component required by the installed application. These files are all included in a single component as, even in a patching situation, all the files must go together. A Feature element is used to create a feature specific to DirectX installation, and its Display attribute is set to **hidden** to prevent the user from seeing the feature in any UI that may be part of your installer.

## Step 2: Add a custom action to invoke the installer
To run the DirectX 9.0 installer a custom action is added that runs before the install is finalized. The [&lt;CustomAction&gt;](~/xsd/wix/customaction.html), [&lt;InstallExecuteSequence&gt;](~/xsd/wix/installexecutesequence.html) and [&lt;Custom&gt;](~/xsd/wix/custom.html) elements are used to create the custom action, as illustrated in the following sample.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">CustomAction</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">InstallDirectX"
              </font><font size="2" color="#FF0000">FileKey</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DXSETUPEXE</font><font size="2">"</font>
<font size="2" color="#FF0000">              ExeCommand</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">/silent</font><font size="2">"</font>
<font size="2" color="#FF0000">       Execute</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">deferred</font><font size="2">"
              </font><font size="2" color="#FF0000">Impersonate</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">no</font><font size="2">"</font>
<font size="2" color="#FF0000">       Return</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">check</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;

&lt;</font><font size="2" color="#A31515">InstallExecuteSequence</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Custom</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Action</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">InstallDirectX</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Before</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">InstallFinalize</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;![CDATA[</font><font size="2" color="#808080">NOT REMOVE</font><font size="2" color="#0000FF">]]&gt;
    &lt;/</font><font size="2" color="#A31515">Custom</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">InstallExecuteSequence</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The CustomAction element creates the custom action that runs the setup. It is given a unique id, and the FileKey attribute is used to reference the installer application from Step 1. The ExeCommand attribute adds the **/silent** flag to the installer to ensure the user is not presented with any DirectX installer user interface. The Execute attribute is set to deferred and the Impersonate attribute is set to no to ensure the custom action will run elevated, if necessary. The Return attribute is set to check to ensure the custom action runs synchronously.

The Custom element is used inside an InstallExecuteSequence to add the custom action to the actual installation process. The Action attribute references the CustomAction by its unique id. The Before attribute is set to InstallFinalize to run the custom action before the overall installation is complete. The condition prevents the DirectX installer from running when the user uninstalls your application, since DirectX components cannot be uninstalled.

## Step 3: Include progress text for the custom action
If you are using standard WiX UI dialogs you can include custom progress text for display while the DirectX installation takes place. The [&lt;UI&gt;](~/xsd/wix/ui.html) and [&lt;ProgressText&gt;](~/xsd/wix/progresstext.html) elements are used, as illustrated in the following example.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">ProgressText</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Action</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">InstallDirectX</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">Installing DirectX 9.0c</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">ProgressText</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The ProgressText element uses the Action attribute to reference the custom action by its unique id. The value of the ProgressText element is set to the display text for the install progress.
