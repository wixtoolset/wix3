---
title: HarvestProjects Target
layout: documentation
---
# HarvestProjects Target

The <b>HarvestProjects</b> target passes <b>HarvestProject</b> items to the
[HeatProject task](~/msbuild/task_reference/heatproject.html) to generate authoring from a project file.

Harvesting projects is disabled by default because it may not always work correctly, but you can enable it by adding the following to the top of your [WiX project file](~/msbuild/authoring_first_msbuild_project.html):

<pre><span style="color: blue">&lt;</span><span style="color: #a31515">PropertyGroup</span><span style="color: blue">&gt;
  &lt;</span><span style="color: #a31515">EnableProjectHarvesting</span><span style="color: blue">&gt;</span>True<span style="color: blue">&lt;/</span><span style="color: #a31515">EnableProjectHarvesting</span><span style="color: blue">&gt;</span>
&lt;/</span><span style="color: #a31515">PropertyGroup</span><span style="color: blue">&gt;</span></pre>

If enabled, this target is processed before compilation. Generated authoring is automatically added to the
<b>Compile</b> item group to be compiled by the [Candle task](~/msbuild/task_reference/candle.html).

<pre><span style="color: blue">&lt;</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;
  &lt;</span><span style="color: #a31515">HeatProject </span><span style="color: red">Include</span><span style="color: blue">="..\TestProject\TestProject.csproj" &gt;
    &lt;</span><span style="color: #a31515">ProjectOutputGroups</span><span style="color: blue">&gt;</span>Binaries;Sources<span style="color: blue">&lt;/</span><span style="color: #a31515">ProjectOutputGroups</span><span style="color: blue">&gt;
  &lt;/</span><span style="color: #a31515">HeatProject</span><span style="color: blue">&gt;
&lt;/</span><span style="color: #a31515">ItemGroup</span><span style="color: blue">&gt;</span></pre>

The following tables describe the common WiX MSBuild properties and items that are
applicable to the <b>HarvestProjects</b> target.

## Items
The following items and item metadata are used by the <b>HarvestProjects</b> target.

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
            <b>@(HarvestProject)</b>
        </td>
        <td>
            Required <b>item</b> group.<br />
            <br />
            The list of projects to harvest. The <b>HeatProject</b> item group is provided only
            for backward compatibility.
        </td>
    </tr>
    <tr>
        <td>
            <b>%(HarvestProject.ProjectOutputGroups)</b>
        </td>
        <td>
            Optional <b>string</b> metadata.<br />
            <br />
            The project output groups to harvest. Separate multiple output groups with semicolons.
            Examples include "Binaries" and "Source".
        </td>
    </tr>
    <tr>
        <td>
            <b>%(HarvestProject.Transforms)</b>
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
The following properties are used by the <b>HarvestProjects</b> target.

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
            <b>$(HarvestProjectsAutogenerateGuids)</b>
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
            <b>$(HarvestProjectsGenerateGuidsNow)</b>
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
            <b>$(HarvestProjectsNoLogo)</b>
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
            <b>$(HarvestProjectsProjectOutputGroups)</b>
        </td>
        <td>
            Optional <b>string</b> property.<br />
            <br />
            The project output groups to harvest from all projects. Separate multiple project
            output groups with semicolons.<br />
            <br />
            This global property is only usable with MSBuild 4.0 or Visual Studio 2010, and newer.
        </td>
    </tr>
    <tr>
        <td>
            <b>$(HarvestProjectsSuppressAllWarnings)</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that all warnings should be suppressed. The default is $(HarvestSuppressAllWarnings) if specified; otherwise, <b>false</b>.
        </td>
    </tr>
    <tr>
        <td>
            <b>$(HarvestProjectsSuppressFragments)</b>
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
            <b>$(HarvestProjectsSuppressSpecificWarnings)</b>
        </td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            Specifies that certain warnings should be suppressed. The default is $(HarvestSuppressSpecificWarnings) if specified.
        </td>
    </tr>
    <tr>
        <td>
            <b>$(HarvestProjectsSuppressUniqueIds)</b>
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
            <b>$(HarvestProjectsTransforms)</b>
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
            <b>$(HarvestProjectsTreatSpecificWarningsAsErrors)</b>
        </td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            Specifies that certain warnings should be treated as errors. The default is $(HarvestTreatSpecificWarningsAsErrors) if specified.
        </td>
    </tr>
    <tr>
        <td>
            <b>$(HarvestProjectsTreatWarningsAsErrors)</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that all warnings should be treated as errors. The default is $(HarvestTreatWarningsAsErrors) if specified; otherwise, <b>false</b>/.
        </td>
    </tr>
    <tr>
        <td>
            <b>$(HarvestProjectsVerboseOutput)</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that the tool should provide verbose output. The default is $(HarvestVerboseOutput) if specified.
        </td>
    </tr>
</table>
