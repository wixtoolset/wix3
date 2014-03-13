---
title: How To: Run the Installed Application After Setup
layout: documentation
---
# How To: Run the Installed Application After Setup
Often when completing the installation of an application it is desirable to offer the user the option of immediately launching the installed program when setup is complete. This how to describes customizing the default WiX UI experience to include a checkbox and a WiX custom action to launch the application if the checkbox is checked.

This how to assumes you have already created a basic WiX project using the steps outlined in [How To: Add a file to your installer](~/howtos/files_and_registry/add_a_file.html).

## Step 1: Add the extension libraries to your project
This walkthrough requires WiX extensions for UI components and custom actions. These extension libraries must must be added to your project prior to use. If you are using WiX on the command-line you need to add the following to your candle and light command lines:

    -ext WixUIExtension -ext WixUtilExtension

If you are using Visual Studio you can add the extensions using the Add Reference dialog:

1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the **WixUIExtension.dll** assembly from the list and click Add
1. Select the **WixUtilExtension.dll** assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add UI to your installer
The WiX [Minimal UI](~/wixui/WixUI_dialog_library.html) sequence includes a basic set of dialogs that includes a finished dialog with optional checkbox. To include the sequence in your project add the following snippet anywhere inside the &lt;Product&gt; element.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">UIRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixUI_Minimal</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
&lt;/</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;</font>
</pre>
<p>To display the checkbox on the last screen of the installer include the following snippet anywhere inside the &lt;Product&gt; element:</p>
<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Launch My Application Name</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;</font>
</pre>

The WIXUI\_EXITDIALOGOPTIONALCHECKBOXTEXT property is provided by the standard UI sequence that, when set, displays the checkbox and uses the specified value as the checkbox label.

## Step 3: Include the custom action
Custom actions are included in a WiX project using the [&lt;CustomAction&gt;](~/xsd/wix/customaction.html) element. Running an application is accomplished with the WixShellExecTarget custom action. To tell Windows Installer about the custom action, and to set its properties, include the following in your project anywhere inside the &lt;Product&gt; element:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixShellExecTarget</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">[#myapplication.exe]</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
&lt;</font><font size="2" color="#A31515">CustomAction</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">LaunchApplication</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">BinaryKey</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixCA</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">DllEntry</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixShellExec</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Impersonate</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;</font>
</pre>

The Property element sets the WixShellExecTarget to the location of the installed application. WixShellExecTarget is the property Id the WixShellExec custom action expects will be set to the location of the file to run. The Value property uses the special # character to tell WiX to look up the full installed path of the file with the id myapplication.exe.

The CustomAction element includes the action in the installer. It is given a unique Id, and the BinaryKey and DllEntry properties indicate the assembly and entry point for the custom action. The Impersonate property tells Windows Installer to run the custom action as the installing user.

## Step 4: Trigger the custom action
Simply including the custom action, as in Step 3, isn&apos;t sufficient to cause it to run. Windows Installer must also be told when the custom action should be triggered. This is done by using the [&lt;Publish&gt;](~/xsd/wix/publish.html) element to add it to the actions run when the user clicks the Finished button on the final page of the UI dialogs. The Publish element should be included inside the &lt;UI&gt; element from Step 2, and looks like this:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Publish</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Dialog</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ExitDialog</font><font size="2">"
    </font><font size="2" color="#FF0000">Control</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Finish</font><font size="2">"</font><font size="2" color="#0000FF"> 
</font><font size="2" color="#FF0000">    Event</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DoAction</font><font size="2">"</font><font size="2" color="#0000FF"> 
    </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">LaunchApplication</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1 and NOT Installed</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">Publish</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The Dialog property specifies the dialog the Custom Action will be attached to, in this case the ExitDialog. The Control property specifies that the Finish button on the dialog triggers the custom action. The Event property indicates that a custom action should be run when the button is clicked, and the Value property specifies the custom action that was included in Step 3. The condition on the element prevents the action from running unless the checkbox from Step 2 was checked and the application was actually installed (as opposed to being removed or repaired).

## The Complete Sample
<pre>
<font size="2" color="#0000FF">&lt;?</font><font size="2" color="#A31515">xml</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">version</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1.0</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">encoding</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">UTF-8</font><font size="2">"</font><font size="2" color="#0000FF">?&gt;
&lt;&lt;</font><font size="2" color="#A31515">Wix</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">xmlns</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/2006/wi</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Product</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">*</font><font size="2">"</font>
<font size="2" color="#FF0000">             UpgradeCode</font><font size="2" color="#0000FF">=</font><font size="2">"PUT-GUID-HERE"
             </font><font size="2" color="#FF0000">Version</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1.0.0.0</font><font size="2">"</font>
<font size="2" color="#FF0000">             Language</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1033</font><font size="2">"
             </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Application Name</font><font size="2">"</font>
<font size="2" color="#FF0000">             Manufacturer</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Manufacturer Name</font><font size="2">"</font><font size="2" color="#0000FF">&gt;    
    &lt;</font><font size="2" color="#A31515">Package</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">InstallerVersion</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">300</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Compressed</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
    &lt;</font><font size="2" color="#A31515">Media</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Cabinet</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">myapplication.cab</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">EmbedCab</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;

    &lt;!--</font><font size="2" color="#008000"> The following three sections are from the How To: Add a File to Your Installer topic</font><font size="2" color="#0000FF">--&gt;
    &lt;</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">TARGETDIR</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">SourceDir</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ProgramFilesFolder</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
            &lt;</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">APPLICATIONROOTDIRECTORY</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Application Name</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;/</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF">&gt;
    &lt;/</font><font size="2" color="#A31515">Directory</font><font size="2" color="#0000FF">&gt;

    &lt;</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">APPLICATIONROOTDIRECTORY</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">myapplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"PUT-GUID-HERE"</font><font size="2" color="#0000FF">&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">myapplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MySourceFiles\MyApplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Checksum</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;/</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">documentation.html</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"PUT-GUID-HERE"</font><font size="2" color="#0000FF">&gt;
            &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">documentation.html</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Source</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MySourceFiles\documentation.html</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;/</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF">&gt;
    &lt;/</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF">&gt;

    &lt;</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MainApplication</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Title</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Main Application</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Level</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">ComponentRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">myapplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
        &lt;</font><font size="2" color="#A31515">ComponentRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">documentation.html</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
    &lt;/</font><font size="2" color="#A31515">Feature</font><font size="2" color="#0000FF">&gt;

    &lt;!--</font><font size="2" color="#008000"> Step 2: Add UI to your installer / Step 4: Trigger the custom action </font><font size="2" color="#0000FF">--&gt;
    &lt;</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">UIRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixUI_Minimal</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
        &lt;</font><font size="2" color="#A31515">Publish</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Dialog</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ExitDialog</font><font size="2">"</font><font size="2" color="#0000FF"> 
            </font><font size="2" color="#FF0000">Control</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Finish</font><font size="2">"</font><font size="2" color="#0000FF"> 
            </font><font size="2" color="#FF0000">Event</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">DoAction</font><font size="2">"</font><font size="2" color="#0000FF"> 
            </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">LaunchApplication</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">WIXUI_EXITDIALOGOPTIONALCHECKBOX = 1 and NOT Installed</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">Publish</font><font size="2" color="#0000FF">&gt;
    &lt;/</font><font size="2" color="#A31515">UI</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Launch My Application Name</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;

    &lt;!--</font><font size="2" color="#008000"> Step 3: Include the custom action </font><font size="2" color="#0000FF">--&gt;
    &lt;</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixShellExecTarget</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">[#myapplication.exe]</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
    &lt;</font><font size="2" color="#A31515">CustomAction</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">LaunchApplication</font><font size="2">"</font><font size="2" color="#0000FF"> 
</font><font size="2" color="#FF0000">        BinaryKey</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixCA</font><font size="2">"</font><font size="2" color="#0000FF"> 
        </font><font size="2" color="#FF0000">DllEntry</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">WixShellExec</font><font size="2">"
        </font><font size="2" color="#FF0000">Impersonate</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
    &lt;/</font><font size="2" color="#A31515">Product</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">Wix</font><font size="2" color="#0000FF">&gt;</font>
</pre>
