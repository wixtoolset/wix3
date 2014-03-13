---
title: HeatDirectory Task
layout: documentation
---

# HeatDirectory Task

The <b>HeatDirectory</b> task wraps [heat.exe](~/overview/heat.html), the WiX harvester,
using the <b>dir</b> harvesting type. Authoring is generated for type libraries and
self-registration from DllRegisterServer for any files found in directories. It
supports a variety of settings that are described in more detail below. To control
these settings in your .wixproj file, you can create a PropertyGroup and specify the
settings that you want to use for your build process. The following is a sample
PropertyGroup that contains settings that will be used by the <b>HeatDirectory</b> task:

<pre><span style="color: blue">&lt;</span><span style="color: #a31515">HeatDirectory
</span><span style="color: red">NoLogo</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryNoLogo)</span>&quot;
<span style="color: red">SuppressAllWarnings</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectorySuppressAllWarnings)</span>&quot;
<span style="color: red">SuppressSpecificWarnings</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectorySuppressSpecificWarnings)</span>&quot;
<span style="color: red">ToolPath</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(WixToolPath)</span>&quot;
<span style="color: red">TreatWarningsAsErrors</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryTreatWarningsAsErrors)</span>&quot;
<span style="color: red">TreatSpecificWarningsAsErrors</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryTreatSpecificWarningsAsErrors)</span>&quot;
<span style="color: red">VerboseOutput</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryVerboseOutput)</span>&quot;
<span style="color: red">AutogenerateGuids</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryAutogenerateGuids)</span>&quot;
<span style="color: red">GenerateGuidsNow</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectoryGenerateGuidsNow)</span>&quot;
<span style="color: red">OutputFile</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(IntermediateOutputPath)_%(HarvestDirectory.Filename)_dir.wxs</span>&quot;
<span style="color: red">SuppressFragments</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectorySuppressFragments)</span>&quot;
<span style="color: red">SuppressUniqueIds</span><span style="color: blue">=</span>&quot;<span style="color: blue">$(HarvestDirectorySuppressUniqueIds)</span>&quot;
<span style="color: red">Transforms</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.Transforms)</span>&quot;
<span style="color: red">Directory</span><span style="color: blue">=</span>&quot;<span style="color: blue">@(HarvestDirectory)</span>&quot;
<span style="color: red">ComponentGroupName</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.ComponentGroupName)</span>&quot;
<span style="color: red">DirectoryRefId</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.DirectoryRefId)</span>&quot;
<span style="color: red">KeepEmptyDirectories</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.KeepEmptyDirectories)</span>&quot;
<span style="color: red">PreprocessorVariable</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.PreprocessorVariable)</span>&quot;
<span style="color: red">SuppressCom</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.SuppressCom)</span>&quot;
<span style="color: red">SuppressRootDirectory</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.SuppressRootDirectory)</span>&quot;
<span style="color: red">SuppressRegistry</span><span style="color: blue">=</span>&quot;<span style="color: blue">%(HarvestDirectory.SuppressRegistry)</span>&quot; <span style="color: blue">/&gt;</span></pre>

The following table describes the common WiX MSBuild parameters that are applicable
to the <b>HeatDirectory</b> task.

<table border="1" cellspacing="0" cellpadding="4">
    <tr>
        <td>
            <b>Parameter</b>
        </td>
        <td>
            <b>Description</b>
        </td>
    </tr>
    <tr>
        <td>
            <b>NoLogo</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that the tool logo should be suppressed.
            The default is <b>false</b>.
            This is equivalent to the -nologo switch.</td>
    </tr>
    <tr>
        <td>
            <b>SuppressAllWarnings</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that all warnings should be suppressed.
            The default is <b>false</b>.
            This is equivalent to the -sw switch.
        </td>
    </tr>
    <tr>
        <td>
            <b>SuppressSpecificWarnings</b>
        </td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            Specifies that certain warnings should be suppressed.
            This is equivalent to the -sw[N] switch.
        </td>
    </tr>
    <tr>
        <td>
            <b>TreatSpecificWarningsAsErrors</b>
        </td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            Specifies that certain warnings should be treated as errors.
            This is equivalent to the -wx[N] switch.
        </td>
    </tr>
    <tr>
        <td>
            <b>TreatWarningsAsErrors</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that all warnings should be treated as errors.
            The default is <b>false</b>.
            This is equivalent to the -wx switch.
        </td>
    </tr>
    <tr>
        <td>
            <b>VerboseOutput</b>
        </td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Specifies that the tool should provide verbose output.
            The default is <b>false</b>.
            This is equivalent to the -v switch.
        </td>
    </tr>
</table>

&nbsp;

The following table describes the parameters that are 
common to all heat tasks that are applicable to the <b>HeatDirectory</b>
task.

<table border="1" cellspacing="0" cellpadding="4">
    <tr>
        <td>
            <b>Parameter</b>
        </td>
        <td>
            <b>Description</b>
        </td>
    </tr>
    <tr>
        <td>
            <b>AutogenerateGuids</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Whether to generate authoring that relies on auto-generation of component GUIDs.
            The default is $(HarvestAutogenerateGuids) if specified; otherwise, <b>true</b>.
        </td>
    </tr>
    <tr>
        <td>
            <b>GenerateGuidsNow</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Whether to generate authoring that generates durable GUIDs when harvesting. The
            default is $(HarvestGenerateGuidsNow) if specified; otherwise, <b>false</b>.</td>
    </tr>
    <tr>
        <td>
            <b>OutputFile</b></td>
        <td>
            Required <b>item</b> parameter.<br />
            <br />
            Specifies the output file that contains the generated authoring.</td>
    </tr>
    <tr>
        <td>
            <b>SuppressFragments</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Whether to suppress generation of separate fragments when harvesting. The default
            is $(HarvestSuppressFragments) if specified; otherwise, <b>true</b>.</td>
    </tr>
    <tr>
        <td>
            <b>SuppressUniqueIds</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Whether to suppress generation of unique component IDs. The default
            is $(HarvestSuppressUniqueIds) if specified; otherwise, <b>false</b>.</td>
    </tr>
    <tr>
        <td>
            <b>Transforms</b></td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            XSL transforms to apply to all generated WiX authoring. Separate multiple transforms
            with semicolons. The default is $(HarvestTransforms) if specified.</td>
    </tr>
    </table>

&nbsp;

The following table describes the parameters that are specific to the <b>HeatDirectory</b>
task.

<table border="1" cellspacing="0" cellpadding="4">
    <tr>
        <td>
            <b>Parameter</b>
        </td>
        <td>
            <b>Description</b>
        </td>
    </tr>
    <tr>
        <td>
            <b>Directory</b></td>
        <td>
            Required <b>item group</b> parameter.<br />
            <br />
            The list of directories to harvest.</td>
    </tr>
    <tr>
        <td>
            <b>ComponentGroupName</b></td>
        <td>
            Optional <b>string</b> parameter. If you are harvesting multiple directories in your project,
            you should specify this metadata to create unique file names for the generated authoring.<br />
            <br />
            The name of the ComponentGroup to create for all the generated authoring.</td>
    </tr>
    <tr>
        <td>
            <b>DirectoryRefId</b></td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            The ID of the directory to reference instead of TARGETDIR.</td>
    </tr>
    <tr>
        <td>
            <b>KeepEmptyDirectories</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Whether to create Directory entries for empty directories.</td>
    </tr>
    <tr>
        <td>
            <b>PreprocessorVariable</b></td>
        <td>
            Optional <b>string</b> parameter.<br />
            <br />
            Substitute SourceDir for another variable name (ex: var.Dir).</td>
    </tr>
    <tr>
        <td>
            <b>SuppressCom</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Suppress generation of COM registry elements. The default is <b>false</b>.</td>
    </tr>
    <tr>
        <td>
            <b>SuppressRegistry</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Suppress generation of any registry elements. The default is <b>false</b>.</td>
    </tr>
    <tr>
        <td>
            <b>SuppressRootDirectory</b></td>
        <td>
            Optional <b>boolean</b> parameter.<br />
            <br />
            Suppress generation of a Directory element for the parent directory of the file.
            The default is <b>false</b>.</td>
    </tr>
</table>
