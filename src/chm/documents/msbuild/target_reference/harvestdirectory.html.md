---
title: HarvestDirectory Target
layout: documentation
---
# HarvestDirectory Target

The <b>HarvestDirectory</b> target passes <b>HarvestDirectory</b> items to the
[HeatDirectory task](~/msbuild/task_reference/heatdirectory.html) to generate
authoring from a file. Authoring is generated for type libraries and self-registration
from `DllRegisterServer` for any files found in directories. This target
is processed before compilation. Generated authoring is automatically added to the
<b>Compile</b> item group to be compiled by the [Candle task](~/msbuild/task_reference/candle.html)</a>.

<pre><span style="color: blue">&lt;</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;
  &lt;</span><span style="color: #a31515">HarvestDirectory </span><span style="color: red">Include</span><span style="color: blue">=</span>&quot;<span style="color: blue">..\TestProject\Data</span>&quot;<span style="color: blue">&gt;
    &lt;</span><span style="color: #a31515">DirectoryRefId</span><span style="color: blue">&gt;</span>DataDir<span style="color: blue">&lt;/</span><span style="color: #a31515">DirectoryRefId</span><span style="color: blue">&gt;
  &lt;/</span><span style="color: #a31515">HarvestDirectory</span><span style="color: blue">&gt;
&lt;/</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;</span></pre>

The following tables describe the common WiX MSBuild properties and items that are
applicable to the <b>HarvestDirectory</b> target.

## Items

The following items and item metadata are used by the <b>HarvestDirectory</b> target.
<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td>
      <b>Item or Metadata</b>
    </td>
    <td>
      <b>Description</b>
    </td>
  </tr>
  <tr>
    <td>
      <b>@(HarvestDirectory)</b>
    </td>
    <td>
      Required <b>item</b> group.<br />
      <br />
      The list of 
      directories to harvest.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.ComponentGroupName)</b>
    </td>
    <td>
      Optional <b>string</b> metadata. If you are harvesting multiple directories in your project,
      you should specify this metadata to create unique file names for the generated authoring.<br />
      <br />
      The name of the ComponentGroup to create for all the generated authoring.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.DirectoryRefId)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      The ID of the directory to reference instead of TARGETDIR.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.KeepEmptyDirectories)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Whether to create Directory entries for empty directories. The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.PreprocessorVariable)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      Substitute SourceDir for another variable name (ex: var.Dir).
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.SuppressCom)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Suppress generation of COM registry elements. The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.SuppressRootDirectory)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Suppress generation of a Directory element for the parent directory of the file.
      The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.SuppressRegistry)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Suppress generation of any registry elements. The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestDirectory.Transforms)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      XSL transforms to apply to the generated WiX authoring. Separate multiple transforms
      with semicolons.
    </td>
  </tr>
</table>

## Properties

The following properties are used by the <b>HarvestDirectory</b> target.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td>
      <b>Property</b>
    </td>
    <td>
      <b>Description</b>
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryAutogenerateGuids)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to generate authoring that relies on auto-generation of component GUIDs.
      The default is $(HarvestAutogenerateGuids) if specified; otherwise, <b>true</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryComponentGroupName)</b>
    </td>
    <td>
      Optional <b>string</b> property. If you are harvesting multiple directories in your project,
      you should specify this metadata to create unique file names for the generated authoring.<br />
      <br />
      The component group name that will contain all generated authoring.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryDirectoryRefId)</b>
    </td>
    <td>
      Optional <b>string</b> property.<br />
      <br />
      The identifier of the Directory element that will contain all generated authoring.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryGenerateGuidsNow)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to generate authoring that generates durable GUIDs when harvesting. The
      default is $(HarvestGenerateGuidsNow) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryKeepEmptyDirectories)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to create Directory entries for empty directories when harvesting. The default
      is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryNoLogo)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to show the logo for heat.exe. The default is $(NoLogo) if specified; otherwise,
      <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryPreprocessorVariable)</b>
    </td>
    <td>
      Optional <b>string</b> property.<br />
      <br />
      Substitute SourceDir for another variable name (ex: var.Dir) in all generated authoring.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressAllWarnings)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that all warnings should be suppressed. The default is $(HarvestSuppressAllWarnings) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressCom)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of COM registry elements when harvesting files in
      directories. The default is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressFragments)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of separate fragments when harvesting. The default
      is $(HarvestSuppressFragments) if specified; otherwise, <b>true</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressRegistry)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of all registry elements when harvesting files in
      directories. The default is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressRootDirectory)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of a Directory element for all authoring when harvesting.
      The default is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressSpecificWarnings)</b>
    </td>
    <td>
      Optional <b>string</b> parameter.<br />
      <br />
      Specifies that certain warnings should be suppressed. The default is $(HarvestSuppressSpecificWarnings) if specified.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectorySuppressUniqueIds)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of unique component IDs. The default is $(HarvestSuppressUniqueIds)
      if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryTransforms)</b>
    </td>
    <td>
      Optional <b>string</b> property.<br />
      <br />
      XSL transforms to apply to all generated WiX authoring. Separate multiple transforms
      with semicolons. The default is $(HarvestTransforms) if specified.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryTreatSpecificWarningsAsErrors)</b>
    </td>
    <td>
      Optional <b>string</b> parameter.<br />
      <br />
      Specifies that certain warnings should be treated as errors. The default is $(HarvestTreatSpecificWarningsAsErrors) if specified.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryTreatWarningsAsErrors)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that all warnings should be treated as errors. The default is $(HarvestTreatWarningsAsErrors) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestDirectoryVerboseOutput)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that the tool should provide verbose output. The default is $(HarvestVerboseOutput) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
</table>
