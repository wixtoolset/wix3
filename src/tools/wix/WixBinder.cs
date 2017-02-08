// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Common binder core of the Windows Installer Xml toolset.
    /// </summary>
    public abstract class WixBinder : IDisposable
    {
        protected BinderCore core;
        protected List<BinderExtension> extensions;
        protected List<InspectorExtension> inspectorExtensions;
        private BinderFileManager fileManager;

        private Localizer localizer;
        protected TempFileCollection tempFiles;
        private WixVariableResolver wixVariableResolver;

        private string outputFile;

        /// <summary>
        /// Creates a binder.
        /// </summary>
        public WixBinder()
        {
            this.extensions = new List<BinderExtension>();
            this.inspectorExtensions = new List<InspectorExtension>();
            this.fileManager = new BinderFileManager();
            this.fileManager.TempFilesLocation = this.TempFilesLocation;
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets or sets the binder file manager class.
        /// </summary>
        /// <value>The binder file manager class.</value>
        public BinderFileManager FileManager
        {
            get { return this.fileManager; }
            set
            {
                this.fileManager = value;
                if (null != value)
                {
                    value.TempFilesLocation = this.TempFilesLocation;
                }
            }
        }

        /// <summary>
        /// Gets or sets the localizer.
        /// </summary>
        /// <value>The localizer.</value>
        public Localizer Localizer
        {
            get { return this.localizer; }
            set { this.localizer = value; }
        }

        /// <summary>
        /// Gets the MessageEventHandler.
        /// </summary>
        /// <value>The MessageEventHandler.</value>
        public MessageEventHandler MessageHandler
        {
            get
            {
                return this.Message;
            }
        }

        /// <summary>
        /// Gets or sets the output file.
        /// </summary>
        /// <value>The output file.</value>
        public string OutputFile
        {
            get { return this.outputFile; }
            set { this.outputFile = value; }
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
                // if we don't have the temporary files object yet, get one
                if (null == this.tempFiles)
                {
                    this.tempFiles = new TempFileCollection();

                    // ensure the base path exists
                    Directory.CreateDirectory(this.tempFiles.BasePath);
                    this.fileManager.TempFilesLocation = this.tempFiles.BasePath;
                }

                return this.tempFiles.BasePath;
            }

            set
            {
                this.DeleteTempFiles();

                if (null == value)
                {
                    this.tempFiles = new TempFileCollection();
                }
                else
                {
                    this.tempFiles = new TempFileCollection(value);
                }

                // ensure the base path exists
                Directory.CreateDirectory(this.tempFiles.BasePath);
                this.fileManager.TempFilesLocation = this.tempFiles.BasePath;
            }
        }

        /// <summary>
        /// Gets or sets the Wix variable resolver.
        /// </summary>
        /// <value>The Wix variable resolver.</value>
        public WixVariableResolver WixVariableResolver
        {
            get { return this.wixVariableResolver; }
            set { this.wixVariableResolver = value; }
        }

        /// <summary>
        /// Binds an output.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="file">The Windows Installer file to create.</param>
        /// <remarks>The Binder.DeleteTempFiles method should be called after calling this method.</remarks>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        public abstract bool Bind(Output output, string file);

        /// <summary>
        /// Does any housekeeping after Bind.
        /// </summary>
        /// <param name="tidy">Whether or not any actual tidying should be done.</param>
        public abstract void Cleanup(bool tidy);

        /// <summary>
        /// Gets help for all the command line arguments.
        /// </summary>
        /// <returns>A string to be added to light's help string.</returns>
        public abstract string GetCommandLineArgumentsHelpString();

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        /// <param name="consoleMessageHandler">The console message handler.</param>
        public abstract StringCollection ParseCommandLine(string[] args, ConsoleMessageHandler consoleMessageHandler);

        /// <summary>
        /// Do any setting up needed after all command line parsing.
        /// </summary>
        public abstract void PostParseCommandLine();

        /// <summary>
        /// Process a list of loaded extensions.
        /// </summary>
        /// <param name="loadedExtensionList">The list of loaded extensions.</param>
        public abstract void ProcessExtensions(WixExtension[] loadedExtensionList);

        /// <summary>
        /// Cleans up the temp files used by the Binder.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public virtual bool DeleteTempFiles()
        {
            if (null == this.tempFiles)
            {
                return true; // no work to do
            }
            else
            {
                bool deleted = Common.DeleteTempFiles(this.TempFilesLocation, this.core);

                if (deleted)
                {
                    ((IDisposable)this.tempFiles).Dispose();
                    this.tempFiles = null; // temp files have been deleted, no need to remember this now
                }

                return deleted;
            }
        }

        /// <summary>
        /// Cleans up the temp files used by the Binder.
        /// </summary>
        public void Dispose()
        {
            this.DeleteTempFiles();
        }

        /// <summary>
        /// Adds an extension.
        /// </summary>
        /// <param name="extension">The extension to add.</param>
        public void AddExtension(WixExtension extension)
        {
            if (null == extension)
            {
                throw new ArgumentNullException("extension");
            }

            if (null != extension.BinderExtension)
            {
                this.extensions.Add(extension.BinderExtension);
            }

            if (null != extension.InspectorExtension)
            {
                this.inspectorExtensions.Add(extension.InspectorExtension);
            }
        }

        /// <summary>
        /// Adds an event handler.
        /// </summary>
        /// <param name="newHandler">The event handler to add.</param>
        public virtual void AddMessageEventHandler(MessageEventHandler newHandler)
        {
            this.Message += newHandler;
        }

        /// <summary>
        /// Final step in binding that transfers (moves/copies) all files generated into the appropriate
        /// location in the source image
        /// </summary>
        /// <param name="fileTransfers">Array of files to transfer.</param>
        /// <param name="fileTransfers">Array of directories to transfer.</param>
        /// <param name="suppressAclReset">Suppress removing ACLs added during file transfer process.</param>
        protected void LayoutMedia(ArrayList fileTransfers, bool suppressAclReset)
        {
            if (this.core.EncounteredError)
            {
                return;
            }

            ArrayList destinationFiles = new ArrayList();

            for (int i = 0; i < fileTransfers.Count; ++i)
            {
                FileTransfer fileTransfer = (FileTransfer)fileTransfers[i];
                string fileSource = fileManager.ResolveFile(fileTransfer.Source, fileTransfer.Type, fileTransfer.SourceLineNumbers, BindStage.Normal);

                // If the source and destination are identical, then there's nothing to do here
                if (0 == String.Compare(fileSource, fileTransfer.Destination, StringComparison.OrdinalIgnoreCase))
                {
                    fileTransfer.Redundant = true;
                    continue;
                }

                bool retry = false;
                do
                {
                    try
                    {
                        if (fileTransfer.Move)
                        {
                            this.core.OnMessage(WixVerboses.MoveFile(fileSource, fileTransfer.Destination));
                            this.FileManager.MoveFile(fileSource, fileTransfer.Destination);
                        }
                        else
                        {
                            this.core.OnMessage(WixVerboses.CopyFile(fileSource, fileTransfer.Destination));
                            this.FileManager.CopyFile(fileSource, fileTransfer.Destination, true);
                        }

                        retry = false;
                        destinationFiles.Add(fileTransfer.Destination);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new WixFileNotFoundException(e.FileName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        string directory = Path.GetDirectoryName(fileTransfer.Destination);
                        this.core.OnMessage(WixVerboses.CreateDirectory(directory));
                        Directory.CreateDirectory(directory);
                        retry = true;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.core.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // try to ensure the file is not read-only
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            try
                            {
                                File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            }
                            catch (ArgumentException) // thrown for unauthorized access errors
                            {
                                throw new WixException(WixErrors.UnauthorizedAccess(fileTransfer.Destination));
                            }

                            // try to delete the file
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                    catch (IOException)
                    {
                        // if we already retried, give up
                        if (retry)
                        {
                            throw;
                        }

                        if (File.Exists(fileTransfer.Destination))
                        {
                            this.core.OnMessage(WixVerboses.RemoveDestinationFile(fileTransfer.Destination));

                            // ensure the file is not read-only, then delete it
                            FileAttributes attributes = File.GetAttributes(fileTransfer.Destination);
                            File.SetAttributes(fileTransfer.Destination, attributes & ~FileAttributes.ReadOnly);
                            try
                            {
                                File.Delete(fileTransfer.Destination);
                            }
                            catch (IOException)
                            {
                                throw new WixException(WixErrors.FileInUse(null, fileTransfer.Destination));
                            }

                            retry = true;
                        }
                        else // no idea what just happened, bail
                        {
                            throw;
                        }
                    }
                } while (retry);
            }

            // finally, if there were any files remove the ACL that may have been added to
            // during the file transfer process
            if (0 < destinationFiles.Count && !suppressAclReset)
            {
                try
                {
                    Microsoft.Tools.WindowsInstallerXml.Cab.Interop.NativeMethods.ResetAcls((string[])destinationFiles.ToArray(typeof(string)), (uint)destinationFiles.Count);
                }
                catch
                {
                    this.core.OnMessage(WixWarnings.UnableToResetAcls());
                }
            }
        }

        /// <summary>
        /// Structure used for all file transfer information.
        /// </summary>
        protected struct FileTransfer
        {
            /// <summary>Source path to file.</summary>
            public string Source;

            /// <summary>Destination path for file.</summary>
            public string Destination;

            /// <summary>Flag if file should be moved (optimal).</summary>
            public bool Move;

            /// <summary>Optional source line numbers where this file transfer orginated.</summary>
            public SourceLineNumberCollection SourceLineNumbers;

            /// <summary>Optional type of file this transfer is moving or copying.</summary>
            public string Type;

            /// <summary>Indicates whether the file transer was a built by this build or copied from other some build.</summary>
            internal bool Built;

            /// <summary>Set during layout of media when the file transfer when the source and target resolve to the same path.</summary>
            internal bool Redundant;

            /// <summary>
            /// Prefer the TryCreate() method to create FileTransfer objects.
            /// </summary>
            /// <param name="source">Source path to file.</param>
            /// <param name="destination">Destination path for file.</param>
            /// <param name="move">File if file should be moved (optimal).</param>
            public FileTransfer(string source, string destination, bool move) :
                this(source, destination, move, null , null)
            {
            }

            /// <summary>
            /// Prefer the TryCreate() method to create FileTransfer objects.
            /// </summary>
            /// <param name="source">Source path to file.</param>
            /// <param name="destination">Destination path for file.</param>
            /// <param name="move">File if file should be moved (optimal).</param>
            /// <param name="type">Optional type of file this transfer is transferring.</param>
            /// <param name="sourceLineNumbers">Optional source line numbers wher this transfer originated.</param>
            public FileTransfer(string source, string destination, bool move, string type, SourceLineNumberCollection sourceLineNumbers)
            {
                this.Source = source;
                this.Destination = destination;
                this.Move = move;

                this.Type = type;
                this.SourceLineNumbers = sourceLineNumbers;

                this.Built = false;
                this.Redundant = false;
            }

            /// <summary>
            /// Creates a file transfer if the source and destination are different.
            /// </summary>
            /// <param name="source">Source path to file.</param>
            /// <param name="destination">Destination path for file.</param>
            /// <param name="move">File if file should be moved (optimal).</param>
            /// <param name="type">Optional type of file this transfer is transferring.</param>
            /// <param name="sourceLineNumbers">Optional source line numbers wher this transfer originated.</param>
            /// <returns>true if the source and destination are the different, false if no file transfer is created.</returns>
            public static bool TryCreate(string source, string destination, bool move, string type, SourceLineNumberCollection sourceLineNumbers, out FileTransfer transfer)
            {
                string sourceFullPath = GetValidatedFullPath(sourceLineNumbers, source);

                string fileLayoutFullPath = GetValidatedFullPath(sourceLineNumbers, destination);

                // if the current source path (where we know that the file already exists) and the resolved
                // path as dictated by the Directory table are not the same, then propagate the file.  The
                // image that we create may have already been done by some other process other than the linker, so 
                // there is no reason to copy the files to the resolved source if they are already there.
                if (String.Equals(sourceFullPath, fileLayoutFullPath, StringComparison.OrdinalIgnoreCase))
                {
                    // TODO: would prefer to return null here.
                    transfer = new FileTransfer(); // create an empty transfer because we must.
                    transfer.Redundant = true;
                    return false;
                }

                transfer = new FileTransfer(source, destination, move, type, sourceLineNumbers);
                return true;
            }

            private static string GetValidatedFullPath(SourceLineNumberCollection sourceLineNumbers, string path)
            {
                string result;

                try
                {
                    result = Path.GetFullPath(path);

                    string filename = Path.GetFileName(result);

                    foreach (string reservedName in Common.ReservedFileNames)
                    {
                        if (reservedName.Equals(filename, StringComparison.OrdinalIgnoreCase))
                        {
                            throw new WixException(WixErrors.InvalidFileName(sourceLineNumbers, path));
                        }
                    }
                }
                catch (System.ArgumentException)
                {
                    throw new WixException(WixErrors.InvalidFileName(sourceLineNumbers, path));
                }
                catch (System.IO.PathTooLongException)
                {
                    throw new WixException(WixErrors.PathTooLong(sourceLineNumbers, path));
                }

                return result;
            }
        }
    }
}
