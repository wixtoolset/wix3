---
title: Introduction to Developing WiX Extensions
layout: documentation
---
# Introduction to Developing WiX Extensions

## Common Requirements
In order to understand how each of the classes of extensions work, one should start by looking at the WiX source code. All extensions have the following things in common:

* Implemented using the .NET Framework 2.0. The rest of the WiX toolset currently only depends on the .NET Framework 2.0, so in order to ensure backwards compatibility, it is a best practice to develop new extensions so that they only depend on the .NET Framework 2.0 as well.
* Build a subclass of the appropriate extension object, which gives it an easily distinguishable name.
* Build a schema of the appropriate syntax to provide validation checking where possible.
* Build internal table definitions and register them with the compiler.
* Build overrides for extensible methods and virtual members which will get invoked at the approriate location during the single pass compile.
* Compiled into a DLL.
* Placed next to WiX EXEs along with all other WiX extension DLLs.
* Registered with WiX by passing the path of the exension DLL as a command line argument to the compiler and/or linker.

## Considerations

Before investing in an extension, one should evaluate whether an external tool and the ?include? syntax (from the preprocessor) will provide the needed flexibility for your technical needs.

Multiple extensions and extension types are supported, but there is no guarantee of the order in which a particular class of extensions will be processed. As a result, there must not be any sequencing dependencies between extensions within the same extension class.

Extension developers might also implement a RequiredVersion attribute on the [Wix](~/xsd/wix/wix.html) element. This allows setup developers using your extension to require a specific version of the extension in case a new feature is introduced or a breaking change is made. You can add an attribute to the Wix element in an extension as shown in the following example.

    <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
        xmlns:xse="http://schemas.microsoft.com/wix/2005/XmlSchemaExtension">
      <xs:attribute name="RequiredVersion" type="xs:string">
        <xs:annotation>
          <xs:documentation>
            The version of this extension required to compile the defining source.
          </xs:documentation>
          <xs:appinfo>
            <xse:parent namespace="http://schemas.microsoft.com/wix/2006/wi" ref="Wix" />
          </xs:appinfo>
        </xs:annotation>
      </xs:attribute>
    </xs:schema>
