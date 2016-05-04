// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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

    /// <summary>
    /// The main entry point for Smoke.
    /// </summary>
    public sealed class Smoke
    {
        private const string msm = ".msm";
        private const string msi = ".msi";
        private const string msp = ".msp";

        private bool addDefault;
        private StringCollection extensionList;
        private StringCollection ices;
        private StringCollection inputFiles;
        private StringCollection invalidArgs;
        private ConsoleMessageHandler messageHandler;
        private string pdbPath;
        private bool showHelp;
        private bool showLogo;
        private StringCollection suppressICEs;
        private bool tidy;
        private Validator validator;

        /// <summary>
        /// Instantiate a new Smoke class.
        /// </summary>
        private Smoke()
        {
            this.extensionList = new StringCollection();
            this.ices = new StringCollection();
            this.inputFiles = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.messageHandler = new ConsoleMessageHandler("SMOK", "smoke.exe");
            this.addDefault = true;
            this.showLogo = true;
            this.suppressICEs = new StringCollection();
            this.tidy = true;
            this.validator = new Validator();
        }

        /// <summary>
        /// The main entry point for smoke.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Smoke smoke = new Smoke();
            return smoke.Run(args);
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

                if (0 == this.inputFiles.Count)
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(SmokeStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return this.messageHandler.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    this.messageHandler.Display(this, WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                validator.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                // load any extensions
                bool validatorExtensionLoaded = false;
                foreach (string extension in this.extensionList)
                {
                    WixExtension wixExtension = WixExtension.Load(extension);

                    ValidatorExtension validatorExtension = wixExtension.ValidatorExtension;
                    if (null != validatorExtension)
                    {
                        if (validatorExtensionLoaded)
                        {
                            throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, SmokeStrings.EXP_CannotLoadLinkerExtension, validatorExtension.GetType().ToString(), validator.Extension.ToString()), "ext");
                        }

                        validator.Extension = validatorExtension;
                        validatorExtensionLoaded = true;
                    }
                }

                // set the message handlers
                validator.Extension.Message += new MessageEventHandler(this.messageHandler.Display);

                // disable ICE33 and ICE66 by default
                this.suppressICEs.Add("ICE33");
                this.suppressICEs.Add("ICE66");

                // set the ICEs
                string[] iceArray = new string[this.ices.Count];
                this.ices.CopyTo(iceArray, 0);
                validator.ICEs = iceArray;

                // set the suppressed ICEs
                string[] suppressICEArray = new string[this.suppressICEs.Count];
                this.suppressICEs.CopyTo(suppressICEArray, 0);
                validator.SuppressedICEs = suppressICEArray;

                // Load the pdb and assign the Output to the validator
                if (null != pdbPath)
                {
                    string pdbFullPath = Path.GetFullPath(pdbPath);
                    Pdb pdb = Pdb.Load(pdbFullPath, false, false);
                    this.validator.Output = pdb.Output;
                }

                foreach (string inputFile in this.inputFiles)
                {
                    // set the default cube file
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    string appDirectory = Path.GetDirectoryName(assembly.Location);

                    if (this.addDefault)
                    {
                           switch (Path.GetExtension(inputFile).ToLower(CultureInfo.InvariantCulture))
                        {
                            case msm:
                                validator.AddCubeFile(Path.Combine(appDirectory, "mergemod.cub"));
                                break;
                            case msi:
                                validator.AddCubeFile(Path.Combine(appDirectory, "darice.cub"));
                                break;
                            default:
                                throw new WixException(WixErrors.UnexpectedFileExtension(inputFile, ".msi, .msm"));
                        }
                    }

                    // print friendly message saying what file is being validated
                    Console.WriteLine(Path.GetFileName(inputFile));
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    
                    try
                    {
                        validator.Validate(Path.GetFullPath(inputFile));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        this.messageHandler.Display(this, WixErrors.UnauthorizedAccess(Path.GetFullPath(inputFile)));
                    }
                    finally
                    {
                        stopwatch.Stop();
                        this.messageHandler.Display(this, WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));
                      
                        if (this.tidy)
                        {
                            if (!validator.DeleteTempFiles())
                            {
                                Console.WriteLine(SmokeStrings.WAR_FailedToDeleteTempDir, validator.TempFilesLocation);
                            }
                        }
                        else
                        {
                            Console.WriteLine(SmokeStrings.INF_TempDirLocatedAt, validator.TempFilesLocation);
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

                // skip blank arguments
                if (null == arg || 0 == arg.Length)
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    if ("cub" == parameter)
                    {
                        string cubeFile = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(cubeFile))
                        {
                            return;
                        }

                        this.validator.AddCubeFile(cubeFile);
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
                    else if (parameter.StartsWith("ice:"))
                    {
                        this.ices.Add(parameter.Substring(4));
                    }
                    else if ("pdb" == parameter)
                    {
                        this.pdbPath = CommandLine.GetFile(parameter, this.messageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.pdbPath))
                        {
                            return;
                        }
                    }
                    else if ("nodefault" == parameter)
                    {
                        this.addDefault = false;
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.tidy = false;
                    }
                    else if (parameter.StartsWith("sice:"))
                    {
                        this.suppressICEs.Add(parameter.Substring(5));
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
                    // Verify the file extension is an expected value
                    if (IsValidFileExtension(arg))
                    {
                        this.inputFiles.AddRange(AppCommon.GetFiles(arg, "Source"));
                    }
                }
            }
        }

        /// <summary>
        /// Examines the file extension to determine if it is for a supported setup file extension.
        /// MSP file extensions are not currently supported and are flagged as invalid.
        /// </summary>
        /// <param name="searchPath">Search path to find files in.</param>
        /// <returns></returns>
        private bool IsValidFileExtension(string searchPath)
        {
            bool isFileValid = false;
            string extension = null;

            try
            {
                extension = Path.GetExtension(searchPath).ToLower(CultureInfo.InvariantCulture);
            }
            catch (ArgumentException)
            {
                // The path contains one or more invalid characters.
                this.messageHandler.Display(this, WixErrors.SmokeMalformedPath());
                // Can not continue further validation of the filename because an invalid character exists in the path.
                // GetExtension threw an ArgumentException before it extracted the extension so we don't know if a valid 
                // file extension is present.  
                //
                // Example input: "|\Setup.msi" or if a control character is present such as ctrl-o "^O\Setup.msi"
                //
                // Either example string would cause Path.GetExtension() to throw an ArgumentException and return null.
                // If we continued validating, the null returned by Path.GetExtension() would be flagged as unknown
                // even though the file extension in the examples given is valid.
                return false;
            }

            if (String.IsNullOrEmpty(extension))
            {
                // Display the unknown extension message if the file extension isn't present.
                this.messageHandler.Display(this, WixErrors.SmokeUnknownFileExtension());
                // Do not continue validating the file extension because there is no file extension to examine.
                return false;
            }

            switch (extension)
            {
                case msm:
                case msi:
                    // The file extension found is supported.
                    isFileValid = true;
                    break;
                case msp:
                    // The file extension found is not currently supported.
                    this.messageHandler.Display(this, WixErrors.SmokeUnsupportedFileExtension());
                    break;
                default:
                    // The file extension was not recognized and is not supported.
                    this.messageHandler.Display(this, WixErrors.SmokeUnknownFileExtension());
                    break;
            }

            return isFileValid;
        }
    }
}
