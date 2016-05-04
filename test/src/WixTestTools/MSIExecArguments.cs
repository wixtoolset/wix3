// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with MSIExec arguments.
    /// </summary>
    public partial class MSIExec
    {
        /// <summary>
        /// Return codes from an MSI install or uninstall
        /// </summary>
        /// <remarks>
        /// Error codes indicative of success are:
        /// ERROR_SUCCESS, ERROR_SUCCESS_REBOOT_INITIATED, and ERROR_SUCCESS_REBOOT_REQUIRED 
        /// </remarks>
        public enum MSIExecReturnCode
        {
            /// <summary>
            /// ERROR_SUCCESS                           0   
            /// Action completed successfully.
            /// </summary>
            SUCCESS = 0,

            /// <summary>
            /// ERROR_INVALID_DATA                      13 
            /// The data is invalid. 
            /// </summary>
            ERROR_INVALID_DATA = 13,

            /// <summary>
            /// ERROR_INVALID_PARAMETER                 87 
            /// One of the parameters was invalid. 
            /// </summary>
            ERROR_INVALID_PARAMETER = 87,

            /// <summary>
            /// ERROR_CALL_NOT_IMPLEMENTED              120 
            /// This value is returned when a custom action attempts to call a function that cannot be called from custom actions. 
            /// The function returns the value ERROR_CALL_NOT_IMPLEMENTED. Available beginning with Windows Installer version 3.0. 
            /// </summary>
            ERROR_CALL_NOT_IMPLEMENTED = 120,

            /// <summary>
            /// ERROR_APPHELP_BLOCK                     1259 
            /// If Windows Installer determines a product may be incompatible with the current operating system, 
            /// it displays a dialog box informing the user and asking whether to try to install anyway. 
            /// This error code is returned if the user chooses not to try the installation. 
            /// </summary>
            ERROR_APPHELP_BLOCK = 1259,

            /// <summary>
            /// ERROR_INSTALL_SERVICE_FAILURE           1601
            /// The Windows Installer service could not be accessed. 
            /// Contact your support personnel to verify that the Windows Installer service is properly registered. 
            /// </summary>
            ERROR_INSTALL_SERVICE_FAILURE = 1601,


            /// <summary>
            /// ERROR_INSTALL_USEREXIT                  1602 
            /// The user cancels installation. 
            /// </summary>
            ERROR_INSTALL_USEREXIT = 1602,

            /// <summary>
            /// ERROR_INSTALL_FAILURE                   1603 
            /// A fatal error occurred during installation.
            /// </summary> 
            ERROR_INSTALL_FAILURE = 1603,

            /// <summary>
            /// ERROR_INSTALL_SUSPEND                   1604 
            /// Installation suspended, incomplete. 
            /// </summary> 
            ERROR_INSTALL_SUSPEND = 1604,

            /// <summary>
            /// ERROR_UNKNOWN_PRODUCT                   1605 
            /// This action is only valid for products that are currently installed. 
            /// </summary> 
            ERROR_UNKNOWN_PRODUCT = 1605,

            /// <summary>
            /// ERROR_UNKNOWN_FEATURE                   1606 
            /// The feature identifier is not registered.
            /// </summary> 
            ERROR_UNKNOWN_FEATURE = 1606,

            /// <summary>
            /// ERROR_UNKNOWN_COMPONENT                 1607 
            /// The component identifier is not registered.
            /// </summary> 
            ERROR_UNKNOWN_COMPONENT = 1607,

            /// <summary>
            /// ERROR_UNKNOWN_PROPERTY                  1608 
            /// This is an unknown property. 
            /// </summary> 
            ERROR_UNKNOWN_PROPERTY = 1608,

            /// <summary>
            /// ERROR_INVALID_HANDLE_STATE              1609 
            /// The handle is in an invalid state. 
            /// </summary> 
            ERROR_INVALID_HANDLE_STATE = 1609,

            /// <summary>
            /// ERROR_BAD_CONFIGURATION                 1610 
            /// The configuration data for this product is corrupt. Contact your support personnel. 
            /// </summary> 
            ERROR_BAD_CONFIGURATION = 1610,

            /// <summary>
            /// ERROR_INDEX_ABSENT                      1611 
            /// The component qualifier not present. 
            /// </summary> 
            ERROR_INDEX_ABSENT = 1611,

            /// <summary>ERROR_INSTALL_SOURCE_ABSENT    1612 
            /// The installation source for this product is not available. 
            /// Verify that the source exists and that you can access it. 
            /// </summary> 
            ERROR_INSTALL_SOURCE_ABSENT = 1612,

            /// <summary>
            /// ERROR_INSTALL_PACKAGE_VERSION           1613 
            /// This installation package cannot be installed by the Windows Installer service. 
            /// You must install a Windows service pack that contains a newer version of the Windows Installer service. 
            /// </summary> 
            ERROR_INSTALL_PACKAGE_VERSION = 1613,

            /// <summary>
            /// ERROR_PRODUCT_UNINSTALLED               1614 
            /// The product is uninstalled. 
            /// </summary> 
            ERROR_PRODUCT_UNINSTALLED = 1614,

            /// <summary>
            /// ERROR_BAD_QUERY_SYNTAX                  1615 
            /// The SQL query syntax is invalid or unsupported. 
            /// </summary> 
            ERROR_BAD_QUERY_SYNTAX = 1615,

            /// <summary>
            /// ERROR_INVALID_FIELD                     1616 
            /// The record field does not exist. 
            /// </summary> 
            ERROR_INVALID_FIELD = 1616,

            /// <summary>
            /// ERROR_INSTALL_ALREADY_RUNNING           1618 
            /// Another installation is already in progress. Complete that installation before proceeding with this install. 
            /// For information about the mutex, see _MSIExecute Mutex. 
            /// </summary>
            ERROR_INSTALL_ALREADY_RUNNING = 1618,

            /// <summary>
            /// ERROR_INSTALL_PACKAGE_OPEN_FAILED       1619 
            /// This installation package could not be opened. Verify that the package exists and is accessible, or contact the 
            /// application vendor to verify that this is a valid Windows Installer package. 
            /// </summary>  
            ERROR_INSTALL_PACKAGE_OPEN_FAILED = 1619,


            /// <summary>
            /// ERROR_INSTALL_PACKAGE_INVALID           1620 
            /// This installation package could not be opened. 
            /// Contact the application vendor to verify that this is a valid Windows Installer package. 
            /// </summary> 
            ERROR_INSTALL_PACKAGE_INVALID = 1620,

            /// <summary>
            /// ERROR_INSTALL_UI_FAILURE                1621 
            /// There was an error starting the Windows Installer service user interface. 
            /// Contact your support personnel. 
            /// </summary> 
            ERROR_INSTALL_UI_FAILURE = 1621,

            /// <summary>
            /// ERROR_INSTALL_LOG_FAILURE               1622 
            /// There was an error opening installation log file. 
            /// Verify that the specified log file location exists and is writable. 
            /// </summary> 
            ERROR_INSTALL_LOG_FAILURE = 1622,

            /// <summary>
            /// ERROR_INSTALL_LANGUAGE_UNSUPPORTED      1623 
            /// This language of this installation package is not supported by your system. 
            /// </summary> 
            ERROR_INSTALL_LANGUAGE_UNSUPPORTED = 1623,

            /// <summary>
            /// ERROR_INSTALL_TRANSFORM_FAILURE         1624 
            /// There was an error applying transforms.
            /// Verify that the specified transform paths are valid. 
            /// </summary> 
            ERROR_INSTALL_TRANSFORM_FAILURE = 1624,


            /// <summary>
            /// ERROR_INSTALL_PACKAGE_REJECTED          1625 
            /// This installation is forbidden by system policy. 
            /// Contact your system administrator. 
            /// </summary> 
            ERROR_INSTALL_PACKAGE_REJECTED = 1625,

            /// <summary>
            /// ERROR_FUNCTION_NOT_CALLED               1626 
            /// The function could not be executed. 
            /// </summary> 
            ERROR_FUNCTION_NOT_CALLED = 1626,

            /// <summary>
            /// ERROR_FUNCTION_FAILED                   1627 
            /// The function failed during execution. 
            /// </summary> 
            ERROR_FUNCTION_FAILED = 1627,

            /// <summary>
            /// ERROR_INVALID_TABLE                     1628 
            /// An invalid or unknown table was specified. 
            /// </summary> 
            ERROR_INVALID_TABLE = 1628,

            /// <summary>
            /// ERROR_DATATYPE_MISMATCH                 1629 
            /// The data supplied is the wrong type. 
            /// </summary> 
            ERROR_DATATYPE_MISMATCH = 1629,

            /// <summary>
            /// ERROR_UNSUPPORTED_TYPE                  1630 
            /// Data of this type is not supported. 
            /// </summary> 
            ERROR_UNSUPPORTED_TYPE = 1630,

            /// <summary>
            /// ERROR_CREATE_FAILED                     1631 
            /// The Windows Installer service failed to start. 
            /// Contact your support personnel. 
            /// </summary> 
            ERROR_CREATE_FAILED = 1631,

            /// <summary>
            /// ERROR_INSTALL_TEMP_UNWRITABLE           1632 
            /// The Temp folder is either full or inaccessible. 
            /// Verify that the Temp folder exists and that you can write to it. 
            /// </summary> 
            ERROR_INSTALL_TEMP_UNWRITABLE = 1632,

            /// <summary>
            /// ERROR_INSTALL_PLATFORM_UNSUPPORTED      1633 
            /// This installation package is not supported on this platform. Contact your application vendor. </summary> 
            ERROR_INSTALL_PLATFORM_UNSUPPORTED = 1633,

            /// <summary>
            /// ERROR_INSTALL_NOTUSED                   1634 
            /// Component is not used on this machine. 
            /// </summary> 
            ERROR_INSTALL_NOTUSED = 1634,

            /// <summary>
            /// ERROR_PATCH_PACKAGE_OPEN_FAILED         1635 
            /// This patch package could not be opened. Verify that the patch package exists and is accessible, 
            /// or contact the application vendor to verify that this is a valid Windows Installer patch package. 
            /// </summary> 
            ERROR_PATCH_PACKAGE_OPEN_FAILED = 1635,

            /// <summary>
            /// ERROR_PATCH_PACKAGE_INVALID             1636 
            /// This patch package could not be opened. 
            /// Contact the application vendor to verify that this is a valid Windows Installer patch package. 
            /// </summary> 
            ERROR_PATCH_PACKAGE_INVALID = 1636,

            /// <summary>
            /// ERROR_PATCH_PACKAGE_UNSUPPORTED         1637 
            /// This patch package cannot be processed by the Windows Installer service.
            /// You must install a Windows service pack that contains a newer version of the Windows Installer service. 
            /// </summary> 
            ERROR_PATCH_PACKAGE_UNSUPPORTED = 1637,

            /// <summary>
            /// ERROR_PRODUCT_VERSION                   1638 
            /// Another version of this product is already installed. 
            /// Installation of this version cannot continue. To configure or remove the existing version of this product, 
            /// use Add/Remove Programs in Control Panel. 
            /// </summary> 
            ERROR_PRODUCT_VERSION = 1638,

            /// <summary>
            /// ERROR_INVALID_COMMAND_LINE              1639 
            /// Invalid command line argument. 
            /// Consult the Windows Installer SDK for detailed command-line help. 
            /// </summary> 
            ERROR_INVALID_COMMAND_LINE = 1639,

            /// <summary>
            /// ERROR_INSTALL_REMOTE_DISALLOWED         1640
            /// The current user is not permitted to perform installations from a client session of a server running the 
            /// Terminal Server role service. 
            /// </summary> 
            ERROR_INSTALL_REMOTE_DISALLOWED = 1640,

            /// <summary>
            /// ERROR_SUCCESS_REBOOT_INITIATED          1641 
            /// The installer has initiated a restart. 
            /// This message is indicative of a success. 
            /// </summary>  
            ERROR_SUCCESS_REBOOT_INITIATED = 1641,

            /// <summary>
            /// ERROR_PATCH_TARGET_NOT_FOUND            1642
            /// The installer cannot install the upgrade patch because the program being upgraded may be missing or the 
            /// upgrade patch updates a different version of the program. 
            /// Verify that the program to be upgraded exists on your computer and that you have the correct upgrade patch. 
            /// </summary>  
            ERROR_PATCH_TARGET_NOT_FOUND = 1642,

            /// <summary>
            /// ERROR_PATCH_PACKAGE_REJECTED            1643 
            /// The patch package is not permitted by system policy. 
            /// </summary> 
            ERROR_PATCH_PACKAGE_REJECTED = 1643,

            /// <summary>
            /// ERROR_INSTALL_TRANSFORM_REJECTED        1644 
            /// One or more customizations are not permitted by system policy. 
            /// </summary> 
            ERROR_INSTALL_TRANSFORM_REJECTED = 1644,

            /// <summary>
            /// ERROR_INSTALL_REMOTE_PROHIBITED         1645 
            /// Windows Installer does not permit installation from a Remote Desktop Connection. 
            /// </summary> 
            ERROR_INSTALL_REMOTE_PROHIBITED = 1645,

            /// <summary>
            /// ERROR_PATCH_REMOVAL_UNSUPPORTED         1646 
            /// The patch package is not a removable patch package. Available beginning with Windows Installer version 3.0. 
            /// </summary> 
            ERROR_PATCH_REMOVAL_UNSUPPORTED = 1646,

            /// <summary>
            /// ERROR_UNKNOWN_PATCH                     1647 
            /// The patch is not applied to this product. Available beginning with Windows Installer version 3.0. 
            /// </summary> 
            ERROR_UNKNOWN_PATCH = 1647,

            /// <summary>
            /// ERROR_PATCH_NO_SEQUENCE                 1648 
            /// No valid sequence could be found for the set of patches. Available beginning with Windows Installer version 3.0. 
            /// </summary> 
            ERROR_PATCH_NO_SEQUENCE = 1648,

            /// <summary>
            /// ERROR_PATCH_REMOVAL_DISALLOWED          1649
            /// Patch removal was disallowed by policy. Available beginning with Windows Installer version 3.0. </summary> 
            ERROR_PATCH_REMOVAL_DISALLOWED = 1649,

            /// <summary>
            /// ERROR_INVALID_PATCH_XML = 1650 
            /// The XML patch data is invalid. Available beginning with Windows Installer version 3.0. 
            /// </summary> 
            ERROR_INVALID_PATCH_XML = 1650,

            /// <summary>
            /// ERROR_PATCH_MANAGED_ADVERTISED_PRODUCT  1651 
            /// Administrative user failed to apply patch for a per-user managed or a per-machine application that is in advertise state. 
            /// Available beginning with Windows Installer version 3.0. </summary> 
            ERROR_PATCH_MANAGED_ADVERTISED_PRODUCT = 1651,

            /// <summary>
            /// ERROR_INSTALL_SERVICE_SAFEBOOT          1652 
            /// Windows Installer is not accessible when the computer is in Safe Mode. 
            /// Exit Safe Mode and try again or try using System Restore to return your computer to a previous state. 
            /// Available beginning with Windows Installer version 4.0. 
            /// </summary> 
            ERROR_INSTALL_SERVICE_SAFEBOOT = 1652,

            /// <summary>
            /// ERROR_ROLLBACK_DISABLED                 1653 
            /// Could not perform a multiple-package transaction because rollback has been disabled. 
            /// Multiple-Package Installations cannot run if rollback is disabled. Available beginning with Windows Installer version 4.5. 
            /// </summary> 
            ERROR_ROLLBACK_DISABLED = 1653,

            /// <summary>
            /// ERROR_SUCCESS_REBOOT_REQUIRED           3010 
            /// A restart is required to complete the install. This message is indicative of a success. 
            /// This does not include installs where the ForceReboot action is run. 
            /// </summary> 
            ERROR_SUCCESS_REBOOT_REQUIRED = 3010
        }

        /// <summary>
        /// Modes of operations for MSIExec; install, administrator install, uninstall .. etc
        /// </summary>
        public enum MSIExecMode
        {
            /// <summary>
            /// Installs or configures a product
            /// </summary>
            Install = 0,

            /// <summary>
            /// Administrative install - Installs a product on the network
            /// </summary>
            AdministrativeInstall,

            /// <summary>
            /// Uninstalls the product
            /// </summary>
            Uninstall,

            /// <summary>
            /// Repairs a product
            /// </summary>
            Repair,

            /// <summary>
            /// Modifies a product
            /// </summary>
            Modify,
        }

        /// <summary>
        /// User interfave levels
        /// </summary>
        public enum MSIExecUserInterfaceLevel
        {
            /// <summary>
            /// No UI
            /// </summary>
            None = 0,

            /// <summary>
            /// Basic UI
            /// </summary>
            Basic,

            /// <summary>
            /// Reduced UI
            /// </summary>
            Reduced,

            /// <summary>
            /// Full UI (default)
            /// </summary>
            Full
        }

        /// <summary>
        /// Logging options
        /// </summary>
        [Flags]
        public enum MSIExecLoggingOptions
        {
            Status_Messages = 0x0001,
            Nonfatal_Warnings = 0x0002,
            All_Error_Messages = 0x0004,
            Start_Up_Of_Actions = 0x0008,
            Action_Specific_Records = 0x0010,
            User_Requests = 0x0020,
            Initial_UI_Parameters = 0x0040,
            OutOfMemory_Or_Fatal_Exit_Information = 0x0080,
            OutOfDiskSpace_Messages = 0x0100,
            Terminal_Properties = 0x0200,
            Verbose_Output = 0x0400,
            Append_To_Existing_Log_File = 0x0800,
            
            Flush_Each_line = 0x1000,
            Extra_Debugging_Information = 0x2000,
            Log_All_Information = 0x4000,
            VOICEWARMUP = 0x0FFF
        }

        #region Private Members

        /// <summary>
        /// Mode of execution (install ,uninstall  or repair)
        /// </summary>
        private MSIExecMode executionMode;

        /// <summary>
        /// Path to msi or ProductCode
        /// </summary>
        private string product;

        // Logging option

        /// <summary>
        /// Logging Options
        /// </summary>
        private MSIExecLoggingOptions loggingOptions;

        /// <summary>
        /// Path to the log file
        /// </summary>
        private string logFile;

        /// <summary>
        /// Unattended mode - progress bar only
        /// </summary>
        private bool passive;

        // Display Options
        /// <summary>
        /// Quiet mode, no user interaction
        /// </summary>
        private bool quiet;


        /// <summary>
        /// Sets user interface level
        /// </summary>
        private MSIExecUserInterfaceLevel userInterfaceLevel;


        // Restart Options

        /// <summary>
        ///Do not restart after the installation is complete
        /// </summary>
        private bool noRestart;

        /// <summary>
        /// Prompts the user for restart if necessary
        /// </summary>
        private bool promptRestart;

        /// <summary>
        ///Always restart the computer after installation
        /// </summary>
        private bool forceRestart;


        /// <summary>
        /// Other arguments.
        /// </summary>
        private string otherArguments;

        #endregion

        #region Public Properties

        /// <summary>
        /// The arguments as they would be passed on the command line.
        /// </summary>
        /// <remarks>
        /// To allow for negative testing, checking for invalid combinations
        /// of arguments is not performed.
        /// </remarks>
        public override string Arguments
        {
            get
            {
                StringBuilder arguments = new StringBuilder();

                // quiet
                if (this.Quiet)
                {
                    arguments.Append(" /quiet ");
                }

                // passive
                if (this.Passive)
                {
                    arguments.Append(" /passive ");
                }

                // UserInterfaceLevel
                switch (this.UserInterfaceLevel)
                {
                    case MSIExecUserInterfaceLevel.None:
                        arguments.Append(" /qn ");
                        break;
                    case MSIExecUserInterfaceLevel.Basic:
                        arguments.Append(" /qb ");
                        break;
                    case MSIExecUserInterfaceLevel.Reduced:
                        arguments.Append(" /qr ");
                        break;
                    case MSIExecUserInterfaceLevel.Full:
                        arguments.Append(" /qf ");
                        break;
                }

                // NoRestart
                if (this.NoRestart)
                {
                    arguments.Append(" /norestart ");
                }

                // PromptRestart
                if (this.PromptRestart)
                {
                    arguments.Append(" /promptrestart ");
                }

                // ForceRestart
                if (this.ForceRestart)
                {
                    arguments.Append(" /forcerestart ");
                }

                // Logging options
                StringBuilder logginOptionsString = new StringBuilder();
                if ((this.LoggingOptions & MSIExecLoggingOptions.Status_Messages) == MSIExecLoggingOptions.Status_Messages)
                {
                    logginOptionsString.Append("i");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Nonfatal_Warnings) == MSIExecLoggingOptions.Nonfatal_Warnings)
                {
                    logginOptionsString.Append("w");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.All_Error_Messages) == MSIExecLoggingOptions.All_Error_Messages)
                {
                    logginOptionsString.Append("e");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Start_Up_Of_Actions) == MSIExecLoggingOptions.Start_Up_Of_Actions)
                {
                    logginOptionsString.Append("a");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Action_Specific_Records) == MSIExecLoggingOptions.Action_Specific_Records)
                {
                    logginOptionsString.Append("r");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.User_Requests) == MSIExecLoggingOptions.User_Requests)
                {
                    logginOptionsString.Append("u");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Initial_UI_Parameters) == MSIExecLoggingOptions.Initial_UI_Parameters)
                {
                    logginOptionsString.Append("c");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.OutOfMemory_Or_Fatal_Exit_Information) == MSIExecLoggingOptions.OutOfMemory_Or_Fatal_Exit_Information)
                {
                    logginOptionsString.Append("m");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.OutOfDiskSpace_Messages) == MSIExecLoggingOptions.OutOfDiskSpace_Messages)
                {
                    logginOptionsString.Append("o");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Terminal_Properties) == MSIExecLoggingOptions.Terminal_Properties)
                {
                    logginOptionsString.Append("p");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Verbose_Output) == MSIExecLoggingOptions.Verbose_Output)
                {
                    logginOptionsString.Append("v");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Extra_Debugging_Information) == MSIExecLoggingOptions.Extra_Debugging_Information)
                {
                    logginOptionsString.Append("x");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Append_To_Existing_Log_File) == MSIExecLoggingOptions.Append_To_Existing_Log_File)
                {
                    logginOptionsString.Append("+");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Flush_Each_line) == MSIExecLoggingOptions.Flush_Each_line)
                {
                    logginOptionsString.Append("!");
                }
                if ((this.LoggingOptions & MSIExecLoggingOptions.Log_All_Information) == MSIExecLoggingOptions.Log_All_Information)
                {
                    logginOptionsString.Append("*");
                }

                // logfile and logging options
                if (0 == logginOptionsString.Length || !string.IsNullOrEmpty(this.LogFile))
                {
                    arguments.Append(" /l");
                    if (0 != logginOptionsString.Length)
                    {
                        arguments.AppendFormat("{0} ",logginOptionsString);
                    }
                    if (!string.IsNullOrEmpty(this.LogFile))
                    {
                        arguments.AppendFormat(" \"{0}\" ", this.LogFile);
                    }
                }

                // OtherArguments
                if (!String.IsNullOrEmpty(this.OtherArguments))
                {
                    arguments.AppendFormat(" {0} ", this.OtherArguments);
                }

                // execution mode
                switch (this.ExecutionMode)
                {
                    case MSIExecMode.Install:
                        arguments.Append(" /package ");
                        break;
                    case MSIExecMode.AdministrativeInstall:
                        arguments.Append(" /a ");
                        break;
                    case MSIExecMode.Repair:
                        arguments.Append(" /f ");
                        break;
                    case MSIExecMode.Uninstall:
                        arguments.Append(" /uninstall ");
                        break;
                };

                // product
                if (!string.IsNullOrEmpty(this.Product))
                {
                    arguments.AppendFormat(" \"{0}\" ", this.Product);
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// Mode of execution (install ,uninstall  or repair)
        /// </summary>
        public MSIExecMode ExecutionMode
        {
            get { return this.executionMode; }
            set { this.executionMode = value; }
        }

        /// <summary>
        /// Path to msi or ProductCode
        /// </summary>
        public string Product
        {
            get { return this.product; }
            set { this.product = value; }
        }


        /*
         * Logging option
         */ 

        /// <summary>
        /// Logging Options
        /// </summary>
        public MSIExecLoggingOptions LoggingOptions
        {
            get { return this.loggingOptions; }
            set { this.loggingOptions = value; }
        }

        /// <summary>
        /// Path to the log file
        /// </summary>
        public string LogFile
        {
            get { return this.logFile; }
            set { this.logFile = value; }
        }

        /// <summary>
        /// Unattended mode - progress bar only
        /// </summary>
        public bool Passive
        {
            get { return this.passive; }
            set { this.passive = value; }
        }


        /*
         *  Display Options
         */
        
        /// <summary>
        /// Quiet mode, no user interaction
        /// </summary>
        public bool Quiet
        {
            get { return this.quiet; }
            set { this.quiet = value; }
        }


        /// <summary>
        /// Sets user interface level
        /// </summary>
        public MSIExecUserInterfaceLevel UserInterfaceLevel
        {
            get { return this.userInterfaceLevel; }
            set { this.userInterfaceLevel = value; }
        }


        /*
         *  Restart Options
         */

        /// <summary>
        ///Do not restart after the installation is complete
        /// </summary>
        public bool NoRestart
        {
            get { return this.noRestart; }
            set { this.noRestart = value; }
        }

        /// <summary>
        /// Prompts the user for restart if necessary
        /// </summary>
        public bool PromptRestart
        {
            get { return this.promptRestart; }
            set { this.promptRestart = value; }
        }

        /// <summary>
        ///Always restart the computer after installation
        /// </summary>
        public bool ForceRestart
        {
            get { return this.forceRestart; }
            set { this.forceRestart = value; }
        }


        /*
         *  Other Arguments
         */
        /// <summary>
        /// Other arguments.
        /// </summary>
        public string OtherArguments
        {
            get { return this.otherArguments; }
            set { this.otherArguments = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values.
        /// </summary>
        public void SetDefaultArguments()
        {
            this.ExecutionMode = MSIExecMode.Install;
            this.Product = String.Empty;
            this.Quiet = true;
            this.Passive = false;
            this.UserInterfaceLevel = MSIExecUserInterfaceLevel.None;
            this.NoRestart = true;
            this.ForceRestart = false;
            this.PromptRestart = false;
            this.LogFile = string.Empty;
            this.LoggingOptions = MSIExecLoggingOptions.VOICEWARMUP;
            this.OtherArguments = String.Empty;
        }
    }
}
