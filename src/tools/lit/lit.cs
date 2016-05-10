// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Main entry point for library tool.
    /// </summary>
    public sealed class Lit
    {
        private StringCollection bindPaths;
        private BinderFileManager binderFileManager;
        private bool bindFiles;
        private StringCollection inputFiles;
        private StringCollection invalidArgs;
        private StringCollection unparsedArgs;
        private string outputFile;
        private bool showLogo;
        private bool showHelp;
        private bool showPedanticMessages;
        private bool suppressSchema;
        private bool suppressVersionCheck;
        private ConsoleMessageHandler messageHandler;
        private StringCollection extensionList;
        private StringCollection localizationFiles;
        private StringCollection sourcePaths;

        /// <summary>
        /// Instantiate a new Lit class.
        /// </summary>
        private Lit()
        {
            this.bindPaths = new StringCollection();
            this.inputFiles = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.unparsedArgs = new StringCollection();
            this.extensionList = new StringCollection();
            this.showLogo = true;
            this.messageHandler = new ConsoleMessageHandler("LIT", "lit.exe");
            this.extensionList = new StringCollection();
            this.localizationFiles = new StringCollection();
            this.sourcePaths = new StringCollection();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Commandline arguments for lit.</param>
        /// <returns>Returns non-zero error code in the case of an error.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Lit lit = new Lit();
            return lit.Run(args);
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
                Librarian librarian = null;
                SectionCollection sections = new SectionCollection();

                // parse the command line
                this.ParseCommandLine(args);

                // load any extensions
                List<WixExtension> loadedExtensionList = new List<WixExtension>();
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);
                    loadedExtensionList.Add(wixExtension);

                    // Have the binder extension parse the command line arguments lit did not recognized.
                    if (0 < this.unparsedArgs.Count)
                    {
                        this.unparsedArgs = wixExtension.ParseCommandLine(this.unparsedArgs, this.messageHandler);
                    }
                }

                this.ParseCommandLinePassTwo(this.unparsedArgs);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (0 == this.inputFiles.Count)
                {
                    this.showHelp = true;
                }
                else if (null == this.outputFile)
                {
                    if (1 < this.inputFiles.Count)
                    {
                        throw new WixException(WixErrors.MustSpecifyOutputWithMoreThanOneInput());
                    }

                    this.outputFile = Path.ChangeExtension(Path.GetFileName(this.inputFiles[0]), ".wixlib");
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(LitStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                // create the librarian
                librarian = new Librarian();
                librarian.Message += new MessageEventHandler(this.messageHandler.Display);
                librarian.ShowPedanticMessages = this.showPedanticMessages;

                if (null != this.bindPaths)
                {
                    foreach (string bindPath in this.bindPaths)
                    {
                        if (-1 == bindPath.IndexOf('='))
                        {
                            this.sourcePaths.Add(bindPath);
                        }
                    }
                }

                foreach (WixExtension wixExtension in loadedExtensionList)
                {
                    librarian.AddExtension(wixExtension);

                    // load the binder file manager regardless of whether it will be used in case there is a collision
                    if (null != wixExtension.BinderFileManager)
                    {
                        if (null != this.binderFileManager)
                        {
                            throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, LitStrings.EXP_CannotLoadBinderFileManager, wixExtension.BinderFileManager.GetType().ToString(), this.binderFileManager.GetType().ToString()), "ext");
                        }

                        this.binderFileManager = wixExtension.BinderFileManager;
                    }
                }

                // add the sections to the librarian
                foreach (string inputFile in this.inputFiles)
                {
                    string inputFileFullPath = Path.GetFullPath(inputFile);
                    string dirName = Path.GetDirectoryName(inputFileFullPath);

                    if (!this.sourcePaths.Contains(dirName))
                    {
                        this.sourcePaths.Add(dirName);
                    }

                    // try loading as an object file
                    try
                    {
                        Intermediate intermediate = Intermediate.Load(inputFileFullPath, librarian.TableDefinitions, this.suppressVersionCheck, this.suppressSchema);
                        sections.AddRange(intermediate.Sections);
                        continue; // next file
                    }
                    catch (WixNotIntermediateException)
                    {
                        // try another format
                    }

                    // try loading as a library file
                    Library loadedLibrary = Library.Load(inputFileFullPath, librarian.TableDefinitions, this.suppressVersionCheck, this.suppressSchema);
                    sections.AddRange(loadedLibrary.Sections);
                }

                // and now for the fun part
                Library library = librarian.Combine(sections);

                // save the library output if an error did not occur
                if (null != library)
                {
                    if (this.bindFiles)
                    {
                        // if the binder file manager has not been loaded yet use the built-in binder extension
                        if (null == this.binderFileManager)
                        {
                            this.binderFileManager = new BinderFileManager();
                        }


                        if (null != this.bindPaths)
                        {
                            foreach (string bindPath in this.bindPaths)
                            {
                                if (-1 == bindPath.IndexOf('='))
                                {
                                    this.binderFileManager.BindPaths.Add(bindPath);
                                }
                                else
                                {
                                    string[] namedPair = bindPath.Split('=');

                                    //It is ok to have duplicate key.
                                    this.binderFileManager.NamedBindPaths.Add(namedPair[0], namedPair[1]);
                                }
                            }
                        }

                        foreach (string sourcePath in this.sourcePaths)
                        {
                            this.binderFileManager.SourcePaths.Add(sourcePath);
                        }
                    }
                    else
                    {
                        this.binderFileManager = null;
                    }

                    foreach (string localizationFile in this.localizationFiles)
                    {
                        Localization localization = Localization.Load(localizationFile, librarian.TableDefinitions, this.suppressSchema);

                        library.AddLocalization(localization);
                    }

                    WixVariableResolver wixVariableResolver = new WixVariableResolver();

                    wixVariableResolver.Message += new MessageEventHandler(this.messageHandler.Display);

                    library.Save(this.outputFile, this.binderFileManager, wixVariableResolver);
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

                    if ("b" == parameter)
                    {
                        string bindPath = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i, true);

                        if (String.IsNullOrEmpty(bindPath))
                        {
                            return;
                        }

                        this.bindPaths.Add(bindPath);
                    }
                    else if ("bf" == parameter)
                    {
                        this.bindFiles = true;
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }

                        this.extensionList.Add(args[i]);
                    }
                    else if ("loc" == parameter)
                    {
                        string locFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(locFile))
                        {
                            return;
                        }

                        this.localizationFiles.Add(locFile);
                    }
                    else if ("nologo" == parameter)
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
                    else if ("pedantic" == parameter)
                    {
                        this.showPedanticMessages = true;
                    }
                    else if ("ss" == parameter)
                    {
                        this.suppressSchema = true;
                    }
                    else if ("sv" == parameter)
                    {
                        this.suppressVersionCheck = true;
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
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else
                    {
                        this.unparsedArgs.Add(arg);
                    }
                }
                else if (arg.Length > 1 && '@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    this.unparsedArgs.Add(arg);
                }
            }
        }

        /// <summary>
        /// Makes the second pass at the command line after binder extensions parse what they can.
        /// Anything at this point should be a source file.
        /// </summary>
        /// <param name="args">The remaining arguments.</param>
        private void ParseCommandLinePassTwo(StringCollection args)
        {
            for (int i = 0; i < args.Count; ++i)
            {
                string arg = args[i];
                if (null == arg || 0 == arg.Length) // skip blank arguments
                {
                    continue;
                }

                if (arg.Length > 1 && ('-' == arg[0] || '/' == arg[0]))
                {
                    string parameter = arg.Substring(1);

                    // We don't expect any unparsed switches at this point.
                    this.invalidArgs.Add(parameter);
                }
                else
                {
                    this.inputFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                }
            }
        }
    }
}
