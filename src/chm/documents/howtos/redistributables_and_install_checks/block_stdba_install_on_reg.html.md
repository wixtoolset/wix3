---
title: How To: Block Bootstrapper Installation Based on Registry Key
layout: documentation
---
# How To: Block Bootstrapper Installation Based on Registry Key

In this example, the bootstrapper will install the 4.0 .Net Framework, if necessary, and then the specific application.
However, the application depends on a previous installation of third-party software. Ideally, the user wants to abort 
the installation and avoid a time-consuming .Net Framework install, if the software can't be used.  An existance check 
for a registry key, in this example, allows the install to abort if it's not found.  Here's how it's done:

The process requires both the WiX Util and the WiX Bal Extensions.  Reference the dll libraries from the bootstrapper 
project, and add the schema to the Wix element. (The .Net Framework extension is included merely as part of the example.)
The Wix element should look like this:

    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi" 
         xmlns:util="http://schemas.microsoft.com/wix/UtilExtension" 
         xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension" 
         xmlns:bal="http://schemas.microsoft.com/wix/BalExtension">

The util:RegistrySearch element defines a WiX variable, ThirdPartyCOMLibraryInstalled, that will be True when 
the key exists.

    <util:RegistrySearch
          Id='SearchForThirdParty' 
          Variable="ThirdPartyCOMLibraryInstalled" 
          Result="exists"
          Root="HKLM"
          Key="SOFTWARE\Classes\ThirdPartyId.Server\CLSID"/>

The WiX variable, ThirdPartyCOMLibraryInstalled, is used as the bal:Condition check expression.  If False, 
the value of the 'Message' attribute is displayed, and the installation is aborted.

    <bal:Condition Message="ThirdParty Application COM Library Required.">
        ThirdPartyCOMLibraryInstalled
    </bal:Condition>

If the code is organized in a Fragment, as in this example, an element must be referenced from the 
Bundle to include it. The util:RegistrySearch element is referenced:

    <util:RegistrySearchRef Id='SearchForThirdParty' />

The complete Bundle illustrates the required elements in context.

    <?xml version="1.0" encoding="UTF-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi" 
         xmlns:util="http://schemas.microsoft.com/wix/UtilExtension" 
         xmlns:netfx="http://schemas.microsoft.com/wix/NetFxExtension" 
         xmlns:bal="http://schemas.microsoft.com/wix/BalExtension">
      <Bundle Name="!(bind.packageName.MyApp)" 
              Version="!(bind.packageVersion.MyApp)" 
              Manufacturer="!(bind.packageManufacturer.MyApp)" 
              UpgradeCode="a07ce1d5-a7ed-4d89-a7ee-fb13a5dd69ba" 
              Copyright="Copyright (c) 2013 [Bundle/@Manufacturer]. All rights reserved."
              IconSourceFile="$(var.My_Application1.ProjectDir)MyCo.ico">
        <Variable Name="InstallFolder" 
            Type="string" 
            Value="[ProgramFilesFolder]MyCo Systems\My_Application\"
        /> 
        <util:RegistrySearchRef Id='SearchForThirdParty' />
        <BootstrapperApplicationRef 
          Id="WixStandardBootstrapperApplication.HyperlinkLicense" >
          <bal:WixStandardBootstrapperApplication 
            LaunchTarget="[InstallFolder]My_Application.exe" 
            SuppressRepair="yes" 
            SuppressOptionsUI="yes"
            LicenseUrl=""
            LogoFile="Resources/MyCoLogoWt64.png"
          />
        </BootstrapperApplicationRef>
        <Chain>
          <PackageGroupRef Id="NetFx40Redist"/>
          <MsiPackage Id ="MyApp" 
            Vital="yes" 
            Name="My Application" 
            SourceFile="$(var.MyApp_Install.TargetDir)MyApp_Install.msi">
            <MsiProperty Name="INSTALLLOCATION" Value="[InstallFolder]" />
          </MsiPackage>
        </Chain>
      </Bundle>
      <Fragment>
        <util:RegistrySearch
              Id='SearchForThirdParty' 
              Variable="ThirdPartyCOMLibraryInstalled" 
              Result="exists"
              Root="HKLM"
              Key="SOFTWARE\Classes\ThirdPartyId.Server\CLSID"/>
        <bal:Condition 
          Message="ThirdParty Application COM Library Required.">
          ThirdPartyCOMLibraryInstalled
        </bal:Condition>
      </Fragment>
    </Wix>
