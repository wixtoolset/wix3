---
title: How To: NGen Managed Assemblies During Installation
layout: documentation
after: create_uninstall_shortcut
---
# How To: NGen Managed Assemblies During Installation
<a target="_blank" href="http://msdn.microsoft.com/en-us/magazine/cc163808.aspx">NGen</a> during installation can improve your managed application&apos;s startup time by creating native images of the managed assemblies on the target machine. This how to describes using the WiX support to NGen managed assemblies at install time.

## Step 1: Add the WiX .NET extensions library to your project
The WiX support for NGen is included in a WiX extension library that must be added to your project prior to use. If you are using WiX on the command-line you need to add the following to your candle and light command lines:

    -ext WixNetFxExtension

If you are using WiX in Visual Studio you can add the extensions using the Add Reference dialog:

1. Open your WiX project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the <strong>WixNetFxExtension.dll</strong> assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add the WiX .NET extensions namespace to your project
Once the library is added to your project you need to add the .NET extensions namespace to your project so you can access the appropriate WiX elements. To do this modify the top-level [&lt;Wix&gt;](~/xsd/wix/wix.html) element in your project by adding the following attribute:

<pre>
<font size="2" color="#FF0000">xmlns:<font size="2" color="#FF0000">netfx</font></font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/NetFxExtension</font><font size="2">"</font>
</pre>

A complete Wix element with the standard namespace and the .NET extensions namespace added looks like this:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Wix</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">xmlns</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/2006/wi</font><font size="2">"</font>
<font size="2" color="#FF0000">     xmlns:netfx</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/NetFxExtension</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font>
</pre>

## Step 3: Mark the managed files for NGen
Once you have the .NET extension library and namespace added to your project you can use the [&lt;NetFx:NativeImage&gt;](~/xsd/netfx/nativeimage.html) element to enable NGen on your managed assemblies. The NativeImage element goes inside a parent File element:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Component</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">myapplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Guid</font><font size="2" color="#0000FF">=</font><font size="2">"<a href="~/howtos/general/generate_guids.html">PUT-GUID-HERE</a>"</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">File</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"myapplication.exe"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Source</font><font size="2" color="#0000FF">=</font><font size="2">"MySourceFiles</font><font size="2" color="#0000FF">\MyApplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">KeyPath</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Checksum</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">yes</font><font size="2">"</font>&gt;<font size="2" color="#0000FF">
        &lt;</font><font size="2" color="#A31515">netfx:NativeImage</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">ngen_MyApplication.exe</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Platform</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">32bit</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Priority</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">0</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">AppBaseDirectory</font><font size="2" color="#0000FF">=</font><font size="2">"APPLICATIONROOTDIRECTORY"</font><font size="2" color="#0000FF">/&gt;
    &lt;/<font size="2" color="#A31515">File</font>&gt;
&lt;/<font size="2" color="#A31515">Component</font>&gt;</font>
</pre>

The Id attribute is a unique identifier for the native image. The Platform attribute specifies the platforms for which the native image should be generated, in this case 32-bit. The Priority attribute specifies when the image generation should occur, in this case immediately during the setup process. The AppBaseDirectory attribute identifies the directory to use to search for dependent assemblies during the image generation. In this case it is set to the install directory for the application.
