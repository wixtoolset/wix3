//-----------------------------------------------------------------------
// <copyright file="WixTool.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Wraps a WiX executable</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using WixTest.Utilities;

    /// <summary>
    /// A base class for a Wix tool, eg. candle.exe
    /// </summary>
    public abstract partial class WixTool : TestTool
    {
        /// <summary>
        /// The expected WiX messages
        /// </summary>
        private List<WixMessage> expectedWixMessages = new List<WixMessage>();

        /// <summary>
        /// Ignore WixMessages that were not expected
        /// </summary>
        private bool ignoreExtraWixMessages;

        /// <summary>
        /// Ignore the order in which WixMessages are printed
        /// </summary>
        private bool ignoreWixMessageOrder;

        /// <summary>
        /// If true, the output for this tool will be set to temp
        /// </summary>
        private bool setOutputFileIfNotSpecified;

        /// <summary>
        /// Constructor for a WixTool. Uses the default tool directory.
        /// </summary>
        /// <param name="toolName">The name of the tool. Eg. candle.exe</param>
        /// <param name="workingDirectory">The working directory of the tool.</param>
        public WixTool(string toolName, string workingDirectory)
            : this(Environment.ExpandEnvironmentVariables(Settings.WixToolDirectory), toolName, workingDirectory)
        {
            if (String.IsNullOrEmpty(Settings.WixToolDirectory))
            {
                throw new ArgumentException(
                    "{0} must be initialized to the WiX tools directory. Use '.' to specify the current directory.",
                    "WixTest.Settings.WixToolDirectory");
            }
        }

        /// <summary>
        /// Constructor for a WixTool.
        /// </summary>
        /// <param name="toolDirectory">The directory of the tool.</param>
        /// <param name="toolName">The name of the tool. Eg. candle.exe</param>
        /// <param name="workingDirectory">The working directory of the tool.</param>
        public WixTool(string toolDirectory, string toolName, string workingDirectory)
            : base(Path.Combine(toolDirectory, toolName))
        {
            this.SetBaseDefaultArguments();
            this.SetDefaultArguments();
            this.ExpectedExitCode = 0;
            this.IgnoreExtraWixMessages = false;
            this.IgnoreWixMessageOrder = false;
            this.SetOutputFileIfNotSpecified = true;
            this.WorkingDirectory = workingDirectory;
        }

        /// <summary>
        /// The expected WiX messages
        /// </summary>
        public List<WixMessage> ExpectedWixMessages
        {
            get { return this.expectedWixMessages; }
            set { this.expectedWixMessages = value; }
        }

        /// <summary>
        /// Ignore WixMessages that were not expected
        /// </summary>
        public bool IgnoreExtraWixMessages
        {
            get { return this.ignoreExtraWixMessages; }
            set { this.ignoreExtraWixMessages = value; }
        }

        /// <summary>
        /// Ignore the order in which WixMessages are printed
        /// </summary>
        public bool IgnoreWixMessageOrder
        {
            get { return this.ignoreWixMessageOrder; }
            set { this.ignoreWixMessageOrder = value; }
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected abstract string OutputFileExtension
        {
            get;
        }

        /// <summary>
        /// If true, the output for this tool will be set to temp
        /// </summary>
        public bool SetOutputFileIfNotSpecified
        {
            get { return this.setOutputFileIfNotSpecified; }
            set { this.setOutputFileIfNotSpecified = value; }
        }

        /// <summary>
        /// Checks that the result from a run matches the expected results
        /// </summary>
        /// <param name="result">A result from a run</param>
        /// <returns>A list of errors</returns>
        public override List<string> CheckResult(Result result)
        {
            List<string> errors = new List<string>();
            errors.AddRange(base.CheckResult(result));

            // Verify that the expected messages are present
            if (this.IgnoreWixMessageOrder)
            {
                errors.AddRange(this.UnorderedWixMessageVerification(result.StandardOutput));
            }
            else
            {
                errors.AddRange(this.OrderedWixMessageVerification(result.StandardOutput));
            }

            return errors;
        }

        /// <summary>
        /// Clears all of the expected results and resets them to the default values
        /// </summary>
        public override void SetDefaultExpectedResults()
        {
            base.SetDefaultExpectedResults();

            this.ExpectedWixMessages = new List<WixMessage>();
        }

        /// <summary>
        /// Sets the OutputFile to a default value if it is not set 
        /// </summary>
        protected virtual void SetDefaultOutputFile()
        {
            if (String.IsNullOrEmpty(this.OutputFile))
            {
                string outputFileName = String.Concat("test", this.OutputFileExtension);
                string outputDirectoryName = FileUtilities.GetUniqueFileName();
                this.OutputFile = Path.Combine(outputDirectoryName, outputFileName);
            }
        }

        /// <summary>
        /// Helper method for finding all the errors and all the warnings in the output
        /// </summary>
        /// <returns>A list of WixMessages in the output</returns>
        private List<WixMessage> FindActualWixMessages(string output)
        {
            List<WixMessage> actualWixMessages = new List<WixMessage>();

            foreach (string line in output.Split('\n', '\r'))
            {
                WixMessage actualWixMessage = WixMessage.FindWixMessage(line);

                if (null != actualWixMessage)
                {
                    actualWixMessages.Add(actualWixMessage);
                }
            }

            return actualWixMessages;
        }

        /// <summary>
        /// Perform ordered verification of the list of WixMessages
        /// </summary>
        /// <param name="output">The standard output</param>
        /// <returns>A list of errors encountered during verification</returns>
        private List<string> OrderedWixMessageVerification(string output)
        {
            List<string> errors = new List<string>();

            if (null == this.ExpectedWixMessages)
            {
                return errors;
            }

            List<WixMessage> actualWixMessages = this.FindActualWixMessages(output);

            for (int i = 0; i < this.ExpectedWixMessages.Count; i++)
            {
                // If the expectedMessage does not have any specified MessageText then ignore it in a comparison
                bool ignoreText = String.IsNullOrEmpty(this.ExpectedWixMessages[i].MessageText);

                if (i >= this.ExpectedWixMessages.Count)
                {
                    // there are more actual WixMessages than expected
                    break;
                }
                else if (i >= actualWixMessages.Count || 0 != WixMessage.Compare(actualWixMessages[i], this.ExpectedWixMessages[i], ignoreText))
                {
                    errors.Add(String.Format("Ordered WixMessage verification failed when trying to find the expected message {0}", expectedWixMessages[i]));
                    break;
                }
                else
                {
                    // the expected WixMessage was found
                    actualWixMessages[i] = null;
                }
            }

            if (!this.IgnoreExtraWixMessages)
            {
                // Now go through the messages that were found but that weren't expected
                foreach (WixMessage actualWixMessage in actualWixMessages)
                {
                    if (null != actualWixMessage)
                    {
                        errors.Add(String.Format("Found an unexpected message: {0}", actualWixMessage.ToString()));
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Run a WixTool
        /// </summary>
        /// <param name="exceptionOnError">If true, throw an exception when expected results don't match actual results.</param>
        /// <returns>The results of the run.</returns>
        public override Result Run(bool exceptionOnError)
        {
            // set working directory
            string originalWorkingdirectory = string.Empty;
            if (!string.IsNullOrEmpty(this.WorkingDirectory))
            {
                originalWorkingdirectory = FileUtilities.SetWorkingDirectory(WorkingDirectory);
            }

            // set output file
            if (this.SetOutputFileIfNotSpecified)
            {
                this.SetDefaultOutputFile();
            }

            // run tool
            Result result = base.Run(exceptionOnError);

            // revert original directory
            if (!string.IsNullOrEmpty(originalWorkingdirectory))
            {
                FileUtilities.SetWorkingDirectory(originalWorkingdirectory);
            }

            return result;
        }

        /// <summary>
        /// Perform unordered verification of the list of WixMessages
        /// </summary>
        /// <param name="output">The standard output</param>
        /// <returns>A list of errors encountered during verification</returns>
        private List<string> UnorderedWixMessageVerification(string output)
        {
            List<string> errors = new List<string>();

            if (null == this.ExpectedWixMessages)
            {
                return errors;
            }

            List<WixMessage> actualWixMessages = this.FindActualWixMessages(output);

            for (int i = 0; i < this.ExpectedWixMessages.Count; i++)
            {
                // If the expectedMessage does not have any specified MessageText then ignore it in a comparison
                bool ignoreText = String.IsNullOrEmpty(this.ExpectedWixMessages[i].MessageText);

                // Flip this bool to true if the expected message is in the list of actual message that were printed
                bool expectedMessageWasFound = false;

                for (int j = 0; j < actualWixMessages.Count; j++)
                {
                    if (null != actualWixMessages[j] && 0 == WixMessage.Compare(actualWixMessages[j], this.ExpectedWixMessages[i], ignoreText))
                    {
                        // Invalidate the message from the list of found errors by setting it to null
                        actualWixMessages[j] = null;

                        expectedMessageWasFound = true;
                    }
                }

                // Check if the expected message was found in the list of actual messages
                if (!expectedMessageWasFound)
                {
                    errors.Add(String.Format("Could not find the expected message: {0}", this.ExpectedWixMessages[i].ToString()));

                    if (String.IsNullOrEmpty(this.ExpectedWixMessages[i].MessageText))
                    {
                        errors.Add("  When unordered WixMessage verification is performed, WixMessage text is not ignored");
                    }
                }
            }

            if (!this.IgnoreExtraWixMessages)
            {
                // Now go through the messages that were found but that weren't expected
                foreach (WixMessage actualWixMessage in actualWixMessages)
                {
                    if (null != actualWixMessage)
                    {
                        errors.Add(String.Format("Found an unexpected message: {0}", actualWixMessage.ToString()));
                    }
                }
            }

            return errors;
        }
    }
}
