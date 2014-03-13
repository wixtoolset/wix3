---
title: How To: Build a Localized Version of Your Installer
layout: documentation
---
# How To: Build a Localized Version of Your Installer
Once you have described all the strings in your installer using language files, as described in [How To: Make your installer localizable](make_installer_localizable.html), you can then build versions of your installer for each supported language. This how to explains building the localized installers both from the command line and using Visual Studio.

## Option 1: Building localized installers from the command line
The first step in building a localized installer is to compile your WiX sources using candle.exe:

    candle.exe myinstaller.wxs -out myinstaller.wixobj

After the intermediate output file is generated you can then use light.exe to generate multiple localized MSIs:

    light.exe myinstaller.wixobj -cultures:en-us -loc en-us.wxl -out myinstaller-en-us.msi
    light.exe myinstaller.wixobj -cultures:fr-fr -loc fr-fr.wxl -out myinstaller-fr-fr.msi

The -loc flag is used to specify the language file to use. It is important to include the -cultures flag on the command line to ensure the correct localized strings are included for extensions such as [WiXUIExtension](~/wixui/WixUI_dialog_library.html).

## Option 2: Building localized installers using Visual Studio
Visual Studio will automatically build localized versions of your installer. If your WiX project includes multiple .wxl files, one localized installer will be built for each culture, unless **Cultures to build** is specified.

For more information, see [Specifying cultures to build](specifying_cultures_to_build.html)
