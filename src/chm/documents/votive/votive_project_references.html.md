---
title: Using Project References and Variables
layout: documentation
after: authoring_first_votive_project
---
# Using Project References and Variables

The WiX project supports adding project references to other projects such as VB and C#. This ensures that build order dependencies are defined correctly within the solution. In addition, it generates a set of WiX preprocessor 
variables that can be referenced in WiX source files and preprocessor definitions which are 
passed to the compiler at build time.

To add a project reference to a WiX project:

1. Right-click on the <b>References</b> node of the project in the Solution 
Explorer and choose <b>Add Reference...</b>.
1. In the Add Reference dialog, click on the **Projects** tab.
1. Select the desired project(s) and click the <b>Add</b> button, and then press OK 
to dismiss the dialog.

## Supported Project Reference Variables

Once a project reference is added, a list of project variables becomes avaliable 
to be referenced in the WiX source code. Project reference variables are useful 
when you do not want to have hard-coded values. For example, the 
$(var.MyProject.ProjectName) variable will query the correct project name at 
build time even if I change the name of the referenced project after the 
reference is added.

The following demonstrates how to use project reference variables in WiX source 
code:

    <File Id="MyExecutable" Name="$(var.MyProject.TargetFileName)" Source="$(var.MyProject.TargetPath)" DiskId="1" />

The WiX project supports the following project reference variables:

<table border="1" cellspacing="0" cellpadding="2" class="style1">
  <tr>
    <td valign="top">
      <p><b>Variable name</b></p>
    </td>
    <td valign="top">
      <p><b>Example usage</b></p>
    </td>
    <td valign="top">
      <p><b>Example value</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.Configuration</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.Configuration)</p>
    </td>
    <td valign="top">
      <p>Debug or Release</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.FullConfiguration</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.FullConfiguration)</p>
    </td>
    <td valign="top">
      <p>Debug|AnyCPU</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.Platform</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.Platform)</p>
    </td>
    <td valign="top">
      <p>AnyCPU, Win32, x64 or ia64</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.ProjectDir</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.ProjectDir)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MyProject\</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.ProjectExt</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.ProjectExt)</p>
    </td>
    <td valign="top">
      <p>.csproj</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.ProjectFileName</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.ProjectFileName)</p>
    </td>
    <td valign="top">
      <p>MyProject.csproj</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.ProjectName</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.ProjectName)</p>
    </td>
    <td valign="top">
      <p>MyProject</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.ProjectPath</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.ProjectPath)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MyProject\MyApp.csproj</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.TargetDir</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.TargetDir)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MyProject\bin\Debug\</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.TargetExt</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.TargetExt)</p>
    </td>
    <td valign="top">
      <p>.exe</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.TargetFileName</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.TargetFileName)</p>
    </td>
    <td valign="top">
      <p>MyProject.exe</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.TargetName</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.TargetName)</p>
    </td>
    <td valign="top">
      <p>MyProject</p>
    </td>
  </tr>
  <tr>
   <td valign="top">
      <p>var.<i>ProjectName</i>.TargetPath</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.TargetPath)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MyProject\bin\Debug\MyProject.exe</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.<i>ProjectName</i>.<i>Culture</i>.TargetPath</p>
    </td>
    <td valign="top">
      <p>$(var.MyProject.en-US.TargetPath)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MyProject\bin\Debug\en-US\MyProject.msm</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.SolutionDir</p>
    </td>
    <td valign="top">
      <p>$(var.SolutionDir)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MySolution\</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.SolutionExt</p>
    </td>
    <td valign="top">
      <p>$(var.SolutionExt)</p>
    </td>
    <td valign="top">
      <p>.sln</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.SolutionFileName</p>
    </td>
    <td valign="top">
      <p>$(var.SolutionFileName)</p>
    </td>
    <td valign="top">
      <p>MySolution.sln</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.SolutionName</p>
    </td>
    <td valign="top">
      <p>$(var.SolutionName)</p>
    </td>
    <td valign="top">
      <p>MySolution</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>var.SolutionPath</p>
    </td>
    <td valign="top">
      <p>$(var.SolutionPath)</p>
    </td>
    <td valign="top">
      <p>C:\users\myusername\Documents\Visual Studio 2010\Projects\MySolution\MySolution.sln</p>
    </td>
  </tr>
</table>

**Note:** var.*ProjectName*.*Culture*.TargetPath is only available for projects that have multiple localized outputs (e.g. MSMs).
