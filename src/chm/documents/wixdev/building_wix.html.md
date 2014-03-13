---
title: Building WiX
layout: documentation
after: wixdev_getting_started
---

# Building WiX

A one-time setup step is necessary to have .NET trust the partially-signed executables such as GenerateWixInclude.exe that are run during the build.
To do this, either run "msbuild tools\OneTimeWixBuildInitialization.proj" from an elevated Visual Studio prompt, or from an elevated Command Prompt:

    Path\to\Windows_SDK\bin\dir\sn.exe" -Vr *,36e4ce08b8ecfb17
    Path\to\Windows_SDK\bin\dir\x64\sn.exe" -Vr *,36e4ce08b8ecfb17
    
Where Path\to\Windows_SDK\bin\dir is one of:

    C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin
    C:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools
    C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools
    
Then, simply run `msbuild` from the root directory of your clone of the WiX Git repository. Running `msbuild` builds `wix.proj`. If you have Visual Studio 2012 installed it may be necessary to add `/p:VisualStudioVersion="11.0"`; if you have Visual Studio 2013 installed it may be necessary to add `/p:PlatformToolset=v120_xp`. This will build debug bits into the &quot;build&quot; directory by default. To build release bits, run `msbuild /p:Configuration=Release`.

In order to fully build WiX, you must have the following Frameworks and SDKs installed:

  <ul>
    <li>The following components from the <a href="http://www.microsoft.com/en-us/download/details.aspx?id=3138" target="_blank">Microsoft Windows SDK for Windows 7 and .NET Framework 3.5 SP1</a>, and/or Visual Studio 2010 and/or Visual Studio 2012:</li>

    <li style="list-style: none; display: inline">
      <ul>
        <li>x86 and x64 compilers, headers and libraries</li>

        <li><a href="http://msdn.microsoft.com/library/ms670169.aspx" target="_blank">HTML Help SDK 1.4</a> or higher [installed to Program Files or Program Files (x86)] (Note that this component is installed by default by Visual Studio 2013.)</li>
      </ul>
    </li>
  </ul>

To build Votive, you must have Visual Studio 2010 and the [Visual Studio 2010 SP1 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=21835) installed.

More information about the Visual Studio SDK can be found at the <a href="http://msdn.microsoft.com/en-us/vstudio/vextend.aspx" target="_blank">Visual Studio Extensibility Center</a>.

To install Votive on Visual Studio 2010, 2012, or 2013, you must have the Professional Edition or higher. The Express editions of Visual Studio do not support packages like Votive.

To build DTF help files, you need the following tools:

* [Sandcastle May 2008 Release](http://sandcastle.codeplex.com/releases/view/13873)
* [Sandcastle Help File Builder 1.8.0.3](http://shfb.codeplex.com/releases/view/29710)

The DTF help build process looks for these tools in an &quot;external&quot; directory parallel to the WiX &quot;src&quot; directory:

* Sandcastle: external\Sandcastle
* Sandcastle Help File Builder: external\SandcastleBuilder

To create a build that can be installed on different machines, create a new strong name key pair and point OFFICIAL\_WIX\_BUILD to it:

    sn -k wix.snk
    sn -p wix.snk wix.pub
    sn -tp wix.pub

Copy the public key and add new InternalsVisibleTo lines in:

* src/Votive/votive2010/vssdk/AssemblyInfo.cs

Then run the build:

    msbuild /p:Configuration=Release /p:OFFICIAL_WIX_BUILD=C:\wix.snk
