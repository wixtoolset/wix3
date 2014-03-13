// <copyright file="ct.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset ClickThrough console.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;

    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The Windows Installer XML Toolset command-line fabricator for ClickThrough.
    /// </summary>
    internal sealed class ClickThroughBuilder
    {
        private StringCollection extensionOptions;
        private string extensionType;
        private ArrayList extensions;
        private SortedList extensionsByType;
        private string outputFile;
        private bool showLogo;
        private bool showHelp;
        private ConsoleMessageHandler messageHandler;

        /// <summary>
        /// Instantiate a new ClickThroughBuilder class.
        /// </summary>
        private ClickThroughBuilder()
        {
            this.extensionOptions = new StringCollection();
            this.extensions = new ArrayList();
            this.extensionsByType = new SortedList();
            this.messageHandler = new ConsoleMessageHandler("CTB", "ct.exe");
            this.showLogo = true;
        }

        /// <summary>
        /// The main entry point for ClickThroughBuilder.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            ClickThroughBuilder clickThroughBuilder = new ClickThroughBuilder();
            return clickThroughBuilder.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            StringCollection extensionList = new StringCollection();
            ClickThroughConsoleExtension extension = null;

            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // load the extension
                if (null != this.extensionType)
                {
                    extension = ClickThroughConsoleExtension.Load(this.extensionType);
                    extension.Fabricator.Message += new MessageEventHandler(this.messageHandler.Display);

                    // parse the extension's command line arguments
                    string[] extensionOptionsArray = new string[this.extensionOptions.Count];
                    this.extensionOptions.CopyTo(extensionOptionsArray, 0);

                    extension.ParseOptions(extensionOptionsArray);

                    // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                    if (this.messageHandler.EncounteredError)
                    {
                        return this.messageHandler.LastErrorNumber;
                    }
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(CTStrings.HelpMessage1);

                    // output the builder types alphabetically
                    if (null != extension)
                    {
                        SortedList builderTypes = new SortedList();
                        foreach (CommandLineOption commandLineOption in extension.CommandLineTypes)
                        {
                            builderTypes.Add(commandLineOption.Option, commandLineOption);
                        }

                        foreach (CommandLineOption commandLineOption in builderTypes.Values)
                        {
                            Console.WriteLine(String.Format(CultureInfo.CurrentUICulture, CTStrings.OptionFormat, commandLineOption.Option, commandLineOption.Description));
                        }
                    }

                    Console.WriteLine(CTStrings.HelpMessage2);
                    AppCommon.DisplayToolFooter();

                    return this.messageHandler.LastErrorNumber;
                }

                // build the output
                if (!extension.Fabricator.Fabricate(this.outputFile))
                {
                    return this.messageHandler.LastErrorNumber;
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
                if (null == arg || 0 == arg.Length) // skip blank arguments
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else if (parameter.StartsWith("sw"))
                    {
                        try
                        {
                            int suppressWarning = Convert.ToInt32(parameter.Substring(2), CultureInfo.InvariantCulture.NumberFormat);

                            if (0 >= suppressWarning)
                            {
                                this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
                            }

                            this.messageHandler.SuppressWarningMessage(suppressWarning);
                        }
                        catch (FormatException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
                        }
                        catch (OverflowException)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalSuppressWarningId(parameter.Substring(2)));
                        }
                    }
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("wx" == parameter)
                    {
                        this.messageHandler.WarningAsError = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                    }
                    else
                    {
                        this.extensionOptions.Add(arg);
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else if (null == this.extensionType)
                {
                    this.extensionType = arg;
                }
                else
                {
                    this.extensionOptions.Add(arg);
                }
            }

            if (null == this.extensionType || null == this.outputFile)
            {
                this.showHelp = true;
            }

            return;
        }
    }
}
