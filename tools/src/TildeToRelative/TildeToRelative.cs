// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Make proper relative paths by replacing the ~/ in all uri.
    /// </summary>
    public sealed class TildeToRelative
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================
        private static readonly Regex TildeUriHtml = new Regex(@"(?<tag><(a\s*href|img\s*src)(\s*=\s*)[""'])~[/\\](?<uri>[^""']+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture |  RegexOptions.IgnoreCase);
        private static readonly Regex TildeUriMD = new Regex(@"(?<tag>\]\()~[/\\](?<uri>[^)]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture |  RegexOptions.IgnoreCase);

        private bool showHelp;
        private string sourceDirectory;
        private string destinationDirectory;
        private List<string> extensionsToProcess = new List<string>{".html", ".htm", ".md"};

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="TildeToRelative"/> class.
        /// </summary>
        public TildeToRelative()
        {
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            TildeToRelative tildeToRelative = new TildeToRelative();
            return tildeToRelative.Run(args);
        }

        /// <summary>
        /// Make proper relative paths by replacing the ~/ in all uri.
        /// </summary>
        private void MakeRelative()
        {
            if (!Directory.Exists(this.sourceDirectory))
            {
                throw new Exception("Source directory does not exist");
            }
            if (!Directory.Exists(this.destinationDirectory))
            {
                Directory.CreateDirectory(this.destinationDirectory);
            }
            string prefix = "";
            ProcessDirectory(this.sourceDirectory, this.destinationDirectory, prefix);
        }
        
        private void ProcessDirectory(string sourceDir, string targetDir, string prefix)
        {
            foreach(string filename in Directory.GetFiles(sourceDir))
            {
                string newFile = Path.Combine(targetDir, Path.GetFileName(filename));
                if (ShouldProcessFile(filename))
                {
                    ProcessFile(filename, newFile, prefix, Path.GetExtension(filename).ToLowerInvariant() != ".md");
                }
                else
                {
                    System.Diagnostics.Trace.WriteLine("copy: " + prefix + ";" + filename + "; " + newFile);
                    File.Copy(filename, newFile);
                }
            }
            foreach(string fullDirectory in Directory.GetDirectories(sourceDir))
            {
                string directory = Path.GetFileName(fullDirectory);
                string newTarget = Path.Combine(targetDir, directory);
                System.Diagnostics.Trace.WriteLine("subDir: " + directory + ";" + newTarget);
                if (!Directory.Exists(newTarget))
                {
                    Directory.CreateDirectory(newTarget);
                }
                ProcessDirectory(Path.Combine(sourceDir, directory), newTarget, prefix + "../");
            }
        }

        private void ProcessFile(string input, string output, string prefix, bool htmlOnly)
        {
            System.Diagnostics.Trace.WriteLine("process: " + prefix + ";" + input + "; " + output);
            string text = File.ReadAllText(input);
            string outputText = TildeUriHtml.Replace(text, "${tag}" + prefix + "${uri}");
            if (!htmlOnly)
            {
                outputText = TildeUriMD.Replace(outputText, "${tag}" + prefix + "${uri}");
            }
            if (!string.Equals(text, outputText))
            {
                System.Diagnostics.Trace.WriteLine("  tildes replaced!");
            }
            File.WriteAllText(output, outputText);
        }

        private bool ShouldProcessFile(string filename)
        {
            string extension = Path.GetExtension(filename);
            return extensionsToProcess.Any(_ => string.Equals(_, extension, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            foreach(var arg in args)
            { 
                if (string.IsNullOrEmpty(arg))
                    continue;

                if (arg.StartsWith("/") || arg.StartsWith("-"))
                {
                    var opt = arg.Substring(1);

                    switch(opt.ToLowerInvariant())
                    {
                        case "?":
                        case "h":
                        case "help":
                            this.showHelp = true;
                            break;
                        default:
                            throw new ArgumentException("Invalid argument.", opt);
                    }
                }
                else if (this.sourceDirectory == null)
                {
                    this.sourceDirectory = Path.GetFullPath(arg);
                    if (!Directory.Exists(this.sourceDirectory))
                        throw new ArgumentException("Source directory does not exist.");
                }
                else if (this.destinationDirectory == null)
                {
                    this.destinationDirectory = arg;
                    if (string.Equals(this.sourceDirectory, this.destinationDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Source directory should be different from target directory.");
                    }
                }
                else
                {
                    throw new ArgumentException("Invalid argument.", arg);
                }
            }
        }

        /// <summary>
        /// Run the application with the given arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        private int Run(string[] args)
        {
            try
            {
                this.ParseCommandLine(args);

                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  TildeToRelative.exe sourceDirectory destinationDirectory");
                    Console.WriteLine();
                    Console.WriteLine("   -?                  this help information");
                    Console.WriteLine();

                    return 0;
                }

                this.MakeRelative();
            }
            catch (Exception e)
            {
                Console.WriteLine("TildeToRelative.exe : fatal error TTR0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

                return 1;
            }

            return 0;
        }
    }
}
