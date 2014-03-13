//-------------------------------------------------------------------------------------------------
// <copyright file="Installer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Represents the Windows Installer, provides wrappers to
// create the top-level objects and access their methods.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Windows Installer message types.
    /// </summary>
    [Flags]
    internal enum InstallMessage
    {
        /// <summary>
        /// Premature termination, possibly fatal out of memory.
        /// </summary>
        FatalExit = 0x00000000,

        /// <summary>
        /// Formatted error message, [1] is message number in Error table.
        /// </summary>
        Error = 0x01000000,

        /// <summary>
        /// Formatted warning message, [1] is message number in Error table.
        /// </summary>
        Warning = 0x02000000,

        /// <summary>
        /// User request message, [1] is message number in Error table.
        /// </summary>
        User = 0x03000000,

        /// <summary>
        /// Informative message for log, not to be displayed.
        /// </summary>
        Info = 0x04000000,

        /// <summary>
        /// List of files in use that need to be replaced.
        /// </summary>
        FilesInUse = 0x05000000,

        /// <summary>
        /// Request to determine a valid source location.
        /// </summary>
        ResolveSource = 0x06000000,

        /// <summary>
        /// Insufficient disk space message.
        /// </summary>
        OutOfDiskSpace = 0x07000000,

        /// <summary>
        /// Progress: start of action, [1] action name, [2] description, [3] template for ACTIONDATA messages.
        /// </summary>
        ActionStart = 0x08000000,

        /// <summary>
        /// Action data. Record fields correspond to the template of ACTIONSTART message.
        /// </summary>
        ActionData = 0x09000000,

        /// <summary>
        /// Progress bar information. See the description of record fields below.
        /// </summary>
        Progress = 0x0A000000,

        /// <summary>
        /// To enable the Cancel button set [1] to 2 and [2] to 1. To disable the Cancel button set [1] to 2 and [2] to 0.
        /// </summary>
        CommonData = 0x0B000000,

        /// <summary>
        /// Sent prior to UI initialization, no string data.
        /// </summary>
        Initilize = 0x0C000000,

        /// <summary>
        /// Sent after UI termination, no string data.
        /// </summary>
        Terminate = 0x0D000000,

        /// <summary>
        /// Sent prior to display or authored dialog or wizard.
        /// </summary>
        ShowDialog = 0x0E000000
    }

    /// <summary>
    /// Windows Installer log modes.
    /// </summary>
    [Flags]
    internal enum InstallLogModes
    {
        /// <summary>
        /// Premature termination of installation.
        /// </summary>
        FatalExit = (1 << ((int)InstallMessage.FatalExit >> 24)),

        /// <summary>
        /// The error messages are logged.
        /// </summary>
        Error = (1 << ((int)InstallMessage.Error >> 24)),

        /// <summary>
        /// The warning messages are logged.
        /// </summary>
        Warning = (1 << ((int)InstallMessage.Warning >> 24)),

        /// <summary>
        /// The user requests are logged.
        /// </summary>
        User = (1 << ((int)InstallMessage.User >> 24)),

        /// <summary>
        /// The status messages that are not displayed are logged.
        /// </summary>
        Info = (1 << ((int)InstallMessage.Info >> 24)),

        /// <summary>
        /// Request to determine a valid source location.
        /// </summary>
        ResolveSource = (1 << ((int)InstallMessage.ResolveSource >> 24)),

        /// <summary>
        /// The was insufficient disk space.
        /// </summary>
        OutOfDiskSpace = (1 << ((int)InstallMessage.OutOfDiskSpace >> 24)),

        /// <summary>
        /// The start of new installation actions are logged.
        /// </summary>
        ActionStart = (1 << ((int)InstallMessage.ActionStart >> 24)),

        /// <summary>
        /// The data record with the installation action is logged.
        /// </summary>
        ActionData = (1 << ((int)InstallMessage.ActionData >> 24)),

        /// <summary>
        /// The parameters for user-interface initialization are logged.
        /// </summary>
        CommonData = (1 << ((int)InstallMessage.CommonData >> 24)),

        /// <summary>
        /// Logs the property values at termination.
        /// </summary>
        PropertyDump = (1 << ((int)InstallMessage.Progress >> 24)),

        /// <summary>
        /// Sends large amounts of information to a log file not generally useful to users.
        /// May be used for technical support.
        /// </summary>
        Verbose = (1 << ((int)InstallMessage.Initilize >> 24)),

        /// <summary>
        /// Sends extra debugging information, such as handle creation information, to the log file.
        /// </summary>
        ExtraDebug = (1 << ((int)InstallMessage.Terminate >> 24)),

        /// <summary>
        /// Progress bar information. This message includes information on units so far and total number of units.
        /// See MsiProcessMessage for an explanation of the message format.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Progress = (1 << ((int)InstallMessage.Progress >> 24)),

        /// <summary>
        /// If this is not a quiet installation, then the basic UI has been initialized.
        /// If this is a full UI installation, the full UI is not yet initialized.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Initialize = (1 << ((int)InstallMessage.Initilize >> 24)),

        /// <summary>
        /// If a full UI is being used, the full UI has ended.
        /// If this is not a quiet installation, the basic UI has not yet ended.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        Terminate = (1 << ((int)InstallMessage.Terminate >> 24)),

        /// <summary>
        /// Sent prior to display of the full UI dialog.
        /// This message is only sent to an external user interface and is not logged.
        /// </summary>
        ShowDialog = (1 << ((int)InstallMessage.ShowDialog >> 24)),

        /// <summary>
        /// Files in use information. When this message is received, a FilesInUse Dialog should be displayed.
        /// </summary>
        FilesInUse = (1 << ((int)InstallMessage.FilesInUse >> 24))
    }

    /// <summary>
    /// Windows Installer UI levels.
    /// </summary>
    [Flags]
    internal enum InstallUILevels
    {
        /// <summary>
        /// No change in the UI level. However, if phWnd is not Null, the parent window can change.
        /// </summary>
        NoChange = 0,

        /// <summary>
        /// The installer chooses an appropriate user interface level.
        /// </summary>
        Default = 1,

        /// <summary>
        /// Completely silent installation.
        /// </summary>
        None = 2,

        /// <summary>
        /// Simple progress and error handling.
        /// </summary>
        Basic = 3,

        /// <summary>
        /// Authored user interface with wizard dialog boxes suppressed.
        /// </summary>
        Reduced = 4,

        /// <summary>
        /// Authored user interface with wizards, progress, and errors.
        /// </summary>
        Full = 5,

        /// <summary>
        /// If combined with the Basic value, the installer shows simple progress dialog boxes but
        /// does not display a Cancel button on the dialog. This prevents users from canceling the install.
        /// Available with Windows Installer version 2.0.
        /// </summary>
        HideCancel = 0x20,

        /// <summary>
        /// If combined with the Basic value, the installer shows simple progress
        /// dialog boxes but does not display any modal dialog boxes or error dialog boxes.
        /// </summary>
        ProgressOnly = 0x40,

        /// <summary>
        /// If combined with any above value, the installer displays a modal dialog
        /// box at the end of a successful installation or if there has been an error.
        /// No dialog box is displayed if the user cancels.
        /// </summary>
        EndDialog = 0x80,

        /// <summary>
        /// If this value is combined with the None value, the installer displays only the dialog
        /// boxes used for source resolution. No other dialog boxes are shown. This value has no
        /// effect if the UI level is not INSTALLUILEVEL_NONE. It is used with an external user
        /// interface designed to handle all of the UI except for source resolution. In this case,
        /// the installer handles source resolution. This value is only available with Windows Installer 2.0 and later.
        /// </summary>
        SourceResOnly = 0x100
    }

    /// <summary>
    /// Represents the Windows Installer, provides wrappers to
    /// create the top-level objects and access their methods.
    /// </summary>
    internal sealed class Installer
    {
        private static TableDefinitionCollection tableDefinitions;
        private static WixActionRowCollection standardActions;

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private Installer()
        {
        }

        /// <summary>
        /// Takes the path to a file and returns a 128-bit hash of that file.
        /// </summary>
        /// <param name="filePath">Path to file that is to be hashed.</param>
        /// <param name="options">The value in this column must be 0. This parameter is reserved for future use.</param>
        /// <param name="hash">Int array that receives the returned file hash information.</param>
        internal static void GetFileHash(string filePath, int options, out int[] hash)
        {
            MsiInterop.MSIFILEHASHINFO hashInterop = new MsiInterop.MSIFILEHASHINFO();
            hashInterop.FileHashInfoSize = 20;

            int error = MsiInterop.MsiGetFileHash(filePath, Convert.ToUInt32(options), hashInterop);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            Debug.Assert(20 == hashInterop.FileHashInfoSize);

            hash = new int[4];
            hash[0] = hashInterop.Data0;
            hash[1] = hashInterop.Data1;
            hash[2] = hashInterop.Data2;
            hash[3] = hashInterop.Data3;
        }

        /// <summary>
        /// Returns the version string and language string in the format that the installer 
        /// expects to find them in the database.  If you just want version information, set 
        /// lpLangBuf and pcchLangBuf to zero. If you just want language information, set 
        /// lpVersionBuf and pcchVersionBuf to zero.
        /// </summary>
        /// <param name="filePath">Specifies the path to the file.</param>
        /// <param name="version">Returns the file version. Set to 0 for language information only.</param>
        /// <param name="language">Returns the file language. Set to 0 for version information only.</param>
        internal static void GetFileVersion(string filePath, out string version, out string language)
        {
            int versionLength = 20;
            int languageLength = 20;
            StringBuilder versionBuffer = new StringBuilder(versionLength);
            StringBuilder languageBuffer = new StringBuilder(languageLength);

            int error = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
            if (234 == error)
            {
                versionBuffer.EnsureCapacity(++versionLength);
                languageBuffer.EnsureCapacity(++languageLength);
                error = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
            }
            else if (1006 == error)
            {
                // file has no version or language, so no error
                error = 0;
            }

            if (0 != error)
            {
                throw new MsiException(error);
            }

            version = versionBuffer.ToString();
            language = languageBuffer.ToString();
        }

        /// <summary>
        /// Gets the table definitions stored in this assembly.
        /// </summary>
        /// <returns>Table definition collection for tables stored in this assembly.</returns>
        internal static TableDefinitionCollection GetTableDefinitions()
        {
            if (null == tableDefinitions)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                XmlReader tableDefinitionsReader = null;

                try
                {
                    tableDefinitionsReader = new XmlTextReader(assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Data.tables.xml"));

#if DEBUG
                    tableDefinitions = TableDefinitionCollection.Load(tableDefinitionsReader, false);
#else
                    tableDefinitions = TableDefinitionCollection.Load(tableDefinitionsReader, true);
#endif
                }
                finally
                {
                    if (null != tableDefinitionsReader)
                    {
                        tableDefinitionsReader.Close();
                    }
                }
            }

            return tableDefinitions.Clone();
        }

        /// <summary>
        /// Gets the standard actions stored in this assembly.
        /// </summary>
        /// <returns>Collection of standard actions in this assembly.</returns>
        internal static WixActionRowCollection GetStandardActions()
        {
            if (null == standardActions)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                XmlReader actionDefinitionsReader = null;

                try
                {
                    actionDefinitionsReader = new XmlTextReader(assembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Data.actions.xml"));
#if DEBUG
                    standardActions = WixActionRowCollection.Load(actionDefinitionsReader, false);
#else
                    standardActions = WixActionRowCollection.Load(actionDefinitionsReader, true);
#endif
                }
                finally
                {
                    if (null != actionDefinitionsReader)
                    {
                        actionDefinitionsReader.Close();
                    }
                }
            }

            return standardActions;
        }

        /// <summary>
        /// Enables an external user-interface handler.
        /// </summary>
        /// <param name="installUIHandler">Specifies a callback function.</param>
        /// <param name="messageFilter">Specifies which messages to handle using the external message handler.</param>
        /// <param name="context">Pointer to an application context that is passed to the callback function.</param>
        /// <returns>The return value is the previously set external handler, or null if there was no previously set handler.</returns>
        internal static InstallUIHandler SetExternalUI(InstallUIHandler installUIHandler, int messageFilter, IntPtr context)
        {
            return MsiInterop.MsiSetExternalUI(installUIHandler, messageFilter, context);
        }

        /// <summary>
        /// Enables the installer's internal user interface.
        /// </summary>
        /// <param name="uiLevel">Specifies the level of complexity of the user interface.</param>
        /// <param name="hwnd">Pointer to a window. This window becomes the owner of any user interface created.</param>
        /// <returns>The previous user interface level is returned. If an invalid dwUILevel is passed, then INSTALLUILEVEL_NOCHANGE is returned.</returns>
        internal static int SetInternalUI(int uiLevel, ref IntPtr hwnd)
        {
            return MsiInterop.MsiSetInternalUI(uiLevel, ref hwnd);
        }

        /// <summary>
        /// Get the source/target and short/long file names from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <returns>An array of strings of length 4.  The contents are: short target, long target, short source, and long source.</returns>
        /// <remarks>
        /// If any particular file name part is not parsed, its set to null in the appropriate location of the returned array of strings.
        /// However, the returned array will always be of length 4.
        /// </remarks>
        internal static string[] GetNames(string value)
        {
            string[] names = new string[4];
            int targetSeparator = value.IndexOf(":", StringComparison.Ordinal);

            // split source and target
            string sourceName = null;
            string targetName = value;
            if (0 <= targetSeparator)
            {
                sourceName = value.Substring(targetSeparator + 1);
                targetName = value.Substring(0, targetSeparator);
            }

            // split the source short and long names
            string sourceLongName = null;
            if (null != sourceName)
            {
                int sourceLongNameSeparator = sourceName.IndexOf("|", StringComparison.Ordinal);
                if (0 <= sourceLongNameSeparator)
                {
                    sourceLongName = sourceName.Substring(sourceLongNameSeparator + 1);
                    sourceName = sourceName.Substring(0, sourceLongNameSeparator);
                }
            }

            // split the target short and long names
            int targetLongNameSeparator = targetName.IndexOf("|", StringComparison.Ordinal);
            string targetLongName = null;
            if (0 <= targetLongNameSeparator)
            {
                targetLongName = targetName.Substring(targetLongNameSeparator + 1);
                targetName = targetName.Substring(0, targetLongNameSeparator);
            }

            // remove the long source name when its identical to the long source name
            if (null != sourceName && sourceName == sourceLongName)
            {
                sourceLongName = null;
            }

            // remove the long target name when its identical to the long target name
            if (null != targetName && targetName == targetLongName)
            {
                targetLongName = null;
            }

            // remove the source names when they are identical to the target names
            if (sourceName == targetName && sourceLongName == targetLongName)
            {
                sourceName = null;
                sourceLongName = null;
            }

            // target name(s)
            if ("." != targetName)
            {
                names[0] = targetName;
            }

            if (null != targetLongName && "." != targetLongName)
            {
                names[1] = targetLongName;
            }

            // source name(s)
            if (null != sourceName)
            {
                names[2] = sourceName;
            }

            if (null != sourceLongName && "." != sourceLongName)
            {
                names[3] = sourceLongName;
            }

            return names;
        }

        /// <summary>
        /// Get a source/target and short/long file name from an MSI Filename column.
        /// </summary>
        /// <param name="value">The Filename value.</param>
        /// <param name="source">true to get a source name; false to get a target name</param>
        /// <param name="longName">true to get a long name; false to get a short name</param>
        /// <returns>The name.</returns>
        internal static string GetName(string value, bool source, bool longName)
        {
            string[] names = GetNames(value);

            if (source)
            {
                if (longName && null != names[3])
                {
                    return names[3];
                }
                else if (null != names[2])
                {
                    return names[2];
                }
            }

            if (longName && null != names[1])
            {
                return names[1];
            }
            else
            {
                return names[0];
            }
        }
    }
}
