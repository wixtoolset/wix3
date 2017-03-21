---
title: WixVSExtension
layout: documentation
after: using_standard_customactions
---
# WixVSExtension

The [WixVSExtension](~/xsd/vs/index.html) includes a set of custom actions to manage help collections. It also includes a set of properties and custom actions that can be used to detect the presence of various versions of Visual Studio and register add-ins, project templates and item templates for use in Visual Studio.

* [Properties](#allproperties)
  * [Visual Studio .NET 2003](#vs2003properties)
  * [Visual Studio 2005](#vs2005properties)
  * [Visual Studio 2008](#vs2008properties)
  * [Visual Studio 2010](#vs2010properties)
  * [Visual Studio 2012](#vs2012properties)
  * [Visual Studio 2013](#vs2013properties)
  * [Visual Studio 2015](#vs2015properties)
  * [Visual Studio 2017](#vs2017properties)
* [Custom Actions](#allcustomactions)

## <a name="allproperties"></a> Properties

Here is a complete list of properties for the <a name="vs2003properties">**Visual Studio .NET 2003**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
  <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2003DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio .NET 2003 if it is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>JSHARP_REDIST_11_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the J# redistributable package 1.1 is installed on the system.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2005properties">**Visual Studio 2005**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
  <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2005 if it is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2005 item templates directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2005 project templates directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2005 XML schemas directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005PROJECTAGGREGATOR2</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2005 project aggregator for managed code add-ins is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2005 root installation directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2005EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vbexpress.exe if Visual Basic 2005 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2005 Standard Edition or higher is installed and the Visual Basic project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2005EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcexpress.exe if Visual C++ 2005 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2005 Standard Edition or higher is installed and the Visual C++ project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2005EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcsexpress.exe if Visual C# 2005 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2005 Standard Edition or higher is installed and the Visual C# project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VJSHARP2005EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vjsexpress.exe if Visual J# 2005 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VJSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2005 Standard Edition or higher is installed and the Visual J# project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2005EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vwdexpress.exe if Visual Web Developer 2005 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2005 Standard Edition or higher is installed and the Visual Web Developer project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio Team Test project system is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VSEXTENSIONS_FOR_NETFX30_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2008 Development Tools for the .NET Framework 3.0 add-in for Visual Studio 2005 is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_WAP_PROJECT_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Web Application Project template for Visual Studio 2005 is installed on the system. This project template is available as a standalone add-in and as a part of visual Studio 2005 SP1.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual Studio 2005 Standard Edition and higher.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VSTF2005_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual Studio 2005 Team Foundation.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2005EXPRESS_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual Basic 2005 Express Edition.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2005EXPRESS_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual C++ 2005 Express Edition.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2005EXPRESS_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual C# 2005 Express Edition.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VJSHARP2005EXPRESS_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual J# 2005 Express Edition.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2005EXPRESS_SP_LEVEL</p>
    </td>
    <td>
      <p>Indicates the service pack level for Visual Web Developer 2005 Express Edition.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>DEXPLORE_2005_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Document Explorer 2005 runtime components package is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>JSHARP_REDIST_20_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the J# redistributable package 2.0 is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>JSHARP_REDIST_20SE_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the J# redistributable package 2.0 second edition is installed on the system.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2008properties">**Visual Studio 2008**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
  <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2008 if it is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2008 item templates directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2008 project templates directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2008 XML schemas directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2008 root installation directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB90EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vbexpress.exe if Visual Basic 2008 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2008 Standard Edition or higher is installed and the Visual Basic project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC90EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcexpress.exe if Visual C++ 2008 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2008 Standard Edition or higher is installed and the Visual C++ project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP90EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcsexpress.exe if Visual C# 2008 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2008 Standard Edition or higher is installed and the Visual C# project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD90EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vwdexpress.exe if Visual Web Developer 2008 Express Edition is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2008 Standard Edition or higher is installed and the Visual Web Developer project system is installed for it.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio Team Test project system is installed on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2008 bootstrapper package folder.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90_SP1</p>
    </td>
    <td>
      <p>Indicates whether or not service pack 1 for Visual Studio 2008 Standard Edition and higher is installed.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB90EXPRESS_SP1</p>
    </td>
    <td>
      <p>Indicates whether or not service pack 1 for Visual Basic 2008 Express Edition is installed.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC90EXPRESS_SP1</p>
    </td>
    <td>
      <p>Indicates whether or not service pack 1 for Visual C++ 2008 Express Edition is installed.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP90EXPRESS_SP1</p>
    </td>
    <td>
      <p>Indicates whether or not service pack 1 for Visual C# 2008 Express Edition is installed.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD90EXPRESS_SP1</p>
    </td>
    <td>
      <p>Indicates whether or not service pack 1 for Visual Web Developer 2008 Express Edition is installed.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>DEXPLORE_2008_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Document Explorer 2008 runtime components package is installed on the system.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2010properties">**Visual Studio 2010**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
    <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2010 if it is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2010 item templates directory. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2010 project templates directory. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2010 XML schemas directory. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2010 root installation directory. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2010EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vbexpress.exe if Visual Basic 2010 Express Edition is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2010 Standard Edition or higher is installed and the Visual Basic project system is installed for it. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2010EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcexpress.exe if Visual C++ 2010 Express Edition is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2010 Standard Edition or higher is installed and the Visual C++ project system is installed for it. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2010EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vcsexpress.exe if Visual C# 2010 Express Edition is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2010 Standard Edition or higher is installed and the Visual C# project system is installed for it. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2010EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vwdexpress.exe if Visual Web Developer 2010 Express Edition is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2010 Standard Edition or higher is installed and the Visual Web Developer project system is installed for it. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2010EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vpdexpress.exe if Visual Studio 2010 Express for Windows Phone is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 Team Test project system is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_DB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 Database project system is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_VSD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 Deployment project system (setup project) is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_WIX_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 Windows Installer XML project system is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_MODELING_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 Modeling project system is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_IDE_FSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2010 F# project system is installed on the system. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2010 bootstrapper package folder. This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2012properties">**Visual Studio 2012**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
    <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2012 if it is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_EXTENSIONS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2012 extensions directory. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2012 item templates directory. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2012 project templates directory. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2012 XML schemas directory. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2012 root installation directory. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2012 Professional Edition or higher is installed and the Visual Basic project system is installed for it. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2012 Professional Edition or higher is installed and the Visual C++ project system is installed for it. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2012 Professional Edition or higher is installed and the Visual C# project system is installed for it. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2012EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vwdexpress.exe if Visual Studio Express 2012 for Web is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2012EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vpdexpress.exe if Visual Studio 2012 Express for Windows Phone is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2012 Professional Edition or higher is installed and the Visual Web Developer project system is installed for it. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2012 Team Test project system is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_DB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2012 Database project system is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_WIX_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Windows Installer XML project system is installed on the system for Visual Studio 2012. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_MODELING_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2012 Modeling project system is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_IDE_FSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2012 F# project system is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2012 bootstrapper package folder. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2013properties">**Visual Studio 2013**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
    <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2013 if it is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_EXTENSIONS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2013 extensions directory. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2013 item templates directory. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2013 project templates directory. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2013 XML schemas directory. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2013 root installation directory. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2013 Professional Edition or higher is installed and the Visual Basic project system is installed for it. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2013 Professional Edition or higher is installed and the Visual C++ project system is installed for it. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2013 Professional Edition or higher is installed and the Visual C# project system is installed for it. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2013EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vwdexpress.exe if Visual Studio Express 2013 for Web is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WINEXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vswinexpress.exe if Visual Studio Express 2013 for Windows is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WDEXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to wdexpress.exe if Visual Studio Express 2013 for Windows Desktop is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2013EXPRESS_IDE</p>
    </td>
    <td>
      <p>Full path to vpdexpress.exe if Visual Studio 2013 Express for Windows Phone is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2013 Professional Edition or higher is installed and the Visual Web Developer project system is installed for it. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2013 Team Test project system is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_WIX_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Windows Installer XML project system is installed on the system for Visual Studio 2013. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_MODELING_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2013 Modeling project system is installed on the system. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_IDE_FSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2013 F# project system is installed on the system. This property is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2013 bootstrapper package folder. This property is available starting with WiX v3.8.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2015properties">**Visual Studio 2015**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
    <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2015 if it is installed on the system. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_EXTENSIONS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2015 extensions directory. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2015 item templates directory. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2015 project templates directory. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2015 XML schemas directory. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2015 root installation directory. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2015 Professional Edition or higher is installed and the Visual Basic project system is installed for it. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2015 Professional Edition or higher is installed and the Visual C++ project system is installed for it. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2015 Professional Edition or higher is installed and the Visual C# project system is installed for it. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2015 Professional Edition or higher is installed and the Visual Web Developer project system is installed for it. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2015 Team Test project system is installed on the system. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_MODELING_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2015 Modeling project system is installed on the system. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_IDE_FSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2015 F# project system is installed on the system. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2015 bootstrapper package folder. This property is available starting with WiX v3.10.</p>
    </td>
  </tr>
</table>

Here is a complete list of properties for the <a name="vs2017properties">**Visual Studio 2017**</a> product family:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
    <tr>
    <td valign="top">
      <p><b>Property name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017DEVENV</p>
    </td>
    <td>
      <p>Full path to devenv.exe for Visual Studio 2017 if it is installed on the system. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_EXTENSIONS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 extensions directory. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_ITEMTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 item templates directory. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_PROJECTTEMPLATES_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 project templates directory. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_SCHEMAS_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 XML schemas directory. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_ROOT_FOLDER</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 root installation directory. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_DIR</p>
    </td>
    <td>
      <p>Full path to the Visual Studio 2017 directory containing devenv.exe. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_VB_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2017 Professional Edition or higher is installed and the Visual Basic project system is installed for it. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_VC_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2017 Professional Edition or higher is installed and the Visual C++ project system is installed for it. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2017 Professional Edition or higher is installed and the Visual C# project system is installed for it. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_VWD_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether Visual Studio 2017 Professional Edition or higher is installed and the Visual Web Developer project system is installed for it. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_VSTS_TESTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2017 Team Test project system is installed on the system. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_MODELING_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2017 Modeling project system is installed on the system. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_IDE_FSHARP_PROJECTSYSTEM_INSTALLED</p>
    </td>
    <td>
      <p>Indicates whether or not the Visual Studio 2017 F# project system is installed on the system. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017_BOOTSTRAPPER_PACKAGE_FOLDER</p>
    </td>
    <td>
      <p>The location of the Visual Studio 2017 bootstrapper package folder. This property is available starting with WiX v3.11.</p>
    </td>
  </tr>
</table>

## <a name="allcustomactions"></a> Custom Actions

Here is a complete list of custom actions:

<table cellspacing="0" cellpadding="4" class="style1" border="1">
  <tr>
    <td valign="top">
      <p><b>Custom action name</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2003Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if a Visual Studio .NET 2003 edition is found on the system.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2005 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS2005DEVENV property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2005 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS2005DEVENV property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2005Setup</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /setup if Visual Basic 2005 Express Edition is found on the system. Including this custom action automatically adds the VB2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /InstallVSTemplates if Visual Basic 2005 Express Edition is found on the system. Including this custom action automatically adds the VB2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2005Setup</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /setup if Visual C++ 2005 Express Edition is found on the system. Including this custom action automatically adds the VC2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /InstallVSTemplates if Visual C++ 2005 Express Edition is found on the system. Including this custom action automatically adds the VC2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2005Setup</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /setup if Visual C# 2005 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /InstallVSTemplates if Visual C# 2005 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VJSHARP2005Setup</p>
    </td>
    <td>
      <p>Runs vjsexpress.exe /setup if Visual J# 2005 Express Edition is found on the system. Including this custom action automatically adds the VJSHARP2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VJSHARP2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vjsexpress.exe /InstallVSTemplates if Visual J# 2005 Express Edition is found on the system. Including this custom action automatically adds the VJSHARP2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2005Setup</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /setup if Visual Web Developer 2005 Express Edition is found on the system. Including this custom action automatically adds the VWD2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2005InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /InstallVSTemplates if Visual Web Developer 2005 Express Edition is found on the system. Including this custom action automatically adds the VWD2005EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2008 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS90DEVENV property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS90InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2008 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS90DEVENV property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB90Setup</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /setup if Visual Basic 2008 Express Edition is found on the system. Including this custom action automatically adds the VB90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB90InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /InstallVSTemplates if Visual Basic 2008 Express Edition is found on the system. Including this custom action automatically adds the VB90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC90Setup</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /setup if Visual C++ 2008 Express Edition is found on the system. Including this custom action automatically adds the VC90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC90InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /InstallVSTemplates if Visual C++ 2008 Express Edition is found on the system. Including this custom action automatically adds the VC90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP90Setup</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /setup if Visual C# 2008 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP90InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /InstallVSTemplates if Visual C# 2008 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD90Setup</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /setup if Visual Web Developer 2008 Express Edition is found on the system. Including this custom action automatically adds the VWD90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD90InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /InstallVSTemplates if Visual Web Developer 2008 Express Edition is found on the system. Including this custom action automatically adds the VWD90EXPRESS_IDE property.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2010 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS2010DEVENV property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2010 Standard Edition or higher is found on the system. Including this custom action automatically adds the VS2010DEVENV property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2010Setup</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /setup if Visual Basic 2010 Express Edition is found on the system. Including this custom action automatically adds the VB2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VB2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vbexpress.exe /InstallVSTemplates if Visual Basic 2010 Express Edition is found on the system. Including this custom action automatically adds the VB2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2010Setup</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /setup if Visual C++ 2010 Express Edition is found on the system. Including this custom action automatically adds the VC2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VC2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcexpress.exe /InstallVSTemplates if Visual C++ 2010 Express Edition is found on the system. Including this custom action automatically adds the VC2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2010Setup</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /setup if Visual C# 2010 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VCSHARP2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vcsexpress.exe /InstallVSTemplates if Visual C# 2010 Express Edition is found on the system. Including this custom action automatically adds the VCSHARP2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2010Setup</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /setup if Visual Web Developer 2010 Express Edition is found on the system. Including this custom action automatically adds the VWD2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /InstallVSTemplates if Visual Web Developer 2010 Express Edition is found on the system. Including this custom action automatically adds the VWD2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2010Setup</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /setup if Visual Studio 2010 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2010InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /InstallVSTemplates if Visual Studio 2010 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2010EXPRESS_IDE property. This custom action is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2012 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2012DEVENV property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2012 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2012DEVENV property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2012Setup</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /setup if Visual Studio Express 2012 for Web is found on the system. Including this custom action automatically adds the VWD2012EXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2012InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /InstallVSTemplates if Visual Studio Express 2012 for Web is found on the system. Including this custom action automatically adds the VWD2012EXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012WinExpressSetup</p>
    </td>
    <td>
      <p>Runs vswinexpress.exe /setup if Visual Studio Express 2012 for Windows 8 is found on the system. Including this custom action automatically adds the VS2012WINEXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2012WinExpressInstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vswinexpress.exe /InstallVSTemplates if Visual Studio Express 2012 for Windows 8 is found on the system. Including this custom action automatically adds the VS2012WINEXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2012Setup</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /setup if Visual Studio 2012 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2012EXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2012InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /InstallVSTemplates if Visual Studio 2012 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2012EXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2013 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2013DEVENV property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2013 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2013DEVENV property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2013Setup</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /setup if Visual Studio Express 2013 for Web is found on the system. Including this custom action automatically adds the VWD2013EXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VWD2013InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vwdexpress.exe /InstallVSTemplates if Visual Studio Express 2013 for Web is found on the system. Including this custom action automatically adds the VWD2013EXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WinExpressSetup</p>
    </td>
    <td>
      <p>Runs vswinexpress.exe /setup if Visual Studio Express 2013 for Windows 8 is found on the system. Including this custom action automatically adds the VS2013WINEXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WinExpressInstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vswinexpress.exe /InstallVSTemplates if Visual Studio Express 2013 for Windows 8 is found on the system. Including this custom action automatically adds the VS2013WINEXPRESS_IDE property. This custom action is available starting with WiX v3.6.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WDExpressSetup</p>
    </td>
    <td>
      <p>Runs WDExpress.exe /setup if Visual Studio Express 2013 for Windows Desktop is found on the system. Including this custom action automatically adds the VS2013WDEXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2013WDExpressInstallVSTemplates</p>
    </td>
    <td>
      <p>Runs WDExpress.exe /InstallVSTemplates if Visual Studio Express 2013 for Windows Desktop is found on the system. Including this custom action automatically adds the VS2013WDEXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2013Setup</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /setup if Visual Studio 2013 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2013EXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VPD2013InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs vpdexpress.exe /InstallVSTemplates if Visual Studio 2013 Express for Windows Phone is found on the system. Including this custom action automatically adds the VPD2013EXPRESS_IDE property. This custom action is available starting with WiX v3.8.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2015 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2013DEVENV property. This custom action is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2015InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2015 Professional Edition or higher is found on the system. Including this custom action automatically adds the VS2013DEVENV property. This custom action is available starting with WiX v3.10.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017Setup</p>
    </td>
    <td>
      <p>Runs devenv.exe /setup if Visual Studio 2017 Community Edition or higher is found on the system. Including this custom action automatically adds the VS2017DEVENV property. This custom action is available starting with WiX v3.11.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>VS2017InstallVSTemplates</p>
    </td>
    <td>
      <p>Runs devenv.exe /InstallVSTemplates if Visual Studio 2017 Community Edition or higher is found on the system. Including this custom action automatically adds the VS2017DEVENV property. This custom action is available starting with WiX v3.11.</p>
    </td>
  </tr>
</table>

## Using WixVSExtension Properties or Custom Actions

To use the WixVSExtension properties or custom actions in an MSI, use the following steps:

* Add PropertyRef or CustomActionRef elements for items listed above that you want to use in your MSI.
* Add the -ext &lt;path to WixVSExtension.dll&gt; command line parameter when calling light.exe to include the WixVSExtension in the MSI linking process.

For example:

    <PropertyRef Id="VS2005_ROOT_FOLDER" />
    <CustomActionRef Id="VS2005Setup" />

When you reference any of the above properties or custom actions, the WixVSExtension automatically schedules the custom actions and pulls in properties used in the custom action conditions and execution logic.
