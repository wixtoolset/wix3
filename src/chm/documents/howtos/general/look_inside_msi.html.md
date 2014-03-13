---
title: How To: Look Inside Your MSI With Orca
layout: documentation
---
# How To: Look Inside Your MSI With Orca
When building installers it can often be useful to look inside your installer to see the actual tables and values that were created by the WiX build process. Microsoft provides a tool with the <a href="http://www.microsoft.com/downloads/details.aspx?FamilyId=6A35AC14-2626-4846-BB51-DDCE49D6FFB6" target="_blank">Windows Installer 4.5 SDK</a>, called Orca, that can be used for this purpose. To install Orca, download and install the Windows Installer 4.5 SDK. After the SDK installation is complete navigate to the install directory (typically **C:\Program Files\Windows Installer 4.5 SDK**) and open the **Tools** folder. Inside the Tools folder run Orca.msi to complete the installation.

Once Orca is installed you can right click on any MSI file from Windows Explorer and select **Edit with Orca** to view the contents of the MSI.
