---
title: Localized Extensions
layout: documentation
---
# Localizing Extensions

You can create your own localized extensions like [WixUIExtension](~/wixui/WixUI_dialog_library.html) using lit.exe. Localized extensions can even contain multiple languages. Products using these extensions can pass the -cultures switch to [light.exe](~/overview/light.html) along with the -ext switch to reference the extension.

WiX extensions contain libraries comprised of fragments. These fragments may contain properties, search properties, dialogs, and more. Just like when localizing products, replace any localizable fields with variables in the format !(loc.<i>variableName</i>). Product would be authored to reference elements in this library, and when compiled would themselves contain the localization variables.
The following shows an example on how to localize an extension

## Step 1: Author a WiX Fragment

Create a .wxs file named example.wxs and add the following content:

    <?xml version="1.0" encoding="utf-8"?>
    <Fragment>
        <Error Id="50000">!(loc.errormsg)</Error>
    </Fragment>

You have just authored a Fragment that will be compiled into a WiX library. It
contains an error message that references a localized string.

## Step 2: Author the Localization File

The WiX localization files, or .wxl files, are a collection of strings. For
libraries, extension developers can choose whether or not those strings can be
overwritten by .wxl files specified during linkage of the product. Create a .wxl
file named en-us.wxl and add the following content:

    <?xml version="1.0" encoding="utf-8"?>
    <WixLocalization Culture="en-us" xmlns="http://schemas.microsoft.com/wix/2006/localization">
      <String Id="errormsg" Overridable="yes">General Failure</String>
    </WixLocalization>

These [String](~/xsd/wixloc/string.html) elements are attributed as @Overridable=&quot;yes&quot; to allow for product developers to override these strings with their own values if they so choose. For example, a product developer may wish to use &quot;Previous&quot; instead of &quot;Back&quot;, so they can define the same String/@Id in their own .wxl while still linking to the extension where that string is used. This offers product developers the benefits of the library while allowing for customizations. Extension developers can also choose to disallow overriding certain strings if it makes sense to do so.

## Step 3: Build the library

When both the fragment authoring and localization file are complete, they can be compiled and linked together using candle.exe and lit.exe.

First compile the .wxs source.

    candle.exe example.wxs -out example1.wixobj

Now link together the .wixobj file and the .wxl file in the extension library.

    lit.exe example.wixobj -loc en-us.wxl -out example.wixlib

You can add more than one .wxl file for each culture you want available. To be useful, the .wixlib should be embedded into a managed assembly and returned by WixExtension.GetLibrary().

## Using the Libraries

Product developers reference elements within your .wixlib, as shown in the [WixUIExtension](~/wixui/WixUI_dialog_library.html) example. When compiling and linking, the extension is specified on the command line using the -ext switch. If any additional localization variables are used in the product authoring or would override localization variables in the library, those .wxl files are passed to the -loc switch as shown in the example below.

    candle.exe example.wxs -ext WixUIExtension -out example.wixobj
    light.exe example.wixobj -ext WixUIExtension -cultures:en-us -loc en-us.wxl -out example.msi
