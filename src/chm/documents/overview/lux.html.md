---
title: Unit-testing custom actions with Lux
layout: documentation
after: wixcop
---
# Unit-testing custom actions with Lux

Custom actions are a frequent cause of installation failures so it&apos;s important to test them thoroughly.
Custom actions themselves usually aren&apos;t tested. The traditional testing approach is to run functional 
tests on an entire installer and to cover as many scenarios and platform combinations as possible.
  
## Custom action patterns

WiX compiler extensions provide one way of improving custom action quality: Because compiler extensions
run at build time instead of install time, they can perform all sorts of data validation and conversion
on strongly-typed authoring before converting it to rows and columns of custom tables in the MSI
package.

Immediate custom actions then read those custom tables, check current state (for example, component 
action state, the state of the machine itself), and serialize the resulting data into a custom action
data property.
Immediate custom actions are the place to do the logic that needs live state and cannot be determined
at build time by a compiler extension.
Because immediate custom actions run in the security context of the installing user
and outside an installation transaction, they generally do not have permissions to modify the machine
and if they fail, the installation simply ends without the need to do any cleanup or rollback.

Deferred custom actions read the custom action data property set by immediate custom actions to know
what to do. One way to improve custom action reliability is to make as few decisions as possible in
deferred custom actions; instead, implement all the logic in compiler extensions and immediate
custom actions and have deferred custom actions simply read the custom action data property in a 
loop to modify the machine.
  
The WiX custom actions that modify the machine use this pattern. For example, XmlConfig authoring
is validated by the WixUtilExtension compiler extension and translated to rows and columns in the
XmlConfig table. The SchedXmlConfig immediate custom action reads the XmlConfig table, constructs
a custom action data property based on the XmlConfig table and machine&apos;s state (including checking
component state and storing existing file data to support rollback), then schedules the 
ExecXmlConfig deferred custom action to execute the XML changes and the ExecXmlConfigRollback
rollback custom action to roll back the changes.
  
## Testing with Lux

Lux is a WiX extension (and associated tools) that let you write data-driven unit tests for your custom actions.

The executive summary: Lux runs your immediate custom actions then validates they set properties to the values you expect.
  
While it&apos;s a simple approach, if your custom actions are factored as discussed above, validating
the properties set by immediate custom actions can validate all the interaction between your
custom actions, the MSI package, and MSI itself. 
  
If your custom actions aren&apos;t factored as discussed--for example, if your deferred custom actions
expect only an installation directory and have logic to construct file paths from it--then it&apos;s
likely that your immediate custom actions don&apos;t have a lot of logic that&apos;s useful to test.
  
Lux does not help you test the custom action code that actually modifies the machine;
for that, continue to use other unit-test frameworks and automated tests. By working only with immediate custom
actions, Lux can let MSI run the custom actions as-is, eliminating the need to write custom 
[test doubles](http://xunitpatterns.com/Test%20Double.html) for the MSI API. 
Lux runs from a per-user package so unless you run the tests from an elevated command prompt, 
none of the custom actions get elevated privileges and therefore cannot modify the machine.

Here&apos;s how Lux works:
  
1. You write your unit tests using XML in WiX source files.
1. The Lux extension converts the XML to a table in a test .msi package.
1. The Lux custom action runs after all other immediate custom actions and evaluates your unit tests.

# Authoring unit tests

Lux supports the following unit tests:

* Property values
* Expressions
* Multi-value properties
* Name/value-pair properties

Note that you should always author unit tests in fragments separate from your custom action authoring or any other product authoring.
If you mix unit tests with other authoring, WiX includes the unit-test data in your &quot;real&quot; installers.

## Property value tests

A simple test lets you specify a property to test, a value to test against, and the operator to compare with (which defaults to &quot;equal&quot;).

    <Fragment>
      <lux:UnitTest CustomAction="TestCustomActionSimple" Property="SIMPLE" Value="[INSTALLFOLDER]" Operator="equal" />
    </Fragment>

When the test runs, Lux compares the value of the SIMPLE property against the (formatted) value [INSTALLFOLDER].
If the two match (because the operator is &quot;equal&quot;), the test passes. Legal values of the Operator attribute are:

<dl>
    <dt><dfn>equal</dfn></dt>
        <dd>(Default) Compares Property to Value and succeeds if they are equal.</dd>
    <dt><dfn>notEqual</dfn></dt>
        <dd>Compares Property to Value and succeeds if they are NOT equal.</dd>
    <dt><dfn>caseInsensitiveEqual</dfn></dt>
        <dd>Compares Property to Value and succeeds if they are equal (ignoring case).</dd>
    <dt><dfn>caseInsensitiveNotEqual</dfn></dt>
        <dd>Compares Property to Value and succeeds if they are NOT equal (ignoring case).</dd>
</dl>
  
## Test conditions

Conditions let you validate code paths in your custom action. For example, if your custom action behaves differently on Windows XP than it does on Windows Vista and later, 
you can create two tests with mutually exclusive conditions:

    <Fragment>
      <lux:UnitTest CustomAction="TestCustomActionSimple" Property="SIMPLE" Value="[INSTALLFOLDER]">
        <lux:Condition><![CDATA[VersionNT < 600]]></lux:Condition>
      </lux:UnitTest>
      <lux:UnitTest CustomAction="TestCustomActionSimple" Property="SIMPLE" Value="[INSTALLFOLDER]">
        <lux:Condition><![CDATA[VersionNT >= 600]]></lux:Condition>
      </lux:UnitTest>
    </Fragment>

If a test has a condition, the test runs only if its condition is true.
  
## Expression tests

Expression tests let you test any valid MSI expression. If the expression is true, the test passes. If the expression is false or invalid, the test fails.

    <Fragment>
      <lux:UnitTest CustomAction="TestCustomActionSimple">
        <lux:Expression>NOT MsiSystemRebootPending AND SIMPLE</lux:Expression>
      </lux:UnitTest>
    </Fragment>

## Multi-value property tests

Because deferred custom actions can access only a single custom-action data property, custom actions that need more than one piece of data encode it in a single string. 
One way is to have the immediate custom action separate multiple elements with a known separator character, then have the deferred custom action split the string at
those separate characters. Lux supports such separators using the ValueSeparator and Index attributes.

    <Fragment>
      <lux:UnitTest CustomAction="TestCustomActionMultiValue" Property="MULTIVALUE" ValueSeparator="*">
        <lux:Condition>VersionNT</lux:Condition>
        <lux:UnitTest Index="0" Value="1" />
        <lux:UnitTest Index="1" Value="[INSTALLFOLDER]">
          <lux:Condition>NOT Installed</lux:Condition>
        </lux:UnitTest>
        <lux:UnitTest Index="2" Value="WIXEAST" />
      </lux:UnitTest>
    </Fragment>

A condition under the parent UnitTest element applies to all individual unit tests. Override it with a Condition child element.
  
## Name/value-pair property tests

Another way of providing multiple values to a deferred custom action is to combine name/value pairs into a single string. Lux supports name/value-pair
properties using the NameValueSeparator and Index attributes.

    <Fragment>
      <lux:UnitTest CustomAction="TestCustomActionNameValuePairs" Property="NAMEVALUEPAIRS" NameValueSeparator="#">
        <lux:UnitTest Index="InstallationRoot" Value="[INSTALLFOLDER]" />
        <lux:UnitTest Index="Developers" Operator="caseInsensitiveNotEqual" Value="WIXEAST" />
      </lux:UnitTest>
    </Fragment>
  
# Test mutations

Immediate custom actions frequently need to create different custom action data depending
on global machine state. For example, if a component is already installed, a custom action 
might have different behavior to upgrade the component, versus installing it for the first 
time.
  
Because Lux runs only immediate custom actions, it&apos;s not possible to actually update the
global machine state. One approach is to create multiple custom action DLLs, mocking MSI 
functions to return hard-coded values. Lux simplifies this model with <i>test mutations</i>.
  
Test mutations let you author unit tests with different expected results. The mutation
id is passed as the value of the WIXLUX\_RUNNING\_MUTATION property. Your custom action,
typically in an &apos;#ifdef DEBUG&apos; block, retrieves the WIXLUX\_RUNNING\_MUTATION property
and mock different behavior based on the mutation. To author test mutations, use the
Mutation element with UnitTest elements as children. For example:

    <lux:Mutation Id="SimulateDiskFull">
      <lux:UnitTest ... />
    </lux:Mutation>

Nit runs the test package once for each mutation, setting the WIXLUX\_RUNNING\_MUTATION 
property to one mutation id at a time. Tests that aren&apos;t children of a mutation are run 
every time.

# Building test packages

Lux unit tests run from a minimal package that includes just your unit tests and the resources they need to run. Because Lux runs only immediate
custom actions, it doesn&apos;t need a full, per-machine package that includes all the files and other resources to be installed. Such a minimal package
saves build time but does require that your WiX source code be well modularized with fragments.
For example, you should always author unit tests in fragments separate from any other authoring.
If you mix unit tests with other authoring, WiX includes the unit-test data in your &quot;real&quot; installers.
Likewise, any other WiX authoring included in unit-test fragments is included in test packages.
  
Lux comes with a tool that simplifies the creation of test packages. Its name is lux.exe. To use lux.exe:
  
1. Compile the source file containing your unit tests.
1. Run lux.exe on the .wixobj file and specify a source file for the test package.
1. Compile the test package source.
1. Link the test package .wixobj with the unit tests .wixobj.
  
For example:
  
    candle -ext WixLuxExtension CustomActions.wxs
    lux CustomActions.wixobj -out LuxSample1_test.wxs
    candle -ext WixLuxExtension LuxSample1_test.wxs
    light -ext WixLuxExtension LuxSample1_test.wixobj CustomActions.wixobj -out LuxSample1_test.msi

Lux also includes an MSBuild task and .targets file to let you build test packages from the same .wixproj you use to build your installers.
  To build a test package, build the BuildTestPackage target using MSBuild 3.5:
  
    %WINDIR%\Microsoft.NET\Framework\v3.5\MSBuild.exe /t:BuildTestPackage

# Running unit tests

After building the test package, you can run it with logging enabled to capture test results:

    msiexec /l test1.log /i bin\Debug\LuxSample1_test.msi

Search the log for <b>WixRunImmediateUnitTests</b> to see test results and other logging from the Lux custom action.
  
## Nit: The Lux test runner

Lux also includes Nit, a console program that monitors the logging messages emitted by unit tests and reports success or failure. To use Nit on your test packages, just specify their filenames as arguments to nit.exe. For example:

    nit LuxSample1_test.msi

Lux also lets you run Nit on your test packages from the same .wixproj you use to build your installers.
To run a test package under Nit, build the Test target using MSBuild 3.5:
  
    %WINDIR%\Microsoft.NET\Framework\v3.5\MSBuild.exe /t:Test

The test package will be built before the tests are run, if necessary. The output looks like the following, with failing tests highlighted in red as build errors:
  
    Test:
      Windows Installer Xml Unit Test Runner version 3.5.1204.0
      Copyright (C) Outercurve Foundation. All rights reserved.
    
      Test luxB21F0D12E0701DBA30FFB92A532A5390 passed: Property 'SIMPLE' matched expected value '[INSTALLFOLDER]'.
      Test TestConditionBeforeVista passed: Property 'SIMPLE' matched expected value '[INSTALLFOLDER]'.
      Test TestConditionVistaOrLater passed: Property 'SIMPLE' matched expected value '[INSTALLFOLDER]'.
      Test TestExpressionTruth passed: Expression 'NOT MsiSystemRebootPending AND SIMPLE' evaluated to true.
    nit.exe : error NIT8103: Test luxA6D27EC5903612D7F3786FF71952E314 failed: Property 'MULTIVALUE' expected value '2' but actual value was '1'.
      Test lux210257649C16AFA33793F1CDDF575505 passed: Property 'MULTIVALUE' matched expected value '[INSTALLFOLDER]'.
    nit.exe : error NIT8103: Test lux402940A90D3ADAD181D599AB8C260FA0 failed: Property 'MULTIVALUE' expected value 'xxxWIXEAST' but actual value was 'WIXEAST'.
      Test lux453EC8DB458A8F66F0D22970CFF2AE99 passed: Property 'NAMEVALUEPAIRS' matched expected value '[INSTALLFOLDER]'.
      Test lux20CB4F88795F22D15631FD60BA03AFEB passed: Property 'NAMEVALUEPAIRS' matched expected value 'WIXWEST'.
    nit.exe : error NIT8102: 2 tests failed. 7 tests passed.
    Done Building Project "C:\Delivery\Dev\wix35\src\lux\samples\LuxSample1\LuxSample1.wixproj" (Test target(s)) -- FAILED.
    
    Build FAILED.

    "C:\Delivery\Dev\wix35\src\lux\samples\LuxSample1\LuxSample1.wixproj" (Test target) (1) ->
    (Test target) -> 
      nit.exe : error NIT8103: Test luxA6D27EC5903612D7F3786FF71952E314 failed: Property 'MULTIVALUE' expected value '2' but actual value was '1'.
      nit.exe : error NIT8103: Test lux402940A90D3ADAD181D599AB8C260FA0 failed: Property 'MULTIVALUE' expected value 'xxxWIXEAST' but actual value was 'WIXEAST'.
      nit.exe : error NIT8102: 2 tests failed. 7 tests passed.

        0 Warning(s)
        3 Error(s)

    Time Elapsed 00:00:07.87
  
# FAQ

<dl>
    <dt>Are these really unit tests? They look a lot like <a href="http://fit.c2.com/">Fit tests</a>.</dt>
        <dd>Fit tests are tabular and data-driven, so they have a lot in common with Lux's unit tests. But fit tests are focused on high-level outputs, 
            whereas unit tests are low-level developer tests.
        </dd>
    <dt>Using the custom action code as-is sounds good, but are there any limitations with that approach?</dt>
        <dd>Yes. Because you are running the actual custom action, any code paths that rely on machine state reflect the state of the machine you run the tests on.
            For example, code that has different behavior on different versions of Windows runs only one way, just like it does in a normal installer. 
            You can add debug code that looks for the presence of the WIXLUXTESTPACKAGE property; it's set to 1 in a test package.
        </dd>
    <dt>I have unit tests that fail because directory properties are being returned as empty strings. Why?</dt>
        <dd>The most likely cause is that your directories are defined as children of your installer's Product element. Lux.exe builds its own Product element to
            product a minimal test package, so none of the resources defined in your Product are available to the unit tests. The simplest solution is to move those
            resources to their own Fragment.
        </dd>
    <dt>Do I have to write my custom actions in C++?</dt>
        <dd>No, Lux works with any immediate custom actions, regardless of the language they're written in, including
            MSI type 51 property-setting custom actions.
        </dd>
</dl>
