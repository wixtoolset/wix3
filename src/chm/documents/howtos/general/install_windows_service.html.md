---
title: How To: Install a windows service
layout: documentation
---
# How To: Install a windows service
To install a Windows service, you use the [&lt;ServiceInstall&gt;](./../../xsd/wix/serviceinstall.html) element from the Wix schema.
Fine-tuned configuration can be made using the [&lt;ServiceControl&gt;](./../../xsd/wix/servicecontrol.html) element, and the &lt;ServiceConfig&gt;
elements from the Wix and Util schema.

## Step 1: Add the &lt;ServiceInstall&gt; element to Product.wxs
Your main XML file, normally named Product.wxs, should include a &lt;ServiceInstall&gt; element with the basic information about the service to install.
This element should be the child of a [&lt;Component&gt;](./../../xsd/wix/component.html). It is most useful to use the &lt;Component&gt; representing
the service executable file as the parent.

In the &lt;ServiceInstall&gt; you define various attributes of the service, as explained in the element's documentation.

**Tip:** to specify a system account, such as LocalService or NetworkService, use the prefix "NT AUTHORITY", e.g. use the value `NT AUTHORITY\LocalService`
as the `Account` attribute value, to make the service run under this account.

## Step 2: Configure service failure actions (Optional)
Using the [&lt;ServiceConfig&gt;](./../../xsd/util/serviceconfig.html) element in the `util` schema, you can configure how the service behaves if it
fails. To use it, first, [include](extension_usage_introduction.html#using-wix-extensions-in-visual-studio) the [util](./../../xsd/util/index.html)
schema in your XML file, and prefix the element name with the `util` prefix:

    <ServiceInstall>
        <util:ServiceConfig FirstFailureActionType="restart"
                            SecondFailureActionType="restart"
                            ThirdFailureActionType="restart" /> 
    </ServiceInstall>

## Step 3: Configure additional options (Optional)
Using the [&lt;ServiceConfig&gt;](./../../xsd/wix/serviceconfig.html) element in the `wix` schema, You can configure additional settings, such as
DelayedAutoStart, or whether to configure the service when installing, reinstalling or uninstalling the installer. Wix is the default schema
name in Wix Toolkit, and need not be included explicitly to be used. Its parent element is &lt;Component&gt; or &lt;ServiceInstall&gt;, so it can be
added along side the above &lt;util:ServiceConfig&gt;:

    <ServiceInstall>
        <util:ServiceConfig FirstFailureActionType="restart"
                            SecondFailureActionType="restart"
                            ThirdFailureActionType="restart" />
        <ServiceConfig DelayedAutoStart="yes"
                       OnInstall="yes" /> 
    </ServiceInstall>
