---
title: How To: Write a Registry Entry During Installation
layout: documentation
---
# How To: Write a Registry Entry During Installation
Writing registry entries during installation is similar to writing files during installation. You describe the registry hierarchy you want to write into, specify the registry values to create, then add the component to your feature list.

## Step 1: Describe the registry layout and values
The following example illustrates how to write two registry entries, one to a specific value and the other to the default value.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">TARGETDIR</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">RegistryEntries</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"<a href="~/howtos/general/generate_guids.html">PUT-GUID-HERE</a>"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">RegistryKey</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Root</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">HKCU</font><font size="2">"
                     </font><font size="2" color="#FF0000">Key</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Software\Microsoft\MyApplicationName</font><font size="2">"</font>
<font size="2" color="#FF0000">              Action</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">createAndRemoveOnUninstall</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
            &lt;</font><font size="2" color="#A31515">RegistryValue</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Type</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">integer</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">SomeIntegerValue</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
            &lt;</font><font size="2" color="#A31515">RegistryValue</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Type</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">string</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Value</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Default Value</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
        &lt;/</font><font size="2" color="#A31515">RegistryKey</font><font size="2" color="#0000FF">&gt;
    &lt;/</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">DirectoryRef</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The snippet begins with a DirectoryRef that points to the <a href="http://msdn.microsoft.com/library/aa372064.aspx" target="_blank">TARGETDIR</a> directory defined by Windows Installer. This effectively means the registry entries should be installed to the target user&apos;s machine. Under the DirectoryRef is a Component element that groups together the registry entries to be installed. The component is given an id for reference later in the WiX project and a unique guid.

The registry entries are created by first using the [&lt;RegistryKey&gt;](~/xsd/wix/registrykey.html) element to specify where in the registry the values should go. In this example the key is under **HKEY\_CURRENT\_USER\Software\Microsoft\MyApplicationName**. The optional Action attribute is used to tell Windows Installer that the key should be created (if necessary) on install, and that the key and all its sub-values should be removed on uninstall.

Under the RegistryKey element the [&lt;RegistryValue&gt;](~/xsd/wix/registryvalue.html) element is used to create the actual registry values. The first is the SomeIntegerValue value, which is of type integer and has a value of 1. It is also marked as the KeyPath for the component, which is used by the Windows Installer to determine whether this component is installed on the machine. The second RegistryValue element sets the default value for the key to a string value of Default Value.

The id attribute is omitted on the RegistryKey and RegistryValue elements because there is no need to refer to these items elsewhere in the WiX project file. WiX will auto-generate ids for the elements based on the registry key, value, and parent component name.

## Step 2: Tell Windows Installer to install the entries
After defining the directory structure and listing the registry entries to package into the installer, the last step is to tell Windows Installer to actually install the registry entry. The [&lt;Feature&gt;](~/xsd/wix/feature.html) element is used to do this. The following snippet adds a reference to the registry entries component, and should be inserted inside a parent Feature element.

<pre>
<font size="2" color="#A31515">&lt;ComponentRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"RegistryEntries"</font><font size="2" color="#0000FF"> /&gt;</font>
</pre>

The [&lt;ComponentRef&gt;](~/xsd/wix/componentref.html) element is used to reference the component created in Step 1 via the Id attribute.
