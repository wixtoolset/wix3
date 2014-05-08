---
title: Chain Packages into a Bundle
layout: documentation
after: bundle_define_searches
---
# Chain Packages into a Bundle

To chain a package, you can either put the package definition directly under the [&lt;Chain&gt;](~/xsd/wix/chain.html) element or put a [&lt;PackageGroupRef&gt;](~/xsd/wix/packagegroupref.html) inside the [&lt;Chain&gt;](~/xsd/wix/chain.html) to reference a shared package definition.

Here&apos;s an example of having the definition directly under [&lt;Chain&gt;](~/xsd/wix/chain.html):

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot; /&gt;

        <strong class="highlight">&lt;Chain&gt;
            &lt;ExePackage
              SourceFile=&quot;path\to\MyPackage.exe&quot;
              DownloadUrl=&quot;http://example.com/?mypackage.exe&quot;
              InstallCommand=&quot;/q /ACTION=Install&quot;
              RepairCommand=&quot;/q ACTION=Repair /hideconsole&quot; /&gt;
        &lt;/Chain&gt;</strong>
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>

Here&apos;s an example of referencing a shared package definition:

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;&gt;
      &lt;Bundle&gt;
        &lt;BootstrapperApplicationRef Id=&quot;WixStandardBootstrapperApplication.RtfLicense&quot; /&gt;

        <strong class="highlight">&lt;Chain&gt;
            &lt;PackageGroupRef Id=&quot;MyPackage&quot; /&gt;
        &lt;/Chain&gt;</strong>
      &lt;/Bundle&gt;
    &lt;/Wix&gt;</pre>
