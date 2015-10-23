---
title: WixBroadcastSettingChange and WixBroadcastEnvironmentChange Custom Actions
layout: documentation
after: using_standard_customactions
---

# WixBroadcastSettingChange and WixBroadcastEnvironmentChange Custom Actions

The WixBroadcastSettingChange and WixBroadcastEnvironmentChange custom actions are immediate custom actions that send a WM\_SETTINGCHANGE message to all top-level windows indicating that settings have changed. WixBroadcastSettingChange indicates that unspecified settings have changed. WixBroadcastEnvironmentChange indicates that environment variables have changed.

Other programs can listen for WM\_SETTINGCHANGE and update any internal state with the new setting.

Windows Installer itself sends the WM\_SETTINGCHANGE message for settings it changes while processing an MSI package but cannot do so for changes a package makes via custom action. Also, Windows Installer does not send WM\_SETTINGCHANGE for environment variable changes when a reboot is pending.

There are two steps you need to take to use the WixBroadcastSettingChange or WixBroadcastEnvironmentChange custom actions in your MSI package:

## Step 1: Add the WiX utilities extensions library to your project

WixBroadcastSettingChange and WixBroadcastEnvironmentChange are included in a WiX extension library that must be added to your project prior to use. If you are using WiX on the command line you need to add the following to your light command line:

    light.exe myproject.wixobj -ext WixUtilExtension

If you are using Votive you can add the extension using the Add Reference dialog:

1. Open your Votive project in Visual Studio
1. Right click on your project in Solution Explorer and select Add Reference...
1. Select the **WixUtilExtension.dll** assembly from the list and click Add
1. Close the Add Reference dialog

## Step 2: Add a reference to the WixBroadcastSettingChange or WixBroadcastEnvironmentChange custom actions

To add a reference to the WixBroadcastSettingChange or WixBroadcastEnvironmentChange custom actions, include one of the following elements in your WiX setup authoring:

    <CustomActionRef Id="WixBroadcastSettingChange" />
    <CustomActionRef Id="WixBroadcastEnvironmentChange" />

This will cause WiX to add the custom action to your MSI and schedule it immediately after the <a href="http://msdn.microsoft.com/library/aa369505.aspx" target="_blank">InstallFinalize</a> standard action.
