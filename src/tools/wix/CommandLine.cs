// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Common utilities for Wix command-line processing.
    /// </summary>
    public static class CommandLine
    {
        /// <summary>
        /// Validates that a valid string parameter (without "/" or "-"), and returns a bool indicating its validity
        /// </summary>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be validated.</param>
        /// <returns>True if a valid string parameter exists there, false if not.</returns>
        public static bool IsValidArg(string[] args, int index)
        {
            if (args.Length <= index || String.IsNullOrEmpty(args[index]) || '/' == args[index][0] || '-' == args[index][0])
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="path">The path to test.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(ConsoleMessageHandler messageHandler, string path)
        {
            return VerifyPath(messageHandler, path, false);
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="path">The path to test.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The full path if it is valid, null if it is invalid.</returns>
        public static string VerifyPath(ConsoleMessageHandler messageHandler, string path, bool allowPrefix)
        {
            string fullPath;

            if (0 <= path.IndexOf('\"'))
            {
                messageHandler.Display(null, WixErrors.PathCannotContainQuote(path));
                return null;
            }

            try
            {
                string prefix = null;
                if (allowPrefix)
                {
                    int prefixLength = path.IndexOf('=') + 1;
                    if (0 != prefixLength)
                    {
                      prefix = path.Substring(0, prefixLength);
                      path = path.Substring(prefixLength);
                    }
                }

                if (String.IsNullOrEmpty(prefix))
                {
                    fullPath = Path.GetFullPath(path);
                }
                else
                {
                    fullPath = String.Concat(prefix, Path.GetFullPath(path));
                }
            }
            catch (Exception e)
            {
                messageHandler.Display(null, WixErrors.InvalidCommandLineFileName(path, e.Message));
                return null;
            }

            return fullPath;
        }

        /// <summary>
        /// Validates that a commandline parameter is a valid file or directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetFileOrDirectory(string commandlineSwitch, ConsoleMessageHandler messageHandler, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                messageHandler.Display(null, WixErrors.FileOrDirectoryPathRequired(commandlineSwitch));
                return null;
            }

            return VerifyPath(messageHandler, args[index]);
        }

        /// <summary>
        /// Validates that a string is a valid directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetDirectory(string commandlineSwitch, ConsoleMessageHandler messageHandler, string[] args, int index)
        {
            return GetDirectory(commandlineSwitch, messageHandler, args, index, false);
        }

        /// <summary>
        /// Validates that a string is a valid directory name, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <param name="allowPrefix">Indicates if a colon-delimited prefix is allowed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetDirectory(string commandlineSwitch, ConsoleMessageHandler messageHandler, string[] args, int index, bool allowPrefix)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                messageHandler.Display(null, WixErrors.DirectoryPathRequired(commandlineSwitch));
                return null;
            }

            if (File.Exists(args[index]))
            {
                messageHandler.Display(null, WixErrors.ExpectedDirectoryGotFile(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(messageHandler, args[index], allowPrefix);
        }

        /// <summary>
        /// Validates that a string is a valid filename, and throws appropriate warnings/errors if not
        /// </summary>
        /// <param name="commandlineSwitch">The commandline switch we're parsing (for error display purposes).</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="args">The list of strings to check.</param>
        /// <param name="index">The index (in args) of the commandline parameter to be parsed.</param>
        /// <returns>The string if it is valid, null if it is invalid.</returns>
        public static string GetFile(string commandlineSwitch, ConsoleMessageHandler messageHandler, string[] args, int index)
        {
            commandlineSwitch = String.Concat("-", commandlineSwitch);

            if (!IsValidArg(args, index))
            {
                messageHandler.Display(null, WixErrors.FilePathRequired(commandlineSwitch));
                return null;
            }

            if (Directory.Exists(args[index]))
            {
                messageHandler.Display(null, WixErrors.ExpectedFileGotDirectory(commandlineSwitch, args[index]));
                return null;
            }

            return VerifyPath(messageHandler, args[index]);
        }
    }
}
