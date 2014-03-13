//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.DocCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public enum CommandLineOperation
    {
        Compile,
    };

    /// <summary>
    /// Command-line parsing.
    /// </summary>
    public class CommandLine
    {
        private CommandLine()
        {
            this.Variables = new List<string>();
        }

        public CommandLineOperation Operation { get; private set; }

        public string InputFolder { get; private set; }

        public string OutputFolder { get; private set; }

        public string LayoutsFolder { get; private set; }

        public string AppendMarkdownTableOfContentsFile { get; private set; }

        public string HtmlHelpProjectFile { get; private set; }

        public bool IgnoreXsdSimpleTypeInTableOfContents { get; private set; }

        public List<string> Variables { get; private set; }

        public static void ShowHelp()
        {
            Console.WriteLine("doccompiler.exe [-?] [-htmlhelp project file] inputFolder outputFolder [layoutFolder]");
        }

        /// <summary>
        /// Parses the command-line.
        /// </summary>
        /// <param name="args">Arguments from command-line.</param>
        /// <param name="messaging">Messaging object to send errors.</param>
        /// <param name="commandLine">Command line object created from command-line arguments</param>
        /// <returns>True if command-line is parsed, false if a failure was occurred.</returns>
        public static bool TryParseArguments(string[] args, out CommandLine commandLine)
        {
            commandLine = new CommandLine();

            for (int i = 0; i < args.Length; ++i)
            {
                if ('-' == args[i][0] || '/' == args[i][0])
                {
                    string arg = args[i].Substring(1).ToLowerInvariant();
                    switch (arg)
                    {
                        case "?":
                        case "help":
                            return false;

                        case "d":
                        case "define":
                            ++i;
                            if (args.Length == i)
                            {
                                Console.Error.WriteLine("Missing variable definition for '-{0}' option. Provide a variable definition in the form of: name or name=value.", arg);
                                return false;
                            }
                            else
                            {
                                commandLine.Variables.Add(args[i]);
                            }
                            break;

                        case "appendmdtoc":
                            ++i;
                            if (args.Length == i)
                            {
                                Console.Error.WriteLine("Missing filename for '-{0}' option. Provide a file to append the table of contents in Markdown format.", arg);
                                return false;
                            }
                            else
                            {
                                commandLine.AppendMarkdownTableOfContentsFile = args[i];
                            }
                            break;

                        case "hh":
                        case "htmlhelp":
                            ++i;
                            if (args.Length == i)
                            {
                                Console.Error.WriteLine("Missing filename for '-{0}' option. Provide a file to output the html help project.", arg);
                                return false;
                            }
                            else
                            {
                                commandLine.HtmlHelpProjectFile = args[i];
                            }
                            break;

                        case "ignorexsdsimpletypeintoc":
                            commandLine.IgnoreXsdSimpleTypeInTableOfContents = true;
                            break;

                        default:
                            Console.Error.WriteLine("Unrecognized commandline parameter '{0}'.", arg);
                            return false;
                    }
                }
                else if (String.IsNullOrEmpty(commandLine.InputFolder))
                {
                    if (!System.IO.Directory.Exists(args[i]))
                    {
                        Console.Error.WriteLine("Input folder '{0}' could not be found.", args[i]);
                        return false;
                    }
                    else
                    {
                        commandLine.InputFolder = Path.GetFullPath(args[i] + Path.DirectorySeparatorChar);
                    }
                }
                else if (String.IsNullOrEmpty(commandLine.OutputFolder))
                {
                    commandLine.OutputFolder = Path.GetFullPath(args[i] + Path.DirectorySeparatorChar);
                }
                else if (String.IsNullOrEmpty(commandLine.LayoutsFolder))
                {
                    if (!System.IO.Directory.Exists(args[i]))
                    {
                        Console.Error.WriteLine("Layouts folder '{0}' could not be found.", args[i]);
                        return false;
                    }
                    else
                    {
                        commandLine.LayoutsFolder = Path.GetFullPath(args[i] + Path.DirectorySeparatorChar);
                    }
                }
            }

            if (String.IsNullOrEmpty(commandLine.InputFolder))
            {
                Console.Error.WriteLine("Input folder must be provided.");
                return false;
            }

            if (String.IsNullOrEmpty(commandLine.OutputFolder))
            {
                Console.Error.WriteLine("Output folder must be provided.");
                return false;
            }

            return true;
        }
    }
}
