---
title: Changing the WiX Standard Bootstrapper Application Branding
layout: documentation
after: wixstdba_license
---
# Changing the WiX Standard Bootstrapper Application Branding

The WiX Standard Bootstrapper Application displays a generic logo in the bottom left corner of the user interface. It is possible to change the image displayed using the WixStandardBootstrapperApplication element provided by WixBalExtension. The following example uses a &quot;customlogo.png&quot; file found in the &quot;path\to&quot; folder relative to the linker bind paths.

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
         xmlns:bal=&quot;http://schemas.microsoft.com/wix/BalExtension&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            LicenseFile=&quot;path\to\license.rtf&quot;
            <strong class="highlight">LogoFile=&quot;path\to\customlogo.png&quot;</strong>
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;

        &lt;Chain&gt;
          ...
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

For the HyperlinkSidebarLicense UI, there are two logos and they can be configured as follows:

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
         xmlns:bal=&quot;http://schemas.microsoft.com/wix/BalExtension&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.HyperlinkSidebarLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            LicenseUrl=&quot;License.htm&quot;
            <strong class="highlight">LogoFile=&quot;path\to\customlogo.png&quot; LogoSideFile=&quot;path\to\customsidelogo.png&quot;</strong>
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;

        &lt;Chain&gt;
          ...
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>
