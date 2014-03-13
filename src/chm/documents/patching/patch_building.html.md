---
title: Using Patch Creation Properties
layout: documentation
---

# Using Patch Creation Properties

A patch contains the differences between one or more pairs of Windows Installer packages. The tool PatchWiz.dll in the <a href="http://www.microsoft.com/en-us/download/details.aspx?id=3138" target="_blank">Windows SDK</a> compares pairs of packages and produces a patch using a file called a Patch Creation Properties (PCP) file.

It is recommended that you download the latest Windows SDK to get the newest tools for building patches.

## Setting Up the Sample

A Patch Creation Properties (PCP) file instructs PatchWiz.dll to generate a patch from differences in one or more pairs of packages. A patch contains the differences between the target and upgrade packages, and will transform the target package to the upgrade package. Both the target and upgrade packages are created below.

### Create a directory that will contain the sample

Create a directory from which you plan on running the sample. This will be the sample root.

    md C:\sample

### Create two subdirectories

Under the sample root create two subdirectories called &quot;1.0&quot; and &quot;1.1&quot;.

    md C:\sample\1.0
    md C:\sample\1.1

### Create a text file called Sample.txt for 1.0

Create a text file in the &quot;1.0&quot; directory called Sample.txt and put some text in it telling you that it is the 1.0 version of the file.

    echo This is version 1.0 > C:\sample\1.0\Sample.txt

### Create a text file called Sample.txt for 1.1

Create a text file in the &quot;1.1&quot; directory called Sample.txt and put some text in it telling you that it is the 1.1 version of the file.

    echo This is version 1.1 > C:\sample\1.1\Sample.txt

### Create your product authoring in the sample root folder

Create your product authoring in the sample root folder called Product.wxs with the following contents:

    <?xml version="1.0" encoding="UTF-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
        <Product Id="48C49ACE-90CF-4161-9C6E-9162115A54DD"
            Name="WiX Patch Example Product"
            Language="1033"
            Version="1.0.0"
            Manufacturer="Dynamo Corporation"
            UpgradeCode="48C49ACE-90CF-4161-9C6E-9162115A54DD">
            <Package Description="Installs a file that will be patched."
                Comments="This Product does not install any executables"
                InstallerVersion="200"
                Compressed="yes" />
     
            <Media Id="1" Cabinet="product.cab" EmbedCab="yes" />
            <FeatureRef Id="SampleProductFeature"/>
        </Product>
     
        <Fragment>
            <Feature Id="SampleProductFeature" Title="Sample Product Feature" Level="1">
                <ComponentRef Id="SampleComponent" />
            </Feature>
        </Fragment>
     
        <Fragment>
            <DirectoryRef Id="SampleProductFolder">
                <Component Id="SampleComponent" Guid="{C28843DA-EF08-41CC-BA75-D2B99D8A1983}" DiskId="1">
                    <File Id="SampleFile" Name="Sample.txt" Source=".\$(var.Version)\Sample.txt" />
                </Component>
            </DirectoryRef>
        </Fragment>
     
        <Fragment>
            <Directory Id="TARGETDIR" Name="SourceDir">
                <Directory Id="ProgramFilesFolder" Name="PFiles">
                    <Directory Id="SampleProductFolder" Name="Patch Sample Directory">
                    </Directory>
                </Directory>
            </Directory>
        </Fragment>
    </Wix>

### Create your patch authoring in the sample root

Create your Patch Creation Properties (PCP) authoring in the sample root called Patch.wxs with the following content:

    <?xml version="1.0" encoding="utf-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
        <PatchCreation
            Id="224C316C-5894-4771-BABF-21A3AC1F75FF"
            CleanWorkingFolder="yes"
            OutputPath="patch.pcp"
            WholeFilesOnly="yes"
            >
     
            <PatchInformation 
                Description="Small Update Patch" 
                Comments="Small Update Patch" 
                ShortNames="no" 
                Languages="1033" 
                Compressed="yes" 
                Manufacturer="Dynamo Corp"/>
     
            <PatchMetadata
                AllowRemoval="yes"
                Description="Small Update Patch"
                ManufacturerName="Dynamo Corp"
                TargetProductName="Sample"
                MoreInfoURL="http://www.dynamocorp.com/"
                Classification="Update"
                DisplayName="Sample Patch"/>
     
            <Family DiskId="5000"
                MediaSrcProp="Sample" 
                Name="Sample"
                SequenceStart="5000">
                <UpgradeImage SourceFile="C:\sample\1.1\admin\product.msi" Id="SampleUpgrade">
                    <TargetImage SourceFile="C:\sample\1.0\admin\product.msi" Order="2"      
                        Id="SampleTarget" IgnoreMissingFiles="no" />
                </UpgradeImage>
            </Family>
     
            <PatchSequence PatchFamily="SamplePatchFamily"
                Sequence="1.0.0.0"
                Supersede="yes" />
     
        </PatchCreation>
    </Wix>

Note that <b>SequenceStart</b> must be greater than the last sequence in the File table in the target package or the patch will not install.

## Build the Target and Upgrade Packages

Open a command prompt and make sure the following WiX and Windows Installer SDK tools are in your PATH.

* Candle.exe
* Light.exe
* MsiMsp.exe
* PatchWiz.dll
* MSPatchC.dll
* MakeCab.exe

### Build the target package

    candle.exe -dVersion=1.0 product.wxs
    light.exe product.wixobj -out 1.0\product.msi

### Perform an administrative installation of the target package

Msiexec.exe is used to perform an administrative installation but nothing is actually registered on your system. It is mainly file extraction.

    msiexec.exe /a 1.0\product.msi /qb TARGETDIR=C:\sample\1.0\admin

### Build the upgrade package

    candle.exe -dVersion=1.1 product.wxs
    light.exe product.wixobj -out 1.1\product.msi

### Perform an administrative installation of the upgrade package

    msiexec.exe /a 1.1\product.msi /qb TARGETDIR=C:\sample\1.1\admin

## Build the Patch

The Patch.wxs file is compiled into a PCP file that is then processed by MsiMsp.exe to product the patch package.

    candle.exe patch.wxs
    light.exe patch.wixobj -out patch\patch.pcp
    msimsp.exe -s patch\patch.pcp -p patch\patch.msp -l patch.log

## Verify the Patch

To verify that the patch works, install the product and then the patch.

### Install the 1.0 product

    msiexec.exe /i 1.0\product.msi /l*vx install.log

### Verify version 1.0

Go to &quot;Program Files\Patch Sample Directory&quot; and open Sample.txt. Verify that this is the 1.0 version. Close Sample.txt.

### Install the patch

    msiexec.exe /p patch\patch.msp /l*vx patch.log

### Verify version 1.1

Go to &quot;Program Files\Patch Sample Directory&quot; and open Sample.txt. Verify that this is now the 1.1 version. Close Sample.txt.

### Uninstall the patch

On Windows XP Service Pack 2 and Windows Server 2003, go to &quot;Add/Remove Programs&quot; in the Control Panel and make sure that Show Updates is checked. On Windows Vista and newer, go to &quot;Programs&quot; then &quot;View installed updates&quot; in the Control panel. Select &quot;Sample Patch&quot; from under &quot;WiX Patch Example Product&quot; and click the Uninstall button.

Go to &quot;Program files\Patch Sample Directory&quot; and open Sample.txt. Verify that this is again the 1.0 version. Close Sample.txt.

### Uninstall the product

On Windows XP Service Pack 2 and Windows Server 2003, go to &quot;Add/Remove Programs&quot; in the Control Panel. On Windows Vista and newer, go to &quot;Programs&quot; then &quot;Uninstall a program&quot; in the Control Panel. Select &quot;WiX Patch Example Product&quot; and click the Uninstall button.

## Restrictions

Please review [restrictions](patch_restrictions.html) on how patches must be built to avoid problem during patch installation.
