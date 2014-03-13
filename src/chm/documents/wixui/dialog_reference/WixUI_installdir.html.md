---
title: WixUI_InstallDir Dialog Set
layout: documentation
---

# WixUI_InstallDir Dialog Set

WixUI_InstallDir does not allow the user to choose what features to install, but it adds a dialog to let the user choose a directory where the product will be installed.

This dialog set is defined in the file <b>WixUI_InstallDir.wxs</b> in the WixUIExtension in the WiX source code.

## Using WixUI_InstallDir

To use WixUI\_InstallDir, you must set a property named WIXUI\_INSTALLDIR with a value of the ID of the directory you want the user to be able to specify the location of. The directory ID must be all uppercase characters because it must be passed from the UI to the execute sequence to take effect. For example:

<blockquote><pre>
&lt;Directory Id="TARGETDIR" Name="SourceDir"&gt;
  &lt;Directory Id="ProgramFilesFolder" Name="PFiles"&gt;
    &lt;Directory Id="<b>TESTFILEPRODUCTDIR</b>" Name="Test File"&gt;
      ...
    &lt;/Directory&gt;
   &lt;/Directory&gt;
&lt;/Directory&gt;
...
&lt;Property Id="WIXUI_INSTALLDIR" Value="<b>TESTFILEPRODUCTDIR</b>" /&gt;
&lt;UIRef Id="WixUI_InstallDir" /&gt;
</pre></blockquote>

## WixUI_InstallDir Dialogs

WixUI_InstallDir includes the following dialogs:

* BrowseDlg
* DiskCostDlg
* InstallDirDlg
* InvalidDirDlg
* LicenseAgreementDlg
* WelcomeDlg

In addition, WixUI_InstallDir includes the following common dialogs that appear in all WixUI dialog sets:

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
