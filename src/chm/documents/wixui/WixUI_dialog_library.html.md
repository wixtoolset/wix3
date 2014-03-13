---
title: Using Built-in WixUI Dialog Sets
layout: documentation
---

# Using Built-in WixUI Dialog Sets

The WixUI dialog library contains the following built-in dialog sets that provide a familiar wizard-style setup user interface.

1. [WixUI_Advanced](dialog_reference/WixUI_advanced.html)
1. [WixUI_FeatureTree](dialog_reference/WixUI_featuretree.html)
1. [WixUI_InstallDir](dialog_reference/WixUI_installdir.html)
1. [WixUI_Minimal](dialog_reference/WixUI_minimal.html)
1. [WixUI_Mondo](dialog_reference/WixUI_mondo.html)

The built-in WixUI dialog sets are also customizable, from the bitmaps shown in the UI to adding and removing custom dialogs. See [Customizing the WixUI Dialog Sets](WixUI_customizations.html) for additional information.

## How to add a built-in WixUI dialog set to a product installer

Assuming you have an existing installer that is functional but is just lacking a user interface, here are the steps you need to follow to include a built-in WixUI dialog set:

<ol>
  <li>Add a UIRef element to your setup authoring that has an Id that matches the name of one of the dialog sets described above. For example:
    <pre>
&lt;Product ...&gt;
&lt;UIRef Id="WixUI_InstallDir" /&gt;
&lt;/Product&gt;
</pre>
  </li>

  <li>Pass the -ext and -cultures switches to <a href="~/overview/light.html">light.exe</a> to reference the WixUIExtension. For example:
    <pre>
light -ext WixUIExtension -cultures:en-us Product.wixobj -out Product.msi
</pre>

    <p>Note - If you are using WiX in Visual Studio you can add the WixUIExtension using the Add Reference dialog and the necessary command lines will automatically be added when linking your .msi. To do this, use the following steps:</p>

    <ol>
      <li>Open your WiX project in Visual Studio</li>

      <li>Right click on your project in Solution Explorer and select Add Reference...</li>

      <li>Select the <strong>WixUIExtension.dll</strong> assembly from the list and click Add</li>

      <li>Close the Add Reference dialog</li>
    </ol>
  </li>
</ol>
