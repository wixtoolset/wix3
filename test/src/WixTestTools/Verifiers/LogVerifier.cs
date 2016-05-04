// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Verifiers
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using Xunit;

    /// <summary>
    /// The LogVerifier can verify a log file for given regular expressions.
    /// </summary>
    public class LogVerifier
    {
        // Member Variables
        private string logFile;

        /// <summary>
        /// Prevent creation of LogVerifier without log file
        /// </summary>
        private LogVerifier()
        { }

        /// <summary>
        /// Constructor for log files where the exact file name is known.
        /// </summary>
        /// <param name="fileName">The full path to the log file</param>
        public LogVerifier(string fileName)
        {
            if (null == fileName)
                throw new ArgumentNullException("fileName");

            if (!File.Exists(fileName))
                throw new ArgumentException(String.Format(@"File doesn't exist:{0}", fileName), "fileName");

            logFile = fileName;
        }

        /// <summary>
        /// Constructor for log files where the exact file name is known.
        /// </summary>
        /// <param name="directory">The directory in which the log file is located.</param>
        /// <param name="fileName">The name of the log file.</param>
        public LogVerifier(string directory, string fileName)
            : this(Path.Combine(directory, fileName))
        { }

        /// <summary>
        /// Scans a log file line by line until the regex pattern is matched or eof is reached.
        /// This method would be used in the case where the log file is very large, the regex doesn't
        /// span multiple lines, and only one match is required.
        /// </summary>
        /// <param name="regex">A regular expression</param>
        /// <returns>True if a match is found, False otherwise.</returns>
        public bool LineByLine(Regex regex)
        {
            string line = string.Empty;
            StreamReader sr = new StreamReader(logFile);

            // Read from a file stream line by line.
            while ((line = sr.ReadLine()) != null)
            {
                if (regex.Match(line).Success)
                {
                    sr.Close();
                    sr.Dispose();
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Scans a log file line by line until the regex pattern is matched or eof is reached.
        /// This method would be used in the case where the log file is very large, the regex doesn't
        /// span multiple lines, and only one match is required.
        /// No RegexOptions are used and matches are case sensitive.
        /// </summary>
        /// <param name="regex">A regular expression string.</param>
        /// <returns>True if a match is found, False otherwise.</returns>
        public bool LineByLine(string regex)
        {
            return LineByLine(new Regex(regex));
        }


        /// <summary>
        /// Scans a log file for matches to the regex.
        /// </summary>
        /// <param name="regex">A regular expression</param>
        /// <returns>The number of matches</returns>
        public int EntireFileAtOnce(Regex regex)
        {
            string logFileText = this.ReadLogFile();
            return regex.Matches(logFileText).Count;
        }

        /// <summary>
        /// Scans a log file for matches to the regex.
        /// </summary>
        /// <param name="regex">A regular expression</param>
        /// <returns>The number of matches</returns>
        public bool EntireFileAtOncestr(string regex)
        {
            string logFileText = this.ReadLogFile();
            return logFileText.Contains(regex);
        }
        /// <summary>
        /// Scans a log file for matches to the regex string.
        /// Only the Multiline RegexOption is used and matches are case sensitive.
        /// </summary>
        /// <param name="regex">A regular expression</param>
        /// <returns>The number of matches</returns>
        public int EntireFileAtOnce(string regex)
        {
            return EntireFileAtOnce(new Regex(regex, RegexOptions.Multiline));
        }

        /// <summary>
        /// Scans a log file for matches to the regex string.
        /// </summary>
        /// <param name="regex">A regular expression</param>
        /// <param name="ignoreCase">Specify whether to perform case sensitive matches</param>
        /// <returns>The number of matches</returns>
        public int EntireFileAtOnce(string regex, bool ignoreCase)
        {
            if (!ignoreCase)
                return EntireFileAtOnce(new Regex(regex, RegexOptions.Multiline));
            else
                return EntireFileAtOnce(new Regex(regex, RegexOptions.Multiline | RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is not found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextInLog(string regex, bool ignoreCase)
        {
            Assert.True(EntireFileAtOncestr(regex),
                String.Format("The log does not contain a match to the regular expression \"{0}\" ", regex));
        }

        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is not found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextInLog(Regex regex, bool ignoreCase)
        {
            Assert.True(EntireFileAtOnce(regex) >= 1,
                String.Format("The log does not contain a match to the regular expression \"{0}\" ", regex.ToString()));
        }

        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is not found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextInLog(string regex)
        {
            AssertTextInLog(regex, true);
        }

        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is not found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextInLog(Regex regex)
        {
            AssertTextInLog(regex, true);
        }


        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is  found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextNotInLog(Regex regex, bool ignoreCase)
        {
            Assert.True(EntireFileAtOnce(regex) < 1,
                String.Format("The log contain a match to the regular expression \"{0}\" ", regex.ToString()));
        }

        /// <summary>
        /// Search through the log and Assert.Fail() if a specified string is not found.
        /// </summary>
        /// <param name="regex">Search expression</param>
        /// <param name="ignoreCase">Perform case insensitive match</param>
        public void AssertTextNotInLog(string regex, bool ignoreCase)
        {
            Assert.False(EntireFileAtOncestr(regex),
                String.Format("The log does not contain a match to the regular expression \"{0}\" ", regex));
        }

        /// <summary>
        /// Checks if a meesage is in a file
        /// </summary>
        /// <param name="logFileName">The full path to the log file</param>
        /// <param name="message">Search expression</param>
        /// <returns>True if the message was found, false otherwise</returns>
        public static bool MessageInLogFile(string logFileName, string message)
        {
            LogVerifier logVerifier = new LogVerifier(logFileName);
            return logVerifier.EntireFileAtOncestr(message);
        }

        /// <summary>
        /// Checks if a meesage is in a file
        /// </summary>
        /// <param name="logFileName">The full path to the log file</param>
        /// <param name="message">Search expression (regex)</param>
        /// <returns>True if the message was found, false otherwise</returns>
        public static bool MessageInLogFileRegex(string logFileName, string regexMessage)
        {
            LogVerifier logVerifier = new LogVerifier(logFileName);
            return logVerifier.EntireFileAtOnce(regexMessage) > 0;
        }

        /// <summary>
        /// Read in the entire log file at once.
        /// </summary>
        /// <returns>Contents of log file.</returns>
        private string ReadLogFile()
        {
            // Retry a few times.
            for (int retry = 0; ; ++retry)
            {
                try
                {
                    using (StreamReader sr = new StreamReader(this.logFile))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch // we'll catch everything a few times until we give up.
                {
                    if (retry > 4)
                    {
                        throw;
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}
