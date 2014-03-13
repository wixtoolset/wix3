---
title: Tools and Concepts
layout: documentation
after: /main/
---
# Tools and Concepts

The WiX toolset is tightly coupled with the Windows Installer technology. In 
order to fully utilize the features in WiX, you must be familiar with the 
Windows Installer concepts. This section assumes you have a working knowledge of 
the Windows Installer database format. For information on Windows Installer, see [Useful Windows Installer Information](msi_useful_links.html).

## WiX File Types
There is a set of tools that WiX offers to fulfill the needs of building Windows 
Installer-based packages. Each tool outputs a type of file that can be consumed 
as inputs of another tool. After processing through the appropriate tools, the 
final installer is produced.

To get familiar with the WiX file types, see [File Types](files.html).

## WiX Tools

Once you are familiar with the file types, see how the file types are produced 
by what WiX tools by visiting [List of Tools](alltools.html).
For a graphical view of the WiX tools and how they interact with each other, see
[WiX Toolset Diagram](tools.html).

## WiX Schema

The core WiX schema is a close mirror with the MSI tables. For helpful hints on how the WiX schema maps to MSI tables, see [MSI Tables to WiX Schema](msitowix.html).
