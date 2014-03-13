---
title: Extensions
layout: documentation
after: extension_development_intro
---

# Extensions

WiX supports the following classes of extensions:

* <b>Binder Extensions</b> allow clients to modify the behavior of the Binder.
* <b>BinderFileManager Extensions</b> allow clients to simply modify the file source resolution and file differencing features of the Binder.
* <b>Compiler Extensions</b> allow clients to custom compile authored XML into internal table representation before it is written to binary form.
* <b>Decompiler Extensions</b> allow clients to decompile custom tables into XML.
* <b>Harvester Extensions</b> allow clients to modify the behavior of the Harvester.
* <b>Inspector Extensions</b> allow clients to inspect source, intermediate, and output documents at various times during the build to validate business rules as early as possible.
* <b>Mutator Extensions</b> allow clients to modify the behavior of the Mutator.
* <b>Preprocessor Extensions</b> allow clients to modify authoring files before they are processed by the compiler.
* <b>Unbinder Extensions</b> allow clients to modify the behavior of the Unbinder.
* <b>Validator Extensions</b> allow clients to process ICE validation messages. By default, ICE validation messages are output to the console.
* <b>WixBinder Extensions</b> allow clients to completely change the Binder to, for example, create different output formats from WiX authoring.

For information on how to use WiX extensions on the command line or inside the Visual Studio IDE, please visit the [Using WiX extensions](~/howtos/general/extension_usage_introduction.html) topic.

For information on how to use localized WiX extensions, please visit the [Localized extensions](localized_extensions.html) topic.
