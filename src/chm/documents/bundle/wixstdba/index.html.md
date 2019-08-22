---
title: Working with WiX Standard Bootstrapper Application
layout: documentation
---
# Working with WiX Standard Bootstrapper Application

As described in the introduction to [building installation package bundles](~/bundle/index.html), every bundle requires a bootstrapper application DLL to drive the Burn engine. Custom bootstrapper applications can be created but require the developer to write native or managed code. Therefore, the WiX toolset provides a standard bootstrapper application that developers can use and customize in particular ways.

There are several variants of the WiX Standard Bootstrapper Application.

1. WixStandardBootstrapperApplication.RtfLicense - the first variant displays the license in the welcome dialog similar to the WixUI Advanced.
1. WixStandardBootstrapperApplication.HyperlinkLicense - the second variant provides an optional hyperlink to the license agreement on the welcome dialog, providing a more modern and streamlined look.
1. WixStandardBootstrapperApplication.HyperlinkSidebarLicense - the third variant is based on HyperlinkLicense but provides a larger dialog and larger image on the initial page.
1. WixStandardBootstrapperApplication.RtfLargeLicense - this variant is similar to RtfLicense but is a larger dialog and supports the option of displaying the version number.
1. WixStandardBootstrapperApplication.HyperlinkLargeLicense - this variant is similar to HyperlinkLicense but is a larger dialog and supports the option of displaying the version number.

To use the WiX Standard Bootstrapper Application, a [&lt;BootstrapperApplicationRef&gt;](~/xsd/wix/bootstrapperapplicationref.html) element must reference one of the above identifiers. The following example uses the bootstrapper application that displays the license:

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;&gt;
      &lt;Bundle&gt;
        <strong class="highlight">&lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot; /&gt;</strong>
        &lt;Chain&gt;
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

HyperlinkLargeTheme, HyperlinkSidebarTheme, and RtfLargeTheme can optionally display the bundle version on the welcome page:

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
            xmlns:bal=&quot;http://schemas.microsoft.com/wix/BalExtension&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            LicenseFile=&quot;path\to\license.rtf&quot;
            <strong class="highlight">ShowVersion=&quot;yes&quot;</strong>
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;
        &lt;Chain&gt;
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

When building the bundle, the WixBalExtension must be provided. If the above code was in a file called &quot;example.wxs&quot;, the following steps would create an &quot;example.exe&quot; bundle:

    candle.exe example.wxs -ext WixBalExtension
    light.exe example.wixobj -ext WixBalExtension

The following topics provide information about how to customize the WiX Standard Bootstrapper Application:

*  [Specifying the WiX Standard Bootstrapper Application License](wixstdba_license.html)
*  [Changing the WiX Standard Bootstrapper Application Branding](wixstdba_branding.html)
*  [Customize the WiX Standard Bootstrapper Application Layout](wixstdba_customize.html)
*  [Using WiX Standard Bootstrapper Application Variables](wixstdba_variables.html)