---
title: Standard Custom Actions
layout: documentation
after: /howtos/
---
# Standard Custom Actions

The WiX toolset contains several custom actions to handle configuring resources such as Internet Information Services web sites and virtual directories, SQL Server databases and scripts, user accounts, file shares, and more. These custom actions are provided in WiX extensions.

To get started using standard custom actions, see the [Using Standard Custom Actions](using_standard_customactions.html) topic.

For information about specific types of standard custom actions, see the following topics:

* [FileShare custom action](~/xsd/util/fileshare.html) (located in WixUtilExtension) - create and configure file shares.
* [Internet shortcut custom action](~/xsd/util/internetshortcut.html) (located in WixUtilExtension) - create shortcuts that point to Web sites.
* [OSInfo custom actions](osinfo.html) (located in WixUtilExtension) - set properties for OS information and standard directories that are not provided by default by Windows Installer.
* [Performance Counter custom action](perfmon.html) (located in WixUtilExtension) - install and uninstall performance counters.
* [Quiet Execution custom action](qtexec.html) (located in WixUtilExtension) - launch console executables without displaying a window.
* [Secure Objects custom action](~/xsd/util/permissionex.html) (located in WixUtilExtension) - secure (using ACLs) objects that the <a href='http://msdn2.microsoft.com/library/aa369774.aspx' target="_blank">LockPermissions table</a> cannot.
* [Service Configuration custom action](~/xsd/util/serviceconfig.html) (located in WixUtilExtension) - configure attributes of a Windows service that the <a href='http://msdn2.microsoft.com/library/aa371637.aspx' target="_blank">ServiceInstall table</a> cannot.
* [ShellExecute custom action](shellexec.html) (located in WixUtilExtension) - launch document or URL targets via the Windows shell.
* [User custom actions](~/xsd/util/user.html) (located in WixUtilExtension) - create and configure new users.
* [WixDirectXExtension](wixdirectxextension.html) - custom action that can be used to check the DirectX capabilities of the video card on the system.
* [WixExitEarlyWithSuccess](wixexitearlywithsuccess.html) (located in WixUtilExtension) - custom action that can be used to exit setup without installing the product. This can be useful in some major upgrade scenarios.
* [WixFailWhenDeferred](wixfailwhendeferred.html) (located in WixUtilExtension) - custom action that can be used to simulate installation failures to test rollback scenarios.
* [WixFirewallExtension](~/xsd/firewall/index.html) - Firewall custom action that can be used to add exceptions to the Windows Firewall.
* [WixGamingExtension](wixgamingextension.html) - Gaming custom action that can be used to add icons and tasks to Windows Game Explorer.
* [WixIIsExtension](~/xsd/iis/index.html) - Internet Information Services (IIS) custom actions that can be used to create and configure web sites, virtual directories, web applications, etc.
* [WixNetFxExtension](wixnetfxextension.html) - custom action to generate native code for .NET assemblies; properties to detect .NET Framework install state and service pack levels.
* [WixSqlExtension](~/xsd/sql/index.html) - SQL Server custom actions that can be used to create databases and execute SQL scripts and statements.
* [WixVSExtension](wixvsextension.html) - custom action to register help collections and Visual Studio packages; properties to detect install state and service pack levels for various Visual Studio editions.
* [XmlFile custom action](~/xsd/util/xmlfile.html) (located in WixUtilExtension) - configure and modify XML files as part of your installation package.
