---
title: How To: Create a Shortcut to a Webpage
layout: documentation
after: create_start_menu_shortcut
---
# How To: Create a Shortcut to a Webpage
WiX provides support for creating shortcuts to Internet sites as part of the install process. This how to demonstrates referencing the necessary utility library and adding an Internet shortcut to your installer. It assumes you have already followed the steps in the [How To: Create a shortcut on the Start Menu](create_start_menu_shortcut.html).

## Step 1: Add the WiX Utility extensions library to your project
The WiX support for Internet shortcuts is included in a WiX extension library that must be added to your project prior to use. If you are using WiX on the command-line you need to add the following to your candle and light command lines:

    -ext WiXUtilExtension

If you are using WiX in Visual Studio you can add the extensions using the Add Reference dialog:

1. Open your WiX project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the **WixUtilExtension.dll** assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add the WiX Utility extensions namespace to your project
Once the library is added to your project, you need to add the Utility extensions namespace to your project so you can access the appropriate WiX elements. To do this modify the top-level [&lt;Wix&gt;](~/xsd/wix/wix.html) element in your project by adding the following attribute:

<pre>
<font size="2" color="#FF0000">xmlns:util</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/UtilExtension</font><font size="2">"</font>
</pre>

A complete Wix element with the standard namespace and the Utility extensions namespace added looks like this:

<pre>
<font size="2" color="#0000FF">
&lt;</font><font size="2" color="#A31515">Wix</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">xmlns</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/2006/wi</font><font size="2">"
</font>  <font size="2" color="#FF0000">   xmlns:util</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/UtilExtension</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font>
</pre>

## Step 3: Add the Internet shortcut to your installer package

Internet shortcuts are created using the [&lt;Util:InternetShortcut&gt;](~/xsd/util/internetshortcut.html) element. The following example adds an InternetShortcut element to the existing shortcut creation example from [How To: Create a shortcut on the Start Menu](create_start_menu_shortcut.html).

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ApplicationProgramsFolder</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ApplicationShortcut</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"<a href="~/howtos/general/generate_guids.html">PUT-GUID-HERE</a>"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Shortcut</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ApplicationStartMenuShortcut</font><font size="2">"</font><font size="2" color="#0000FF"> 
                  </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Application Name</font><font size="2">"</font><font size="2" color="#FF0000">
                  Description</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Application Description</font><font size="2">"
                  </font><font size="2" color="#FF0000">Target</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">[#MyApplicationExeFileId]"
                  </font><font size="2" color="#FF0000">WorkingDirectory</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">APPLICATIONROOTDIRECTORY</font><font size="2">"</font>/&gt;
        &lt;<font size="2" color="#A31515">util:InternetShortcut</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">OnlineDocumentationShortcut</font><font size="2">"</font>
<font size="2">                        </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">My Online Documentation</font><font size="2">"
                               </font><font size="2" color="#FF0000">Target</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://wixtoolset.org/</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;</font><font size="2" color="#A31515">RemoveFolder</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ApplicationProgramsFolder</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">On</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">uninstall</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;</font><font size="2" color="#A31515">RegistryValue</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Root</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">HKCU</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Key</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Software\MyCompany\MyApplicationName</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">installed</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Type</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">integer</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
    &lt;/</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The InternetShortcut is given a unique id with the Id attribute. in this case the application's Start Menu folder. The Name attribute specifies the name of the shortcut on the Start Menu. The Target attribute specifies the destination address for the shortcut. The [&lt;DirectoryRef&gt;](~/xsd/wix/directoryref.html) element is used to refer to the directory structure already defined by the project file. By referencing the ApplicationProgramsFolder directory the shortcut will be installed into the user's Start Menu inside the My Application Name folder.
