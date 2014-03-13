---
title: Integrating WiX Projects Into Daily Builds
layout: documentation
---

# Integrating WiX Projects Into Daily Builds

One of the most common reasons for using MSBuild with WiX project files is to integrate the build of an installer into an existing daily build process. This is often coupled with a need to build WiX projects without having to pre-install any WiX tools on the daily build machine. WiX projects and the WiX tools to build them can be added to most daily build processes that support MSBuild using a few simple steps.

## Step 1: Check in the WiX Tools

To avoid having to install WiX on build machines you can check all the tools necessary to build WiX projects into your source code control system. Here&apos;s how:

1. Create a directory in your source code control system to hold the WiX tools. It&apos;s common to create a numbered subdirectory matching the version of WiX that you&apos;re checking in. Ex: **wix\\[[Version]]**
1. Unzip the contents of <strong>wix[[Version.Major]][[Version.Minor]]-binaries.zip\\*</strong> into the directory created in step 1.
1. If you use Deployment Tools Foundation or the WiX SDK header files and libraries, create a parallel directory tree to the one you created in step 1 and copy the contents of <strong>wix[[Version.Major]][[Version.Minor]]-binaries.zip\sdk\\*</strong> into that directory.
1. Add and check in the files from steps 1 through 3.

## Step 2: Modify Your .wixproj File

After checking the WiX tools into source code control the .wixproj file must be modified to point to the location of the checked in tools. Open the .wixproj file in any text editor, such as Visual Studio, and add the following to the file anywhere between the &lt;Project&gt; element before the &lt;Import&gt; element:

<pre><font size="2" color="#0000FF">&lt;<font size="2" color="#A31515">PropertyGroup</font>&gt;
      &lt;</font><font size="2" color="#A31515">WixToolPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">$(SourceCodeControlRoot)\wix\[[Version]]</font><font size="2" color="#0000FF">\&lt;/</font><font size="2" color="#A31515">WixToolPath</font><font size="2" color="#0000FF">&gt;
      &lt;</font><font size="2" color="#A31515">WixTargetsPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">$(WixToolPath)Wix.targets</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">WixTargetsPath</font><font size="2" color="#0000FF">&gt;
      &lt;</font><font size="2" color="#A31515">WixTasksPath</font><font size="2" color="#0000FF">&gt;</font><font size="2">$(WixToolPath)wixtasks.dll</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">WixTasksPath</font><font size="2" color="#0000FF">&gt;
&lt;</font><font size="2" color="#A31515">/PropertyGroup</font><font size="2" color="#0000FF">&gt;</font></pre>

The WixToolPath must be set to point to the location of the WiX tools directory created in Step 1. The method used to reference the location will vary depending on your build system, but common choices are an MSBuild property that is set via an environment variable (such as **$(BinariesRoot)** in a Team Foundation Server build) or a custom property passed in on the command-line.

You can also use a relative path to the directory (such as <strong>..\\..\\tools\\</strong>), but note that the WixTargetsPath property value must be relative to the .wixproj project file that uses it. The WixTasksPath property is used inside wix.targets to load WixTasks.dll; its value, if a relative path, must be relative to the wix.targets file. Those two files usually live together, so the value would be WixTasks.dll with no extra path information.

**Note that WixToolPath must end in a backslash.**
