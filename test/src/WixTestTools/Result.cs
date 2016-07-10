// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Contains the results of a process execution
    /// </summary>
    public class Result
    {
        /// <summary>
        /// The command that was run
        /// </summary>
        private string command;

        /// <summary>
        /// The exit code
        /// </summary>
        private int exitCode;

        /// <summary>
        /// The standard error
        /// </summary>
        private string standardError;

        /// <summary>
        /// The standard output
        /// </summary>
        private string standardOutput;

        /// <summary>
        /// Constructor
        /// </summary>
        public Result()
        {
        }

        /// <summary>
        /// The command that was run
        /// </summary>
        public string Command
        {
            get { return this.command; }
            set { this.command = value; }
        }

        /// <summary>
        /// The exit code
        /// </summary>
        public int ExitCode
        {
            get { return this.exitCode; }
            set { this.exitCode = value; }
        }

        /// <summary>
        /// The standard error
        /// </summary>
        public string StandardError
        {
            get { return this.standardError; }
            set { this.standardError = value; }
        }

        /// <summary>
        /// The standard output
        /// </summary>
        public string StandardOutput
        {
            get { return this.standardOutput; }
            set { this.standardOutput = value; }
        }

        /// <summary>
        /// Populates a string with data contained in this Result
        /// </summary>
        /// <returns>A string</returns>
        public override string ToString()
        {
            StringBuilder returnValue = new StringBuilder();
            returnValue.AppendLine();
            returnValue.AppendLine("----------------");
            returnValue.AppendLine("Tool run result:");
            returnValue.AppendLine("----------------");
            returnValue.AppendLine("Command:");
            returnValue.AppendLine(this.command);
            returnValue.AppendLine();
            returnValue.AppendLine("Standard Output:");
            returnValue.AppendLine(this.standardOutput);
            returnValue.AppendLine("Standard Error:");
            returnValue.AppendLine(this.standardError);
            returnValue.AppendLine("Exit Code:");
            returnValue.AppendLine(Convert.ToString(this.exitCode));
            returnValue.AppendLine("----------------");

            return returnValue.ToString();
        }
    }
}
