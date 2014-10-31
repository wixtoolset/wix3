---
title: Project Property Pages
layout: documentation
after: votive_item_templates
---

# Project Property Pages

To access the WiX project property pages, right-click on a WiX project in the Visual Studio Solution Explorer and choose Properties. WiX projects contain the following property pages:

* Installer
* Build
* Build Events
* Paths
* Tool Settings

## Installer Property Page

The Installer tab contains the following configurable options:

* <b>Output name</b> - a text box that contains the name of the file that will be created by the build process.
* <b>Output type</b> - a drop-down list that allows you to select the output type: An MSI package, merge module, WiX library, or bootstrapper.

## Build Property Page

The Build tab contains the following configurable options:

* The <b>General</b> section allows you to define configuration-specific constants and specify the culture to build.   For more information see [Specifying cultures to build](~/howtos/ui_and_localization/specifying_cultures_to_build.html).
* The <b>Messages</b> section allows you to specify warning levels, toggle treating warnings as errors and verbose output.
* The <b>Output</b> section allows you to specify the output path, toggle delete temproary files, suppress output of the wixpdb file, and toggle whether or not to bind files into the library file (if it is a WiX Library project).

## Build Events Property Page

The Build Events tab contains the following configurable options:

* <b>Pre-build event command line</b> - a text box that contains the pre-build events to execute before building the current project.
* <b>Post-build event command line</b> - a text box that contains the post-build events to execute after building the current project.
* <b>Run the post-build event</b> - a drop-down combo box that allows you to specify the conditions in which post-build events should be executed.

The Build Events tab contains buttons named <b>Edit Pre-build...</b> and <b>Edit Post-build...</b> that display edit dialogs for the pre and post-build event command lines. The edit dialogs contain a list of all valid WiX project reference variables and their values based on the current project settings.

## Paths Property Page

The Paths tab contains the following configurable options:

* The <b>Reference Paths</b> section allows you to define paths you want to use when locating references (WiX extensions and 
WiX libraries).
* The <b>Include Paths</b> section allows you to define paths you want to use when locating WiX Include files.

## Tool Settings Property Page

The Tool Settings tab contains the following configurable options:

* The <b>ICE validation</b> section allows you to toggle ICE validation suppression or specify which ICE validation to suppress.
* The <b>Additional parameters</b> section allows you to specify command line arguments to pass directly to the WiX tools 
at build time.
