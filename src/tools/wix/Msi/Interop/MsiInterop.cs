// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Msi.Interop
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A callback function that the installer calls for progress notification and error messages.
    /// </summary>
    /// <param name="context">Pointer to an application context.
    /// This parameter can be used for error checking.</param>
    /// <param name="messageType">Specifies a combination of one message box style,
    /// one message box icon type, one default button, and one installation message type.</param>
    /// <param name="message">Specifies the message text.</param>
    /// <returns>-1 for an error, 0 if no action was taken, 1 if OK, 3 to abort.</returns>
    internal delegate int InstallUIHandler(IntPtr context, uint messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);

    /// <summary>
    /// Class exposing static functions and structs from MSI API.
    /// </summary>
    internal sealed class MsiInterop
    {
        // Patching constants
        internal const int MsiMaxStreamNameLength = 62; // http://msdn2.microsoft.com/library/aa370551.aspx

        // Component.Attributes
        internal const int MsidbComponentAttributesLocalOnly = 0;
        internal const int MsidbComponentAttributesSourceOnly = 1;
        internal const int MsidbComponentAttributesOptional = 2;
        internal const int MsidbComponentAttributesRegistryKeyPath = 4;
        internal const int MsidbComponentAttributesSharedDllRefCount = 8;
        internal const int MsidbComponentAttributesPermanent = 16;
        internal const int MsidbComponentAttributesODBCDataSource = 32;
        internal const int MsidbComponentAttributesTransitive = 64;
        internal const int MsidbComponentAttributesNeverOverwrite = 128;
        internal const int MsidbComponentAttributes64bit = 256;
        internal const int MsidbComponentAttributesDisableRegistryReflection = 512;
        internal const int MsidbComponentAttributesUninstallOnSupersedence = 1024;
        internal const int MsidbComponentAttributesShared = 2048;

        // BBControl.Attributes & Control.Attributes
        internal const int MsidbControlAttributesVisible           = 0x00000001;
        internal const int MsidbControlAttributesEnabled           = 0x00000002;
        internal const int MsidbControlAttributesSunken            = 0x00000004;
        internal const int MsidbControlAttributesIndirect          = 0x00000008;
        internal const int MsidbControlAttributesInteger           = 0x00000010;
        internal const int MsidbControlAttributesRTLRO             = 0x00000020;
        internal const int MsidbControlAttributesRightAligned      = 0x00000040;
        internal const int MsidbControlAttributesLeftScroll        = 0x00000080;
        internal const int MsidbControlAttributesBiDi              = MsidbControlAttributesRTLRO | MsidbControlAttributesRightAligned | MsidbControlAttributesLeftScroll;

        // Text controls
        internal const int MsidbControlAttributesTransparent       = 0x00010000;
        internal const int MsidbControlAttributesNoPrefix          = 0x00020000;
        internal const int MsidbControlAttributesNoWrap            = 0x00040000;
        internal const int MsidbControlAttributesFormatSize        = 0x00080000;
        internal const int MsidbControlAttributesUsersLanguage     = 0x00100000;

        // Edit controls
        internal const int MsidbControlAttributesMultiline         = 0x00010000;
        internal const int MsidbControlAttributesPasswordInput     = 0x00200000;

        // ProgressBar controls
        internal const int MsidbControlAttributesProgress95        = 0x00010000;

        // VolumeSelectCombo and DirectoryCombo controls
        internal const int MsidbControlAttributesRemovableVolume   = 0x00010000;
        internal const int MsidbControlAttributesFixedVolume       = 0x00020000;
        internal const int MsidbControlAttributesRemoteVolume      = 0x00040000;
        internal const int MsidbControlAttributesCDROMVolume       = 0x00080000;
        internal const int MsidbControlAttributesRAMDiskVolume     = 0x00100000;
        internal const int MsidbControlAttributesFloppyVolume      = 0x00200000;

        // VolumeCostList controls
        internal const int MsidbControlShowRollbackCost            = 0x00400000;

        // ListBox and ComboBox controls
        internal const int MsidbControlAttributesSorted            = 0x00010000;
        internal const int MsidbControlAttributesComboList         = 0x00020000;

        // picture button controls
        internal const int MsidbControlAttributesImageHandle       = 0x00010000;
        internal const int MsidbControlAttributesPushLike          = 0x00020000;
        internal const int MsidbControlAttributesBitmap            = 0x00040000;
        internal const int MsidbControlAttributesIcon              = 0x00080000;
        internal const int MsidbControlAttributesFixedSize         = 0x00100000;
        internal const int MsidbControlAttributesIconSize16        = 0x00200000;
        internal const int MsidbControlAttributesIconSize32        = 0x00400000;
        internal const int MsidbControlAttributesIconSize48        = 0x00600000;
        internal const int MsidbControlAttributesElevationShield   = 0x00800000;

        // RadioButton controls
        internal const int MsidbControlAttributesHasBorder         = 0x01000000;

        // CustomAction.Type
        // executable types
        internal const int MsidbCustomActionTypeDll              = 0x00000001;  // Target = entry point name
        internal const int MsidbCustomActionTypeExe              = 0x00000002;  // Target = command line args
        internal const int MsidbCustomActionTypeTextData         = 0x00000003;  // Target = text string to be formatted and set into property
        internal const int MsidbCustomActionTypeJScript          = 0x00000005;  // Target = entry point name; null if none to call
        internal const int MsidbCustomActionTypeVBScript         = 0x00000006;  // Target = entry point name; null if none to call
        internal const int MsidbCustomActionTypeInstall          = 0x00000007;  // Target = property list for nested engine initialization
        internal const int MsidbCustomActionTypeSourceBits       = 0x00000030;
        internal const int MsidbCustomActionTypeTargetBits       = 0x00000007;
        internal const int MsidbCustomActionTypeReturnBits       = 0x000000C0;
        internal const int MsidbCustomActionTypeExecuteBits      = 0x00000700;

        // source of code
        internal const int MsidbCustomActionTypeBinaryData       = 0x00000000;  // Source = Binary.Name; data stored in stream
        internal const int MsidbCustomActionTypeSourceFile       = 0x00000010;  // Source = File.File; file part of installation
        internal const int MsidbCustomActionTypeDirectory        = 0x00000020;  // Source = Directory.Directory; folder containing existing file
        internal const int MsidbCustomActionTypeProperty         = 0x00000030;  // Source = Property.Property; full path to executable

        // return processing; default is syncronous execution; process return code
        internal const int MsidbCustomActionTypeContinue         = 0x00000040;  // ignore action return status; continue running
        internal const int MsidbCustomActionTypeAsync            = 0x00000080;  // run asynchronously

        // execution scheduling flags; default is execute whenever sequenced
        internal const int MsidbCustomActionTypeFirstSequence    = 0x00000100;  // skip if UI sequence already run
        internal const int MsidbCustomActionTypeOncePerProcess   = 0x00000200;  // skip if UI sequence already run in same process
        internal const int MsidbCustomActionTypeClientRepeat     = 0x00000300;  // run on client only if UI already run on client
        internal const int MsidbCustomActionTypeInScript         = 0x00000400;  // queue for execution within script
        internal const int MsidbCustomActionTypeRollback         = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        internal const int MsidbCustomActionTypeCommit           = 0x00000200;  // in conjunction with InScript: run Commit ops from script on success

        // security context flag; default to impersonate as user; valid only if InScript
        internal const int MsidbCustomActionTypeNoImpersonate    = 0x00000800;  // no impersonation; run in system context
        internal const int MsidbCustomActionTypeTSAware          = 0x00004000;  // impersonate for per-machine installs on TS machines
        internal const int MsidbCustomActionType64BitScript      = 0x00001000;  // script should run in 64bit process
        internal const int MsidbCustomActionTypeHideTarget       = 0x00002000;  // don't record the contents of the Target field in the log file.

        internal const int MsidbCustomActionTypePatchUninstall   = 0x00008000;  // run on patch uninstall

        // Dialog.Attributes
        internal const int MsidbDialogAttributesVisible          = 0x00000001;
        internal const int MsidbDialogAttributesModal            = 0x00000002;
        internal const int MsidbDialogAttributesMinimize         = 0x00000004;
        internal const int MsidbDialogAttributesSysModal         = 0x00000008;
        internal const int MsidbDialogAttributesKeepModeless     = 0x00000010;
        internal const int MsidbDialogAttributesTrackDiskSpace   = 0x00000020;
        internal const int MsidbDialogAttributesUseCustomPalette = 0x00000040;
        internal const int MsidbDialogAttributesRTLRO            = 0x00000080;
        internal const int MsidbDialogAttributesRightAligned     = 0x00000100;
        internal const int MsidbDialogAttributesLeftScroll       = 0x00000200;
        internal const int MsidbDialogAttributesBiDi             = MsidbDialogAttributesRTLRO | MsidbDialogAttributesRightAligned | MsidbDialogAttributesLeftScroll;
        internal const int MsidbDialogAttributesError            = 0x00010000;
        internal const int CommonControlAttributesInvert         = MsidbControlAttributesVisible + MsidbControlAttributesEnabled;
        internal const int DialogAttributesInvert                = MsidbDialogAttributesVisible + MsidbDialogAttributesModal + MsidbDialogAttributesMinimize;

        // Feature.Attributes
        internal const int MsidbFeatureAttributesFavorLocal = 0;
        internal const int MsidbFeatureAttributesFavorSource = 1;
        internal const int MsidbFeatureAttributesFollowParent = 2;
        internal const int MsidbFeatureAttributesFavorAdvertise = 4;
        internal const int MsidbFeatureAttributesDisallowAdvertise = 8;
        internal const int MsidbFeatureAttributesUIDisallowAbsent = 16;
        internal const int MsidbFeatureAttributesNoUnsupportedAdvertise = 32;

        // File.Attributes
        internal const int MsidbFileAttributesReadOnly = 1;
        internal const int MsidbFileAttributesHidden = 2;
        internal const int MsidbFileAttributesSystem = 4;
        internal const int MsidbFileAttributesVital = 512;
        internal const int MsidbFileAttributesChecksum = 1024;
        internal const int MsidbFileAttributesPatchAdded = 4096;
        internal const int MsidbFileAttributesNoncompressed = 8192;
        internal const int MsidbFileAttributesCompressed = 16384;

        // IniFile.Action & RemoveIniFile.Action
        internal const int MsidbIniFileActionAddLine    = 0;
        internal const int MsidbIniFileActionCreateLine = 1;
        internal const int MsidbIniFileActionRemoveLine = 2;
        internal const int MsidbIniFileActionAddTag     = 3;
        internal const int MsidbIniFileActionRemoveTag  = 4;

        // MoveFile.Options
        internal const int MsidbMoveFileOptionsMove = 1;

        // ServiceInstall.Attributes
        internal const int MsidbServiceInstallOwnProcess        = 0x00000010;
        internal const int MsidbServiceInstallShareProcess      = 0x00000020;
        internal const int MsidbServiceInstallInteractive       = 0x00000100;
        internal const int MsidbServiceInstallAutoStart         = 0x00000002;
        internal const int MsidbServiceInstallDemandStart       = 0x00000003;
        internal const int MsidbServiceInstallDisabled          = 0x00000004;
        internal const int MsidbServiceInstallErrorIgnore       = 0x00000000;
        internal const int MsidbServiceInstallErrorNormal       = 0x00000001;
        internal const int MsidbServiceInstallErrorCritical     = 0x00000003;
        internal const int MsidbServiceInstallErrorControlVital = 0x00008000;

        // ServiceConfig.Event
        internal const int MsidbServiceConfigEventInstall       = 0x00000001;
        internal const int MsidbServiceConfigEventUninstall     = 0x00000002;
        internal const int MsidbServiceConfigEventReinstall     = 0x00000004;

        // ServiceControl.Attributes
        internal const int MsidbServiceControlEventStart           = 0x00000001;
        internal const int MsidbServiceControlEventStop            = 0x00000002;
        internal const int MsidbServiceControlEventDelete          = 0x00000008;
        internal const int MsidbServiceControlEventUninstallStart  = 0x00000010;
        internal const int MsidbServiceControlEventUninstallStop   = 0x00000020;
        internal const int MsidbServiceControlEventUninstallDelete = 0x00000080;

        // TextStyle.StyleBits
        internal const int MsidbTextStyleStyleBitsBold      = 1;
        internal const int MsidbTextStyleStyleBitsItalic    = 2;
        internal const int MsidbTextStyleStyleBitsUnderline = 4;
        internal const int MsidbTextStyleStyleBitsStrike    = 8;

        // Upgrade.Attributes
        internal const int MsidbUpgradeAttributesMigrateFeatures     = 0x00000001;
        internal const int MsidbUpgradeAttributesOnlyDetect          = 0x00000002;
        internal const int MsidbUpgradeAttributesIgnoreRemoveFailure = 0x00000004;
        internal const int MsidbUpgradeAttributesVersionMinInclusive = 0x00000100;
        internal const int MsidbUpgradeAttributesVersionMaxInclusive = 0x00000200;
        internal const int MsidbUpgradeAttributesLanguagesExclusive  = 0x00000400;

        // Registry Hive Roots
        internal const int MsidbRegistryRootClassesRoot = 0;
        internal const int MsidbRegistryRootCurrentUser = 1;
        internal const int MsidbRegistryRootLocalMachine = 2;
        internal const int MsidbRegistryRootUsers = 3;

        // Locator Types
        internal const int MsidbLocatorTypeDirectory = 0;
        internal const int MsidbLocatorTypeFileName = 1;
        internal const int MsidbLocatorTypeRawValue = 2;
        internal const int MsidbLocatorType64bit = 16;

        internal const int MsidbClassAttributesRelativePath = 1;

        // RemoveFile.InstallMode
        internal const int MsidbRemoveFileInstallModeOnInstall = 0x00000001;
        internal const int MsidbRemoveFileInstallModeOnRemove  = 0x00000002;
        internal const int MsidbRemoveFileInstallModeOnBoth    = 0x00000003;

        // ODBCDataSource.Registration
        internal const int MsidbODBCDataSourceRegistrationPerMachine = 0;
        internal const int MsidbODBCDataSourceRegistrationPerUser    = 1;

        // ModuleConfiguration.Format
        internal const int MsidbModuleConfigurationFormatText = 0;
        internal const int MsidbModuleConfigurationFormatKey = 1;
        internal const int MsidbModuleConfigurationFormatInteger = 2;
        internal const int MsidbModuleConfigurationFormatBitfield = 3;

        // ModuleConfiguration.Attributes
        internal const int MsidbMsmConfigurableOptionKeyNoOrphan = 1;
        internal const int MsidbMsmConfigurableOptionNonNullable = 2;

        // ' Windows API function ShowWindow constants - used in Shortcut table
        internal const int SWSHOWNORMAL                         = 0x00000001;
        internal const int SWSHOWMAXIMIZED                      = 0x00000003;
        internal const int SWSHOWMINNOACTIVE                    = 0x00000007;

        // NameToBit arrays
        // UI elements
        internal static readonly string[] CommonControlAttributes = { "Hidden", "Disabled", "Sunken", "Indirect", "Integer", "RightToLeft", "RightAligned", "LeftScroll" };
        internal static readonly string[] TextControlAttributes = { "Transparent", "NoPrefix", "NoWrap", "FormatSize", "UserLanguage" };
        internal static readonly string[] HyperlinkControlAttributes = { "Transparent" };
        internal static readonly string[] EditControlAttributes = { "Multiline", null, null, null,    null, "Password" };
        internal static readonly string[] ProgressControlAttributes = { "ProgressBlocks" };
        internal static readonly string[] VolumeControlAttributes = { "Removable", "Fixed", "Remote", "CDROM", "RAMDisk", "Floppy", "ShowRollbackCost" };
        internal static readonly string[] ListboxControlAttributes = { "Sorted", null, null, null, "UserLanguage" };
        internal static readonly string[] ListviewControlAttributes = { "Sorted", null, null, null, "FixedSize", "Icon16", "Icon32" };
        internal static readonly string[] ComboboxControlAttributes = { "Sorted", "ComboList", null, null, "UserLanguage" };
        internal static readonly string[] RadioControlAttributes = { "Image", "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", null, "HasBorder" };
        internal static readonly string[] ButtonControlAttributes = { "Image", null, "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", "ElevationShield" };
        internal static readonly string[] IconControlAttributes = { "Image", null, null, null, "FixedSize", "Icon16", "Icon32" };
        internal static readonly string[] BitmapControlAttributes = { "Image", null, null, null, "FixedSize" };
        internal static readonly string[] CheckboxControlAttributes = { null, "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32" };

        internal const int MsidbEmbeddedUI                      = 0x01;
        internal const int MsidbEmbeddedHandlesBasic            = 0x02;

        internal const int INSTALLLOGMODE_FATALEXIT             = 0x00001;
        internal const int INSTALLLOGMODE_ERROR                 = 0x00002;
        internal const int INSTALLLOGMODE_WARNING               = 0x00004;
        internal const int INSTALLLOGMODE_USER                  = 0x00008;
        internal const int INSTALLLOGMODE_INFO                  = 0x00010;
        internal const int INSTALLLOGMODE_FILESINUSE            = 0x00020;
        internal const int INSTALLLOGMODE_RESOLVESOURCE         = 0x00040;
        internal const int INSTALLLOGMODE_OUTOFDISKSPACE        = 0x00080;
        internal const int INSTALLLOGMODE_ACTIONSTART           = 0x00100;
        internal const int INSTALLLOGMODE_ACTIONDATA            = 0x00200;
        internal const int INSTALLLOGMODE_PROGRESS              = 0x00400;
        internal const int INSTALLLOGMODE_COMMONDATA            = 0x00800;
        internal const int INSTALLLOGMODE_INITIALIZE            = 0x01000;
        internal const int INSTALLLOGMODE_TERMINATE             = 0x02000;
        internal const int INSTALLLOGMODE_SHOWDIALOG            = 0x04000;
        internal const int INSTALLLOGMODE_RMFILESINUSE       = 0x02000000;
        internal const int INSTALLLOGMODE_INSTALLSTART       = 0x04000000;
        internal const int INSTALLLOGMODE_INSTALLEND         = 0x08000000;

        internal const int MSICONDITIONFALSE = 0;   // The table is temporary.
        internal const int MSICONDITIONTRUE = 1;   // The table is persistent.
        internal const int MSICONDITIONNONE = 2;   // The table is unknown.
        internal const int MSICONDITIONERROR = 3;   // An invalid handle or invalid parameter was passed to the function.

        internal const int MSIDBOPENREADONLY = 0;
        internal const int MSIDBOPENTRANSACT = 1;
        internal const int MSIDBOPENDIRECT = 2;
        internal const int MSIDBOPENCREATE = 3;
        internal const int MSIDBOPENCREATEDIRECT = 4;
        internal const int MSIDBOPENPATCHFILE = 32;

        internal const int MSIMODIFYSEEK = -1;   // Refreshes the information in the supplied record without changing the position in the result set and without affecting subsequent fetch operations. The record may then be used for subsequent Update, Delete, and Refresh. All primary key columns of the table must be in the query and the record must have at least as many fields as the query. Seek cannot be used with multi-table queries. This mode cannot be used with a view containing joins. See also the remarks.
        internal const int MSIMODIFYREFRESH = 0;   // Refreshes the information in the record. Must first call MsiViewFetch with the same record. Fails for a deleted row. Works with read-write and read-only records.
        internal const int MSIMODIFYINSERT = 1;   // Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYUPDATE = 2;   // Updates an existing record. Nonprimary keys only. Must first call MsiViewFetch. Fails with a deleted record. Works only with read-write records.
        internal const int MSIMODIFYASSIGN = 3;   // Writes current data in the cursor to a table row. Updates record if the primary keys match an existing row and inserts if they do not match. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYREPLACE = 4;   // Updates or deletes and inserts a record into a table. Must first call MsiViewFetch with the same record. Updates record if the primary keys are unchanged. Deletes old row and inserts new if primary keys have changed. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYMERGE = 5;   // Inserts or validates a record in a table. Inserts if primary keys do not match any row and validates if there is a match. Fails if the record does not match the data in the table. Fails if there is a record with a duplicate key that is not identical. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYDELETE = 6;   // Remove a row from the table. You must first call the MsiViewFetch function with the same record. Fails if the row has been deleted. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYINSERTTEMPORARY = 7;   // Inserts a temporary record. The information is not persistent. Fails if a row with the same primary key exists. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATE = 8;   // Validates a record. Does not validate across joins. You must first call the MsiViewFetch function with the same record. Obtain validation errors with MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATENEW = 9;   // Validate a new record. Does not validate across joins. Checks for duplicate keys. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATEFIELD = 10;   // Validates fields of a fetched or new record. Can validate one or more fields of an incomplete record. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATEDELETE = 11;   // Validates a record that will be deleted later. You must first call MsiViewFetch. Fails if another row refers to the primary keys of this row. Validation does not check for the existence of the primary keys of this row in properties or strings. Does not check if a column is a foreign key to multiple tables. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.

        internal const uint VTI2 = 2;
        internal const uint VTI4 = 3;
        internal const uint VTLPWSTR = 30;
        internal const uint VTFILETIME = 64;

        internal const int MSICOLINFONAMES = 0;  // return column names
        internal const int MSICOLINFOTYPES = 1;  // return column definitions, datatype code followed by width

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private MsiInterop()
        {
        }

        /// <summary>
        /// PInvoke of MsiCloseHandle.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCloseHandle", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiCloseHandle(uint database);

        /// <summary>
        /// PInvoke of MsiCreateRecord
        /// </summary>
        /// <param name="parameters">Count of columns in the record.</param>
        /// <returns>Handle referencing the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCreateRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern uint MsiCreateRecord(int parameters);

        /// <summary>
        /// Creates summary information of an existing transform to include validation and error conditions.
        /// </summary>
        /// <param name="database">The handle to the database that contains the new database summary information.</param>
        /// <param name="referenceDatabase">The handle to the database that contains the original summary information.</param>
        /// <param name="transformFile">The name of the transform to which the summary information is added.</param>
        /// <param name="errorConditions">The error conditions that should be suppressed when the transform is applied.</param>
        /// <param name="validations">Specifies the properties to be validated to verify that the transform can be applied to the database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCreateTransformSummaryInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiCreateTransformSummaryInfo(uint database, uint referenceDatabase, string transformFile, TransformErrorConditions errorConditions, TransformValidations validations);

        /// <summary>
        /// Applies a transform to a database.
        /// </summary>
        /// <param name="database">Handle to the database obtained from MsiOpenDatabase to transform.</param>
        /// <param name="transformFile">Specifies the name of the transform file to apply.</param>
        /// <param name="errorConditions">Error conditions that should be suppressed.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseApplyTransformW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseApplyTransform(uint database, string transformFile, TransformErrorConditions errorConditions);

        /// <summary>
        /// PInvoke of MsiDatabaseCommit.
        /// </summary>
        /// <param name="database">Handle to a databse.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseCommit", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseCommit(uint database);

        /// <summary>
        /// PInvoke of MsiDatabaseExportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseExportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseExport(uint database, string tableName, string folderPath, string fileName);

        /// <summary>
        /// Generates a transform file of differences between two databases.
        /// </summary>
        /// <param name="database">Handle to the database obtained from MsiOpenDatabase that includes the changes.</param>
        /// <param name="databaseReference">Handle to the database obtained from MsiOpenDatabase that does not include the changes.</param>
        /// <param name="transformFile">A null-terminated string that specifies the name of the transform file being generated.
        /// This parameter can be null. If szTransformFile is null, you can use MsiDatabaseGenerateTransform to test whether two
        /// databases are identical without creating a transform. If the databases are identical, the function returns ERROR_NO_DATA.
        /// If the databases are different the function returns NOERROR.</param>
        /// <param name="reserved1">This is a reserved argument and must be set to 0.</param>
        /// <param name="reserved2">This is a reserved argument and must be set to 0.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseGenerateTransformW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseGenerateTransform(uint database, uint databaseReference, string transformFile, int reserved1, int reserved2);

        /// <summary>
        /// PInvoke of MsiDatabaseImportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseImportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseImport(uint database, string folderPath, string fileName);

        /// <summary>
        /// PInvoke of MsiDatabaseMergeW.
        /// </summary>
        /// <param name="database">The handle to the database obtained from MsiOpenDatabase.</param>
        /// <param name="databaseMerge">The handle to the database obtained from MsiOpenDatabase to merge into the base database.</param>
        /// <param name="tableName">The name of the table to receive merge conflict information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseMergeW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseMerge(uint database, uint databaseMerge, string tableName);

        /// <summary>
        /// PInvoke of MsiDatabaseOpenViewW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="query">SQL query.</param>
        /// <param name="view">View handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseOpenViewW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseOpenView(uint database, string query, out uint view);

        /// <summary>
        /// PInvoke of MsiGetFileHashW.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="options">Hash options (must be 0).</param>
        /// <param name="hash">Buffer to recieve hash.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetFileHashW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetFileHash(string filePath, uint options, MSIFILEHASHINFO hash);

        /// <summary>
        /// PInvoke of MsiGetFileVersionW.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="versionBuf">Buffer to receive version info.</param>
        /// <param name="versionBufSize">Size of version buffer.</param>
        /// <param name="langBuf">Buffer to recieve lang info.</param>
        /// <param name="langBufSize">Size of lang buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetFileVersionW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetFileVersion(string filePath, StringBuilder versionBuf, ref int versionBufSize, StringBuilder langBuf, ref int langBufSize);

        /// <summary>
        /// PInvoke of MsiGetLastErrorRecord.
        /// </summary>
        /// <returns>Handle to error record if one exists.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetLastErrorRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern uint MsiGetLastErrorRecord();

        /// <summary>
        /// PInvoke of MsiDatabaseGetPrimaryKeysW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="record">Handle to receive resulting record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseGetPrimaryKeysW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseGetPrimaryKeys(uint database, string tableName, out uint record);

        /// <summary>
        /// PInvoke of MsiDoActionW.
        /// </summary>
        /// <param name="product">Handle to the installation provided to a DLL custom action or
        /// obtained through MsiOpenPackage, MsiOpenPackageEx, or MsiOpenProduct.</param>
        /// <param name="action">Specifies the action to execute.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDoActionW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDoAction(uint product, string action);

        /// <summary>
        /// PInvoke of MsiGetSummaryInformationW.  Can use either database handle or database path as input.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="databasePath">Path to a database.</param>
        /// <param name="updateCount">Max number of updated values.</param>
        /// <param name="summaryInfo">Handle to summary information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetSummaryInformation(uint database, string databasePath, uint updateCount, ref uint summaryInfo);

        /// <summary>
        /// PInvoke of MsiDatabaseIsTablePersitentW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>MSICONDITION</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseIsTablePersistentW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseIsTablePersistent(uint database, string tableName);

        /// <summary>
        /// PInvoke of MsiOpenDatabaseW.
        /// </summary>
        /// <param name="databasePath">Path to database.</param>
        /// <param name="persist">Persist mode.</param>
        /// <param name="database">Handle to database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenDatabaseW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiOpenDatabase(string databasePath, IntPtr persist, out uint database);

        /// <summary>
        /// PInvoke of MsiOpenPackageW.
        /// </summary>
        /// <param name="packagePath">The path to the package.</param>
        /// <param name="product">A pointer to a variable that receives the product handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenPackageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiOpenPackage(string packagePath, out uint product);

        /// <summary>
        /// PInvoke of MsiRecordIsNull.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to check for null value.</param>
        /// <returns>true if the field is null, false if not, and an error code for any error.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordIsNull", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordIsNull(uint record, int field);

        /// <summary>
        /// PInvoke of MsiRecordGetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to retrieve integer from.</param>
        /// <returns>Integer value.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetInteger(uint record, int field);

        /// <summary>
        /// PInvoke of MsiRectordSetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set integer value in.</param>
        /// <param name="value">Value to set field to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetInteger(uint record, int field, int value);

        /// <summary>
        /// PInvoke of MsiRecordGetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to get string value from.</param>
        /// <param name="valueBuf">Buffer to recieve value.</param>
        /// <param name="valueBufSize">Size of buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetString(uint record, int field, StringBuilder valueBuf, ref int valueBufSize);

        /// <summary>
        /// PInvoke of MsiRecordSetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set string value in.</param>
        /// <param name="value">String value.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetString(uint record, int field, string value);

        /// <summary>
        /// PInvoke of MsiRecordSetStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set stream value in.</param>
        /// <param name="filePath">Path to file to set stream value to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStreamW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetStream(uint record, int field, string filePath);

        /// <summary>
        /// PInvoke of MsiRecordReadStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to read stream from.</param>
        /// <param name="dataBuf">Data buffer to recieve stream value.</param>
        /// <param name="dataBufSize">Size of data buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordReadStream", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordReadStream(uint record, int field, byte[] dataBuf, ref int dataBufSize);

        /// <summary>
        /// PInvoke of MsiRecordGetFieldCount.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <returns>Count of fields in the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetFieldCount", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetFieldCount(uint record);

        /// <summary>
        /// PInvoke of MsiSetExternalUIW.
        /// </summary>
        /// <param name="installUIHandler">Specifies a callback function that conforms to the INSTALLUI_HANDLER specification.</param>
        /// <param name="installLogMode">Specifies which messages to handle using the external message handler. If the external
        /// handler returns a non-zero result, then that message will not be sent to the UI, instead the message will be logged
        /// if logging has been enabled.</param>
        /// <param name="context">Pointer to an application context that is passed to the callback function.
        /// This parameter can be used for error checking.</param>
        /// <returns>The return value is the previously set external handler, or zero (0) if there was no previously set handler.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSetExternalUIW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern InstallUIHandler MsiSetExternalUI(InstallUIHandler installUIHandler, int installLogMode, IntPtr context);

        /// <summary>
        /// PInvoke of MsiSetInternalUI.
        /// </summary>
        /// <param name="uiLevel">Specifies the level of complexity of the user interface.</param>
        /// <param name="hwnd">Pointer to a window. This window becomes the owner of any user interface created.
        /// A pointer to the previous owner of the user interface is returned.
        /// If this parameter is null, the owner of the user interface does not change.</param>
        /// <returns>The previous user interface level is returned. If an invalid dwUILevel is passed, then INSTALLUILEVEL_NOCHANGE is returned.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSetInternalUI", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiSetInternalUI(int uiLevel, ref IntPtr hwnd);

        /// <summary>
        /// PInvoke of MsiSummaryInfoGetPropertyW.
        /// </summary>
        /// <param name="summaryInfo">Handle to summary info.</param>
        /// <param name="property">Property to get value from.</param>
        /// <param name="dataType">Data type of property.</param>
        /// <param name="integerValue">Integer to receive integer value.</param>
        /// <param name="fileTimeValue">File time to receive file time value.</param>
        /// <param name="stringValueBuf">String buffer to receive string value.</param>
        /// <param name="stringValueBufSize">Size of string buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSummaryInfoGetPropertyW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiSummaryInfoGetProperty(uint summaryInfo, int property, out uint dataType, out int integerValue, ref FILETIME fileTimeValue, StringBuilder stringValueBuf, ref int stringValueBufSize);

        /// <summary>
        /// PInvoke of MsiViewGetColumnInfo.
        /// </summary>
        /// <param name="view">Handle to view.</param>
        /// <param name="columnInfo">Column info.</param>
        /// <param name="record">Handle for returned record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewGetColumnInfo", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewGetColumnInfo(uint view, int columnInfo, out uint record);

        /// <summary>
        /// PInvoke of MsiViewExecute.
        /// </summary>
        /// <param name="view">Handle of view to execute.</param>
        /// <param name="record">Handle to a record that supplies the parameters for the view.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewExecute", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewExecute(uint view, uint record);

        /// <summary>
        /// PInvoke of MsiViewFetch.
        /// </summary>
        /// <param name="view">Handle of view to fetch a row from.</param>
        /// <param name="record">Handle to receive record info.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewFetch", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewFetch(uint view, out uint record);

        /// <summary>
        /// PInvoke of MsiViewModify.
        /// </summary>
        /// <param name="view">Handle of view to modify.</param>
        /// <param name="modifyMode">Modify mode.</param>
        /// <param name="record">Handle of record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewModify", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewModify(uint view, int modifyMode, uint record);

        /// <summary>
        /// contains the file hash information returned by MsiGetFileHash and used in the MsiFileHash table.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal class MSIFILEHASHINFO
        {
            [FieldOffset(0)] internal uint FileHashInfoSize;
            [FieldOffset(4)] internal int Data0;
            [FieldOffset(8)] internal int Data1;
            [FieldOffset(12)]internal int Data2;
            [FieldOffset(16)]internal int Data3;
        }
    }
}
