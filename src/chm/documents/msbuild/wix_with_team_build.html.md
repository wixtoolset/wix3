---
title: Building WiX Projects In Team Foundation Build
layout: documentation
after: daily_builds
---
# Building WiX Projects In Team Foundation Build

Once you have created a [WiX project file](authoring_first_msbuild_project.html), you need to perform some additional steps in order to successfully build the WiX project in Team Foundation Build. Without these additional steps, the WiX project will be ignored by default by Team Foundation Build even though it is an MSBuild-compatible project.

## Step 1: Update the Solution Build Configuration
  
By default, WiX projects will not be built when building the &apos;Any CPU&apos; platform because Windows Installer packages are CPU-specific. As a result, you need to use the following steps to update the solution build configuration to include your WiX project and its dependencies as part of a Team Foundation Build.

1. In the solution, open Configuration Manager (Build | Configuration Manager).
1. Set the &apos;Debug&apos; configuration as the active configuration.
1. Select the &apos;x86&apos; platform that you plan to build from the drop-down list.
1. Ensure that the WiX project is checked in the &apos;Build&apos; column.
1. Ensure that any project references that the WiX project uses are also checked in the &apos;Build&apos; column.
1. Set the &apos;Release&apos; configuration as the active configuration.
1. Repeat steps 3-5 to ensure that the WiX project and its dependencies will build for the &apos;Release&apos; configuration.
1. If you plan to build the &apos;x64&apos; platform, repeat steps 3-7 for the &apos;x64&apos; platform.
1. Close Configuration Manager and save the solution.

## Step 2: Add the Build Configurations to TFSBuild.proj

Now that you have added the WiX project and its dependent projects to the &apos;x86&apos; and/or &apos;x64&apos; build configurations, Team Foundation Build will build your WiX project in these build configurations. However, these build configurations may not be specified in your Team Foundation Build Definition (TFSBuild.proj).

When you create a new Build Definition, you can select the &apos;Debug/Mixed Platforms&apos; and &apos;Release/Mixed Platforms&apos; build configurations to build all projects in your solution, including WiX projects.

If you have an existing Build Definition, you need to use the following steps to modify it so it will build WiX projects along with the other projects in your solution.

<ol>
  <li>Right-click on the Build Definition and select View Configuration Folder.</li>
  <li>Check out and open the file named TFSBuild.proj.</li>
  <li>Add the following build configurations to the &lt;ConfigurationToBuild&gt; section if they do not already exist there, or update them if they do already exist:

<pre><font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">ConfigurationToBuild</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Include</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Debug|Mixed Platforms</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">FlavorToBuild</font><font size="2" color="#0000FF">&gt;</font><font size="2">Debug</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">FlavorToBuild</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">PlatformToBuild</font><font size="2" color="#0000FF">&gt;</font><font size="2">Mixed Platforms</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">PlatformToBuild</font><font size="2" color="#0000FF">&gt;
&lt;</font><font size="2" color="#A31515">/ConfigurationToBuild</font><font size="2" color="#0000FF">&gt;</font>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">ConfigurationToBuild</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Include</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">Release|Mixed Platforms</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">FlavorToBuild</font><font size="2" color="#0000FF">&gt;</font><font size="2">Release</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">FlavorToBuild</font><font size="2" color="#0000FF">&gt;
        &lt;</font><font size="2" color="#A31515">PlatformToBuild</font><font size="2" color="#0000FF">&gt;</font><font size="2">Mixed Platforms</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">PlatformToBuild</font><font size="2" color="#0000FF">&gt;
&lt;</font><font size="2" color="#A31515">/ConfigurationToBuild</font><font size="2" color="#0000FF">&gt;</font></pre>

  </li>
  <li>Close, save and check in the changes to TFSBuild.proj.</li>
</ol>

After making the above changes and queuing the build, you will see folders named &apos;Debug&apos; and &apos;Release&apos; in the build output. Each of these folders will contain a sub-folder named &apos;en-us&apos; (or another culture depending on the settings in the WiX project) that contains the built Windows Installer package.
