---
title: Linker (light)
layout: documentation
after: candle
---

# Linker (light)

The Windows Installer XML linker is exposed by light.exe. Light is responsible for processing one or more .wixobj files, retrieving metadata from various external files and creating a Windows Installer database (MSI or MSM). When necessary, light will also create cabinets and embed streams in the created Windows Installer database.

The linker begins by searching the set of object files provided on the command line to find the entry section. If more than one entry section is found, light fails with an error. This failure is necessary because the entry section defines what type of Windows Installer database is being created, a MSI or MSM. It is not possible to create two databases from a single link operation.

While the linker was determining the entry section, the symbols defined in each object file are stored in a symbol table. After the entry section is found, the linker attempts to resolve all of the references in the section by finding symbols in the symbol table. When a symbol is found in a different section, the linker recursively attempts to resolve references in the new section. This process of gathering the sections necessary to resolve all of the references continues until all references are satisfied. If a symbol cannot be found in any of the provided object files, the linker aborts processing with an error indicating the undefined symbol.

After all of the sections have been found, complex and reverse references are processed. This processing is where Components and Merge Modules are hooked to their parent Features or, in the case of Merge Modules, Components are added to the ModuleComponents table. The reverse reference processing adds the appropriate Feature identifier to the necessary fields for elements like, Shortcut, Class, and TypeLib.

Once all of the references are resolved, the linker processes all of the rows retrieving the language, version, and hash for referenced files, calculating the media layout, and including the necessary standard actions to ensure a successful installation sequence. This part of the processing typically ends up generating additional rows that get added associated with the entry section to ensure they are included in the final Windows Installer database.

Finally, light works through the mechanics of generating IDT files and importing them into the Windows Installer database. After the database is fully created, the final post processing is done to merge in any Merge Modules and create a cabinet if necessary. The result is a fully functional Windows Installer database.

## Usage Information

    light.exe [-?] [-b basePath] [-nologo] [-out outputFile] objectFile [objectFile ...] [@responseFile]

Light supports the following command line parameters:

<table border="1" cellspacing="0" cellpadding="2" class="style1">
  <tr>
    <td valign="top">
      <p><b>Switch</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ai</p>
    </td>
    <td>
      <p>Allow identical rows; identical rows will be treated as a warning.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-au</p>
    </td>
    <td>
      <p>Allow unresolved references; this will cause invalid output to be created.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-b &lt;path&gt;</p>
    </td>
    <td>
      <p>Specify a base path to locate all files; the default value is the current working directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-bcgg</p>
    </td>
    <td>
      <p>Use backwards compatible guid generation algorithm (rarely needed). </p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-bf</p>
    </td>
    <td>
      <p>Bind files into a wixout; this switch is only valid when also providing the -xo option.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-binder &lt;classname&gt;</p>
    </td>
    <td>
      <p>Specify a specific custom binder to use provided by an extension.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-cc</p>
    </td>
    <td>
      <p>Specify a path to cache built cabinet files; the path will not be deleted after linking.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ct &lt;N&gt;</p>
    </td>
    <td>
      <p>Specify the number of threads to use when creating cabinets; the default is the %NUMBER_OF_PROCESSORS% environment variable.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-cultures:&lt;cultures&gt;</p>
    </td>
    <td>
      <p>Specifies a semicolon or comma delimited list of localized string cultures to load from .wxl files and libraries.  Precedence of cultures is from left to right.  For more information see <a href="~/howtos/ui_and_localization/specifying_cultures_to_build.html">Specifying cultures to build</a>.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-cub</p>
    </td>
    <td>
      <p>Provide a .cub file containing additional internal consistency evaluators (ICEs) to run.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-d&lt;name&gt;=&lt;value&gt;</p>
    </td>
    <td>
      <p>Define a WiX variable.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-dcl:level</p>
    </td>
    <td>
      <p>Set the default cabinet compression level. Possible values are low, medium, high, 
            none, and mszip (default).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-dut</p>
    </td>
    <td>
      <p>Drop unreal tables from the output image.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-eav</p>
    </td>
    <td>
      <p>Exact assembly versions. If this option is not specified, the assembly version is padded with zeros
           in certain cases to work around a bug that exists in the initial release of the .NET Framework 1.1.
           This bug was subsequently fixed in the .NET Framework 1.1 SP1. Use this option if you require non-padded
           assembly versions in the MsiAssemblyName table (or in relevant bind variables), and do not mind if your
           MSI is incompatible with the initial release of the .NET Framework 1.1. For more information, see
           <A href="http://blogs.msdn.com/b/astebner/archive/2005/02/12/371646.aspx" target="_blank">this blog post</A>.
           <BR/><BR/>Note that when using this option, your setup will still be compatible with the .NET Framework 1.0 RTM,
           .NET Framework 1.1 SP1, .NET Framework 2.0, and later versions of the .NET Framework.
           <BR/><BR/>This property is available starting with WiX v3.5.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ext</p>
    </td>
    <td>
      <p>Specify an extension assembly.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-fv</p>
    </td>
    <td>
      <p>Add a FileVersion attribute to each assembly in the MsiAssemblyName table (rarely needed).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ice: &lt;ICE&gt;</p>
    </td>
    <td>
      <p>Specify a specific internal consistency evaluator (ICE) to run.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-loc &lt;loc.wxl&gt;</p>
    </td>
    <td>
      <p>Provide a .wxl file to read localization strings from.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-nologo</p>
    </td>
    <td>
      <p>Skip printing Light logo information.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-notidy</p>
    </td>
    <td>
      <p>Prevent Light from deleting temporary files after linking is complete (useful for debugging).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-O1</p>
    </td>
    <td>
      <p>Optimize smart cabbing for smallest cabinets (deprecated).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-O2</p>
    </td>
    <td>
      <p>Optimize smart cabbing for faster install time (deprecated).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-out</p>
    </td>
    <td>
      <p>Specify an output file; by default, Light will write to the current working directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-pdbout &lt;output.wixpdb&gt;</p>
    </td>
    <td>
      <p>Save the wixpdb to a specific file. The default is the same name as the output 
            with the wixpdb extension.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-pedantic</p>
    </td>
    <td>
      <p>Display pedantic output messages.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-reusecab</p>
    </td>
    <td>
      <p>Reuse cabinets from the cabinet cache instead of rebuilding cabinets.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sa</p>
    </td>
    <td>
      <p>Suppress assemblies: do not get assembly name information for assemblies.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sacl</p>
    </td>
    <td>
      <p>Suppress resetting ACLs (useful when laying out an image to a network share).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sadmin</p>
    </td>
     <td>
      <p>Suppress adding default Admin sequence actions.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sadv</p>
    </td>
    <td>
      <p>Suppress adding default Advt sequence actions.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sloc</p>
    </td>
    <td>
      <p>Suppress localization.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sice:&lt;ICE&gt;</p>
    </td>
    <td>
      <p>Suppress running internal consistency evaluators (ICEs) with specific IDs.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sma</p>
    </td>
    <td>
      <p>Suppress processing the data in the MsiAssembly table.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sf</p>
    </td>
    <td>
      <p>Suppress files: do not get any file information; this switch is equivalent to the combination of the -sa and -sh switches.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sh</p>
    </td>
    <td>
      <p>Suppress file information: do not get hash, version, language and other file properties.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sl</p>
    </td>
    <td>
      <p>Suppress layout creation.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-spdb</p>
    </td>
    <td>
      <p>Suppress outputting the wixpdb.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ss</p>
    </td>
    <td>
      <p>Suppress schema validation for documents; this switch provides a performance boost during linking.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sts</p>
    </td>
    <td>
      <p>Suppress tagging sectionId attribute on rows.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sui</p>
    </td>
    <td>
      <p>Suppress adding default UI sequence actions.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sv</p>
    </td>
    <td>
      <p>Suppress intermediate file version mismatch checking.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sval</p>
    </td>
    <td>
      <p>Suppress MSI/MSM validation.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sw&lt;N&gt;</p>
    </td>
    <td>
      <p>Suppress warnings with specific message IDs.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-swall</p>
    </td>
    <td>
      <p>Suppress all warnings (deprecated).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-usf &lt;output.xml&gt;</p>
    </td>
    <td>
      <p>Specify an unreferenced symbols file.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-v</p>
    </td>
    <td>
      <p>Generate verbose output.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wx &lt;N&gt;</p>
    </td>
    <td>
      <p>Treat warnings as errors.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wxall</p>
    </td>
    <td>
      <p>Treat all warnings as errors (deprecated).</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-xo</p>
    </td>
    <td>
      <p>Generate XML output instead of an MSI.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-?</p>
    </td>
    <td>
      <p>Display Light help information.</p>
    </td>
  </tr>
</table>

## Binder Variables

### Standard Binder Variables

Some properties are not available until the linker is about to generate, or bind, the final output. These variables are called binder variables and supported binder variables are listed below.

#### All Versioned Files

The following standard binder variables are available for all versioned binaries.

<table border="1" cellspacing="0" cellpadding="2">
  <tr>
    <td>
      <p><b>Variable name</b></p>
    </td>
    <td>
      <p><b>Example usage</b></p>
    </td>
    <td>
      <p><b>Example value</b></p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.fileLanguage.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.fileLanguage.MyFile)</p>
    </td>
    <td>
      <p>1033</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.fileVersion.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.fileVersion.MyFile)</p>
    </td>
    <td>
      <p>1.0.0.0</p>
    </td>
  </tr>
</table>

#### Assemblies

The following standard binder variables are available for all managed and native assemblies (except where noted), where the File/@Assembly attribute is set to &quot;.net&quot; or &quot;win32&quot;.

<table border="1" cellspacing="0" cellpadding="2">
  <tr>
    <td>
      <p><b>Variable name</b></p>
    </td>
    <td>
      <p><b>Example usage</b></p>
    </td>
    <td>
      <p><b>Example value</b></p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyCulture.<i>FileID</i><br />
        <i>(managed only)</i></p>
    </td>
    <td>
      <p>!(bind.assemblyCulture.MyAssembly)</p>
    </td>
    <td>
      <p>neutral</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyFileVersion.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.assemblyFileVersion.MyAssembly)</p>
    </td>
    <td>
      <p>1.0.0.0</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyFullName.<i>FileID</i><br />
        <i>(managed only)</i></p>
    </td>
    <td>
      <p>!(bind.assemblyFullName.MyAssembly)</p>
    </td>
    <td>
      <p>MyAssembly, version=1.0.0.0, culture=neutral, publicKeyToken=0123456789ABCDEF, processorArchitecture=MSIL</p>
      <p><b>Note:</b> The <i>publicKeyToken</i> value is uppercased.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyFullNamePreservedCase.<i>FileID</i><br />
        <i>(managed only)</i></p>
    </td>
    <td>
      <p>!(bind.assemblyFullNamePreservedCase.MyAssembly)</p>
    </td>
    <td>
      <p>MyAssembly, version=1.0.0.0, culture=neutral, publicKeyToken=0123456789abcdef, processorArchitecture=MSIL</p>
      <p><b>Note:</b> The <i>publicKeyToken</i> value's casing is preserved.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyName.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.assemblyName.MyAssembly)</p>
    </td>
    <td>
      <p>MyAssembly</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyProcessorArchitecture.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.assemblyProcessorArchitecture.MyAssembly)</p>
    </td>
    <td>
      <p>MSIL</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyPublicKeyToken.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.assemblyPublicKeyToken.MyAssembly)</p>
    </td>
    <td>
      <p>0123456789abcdef</p>
      <p><b>Note:</b> The value is uppercased for managed assemblies.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyPublicKeyTokenPreservedCase.<i>FileID</i><br />
        <i>(managed only)</i></p>
    </td>
    <td>
      <p>!(bind.assemblyPublicKeyTokenPreservedCase.MyAssembly)</p>
    </td>
    <td>
      <p>0123456789abcdef</p>
      <p><b>Note:</b> The value's casing is preserved.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyType.<i>FileID</i><br />
        <i>(native only)</i></p>
    </td>
    <td>
      <p>!(bind.assemblyType.MyAssembly)</p>
    </td>
    <td>
      <p>win32</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.assemblyVersion.<i>FileID</i></p>
    </td>
    <td>
      <p>!(bind.assemblyVersion.MyAssembly)</p>
    </td>
    <td>
      <p>1.0.0.0</p>
    </td>
  </tr>
</table>

#### Properties

You can also reference property values from the Property table at bind time; however, you cannot reference properties as binder variables
  within other properties, including the attributes on the Product element - many of which are compiled into the Property table. You can reference
  other binder variables like file information above in properties, or even localization and custom binder variables documented below.

Specializations for each field of the ProductVersion property are also provided as shown below. If you have defined properties like ProductVersion.Major
  in your package authoring they will not be overwritten, but will be used instead of the automatic binder variables with the same name.

<table border="1" cellspacing="0" cellpadding="2">
  <tr>
    <td>
      <p><b>Variable name</b></p>
    </td>
    <td>
      <p><b>Example usage</b></p>
    </td>
    <td>
      <p><b>Example value</b></p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.property.<i>Property</i></p>
    </td>
    <td>
      <p>!(bind.property.ProductVersion)</p>
    </td>
    <td>
      <p>1.2.3.4</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.property.ProductVersion.Major</p>
    </td>
    <td>
      <p>!(bind.property.ProductVersion.Major)</p>
    </td>
    <td>
      <p>1</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.property.ProductVersion.Minor</p>
    </td>
    <td>
      <p>!(bind.property.ProductVersion.Minor)</p>
    </td>
    <td>
      <p>2</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.property.ProductVersion.Build</p>
    </td>
    <td>
      <p>!(bind.property.ProductVersion.Build)</p>
    </td>
    <td>
      <p>3</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.property.ProductVersion.Revision</p>
    </td>
    <td>
      <p>!(bind.property.ProductVersion.Revision)</p>
    </td>
    <td>
      <p>4</p>
    </td>
  </tr>
</table>

### Package Properties

You can reference the following properties from packages in your bundle. This allows developers to use property values already defined in their packages to set attributes in their bundle.

<table border="1" cellspacing="0" cellpadding="2">
  <tr>
    <td>
      <p><b>Variable name</b></p>
    </td>
    <td>
      <p><b>Example usage</b></p>
    </td>
    <td>
      <p><b>Example value</b></p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.packageDescription.<i>PackageID</i></p>
    </td>
    <td>
      <p>!(bind.packageDescription.MyProduct)</p>
    </td>
    <td>
      <p>Description of My Product</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.packageLanguage.<i>PackageID</i></p>
    </td>
    <td>
      <p>!(bind.packageLanguage.MyProduct)</p>
    </td>
    <td>
      <p>1033</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.packageManufacturer.<i>PackageID</i></p>
    </td>
    <td>
      <p>!(bind.packageManufacturer.MyProduct)</p>
    </td>
    <td>
      <p>My Company</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.packageName.<i>PackageID</i></p>
    </td>
    <td>
      <p>!(bind.packageName.MyProduct)</p>
    </td>
    <td>
      <p>My Product</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>bind.packageVersion.<i>PackageID</i></p>
    </td>
    <td>
      <p>!(bind.packageVersion.MyProduct)</p>
    </td>
    <td>
      <p>1.2.3.4</p>
    </td>
  </tr>
</table>

### Localization Variables

Variables can be passed in before binding the output file from a WiX localization file, or .wxl file. This process allows the developer to link one or more .wixobj files together with diferent .wxl files to produce different localized packages.

Localization variables are in the following format:

<pre>!(loc.<i>VariableName</i>)</pre>

### Custom Binder Variables

You can create your own binder variables using the [WixVariable](~/xsd/wix/wixvariable.html) element or by simply typing your own variable name in the following format:

<pre>!(bind.<i>VariableName</i>)</pre>

Custom binder variables allow you to use the same .wixobj files but specify different values when linking, similar to how localization variables are used. You might use binder variables for different builds, like varying the target processor architecture.
