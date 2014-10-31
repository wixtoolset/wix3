---
title: Reading the Default WiX Project Template
layout: documentation
after: votive_property_pages
---

# Reading the Default WiX Project Template

Once a WiX project is created, it creates a file containing the beginning of
the setup code for the project. Everything needed to create an MSI can be added
to this file.

**Note**: If you are not familiar with Windows Installer setup packages, you are strongly encouraged to review the MSDN documentation about the <a href="http://msdn.microsoft.com/library/Aa369294.aspx" target="_blank">Installation Package</a> before continuing. It will provide a lot of valuable context as we dig into the details of a Windows Installer setup package.

    <?xml version="1.0" encoding="UTF-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
        <Product Id="*" Name="MySetup" Language="1033" Version="1.0.0.0" Manufacturer="MyCompany" UpgradeCode="$guid3$">
            <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
    
            <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />
            <MediaTemplate />
    
            <Feature Id="ProductFeature" Title="MySetup" Level="1">
                <ComponentGroupRef Id="ProductComponents" />
            </Feature>
        </Product>
    
        <Fragment>
            <Directory Id="TARGETDIR" Name="SourceDir">
                <Directory Id="ProgramFilesFolder">
                    <Directory Id="INSTALLFOLDER" Name="MySetup" />
                </Directory>
            </Directory>
        </Fragment>
    
        <Fragment>
            <ComponentGroup Id="ProductComponents" Directory="INSTALLFOLDER">
                <!-- <Component Id="ProductComponent"> -->
                    <!-- TODO: Insert files, registry keys, and other resources here. -->
                <!-- </Component> -->
            </ComponentGroup>
        </Fragment>
    </Wix>

If you are familiar with the Windows Installer, the structure of the .wxs file should be familiar. First, the Wix element exists purely to wrap the rest of the content in the file. The Wix element also specifies the namespace, the xmlns attribute that enables validation during compile and auto-complete in Visual Studio via IntelliSense. Next, the Product element defines the required Windows Installer properties used to identify the product, such as the <a href="http://msdn.microsoft.com/library/Aa370854.aspx" target="_blank">ProductCode</a>, <a href="http://msdn.microsoft.com/library/Aa370857.aspx" target="_blank">ProductName</a>, <a href="http://msdn.microsoft.com/library/Aa370856.aspx" target="_blank">ProductLanguage</a>, and <a href="http://msdn.microsoft.com/library/Aa370859.aspx" target="_blank">ProductVersion</a>. Third, the Package element contains the attributes for the <a href="http://msdn.microsoft.com/library/Aa372045.aspx" target="_blank">Summary Information Stream</a> that provides information about the setup package itself. The rest of the elements, except the ComponentRef element, map to Windows Installer tables by the same name, for example the <a href="http://msdn.microsoft.com/library/Aa368295.aspx" target="_blank">Directory table</a>, <a href="http://msdn.microsoft.com/library/Aa368007.aspx" target="_blank">Component table</a>, and <a href="http://msdn.microsoft.com/library/Aa368585.aspx" target="_blank">Feature table</a>. The Component element is tied to the Features which maps to the entries in the <a href="http://msdn.microsoft.com/library/Aa368579.aspx" target="_blank">FeatureComponents table</a>.

The default template that is generated when you create a new WiX project will generates a build warning. In the Output window, you may see this warning:

> The cabinet &apos;MySetup.cab&apos; does not contain any files. If this installation contains no files, this warning can likely be safely ignored. Otherwise, please add files to the cabinet or remove it.

Because the WiX project does not yet reference an application, there is nothing
to install. Once a file is added to the installer, this warning will go away.
