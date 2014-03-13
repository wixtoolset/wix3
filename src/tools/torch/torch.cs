//-------------------------------------------------------------------------------------------------
// <copyright file="torch.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The torch transform builder application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Msi;

    /// <summary>
    /// The torch transform builder application.
    /// </summary>
    public sealed class Torch
    {
        private bool adminImage;
        private string exportBasePath;
        private StringCollection extensionList;
        private StringCollection inputFiles;
        private StringCollection invalidArgs;
        private ConsoleMessageHandler messageHandler;
        private string outputFile;
        private bool preserveUnchangedRows;
        private bool showHelp;
        private bool showLogo;
        private bool showPedanticMessages;
        private bool tidy;
        private TransformFlags validationFlags;
        private bool xmlInputs;
        private bool xmlOutput;

        private const string wixMstExtension = ".wixmst";
        private const string wixPdbExtension = ".wixpdb";
        private const string wixOutExtension = ".wixout";
        private const string msiExtension = ".msi";

        /// <summary>
        /// Instantiate a new Torch class.
        /// </summary>
        private Torch()
        {
            this.extensionList = new StringCollection();
            this.inputFiles = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("TRCH", "torch.exe");
            this.showLogo = true;
            this.tidy = true;
            this.validationFlags = 0;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">Arguments to torch.</param>
        /// <returns>0 if sucessful, otherwise 1.</returns>
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Torch torch = new Torch();
            return torch.Run(args);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            Microsoft.Tools.WindowsInstallerXml.Binder binder = null;
            Differ differ = null;
            Unbinder unbinder = null;

            TempFileCollection tempFileCollection = null;

            try
            {
                // parse the command line
                this.ParseCommandLine(args);

                // validate the inputs
                if (this.xmlInputs && this.adminImage)
                {
                    this.messageHandler.Display(this, WixErrors.IllegalCommandlineArgumentCombination("a", "xi"));
                    this.showHelp = true;
                }

                string[] allValidExtensions = new string[] { wixMstExtension, wixOutExtension, wixPdbExtension, msiExtension };
                string[] expectedSingleInputExtensions = new string[] { wixMstExtension, wixOutExtension };
                string[] expectedDoubleInputXmlExtensions = new string[] { wixOutExtension, wixPdbExtension };
                string[] expectedDoubleInputMsiExtensions = new string[] { msiExtension };

                // Validate that all inputs have the correct extension and we dont have too many inputs.
                if (1 == this.inputFiles.Count)
                {
                    string inputFile = this.inputFiles[0];

                    bool hasValidExtension = false;
                    foreach (string extension in expectedSingleInputExtensions)
                    {
                        if (String.Equals(Path.GetExtension(inputFile), extension, StringComparison.OrdinalIgnoreCase))
                        {
                            hasValidExtension = true;
                            break;
                        }
                    }

                    if (!hasValidExtension)
                    {
                        bool missingInput = false;

                        // Check if its using an extension that could be valid in other scenarios.
                        foreach (string validExtension in allValidExtensions)
                        {
                            if (String.Equals(Path.GetExtension(inputFile), validExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                this.messageHandler.Display(this, WixErrors.WrongFileExtensionForNumberOfInputs(Path.GetExtension(inputFile), inputFile));
                                missingInput = true;
                                break;
                            }
                        }

                        if (!missingInput)
                        {
                            this.messageHandler.Display(this, WixErrors.UnexpectedFileExtension(inputFile, String.Join(", ", expectedSingleInputExtensions)));
                        }
                    }
                }
                else if (2 == this.inputFiles.Count)
                {
                    foreach (string inputFile in inputFiles)
                    {
                        bool hasValidExtension = false;
                        string[] expectedExtensions = allValidExtensions;
                        if (this.xmlInputs)
                        {
                            foreach (string extension in expectedDoubleInputXmlExtensions)
                            {
                                if (String.Equals(Path.GetExtension(inputFile), extension, StringComparison.OrdinalIgnoreCase))
                                {
                                    hasValidExtension = true;
                                    expectedExtensions = expectedDoubleInputXmlExtensions;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (string extension in expectedDoubleInputMsiExtensions)
                            {
                                if (String.Equals(Path.GetExtension(inputFile), extension, StringComparison.OrdinalIgnoreCase))
                                {
                                    hasValidExtension = true;
                                    expectedExtensions = expectedDoubleInputMsiExtensions;
                                    break;
                                }
                            }
                        }

                        if (!hasValidExtension)
                        {
                            this.messageHandler.Display(this, WixErrors.UnexpectedFileExtension(inputFile, String.Join(", ", expectedExtensions)));
                        }
                    }
                }
                else
                {
                    this.showHelp = true;
                }

                // exit if there was an error parsing the command line or with a file extension (otherwise the logo appears after error messages)
                if (this.messageHandler.EncounteredError)
                {
                    return this.messageHandler.LastErrorNumber;
                }

                if (null == this.outputFile)
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(TorchStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                binder = new Microsoft.Tools.WindowsInstallerXml.Binder();
                differ = new Differ();
                unbinder = new Unbinder();

                // load any extensions
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);
                    unbinder.AddExtension(wixExtension);
                    binder.AddExtension(wixExtension);
                    differ.AddExtension(wixExtension);
                }

                binder.Message += new MessageEventHandler(this.messageHandler.Display);
                differ.Message += new MessageEventHandler(this.messageHandler.Display);
                unbinder.Message += new MessageEventHandler(this.messageHandler.Display);

                binder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                unbinder.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                tempFileCollection = new TempFileCollection(Environment.GetEnvironmentVariable("WIX_TEMP"));

                binder.WixVariableResolver = new WixVariableResolver();
                differ.PreserveUnchangedRows = this.preserveUnchangedRows;
                differ.ShowPedanticMessages = this.showPedanticMessages;
                unbinder.SuppressExtractCabinets = true;
                unbinder.IsAdminImage = this.adminImage;

                if (null == this.exportBasePath)
                {
                    this.exportBasePath = tempFileCollection.BasePath;
                }

                // load and process the inputs
                Output transform;
                if (1 == this.inputFiles.Count)
                {
                    transform = Output.Load(this.inputFiles[0], false, false);
                    if (OutputType.Transform != transform.Type)
                    {
                        this.messageHandler.Display(this, WixErrors.InvalidWixTransform(this.inputFiles[0]));
                        return this.messageHandler.LastErrorNumber;
                    }
                }
                else // 2 inputs
                {
                    Output targetOutput;
                    Output updatedOutput;

                    if (this.xmlInputs)
                    {
                        // load the target database
                        if (String.Equals(Path.GetExtension(inputFiles[0]), wixPdbExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            Pdb targetPdb = Pdb.Load(this.inputFiles[0], false, false);
                            targetOutput = targetPdb.Output;
                        }
                        else
                        {
                            targetOutput = Output.Load(this.inputFiles[0], false, false);
                        }

                        // load the updated database
                        if (String.Equals(Path.GetExtension(inputFiles[1]), wixPdbExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            Pdb updatedPdb = Pdb.Load(this.inputFiles[1], false, false);
                            updatedOutput = updatedPdb.Output;
                        }
                        else
                        {
                            updatedOutput = Output.Load(this.inputFiles[1], false, false);
                        }

                        this.xmlOutput = true;
                    }
                    else
                    {
                        // load the target database
                        targetOutput = unbinder.Unbind(this.inputFiles[0], OutputType.Product, Path.Combine(this.exportBasePath, "targetBinaries"));

                        // load the updated database
                        updatedOutput = unbinder.Unbind(this.inputFiles[1], OutputType.Product, Path.Combine(this.exportBasePath, "updatedBinaries"));
                    }

                    // diff the target and updated databases
                    transform = differ.Diff(targetOutput, updatedOutput, this.validationFlags);

                    if (null == transform.Tables || 0 >= transform.Tables.Count)
                    {
                        throw new WixException(WixErrors.NoDifferencesInTransform(transform.SourceLineNumbers));
                    }
                }

                // output the transform
                if (null != transform)
                {
                    // If either the user selected xml output or gave xml input, save as xml output.
                    // With xml inputs, many funtions of the binder have not been performed on the inputs (ie. file sequencing). This results in bad IDT files which cannot be put in a transform.
                    if (this.xmlOutput)
                    {
                        transform.Save(this.outputFile, null, null, tempFileCollection.BasePath);
                    }
                    else
                    {
                        binder.Bind(transform, this.outputFile);
                    }
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
                    if (this.tidy)
                    {
                        if (!binder.DeleteTempFiles())
                        {
                            Console.WriteLine(TorchStrings.WAR_FailedToDeleteTempDir, binder.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(TorchStrings.INF_BinderTempDirLocatedAt, binder.TempFilesLocation);
                    }
                }

                if (null != unbinder)
                {
                    if (this.tidy)
                    {
                        if (!unbinder.DeleteTempFiles())
                        {
                            Console.WriteLine(TorchStrings.WAR_FailedToDeleteTempDir, binder.TempFilesLocation);
                        }
                    }
                    else
                    {
                        Console.WriteLine(TorchStrings.INF_UnbinderTempDirLocatedAt, binder.TempFilesLocation);
                    }
                }

                if (null != tempFileCollection)
                {
                    if (this.tidy)
                    {
                        try
                        {
                            Directory.Delete(tempFileCollection.BasePath, true);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // if the path doesn't exist, then there is nothing for us to worry about
                        }
                        catch
                        {
                            Console.WriteLine(TorchStrings.WAR_FailedToDeleteTempDir, tempFileCollection.BasePath);
                        }
                    }
                    else
                    {
                        Console.WriteLine(TorchStrings.INF_TorchTempDirLocatedAt, tempFileCollection.BasePath);
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
            bool usingTransformType = false;
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

                    if ("a" == parameter)
                    {
                        this.adminImage = true;
                    }
                    else if ("ax" == parameter)
                    {
                        this.exportBasePath = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.exportBasePath))
                        {
                            return;
                        }

                        this.adminImage = true;
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
                    else if ("p" == parameter)
                    {
                        this.preserveUnchangedRows = true;
                    }
                    else if ("pedantic" == parameter)
                    {
                        this.showPedanticMessages = true;
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
                    else if ("serr" == parameter)
                    {
                        // arguments consistent with msitran.exe
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                            return;
                        }

                        if (usingTransformType)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalValidationArguments());
                        }
                        else
                        {
                            switch (args[i].ToLower())
                            {
                                case "a":
                                    this.validationFlags |= TransformFlags.ErrorAddExistingRow;
                                    break;
                                case "b":
                                    this.validationFlags |= TransformFlags.ErrorDeleteMissingRow;
                                    break;
                                case "c":
                                    this.validationFlags |= TransformFlags.ErrorAddExistingTable;
                                    break;
                                case "d":
                                    this.validationFlags |= TransformFlags.ErrorDeleteMissingTable;
                                    break;
                                case "e":
                                    this.validationFlags |= TransformFlags.ErrorUpdateMissingRow;
                                    break;
                                case "f":
                                    this.validationFlags |= TransformFlags.ErrorChangeCodePage;
                                    break;
                                default:
                                    this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                                    break;
                            }
                        }
                    }
                    else if ("t" == parameter)
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                            return;
                        }

                        if (0 != this.validationFlags)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalValidationArguments());
                        }
                        else
                        {
                            switch (args[i].ToLower())
                            {
                                case "language":
                                    this.validationFlags = TransformFlags.LanguageTransformDefault;
                                    break;
                                case "instance":
                                    this.validationFlags = TransformFlags.InstanceTransformDefault;
                                    break;
                                case "patch":
                                    this.validationFlags = TransformFlags.PatchTransformDefault;
                                    break;
                                default:
                                    this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                                    return;
                            }

                            usingTransformType = true;
                        }
                    }
                    else if ("v" == parameter)
                    {
                        this.messageHandler.ShowVerboseMessages = true;
                    }
                    else if ("val" == parameter)
                    {
                        // arguments consistent with msitran.exe
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                            return;
                        }

                        if (usingTransformType)
                        {
                            this.messageHandler.Display(this, WixErrors.IllegalValidationArguments());
                        }
                        else
                        {
                            switch (args[i].ToLower())
                            {
                                case "g":
                                    this.validationFlags |= TransformFlags.ValidateUpgradeCode;
                                    break;
                                case "l":
                                    this.validationFlags |= TransformFlags.ValidateLanguage;
                                    break;
                                case "r":
                                    this.validationFlags |= TransformFlags.ValidateProduct;
                                    break;
                                case "s":
                                    this.validationFlags |= TransformFlags.ValidateMajorVersion;
                                    break;
                                case "t":
                                    this.validationFlags |= TransformFlags.ValidateMinorVersion;
                                    break;
                                case "u":
                                    this.validationFlags |= TransformFlags.ValidateUpdateVersion;
                                    break;
                                case "v":
                                    this.validationFlags |= TransformFlags.ValidateNewLessBaseVersion;
                                    break;
                                case "w":
                                    this.validationFlags |= TransformFlags.ValidateNewLessEqualBaseVersion;
                                    break;
                                case "x":
                                    this.validationFlags |= TransformFlags.ValidateNewEqualBaseVersion;
                                    break;
                                case "y":
                                    this.validationFlags |= TransformFlags.ValidateNewGreaterEqualBaseVersion;
                                    break;
                                case "z":
                                    this.validationFlags |= TransformFlags.ValidateNewGreaterBaseVersion;
                                    break;
                                default:
                                    this.messageHandler.Display(this, WixErrors.ExpectedArgument(parameter));
                                    break;
                            }
                        }
                    }
                    else if ("x" == parameter)
                    {
                        this.exportBasePath = CommandLine.GetDirectory(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.exportBasePath))
                        {
                            return;
                        }
                    }
                    else if ("xi" == parameter)
                    {
                        this.xmlInputs = true;
                    }
                    else if ("xo" == parameter)
                    {
                        this.xmlOutput = true;
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
                    string inputFile = CommandLine.VerifyPath(this.messageHandler, arg);

                    if (String.IsNullOrEmpty(inputFile))
                    {
                        return;
                    }

                    this.inputFiles.Add(inputFile);
                }
            }
        }
    }
}
