// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// This class is used to wrap a generic command line tool.
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// Information of a user to run as
        /// </summary>
        public class RunAsUserInformation
        {
            private string domain;
            private string username;
            private string password;

            /// <summary>
            /// Domain
            /// </summary>
            public string Domain
            {
                set { this.domain = value; }
                get { return this.domain; }
            }

            /// <summary>
            /// User Name
            /// </summary>
            public string Username
            {
                set { this.username = value; }
                get { return this.username; }
            }

            /// <summary>
            /// Password
            /// </summary>
            public string Password
            {
                set { this.password = value; }
                get { return this.password; }
            }

            /// <summary>
            /// Create a new RunAsUserInfromation object
            /// </summary>
            /// <param name="domain">Domain name of the user.</param>
            /// <param name="newUserName">Username</param>
            /// <param name="password">Password</param>
            public RunAsUserInformation(string domain, string username, string password)
            {
                this.Domain = domain;
                this.Username = username;
                this.Password = password;
            }
        }

        /// <summary>
        /// The arguments to pass to the tool
        /// </summary>
        private string arguments;

        /// <summary>
        /// Environment Variables to be passed to the tool
        /// </summary>
        private Dictionary<string, string> environmentVariables;

        /// <summary>
        /// Print output from the tool execution to the console
        /// </summary>
        private bool printOutputToConsole = false;

        /// <summary>
        /// User information for the account to the run the tool under
        /// </summary>
        private RunAsUserInformation runAsUser;

        /// <summary>
        /// The full path to the tool
        /// </summary>
        private string toolFile;

        /// <summary>
        /// The previous run results
        /// </summary>
        private Stack<Result> results = new Stack<Result>();

        /// <summary>
        /// The working directory of the tool
        /// </summary>
        private string workingDirectory;

        /// <summary>
        /// Constructor for a tool
        /// </summary>
        public Tool()
            : this(null, null)
        {
        }

        /// <summary>
        /// Constructor for a tool
        /// </summary>
        /// <param name="toolFile">The full path to the tool. Eg. c:\bin\candle.exe</param>
        /// <param name="arguments">The command line arguments to use when running the tool</param>
        public Tool(string toolFile, string arguments)
        {
            // Replace null with an empty string
            this.Arguments = (arguments ?? String.Empty);
            this.ToolFile = (toolFile ?? String.Empty);
            this.EnvironmentVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// The arguments to pass to the tool
        /// </summary>
        public virtual string Arguments
        {
            get { return this.arguments; }
            set { this.arguments = value; }
        }

        /// <summary>
        /// The full command line that is executed
        /// </summary>
        public string CommandLine
        {
            get
            {
                return String.Format("{0} {1}", this.ToolFile, this.Arguments);
            }
        }

        /// <summary>
        /// Environment Variables to be passed to the tool
        /// </summary>
        public Dictionary<string, string> EnvironmentVariables
        {
            get { return this.environmentVariables; }
            set { this.environmentVariables = value; }
        }

        /// <summary>
        /// Print output from the tool execution to the console
        /// </summary>
        public bool PrintOutputToConsole
        {
            get { return this.printOutputToConsole; }
            set { this.printOutputToConsole = value; }
        }

        /// <summary>
        /// The result of the last run
        /// </summary>
        public Result Result
        {
            get { return this.Results.Peek(); }
        }

        /// <summary>
        /// The previous run results
        /// </summary>
        public Stack<Result> Results
        {
            get { return this.results; }
            set { this.results = value; }
        }

        /// <summary>
        /// User information for the account to the run the tool under
        /// </summary>
        public RunAsUserInformation RunAsUser
        {
            get { return this.runAsUser; }
            set { this.runAsUser = value; }
        }

        /// <summary>
        /// The full path to the tool
        /// </summary>
        public string ToolFile
        {
            get { return this.toolFile; }
            set { this.toolFile = value; }
        }

        /// <summary>
        /// The working directory of the tool
        /// </summary>
        public string WorkingDirectory
        {
            get { return (this.workingDirectory ?? String.Empty); }
            set { this.workingDirectory = value; }
        }

        /// <summary>
        /// Print the last run result to the Console
        /// </summary>
        public virtual void PrintResult()
        {
            if (null != this.Result)
            {
                Console.WriteLine(this.Result.ToString());
            }
            else
            {
                Console.WriteLine("No results");
            }
        }

        /// <summary>
        /// Executes the tool with the currently set arguments
        /// </summary>
        /// <returns>A Result object containing data about the run</returns>
        public virtual Result Run()
        {
            return this.Run(this.Arguments);
        }

        /// <summary>
        /// Executes the tool with the specified arguments
        /// </summary>
        /// <param name="arguments">The arguments to pass on the command line. The value of arguments will be persisted in the Arguments property.</param>
        /// <returns>A Result object containing data about the run</returns>
        public virtual Result Run(string arguments)
        {
            if (String.Empty == this.ToolFile)
            {
                throw new Exception("The tool is not specified");
            }

            // Expand environment variables in the tool file
            string expandedToolFile = Environment.ExpandEnvironmentVariables(this.ToolFile);

            if (!File.Exists(expandedToolFile))
            {
                throw new FileNotFoundException(String.Format("The file {0} could not be found", expandedToolFile), expandedToolFile);
            }

            Process process = new Process();
            process.StartInfo.FileName = expandedToolFile;
            process.StartInfo.Arguments = Environment.ExpandEnvironmentVariables(arguments);
            process.StartInfo.WorkingDirectory = Environment.ExpandEnvironmentVariables(this.WorkingDirectory);
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            foreach(string environmentVariable in this.EnvironmentVariables.Keys)
            {
                if (!process.StartInfo.EnvironmentVariables.ContainsKey(environmentVariable))
                {
                    process.StartInfo.EnvironmentVariables.Add(environmentVariable, this.EnvironmentVariables[environmentVariable]);
                }
                else
                {
                    process.StartInfo.EnvironmentVariables[environmentVariable] = this.EnvironmentVariables[environmentVariable];
                }
            }

            // RunAsUser
            if (null != this.RunAsUser)
            {
                process.StartInfo.Domain = RunAsUser.Domain;
                process.StartInfo.UserName = RunAsUser.Username;
                process.StartInfo.Password = new System.Security.SecureString();
                process.StartInfo.Password.Clear(); // Make sure there is no junk
                foreach (char c in RunAsUser.Password.ToCharArray())
                {
                    process.StartInfo.Password.AppendChar(c);
                }
                
                process.StartInfo.LoadUserProfile = true;
            }
            
            // Create the Result object
            Result result = new Result();
            result.Command = this.CommandLine;

            try
            {
                // Run the process
                process.Start();

                StringBuilder standardOutput = new StringBuilder();
                while (!process.StandardOutput.EndOfStream)
                {
                    standardOutput.AppendLine(process.StandardOutput.ReadLine());
                }

                result.StandardOutput = standardOutput.ToString();

                StringBuilder standardError = new StringBuilder();
                while (!process.StandardError.EndOfStream)
                {
                    standardError.AppendLine(process.StandardError.ReadLine());
                }

                result.StandardError = standardError.ToString();

                process.WaitForExit();
                result.ExitCode = process.ExitCode;
            }
            finally
            {
                if (null != process)
                {
                    process.Close();
                }
            }

            if (this.PrintOutputToConsole)
            {
                Console.WriteLine(result.ToString());
            }

            this.Results.Push(result);
            return result;
        }
    }
}
