//-------------------------------------------------------------------------------------------------
// <copyright file="heat.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset Harvester application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The Windows Installer XML Toolset Harvester application.
    /// </summary>
    public sealed class Heat
    {
        private string extensionArgument;
        private StringCollection extensionOptions;
        private string extensionType;
        private ArrayList extensions;
        private SortedList extensionsByType;
        private string outputFile;
        private bool showLogo;
        private bool showHelp;
        private int indent;
        private ConsoleMessageHandler messageHandler;
        private HeatCore heatCore;

        /// <summary>
        /// Instantiate a new Heat class.
        /// </summary>
        private Heat()
        {
            this.extensionOptions = new StringCollection();
            this.extensions = new ArrayList();
            this.extensionsByType = new SortedList();
            this.messageHandler = new ConsoleMessageHandler("HEAT", "heat.exe");
            this.indent = 4;
            this.showLogo = true;
        }

        /// <summary>
        /// The main entry point for heat.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Heat heat = new Heat();
            return heat.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            StringCollection extensionList = new StringCollection();
            heatCore = new HeatCore(new MessageEventHandler(this.messageHandler.Display));

            HarvesterCore harvesterCore = new HarvesterCore(new MessageEventHandler(this.messageHandler.Display));
            heatCore.Harvester.Core = harvesterCore;
            heatCore.Mutator.Core = harvesterCore;

            try
            {
                // read the configuration file (heat.exe.config)
                AppCommon.ReadConfiguration(extensionList);

                // load any extensions
                foreach (string extensionType in extensionList)
                {
                    this.LoadExtension(extensionType);
                }

                // exit if there was an error loading an extension
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                // parse the command line
                this.ParseCommandLine(args);

                if (this.showHelp)
                {
                    return this.DisplayHelp();
                }

                // exit if there was an error parsing the core command line
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                // set the extension argument for use by all extensions
                harvesterCore.ExtensionArgument = this.extensionArgument;

                // parse the extension's command line arguments
                string[] extensionOptionsArray = new string[this.extensionOptions.Count];
                this.extensionOptions.CopyTo(extensionOptionsArray, 0);
                foreach (HeatExtension heatExtension in this.extensions)
                {
                    heatExtension.ParseOptions(this.extensionType, extensionOptionsArray);
                }

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                // harvest the output
                Wix.Wix wix = heatCore.Harvester.Harvest(this.extensionArgument);
                if (null == wix)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                // mutate the output
                if (!heatCore.Mutator.Mutate(wix))
                {
                    return this.messageHandler.LastErrorNumber;
                }

                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Indent = true;
                xmlSettings.IndentChars = new string(' ', this.indent);
                xmlSettings.OmitXmlDeclaration = true;

                string wixString;
                using (StringWriter stringWriter = new StringWriter())
                {
                    using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                    {
                        wix.OutputXml(xmlWriter);
                    }

                    wixString = stringWriter.ToString();
                }

                string mutatedWixString = heatCore.Mutator.Mutate(wixString);
                if (String.IsNullOrEmpty(mutatedWixString))
                {
                    return this.messageHandler.LastErrorNumber;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(this.outputFile));

                using (StreamWriter streamWriter = new StreamWriter(this.outputFile, false, System.Text.Encoding.UTF8))
                {
                    xmlSettings.OmitXmlDeclaration = false;
                    xmlSettings.Encoding = System.Text.Encoding.UTF8;
                    using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, xmlSettings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.Flush();
                    }

                    streamWriter.WriteLine();
                    streamWriter.Write(mutatedWixString);
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
        /// Shows the help screen.
        /// </summary>
        /// <returns>Returns the last error found in the message handler.</returns>
        private int DisplayHelp()
        {
            AppCommon.DisplayToolHeader();

            Console.WriteLine(HeatStrings.HelpMessageBegin);

            // output the harvest types alphabetically
            SortedList harvestOptions = new SortedList();
            foreach (HeatExtension heatExtension in this.extensions)
            {
                foreach (HeatCommandLineOption commandLineOption in heatExtension.CommandLineTypes)
                {
                    harvestOptions.Add(commandLineOption.Option, commandLineOption);
                }
            }

            harvestOptions.Add("-ext", new HeatCommandLineOption("-ext", HeatStrings.HelpMessageExtension));
            harvestOptions.Add("-nologo", new HeatCommandLineOption("-nologo", HeatStrings.HelpMessageNoLogo));
            harvestOptions.Add("-indent <N>", new HeatCommandLineOption("-indent <N>", HeatStrings.HelpMessageIndentation));
            harvestOptions.Add("-o[ut]", new HeatCommandLineOption("-out", HeatStrings.HelpMessageOut));
            harvestOptions.Add("-sw<N>", new HeatCommandLineOption("-sw<N>", HeatStrings.HelpMessageSuppressWarning));
            harvestOptions.Add("-swall", new HeatCommandLineOption("-swall", HeatStrings.HelpMessageSuppressAllWarnings));
            harvestOptions.Add("-v", new HeatCommandLineOption("-v", HeatStrings.HelpMessageVerbose));
            harvestOptions.Add("-wx[N]", new HeatCommandLineOption("-wx[N]", HeatStrings.HelpMessageTreatWarningAsError));
            harvestOptions.Add("-wxall", new HeatCommandLineOption("-wxall", HeatStrings.HelpMessageTreatAllWarningsAsErrors));

            foreach (HeatCommandLineOption commandLineOption in harvestOptions.Values)
            {
                if (!commandLineOption.Option.StartsWith("-"))
                {
                    Console.WriteLine(HeatStrings.HelpMessageOptionFormat, commandLineOption.Option, commandLineOption.Description);
                }
            }

            Console.WriteLine();
            Console.WriteLine(HeatStrings.HelpMessageOptionHeading);

            foreach (HeatCommandLineOption commandLineOption in harvestOptions.Values)
            {
                if (commandLineOption.Option.StartsWith("-"))
                {
                    Console.WriteLine(HeatStrings.HelpMessageOptionFormat, commandLineOption.Option, commandLineOption.Description);
                }
            }

            Console.WriteLine(HeatStrings.HelpMessageOptionFormat, "-? | -help", HeatStrings.HelpMessageThisHelpInfo);
            AppCommon.DisplayToolFooter();

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

                if (String.Equals(arg.Substring(1), "?", StringComparison.OrdinalIgnoreCase) || String.Equals(arg.Substring(1), "help", StringComparison.OrdinalIgnoreCase))
                {
                    this.showHelp = true;
                    return;
                }
                else if (0 == i)
                {
                    if ('@' == arg[0])
                    {
                        this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                    }
                    else
                    {
                        if (!arg.StartsWith("-"))
                        {
                            this.extensionType = arg;
                        }
                        else
                        {
                            this.messageHandler.Display(this, WixErrors.HarvestTypeNotFound(arg));
                        }
                    }
                }
                else if (1 == i)
                {
                    this.extensionArgument = arg;
                }
                else if ('-' == arg[0] || '/' == arg[0])
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
                    else if ("swall" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("swall", "sw"));
                        this.messageHandler.SuppressAllWarnings = true;
                    }
                    else if (parameter.StartsWith("sw"))
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
                    else if ("wxall" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("wxall", "wx"));
                        this.messageHandler.WarningAsError = true;
                    }
                    else if (parameter.StartsWith("wx"))
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
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                        }
                        else
                        {
                            this.LoadExtension(args[i]);
                        }
                    }
                    else if ("indent" == parameter)
                    {
                        try
                        {
                            this.indent = Int32.Parse(args[++i], CultureInfo.InvariantCulture);
                        }
                        catch
                        {
                            throw new ArgumentException("Invalid numeric argument.", parameter);
                        }
                    }
                }

                if ('@' != arg[0])
                {
                    this.extensionOptions.Add(arg);
                }
            }

            if (String.IsNullOrEmpty(this.extensionType))
            {
                this.showHelp = true;
            }
            else if (String.IsNullOrEmpty(this.extensionArgument))
            {
                this.messageHandler.Display(this, WixErrors.HarvestSourceNotSpecified());
            }
            else if (String.IsNullOrEmpty(this.outputFile))
            {
                this.messageHandler.Display(this, WixErrors.OutputTargetNotSpecified());
            }

            return;
        }

        private void LoadExtension(string extensionType)
        {
            HeatExtension heatExtension = HeatExtension.Load(extensionType);
            heatExtension.MessageHandler = this.messageHandler;

            this.extensions.Add(heatExtension);

            foreach (HeatCommandLineOption commandLineOption in heatExtension.CommandLineTypes)
            {
                if (this.extensionsByType.Contains(commandLineOption.Option))
                {
                    this.messageHandler.Display(this, WixErrors.DuplicateCommandLineOptionInExtension(commandLineOption.Option));
                    return;
                }

                this.extensionsByType.Add(commandLineOption.Option, heatExtension);
            }

            heatExtension.Core = heatCore;
        }
    }
}
