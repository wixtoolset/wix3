---
title: Author a Bundle Package Manifest
layout: documentation
after: authoring_bundle_application
---
# Author a Bundle Package Manifest

In order for any package to be consumable by a Bundle, a package definition needs to be authored that describes the package. This authoring can either go directly under the [&lt;Chain&gt;](~/xsd/wix/chain.html) element in the Bundle authoring, or in a [&lt;Fragment&gt;](~/xsd/wix/fragment.html) which can then be consumed by a Bundle by putting a [&lt;PackageGroupRef&gt;](~/xsd/wix/packagegroupref.html) inside the [&lt;Chain&gt;](~/xsd/wix/chain.html). The latter method enables sharing of the same package definition across different Bundle packages.

The WiX schema supports the following chained package types:

* [&lt;MsiPackage&gt;](~/xsd/wix/msipackage.html)
* [&lt;ExePackage&gt;](~/xsd/wix/exepackage.html)
* [&lt;MspPackage&gt;](~/xsd/wix/msppackage.html)
* [&lt;MsuPackage&gt;](~/xsd/wix/msupackage.html)

Here is an example of authoring an ExePackage in a sharable fragment:

    <?xml version="1.0"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
      <Fragment>
        <PackageGroup Id="MyPackage">
            <ExePackage 
              SourceFile="[sources]\packages\shared\MyPackage.exe"
              DetectCondition="ExeDetectedVariable"
              DownloadUrl="http://example.com/?mypackage.exe"
              InstallCommand="/q /ACTION=Install"
              RepairCommand="/q ACTION=Repair /hideconsole"
              UninstallCommand="/q ACTION=Uninstall /hideconsole" />
        </PackageGroup>
      </Fragment>
    </Wix>

Now let&apos;s add an install condition to the package so that it only installs on x86 Windows XP and above. There are [built-in variables](bundle_built_in_variables.html) that can be used to construct these condition statements. The highlighted section shows how to leverage the built-in variables to create that condition:

<pre>    &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;&gt;
      &lt;Fragment&gt;
        &lt;PackageGroup Id=&quot;MyPackage&quot;&gt;
            &lt;ExePackage 
              SourceFile=&quot;[sources]\packages\shared\MyPackage.exe&quot;
              DetectCondition=&quot;ExeDetectedVariable&quot;
              DownloadUrl=&quot;http://example.com/?mypackage.exe&quot;
              InstallCommand=&quot;/q /ACTION=Install&quot;
              RepairCommand=&quot;/q ACTION=Repair /hideconsole&quot;
              UninstallCommand=&quot;/q ACTION=Uninstall /hideconsole&quot; 
              <strong class="highlight">InstallCondition=&quot;NOT VersionNT64 AND VersionNT &gt;= v5.1&quot;</strong> /&gt;
        &lt;/PackageGroup&gt;
      &lt;/Fragment&gt;
    &lt;/Wix&gt;    </pre>

The VersionNT property takes up to a four-part version number ([Major].[Minor].[Build].[Revision]). For a list of major and minor versions of the Windows Operating System, see <a href="http://msdn.microsoft.com/library/ms724832.aspx" target="_blank">Operating System Version</a>.

You can also define your own variables and store search results in them. See [Define Searches using Variables](bundle_define_searches.html).
