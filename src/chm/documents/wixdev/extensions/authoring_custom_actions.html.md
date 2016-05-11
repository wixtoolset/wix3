---
title: Adding Custom Actions
layout: documentation
after: extension_development_preprocessor
---
# Adding a Custom Action

This example shows how to author a binary custom action called &quot;FooAction&quot;. A common example is a dll custom action that launches notepad.exe or some other application as part of their install. Before you start, you will need a sample dll that has an entrypoint called &quot;FooEntryPoint&quot;. This sample assumes you have already reviewed the [Creating a Skeleton Extension](extension_development_simple_example.html) topic.

## Step 1: Create a Fragment

You could directly reference the custom action in the same source file as the product definition. However, that will not enable the same custom action to be used elsewhere. So rather than putting the custom action definition in the same source file, let&apos;s exercise a little modularity and create a new source file to define the custom action called &quot;ca.wxs&quot;.

<pre>
&lt;?xml version='1.0'?&gt;
&lt;Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'&gt;
<b><span class="style1">   &lt;Fragment&gt;</span>
<span class="style1">      &lt;CustomAction Id='FooAction' BinaryKey='FooBinary' DllEntry='FooEntryPoint' Execute='immediate'</span>
<span class="style1">                    Return='check'/&gt;</span>
 
<span class="style1">      &lt;Binary Id='FooBinary' SourceFile='foo.dll'/&gt;</span>
<span class="style1">   &lt;/Fragment&gt;</span></b>
&lt;/Wix&gt;
</pre>

Okay, that&apos;s it. We&apos;re done with editing the &quot;ca.wxs&quot; source file. That little bit of code should compile but it will not link. Remember linking requires that you have an entry section. A &lt;Fragment/&gt; alone is not an entry section. Go to the next step to link the source file.

## Step 2: Add the custom action

We would need to link this source file along with a source file that contained &lt;Product/&gt; or &lt;Module/&gt; to successfully complete.

<pre>
&lt;?xml version='1.0'?&gt;
&lt;Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'&gt;
   &lt;Product Id='PUT-GUID-HERE' Name='Test Package' Language='1033' 
            Version='1.0.0.0' Manufacturer='.NET Foundation'&gt;
      &lt;Package Description='My first Windows Installer package'
            Comments='This is my first attempt at creating a Windows Installer database' 
            Manufacturer='.NET Foundation' InstallerVersion='200' Compressed='yes' /&gt;
 
      &lt;Media Id='1' Cabinet='product.cab' EmbedCab='yes' /&gt;
 
      &lt;Directory Id='TARGETDIR' Name='SourceDir'&gt;
         &lt;Directory Id='ProgramFilesFolder' Name='PFiles'&gt;
            &lt;Directory Id='MyDir' Name='Test Program'&gt;
               &lt;Component Id='MyComponent' Guid='PUT-GUID-HERE'&gt;
                  &lt;File Id='readme' Name='readme.txt' DiskId='1' Source='readme.txt' /&gt;
               &lt;/Component&gt;
 
               &lt;Merge Id='MyModule' Language='1033' SourceFile='module.msm' DiskId='1' /&gt;
            &lt;/Directory&gt;
         &lt;/Directory&gt;
      &lt;/Directory&gt;
 
      &lt;Feature Id='MyFeature' Title='My 1st Feature' Level='1'&gt;
         &lt;ComponentRef Id='MyComponent' /&gt;
         &lt;MergeRef Id='MyModule' /&gt;
      &lt;/Feature&gt;
<b>
 <span class="style1">     &lt;InstallExecuteSequence&gt;</span>
<span class="style1">         &lt;Custom Action='FooAction' After='InstallFiles'/&gt;</span>
<span class="style1">      &lt;/InstallExecuteSequence&gt;</span></b>
   &lt;/Product&gt;
&lt;/Wix&gt;
</pre>

Those three lines are all you need to add to your Windows Installer package source file to call the &quot;FooAction&quot; CustomAction. Now that we have two files to link together our call to light.exe gets a little more complicated. Here are the compile, link, and installation steps.

<pre>
C:\test&gt; <span class="style2">candle product.wxs ca.wxs</span>
 
C:\test&gt; <span class="style2">light product.wixobj ca.wixobj &ndash;out product.msi</span>
 
C:\test&gt; <span class="style2">msiexec /i product.msi</span>
</pre>

Now as part of your installation, whatever &quot;FooAction&quot; is supposed to perform, you should see happen after the InstallFiles action.
