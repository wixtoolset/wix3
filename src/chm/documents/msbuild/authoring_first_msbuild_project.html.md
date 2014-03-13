---
title: Creating a .wixproj File
layout: documentation
---
# Creating a .wixproj File

In order to build WiX using MSBuild, a .wixproj file must be created. The easiest way to create a new .wixproj for your installer is to WiX in Visual Studio because it automatically generates standard msbuild project files that can be built on the command line by simply typing:

*msbuild &lt;projectfile&gt;.wixproj*

If you do not have Visual Studio available, a .wixproj file can be created using any text editor. The following is a sample .wixproj file that builds an installer consisting of a single product.wxs file. 
If you want to copy and paste this example, remember to change the &lt;ProjectGuid&gt; 
value to match your own.

<pre><font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Project</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">DefaultTargets</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Build</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">xmlns</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/developer/msbuild/2003</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">Configuration</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Condition</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF"> '$(Configuration)' == '' </font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">Debug</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">Configuration</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">Platform</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Condition</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF"> '$(Platform)' == '' </font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">x86</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">Platform</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">ProductVersion</font><font size="2" color="#0000FF">&gt;</font><font size="2">3.0</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">ProductVersion</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">ProjectGuid</font><font size="2" color="#0000FF">&gt;</font><font size="2">{c523055d-a9d0-4318-ae85-ec934d33204b}</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">ProjectGuid</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">SchemaVersion</font><font size="2" color="#0000FF">&gt;</font><font size="2">2.0</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">SchemaVersion</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">OutputName</font><font size="2" color="#0000FF">&gt;</font><font size="2">WixProject1</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">OutputName</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">OutputType</font><font size="2" color="#0000FF">&gt;</font><font size="2">Package</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">OutputType</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">WixTargetsPath</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Condition</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF"> '$(WixTargetsPath)' == '' </font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font><font size="2">$(MSBuildExtensionsPath)\Microsoft\WiX\v[[Version.Major]].x\Wix.targets</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">WixTargetsPath</font><font size="2" color="#0000FF">&gt;
        &lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Condition</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF"> '$(Configuration)|$(Platform)' == 'Debug|x86' </font><font size="2">"</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">OutputPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">bin\$(Configuration)\</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">OutputPath</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">IntermediateOutputPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">obj\$(Configuration)\</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">IntermediateOutputPath</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">DefineConstants</font><font size="2" color="#0000FF">&gt;</font><font size="2">Debug</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">DefineConstants</font><font size="2" color="#0000FF">&gt;
        &lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Condition</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF"> '$(Configuration)|$(Platform)' == 'Release|x86' </font><font size="2">"</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">OutputPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">bin\$(Configuration)\</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">OutputPath</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">IntermediateOutputPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">obj\$(Configuration)\</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">IntermediateOutputPath</font><font size="2" color="#0000FF">&gt;
        &lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">ItemGroup</font><font size="2" color="#0000FF">&gt;
                &lt;</font><font size="2" color="#A31515">Compile</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Include</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Product.wxs</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
        &lt;/</font><font size="2" color="#A31515">ItemGroup</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Import</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Project</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">$(WixTargetsPath)</font><font size="2">"</font><font size="2" color="#0000FF"> /&gt;
    &lt;/</font><font size="2" color="#A31515">Project</font><font size="2" color="#0000FF">&gt;</font></pre>


Additional .wxs files can be added using additional &lt;Compile&gt; elements within an ItemGroup. Localization files (.wxl) should be added using the &lt;EmbeddedResource&gt; element within an ItemGroup. Include files (.wxi) should be added using the &lt;Content&gt; element within an ItemGroup.
