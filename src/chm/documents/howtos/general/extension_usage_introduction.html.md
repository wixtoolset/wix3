---
title: How To: Use WiX Extensions
layout: documentation
---
# How To: Use WiX Extensions

The WiX extensions can be used both on the command line and within the Visual Studio IDE. When you use WiX extensions in the Visual Studio IDE, you can also enable IntelliSense for each WiX extension.

## Using WiX extensions on the command line

To use a WiX extension when calling the WiX tools from the command line, use the -ext command line parameter and supply the extension assembly (DLL) needed for your project. Each extension DLL must be passed in via separate -ext parameters. For example:

    light.exe MySetup.wixobj
     -ext WixUIExtension
     -ext WixUtilExtension
     -ext "C:\My WiX Extensions\FooExtension.dll"
     -out MySetup.msi

Extension assemblies in the same directory as the WiX tools can be referred to without path or .dll extension. Extension assemblies in other directories must use a complete path name, including .dll extension.

Note: <a href="http://msdn.microsoft.com/library/930b76w0.aspx" target="_blank">Code Access Security</a> manages the trust levels of assemblies loaded by managed code, including WiX extensions. By default, CAS prevents a WiX tool running on a local machine from loading a WiX extension on a network share.

## Using WiX extensions in Visual Studio

To use a WiX extension when building in Visual Studio with the WiX Visual Studio package:

1. Right-click on the WiX project in the Visual Studio solution explorer and select Add Reference...
1. In the Add WiX Library Reference dialog, click on the Browse tab and browse to the WiX extension DLL that you want to include.
1. Click the Add button to add a reference to the chosen extension DLL.
1. Browse and add other extension DLLs as needed.

To enable IntelliSense for a WiX extension in the Visual Studio IDE, you need to add an XMLNS declaration to the &lt;Wix&gt; element in your .wxs file. For example, if you want to use the NativeImage functionality in the WixNetFxExtension, the &lt;Wix&gt; element would look like the following:

    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
         xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension">

After adding this, you can add an element named &lt;netfx:NativeImage/&gt; and view IntelliSense for the attributes supported by the NativeImage element.
