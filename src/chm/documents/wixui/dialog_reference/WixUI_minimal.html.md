---
title: WixUI_Minimal Dialog Set
layout: documentation
---
# WixUI_Minimal Dialog Set

WixUI\_Minimal is the simplest of the built-in WixUI dialog sets. Its sole dialog combines the welcome and license agreement dialogs and omits the feature customization dialog. WixUI\_Minimal is appropriate when your product has no optional features and does not support changing the installation directory.

This dialog set is defined in the file `WixUI_Minimal.wxs` in the WixUIExtension in the WiX source code.

## WixUI_Minimal Dialogs

WixUI_Minimal includes the following dialog:


* WelcomeEulaDlg


In addition, WixUI_Minimal includes the following common dialogs that appear in all WixUI dialog sets:


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
