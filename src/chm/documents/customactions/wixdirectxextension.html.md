---
title: WixDirectXExtension
layout: documentation
after: using_standard_customactions
---

# WixDirectXExtension

The WixDirectXExtension includes a custom action named WixQueryDirectXCaps that sets properties you can use to check the DirectX capabilities of the installing user&apos;s video card.

<table cellspacing="0" cellpadding="4" class="style1">
  <tr>
    <td valign="top">
      <p><b>WixDirectXExtension properties</b></p>
    </td>

    <td></td>
  </tr>

  <tr>
    <td valign="top">
      <p>WIX_DIRECTX_PIXELSHADERVERSION</p>
    </td>

    <td>
      <p>Pixel shader version capability, expressed as <i>major</i>*100 + <i>minor</i>. For example, a shader model 3.0-compliant system would have a WIX_DIRECTX_PIXELSHADERVERSION value of 300.</p>
    </td>
  </tr>

  <tr>
    <td valign="top">
      <p>WIX_DIRECTX_VERTEXSHADERVERSION</p>
    </td>

    <td>
      <p>Vertex shader version capability, expressed as <i>major</i>*100 + <i>minor</i>. For example, a shader model 3.0-compliant system would have a WIX_DIRECTX_VERTEXSHADERVERSION value of 300.</p>
    </td>
  </tr>
</table>

To use the WixDirectXExtension properties in an MSI, use the following steps:

* Add PropertyRef elements for items listed above that you want to use in your MSI.
* Add the -ext &lt;path to WixDirectXExtension.dll&gt; command line parameter when calling light.exe to include the WixDirectXExtension in the MSI linking process.
* Or, using an MSBuild-based .wixproj project, add &lt;path to WixDirectXExtension.dll&gt; to the WixExtension item group. When using Votive in Visual Studio, this can be done by right-clicking on the References node in a WiX project, choosing Add Reference... then browsing for WixDirectXExtension.dll and adding a reference.

For example:

    <PropertyRef Id="WIX_DIRECTX_PIXELSHADERVERSION" />
    
    <CustomAction Id="CA_CheckPixelShaderVersion" Error="[ProductName] requires pixel shader version 3.0 or greater." />
    
    <InstallExecuteSequence>
    <Custom Action="CA_CheckPixelShaderVersion" After="WixQueryDirectXCaps">
      <![CDATA[WIX_DIRECTX_PIXELSHADERVERSION < 300]]>
    </Custom>
    </InstallExecuteSequence>
    
    <InstallUISequence>
    <Custom Action="CA_CheckPixelShaderVersion" After="WixQueryDirectXCaps">
      <![CDATA[WIX_DIRECTX_PIXELSHADERVERSION < 300]]>
    </Custom>
    </InstallUISequence>

Note that the WixDirectXExtension properties are set to the value <b>NotSet</b> by default. The WixDirectXExtension custom action is configured to not fail if it encounters any errors when trying to determine DirectX capabilities. In this type of scenario, the properties will be set to their <b>NotSet</b> default values. In your setup authoring, you can compare the property values to the <b>NotSet</b> value or to a specific value to determine whether WixDirectXExtension was able to query DirectX capabilities and if so, what they are.
