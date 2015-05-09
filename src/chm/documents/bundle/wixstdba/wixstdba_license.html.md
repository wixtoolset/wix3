---
title: Specifying the WiX Standard Bootstrapper Application License
layout: documentation
---

# Specifying the WiX Standard Bootstrapper Application License

The WiX Standard Bootstrapper Application (WixStdBA) supports displaying a license in RTF format and/or linking to a license file that either exists locally or on the web. The license file is specified in the <bal:WixStandardBootstrapperApplication> element using the LicenseFile or LicenseUrl attribute, depending on which WixStdBA theme is used.

When using a WixStdBA theme that displays the RTF license, it is highly recommended that the license is overridden because the default uses &quot;Lorem ipsum&quot; placeholder text. The following example uses a license.rtf file found in the &quot;path\to&quot; folder relative to the linker bind paths.

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot; xmlns:bal=&quot;http://schemas.microsoft.com/wix/BalExtension&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            <strong class="highlight">LicenseFile=&quot;path\to\license.rtf&quot;</strong>
            LogoFile=&quot;path\to\customlogo.png&quot;
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;

        &lt;Chain&gt;
          ...
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

The following example links to a license page on the internet.

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot; xmlns:bal=&quot;http://schemas.microsoft.com/wix/BalExtension&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.HyperlinkLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            <strong class="highlight">LicenseUrl=&quot;http://example.com/license.html&quot;</strong>
            LogoFile=&quot;path\to\customlogo.png&quot;
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;

        &lt;Chain&gt;
          ...
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

When using a WixStdBA theme that displays the license as a hyperlink, the license is optional. Provide an empty string for WixStandardBootstrapperApplication/@LicenseUrl---the hyperlink and accept license checkbox are not displayed, providing an &quot;unlicensed&quot; installation experience. 

If you get an error indicating `The Windows Installer XML variable !(wix.WixStdbaLicenseUrl) is unknown`, provide a value for WixStandardBootstrapperApplication/@LicenseUrl, even if it's an empty string.