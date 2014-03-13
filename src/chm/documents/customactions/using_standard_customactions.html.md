---
title: Using Standard Custom Actions
layout: documentation
---

# Using Standard Custom Actions

Custom actions add the ability to install and configure many new types of resources. Each of these resource types has one or more elements that allow you to install them with your MSI package. The only things you need to do are understand the appropriate elements for the resources you want to install and set the required attributes on these elements. The elements need to be prefixed with the appropriate namespace for the WiX extension they are defined in. You must pass the full path to the extension DLL as part of the command lines to the compiler and linker so they automatically add the all of the proper error messages, custom action records, and binary records into your final MSI.

## Example

First, let&apos;s try an example that creates a user account when the MSI is installed. This functionality is defined in WixUtilExtension.dll and exposed to the user as the &lt;User&gt; element.

    <Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension' >
        <Product Id='PutGuidHere' Name='TestUserProduct' Language='1033' Version='0.0.0.0'>
            <Package Id='PUT-GUID-HERE' Description='Test User Package' InstallerVersion='200' Compressed='yes' />
                <Directory Id='TARGETDIR' Name='SourceDir'>
                    <Component Id='TestUserProductComponent' Guid='PutGuidHere'>
                        <util:User Id='TEST_USER1' Name='testName1' Password='pa$$$$word'/>
                    </Component>
            </Directory>
    
            <Feature Id='TestUserProductFeature' Title='Test User Product Feature' Level='1'>
                <ComponentRef Id='TestUserProductComponent' />
            </Feature>
        </Product>
    </Wix>

This is a simple example that will create a new user on the machine named &quot;testName1&quot; with the password &quot;pa$$word&quot; (the preprocessor replaces $$$$ with $$).

To build the MSI from this WiX authoring:

1. Put the above code in a file named yourfile.wxs.
1. Replace the &quot;PUT-GUID-HERE&quot; attributes with real GUIDs.
1. Run `candle.exe yourfile.wxs -ext %full path to WixUtilExtension.dll%`
1. Run `light.exe yourfile.wixobj -ext %full path to WixUtilExtension.dll% -out yourfile.msi yourfile.wixout`

Now, use Orca to open up the resulting MSI and take a look at the Error table, the CustomAction table, and the Binary table. You will notice that all of the relevant data for managing users has been added into the MSI. This happened because you have done two key things:

1. You made use of a &lt;User&gt; element under a &lt;Component&gt; element. This indicates that a user is to be installed as part of the MSI package, and the WiX compiler automatically added the appropriate MSI table data used by the custom action.
1. You linked with the appropriate extension DLL (WixUtilExtension.dll). This caused the linker to automatically pull all of the relevant custom actions, error messages, and binary table rows into the MSI.
