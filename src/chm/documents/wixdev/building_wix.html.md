---
title: Building WiX
layout: documentation
after: wixdev_getting_started
---

# Building WiX

Simply run `msbuild` from the wix directory; if you have Visual Studio 2012 installed it may be necessary to add `/p:VisualStudioVersion="11.0"`. This will build debug bits into the &quot;build&quot; directory by default. To build release bits, run `msbuild /p:Configuration=Release`

In order to fully build WiX, you must have the following Frameworks and SDKs installed:

  <ul>
    <li>The following components from the <a href="http://www.microsoft.com/downloads/details.aspx?FamilyId=E6E1C3DF-A74F-4207-8586-711EBE331CDC" target="_blank">Windows SDK for Windows Server 2008 and .NET Framework 3.5</a>, Visual Studio 2008, Microsoft Windows 7 SDK, and/or Visual Studio 2010 and/or Visual Studio 2012:</li>

    <li style="list-style: none; display: inline">
      <ul>
        <li>x86 and x64 compilers, headers and libraries</li>

        <li><a href="http://msdn2.microsoft.com/library/ms670169.aspx" target="_blank">HTML Help SDK 1.4</a> or higher [installed to Program Files or Program Files (x86)]</li>
      </ul>
    </li>
  </ul>

To build Sconce and Votive, you must have the following SDKs installed:

* <a href="http://wix.codeplex.com/SourceControl/BrowseLatest" target="_blank">Visual Studio 2005 SDK Version 4.0</a> (choose devbundle branch and browse to src\packages\VS2005SDK)
* <a href="http://www.microsoft.com/en-us/download/details.aspx?id=21827" target="_blank">Visual Studio 2008 SDK</a>
* <a href="http://www.microsoft.com/en-us/download/details.aspx?id=21835" target="_blank">Visual Studio 2010 SP1 SDK</a>
* <a href="http://www.microsoft.com/en-us/download/details.aspx?id=30668" target="_blank">Visual Studio 2012 SDK</a>

More information about the Visual Studio SDK can be found at the <a href="http://msdn.microsoft.com/en-gb/vstudio/vextend.aspx" target="_blank">Visual Studio Extensibility Center</a>.

To install Votive on Visual Studio 2005, 2008, 2010 or 2012, you must have the Standard Edition or higher.

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

* src\Votive\sconce\Properties\AssemblyInfo.cs
* src\Votive\sdk\_vs2010\common\source\csharp\project\AssemblyInfo.cs
* src\Votive\sdk\_vs2010\common\source\csharp\project\attributes.cs

Then run the build:

    msbuild /p:Configuration=Release /p:OFFICIAL_WIX_BUILD=C:\wix.snk
