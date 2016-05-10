// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A class that wraps WixUnit.
    /// </summary>
    public partial class WixUnit : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory.
        /// </summary>
        public WixUnit()
            : this((string)null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool.</param>
        public WixUnit(string workingDirectory)
            : base("wixunit.exe", workingDirectory)
        {
            this.IgnoreExtraWixMessages = true;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="wixUnit">The object to copy</param>
        public WixUnit(WixUnit wixUnit)
            : this()
        {
            this.WixUnitEnvironmentVariables = wixUnit.WixUnitEnvironmentVariables;
            this.Help = wixUnit.Help;
            this.NoTidy = wixUnit.NoTidy;
            this.OtherArguments = wixUnit.OtherArguments;
            this.RunFailedTests = wixUnit.RunFailedTests;
            this.SingleThreaded = wixUnit.SingleThreaded;
            this.TestFile = wixUnit.TestFile;
            this.Tests = wixUnit.Tests;
            this.Update = wixUnit.Update;
            this.Validate = wixUnit.Validate;
            this.VerboseOutput = wixUnit.VerboseOutput;
            this.WorkingDirectory = wixUnit.WorkingDirectory;
        }

        /// <summary>
        /// Checks that the result from a run matches the expected results.
        /// </summary>
        /// <param name="result">A result from a run.</param>
        /// <returns>A list of errors.</returns>
        public override List<string> CheckResult(Result result)
        {
            List<string> errors = new List<string>();
            errors.AddRange(base.CheckResult(result));

            // Explicitly look for WixUnit error messages. We ignore all other tool error messages because
            // they are handled by WixUnit.
            errors.AddRange(this.FindWixUnitWixMessages(result.StandardOutput));

            Regex failedTestRun = new Regex(@"Failed (?<numFailedTests>\d*) out of (?<numTests>\d*) unit test");
            Match messageMatch = failedTestRun.Match(result.StandardOutput);

            if (messageMatch.Success)
            {
                int numFailedTests = Convert.ToInt32(messageMatch.Groups["numFailedTests"].Value);
                int numTests = Convert.ToInt32(messageMatch.Groups["numTests"].Value);
                errors.Add(String.Format("{0} test(s) failed", numFailedTests));
            }

            return errors;
        }

        /// <summary>
        /// Run WixUnit on a particular test
        /// </summary>
        /// <param name="test">The test to run</param>
        /// <returns>The results of the run</returns>
        public Result RunTest(string test)
        {
            // Create a copy of the this object with a new list of tests
            WixUnit wixUnit = new WixUnit(this);
            wixUnit.Tests = new List<string>();
            wixUnit.Tests.Add(test);

            return wixUnit.Run();
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return String.Empty; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "WixUnit"; }
        }

        /// <summary>
        /// Helper method for finding WixUnit errors and warnings in the output
        /// </summary>
        /// <param name="output">The text to search</param>
        /// <returns>A list of WixMessages in the output</returns>
        private List<string> FindWixUnitWixMessages(string output)
        {
            List<string> wixUnitWixMessages = new List<string>();

            foreach (string line in output.Split('\n', '\r'))
            {
                WixMessage wixUnitWixMessage = WixMessage.FindWixMessage(line, WixTools.Wixunit);

                if (null != wixUnitWixMessage)
                {
                    wixUnitWixMessages.Add(wixUnitWixMessage.ToString());
                }
            }

            return wixUnitWixMessages;
        }
    }
}
