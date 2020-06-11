// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Base class for WiX tool tasks; executes tools in-process
    /// so that repeated invocations are much faster.
    /// </summary>
    public abstract class WixToolTask : ToolTask, IDisposable
    {
        private string additionalOptions;
        private bool disposed;
        private bool noLogo;
        private bool runAsSeparateProcess;
        private bool suppressAllWarnings;
        private string[] suppressSpecificWarnings;
        private string[] treatSpecificWarningsAsErrors;
        private bool treatWarningsAsErrors;
        private bool verboseOutput;
        private Queue<string> messageQueue;
        private ManualResetEvent messagesAvailable;
        private ManualResetEvent toolExited;
        private int exitCode;

        /// <summary>
        /// Gets or sets additional options that are appended the the tool command-line.
        /// </summary>
        /// <remarks>
        /// This allows the task to support extended options in the tool which are not
        /// explicitly implemented as properties on the task.
        /// </remarks>
        public string AdditionalOptions
        {
            get { return this.additionalOptions; }
            set { this.additionalOptions = value; }
        }

        /// <summary>
        /// Gets or sets a flag indicating whether the task should be run as separate
        /// process instead of in-proc with MSBuild which is the default.
        /// </summary>
        public bool RunAsSeparateProcess
        {
            get { return this.runAsSeparateProcess; }
            set { this.runAsSeparateProcess = value; }
        }

        #region Common Options
        /// <summary>
        /// Gets or sets whether all warnings should be suppressed.
        /// </summary>
        public bool SuppressAllWarnings
        {
            get { return this.suppressAllWarnings; }
            set { this.suppressAllWarnings = value; }
        }

        /// <summary>
        /// Gets or sets a list of specific warnings to be suppressed.
        /// </summary>
        public string[] SuppressSpecificWarnings
        {
            get { return this.suppressSpecificWarnings; }
            set { this.suppressSpecificWarnings = value; }
        }

        /// <summary>
        /// Gets or sets whether all warnings should be treated as errors.
        /// </summary>
        public bool TreatWarningsAsErrors
        {
            get { return this.treatWarningsAsErrors; }
            set { this.treatWarningsAsErrors = value; }
        }

        /// <summary>
        /// Gets or sets a list of specific warnings to treat as errors.
        /// </summary>
        public string[] TreatSpecificWarningsAsErrors
        {
            get { return this.treatSpecificWarningsAsErrors; }
            set { this.treatSpecificWarningsAsErrors = value; }
        }

        /// <summary>
        /// Gets or sets whether to display verbose output.
        /// </summary>
        public bool VerboseOutput
        {
            get { return this.verboseOutput; }
            set { this.verboseOutput = value; }
        }

        /// <summary>
        /// Gets or sets whether to display the logo.
        /// </summary>
        public bool NoLogo
        {
            get { return this.noLogo; }
            set { this.noLogo = value; }
        }
        #endregion

        /// <summary>
        /// Cleans up the ManualResetEvent members
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
                disposed = true;
            }
        }

        /// <summary>
        /// Cleans up the ManualResetEvent members
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                messagesAvailable.Close();
                toolExited.Close();
            }
        }

        /// <summary>
        /// Generate the command line arguments to write to the response file from the properties.
        /// </summary>
        /// <returns>Command line string.</returns>
        protected override string GenerateResponseFileCommands()
        {
            WixCommandLineBuilder commandLineBuilder = new WixCommandLineBuilder();
            this.BuildCommandLine(commandLineBuilder);
            return commandLineBuilder.ToString();
        }

        /// <summary>
        /// Builds a command line from options in this and derivative tasks.
        /// </summary>
        /// <remarks>
        /// Derivative classes should call BuildCommandLine() on the base class to ensure that common command line options are added to the command.
        /// </remarks>
        protected virtual void BuildCommandLine(WixCommandLineBuilder commandLineBuilder)
        {
            commandLineBuilder.AppendIfTrue("-nologo", this.NoLogo);
            commandLineBuilder.AppendArrayIfNotNull("-sw", this.SuppressSpecificWarnings);
            commandLineBuilder.AppendIfTrue("-sw", this.SuppressAllWarnings);
            commandLineBuilder.AppendIfTrue("-v", this.VerboseOutput);
            commandLineBuilder.AppendArrayIfNotNull("-wx", this.TreatSpecificWarningsAsErrors);
            commandLineBuilder.AppendIfTrue("-wx", this.TreatWarningsAsErrors);
        }

        /// <summary>
        /// Executes a tool in-process by loading the tool assembly and invoking its entrypoint.
        /// </summary>
        /// <param name="pathToTool">Path to the tool to be executed; must be a managed executable.</param>
        /// <param name="responseFileCommands">Commands to be written to a response file.</param>
        /// <param name="commandLineCommands">Commands to be passed directly on the command-line.</param>
        /// <returns>The tool exit code.</returns>
        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            if (this.RunAsSeparateProcess)
            {
                return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }

            this.messageQueue = new Queue<string>();
            this.messagesAvailable = new ManualResetEvent(false);
            this.toolExited = new ManualResetEvent(false);

            Util.RunningInMsBuild = true;

            WixToolTaskLogger logger = new WixToolTaskLogger(this.messageQueue, this.messagesAvailable);
            TextWriter saveConsoleOut = Console.Out;
            TextWriter saveConsoleError = Console.Error;
            Console.SetOut(logger);
            Console.SetError(logger);

            string responseFile = null;
            try
            {
                string responseFileSwitch;
                responseFile = this.GetTemporaryResponseFile(responseFileCommands, out responseFileSwitch);
                if (!String.IsNullOrEmpty(responseFileSwitch))
                {
                    commandLineCommands = commandLineCommands + " " + responseFileSwitch;
                }

                string[] arguments = CommandLineResponseFile.ParseArgumentsToArray(commandLineCommands);

                Thread toolThread = new Thread(new ParameterizedThreadStart(this.ExecuteToolThread));
                toolThread.Start(new object[] { pathToTool, arguments });

                this.HandleToolMessages();

                if (this.exitCode == 0 && this.Log.HasLoggedErrors)
                {
                    this.exitCode = -1;
                }

                return this.exitCode;
            }
            finally
            {
                if (responseFile != null)
                {
                    File.Delete(responseFile);
                }

                Console.SetOut(saveConsoleOut);
                Console.SetError(saveConsoleError);
            }
        }

        /// <summary>
        /// Called by a new thread to execute the tool in that thread.
        /// </summary>
        /// <param name="parameters">Tool path and arguments array.</param>
        private void ExecuteToolThread(object parameters)
        {
            try
            {
                object[] pathAndArguments = (object[])parameters;
                Assembly toolAssembly = Assembly.LoadFrom((string)pathAndArguments[0]);
                this.exitCode = (int)toolAssembly.EntryPoint.Invoke(null, new object[] { pathAndArguments[1] });
            }
            catch (FileNotFoundException fnfe)
            {
                Log.LogError("Unable to load tool from path {0}.  Consider setting the ToolPath parameter to $(WixToolPath).", fnfe.FileName);
                this.exitCode = -1;
            }
            catch (Exception ex)
            {
                this.exitCode = -1;
                this.LogEventsFromTextOutput(ex.Message, MessageImportance.High);
                foreach (string stackTraceLine in ex.StackTrace.Split('\n'))
                {
                    this.LogEventsFromTextOutput(stackTraceLine.TrimEnd(), MessageImportance.High);
                }

                throw;
            }
            finally
            {
                this.toolExited.Set();
            }
        }

        /// <summary>
        /// Waits for messages from the tool thread and sends them to the MSBuild logger on the original thread.
        /// Returns when the tool thread exits.
        /// </summary>
        private void HandleToolMessages()
        {
            WaitHandle[] waitHandles = new WaitHandle[] { this.messagesAvailable, this.toolExited };
            while (WaitHandle.WaitAny(waitHandles) == 0)
            {
                string message = null;

                lock (this.messageQueue)
                {
                    if (this.messageQueue.Count > 0)
                    {
                        message = messageQueue.Dequeue();
                    }
                    else
                    {
                        this.messagesAvailable.Reset();
                    }
                }

                if (!String.IsNullOrEmpty(message))
                {
                    // This log to text output must live outside the message lock to
                    // prevent dead locks when the WixToolTaskLogger.Write() is called
                    // inside a Console.WriteLine() call.
                    this.LogEventsFromTextOutput(message, MessageImportance.Normal);
                }
            }
        }

        /// <summary>
        /// Creates a temporary response file for tool execution.
        /// </summary>
        /// <returns>Path to the response file.</returns>
        /// <remarks>
        /// The temporary file should be deleted after the tool execution is finished.
        /// </remarks>
        private string GetTemporaryResponseFile(string responseFileCommands, out string responseFileSwitch)
        {
            string responseFile = null;
            responseFileSwitch = null;

            if (!String.IsNullOrEmpty(responseFileCommands))
            {
                responseFile = Path.GetTempFileName();
                using (StreamWriter writer = new StreamWriter(responseFile, false, this.ResponseFileEncoding))
                {
                    writer.Write(responseFileCommands);
                }
                responseFileSwitch = this.GetResponseFileSwitch(responseFile);
            }
            return responseFile;
        }

        /// <summary>
        /// Cycles thru each task to find correct path of the file in question.
        /// Looks at item spec, hintpath and then in user defined Reference Paths
        /// </summary>
        /// <param name="tasks">Input task array</param>
        /// <param name="referencePaths">SemiColon delimited directories to search</param>
        /// <returns>List of task item file paths</returns>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists")]
        protected static List<string> AdjustFilePaths(ITaskItem[] tasks, string[] referencePaths)
        {
            List<string> sourceFilePaths = new List<string>();

            if (tasks == null)
            {
                return sourceFilePaths;
            }

            foreach (ITaskItem task in tasks)
            {
                string filePath = task.ItemSpec;
                if (!File.Exists(filePath))
                {
                    filePath = task.GetMetadata("HintPath");
                    if (!File.Exists(filePath))
                    {
                        string searchPath = FileSearchHelperMethods.SearchFilePaths(referencePaths, filePath);
                        if (!String.IsNullOrEmpty(searchPath))
                        {
                            filePath = searchPath;
                        }
                    }
                }
                sourceFilePaths.Add(filePath);
            }

            return sourceFilePaths;
        }

        /// <summary>
        /// Used as a replacement for Console.Out to capture output from a tool
        /// and redirect it to the MSBuild logging system.
        /// </summary>
        private class WixToolTaskLogger : TextWriter
        {
            private StringBuilder buffer;
            private Queue<string> messageQueue;
            private ManualResetEvent messagesAvailable;

            /// <summary>
            /// Creates a new logger that sends tool output to the tool task's log handler.
            /// </summary>
            public WixToolTaskLogger(Queue<string> messageQueue, ManualResetEvent messagesAvailable) : base(CultureInfo.CurrentCulture)
            {
                this.messageQueue = messageQueue;
                this.messagesAvailable = messagesAvailable;
                this.buffer = new StringBuilder();
            }

            /// <summary>
            /// Gets the encoding of the logger.
            /// </summary>
            public override Encoding Encoding
            {
                get { return Encoding.Unicode; }
            }

            /// <summary>
            /// Redirects output to a buffer; watches for newlines and sends each line to the
            /// MSBuild logging system.
            /// </summary>
            /// <param name="value">Character being written.</param>
            /// <remarks>All other Write() variants eventually call into this one.</remarks>
            public override void Write(char value)
            {
                if (value == '\n')
                {
                    if (this.buffer.Length > 0 && this.buffer[this.buffer.Length - 1] == '\r')
                    {
                        this.buffer.Length = this.buffer.Length - 1;
                    }

                    lock (this.messageQueue)
                    {
                        this.messageQueue.Enqueue(this.buffer.ToString());
                        this.messagesAvailable.Set();
                    }

                    this.buffer.Length = 0;
                }
                else
                {
                    this.buffer.Append(value);
                }
            }
        }
    }
}
