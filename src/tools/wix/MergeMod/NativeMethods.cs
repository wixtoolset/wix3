//-------------------------------------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Merge merge modules into an MSI file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.MergeMod
{
    using System;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Errors returned by merge operations.
    /// </summary>
    [Guid("0ADDA825-2C26-11D2-AD65-00A0C9AF11A6")]
    internal enum MsmErrorType
    {
        /// <summary>
        /// A request was made to open a module with a language not supported by the module.
        /// No more general language is supported by the module.
        /// Adds msmErrorLanguageUnsupported to the Type property and the requested language
        /// to the Language Property (Error Object).  All Error object properties are empty.
        /// The OpenModule function returns ERROR_INSTALL_LANGUAGE_UNSUPPORTED (as HRESULT).
        /// </summary>
        msmErrorLanguageUnsupported = 1,

        /// <summary>
        /// A request was made to open a module with a supported language but the module has
        /// an invalid language transform.  Adds msmErrorLanguageFailed to the Type property
        /// and the applied transform's language to the Language Property of the Error object.
        /// This may not be the requested language if a more general language was used.
        /// All other properties of the Error object are empty.  The OpenModule function
        /// returns ERROR_INSTALL_LANGUAGE_UNSUPPORTED (as HRESULT).
        /// </summary>
        msmErrorLanguageFailed = 2,

        /// <summary>
        /// The module cannot be merged because it excludes, or is excluded by, another module
        /// in the database.  Adds msmErrorExclusion to the Type property of the Error object.
        /// The ModuleKeys property or DatabaseKeys property contains the primary keys of the
        /// excluded module's row in the ModuleExclusion table.  If an existing module excludes
        /// the module being merged, the excluded module's ModuleSignature information is added
        /// to ModuleKeys.  If the module being merged excludes an existing module, DatabaseKeys
        /// contains the excluded module's ModuleSignature information.  All other properties
        /// are empty (or -1).
        /// </summary>
        msmErrorExclusion = 3,

        /// <summary>
        /// Merge conflict during merge.  The value of the Type property is set to
        /// msmErrorTableMerge.  The DatabaseTable property and DatabaseKeys property contain
        /// the table name and primary keys of the conflicting row in the database.  The
        /// ModuleTable property and ModuleKeys property contain the table name and primary keys
        /// of the conflicting row in the module.  The ModuleTable and ModuleKeys entries may be
        /// null if the row does not exist in the database.  For example, if the conflict is in a
        /// generated FeatureComponents table entry.  On Windows Installer version 2.0, when
        /// merging a configurable merge module, configuration may cause these properties to
        /// refer to rows that do not exist in the module.
        /// </summary>
        msmErrorTableMerge = 4,

        /// <summary>
        /// There was a problem resequencing a sequence table to contain the necessary merged
        /// actions.  The Type property is set to msmErrorResequenceMerge. The DatabaseTable
        /// and DatabaseKeys properties contain the sequence table name and primary keys
        /// (action name) of the conflicting row.  The ModuleTable and ModuleKeys properties
        /// contain the sequence table name and primary key (action name) of the conflicting row.
        /// On Windows Installer version 2.0, when merging a configurable merge module,
        /// configuration may cause these properties to refer to rows that do not exist in the module.
        /// </summary>
        msmErrorResequenceMerge = 5,

        /// <summary>
        /// Not used.
        /// </summary>
        msmErrorFileCreate = 6,

        /// <summary>
        /// There was a problem creating a directory to extract a file to disk.  The Path property
        /// contains the directory that could not be created.  All other properties are empty or -1.
        /// Not available with Windows Installer version 1.0.
        /// </summary>
        msmErrorDirCreate = 7,

        /// <summary>
        /// A feature name is required to complete the merge, but no feature name was provided.
        /// The Type property is set to msmErrorFeatureRequired.  The DatabaseTable and DatabaseKeys
        /// contain the table name and primary keys of the conflicting row.  The ModuleTable and
        /// ModuleKeys properties contain the table name and primary keys of the row cannot be merged.
        /// On Windows Installer version 2.0, when merging a configurable merge module, configuration
        /// may cause these properties to refer to rows that do not exist in the module.
        /// If the failure is in a generated FeatureComponents table, the DatabaseTable and
        /// DatabaseKeys properties are empty and the ModuleTable and ModuleKeys properties refer to
        /// the row in the Component table causing the failure.
        /// </summary>
        msmErrorFeatureRequired = 8,

        /// <summary>
        /// Available with Window Installer version 2.0. Substitution of a Null value into a
        /// non-nullable column.  This enters msmErrorBadNullSubstitution in the Type property and
        /// enters "ModuleSubstitution" and the keys from the ModuleSubstitution table for this row
        /// into the ModuleTable property and ModuleKeys property.  All other properties of the Error
        /// object are set to an empty string or -1.  This error causes the immediate failure of the
        /// merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadNullSubstitution = 9,

        /// <summary>
        /// Available with Window Installer version 2.0.  Substitution of Text Format Type or Integer
        /// Format Type into a Binary Type data column.  This type of error returns
        /// msmErrorBadSubstitutionType in the Type property and enters "ModuleSubstitution" and the
        /// keys from the ModuleSubstitution table for this row into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadSubstitutionType = 10,

        /// <summary>
        /// Available with Window Installer Version 2.0.  A row in the ModuleSubstitution table
        /// references a configuration item not defined in the ModuleConfiguration table.
        /// This type of error returns msmErrorMissingConfigItem in the Type property and enters
        /// "ModuleSubstitution" and the keys from the ModuleSubstitution table for this row into
        /// the ModuleTable property. All other properties of the Error object are set to an empty
        /// string or -1.  This error causes the immediate failure of the merge and the MergeEx
        /// function to return E_FAIL.
        /// </summary>
        msmErrorMissingConfigItem = 11,

        /// <summary>
        /// Available with Window Installer version 2.0.  The authoring tool has returned a Null
        /// value for an item marked with the msmConfigItemNonNullable attribute.  An error of this
        /// type returns msmErrorBadNullResponse in the Type property and enters "ModuleSubstitution"
        /// and the keys from the ModuleSubstitution table for for the item into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadNullResponse = 12,

        /// <summary>
        /// Available with Window Installer version 2.0.  The authoring tool returned a failure code
        /// (not S_OK or S_FALSE) when asked for data. An error of this type will return
        /// msmErrorDataRequestFailed in the Type property and enters "ModuleSubstitution"
        /// and the keys from the ModuleSubstitution table for the item into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorDataRequestFailed = 13,

        /// <summary>
        /// Available with Windows Installer 2.0 and later versions.  Indicates that an attempt was
        /// made to merge a 64-bit module into a package that was not a 64-bit package.  An error of
        /// this type returns msmErrorPlatformMismatch in the Type property.  All other properties of
        /// the error object are set to an empty string or -1. This error causes the immediate failure
        /// of the merge and causes the Merge function or MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorPlatformMismatch = 14,
    }

    /// <summary>
    /// IMsmMerge2 interface.
    /// </summary>
    [ComImport, Guid("351A72AB-21CB-47ab-B7AA-C4D7B02EA305")]
    internal interface IMsmMerge2
    {
        /// <summary>
        /// The OpenDatabase method of the Merge object opens a Windows Installer installation
        /// database, located at a specified path, that is to be merged with a module.
        /// </summary>
        /// <param name="path">Path to the database being opened.</param>
        void OpenDatabase(string path);

        /// <summary>
        /// The OpenModule method of the Merge object opens a Windows Installer merge module
        /// in read-only mode. A module must be opened before it can be merged with an installation database.
        /// </summary>
        /// <param name="fileName">Fully qualified file name pointing to a merge module.</param>
        /// <param name="language">A valid language identifier (LANGID).</param>
        void OpenModule(string fileName, short language);

        /// <summary>
        /// The CloseDatabase method of the Merge object closes the currently open Windows Installer database.
        /// </summary>
        /// <param name="commit">true if changes should be saved, false otherwise.</param>
        void CloseDatabase(bool commit);

        /// <summary>
        /// The CloseModule method of the Merge object closes the currently open Windows Installer merge module.
        /// </summary>
        void CloseModule();

        /// <summary>
        /// The OpenLog method of the Merge object opens a log file that receives progress and error messages.
        /// If the log file already exists, the installer appends new messages. If the log file does not exist,
        /// the installer creates a log file.
        /// </summary>
        /// <param name="fileName">Fully qualified filename pointing to a file to open or create.</param>
        void OpenLog(string fileName);

        /// <summary>
        /// The CloseLog method of the Merge object closes the current log file.
        /// </summary>
        void CloseLog();

        /// <summary>
        /// The Log method of the Merge object writes a text string to the currently open log file.
        /// </summary>
        /// <param name="message">The text string to display.</param>
        void Log(string message);

        /// <summary>
        /// Gets the errors from the last merge operation.
        /// </summary>
        /// <value>The errors from the last merge operation.</value>
        IMsmErrors Errors
        {
            get;
        }

        /// <summary>
        /// Gets a collection of Dependency objects that enumerates a set of unsatisfied dependencies for the current database.
        /// </summary>
        /// <value>A  collection of Dependency objects that enumerates a set of unsatisfied dependencies for the current database.</value>
        object Dependencies
        {
            get;
        }

        /// <summary>
        /// The Merge method of the Merge object executes a merge of the current database and current
        /// module. The merge attaches the components in the module to the feature identified by Feature.
        /// The root of the module's directory tree is redirected to the location given by RedirectDir.
        /// </summary>
        /// <param name="feature">The name of a feature in the database.</param>
        /// <param name="redirectDir">The key of an entry in the Directory table of the database.
        /// This parameter may be NULL or an empty string.</param>
        void Merge(string feature, string redirectDir);

        /// <summary>
        /// The Connect method of the Merge object connects a module to an additional feature.
        /// The module must have already been merged into the database or will be merged into the database.
        /// The feature must exist before calling this function.
        /// </summary>
        /// <param name="feature">The name of a feature already existing in the database.</param>
        void Connect(string feature);

        /// <summary>
        /// The ExtractCAB method of the Merge object extracts the embedded .cab file from a module and
        /// saves it as the specified file. The installer creates this file if it does not already exist
        /// and overwritten if it does exist.
        /// </summary>
        /// <param name="fileName">The fully qualified destination file.</param>
        void ExtractCAB(string fileName);

        /// <summary>
        /// The ExtractFiles method of the Merge object extracts the embedded .cab file from a module
        /// and then writes those files to the destination directory.
        /// </summary>
        /// <param name="path">The fully qualified destination directory.</param>
        void ExtractFiles(string path);

        /// <summary>
        /// The MergeEx method of the Merge object is equivalent to the Merge function, except that it
        /// takes an extra argument.  The Merge method executes a merge of the current database and
        /// current module. The merge attaches the components in the module to the feature identified
        /// by Feature. The root of the module's directory tree is redirected to the location given by RedirectDir.
        /// </summary>
        /// <param name="feature">The name of a feature in the database.</param>
        /// <param name="redirectDir">The key of an entry in the Directory table of the database. This parameter may
        /// be NULL or an empty string.</param>
        /// <param name="configuration">The pConfiguration argument is an interface implemented by the client. The argument may
        /// be NULL. The presence of this argument indicates that the client is capable of supporting the configuration
        /// functionality, but does not obligate the client to provide configuration data for any specific configurable item.</param>
        void MergeEx(string feature, string redirectDir, IMsmConfigureModule configuration);

        /// <summary>
        /// The ExtractFilesEx method of the Merge object extracts the embedded .cab file from a module and
        /// then writes those files to the destination directory.
        /// </summary>
        /// <param name="path">The fully qualified destination directory.</param>
        /// <param name="longFileNames">Set to specify using long file names for path segments and final file names.</param>
        /// <param name="filePaths">This is a list of fully-qualified paths for the files that were successfully extracted.
        /// The list is empty if no files can be extracted.  This argument may be null.  No list is provided if pFilePaths is null.</param>
        void ExtractFilesEx(string path, bool longFileNames, ref IntPtr filePaths);

        /// <summary>
        /// Gets a collection ConfigurableItem objects, each of which represents a single row from the ModuleConfiguration table.
        /// </summary>
        /// <value>A collection ConfigurableItem objects, each of which represents a single row from the ModuleConfiguration table.</value>
        /// <remarks>Semantically, each interface in the enumerator represents an item that can be configured by the module consumer.
        /// The collection is a read-only collection and implements the standard read-only collection interfaces of Item(), Count() and _NewEnum().
        /// The IEnumMsmConfigItems enumerator implements Next(), Skip(), Reset(), and Clone() with the standard semantics.</remarks>
        object ConfigurableItems
        {
            get;
        }

        /// <summary>
        /// The CreateSourceImage method of the Merge object allows the client to extract the files from a module to
        /// a source image on disk after a merge, taking into account changes to the module that might have been made
        /// during module configuration. The list of files to be extracted is taken from the file table of the module
        /// during the merge process. The list of files consists of every file successfully copied from the file table
        /// of the module to the target database. File table entries that were not copied due to primary key conflicts
        /// with existing rows in the database are not a part of this list. At image creation time, the directory for
        /// each of these files comes from the open (post-merge) database. The path specified in the Path parameter is
        /// the root of the source image for the install. fLongFileNames determines whether or not long file names are
        /// used for both path segments and final file names. The function fails if no database is open, no module is
        /// open, or no merge has been performed.
        /// </summary>
        /// <param name="path">The path of the root of the source image for the install.</param>
        /// <param name="longFileNames">Determines whether or not long file names are used for both path segments and final file names. </param>
        /// <param name="filePaths">This is a list of fully-qualified paths for the files that were successfully extracted.
        /// The list is empty if no files can be extracted.  This argument may be null.  No list is provided if pFilePaths is null.</param>
        void CreateSourceImage(string path, bool longFileNames, ref IntPtr filePaths);

        /// <summary>
        /// The get_ModuleFiles function implements the ModuleFiles property of the GetFiles object. This function
        /// returns the primary keys in the File table of the currently open module. The primary keys are returned
        /// as a collection of strings. The module must be opened by a call to the OpenModule function before calling get_ModuleFiles.
        /// </summary>
        IMsmStrings ModuleFiles
        {
            get;
        }
    }

    /// <summary>
    /// Collection of merge errors.
    /// </summary>
    [ComImport, Guid("0ADDA82A-2C26-11D2-AD65-00A0C9AF11A6")]
    internal interface IMsmErrors
    {
        /// <summary>
        /// Gets the IMsmError at the specified index.
        /// </summary>
        /// <param name="index">The one-based index of the IMsmError to get.</param>
        IMsmError this[int index]
        {
            get;
        }

        /// <summary>
        /// Gets the count of IMsmErrors in this collection.
        /// </summary>
        /// <value>The count of IMsmErrors in this collection.</value>
        int Count
        {
            get;
        }
    }

    /// <summary>
    /// A merge error.
    /// </summary>
    [ComImport, Guid("0ADDA828-2C26-11D2-AD65-00A0C9AF11A6")]
    internal interface IMsmError
    {
        /// <summary>
        /// Gets the type of merge error.
        /// </summary>
        /// <value>The type of merge error.</value>
        MsmErrorType Type
        {
            get;
        }

        /// <summary>
        /// Gets the path information from the merge error.
        /// </summary>
        /// <value>The path information from the merge error.</value>
        string Path
        {
            get;
        }

        /// <summary>
        /// Gets the language information from the merge error.
        /// </summary>
        /// <value>The language information from the merge error.</value>
        short Language
        {
            get;
        }

        /// <summary>
        /// Gets the database table from the merge error.
        /// </summary>
        /// <value>The database table from the merge error.</value>
        string DatabaseTable
        {
            get;
        }

        /// <summary>
        /// Gets the collection of database keys from the merge error.
        /// </summary>
        /// <value>The collection of database keys from the merge error.</value>
        IMsmStrings DatabaseKeys
        {
            get;
        }

        /// <summary>
        /// Gets the module table from the merge error.
        /// </summary>
        /// <value>The module table from the merge error.</value>
        string ModuleTable
        {
            get;
        }

        /// <summary>
        /// Gets the collection of module keys from the merge error.
        /// </summary>
        /// <value>The collection of module keys from the merge error.</value>
        IMsmStrings ModuleKeys
        {
            get;
        }
    }

    /// <summary>
    /// A collection of strings.
    /// </summary>
    [ComImport, Guid("0ADDA827-2C26-11D2-AD65-00A0C9AF11A6")]
    internal interface IMsmStrings
    {
        /// <summary>
        /// Gets the string at the specified index.
        /// </summary>
        /// <param name="index">The one-based index of the string to get.</param>
        string this[int index]
        {
            get;
        }

        /// <summary>
        /// Gets the count of strings in this collection.
        /// </summary>
        /// <value>The count of strings in this collection.</value>
        int Count
        {
            get;
        }
    }

    /// <summary>
    /// Callback for configurable merge modules.
    /// </summary>
    [ComImport, Guid("AC013209-18A7-4851-8A21-2353443D70A0"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IMsmConfigureModule
    {
        /// <summary>
        /// Callback to retrieve text data for configurable merge modules.
        /// </summary>
        /// <param name="name">Name of the data to be retrieved.</param>
        /// <param name="configData">The data corresponding to the name.</param>
        /// <returns>The error code (HRESULT).</returns>
        [PreserveSig]
        int ProvideTextData([In, MarshalAs(UnmanagedType.BStr)] string name, [MarshalAs(UnmanagedType.BStr)] out string configData);

        /// <summary>
        /// Callback to retrieve integer data for configurable merge modules.
        /// </summary>
        /// <param name="name">Name of the data to be retrieved.</param>
        /// <param name="configData">The data corresponding to the name.</param>
        /// <returns>The error code (HRESULT).</returns>
        [PreserveSig]
        int ProvideIntegerData([In, MarshalAs(UnmanagedType.BStr)] string name, out int configData);
    }

    /// <summary>
    /// Merge merge modules into an MSI file.
    /// </summary>
    [ComImport, Guid("F94985D5-29F9-4743-9805-99BC3F35B678")]
    internal class MsmMerge2
    {
    }

    /// <summary>
    /// Defines the standard COM IClassFactory interface.
    /// </summary>
    [ComImport, Guid("00000001-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IClassFactory
    {
        [return:MarshalAs(UnmanagedType.IUnknown)]
        object CreateInstance(IntPtr unkOuter, [MarshalAs(UnmanagedType.LPStruct)] Guid iid);
    }

    /// <summary>
    /// Contains native methods for merge operations.
    /// </summary>
    internal class NativeMethods
    {
        [DllImport("mergemod.dll", EntryPoint="DllGetClassObject", PreserveSig=false)]
        [return: MarshalAs(UnmanagedType.IUnknown)]
        private static extern object MergeModGetClassObject([MarshalAs(UnmanagedType.LPStruct)] Guid clsid, [MarshalAs(UnmanagedType.LPStruct)] Guid iid);

        /// <summary>
        /// Load the merge object directly from a local mergemod.dll without going through COM registration.
        /// </summary>
        /// <returns>Merge interface.</returns>
        internal static IMsmMerge2 GetMsmMerge()
        {
            IClassFactory classFactory = (IClassFactory) MergeModGetClassObject(typeof(MsmMerge2).GUID, typeof(IClassFactory).GUID);
            return (IMsmMerge2) classFactory.CreateInstance(IntPtr.Zero, typeof(IMsmMerge2).GUID);
        }
    }
}
