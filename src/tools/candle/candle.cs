// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.InteropServices;
    using System.Xml;

    /// <summary>
    /// The main entry point for candle.
    /// </summary>
    public sealed class Candle
    {
        private StringCollection invalidArgs;
        private StringCollection sourceFiles;
        private Hashtable parameters;
        private Platform platform;
        private StringCollection includeSearchPaths;
        private StringCollection extensionList;
        private string outputFile;
        private string outputDirectory;
        private bool suppressFilesVitalByDefault;
        private bool showLogo;
        private bool showHelp;
        private bool suppressSchema;
        private bool showPedanticMessages;
        private bool preprocessToStdout;
        private String preprocessFile;
        private ConsoleMessageHandler messageHandler;
        private bool fipsCompliant;
        private bool allowPerSourceOutputSpecification;

        private static readonly char[] sourceOutputSeparator = new char[] { ';' };

        /// <summary>
        /// Instantiate a new Candle class.
        /// </summary>
        private Candle()
        {
            this.invalidArgs = new StringCollection();
            this.sourceFiles = new StringCollection();
            this.parameters = new Hashtable();
            this.platform = Platform.X86;
            this.includeSearchPaths = new StringCollection();
            this.extensionList = new StringCollection();
            this.showLogo = true;
            this.messageHandler = new ConsoleMessageHandler("CNDL", "candle.exe");
            this.fipsCompliant = false;
            this.allowPerSourceOutputSpecification = false;
        }

        /// <summary>
        /// The main entry point for candle.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Candle candle = new Candle();
            return candle.Run(args);
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

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (!this.fipsCompliant)
                {
                    try
                    {
                        System.Security.Cryptography.MD5.Create();
                    }
                    catch (TargetInvocationException)
                    {
                        this.messageHandler.Display(this, WixErrors.UseFipsArgument());
                        return this.messageHandler.LastErrorNumber;
                    }
                }

                if (0 == this.sourceFiles.Count)
                {
                    this.showHelp = true;
                }
                else if (1 < this.sourceFiles.Count && null != this.outputFile)
                {
                    throw new ArgumentException(CandleStrings.CannotSpecifyMoreThanOneSourceFileForSingleTargetFile, "-out");
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(CandleStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                // create the preprocessor and compiler
                Preprocessor preprocessor = new Preprocessor();
                preprocessor.Message += new MessageEventHandler(this.messageHandler.Display);
                for (int i = 0; i < this.includeSearchPaths.Count; ++i)
                {
                    preprocessor.IncludeSearchPaths.Add(this.includeSearchPaths[i]);
                }
                preprocessor.CurrentPlatform = this.platform;

                Compiler compiler = new Compiler();
                compiler.Message += new MessageEventHandler(this.messageHandler.Display);
                compiler.SuppressFilesVitalByDefault = this.suppressFilesVitalByDefault;
                compiler.ShowPedanticMessages = this.showPedanticMessages;
                compiler.SuppressValidate = this.suppressSchema;
                compiler.CurrentPlatform = this.platform;
                compiler.FipsCompliant = this.fipsCompliant;

                // load any extensions
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);

                    preprocessor.AddExtension(wixExtension);
                    compiler.AddExtension(wixExtension);
                }

                // preprocess then compile each source file
                Dictionary<string, List<string>> sourcesForOutput = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (string sourceFileOrig in this.sourceFiles)
                {
                    string sourceFile = sourceFileOrig;
                    string targetFile = null;

                    if (this.allowPerSourceOutputSpecification)
                    {
                        string[] parts = sourceFileOrig.Split(Candle.sourceOutputSeparator, 2);
                        if (2 == parts.Length)
                        {
                            sourceFile = parts[0];
                            targetFile = parts[1];
                        }
                    }

                    string sourceFilePath = Path.GetFullPath(sourceFile);
                    string sourceFileName = Path.GetFileName(sourceFile);

                    if (null == targetFile)
                    {
                        if (null != this.outputFile)
                        {
                            targetFile = this.outputFile;
                        }
                        else if (null != this.outputDirectory)
                        {
                            targetFile = Path.Combine(this.outputDirectory, Path.ChangeExtension(sourceFileName, ".wixobj"));
                        }
                        else
                        {
                            targetFile = Path.ChangeExtension(sourceFileName, ".wixobj");
                        }
                    }
                    else if (!Path.IsPathRooted(targetFile) && null != this.outputDirectory)
                    {
                        targetFile = Path.Combine(this.outputDirectory, targetFile);
                    }

                    // print friendly message saying what file is being compiled
                    Console.WriteLine(sourceFileName);

                    // preprocess the source
                    XmlDocument sourceDocument;
                    try
                    {
                        if (this.preprocessToStdout)
                        {
                            preprocessor.PreprocessOut = Console.Out;
                        }
                        else if (null != this.preprocessFile)
                        {
                            preprocessor.PreprocessOut = new StreamWriter(this.preprocessFile);
                        }

                        sourceDocument = preprocessor.Process(sourceFilePath, this.parameters);
                    }
                    finally
                    {
                        if (null != preprocessor.PreprocessOut && Console.Out != preprocessor.PreprocessOut)
                        {
                            preprocessor.PreprocessOut.Close();
                        }
                    }

                    // if we're not actually going to compile anything, move on to the next file
                    if (null == sourceDocument || this.preprocessToStdout || null != this.preprocessFile)
                    {
                        continue;
                    }

                    // and now we do what we came here to do...
                    Intermediate intermediate = compiler.Compile(sourceDocument);

                    // save the intermediate to disk if no errors were found for this source file
                    if (null != intermediate)
                    {
                        intermediate.Save(targetFile);
                    }

                    // Track which source files result in a given output file, to ensure we aren't
                    // overwriting the output.
                    List<string> sources = null;
                    string targetPath = Path.GetFullPath(targetFile);
                    if (!sourcesForOutput.TryGetValue(targetPath, out sources))
                    {
                        sources = new List<string>();
                        sourcesForOutput.Add(targetPath, sources);
                    }
                    sources.Add(sourceFile);
                }

                // Show an error for every output file that had more than 1 source file.
                foreach (KeyValuePair<string, List<string>> outputSources in sourcesForOutput)
                {
                    if (1 < outputSources.Value.Count)
                    {
                        string sourceFiles = CompilerCore.CreateValueList(ValueListKind.None, outputSources.Value);
                        this.messageHandler.Display(this, WixErrors.DuplicateSourcesForOutput(sourceFiles, outputSources.Key));
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

                if (1 == arg.Length) // treat '-' and '@' as filenames when by themselves.
                {
                    this.sourceFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);
                    if ("allowPerSourceOutputSpecification" == parameter)
                    {
                        // This is a *long* parameter name; but we want it to be painful because it's
                        // non-standard and we don't really want to have it at all.
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("allowPerSourceOutputSpecification"));

                        this.allowPerSourceOutputSpecification = true;
                    }
                    else if ('d' == parameter[0])
                    {
                        if (1 >= parameter.Length || '=' == parameter[1])
                        {
                            this.messageHandler.Display(this, WixErrors.InvalidVariableDefinition(arg));
                            return;
                        }

                        parameter = arg.Substring(2);

                        string[] value = parameter.Split("=".ToCharArray(), 2);

                        if (this.parameters.ContainsKey(value[0]))
                        {
                            this.messageHandler.Display(this, WixErrors.DuplicateVariableDefinition(value[0], (1 == value.Length) ? String.Empty : value[1], (string)this.parameters[value[0]]));
                            return;
                        }

                        if (1 == value.Length)
                        {
                            this.parameters.Add(value[0], String.Empty);
                        }
                        else
                        {
                            this.parameters.Add(value[0], value[1]);
                        }
                    }
                    else if ("fips" == parameter)
                    {
                        this.fipsCompliant = true;
                    }
                    else if ('I' == parameter[0])
                    {
                        this.includeSearchPaths.Add(parameter.Substring(1));
                    }
                    else if ("ext" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.TypeSpecificationForExtensionRequired("-ext"));
                            return;
                        }
                        else
                        {
                            this.extensionList.Add(args[i]);
                        }
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        string path = CommandLine.GetFileOrDirectory(parameter, this.messageHandler, args, ++i);

                        if (!String.IsNullOrEmpty(path))
                        {
                            if (path.EndsWith("\\", StringComparison.Ordinal) || path.EndsWith("/", StringComparison.Ordinal))
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
                    else if ("pedantic" == parameter)
                    {
                        this.showPedanticMessages = true;
                    }
                    else if ("platform" == parameter || "arch" == parameter)
                    {
                        if ("platform" == parameter)
                        {
                            this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("platform", "arch"));
                        }

                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.InvalidPlatformParameter(parameter, String.Empty));
                            return;
                        }

                        if (String.Equals(args[i], "intel", StringComparison.OrdinalIgnoreCase) || String.Equals(args[i], "x86", StringComparison.OrdinalIgnoreCase))
                        {
                            this.platform = Platform.X86;
                        }
                        else if (String.Equals(args[i], "x64", StringComparison.OrdinalIgnoreCase))
                        {
                            this.platform = Platform.X64;
                        }
                        else if (String.Equals(args[i], "intel64", StringComparison.OrdinalIgnoreCase) || String.Equals(args[i], "ia64", StringComparison.OrdinalIgnoreCase))
                        {
                            this.platform = Platform.IA64;
                        }
                        else if (String.Equals(args[i], "arm", StringComparison.OrdinalIgnoreCase))
                        {
                            this.platform = Platform.ARM;
                        }
                        else
                        {
                            this.messageHandler.Display(this, WixErrors.InvalidPlatformParameter(parameter, args[i]));
                        }
                    }
                    else if ('p' == parameter[0])
                    {
                        String file = arg.Substring(2);
                        this.preprocessFile = file;
                        this.preprocessToStdout = (0 == file.Length);
                    }
                    else if ("sfdvital" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("sfdvital"));
                        this.suppressFilesVitalByDefault = true;
                    }
                    else if ("ss" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("ss"));
                        this.suppressSchema = true;
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
                    else if ("trace" == parameter)
                    {
                        this.messageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("trace"));
                        this.messageHandler.SourceTrace = true;
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
                        this.invalidArgs.Add(parameter);
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    string sourceArg = arg;
                    string targetArg = null;

                    if (this.allowPerSourceOutputSpecification)
                    {
                        string[] parts = arg.Split(Candle.sourceOutputSeparator, 2);
                        if (2 == parts.Length)
                        {
                            sourceArg = parts[0];
                            targetArg = parts[1];
                        }
                    }

                    string[] files = AppCommon.GetFiles(sourceArg, "Source");

                    if (this.allowPerSourceOutputSpecification && null != targetArg)
                    {
                        // files should contain only one item!
                        if (1 < files.Length)
                        {
                            string sourceList = CompilerCore.CreateValueList(ValueListKind.None, files);
                            this.messageHandler.Display(this, WixErrors.MultipleFilesMatchedWithOutputSpecification(arg, sourceList));
                        }
                        else
                        {
                            this.sourceFiles.Add(string.Concat(files[0], Candle.sourceOutputSeparator[0], targetArg));
                        }
                    }
                    else
                    {
                        this.sourceFiles.AddRange(files);
                    }
                }
            }

            return;
        }
    }
}
