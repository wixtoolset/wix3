---
title: Quiet Execution Custom Action
layout: documentation
after: using_standard_customactions
---

# Quiet Execution Custom Action

The QtExec custom action allows you to run an arbitrary command line in an MSI-based setup in silent mode. QtExec is commonly used to suppress console windows that would otherwise appear appear when invoking the executable directly. The custom action is located in the WixCA library, which is a part of the WixUtilExtension.

## Immediate execution

When the QtExec action is run as an immediate custom action, it will try to execute the command stored in the QtExecCmdLine property. The following is an example of authoring an immediate QtExec custom action:

    <Property Id="QtExecCmdLine" Value="command line to run"/>
    <CustomAction Id="QtExecExample" BinaryKey="WixCA" DllEntry="CAQuietExec" Execute="immediate" Return="check"/>
    .
    .
    .
    <InstallExecuteSequence>
      <Custom Action="QtExecExample" After="TheActionYouWantItAfter"/>
    </InstallExecuteSequence>

This will result in running the command line in the immediate sequence. If the exit code of the command line in this example indicates an error (meaning that the return code is not equal to 0) then the setup will fail because the Return value is set to &ldquo;check.&quot; Changing the Return value to &quot;ignore&quot; will cause the setup to log the failure but skip it and continue instead of failing the entire setup.

If you want to run more than one command line in the immediate sequence then you will need to schedule multiple QtExec custom actions and set the QtExecCmdLine property to a new value by scheduling a property-setting custom action immediately before each instance of the QtExec custom action.

## Deferred execution

When the QtExec action is run as a deferred custom action, it will try to execute the command line stored in the value of the custom action data. For deferred QtExec custom actions, the custom action data is a property that has the same Id value as the custom action Id. The following is an example of authoring a deferred QtExec custom action:

    <Property Id="QtExecDeferredExample" Value="command line to run"/>
    <CustomAction Id="QtExecDeferredExample" BinaryKey="WixCA" DllEntry="CAQuietExec"
                Execute="deferred" Return="check" Impersonate="no"/>
    .
    .
    .
    <InstallExecuteSequence>
      <Custom Action="QtExecDeferredExample" After="TheActionYouWantItAfter"/>
    </InstallExecuteSequence>

If you need to set a command line that uses other Windows Installer properties, you must schedule an immediate custom action to set the command line property value and schedule a deferred custom action to run QtExec. The Property value used in the immediate custom action must match the Id value used in the deferred custom action. A common use of this pattern for QtExec custom actions is to run an executable that will be installed as a part of the setup. The following is an example of authoring a deferred QtExec custom action that relies on another property value:

    <CustomAction Id="QtExecDeferredExampleWithProperty_Cmd" Property="QtExecDeferredExampleWithProperty"
                Value="&quot;[#MyExecutable.exe]&quot;" Execute="immediate"/>
    <CustomAction Id="QtExecDeferredExampleWithProperty" BinaryKey="WixCA" DllEntry="CAQuietExec"
                Execute="deferred" Return="check" Impersonate="no"/>
    .
    .
    .
    <InstallExecuteSequence>
      <Custom Action="QtExecDeferredExampleWithProperty_Cmd" After="CostFinalize"/>
      <Custom Action="QtExecDeferredExampleWithProperty" After="TheActionYouWantItAfter"/>
    </InstallExecuteSequence>

## Running 64-bit executables

If you need to run a 64-bit executable, use the 64-bit aware QtExec. To use the 64-bit QtExec change the CustomAction element&apos;s DllEntry attribute to &quot;CAQuietExec64&quot; and for immediate execution use the &quot;QtExec64CmdLine&quot; property. The following example combines the examples above the 64-bit aware QtExec for both. Notice that the CustomAction element&apos;s Id attributes do not need to change:

    <Property Id="QtExec64CmdLine" Value="64-bit command line to run"/>
    <CustomAction Id="QtExecExample" BinaryKey="WixCA" DllEntry="CAQuietExec64" Execute="immediate" Return="check"/>
    .
    .
    .
    <CustomAction Id="QtExecDeferredExampleWithProperty_Cmd" Property="QtExecDeferredExampleWithProperty"
                Value="&quot;[#MyExecutable.exe]&quot;" Execute="immediate" Return="ignore"/>
    <CustomAction Id="QtExecDeferredExampleWithProperty" BinaryKey="WixCA" DllEntry="CAQuietExec64"
                Execute="deferred" Return="check" Impersonate="no"/>
    .
    .
    .
    <InstallExecuteSequence>
      <Custom Action="QtExecExample" After="TheImmediateActionYouWantItAfter"/>
    
      <Custom Action="QtExecDeferredExampleWithProperty_Cmd" After="CostFinalize"/>
      <Custom Action="QtExecDeferredExampleWithProperty" After="TheDeferredActionYouWantItAfter"/>
    </InstallExecuteSequence>

## Building an MSI that uses QtExec

In order to use QtExec, you must include a reference to the WixUtilExtension when building your MSI. To do this, add the command line argument `-ext WixUtilExtension.dll` when calling Light.exe.
