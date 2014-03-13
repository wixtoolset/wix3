//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.MdCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Command-line parsing.
    /// </summary>
    public class CommandLine
    {
        private CommandLine()
        {
            this.Files = new List<string>();
            this.Variables = new List<string>();
        }

        /// <summary>
        /// List of files to process.
        /// </summary>
        public List<string> Files { get; private set; }

        public List<string> Variables { get; private set; }

        public string Layout { get; set; }

        public Uri RelativeUri { get; set; }

        public string Output { get; set; }

        public static void ShowHelp()
        {
            Console.WriteLine("mdcompiler.exe [-?] [-relative folder] [-layout folder] [-d variable=value] [-out file] file1 file2 ... fileN");
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
            bool success = true;
            string relativePath = ".";

            commandLine = new CommandLine();

            for (int i = 0; i < args.Length; ++i)
            {
                if ('-' == args[i][0] || '/' == args[i][0])
                {
                    string arg = args[i].Substring(1).ToLowerInvariant();
                    if ("?" == arg || "help" == arg)
                    {
                        return false;
                    }
                    else if ("d" == arg || "define" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine("Missing variable definition for '-define' option. Provide a variable definition in the form of: name or name=value.");
                            success = false;
                        }
                        else
                        {
                            commandLine.Variables.Add(args[i]);
                        }
                    }
                    else if ("l" == arg || "layout" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine("Missing folder specification for '-layout' option. Provide a valid path to a folder.");
                            success = false;
                        }
                        else
                        {
                            string sourcePath = Path.GetFullPath(args[i]);
                            if (!Directory.Exists(sourcePath))
                            {
                                Console.Error.WriteLine("Layout folder '{0}' could not be found.", sourcePath);
                                success = false;
                            }
                            else
                            {
                                commandLine.Layout = sourcePath;
                            }
                        }
                    }
                    else if ("o" == arg || "out" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine("Missing file specification for '-out' option.");
                            success = false;
                        }
                        else
                        {
                            string outputPath = Path.GetFullPath(args[i]);
                            commandLine.Output = outputPath;
                        }
                    }
                    else if ("r" == arg || "relative" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine("Missing path specification for '-relative' option.");
                            success = false;
                        }
                        else
                        {
                            relativePath = args[i];
                        }
                    }
                }
                else
                {
                    string[] file = args[i].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    string sourcePath = Path.GetFullPath(file[0]);
                    if (!File.Exists(sourcePath))
                    {
                        Console.Error.WriteLine("Source file '{0}' could not be found.", sourcePath);
                        success = false;
                    }
                    else
                    {
                        commandLine.Files.Add(sourcePath);
                    }
                }
            }

            if (0 == commandLine.Files.Count)
            {
                Console.Error.WriteLine("No inputs specified. Specify at least one file.");
                success = false;
            }

            if (!String.IsNullOrEmpty(relativePath))
            {
                commandLine.RelativeUri = new Uri(Path.GetFullPath(relativePath + Path.DirectorySeparatorChar));
            }

            return success;
        }
    }
}
