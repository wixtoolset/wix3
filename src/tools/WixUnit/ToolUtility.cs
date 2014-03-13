//-------------------------------------------------------------------------------------------------
// <copyright file="ToolUtility.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utilities for working with tools.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Utilities for working with tools.
    /// </summary>
    internal sealed class ToolUtility
    {
        private static Regex wixErrorMessage = new Regex(@"^.*: error [^:\d]*(?<errorNumber>\d*).*:.*$", RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex wixWarningMessage = new Regex(@"^.*: warning [^:\d]*(?<warningNumber>\d*).*:.*$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private ToolUtility()
        {
        }

        /// <summary>
        /// Get the unexpected errors and warnings from an ArrayList of output strings.
        /// </summary>
        /// <param name="output">The output strings.</param>
        /// <param name="expectedErrors">The expected errors, semicolon delimited.</param>
        /// <param name="expectedWarnings">The expected warnings, semicolon delimited.</param>
        /// <returns>The unexpected warnings and errors.</returns>
        public static ArrayList GetErrors(ArrayList output, string expectedErrors, string expectedWarnings)
        {
            Hashtable expectedErrorNumbers = new Hashtable();
            Hashtable expectedWarningNumbers = new Hashtable();
            ArrayList errors = new ArrayList();

            if (expectedErrors.Length > 0)
            {
                foreach (string error in expectedErrors.Split(';'))
                {
                    int errorNumber = Convert.ToInt32(error, CultureInfo.InvariantCulture);

                    expectedErrorNumbers.Add(errorNumber, null);
                }
            }

            if (expectedWarnings.Length > 0)
            {
                foreach (string warning in expectedWarnings.Split(';'))
                {
                    int warningNumber = Convert.ToInt32(warning, CultureInfo.InvariantCulture);

                    expectedWarningNumbers.Add(warningNumber, null);
                }
            }

            bool treatAllLinesAsErrors = false;
            foreach (string line in output)
            {
                if (treatAllLinesAsErrors)
                {
                    errors.Add(line);
                }
                else
                {
                    Match errorMatch = wixErrorMessage.Match(line);
                    Match warningMatch = wixWarningMessage.Match(line);

                    if (errorMatch.Success)
                    {
                        int errorNumber = 0;
                        Int32.TryParse(errorMatch.Groups["errorNumber"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out errorNumber);
                        
                        // error number 1 is special because it includes a stack trace which much be kept in the error output
                        if (errorNumber == 1)
                        {
                            treatAllLinesAsErrors = true;
                        }

                        if (expectedErrorNumbers.Contains(errorNumber))
                        {
                            expectedErrorNumbers[errorNumber] = String.Empty;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                    else if (line.StartsWith("Unhandled Exception:")) // .NET error
                    {
                        errors.Add(line);
                    }

                    if (warningMatch.Success)
                    {
                        int warningNumber = 0;
                        Int32.TryParse(warningMatch.Groups["warningNumber"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out warningNumber);

                        if (expectedWarningNumbers.Contains(warningNumber))
                        {
                            expectedWarningNumbers[warningNumber] = String.Empty;
                        }
                        else
                        {
                            errors.Add(line);
                        }
                    }
                }
            }

            foreach (DictionaryEntry entry in expectedErrorNumbers)
            {
                if (entry.Value == null)
                {
                    errors.Add(String.Format(CultureInfo.InvariantCulture, "Expected error {0} not found.", entry.Key));
                }
            }

            foreach (DictionaryEntry entry in expectedWarningNumbers)
            {
                if (entry.Value == null)
                {
                    errors.Add(String.Format(CultureInfo.InvariantCulture, "Expected warning {0} not found.", entry.Key));
                }
            }

            return errors;
        }

        /// <summary>
        /// Run a tool with the given file name and command line.
        /// </summary>
        /// <param name="toolFile">The tool's file name.</param>
        /// <param name="commandLine">The command line.</param>
        /// <returns>An ArrayList of output strings.</returns>
        public static ArrayList RunTool(string toolFile, string commandLine)
        {
            // The returnCode variable doesn't get used but it must be created to pass as an argument
            int returnCode;
            return ToolUtility.RunTool(toolFile, commandLine, out returnCode);
        }

        /// <summary>
        /// Run a tool with the given file name and command line.
        /// </summary>
        /// <param name="toolFile">The tool's file name.</param>
        /// <param name="commandLine">The command line.</param>
        /// <param name="returnCode">Store the return code of the process.</param>
        /// <returns>An ArrayList of output strings.</returns>
        public static ArrayList RunTool(string toolFile, string commandLine, out int returnCode)
        {
            // Expand environment variables
            toolFile = Environment.ExpandEnvironmentVariables(toolFile);
            commandLine = Environment.ExpandEnvironmentVariables(commandLine);

            ArrayList output = new ArrayList();
            Process process = null;

            // The returnCode must get initialized outside of the try block
            returnCode = 0;

            try
            {
                process = new Process();
                process.StartInfo.FileName = toolFile;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                // run the tool
                output.Add(String.Empty);
                output.Add(String.Format(CultureInfo.InvariantCulture, "Command: {0} {1}", toolFile, commandLine));
                process.StartInfo.Arguments = commandLine;
                process.Start();

                string line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    output.Add(line);
                }

                // WiX tools log all output to stdout but .NET may put error output in stderr
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    output.Add(line);
                }

                process.WaitForExit();
                returnCode = process.ExitCode;
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
            }

            return output;
        }
    }
}
