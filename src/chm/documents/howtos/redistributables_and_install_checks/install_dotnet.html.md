---
title: How To: Install the .NET Framework Using Burn
layout: documentation
---

# How To: Install the .NET Framework Using Burn
Applications written using the .NET Framework often need to bundle the .NET framework and install it with their application.  Wix 3.6 and later makes this easy with Burn.

## Step 1: Create a bundle for your application
Follow the instructions in [Building Installation Package Bundles](~/bundle/index.html).

## Step 2: Add a reference to one of the .NET PackageGroups
<ol>
<li>Add a reference to WixNetFxExtension to your bundle project.</li>
<li>Add a PackageGroupRef element to your bundle&apos;s chain that references the .NET package required by your application.  For a full list, see [WixNetfxExtension](~/customactions/wixnetfxextension.html). Ensure that the PayloadGroupRef is placed before any other packages that require .NET.</li>
<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Chain</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">PackageGroupRef</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">NetFx45Web</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
    &lt;</font><font size="2" color="#A31515">MsiPackage</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">MyApplication</font><font size="2">"</font><font size="2" color="#FF0000"> SourceFile</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">$(var.MyApplicationSetup.TargetPath)</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
&lt;/</font><font size="2" color="#A31515">Chain</font><font size="2" color="#0000FF">&gt;</font>
</pre>
</ol>

## Step 3: Optionally package the .NET Framework redistributable

The .NET PackageGroups use remote payloads to download the .NET redistributable when required. If you want to create a bundle that does not require Internet connectivity, you can package the .NET redistributable with your bundle. Doing so requires you have a local copy of the redistributable, such as checked in to your source-control system.

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Bundle</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">PayloadGroup</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">NetFx452RedistPayload</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">Payload</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">redist\NDP452-KB2901907-x86-x64-AllOS-ENU.exe</font><font size="2">"</font>
                 <font size="2" color="#FF0000">SourceFile</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">X:\path\to\redists\in\repo\NDP452-KB2901907-x86-x64-AllOS-ENU.exe</font><font size="2">"</font><font size="2" color="#0000FF">/&gt;
    &lt;/</font><font size="2" color="#A31515">PayloadGroup</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">Bundle</font><font size="2" color="#0000FF">&gt;</font>
</pre>

Note that the PackageGroupRef in the bundle's chain is still required.

## Customizing your bootstrapper application
Any native bootstrapper application, including the [WiX Standard Bootstrapper Application](~/bundle/wixstdba/index.html), will work well with bundles that include .NET.

Managed bootstrapper applications must take care when including .NET to ensure that they do not unnecessarily depend on the .NET framework version being installed.

<ol>
<li>Reference the managed bootstrapper application host from your bundle.</li>
<pre><font color="blue">&lt;</font><font color="maroon">BootstrapperApplicationRef</font>
  <font color="red">Id</font>="<font color="blue">ManagedBootstrapperApplicationHost</font>"<font color="blue">&gt;</font>
  <font color="blue">&lt;</font><font color="maroon">Payload</font>
    <font color="red">Name</font>="<font color="blue">BootstrapperCore.config</font>"
    <font color="red">SourceFile</font>="<font color="blue">$(var.MyMBA.TargetDir)\TestUX.BootstrapperCore.config</font>"/<font color="blue">&gt;</font>
  <font color="blue">&lt;</font><font color="maroon">Payload</font>
    <font color="red">SourceFile</font>="<font color="blue">$(var.MyMBA.TargetPath)</font>"/<font color="blue">&gt;</font>
<font color="blue">&lt;</font>/<font color="maroon">BootstrapperApplicationRef</font><font color="blue">&gt;</font></pre>
<li>Target your bootstrapper application to the version of .NET built into the operating system.  For Windows 7, this is .NET 3.5.</li>
<li>Support using the newer versions of .NET if the older versions are not available.  The following example shows the content of the BootstrapperCore.config file.</li>
<pre>
<font color="blue">&lt;</font><font color="maroon">configuration</font><font color="blue">&gt;</font>
  <font color="blue">&lt;</font><font color="maroon">configSections</font><font color="blue">&gt;</font>
    <font color="blue">&lt;</font><font color="maroon">sectionGroup</font> <font color="red">name</font>="<font color="blue">wix.bootstrapper</font>" <font color="red">type</font>="<font color="blue">Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperSectionGroup, BootstrapperCore</font>"<font color="blue">&gt;</font>
      <font color="blue">&lt;</font><font color="maroon">section</font> <font color="red">name</font>="<font color="blue">host</font>" <font color="red">type</font>="<font color="blue">Microsoft.Tools.WindowsInstallerXml.Bootstrapper.HostSection, BootstrapperCore</font>" /<font color="blue">&gt;</font>
    <font color="blue">&lt;</font>/<font color="maroon">sectionGroup</font><font color="blue">&gt;</font>
  <font color="blue">&lt;</font>/<font color="maroon">configSections</font><font color="blue">&gt;</font>
  <font color="blue">&lt;</font><font color="maroon">startup</font> <font color="red">useLegacyV2RuntimeActivationPolicy</font>="<font color="blue">true</font>"<font color="blue">&gt;</font>
    <font color="blue">&lt;</font><font color="maroon">supportedRuntime</font> <font color="red">version</font>="<font color="blue">v2.0.50727</font>" /<font color="blue">&gt;</font>
    <font color="blue">&lt;</font><font color="maroon">supportedRuntime</font> <font color="red">version</font>="<font color="blue">v4.0</font>" /<font color="blue">&gt;</font>
  <font color="blue">&lt;</font>/<font color="maroon">startup</font><font color="blue">&gt;</font>
  <font color="blue">&lt;</font><font color="maroon">wix.bootstrapper</font><font color="blue">&gt;</font>
    <font color="blue">&lt;</font><font color="maroon">host</font> <font color="red">assemblyName</font>="<font color="blue">MyBootstrapperApplicationAssembly</font>"<font color="blue">&gt;</font>
      <font color="blue">&lt;</font><font color="maroon">supportedFramework</font> <font color="red">version</font>="<font color="blue">v3.5</font>" /<font color="blue">&gt;</font>
      <font color="blue">&lt;</font><font color="maroon">supportedFramework</font> <font color="red">version</font>="<font color="blue">v4\Client</font>" /<font color="blue">&gt;</font> 
        <font color="blue">&lt;</font>!-- Example only. Replace the host/@assemblyName attribute with 
        an assembly that implements BootstrapperApplication. --<font color="blue">&gt;</font>
        <font color="blue">&lt;</font><font color="maroon">host</font> <font color="red">assemblyName</font>="<font color="blue">$(var.MyMBA.TargetPath)</font>" /<font color="blue">&gt;</font>
    <font color="blue">&lt;</font>/<font color="maroon">host</font><font color="blue">&gt;</font>
  <font color="blue">&lt;</font>/<font color="maroon">wix.bootstrapper</font><font color="blue">&gt;</font>
<font color="blue">&lt;</font>/<font color="maroon">configuration</font><font color="blue">&gt;</font>
</pre>
</ol>
