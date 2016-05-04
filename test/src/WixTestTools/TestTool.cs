// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Adds verification to a Tool
    /// </summary>
    public class TestTool : Tool
    {
        /// <summary>
        /// Stores the errors that occurred when a run was checked against its expected results
        /// </summary>
        private List<string> errors;

        /// <summary>
        /// A list of Regex's that are expected to match stderr
        /// </summary>
        private List<Regex> expectedErrorRegexs = new List<Regex>();

        /// <summary>
        /// The expected error strings to stderr
        /// </summary>
        private List<string> expectedErrorStrings = new List<string>();

        /// <summary>
        /// The expected exit code of the tool
        /// </summary>
        private int? expectedExitCode = null;

        /// <summary>
        /// A list of Regex's that are expected to match stdout
        /// </summary>
        private List<Regex> expectedOutputRegexs = new List<Regex>();

        /// <summary>
        /// The expected output strings to stdout
        /// </summary>
        private List<string> expectedOutputStrings = new List<string>();

        /// <summary>
        /// Constructor for a TestTool
        /// </summary>
        public TestTool()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor for a TestTool
        /// </summary>
        /// <param name="toolFile">The full path to the tool. Eg. c:\bin\candle.exe</param>
        public TestTool(string toolFile)
            : this(toolFile, null)
        {
        }

        /// <summary>
        /// Constructor for a TestTool
        /// </summary>
        /// <param name="toolFile">The full path to the tool. Eg. c:\bin\candle.exe</param>
        /// <param name="arguments">The command line arguments to use when running the tool</param>
        public TestTool(string toolFile, string arguments)
            : base(toolFile, arguments)
        {
            this.PrintOutputToConsole = true;
        }

        /// <summary>
        /// Stores the errors that occurred when a run was checked against its expected results
        /// </summary>
        public List<string> Errors
        {
            get { return this.errors; }
            set { this.errors = value; }
        }

        /// <summary>
        /// A list of Regex's that are expected to match stderr
        /// </summary>
        public List<Regex> ExpectedErrorRegexs
        {
            get { return this.expectedErrorRegexs; }
            set { this.expectedErrorRegexs = value; }
        }

        /// <summary>
        /// The expected error strings to stderr
        /// </summary>
        public List<string> ExpectedErrorStrings
        {
            get { return this.expectedErrorStrings; }
            set { this.expectedErrorStrings = value; }
        }

        /// <summary>
        /// The expected exit code of the tool
        /// </summary>
        public int? ExpectedExitCode
        {
            get { return this.expectedExitCode; }
            set { this.expectedExitCode = value; }
        }

        /// <summary>
        /// A list of Regex's that are expected to match stdout
        /// </summary>
        public List<Regex> ExpectedOutputRegexs
        {
            get { return this.expectedOutputRegexs; }
            set { this.expectedOutputRegexs = value; }
        }

        /// <summary>
        /// The expected output strings to stdout
        /// </summary>
        public List<string> ExpectedOutputStrings
        {
            get { return this.expectedOutputStrings; }
            set { this.expectedOutputStrings = value; }
        }

        /// <summary>
        /// Print the errors from the last run
        /// </summary>
        public void PrintErrors()
        {
            if (null != this.Errors)
            {
                Console.WriteLine("Errors:");

                foreach (string error in this.Errors)
                {
                    Console.WriteLine(error);
                }
            }
        }

        /// <summary>
        /// Run the tool
        /// </summary>
        /// <returns>The results of the run</returns>
        public override Result Run()
        {
            return this.Run(true);
        }

        /// <summary>
        /// Run the tool
        /// </summary>
        /// <param name="exceptionOnError">Throw an exception if the expected results don't match the actual results</param>
        /// <exception cref="System.Exception">Thrown when the expected results don't match the actual results</exception>
        /// <returns>The results of the run</returns>
        public virtual Result Run(bool exceptionOnError)
        {
            Result result = base.Run(this.Arguments);

            this.errors = this.CheckResult(result);

            if (exceptionOnError && 0 < this.errors.Count)
            {
                if (this.PrintOutputToConsole)
                {
                    this.PrintErrors();
                }

                TestException e = new TestException(String.Format("Expected results did not match actual results", this.CommandLine, result.ExitCode), result);
                throw e;
            }

            return result;
        }

        /// <summary>
        /// Checks that the result from a run matches the expected results
        /// </summary>
        /// <param name="result">A result from a run</param>
        /// <returns>A list of errors</returns>
        public virtual List<string> CheckResult(Result result)
        {
            List<string> errors = new List<string>();

            // Verify that the expected return code matched the actual return code
            if (null != this.ExpectedExitCode && this.ExpectedExitCode != result.ExitCode)
            {
                errors.Add(String.Format("Expected exit code {0} did not match actual exit code {1}", this.expectedExitCode, result.ExitCode));
            }

            // Verify that the expected error string are in stderr
            if (null != this.ExpectedErrorStrings)
            {
                foreach (string expectedString in this.ExpectedErrorStrings)
                {
                    if (!result.StandardError.Contains(expectedString))
                    {
                        errors.Add(String.Format("The text '{0}' was not found in stderr", expectedString));
                    }
                }
            }

            // Verify that the expected output string are in stdout
            if (null != this.ExpectedOutputStrings)
            {
                foreach (string expectedString in this.ExpectedOutputStrings)
                {
                    if (!result.StandardOutput.Contains(expectedString))
                    {
                        errors.Add(String.Format("The text '{0}' was not found in stdout", expectedString));
                    }
                }
            }

            // Verify that the expected regular expressions match stderr
            if (null != this.ExpectedOutputRegexs)
            {
                foreach (Regex expectedRegex in this.ExpectedOutputRegexs)
                {
                    if (!expectedRegex.IsMatch(result.StandardOutput))
                    {
                        errors.Add(String.Format("Regex {0} did not match stdout", expectedRegex.ToString()));
                    }
                }
            }

            // Verify that the expected regular expressions match stdout
            if (null != this.ExpectedErrorRegexs)
            {
                foreach (Regex expectedRegex in this.ExpectedErrorRegexs)
                {
                    if (!expectedRegex.IsMatch(result.StandardError))
                    {
                        errors.Add(String.Format("Regex {0} did not match stderr", expectedRegex.ToString()));
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Clears all of the expected results and resets them to the default values
        /// </summary>
        public virtual void SetDefaultExpectedResults()
        {
            this.ExpectedErrorRegexs = new List<System.Text.RegularExpressions.Regex>();
            this.ExpectedErrorStrings = new List<string>();
            this.ExpectedExitCode = null;
            this.ExpectedOutputRegexs = new List<System.Text.RegularExpressions.Regex>();
            this.ExpectedOutputStrings = new List<string>();
        }
    }
}
