// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// The main entry point for light.
    /// </summary>
    public sealed class Light
    {
        private string[] cultures;
        private bool allowIdenticalRows;
        private bool allowDuplicateDirectoryIds;
        private bool allowUnresolvedReferences;
        private bool bindFiles;
        private WixBinder binder;
        private string binderClass;
        private bool dropUnrealTables;
        private StringCollection inputFiles;
        private StringCollection invalidArgs;
        private StringCollection unparsedArgs;
        private bool outputXml;
        private bool sectionIdOnRows;
        private bool showHelp;
        private bool showLogo;
        private bool suppressAdminSequence;
        private bool suppressAdvertiseSequence;
        private bool suppressLocalization;
        private bool suppressMsiAssemblyTable;
        private bool suppressSchema;
        private bool suppressUISequence;
        private bool suppressVersionCheck;
        private bool tidy;
        private string outputFile;
        private ConsoleMessageHandler messageHandler;
        private string unreferencedSymbolsFile;
        private bool showPedanticMessages;
        private StringCollection bindPaths;
        private StringCollection extensionList;
        private StringCollection localizationFiles;
        private StringCollection sourcePaths;
        private WixVariableResolver wixVariableResolver;

        /// <summary>
        /// Instantiate a new Light class.
        /// </summary>
        private Light()
        {
            this.bindPaths = new StringCollection();
            this.extensionList = new StringCollection();
            this.localizationFiles = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("LGHT", "light.exe");
            this.inputFiles = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.unparsedArgs = new StringCollection();
            this.sourcePaths = new StringCollection();
            this.showLogo = true;
            this.tidy = true;
            this.sectionIdOnRows = true;

            this.wixVariableResolver = new WixVariableResolver();
            this.wixVariableResolver.Message += new MessageEventHandler(this.messageHandler.Display);
        }

        /// <summary>
        /// The main entry point for light.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Light light = new Light();
            return light.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            Linker linker = null;
            Localizer localizer = null;
            SectionCollection sections = new SectionCollection();
            ArrayList transforms = new ArrayList();

            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // load any extensions
                List<WixExtension> loadedExtensionList = new List<WixExtension>();
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);
                    loadedExtensionList.Add(wixExtension);

                    // If the extension provides a binder, use that now if it
                    // matches the class from the command line.
                    if (null != wixExtension.CustomBinder && null != this.binderClass && wixExtension.CustomBinder.GetType().Name.Equals(this.binderClass, StringComparison.Ordinal))
                    {
                        this.binder = wixExtension.CustomBinder;
                    }
                }

                // If a binder was specified, but not found then show an error.
                if (!String.IsNullOrEmpty(this.binderClass) && null == this.binder)
                {
                    throw new WixException(WixErrors.SpecifiedBinderNotFound(this.binderClass));
                }

                // create the linker, binder, and validator
                linker = new Linker();
                if (null == this.binder)
                {
                    this.binder = new Microsoft.Tools.WindowsInstallerXml.Binder();
                }

                // have the binder parse the command line arguments light did not recognize
                string[] unparsedArgsArray = new string[this.unparsedArgs.Count];
                this.unparsedArgs.CopyTo(unparsedArgsArray, 0);
                StringCollection remainingArgs = this.binder.ParseCommandLine(unparsedArgsArray, this.messageHandler);

                // Loop through the extensions to give them a shot at processing the remaining command-line args.
                foreach (WixExtension wixExtension in loadedExtensionList)
                {
                    if (0 == remainingArgs.Count)
                    {
                        break;
                    }

                    remainingArgs = wixExtension.ParseCommandLine(remainingArgs, this.messageHandler);
                }

                this.ParseCommandLinePassTwo(remainingArgs);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }

                this.invalidArgs = null;

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

                    this.outputFile = Path.ChangeExtension(Path.GetFileName(this.inputFiles[0]), ".wix"); // we'll let the linker change the extension later
                }

                this.binder.OutputFile = this.outputFile;
                this.binder.PostParseCommandLine();

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    this.PrintHelp();
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                linker.AllowIdenticalRows = this.allowIdenticalRows;
                linker.AllowDuplicateDirectoryIds = this.allowDuplicateDirectoryIds;
                linker.AllowUnresolvedReferences = this.allowUnresolvedReferences;
                linker.Cultures = this.cultures;
                linker.UnreferencedSymbolsFile = this.unreferencedSymbolsFile;
                linker.ShowPedanticMessages = this.showPedanticMessages;
                linker.DropUnrealTables = this.dropUnrealTables;
                linker.SuppressLocalization = this.suppressLocalization;
                linker.SuppressMsiAssemblyTable = this.suppressMsiAssemblyTable;
                linker.WixVariableResolver = this.wixVariableResolver;

                // set the sequence suppression options
                linker.SuppressAdminSequence = this.suppressAdminSequence;
                linker.SuppressAdvertiseSequence = this.suppressAdvertiseSequence;
                linker.SuppressUISequence = this.suppressUISequence;

                linker.SectionIdOnRows = this.sectionIdOnRows;

                this.binder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                this.binder.WixVariableResolver = this.wixVariableResolver;

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

                // instantiate the localizer and load any localization files
                if (!this.suppressLocalization || 0 < this.localizationFiles.Count || null != this.cultures || !this.outputXml)
                {
                    List<Localization> localizations = new List<Localization>();
                    localizer = new Localizer();

                    localizer.Message += new MessageEventHandler(this.messageHandler.Display);

                    // load each localization file
                    foreach (string localizationFile in this.localizationFiles)
                    {
                        Localization localization = Localization.Load(localizationFile, linker.TableDefinitions, this.suppressSchema);
                        localizations.Add(localization);
                    }

                    if (null != this.cultures)
                    {
                        // add localizations in order specified in cultures
                        foreach (string culture in this.cultures)
                        {
                            foreach (Localization localization in localizations)
                            {
                                if (culture.Equals(localization.Culture, StringComparison.OrdinalIgnoreCase))
                                {
                                    localizer.AddLocalization(localization);
                                }
                            }
                        }
                    }
                    else 
                    {
                        bool neutralFound = false;
                        foreach (Localization localization in localizations)
                        {
                            if (0 == localization.Culture.Length)
                            {
                                // if a neutral wxl was provided use it
                                localizer.AddLocalization(localization);
                                neutralFound = true;
                            }
                        }

                        if (!neutralFound)
                        {
                            // cultures wasn't specified and no neutral wxl are available, include all of the files
                            foreach (Localization localization in localizations)
                            {
                                localizer.AddLocalization(localization);
                            }
                        }
                    }

                    // immediately stop processing if any errors were found
                    if (this.messageHandler.EncounteredError)
                    {
                        return this.messageHandler.LastErrorNumber;
                    }

                    // tell all of the objects about the localizer
                    linker.Localizer = localizer;
                    this.binder.Localizer = localizer;
                    this.wixVariableResolver.Localizer = localizer;
                }

                // process loaded extensions
                foreach (WixExtension wixExtension in loadedExtensionList)
                {
                    linker.AddExtension(wixExtension);
                    this.binder.AddExtension(wixExtension);

                    // load the extension's localizations
                    Library library = wixExtension.GetLibrary(linker.TableDefinitions);
                    if (null != library)
                    {
                        // load the extension's default culture if it provides one and we don't specify any cultures
                        string[] extensionCultures = this.cultures;
                        if (null == extensionCultures && null != wixExtension.DefaultCulture)
                        {
                            extensionCultures = new string[] { wixExtension.DefaultCulture };
                        }

                        library.GetLocalizations(extensionCultures, localizer);
                    }
                }

                this.binder.ProcessExtensions(loadedExtensionList.ToArray());

                // set the message handlers
                linker.Message += new MessageEventHandler(this.messageHandler.Display);
                this.binder.AddMessageEventHandler(new MessageEventHandler(this.messageHandler.Display));

                Output output = null;

                // loop through all the believed object files
                foreach (string inputFile in this.inputFiles)
                {
                    string dirName = Path.GetDirectoryName(inputFile);
                    string inputFileFullPath = Path.GetFullPath(inputFile);

                    if (!this.sourcePaths.Contains(dirName))
                    {
                        this.sourcePaths.Add(dirName);
                    }

                    // try loading as an object file
                    try
                    {
                        Intermediate intermediate = Intermediate.Load(inputFileFullPath, linker.TableDefinitions, this.suppressVersionCheck, this.suppressSchema);
                        sections.AddRange(intermediate.Sections);
                        continue; // next file
                    }
                    catch (WixNotIntermediateException)
                    {
                        // try another format
                    }

                    // try loading as a library file
                    try
                    {
                        Library library = Library.Load(inputFileFullPath, linker.TableDefinitions, this.suppressVersionCheck, this.suppressSchema);
                        library.GetLocalizations(this.cultures, localizer);
                        sections.AddRange(library.Sections);
                        continue; // next file
                    }
                    catch (WixNotLibraryException)
                    {
                        // try another format
                    }

                    // try loading as an output file
                    output = Output.Load(inputFileFullPath, this.suppressVersionCheck, this.suppressSchema);
                }

                // immediately stop processing if any errors were found
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                // set the binder file manager information
                foreach (string bindPath in this.bindPaths)
                {
                    //Checking as IndexOf will return 0 if the string value is String.Empty.
                    if (String.IsNullOrEmpty(bindPath))
                    {
                        continue;
                    }

                    if (-1 == bindPath.IndexOf('='))
                    {
                        this.binder.FileManager.BindPaths.Add(bindPath);
                    }
                    else
                    {
                        string[] namedPair = bindPath.Split('=');

                        //It is ok to have duplicate key.
                        this.binder.FileManager.NamedBindPaths.Add(namedPair[0], namedPair[1]);
                    }
                }

                foreach (string sourcePath in this.sourcePaths)
                {
                    this.binder.FileManager.SourcePaths.Add(sourcePath);
                }

                // and now for the fun part
                if (null == output)
                {
                    OutputType expectedOutputType = OutputType.Unknown;
                    if (this.outputFile != null)
                    {
                        expectedOutputType = Output.GetOutputType(Path.GetExtension(this.outputFile));
                    }

                    output = linker.Link(sections, transforms, expectedOutputType);

                    // if an error occurred during linking, stop processing
                    if (null == output)
                    {
                        return this.messageHandler.LastErrorNumber;
                    }
                }
                else if (0 != sections.Count)
                {
                    throw new InvalidOperationException(LightStrings.EXP_CannotLinkObjFilesWithOutpuFile);
                }

                // Now that the output object is either linked or loaded, tell the binder file manager about it.
                this.binder.FileManager.Output = output;

                // only output the xml if its a patch build or user specfied to only output wixout
                if (this.outputXml || OutputType.Patch == output.Type)
                {
                    string outputExtension = Path.GetExtension(this.outputFile);
                    if (null == outputExtension || 0 == outputExtension.Length || ".wix" == outputExtension)
                    {
                        if (OutputType.Patch == output.Type)
                        {
                            this.outputFile = Path.ChangeExtension(this.outputFile, ".wixmsp");
                        }
                        else
                        {
                            this.outputFile = Path.ChangeExtension(this.outputFile, ".wixout");
                        }
                    }

                    output.Save(this.outputFile, (this.bindFiles ? this.binder.FileManager : null), this.wixVariableResolver, this.binder.TempFilesLocation);
                }
                else // finish creating the MSI/MSM
                {
                    string outputExtension = Path.GetExtension(this.outputFile);
                    if (null == outputExtension || 0 == outputExtension.Length || ".wix" == outputExtension)
                    {
                        outputExtension = Output.GetExtension(output.Type);
                        this.outputFile = Path.ChangeExtension(this.outputFile, outputExtension);
                    }

                    this.binder.Bind(output, this.outputFile);
                }
            }
            catch (WixException we)
            {
                if (we is WixInvalidIdtException)
                {
                    // make sure the IDT files stay around
                    this.tidy = false;
                }

                this.messageHandler.Display(this, we.Error);
            }
            catch (Exception e)
            {
                // make sure the files stay around for debugging
                this.tidy = false;

                this.messageHandler.Display(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }
            finally
            {
                if (null != binder)
                {
                    this.binder.Cleanup(this.tidy);
                }
            }

            return this.messageHandler.LastErrorNumber;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings are not round tripped, and have no security impact")]
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (null == arg || 0 == arg.Length) // skip blank arguments
                {
                    continue;
                }

                if (arg.Length > 1 && ('-' == arg[0] || '/' == arg[0]))
                {
                    string parameter = arg.Substring(1);

                    if (parameter.Equals("ai", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("ai"));
                        this.allowIdenticalRows = true;
                    }
                    else if (parameter.Equals("ad", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("ad"));
                        this.allowDuplicateDirectoryIds = true;
                    }
                    else if (parameter.Equals("au", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("au"));
                        this.allowUnresolvedReferences = true;
                    }
                    else if (parameter.Equals("b", StringComparison.Ordinal))
                    {
                        string path = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i, true);

                        if (String.IsNullOrEmpty(path))
                        {
                            return;
                        }                       

                        this.bindPaths.Add(path);
                    }
                    else if (parameter.Equals("binder", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("binder"));

                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalBinderClassName());
                            return;
                        }

                        this.binderClass = args[i];
                    }
                    else if (parameter.Equals("bf", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("bf"));

                        this.bindFiles = true;
                    }
                    else if (parameter.StartsWith("cultures:", StringComparison.Ordinal))
                    {
                        string culturesString = arg.Substring(10).ToLower(CultureInfo.InvariantCulture);
                        // When null is used treat it as if cultures wasn't specified.  
                        // This is needed for batching over the light task when using MSBuild 2.0 which doesn't 
                        // support empty items
                        if (culturesString.Equals("null", StringComparison.OrdinalIgnoreCase))
                        {
                            this.cultures = null;
                        }
                        else
                        {
                            this.cultures = culturesString.Split(';', ',');

                            for (int c = 0; c < this.cultures.Length; c++)
                            {
                                // Neutral is different from null. For neutral we still want to do WXL filtering.
                                // Set the culture to the empty string = identifier for the invariant culture
                                if (this.cultures[c].Equals("neutral", StringComparison.OrdinalIgnoreCase))
                                {
                                    this.cultures[c] = String.Empty;
                                }
                            }
                        }
                    }
                    else if (parameter.Equals("dut", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("dut"));
                        this.dropUnrealTables = true;
                    }
                    else if (parameter.Equals("ext", StringComparison.Ordinal))
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }

                        this.extensionList.Add(args[i]);
                    }
                    else if (parameter.Equals("loc", StringComparison.Ordinal))
                    {
                        string locFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(locFile))
                        {
                            return;
                        }

                        this.localizationFiles.Add(locFile);
                    }
                    else if (parameter.Equals("nologo", StringComparison.Ordinal))
                    {
                        this.showLogo = false;
                    }
                    else if (parameter.Equals("notidy", StringComparison.Ordinal))
                    {
                        this.tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else if (parameter.Equals("pedantic", StringComparison.Ordinal))
                    {
                        this.showPedanticMessages = true;
                    }
                    else if (parameter.Equals("sadmin", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sadmin"));
                        this.suppressAdminSequence = true;
                    }
                    else if (parameter.Equals("sadv", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sadv"));
                        this.suppressAdvertiseSequence = true;
                    }
                    else if (parameter.Equals("sloc", StringComparison.Ordinal))
                    {
                        this.suppressLocalization = true;
                    }
                    else if (parameter.Equals("sma", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sma"));
                        this.suppressMsiAssemblyTable = true;
                    }
                    else if (parameter.Equals("ss", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("ss"));
                        this.suppressSchema = true;
                    }
                    else if (parameter.Equals("sts", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sts"));
                        this.sectionIdOnRows = false;
                    }
                    else if (parameter.Equals("sui", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sui"));
                        this.suppressUISequence = true;
                    }
                    else if (parameter.Equals("sv", StringComparison.Ordinal))
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sv"));
                        this.suppressVersionCheck = true;
                    }
                    else if (parameter.Equals("swall", StringComparison.Ordinal))
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
                    else if (parameter.Equals("usf", StringComparison.Ordinal))
                    {
                        this.unreferencedSymbolsFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.unreferencedSymbolsFile))
                        {
                            return;
                        }
                    }
                    else if (parameter.Equals("v", StringComparison.Ordinal))
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if (parameter.Equals("wxall", StringComparison.Ordinal))
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
                    else if (parameter.Equals("xo", StringComparison.Ordinal))
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

            if (this.bindFiles && !this.outputXml)
            {
                throw new ArgumentException(LightStrings.EXP_BindFileOptionNotApplicable);
            }
        }

        /// <summary>
        /// Makes the second pass at the command line after the binder parses
        /// what it can. Anything at this point should either be a source file
        /// or a command line variable defintion.
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

                    // -d is parsed here because the binder may specify other
                    // command line switches that start with d.
                    // For example, the MSI binder has -dcl.
                    if (parameter.StartsWith("d", StringComparison.Ordinal))
                    {
                        parameter = arg.Substring(2);
                        string[] value = parameter.Split("=".ToCharArray(), 2);

                        if (1 == value.Length)
                        {
                            this.messageHandler.Display(this, WixErrors.ExpectedWixVariableValue(value[0]));
                        }
                        else
                        {
                            this.wixVariableResolver.AddVariable(value[0], value[1]);
                        }
                    }
                    else
                    {
                        // we don't expect any unparsed switches other than -d at this point
                        this.invalidArgs.Add(parameter);
                    }
                }
                else
                {
                    this.inputFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                }
            }
        }

        /// <summary>
        /// Prints usage help.
        /// </summary>
        private void PrintHelp()
        {
            string lightArgs = LightStrings.CommandLineArguments;
            string binderArgs = this.binder.GetCommandLineArgumentsHelpString();

            Console.WriteLine(String.Format(LightStrings.HelpMessage, lightArgs, binderArgs));
        }
    }
}
