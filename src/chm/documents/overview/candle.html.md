---
title: Compiler
layout: documentation
after: preprocessor
---
# Compiler

The Windows Installer XML compiler is exposed by candle.exe. Candle is responsible for preprocessing the input .wxs files into valid well-formed XML documents against the WiX schema, wix.xsd. Then, each post-processed source file is compiled into a .wixobj file.

The compilation process is relatively straight forward. The WiX schema lends itself to a simple recursive descent parser. The compiler processes each element in turn creating new symbols, calculating the necessary references and generating the raw data for the .wixobj file.
