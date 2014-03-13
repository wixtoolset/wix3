//-------------------------------------------------------------------------------------------------
// <copyright file="nit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Lux unit-test framework test runner.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Lux
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The main entry point for Nit
    /// </summary>
    public sealed class Nit
    {
        private List<string> inputFiles = new List<string>();
        private List<string> invalidArgs = new List<string>();
        private bool showLogo = true;
        private bool showHelp;
        private ConsoleMessageHandler messageHandler = new ConsoleMessageHandler("NIT", "nit.exe");

        /// <summary>
        /// Prevents a default instance of the Nit class from being created.
        /// </summary>
        private Nit()
        {
        }

        /// <summary>
        /// The main entry point for Nit.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Nit nit = new Nit();
            return nit.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            try
            {
                // parse the command line
                this.ParseCommandLine(args);
                this.messageHandler.ShowVerboseMessages = true; // always verbose, to show passed tests

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(NitStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }

                this.invalidArgs = null;

                // gotta have something to do
                if (0 == this.inputFiles.Count)
                {
                    Console.WriteLine(NitStrings.HelpMessage);
                    this.messageHandler.Display(this, NitErrors.MalfunctionNeedInput());
                    return this.messageHandler.LastErrorNumber;
                }

                // run tests and report results
                TestRunner runner = new TestRunner();
                runner.InputFiles = this.inputFiles;
                runner.Message += this.messageHandler.Display;

                int failures = 0;
                int passes = 0;
                runner.RunTests(out passes, out failures);

                if (0 < failures)
                {
                    this.messageHandler.Display(this, NitErrors.TotalTestFailures(failures, passes));
                    return this.messageHandler.LastErrorNumber;
                }
                else
                {
                    this.messageHandler.Display(this, NitVerboses.OneHundredPercent(passes));
                }
            }
            catch (WixException we)
            {
                this.messageHandler.Display(this, we.Error);
            }
            catch (Exception e)
            {
                this.messageHandler.Display(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            return this.messageHandler.LastErrorNumber;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (null == arg || 0 == arg.Length)
                {
                    // skip blank arguments
                    continue;
                }

                if (1 == arg.Length)
                {
                    // treat '-' and '@' as filenames when by themselves.
                    this.inputFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else
                    {
                        this.invalidArgs.Add(parameter);
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    this.inputFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                }
            }

            return;
        }
    }
}