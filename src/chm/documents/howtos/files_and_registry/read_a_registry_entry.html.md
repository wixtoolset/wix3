---
title: How To: Read a Registry Entry During Installation
layout: documentation
---
# How To: Read a Registry Entry During Installation
Installers often need to look up the value of a registry entry during the installation process. The resulting registry value is often used in a conditional statement later in install, such as to install a specific component if a registry entry is not found. This how to demonstrates reading an integer value from the registry and verifying that it exists in a <a href="http://msdn.microsoft.com/library/aa369752.aspx" target="_blank">launch condition</a>.

## Step 1: Read the registry entry into a property
Registry entries are read using the [&lt;RegistrySearch&gt;](~/xsd/wix/registrysearch.html) element. The following snippet looks for the the presence of the key that identifies the installation of .NET Framework 2.0 on the target machine*.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">NETFRAMEWORK20</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">RegistrySearch</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">NetFramework20</font><font size="2">"
                    </font><font size="2" color="#FF0000">Root</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">HKLM</font><font size="2">"
                    </font><font size="2" color="#FF0000">Key</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Software\Microsoft\NET Framework Setup\NDP\v2.0.50727</font><font size="2">"</font>
<font size="2" color="#FF0000">             Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Install</font><font size="2">"
                    </font><font size="2" color="#FF0000">Type</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">raw</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
&lt;/</font><font size="2" color="#A31515">Property</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The RegistrySearch element specifies a unique id, the root in the registry to search, and the key to look under. The name attribute specifies the specific value to query. The type attribute specifies how the value should be treated. Raw indicates that the value should be prefixed according to the data type of the value. In this case, since Install is a DWORD, the resulting value will be prepended with a #.

The above sample will set the NETFRAMEWORK20 property to &quot;#1&quot; if the registry key was found, and to nothing if it wasn&apos;t.

## Step 2: Use the property in a condition
After the property is set you can use it in a condition anywhere in your WiX project. The following snippet demonstrates how to use it to block installation if .NET Framework 2.0 is not installed.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Message</font><font size="2" color="#0000FF">=</font><font size="2">"This application requires .NET Framework 2.0. Please install the .NET Framework then run this installer again."</font><font size="2" color="#0000FF">&gt;
    &lt;![CDATA[</font><font size="2" color="#808080">Installed OR</font> <font size="2" color="#0000FF">NETFRAMEWORK20]]&gt;
&lt;/</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF">&gt;</font>
</pre>

<a href="http://msdn.microsoft.com/library/aa369297.aspx" target="_blank">Installed</a> is a Windows Installer property that ensures the check is only done when the user is installing the application, rather than on a repair or remove. The NETFRAMEWORK20 part of the condition will pass if the property was set. If it is not set the installer will display the error message then abort the installation process.

\* This registry entry is used for sample purposes only. If you want to detect the installed version of .NET Framework you can use the built-in WiX support. For more information see [How To: Check for .NET Framework Versions](~/howtos/redistributables_and_install_checks/check_for_dotnet.html).
