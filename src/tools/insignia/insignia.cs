//-------------------------------------------------------------------------------------------------
// <copyright file="insignia.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Insignia inscriber application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// The main entry point for Insignia.
    /// </summary>
    public sealed class Insignia
    {
        private string bundlePath;
        private string bundleWithAttachedContainerPath;
        private StringCollection invalidArgs;
        private ConsoleMessageHandler messageHandler;
        private string msiPath;
        private string outputPath;
        private bool showHelp;
        private bool showLogo;
        private bool tidy;

        /// <summary>
        /// Instantiate a new Insignia class.
        /// </summary>
        private Insignia()
        {
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("INSG", "Insignia.exe");
            this.showLogo = true;
            this.tidy = true;
        }

        /// <summary>
        /// The main entry point for Insignia.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Insignia Insignia = new Insignia();
            return Insignia.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            bool inscribed = false;
            try
            {
                Inscriber inscriber = null;

                // parse the command line
                this.ParseCommandLine(args);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (String.IsNullOrEmpty(this.msiPath) && String.IsNullOrEmpty(this.bundlePath))
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(InsigniaStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                // Calculate the output path.
                string inputPath = String.IsNullOrEmpty(this.msiPath) ? Path.GetFullPath(this.bundlePath) : Path.GetFullPath(this.msiPath);
                if (String.IsNullOrEmpty(this.outputPath))
                {
                    this.outputPath = inputPath;
                }
                else if (this.outputPath.EndsWith("\\"))
                {
                    this.outputPath = Path.GetFullPath(Path.Combine(this.outputPath, Path.GetFileName(inputPath)));
                }

                inscriber = new Inscriber();
                inscriber.MessageHandler += new MessageEventHandler(this.messageHandler.Display);

                // Set the temp directory - if it's null, we'll default appropriately
                inscriber.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                if (!String.IsNullOrEmpty(this.msiPath))
                {
                    try
                    {
                        inscribed = inscriber.InscribeDatabase(inputPath, this.outputPath, this.tidy);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        this.messageHandler.Display(this, WixErrors.UnauthorizedAccess(inputPath));
                    }
                }
                else if (!String.IsNullOrEmpty(this.bundleWithAttachedContainerPath))
                {
                    inscribed = inscriber.InscribeBundle(this.bundleWithAttachedContainerPath, this.bundlePath, this.outputPath);
                }
                else
                {
                    inscribed = inscriber.InscribeBundleEngine(this.bundlePath, this.outputPath);
                }

                if (this.tidy)
                {
                    inscriber.DeleteTempFiles();
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

            // On success but nothing inscribed then return -1. Otherwise, return whatever last error number
            // was (which could be zero if successfully inscribed).
            return (0 == this.messageHandler.LastErrorNumber && !inscribed) ? -1 : this.messageHandler.LastErrorNumber;
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

                // skip blank arguments
                if (null == arg || 0 == arg.Length)
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else if ("ab" == parameter) // attach container to bundle
                    {
                        this.bundlePath = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);
                        this.bundleWithAttachedContainerPath = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);
                    }
                    else if ("ib" == parameter) // inscribe bundle
                    {
                        this.bundlePath = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);
                    }
                    else if ("im" == parameter) // inscribe msi
                    {
                        this.msiPath = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputPath = CommandLine.GetFileOrDirectory(parameter, this.messageHandler, args, ++i);
                    }
                    else if ("swall" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("swall", "sw"));
                        this.messageHandler.SuppressAllWarnings = true;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                this.messageHandler.SuppressAllWarnings = true;
                            }
                            else
                            {
                                int suppressWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= suppressWarning)
                                {
                                    this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(paramArg));
                                }

                                this.messageHandler.SuppressWarningMessage(suppressWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                        catch (OverflowException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                    }
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("wxall" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("wxall", "wx"));
                        this.messageHandler.WarningAsError = true;
                    }
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                this.messageHandler.WarningAsError = true;
                            }
                            else
                            {
                                int elevateWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= elevateWarning)
                                {
                                    this.messageHandler.Display(this, WixErrors.IllegalWarningIdAsError(paramArg));
                                }

                                this.messageHandler.ElevateWarningMessage(elevateWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                        catch (OverflowException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalWarningIdAsError(paramArg));
                        }
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
                    this.invalidArgs.Add(arg);
                }
            }
        }
    }
}
