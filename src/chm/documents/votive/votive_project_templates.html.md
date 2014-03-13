---
title: Project Templates
layout: documentation
---

# Project Templates

The WiX Visual Studio package provides the following Visual Studio project templates:

* **WiX Project** - used to create a new Windows Installer package (.msi) file. Each new WiX project includes a .wxs file that consists of a &lt;Product&gt; element that contains a skeleton with the WiX authoring required to create a fully functional Windows Installer package. The &lt;Product&gt; element includes &lt;Package&gt;, &lt;Media&gt;, &lt;Directory&gt;, &lt;Component&gt; and &lt;Feature&gt; elements.
* **WiX Library Project** - used to create a new WiX library (.wixlib) file. A .wixlib file is a library of setup functionality that can be easily shared across different WiX-based packages by including it when linking the setup package. Each new WiX library project includes a .wxs file that consists of an empty &lt;Fragment&gt; element that can be populated with WiX authoring that can be shared by multiple packages.
* **WiX Merge Module Project** - used to create a new Windows Installer merge module (.msm) file. A merge module contains a set of Windows Installer resources that can be shared by multiple Windows Installer installation packages by merging the contents of the module into the .msi package. Each new WiX merge module project includes a .wxs file that consists of a &lt;Module&gt; element that contains a skeleton with the WiX authoring required to create a fully functional merge module. The &lt;Module&gt; element includes &lt;Package&gt;, &lt;Directory&gt; and &lt;Component&gt; elements.

To create a new project:

1. Click on File | New | Project&nbsp; on the Visual Studio menu.
1. Navigate to the Windows Installer XML node.
1. Select the project template and press OK.
