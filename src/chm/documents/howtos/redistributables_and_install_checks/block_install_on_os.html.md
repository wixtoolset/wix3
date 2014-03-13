---
title: How To: Block Installation Based on OS Version
layout: documentation
---
# How To: Block Installation Based on OS Version
Windows Installer provides the standard <a href="http://msdn.microsoft.com/library/aa372495.aspx" target="_blank">VersionNT</a> property that can be used to detect the version of the user&apos;s operating system. Often it is desirable to use this property to block installation of an application on incompatible versions of an operating system. The following sample demonstrates how to use this property to block installation of an application on operating systems prior to Windows Vista/Windows Server 2008.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Message</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">This application is only supported on Windows Vista, Windows Server 2008, or higher.</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
    &lt;![CDATA[Installed OR (</font><font size="2" color="#808080">VersionNT &gt;= 600</font><font size="2" color="#0000FF">)]]&gt;
&lt;/</font><font size="2" color="#A31515">Condition</font><font size="2" color="#0000FF">&gt;</font>
</pre>

<a href="http://msdn.microsoft.com/library/aa369297.aspx" target="_blank">Installed</a> is a Windows Installer property that ensures the check is only done when the user is installing the application, rather than on a repair or remove. The VersionNT part will pass if the property&apos;s value is greater than or equal to 600, the version that matches Windows Vista, the installation will proceed. The values for different versions of the Windows operating system are <a href="http://msdn.microsoft.com/library/aa370556.aspx" target="_blank">available on MSDN</a>.

To check for versions of 64-bit Windows use the <a href="http://msdn.microsoft.com/library/aa372497.aspx" target="_blank">VersionNT64</a> property. To check for versions of Windows prior to Windows NT use the <a href="http://msdn.microsoft.com/library/aa370556.aspx" target="_blank">Windows9X</a> property.

