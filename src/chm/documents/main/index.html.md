---
title: Windows Installer XML (WiX)
chm: default
layout: documentation
---
# Introduction to Windows Installer XML (WiX) toolset

## What is WiX?

WiX is a set of tools that allows you to create Windows Installer-based 
deployment packages for your application. The WiX toolset is based on a 
declarative XML authoring model. You can use WiX on the command line by using the WiX tools 
or MSBuild. In addition, there is also a WiX Visual Studio plug-in that supports 
VS2005, VS2008, and VS2010. The WiX toolset supports building the following types of 
Windows Installer files:

* Installer (.msi)
* Patches (.msp)
* Merge Modules (.msm)
* Transforms (.mst)

WiX supports a broad spectrum of Windows Installer features. In addition, WiX 
also offers a set of built-in custom actions that can be used and incorporated 
in Windows Installer packages. The custom actions are offered in a set of WiX 
extensions. Some common WiX extensions include support for Internet Information 
System (IIS), Structured Query Language (SQL), the .NET Framework, Visual 
Studio, and Windows etc.

## How does WiX work?

The WiX source code is written in XML format with a .wxs file extension. 
The WiX tools follow the traditional compile and link model used to create 
executables from source code. At build time, the WiX source files are validated 
against the core WiX schema, then processed by a preprocessor, compiler, and 
linker to create the final result. There are a set of WiX tools that can be used 
to produce different output types.
For a complete list of file types and tools in WiX, see the [File Types](~/overview/files.html)
and the
[List of Tools](~/overview/alltools.html) sections.

See the following topics for more detailed information:

* [Fundamental Tools and Concepts](~/overview/index.html)
* [Creating Installation Package Bundles](~/bundle/index.html)
* [Working in Visual Studio](~/votive/index.html)
* [Working with MSBuild](~/msbuild/index.html)
* [How To Guides](~/howtos/index.html)
* [Standard Custom Actions](~/customactions/index.html)
* [Creating an Installation Patch](~/patching/index.html)
* [WiX Schema Reference](~/xsd/index.html)
* [Developing for WiX](~/wixdev/index.html)

## WiX system requirements

WiX supports both .NET 3.5 and 4.0 and later. WiX's MSBuild supports requires .NET 3.5,
which is not installed by default on Windows 8 and Windows Server 2012 and later. To
install the .NET 3.5 feature, go to Control Panel, open Programs and Features, and 
choose *Turn Windows features on or off*. In the list of features, choose *.NET 
Framework 3.5 (includes .NET 2.0 and 3.0)* and then choose OK.

In the next version of WiX (v3.11), .NET 4.0 will be required; building using .NET 3.5
will no longer be supported.
