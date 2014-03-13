---
title: WixWaitForEvent Custom Action
layout: documentation
after: using_standard_customactions
---
# WixWaitForEvent Custom Action

If you have scenarios you want to test where a package or bundle takes a while to
install, you can write a simple MSI package that includes the WixWaitForEvent custom
action to simulate this behavior. This custom action waits for either of the globally
named automatic reset events documented below and will either return ERROR\_INSTALL\_FAILURE
or ERROR\_SUCCESS depending on which event you signal.

* Global\WixWaitForEventFail - when signaled, the custom action returns ERROR\_INSTALL\_FAILURE.
* Global\WixWaitForEventSucceed - when signaled, the custom action returns ERROR\_SUCCESS.

This is especially useful in test cases when you don&apos;t want or need to build
your entire product and only want small test packages.

You can also test MSP packages using this custom action. If the WixWaitForEvent
custom action is authored into the target MSI, depending on what condition you author
the custom actions will still run. You can also add this custom action to your upgrade
MSI package used for building your MSP package, but then the custom actions will
not run during MSP uninstall unless you explicitly author them as patch uninstall
custom actions.

Follow the steps below to include this custom action in your MSI package and schedule
it whenever in your sequence you prefer. You can use the WixWaitForEvent 
immediate custom action or the WixWaitForEventDeferred deferred custom action. If you want to author the custom action
in additional places throughout your sequence, you will have to author the 
CustomAction elements yourself using different CustomAction/@Id attribute 
values. The binary is WixCA and the entry point is WixWaitForEvent.

## Step 1: Add the WiX utilities extensions library to your project

The WiX support for WixWaitForEvent is included in a WiX extension library that
must be added to your project prior to use. If you are using WiX on the command
line you need to add the following to your light command line:

<pre>
light.exe myproject.wixobj -ext <span>WixUtilExtension</span>
</pre>

If you are using Votive you can add the extension using the Add Reference dialog:

1. Open your Votive project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the <strong>WixUtilExtension.dll</strong> assembly from the list and click
Add
1. Close the Add Reference dialog

## Step 2: Add a reference to the WixWaitForEvent custom action

To add a reference to the WixWaitForEvent 
immediate custom action, include the following in
your WiX setup authoring:

    <CustomActionRef Id="WixWaitForEvent" />

This will cause WiX to add the WaitWaitForEvent custom action to your MSI 
as an immediate custom action scheduled immediately before InstallFinalize. This 
will block the installation after script generation. You can
schedule it anywhere else in your sequence.

To add a reference to the WixWaitForEventDeferred deferred custom action, 
include the following in your WiX setup authoring:

    <CustomActionRef Id="WixWaitForEventDeferred" />

This deferred custom action is scheduled immediately after InstallInitialize so 
it will block after starting script execution. You can schedule this custom 
action anywhere else in your sequence as well.

You can schedule both custom actions in the same package, but you will need to 
signal either of the named automatic reset events documented above both times.

## Step 3: Build your MSI and test various scenarios

Once you&apos;ve built your MSI package you can install it using msiexec.exe, Burn,
or by any other means you wish. When Windows Installer executes your custom action,
Windows Installer will wait for you to signal either the event documented above.
Depending on the named event you signal, the custom action will fail or succeed 
causing the MSI or MSP package to fail or succeed.
