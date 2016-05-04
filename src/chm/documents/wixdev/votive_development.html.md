---
title: Developing for Votive
layout: documentation
---
# Developing for Votive

If you want to contribute code to the Votive project or debug Votive, you must download and install the Visual Studio 2010 SDK, available at the <a href="http://msdn.microsoft.com/en-gb/vstudio/vextend.aspx" target="_blank">Visual Studio Extensibility Developer Center</a>. The Visual Studio 2010 SDK is non-invasive and will create an experimental hive in the registry that will leave your retail version of Visual Studio 2010 unaffected.

To start debugging Votive, set your breakpoints then press F5 in the Wix.sln for Visual Studio. The custom build actions in the Votive project will set up and register Votive in the experimental hive, so running Wix39.exe is not required, nor suggested.
