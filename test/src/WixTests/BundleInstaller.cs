// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Installs or uninstalls bundles.
    /// </summary>
    public class BundleInstaller
    {
        /// <param name="path">Path to the bundle.</param>
        public BundleInstaller(WixTests test, string path)
        {
            this.TestName = test.TestContext.TestName;
            this.Bundle = path;
        }

        /// <summary>
        /// Gets or sets the path to the bundle to install/uninstall.
        /// </summary>
        public string Bundle { get; set; }

        /// <summary>
        /// Gets or sets the name of the test using the installer.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>
        /// Gets the log file from the last install/uninstall.
        /// </summary>
        public string LastLogFile { get; private set; }

        /// <summary>
        /// Installs the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public BundleInstaller Install(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            this.LastLogFile = this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Install, arguments);
            return this;
        }

        /// <summary>
        /// Modify the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public BundleInstaller Modify(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            this.LastLogFile = this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Modify, arguments);
            return this;
        }

        /// <summary>
        /// Repairs the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public BundleInstaller Repair(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            this.LastLogFile = this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Repair, arguments);
            return this;
        }

        /// <summary>
        /// Uninstalls the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code, defaults to success.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        public BundleInstaller Uninstall(int expectedExitCode = (int)MSIExec.MSIExecReturnCode.SUCCESS, params string[] arguments)
        {
            this.LastLogFile = this.RunBundleWithArguments(expectedExitCode, MSIExec.MSIExecMode.Uninstall, arguments);
            return this;
        }

        /// <summary>
        /// Executes the bundle with optional arguments.
        /// </summary>
        /// <param name="expectedExitCode">Expected exit code.</param>
        /// <param name="mode">Install mode.</param>
        /// <param name="arguments">Optional arguments to pass to the tool.</param>
        /// <returns>Path to the generated log file.</returns>
        private string RunBundleWithArguments(int expectedExitCode, MSIExec.MSIExecMode mode, params string[] arguments)
        {
            TestTool bundle = new TestTool(this.Bundle, null);
            StringBuilder sb = new StringBuilder();

            // Be sure to run silent.
            sb.Append(" -quiet");

            // Generate the log file name.
            string logFile = Path.Combine(Path.GetTempPath(), String.Format("{0}_{1:yyyyMMddhhmmss}_{3}_{2}.log", this.TestName, DateTime.UtcNow, Path.GetFileNameWithoutExtension(this.Bundle), mode));
            sb.AppendFormat(" -log \"{0}\"", logFile);

            // Set operation.
            switch (mode)
            {
                case MSIExec.MSIExecMode.Modify:
                    sb.Append(" -modify");
                    break;

                case MSIExec.MSIExecMode.Repair:
                    sb.Append(" -repair");
                    break;

                case MSIExec.MSIExecMode.Uninstall:
                    sb.Append(" -uninstall");
                    break;
            }

            // Add additional arguments.
            if (null != arguments)
            {
                sb.Append(" ");
                sb.Append(String.Join(" ", arguments));
            }

            // Set the arguments.
            bundle.Arguments = sb.ToString();

            // Run the tool and assert the expected code.
            bundle.ExpectedExitCode = expectedExitCode;
            bundle.Run();

            // Return the log file name.
            return logFile;
        }
    }
}
