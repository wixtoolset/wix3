---
title: Define Searches Using Variables
layout: documentation
after: bundle_built_in_variables
---
# Define Searches Using Variables

Searches are used to detect if the target machine meets certain conditions. The result of a search is stored into a variable. Variables are then used to construct install conditions. The search schemas are in the WixUtilExtension. Here is the list of supported searches:

* [&lt;FileSearch&gt;](~/xsd/util/filesearch.html)
* [&lt;RegistrySearch&gt;](~/xsd/util/registrysearch.html)
* [&lt;DirectorySearch&gt;](~/xsd/util/directorysearch.html)
* [&lt;ComponentSearch&gt;](~/xsd/util/componentsearch.html)
* [&lt;ProductSearch&gt;](~/xsd/util/productsearch.html)

A search can be dependent on the result of another search. Keep in mind that all searches are in the WiXUtilExtension. So remember to add the WiXUtilExtension namespace in the authoring. Here is an example:

    <?xml version="1.0"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi"
         xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
      <Fragment>
        <util:RegistrySearch Id="Path"
            Variable="UniqueId"
            Root="HKLM,SOFTWARE\Microsoft\MyProduct\Unique Id\"
            Key="Product"
            Result="Value" />
        <util:RegistrySearch 
            Variable="patchLevel"
            Root="HKLM,SOFTWARE\Microsoft\MyProduct\[UniqueId]\Setup"
            Key="PatchLevel"
            Result="Exists" 
            After="Path" />
      </Fragment>
    </Wix>

After the searches are defined and stored into variables, the variables can then be used in install conditions. For example, you can use the result of the registry searches in the install condition of your package by adding both the searches and the install conditions. Here&apos;s an example of a complete fragment that contains a package definition with conditions and searches:

<pre>   &lt;?xml version=&quot;1.0&quot;?&gt;
    &lt;Wix xmlns=&quot;http://schemas.microsoft.com/wix/2006/wi&quot;
         xmlns:util=&quot;http://schemas.microsoft.com/wix/UtilExtension&quot;&gt;
      &lt;Fragment&gt;
        &lt;util:RegistrySearch Id=&quot;Path&quot;
            Variable=&quot;UniqueId&quot;
            Root=&quot;HKLM,SOFTWARE\Microsoft\MyProduct\Unique Id\&quot;
            Key=&quot;Product&quot;
            Result=&quot;Value&quot; /&gt;
        &lt;util:RegistrySearch 
            Variable=&quot;patchLevel&quot;
            Root=&quot;HKLM,SOFTWARE\Microsoft\MyProduct\[UniqueId]\Setup&quot;
            Key=&quot;PatchLevel&quot;
            Result=&quot;Exists&quot; 
            After=&quot;Path&quot; /&gt;

        &lt;PackageGroup Id=&quot;MyPackage&quot;&gt;
          &lt;ExePackage 
            SourceFile=&quot;[sources]\packages\shared\MyPackage.exe&quot;
            DownloadURL=&quot;http://mywebdomain.com/?mypackage.exe&quot;
            InstallCommand=&quot;/q /ACTION=Install&quot;
            RepairCommand=&quot;/q ACTION=Repair /hideconsole&quot;
            UninstallCommand=&quot;/q ACTION=Uninstall /hideconsole&quot;
            InstallCondition=&quot;x86 = 1 AND OSVersion &gt;= v5.0.5121.0 <strong class="highlight">AND PatchLevel = 0</strong>&quot; /&gt;
        &lt;/PackageGroup&gt;
      &lt;/Fragment&gt;
    &lt;/Wix&gt;  </pre>

Now you have a fully-defined fragment that can be shared to be consumed by other Burn packages. To see how to chain this package into a Burn package, see [Chain Packages into a Bundle](bundle_author_chain.html).
