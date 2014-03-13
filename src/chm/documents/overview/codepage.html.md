---
title: Code Pages
layout: documentation
after: msitowix
---
# Code Pages

Code pages map character codes to actual characters, or graphemes. Code pages are also used to convert from one encoding to another.

## Code Pages in Windows Installer

Windows Installer stores strings in a package according to a particular code page. A separate code page is used for the summary information stream and the rest of the package database, which includes the ActionText, Error, Property, and other tables.

For more information about code pages in Windows Installer, read <a href="http://msdn2.microsoft.com/library/aa367867.aspx" target="_blank">Code Page Handling</a>.

## Setting the Code Page using WiX

Top-level elements like [Product](~/xsd/wix/product.html), [Module](~/xsd/wix/module.html), [Patch](~/xsd/wix/patch.html), and [PatchCreation](~/xsd/wix/patchcreation.html) support a Codepage attribute. You can set this to a valid Windows code page by integer like 1252, or by web name like Windows-1252. UTF-7 and UTF-8 are not officially supported because of user interface issues. Unicode is not supported.

To support authoring a single package that can be localized into multiple languages, you can set the [Package](~/xsd/wix/package.html)/@SummaryCodepage or [PatchInformation](~/xsd/wix/patchinformation.html)/@SummaryCodepage element to an localization expression like !(loc.SummaryCodepage). You then define the SummaryCodepage value in a [localization file](files.html), typically ending in a .wxl extension. The root WixLocalization element also supports a Codepage attribute that is used to encode the rest of the package database.

You can also set the code page to 0. In this case, Windows Installer treats strings as neutral, meaning that you can only safely use ASCII characters - the first 128 ANSI characters - but the database will be supported across Windows platforms. See <a href="http://msdn2.microsoft.com/library/aa368057.aspx" target="_blank">Creating a Database with a Neutral Code Page</a> for more information.

For a walkthrough about how to author a build localized packages using WiX see [How To: Make your installer localizable](~/howtos/ui_and_localization/make_installer_localizable.html) and [How To: Build a localized version of your installer](~/howtos/ui_and_localization/build_a_localized_version.html).
