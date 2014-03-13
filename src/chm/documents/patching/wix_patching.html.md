---
title: Using Purely WiX
layout: documentation
---

# Using Purely WiX

A patch can be created purely in WiX using the tools named Torch.exe and Pyro.exe. Using these tools eliminates the need to perform administrative installs or even to bind the upgrade product which, for large products, can be exhausting.

## Setting Up the Sample

A sample product is created which puts different resources into fragments. You put resources into separate fragments so that the resources in each fragment can be filtered out of a patch. You might filter some resources out of a patch if you want to limit the patch to update only parts of your product or products.

### Create a directory that will contain the sample

Create a directory from which you plan to run the sample. This will be the sample root.

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

Create your patch authoring in the sample root called Patch.wxs with the following content:

    <?xml version="1.0" encoding="UTF-8"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
        <Patch 
            AllowRemoval="yes"
            Manufacturer="Dynamo Corp" 
            MoreInfoURL="http://www.dynamocorp.com/"
            DisplayName="Sample Patch" 
            Description="Small Update Patch" 
            Classification="Update"
            >
     
            <Media Id="5000" Cabinet="RTM.cab">
                <PatchBaseline Id="RTM"/>
            </Media>
     
            <PatchFamilyRef Id="SamplePatchFamily"/>
        </Patch>
     
        <Fragment>    
            <PatchFamily Id='SamplePatchFamily' Version='1.0.0.0' Supersede='yes'>
                <ComponentRef Id="SampleComponent"/>
            </PatchFamily>
        </Fragment>
    </Wix>

## Building the Patch Sample

Open a command prompt and make sure that the following WiX tools are in your PATH.

* Candle.exe
* Light.exe
* Torch.exe
* Pyro.exe

Your WiX toolset version should be at least 3.0.3001.0

### Build the target layout

While only the .wixout is needed, the target product layout is created to test installing the patch. The product must also be installed before or along with the patch.

    cd C:\sample
    candle.exe -dVersion=1.0 product.wxs
    light.exe product.wixobj -out 1.0\product.msi

### Build the upgrade layout

    candle.exe -dVersion=1.1 product.wxs
    light.exe product.wixobj -out 1.1\product.msi

### Create the transform between your products

    torch.exe -p -xi 1.0\product.wixpdb 1.1\product.wixpdb -out patch\diff.wixmst

### Build the patch

The patch.wxs file is compiled and linked like a product, but then it is processed along with any number of transforms that you want the patch to contain. That produces an MSP file that targets any of the products from which transforms were created after filtering.

    candle.exe patch.wxs
    light.exe patch.wixobj -out patch\patch.wixmsp
    pyro.exe patch\patch.wixmsp -out patch\patch.msp -t RTM patch\diff.wixmst

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

On Windows XP Service Pack 2 and Windows Server 2003, go to &quot;Add/Remove Programs&quot; in the Control Panel and make sure that Show Updates is checked. On Windows Vista and newer, go to &quot;Programs&quot; then &quot;View installed updates&quot; in the Control Panel. Select &quot;Sample Patch&quot; from under &quot;WiX Patch Example Product&quot; and click the Uninstall button.

Go to &quot;Program files\Patch Sample Directory&quot; and open Sample.txt. Verify that this is again the 1.0 version. Close Sample.txt.

### Uninstall the product

On Windows XP Service Pack 2 and Windows Server 2003, go to &quot;Add/Remove Programs&quot; in the Control Panel. On Windows Vista and newer, go to &quot;Programs&quot; then &quot;Uninstall a program&quot; in the Control Panel. Select &quot;WiX Patch Example Product&quot; and click the Uninstall button.

## Restrictions

In addition to [restrictions](patch_restrictions.html) about what can be in a patch in order for it to install and uninstall correctly, the following restrictions ensure that your patch works correctly.

### Patch families can only grow

Patch families are used to filter resources that should end up in a patch. Once the patch is created, these patch families dictate which patches are superseded. If a resource is removed from a patch family in a newer patch and that resource is contained in an older patch with the same patch family, then when the older patch is superseded, that resource will be regressed back to its previous state before the older patch was installed.

Note that in order for one patch to supersede any other patches, all patch families must be superseded. A single patch family is referenced in the example above for simplicity.

### Certain elements cannot be added to uninstallable patches

There are certain elements referenced in [restrictions](patch_restrictions.html) that cannot be added or modified if the patch is to be uninstallable. If a Patch/@AllowRemoval is set to &quot;yes&quot; and any of these elements are added or modified, Pyro.exe will return an error. These elements compile into tables that Windows Installer restricts in patches, so WiX informs you and prevents you from creating a patch that is not uninstallable when you want it to be uninstallable.
