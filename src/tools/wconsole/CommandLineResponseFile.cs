// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Common utilities for Wix command-line processing.
    /// </summary>
    public static class CommandLineResponseFile
    {
        /// <summary>
        /// Parses a response file.
        /// </summary>
        /// <param name="responseFile">The file to parse.</param>
        /// <returns>The array of arguments.</returns>
        public static string[] Parse(string responseFile)
        {
            string arguments;

            using (StreamReader reader = new StreamReader(responseFile))
            {
                arguments = reader.ReadToEnd();
            }

            return CommandLineResponseFile.ParseArgumentsToArray(arguments);
        }

        /// <summary>
        /// Parses an argument string into an argument array based on whitespace and quoting.
        /// </summary>
        /// <param name="arguments">Argument string.</param>
        /// <returns>Argument array.</returns>
        public static string[] ParseArgumentsToArray(string arguments)
        {
            // Scan and parse the arguments string, dividing up the arguments based on whitespace.
            // Unescaped quotes cause whitespace to be ignored, while the quotes themselves are removed.
            // Quotes may begin and end inside arguments; they don't necessarily just surround whole arguments.
            // Escaped quotes and escaped backslashes also need to be unescaped by this process.

            // Collects the final list of arguments to be returned.
            List<string> argsList = new List<string>();

            // True if we are inside an unescaped quote, meaning whitespace should be ignored.
            bool insideQuote = false;

            // Index of the start of the current argument substring; either the start of the argument
            // or the start of a quoted or unquoted sequence within it.
            int partStart = 0;

            // The current argument string being built; when completed it will be added to the list.
            StringBuilder arg = new StringBuilder();

            for (int i = 0; i <= arguments.Length; i++)
            {
                if (i == arguments.Length || (Char.IsWhiteSpace(arguments[i]) && !insideQuote))
                {
                    // Reached a whitespace separator or the end of the string.

                    // Finish building the current argument.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // Skip over the whitespace character.
                    partStart = i + 1;

                    // Add the argument to the list if it's not empty.
                    if (arg.Length > 0)
                    {
                        argsList.Add(CommandLineResponseFile.ExpandEnvVars(arg.ToString()));
                        arg.Length = 0;
                    }
                }
                else if (i > partStart && arguments[i - 1] == '\\')
                {
                    // Check the character following an unprocessed backslash.
                    // Unescape quotes, and backslashes followed by a quote.
                    if (arguments[i] == '"' || (arguments[i] == '\\' && arguments.Length > i + 1 && arguments[i + 1] == '"'))
                    {
                        // Unescape the quote or backslash by skipping the preceeding backslash.
                        arg.Append(arguments.Substring(partStart, i - 1 - partStart));
                        arg.Append(arguments[i]);
                        partStart = i + 1;
                    }
                }
                else if (arguments[i] == '"')
                {
                    // Add the quoted or unquoted section to the argument string.
                    arg.Append(arguments.Substring(partStart, i - partStart));

                    // And skip over the quote character.
                    partStart = i + 1;

                    insideQuote = !insideQuote;
                }
            }

            return argsList.ToArray();
        }

        /// <summary>
        /// Expand enxironment variables contained in the passed string
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        static private string ExpandEnvVars(string arguments)
        {
            IDictionary id = Environment.GetEnvironmentVariables();

            Regex regex = new Regex("(?<=\\%)(?:[\\w\\.]+)(?=\\%)");
            MatchCollection matches = regex.Matches(arguments);

            string value = String.Empty;
            for (int i = 0; i <= (matches.Count - 1); i++)
            {
                try
                {
                    string key = matches[i].Value;
                    regex = new Regex(String.Concat("(?i)(?:\\%)(?:" , key , ")(?:\\%)"));
                    value = id[key].ToString();
                    arguments = regex.Replace(arguments, value);
                }
                catch (NullReferenceException)
                {
                    // Collapse unresolved environment variables.
                    arguments = regex.Replace(arguments, value);
                }
            }

            return arguments;
        }
    }
}
