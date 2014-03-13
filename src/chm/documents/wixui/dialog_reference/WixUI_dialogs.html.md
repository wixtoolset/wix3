---
title: WixUI Dialogs
layout: documentation
after: wixui_mondo
---
# WixUI Dialogs

The following table describes each of the built-in dialogs that is defined in the WixUI dialog library.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Dialog Name</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>AdvancedWelcomeEulaDlg</b></td>
    <td>A dialog that displays the end user license agreement. Unlike the LicenseAgreementDlg, it has Advanced and Install buttons instead of Next and Back buttons. This dialog is used by the WixUI_Advanced dialog set to provide the user with a quick way to perform a default installation.</td>
  </tr>
  <tr>
    <td><b>BrowseDlg</b></td>
    <td>A dialog that allows the user to browse for a destination folder.</td>
  </tr>
  <tr>
    <td><b>CancelDlg</b></td>
    <td>A dialog that appears after the user clicks a Cancel button on any dialog and confirms whether or not the user really wants to cancel the installation.</td>
  </tr>
  <tr>
    <td><b>CustomizeDlg</b></td>
    <td>A dialog that displays a feature selection tree with a Browse button, Disk Usage button, and a text box that contains information about the currently selected feature.</td>
  </tr>
  <tr>
    <td><b>DiskCostDlg</b></td>
    <td>A dialog that allows the user to select which drive to install to and that displays disk space usage information for each drive.</td>
  </tr>
  <tr>
    <td><b>ErrorDlg</b></td>
    <td>A dialog that displays an error message to the user and can provide an option to retry the previous action.</td>
  </tr>
  <tr>
    <td><b>ExitDlg</b></td>
    <td>A dialog that displays a summary dialog after setup completes successfully. It can also optionally display a checkbox and custom text. For details about how to add a checkbox and custom text to this dialog, see <a href="~/wixui/WixUI_customizations.html">Customizing Built-in WixUI Dialog Sets</a> and <a href="~/howtos/ui_and_localization/run_program_after_install.html">How To: Run the Installed Application After Setup</a>.</td>
  </tr>
  <tr>
    <td><b>FatalError</b></td>
    <td>A dialog that displays a summary error dialog if setup fails.</td>
  </tr>
  <tr>
    <td><b>FeaturesDlg</b></td>
    <td>A dialog that displays a feature selection tree with a text box that contains information about the currently selected feature. Unlike the CustomizeDlg, it does not contain Browse or Disk Space buttons.</td>
  </tr>
  <tr>
    <td><b>FilesInUse</b></td>
    <td>A dialog that displays a list of applications that are holding files in use that need to be updated by the current installation process. It includes Retry, Ignore and Exit buttons.</td>
  </tr>
  <tr>
    <td><b>InstallDirDlg</b></td>
    <td>A dialog that has a text box that allows the user to type in a non-default installation path and a Browse button that allows the user to select a non-default installation folder. By default, the InstallDirDlg dialog validates that any path the user enters is valid for Windows Installer: That is, it's a path on a local hard drive, not a network path or on a removable drive. If you wish to disable path validation and allow invalid paths, set the public property WIXUI_DONTVALIDATEPATH to 1.</td>
  </tr>
  <tr>
    <td><b>InstallScopeDlg</b></td>
    <td>A dialog that allows the user to choose to install the product for all users or for the current user.</td>
  </tr>
  <tr>
    <td><b>InvalidDirDlg</b></td>
    <td>A dialog that displays an error if the user selects an invalid installation directory.</td>
  </tr>
  <tr>
    <td><b>LicenseAgreementDlg</b></td>
    <td>A dialog that displays the end user license agreement and includes Back and Next buttons. Unlike the AdvancedWelcomeEulaDlg, this dialog does not allow the user to start a default installation.</td>
  </tr>
  <tr>
    <td><b>MaintenanceTypeDlg</b></td>
    <td>A dialog that includes buttons that allow the user to change which features are installed, repair the product or remove the product. It only appears when the user runs setup after a product has been installed.</td>
  </tr>
  <tr>
    <td><b>MaintenanceWelcomeDlg</b></td>
    <td>An introductory dialog that appears when running setup after the product has been installed.</td>
  </tr>
  <tr>
    <td><b>MsiRMFilesInUse</b></td>
    <td>A dialog that is similar to the FilesInUse dialog, but that interacts with Restart Manager. It allows the user to attempt to automatically close applications or ignore the prompt and result in the setup requiring a reboot after it completes.</td>
  </tr>
  <tr>
    <td><b>OutOfDiskDlg</b></td>
    <td>A dialog that informs the user that they have insufficient disk space on the selected drive and advises them to free up additional disk space or reduce the number of features to be installed to the drive.</td>
  </tr>
  <tr>
    <td><b>OutOfRbDiskDlg</b></td>
    <td>A dialog that is similar to the OutOfDiskDlg, but also allows the user to disable Windows Installer rollback functionality in order to conserve disk space required by setup.</td>
  </tr>
  <tr>
    <td><b>PrepareDlg</b></td>
    <td>A simple progress dialog that appears during setup initialization before the first interactive dialog appears.</td>
  </tr>
  <tr>
    <td><b>ProgressDlg</b></td>
    <td>A dialog that appears during installation that displays a progress bar and messages about actions are being performed.</td>
  </tr>
  <tr>
    <td><b>ResumeDlg</b></td>
    <td>An introductory dialog that appears when resuming a suspended setup.</td>
  </tr>
  <tr>
    <td><b>SetupTypeDlg</b></td>
    <td>A dialog that allows the user to choose Typical, Custom or Complete installation configurations.</td>
  </tr>
  <tr>
    <td><b>UserExit</b></td>
    <td>A dialog that that is similar to the FatalError dialog. It displays a summary dialog if the user chooses to cancel setup.</td>
  </tr>
  <tr>
    <td><b>VerifyReadyDlg</b></td>
    <td>A dialog that appears immediately before starting installation. It asks the user for final confirmation before starting to make changes to the system.</td>
  </tr>
  <tr>
    <td><b>WaitForCostingDlg</b></td>
    <td>A dialog that appears if the user advances too far in the setup wizard before Windows Installer has finished calculating disk cost requirements.</td>
  </tr>
  <tr>
    <td><b>WelcomeDlg</b></td>
    <td>An introductory dialog that appears when running setup for a product that has not yet been installed.</td>
  </tr>
  <tr>
    <td><b>WelcomeEulaDlg</b></td>
    <td>A dialog that displays an end user license agreement and allows the user to start installation after accepting the agreement. It is only used by the WixUI_Minimal dialog set and is intended for simple setup programs that do not offer any user configurable options.</td>
  </tr>
</table>
