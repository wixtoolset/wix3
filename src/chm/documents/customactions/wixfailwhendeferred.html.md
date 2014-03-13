---
title: WixFailWhenDeferred Custom Action
layout: documentation
after: using_standard_customactions
---
# WixFailWhenDeferred Custom Action

When authoring <a href="http://msdn.microsoft.com/library/aa368268.aspx" target="_blank">deferred custom actions</a> (which are custom actions that change the system state) in an MSI, it is necessary to also provide an equivalent set of rollback custom actions to undo the system state change in case the MSI fails and rolls back. The rollback behavior typically needs to behave differently depending on if the MSI is currently being installed, repaired or uninstalled. This means that the following scenarios need to be accounted for when coding and testing a set of deferred custom actions to make sure that they are working as expected during both success and failure cases:

1. Successful install
1. Failed install
1. Successful repair
1. Failed repair
1. Successful uninstall
1. Failed uninstall

The failure cases are often difficult to simulate by unit testing the custom action code directly because deferred custom action code typically depends on state information provided to it by Windows Installer during an active installation session. As a result, this type of testing usually requires fault injection in order to cause the rollback custom actions to be executed at the proper times during real installation scenarios.

WiX includes a simple deferred custom action named WixFailWhenDeferred to help make it easier to test rollback custom actions in an MSI. WixFailWhenDeferred will always fail when it is executed during the installation, repair or uninstallation of an MSI. Adding the WixFailWhenDeferred custom action to your MSI allows you to easily inject a failure into your MSI in order to test your rollback custom actions.

There are 3 steps you need to take to use the WixFailWhenDeferred custom action to test the rollback custom actions in your MSI:

## Step 1: Add the WiX utilities extensions library to your project

The WiX support for WixFailWhenDeferred is included in a WiX extension library that must be added to your project prior to use. If you are using WiX on the command line you need to add the following to your light command line:

    light.exe myproject.wixobj -ext WixUtilExtension

If you are using Votive you can add the extension using the Add Reference dialog:

1. Open your Votive project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the <strong>WixUtilExtension.dll</strong> assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add a reference to the WixFailWhenDeferred custom action

To add a reference to the WixFailWhenDeferred custom action, include the following in your WiX setup authoring:

    <CustomActionRef Id="WixFailWhenDeferred" />

This will cause WiX to add the WixFailWhenDeferred custom action to your MSI, schedule it immediately before the <a href="http://msdn.microsoft.com/library/aa369505.aspx" target="_blank">InstallFinalize</a> action and condition it to only run if the property WIXFAILWHENDEFERRED=1.

## Step 3: Build your MSI and test various scenarios

The WixFailWhenDeferred custom action is conditioned to run only when the <a href="http://msdn.microsoft.com/library/aa370912.aspx" target="_blank">Windows Installer public property</a> WIXFAILWHENDEFERRED=1. After building your MSI with a reference to the WixFailWhenDeferred custom action, you can use the following set of command lines to simulate a series of standard install and rollback testing scenarios:

1. <b>Standard install:</b> msiexec.exe /i MyProduct.msi /qb /l*vx %temp%\MyProductInstall.log
1. <b>Install rollback:</b> msiexec.exe /i MyProduct.msi /qb /l*vx %temp%\MyProductInstallFailure.log WIXFAILWHENDEFERRED=1
1. <b>Standard repair:</b> msiexec.exe /fvecmus MyProduct.msi /qb /l*vx %temp%\MyProductRepair.log
1. <b>Repair rollback:</b> msiexec.exe /fvecmus MyProduct.msi /qb /l*vx %temp%\MyProductRepairFailure.log WIXFAILWHENDEFERRED=1
1. <b>Standard uninstall:</b> msiexec.exe /x MyProduct.msi /qb /l*vx %temp%\MyProductUninstall.log
1. <b>Uninstall rollback:</b> msiexec.exe /x MyProduct.msi /qb /l*vx %temp%\MyProductUninstallFailure.log WIXFAILWHENDEFERRED=1
