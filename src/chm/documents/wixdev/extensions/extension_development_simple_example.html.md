---
title: Creating a Simple WiX Extension
layout: documentation
after: extensions
---

# Creating a Skeleton WiX Extension

WiX extensions are used to extend and customize what WiX builds and how it builds it.

The first step in creating a WiX extension is to create a class that extends the WixExtension class. This class will be the container for all the extensions you plan on implementing. This can be done by using the following steps:

1. In Visual Studio, create a new C# library (.dll) project named SampleWixExtension.
1. Add a reference to wix.dll to your project.
1. Add a using statement that refers to the Microsoft.Tools.WindowsInstallerXml namespace.

        using Microsoft.Tools.WindowsInstallerXml;
1. Make your SampleWixExtension class inherit from WixExtension.

        public class SampleWixExtension : WixExtension {}
1. Add the AssemblyDefaultWixExtensionAttribute to your AssemblyInfo.cs.

        [assembly: AssemblyDefaultWixExtension(typeof(SampleWixExtension.SampleWixExtension))]
1. Build the project.

Although this WiX extension will not do anything yet, you can now pass the newly built SampleWixExtension.dll on the command line to the Candle and Light by using the -ext flag like the following:

    candle.exe Product.wxs -ext SampleWixExtension.dll
    light.exe Product.wxs -ext SampleWixExtension.dll

This covers the basics of creating the skeleton of an extension. You can now use 
this skeleton code to build your own custom action. After you are done, you can 
author the custom action in the WiX source code by following the [Adding a Custom Action](authoring_custom_actions.html) topic. 
You can also build your own extensions to the WiX toolset using this skeleton 
code. For an example of building an extension, see
[Creating a Preprocessor Extension](extension_development_preprocessor.html).
