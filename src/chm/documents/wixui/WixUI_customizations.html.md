---
title: Customizing Built-in WixUI Dialog Sets
layout: documentation
after: wixui_dialog_library
---

# Customizing Built-in WixUI Dialog Sets

The built-in WixUI dialog sets can be customized in the following ways:

* Specifying a product-specific license agreement file.
* Specifying product-specific setup UI bitmaps.
* Adding an optional checkbox and optional text to the ExitDlg.
* Customizing the text displayed in built-in dialogs.
* Changing the UI sequence of a built-in dialog set.
* Inserting a custom dialog into a built-in dialog set.

## Specifying a license file

WixUIExtension.dll includes a default, placeholder license agreement. To specify your product&apos;s license, override the default by specifying a WiX variable named WixUILicenseRtf with the value of an RTF file that contains your license text. You can define the variable in your WiX authoring:

    <WixVariable Id="WixUILicenseRtf" Value="bobpl.rtf" />

Alternatively, you can define the variable using the -d switch when running <b>light</b>:

    light -ext WixUIExtension -cultures:en-us -dWixUILicenseRtf=bobpl.rtf Product.wixobj -out Product.msi

The file you specify must be in a directory <b>light</b> is looking in for files. Use the <b>-b</b> switch to add directories.

There is a known issue with the rich text control used to display the text of the license file that can cause the text to appear blank until the user scrolls down in the control. This is typically caused by complex RTF content (such as the RTF generated when saving an RTF file in Microsoft Word). If you run into this behavior in your setup UI, one of the following workarounds will fix it in most cases:

* Open your RTF file in WordPad and save it from there in order to remove the complex RTF content from the file. After saving it, rebuild your MSI.
* Use a dialog set other than the WixUI\_Minimal set. This problem typically only occurs when the license agreement screen is the first one displayed during setup, which only happens with the WixUI\_Minimal dialog set.

## Replacing the default bitmaps

The WixUI dialog library includes default bitmaps for the background of the welcome and completion dialogs and the top banner of the other dialogs. You can replace those bitmaps with your own for product branding purposes. To replace default bitmaps, specify WiX variable values with the file names of your bitmaps, just like when replacing the default license text.

<table border="1" cellspacing="0" cellpadding="4" id="table1">
  <tr>
    <td><b>Variable name</b></td>
    <td><b>Description</b></td>
    <td><b>Dimensions</b></td>
  </tr>
  <tr>
    <td>WixUIBannerBmp</td>
    <td>Top banner</td>
    <td>493 &times; 58</td>
  </tr>
  <tr>
    <td>WixUIDialogBmp</td>
    <td>Background bitmap used on the welcome and completion dialogs</td>
    <td>493 &times; 312</td>
  </tr>
  <tr>
    <td>WixUIExclamationIco</td>
    <td>Exclamation icon on the WaitForCostingDlg</td>
    <td>32 &times; 32</td>
  </tr>
  <tr>
    <td>WixUIInfoIco</td>
    <td>Information icon on the cancel and error dialogs</td>
    <td>32 &times; 32</td>
  </tr>
  <tr>
    <td>WixUINewIco</td>
    <td>Button glyph on the BrowseDlg</td>
    <td>16 &times; 16</td>
  </tr>
  <tr>
    <td>WixUIUpIco</td>
    <td>Button glyph on the BrowseDlg</td>
    <td>16 &times; 16</td>
  </tr>
</table>

## Customizing the ExitDlg

The ExitDlg is the [dialog in the built-in WixUI dialog sets](dialog_reference/WixUI_dialogs.html) that is displayed at the end of a successful setup. The ExitDlg supports showing both optional, customizable text and an optional checkbox.

See [How To: Run the Installed Application After Setup](~/howtos/ui_and_localization/run_program_after_install.html) for an example of how to show a checkbox on the ExitDlg.

To show optional text on the ExitDlg, set the WIXUI_EXITDIALOGOPTIONALTEXT property to the string you want to show. For example:

    <Property Id="WIXUI_EXITDIALOGOPTIONALTEXT" Value="Thank you for installing this product." />

The optional text has the following behavior:

* The optional text is displayed as literal text, so properties surrounded by square brackets such as [ProductName] will not be resolved. If you need to include property values in the optional text, you must schedule a custom action to set the property. For example:

        <CustomAction Id="CA_Set_WIXUI_EXITDIALOGOPTIONALTEXT" Property="WIXUI_EXITDIALOGOPTIONALTEXT" Value="Thank you for installing [ProductName]."/>
        <InstallUISequence>
          <Custom Action="CA_Set_WIXUI_EXITDIALOGOPTIONALTEXT" After="FindRelatedProducts">NOT Installed</Custom>
        </InstallUISequence>

* Long strings will wrap across multiple lines.
* The optional text is only shown during initial installation, not during maintenance mode or uninstall.

## Customizing the text in built-in dialogs

All text displayed in built-in WixUI dialog sets can be overridden with custom strings if desired. In order to do so, you must add a string to your product&apos;s WiX localization (.wxl) file that has the same Id value as the string that you want to override. You can find the WixUI string Id values by looking in the file named WixUI_en-us.wxl in the WiX source code.

For example, to override the descriptive text on the WelcomeDlg, you would add the following to a .wxl file in your project:

    <String Id="WelcomeDlgDescription">This is a custom welcome message. Click Next to continue or Cancel to exit.</String>

## Changing the UI sequence of a built-in dialog set

Each of the WixUI dialog sets contains a pre-defined set of dialogs that will be displayed in a specific order. Information about the dialogs included in each built-in WixUI dialog set can be found in the [WixUI Dialog Library Reference](dialog_reference/index.html).

It is possible to change the default sequence of a built-in dialog set. To do so, you must copy the contents of the &lt;Fragment/&gt; that includes the definition of the dialog set that you want to customize from the WiX source code to your project. Then, you must modify the &lt;Publish/&gt; elements to define the exact dialog sequence that you want in your installation experience.

For example, to remove the LicenseAgreementDlg from the [WixUI_InstallDir](dialog_reference/WixUI_installdir.html) dialog set, you would do the following:

<ol>
  <li>Copy the full contents of the &lt;Fragment/&gt; defined in WixUI_InstallDir.wxs in the WiX source code to your project.</li>
  <li>Remove the &lt;Publish/&gt; elements that are used to add Back and Next events for the LicenseAgreementDlg.</li>
  <li>Change the &lt;Publish/&gt; element that is used to add a Next event to the WelcomeDlg to go to the InstallDirDlg instead of the LicenseAgreementDlg. For example:
<pre>
&lt;Publish Dialog="WelcomeDlg" Control="Next" Event="NewDialog" Value="<b>InstallDirDlg</b>"&gt;1&lt;/Publish&gt;
</pre>
  </li>
  <li>Change the &lt;Publish/&gt; element that is used to add a Back event to the InstallDirDlg to go to the WelcomeDlg instead of the LicenseAgreementDlg. For example:
<pre>
&lt;Publish Dialog="InstallDirDlg" Control="Back" Event="NewDialog" Value="<b>WelcomeDlg</b>"&gt;1&lt;/Publish&gt;
</pre>
  </li>
</ol>

## Inserting a custom dialog into a built-in dialog set

You can add custom dialogs to the UI sequence in a built-in WixUI dialog set. To do so, you must define a &lt;UI/&gt; element for your new dialog. Then, you must copy the contents of the &lt;Fragment/&gt; that includes the definition of the dialog set that you want to customize from the WiX source code to your project. Finally, you must modify the &lt;Publish/&gt; elements to define the exact dialog sequence that you want in your installation experience.

For example, to insert a dialog named SpecialDlg between the WelcomeDlg and the LicenseAgreementDlg in the [WixUI_InstallDir](dialog_reference/WixUI_installdir.html) dialog set, you would do the following:

<ol>
  <li>Define the appearance of the SpecialDlg in a &lt;UI/&gt; element in your project.</li>
  <li>Copy the full contents of the &lt;Fragment/&gt; defined in WixUI_InstallDir.wxs in the WiX source code to your project.</li>
  <li>Add &lt;Publish/&gt; elements that define the Back and Next events for the SpecialDlg. For example:
<pre>
&lt;Publish Dialog="<b>SpecialDlg</b>" Control="Back" Event="NewDialog" Value="WelcomeDlg"&gt;1&lt;/Publish&gt;
&lt;Publish Dialog="<b>SpecialDlg</b>" Control="Next" Event="NewDialog" Value="LicenseAgreementDlg"&gt;1&lt;/Publish&gt;
</pre>
  </li>
  <li>Change the &lt;Publish/&gt; element that is used to add a Next event to the WelcomeDlg to go to the SpecialDlg instead of the LicenseAgreementDlg. For example:
<pre>
&lt;Publish Dialog="WelcomeDlg" Control="Next" Event="NewDialog" Value="<b>SpecialDlg</b>"&gt;1&lt;/Publish&gt;
</pre>
  </li>
  <li>Change the &lt;Publish/&gt; element that is used to add a Back event to the LicenseAgreementDlg to go to the SpecialDlg instead of the WelcomeDlg. For example:
<pre>
&lt;Publish Dialog="LicenseAgreementDlg" Control="Back" Event="NewDialog" Value="<b>SpecialDlg</b>"&gt;1&lt;/Publish&gt;
</pre>
  </li>
</ol>
