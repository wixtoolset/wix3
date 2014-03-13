---
title: WixUI_Advanced Dialog Set
layout: documentation
---
# WixUI_Advanced Dialog Set

The WixUI\_Advanced dialog set provides the option of a one-click install like WixUI\_Minimal, but it also allows directory and feature selection like other dialog sets if the user chooses to configure advanced options.

This dialog set is defined in the file <b>WixUI_Advanced.wxs</b> in the WixUIExtension in the WiX source code.

## Using WixUI_Advanced

To use WixUI_Advanced, you must include the following information in your setup authoring:

1. A directory with an Id named <b>APPLICATIONFOLDER</b>. This directory will be the default installation location for the product. For example:

        <Directory Id="TARGETDIR" Name="SourceDir">
          <Directory Id="ProgramFilesFolder" Name="PFiles">
            <Directory Id="APPLICATIONFOLDER" Name="My Application Folder">
              ...
            </Directory>
          </Directory>
        </Directory>
  
1. A property with an Id named <b>ApplicationFolderName</b> and a value set to a string that represents the default folder name. This property is used to form the default installation location.

    For a per-machine installation, the default installation location will be [ProgramFilesFolder][ApplicationFolderName] and the user will be able to change it in the setup UI. For a per-user installation, the default installation location will be [LocalAppDataFolder]Apps\[ApplicationFolderName] and the user will not be able to change it in the setup UI.

    For example:

        <Property Id="ApplicationFolderName" Value="My Application Folder" />

1. A property with an Id named <b>WixAppFolder</b> and a value set to <b>WixPerMachineFolder</b> or <b>WixPerUserFolder</b>. This property sets the default selected value of the radio button on the install scope dialog in the setup UI where the user can choose whether to install the product per-machine or per-user. For example:

        <Property Id="WixAppFolder" Value="WixPerMachineFolder" />

It is possible to suppress the install scope dialog in the WixUI_Advanced dialog set so the user will not be able to choose a per-machine or per-user installation. To do this, you must set the <b>WixUISupportPerMachine</b> or <b>WixUISupportPerUser</b> WiX variables to 0. The default value for each of these variables is 1, and you should not set both of these values to 0 in the same .msi. For example, to remove the install scope dialog and support only a per-machine installation, you can set the following:

    <WixVariable Id="WixUISupportPerUser" Value="0" />

The install scope dialog will automatically set the <a href="http://msdn.microsoft.com/library/aa367559.aspx" target="_blank">ALLUSERS</a> property for the installation session based on the user&apos;s selection. If you suppress the install scope dialog by setting either of these WiX variable values, you must manually set the ALLUSERS property to an appropriate value based on whether you want a per-machine or per-user installation.

## WixUI_Advanced Dialogs

WixUI_Advanced includes the following dialogs:

* AdvancedWelcomeEulaDlg
* BrowseDlg
* DiskCostDlg
* FeaturesDlg
* InstallDirDlg
* InstallScopeDlg
* InvalidDirDlg

In addition, WixUI_Advanced includes the following common dialogs that appear in all WixUI dialog sets:

* CancelDlg
* ErrorDlg
* ExitDlg
* FatalError
* FilesInUse
* MaintenanceTypeDlg
* MaintenanceWelcomeDlg
* MsiRMFilesInUse
* OutOfDiskDlg
* OutOfRbDiskDlg
* PrepareDlg
* ProgressDlg
* ResumeDlg
* UserExit
* VerifyReadyDlg
* WaitForCostingDlg

See [the WixUI dialog reference](WixUI_dialogs.html) for detailed descriptions of each of the above dialogs.
