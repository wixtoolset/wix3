---
title: Harvest Tool (Heat)
layout: documentation
after: lit
---
# Harvest Tool (Heat)

Generates WiX authoring from various input formats.

Every time heat is run it regenerates the output file and any changes are lost.

## Usage Information
    heat.exe [-?] harvestType <harvester arguments> -out sourceFile.wxs

Heat supports the harvesting types:

<table border="1" cellspacing="0" cellpadding="2" class="style1">
  <tr>
    <td valign="top">
      <p><b>Harvest Type</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>dir&nbsp;</p>
    </td>
    <td>
      <p>Harvest a directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>file</p>
    </td>
    <td>
      <p>Harvest a file.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>project</p>
    </td>
    <td>
      <p>Harvest outputs of a Visual Studio project.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>website</p>
    </td>
    <td>
      <p>Harvest an IIS web site.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>perf</p>
    </td>
    <td>
      <p>Harvest performance counters from a category.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>reg</p>
    </td>
    <td>
      <p>Harvest registy information from a reg file..</p>
    </td>
  </tr>
</table>

Heat supports the following command line parameters:

<table border="1" cellspacing="0" cellpadding="2" class="style1">
  <tr>
    <td valign="top">
      <p><b>Switch</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ag</p>
    </td>
    <td>
      <p>Auto generate component guids at compile time, e.g. set Guid=&quot;*&quot;.</p>
    </td>
  </tr>
  <tr>
    <td valign="top" style="white-space: nowrap">
      <p>-cg &lt;ComponentGroupName&gt;</p>
    </td>
    <td>
      <p>Component group name (cannot contain spaces e.g -cg MyComponentGroup).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-configuration</p>
    </td>
    <td>
      <p>Configuration to set when harvesting the project.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-directoryid</p>
    </td>
    <td>
      <p>Overridden directory id for generated directory elements.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-dr &lt;DirectoryName&gt;</p>
    </td>
    <td>
      <p>Directory reference to root directories (cannot contains spaces e.g. -dr MyAppDirRef).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ext &lt;extension&gt;</p>
    </td>
    <td>
      <p>Extension assembly or &quot;class, assembly&quot;.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-generate</p>
    </td>
    <td>
      <p>Specify what elements to generate, one of: components, container, payloadgroup,
          layout (default is components).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-gg</p>
    </td>
    <td>
      <p>Generate guids now. All components are given a guid when heat is run.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-g1</p>
    </td>
    <td>
      <p>Generate component guids without curly braces.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ke</p>
    </td>
    <td>
      <p>Keep empty directories.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-nologo</p>
    </td>
    <td>
      <p>Skip printing heat logo information.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-out</p>
    </td>
    <td>
      <p>Specify output file (default: write to current directory).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-platform</p>
    </td>
    <td>
      <p>Platform to set when harvesting the project.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-pog:&lt;group&gt;</p>
    </td>
    <td>
      <p>Specify output group of Visual Studio project, one of: Binaries, Symbols, Documents,
          Satellites, Sources, Content.</p>
      <ul>
        <li>Binaries - primary output of the project, e.g. the assembly exe or dll.</li>
        <li>Symbols - debug symbol files, e.g. pdb.</li>
        <li>Documents - documentation files.</li>
        <li>Satellites - the localized resource assemblies.</li>
        <li>Sources - source files.</li>
        <li>Content - content files.</li>
      </ul>
      <p>This option may be repeated for multiple output groups; e.g. -pog:Binaries -pog:Content.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-projectname</p>
    </td>
    <td>
      <p>Overridden project name to use in variables.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-scom</p>
    </td>
    <td>
      <p>Suppress COM elements.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sfrag</p>
    </td>
    <td>
      <p>Suppress generation of fragments for directories and components.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-srd</p>
    </td>
    <td>
      <p>Suppress harvesting the root directory as an element.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sreg</p>
    </td>
    <td>
      <p>Suppress registry harvesting.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-suid</p>
    </td>
    <td>
      <p>Suppress unique identifiers for files, components, &amp; directories.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-svb6</p>
    </td>
    <td>
      <p>Suppress VB6 COM registration entries. When registering a COM component created
          in VB6 it adds registry entries that are part of the VB6 runtime component. This
          flag is recommend for VB6 components to avoid breaking the VB6 runtime on uninstall.</p>
      <p>The following values are excluded:<br />
          - CLSID\{D5DE8D20-5BB8-11D1-A1E3-00A0C90F2731}<br />
          - Typelib\{EA544A21-C82D-11D1-A3E4-00A0C90AEA82}<br />
          - Typelib\{000204EF-0000-0000-C000-000000000046}<br />
          - Any Interfaces that reference these two type libraries</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sw&lt;N&gt;</p>
    </td>
    <td>
      <p>Suppress all warnings or a specific message ID, e.g. -sw1011 -sw1012.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-swall</p>
    </td>
    <td>
      <p>Suppress all warnings (<em>deprecated</em>).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-t &lt;xsl&gt;</p>
    </td>
    <td>
      <p>Transform harvested output with XSL file.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-indent &lt;n&gt;</p>
    </td>
    <td>
      <p>Indentation multiple (overrides default of 4).</p>
    </td>
  </tr>
  <tr>
    <td valign="top" style="white-space: nowrap">
      <p>-template &lt;template&gt;</p>
    </td>
    <td>
      <p>Use template, one of: fragment, module, product.<br />
          Default: fragment.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-v</p>
    </td>
    <td>
      <p>Verbose output.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-var &lt;VariableName&gt;</p>
    </td>
    <td>
      <p>Substitute File/@Source=&quot;SourceDir&quot; with a preprocessor or a wix variable
          (e.g. -var var.MySource will become File/@Source=&quot;$(var.MySource)\myfile.txt&quot;
          and -var wix.MySource will become File/@Source=&quot;!(wix.MySource)\myfile.txt&quot;.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wixvar</p>
    </td>
    <td>
      <p>Generate binder variables instead of preprocessor variables.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wx[N]</p>
    </td>
    <td>
      <p>Treat all warnings or a specific message ID as an error. e.g. -wx1011 -wx1012.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wxall</p>
    </td>
    <td>
      <p>Treat all warnings as errors (<em>deprecated</em>).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-? | -help</p>
    </td>
    <td>
      <p>&nbsp;Display heat help information.</p>
    </td>
  </tr>
</table>

## Command line examples

### Harvest a directory

    heat dir ".\My Files" -gg -sfrag -template:fragment -out directory.wxs

This will harvest the sub folder &quot;My Files&quot; as a single fragment to the
file directory.wxs. It will generate guids for all the files as they are found.

#### Harvest a file

    heat file ".\My Files\File.dll" -ag -template:fragment -out file.wxs

This will harvest the file &quot;File.dll&quot; as a single fragment to the file
file.wxs. The component guid will be set to &quot;*&quot;.

#### Harvest a Visual Studio project

    heat project "MyProject.csproj" -pog:Binaries -ag -template:fragment -out project.wxs

This will harvest the binary output files from the Visual Studio project &quot;MyProject.csproj&quot;
as a single fragment to the file project.wxs. The component guid will be set to &quot;*&quot;.</p>

#### Harvest a Website

    heat website "Default Web Site" -template:fragment -out website.wxs


This will harvest the website &quot;Default Web Site&quot; as a single fragment
to the file website.wxs.

#### Harvest a VB6 COM component

    heat file ".\My Files\VB6File.dll" -ag -template:fragment -svb6 -out vb6file.wxs

This will harvest the VB6 COM component &quot;VB6File.dll&quot;as a single fragment
to the file vb6file.wxs and suppress the VB6 runtime specific registy entries.

#### Harvest performance counters

    heat perf "My Category" -out perf.wxs

This will harvest all the performance counters from the category &quot;My Category&quot;.

#### Harvest a registry file

    heat reg registry.reg -out reg.wxs

This will harvest all the registry information from the file registry.reg. The registry
file can be either a standard &quot;Windows Registry Editor Version 5.00&quot; reigstry
file or a legacy Win9.x/NT4 (REGEDIT4) reigstry file.
