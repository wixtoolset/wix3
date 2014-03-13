---
title: HarvestFile Target
layout: documentation
---

# HarvestFile Target

The <b>HarvestFile</b> target passes <b>HarvestFile</b> items to the
[HeatFile task](~/msbuild/task_reference/heatfile.html) to generate authoring from a file. Authoring is generated
for type libraries and self-registration from `DllRegisterServer`. This
target is processed before compilation. Generated authoring is automatically added
to the <b>Compile</b> item group to be compiled by the [Candle task](~/msbuild/task_reference/candle.html)

<pre><span style="color: blue">&lt;</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;
  &lt;</span><span style="color: #a31515">HarvestFile </span><span style="color: red">Include</span><span style="color: blue">=</span>&quot;<span style="color: blue">comserver.dll</span>&quot;<span style="color: blue">&gt;
    &lt;</span><span style="color: #a31515">ComponentGroupName</span><span style="color: blue">&gt;</span>COM<span style="color: blue">&lt;/</span><span style="color: #a31515">ComponentGroupName</span><span style="color: blue">&gt;
    &lt;</span><span style="color: #a31515">DirectoryRefId</span><span style="color: blue">&gt;</span>ServerDir<span style="color: blue">&lt;/</span><span style="color: #a31515">DirectoryRefId</span><span style="color: blue">&gt;
  &lt;/</span><span style="color: #a31515">HarvestFile</span><span style="color: blue">&gt;
&lt;/</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;</span></pre>

The following tables describe the common WiX MSBuild properties and items that are
applicable to the <b>HarvestFile</b> target.

## Items

The following items and item metadata are used by the <b>HarvestFile</b> target.

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
      <b>@(HarvestFile)</b>
    </td>
    <td>
      Required <b>item</b> group.<br />
      <br />
      The list of files to harvest.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.ComponentGroupName)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      The name of the ComponentGroup to create for all the generated authoring.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.DirectoryRefId)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      The ID of the directory to reference instead of TARGETDIR.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.PreprocessorVariable)</b>
    </td>
    <td>
      Optional <b>string</b> metadata.<br />
      <br />
      Substitute SourceDir for another variable name (ex: var.Dir).
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.SuppressCom)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Suppress generation of COM registry elements. The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.SuppressRootDirectory)</b>
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
      <b>%(HarvestFile.SuppressRegistry)</b>
    </td>
    <td>
      Optional <b>boolean</b> metadata.<br />
      <br />
      Suppress generation of any registry elements. The default is <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>%(HarvestFile.Transforms)</b>
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

The following properties are used by the <b>HarvestFile</b> target.

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
      <b>$(HarvestFileAutogenerateGuids)</b>
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
      <b>$(HarvestFileComponentGroupName)</b>
    </td>
    <td>
      Optional <b>string</b> property.<br />
      <br />
      The component group name that will contain all generated authoring.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileDirectoryRefId)</b>
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
      <b>$(HarvestFileGenerateGuidsNow)</b>
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
      <b>$(HarvestFileNoLogo)</b>
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
      <b>$(HarvestFilePreprocessorVariable)</b>
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
      <b>$(HarvestFileSuppressAllWarnings)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that all warnings should be suppressed. The default is $(HarvestSuppressAllWarnings) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileSuppressCom)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of COM registry elements when harvesting. The default
      is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileSuppressFragments)</b>
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
      <b>$(HarvestFileSuppressRegistry)</b>
    </td>
    <td>
      Optional <b>boolean</b> property.<br />
      <br />
      Whether to suppress generation of all registry elements when harvesting. The default
      is <b>false</b>.<br />
      <br />
      This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileSuppressRootDirectory)</b>
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
      <b>$(HarvestFileSuppressSpecificWarnings)</b>
    </td>
    <td>
      Optional <b>string</b> parameter.<br />
      <br />
      Specifies that certain warnings should be suppressed. The default is $(HarvestSuppressSpecificWarnings) if specified.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileSuppressUniqueIds)</b>
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
      <b>$(HarvestFileTransforms)</b>
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
      <b>$(HarvestFileTreatSpecificWarningsAsErrors)</b>
    </td>
    <td>
      Optional <b>string</b> parameter.<br />
      <br />
      Specifies that certain warnings should be treated as errors. The default is $(HarvestTreatSpecificWarningsAsErrors) if specified.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileTreatWarningsAsErrors)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that all warnings should be treated as errors. The default is $(HarvestTreatWarningsAsErrors) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
  <tr>
    <td>
      <b>$(HarvestFileVerboseOutput)</b>
    </td>
    <td>
      Optional <b>boolean</b> parameter.<br />
      <br />
      Specifies that the tool should provide verbose output. The default is $(HarvestVerboseOutput) if specified; otherwise, <b>false</b>.
    </td>
  </tr>
</table>
