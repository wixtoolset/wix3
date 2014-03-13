---
title: WixExitEarlyWithSuccess Custom Action
layout: documentation
after: using_standard_customactions
---

# WixExitEarlyWithSuccess Custom Action

The WixExitEarlyWithSuccess custom action is an immediate custom action that does nothing except return the value <a href="http://msdn.microsoft.com/library/aa368072.aspx" target="_blank">ERROR\_NO\_MORE\_ITEMS</a>. This return value causes Windows Installer to skip all remaining actions in the .msi and return a process exit code that indicates a successful installation.

This custom action is useful in cases where you want setup to exit without actually installing anything, but want it to return success to the calling process. A common scenario where this type of behavior is useful is in an out-of-order installation scenario for an .msi that implements <a href="http://msdn.microsoft.com/library/aa369786.aspx" target="_blank">major upgrades</a>. When a user has version 2 of an .msi installed and then attempts to install version 1, this custom action can be used in conjunction with the <a href="http://msdn.microsoft.com/library/aa372379.aspx" target="_blank">Upgrade table</a> to detect that version 2 is already installed to cause setup to exit without installing anything and return success. If any applications redistribute version 1 of the .msi, their installation processes will continue to work even if the user has version 2 of the .msi installed on their system.

There are 3 steps you need to take to use the WixExitEarlyWithSuccess custom action in your MSI:

## Step 1: Add the WiX utilities extensions library to your project

The WiX support for WixExitEarlyWithSuccess is included in a WiX extension library that must be added to your project prior to use. If you are using WiX on the command line you need to add the following to your light command line:

    light.exe myproject.wixobj -ext WixUtilExtension

If you are using Votive you can add the extension using the Add Reference dialog:

1. Open your Votive project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the **WixUtilExtension.dll** assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add a reference to the WixExitEarlyWithSuccess custom action

To add a reference to the WixExitEarlyWithSuccess custom action, include the following in your WiX setup authoring:

    <CustomActionRef Id="WixExitEarlyWithSuccess" />

This will cause WiX to add the WixExitEarlyWithSuccess custom action to your MSI, schedule it immediately after the <a href="http://msdn.microsoft.com/library/aa368600.aspx" target="_blank">FindRelatedProducts</a> action and condition it to only run if the property named NEWERVERSIONDETECTED is set.

## Step 3: Add logic to define the NEWERVERSIONDETECTED property at the appropriate times

In order to cause the WixExitEarlyWithSuccess to run at the desired times, you must add logic to your installer to create the NEWERVERSIONDETECTED property. To implement the major upgrade example described above, you can add an Upgrade element like the following:

    <Upgrade Id="!(loc.Property_UpgradeCode)">
      <UpgradeVersion Minimum="$(var.ProductVersion)" OnlyDetect="yes" Property="NEWERVERSIONDETECTED" />
    </Upgrade>
