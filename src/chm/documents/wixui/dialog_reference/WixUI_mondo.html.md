---
title: WixUI_Mondo Dialog Set
layout: documentation
---
# WixUI_Mondo Dialog Set

WixUI\_Mondo includes a set dialogs that allow granular installation customization options. WixUI\_Mondo is appropriate when some product features are not installed by default and there is a meaningful difference between typical and complete installs.

<i>Note</i>: WixUI_Mondo uses <a href="http://msdn.microsoft.com/library/aa371688.aspx" target="_blank">SetInstallLevel</a> control events to set the install level when the user chooses Typical or Complete. For Typical, the install level is set to 3; for Complete, 1000. For details about feature levels and install levels, see <a href="http://msdn.microsoft.com/library/aa369536.aspx" target="_blank">INSTALLLEVEL Property</a>.

This dialog set is defined in the file <b>WixUI_Mondo.wxs</b> in the WixUIExtension in the WiX source code.

## WixUI_Mondo Dialogs

WixUI_Mondo includes the following dialogs:

* BrowseDlg
* CustomizeDlg
* DiskCostDlg
* LicenseAgreementDlg
* SetupTypeDlg
* WelcomeDlg

In addition, WixUI_Mondo includes the following common dialogs that appear in all WixUI dialog sets:

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
