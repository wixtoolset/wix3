//-------------------------------------------------------------------------------------------------
// <copyright file="Validator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Runs internal consistency evaluators (ICEs) from cub files against a database.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Runs internal consistency evaluators (ICEs) from cub files against a database.
    /// </summary>
    public sealed class Validator : IMessageHandler
    {
        private string actionName;
        private StringCollection cubeFiles;
        private bool encounteredError;
        private ValidatorExtension extension;
        private string[] ices;
        private Output output;
        private string[] suppressedICEs;
        private TempFileCollection tempFiles;
        private InstallUIHandler validationUIHandler;
        private bool validationSessionComplete;

        /// <summary>
        /// Instantiate a new Validator.
        /// </summary>
        public Validator()
        {
            this.cubeFiles = new StringCollection();
            this.extension = new ValidatorExtension();
            this.validationUIHandler = new InstallUIHandler(this.ValidationUIHandler);
        }

        /// <summary>
        /// Gets or sets a <see cref="ValidatorExtension"/> that directs messages from the validator.
        /// </summary>
        /// <value>A <see cref="ValidatorExtension"/> that directs messages from the validator.</value>
        public ValidatorExtension Extension
        {
            get { return this.extension; }
            set { this.extension = value; }
        }

        /// <summary>
        /// Gets or sets the list of ICEs to run.
        /// </summary>
        /// <value>The list of ICEs.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ICEs
        {
            get { return this.ices; }
            set { this.ices = value; }
        }

        /// <summary>
        /// Gets or sets the output used for finding source line information.
        /// </summary>
        /// <value>The output used for finding source line information.</value>
        public Output Output
        {
            // cache Output object until validation for changes in extension
            get { return this.output; }
            set { this.output = value; }
        }

        /// <summary>
        /// Gets or sets the suppressed ICEs.
        /// </summary>
        /// <value>The suppressed ICEs.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] SuppressedICEs
        {
            get { return this.suppressedICEs; }
            set { this.suppressedICEs = value; }
        }

        /// <summary>
        /// Gets or sets the temporary path for the Binder.  If left null, the binder
        /// will use %TEMP% environment variable.
        /// </summary>
        /// <value>Path to temp files.</value>
        public string TempFilesLocation
        {
            get
            {
                return null == this.tempFiles ? String.Empty : this.tempFiles.BasePath;
            }

            set
            {
                if (null == value)
                {
                    this.tempFiles = new TempFileCollection();
                }
                else
                {
                    this.tempFiles = new TempFileCollection(value);
                }
            }
        }

        /// <summary>
        /// Add a cube file to the validation run.
        /// </summary>
        /// <param name="cubeFile">A cube file.</param>
        public void AddCubeFile(string cubeFile)
        {
            this.cubeFiles.Add(cubeFile);
        }

        /// <summary>
        /// Validate a database.
        /// </summary>
        /// <param name="databaseFile">The database to validate.</param>
        /// <returns>true if validation succeeded; false otherwise.</returns>
        public bool Validate(string databaseFile)
        {
            Dictionary<string, string> indexedICEs = new Dictionary<string, string>();
            Dictionary<string, string> indexedSuppressedICEs = new Dictionary<string, string>();
            int previousUILevel = (int)InstallUILevels.Basic;
            IntPtr previousHwnd = IntPtr.Zero;
            InstallUIHandler previousUIHandler = null;

            if (null == databaseFile)
            {
                throw new ArgumentNullException("databaseFile");
            }

            // initialize the validator extension
            this.extension.DatabaseFile = databaseFile;
            this.extension.Output = this.output;
            this.extension.InitializeValidator();

            // if we don't have the temporary files object yet, get one
            if (null == this.tempFiles)
            {
                this.tempFiles = new TempFileCollection();
            }
            Directory.CreateDirectory(this.TempFilesLocation); // ensure the base path is there

            // index the ICEs
            if (null != this.ices)
            {
                foreach (string ice in this.ices)
                {
                    indexedICEs[ice] = null;
                }
            }

            // index the suppressed ICEs
            if (null != this.suppressedICEs)
            {
                foreach (string suppressedICE in this.suppressedICEs)
                {
                    indexedSuppressedICEs[suppressedICE] = null;
                }
            }

            // copy the database to a temporary location so it can be manipulated
            string tempDatabaseFile = Path.Combine(this.TempFilesLocation, Path.GetFileName(databaseFile));
            File.Copy(databaseFile, tempDatabaseFile);

            // remove the read-only property from the temporary database
            FileAttributes attributes = File.GetAttributes(tempDatabaseFile);
            File.SetAttributes(tempDatabaseFile, attributes & ~FileAttributes.ReadOnly);

            Mutex mutex = new Mutex(false, "WixValidator");
            try
            {
                if (!mutex.WaitOne(0, false))
                {
                    this.OnMessage(WixVerboses.ValidationSerialized());
                    mutex.WaitOne();
                }

                using (Database database = new Database(tempDatabaseFile, OpenDatabase.Direct))
                {
                    bool propertyTableExists = database.TableExists("Property");
                    string productCode = null;

                    // remove the product code from the database before opening a session to prevent opening an installed product
                    if (propertyTableExists)
                    {
                        using (View view = database.OpenExecuteView("SELECT `Value` FROM `Property` WHERE Property = 'ProductCode'"))
                        {
                            using (Record record = view.Fetch())
                            {
                                if (null != record)
                                {
                                    productCode = record.GetString(1);

                                    using (View dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                                    {
                                    }
                                }
                            }
                        }
                    }

                    // merge in the cube databases
                    foreach (string cubeFile in this.cubeFiles)
                    {
                        try
                        {
                            using (Database cubeDatabase = new Database(cubeFile, OpenDatabase.ReadOnly))
                            {
                                try
                                {
                                    database.Merge(cubeDatabase, "MergeConflicts");
                                }
                                catch
                                {
                                    // ignore merge errors since they are expected in the _Validation table
                                }
                            }
                        }
                        catch (Win32Exception e)
                        {
                            if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                            {
                                throw new WixException(WixErrors.CubeFileNotFound(cubeFile));
                            }

                            throw;
                        }
                    }

                    // commit the database before proceeding to ensure the streams don't get confused
                    database.Commit();

                    // the property table may have been added to the database
                    // from a cub database without the proper validation rows
                    if (!propertyTableExists)
                    {
                        using (View view = database.OpenExecuteView("DROP table `Property`"))
                        {
                        }
                    }

                    // get all the action names for ICEs which have not been suppressed
                    List<string> actions = new List<string>();
                    using (View view = database.OpenExecuteView("SELECT `Action` FROM `_ICESequence` ORDER BY `Sequence`"))
                    {
                        while (true)
                        {
                            using (Record record = view.Fetch())
                            {
                                if (null == record)
                                {
                                    break;
                                }

                                string action = record.GetString(1);

                                if (!indexedSuppressedICEs.ContainsKey(action))
                                {
                                    actions.Add(action);
                                }
                            }
                        }
                    }

                    if (0 != indexedICEs.Count)
                    {
                        // Walk backwards and remove those that arent in the list
                        for (int i = actions.Count - 1; 0 <= i; i--)
                        {
                            if (!indexedICEs.ContainsKey(actions[i]))
                            {
                                actions.RemoveAt(i);
                            }
                        }
                    }

                    // disable the internal UI handler and set an external UI handler
                    previousUILevel = Installer.SetInternalUI((int)InstallUILevels.None, ref previousHwnd);
                    previousUIHandler = Installer.SetExternalUI(this.validationUIHandler, (int)InstallLogModes.Error | (int)InstallLogModes.Warning | (int)InstallLogModes.User, IntPtr.Zero);

                    // create a session for running the ICEs
                    this.validationSessionComplete = false;
                    using (Session session = new Session(database))
                    {
                        // add the product code back into the database
                        if (null != productCode)
                        {
                            // some CUBs erroneously have a ProductCode property, so delete it if we just picked one up
                            using (View dropProductCodeView = database.OpenExecuteView("DELETE FROM `Property` WHERE `Property` = 'ProductCode'"))
                            {
                            }

                            using (View view = database.OpenExecuteView(String.Format(CultureInfo.InvariantCulture, "INSERT INTO `Property` (`Property`, `Value`) VALUES ('ProductCode', '{0}')", productCode)))
                            {
                            }
                        }

                        foreach (string action in actions)
                        {
                            this.actionName = action;
                            try
                            {
                                session.DoAction(action);
                            }
                            catch (Win32Exception e)
                            {
                                if (!this.encounteredError)
                                {
                                    throw e;
                                }
                                else
                                {
                                    this.encounteredError = false;
                                }
                            }
                            this.actionName = null;
                        }

                        // Mark the validation session complete so we ignore any messages that MSI may fire
                        // during session clean-up.
                        this.validationSessionComplete = true;
                    }
                }
            }
            catch (Win32Exception e)
            {
                // avoid displaying errors twice since one may have already occurred in the UI handler
                if (!this.encounteredError)
                {
                    if (0x6E == e.NativeErrorCode) // ERROR_OPEN_FAILED
                    {
                        // databaseFile is not passed since during light
                        // this would be the temporary copy and there would be
                        // no final output since the error occured; during smoke
                        // they should know the path passed into smoke
                        this.OnMessage(WixErrors.ValidationFailedToOpenDatabase());
                    }
                    else if (0x64D == e.NativeErrorCode)
                    {
                        this.OnMessage(WixErrors.ValidationFailedDueToLowMsiEngine());
                    }
                    else if (0x654 == e.NativeErrorCode)
                    {
                        this.OnMessage(WixErrors.ValidationFailedDueToInvalidPackage());
                    }
                    else if (0x658 == e.NativeErrorCode)
                    {
                        this.OnMessage(WixErrors.ValidationFailedDueToMultilanguageMergeModule());
                    }
                    else if (0x659 == e.NativeErrorCode)
                    {
                        this.OnMessage(WixWarnings.ValidationFailedDueToSystemPolicy());
                    }
                    else
                    {
                        string msgTemp = e.Message;

                        if (null != this.actionName)
                        {
                            msgTemp = String.Concat("Action - '", this.actionName, "' ", e.Message);
                        }

                        this.OnMessage(WixErrors.Win32Exception(e.NativeErrorCode, msgTemp));
                    }
                }
            }
            finally
            {
                Installer.SetExternalUI(previousUIHandler, 0, IntPtr.Zero);
                Installer.SetInternalUI(previousUILevel, ref previousHwnd);

                this.validationSessionComplete = false; // no validation session at this point, so reset the completion flag.

                mutex.ReleaseMutex();
                this.cubeFiles.Clear();
                this.extension.FinalizeValidator();
            }

            return !this.encounteredError;
        }

        /// <summary>
        /// Cleans up the temp files used by the Validator.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public bool DeleteTempFiles()
        {
            if (null == this.tempFiles)
            {
                return true; // no work to do
            }
            else
            {
                bool deleted = AppCommon.DeleteDirectory(this.tempFiles.BasePath, this);

                if (deleted)
                {
                    this.tempFiles = null; // temp files have been deleted, no need to remember this now
                }

                return deleted;
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            this.extension.OnMessage(e);
        }

        /// <summary>
        /// The validation external UI handler.
        /// </summary>
        /// <param name="context">Pointer to an application context.
        /// This parameter can be used for error checking.</param>
        /// <param name="messageType">Specifies a combination of one message box style,
        /// one message box icon type, one default button, and one installation message type.</param>
        /// <param name="message">Specifies the message text.</param>
        /// <returns>-1 for an error, 0 if no action was taken, 1 if OK, 3 to abort.</returns>
        private int ValidationUIHandler(IntPtr context, uint messageType, string message)
        {
            try
            {
                // If we're getting messges during the validation session, send them to
                // the extension. Otherwise, ignore the messages.
                if (!this.validationSessionComplete)
                {
                    this.extension.Log(message, this.actionName);
                }
            }
            catch (WixException ex)
            {
                this.OnMessage(ex.Error);
            }

            return 1;
        }
    }
}
