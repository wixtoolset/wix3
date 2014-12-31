//-------------------------------------------------------------------------------------------------
// <copyright file="melt.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Tool to decompile merge modules to ComponentGroups and extract files from MSI databases and 
// rewrite corresponding .wixpdb files to the extracted paths.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.Collections.Specialized;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Deployment.WindowsInstaller.Package;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Entry point for the melter
    /// </summary>
    public sealed class Melt
    {
        private string exportBasePath;
        private StringCollection extensionList;
        private StringCollection invalidArgs;
        private string id;
        private string inputFile;
        private string inputPdbFile;
        private ConsoleMessageHandler messageHandler;
        private string outputFile;
        private OutputType outputType;
        private bool showHelp;
        private bool showLogo;
        private bool tidy;
        private bool suppressExtraction;

        /// <summary>
        /// Instantiate a new Melt class.
        /// </summary>
        private Melt()
        {
            this.extensionList = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("MELT", "melt.exe");
            this.showLogo = true;
            this.tidy = true;
            this.id = null;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to decompiler.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Melt melt = new Melt();
            return melt.Run(args);
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

                if (String.IsNullOrEmpty(this.inputFile) || String.IsNullOrEmpty(this.outputFile) || (OutputType.Product == this.outputType && String.IsNullOrEmpty(this.inputPdbFile)))
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(MeltStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                if (null == this.exportBasePath)
                {
                    this.exportBasePath = System.IO.Path.GetDirectoryName(this.outputFile);
                }

                if (OutputType.Module == this.outputType)
                {
                    MeltModule();
                }
                else if (OutputType.Product == this.outputType)
                {
                    MeltProduct();
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
        /// Extracts files from a merge module and creates corresponding ComponentGroup WiX authoring.
        /// </summary>
        private void MeltModule()
        {
            Decompiler decompiler = null;
            Unbinder unbinder = null;
            Melter melter = null;

            try
            {
                // create the decompiler, unbinder, and melter
                decompiler = new Decompiler();
                unbinder = new Unbinder();
                melter = new Melter(decompiler, id);

                // read the configuration file (melt.exe.config)
                AppCommon.ReadConfiguration(this.extensionList);

                // load any extensions
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);

                    decompiler.AddExtension(wixExtension);
                    unbinder.AddExtension(wixExtension);
                }

                // set options
                decompiler.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                unbinder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                unbinder.SuppressDemodularization = true;

                decompiler.Message += new MessageEventHandler(this.messageHandler.Display);
                unbinder.Message += new MessageEventHandler(this.messageHandler.Display);
                melter.Message += new MessageEventHandler(this.messageHandler.Display);

                // print friendly message saying what file is being decompiled
                Console.WriteLine(Path.GetFileName(this.inputFile));

                // unbind
                Output output = unbinder.Unbind(this.inputFile, this.outputType, this.exportBasePath);

                if (null != output)
                {
                    Wix.Wix wix = melter.Melt(output);
                    if (null != wix)
                    {
                        XmlTextWriter writer = null;

                        try
                        {
                            writer = new XmlTextWriter(this.outputFile, System.Text.Encoding.UTF8);

                            writer.Indentation = 4;
                            writer.IndentChar = ' ';
                            writer.QuoteChar = '"';
                            writer.Formatting = Formatting.Indented;

                            writer.WriteStartDocument();
                            wix.OutputXml(writer);
                            writer.WriteEndDocument();
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
            finally
            {
                if (null != decompiler)
                {
                    if (this.tidy)
                    {
                        if (!decompiler.DeleteTempFiles())
                        {
                            Console.WriteLine(MeltStrings.WAR_FailedToDeleteTempDir, decompiler.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(MeltStrings.INF_TempDirLocatedAt, decompiler.TempFilesLocation);
                    }
                }

                if (null != unbinder)
                {
                    if (this.tidy)
                    {
                        if (!unbinder.DeleteTempFiles())
                        {
                            Console.WriteLine(MeltStrings.WAR_FailedToDeleteTempDir, unbinder.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(MeltStrings.INF_TempDirLocatedAt, unbinder.TempFilesLocation);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts files from an MSI database and rewrites the paths embedded in the source .wixpdb to the output .wixpdb.
        /// </summary>
        private void MeltProduct()
        {
            // print friendly message saying what file is being decompiled
            Console.WriteLine("{0} / {1}", Path.GetFileName(this.inputFile), Path.GetFileName(this.inputPdbFile));

            Pdb inputPdb = Pdb.Load(this.inputPdbFile, true, true);
            
            // extract files from the .msi (unless suppressed) and get the path map of File ids to target paths
            string outputDirectory = this.exportBasePath ?? Environment.GetEnvironmentVariable("WIX_TEMP");
            IDictionary<string, string> paths = null;
            IDictionary<string, string> binaryPaths = null;
            
            using (InstallPackage package = new InstallPackage(this.inputFile, DatabaseOpenMode.ReadOnly, null, outputDirectory))
            {
                if (!this.suppressExtraction)
                {
                    package.ExtractFiles();
                }

                paths = package.Files.SourcePaths;
                binaryPaths = package.GetBinaryFilePaths();                
            }            
            
            Table wixFileTable = inputPdb.Output.Tables["WixFile"];
            if (null != wixFileTable)
            {
                foreach (Row row in wixFileTable.Rows)
                {
                    WixFileRow fileRow = row as WixFileRow;
                    if (null != fileRow)
                    {
                        string newPath;
                        if (paths.TryGetValue(fileRow.File, out newPath))
                        {
                            fileRow.Source = Path.Combine(outputDirectory, newPath);
                        }
                    }
                }
            }

            Table binaryTable = inputPdb.Output.Tables["Binary"];
            if (null != binaryTable)
            {
                foreach (Row row in binaryTable.Rows)
                {
                    string filename = (string)row.Fields[0].Data;                    
                    row.Fields[1].Data = binaryPaths[filename];
                }
            }

            string tempPath = Path.Combine(Environment.GetEnvironmentVariable("WIX_TEMP") ?? Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                inputPdb.Save(this.outputFile, null, null, tempPath);
            }
            finally
            {
                if (this.tidy)
                {
                    if (!AppCommon.DeleteDirectory(tempPath, this.messageHandler))
                    {
                        Console.WriteLine(MeltStrings.WAR_FailedToDeleteTempDir, tempPath);
                    }
                }
                else
                {
                    Console.WriteLine(MeltStrings.INF_TempDirLocatedAt, tempPath);
                }
            }
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
                    else if ("id" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.ExpectedArgument(String.Concat("-", parameter)));
                            return;
                        }

                        this.id = args[i];
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
                        this.outputFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.outputFile))
                        {
                            return;
                        }
                    }
                    else if ("pdb" == parameter)
                    {
                        this.inputPdbFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.inputPdbFile))
                        {
                            return;
                        }
                    }
                    else if ("sextract" == parameter)
                    {
                        this.suppressExtraction = true;
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
                    else if ("x" == parameter)
                    {
                        this.exportBasePath = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.exportBasePath))
                        {
                            return;
                        }
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
                            string extension = Path.GetExtension(this.inputFile);
                            this.outputType = Output.GetOutputType(extension);

                            if (OutputType.Unknown == this.outputType)
                            {
                                this.messageHandler.Display(this, WixErrors.UnexpectedFileExtension(extension, ".msm, .msi"));
                                return;
                            }
                        }
                    }
                    else if (null == this.outputFile)
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
