---
title: Customize the WiX Standard Bootstrapper Application Layout
layout: documentation
after: wixstdba_branding
---
# Customize the WiX Standard Bootstrapper Application Layout

The WiX Standard Bootstrapper Application contains a predefined user interface layout. It is possible to customize the layout using the WixStandardBootstrapperApplication element provided by WixBalExtension. The following example uses a &quot;customtheme.xml&quot; file found in the &quot;path\\to&quot; folder relative to the linker bind paths.

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
         xmlns:bal="http://schemas.microsoft.com/wix/BalExtension"&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot;&gt;
          &lt;bal:WixStandardBootstrapperApplication
            LicenseFile="path\to\license.rtf"
            <strong class="highlight">ThemeFile="path\to\customtheme.xml"</strong>
            /&gt;
        &lt;/BootstrapperApplicationRef&gt;

        &lt;Chain&gt;
          ...
        &lt;/Chain&gt;
      &lt;/Bundle&gt;
    &lt;/Wix&gt;
</pre>

The progress page of the bootstrapper application can be customized to include Windows Installer ActionData messages by adding a Text control with the name ExecuteProgressActionDataText.

<pre>
    &lt;Page Name=&quot;Progress&quot;&gt;<br />        &lt;Text X=&quot;11&quot; Y=&quot;80&quot; Width=&quot;-11&quot; Height=&quot;30&quot; FontId=&quot;2&quot; DisablePrefix=&quot;yes&quot;&gt;#(loc.ProgressHeader)&lt;/Text&gt;<br />        &lt;Text X=&quot;11&quot; Y=&quot;121&quot; Width=&quot;70&quot; Height=&quot;17&quot; FontId=&quot;3&quot; DisablePrefix=&quot;yes&quot;&gt;#(loc.ProgressLabel)&lt;/Text&gt;<br />        &lt;Text Name=&quot;OverallProgressPackageText&quot; X=&quot;85&quot; Y=&quot;121&quot; Width=&quot;-11&quot; Height=&quot;17&quot; FontId=&quot;3&quot; DisablePrefix=&quot;yes&quot;&gt;#(loc.OverallProgressPackageText)&lt;/Text&gt;<br />        &lt;Progressbar Name=&quot;OverallCalculatedProgressbar&quot; X=&quot;11&quot; Y=&quot;143&quot; Width=&quot;-11&quot; Height=&quot;15&quot; /&gt;<br />        <strong class="highlight">&lt;Text Name=&quot;ExecuteProgressActionDataText&quot; X=&quot;11&quot; Y=&quot;163&quot; Width=&quot;-11&quot; Height=&quot;17&quot; FontId=&quot;3&quot; DisablePrefix=&quot;yes&quot; /&gt;</strong><br />        &lt;Button Name=&quot;ProgressCancelButton&quot; X=&quot;-11&quot; Y=&quot;-11&quot; Width=&quot;75&quot; Height=&quot;23&quot; TabStop=&quot;yes&quot; FontId=&quot;0&quot;&gt;#(loc.ProgressCancelButton)&lt;/Button&gt;<br />    &lt;/Page&gt;
</pre>

The overall size of the bootstrapper application window can be customized by changing the Width and Height attributes of the Window element within the theme along with modifying the size and/or position of all the controls.

<pre>
&lt;Theme xmlns=&quot;http://wixtoolset.org/schemas/thmutil/2010&quot;&gt;<br />    &lt;Window <strong class="highlight">Width=&quot;485&quot; Height=&quot;300&quot;</strong> HexStyle=&quot;100a0000&quot; FontId=&quot;0&quot;&gt;#(loc.Caption)&lt;/Window&gt;</pre>

To view a theme file without having to build a bundle, you can use the ThmViewer.exe which is located in %WIX%\\bin\\.
