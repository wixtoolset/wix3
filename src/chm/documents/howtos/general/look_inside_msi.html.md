---
title: How To: Look Inside Your MSI With Orca
layout: documentation
---
# How To: Look Inside Your MSI With Orca
When building installers it can often be useful to look inside your installer to see the actual tables and values that were created by the WiX build process. Microsoft provides a tool with the <a href="http://www.microsoft.com/en-us/download/details.aspx?id=3138" target="_blank">Windows SDK</a>, called Orca, that can be used for this purpose. To install Orca, download and install the Windows SDK. After the SDK installation is complete navigate to the install directory (typically **C:\Program Files\Microsoft SDKs\Windows\v7.0**) and open the **Tools** folder. Inside the Tools folder run Orca.msi to complete the installation. (If the Windows 8.1 SDK is installed, then Orca-x86.msi can typically be found in **c:\Program Files\Windows Kits\8.1\bin\x86**)

Once Orca is installed you can right click on any MSI file from Windows Explorer and select **Edit with Orca** to view the contents of the MSI.
