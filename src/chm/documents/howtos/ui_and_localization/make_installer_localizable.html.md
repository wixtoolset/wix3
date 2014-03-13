---
title: How To: Make your installer localizable
layout: documentation
after: specifying_cultures_to_build
---
# How To: Make your installer localizable
WiX supports building localized installers through the use of language files that include localized strings. It is a good practice to put all your strings in a language file as you create your setup, even if you do not currently plan on shipping localized versions of your installer. This how to describes how to create a language file and use its strings in your WiX project.

## Step 1: Create the language file
Language files end in the .wxl extension and specify their culture using the [&lt;WixLocalization&gt;](~/xsd/wixloc/wixlocalization.html) element. To create a language file on the command line create a new file with the appropriate name and add the following:

<pre>
<font size="2" color="#0000FF">&lt;?</font><font size="2" color="#A31515">xml</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">version</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1.0</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">encoding</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">utf-8</font><font size="2">"</font><font size="2" color="#0000FF">?&gt;
&lt;</font><font size="2" color="#A31515">WixLocalization</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Culture</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">en-us</font><font size="2">"</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">xmlns</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">http://schemas.microsoft.com/wix/2006/localization</font><font size="2">"</font><font size="2" color="#0000FF">&gt;
&lt;/<font size="2" color="#A31515">WixLocalization</font>&gt;</font>
</pre>

If you are using Visual Studio you can add a new language file to your project by doing the following:

1. Right click on your project in Solution Explorer and select Add > New Item...
1. Select WiX Localization File, give the file an appropriate name, and select Add

By default Visual Studio creates language files in the en-us culture. To create a language file for a different culture change the Culture attribute to the appropriate culture string.

## Step 2: Add the localized strings
Localized strings are defined using the [&lt;String&gt;](~/xsd/wixloc/string.html) element. Each element consists of a unique id for later reference in your WiX project and the string value. For example:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">String</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"ApplicationName"</font><font size="2" color="#0000FF">&gt;My Application Name&lt;/</font><font size="2" color="#A31515">String</font><font size="2" color="#0000FF">&gt;
&lt;</font><font size="2" color="#A31515">String</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"ManufacturerName"</font><font size="2" color="#0000FF">&gt;My Manufacturer Name&lt;/</font><font size="2" color="#A31515">String</font><font size="2" color="#0000FF">&gt;</font>
</pre>

The String element goes inside the WixLocalization element, and you should add one String element for each piece of text you need to localize.

## Step 3: Use the localized strings in your project
Once you have defined the strings you can use them in your project wherever you would normally use text. For example, to set your product&apos;s Name and Manufacturer to the localized strings do the following:

<pre>
<font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">Product</font><font size="2" color="#0000FF"> </font><font size="2" color="#FF0000">Id</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">*"
         </font><font size="2" color="#FF0000">UpgradeCode</font><font size="2" color="#0000FF">=</font><font size="2">"<a href="~/howtos/general/generate_guids.html">PUT-GUID-HERE</a>"</font>
<font size="2" color="#FF0000">  Version</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1.0.0.0</font><font size="2">"</font>
<font size="2" color="#FF0000">  Language</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">1033</font><font size="2">"</font>
<font size="2" color="#FF0000">  Name</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">!(loc.ApplicationName)</font><font size="2">"
         </font><font size="2" color="#FF0000">Manufacturer</font><font size="2" color="#0000FF">=</font><font size="2">"</font><font size="2" color="#0000FF">!(loc.ManufacturerName)</font><font size="2">"</font><font size="2" color="#0000FF">&gt;</font>
</pre>

Localization strings are referenced using the **!(loc.stringname)** syntax. These references will be replaced with the actual strings for the appropriate locale at build time.

For information on how to compile localized versions of your installer once you have the necessary language files see [How To: Build a localized version of your installer](build_a_localized_version.html).
