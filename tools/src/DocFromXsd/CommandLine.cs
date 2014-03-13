//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLine.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.DocFromXsd
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
        }

        /// <summary>
        /// List of files to process.
        /// </summary>
        public List<string> Files { get; private set; }

        /// <summary>
        /// Output folder.
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// Show the help text.
        /// </summary>
        public static void ShowHelp()
        {
            Console.WriteLine("DocFromXsd.exe [-?] -out folder xsd1 xsd2 ... xsdN");
        }

        /// <summary>
        /// Parses the command-line.
        /// </summary>
        /// <param name="args">Arguments from command-line.</param>
        /// <param name="commandLine">Command line object created from command-line arguments</param>
        /// <returns>True if command-line is parsed, false if a failure was occurred.</returns>
        public static bool TryParseArguments(string[] args, out CommandLine commandLine)
        {
            bool success = true;

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
                    else if ("o" == arg || "out" == arg)
                    {
                        ++i;
                        if (args.Length == i)
                        {
                            Console.Error.WriteLine(String.Format("Missing file specification for '-out' option."));
                            success = false;
                        }
                        else
                        {
                            string outputPath = Path.GetFullPath(args[i]);
                            commandLine.OutputFolder = outputPath;
                        }
                    }
                }
                else
                {
                    string[] file = args[i].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    string sourcePath = Path.GetFullPath(file[0]);
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        Console.Error.WriteLine(String.Format("Source file '{0}' could not be found.", sourcePath));
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
                Console.Error.WriteLine(String.Format("No inputs specified. Specify at least one file."));
                success = false;
            }

            if (String.IsNullOrEmpty(commandLine.OutputFolder))
            {
                Console.Error.WriteLine(String.Format("Ouput folder was not specified. Specify an output folder using the '-out' switch."));
                success = false;
            }

            return success;
        }
    }
}
