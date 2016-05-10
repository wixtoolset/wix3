// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Entry point for decompiler
    /// </summary>
    public sealed class Dark
    {
        private string exportBasePath;
        private StringCollection extensionList;
        private StringCollection invalidArgs;
        private string inputFile;
        private ConsoleMessageHandler messageHandler;
        private string outputDirectory;
        private string outputFile;
        private OutputType outputType;
        private bool outputXml;
        private bool showHelp;
        private bool showLogo;
        private bool suppressCustomTables;
        private bool suppressDroppingEmptyTables;
        private bool suppressRelativeActionSequencing;
        private bool suppressUI;
        private bool tidy;

        /// <summary>
        /// Instantiate a new Dark class.
        /// </summary>
        private Dark()
        {
            this.extensionList = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("DARK", "dark.exe");
            this.showLogo = true;
            this.tidy = true;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to decompiler.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Dark dark = new Dark();
            return dark.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            Decompiler decompiler = null;
            Mutator mutator = null;
            Unbinder unbinder = null;

            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (null == this.inputFile)
                {
                    this.showHelp = true;
                }
                else if (null == this.outputFile)
                {
                    if (null == this.outputDirectory)
                    {
                        this.outputFile = Path.ChangeExtension(Path.GetFileName(this.inputFile), ".wxs");
                    }
                    else
                    {
                        this.outputFile = Path.Combine(this.outputDirectory, Path.ChangeExtension(Path.GetFileName(this.inputFile), ".wxs"));
                    }
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(DarkStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                // create the decompiler and mutator
                decompiler = new Decompiler();
                mutator = new Mutator();
                mutator.Core = new HarvesterCore(new MessageEventHandler(this.messageHandler.Display));
                unbinder = new Unbinder();

                // read the configuration file (dark.exe.config)
                AppCommon.ReadConfiguration(this.extensionList);

                // load any extensions
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);

                    decompiler.AddExtension(wixExtension);
                    unbinder.AddExtension(wixExtension);
                }

                // set options
                decompiler.SuppressCustomTables = this.suppressCustomTables;
                decompiler.SuppressDroppingEmptyTables = this.suppressDroppingEmptyTables;
                decompiler.SuppressRelativeActionSequencing = this.suppressRelativeActionSequencing;
                decompiler.SuppressUI = this.suppressUI;
                decompiler.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                if (!String.IsNullOrEmpty(this.exportBasePath))
                {
                    decompiler.ExportFilePath = this.exportBasePath;
                }

                unbinder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                decompiler.Message += new MessageEventHandler(this.messageHandler.Display);
                unbinder.Message += new MessageEventHandler(this.messageHandler.Display);

                // print friendly message saying what file is being decompiled
                Console.WriteLine(Path.GetFileName(this.inputFile));

                // unbind
                // TODO: passing a bundle to the decompiler without the /x parameter specified stumbles here
                //        as the exportBasePath will be null. Need a design decision whether to throw an 
                //        message below or throw a message here
                Output output = unbinder.Unbind(this.inputFile, this.outputType, this.exportBasePath);
                
                if (null != output)
                {
                    if (OutputType.Patch == this.outputType || OutputType.Transform == this.outputType || this.outputXml)
                    {
                        output.Save(this.outputFile, null, new WixVariableResolver(), null);
                    }
                    else // decompile
                    {
                        Wix.Wix wix = decompiler.Decompile(output);

                        // output
                        if (null != wix)
                        {
                            XmlTextWriter writer = null;

                            // mutate the Wix document
                            if (!mutator.Mutate(wix))
                            {
                                return this.messageHandler.LastErrorNumber;
                            }

                            try
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(this.outputFile)));

                                writer = new XmlTextWriter(this.outputFile, System.Text.Encoding.UTF8);

                                writer.Indentation = 4;
                                writer.IndentChar = ' ';
                                writer.QuoteChar = '"';
                                writer.Formatting = Formatting.Indented;

                                writer.WriteStartDocument();
                                wix.OutputXml(writer);
                                writer.WriteEndDocument();
                            }
                            catch (Exception e)
                            {
                                this.messageHandler.Display(this, WixErrors.FileWriteError(this.outputFile, e.Message));
                                return this.messageHandler.LastErrorNumber;
                            }
                            finally
                            {
                                if (null != writer)
                                {
                                    writer.Close();
                                }
                            }
                        }
                    }
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
            finally
            {
                if (null != decompiler)
                {
                    if (this.tidy)
                    {
                        if (!decompiler.DeleteTempFiles())
                        {
                            Console.WriteLine(DarkStrings.WAR_FailedToDeleteTempDir, decompiler.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(DarkStrings.INF_TempDirLocatedAt, decompiler.TempFilesLocation);
                    }
                }

                if (null != unbinder)
                {
                    if (this.tidy)
                    {
                        if (!unbinder.DeleteTempFiles())
                        {
                            Console.WriteLine(DarkStrings.WAR_FailedToDeleteTempDir, unbinder.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(DarkStrings.INF_TempDirLocatedAt, unbinder.TempFilesLocation);
                    }
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

                    if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }

                        this.extensionList.Add(args[i]);
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
                        string path = CommandLine.GetFileOrDirectory(parameter, this.messageHandler, args, ++i);

                        if (!String.IsNullOrEmpty(path))
                        {
                            if (path.EndsWith("\\") || path.EndsWith("/"))
                            {
                                this.outputDirectory = path;
                            }
                            else
                            {
                                this.outputFile = path;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else if ("sct" == parameter)
                    {
                        this.suppressCustomTables = true;
                    }
                    else if ("sdet" == parameter)
                    {
                        this.suppressDroppingEmptyTables = true;
                    }
                    else if ("sras" == parameter)
                    {
                        this.suppressRelativeActionSequencing = true;
                    }
                    else if ("sui" == parameter)
                    {
                        this.suppressUI = true;
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
                    else if ("x" == parameter)
                    {
                        this.exportBasePath = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i);

                        // If a directory was not provided, skip the parameter.
                        if (String.IsNullOrEmpty(this.exportBasePath))
                        {
                            return;
                        }

                        // Always use the actual value provided on the command-line so the authoring
                        // matches what they typed on the command-line.
                        this.exportBasePath = args[i];
                    }
                    else if ("xo" == parameter)
                    {
                        this.outputXml = true;
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
                else
                {
                    if (null == this.inputFile)
                    {
                        this.inputFile = CommandLine.VerifyPath(this.messageHandler, arg);

                        if (String.IsNullOrEmpty(this.inputFile))
                        {
                            return;
                        }

                        // guess the output type based on the extension of the input file
                        if (OutputType.Unknown == this.outputType)
                        {
                            string extension = Path.GetExtension(this.inputFile).ToLower(CultureInfo.InvariantCulture);
                            switch (extension)
                            {
                                case ".msi":
                                    this.outputType = OutputType.Product;
                                    break;
                                case ".msm":
                                    this.outputType = OutputType.Module;
                                    break;
                                case ".msp":
                                    this.outputType = OutputType.Patch;
                                    break;
                                case ".mst":
                                    this.outputType = OutputType.Transform;
                                    break;
                                case ".exe":
                                    this.outputType = OutputType.Bundle;
                                    break;
                                case ".pcp":
                                    this.outputType = OutputType.PatchCreation;
                                    break;
                                default:
                                    this.messageHandler.Display(this, WixErrors.UnexpectedFileExtension(Path.GetFileName(this.inputFile), ".msi, .msm, .msp, .mst, .exe, .pcp"));
                                    return;
                            }
                        }
                    }
                    else if (null == this.outputFile && null == this.outputDirectory)
                    {
                        this.outputFile = CommandLine.VerifyPath(this.messageHandler, arg);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else
                    {
                        this.messageHandler.Display(this, WixErrors.AdditionalArgumentUnexpected(arg));
                    }
                }
            }
        }
    }
}
