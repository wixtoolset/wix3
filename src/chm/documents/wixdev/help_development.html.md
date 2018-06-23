---
title: Adding to the WiX Documentation
layout: documentation
after: votive_development
---

# Adding to the WiX Documentation

WiX documentation is compiled into the file WiX.chm as a part of the WiX build process. The source files for help are located in the wix\src\chm directory. The documentation is written in [markdown](http://daringfireball.net/projects/markdown/syntax).

## What the WiX help compiler does

The WiX help compiler does the following:

* Parses .xsd schema files referenced in chm.helpproj and generates help topics for the attributes and elements that are annotated in the .xsd files.

* Compiles all of the markdown files contained in the **documents** directory into HTML stripping the final file extension (.md).
> Each markdown document consists of a metadata header followed by the content of the topic page.

* Sorts the HTML files according to the **after** metadata and includes all the HTML and content files processed in the list of documentation to build into the CHM.

## How to add a new topic to WiX.chm

Adding a new topic to WiX.chm requires the following steps:

1. Fork the wix3 repository from https://github.com/wixtoolset/wix3.
1. Add a new markdown document with the contents of the new topic to the WiX source tree under src\chm\documents.
1. Add any relevant images to the src\chm\files\content sub-directory in the WiX source tree.  
   When forming paths to internal content, the contents of the *documents* and *files* directories are merged into the **~/** directory.
1. Add the metadata at the top of your topic document. Set the *title* metadata to the name of the topic.
   Set the *layout* metadata to the *documentation* layout type, and optionally set the *after* metadata to the basename
   (without the .html[.md] extension) of the topic this page will follow.
1. Commit, push, and make a pull request to `wixtoolset:develop`.

An example of the metadata header (includes the triple-dash delimiting lines):

    ---
    title: Adding to the WiX Documentation
    layout: documentation
    after: votive_development
    ---

Help topics may contain links to external Web pages, and may also contain relative links to other help topics or attributes or elements defined in one of the .xsd schema files.

To build the new content type `msbuild` from the command line in the src\chm directory.  
It is not necessary to build the entire toolset to build the documentation, but you must first build the tools\src directory once (using the same build command) before building the chm.
To build the tools, you will need to install the **Desktop development with C++** workload in Visual Studio, using the Visual Studio Installer.
