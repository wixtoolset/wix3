---
title: How To: Install a Windows service
layout: documentation
after: specifying_source_files
---
# How To: Install a Windows service
To install a Windows service, use the [&lt;ServiceInstall&gt;](./../../xsd/wix/serviceinstall.html) 
element. Other configuration can be made using the 
[&lt;ServiceControl&gt;](./../../xsd/wix/servicecontrol.html) element and the 
[&lt;ServiceConfig&gt;](./../../xsd/util/serviceconfig.html) element from WixUtilExtension.

## Step 1: Install the service
The &lt;ServiceInstall&gt; element contains the basic information about the service to install.
This element should be the child of a [&lt;Component&gt;](./../../xsd/wix/component.html) element
whose key path is a sibling [&lt;File&gt;](./../../xsd/wix/file.html) element that specifies the 
service executable file.

**Tip:** to specify a system account, such as LocalService or NetworkService, use the prefix 
`NT AUTHORITY`. For example, use `NT AUTHORITY\LocalService` as the `Account` attribute value
to run the service under this account.

## Step 2: Configure the service (optional)
Using the [&lt;util:ServiceConfig&gt;](./../../xsd/util/serviceconfig.html) element from WixUtilExtension, 
you can configure how the service behaves if it fails. To use it,
[add](extension_usage_introduction.html) WixUtilExtension to your project, add the 
the [util](./../../xsd/util/index.html) namespace to your WiX authoring,
and prefix the element name with the `util` prefix:

    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi" xmlns:util="http://schemas.microsoft.com/wix/UtilExtension">
      ...
      <ServiceInstall>
          <util:ServiceConfig FirstFailureActionType="restart"
                              SecondFailureActionType="restart"
                              ThirdFailureActionType="restart" /> 
      </ServiceInstall>
