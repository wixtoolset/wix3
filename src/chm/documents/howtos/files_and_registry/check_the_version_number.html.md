---
title: How To: Check the Version Number of a File During Installation
layout: documentation
---
# How To: Check the Version Number of a File During Installation
Installers often need to look up the version number of a file on disk during the installation process. The check is often used in advance of a conditional statement later in install, such as to block the user from installing if a file is missing, or to display custom installation UI depending on whether the file version is high enough. This how to demonstrates verifying the version of a file on disk, then using the resulting property to block the application&apos;s installation if the file version is lower than expected.

## Step 1: Determine the version of the file
File versions are determined using the [&lt;Property&gt;](~/xsd/wix/property.html), [&lt;DirectorySearch&gt;](~/xsd/wix/directorysearch.html) and [&lt;FileSearch&gt;](~/xsd/wix/filesearch.html) elements. The following snippet looks for the user32.dll file in the machine&apos;s System32 directory and checks to see if it is at least version 6.0.6001.1751.

<pre>
&lt;<font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"USER32VERSION"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">DirectorySearch</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"SystemFolderDriverVersion"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Path</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">[SystemFolder]</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">FileSearch</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"user32.dll"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">MinVersion</font><font size="2" color="#0000FF">=</font><font size="2">"6.0.6001.1750"</font><font size="2" color="#0000FF">/&gt;
    &lt;/</font><font size="2" color="#A31515">DirectorySearch</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF">&gt;</font>
</pre>

Searching for a file is accomplished by describing the directories to search, and then specifying the file to look up in that directory.

The Property element defines the Id for the results of the file search. This Id is used later in the WiX project, for example in conditions. The DirectorySearch element is used to build the directory hierarchy to search for the file. In this case it is given a unique Id, and the path is set to the Windows Installer defined <a href="http://msdn.microsoft.com/library/aa372055.aspx" target="_blank">SystemFolder</a> property which points to the user&apos;s **Windows\System32** directory. The FileSearch element specifies the name of the file to look for in the parent DirectorySearch folder. The MinVersion attribute specifies the minimum version of the file to find.

If the file is found successfully the USER32VERSION property will be set to the full path to the user32.dll file.

**Important:** When doing a locale-neutral search for a file, **you must set the MinVersion property to one revision number lower than the actual version you want to search for**. In this example, while we want to find file version 6.0.6001.1751, the MinVersion is set to 6.0.6001.1750. This is because of a quirk in how the Windows Installer matches file versions. <a href="http://msdn.microsoft.com/library/aa371853.aspx" target="_blank">More information</a> is available in the Windows Installer documentation.

## Step 2: Use the property in a condition
Once you have determined whether the file exists with the requested version you can use the property in a condition. The following is a simple example that prevents installation of the application if the user32.dll file version is too low.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Message</font><font size="2" color="#0000FF">=</font><font size="2">"The installed version of user32.dll is not high enough to support this installer."</font><font size="2" color="#0000FF">&gt;
    &lt;![CDATA[</font><font size="2" color="#808080">Installed OR USER32VERSION</font><font size="2" color="#0000FF">]]&gt;
&lt;/</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF">&gt;</font>
</pre>

<a href="http://msdn.microsoft.com/library/aa369297.aspx" target="_blank">Installed</a> is a Windows Installer property that ensures the check is only done when the user is installing the application, rather than on a repair or remove. The USER32VERSION part will pass if the property is set to anything, and will fail if it is not set. The file check in Step 1 will set the property to the full path of the user32.dll file if it is found with an appropriate file version, and will not set it otherwise.
