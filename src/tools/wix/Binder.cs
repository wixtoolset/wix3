// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using System.Xml.XPath;
    using Microsoft.Tools.WindowsInstallerXml.Cab;
    using Microsoft.Tools.WindowsInstallerXml.CLR.Interop;
    using Microsoft.Tools.WindowsInstallerXml.MergeMod;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    // TODO: (4.0) Refactor so that these don't need to be copied.
    // Copied verbatim from ext\UtilExtension\wixext\UtilCompiler.cs
    [Flags]
    internal enum WixFileSearchAttributes
    {
        Default = 0x001,
        MinVersionInclusive = 0x002,
        MaxVersionInclusive = 0x004,
        MinSizeInclusive = 0x008,
        MaxSizeInclusive = 0x010,
        MinDateInclusive = 0x020,
        MaxDateInclusive = 0x040,
        WantVersion = 0x080,
        WantExists = 0x100,
        IsDirectory = 0x200,
    }

    [Flags]
    internal enum WixRegistrySearchAttributes
    {
        Raw = 0x01,
        Compatible = 0x02,
        ExpandEnvironmentVariables = 0x04,
        WantValue = 0x08,
        WantExists = 0x10,
        Win64 = 0x20,
    }

    internal enum WixComponentSearchAttributes
    {
        KeyPath = 0x1,
        State = 0x2,
        WantDirectory = 0x4,
    }

    [Flags]
    internal enum WixProductSearchAttributes
    {
        Version = 0x01,
        Language = 0x02,
        State = 0x04,
        Assignment = 0x08,
        UpgradeCode = 0x10,
    }


    /// <summary>
    /// Binder core of the Windows Installer Xml toolset.
    /// </summary>
    public sealed class Binder : WixBinder, IDisposable
    {
        // as outlined in RFC 4122, this is our namespace for generating name-based (version 3) UUIDs
        private static readonly Guid WixComponentGuidNamespace = new Guid("{3064E5C6-FB63-4FE9-AC49-E446A792EFA5}");

        // String to be used for setting Row values to NULL that are non-nullable
        private static readonly string NullString = ((char)21).ToString();

        internal static readonly string PARAM_SPSD_NAME = "SuppressPatchSequenceData";

        // The following constants must stay in sync with src\burn\engine\core.h
        private const string BURN_BUNDLE_NAME = "WixBundleName";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE = "WixBundleOriginalSource";
        private const string BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = "WixBundleOriginalSourceFolder";
        private const string BURN_BUNDLE_LAST_USED_SOURCE = "WixBundleLastUsedSource";

        private string emptyFile;

        private bool backwardsCompatibleGuidGen;
        private int cabbingThreadCount;
        private string cabCachePath;
        private Cab.CompressionLevel defaultCompressionLevel;
        private bool exactAssemblyVersions;
        private bool setMsiAssemblyNameFileVersion;
        private string pdbFile;
        private bool reuseCabinets;
        private bool suppressAssemblies;
        private bool suppressAclReset;
        private bool suppressFileHashAndInfo;
        private StringCollection suppressICEs;
        private bool suppressLayout;
        private bool suppressPatchSequenceData;
        private bool suppressWixPdb;
        private bool suppressValidation;

        private bool allowEmptyTransforms;
        private StringCollection nonEmptyProductCodes;
        private StringCollection nonEmptyTransformNames;
        private StringCollection emptyTransformNames;

        private StringCollection ices;
        private StringCollection invalidArgs;
        private bool suppressAddingValidationRows;
        private Validator validator;

        private string contentsFile;
        private string outputsFile;
        private string builtOutputsFile;
        private string wixprojectFile;

        // Following object handles are needed by the NewCabNamesCallBack callback
        private ArrayList fileTransfers; // File Transfers for BindDatabase Only and not the one for BindBundle
        private Output output;
        internal FileSplitCabNamesCallback newCabNamesCallBack;
        private Dictionary<string, string> lastCabinetAddedToMediaTable; // Key is First Cabinet Name, Value is Last Cabinet Added in the Split Sequence

        /// <summary>
        /// Creates an MSI binder.
        /// </summary>
        public Binder()
        {
            this.defaultCompressionLevel = Cab.CompressionLevel.Mszip;
            this.suppressICEs = new StringCollection();

            this.ices = new StringCollection();
            this.invalidArgs = new StringCollection();
            this.validator = new Validator();

            // Need fileTransfers handle for NewCabNamesCallBack callback
            this.fileTransfers = new ArrayList();
            this.newCabNamesCallBack = NewCabNamesCallBack;
            this.lastCabinetAddedToMediaTable = new Dictionary<string, string>();

            this.nonEmptyProductCodes = new StringCollection();
            this.nonEmptyTransformNames = new StringCollection();
            this.emptyTransformNames = new StringCollection();
        }

        /// <summary>
        /// Gets or sets whether the GUID generation should use a backwards
        /// compatible version (i.e. MD5).
        /// </summary>
        public bool BackwardsCompatibleGuidGen
        {
            get { return this.backwardsCompatibleGuidGen; }
            set { this.backwardsCompatibleGuidGen = value; }
        }

        /// <summary>
        /// Gets or sets the number of threads to use for cabinet creation.
        /// </summary>
        /// <value>The number of threads to use for cabinet creation.</value>
        public int CabbingThreadCount
        {
            get { return this.cabbingThreadCount; }
            set { this.cabbingThreadCount = value; }
        }

        /// <summary>
        /// Gets or sets the default compression level to use for cabinets
        /// that don't have their compression level explicitly set.
        /// </summary>
        public Cab.CompressionLevel DefaultCompressionLevel
        {
            get { return this.defaultCompressionLevel; }
            set { this.defaultCompressionLevel = value; }
        }

        /// <summary>
        /// Gets or sets the exact assembly versions flag (see docs).
        /// </summary>
        public bool ExactAssemblyVersions
        {
            get { return this.exactAssemblyVersions; }
            set { this.exactAssemblyVersions = value; }
        }

        /// <summary>
        /// Gets and sets the location to save the WixPdb.
        /// </summary>
        /// <value>The location in which to save the WixPdb. Null if the the WixPdb should not be output.</value>
        public string PdbFile
        {
            get { return this.pdbFile; }
            set { this.pdbFile = value; }
        }

        /// <summary>
        /// Gets and sets the option to set the file version in the MsiAssemblyName table.
        /// </summary>
        /// <value>The option to set the file version in the MsiAssemblyName table.</value>
        public bool SetMsiAssemblyNameFileVersion
        {
            get { return this.setMsiAssemblyNameFileVersion; }
            set { this.setMsiAssemblyNameFileVersion = value; }
        }

        /// <summary>
        /// Gets and sets the option to suppress resetting ACLs by the binder.
        /// </summary>
        /// <value>The option to suppress resetting ACLs by the binder.</value>
        public bool SuppressAclReset
        {
            get { return this.suppressAclReset; }
            set { this.suppressAclReset = value; }
        }

        /// <summary>
        /// Gets and sets the option to suppress adding _Validation rows.
        /// </summary>
        /// <value>The option to suppress adding _Validation rows.</value>
        public bool SuppressAddingValidationRows
        {
            get { return this.suppressAddingValidationRows; }
            set { this.suppressAddingValidationRows = value; }
        }

        /// <summary>
        /// Gets and sets the option to suppress grabbing assembly name information from assemblies.
        /// </summary>
        /// <value>The option to suppress grabbing assembly name information from assemblies.</value>
        public bool SuppressAssemblies
        {
            get { return this.suppressAssemblies; }
            set { this.suppressAssemblies = value; }
        }

        /// <summary>
        /// Gets and sets the option to suppress grabbing the file hash, version and language at link time.
        /// </summary>
        /// <value>The option to suppress grabbing the file hash, version and language.</value>
        public bool SuppressFileHashAndInfo
        {
            get { return this.suppressFileHashAndInfo; }
            set { this.suppressFileHashAndInfo = value; }
        }

        /// <summary>
        /// Gets and sets the option to suppress creating an image for MSI/MSM.
        /// </summary>
        /// <value>The option to suppress creating an image for MSI/MSM.</value>
        public bool SuppressLayout
        {
            get { return this.suppressLayout; }
            set { this.suppressLayout = value; }
        }

        /// <summary>
        /// Gets and sets the option to allow patching despite having one or more empty transforms.
        /// </summary>
        /// <value>The option to allow patching despite having one or more empty transforms.</value>
        public bool AllowEmptyTransforms
        {
            get { return this.allowEmptyTransforms; }
            set { this.allowEmptyTransforms = value; }
        }


        /// <summary>
        /// Gets and sets the option to suppress MSI/MSM validation.
        /// </summary>
        /// <value>The option to suppress MSI/MSM validation.</value>
        /// <remarks>This must be set before calling Bind.</remarks>
        public bool SuppressValidation
        {
            get
            {
                return this.suppressValidation;
            }

            set
            {
                // make a new validator if validation has been turned off and is now being turned back on
                if (!value && null == this.validator)
                {
                    this.validator = new Validator();
                }

                this.suppressValidation = value;
            }
        }

        /// <summary>
        /// Gets help for all the command line arguments for this binder.
        /// </summary>
        /// <returns>A string to be added to light's help string.</returns>
        public override string GetCommandLineArgumentsHelpString()
        {
            return WixStrings.BinderArguments;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        /// <param name="consoleMessageHandler">The console message handler.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings are not round tripped, and have no security impact")]
        public override StringCollection ParseCommandLine(string[] args, ConsoleMessageHandler consoleMessageHandler)
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
                    if (parameter.Equals("bcgg", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch(parameter));
                        this.backwardsCompatibleGuidGen = true;
                    }
                    else if (parameter.Equals("cc", StringComparison.Ordinal))
                    {
                        this.cabCachePath = CommandLine.GetDirectory(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.cabCachePath))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("ct", StringComparison.Ordinal))
                    {
                        if (!CommandLine.IsValidArg(args, ++i))
                        {
                            consoleMessageHandler.Display(this, WixErrors.IllegalCabbingThreadCount(String.Empty));
                            return this.invalidArgs;
                        }

                        try
                        {
                            this.cabbingThreadCount = Convert.ToInt32(args[i], CultureInfo.InvariantCulture.NumberFormat);

                            if (0 >= this.cabbingThreadCount)
                            {
                                consoleMessageHandler.Display(this, WixErrors.IllegalCabbingThreadCount(args[i]));
                            }

                            consoleMessageHandler.Display(this, WixVerboses.SetCabbingThreadCount(this.cabbingThreadCount.ToString()));
                        }
                        catch (FormatException)
                        {
                            consoleMessageHandler.Display(this, WixErrors.IllegalCabbingThreadCount(args[i]));
                        }
                        catch (OverflowException)
                        {
                            consoleMessageHandler.Display(this, WixErrors.IllegalCabbingThreadCount(args[i]));
                        }
                    }
                    else if (parameter.Equals("cub", StringComparison.Ordinal))
                    {
                        string cubeFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(cubeFile))
                        {
                            return this.invalidArgs;
                        }

                        this.validator.AddCubeFile(cubeFile);
                    }
                    else if (parameter.StartsWith("dcl:", StringComparison.Ordinal))
                    {
                        string defaultCompressionLevel = arg.Substring(5);

                        if (String.IsNullOrEmpty(defaultCompressionLevel))
                        {
                            return this.invalidArgs;
                        }

                        this.defaultCompressionLevel = WixCreateCab.CompressionLevelFromString(defaultCompressionLevel);
                    }
                    else if (parameter.Equals("eav", StringComparison.Ordinal))
                    {
                        this.exactAssemblyVersions = true;
                    }
                    else if (parameter.Equals("fv", StringComparison.Ordinal))
                    {
                        this.setMsiAssemblyNameFileVersion = true;
                    }
                    else if (parameter.StartsWith("ice:", StringComparison.Ordinal))
                    {
                        this.ices.Add(parameter.Substring(4));
                    }
                    else if (parameter.Equals("contentsfile", StringComparison.Ordinal))
                    {
                        this.contentsFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.contentsFile))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("outputsfile", StringComparison.Ordinal))
                    {
                        this.outputsFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.outputsFile))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("builtoutputsfile", StringComparison.Ordinal))
                    {
                        this.builtOutputsFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.builtOutputsFile))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("wixprojectfile", StringComparison.Ordinal))
                    {
                        this.wixprojectFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.wixprojectFile))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("O1", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("O1"));
                    }
                    else if (parameter.Equals("O2", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch("O2"));
                    }
                    else if (parameter.Equals("pdbout", StringComparison.Ordinal))
                    {
                        this.pdbFile = CommandLine.GetFile(parameter, consoleMessageHandler, args, ++i);

                        if (String.IsNullOrEmpty(this.pdbFile))
                        {
                            return this.invalidArgs;
                        }
                    }
                    else if (parameter.Equals("reusecab", StringComparison.Ordinal))
                    {
                        this.reuseCabinets = true;
                    }
                    else if (parameter.Equals("sa", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch(parameter));
                        this.suppressAssemblies = true;
                    }
                    else if (parameter.Equals("sacl", StringComparison.Ordinal))
                    {
                        this.suppressAclReset = true;
                    }
                    else if (parameter.Equals("sbuildinfo", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch(parameter));
                    }
                    else if (parameter.Equals("sf", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch(parameter));
                        this.suppressAssemblies = true;
                        this.suppressFileHashAndInfo = true;
                    }
                    else if (parameter.Equals("sh", StringComparison.Ordinal))
                    {
                        consoleMessageHandler.Display(this, WixWarnings.DeprecatedCommandLineSwitch(parameter));
                        this.suppressFileHashAndInfo = true;
                    }
                    else if (parameter.StartsWith("sice:", StringComparison.Ordinal))
                    {
                        this.suppressICEs.Add(parameter.Substring(5));
                    }
                    else if (parameter.Equals("sl", StringComparison.Ordinal))
                    {
                        this.suppressLayout = true;
                    }
                    else if (parameter.Equals("spdb", StringComparison.Ordinal))
                    {
                        this.suppressWixPdb = true;
                    }
                    else if (parameter.Equals("spsd", StringComparison.Ordinal))
                    {
                        this.suppressPatchSequenceData = true;
                    }
                    else if (parameter.Equals("sval", StringComparison.Ordinal))
                    {
                        this.suppressValidation = true;
                    }
                    else
                    {
                        this.invalidArgs.Add(arg);
                    }
                }
                else
                {
                    this.invalidArgs.Add(arg);
                }
            }

            this.pdbFile = this.suppressWixPdb ? null : this.pdbFile;

            return this.invalidArgs;
        }

        /// <summary>
        /// Do any setting up needed after all command line parsing.
        /// </summary>
        public override void PostParseCommandLine()
        {
            if (!this.suppressWixPdb && null == this.pdbFile && null != this.OutputFile)
            {
                this.pdbFile = Path.ChangeExtension(this.OutputFile, ".wixpdb");
            }
        }

        /// <summary>
        /// Adds an event handler.
        /// </summary>
        /// <param name="newHandler">The event handler to add.</param>
        public override void AddMessageEventHandler(MessageEventHandler newHandler)
        {
            base.AddMessageEventHandler(newHandler);
            validator.Extension.Message += newHandler;
        }

        /// <summary>
        /// Binds an output.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="file">The Windows Installer file to create.</param>
        /// <remarks>The Binder.DeleteTempFiles method should be called after calling this method.</remarks>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        public override bool Bind(Output output, string file)
        {
            // Need output object handle for NewCabNamesCallBack callback
            this.output = output;

            // ensure the cabinet cache path exists if we are going to use it
            if (null != this.cabCachePath && !Directory.Exists(this.cabCachePath))
            {
                Directory.CreateDirectory(this.cabCachePath);
            }

            // tell the binder about the validator if validation isn't suppressed
            if (!this.suppressValidation && (OutputType.Module == output.Type || OutputType.Product == output.Type))
            {
                if (String.IsNullOrEmpty(this.validator.TempFilesLocation))
                {
                    this.validator.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");
                }

                // set the default cube file
                Assembly lightAssembly = Assembly.GetExecutingAssembly();
                string lightDirectory = Path.GetDirectoryName(lightAssembly.Location);
                if (OutputType.Module == output.Type)
                {
                    this.validator.AddCubeFile(Path.Combine(lightDirectory, "mergemod.cub"));
                }
                else // product
                {
                    this.validator.AddCubeFile(Path.Combine(lightDirectory, "darice.cub"));
                }

                // by default, disable ICEs that have equivalent-or-better checks in WiX
                this.suppressICEs.Add("ICE08");
                this.suppressICEs.Add("ICE33");
                this.suppressICEs.Add("ICE47");
                this.suppressICEs.Add("ICE66");

                // set the ICEs
                string[] iceArray = new string[this.ices.Count];
                this.ices.CopyTo(iceArray, 0);
                this.validator.ICEs = iceArray;

                // set the suppressed ICEs
                string[] suppressICEArray = new string[this.suppressICEs.Count];
                this.suppressICEs.CopyTo(suppressICEArray, 0);
                this.validator.SuppressedICEs = suppressICEArray;
            }
            else
            {
                this.validator = null;
            }

            this.core = new BinderCore(this.MessageHandler);
            this.FileManager.MessageHandler = this.core;

            // Add properties we need to pass to other classes.
            if (this.suppressPatchSequenceData)
            {
                // Avoid unnecessary boxing if false.
                this.core.SetProperty(Binder.PARAM_SPSD_NAME, true);
            }

            this.core.SetProperty(BinderCore.OutputPath, file);

            if (!String.IsNullOrEmpty(this.outputsFile))
            {
                this.core.SetProperty(BinderCore.IntermediateFolder, Path.GetDirectoryName(this.outputsFile));
            }
            else if (!String.IsNullOrEmpty(this.contentsFile))
            {
                this.core.SetProperty(BinderCore.IntermediateFolder, Path.GetDirectoryName(this.contentsFile));
            }
            else if (!String.IsNullOrEmpty(this.builtOutputsFile))
            {
                this.core.SetProperty(BinderCore.IntermediateFolder, Path.GetDirectoryName(this.builtOutputsFile));
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.Core = this.core;
            }

            if (null == output)
            {
                throw new ArgumentNullException("output");
            }

            this.core.EncounteredError = false;

            switch (output.Type)
            {
                case OutputType.Bundle:
                    return this.BindBundle(output, file);
                case OutputType.Transform:
                    return this.BindTransform(output, file);
                default:
                    return this.BindDatabase(output, file);
            }
        }

        /// <summary>
        /// Does any housekeeping after Bind.
        /// </summary>
        /// <param name="tidy">Whether or not any actual tidying should be done.</param>
        public override void Cleanup(bool tidy)
        {
            // If Bind hasn't been called yet, core will be null. There will be
            // nothing to cleanup.
            if (this.core == null)
            {
                return;
            }

            if (tidy)
            {
                if (!this.DeleteTempFiles())
                {
                    this.core.OnMessage(WixWarnings.FailedToDeleteTempDir(this.TempFilesLocation));
                }
            }
            else
            {
                this.core.OnMessage(WixVerboses.BinderTempDirLocatedAt(this.TempFilesLocation));
            }

            if (null != this.validator && !String.IsNullOrEmpty(this.validator.TempFilesLocation))
            {
                if (tidy)
                {
                    if (!this.validator.DeleteTempFiles())
                    {
                        this.core.OnMessage(WixWarnings.FailedToDeleteTempDir(this.validator.TempFilesLocation));
                    }
                }
                else
                {
                    this.core.OnMessage(WixVerboses.ValidatorTempDirLocatedAt(this.validator.TempFilesLocation));
                }
            }
        }

        /// <summary>
        /// Cleans up the temp files used by the Binder.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public override bool DeleteTempFiles()
        {
            bool deleted = base.DeleteTempFiles();
            if (deleted)
            {
                this.emptyFile = null;
            }

            return deleted;
        }

        /// <summary>
        /// Process a list of loaded extensions.
        /// </summary>
        /// <param name="loadedExtensionList">The list of loaded extensions.</param>
        public override void ProcessExtensions(WixExtension[] loadedExtensionList)
        {
            bool binderFileManagerLoaded = false;
            bool validatorExtensionLoaded = false;

            foreach (WixExtension wixExtension in loadedExtensionList)
            {
                if (null != wixExtension.BinderFileManager)
                {
                    if (binderFileManagerLoaded)
                    {
                        core.OnMessage(WixErrors.CannotLoadBinderFileManager(wixExtension.BinderFileManager.GetType().ToString(), this.FileManager.ToString()));
                    }

                    this.FileManager = wixExtension.BinderFileManager;
                    binderFileManagerLoaded = true;
                }

                ValidatorExtension validatorExtension = wixExtension.ValidatorExtension;
                if (null != validatorExtension)
                {
                    if (validatorExtensionLoaded)
                    {
                        core.OnMessage(WixErrors.CannotLoadLinkerExtension(validatorExtension.GetType().ToString(), this.validator.Extension.ToString()));
                    }

                    this.validator.Extension = validatorExtension;
                    validatorExtensionLoaded = true;
                }
            }

            this.FileManager.CabCachePath = this.cabCachePath;
            this.FileManager.ReuseCabinets = this.reuseCabinets;
        }

        /// <summary>
        /// Creates the MSI/MSM/PCP database.
        /// </summary>
        /// <param name="output">Output to create database for.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <param name="keepAddedColumns">Whether to keep columns added in a transform.</param>
        /// <param name="useSubdirectory">Whether to use a subdirectory based on the <paramref name="databaseFile"/> file name for intermediate files.</param>
        internal void GenerateDatabase(Output output, string databaseFile, bool keepAddedColumns, bool useSubdirectory)
        {
            // add the _Validation rows
            if (!this.suppressAddingValidationRows)
            {
                Table validationTable = output.EnsureTable(this.core.TableDefinitions["_Validation"]);

                foreach (Table table in output.Tables)
                {
                    if (!table.Definition.IsUnreal)
                    {
                        // add the validation rows for this table
                        table.Definition.AddValidationRows(validationTable);
                    }
                }
            }

            // set the base directory
            string baseDirectory = this.TempFilesLocation;
            if (useSubdirectory)
            {
                string filename = Path.GetFileNameWithoutExtension(databaseFile);
                baseDirectory = Path.Combine(baseDirectory, filename);

                // make sure the directory exists
                Directory.CreateDirectory(baseDirectory);
            }

            try
            {
                OpenDatabase type = OpenDatabase.CreateDirect;

                // set special flag for patch files
                if (OutputType.Patch == output.Type)
                {
                    type |= OpenDatabase.OpenPatchFile;
                }

                // try to create the database
                using (Database db = new Database(databaseFile, type))
                {
                    // localize the codepage if a value was specified by the localizer
                    if (null != this.Localizer && -1 != this.Localizer.Codepage)
                    {
                        output.Codepage = this.Localizer.Codepage;
                    }

                    // if we're not using the default codepage, import a new one into our
                    // database before we add any tables (or the tables would be added
                    // with the wrong codepage)
                    if (0 != output.Codepage)
                    {
                        this.SetDatabaseCodepage(db, output);
                    }

                    // insert substorages (like transforms inside a patch)
                    if (0 < output.SubStorages.Count)
                    {
                        using (View storagesView = new View(db, "SELECT `Name`, `Data` FROM `_Storages`"))
                        {
                            foreach (SubStorage subStorage in output.SubStorages)
                            {
                                string transformFile = Path.Combine(this.TempFilesLocation, String.Concat(subStorage.Name, ".mst"));

                                // bind the transform
                                if (this.BindTransform(subStorage.Data, transformFile))
                                {
                                    // add the storage
                                    using (Record record = new Record(2))
                                    {
                                        record.SetString(1, subStorage.Name);
                                        record.SetStream(2, transformFile);
                                        storagesView.Modify(ModifyView.Assign, record);
                                    }
                                }
                            }
                        }

                        // some empty transforms may have been excluded
                        // we need to remove these from the final patch summary information
                        if (OutputType.Patch == output.Type && this.AllowEmptyTransforms)
                        {
                            Table patchSummaryInfo = output.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
                            for (int i = patchSummaryInfo.Rows.Count - 1; i >= 0; i--)
                            {
                                Row row = patchSummaryInfo.Rows[i];
                                if ((int)SummaryInformation.Patch.ProductCodes == (int)row[0])
                                {
                                    if (nonEmptyProductCodes.Count > 0)
                                    {
                                        string[] productCodes = new string[nonEmptyProductCodes.Count];
                                        nonEmptyProductCodes.CopyTo(productCodes, 0);
                                        row[1] = String.Join(";", productCodes);
                                    }
                                    else
                                    {
                                        row[1] = Binder.NullString;
                                    }
                                }
                                else if ((int)SummaryInformation.Patch.TransformNames == (int)row[0])
                                {
                                    if (nonEmptyTransformNames.Count > 0)
                                    {
                                        string[] transformNames = new string[nonEmptyTransformNames.Count];
                                        nonEmptyTransformNames.CopyTo(transformNames, 0);
                                        row[1] = String.Join(";", transformNames);
                                    }
                                    else
                                    {
                                        row[1] = Binder.NullString;
                                    }
                                }
                            }
                        }
                    }

                    foreach (Table table in output.Tables)
                    {
                        Table importTable = table;
                        bool hasBinaryColumn = false;

                        // skip all unreal tables other than _Streams
                        if (table.Definition.IsUnreal && "_Streams" != table.Name)
                        {
                            continue;
                        }

                        // Do not put the _Validation table in patches, it is not needed
                        if (OutputType.Patch == output.Type && "_Validation" == table.Name)
                        {
                            continue;
                        }

                        // The only way to import binary data is to copy it to a local subdirectory first.
                        // To avoid this extra copying and perf hit, import an empty table with the same
                        // definition and later import the binary data from source using records.
                        foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
                        {
                            if (ColumnType.Object == columnDefinition.Type)
                            {
                                importTable = new Table(table.Section, table.Definition);
                                hasBinaryColumn = true;
                                break;
                            }
                        }

                        // create the table via IDT import
                        if ("_Streams" != importTable.Name)
                        {
                            try
                            {
                                db.ImportTable(output.Codepage, this.core, importTable, baseDirectory, keepAddedColumns);
                            }
                            catch (WixInvalidIdtException)
                            {
                                // If ValidateRows finds anything it doesn't like, it throws
                                importTable.ValidateRows();

                                // Otherwise we rethrow the InvalidIdt
                                throw;
                            }
                        }

                        // insert the rows via SQL query if this table contains object fields
                        if (hasBinaryColumn)
                        {
                            StringBuilder query = new StringBuilder("SELECT ");

                            // build the query for the view
                            bool firstColumn = true;
                            foreach (ColumnDefinition columnDefinition in table.Definition.Columns)
                            {
                                if (!firstColumn)
                                {
                                    query.Append(",");
                                }
                                query.AppendFormat(" `{0}`", columnDefinition.Name);
                                firstColumn = false;
                            }
                            query.AppendFormat(" FROM `{0}`", table.Name);

                            using (View tableView = db.OpenExecuteView(query.ToString()))
                            {
                                // import each row containing a stream
                                foreach (Row row in table.Rows)
                                {
                                    using (Record record = new Record(table.Definition.Columns.Count))
                                    {
                                        StringBuilder streamName = new StringBuilder();
                                        bool needStream = false;

                                        // the _Streams table doesn't prepend the table name (or a period)
                                        if ("_Streams" != table.Name)
                                        {
                                            streamName.Append(table.Name);
                                        }

                                        for (int i = 0; i < table.Definition.Columns.Count; i++)
                                        {
                                            ColumnDefinition columnDefinition = table.Definition.Columns[i];

                                            switch (columnDefinition.Type)
                                            {
                                                case ColumnType.Localized:
                                                case ColumnType.Preserved:
                                                case ColumnType.String:
                                                    if (columnDefinition.IsPrimaryKey)
                                                    {
                                                        if (0 < streamName.Length)
                                                        {
                                                            streamName.Append(".");
                                                        }
                                                        streamName.Append((string)row[i]);
                                                    }

                                                    record.SetString(i + 1, (string)row[i]);
                                                    break;
                                                case ColumnType.Number:
                                                    record.SetInteger(i + 1, Convert.ToInt32(row[i], CultureInfo.InvariantCulture));
                                                    break;
                                                case ColumnType.Object:
                                                    if (null != row[i])
                                                    {
                                                        needStream = true;
                                                        try
                                                        {
                                                            record.SetStream(i + 1, (string)row[i]);
                                                        }
                                                        catch (Win32Exception e)
                                                        {
                                                            if (0xA1 == e.NativeErrorCode) // ERROR_BAD_PATHNAME
                                                            {
                                                                throw new WixException(WixErrors.FileNotFound(row.SourceLineNumbers, (string)row[i]));
                                                            }
                                                            else
                                                            {
                                                                throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, e.Message));
                                                            }
                                                        }
                                                    }
                                                    break;
                                            }
                                        }

                                        // stream names are created by concatenating the name of the table with the values
                                        // of the primary key (delimited by periods)
                                        // check for a stream name that is more than 62 characters long (the maximum allowed length)
                                        if (needStream && MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                        {
                                            this.core.OnMessage(WixErrors.StreamNameTooLong(row.SourceLineNumbers, table.Name, streamName.ToString(), streamName.Length));
                                        }
                                        else // add the row to the database
                                        {
                                            tableView.Modify(ModifyView.Assign, record);
                                        }
                                    }
                                }
                            }

                            // Remove rows from the _Streams table for wixpdbs.
                            if ("_Streams" == table.Name)
                            {
                                table.Rows.Clear();
                            }
                        }
                    }

                    // we're good, commit the changes to the new MSI
                    db.Commit();
                }
            }
            catch (IOException)
            {
                // TODO: this error message doesn't seem specific enough
                throw new WixFileNotFoundException(SourceLineNumberCollection.FromFileName(databaseFile), databaseFile);
            }
        }

        /// <summary>
        /// Get the source path of a directory.
        /// </summary>
        /// <param name="directories">All cached directories.</param>
        /// <param name="componentIdGenSeeds">Hash table of Component GUID generation seeds indexed by directory id.</param>
        /// <param name="directory">Directory identifier.</param>
        /// <param name="canonicalize">Canonicalize the path for standard directories.</param>
        /// <returns>Source path of a directory.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way autogenerated GUIDs are generated. Furthermore, there is no security hole here, as the strings won't need to " +
                         "make a round trip")]
        private static string GetDirectoryPath(Hashtable directories, Hashtable componentIdGenSeeds, string directory, bool canonicalize)
        {
            if (!directories.Contains(directory))
            {
                throw new WixException(WixErrors.ExpectedDirectory(directory));
            }
            ResolvedDirectory resolvedDirectory = (ResolvedDirectory)directories[directory];

            if (null == resolvedDirectory.Path)
            {
                if (null != componentIdGenSeeds && componentIdGenSeeds.Contains(directory))
                {
                    resolvedDirectory.Path = (string)componentIdGenSeeds[directory];
                }
                else if (canonicalize && Util.IsStandardDirectory(directory))
                {
                    // when canonicalization is on, standard directories are treated equally
                    resolvedDirectory.Path = directory;
                }
                else
                {
                    string name = resolvedDirectory.Name;

                    if (canonicalize && null != name)
                    {
                        name = name.ToLower(CultureInfo.InvariantCulture);
                    }

                    if (String.IsNullOrEmpty(resolvedDirectory.DirectoryParent))
                    {
                        resolvedDirectory.Path = name;
                    }
                    else
                    {
                        string parentPath = GetDirectoryPath(directories, componentIdGenSeeds, resolvedDirectory.DirectoryParent, canonicalize);

                        if (null != resolvedDirectory.Name)
                        {
                            resolvedDirectory.Path = Path.Combine(parentPath, name);
                        }
                        else
                        {
                            resolvedDirectory.Path = parentPath;
                        }
                    }
                }
            }

            return resolvedDirectory.Path;
        }

        /// <summary>
        /// Gets the source path of a file.
        /// </summary>
        /// <param name="directories">All cached directories in <see cref="ResolvedDirectory"/>.</param>
        /// <param name="directoryId">Parent directory identifier.</param>
        /// <param name="fileName">File name (in long|source format).</param>
        /// <param name="compressed">Specifies the package is compressed.</param>
        /// <param name="useLongName">Specifies the package uses long file names.</param>
        /// <returns>Source path of file relative to package directory.</returns>
        internal static string GetFileSourcePath(Hashtable directories, string directoryId, string fileName, bool compressed, bool useLongName)
        {
            string fileSourcePath = Installer.GetName(fileName, true, useLongName);

            if (compressed)
            {
                // Use just the file name of the file since all uncompressed files must appear
                // in the root of the image in a compressed package.
            }
            else
            {
                // Get the relative path of where we want the file to be layed out as specified
                // in the Directory table.
                string directoryPath = Binder.GetDirectoryPath(directories, null, directoryId, false);
                fileSourcePath = Path.Combine(directoryPath, fileSourcePath);
            }

            // Strip off "SourceDir" if it's still on there.
            if (fileSourcePath.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                fileSourcePath = fileSourcePath.Substring(10);
            }

            return fileSourcePath;
        }

        /// <summary>
        /// Set an MsiAssemblyName row.  If it was directly authored, override the value, otherwise
        /// create a new row.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="assemblyNameTable">MsiAssemblyName table.</param>
        /// <param name="fileRow">FileRow containing the assembly read for the MsiAssemblyName row.</param>
        /// <param name="name">MsiAssemblyName name.</param>
        /// <param name="value">MsiAssemblyName value.</param>
        /// <param name="infoCache">Cache to populate with file information (optional).</param>
        /// <param name="modularizationGuid">The modularization GUID (in the case of merge modules).</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        private void SetMsiAssemblyName(Output output, Table assemblyNameTable, FileRow fileRow, string name, string value, IDictionary<string, string> infoCache, string modularizationGuid)
        {
            // check for null value (this can occur when grabbing the file version from an assembly without one)
            if (null == value || 0 == value.Length)
            {
                this.core.OnMessage(WixWarnings.NullMsiAssemblyNameValue(fileRow.SourceLineNumbers, fileRow.Component, name));
            }
            else
            {
                Row assemblyNameRow = null;

                // override directly authored value
                foreach (Row row in assemblyNameTable.Rows)
                {
                    if ((string)row[0] == fileRow.Component && (string)row[1] == name)
                    {
                        assemblyNameRow = row;
                        break;
                    }
                }

                // if the assembly will be GAC'd and the name in the file table doesn't match the name in the MsiAssemblyName table, error because the install will fail.
                if ("name" == name && FileAssemblyType.DotNetAssembly == fileRow.AssemblyType && String.IsNullOrEmpty(fileRow.AssemblyApplication) && !String.Equals(Path.GetFileNameWithoutExtension(fileRow.LongFileName), value, StringComparison.OrdinalIgnoreCase))
                {
                    this.core.OnMessage(WixErrors.GACAssemblyIdentityWarning(fileRow.SourceLineNumbers, Path.GetFileNameWithoutExtension(fileRow.LongFileName), value));
                }

                if (null == assemblyNameRow)
                {
                    assemblyNameRow = assemblyNameTable.CreateRow(fileRow.SourceLineNumbers);
                    assemblyNameRow[0] = fileRow.Component;
                    assemblyNameRow[1] = name;
                    assemblyNameRow[2] = value;

                    // put the MsiAssemblyName row in the same section as the related File row
                    assemblyNameRow.SectionId = fileRow.SectionId;

                    if (null == fileRow.AssemblyNameRows)
                    {
                        fileRow.AssemblyNameRows = new RowCollection();
                    }
                    fileRow.AssemblyNameRows.Add(assemblyNameRow);
                }
                else
                {
                    assemblyNameRow[2] = value;
                }

                if (infoCache != null)
                {
                    string key = String.Format(CultureInfo.InvariantCulture, "assembly{0}.{1}", name, Demodularize(output, modularizationGuid, fileRow.File)).ToLower(CultureInfo.InvariantCulture);
                    infoCache[key] = (string)assemblyNameRow[2];
                }
            }
        }

        /// <summary>
        /// Merge data from a row in the WixPatchSymbolsPaths table into an associated FileRow.
        /// </summary>
        /// <param name="row">Row from the WixPatchSymbolsPaths table.</param>
        /// <param name="fileRow">FileRow into which to set symbol information.</param>
        /// <comment>This includes PreviousData as well.</comment>
        private static void MergeSymbolPaths(Row row, FileRow fileRow)
        {
            if (null == fileRow.Symbols)
            {
                fileRow.Symbols = (string)row[2];
            }
            else
            {
                fileRow.Symbols = String.Concat(fileRow.Symbols, ";", (string)row[2]);
            }

            Field field = row.Fields[2];
            if (null != field.PreviousData)
            {
                if (null == fileRow.PreviousSymbols)
                {
                    fileRow.PreviousSymbols = field.PreviousData;
                }
                else
                {
                    fileRow.PreviousSymbols = String.Concat(fileRow.PreviousSymbols, ";", field.PreviousData);
                }
            }
        }

        /// <summary>
        /// Merge data from the unreal tables into the real tables.
        /// </summary>
        /// <param name="tables">Collection of all tables.</param>
        private void MergeUnrealTables(TableCollection tables)
        {
            // merge data from the WixBBControl rows into the BBControl rows
            Table wixBBControlTable = tables["WixBBControl"];
            Table bbControlTable = tables["BBControl"];
            if (null != wixBBControlTable && null != bbControlTable)
            {
                // index all the BBControl rows by their identifier
                Hashtable indexedBBControlRows = new Hashtable(bbControlTable.Rows.Count);
                foreach (BBControlRow bbControlRow in bbControlTable.Rows)
                {
                    indexedBBControlRows.Add(bbControlRow.GetPrimaryKey('/'), bbControlRow);
                }

                foreach (Row row in wixBBControlTable.Rows)
                {
                    BBControlRow bbControlRow = (BBControlRow)indexedBBControlRows[row.GetPrimaryKey('/')];

                    bbControlRow.SourceFile = (string)row[2];
                }
            }

            // merge data from the WixControl rows into the Control rows
            Table wixControlTable = tables["WixControl"];
            Table controlTable = tables["Control"];
            if (null != wixControlTable && null != controlTable)
            {
                // index all the Control rows by their identifier
                Hashtable indexedControlRows = new Hashtable(controlTable.Rows.Count);
                foreach (ControlRow controlRow in controlTable.Rows)
                {
                    indexedControlRows.Add(controlRow.GetPrimaryKey('/'), controlRow);
                }

                foreach (Row row in wixControlTable.Rows)
                {
                    ControlRow controlRow = (ControlRow)indexedControlRows[row.GetPrimaryKey('/')];

                    controlRow.SourceFile = (string)row[2];
                }
            }

            // merge data from the WixFile rows into the File rows
            Table wixFileTable = tables["WixFile"];
            Table fileTable = tables["File"];
            if (null != wixFileTable && null != fileTable)
            {
                // index all the File rows by their identifier
                Hashtable indexedFileRows = new Hashtable(fileTable.Rows.Count, StringComparer.OrdinalIgnoreCase);

                foreach (FileRow fileRow in fileTable.Rows)
                {
                    try
                    {
                        indexedFileRows.Add(fileRow.File, fileRow);
                    }
                    catch (ArgumentException)
                    {
                        this.core.OnMessage(WixErrors.DuplicateFileId(fileRow.File));
                    }
                }

                if (this.core.EncounteredError)
                {
                    return;
                }

                foreach (WixFileRow row in wixFileTable.Rows)
                {
                    FileRow fileRow = (FileRow)indexedFileRows[row.File];

                    if (null != row[1])
                    {
                        fileRow.AssemblyType = (FileAssemblyType)Enum.ToObject(typeof(FileAssemblyType), row.AssemblyAttributes);
                    }
                    else
                    {
                        fileRow.AssemblyType = FileAssemblyType.NotAnAssembly;
                    }
                    fileRow.AssemblyApplication = row.AssemblyApplication;
                    fileRow.AssemblyManifest = row.AssemblyManifest;
                    fileRow.Directory = row.Directory;
                    fileRow.DiskId = row.DiskId;
                    fileRow.Source = row.Source;
                    fileRow.PreviousSource = row.PreviousSource;
                    fileRow.ProcessorArchitecture = row.ProcessorArchitecture;
                    fileRow.PatchGroup = row.PatchGroup;
                    fileRow.PatchAttributes = row.PatchAttributes;
                    fileRow.RetainLengths = row.RetainLengths;
                    fileRow.IgnoreOffsets = row.IgnoreOffsets;
                    fileRow.IgnoreLengths = row.IgnoreLengths;
                    fileRow.RetainOffsets = row.RetainOffsets;
                    fileRow.PreviousRetainLengths = row.PreviousRetainLengths;
                    fileRow.PreviousIgnoreOffsets = row.PreviousIgnoreOffsets;
                    fileRow.PreviousIgnoreLengths = row.PreviousIgnoreLengths;
                    fileRow.PreviousRetainOffsets = row.PreviousRetainOffsets;
                }
            }

            // merge data from the WixPatchSymbolPaths rows into the File rows
            Table wixPatchSymbolPathsTable = tables["WixPatchSymbolPaths"];
            Table mediaTable = tables["Media"];
            Table directoryTable = tables["Directory"];
            Table componentTable = tables["Component"];
            if (null != wixPatchSymbolPathsTable)
            {
                int fileRowNum = (null != fileTable) ? fileTable.Rows.Count : 0;
                int componentRowNum = (null != fileTable) ? componentTable.Rows.Count : 0;
                int directoryRowNum = (null != directoryTable) ? directoryTable.Rows.Count : 0;
                int mediaRowNum = (null != mediaTable) ? mediaTable.Rows.Count : 0;

                Hashtable fileRowsByFile = new Hashtable(fileRowNum);
                Hashtable fileRowsByComponent = new Hashtable(componentRowNum);
                Hashtable fileRowsByDirectory = new Hashtable(directoryRowNum);
                Hashtable fileRowsByDiskId = new Hashtable(mediaRowNum);

                // index all the File rows by their identifier
                if (null != fileTable)
                {
                    foreach (FileRow fileRow in fileTable.Rows)
                    {
                        fileRowsByFile.Add(fileRow.File, fileRow);

                        ArrayList fileRows = (ArrayList)fileRowsByComponent[fileRow.Component];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByComponent.Add(fileRow.Component, fileRows);
                        }
                        fileRows.Add(fileRow);

                        fileRows = (ArrayList)fileRowsByDirectory[fileRow.Directory];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByDirectory.Add(fileRow.Directory, fileRows);
                        }
                        fileRows.Add(fileRow);

                        fileRows = (ArrayList)fileRowsByDiskId[fileRow.DiskId];
                        if (null == fileRows)
                        {
                            fileRows = new ArrayList();
                            fileRowsByDiskId.Add(fileRow.DiskId, fileRows);
                        }
                        fileRows.Add(fileRow);
                    }
                }

                wixPatchSymbolPathsTable.Rows.Sort(new WixPatchSymbolPathsComparer());

                foreach (Row row in wixPatchSymbolPathsTable.Rows)
                {
                    switch ((string)row[0])
                    {
                        case "File":
                            MergeSymbolPaths(row, (FileRow)fileRowsByFile[row[1]]);
                            break;

                        case "Product":
                            foreach (FileRow fileRow in fileRowsByFile)
                            {
                                MergeSymbolPaths(row, fileRow);
                            }
                            break;

                        case "Component":
                            ArrayList fileRowsByThisComponent = (ArrayList)(fileRowsByComponent[row[1]]);
                            if (null != fileRowsByThisComponent)
                            {
                                foreach (FileRow fileRow in fileRowsByThisComponent)
                                {
                                    MergeSymbolPaths(row, fileRow);
                                }
                            }

                            break;

                        case "Directory":
                            ArrayList fileRowsByThisDirectory = (ArrayList)(fileRowsByDirectory[row[1]]);
                            if (null != fileRowsByThisDirectory)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDirectory)
                                {
                                    MergeSymbolPaths(row, fileRow);
                                }
                            }
                            break;

                        case "Media":
                            ArrayList fileRowsByThisDiskId = (ArrayList)(fileRowsByDiskId[row[1]]);
                            if (null != fileRowsByThisDiskId)
                            {
                                foreach (FileRow fileRow in fileRowsByThisDiskId)
                                {
                                    MergeSymbolPaths(row, fileRow);
                                }
                            }
                            break;

                        default:
                            // error
                            break;
                    }
                }
            }

            // copy data from the WixMedia rows into the Media rows
            Table wixMediaTable = tables["WixMedia"];
            if (null != wixMediaTable && null != mediaTable)
            {
                // index all the Media rows by their identifier
                Hashtable indexedMediaRows = new Hashtable(mediaTable.Rows.Count);
                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    indexedMediaRows.Add(mediaRow.DiskId, mediaRow);
                }

                foreach (Row row in wixMediaTable.Rows)
                {
                    MediaRow mediaRow = (MediaRow)indexedMediaRows[row[0]];

                    // compression level
                    if (null != row[1])
                    {
                        mediaRow.CompressionLevel = WixCreateCab.CompressionLevelFromString((string)row[1]);
                    }

                    // layout
                    if (null != row[2])
                    {
                        mediaRow.Layout = (string)row[2];
                    }
                }
            }
        }

        /// <summary>
        /// Signal a warning if a non-keypath file was changed in a patch without also changing the keypath file of the component.
        /// </summary>
        /// <param name="output">The output to validate.</param>
        private void ValidateFileRowChanges(Output transform)
        {
            Table componentTable = transform.Tables["Component"];
            Table fileTable = transform.Tables["File"];

            // There's no sense validating keypaths if the transform has no component or file table
            if (componentTable == null || fileTable == null)
            {
                return;
            }

            Dictionary<string, string> componentKeyPath = new Dictionary<string, string>(componentTable.Rows.Count);

            // Index the Component table for non-directory & non-registry key paths.
            foreach (Row row in componentTable.Rows)
            {
                if (null != row.Fields[5].Data &&
                    0 != ((int)row.Fields[3].Data & MsiInterop.MsidbComponentAttributesRegistryKeyPath))
                {
                    componentKeyPath.Add(row.Fields[0].Data.ToString(), row.Fields[5].Data.ToString());
                }
            }

            Dictionary<string, string> componentWithChangedKeyPath = new Dictionary<string, string>();
            Dictionary<string, string> componentWithNonKeyPathChanged = new Dictionary<string, string>();
            // Verify changes in the file table, now that file diffing has occurred
            foreach (FileRow row in fileTable.Rows)
            {
                string fileId = row.Fields[0].Data.ToString();
                string componentId = row.Fields[1].Data.ToString();

                if (RowOperation.Modify != row.Operation)
                {
                    continue;
                }

                // If this file is the keypath of a component
                if (componentKeyPath.ContainsValue(fileId))
                {
                    if (!componentWithChangedKeyPath.ContainsKey(componentId))
                    {
                        componentWithChangedKeyPath.Add(componentId, fileId);
                    }
                }
                else
                {
                    if (!componentWithNonKeyPathChanged.ContainsKey(componentId))
                    {
                        componentWithNonKeyPathChanged.Add(componentId, fileId);
                    }
                }
            }

            foreach (KeyValuePair<string, string> componentFile in componentWithNonKeyPathChanged)
            {
                // Make sure all changes to non keypath files also had a change in the keypath.
                if (!componentWithChangedKeyPath.ContainsKey(componentFile.Key) && componentKeyPath.ContainsKey(componentFile.Key))
                {
                    this.core.OnMessage(WixWarnings.UpdateOfNonKeyPathFile((string)componentFile.Value, (string)componentFile.Key, (string)componentKeyPath[componentFile.Key]));
                }
            }
        }

        /// <summary>
        /// Binds the summary information table of a database.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="longNames">Returns a flag indicating if uncompressed files use long filenames.</param>
        /// <param name="compressed">Returns a flag indicating if files are compressed by default.</param>
        /// <returns>Modularization guid, or null if the output is not a module.</returns>
        private string BindDatabaseSummaryInfo(Output output, out bool longNames, out bool compressed)
        {
            longNames = false;
            compressed = false;
            string modularizationGuid = null;
            Table summaryInformationTable = output.Tables["_SummaryInformation"];
            if (null != summaryInformationTable)
            {
                bool foundCreateDataTime = false;
                bool foundLastSaveDataTime = false;
                bool foundCreatingApplication = false;
                string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                foreach (Row summaryInformationRow in summaryInformationTable.Rows)
                {
                    switch ((int)summaryInformationRow[0])
                    {
                        case 1: // PID_CODEPAGE
                            // make sure the code page is an int and not a web name or null
                            string codepage = (string)summaryInformationRow[1];

                            if (null == codepage)
                            {
                                codepage = "0";
                            }
                            else
                            {
                                summaryInformationRow[1] = Common.GetValidCodePage(codepage, false, true, summaryInformationRow.SourceLineNumbers).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case 9: // PID_REVNUMBER
                            string packageCode = (string)summaryInformationRow[1];

                            if (OutputType.Module == output.Type)
                            {
                                modularizationGuid = packageCode.Substring(1, 36).Replace('-', '_');
                            }
                            else if ("*" == packageCode)
                            {
                                // set the revision number (package/patch code) if it should be automatically generated
                                summaryInformationRow[1] = Common.GenerateGuid();
                            }
                            break;
                        case 12: // PID_CREATE_DTM
                            foundCreateDataTime = true;
                            break;
                        case 13: // PID_LASTSAVE_DTM
                            foundLastSaveDataTime = true;
                            break;
                        case 15: // PID_WORDCOUNT
                            if (OutputType.Patch == output.Type)
                            {
                                longNames = true;
                                compressed = true;
                            }
                            else
                            {
                                longNames = (0 == (Convert.ToInt32(summaryInformationRow[1], CultureInfo.InvariantCulture) & 1));
                                compressed = (2 == (Convert.ToInt32(summaryInformationRow[1], CultureInfo.InvariantCulture) & 2));
                            }
                            break;
                        case 18: // PID_APPNAME
                            foundCreatingApplication = true;
                            break;
                    }
                }

                // add a summary information row for the create time/date property if its not already set
                if (!foundCreateDataTime)
                {
                    Row createTimeDateRow = summaryInformationTable.CreateRow(null);
                    createTimeDateRow[0] = 12;
                    createTimeDateRow[1] = now;
                }

                // add a summary information row for the last save time/date property if its not already set
                if (!foundLastSaveDataTime)
                {
                    Row lastSaveTimeDateRow = summaryInformationTable.CreateRow(null);
                    lastSaveTimeDateRow[0] = 13;
                    lastSaveTimeDateRow[1] = now;
                }

                // add a summary information row for the creating application property if its not already set
                if (!foundCreatingApplication)
                {
                    Row creatingApplicationRow = summaryInformationTable.CreateRow(null);
                    creatingApplicationRow[0] = 18;
                    creatingApplicationRow[1] = String.Format(CultureInfo.InvariantCulture, AppCommon.GetCreatingApplicationString());
                }
            }

            return modularizationGuid;
        }

        /// <summary>
        /// Populates the WixBuildInfo table in an output.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="databaseFile">The output file if OutputFile not set.</param>
        private void WriteBuildInfoTable(Output output, string outputFile)
        {
            Table buildInfoTable = output.EnsureTable(this.core.TableDefinitions["WixBuildInfo"]);
            Row buildInfoRow = buildInfoTable.CreateRow(null);

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
            buildInfoRow[0] = fileVersion.FileVersion;
            buildInfoRow[1] = this.OutputFile ?? outputFile;

            if (!String.IsNullOrEmpty(this.wixprojectFile))
            {
                buildInfoRow[2] = this.wixprojectFile;
            }

            if (!String.IsNullOrEmpty(this.pdbFile))
            {
                buildInfoRow[3] = this.pdbFile;
            }
        }

        /// <summary>
        /// Binds a databse.
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="databaseFile">The database file to create.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        [SuppressMessage("Microsoft.Globalization", "CA1309:UseOrdinalStringComparison", Justification = "These strings need to be culture insensitive rather than ordinal because they are used for sorting")]
        private bool BindDatabase(Output output, string databaseFile)
        {
            foreach (BinderExtension extension in this.extensions)
            {
                extension.DatabaseInitialize(output);
            }

            bool compressed = false;
            FileRowCollection fileRows = new FileRowCollection(OutputType.Patch == output.Type);
            bool longNames = false;
            MediaRowCollection mediaRows = new MediaRowCollection();
            Hashtable suppressModularizationIdentifiers = null;
            StringCollection suppressedTableNames = new StringCollection();
            Table propertyTable = output.Tables["Property"];

            this.WriteBuildInfoTable(output, databaseFile);

            // gather all the wix variables
            Table wixVariableTable = output.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    this.WixVariableResolver.AddVariable(wixVariableRow);
                }
            }

            // gather all the suppress modularization identifiers
            Table wixSuppressModularizationTable = output.Tables["WixSuppressModularization"];
            if (null != wixSuppressModularizationTable)
            {
                suppressModularizationIdentifiers = new Hashtable(wixSuppressModularizationTable.Rows.Count);

                foreach (Row row in wixSuppressModularizationTable.Rows)
                {
                    suppressModularizationIdentifiers[row[0]] = null;
                }
            }

            // localize fields, resolve wix variables, and resolve file paths
            Hashtable cabinets = new Hashtable();
            ArrayList delayedFields = new ArrayList();
            this.ResolveFields(output.Tables, cabinets, delayedFields);

            // if there are any fields to resolve later, create the cache to populate during bind
            IDictionary<string, string> variableCache = null;
            if (0 < delayedFields.Count)
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            this.LocalizeUI(output.Tables);

            // process the summary information table before the other tables
            string modularizationGuid = this.BindDatabaseSummaryInfo(output, out longNames, out compressed);

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // modularize identifiers and add tables with real streams to the import tables
            if (OutputType.Module == output.Type)
            {
                foreach (Table table in output.Tables)
                {
                    table.Modularize(modularizationGuid, suppressModularizationIdentifiers);
                }

                // Reset the special property lists after modularization. The linker creates these properties before modularization
                // so we have to reconstruct them for merge modules after modularization in the binder.
                Table wixPropertyTable = output.Tables["WixProperty"];
                if (null != wixPropertyTable)
                {
                    // Create lists of the properties that contribute to the special lists of properties.
                    SortedList adminProperties = new SortedList();
                    SortedList secureProperties = new SortedList();
                    SortedList hiddenProperties = new SortedList();

                    foreach (WixPropertyRow wixPropertyRow in wixPropertyTable.Rows)
                    {
                        if (wixPropertyRow.Admin)
                        {
                            adminProperties[wixPropertyRow.Id] = null;
                        }

                        if (wixPropertyRow.Hidden)
                        {
                            hiddenProperties[wixPropertyRow.Id] = null;
                        }

                        if (wixPropertyRow.Secure)
                        {
                            secureProperties[wixPropertyRow.Id] = null;
                        }
                    }

                    if (0 < adminProperties.Count || 0 < hiddenProperties.Count || 0 < secureProperties.Count)
                    {
                        Table table = output.Tables["Property"];
                        foreach (Row propertyRow in table.Rows)
                        {
                            if ("AdminProperties" == (string)propertyRow[0])
                            {
                                propertyRow[1] = GetPropertyListString(adminProperties);
                            }

                            if ("MsiHiddenProperties" == (string)propertyRow[0])
                            {
                                propertyRow[1] = GetPropertyListString(hiddenProperties);
                            }

                            if ("SecureCustomProperties" == (string)propertyRow[0])
                            {
                                propertyRow[1] = GetPropertyListString(secureProperties);
                            }
                        }
                    }
                }
            }

            // merge unreal table data into the real tables
            // this must occur after all variables and source paths have been resolved
            this.MergeUnrealTables(output.Tables);

            if (this.core.EncounteredError)
            {
                return false;
            }

            if (OutputType.Patch == output.Type)
            {
                foreach (SubStorage substorage in output.SubStorages)
                {
                    Output transform = (Output)substorage.Data;
                    this.ResolveFields(transform.Tables, cabinets, null);
                    this.MergeUnrealTables(transform.Tables);
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // index the File table for quicker access later
            // this must occur after the unreal data has been merged in
            Table fileTable = output.Tables["File"];
            if (null != fileTable)
            {
                fileRows.AddRange(fileTable.Rows);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // add binder variables for all properties
            propertyTable = output.Tables["Property"];
            if (null != propertyTable)
            {
                foreach (Row propertyRow in propertyTable.Rows)
                {
                    string property = propertyRow[0].ToString();

                    // set the ProductCode if its generated
                    if (OutputType.Product == output.Type && "ProductCode" == property && "*" == propertyRow[1].ToString())
                    {
                        propertyRow[1] = Common.GenerateGuid();

                        // Update the target ProductCode in any instance transforms
                        foreach (SubStorage subStorage in output.SubStorages)
                        {
                            Output subStorageOutput = (Output)subStorage.Data;
                            if (OutputType.Transform != subStorageOutput.Type)
                            {
                                continue;
                            }

                            Table instanceSummaryInformationTable = subStorageOutput.Tables["_SummaryInformation"];
                            foreach (Row row in instanceSummaryInformationTable.Rows)
                            {
                                if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                                {
                                    row[1] = ((string)row[1]).Replace("*", (string)propertyRow[1]);
                                    break;
                                }
                            }
                        }
                    }

                    // add the property name and value to the variableCache
                    if (0 != delayedFields.Count)
                    {
                        string key = String.Concat("property.", Demodularize(output, modularizationGuid, property));
                        variableCache[key] = (string)propertyRow[1];
                    }
                }
            }

            // extract files that come from cabinet files (this does not extract files from merge modules)
            this.ExtractCabinets(cabinets);

            // retrieve files and their information from merge modules
            if (OutputType.Product == output.Type)
            {
                this.ProcessMergeModules(output, fileRows);
            }
            else if (OutputType.Patch == output.Type)
            {
                // merge transform data into the output object
                this.CopyTransformData(output, fileRows);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // assign files to media
            AutoMediaAssigner autoMediaAssigner = new AutoMediaAssigner(output, this.core, compressed);
            autoMediaAssigner.AssignFiles(fileRows);

            // update file version, hash, assembly, etc.. information
            this.core.OnMessage(WixVerboses.UpdatingFileInformation());
            Hashtable indexedFileRows = this.UpdateFileInformation(output, fileRows, autoMediaAssigner.MediaRows, variableCache, modularizationGuid);

            // set generated component guids
            this.SetComponentGuids(output);

            // With the Component Guids set now we can create instance transforms.
            this.CreateInstanceTransforms(output);

            this.ValidateComponentGuids(output);

            this.UpdateControlText(output);

            if (0 < delayedFields.Count)
            {
                this.ResolveDelayedFields(output, delayedFields, variableCache, modularizationGuid);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // Extended binder extensions can be called now that fields are resolved.
            foreach (BinderExtension extension in this.extensions)
            {
                BinderExtensionEx extensionEx = extension as BinderExtensionEx;
                if (null != extensionEx)
                {
                    output.EnsureTable(this.core.TableDefinitions["WixBindUpdatedFiles"]);
                    extensionEx.DatabaseAfterResolvedFields(output);
                }
            }

            Table updatedFiles = output.Tables["WixBindUpdatedFiles"];
            if (null != updatedFiles)
            {
                foreach (Row updatedFile in updatedFiles.Rows)
                {
                    FileRow updatedFileRow = (FileRow)indexedFileRows[updatedFile[0]];
                    this.UpdateFileRow(output, null, modularizationGuid, indexedFileRows, updatedFileRow, true);
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // create cabinet files and process uncompressed files
            string layoutDirectory = Path.GetDirectoryName(databaseFile);
            FileRowCollection uncompressedFileRows = null;
            if (!this.suppressLayout || OutputType.Module == output.Type)
            {
                this.core.OnMessage(WixVerboses.CreatingCabinetFiles());
                uncompressedFileRows = this.CreateCabinetFiles(output, fileRows, this.fileTransfers, autoMediaAssigner.MediaRows, layoutDirectory, compressed, autoMediaAssigner);
            }

            if (OutputType.Patch == output.Type)
            {
                // copy output data back into the transforms
                this.CopyTransformData(output, null);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // add back suppressed tables which must be present prior to merging in modules
            if (OutputType.Product == output.Type)
            {
                Table wixMergeTable = output.Tables["WixMerge"];

                if (null != wixMergeTable && 0 < wixMergeTable.Rows.Count)
                {
                    foreach (SequenceTable sequence in Enum.GetValues(typeof(SequenceTable)))
                    {
                        string sequenceTableName = sequence.ToString();
                        Table sequenceTable = output.Tables[sequenceTableName];

                        if (null == sequenceTable)
                        {
                            sequenceTable = output.EnsureTable(this.core.TableDefinitions[sequenceTableName]);
                        }

                        if (0 == sequenceTable.Rows.Count)
                        {
                            suppressedTableNames.Add(sequenceTableName);
                        }
                    }
                }
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.DatabaseFinalize(output);
            }

            // generate database file
            this.core.OnMessage(WixVerboses.GeneratingDatabase());
            string tempDatabaseFile = Path.Combine(this.TempFilesLocation, Path.GetFileName(databaseFile));
            this.GenerateDatabase(output, tempDatabaseFile, false, false);

            FileTransfer transfer;
            if (FileTransfer.TryCreate(tempDatabaseFile, databaseFile, true, output.Type.ToString(), null, out transfer)) // note where this database needs to move in the future
            {
                transfer.Built = true;
                this.fileTransfers.Add(transfer);
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return false;
            }

            // Output the output to a file
            if (null != this.pdbFile)
            {
                Pdb pdb = new Pdb(null);
                pdb.Output = output;
                pdb.Save(this.pdbFile, null, this.WixVariableResolver, this.TempFilesLocation);
            }

            // merge modules
            if (OutputType.Product == output.Type)
            {
                this.core.OnMessage(WixVerboses.MergingModules());
                this.MergeModules(tempDatabaseFile, output, fileRows, suppressedTableNames);

                // stop processing if an error previously occurred
                if (this.core.EncounteredError)
                {
                    return false;
                }
            }

            // inspect the MSI prior to running ICEs
            InspectorCore inspectorCore = new InspectorCore(this.MessageHandler);
            foreach (InspectorExtension inspectorExtension in this.inspectorExtensions)
            {
                inspectorExtension.Core = inspectorCore;
                inspectorExtension.InspectDatabase(tempDatabaseFile, output);

                // reset
                inspectorExtension.Core = null;
            }

            if (inspectorCore.EncounteredError)
            {
                return false;
            }

            // validate the output if there is an MSI validator
            if (null != this.validator)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                // set the output file for source line information
                this.validator.Output = output;

                this.core.OnMessage(WixVerboses.ValidatingDatabase());
                this.core.EncounteredError = !this.validator.Validate(tempDatabaseFile);

                stopwatch.Stop();
                this.core.OnMessage(WixVerboses.ValidatedDatabase(stopwatch.ElapsedMilliseconds));

                // stop processing if an error previously occurred
                if (this.core.EncounteredError)
                {
                    return false;
                }
            }

            // process uncompressed files
            if (!this.suppressLayout)
            {
                this.ProcessUncompressedFiles(tempDatabaseFile, uncompressedFileRows, this.fileTransfers, autoMediaAssigner.MediaRows, layoutDirectory, compressed, longNames);
            }

            // layout media
            try
            {
                this.core.OnMessage(WixVerboses.LayingOutMedia());
                this.LayoutMedia(this.fileTransfers, this.suppressAclReset);
            }
            finally
            {
                if (!String.IsNullOrEmpty(this.contentsFile))
                {
                    this.CreateContentsFile(this.contentsFile, fileRows);
                }

                if (!String.IsNullOrEmpty(this.outputsFile))
                {
                    this.CreateOutputsFile(this.outputsFile, this.fileTransfers, this.pdbFile);
                }

                if (!String.IsNullOrEmpty(this.builtOutputsFile))
                {
                    this.CreateBuiltOutputsFile(this.builtOutputsFile, this.fileTransfers, this.pdbFile);
                }
            }

            return !this.core.EncounteredError;
        }

        /// <summary>
        /// Get a sorted property list as a semicolon-delimited string.
        /// </summary>
        /// <param name="properties">SortedList of the properties.</param>
        /// <returns>Semicolon-delimited string representing the property list.</returns>
        private static string GetPropertyListString(SortedList properties)
        {
            bool first = true;
            StringBuilder propertiesString = new StringBuilder();

            foreach (string propertyName in properties.Keys)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    propertiesString.Append(';');
                }
                propertiesString.Append(propertyName);
            }

            return propertiesString.ToString();
        }

        /// <summary>
        /// Localize dialogs and controls.
        /// </summary>
        /// <param name="tables">The tables to localize.</param>
        private void LocalizeUI(TableCollection tables)
        {
            Table dialogTable = tables["Dialog"];
            if (null != dialogTable)
            {
                foreach (Row row in dialogTable.Rows)
                {
                    string dialog = (string)row[0];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, null);
                    if (null != localizedControl)
                    {
                        if (CompilerCore.IntegerNotSet != localizedControl.X)
                        {
                            row[1] = localizedControl.X;
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Y)
                        {
                            row[2] = localizedControl.Y;
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Width)
                        {
                            row[3] = localizedControl.Width;
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Height)
                        {
                            row[4] = localizedControl.Height;
                        }

                        row[5] = (int)row[5] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[6] = localizedControl.Text;
                        }
                    }
                }
            }

            Table controlTable = tables["Control"];
            if (null != controlTable)
            {
                foreach (Row row in controlTable.Rows)
                {
                    string dialog = (string)row[0];
                    string control = (string)row[1];
                    LocalizedControl localizedControl = this.Localizer.GetLocalizedControl(dialog, control);
                    if (null != localizedControl)
                    {
                        if (CompilerCore.IntegerNotSet != localizedControl.X)
                        {
                            row[3] = localizedControl.X.ToString();
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Y)
                        {
                            row[4] = localizedControl.Y.ToString();
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Width)
                        {
                            row[5] = localizedControl.Width.ToString();
                        }

                        if (CompilerCore.IntegerNotSet != localizedControl.Height)
                        {
                            row[6] = localizedControl.Height.ToString();
                        }

                        row[7] = (int)row[7] | localizedControl.Attributes;

                        if (!String.IsNullOrEmpty(localizedControl.Text))
                        {
                            row[9] = localizedControl.Text;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolve source fields in the tables included in the output
        /// </summary>
        /// <param name="tables">The tables to resolve.</param>
        /// <param name="cabinets">Cabinets containing files that need to be patched.</param>
        /// <param name="delayedFields">The collection of delayed fields. Null if resolution of delayed fields is not allowed</param>
        private void ResolveFields(TableCollection tables, Hashtable cabinets, ArrayList delayedFields)
        {
            foreach (Table table in tables)
            {
                foreach (Row row in table.Rows)
                {
                    foreach (Field field in row.Fields)
                    {
                        bool isDefault = true;
                        bool delayedResolve = false;

                        // Check to make sure we're in a scenario where we can handle variable resolution.
                        if (null != delayedFields)
                        {
                            // resolve localization and wix variables
                            if (field.Data is string)
                            {
                                field.Data = this.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, (string)field.Data, false, ref isDefault, ref delayedResolve);
                                if (delayedResolve)
                                {
                                    delayedFields.Add(new DelayedField(row, field));
                                }
                            }
                        }

                        // resolve file paths
                        if (!this.WixVariableResolver.EncounteredError && ColumnType.Object == field.Column.Type)
                        {
                            ObjectField objectField = (ObjectField)field;

                            // skip file resolution if the file is to be deleted
                            if (RowOperation.Delete == row.Operation)
                            {
                                continue;
                            }

                            // file is compressed in a cabinet (and not modified above)
                            if (null != objectField.CabinetFileId && isDefault)
                            {
                                // index cabinets that have not been previously encountered
                                if (!cabinets.ContainsKey(objectField.BaseUri))
                                {
                                    Uri baseUri = new Uri(objectField.BaseUri);
                                    string localFileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseUri.LocalPath);
                                    string extractedDirectoryName = String.Format(CultureInfo.InvariantCulture, "cab_{0}_{1}", cabinets.Count, localFileNameWithoutExtension);

                                    // index the cabinet file's base URI (source location) and extracted directory
                                    cabinets.Add(objectField.BaseUri, Path.Combine(this.TempFilesLocation, extractedDirectoryName));
                                }

                                // set the path to the file once its extracted from the cabinet
                                objectField.Data = Path.Combine((string)cabinets[objectField.BaseUri], objectField.CabinetFileId);
                            }
                            else if (null != objectField.Data) // non-compressed file (or localized value)
                            {
                                // when SuppressFileHasAndInfo is true do not resolve file paths
                                if (this.suppressFileHashAndInfo && table.Name == "WixFile")
                                {
                                    continue;
                                }

                                try
                                {
                                    if (OutputType.Patch != this.FileManager.Output.Type) // Normal binding for non-Patch scenario such as link (light.exe)
                                    {
                                        // keep a copy of the un-resolved data for future replay. This will be saved into wixpdb file
                                        if (null == objectField.UnresolvedData)
                                        {
                                            objectField.UnresolvedData = (string)objectField.Data;
                                        }

                                        // resolve the path to the file
                                        objectField.Data = this.FileManager.ResolveFile((string)objectField.Data, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                    }
                                    else if (!(this.FileManager.ReBaseTarget || this.FileManager.ReBaseUpdated)) // Normal binding for Patch Scenario (normal patch, no re-basing logic)
                                    {
                                        // resolve the path to the file
                                        objectField.Data = this.FileManager.ResolveFile((string)objectField.Data, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                    }
                                    else // Re-base binding path scenario caused by pyro.exe -bt -bu
                                    {
                                        // by default, use the resolved Data for file lookup
                                        string filePathToResolve = (string)objectField.Data;

                                        // if -bu is used in pyro command, this condition holds true and the tool
                                        // will use pre-resolved source for new wixpdb file
                                        if (this.FileManager.ReBaseUpdated)
                                        {
                                            // try to use the unResolved Source if it exists.
                                            // New version of wixpdb file keeps a copy of pre-resolved Source. i.e. !(bindpath.test)\foo.dll
                                            // Old version of winpdb file does not contain this attribute and the value is null.
                                            if (null != objectField.UnresolvedData)
                                            {
                                                filePathToResolve = objectField.UnresolvedData;
                                            }
                                        }

                                        objectField.Data = this.FileManager.ResolveFile(filePathToResolve, table.Name, row.SourceLineNumbers, BindStage.Updated);
                                    }
                                }
                                catch (WixFileNotFoundException)
                                {
                                    // display the error with source line information
                                    this.core.OnMessage(WixErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.Data));
                                }
                            }

                            isDefault = true;
                            if (null != objectField.PreviousData)
                            {
                                objectField.PreviousData = this.WixVariableResolver.ResolveVariables(row.SourceLineNumbers, objectField.PreviousData, false, ref isDefault);
                                if (!this.WixVariableResolver.EncounteredError)
                                {
                                    // file is compressed in a cabinet (and not modified above)
                                    if (null != objectField.PreviousCabinetFileId && isDefault)
                                    {
                                        // when loading transforms from disk, PreviousBaseUri may not have been set
                                        if (null == objectField.PreviousBaseUri)
                                        {
                                            objectField.PreviousBaseUri = objectField.BaseUri;
                                        }

                                        // index cabinets that have not been previously encountered
                                        if (!cabinets.ContainsKey(objectField.PreviousBaseUri))
                                        {
                                            Uri baseUri = new Uri(objectField.PreviousBaseUri);
                                            string localFileNameWithoutExtension = Path.GetFileNameWithoutExtension(baseUri.LocalPath);
                                            string extractedDirectoryName = String.Format(CultureInfo.InvariantCulture, "cab_{0}_{1}", cabinets.Count, localFileNameWithoutExtension);

                                            // index the cabinet file's base URI (source location) and extracted directory
                                            cabinets.Add(objectField.PreviousBaseUri, Path.Combine(this.TempFilesLocation, extractedDirectoryName));
                                        }

                                        // set the path to the file once its extracted from the cabinet
                                        objectField.PreviousData = Path.Combine((string)cabinets[objectField.PreviousBaseUri], objectField.PreviousCabinetFileId);
                                    }
                                    else if (null != objectField.PreviousData) // non-compressed file (or localized value)
                                    {
                                        // when SuppressFileHasAndInfo is true do not resolve file paths
                                        if (this.suppressFileHashAndInfo && table.Name == "WixFile")
                                        {
                                            continue;
                                        }

                                        try
                                        {
                                            if (!this.FileManager.ReBaseTarget && !this.FileManager.ReBaseUpdated)
                                            {
                                                // resolve the path to the file
                                                objectField.PreviousData = this.FileManager.ResolveFile((string)objectField.PreviousData, table.Name, row.SourceLineNumbers, BindStage.Normal);
                                            }
                                            else
                                            {
                                                if (this.FileManager.ReBaseTarget)
                                                {
                                                    // if -bt is used, it come here
                                                    // Try to use the original unresolved source from either target build or update build
                                                    // If both target and updated are of old wixpdb, it behaves the same as today, no re-base logic here
                                                    // If target is old version and updated is new version, it uses unresolved path from updated build
                                                    // If both target and updated are of new versions, it uses unresolved path from target build
                                                    if (null != objectField.UnresolvedPreviousData || null != objectField.UnresolvedData)
                                                    {
                                                        objectField.PreviousData = objectField.UnresolvedPreviousData ?? objectField.UnresolvedData;
                                                    }
                                                }

                                                // resolve the path to the file
                                                objectField.PreviousData = this.FileManager.ResolveFile((string)objectField.PreviousData, table.Name, row.SourceLineNumbers, BindStage.Target);

                                            }
                                        }
                                        catch (WixFileNotFoundException)
                                        {
                                            // display the error with source line information
                                            this.core.OnMessage(WixErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.PreviousData));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // remember if the variable resolver found an error
            if (this.WixVariableResolver.EncounteredError)
            {
                this.core.EncounteredError = true;
            }
        }

        /// <summary>
        /// Extracts embedded cabinets for resolved data.
        /// </summary>
        /// <param name="cabinets">Collection of cabinets containing resolved data.</param>
        private void ExtractCabinets(Hashtable cabinets)
        {
            foreach (DictionaryEntry cabinet in cabinets)
            {
                Uri baseUri = new Uri((string)cabinet.Key);
                string localPath;

                if ("embeddedresource" == baseUri.Scheme)
                {
                    int bytesRead;
                    byte[] buffer = new byte[512];

                    string originalLocalPath = Path.GetFullPath(baseUri.LocalPath.Substring(1));
                    string resourceName = baseUri.Fragment.Substring(1);
                    Assembly assembly = Assembly.LoadFile(originalLocalPath);

                    localPath = String.Concat(cabinet.Value, ".cab");

                    using (FileStream fs = File.OpenWrite(localPath))
                    {
                        using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                        {
                            while (0 < (bytesRead = resourceStream.Read(buffer, 0, buffer.Length)))
                            {
                                fs.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
                else // normal file
                {
                    localPath = baseUri.LocalPath;
                }

                // extract the cabinet's files into a temporary directory
                Directory.CreateDirectory((string)cabinet.Value);

                using (WixExtractCab extractCab = new WixExtractCab())
                {
                    extractCab.Extract(localPath, (string)cabinet.Value);
                }
            }
        }

        /// <summary>
        /// Resolves the fields which had variables that needed to be resolved after the file information
        /// was loaded.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="delayedFields">The fields which had resolution delayed.</param>
        /// <param name="variableCache">The file information to use when resolving variables.</param>
        /// <param name="modularizationGuid">The modularization guid (used in case of a merge module).</param>
        private void ResolveDelayedFields(Output output, ArrayList delayedFields, IDictionary<string, string> variableCache, string modularizationGuid)
        {
            List<DelayedField> deferredFields = new List<DelayedField>();

            foreach (DelayedField delayedField in delayedFields)
            {
                try
                {
                    Row propertyRow = delayedField.Row;

                    // process properties first in case they refer to other binder variables
                    if ("Property" == propertyRow.Table.Name)
                    {
                        string value = WixVariableResolver.ResolveDelayedVariables(propertyRow.SourceLineNumbers, (string)delayedField.Field.Data, variableCache);

                        // update the variable cache with the new value
                        string key = String.Concat("property.", Demodularize(output, modularizationGuid, (string)propertyRow[0]));
                        variableCache[key] = value;

                        // update the field data
                        delayedField.Field.Data = value;
                    }
                    else
                    {
                        deferredFields.Add(delayedField);
                    }
                }
                catch (WixException we)
                {
                    this.core.OnMessage(we.Error);
                    continue;
                }
            }

            // add specialization for ProductVersion fields
            string keyProductVersion = "property.ProductVersion";
            if (variableCache.ContainsKey(keyProductVersion))
            {
                string value = variableCache[keyProductVersion];
                Version productVersion = null;

                try
                {
                    productVersion = new Version(value);

                    // Don't add the variable if it already exists (developer defined a property with the same name).
                    string fieldKey = String.Concat(keyProductVersion, ".Major");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Major.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Minor");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Minor.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Build");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Build.ToString(CultureInfo.InvariantCulture);
                    }

                    fieldKey = String.Concat(keyProductVersion, ".Revision");
                    if (!variableCache.ContainsKey(fieldKey))
                    {
                        variableCache[fieldKey] = productVersion.Revision.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch
                {
                    // Ignore the error introduced by new behavior.
                }
            }

            // process the remaining fields in case they refer to property binder variables
            foreach (DelayedField delayedField in deferredFields)
            {
                try
                {
                    delayedField.Field.Data = WixVariableResolver.ResolveDelayedVariables(delayedField.Row.SourceLineNumbers, (string)delayedField.Field.Data, variableCache);
                }
                catch (WixException we)
                {
                    this.core.OnMessage(we.Error);
                    continue;
                }
            }
        }

        /// <summary>
        /// Tests sequence table for PatchFiles and associated actions
        /// </summary>
        /// <param name="iesTable">The table to test.</param>
        /// <param name="hasPatchFilesAction">Set to true if PatchFiles action is found. Left unchanged otherwise.</param>
        /// <param name="seqInstallFiles">Set to sequence value of InstallFiles action if found. Left unchanged otherwise.</param>
        /// <param name="seqDuplicateFiles">Set to sequence value of DuplicateFiles action if found. Left unchanged otherwise.</param>
        private static void TestSequenceTableForPatchFilesAction(Table iesTable, ref bool hasPatchFilesAction, ref int seqInstallFiles, ref int seqDuplicateFiles)
        {
            if (null != iesTable)
            {
                foreach (Row iesRow in iesTable.Rows)
                {
                    if (String.Equals("PatchFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        hasPatchFilesAction = true;
                    }
                    if (String.Equals("InstallFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        seqInstallFiles = (int)iesRow.Fields[2].Data;
                    }
                    if (String.Equals("DuplicateFiles", (string)iesRow[0], StringComparison.Ordinal))
                    {
                        seqDuplicateFiles = (int)iesRow.Fields[2].Data;
                    }
                }
            }
        }

        /// <summary>
        /// Adds the PatchFiles action to the sequence table if it does not already exist.
        /// </summary>
        /// <param name="table">The sequence table to check or modify.</param>
        /// <param name="mainTransform">The primary authoring transform.</param>
        /// <param name="pairedTransform">The secondary patch transform.</param>
        /// <param name="mainFileRow">The file row that contains information about the patched file.</param>
        private void AddPatchFilesActionToSequenceTable(SequenceTable table, Output mainTransform, Output pairedTransform, Row mainFileRow)
        {
            // Find/add PatchFiles action (also determine sequence for it).
            // Search mainTransform first, then pairedTransform (pairedTransform overrides).
            bool hasPatchFilesAction = false;
            int seqInstallFiles = 0;
            int seqDuplicateFiles = 0;
            string tableName = table.ToString();

            TestSequenceTableForPatchFilesAction(
                    mainTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref seqInstallFiles,
                    ref seqDuplicateFiles);
            TestSequenceTableForPatchFilesAction(
                    pairedTransform.Tables[tableName],
                    ref hasPatchFilesAction,
                    ref seqInstallFiles,
                    ref seqDuplicateFiles);
            if (!hasPatchFilesAction)
            {
                Table iesTable = pairedTransform.EnsureTable(this.core.TableDefinitions[tableName]);
                if (0 == iesTable.Rows.Count)
                {
                    iesTable.Operation = TableOperation.Add;
                }
                Row patchAction = iesTable.CreateRow(null);
                WixActionRow wixPatchAction = Installer.GetStandardActions()[table, "PatchFiles"];
                int sequence = wixPatchAction.Sequence;
                // Test for default sequence value's appropriateness
                if (seqInstallFiles >= sequence || (0 != seqDuplicateFiles && seqDuplicateFiles <= sequence))
                {
                    if (0 != seqDuplicateFiles)
                    {
                        if (seqDuplicateFiles < seqInstallFiles)
                        {
                            throw new WixException(WixErrors.InsertInvalidSequenceActionOrder(mainFileRow.SourceLineNumbers, iesTable.Name, "InstallFiles", "DuplicateFiles", wixPatchAction.Action));
                        }
                        else
                        {
                            sequence = (seqDuplicateFiles + seqInstallFiles) / 2;
                            if (seqInstallFiles == sequence || seqDuplicateFiles == sequence)
                            {
                                throw new WixException(WixErrors.InsertSequenceNoSpace(mainFileRow.SourceLineNumbers, iesTable.Name, "InstallFiles", "DuplicateFiles", wixPatchAction.Action));
                            }
                        }
                    }
                    else
                    {
                        sequence = seqInstallFiles + 1;
                    }
                }
                patchAction[0] = wixPatchAction.Action;
                patchAction[1] = wixPatchAction.Condition;
                patchAction[2] = sequence;
                patchAction.Operation = RowOperation.Add;
            }
        }

        /// <summary>
        /// Copy file data between transform substorages and the patch output object
        /// </summary>
        /// <param name="output">The output to bind.</param>
        /// <param name="allFileRows">True if copying from transform to patch, false the other way.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        private bool CopyTransformData(Output output, FileRowCollection allFileRows)
        {
            bool copyToPatch = (allFileRows != null);
            bool copyFromPatch = !copyToPatch;
            if (OutputType.Patch != output.Type)
            {
                return true;
            }

            Hashtable patchMediaRows = new Hashtable();
            Hashtable patchMediaFileRows = new Hashtable();
            Table patchFileTable = output.EnsureTable(this.core.TableDefinitions["File"]);
            if (copyFromPatch)
            {
                // index patch files by diskId+fileId
                foreach (FileRow patchFileRow in patchFileTable.Rows)
                {
                    int diskId = patchFileRow.DiskId;
                    if (!patchMediaFileRows.Contains(diskId))
                    {
                        patchMediaFileRows[diskId] = new FileRowCollection();
                    }
                    FileRowCollection mediaFileRows = (FileRowCollection)patchMediaFileRows[diskId];
                    mediaFileRows.Add(patchFileRow);
                }

                Table patchMediaTable = output.EnsureTable(this.core.TableDefinitions["Media"]);
                foreach (MediaRow patchMediaRow in patchMediaTable.Rows)
                {
                    patchMediaRows[patchMediaRow.DiskId] = patchMediaRow;
                }
            }

            // index paired transforms
            Hashtable pairedTransforms = new Hashtable();
            foreach (SubStorage substorage in output.SubStorages)
            {
                if ("#" == substorage.Name.Substring(0, 1))
                {
                    pairedTransforms[substorage.Name.Substring(1)] = substorage.Data;
                }
            }

            try
            {
                // copy File bind data into substorages
                foreach (SubStorage substorage in output.SubStorages)
                {
                    if ("#" == substorage.Name.Substring(0, 1))
                    {
                        // no changes necessary for paired transforms
                        continue;
                    }

                    Output mainTransform = (Output)substorage.Data;
                    Table mainWixFileTable = mainTransform.Tables["WixFile"];
                    Table mainMsiFileHashTable = mainTransform.Tables["MsiFileHash"];

                    int numWixFileRows = (null != mainWixFileTable) ? mainWixFileTable.Rows.Count : 0;
                    int numMsiFileHashRows = (null != mainMsiFileHashTable) ? mainMsiFileHashTable.Rows.Count : 0;

                    this.FileManager.ActiveSubStorage = substorage;
                    Hashtable wixFiles = new Hashtable(numWixFileRows);
                    Hashtable mainMsiFileHashIndex = new Hashtable(numMsiFileHashRows);
                    Table mainFileTable = mainTransform.Tables["File"];
                    Output pairedTransform = (Output)pairedTransforms[substorage.Name];

                    if (null != mainWixFileTable)
                    {
                        // Index the WixFile table for later use.
                        foreach (WixFileRow row in mainWixFileTable.Rows)
                        {
                            wixFiles.Add(row.Fields[0].Data.ToString(), row);
                        }
                    }

                    // copy Media.LastSequence and index the MsiFileHash table if it exists.
                    if (copyFromPatch)
                    {
                        Table pairedMediaTable = pairedTransform.Tables["Media"];
                        foreach (MediaRow pairedMediaRow in pairedMediaTable.Rows)
                        {
                            MediaRow patchMediaRow = (MediaRow)patchMediaRows[pairedMediaRow.DiskId];
                            pairedMediaRow.Fields[1] = patchMediaRow.Fields[1];
                        }

                        if (null != mainMsiFileHashTable)
                        {
                            // Index the MsiFileHash table for later use.
                            foreach (Row row in mainMsiFileHashTable.Rows)
                            {
                                mainMsiFileHashIndex.Add(row[0], row);
                            }
                        }

                        // Validate file row changes for keypath-related issues
                        this.ValidateFileRowChanges(mainTransform);
                    }

                    // index File table of pairedTransform
                    FileRowCollection pairedFileRows = new FileRowCollection();
                    Table pairedFileTable = pairedTransform.Tables["File"];
                    if (null != pairedFileTable)
                    {
                        pairedFileRows.AddRange(pairedFileTable.Rows);
                    }

                    if (null != mainFileTable)
                    {
                        if (copyFromPatch)
                        {
                            // Remove the MsiFileHash table because it will be updated later with the final file hash for each file
                            mainTransform.Tables.Remove("MsiFileHash");
                        }

                        foreach (FileRow mainFileRow in mainFileTable.Rows)
                        {
                            if (mainFileRow.Operation == RowOperation.Delete)
                            {
                                continue;
                            }

                            if (!copyToPatch && mainFileRow.Operation == RowOperation.None)
                            {
                                continue;
                            }
                            // When copying to the patch, we need compare the underlying files and include all file changes.
                            else if (copyToPatch)
                            {
                                WixFileRow wixFileRow = (WixFileRow)wixFiles[mainFileRow.Fields[0].Data.ToString()];
                                ObjectField objectField = (ObjectField)wixFileRow.Fields[6];
                                FileRow pairedFileRow = pairedFileRows[mainFileRow.Fields[0].Data.ToString()];

                                // If the file is new, we always need to add it to the patch.
                                if (mainFileRow.Operation != RowOperation.Add)
                                {
                                    // If PreviousData doesn't exist, target and upgrade layout point to the same location. No need to compare.
                                    if (null == objectField.PreviousData)
                                    {
                                        if (mainFileRow.Operation == RowOperation.None)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // TODO: should this entire condition be placed in the binder file manager?
                                        if ((0 == (PatchAttributeType.Ignore & wixFileRow.PatchAttributes)) &&
                                            !this.FileManager.CompareFiles(objectField.PreviousData.ToString(), objectField.Data.ToString()))
                                        {
                                            // If the file is different, we need to mark the mainFileRow and pairedFileRow as modified.
                                            mainFileRow.Operation = RowOperation.Modify;
                                            if (null != pairedFileRow)
                                            {
                                                // Always patch-added, but never non-compressed.
                                                pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                                pairedFileRow.Fields[6].Modified = true;
                                                pairedFileRow.Operation = RowOperation.Modify;
                                            }
                                        }
                                        else
                                        {
                                            // The File is same. We need mark all the attributes as unchanged.
                                            mainFileRow.Operation = RowOperation.None;
                                            foreach (Field field in mainFileRow.Fields)
                                            {
                                                field.Modified = false;
                                            }

                                            if (null != pairedFileRow)
                                            {
                                                pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesPatchAdded;
                                                pairedFileRow.Fields[6].Modified = false;
                                                pairedFileRow.Operation = RowOperation.None;
                                            }
                                            continue;
                                        }
                                    }
                                }
                                else if (null != pairedFileRow) // RowOperation.Add
                                {
                                    // Always patch-added, but never non-compressed.
                                    pairedFileRow.Attributes |= MsiInterop.MsidbFileAttributesPatchAdded;
                                    pairedFileRow.Attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                                    pairedFileRow.Fields[6].Modified = true;
                                    pairedFileRow.Operation = RowOperation.Add;
                                }
                            }

                            // index patch files by diskId+fileId
                            int diskId = mainFileRow.DiskId;
                            if (!patchMediaFileRows.Contains(diskId))
                            {
                                patchMediaFileRows[diskId] = new FileRowCollection();
                            }
                            FileRowCollection mediaFileRows = (FileRowCollection)patchMediaFileRows[diskId];

                            string fileId = mainFileRow.File;
                            FileRow patchFileRow = mediaFileRows[fileId];
                            if (copyToPatch)
                            {
                                if (null == patchFileRow)
                                {
                                    patchFileRow = (FileRow)patchFileTable.CreateRow(null);
                                    patchFileRow.CopyFrom(mainFileRow);
                                    mediaFileRows.Add(patchFileRow);
                                    allFileRows.Add(patchFileRow);
                                }
                                else
                                {
                                    // TODO: confirm the rest of data is identical?

                                    // make sure Source is same. Otherwise we are silently ignoring a file.
                                    if (0 != String.Compare(patchFileRow.Source, mainFileRow.Source, StringComparison.OrdinalIgnoreCase))
                                    {
                                        this.core.OnMessage(WixErrors.SameFileIdDifferentSource(mainFileRow.SourceLineNumbers, fileId, patchFileRow.Source, mainFileRow.Source));
                                    }
                                    // capture the previous file versions (and associated data) from this targeted instance of the baseline into the current filerow.
                                    patchFileRow.AppendPreviousDataFrom(mainFileRow);
                                }
                            }
                            else
                            {
                                // copy data from the patch back to the transform
                                if (null != patchFileRow)
                                {
                                    FileRow pairedFileRow = (FileRow)pairedFileRows[fileId];
                                    for (int i = 0; i < patchFileRow.Fields.Length; i++)
                                    {
                                        string patchValue = patchFileRow[i] == null ? "" : patchFileRow[i].ToString();
                                        string mainValue = mainFileRow[i] == null ? "" : mainFileRow[i].ToString();

                                        if (1 == i)
                                        {
                                            // File.Component_ changes should not come from the shared file rows
                                            // that contain the file information as each individual transform might
                                            // have different changes (or no changes at all).
                                        }
                                        // File.Attributes should not changed for binary deltas
                                        else if (6 == i)
                                        {
                                            if (null != patchFileRow.Patch)
                                            {
                                                // File.Attribute should not change for binary deltas
                                                pairedFileRow.Attributes = mainFileRow.Attributes;
                                                mainFileRow.Fields[i].Modified = false;
                                            }
                                        }
                                        // File.Sequence is updated in pairedTransform, not mainTransform
                                        else if (7 == i)
                                        {
                                            // file sequence is updated in Patch table instead of File table for delta patches
                                            if (null != patchFileRow.Patch)
                                            {
                                                pairedFileRow.Fields[i].Modified = false;
                                            }
                                            else
                                            {
                                                pairedFileRow[i] = patchFileRow[i];
                                                pairedFileRow.Fields[i].Modified = true;
                                            }
                                            mainFileRow.Fields[i].Modified = false;
                                        }
                                        else if (patchValue != mainValue)
                                        {
                                            mainFileRow[i] = patchFileRow[i];
                                            mainFileRow.Fields[i].Modified = true;
                                            if (mainFileRow.Operation == RowOperation.None)
                                            {
                                                mainFileRow.Operation = RowOperation.Modify;
                                            }
                                        }
                                    }

                                    // copy MsiFileHash row for this File
                                    Row patchHashRow;
                                    if (mainMsiFileHashIndex.ContainsKey(patchFileRow.File))
                                    {
                                        patchHashRow = (Row)mainMsiFileHashIndex[patchFileRow.File];
                                    }
                                    else
                                    {
                                        patchHashRow = patchFileRow.HashRow;
                                    }

                                    if (null != patchHashRow)
                                    {
                                        Table mainHashTable = mainTransform.EnsureTable(this.core.TableDefinitions["MsiFileHash"]);
                                        Row mainHashRow = mainHashTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        for (int i = 0; i < patchHashRow.Fields.Length; i++)
                                        {
                                            mainHashRow[i] = patchHashRow[i];
                                            if (i > 1)
                                            {
                                                // assume all hash fields have been modified
                                                mainHashRow.Fields[i].Modified = true;
                                            }
                                        }

                                        // assume the MsiFileHash operation follows the File one
                                        mainHashRow.Operation = mainFileRow.Operation;
                                    }

                                    // copy MsiAssemblyName rows for this File
                                    RowCollection patchAssemblyNameRows = patchFileRow.AssemblyNameRows;
                                    if (null != patchAssemblyNameRows)
                                    {
                                        Table mainAssemblyNameTable = mainTransform.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
                                        foreach (Row patchAssemblyNameRow in patchAssemblyNameRows)
                                        {
                                            // Copy if there isn't an identical modified/added row already in the transform.
                                            bool foundMatchingModifiedRow = false;
                                            foreach (Row mainAssemblyNameRow in mainAssemblyNameTable.Rows)
                                            {
                                                if (RowOperation.None != mainAssemblyNameRow.Operation && mainAssemblyNameRow.GetPrimaryKey('/').Equals(patchAssemblyNameRow.GetPrimaryKey('/')))
                                                {
                                                    foundMatchingModifiedRow = true;
                                                    break;
                                                }
                                            }

                                            if (!foundMatchingModifiedRow)
                                            {
                                                Row mainAssemblyNameRow = mainAssemblyNameTable.CreateRow(mainFileRow.SourceLineNumbers);
                                                for (int i = 0; i < patchAssemblyNameRow.Fields.Length; i++)
                                                {
                                                    mainAssemblyNameRow[i] = patchAssemblyNameRow[i];
                                                }

                                                // assume value field has been modified
                                                mainAssemblyNameRow.Fields[2].Modified = true;
                                                mainAssemblyNameRow.Operation = mainFileRow.Operation;
                                            }
                                        }
                                    }

                                    // Add patch header for this file
                                    if (null != patchFileRow.Patch)
                                    {
                                        // Add the PatchFiles action automatically to the AdminExecuteSequence and InstallExecuteSequence tables.
                                        AddPatchFilesActionToSequenceTable(SequenceTable.AdminExecuteSequence, mainTransform, pairedTransform, mainFileRow);
                                        AddPatchFilesActionToSequenceTable(SequenceTable.InstallExecuteSequence, mainTransform, pairedTransform, mainFileRow);

                                        // Add to Patch table
                                        Table patchTable = pairedTransform.EnsureTable(this.core.TableDefinitions["Patch"]);
                                        if (0 == patchTable.Rows.Count)
                                        {
                                            patchTable.Operation = TableOperation.Add;
                                        }
                                        Row patchRow = patchTable.CreateRow(mainFileRow.SourceLineNumbers);
                                        patchRow[0] = patchFileRow.File;
                                        patchRow[1] = patchFileRow.Sequence;
                                        FileInfo patchFile = new FileInfo(patchFileRow.Source);
                                        patchRow[2] = (int)patchFile.Length;
                                        patchRow[3] = 0 == (PatchAttributeType.AllowIgnoreOnError & patchFileRow.PatchAttributes) ? 0 : 1;
                                        string streamName = patchTable.Name + "." + patchRow[0] + "." + patchRow[1];
                                        if (MsiInterop.MsiMaxStreamNameLength < streamName.Length)
                                        {
                                            streamName = "_" + Guid.NewGuid().ToString("D").ToUpper(CultureInfo.InvariantCulture).Replace('-', '_');
                                            Table patchHeadersTable = pairedTransform.EnsureTable(this.core.TableDefinitions["MsiPatchHeaders"]);
                                            if (0 == patchHeadersTable.Rows.Count)
                                            {
                                                patchHeadersTable.Operation = TableOperation.Add;
                                            }
                                            Row patchHeadersRow = patchHeadersTable.CreateRow(mainFileRow.SourceLineNumbers);
                                            patchHeadersRow[0] = streamName;
                                            patchHeadersRow[1] = patchFileRow.Patch;
                                            patchRow[5] = streamName;
                                            patchHeadersRow.Operation = RowOperation.Add;
                                        }
                                        else
                                        {
                                            patchRow[4] = patchFileRow.Patch;
                                        }
                                        patchRow.Operation = RowOperation.Add;
                                    }
                                }
                                else
                                {
                                    // TODO: throw because all transform rows should have made it into the patch
                                }
                            }
                        }
                    }

                    if (copyFromPatch)
                    {
                        output.Tables.Remove("Media");
                        output.Tables.Remove("File");
                        output.Tables.Remove("MsiFileHash");
                        output.Tables.Remove("MsiAssemblyName");
                    }
                }
            }
            finally
            {
                this.FileManager.ActiveSubStorage = null;
            }

            return true;
        }

        /// <summary>
        /// Takes an id, and demodularizes it (if possible).
        /// </summary>
        /// <remarks>
        /// If the output type is a module, returns a demodularized version of an id. Otherwise, returns the id.
        /// </remarks>
        /// <param name="output">The output to bind.</param>
        /// <param name="modularizationGuid">The modularization GUID.</param>
        /// <param name="id">The id to demodularize.</param>
        /// <returns>The demodularized id.</returns>
        private static string Demodularize(Output output, string modularizationGuid, string id)
        {
            if (OutputType.Module == output.Type && id.EndsWith(String.Concat(".", modularizationGuid), StringComparison.Ordinal))
            {
                id = id.Substring(0, id.Length - 37);
            }

            return id;
        }

        /// <summary>
        /// Populates the variable cache with specific package properties.
        /// </summary>
        /// <param name="package">The package with properties to cache.</param>
        /// <param name="variableCache">The property cache.</param>
        private static void PopulatePackageVariableCache(ChainPackageInfo package, IDictionary<string, string> variableCache)
        {
            string id = package.Id;

            variableCache.Add(String.Concat("packageDescription.", id), package.Description);
            variableCache.Add(String.Concat("packageLanguage.", id), package.Language);
            variableCache.Add(String.Concat("packageManufacturer.", id), package.Manufacturer);
            variableCache.Add(String.Concat("packageName.", id), package.DisplayName);
            variableCache.Add(String.Concat("packageVersion.", id), package.Version);
        }

        /// <summary>
        /// Binds a bundle.
        /// </summary>
        /// <param name="bundle">The bundle to bind.</param>
        /// <param name="bundleFile">The bundle to create.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        private bool BindBundle(Output bundle, string bundleFile)
        {
            // First look for data we expect to find... Chain, WixGroups, etc.
            Table chainPackageTable = bundle.Tables["ChainPackage"];
            if (null == chainPackageTable || 0 == chainPackageTable.Rows.Count)
            {
                // We shouldn't really get past the linker phase if there are
                // no group items... that means that there's no UX, no Chain,
                // *and* no Containers!
                throw new WixException(WixErrors.MissingBundleInformation("ChainPackage"));
            }

            Table wixGroupTable = bundle.Tables["WixGroup"];
            if (null == wixGroupTable || 0 == wixGroupTable.Rows.Count)
            {
                // We shouldn't really get past the linker phase if there are
                // no group items... that means that there's no UX, no Chain,
                // *and* no Containers!
                throw new WixException(WixErrors.MissingBundleInformation("WixGroup"));
            }

            // Ensure there is one and only one row in the WixBundle table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table bundleTable = bundle.Tables["WixBundle"];
            if (null == bundleTable || 1 != bundleTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixBundle"));
            }

            // Ensure there is one and only one row in the WixBootstrapperApplication table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table baTable = bundle.Tables["WixBootstrapperApplication"];
            if (null == baTable || 1 != baTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixBootstrapperApplication"));
            }

            // Ensure there is one and only one row in the WixChain table.
            // The compiler and linker behavior should have colluded to get
            // this behavior.
            Table chainTable = bundle.Tables["WixChain"];
            if (null == chainTable || 1 != chainTable.Rows.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("WixChain"));
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.BundleInitialize(bundle);
            }

            if (this.core.EncounteredError)
            {
                return false;
            }

            this.WriteBuildInfoTable(bundle, bundleFile);

            // gather all the wix variables
            Table wixVariableTable = bundle.Tables["WixVariable"];
            if (null != wixVariableTable)
            {
                foreach (WixVariableRow wixVariableRow in wixVariableTable.Rows)
                {
                    this.WixVariableResolver.AddVariable(wixVariableRow);
                }
            }

            Hashtable cabinets = new Hashtable();
            ArrayList delayedFields = new ArrayList();

            // localize fields, resolve wix variables, and resolve file paths
            this.ResolveFields(bundle.Tables, cabinets, delayedFields);

            // if there are any fields to resolve later, create the cache to populate during bind
            IDictionary<string, string> variableCache = null;
            if (0 < delayedFields.Count)
            {
                variableCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            }

            if (this.core.EncounteredError)
            {
                return false;
            }

            Table relatedBundleTable = bundle.Tables["RelatedBundle"];
            List<RelatedBundleInfo> allRelatedBundles = new List<RelatedBundleInfo>();
            if (null != relatedBundleTable && 0 < relatedBundleTable.Rows.Count)
            {
                Dictionary<string, bool> deduplicatedRelatedBundles = new Dictionary<string, bool>();
                foreach (Row row in relatedBundleTable.Rows)
                {
                    string id = (string)row[0];
                    if (!deduplicatedRelatedBundles.ContainsKey(id))
                    {
                        deduplicatedRelatedBundles[id] = true;
                        allRelatedBundles.Add(new RelatedBundleInfo(row));
                    }
                }
            }

            // Ensure that the bundle has our well-known persisted values.
            Table variableTable = bundle.EnsureTable(this.core.TableDefinitions["Variable"]);
            VariableRow bundleNameWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleNameWellKnownVariable.Id = Binder.BURN_BUNDLE_NAME;
            bundleNameWellKnownVariable.Hidden = false;
            bundleNameWellKnownVariable.Persisted = true;

            VariableRow bundleOriginalSourceWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleOriginalSourceWellKnownVariable.Id = Binder.BURN_BUNDLE_ORIGINAL_SOURCE;
            bundleOriginalSourceWellKnownVariable.Hidden = false;
            bundleOriginalSourceWellKnownVariable.Persisted = true;

            VariableRow bundleOriginalSourceFolderWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleOriginalSourceFolderWellKnownVariable.Id = Binder.BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER;
            bundleOriginalSourceFolderWellKnownVariable.Hidden = false;
            bundleOriginalSourceFolderWellKnownVariable.Persisted = true;

            VariableRow bundleLastUsedSourceWellKnownVariable = (VariableRow)variableTable.CreateRow(null);
            bundleLastUsedSourceWellKnownVariable.Id = Binder.BURN_BUNDLE_LAST_USED_SOURCE;
            bundleLastUsedSourceWellKnownVariable.Hidden = false;
            bundleLastUsedSourceWellKnownVariable.Persisted = true;

            // To make lookups easier, we load the variable table bottom-up, so
            // that we can index by ID.
            List<VariableInfo> allVariables = new List<VariableInfo>(variableTable.Rows.Count);
            foreach (VariableRow variableRow in variableTable.Rows)
            {
                allVariables.Add(new VariableInfo(variableRow));
            }

            // TODO: Although the WixSearch tables are defined in the Util extension,
            // the Bundle Binder has to know all about them. We hope to revisit all
            // of this in the 4.0 timeframe.
            Dictionary<string, WixSearchInfo> allSearches = new Dictionary<string, WixSearchInfo>();
            Table wixFileSearchTable = bundle.Tables["WixFileSearch"];
            if (null != wixFileSearchTable && 0 < wixFileSearchTable.Rows.Count)
            {
                foreach (Row row in wixFileSearchTable.Rows)
                {
                    WixFileSearchInfo fileSearchInfo = new WixFileSearchInfo(row);
                    allSearches.Add(fileSearchInfo.Id, fileSearchInfo);
                }
            }

            Table wixRegistrySearchTable = bundle.Tables["WixRegistrySearch"];
            if (null != wixRegistrySearchTable && 0 < wixRegistrySearchTable.Rows.Count)
            {
                foreach (Row row in wixRegistrySearchTable.Rows)
                {
                    WixRegistrySearchInfo registrySearchInfo = new WixRegistrySearchInfo(row);
                    allSearches.Add(registrySearchInfo.Id, registrySearchInfo);
                }
            }

            Table wixComponentSearchTable = bundle.Tables["WixComponentSearch"];
            if (null != wixComponentSearchTable && 0 < wixComponentSearchTable.Rows.Count)
            {
                foreach (Row row in wixComponentSearchTable.Rows)
                {
                    WixComponentSearchInfo componentSearchInfo = new WixComponentSearchInfo(row);
                    allSearches.Add(componentSearchInfo.Id, componentSearchInfo);
                }
            }

            Table wixProductSearchTable = bundle.Tables["WixProductSearch"];
            if (null != wixProductSearchTable && 0 < wixProductSearchTable.Rows.Count)
            {
                foreach (Row row in wixProductSearchTable.Rows)
                {
                    WixProductSearchInfo productSearchInfo = new WixProductSearchInfo(row);
                    allSearches.Add(productSearchInfo.Id, productSearchInfo);
                }
            }

            // Merge in the variable/condition info and get the canonical ordering for
            // the searches.
            List<WixSearchInfo> orderedSearches = new List<WixSearchInfo>();
            Table wixSearchTable = bundle.Tables["WixSearch"];
            if (null != wixSearchTable && 0 < wixSearchTable.Rows.Count)
            {
                orderedSearches.Capacity = wixSearchTable.Rows.Count;
                foreach (Row row in wixSearchTable.Rows)
                {
                    WixSearchInfo searchInfo = allSearches[(string)row[0]];
                    searchInfo.AddWixSearchRowInfo(row);
                    orderedSearches.Add(searchInfo);
                }
            }

            // extract files that come from cabinet files (this does not extract files from merge modules)
            this.ExtractCabinets(cabinets);

            WixBundleRow bundleInfo = (WixBundleRow)bundleTable.Rows[0];
            bundleInfo.PerMachine = true; // default to per-machine but the first-per user package would flip it.

            // Get update if specified.
            Table bundleUpdateTable = bundle.Tables["WixBundleUpdate"];
            WixBundleUpdateRow bundleUpdateRow = null;
            if (null != bundleUpdateTable)
            {
                bundleUpdateRow = (WixBundleUpdateRow)bundleUpdateTable.Rows[0];
            }

            // Get update registration if specified.
            Table updateRegistrationTable = bundle.Tables["WixUpdateRegistration"];
            WixUpdateRegistrationRow updateRegistrationInfo = null;
            if (null != updateRegistrationTable)
            {
                updateRegistrationInfo = (WixUpdateRegistrationRow)updateRegistrationTable.Rows[0];
            }

            // Get the explicit payloads.
            Table payloadTable = bundle.Tables["Payload"];
            Dictionary<string, PayloadInfoRow> allPayloads = new Dictionary<string, PayloadInfoRow>(payloadTable.Rows.Count);

            Table payloadInfoTable = bundle.EnsureTable(core.TableDefinitions["PayloadInfo"]);
            foreach (PayloadInfoRow row in payloadInfoTable.Rows)
            {
                allPayloads.Add(row.Id, row);
            }

            RowDictionary<Row> payloadDisplayInformationRows = new RowDictionary<Row>(bundle.Tables["PayloadDisplayInformation"]);
            foreach (Row row in payloadTable.Rows)
            {
                string id = (string)row[0];

                PayloadInfoRow payloadInfo = null;

                if (allPayloads.ContainsKey(id))
                {
                    payloadInfo = allPayloads[id];
                }
                else
                {
                    allPayloads.Add(id, payloadInfo = (PayloadInfoRow)payloadInfoTable.CreateRow(row.SourceLineNumbers));
                }

                payloadInfo.FillFromPayloadRow(bundle, row);

                // Check if there is an override row for the display name or description.
                Row payloadDisplayInformationRow;
                if (payloadDisplayInformationRows.TryGet(id, out payloadDisplayInformationRow))
                {
                    if (!String.IsNullOrEmpty(payloadDisplayInformationRow[1] as string))
                    {
                        payloadInfo.ProductName = (string)payloadDisplayInformationRow[1];
                    }

                    if (!String.IsNullOrEmpty(payloadDisplayInformationRow[2] as string))
                    {
                        payloadInfo.Description = (string)payloadDisplayInformationRow[2];
                    }
                }

                if (payloadInfo.Packaging == PackagingType.Unknown)
                {
                    payloadInfo.Packaging = bundleInfo.DefaultPackagingType;
                }

                string normalizedPath = payloadInfo.Name.Replace('\\', '/');
                if (normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../"))
                {
                    this.core.OnMessage(WixWarnings.PayloadMustBeRelativeToCache(payloadInfo.SourceLineNumbers, "Payload", "Name", payloadInfo.Name));
                }
            }

            Dictionary<string, ContainerInfo> containers = new Dictionary<string, ContainerInfo>();
            Dictionary<string, bool> payloadsAddedToContainers = new Dictionary<string, bool>();

            // Create the list of containers.
            Table containerTable = bundle.Tables["Container"];
            if (null != containerTable)
            {
                foreach (Row row in containerTable.Rows)
                {
                    ContainerInfo container = new ContainerInfo(row, this.FileManager);
                    containers.Add(container.Id, container);
                }
            }

            // Create the default attached container for payloads that need to be attached but don't have an explicit container.
            ContainerInfo defaultAttachedContainer = new ContainerInfo("WixAttachedContainer", "bundle-attached.cab", "attached", null, this.FileManager);
            containers.Add(defaultAttachedContainer.Id, defaultAttachedContainer);

            Row baRow = baTable.Rows[0];
            string baPayloadId = (string)baRow[0];

            // Create lists of which payloads go in each container or are layout only.
            foreach (Row row in wixGroupTable.Rows)
            {
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                if (Enum.GetName(typeof(ComplexReferenceChildType), ComplexReferenceChildType.Payload) == rowChildType)
                {
                    PayloadInfoRow payload = allPayloads[rowChildName];

                    if (Enum.GetName(typeof(ComplexReferenceParentType), ComplexReferenceParentType.Container) == rowParentType)
                    {
                        ContainerInfo container = containers[rowParentName];

                        // Make sure the BA DLL is the first payload.
                        if (payload.Id.Equals(baPayloadId))
                        {
                            container.Payloads.Insert(0, payload);
                        }
                        else
                        {
                            container.Payloads.Add(payload);
                        }

                        payload.Container = container.Id;
                        payloadsAddedToContainers.Add(rowChildName, false);
                    }
                    else if (Enum.GetName(typeof(ComplexReferenceParentType), ComplexReferenceParentType.Layout) == rowParentType)
                    {
                        payload.LayoutOnly = true;
                    }
                }
            }

            ContainerInfo burnUXContainer;
            containers.TryGetValue(Compiler.BurnUXContainerId, out burnUXContainer);
            List<PayloadInfoRow> uxPayloads = null == burnUXContainer ? null : burnUXContainer.Payloads;

            // If we didn't get any UX payloads, it's an error!
            if (null == uxPayloads || 0 == uxPayloads.Count)
            {
                throw new WixException(WixErrors.MissingBundleInformation("BootstrapperApplication"));
            }

            // Get the catalog information
            Dictionary<string, CatalogInfo> catalogs = new Dictionary<string, CatalogInfo>();
            Table catalogTable = bundle.Tables["WixCatalog"];
            if (null != catalogTable)
            {
                foreach (WixCatalogRow catalogRow in catalogTable.Rows)
                {
                    // Each catalog is also a payload
                    string payloadId = Common.GenerateIdentifier("pay", true, catalogRow.SourceFile);
                    string catalogFile = this.FileManager.ResolveFile(catalogRow.SourceFile, "Catalog", catalogRow.SourceLineNumbers, BindStage.Normal);
                    PayloadInfoRow payloadInfo = PayloadInfoRow.Create(catalogRow.SourceLineNumbers, bundle, payloadId, Path.GetFileName(catalogFile), catalogFile, true, false, null, burnUXContainer.Id, PackagingType.Embedded);

                    // Add the payload to the UX container
                    allPayloads.Add(payloadInfo.Id, payloadInfo);
                    burnUXContainer.Payloads.Add(payloadInfo);
                    payloadsAddedToContainers.Add(payloadInfo.Id, true);

                    // Create the catalog info
                    CatalogInfo catalog = new CatalogInfo(catalogRow, payloadId);
                    catalogs.Add(catalog.Id, catalog);
                }
            }

            // Get the chain packages, this may add more payloads.
            Dictionary<string, ChainPackageInfo> allPackages = new Dictionary<string, ChainPackageInfo>();
            Dictionary<string, RollbackBoundaryInfo> allBoundaries = new Dictionary<string, RollbackBoundaryInfo>();
            foreach (Row row in chainPackageTable.Rows)
            {
                Compiler.ChainPackageType type = (Compiler.ChainPackageType)Enum.Parse(typeof(Compiler.ChainPackageType), row[1].ToString(), true);
                if (Compiler.ChainPackageType.RollbackBoundary == type)
                {
                    RollbackBoundaryInfo rollbackBoundary = new RollbackBoundaryInfo(row);
                    allBoundaries.Add(rollbackBoundary.Id, rollbackBoundary);
                }
                else // package
                {
                    Table chainPackageInfoTable = bundle.EnsureTable(this.core.TableDefinitions["ChainPackageInfo"]);

                    ChainPackageInfo packageInfo = new ChainPackageInfo(row, wixGroupTable, allPayloads, containers, this.FileManager, this.core, bundle);
                    allPackages.Add(packageInfo.Id, packageInfo);

                    chainPackageInfoTable.Rows.Add(packageInfo);

                    // Add package properties to resolve fields later.
                    if (null != variableCache)
                    {
                        Binder.PopulatePackageVariableCache(packageInfo, variableCache);
                    }
                }
            }

            // Determine patches to automatically slipstream.
            this.AutomaticallySlipstreamPatches(bundle, allPackages.Values);

            // NOTE: All payloads should be generated before here with the exception of specific engine and ux data files.

            ArrayList fileTransfers = new ArrayList();
            string layoutDirectory = Path.GetDirectoryName(bundleFile);

            // Handle any payloads not explicitly in a container.
            foreach (string payloadName in allPayloads.Keys)
            {
                if (!payloadsAddedToContainers.ContainsKey(payloadName))
                {
                    PayloadInfoRow payload = allPayloads[payloadName];
                    if (PackagingType.Embedded == payload.Packaging)
                    {
                        payload.Container = defaultAttachedContainer.Id;
                        defaultAttachedContainer.Payloads.Add(payload);
                    }
                    else if (!String.IsNullOrEmpty(payload.FullFileName))
                    {
                        FileTransfer transfer;
                        if (FileTransfer.TryCreate(payload.FullFileName, Path.Combine(layoutDirectory, payload.Name), false, "Payload", payload.SourceLineNumbers, out transfer))
                        {
                            fileTransfers.Add(transfer);
                        }
                    }
                }
            }

            // Give the UX payloads their embedded IDs...
            for (int uxPayloadIndex = 0; uxPayloadIndex < uxPayloads.Count; ++uxPayloadIndex)
            {
                PayloadInfoRow payload = uxPayloads[uxPayloadIndex];

                // In theory, UX payloads could be embedded in the UX CAB, external to the
                // bundle EXE, or even downloaded. The current engine requires the UX to be
                // fully present before any downloading starts, so that rules out downloading.
                // Also, the burn engine does not currently copy external UX payloads into
                // the temporary UX directory correctly, so we don't allow external either.
                if (PackagingType.Embedded != payload.Packaging)
                {
                    core.OnMessage(WixWarnings.UxPayloadsOnlySupportEmbedding(payload.SourceLineNumbers, payload.FullFileName));
                    payload.Packaging = PackagingType.Embedded;
                }

                payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloadIndex);
            }

            if (this.core.EncounteredError)
            {
                return false;
            }

            // If catalog files exist, non-UX payloads should validate with the catalog
            if (catalogs.Count > 0)
            {
                foreach (PayloadInfoRow payloadInfo in allPayloads.Values)
                {
                    if (String.IsNullOrEmpty(payloadInfo.EmbeddedId))
                    {
                        VerifyPayloadWithCatalog(payloadInfo, catalogs);
                    }
                }
            }

            if (this.core.EncounteredError)
            {
                return false;
            }

            // Process the chain of packages to add them in the correct order
            // and assign the forward rollback boundaries as appropriate. Remember
            // rollback boundaries are authored as elements in the chain which
            // we re-interpret here to add them as attributes on the next available
            // package in the chain. Essentially we mark some packages as being
            // the start of a rollback boundary when installing and repairing.
            // We handle uninstall (aka: backwards) rollback boundaries after
            // we get these install/repair (aka: forward) rollback boundaries
            // defined.
            ChainInfo chain = new ChainInfo(chainTable.Rows[0]); // WixChain table always has one and only row in it.
            RollbackBoundaryInfo previousRollbackBoundary = new RollbackBoundaryInfo("WixDefaultBoundary"); // ensure there is always a rollback boundary at the beginning of the chain.
            foreach (Row row in wixGroupTable.Rows)
            {
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                if ("PackageGroup" == rowParentType && "WixChain" == rowParentName && "Package" == rowChildType)
                {
                    ChainPackageInfo packageInfo = null;
                    if (allPackages.TryGetValue(rowChildName, out packageInfo))
                    {
                        if (null != previousRollbackBoundary)
                        {
                            chain.RollbackBoundaries.Add(previousRollbackBoundary);

                            packageInfo.RollbackBoundary = previousRollbackBoundary;
                            previousRollbackBoundary = null;
                        }

                        chain.Packages.Add(packageInfo);
                    }
                    else // must be a rollback boundary.
                    {
                        // Discard the next rollback boundary if we have a previously defined boundary. Of course,
                        // a boundary specifically defined will override the default boundary.
                        RollbackBoundaryInfo nextRollbackBoundary = allBoundaries[rowChildName];
                        if (null != previousRollbackBoundary && !previousRollbackBoundary.Default)
                        {
                            this.core.OnMessage(WixWarnings.DiscardedRollbackBoundary(nextRollbackBoundary.SourceLineNumbers, nextRollbackBoundary.Id));
                        }
                        else
                        {
                            previousRollbackBoundary = nextRollbackBoundary;
                        }
                    }
                }
            }

            if (null != previousRollbackBoundary)
            {
                this.core.OnMessage(WixWarnings.DiscardedRollbackBoundary(previousRollbackBoundary.SourceLineNumbers, previousRollbackBoundary.Id));
            }

            // With the forward rollback boundaries assigned, we can now go
            // through the packages with rollback boundaries and assign backward
            // rollback boundaries. Backward rollback boundaries are used when
            // the chain is going "backwards" which (AFAIK) only happens during
            // uninstall.
            //
            // Consider the scenario with three packages: A, B and C. Packages A
            // and C are marked as rollback boundary packages and package B is
            // not. The naive implementation would execute the chain like this
            // (numbers indicate where rollback boundaries would end up):
            //      install:    1 A B 2 C
            //      uninstall:  2 C B 1 A
            //
            // The uninstall chain is wrong, A and B should be grouped together
            // not C and B. The fix is to label packages with a "backwards"
            // rollback boundary used during uninstall. The backwards rollback
            // boundaries are assigned to the package *before* the next rollback
            // boundary. Using our example from above again, I'll mark the
            // backwards rollback boundaries prime (aka: with ').
            //      install:    1 A B 1' 2 C 2'
            //      uninstall:  2' C 2 1' B A 1
            //
            // If the marked boundaries are ignored during install you get the
            // same thing as above (good) and if the non-marked boundaries are
            // ignored during uninstall then A and B are correctly grouped.
            // Here's what it looks like without all the markers:
            //      install:    1 A B 2 C
            //      uninstall:  2 C 1 B A
            // Woot!
            string previousRollbackBoundaryId = null;
            ChainPackageInfo previousPackage = null;
            foreach (ChainPackageInfo package in chain.Packages)
            {
                if (null != package.RollbackBoundary)
                {
                    if (null != previousPackage)
                    {
                        previousPackage.RollbackBoundaryBackwardId = previousRollbackBoundaryId;
                    }

                    previousRollbackBoundaryId = package.RollbackBoundary.Id;
                }

                previousPackage = package;
            }

            if (!String.IsNullOrEmpty(previousRollbackBoundaryId) && null != previousPackage)
            {
                previousPackage.RollbackBoundaryBackwardId = previousRollbackBoundaryId;
            }

            // Give all embedded payloads that don't have an embedded ID yet an embedded ID.
            int payloadIndex = 0;
            foreach (PayloadInfoRow payload in allPayloads.Values)
            {
                Debug.Assert(PackagingType.Unknown != payload.Packaging);

                if (PackagingType.Embedded == payload.Packaging && String.IsNullOrEmpty(payload.EmbeddedId))
                {
                    payload.EmbeddedId = String.Format(CultureInfo.InvariantCulture, BurnCommon.BurnAttachedContainerEmbeddedIdFormat, payloadIndex);
                    ++payloadIndex;
                }
            }

            // Load the MsiProperty information...
            Table msiPropertyTable = bundle.Tables["MsiProperty"];
            if (null != msiPropertyTable && 0 < msiPropertyTable.Rows.Count)
            {
                foreach (Row row in msiPropertyTable.Rows)
                {
                    MsiPropertyInfo msiProperty = new MsiPropertyInfo(row);

                    ChainPackageInfo package;
                    if (allPackages.TryGetValue(msiProperty.PackageId, out package))
                    {
                        package.MsiProperties.Add(msiProperty);
                    }
                    else
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", msiProperty.PackageId));
                    }
                }
            }

            // Load the SlipstreamMsp information...
            Table slipstreamMspTable = bundle.Tables["SlipstreamMsp"];
            if (null != slipstreamMspTable && 0 < slipstreamMspTable.Rows.Count)
            {
                foreach (Row row in slipstreamMspTable.Rows)
                {
                    string msiPackageId = (string)row[0];
                    string mspPackageId = (string)row[1];

                    if (!allPackages.ContainsKey(mspPackageId))
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", mspPackageId));
                        continue;
                    }

                    ChainPackageInfo package;
                    if (!allPackages.TryGetValue(msiPackageId, out package))
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", msiPackageId));
                        continue;
                    }

                    package.SlipstreamMsps.Add(mspPackageId);
                }
            }

            // Load the ExitCode information...
            Table exitCodeTable = bundle.Tables["ExitCode"];
            if (null != exitCodeTable && 0 < exitCodeTable.Rows.Count)
            {
                foreach (Row row in exitCodeTable.Rows)
                {
                    ExitCodeInfo exitCode = new ExitCodeInfo(row);

                    ChainPackageInfo package;
                    if (allPackages.TryGetValue(exitCode.PackageId, out package))
                    {
                        package.ExitCodes.Add(exitCode);
                    }
                    else
                    {
                        core.OnMessage(WixErrors.IdentifierNotFound("Package", exitCode.PackageId));
                    }
                }
            }

            // Load the CommandLine information...
            Dictionary<string, List<WixCommandLineRow>> commandLinesByPackage = new Dictionary<string, List<WixCommandLineRow>>();
            Table commandLineTable = bundle.Tables["WixCommandLine"];
            if (null != commandLineTable && 0 < commandLineTable.Rows.Count)
            {
                foreach (WixCommandLineRow row in commandLineTable.Rows)
                {
                    if (!commandLinesByPackage.ContainsKey(row.PackageId))
                    {
                        commandLinesByPackage.Add(row.PackageId, new List<WixCommandLineRow>());
                    }

                    List<WixCommandLineRow> commandLines = commandLinesByPackage[row.PackageId];
                    commandLines.Add(row);
                }
            }

            // Resolve any delayed fields before generating the manifest.
            if (0 < delayedFields.Count)
            {
                this.ResolveDelayedFields(bundle, delayedFields, variableCache, null);
            }

            // Process WixApprovedExeForElevation rows.
            Table wixApprovedExeForElevationTable = bundle.Tables["WixApprovedExeForElevation"];
            List<ApprovedExeForElevation> approvedExesForElevation = new List<ApprovedExeForElevation>();
            if (null != wixApprovedExeForElevationTable && 0 < wixApprovedExeForElevationTable.Rows.Count)
            {
                foreach (WixApprovedExeForElevationRow wixApprovedExeForElevationRow in wixApprovedExeForElevationTable.Rows)
                {
                    ApprovedExeForElevation approvedExeForElevation = new ApprovedExeForElevation(wixApprovedExeForElevationRow);
                    approvedExesForElevation.Add(approvedExeForElevation);
                }
            }

            // Set the overridable bundle provider key.
            this.SetBundleProviderKey(bundle, bundleInfo);

            // Import or generate dependency providers for packages in the manifest.
            this.ProcessDependencyProviders(bundle, allPackages);

            // Generate the core-defined BA manifest tables...
            this.GenerateBAManifestPackageTables(bundle, chain.Packages);

            this.GenerateBAManifestPayloadTables(bundle, chain.Packages, allPayloads);

            foreach (BinderExtension extension in this.extensions)
            {
                extension.BundleFinalize(bundle);
            }

            // Start creating the bundle.
            this.PopulateBundleInfoFromChain(bundleInfo, chain.Packages);
            this.PopulateChainInfoTables(bundle, bundleInfo, chain.Packages);
            this.GenerateBAManifestBundleTables(bundle, bundleInfo);

            // Copy the burn.exe to a writable location then mark it to be moved to its
            // final build location.
            string stubPlatform;
            if (Platform.X64 == bundleInfo.Platform) // today, the x64 Burn uses the x86 stub.
            {
                stubPlatform = "x86";
            }
            else
            {
                stubPlatform = bundleInfo.Platform.ToString();
            }
            string wixExeDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform);
            string stubFile = Path.Combine(wixExeDirectory, "burn.exe");
            string bundleTempPath = Path.Combine(this.TempFilesLocation, Path.GetFileName(bundleFile));

            this.core.OnMessage(WixVerboses.GeneratingBundle(bundleTempPath, stubFile));
            File.Copy(stubFile, bundleTempPath, true);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            FileTransfer bundleTransfer;
            if (FileTransfer.TryCreate(bundleTempPath, bundleFile, true, "Bundle", bundleInfo.SourceLineNumbers, out bundleTransfer))
            {
                bundleTransfer.Built = true;
                fileTransfers.Add(bundleTransfer);
            }

            // Create our manifests, CABs and final EXE...
            string baManifestPath = Path.Combine(this.TempFilesLocation, "bundle-BootstrapperApplicationData.xml");
            this.CreateBootstrapperApplicationManifest(bundle, baManifestPath, uxPayloads);

            // Add the bootstrapper application manifest to the set of UX payloads.
            PayloadInfoRow baManifestPayload = PayloadInfoRow.Create(null /*TODO*/, bundle, Common.GenerateIdentifier("ux", true, "BootstrapperApplicationData.xml"),
                "BootstrapperApplicationData.xml", baManifestPath, false, true, null, burnUXContainer.Id, PackagingType.Embedded);
            baManifestPayload.EmbeddedId = string.Format(CultureInfo.InvariantCulture, BurnCommon.BurnUXContainerEmbeddedIdFormat, uxPayloads.Count);
            uxPayloads.Add(baManifestPayload);

            // Create all the containers except the UX container first so the manifest in the UX container can contain all size and hash information.
            foreach (ContainerInfo container in containers.Values)
            {
                if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                {
                    this.CreateContainer(container, null);
                }
            }

            string manifestPath = Path.Combine(this.TempFilesLocation, "bundle-manifest.xml");
            this.CreateBurnManifest(bundleFile, bundleInfo, bundleUpdateRow, updateRegistrationInfo, manifestPath, allRelatedBundles, allVariables, orderedSearches, allPayloads, chain, containers, catalogs, bundle.Tables["WixBundleTag"], approvedExesForElevation, commandLinesByPackage);

            this.UpdateBurnResources(bundleTempPath, bundleFile, bundleInfo);

            // update the .wixburn section to point to at the UX and attached container(s) then attach the container(s) if they should be attached.
            using (BurnWriter writer = BurnWriter.Open(bundleTempPath, this.core))
            {
                FileInfo burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, bundleInfo.BundleId);

                // Always create UX container and attach it first
                this.CreateContainer(burnUXContainer, manifestPath);
                writer.AppendContainer(burnUXContainer.TempPath, BurnWriter.Container.UX);

                // Now append all other attached containers
                foreach (ContainerInfo container in containers.Values)
                {
                    if (container.Type == "attached")
                    {
                        // The container was only created if it had payloads.
                        if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                        {
                            writer.AppendContainer(container.TempPath, BurnWriter.Container.Attached);
                        }
                    }
                }
            }

            // Output the bundle to a file
            if (null != this.pdbFile)
            {
                Pdb pdb = new Pdb(null);
                pdb.Output = bundle;
                pdb.Save(this.pdbFile, null, this.WixVariableResolver, this.TempFilesLocation);
            }

            // Add detached containers to the list of file transfers.
            foreach (ContainerInfo container in containers.Values)
            {
                if ("detached" == container.Type)
                {
                    FileTransfer transfer;
                    if (FileTransfer.TryCreate(Path.Combine(this.TempFilesLocation, container.Name), Path.Combine(layoutDirectory, container.Name), true, "Container", container.SourceLineNumbers, out transfer))
                    {
                        transfer.Built = true;
                        fileTransfers.Add(transfer);
                    }
                }
            }

            // layout media
            string bundleFilename = Path.GetFileName(bundleFile);
            if ("setup.exe".Equals(bundleFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.core.OnMessage(WixErrors.InsecureBundleFilename(bundleFilename));
            }

            try
            {
                this.core.OnMessage(WixVerboses.LayingOutMedia());
                this.LayoutMedia(fileTransfers, this.suppressAclReset);
            }
            finally
            {
                if (!String.IsNullOrEmpty(this.contentsFile))
                {
                    this.CreateContentsFile(this.contentsFile, allPayloads.Values);
                }

                if (!String.IsNullOrEmpty(this.outputsFile))
                {
                    this.CreateOutputsFile(this.outputsFile, fileTransfers, this.pdbFile);
                }

                if (!String.IsNullOrEmpty(this.builtOutputsFile))
                {
                    this.CreateBuiltOutputsFile(this.builtOutputsFile, fileTransfers, this.pdbFile);
                }
            }

            return !this.core.EncounteredError;
        }

        private void GenerateBAManifestPackageTables(Output bundle, List<ChainPackageInfo> chainPackages)
        {
            Table wixPackagePropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixPackageProperties"]);

            foreach (ChainPackageInfo package in chainPackages)
            {
                Row row = wixPackagePropertiesTable.CreateRow(package.SourceLineNumbers);
                row[0] = package.Id;
                row[1] = package.Vital ? "yes" : "no";
                row[2] = package.DisplayName;
                row[3] = package.Description;
                row[4] = package.Size.ToString(CultureInfo.InvariantCulture); // TODO: DownloadSize (compressed) (what does this mean when it's embedded?)
                row[5] = package.Size.ToString(CultureInfo.InvariantCulture); // Package.Size (uncompressed)
                row[6] = package.InstallSize.ToString(CultureInfo.InvariantCulture); // InstallSize (required disk space)
                row[7] = package.ChainPackageType.ToString(CultureInfo.InvariantCulture);
                row[8] = package.Permanent ? "yes" : "no";
                row[9] = package.LogPathVariable;
                row[10] = package.RollbackLogPathVariable;
                row[11] = (PackagingType.Embedded == package.PackagePayload.Packaging) ? "yes" : "no";
                row[12] = package.DisplayInternalUI ? "yes" : "no";
                if (!String.IsNullOrEmpty(package.ProductCode))
                {
                    row[13] = package.ProductCode;
                }
                if (!String.IsNullOrEmpty(package.UpgradeCode))
                {
                    row[14] = package.UpgradeCode;
                }
                if (!String.IsNullOrEmpty(package.Version))
                {
                    row[15] = package.Version;
                }
                if (!String.IsNullOrEmpty(package.InstallCondition))
                {
                    row[16] = package.InstallCondition;
                }
                switch (package.Cache)
                {
                    case YesNoAlwaysType.No:
                        row[17] = "no";
                        break;
                    case YesNoAlwaysType.Yes:
                        row[17] = "yes";
                        break;
                    case YesNoAlwaysType.Always:
                        row[17] = "always";
                        break;
                }

                Table wixPackageFeatureInfoTable = bundle.EnsureTable(this.core.TableDefinitions["WixPackageFeatureInfo"]);

                foreach (MsiFeature feature in package.MsiFeatures)
                {
                    Row packageFeatureInfoRow = wixPackageFeatureInfoTable.CreateRow(package.SourceLineNumbers);
                    packageFeatureInfoRow[0] = package.Id;
                    packageFeatureInfoRow[1] = feature.Name;
                    packageFeatureInfoRow[2] = Convert.ToString(feature.Size, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[3] = feature.Parent;
                    packageFeatureInfoRow[4] = feature.Title;
                    packageFeatureInfoRow[5] = feature.Description;
                    packageFeatureInfoRow[6] = Convert.ToString(feature.Display, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[7] = Convert.ToString(feature.Level, CultureInfo.InvariantCulture);
                    packageFeatureInfoRow[8] = feature.Directory;
                    packageFeatureInfoRow[9] = Convert.ToString(feature.Attributes, CultureInfo.InvariantCulture);
                }
            }
        }

        private void GenerateBAManifestPayloadTables(Output bundle, List<ChainPackageInfo> chainPackages, Dictionary<string, PayloadInfoRow> payloads)
        {
            Table wixPayloadPropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixPayloadProperties"]);

            foreach (ChainPackageInfo package in chainPackages)
            {
                PayloadInfoRow packagePayload = payloads[package.Payload];

                Row payloadRow = wixPayloadPropertiesTable.CreateRow(packagePayload.SourceLineNumbers);
                payloadRow[0] = packagePayload.Id;
                payloadRow[1] = package.Id;
                payloadRow[2] = packagePayload.Container;
                payloadRow[3] = packagePayload.Name;
                payloadRow[4] = packagePayload.FileSize.ToString();
                payloadRow[5] = packagePayload.DownloadUrl;
                payloadRow[6] = packagePayload.LayoutOnly ? "yes" : "no";

                foreach (PayloadInfoRow childPayload in package.Payloads)
                {
                    payloadRow = wixPayloadPropertiesTable.CreateRow(childPayload.SourceLineNumbers);
                    payloadRow[0] = childPayload.Id;
                    payloadRow[1] = package.Id;
                    payloadRow[2] = childPayload.Container;
                    payloadRow[3] = childPayload.Name;
                    payloadRow[4] = childPayload.FileSize.ToString();
                    payloadRow[5] = childPayload.DownloadUrl;
                    payloadRow[6] = childPayload.LayoutOnly ? "yes" : "no";
                }
            }

            foreach (PayloadInfoRow payload in payloads.Values)
            {
                if (payload.LayoutOnly)
                {
                    Row row = wixPayloadPropertiesTable.CreateRow(payload.SourceLineNumbers);
                    row[0] = payload.Id;
                    row[1] = null;
                    row[2] = payload.Container;
                    row[3] = payload.Name;
                    row[4] = payload.FileSize.ToString();
                    row[5] = payload.DownloadUrl;
                    row[6] = payload.LayoutOnly ? "yes" : "no";
                }
            }
        }

        private void AutomaticallySlipstreamPatches(Output bundle, ICollection<ChainPackageInfo> packages)
        {
            List<ChainPackageInfo> msiPackages = new List<ChainPackageInfo>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsProductCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();
            Dictionary<string, List<WixBundlePatchTargetCodeRow>> targetsUpgradeCode = new Dictionary<string, List<WixBundlePatchTargetCodeRow>>();

            foreach (ChainPackageInfo package in packages)
            {
                if (Compiler.ChainPackageType.Msi == package.ChainPackageType)
                {
                    // Keep track of all MSI packages.
                    msiPackages.Add(package);
                }
                else if (Compiler.ChainPackageType.Msp == package.ChainPackageType && package.Slipstream)
                {
                    // Index target ProductCodes and UpgradeCodes for slipstreamed MSPs.
                    foreach (WixBundlePatchTargetCodeRow row in package.TargetCodes)
                    {
                        if (row.TargetsProductCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsProductCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsProductCode.Add(row.TargetCode, rows);
                            }

                            rows.Add(row);
                        }
                        else if (row.TargetsUpgradeCode)
                        {
                            List<WixBundlePatchTargetCodeRow> rows;
                            if (!targetsUpgradeCode.TryGetValue(row.TargetCode, out rows))
                            {
                                rows = new List<WixBundlePatchTargetCodeRow>();
                                targetsUpgradeCode.Add(row.TargetCode, rows);
                            }
                        }
                    }
                }
            }

            Table slipstreamMspTable = bundle.EnsureTable(this.core.TableDefinitions["SlipstreamMsp"]);
            RowDictionary<Row> slipstreamMspRows = new RowDictionary<Row>(slipstreamMspTable);

            // Loop through the MSI and slipstream patches targeting it.
            foreach (ChainPackageInfo msi in msiPackages)
            {
                List<WixBundlePatchTargetCodeRow> rows;
                if (targetsProductCode.TryGetValue(msi.ProductCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Row slipstreamMspRow = slipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.Id;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            slipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }

                if (!String.IsNullOrEmpty(msi.UpgradeCode) && targetsUpgradeCode.TryGetValue(msi.UpgradeCode, out rows))
                {
                    foreach (WixBundlePatchTargetCodeRow row in rows)
                    {
                        Row slipstreamMspRow = slipstreamMspTable.CreateRow(row.SourceLineNumbers, false);
                        slipstreamMspRow[0] = msi.Id;
                        slipstreamMspRow[1] = row.MspPackageId;

                        if (slipstreamMspRows.TryAdd(slipstreamMspRow))
                        {
                            slipstreamMspTable.Rows.Add(slipstreamMspRow);
                        }
                    }

                    rows = null;
                }
            }
        }

        private void PopulateBundleInfoFromChain(WixBundleRow bundleInfo, List<ChainPackageInfo> chainPackages)
        {
            foreach (ChainPackageInfo package in chainPackages)
            {
                if (bundleInfo.PerMachine && YesNoDefaultType.No == package.PerMachine)
                {
                    this.core.OnMessage(WixVerboses.SwitchingToPerUserPackage(package.PackagePayload.FullFileName));
                    bundleInfo.PerMachine = false;
                }
            }
        }

        private void PopulateChainInfoTables(Output bundle, WixBundleRow bundleInfo, List<ChainPackageInfo> chainPackages)
        {
            bool hasPerMachineNonPermanentPackages = false;

            foreach (ChainPackageInfo package in chainPackages)
            {
                // Update package scope from bundle scope if default.
                if (YesNoDefaultType.Default == package.PerMachine)
                {
                    package.PerMachine = bundleInfo.PerMachine ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                }

                // Keep track if any per-machine non-permanent packages exist.
                if (YesNoDefaultType.Yes == package.PerMachine && 0 < package.Provides.Count && !package.Permanent)
                {
                    hasPerMachineNonPermanentPackages = true;
                }

                switch (package.ChainPackageType)
                {
                    case Compiler.ChainPackageType.Msi:
                        Table chainMsiPackageTable = bundle.EnsureTable(this.core.TableDefinitions["ChainMsiPackage"]);
                        ChainMsiPackageRow row = (ChainMsiPackageRow)chainMsiPackageTable.CreateRow(null);
                        row.ChainPackage = package.Id;
                        row.ProductCode = package.ProductCode;
                        row.ProductLanguage = Convert.ToInt32(package.Language, CultureInfo.InvariantCulture);
                        row.ProductName = package.DisplayName;
                        row.ProductVersion = package.Version;
                        if (!String.IsNullOrEmpty(package.UpgradeCode))
                        {
                            row.UpgradeCode = package.UpgradeCode;
                        }
                        break;
                    default:
                        break;
                }
            }

            // We will only register packages in the same scope as the bundle.
            // Warn if any packages with providers are in a different scope
            // and not permanent (permanents typically don't need a ref-count).
            if (!bundleInfo.PerMachine && hasPerMachineNonPermanentPackages)
            {
                this.core.OnMessage(WixWarnings.NoPerMachineDependencies());
            }
        }

        private void GenerateBAManifestBundleTables(Output bundle, WixBundleRow bundleInfo)
        {
            Table wixBundlePropertiesTable = bundle.EnsureTable(this.core.TableDefinitions["WixBundleProperties"]);
            Row row = wixBundlePropertiesTable.CreateRow(bundleInfo.SourceLineNumbers);
            row[0] = bundleInfo.Name;
            row[1] = bundleInfo.LogPathVariable;
            row[2] = (YesNoDefaultType.Yes == bundleInfo.Compressed) ? "yes" : "no";
            row[3] = bundleInfo.BundleId.ToString("B");
            row[4] = bundleInfo.UpgradeCode;
            row[5] = bundleInfo.PerMachine ? "yes" : "no";
        }

        private void CreateContainer(ContainerInfo container, string manifestFile)
        {
            int payloadCount = container.Payloads.Count; // The number of embedded payloads
            if (!String.IsNullOrEmpty(manifestFile))
            {
                ++payloadCount;
            }

            using (WixCreateCab cab = new WixCreateCab(Path.GetFileName(container.TempPath), Path.GetDirectoryName(container.TempPath), payloadCount, 0, 0, this.defaultCompressionLevel))
            {
                // If a manifest was provided always add it as "payload 0" to the container.
                if (!String.IsNullOrEmpty(manifestFile))
                {
                    cab.AddFile(manifestFile, "0");
                }

                foreach (PayloadInfoRow payload in container.Payloads)
                {
                    Debug.Assert(PackagingType.Embedded == payload.Packaging);
                    this.core.OnMessage(WixVerboses.LoadingPayload(payload.FullFileName));
                    cab.AddFile(payload.FullFileName, payload.EmbeddedId);
                }

                cab.Complete();
            }
        }

        private void CreateBootstrapperApplicationManifest(Output bundle, string path, List<PayloadInfoRow> uxPayloads)
        {
            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.Unicode))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("BootstrapperApplicationData", "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData");

                foreach (Table table in bundle.Tables)
                {
                    if (table.Definition.IsBootstrapperApplicationData && null != table.Rows && 0 < table.Rows.Count)
                    {
                        // We simply assert that the table (and field) name is valid, because
                        // this is up to the extension developer to get right. An author will
                        // only affect the attribute value, and that will get properly escaped.
#if DEBUG
                        Debug.Assert(CompilerCore.IsIdentifier(table.Name));
                        foreach (ColumnDefinition column in table.Definition.Columns)
                        {
                            Debug.Assert(CompilerCore.IsIdentifier(column.Name));
                        }
#endif // DEBUG

                        foreach (Row row in table.Rows)
                        {
                            writer.WriteStartElement(table.Name);

                            foreach (Field field in row.Fields)
                            {
                                if (null != field.Data)
                                {
                                    writer.WriteAttributeString(field.Column.Name, field.Data.ToString());
                                }
                            }

                            writer.WriteEndElement();
                        }
                    }
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void CreateBurnManifest(string outputPath, WixBundleRow bundleInfo, WixBundleUpdateRow updateRow, WixUpdateRegistrationRow updateRegistrationInfo, string path, List<RelatedBundleInfo> allRelatedBundles, List<VariableInfo> allVariables, List<WixSearchInfo> orderedSearches, Dictionary<string, PayloadInfoRow> allPayloads, ChainInfo chain, Dictionary<string, ContainerInfo> containers, Dictionary<string, CatalogInfo> catalogs, Table wixBundleTagTable, List<ApprovedExeForElevation> approvedExesForElevation, Dictionary<string, List<WixCommandLineRow>> commandLinesByPackage)
        {
            string executableName = Path.GetFileName(outputPath);

            using (XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("BurnManifest", BurnCommon.BurnNamespace);

                // Write the condition, if there is one
                if (null != bundleInfo.Condition)
                {
                    writer.WriteElementString("Condition", bundleInfo.Condition);
                }

                // Write the log element if default logging wasn't disabled.
                if (!String.IsNullOrEmpty(bundleInfo.LogPrefix))
                {
                    writer.WriteStartElement("Log");
                    if (!String.IsNullOrEmpty(bundleInfo.LogPathVariable))
                    {
                        writer.WriteAttributeString("PathVariable", bundleInfo.LogPathVariable);
                    }
                    writer.WriteAttributeString("Prefix", bundleInfo.LogPrefix);
                    writer.WriteAttributeString("Extension", bundleInfo.LogExtension);
                    writer.WriteEndElement();
                }

                if (null != updateRow)
                {
                    writer.WriteStartElement("Update");
                    writer.WriteAttributeString("Location", updateRow.Location);
                    writer.WriteEndElement(); // </Update>
                }

                // Write the RelatedBundle elements
                foreach (RelatedBundleInfo relatedBundle in allRelatedBundles)
                {
                    relatedBundle.WriteXml(writer);
                }

                // Write the variables
                foreach (VariableInfo variable in allVariables)
                {
                    variable.WriteXml(writer);
                }

                // Write the searches
                foreach (WixSearchInfo searchinfo in orderedSearches)
                {
                    searchinfo.WriteXml(writer);
                }

                // write the UX element
                writer.WriteStartElement("UX");
                if (!String.IsNullOrEmpty(bundleInfo.SplashScreenBitmapPath))
                {
                    writer.WriteAttributeString("SplashScreen", "yes");
                }

                // write the UX allPayloads...
                List<PayloadInfoRow> uxPayloads = containers[Compiler.BurnUXContainerId].Payloads;
                foreach (PayloadInfoRow payload in uxPayloads)
                {
                    writer.WriteStartElement("Payload");
                    WriteBurnManifestPayloadAttributes(writer, payload, true, this.FileManager, allPayloads);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();

                // write the catalog elements
                if (catalogs.Count > 0)
                {
                    foreach (CatalogInfo catalog in catalogs.Values)
                    {
                        writer.WriteStartElement("Catalog");
                        writer.WriteAttributeString("Id", catalog.Id);
                        writer.WriteAttributeString("Payload", catalog.PayloadId);
                        writer.WriteEndElement();
                    }
                }

                int attachedContainerIndex = 1; // count starts at one because UX container is "0".
                foreach (ContainerInfo container in containers.Values)
                {
                    if (Compiler.BurnUXContainerId != container.Id && 0 < container.Payloads.Count)
                    {
                        writer.WriteStartElement("Container");
                        WriteBurnManifestContainerAttributes(writer, executableName, container, attachedContainerIndex, this.FileManager);
                        writer.WriteEndElement();
                        if ("attached" == container.Type)
                        {
                            attachedContainerIndex++;
                        }
                    }
                }

                foreach (PayloadInfoRow payload in allPayloads.Values)
                {
                    if (PackagingType.Embedded == payload.Packaging && Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, true, this.FileManager, allPayloads);
                        writer.WriteEndElement();
                    }
                    else if (PackagingType.External == payload.Packaging)
                    {
                        writer.WriteStartElement("Payload");
                        WriteBurnManifestPayloadAttributes(writer, payload, false, this.FileManager, allPayloads);
                        writer.WriteEndElement();
                    }
                }

                foreach (RollbackBoundaryInfo rollbackBoundary in chain.RollbackBoundaries)
                {
                    writer.WriteStartElement("RollbackBoundary");
                    writer.WriteAttributeString("Id", rollbackBoundary.Id);
                    writer.WriteAttributeString("Vital", YesNoType.Yes == rollbackBoundary.Vital ? "yes" : "no");
                    writer.WriteEndElement();
                }

                // Write the registration information...
                writer.WriteStartElement("Registration");

                writer.WriteAttributeString("Id", bundleInfo.BundleId.ToString("B"));
                writer.WriteAttributeString("ExecutableName", executableName);
                writer.WriteAttributeString("PerMachine", bundleInfo.PerMachine ? "yes" : "no");
                writer.WriteAttributeString("Tag", bundleInfo.Tag);
                writer.WriteAttributeString("Version", bundleInfo.Version);
                writer.WriteAttributeString("ProviderKey", bundleInfo.ProviderKey);

                writer.WriteStartElement("Arp");
                writer.WriteAttributeString("Register", (0 < bundleInfo.DisableModify && bundleInfo.DisableRemove) ? "no" : "yes"); // do not register if disabled modify and remove.
                writer.WriteAttributeString("DisplayName", bundleInfo.Name);
                writer.WriteAttributeString("DisplayVersion", bundleInfo.Version);

                if (!String.IsNullOrEmpty(bundleInfo.Publisher))
                {
                    writer.WriteAttributeString("Publisher", bundleInfo.Publisher);
                }

                if (!String.IsNullOrEmpty(bundleInfo.HelpLink))
                {
                    writer.WriteAttributeString("HelpLink", bundleInfo.HelpLink);
                }

                if (!String.IsNullOrEmpty(bundleInfo.HelpTelephone))
                {
                    writer.WriteAttributeString("HelpTelephone", bundleInfo.HelpTelephone);
                }

                if (!String.IsNullOrEmpty(bundleInfo.AboutUrl))
                {
                    writer.WriteAttributeString("AboutUrl", bundleInfo.AboutUrl);
                }

                if (!String.IsNullOrEmpty(bundleInfo.UpdateUrl))
                {
                    writer.WriteAttributeString("UpdateUrl", bundleInfo.UpdateUrl);
                }

                if (!String.IsNullOrEmpty(bundleInfo.ParentName))
                {
                    writer.WriteAttributeString("ParentDisplayName", bundleInfo.ParentName);
                }

                if (1 == bundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "yes");
                }
                else if (2 == bundleInfo.DisableModify)
                {
                    writer.WriteAttributeString("DisableModify", "button");
                }

                if (bundleInfo.DisableRemove)
                {
                    writer.WriteAttributeString("DisableRemove", "yes");
                }
                writer.WriteEndElement(); // </Arp>

                if (null != updateRegistrationInfo)
                {
                    writer.WriteStartElement("Update"); // <Update>
                    writer.WriteAttributeString("Manufacturer", updateRegistrationInfo.Manufacturer);

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.Department))
                    {
                        writer.WriteAttributeString("Department", updateRegistrationInfo.Department);
                    }

                    if (!String.IsNullOrEmpty(updateRegistrationInfo.ProductFamily))
                    {
                        writer.WriteAttributeString("ProductFamily", updateRegistrationInfo.ProductFamily);
                    }

                    writer.WriteAttributeString("Name", updateRegistrationInfo.Name);
                    writer.WriteAttributeString("Classification", updateRegistrationInfo.Classification);
                    writer.WriteEndElement(); // </Update>
                }

                if (null != wixBundleTagTable)
                {
                    foreach (Row row in wixBundleTagTable.Rows)
                    {
                        writer.WriteStartElement("SoftwareTag");
                        writer.WriteAttributeString("Filename", (string)row[0]);
                        writer.WriteAttributeString("Regid", (string)row[1]);
                        writer.WriteAttributeString("Path", (string)row[3]);
                        writer.WriteCData((string)row[5]);
                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndElement(); // </Register>

                // write the Chain...
                writer.WriteStartElement("Chain");
                if (chain.DisableRollback)
                {
                    writer.WriteAttributeString("DisableRollback", "yes");
                }

                if (chain.DisableSystemRestore)
                {
                    writer.WriteAttributeString("DisableSystemRestore", "yes");
                }

                if (chain.ParallelCache)
                {
                    writer.WriteAttributeString("ParallelCache", "yes");
                }

                // Build up the list of target codes from all the MSPs in the chain.
                List<WixBundlePatchTargetCodeRow> targetCodes = new List<WixBundlePatchTargetCodeRow>();

                foreach (ChainPackageInfo package in chain.Packages)
                {
                    writer.WriteStartElement(String.Format(CultureInfo.InvariantCulture, "{0}Package", package.ChainPackageType));

                    writer.WriteAttributeString("Id", package.Id);

                    switch (package.Cache)
                    {
                        case YesNoAlwaysType.No:
                            writer.WriteAttributeString("Cache", "no");
                            break;
                        case YesNoAlwaysType.Yes:
                            writer.WriteAttributeString("Cache", "yes");
                            break;
                        case YesNoAlwaysType.Always:
                            writer.WriteAttributeString("Cache", "always");
                            break;
                    }

                    writer.WriteAttributeString("CacheId", package.CacheId);
                    writer.WriteAttributeString("InstallSize", Convert.ToString(package.InstallSize));
                    writer.WriteAttributeString("Size", Convert.ToString(package.Size));
                    writer.WriteAttributeString("PerMachine", YesNoDefaultType.Yes == package.PerMachine ? "yes" : "no");
                    writer.WriteAttributeString("Permanent", package.Permanent ? "yes" : "no");
                    writer.WriteAttributeString("Vital", package.Vital ? "yes" : "no");

                    if (null != package.RollbackBoundary)
                    {
                        writer.WriteAttributeString("RollbackBoundaryForward", package.RollbackBoundary.Id);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackBoundaryBackwardId))
                    {
                        writer.WriteAttributeString("RollbackBoundaryBackward", package.RollbackBoundaryBackwardId);
                    }

                    if (!String.IsNullOrEmpty(package.LogPathVariable))
                    {
                        writer.WriteAttributeString("LogPathVariable", package.LogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.RollbackLogPathVariable))
                    {
                        writer.WriteAttributeString("RollbackLogPathVariable", package.RollbackLogPathVariable);
                    }

                    if (!String.IsNullOrEmpty(package.InstallCondition))
                    {
                        writer.WriteAttributeString("InstallCondition", package.InstallCondition);
                    }

                    if (Compiler.ChainPackageType.Exe == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("InstallArguments", package.InstallCommand);
                        writer.WriteAttributeString("UninstallArguments", package.UninstallCommand);
                        writer.WriteAttributeString("RepairArguments", package.RepairCommand);
                        writer.WriteAttributeString("Repairable", package.Repairable ? "yes" : "no");
                        if (!String.IsNullOrEmpty(package.Protocol))
                        {
                            writer.WriteAttributeString("Protocol", package.Protocol);
                        }
                    }
                    else if (Compiler.ChainPackageType.Msi == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("ProductCode", package.ProductCode);
                        writer.WriteAttributeString("Language", package.Language);
                        writer.WriteAttributeString("Version", package.Version);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");
                    }
                    else if (Compiler.ChainPackageType.Msp == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("PatchCode", package.PatchCode);
                        writer.WriteAttributeString("PatchXml", package.PatchXml);
                        writer.WriteAttributeString("DisplayInternalUI", package.DisplayInternalUI ? "yes" : "no");

                        // If there is still a chance that all of our patches will target a narrow set of
                        // product codes, add the patch list to the overall list.
                        if (null != targetCodes)
                        {
                            if (!package.TargetUnspecified)
                            {
                                targetCodes.AddRange(package.TargetCodes);
                            }
                            else // we have a patch that targets the world, so throw the whole list away.
                            {
                                targetCodes = null;
                            }
                        }
                    }
                    else if (Compiler.ChainPackageType.Msu == package.ChainPackageType)
                    {
                        writer.WriteAttributeString("DetectCondition", package.DetectCondition);
                        writer.WriteAttributeString("KB", package.MsuKB);
                    }

                    foreach (MsiFeature feature in package.MsiFeatures)
                    {
                        writer.WriteStartElement("MsiFeature");
                        writer.WriteAttributeString("Id", feature.Name);
                        writer.WriteEndElement();
                    }

                    foreach (MsiPropertyInfo msiProperty in package.MsiProperties)
                    {
                        writer.WriteStartElement("MsiProperty");
                        writer.WriteAttributeString("Id", msiProperty.Name);
                        writer.WriteAttributeString("Value", msiProperty.Value);
                        writer.WriteEndElement();
                    }

                    foreach (string slipstreamMsp in package.SlipstreamMsps)
                    {
                        writer.WriteStartElement("SlipstreamMsp");
                        writer.WriteAttributeString("Id", slipstreamMsp);
                        writer.WriteEndElement();
                    }

                    foreach (ExitCodeInfo exitCode in package.ExitCodes)
                    {
                        writer.WriteStartElement("ExitCode");
                        writer.WriteAttributeString("Type", exitCode.Type);
                        writer.WriteAttributeString("Code", exitCode.Code);
                        writer.WriteEndElement();
                    }

                    if (commandLinesByPackage.ContainsKey(package.Id))
                    {
                        foreach (WixCommandLineRow commandLine in commandLinesByPackage[package.Id])
                        {
                            writer.WriteStartElement("CommandLine");
                            writer.WriteAttributeString("InstallArgument", commandLine.InstallArgument);
                            writer.WriteAttributeString("UninstallArgument", commandLine.UninstallArgument);
                            writer.WriteAttributeString("RepairArgument", commandLine.RepairArgument);
                            writer.WriteAttributeString("Condition", commandLine.Condition);
                            writer.WriteEndElement();
                        }
                    }

                    // Output the dependency information.
                    foreach (ProvidesDependency dependency in package.Provides)
                    {
                        // TODO: Add to wixpdb as an imported table, or link package wixpdbs to bundle wixpdbs.
                        dependency.WriteXml(writer);
                    }

                    foreach (RelatedPackage related in package.RelatedPackages)
                    {
                        writer.WriteStartElement("RelatedPackage");
                        writer.WriteAttributeString("Id", related.Id);
                        if (!String.IsNullOrEmpty(related.MinVersion))
                        {
                            writer.WriteAttributeString("MinVersion", related.MinVersion);
                            writer.WriteAttributeString("MinInclusive", related.MinInclusive ? "yes" : "no");
                        }
                        if (!String.IsNullOrEmpty(related.MaxVersion))
                        {
                            writer.WriteAttributeString("MaxVersion", related.MaxVersion);
                            writer.WriteAttributeString("MaxInclusive", related.MaxInclusive ? "yes" : "no");
                        }
                        writer.WriteAttributeString("OnlyDetect", related.OnlyDetect ? "yes" : "no");
                        if (0 < related.Languages.Count)
                        {
                            writer.WriteAttributeString("LangInclusive", related.LangInclusive ? "yes" : "no");
                            foreach (string language in related.Languages)
                            {
                                writer.WriteStartElement("Language");
                                writer.WriteAttributeString("Id", language);
                                writer.WriteEndElement();
                            }
                        }
                        writer.WriteEndElement();
                    }

                    // Write any contained Payloads with the PackagePayload being first
                    writer.WriteStartElement("PayloadRef");
                    writer.WriteAttributeString("Id", package.PackagePayload.Id);
                    writer.WriteEndElement();

                    foreach (PayloadInfoRow payload in package.Payloads)
                    {
                        if (payload.Id != package.PackagePayload.Id)
                        {
                            writer.WriteStartElement("PayloadRef");
                            writer.WriteAttributeString("Id", payload.Id);
                            writer.WriteEndElement();
                        }
                    }

                    writer.WriteEndElement(); // </XxxPackage>
                }
                writer.WriteEndElement(); // </Chain>

                if (null != targetCodes)
                {
                    foreach (WixBundlePatchTargetCodeRow targetCode in targetCodes)
                    {
                        writer.WriteStartElement("PatchTargetCode");
                        writer.WriteAttributeString("TargetCode", targetCode.TargetCode);
                        writer.WriteAttributeString("Product", targetCode.TargetsProductCode ? "yes" : "no");
                        writer.WriteEndElement();
                    }
                }

                // write the ApprovedExeForElevation elements
                if (0 < approvedExesForElevation.Count)
                {
                    foreach (ApprovedExeForElevation approvedExeForElevation in approvedExesForElevation)
                    {
                        writer.WriteStartElement("ApprovedExeForElevation");
                        writer.WriteAttributeString("Id", approvedExeForElevation.Id);
                        writer.WriteAttributeString("Key", approvedExeForElevation.Key);

                        if (!String.IsNullOrEmpty(approvedExeForElevation.ValueName))
                        {
                            writer.WriteAttributeString("ValueName", approvedExeForElevation.ValueName);
                        }

                        if (approvedExeForElevation.Win64)
                        {
                            writer.WriteAttributeString("Win64", "yes");
                        }

                        writer.WriteEndElement();
                    }
                }

                writer.WriteEndDocument(); // </BurnManifest>
            }
        }

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleRow bundleInfo)
        {
            Microsoft.Deployment.Resources.ResourceCollection resources = new Microsoft.Deployment.Resources.ResourceCollection();
            Microsoft.Deployment.Resources.VersionResource version = new Microsoft.Deployment.Resources.VersionResource("#1", 1033);

            version.Load(bundleTempPath);
            resources.Add(version);

            // Ensure the bundle info provides a full four part version.
            Version fourPartVersion = new Version(bundleInfo.Version);
            int major = (fourPartVersion.Major < 0) ? 0 : fourPartVersion.Major;
            int minor = (fourPartVersion.Minor < 0) ? 0 : fourPartVersion.Minor;
            int build = (fourPartVersion.Build < 0) ? 0 : fourPartVersion.Build;
            int revision = (fourPartVersion.Revision < 0) ? 0 : fourPartVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                throw new WixException(WixErrors.InvalidModuleOrBundleVersion(bundleInfo.SourceLineNumbers, "Bundle", bundleInfo.Version));
            }

            fourPartVersion = new Version(major, minor, build, revision);
            version.FileVersion = fourPartVersion;
            version.ProductVersion = fourPartVersion;

            Microsoft.Deployment.Resources.VersionStringTable strings = version[1033];
            strings["LegalCopyright"] = bundleInfo.Copyright;
            strings["OriginalFilename"] = Path.GetFileName(outputPath);
            strings["FileVersion"] = bundleInfo.Version;    // string versions do not have to be four parts.
            strings["ProductVersion"] = bundleInfo.Version; // string versions do not have to be four parts.

            if (!String.IsNullOrEmpty(bundleInfo.Name))
            {
                strings["ProductName"] = bundleInfo.Name;
                strings["FileDescription"] = bundleInfo.Name;
            }

            if (!String.IsNullOrEmpty(bundleInfo.Publisher))
            {
                strings["CompanyName"] = bundleInfo.Publisher;
            }
            else
            {
                strings["CompanyName"] = String.Empty;
            }

            if (!String.IsNullOrEmpty(bundleInfo.IconPath))
            {
                Deployment.Resources.GroupIconResource iconGroup = new Deployment.Resources.GroupIconResource("#1", 1033);
                iconGroup.ReadFromFile(bundleInfo.IconPath);
                resources.Add(iconGroup);

                foreach (Deployment.Resources.Resource icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            if (!String.IsNullOrEmpty(bundleInfo.SplashScreenBitmapPath))
            {
                Deployment.Resources.BitmapResource bitmap = new Deployment.Resources.BitmapResource("#1", 1033);
                bitmap.ReadFromFile(bundleInfo.SplashScreenBitmapPath);
                resources.Add(bitmap);
            }

            resources.Save(bundleTempPath);
        }

        private void WriteBurnManifestContainerAttributes(XmlTextWriter writer, string executableName, ContainerInfo container, int containerIndex, BinderFileManager fileManager)
        {
            writer.WriteAttributeString("Id", container.Id);
            writer.WriteAttributeString("FileSize", container.FileInfo.Length.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", Common.GetFileHash(container.FileInfo));
            if (container.Type == "detached")
            {
                string resolvedUrl = fileManager.ResolveUrl(container.DownloadUrl, null, null, container.Id, container.Name);
                if (!String.IsNullOrEmpty(resolvedUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                }
                else if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    writer.WriteAttributeString("DownloadUrl", container.DownloadUrl);
                }

                writer.WriteAttributeString("FilePath", container.Name);
            }
            else if (container.Type == "attached")
            {
                if (!String.IsNullOrEmpty(container.DownloadUrl))
                {
                    this.core.OnMessage(WixWarnings.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id));
                }

                writer.WriteAttributeString("FilePath", executableName); // attached containers use the name of the bundle since they are attached to the executable.
                writer.WriteAttributeString("AttachedIndex", containerIndex.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Attached", "yes");
                writer.WriteAttributeString("Primary", "yes");
            }
        }

        private void WriteBurnManifestPayloadAttributes(XmlTextWriter writer, PayloadInfoRow payload, bool embeddedOnly, BinderFileManager fileManager, Dictionary<string, PayloadInfoRow> allPayloads)
        {
            Debug.Assert(!embeddedOnly || PackagingType.Embedded == payload.Packaging);

            writer.WriteAttributeString("Id", payload.Id);
            writer.WriteAttributeString("FilePath", payload.Name);
            writer.WriteAttributeString("FileSize", payload.FileSize.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Hash", payload.Hash);

            if (payload.LayoutOnly)
            {
                writer.WriteAttributeString("LayoutOnly", "yes");
            }

            if (!String.IsNullOrEmpty(payload.PublicKey))
            {
                writer.WriteAttributeString("CertificateRootPublicKeyIdentifier", payload.PublicKey);
            }

            if (!String.IsNullOrEmpty(payload.Thumbprint))
            {
                writer.WriteAttributeString("CertificateRootThumbprint", payload.Thumbprint);
            }

            switch (payload.Packaging)
            {
                case PackagingType.Embedded: // this means it's in a container.
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        this.core.OnMessage(WixWarnings.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id));
                    }

                    writer.WriteAttributeString("Packaging", "embedded");
                    writer.WriteAttributeString("SourcePath", payload.EmbeddedId);

                    if (Compiler.BurnUXContainerId != payload.Container)
                    {
                        writer.WriteAttributeString("Container", payload.Container);
                    }
                    break;

                case PackagingType.External:
                    string packageId = payload.ParentPackagePayload;
                    string parentUrl = payload.ParentPackagePayload == null ? null : allPayloads[payload.ParentPackagePayload].DownloadUrl;
                    string resolvedUrl = fileManager.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id, payload.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", resolvedUrl);
                    }
                    else if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        writer.WriteAttributeString("DownloadUrl", payload.DownloadUrl);
                    }

                    writer.WriteAttributeString("Packaging", "external");
                    writer.WriteAttributeString("SourcePath", payload.Name);
                    break;
            }

            if (!String.IsNullOrEmpty(payload.CatalogId))
            {
                writer.WriteAttributeString("Catalog", payload.CatalogId);
            }
        }

        private void VerifyPayloadWithCatalog(PayloadInfoRow payloadInfo, Dictionary<string, CatalogInfo> catalogs)
        {
            bool validated = false;

            foreach (CatalogInfo catalog in catalogs.Values)
            {
                if (!validated)
                {
                    // Get the file hash
                    uint cryptHashSize = 20;
                    byte[] cryptHashBytes = new byte[cryptHashSize];
                    int error;
                    IntPtr fileHandle = IntPtr.Zero;
                    using (FileStream payloadStream = File.OpenRead(payloadInfo.FullFileName))
                    {
                        // Get the file handle
                        fileHandle = payloadStream.SafeFileHandle.DangerousGetHandle();

                        // 20 bytes is usually the hash size.  Future hashes may be bigger
                        if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(
                            fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                        {
                            error = Marshal.GetLastWin32Error();
                            if (VerifyInterop.ErrorInsufficientBuffer == error)
                            {
                                error = 0;
                                cryptHashBytes = new byte[cryptHashSize];
                                if (!VerifyInterop.CryptCATAdminCalcHashFromFileHandle(
                                    fileHandle, ref cryptHashSize, cryptHashBytes, 0))
                                {
                                    error = Marshal.GetLastWin32Error();
                                }
                            }
                            if (0 != error)
                            {
                                this.core.OnMessage(WixErrors.CatalogFileHashFailed(payloadInfo.FullFileName, error));
                            }
                        }
                    }

                    VerifyInterop.WinTrustCatalogInfo catalogData = new VerifyInterop.WinTrustCatalogInfo();
                    VerifyInterop.WinTrustData trustData = new VerifyInterop.WinTrustData();
                    try
                    {
                        // Create WINTRUST_CATALOG_INFO structure
                        catalogData.cbStruct = (uint)Marshal.SizeOf(catalogData);
                        catalogData.cbCalculatedFileHash = cryptHashSize;
                        catalogData.pbCalculatedFileHash = Marshal.AllocCoTaskMem((int)cryptHashSize);
                        Marshal.Copy(cryptHashBytes, 0, catalogData.pbCalculatedFileHash, (int)cryptHashSize);

                        StringBuilder hashString = new StringBuilder();
                        foreach (byte hashByte in cryptHashBytes)
                        {
                            hashString.Append(hashByte.ToString("X2"));
                        }
                        catalogData.pcwszMemberTag = hashString.ToString();

                        // The file names need to be lower case for older OSes
                        catalogData.pcwszMemberFilePath = payloadInfo.FullFileName.ToLowerInvariant();
                        catalogData.pcwszCatalogFilePath = catalog.FileInfo.FullName.ToLowerInvariant();

                        // Create WINTRUST_DATA structure
                        trustData.cbStruct = (uint)Marshal.SizeOf(trustData);
                        trustData.dwUIChoice = VerifyInterop.WTD_UI_NONE;
                        trustData.fdwRevocationChecks = VerifyInterop.WTD_REVOKE_NONE;
                        trustData.dwUnionChoice = VerifyInterop.WTD_CHOICE_CATALOG;
                        trustData.dwStateAction = VerifyInterop.WTD_STATEACTION_VERIFY;
                        trustData.dwProvFlags = VerifyInterop.WTD_REVOCATION_CHECK_NONE;

                        // Create the structure pointers for unmanaged
                        trustData.pCatalog = Marshal.AllocCoTaskMem(Marshal.SizeOf(catalogData));
                        Marshal.StructureToPtr(catalogData, trustData.pCatalog, false);

                        // Call WinTrustVerify to validate the file with the catalog
                        IntPtr noWindow = new IntPtr(-1);
                        Guid verifyGuid = new Guid(VerifyInterop.GenericVerify2);
                        long verifyResult = VerifyInterop.WinVerifyTrust(noWindow, ref verifyGuid, ref trustData);
                        if (0 == verifyResult)
                        {
                            validated = true;
                            payloadInfo.CatalogId = catalog.Id;
                        }
                    }
                    finally
                    {
                        // Free the structure memory
                        if (IntPtr.Zero != trustData.pCatalog)
                        {
                            Marshal.FreeCoTaskMem(trustData.pCatalog);
                        }

                        if (IntPtr.Zero != catalogData.pbCalculatedFileHash)
                        {
                            Marshal.FreeCoTaskMem(catalogData.pbCalculatedFileHash);
                        }
                    }
                }
            }

            // Error message if the file was not validated by one of the catalogs
            if (!validated)
            {
                this.core.OnMessage(WixErrors.CatalogVerificationFailed(payloadInfo.FullFileName));
            }
        }

        /// <summary>
        /// Binds a transform.
        /// </summary>
        /// <param name="transform">The transform to bind.</param>
        /// <param name="transformFile">The transform to create.</param>
        /// <returns>true if binding completed successfully; false otherwise</returns>
        private bool BindTransform(Output transform, string transformFile)
        {
            foreach (BinderExtension extension in this.extensions)
            {
                extension.TransformInitialize(transform);
            }

            int transformFlags = 0;

            Output targetOutput = new Output(null);
            Output updatedOutput = new Output(null);

            // TODO: handle added columns

            // to generate a localized transform, both the target and updated
            // databases need to have the same code page. the only reason to
            // set different code pages is to support localized primary key
            // columns, but that would only support deleting rows. if this
            // becomes necessary, define a PreviousCodepage property on the
            // Output class and persist this throughout transform generation.
            targetOutput.Codepage = transform.Codepage;
            updatedOutput.Codepage = transform.Codepage;

            // remove certain Property rows which will be populated from summary information values
            string targetUpgradeCode = null;
            string updatedUpgradeCode = null;

            Table propertyTable = transform.Tables["Property"];
            if (null != propertyTable)
            {
                for (int i = propertyTable.Rows.Count - 1; i >= 0; i--)
                {
                    Row row = propertyTable.Rows[i];

                    if ("ProductCode" == (string)row[0] || "ProductLanguage" == (string)row[0] || "ProductVersion" == (string)row[0] || "UpgradeCode" == (string)row[0])
                    {
                        propertyTable.Rows.RemoveAt(i);

                        if ("UpgradeCode" == (string)row[0])
                        {
                            updatedUpgradeCode = (string)row[1];
                        }
                    }
                }
            }

            Table targetSummaryInfo = targetOutput.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
            Table updatedSummaryInfo = updatedOutput.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
            Table targetPropertyTable = targetOutput.EnsureTable(this.core.TableDefinitions["Property"]);
            Table updatedPropertyTable = updatedOutput.EnsureTable(this.core.TableDefinitions["Property"]);

            string targetProductCode = null;

            // process special summary information values
            foreach (Row row in transform.Tables["_SummaryInformation"].Rows)
            {
                if ((int)SummaryInformation.Transform.CodePage == (int)row[0])
                {
                    // convert from a web name if provided
                    string codePage = (string)row.Fields[1].Data;
                    if (null == codePage)
                    {
                        codePage = "0";
                    }
                    else
                    {
                        codePage = Common.GetValidCodePage(codePage).ToString(CultureInfo.InvariantCulture);
                    }

                    string previousCodePage = (string)row.Fields[1].PreviousData;
                    if (null == previousCodePage)
                    {
                        previousCodePage = "0";
                    }
                    else
                    {
                        previousCodePage = Common.GetValidCodePage(previousCodePage).ToString(CultureInfo.InvariantCulture);
                    }

                    Row targetCodePageRow = targetSummaryInfo.CreateRow(null);
                    targetCodePageRow[0] = 1; // PID_CODEPAGE
                    targetCodePageRow[1] = previousCodePage;

                    Row updatedCodePageRow = updatedSummaryInfo.CreateRow(null);
                    updatedCodePageRow[0] = 1; // PID_CODEPAGE
                    updatedCodePageRow[1] = codePage;
                }
                else if ((int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ||
                         (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                {
                    // the target language
                    string[] propertyData = ((string)row[1]).Split(';');
                    string lang = 2 == propertyData.Length ? propertyData[1] : "0";

                    Table tempSummaryInfo = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ? targetSummaryInfo : updatedSummaryInfo;
                    Table tempPropertyTable = (int)SummaryInformation.Transform.TargetPlatformAndLanguage == (int)row[0] ? targetPropertyTable : updatedPropertyTable;

                    Row productLanguageRow = tempPropertyTable.CreateRow(null);
                    productLanguageRow[0] = "ProductLanguage";
                    productLanguageRow[1] = lang;

                    // set the platform;language on the MSI to be generated
                    Row templateRow = tempSummaryInfo.CreateRow(null);
                    templateRow[0] = 7; // PID_TEMPLATE
                    templateRow[1] = (string)row[1];
                }
                else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                {
                    string[] propertyData = ((string)row[1]).Split(';');

                    Row targetProductCodeRow = targetPropertyTable.CreateRow(null);
                    targetProductCodeRow[0] = "ProductCode";
                    targetProductCodeRow[1] = propertyData[0].Substring(0, 38);
                    targetProductCode = propertyData[0].Substring(0, 38);

                    Row targetProductVersionRow = targetPropertyTable.CreateRow(null);
                    targetProductVersionRow[0] = "ProductVersion";
                    targetProductVersionRow[1] = propertyData[0].Substring(38);

                    Row updatedProductCodeRow = updatedPropertyTable.CreateRow(null);
                    updatedProductCodeRow[0] = "ProductCode";
                    updatedProductCodeRow[1] = propertyData[1].Substring(0, 38);

                    Row updatedProductVersionRow = updatedPropertyTable.CreateRow(null);
                    updatedProductVersionRow[0] = "ProductVersion";
                    updatedProductVersionRow[1] = propertyData[1].Substring(38);

                    // UpgradeCode is optional and may not exists in the target
                    // or upgraded databases, so do not include a null-valued
                    // UpgradeCode property.

                    targetUpgradeCode = propertyData[2];
                    if (!String.IsNullOrEmpty(targetUpgradeCode))
                    {
                        Row targetUpgradeCodeRow = targetPropertyTable.CreateRow(null);
                        targetUpgradeCodeRow[0] = "UpgradeCode";
                        targetUpgradeCodeRow[1] = targetUpgradeCode;

                        // If the target UpgradeCode is specified, an updated
                        // UpgradeCode is required.
                        if (String.IsNullOrEmpty(updatedUpgradeCode))
                        {
                            updatedUpgradeCode = targetUpgradeCode;
                        }
                    }

                    if (!String.IsNullOrEmpty(updatedUpgradeCode))
                    {
                        Row updatedUpgradeCodeRow = updatedPropertyTable.CreateRow(null);
                        updatedUpgradeCodeRow[0] = "UpgradeCode";
                        updatedUpgradeCodeRow[1] = updatedUpgradeCode;
                    }
                }
                else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                {
                    transformFlags = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                }
                else if ((int)SummaryInformation.Transform.Reserved11 == (int)row[0])
                {
                    // PID_LASTPRINTED should be null for transforms
                    row.Operation = RowOperation.None;
                }
                else
                {
                    // add everything else as is
                    Row targetRow = targetSummaryInfo.CreateRow(null);
                    targetRow[0] = row[0];
                    targetRow[1] = row[1];

                    Row updatedRow = updatedSummaryInfo.CreateRow(null);
                    updatedRow[0] = row[0];
                    updatedRow[1] = row[1];
                }
            }

            // Validate that both databases have an UpgradeCode if the
            // authoring transform will validate the UpgradeCode; otherwise,
            // MsiCreateTransformSummaryinfo() will fail with 1620.
            if (((int)TransformFlags.ValidateUpgradeCode & transformFlags) != 0 &&
                (String.IsNullOrEmpty(targetUpgradeCode) || String.IsNullOrEmpty(updatedUpgradeCode)))
            {
                this.core.OnMessage(WixErrors.BothUpgradeCodesRequired());
            }

            foreach (Table table in transform.Tables)
            {
                // Ignore unreal tables when building transforms except the _Stream table.
                // These tables are ignored when generating the database so there is no reason
                // to process them here.
                if (table.Definition.IsUnreal && "_Streams" != table.Name)
                {
                    continue;
                }

                // process table operations
                switch (table.Operation)
                {
                    case TableOperation.Add:
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                    case TableOperation.Drop:
                        targetOutput.EnsureTable(table.Definition);
                        continue;
                    default:
                        targetOutput.EnsureTable(table.Definition);
                        updatedOutput.EnsureTable(table.Definition);
                        break;
                }

                // process row operations
                foreach (Row row in table.Rows)
                {
                    switch (row.Operation)
                    {
                        case RowOperation.Add:
                            Table updatedTable = updatedOutput.EnsureTable(table.Definition);
                            updatedTable.Rows.Add(row);
                            continue;
                        case RowOperation.Delete:
                            Table targetTable = targetOutput.EnsureTable(table.Definition);
                            targetTable.Rows.Add(row);

                            // fill-in non-primary key values
                            foreach (Field field in row.Fields)
                            {
                                if (!field.Column.IsPrimaryKey)
                                {
                                    if (ColumnType.Number == field.Column.Type && !field.Column.IsLocalizable)
                                    {
                                        field.Data = field.Column.MinValue;
                                    }
                                    else if (ColumnType.Object == field.Column.Type)
                                    {
                                        if (null == this.emptyFile)
                                        {
                                            this.emptyFile = this.tempFiles.AddExtension("empty");
                                            using (FileStream fileStream = File.Create(this.emptyFile))
                                            {
                                            }
                                        }

                                        field.Data = emptyFile;
                                    }
                                    else
                                    {
                                        field.Data = "0";
                                    }
                                }
                            }
                            continue;
                    }

                    // Assure that the file table's sequence is populated
                    if ("File" == table.Name)
                    {
                        foreach (Row fileRow in table.Rows)
                        {
                            if (null == fileRow[7])
                            {
                                if (RowOperation.Add == fileRow.Operation)
                                {
                                    this.core.OnMessage(WixErrors.InvalidAddedFileRowWithoutSequence(fileRow.SourceLineNumbers, (string)fileRow[0]));
                                    break;
                                }

                                // Set to 1 to prevent invalid IDT file from being generated
                                fileRow[7] = 1;
                            }
                        }
                    }

                    // process modified and unmodified rows
                    bool modifiedRow = false;
                    Row targetRow = new Row(null, table.Definition);
                    Row updatedRow = row;
                    for (int i = 0; i < row.Fields.Length; i++)
                    {
                        Field updatedField = row.Fields[i];

                        if (updatedField.Modified)
                        {
                            // set a different value in the target row to ensure this value will be modified during transform generation
                            if (ColumnType.Number == updatedField.Column.Type && !updatedField.Column.IsLocalizable)
                            {
                                if (null == updatedField.Data || 1 != (int)updatedField.Data)
                                {
                                    targetRow[i] = 1;
                                }
                                else
                                {
                                    targetRow[i] = 2;
                                }
                            }
                            else if (ColumnType.Object == updatedField.Column.Type)
                            {
                                if (null == emptyFile)
                                {
                                    emptyFile = this.tempFiles.AddExtension("empty");
                                    using (FileStream fileStream = File.Create(emptyFile))
                                    {
                                    }
                                }

                                targetRow[i] = emptyFile;
                            }
                            else
                            {
                                if ("0" != (string)updatedField.Data)
                                {
                                    targetRow[i] = "0";
                                }
                                else
                                {
                                    targetRow[i] = "1";
                                }
                            }

                            modifiedRow = true;
                        }
                        else if (ColumnType.Object == updatedField.Column.Type)
                        {
                            ObjectField objectField = (ObjectField)updatedField;

                            // create an empty file for comparing against
                            if (null == objectField.PreviousData)
                            {
                                if (null == emptyFile)
                                {
                                    emptyFile = this.tempFiles.AddExtension("empty");
                                    using (FileStream fileStream = File.Create(emptyFile))
                                    {
                                    }
                                }

                                targetRow[i] = emptyFile;
                                modifiedRow = true;
                            }
                            else if (!this.FileManager.CompareFiles(objectField.PreviousData, (string)objectField.Data))
                            {
                                targetRow[i] = objectField.PreviousData;
                                modifiedRow = true;
                            }
                        }
                        else // unmodified
                        {
                            if (null != updatedField.Data)
                            {
                                targetRow[i] = updatedField.Data;
                            }
                        }
                    }

                    // modified rows and certain special rows go in the target and updated msi databases
                    if (modifiedRow ||
                        ("Property" == table.Name &&
                            ("ProductCode" == (string)row[0] ||
                            "ProductLanguage" == (string)row[0] ||
                            "ProductVersion" == (string)row[0] ||
                            "UpgradeCode" == (string)row[0])))
                    {
                        Table targetTable = targetOutput.EnsureTable(table.Definition);
                        targetTable.Rows.Add(targetRow);

                        Table updatedTable = updatedOutput.EnsureTable(table.Definition);
                        updatedTable.Rows.Add(updatedRow);
                    }
                }
            }

            foreach (BinderExtension extension in this.extensions)
            {
                extension.TransformFinalize(transform);
            }

            // Any errors encountered up to this point can cause errors during generation.
            if (this.core.EncounteredError)
            {
                return false;
            }

            string transformFileName = Path.GetFileNameWithoutExtension(transformFile);
            string targetDatabaseFile = Path.Combine(this.TempFilesLocation, String.Concat(transformFileName, "_target.msi"));
            string updatedDatabaseFile = Path.Combine(this.TempFilesLocation, String.Concat(transformFileName, "_updated.msi"));

            this.suppressAddingValidationRows = true;
            this.GenerateDatabase(targetOutput, targetDatabaseFile, false, true);
            this.GenerateDatabase(updatedOutput, updatedDatabaseFile, true, true);

            // make sure the directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(transformFile));

            // create the transform file
            using (Database targetDatabase = new Database(targetDatabaseFile, OpenDatabase.ReadOnly))
            {
                using (Database updatedDatabase = new Database(updatedDatabaseFile, OpenDatabase.ReadOnly))
                {
                    // Skip adding the patch transform if the product transform was empty.
                    // Patch transforms are usually not empty due to changes such as Media Table Row insertions.
                    if ((this.AllowEmptyTransforms && this.emptyTransformNames.Contains(transformFileName.Replace("#", ""))) || !updatedDatabase.GenerateTransform(targetDatabase, transformFile))
                    {
                        if (this.AllowEmptyTransforms)
                        {
                            // Only output the message once for the product transform.
                            if (!transformFileName.StartsWith("#"))
                            {
                                this.core.OnMessage(WixWarnings.NoDifferencesInTransform(transform.SourceLineNumbers));
                            }
                            this.emptyTransformNames.Add(transformFileName);
                            return false;
                        }
                        else
                        {
                            throw new WixException(WixErrors.NoDifferencesInTransform(transform.SourceLineNumbers));
                        }
                    }
                    else
                    {
                        if (targetProductCode != null && !this.nonEmptyProductCodes.Contains(targetProductCode))
                        {
                            this.nonEmptyProductCodes.Add(targetProductCode);
                        }
                        if (!this.nonEmptyTransformNames.Contains(":" + transformFileName))
                        {
                            this.nonEmptyTransformNames.Add(":" + transformFileName);
                        }
                        updatedDatabase.CreateTransformSummaryInfo(targetDatabase, transformFile, (TransformErrorConditions)(transformFlags & 0xFFFF), (TransformValidations)((transformFlags >> 16) & 0xFFFF));
                    }
                }
            }

            return !this.core.EncounteredError;
        }

        /// <summary>
        /// Retrieve files and their information from merge modules.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        private void ProcessMergeModules(Output output, FileRowCollection fileRows)
        {
            Table wixMergeTable = output.Tables["WixMerge"];
            if (null != wixMergeTable)
            {
                IMsmMerge2 merge = NativeMethods.GetMsmMerge();

                // Get the output's minimum installer version
                int outputInstallerVersion = int.MinValue;
                Table summaryInformationTable = output.Tables["_SummaryInformation"];
                if (null != summaryInformationTable)
                {
                    foreach (Row row in summaryInformationTable.Rows)
                    {
                        if (14 == (int)row[0])
                        {
                            outputInstallerVersion = Convert.ToInt32(row[1], CultureInfo.InvariantCulture);
                            break;
                        }
                    }
                }

                foreach (Row row in wixMergeTable.Rows)
                {
                    bool containsFiles = false;
                    WixMergeRow wixMergeRow = (WixMergeRow)row;

                    try
                    {
                        // read the module's File table to get its FileMediaInformation entries and gather any other information needed from the module.
                        using (Database db = new Database(wixMergeRow.SourceFile, OpenDatabase.ReadOnly))
                        {
                            if (db.TableExists("File") && db.TableExists("Component"))
                            {
                                Hashtable uniqueModuleFileIdentifiers = System.Collections.Specialized.CollectionsUtil.CreateCaseInsensitiveHashtable();

                                using (View view = db.OpenExecuteView("SELECT `File`, `Directory_` FROM `File`, `Component` WHERE `Component_`=`Component`"))
                                {
                                    // add each file row from the merge module into the file row collection (check for errors along the way)
                                    while (true)
                                    {
                                        using (Record record = view.Fetch())
                                        {
                                            if (null == record)
                                            {
                                                break;
                                            }

                                            // NOTE: this is very tricky - the merge module file rows are not added to the
                                            // file table because they should not be created via idt import.  Instead, these
                                            // rows are created by merging in the actual modules
                                            FileRow fileRow = new FileRow(null, this.core.TableDefinitions["File"]);
                                            fileRow.File = record[1];
                                            fileRow.Compressed = wixMergeRow.FileCompression;
                                            fileRow.Directory = record[2];
                                            fileRow.DiskId = wixMergeRow.DiskId;
                                            fileRow.FromModule = true;
                                            fileRow.PatchGroup = -1;
                                            fileRow.Source = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", wixMergeRow.Number.ToString(CultureInfo.InvariantCulture.NumberFormat), Path.DirectorySeparatorChar, record[1]);

                                            FileRow collidingFileRow = fileRows[fileRow.File];
                                            FileRow collidingModuleFileRow = (FileRow)uniqueModuleFileIdentifiers[fileRow.File];

                                            if (null == collidingFileRow && null == collidingModuleFileRow)
                                            {
                                                fileRows.Add(fileRow);

                                                // keep track of file identifiers in this merge module
                                                uniqueModuleFileIdentifiers.Add(fileRow.File, fileRow);
                                            }
                                            else // collision(s) detected
                                            {
                                                // case-sensitive collision with another merge module or a user-authored file identifier
                                                if (null != collidingFileRow)
                                                {
                                                    this.core.OnMessage(WixErrors.DuplicateModuleFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, collidingFileRow.File));
                                                }

                                                // case-insensitive collision with another file identifier in the same merge module
                                                if (null != collidingModuleFileRow)
                                                {
                                                    this.core.OnMessage(WixErrors.DuplicateModuleCaseInsensitiveFileIdentifier(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, fileRow.File, collidingModuleFileRow.File));
                                                }
                                            }

                                            containsFiles = true;
                                        }
                                    }
                                }
                            }

                            // Get the summary information to detect the Schema
                            using (SummaryInformation summaryInformation = new SummaryInformation(db))
                            {
                                string moduleInstallerVersionString = summaryInformation.GetProperty(14);

                                try
                                {
                                    int moduleInstallerVersion = Convert.ToInt32(moduleInstallerVersionString, CultureInfo.InvariantCulture);
                                    if (moduleInstallerVersion > outputInstallerVersion)
                                    {
                                        this.core.OnMessage(WixWarnings.InvalidHigherInstallerVersionInModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleInstallerVersion, outputInstallerVersion));
                                    }
                                }
                                catch (FormatException)
                                {
                                    throw new WixException(WixErrors.MissingOrInvalidModuleInstallerVersion(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile, moduleInstallerVersionString));
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        throw new WixException(WixErrors.FileNotFound(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                    }
                    catch (Win32Exception)
                    {
                        throw new WixException(WixErrors.CannotOpenMergeModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.SourceFile));
                    }

                    // if the module has files and creating layout
                    if (containsFiles && !this.suppressLayout)
                    {
                        bool moduleOpen = false;
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (System.FormatException)
                        {
                            this.core.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        try
                        {
                            merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                            moduleOpen = true;

                            string safeMergeId = wixMergeRow.Number.ToString(CultureInfo.InvariantCulture.NumberFormat);

                            // extract the module cabinet, then explode all of the files to a temp directory
                            string moduleCabPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, safeMergeId, ".module.cab");
                            merge.ExtractCAB(moduleCabPath);

                            string mergeIdPath = String.Concat(this.TempFilesLocation, Path.DirectorySeparatorChar, "MergeId.", safeMergeId);
                            Directory.CreateDirectory(mergeIdPath);

                            using (WixExtractCab extractCab = new WixExtractCab())
                            {
                                try
                                {
                                    extractCab.Extract(moduleCabPath, mergeIdPath);
                                }
                                catch (FileNotFoundException)
                                {
                                    throw new WixException(WixErrors.CabFileDoesNotExist(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                                }
                                catch
                                {
                                    throw new WixException(WixErrors.CabExtractionFailed(moduleCabPath, wixMergeRow.SourceFile, mergeIdPath));
                                }
                            }
                        }
                        catch (COMException ce)
                        {
                            throw new WixException(WixErrors.UnableToOpenModule(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile, ce.Message));
                        }
                        finally
                        {
                            if (moduleOpen)
                            {
                                merge.CloseModule();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set the guids for components with generatable guids.
        /// </summary>
        /// <param name="output">Internal representation of the database to operate on.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way autogenerated GUIDs are generated. Furthermore, there is no security hole here, as the strings won't need to " +
                         "make a round trip")]
        private void SetComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Hashtable registryKeyRows = null;
                Hashtable directories = null;
                Hashtable componentIdGenSeeds = null;
                Dictionary<string, List<FileRow>> fileRows = null;

                // find components with generatable guids
                foreach (ComponentRow componentRow in componentTable.Rows)
                {
                    // component guid will be generated
                    if ("*" == componentRow.Guid)
                    {
                        if (null == componentRow.KeyPath || componentRow.IsOdbcDataSourceKeyPath)
                        {
                            this.core.OnMessage(WixErrors.IllegalComponentWithAutoGeneratedGuid(componentRow.SourceLineNumbers));
                        }
                        else if (componentRow.IsRegistryKeyPath)
                        {
                            if (null == registryKeyRows)
                            {
                                Table registryTable = output.Tables["Registry"];

                                registryKeyRows = new Hashtable(registryTable.Rows.Count);

                                foreach (Row registryRow in registryTable.Rows)
                                {
                                    registryKeyRows.Add((string)registryRow[0], registryRow);
                                }
                            }

                            Row foundRow = registryKeyRows[componentRow.KeyPath] as Row;

                            string bitness = String.Empty;
                            if (!this.backwardsCompatibleGuidGen && componentRow.Is64Bit)
                            {
                                bitness = "64";
                            }

                            if (null != foundRow)
                            {
                                string regkey = String.Concat(bitness, foundRow[1], "\\", foundRow[2], "\\", foundRow[3]);
                                componentRow.Guid = Uuid.NewUuid(Binder.WixComponentGuidNamespace, regkey.ToLower(CultureInfo.InvariantCulture), this.backwardsCompatibleGuidGen).ToString("B").ToUpper(CultureInfo.InvariantCulture);
                            }
                        }
                        else // must be a File KeyPath
                        {
                            // if the directory table hasn't been loaded into an indexed hash
                            // of directory ids to target names do that now.
                            if (null == directories)
                            {
                                Table directoryTable = output.Tables["Directory"];

                                int numDirectoryTableRows = (null != directoryTable) ? directoryTable.Rows.Count : 0;

                                directories = new Hashtable(numDirectoryTableRows);

                                // get the target paths for all directories
                                if (null != directoryTable)
                                {
                                    foreach (Row row in directoryTable.Rows)
                                    {
                                        // if the directory Id already exists, we will skip it here since
                                        // checking for duplicate primary keys is done later when importing tables
                                        // into database
                                        if (directories.ContainsKey(row[0]))
                                        {
                                            continue;
                                        }

                                        string targetName = Installer.GetName((string)row[2], false, true);
                                        directories.Add(row[0], new ResolvedDirectory((string)row[1], targetName));
                                    }
                                }
                            }

                            // if the component id generation seeds have not been indexed
                            // from the WixDirectory table do that now.
                            if (null == componentIdGenSeeds)
                            {
                                Table wixDirectoryTable = output.Tables["WixDirectory"];

                                int numWixDirectoryRows = (null != wixDirectoryTable) ? wixDirectoryTable.Rows.Count : 0;

                                componentIdGenSeeds = new Hashtable(numWixDirectoryRows);

                                // if there are any WixDirectory rows, build up the Component Guid
                                // generation seeds indexed by Directory/@Id.
                                if (null != wixDirectoryTable)
                                {
                                    foreach (Row row in wixDirectoryTable.Rows)
                                    {
                                        componentIdGenSeeds.Add(row[0], (string)row[1]);
                                    }
                                }
                            }

                            // if the file rows have not been indexed by File.Component yet
                            // then do that now
                            if (null == fileRows)
                            {
                                Table fileTable = output.Tables["File"];

                                int numFileRows = (null != fileTable) ? fileTable.Rows.Count : 0;

                                fileRows = new Dictionary<string, List<FileRow>>(numFileRows);

                                if (null != fileTable)
                                {
                                    foreach (FileRow file in fileTable.Rows)
                                    {
                                        List<FileRow> files;
                                        if (!fileRows.TryGetValue(file.Component, out files))
                                        {
                                            files = new List<FileRow>();
                                            fileRows.Add(file.Component, files);
                                        }

                                        files.Add(file);
                                    }
                                }
                            }

                            // validate component meets all the conditions to have a generated guid
                            List<FileRow> currentComponentFiles = fileRows[componentRow.Component];
                            int numFilesInComponent = currentComponentFiles.Count;
                            string path = null;

                            foreach (FileRow fileRow in currentComponentFiles)
                            {
                                if (fileRow.File == componentRow.KeyPath)
                                {
                                    // calculate the key file's canonical target path
                                    string directoryPath = GetDirectoryPath(directories, componentIdGenSeeds, componentRow.Directory, true);
                                    string fileName = Installer.GetName(fileRow.FileName, false, true).ToLower(CultureInfo.InvariantCulture);
                                    path = Path.Combine(directoryPath, fileName);

                                    // find paths that are not canonicalized
                                    if (path.StartsWith(@"PersonalFolder\my pictures", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramFilesFolder\common files", StringComparison.Ordinal) ||
                                        path.StartsWith(@"ProgramMenuFolder\startup", StringComparison.Ordinal) ||
                                        path.StartsWith("TARGETDIR", StringComparison.Ordinal) ||
                                        path.StartsWith(@"StartMenuFolder\programs", StringComparison.Ordinal) ||
                                        path.StartsWith(@"WindowsFolder\fonts", StringComparison.Ordinal))
                                    {
                                        this.core.OnMessage(WixErrors.IllegalPathForGeneratedComponentGuid(componentRow.SourceLineNumbers, fileRow.Component, path));
                                    }

                                    // if component has more than one file, the key path must be versioned
                                    if (1 < numFilesInComponent && String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        this.core.OnMessage(WixErrors.IllegalGeneratedGuidComponentUnversionedKeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                                else
                                {
                                    // not a key path, so it must be an unversioned file if component has more than one file
                                    if (1 < numFilesInComponent && !String.IsNullOrEmpty(fileRow.Version))
                                    {
                                        this.core.OnMessage(WixErrors.IllegalGeneratedGuidComponentVersionedNonkeypath(componentRow.SourceLineNumbers));
                                    }
                                }
                            }

                            // if the rules were followed, reward with a generated guid
                            if (!this.core.EncounteredError)
                            {
                                componentRow.Guid = Uuid.NewUuid(Binder.WixComponentGuidNamespace, path, this.backwardsCompatibleGuidGen).ToString("B").ToUpper(CultureInfo.InvariantCulture);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates instance transform substorages in the output.
        /// </summary>
        /// <param name="output">Output containing instance transform definitions.</param>
        private void CreateInstanceTransforms(Output output)
        {
            // Create and add substorages for instance transforms.
            Table wixInstanceTransformsTable = output.Tables["WixInstanceTransforms"];
            if (null != wixInstanceTransformsTable && 0 <= wixInstanceTransformsTable.Rows.Count)
            {
                string targetProductCode = null;
                string targetUpgradeCode = null;
                string targetProductVersion = null;

                Table targetSummaryInformationTable = output.Tables["_SummaryInformation"];
                Table targetPropertyTable = output.Tables["Property"];

                // Get the data from target database
                foreach (Row propertyRow in targetPropertyTable.Rows)
                {
                    if ("ProductCode" == (string)propertyRow[0])
                    {
                        targetProductCode = (string)propertyRow[1];
                    }
                    else if ("ProductVersion" == (string)propertyRow[0])
                    {
                        targetProductVersion = (string)propertyRow[1];
                    }
                    else if ("UpgradeCode" == (string)propertyRow[0])
                    {
                        targetUpgradeCode = (string)propertyRow[1];
                    }
                }

                // Index the Instance Component Rows.
                Dictionary<string, ComponentRow> instanceComponentGuids = new Dictionary<string, ComponentRow>();
                Table targetInstanceComponentTable = output.Tables["WixInstanceComponent"];
                if (null != targetInstanceComponentTable && 0 < targetInstanceComponentTable.Rows.Count)
                {
                    foreach (Row row in targetInstanceComponentTable.Rows)
                    {
                        // Build up all the instances, we'll get the Components rows from the real Component table.
                        instanceComponentGuids.Add((string)row[0], null);
                    }

                    Table targetComponentTable = output.Tables["Component"];
                    foreach (ComponentRow componentRow in targetComponentTable.Rows)
                    {
                        string component = (string)componentRow[0];
                        if (instanceComponentGuids.ContainsKey(component))
                        {
                            instanceComponentGuids[component] = componentRow;
                        }
                    }
                }

                // Generate the instance transforms
                foreach (Row instanceRow in wixInstanceTransformsTable.Rows)
                {
                    string instanceId = (string)instanceRow[0];

                    Output instanceTransform = new Output(instanceRow.SourceLineNumbers);
                    instanceTransform.Type = OutputType.Transform;
                    instanceTransform.Codepage = output.Codepage;

                    Table instanceSummaryInformationTable = instanceTransform.EnsureTable(this.core.TableDefinitions["_SummaryInformation"]);
                    string targetPlatformAndLanguage = null;

                    foreach (Row summaryInformationRow in targetSummaryInformationTable.Rows)
                    {
                        if (7 == (int)summaryInformationRow[0]) // PID_TEMPLATE
                        {
                            targetPlatformAndLanguage = (string)summaryInformationRow[1];
                        }

                        // Copy the row's data to the transform.
                        Row copyOfSummaryRow = instanceSummaryInformationTable.CreateRow(null);
                        copyOfSummaryRow[0] = summaryInformationRow[0];
                        copyOfSummaryRow[1] = summaryInformationRow[1];
                    }

                    // Modify the appropriate properties.
                    Table propertyTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Property"]);

                    // Change the ProductCode property
                    string productCode = (string)instanceRow[2];
                    if ("*" == productCode)
                    {
                        productCode = Common.GenerateGuid();
                    }

                    Row productCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    productCodeRow.Operation = RowOperation.Modify;
                    productCodeRow.Fields[1].Modified = true;
                    productCodeRow[0] = "ProductCode";
                    productCodeRow[1] = productCode;

                    // Change the instance property
                    Row instanceIdRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                    instanceIdRow.Operation = RowOperation.Modify;
                    instanceIdRow.Fields[1].Modified = true;
                    instanceIdRow[0] = (string)instanceRow[1];
                    instanceIdRow[1] = instanceId;

                    if (null != instanceRow[3])
                    {
                        // Change the ProductName property
                        Row productNameRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        productNameRow.Operation = RowOperation.Modify;
                        productNameRow.Fields[1].Modified = true;
                        productNameRow[0] = "ProductName";
                        productNameRow[1] = (string)instanceRow[3];
                    }

                    if (null != instanceRow[4])
                    {
                        // Change the UpgradeCode property
                        Row upgradeCodeRow = propertyTable.CreateRow(instanceRow.SourceLineNumbers);
                        upgradeCodeRow.Operation = RowOperation.Modify;
                        upgradeCodeRow.Fields[1].Modified = true;
                        upgradeCodeRow[0] = "UpgradeCode";
                        upgradeCodeRow[1] = instanceRow[4];

                        // Change the Upgrade table
                        Table targetUpgradeTable = output.Tables["Upgrade"];
                        if (null != targetUpgradeTable && 0 <= targetUpgradeTable.Rows.Count)
                        {
                            string upgradeId = (string)instanceRow[4];
                            Table upgradeTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Upgrade"]);
                            foreach (Row row in targetUpgradeTable.Rows)
                            {
                                // In case they are upgrading other codes to this new product, leave the ones that don't match the
                                // Product.UpgradeCode intact.
                                if (targetUpgradeCode == (string)row[0])
                                {
                                    Row upgradeRow = upgradeTable.CreateRow(null);
                                    upgradeRow.Operation = RowOperation.Add;
                                    upgradeRow.Fields[0].Modified = true;
                                    // I was hoping to be able to RowOperation.Modify, but that didn't appear to function.
                                    // upgradeRow.Fields[0].PreviousData = (string)row[0];

                                    // Inserting a new Upgrade record with the updated UpgradeCode
                                    upgradeRow[0] = upgradeId;
                                    upgradeRow[1] = row[1];
                                    upgradeRow[2] = row[2];
                                    upgradeRow[3] = row[3];
                                    upgradeRow[4] = row[4];
                                    upgradeRow[5] = row[5];
                                    upgradeRow[6] = row[6];

                                    // Delete the old row
                                    Row upgradeRemoveRow = upgradeTable.CreateRow(null);
                                    upgradeRemoveRow.Operation = RowOperation.Delete;
                                    upgradeRemoveRow[0] = row[0];
                                    upgradeRemoveRow[1] = row[1];
                                    upgradeRemoveRow[2] = row[2];
                                    upgradeRemoveRow[3] = row[3];
                                    upgradeRemoveRow[4] = row[4];
                                    upgradeRemoveRow[5] = row[5];
                                    upgradeRemoveRow[6] = row[6];
                                }
                            }
                        }
                    }

                    // If there are instance Components generate new GUIDs for them.
                    if (0 < instanceComponentGuids.Count)
                    {
                        Table componentTable = instanceTransform.EnsureTable(this.core.TableDefinitions["Component"]);
                        foreach (ComponentRow targetComponentRow in instanceComponentGuids.Values)
                        {
                            string guid = targetComponentRow.Guid;
                            if (!String.IsNullOrEmpty(guid))
                            {
                                Row instanceComponentRow = componentTable.CreateRow(targetComponentRow.SourceLineNumbers);
                                instanceComponentRow.Operation = RowOperation.Modify;
                                instanceComponentRow.Fields[1].Modified = true;
                                instanceComponentRow[0] = targetComponentRow[0];
                                instanceComponentRow[1] = Uuid.NewUuid(Binder.WixComponentGuidNamespace, String.Concat(guid, instanceId), this.backwardsCompatibleGuidGen).ToString("B").ToUpper(CultureInfo.InvariantCulture);
                                instanceComponentRow[2] = targetComponentRow[2];
                                instanceComponentRow[3] = targetComponentRow[3];
                                instanceComponentRow[4] = targetComponentRow[4];
                                instanceComponentRow[5] = targetComponentRow[5];
                            }
                        }
                    }

                    // Update the summary information
                    Hashtable summaryRows = new Hashtable(instanceSummaryInformationTable.Rows.Count);
                    foreach (Row row in instanceSummaryInformationTable.Rows)
                    {
                        summaryRows[row[0]] = row;

                        if ((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage == (int)row[0])
                        {
                            row[1] = targetPlatformAndLanguage;
                        }
                        else if ((int)SummaryInformation.Transform.ProductCodes == (int)row[0])
                        {
                            row[1] = String.Concat(targetProductCode, targetProductVersion, ';', productCode, targetProductVersion, ';', targetUpgradeCode);
                        }
                        else if ((int)SummaryInformation.Transform.ValidationFlags == (int)row[0])
                        {
                            row[1] = 0;
                        }
                        else if ((int)SummaryInformation.Transform.Security == (int)row[0])
                        {
                            row[1] = "4";
                        }
                    }

                    if (!summaryRows.Contains((int)SummaryInformation.Transform.UpdatedPlatformAndLanguage))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.UpdatedPlatformAndLanguage;
                        summaryRow[1] = targetPlatformAndLanguage;
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.ValidationFlags))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.ValidationFlags;
                        summaryRow[1] = "0";
                    }
                    else if (!summaryRows.Contains((int)SummaryInformation.Transform.Security))
                    {
                        Row summaryRow = instanceSummaryInformationTable.CreateRow(null);
                        summaryRow[0] = (int)SummaryInformation.Transform.Security;
                        summaryRow[1] = "4";
                    }

                    output.SubStorages.Add(new SubStorage(instanceId, instanceTransform));
                }
            }
        }

        /// <summary>
        /// Validate that there are no duplicate GUIDs in the output.
        /// </summary>
        /// <remarks>
        /// Duplicate GUIDs without conditions are an error condition; with conditions, it's a
        /// warning, as the conditions might be mutually exclusive.
        /// </remarks>
        private void ValidateComponentGuids(Output output)
        {
            Table componentTable = output.Tables["Component"];
            if (null != componentTable)
            {
                Dictionary<string, bool> componentGuidConditions = new Dictionary<string, bool>(componentTable.Rows.Count);

                foreach (ComponentRow row in componentTable.Rows)
                {
                    // we don't care about unmanaged components and if there's a * GUID remaining,
                    // there's already an error that prevented it from being replaced with a real GUID.
                    if (!String.IsNullOrEmpty(row.Guid) && "*" != row.Guid)
                    {
                        bool thisComponentHasCondition = !String.IsNullOrEmpty(row.Condition);
                        bool allComponentsHaveConditions = thisComponentHasCondition;

                        if (componentGuidConditions.ContainsKey(row.Guid))
                        {
                            allComponentsHaveConditions = componentGuidConditions[row.Guid] && thisComponentHasCondition;

                            if (allComponentsHaveConditions)
                            {
                                this.core.OnMessage(WixWarnings.DuplicateComponentGuidsMustHaveMutuallyExclusiveConditions(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                            else
                            {
                                this.core.OnMessage(WixErrors.DuplicateComponentGuids(row.SourceLineNumbers, row.Component, row.Guid));
                            }
                        }

                        componentGuidConditions[row.Guid] = allComponentsHaveConditions;
                    }
                }
            }
        }

        /// <summary>
        /// Update several msi tables with data contained in files references in the File table.
        /// </summary>
        /// <remarks>
        /// For versioned files, update the file version and language in the File table.  For
        /// unversioned files, add a row to the MsiFileHash table for the file.  For assembly
        /// files, add a row to the MsiAssembly table and add AssemblyName information by adding
        /// MsiAssemblyName rows.
        /// </remarks>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        /// <param name="mediaRows">The indexed media rows.</param>
        /// <param name="infoCache">A hashtable to populate with the file information (optional).</param>
        /// <param name="modularizationGuid">The modularization guid (used in case of a merge module).</param>
        private Hashtable UpdateFileInformation(Output output, FileRowCollection fileRows, MediaRowCollection mediaRows, IDictionary<string, string> infoCache, string modularizationGuid)
        {
            // Index for all the fileId's
            // NOTE: When dealing with patches, there is a file table for each transform. In most cases, the data in these rows will be the same, however users of this index need to be aware of this.
            Hashtable fileRowIndex = new Hashtable(fileRows.Count);
            Table mediaTable = output.Tables["Media"];

            // calculate sequence numbers and media disk id layout for all file media information objects
            if (OutputType.Module == output.Type)
            {
                int lastSequence = 0;
                foreach (FileRow fileRow in fileRows)
                {
                    fileRow.Sequence = ++lastSequence;
                    fileRowIndex[fileRow.File] = fileRow;
                }
            }
            else if (null != mediaTable)
            {
                int lastSequence = 0;
                MediaRow mediaRow = null;
                SortedList patchGroups = new SortedList();

                // sequence the non-patch-added files
                foreach (FileRow fileRow in fileRows)
                {
                    fileRowIndex[fileRow.File] = fileRow;
                    if (null == mediaRow)
                    {
                        mediaRow = mediaRows[fileRow.DiskId];
                        if (OutputType.Patch == output.Type)
                        {
                            // patch Media cannot start at zero
                            lastSequence = mediaRow.LastSequence;
                        }
                    }
                    else if (mediaRow.DiskId != fileRow.DiskId)
                    {
                        mediaRow.LastSequence = lastSequence;
                        mediaRow = mediaRows[fileRow.DiskId];
                    }

                    if (0 < fileRow.PatchGroup)
                    {
                        ArrayList patchGroup = (ArrayList)patchGroups[fileRow.PatchGroup];

                        if (null == patchGroup)
                        {
                            patchGroup = new ArrayList();
                            patchGroups.Add(fileRow.PatchGroup, patchGroup);
                        }

                        patchGroup.Add(fileRow);
                    }
                    else
                    {
                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                    mediaRow = null;
                }

                // sequence the patch-added files
                foreach (ArrayList patchGroup in patchGroups.Values)
                {
                    foreach (FileRow fileRow in patchGroup)
                    {
                        if (null == mediaRow)
                        {
                            mediaRow = mediaRows[fileRow.DiskId];
                        }
                        else if (mediaRow.DiskId != fileRow.DiskId)
                        {
                            mediaRow.LastSequence = lastSequence;
                            mediaRow = mediaRows[fileRow.DiskId];
                        }

                        fileRow.Sequence = ++lastSequence;
                    }
                }

                if (null != mediaRow)
                {
                    mediaRow.LastSequence = lastSequence;
                }
            }
            else
            {
                foreach (FileRow fileRow in fileRows)
                {
                    fileRowIndex[fileRow.File] = fileRow;
                }
            }

            // no more work to do here if there are no file rows in the file table
            // note that this does not mean there are no files - the files from
            // merge modules are never put in the output's file table
            Table fileTable = output.Tables["File"];
            if (null == fileTable)
            {
                return fileRowIndex;
            }

            // gather information about files that did not come from merge modules
            foreach (FileRow row in fileTable.Rows)
            {
                UpdateFileRow(output, infoCache, modularizationGuid, fileRowIndex, row, false);
            }

            return fileRowIndex;
        }

        private void UpdateFileRow(Output output, IDictionary<string, string> infoCache, string modularizationGuid, Hashtable fileRowIndex, FileRow fileRow, bool overwriteHash)
        {
            FileInfo fileInfo = null;

            if (!this.suppressFileHashAndInfo || (!this.suppressAssemblies && FileAssemblyType.NotAnAssembly != fileRow.AssemblyType))
            {
                try
                {
                    fileInfo = new FileInfo(fileRow.Source);
                }
                catch (ArgumentException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                    return;
                }
                catch (PathTooLongException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                    return;
                }
                catch (NotSupportedException)
                {
                    this.core.OnMessage(WixErrors.InvalidFileName(fileRow.SourceLineNumbers, fileRow.Source));
                    return;
                }
            }

            if (!this.suppressFileHashAndInfo)
            {
                if (fileInfo.Exists)
                {
                    string version;
                    string language;

                    using (FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        if (Int32.MaxValue < fileStream.Length)
                        {
                            throw new WixException(WixErrors.FileTooLarge(fileRow.SourceLineNumbers, fileRow.Source));
                        }

                        fileRow.FileSize = Convert.ToInt32(fileStream.Length, CultureInfo.InvariantCulture);
                    }

                    try
                    {
                        Installer.GetFileVersion(fileInfo.FullName, out version, out language);
                    }
                    catch (Win32Exception e)
                    {
                        if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                        {
                            throw new WixException(WixErrors.FileNotFound(fileRow.SourceLineNumbers, fileInfo.FullName));
                        }
                        else
                        {
                            throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, e.Message));
                        }
                    }

                    // If there is no version, it is assumed there is no language because it won't matter in the versioning of the install.
                    if (0 == version.Length)   // unversioned files have their hashes added to the MsiFileHash table
                    {
                        if (null != fileRow.Version)
                        {
                            // Check if this is a companion file. If its not, it is a default version.
                            if (!fileRowIndex.ContainsKey(fileRow.Version))
                            {
                                this.core.OnMessage(WixWarnings.DefaultVersionUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Version, fileRow.File));
                            }
                        }
                        else
                        {
                            if (null != fileRow.Language)
                            {
                                this.core.OnMessage(WixWarnings.DefaultLanguageUsedForUnversionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
                            }

                            int[] hash;
                            try
                            {
                                Installer.GetFileHash(fileInfo.FullName, 0, out hash);
                            }
                            catch (Win32Exception e)
                            {
                                if (0x2 == e.NativeErrorCode) // ERROR_FILE_NOT_FOUND
                                {
                                    throw new WixException(WixErrors.FileNotFound(fileRow.SourceLineNumbers, fileInfo.FullName));
                                }
                                else
                                {
                                    throw new WixException(WixErrors.Win32Exception(e.NativeErrorCode, fileInfo.FullName, e.Message));
                                }
                            }

                            if (null == fileRow.HashRow)
                            {
                                Table msiFileHashTable = output.EnsureTable(this.core.TableDefinitions["MsiFileHash"]);
                                fileRow.HashRow = msiFileHashTable.CreateRow(fileRow.SourceLineNumbers);
                            }

                            fileRow.HashRow[0] = fileRow.File;
                            fileRow.HashRow[1] = 0;
                            fileRow.HashRow[2] = hash[0];
                            fileRow.HashRow[3] = hash[1];
                            fileRow.HashRow[4] = hash[2];
                            fileRow.HashRow[5] = hash[3];
                        }
                    }
                    else // update the file row with the version and language information
                    {
                        // Check if the version field references a fileId because this would mean it has a companion file and the version should not be overwritten.
                        if (null == fileRow.Version || !fileRowIndex.ContainsKey(fileRow.Version))
                        {
                            fileRow.Version = version;
                        }

                        if (null != fileRow.Language && 0 == language.Length)
                        {
                            this.core.OnMessage(WixWarnings.DefaultLanguageUsedForVersionedFile(fileRow.SourceLineNumbers, fileRow.Language, fileRow.File));
                        }
                        else
                        {
                            fileRow.Language = language;
                        }

                        // Populate the binder variables for this file information if requested.
                        if (null != infoCache)
                        {
                            if (!String.IsNullOrEmpty(fileRow.Version))
                            {
                                string key = String.Format(CultureInfo.InvariantCulture, "fileversion.{0}", Demodularize(output, modularizationGuid, fileRow.File));
                                infoCache[key] = fileRow.Version;
                            }

                            if (!String.IsNullOrEmpty(fileRow.Language))
                            {
                                string key = String.Format(CultureInfo.InvariantCulture, "filelanguage.{0}", Demodularize(output, modularizationGuid, fileRow.File));
                                infoCache[key] = fileRow.Language;
                            }
                        }
                    }
                }
                else
                {
                    this.core.OnMessage(WixErrors.CannotFindFile(fileRow.SourceLineNumbers, fileRow.File, fileRow.FileName, fileRow.Source));
                }
            }

            // if we're not suppressing automagically grabbing assembly information and this is a
            // CLR assembly, load the assembly and get the assembly name information
            if (!this.suppressAssemblies)
            {
                if (FileAssemblyType.DotNetAssembly == fileRow.AssemblyType)
                {
                    StringDictionary assemblyNameValues = new StringDictionary();

                    CLRInterop.IReferenceIdentity referenceIdentity = null;
                    Guid referenceIdentityGuid = CLRInterop.ReferenceIdentityGuid;
                    uint result = CLRInterop.GetAssemblyIdentityFromFile(fileInfo.FullName, ref referenceIdentityGuid, out referenceIdentity);
                    if (0 == result && null != referenceIdentity)
                    {
                        string culture = referenceIdentity.GetAttribute(null, "Culture");
                        if (null != culture)
                        {
                            assemblyNameValues.Add("Culture", culture);
                        }
                        else
                        {
                            assemblyNameValues.Add("Culture", "neutral");
                        }

                        string name = referenceIdentity.GetAttribute(null, "Name");
                        if (null != name)
                        {
                            assemblyNameValues.Add("Name", name);
                        }

                        string processorArchitecture = referenceIdentity.GetAttribute(null, "ProcessorArchitecture");
                        if (null != processorArchitecture)
                        {
                            assemblyNameValues.Add("ProcessorArchitecture", processorArchitecture);
                        }

                        string publicKeyToken = referenceIdentity.GetAttribute(null, "PublicKeyToken");
                        if (null != publicKeyToken)
                        {
                            bool publicKeyIsNeutral = (String.Equals(publicKeyToken, "neutral", StringComparison.OrdinalIgnoreCase));

                            // Managed code expects "null" instead of "neutral", and
                            // this won't be installed to the GAC since it's not signed anyway.
                            assemblyNameValues.Add("publicKeyToken", publicKeyIsNeutral ? "null" : publicKeyToken.ToUpperInvariant());
                            assemblyNameValues.Add("publicKeyTokenPreservedCase", publicKeyIsNeutral ? "null" : publicKeyToken);
                        }
                        else if (fileRow.AssemblyApplication == null)
                        {
                            throw new WixException(WixErrors.GacAssemblyNoStrongName(fileRow.SourceLineNumbers, fileInfo.FullName, fileRow.Component));
                        }

                        string version = referenceIdentity.GetAttribute(null, "Version");
                        if (null != version)
                        {
                            assemblyNameValues.Add("Version", version);
                        }
                    }
                    else
                    {
                        this.core.OnMessage(WixErrors.InvalidAssemblyFile(fileRow.SourceLineNumbers, fileInfo.FullName, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", result)));
                        return;
                    }

                    Table assemblyNameTable = output.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
                    if (assemblyNameValues.ContainsKey("name"))
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "name", assemblyNameValues["name"], infoCache, modularizationGuid);
                    }

                    string fileVersion = null;
                    if (this.setMsiAssemblyNameFileVersion)
                    {
                        string language;

                        Installer.GetFileVersion(fileInfo.FullName, out fileVersion, out language);
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "fileVersion", fileVersion, infoCache, modularizationGuid);
                    }

                    if (assemblyNameValues.ContainsKey("version"))
                    {
                        string assemblyVersion = assemblyNameValues["version"];

                        if (!this.exactAssemblyVersions)
                        {
                            // there is a bug in fusion that requires the assembly's "version" attribute
                            // to be equal to or longer than the "fileVersion" in length when its present;
                            // the workaround is to prepend zeroes to the last version number in the assembly version
                            if (this.setMsiAssemblyNameFileVersion && null != fileVersion && fileVersion.Length > assemblyVersion.Length)
                            {
                                string padding = new string('0', fileVersion.Length - assemblyVersion.Length);
                                string[] assemblyVersionNumbers = assemblyVersion.Split('.');

                                if (assemblyVersionNumbers.Length > 0)
                                {
                                    assemblyVersionNumbers[assemblyVersionNumbers.Length - 1] = String.Concat(padding, assemblyVersionNumbers[assemblyVersionNumbers.Length - 1]);
                                    assemblyVersion = String.Join(".", assemblyVersionNumbers);
                                }
                            }
                        }

                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "version", assemblyVersion, infoCache, modularizationGuid);
                    }

                    if (assemblyNameValues.ContainsKey("culture"))
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "culture", assemblyNameValues["culture"], infoCache, modularizationGuid);
                    }

                    if (assemblyNameValues.ContainsKey("publicKeyToken"))
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "publicKeyToken", assemblyNameValues["publicKeyToken"], infoCache, modularizationGuid);
                    }

                    if (null != fileRow.ProcessorArchitecture && 0 < fileRow.ProcessorArchitecture.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", fileRow.ProcessorArchitecture, infoCache, modularizationGuid);
                    }

                    if (assemblyNameValues.ContainsKey("processorArchitecture"))
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", assemblyNameValues["processorArchitecture"], infoCache, modularizationGuid);
                    }

                    // add the assembly name to the information cache
                    if (null != infoCache)
                    {
                        string fileId = Demodularize(output, modularizationGuid, fileRow.File);
                        string key = String.Concat("assemblyfullname.", fileId);
                        string assemblyName = String.Concat(assemblyNameValues["name"], ", version=", assemblyNameValues["version"], ", culture=", assemblyNameValues["culture"], ", publicKeyToken=", String.IsNullOrEmpty(assemblyNameValues["publicKeyToken"]) ? "null" : assemblyNameValues["publicKeyToken"]);
                        if (assemblyNameValues.ContainsKey("processorArchitecture"))
                        {
                            assemblyName = String.Concat(assemblyName, ", processorArchitecture=", assemblyNameValues["processorArchitecture"]);
                        }

                        infoCache[key] = assemblyName;

                        // Add entries with the preserved case publicKeyToken
                        string pcAssemblyNameKey = String.Concat("assemblyfullnamepreservedcase.", fileId);
                        infoCache[pcAssemblyNameKey] = (assemblyNameValues["publicKeyToken"] == assemblyNameValues["publicKeyTokenPreservedCase"]) ? assemblyName : assemblyName.Replace(assemblyNameValues["publicKeyToken"], assemblyNameValues["publicKeyTokenPreservedCase"]);

                        string pcPublicKeyTokenKey = String.Concat("assemblypublickeytokenpreservedcase.", fileId);
                        infoCache[pcPublicKeyTokenKey] = assemblyNameValues["publicKeyTokenPreservedCase"];
                    }
                }
                else if (FileAssemblyType.Win32Assembly == fileRow.AssemblyType)
                {
                    // Able to use the index because only the Source field is used and it is used only for more complete error messages.
                    FileRow fileManifestRow = (FileRow)fileRowIndex[fileRow.AssemblyManifest];

                    if (null == fileManifestRow)
                    {
                        this.core.OnMessage(WixErrors.MissingManifestForWin32Assembly(fileRow.SourceLineNumbers, fileRow.File, fileRow.AssemblyManifest));
                    }

                    string type = null;
                    string name = null;
                    string version = null;
                    string processorArchitecture = null;
                    string publicKeyToken = null;

                    // loading the dom is expensive we want more performant APIs than the DOM
                    // Navigator is cheaper than dom.  Perhaps there is a cheaper API still.
                    try
                    {
                        XPathDocument doc = new XPathDocument(fileManifestRow.Source);
                        XPathNavigator nav = doc.CreateNavigator();
                        nav.MoveToRoot();

                        // this assumes a particular schema for a win32 manifest and does not
                        // provide error checking if the file does not conform to schema.
                        // The fallback case here is that nothing is added to the MsiAssemblyName
                        // table for an out of tolerance Win32 manifest.  Perhaps warnings needed.
                        if (nav.MoveToFirstChild())
                        {
                            while (nav.NodeType != XPathNodeType.Element || nav.Name != "assembly")
                            {
                                nav.MoveToNext();
                            }

                            if (nav.MoveToFirstChild())
                            {
                                bool hasNextSibling = true;
                                while (nav.NodeType != XPathNodeType.Element || nav.Name != "assemblyIdentity" && hasNextSibling)
                                {
                                    hasNextSibling = nav.MoveToNext();
                                }
                                if (!hasNextSibling)
                                {
                                    this.core.OnMessage(WixErrors.InvalidManifestContent(fileRow.SourceLineNumbers, fileManifestRow.Source));
                                    return;
                                }

                                if (nav.MoveToAttribute("type", String.Empty))
                                {
                                    type = nav.Value;
                                    nav.MoveToParent();
                                }

                                if (nav.MoveToAttribute("name", String.Empty))
                                {
                                    name = nav.Value;
                                    nav.MoveToParent();
                                }

                                if (nav.MoveToAttribute("version", String.Empty))
                                {
                                    version = nav.Value;
                                    nav.MoveToParent();
                                }

                                if (nav.MoveToAttribute("processorArchitecture", String.Empty))
                                {
                                    processorArchitecture = nav.Value;
                                    nav.MoveToParent();
                                }

                                if (nav.MoveToAttribute("publicKeyToken", String.Empty))
                                {
                                    publicKeyToken = nav.Value;
                                    nav.MoveToParent();
                                }
                            }
                        }
                    }
                    catch (FileNotFoundException fe)
                    {
                        this.core.OnMessage(WixErrors.FileNotFound(SourceLineNumberCollection.FromFileName(fileManifestRow.Source), fe.FileName, "AssemblyManifest"));
                    }
                    catch (XmlException xe)
                    {
                        this.core.OnMessage(WixErrors.InvalidXml(SourceLineNumberCollection.FromFileName(fileManifestRow.Source), "manifest", xe.Message));
                    }

                    Table assemblyNameTable = output.EnsureTable(this.core.TableDefinitions["MsiAssemblyName"]);
                    if (null != name && 0 < name.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "name", name, infoCache, modularizationGuid);
                    }

                    if (null != version && 0 < version.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "version", version, infoCache, modularizationGuid);
                    }

                    if (null != type && 0 < type.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "type", type, infoCache, modularizationGuid);
                    }

                    if (null != processorArchitecture && 0 < processorArchitecture.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "processorArchitecture", processorArchitecture, infoCache, modularizationGuid);
                    }

                    if (null != publicKeyToken && 0 < publicKeyToken.Length)
                    {
                        SetMsiAssemblyName(output, assemblyNameTable, fileRow, "publicKeyToken", publicKeyToken, infoCache, modularizationGuid);
                    }
                }
            }
        }

        /// <summary>
        /// Update Control and BBControl text by reading from files when necessary.
        /// </summary>
        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        private void UpdateControlText(Output output)
        {
            // Control table
            Table controlTable = output.Tables["Control"];
            if (null != controlTable)
            {
                foreach (ControlRow controlRow in controlTable.Rows)
                {
                    if (null != controlRow.SourceFile)
                    {
                        controlRow.Text = this.ReadTextFile(controlRow.SourceLineNumbers, controlRow.SourceFile);
                    }
                }
            }

            // BBControl table
            Table bbcontrolTable = output.Tables["BBControl"];
            if (null != bbcontrolTable)
            {
                foreach (BBControlRow bbcontrolRow in bbcontrolTable.Rows)
                {
                    if (null != bbcontrolRow.SourceFile)
                    {
                        bbcontrolRow.Text = this.ReadTextFile(bbcontrolRow.SourceLineNumbers, bbcontrolRow.SourceFile);
                    }
                }
            }
        }

        /// <summary>
        /// Reads a text file and returns the contents.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for row from source.</param>
        /// <param name="source">Source path to file to read.</param>
        /// <returns>Text string read from file.</returns>
        private string ReadTextFile(SourceLineNumberCollection sourceLineNumbers, string source)
        {
            string text = null;

            try
            {
                using (StreamReader reader = new StreamReader(source))
                {
                    text = reader.ReadToEnd();
                }
            }
            catch (DirectoryNotFoundException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (FileNotFoundException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (IOException e)
            {
                this.core.OnMessage(WixErrors.BinderFileManagerMissingFile(sourceLineNumbers, e.Message));
            }
            catch (NotSupportedException)
            {
                this.core.OnMessage(WixErrors.FileNotFound(sourceLineNumbers, source));
            }

            return text;
        }

        /// <summary>
        /// Merges in any modules to the output database.
        /// </summary>
        /// <param name="tempDatabaseFile">The temporary database file.</param>
        /// <param name="output">Output that specifies database and modules to merge.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        /// <param name="suppressedTableNames">The names of tables that are suppressed.</param>
        /// <remarks>Expects that output's database has already been generated.</remarks>
        private void MergeModules(string tempDatabaseFile, Output output, FileRowCollection fileRows, StringCollection suppressedTableNames)
        {
            Debug.Assert(OutputType.Product == output.Type);

            Table wixMergeTable = output.Tables["WixMerge"];
            Table wixFeatureModulesTable = output.Tables["WixFeatureModules"];

            // check for merge rows to see if there is any work to do
            if (null == wixMergeTable || 0 == wixMergeTable.Rows.Count)
            {
                return;
            }

            IMsmMerge2 merge = null;
            bool commit = true;
            bool logOpen = false;
            bool databaseOpen = false;
            string logPath = null;
            try
            {
                merge = NativeMethods.GetMsmMerge();

                logPath = Path.Combine(this.TempFilesLocation, "merge.log");
                merge.OpenLog(logPath);
                logOpen = true;

                merge.OpenDatabase(tempDatabaseFile);
                databaseOpen = true;

                // process all the merge rows
                foreach (WixMergeRow wixMergeRow in wixMergeTable.Rows)
                {
                    bool moduleOpen = false;

                    try
                    {
                        short mergeLanguage;

                        try
                        {
                            mergeLanguage = Convert.ToInt16(wixMergeRow.Language, CultureInfo.InvariantCulture);
                        }
                        catch (System.FormatException)
                        {
                            this.core.OnMessage(WixErrors.InvalidMergeLanguage(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, wixMergeRow.Language));
                            continue;
                        }

                        this.core.OnMessage(WixVerboses.OpeningMergeModule(wixMergeRow.SourceFile, mergeLanguage));
                        merge.OpenModule(wixMergeRow.SourceFile, mergeLanguage);
                        moduleOpen = true;

                        // If there is merge configuration data, create a callback object to contain it all.
                        ConfigurationCallback callback = null;
                        if (!String.IsNullOrEmpty(wixMergeRow.ConfigurationData))
                        {
                            callback = new ConfigurationCallback(wixMergeRow.ConfigurationData);
                        }

                        // merge the module into the database that's being built
                        this.core.OnMessage(WixVerboses.MergingMergeModule(wixMergeRow.SourceFile));
                        merge.MergeEx(wixMergeRow.Feature, wixMergeRow.Directory, callback);

                        // connect any non-primary features
                        if (null != wixFeatureModulesTable)
                        {
                            foreach (Row row in wixFeatureModulesTable.Rows)
                            {
                                if (wixMergeRow.Id == (string)row[1])
                                {
                                    this.core.OnMessage(WixVerboses.ConnectingMergeModule(wixMergeRow.SourceFile, (string)row[0]));
                                    merge.Connect((string)row[0]);
                                }
                            }
                        }
                    }
                    catch (COMException)
                    {
                        commit = false;
                    }
                    finally
                    {
                        IMsmErrors mergeErrors = merge.Errors;

                        // display all the errors encountered during the merge operations for this module
                        for (int i = 1; i <= mergeErrors.Count; i++)
                        {
                            IMsmError mergeError = mergeErrors[i];
                            StringBuilder databaseKeys = new StringBuilder();
                            StringBuilder moduleKeys = new StringBuilder();

                            // build a string of the database keys
                            for (int j = 1; j <= mergeError.DatabaseKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    databaseKeys.Append(';');
                                }
                                databaseKeys.Append(mergeError.DatabaseKeys[j]);
                            }

                            // build a string of the module keys
                            for (int j = 1; j <= mergeError.ModuleKeys.Count; j++)
                            {
                                if (1 != j)
                                {
                                    moduleKeys.Append(';');
                                }
                                moduleKeys.Append(mergeError.ModuleKeys[j]);
                            }

                            // display the merge error based on the msm error type
                            switch (mergeError.Type)
                            {
                                case MsmErrorType.msmErrorExclusion:
                                    this.core.OnMessage(WixErrors.MergeExcludedModule(wixMergeRow.SourceLineNumbers, wixMergeRow.Id, moduleKeys.ToString()));
                                    break;
                                case MsmErrorType.msmErrorFeatureRequired:
                                    this.core.OnMessage(WixErrors.MergeFeatureRequired(wixMergeRow.SourceLineNumbers, mergeError.ModuleTable, moduleKeys.ToString(), wixMergeRow.SourceFile, wixMergeRow.Id));
                                    break;
                                case MsmErrorType.msmErrorLanguageFailed:
                                    this.core.OnMessage(WixErrors.MergeLanguageFailed(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorLanguageUnsupported:
                                    this.core.OnMessage(WixErrors.MergeLanguageUnsupported(wixMergeRow.SourceLineNumbers, mergeError.Language, wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorResequenceMerge:
                                    this.core.OnMessage(WixWarnings.MergeRescheduledAction(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    break;
                                case MsmErrorType.msmErrorTableMerge:
                                    if ("_Validation" != mergeError.DatabaseTable) // ignore merge errors in the _Validation table
                                    {
                                        this.core.OnMessage(WixWarnings.MergeTableFailed(wixMergeRow.SourceLineNumbers, mergeError.DatabaseTable, databaseKeys.ToString(), wixMergeRow.SourceFile));
                                    }
                                    break;
                                case MsmErrorType.msmErrorPlatformMismatch:
                                    this.core.OnMessage(WixErrors.MergePlatformMismatch(wixMergeRow.SourceLineNumbers, wixMergeRow.SourceFile));
                                    break;
                                default:
                                    this.core.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorWithType, Enum.GetName(typeof(MsmErrorType), mergeError.Type), logPath), "InvalidOperationException", Environment.StackTrace));
                                    break;
                            }
                        }

                        if (0 >= mergeErrors.Count && !commit)
                        {
                            this.core.OnMessage(WixErrors.UnexpectedException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnexpectedMergerErrorInSourceFile, wixMergeRow.SourceFile, logPath), "InvalidOperationException", Environment.StackTrace));
                        }

                        if (moduleOpen)
                        {
                            merge.CloseModule();
                        }
                    }
                }
            }
            finally
            {
                if (databaseOpen)
                {
                    merge.CloseDatabase(commit);
                }

                if (logOpen)
                {
                    merge.CloseLog();
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return;
            }

            using (Database db = new Database(tempDatabaseFile, OpenDatabase.Direct))
            {
                Table suppressActionTable = output.Tables["WixSuppressAction"];

                // suppress individual actions
                if (null != suppressActionTable)
                {
                    foreach (Row row in suppressActionTable.Rows)
                    {
                        if (db.TableExists((string)row[0]))
                        {
                            string query = String.Format(CultureInfo.InvariantCulture, "SELECT * FROM {0} WHERE `Action` = '{1}'", row[0].ToString(), (string)row[1]);

                            using (View view = db.OpenExecuteView(query))
                            {
                                using (Record record = view.Fetch())
                                {
                                    if (null != record)
                                    {
                                        this.core.OnMessage(WixWarnings.SuppressMergedAction((string)row[1], row[0].ToString()));
                                        view.Modify(ModifyView.Delete, record);
                                    }
                                }
                            }
                        }
                    }
                }

                // query for merge module actions in suppressed sequences and drop them
                foreach (string tableName in suppressedTableNames)
                {
                    if (!db.TableExists(tableName))
                    {
                        continue;
                    }

                    using (View view = db.OpenExecuteView(String.Concat("SELECT `Action` FROM ", tableName)))
                    {
                        while (true)
                        {
                            using (Record resultRecord = view.Fetch())
                            {
                                if (null == resultRecord)
                                {
                                    break;
                                }

                                this.core.OnMessage(WixWarnings.SuppressMergedAction(resultRecord.GetString(1), tableName));
                            }
                        }
                    }

                    // drop suppressed sequences
                    using (View view = db.OpenExecuteView(String.Concat("DROP TABLE ", tableName)))
                    {
                    }

                    // delete the validation rows
                    using (View view = db.OpenView(String.Concat("DELETE FROM _Validation WHERE `Table` = ?")))
                    {
                        using (Record record = new Record(1))
                        {
                            record.SetString(1, tableName);
                            view.Execute(record);
                        }
                    }
                }

                // now update the Attributes column for the files from the Merge Modules
                this.core.OnMessage(WixVerboses.ResequencingMergeModuleFiles());
                using (View view = db.OpenView("SELECT `Sequence`, `Attributes` FROM `File` WHERE `File`=?"))
                {
                    foreach (FileRow fileRow in fileRows)
                    {
                        if (!fileRow.FromModule)
                        {
                            continue;
                        }

                        using (Record record = new Record(1))
                        {
                            record.SetString(1, fileRow.File);
                            view.Execute(record);
                        }

                        using (Record recordUpdate = view.Fetch())
                        {
                            if (null == recordUpdate)
                            {
                                throw new InvalidOperationException("Failed to fetch a File row from the database that was merged in from a module.");
                            }

                            recordUpdate.SetInteger(1, fileRow.Sequence);

                            // update the file attributes to match the compression specified
                            // on the Merge element or on the Package element
                            int attributes = 0;

                            // get the current value if its not null
                            if (!recordUpdate.IsNull(2))
                            {
                                attributes = recordUpdate.GetInteger(2);
                            }

                            if (YesNoType.Yes == fileRow.Compressed)
                            {
                                // these are mutually exclusive
                                attributes |= MsiInterop.MsidbFileAttributesCompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            else if (YesNoType.No == fileRow.Compressed)
                            {
                                // these are mutually exclusive
                                attributes |= MsiInterop.MsidbFileAttributesNoncompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                            }
                            else // not specified
                            {
                                Debug.Assert(YesNoType.NotSet == fileRow.Compressed);

                                // clear any compression bits
                                attributes &= ~MsiInterop.MsidbFileAttributesCompressed;
                                attributes &= ~MsiInterop.MsidbFileAttributesNoncompressed;
                            }
                            recordUpdate.SetInteger(2, attributes);

                            view.Modify(ModifyView.Update, recordUpdate);
                        }
                    }
                }

                db.Commit();
            }
        }

        /// <summary>
        /// Creates cabinet files.
        /// </summary>
        /// <param name="output">Output to generate image for.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <param name="mediaRows">The indexed media rows.</param>
        /// <param name="layoutDirectory">The directory in which the image should be layed out.</param>
        /// <param name="compressed">Flag if source image should be compressed.</param>
        /// <returns>The uncompressed file rows.</returns>
        private FileRowCollection CreateCabinetFiles(Output output, FileRowCollection fileRows, ArrayList fileTransfers, MediaRowCollection mediaRows, string layoutDirectory, bool compressed, AutoMediaAssigner autoMediaAssigner)
        {
            this.SetCabbingThreadCount();

            // Send Binder object to Facilitate NewCabNamesCallBack Callback
            CabinetBuilder cabinetBuilder = new CabinetBuilder(this.cabbingThreadCount, Marshal.GetFunctionPointerForDelegate(this.newCabNamesCallBack));

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            int MaximumCabinetSizeForLargeFileSplitting;
            int MaximumUncompressedMediaSize;
            this.GetMediaTemplateAttributes(out MaximumCabinetSizeForLargeFileSplitting, out MaximumUncompressedMediaSize);
            cabinetBuilder.MaximumCabinetSizeForLargeFileSplitting = MaximumCabinetSizeForLargeFileSplitting;
            cabinetBuilder.MaximumUncompressedMediaSize = MaximumUncompressedMediaSize;

            if (null != this.MessageHandler)
            {
                cabinetBuilder.Message += new MessageEventHandler(this.MessageHandler);
            }

            foreach (DictionaryEntry entry in autoMediaAssigner.Cabinets)
            {
                MediaRow mediaRow = (MediaRow)entry.Key;
                FileRowCollection files = (FileRowCollection)entry.Value;

                string cabinetDir = this.FileManager.ResolveMedia(mediaRow, layoutDirectory);

                CabinetWorkItem cabinetWorkItem = this.CreateCabinetWorkItem(output, cabinetDir, mediaRow, files, fileTransfers);
                if (null != cabinetWorkItem)
                {
                    cabinetBuilder.Enqueue(cabinetWorkItem);
                }
            }

            // stop processing if an error previously occurred
            if (this.core.EncounteredError)
            {
                return null;
            }

            // create queued cabinets with multiple threads
            int cabError = cabinetBuilder.CreateQueuedCabinets();
            if (0 != cabError)
            {
                this.core.EncounteredError = true;
                return null;
            }

            return autoMediaAssigner.UncompressedFileRows;
        }

        /// <summary>
        /// Sets the codepage of a database.
        /// </summary>
        /// <param name="db">Database to set codepage into.</param>
        /// <param name="output">Output with the codepage for the database.</param>
        private void SetDatabaseCodepage(Database db, Output output)
        {
            // write out the _ForceCodepage IDT file
            string idtPath = Path.Combine(this.TempFilesLocation, "_ForceCodepage.idt");
            using (StreamWriter idtFile = new StreamWriter(idtPath, false, Encoding.ASCII))
            {
                idtFile.WriteLine(); // dummy column name record
                idtFile.WriteLine(); // dummy column definition record
                idtFile.Write(output.Codepage);
                idtFile.WriteLine("\t_ForceCodepage");
            }

            // try to import the table into the MSI
            try
            {
                db.Import(Path.GetDirectoryName(idtPath), Path.GetFileName(idtPath));
            }
            catch (WixInvalidIdtException)
            {
                // the IDT should be valid, so an invalid code page was given
                throw new WixException(WixErrors.IllegalCodepage(output.Codepage));
            }
        }

        /// <summary>
        /// Creates a work item to create a cabinet.
        /// </summary>
        /// <param name="output">Output for the current database.</param>
        /// <param name="cabinetDir">Directory to create cabinet in.</param>
        /// <param name="mediaRow">MediaRow containing information about the cabinet.</param>
        /// <param name="fileRows">Collection of files in this cabinet.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <returns>created CabinetWorkItem object</returns>
        private CabinetWorkItem CreateCabinetWorkItem(Output output, string cabinetDir, MediaRow mediaRow, FileRowCollection fileRows, ArrayList fileTransfers)
        {
            CabinetWorkItem cabinetWorkItem = null;
            string tempCabinetFile = Path.Combine(this.TempFilesLocation, mediaRow.Cabinet);

            // check for an empty cabinet
            if (0 == fileRows.Count)
            {
                string cabinetName = mediaRow.Cabinet;

                // remove the leading '#' from the embedded cabinet name to make the warning easier to understand
                if (cabinetName.StartsWith("#", StringComparison.Ordinal))
                {
                    cabinetName = cabinetName.Substring(1);
                }

                // If building a patch, remind them to run -p for torch.
                if (OutputType.Patch == output.Type)
                {
                    this.core.OnMessage(WixWarnings.EmptyCabinet(mediaRow.SourceLineNumbers, cabinetName, true));
                }
                else
                {
                    this.core.OnMessage(WixWarnings.EmptyCabinet(mediaRow.SourceLineNumbers, cabinetName));
                }
            }

            CabinetBuildOption cabinetBuildOption = this.FileManager.ResolveCabinet(fileRows, ref tempCabinetFile);

            // create a cabinet work item if it's not being skipped
            if (CabinetBuildOption.BuildAndCopy == cabinetBuildOption || CabinetBuildOption.BuildAndMove == cabinetBuildOption)
            {
                int maxThreshold = 0; // default to the threshold for best smartcabbing (makes smallest cabinet).
                Cab.CompressionLevel compressionLevel = this.defaultCompressionLevel;

                if (mediaRow.HasExplicitCompressionLevel)
                {
                    compressionLevel = mediaRow.CompressionLevel;
                }

                cabinetWorkItem = new CabinetWorkItem(fileRows, tempCabinetFile, maxThreshold, compressionLevel, this.FileManager);
            }
            else // reuse the cabinet from the cabinet cache.
            {
                this.core.OnMessage(WixVerboses.ReusingCabCache(mediaRow.SourceLineNumbers, mediaRow.Cabinet, tempCabinetFile));

                try
                {
                    // Ensure the cached cabinet timestamp is current to prevent perpetual incremental builds. The
                    // problematic scenario goes like this. Imagine two cabinets in the cache. Update a file that
                    // goes into one of the cabinets. One cabinet will get rebuilt, the other will be copied from
                    // the cache. Now the file (an input) has a newer timestamp than the reused cabient (an output)
                    // causing the project to look like it perpetually needs a rebuild until all of the reused
                    // cabinets get newer timestamps.
                    File.SetLastWriteTime(tempCabinetFile, DateTime.Now);
                }
                catch (Exception e)
                {
                    this.core.OnMessage(WixWarnings.CannotUpdateCabCache(mediaRow.SourceLineNumbers, tempCabinetFile, e.Message));
                }
            }

            if (mediaRow.Cabinet.StartsWith("#", StringComparison.Ordinal))
            {
                Table streamsTable = output.EnsureTable(this.core.TableDefinitions["_Streams"]);

                Row streamRow = streamsTable.CreateRow(null);
                streamRow[0] = mediaRow.Cabinet.Substring(1);
                streamRow[1] = tempCabinetFile;
            }
            else
            {
                string destinationPath = Path.Combine(cabinetDir, mediaRow.Cabinet);
                FileTransfer transfer;
                if (FileTransfer.TryCreate(tempCabinetFile, destinationPath, CabinetBuildOption.BuildAndMove == cabinetBuildOption, "Cabinet", mediaRow.SourceLineNumbers, out transfer))
                {
                    transfer.Built = true;
                    fileTransfers.Add(transfer);
                }
            }

            return cabinetWorkItem;
        }

        /// <summary>
        /// Process uncompressed files.
        /// </summary>
        /// <param name="tempDatabaseFile">The temporary database file.</param>
        /// <param name="fileRows">The collection of files to copy into the image.</param>
        /// <param name="fileTransfers">Array of files to be transfered.</param>
        /// <param name="mediaRows">The indexed media rows.</param>
        /// <param name="layoutDirectory">The directory in which the image should be layed out.</param>
        /// <param name="compressed">Flag if source image should be compressed.</param>
        /// <param name="longNamesInImage">Flag if long names should be used.</param>
        private void ProcessUncompressedFiles(string tempDatabaseFile, FileRowCollection fileRows, ArrayList fileTransfers, MediaRowCollection mediaRows, string layoutDirectory, bool compressed, bool longNamesInImage)
        {
            if (0 == fileRows.Count || this.core.EncounteredError)
            {
                return;
            }

            Hashtable directories = new Hashtable();
            using (Database db = new Database(tempDatabaseFile, OpenDatabase.ReadOnly))
            {
                using (View directoryView = db.OpenExecuteView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                {
                    while (true)
                    {
                        using (Record directoryRecord = directoryView.Fetch())
                        {
                            if (null == directoryRecord)
                            {
                                break;
                            }

                            string sourceName = Installer.GetName(directoryRecord.GetString(3), true, longNamesInImage);

                            directories.Add(directoryRecord.GetString(1), new ResolvedDirectory(directoryRecord.GetString(2), sourceName));
                        }
                    }
                }

                using (View fileView = db.OpenView("SELECT `Directory_`, `FileName` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_` AND `File`.`File`=?"))
                {
                    using (Record fileQueryRecord = new Record(1))
                    {
                        // for each file in the array of uncompressed files
                        foreach (FileRow fileRow in fileRows)
                        {
                            string relativeFileLayoutPath = null;

                            string mediaLayoutDirectory = this.FileManager.ResolveMedia(mediaRows[fileRow.DiskId], layoutDirectory);

                            // setup up the query record and find the appropriate file in the
                            // previously executed file view
                            fileQueryRecord[1] = fileRow.File;
                            fileView.Execute(fileQueryRecord);

                            using (Record fileRecord = fileView.Fetch())
                            {
                                if (null == fileRecord)
                                {
                                    throw new WixException(WixErrors.FileIdentifierNotFound(fileRow.SourceLineNumbers, fileRow.File));
                                }

                                relativeFileLayoutPath = Binder.GetFileSourcePath(directories, fileRecord[1], fileRecord[2], compressed, longNamesInImage);
                            }

                            // finally put together the base media layout path and the relative file layout path
                            string fileLayoutPath = Path.Combine(mediaLayoutDirectory, relativeFileLayoutPath);
                            FileTransfer transfer;
                            if (FileTransfer.TryCreate(fileRow.Source, fileLayoutPath, false, "File", fileRow.SourceLineNumbers, out transfer))
                            {
                                fileTransfers.Add(transfer);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the thead count to the number of processors if the current thread count is set to 0.
        /// </summary>
        /// <remarks>The thread count value must be greater than 0 otherwise and exception will be thrown.</remarks>
        private void SetCabbingThreadCount()
        {
            // default the number of cabbing threads to the number of processors if it wasn't specified
            if (0 == this.cabbingThreadCount)
            {
                string numberOfProcessors = System.Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");

                try
                {
                    if (null != numberOfProcessors)
                    {
                        this.cabbingThreadCount = Convert.ToInt32(numberOfProcessors, CultureInfo.InvariantCulture.NumberFormat);

                        if (0 >= this.cabbingThreadCount)
                        {
                            throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                        }
                    }
                    else // default to 1 if the environment variable is not set
                    {
                        this.cabbingThreadCount = 1;
                    }

                    this.core.OnMessage(WixVerboses.SetCabbingThreadCount(this.cabbingThreadCount.ToString()));
                }
                catch (ArgumentException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("NUMBER_OF_PROCESSORS", numberOfProcessors));
                }
            }
        }

        /// <summary>
        /// Writes the paths to the content files included in the package to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileRows">Collection of file rows whose source will be written to file.</param>
        private void CreateContentsFile(string path, FileRowCollection fileRows)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (FileRow fileRow in fileRows)
                {
                    contents.WriteLine(fileRow.Source);
                }
            }
        }

        /// <summary>
        /// Writes the paths to the content files included in the bundle to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="payloads">Collection of payloads whose source will be written to file.</param>
        private void CreateContentsFile(string path, IEnumerable<PayloadInfoRow> payloads)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter contents = new StreamWriter(path, false))
            {
                foreach (PayloadInfoRow payload in payloads)
                {
                    if (payload.ContentFile)
                    {
                        contents.WriteLine(payload.FullFileName);
                    }
                }
            }
        }

        /// <summary>
        /// Writes the paths to the output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateOutputsFile(string path, ArrayList fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Don't list files where the source is the same as the destination since
                    // that might be the only place the file exists. The outputs file is often
                    // used to delete stuff and losing the original source would be bad.
                    if (!fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Writes the paths to the built output files to a text file.
        /// </summary>
        /// <param name="path">Path to write file.</param>
        /// <param name="fileTransfers">Collection of files that were transferred to the output directory.</param>
        /// <param name="pdbPath">Optional path to created .wixpdb.</param>
        private void CreateBuiltOutputsFile(string path, ArrayList fileTransfers, string pdbPath)
        {
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (StreamWriter outputs = new StreamWriter(path, false))
            {
                foreach (FileTransfer fileTransfer in fileTransfers)
                {
                    // Only write the built file transfers. Also, skip redundant
                    // files for the same reason spelled out in this.CreateOutputsFile().
                    if (fileTransfer.Built && !fileTransfer.Redundant)
                    {
                        outputs.WriteLine(fileTransfer.Destination);
                    }
                }

                if (!String.IsNullOrEmpty(pdbPath))
                {
                    outputs.WriteLine(Path.GetFullPath(pdbPath));
                }
            }
        }

        /// <summary>
        /// Structure used to hold a row and field that contain binder variables, which need to be resolved
        /// later, once the files have been resolved.
        /// </summary>
        private struct DelayedField
        {
            /// <summary>
            /// The row containing the field.
            /// </summary>
            public Row Row;

            /// <summary>
            /// The field needing further resolving.
            /// </summary>
            public Field Field;

            /// <summary>
            /// Basic constructor for struct
            /// </summary>
            /// <param name="row">Row for the field.</param>
            /// <param name="field">Field needing further resolution.</param>
            public DelayedField(Row row, Field field)
            {
                this.Row = row;
                this.Field = field;
            }
        }

        /// <summary>
        /// Callback object for configurable merge modules.
        /// </summary>
        private sealed class ConfigurationCallback : IMsmConfigureModule
        {
            private const int SOk = 0x0;
            private const int SFalse = 0x1;
            private Hashtable configurationData;

            /// <summary>
            /// Creates a ConfigurationCallback object.
            /// </summary>
            /// <param name="configData">String to break up into name/value pairs.</param>
            public ConfigurationCallback(string configData)
            {
                if (String.IsNullOrEmpty(configData))
                {
                    throw new ArgumentNullException("configData");
                }

                string[] pairs = configData.Split(',');
                this.configurationData = new Hashtable(pairs.Length);
                for (int i = 0; i < pairs.Length; ++i)
                {
                    string[] nameVal = pairs[i].Split('=');
                    string name = nameVal[0];
                    string value = nameVal[1];

                    name = name.Replace("%2C", ",");
                    name = name.Replace("%3D", "=");
                    name = name.Replace("%25", "%");

                    value = value.Replace("%2C", ",");
                    value = value.Replace("%3D", "=");
                    value = value.Replace("%25", "%");

                    this.configurationData[name] = value;
                }
            }

            /// <summary>
            /// Returns text data based on name.
            /// </summary>
            /// <param name="name">Name of value to return.</param>
            /// <param name="configData">Out param to put configuration data into.</param>
            /// <returns>S_OK if value provided, S_FALSE if not.</returns>
            public int ProvideTextData(string name, out string configData)
            {
                if (this.configurationData.Contains(name))
                {
                    configData = (string)this.configurationData[name];
                    return SOk;
                }
                else
                {
                    configData = null;
                    return SFalse;
                }
            }

            /// <summary>
            /// Returns integer data based on name.
            /// </summary>
            /// <param name="name">Name of value to return.</param>
            /// <param name="configData">Out param to put configuration data into.</param>
            /// <returns>S_OK if value provided, S_FALSE if not.</returns>
            public int ProvideIntegerData(string name, out int configData)
            {
                if (this.configurationData.Contains(name))
                {
                    string val = (string)this.configurationData[name];
                    configData = Convert.ToInt32(val, CultureInfo.InvariantCulture);
                    return SOk;
                }
                else
                {
                    configData = 0;
                    return SFalse;
                }
            }
        }

        /// <summary>
        /// Gets Compiler Values of MediaTemplate Attributes governing Maximum Cabinet Size after applying Environment Variable Overrides
        /// </summary>
        /// <param name="output">Output to generate image for.</param>
        /// <param name="fileRows">The indexed file rows.</param>
        private void GetMediaTemplateAttributes(out int maxCabSizeForLargeFileSplitting, out int maxUncompressedMediaSize)
        {
            // Get Environment Variable Overrides for MediaTemplate Attributes governing Maximum Cabinet Size
            string mcslfsString = Environment.GetEnvironmentVariable("WIX_MCSLFS");
            string mumsString = Environment.GetEnvironmentVariable("WIX_MUMS");
            int maxCabSizeForLargeFileInMB = 0;
            int maxPreCompressedSizeInMB = 0;
            ulong testOverFlow = 0;

            // Supply Compile MediaTemplate Attributes to Cabinet Builder
            Table mediaTemplateTable = this.output.Tables["WixMediaTemplate"];
            if (mediaTemplateTable != null)
            {
                WixMediaTemplateRow mediaTemplateRow = (WixMediaTemplateRow)mediaTemplateTable.Rows[0];

                // Get the Value for Max Cab Size for File Splitting
                try
                {
                    // Override authored mcslfs value if environment variable is authored.
                    if (!String.IsNullOrEmpty(mcslfsString))
                    {
                        maxCabSizeForLargeFileInMB = Int32.Parse(mcslfsString);
                    }
                    else
                    {
                        maxCabSizeForLargeFileInMB = mediaTemplateRow.MaximumCabinetSizeForLargeFileSplitting;
                    }
                    testOverFlow = (ulong)maxCabSizeForLargeFileInMB * 1024 * 1024;
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MCSLFS", mcslfsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(WixErrors.MaximumCabinetSizeForLargeFileSplittingTooLarge(null, maxCabSizeForLargeFileInMB, CompilerCore.MaxValueOfMaxCabSizeForLargeFileSplitting));
                }

                try
                {
                    // Override authored mums value if environment variable is authored.
                    if (!String.IsNullOrEmpty(mumsString))
                    {
                        maxPreCompressedSizeInMB = Int32.Parse(mumsString);
                    }
                    else
                    {
                        maxPreCompressedSizeInMB = mediaTemplateRow.MaximumUncompressedMediaSize;
                    }
                    testOverFlow = (ulong)maxPreCompressedSizeInMB * 1024 * 1024;
                }
                catch (FormatException)
                {
                    throw new WixException(WixErrors.IllegalEnvironmentVariable("WIX_MUMS", mumsString));
                }
                catch (OverflowException)
                {
                    throw new WixException(WixErrors.MaximumUncompressedMediaSizeTooLarge(null, maxPreCompressedSizeInMB));
                }

                maxCabSizeForLargeFileSplitting = maxCabSizeForLargeFileInMB;
                maxUncompressedMediaSize = maxPreCompressedSizeInMB;
            }
            else
            {
                maxCabSizeForLargeFileSplitting = 0;
                maxUncompressedMediaSize = CompilerCore.DefaultMaximumUncompressedMediaSize;
            }
        }

        /// <summary>
        /// The types that the WixPatchSymbolPaths table can hold (and that the WixPatchSymbolPathsComparer can sort).
        /// </summary>
        internal enum SymbolPathType
        {
            File,
            Component,
            Directory,
            Media,
            Product
        };

        /// <summary>
        /// Sorts the WixPatchSymbolPaths table for processing.
        /// </summary>
        internal sealed class WixPatchSymbolPathsComparer : IComparer
        {
            /// <summary>
            /// Compares two rows from the WixPatchSymbolPaths table.
            /// </summary>
            /// <param name="a">First row to compare.</param>
            /// <param name="b">Second row to compare.</param>
            /// <remarks>Only the File, Product, Component, Directory, and Media tables links are allowed by this method.</remarks>
            /// <returns>Less than zero if a is less than b; Zero if they are equal, and Greater than zero if a is greater than b</returns>
            public int Compare(Object a, Object b)
            {
                Row ra = (Row)a;
                Row rb = (Row)b;

                SymbolPathType ia = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)ra.Fields[0]).Data.ToString());
                SymbolPathType ib = (SymbolPathType)Enum.Parse(typeof(SymbolPathType), ((Field)rb.Fields[0]).Data.ToString());
                return (int)ib - (int)ia;
            }
        }

        #region DependencyExtension
        /// <summary>
        /// Imports authored dependency providers for each package in the manifest,
        /// and generates dependency providers for certain package types that do not
        /// have a provider defined.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="packages">An indexed collection of chained packages.</param>
        private void ProcessDependencyProviders(Output bundle, Dictionary<string, ChainPackageInfo> packages)
        {
            // First import any authored dependencies. These may merge with imported provides from MSI packages.
            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Add package information for each dependency provider authored into the manifest.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    string packageId = (string)wixDependencyProviderRow[1];

                    ChainPackageInfo package = null;
                    if (packages.TryGetValue(packageId, out package))
                    {
                        ProvidesDependency dependency = new ProvidesDependency(wixDependencyProviderRow);

                        if (String.IsNullOrEmpty(dependency.Key))
                        {
                            switch (package.ChainPackageType)
                            {
                                // The WixDependencyExtension allows an empty Key for MSIs and MSPs.
                                case Compiler.ChainPackageType.Msi:
                                    dependency.Key = package.ProductCode;
                                    break;
                                case Compiler.ChainPackageType.Msp:
                                    dependency.Key = package.PatchCode;
                                    break;
                            }
                        }

                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            dependency.Version = package.Version;
                        }

                        // If the version is still missing, a version could not be harvested from the package and was not authored.
                        if (String.IsNullOrEmpty(dependency.Version))
                        {
                            this.core.OnMessage(WixErrors.MissingDependencyVersion(package.Id));
                        }

                        if (String.IsNullOrEmpty(dependency.DisplayName))
                        {
                            dependency.DisplayName = package.DisplayName;
                        }

                        if (!package.Provides.Merge(dependency))
                        {
                            this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                        }
                    }
                }
            }

            // Generate providers for MSI packages that still do not have providers.
            foreach (ChainPackageInfo package in packages.Values)
            {
                if (Compiler.ChainPackageType.Msi == package.ChainPackageType && 0 == package.Provides.Count)
                {
                    ProvidesDependency dependency = new ProvidesDependency(package.ProductCode, package.Version, package.DisplayName, 0);

                    if (!package.Provides.Merge(dependency))
                    {
                        this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                    }
                }
                else if (Compiler.ChainPackageType.Msp == package.ChainPackageType && 0 == package.Provides.Count)
                {
                    ProvidesDependency dependency = new ProvidesDependency(package.PatchCode, package.Version, package.DisplayName, 0);

                    if (!package.Provides.Merge(dependency))
                    {
                        this.core.OnMessage(WixErrors.DuplicateProviderDependencyKey(dependency.Key, package.Id));
                    }
                }
            }
        }

        /// <summary>
        /// Sets the provider key for the bundle.
        /// </summary>
        /// <param name="bundle">The <see cref="Output"/> object for the bundle.</param>
        /// <param name="bundleInfo">The <see cref="BundleInfo"/> containing the provider key and other information for the bundle.</param>
        private void SetBundleProviderKey(Output bundle, WixBundleRow bundleInfo)
        {
            // From DependencyCommon.cs in the WixDependencyExtension.
            const int ProvidesAttributesBundle = 0x10000;

            Table wixDependencyProviderTable = bundle.Tables["WixDependencyProvider"];
            if (null != wixDependencyProviderTable && 0 < wixDependencyProviderTable.Rows.Count)
            {
                // Search the WixDependencyProvider table for the single bundle provider key.
                foreach (Row wixDependencyProviderRow in wixDependencyProviderTable.Rows)
                {
                    object attributes = wixDependencyProviderRow[5];
                    if (null != attributes && 0 != (ProvidesAttributesBundle & (int)attributes))
                    {
                        bundleInfo.ProviderKey = (string)wixDependencyProviderRow[2];
                        break;
                    }
                }
            }

            // Defaults to the bundle ID as the provider key.
        }
        #endregion

        /// <summary>
        /// Delegate for Cabinet Split Callback
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate void FileSplitCabNamesCallback([MarshalAs(UnmanagedType.LPWStr)]string firstCabName, [MarshalAs(UnmanagedType.LPWStr)]string newCabName, [MarshalAs(UnmanagedType.LPWStr)]string fileToken);

        /// <summary>
        /// Call back to Add File Transfer for new Cab and add new Cab to Media table
        /// This callback can come from Multiple Cabinet Builder Threads and so should be thread safe
        /// This callback will not be called in case there is no File splitting. i.e. MaximumCabinetSizeForLargeFileSplitting was not authored
        /// </summary>
        /// <param name="firstCabName">The name of splitting cabinet without extention e.g. "cab1".</param>
        /// <param name="newCabName">The name of the new cabinet that would be formed by splitting e.g. "cab1b.cab"</param>
        /// <param name="fileToken">The file token of the first file present in the splitting cabinet</param>
        internal void NewCabNamesCallBack([MarshalAs(UnmanagedType.LPWStr)]string firstCabName, [MarshalAs(UnmanagedType.LPWStr)]string newCabName, [MarshalAs(UnmanagedType.LPWStr)]string fileToken)
        {
            // Locking Mutex here as this callback can come from Multiple Cabinet Builder Threads
            Mutex mutex = new Mutex(false, "WixCabinetSplitBinderCallback");
            try
            {
                if (!mutex.WaitOne(0, false)) // Check if you can get the lock
                {
                    // Cound not get the Lock
                    this.core.OnMessage(WixVerboses.CabinetsSplitInParallel());
                    mutex.WaitOne(); // Wait on other thread
                }

                string firstCabinetName = firstCabName + ".cab";
                string newCabinetName = newCabName;
                bool transferAdded = false; // Used for Error Handling

                // Create File Transfer for new Cabinet using transfer of Base Cabinet
                foreach (FileTransfer transfer in this.fileTransfers)
                {
                    if (firstCabinetName.Equals(Path.GetFileName(transfer.Source), StringComparison.InvariantCultureIgnoreCase))
                    {
                        string newCabSourcePath = Path.Combine(Path.GetDirectoryName(transfer.Source), newCabinetName);
                        string newCabTargetPath = Path.Combine(Path.GetDirectoryName(transfer.Destination), newCabinetName);

                        FileTransfer newTransfer;
                        if (FileTransfer.TryCreate(newCabSourcePath, newCabTargetPath, transfer.Move, "Cabinet", transfer.SourceLineNumbers, out newTransfer))
                        {
                            newTransfer.Built = true;
                            this.fileTransfers.Add(newTransfer);
                            transferAdded = true;
                            break;
                        }
                    }
                }

                // Check if File Transfer was added
                if (!transferAdded)
                {
                    throw new WixException(WixErrors.SplitCabinetCopyRegistrationFailed(newCabinetName, firstCabinetName));
                }

                // Add the new Cabinets to media table using LastSequence of Base Cabinet
                Table mediaTable = this.output.Tables["Media"];
                Table fileTable = this.output.Tables["File"];
                int diskIDForLastSplitCabAdded = 0; // The DiskID value for the first cab in this cabinet split chain
                int lastSequenceForLastSplitCabAdded = 0; // The LastSequence value for the first cab in this cabinet split chain
                bool lastSplitCabinetFound = false; // Used for Error Handling

                string lastCabinetOfThisSequence = String.Empty;
                // Get the Value of Last Cabinet Added in this split Sequence from Dictionary
                if (!this.lastCabinetAddedToMediaTable.TryGetValue(firstCabinetName, out lastCabinetOfThisSequence))
                {
                    // If there is no value for this sequence, then use first Cabinet is the last one of this split sequence
                    lastCabinetOfThisSequence = firstCabinetName;
                }

                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    // Get details for the Last Cabinet Added in this Split Sequence
                    if ((lastSequenceForLastSplitCabAdded == 0) && lastCabinetOfThisSequence.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        lastSequenceForLastSplitCabAdded = mediaRow.LastSequence;
                        diskIDForLastSplitCabAdded = mediaRow.DiskId;
                        lastSplitCabinetFound = true;
                    }

                    // Check for Name Collision for the new Cabinet added
                    if (newCabinetName.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Name Collision of generated Split Cabinet Name and user Specified Cab name for current row
                        throw new WixException(WixErrors.SplitCabinetNameCollision(newCabinetName, firstCabinetName));
                    }
                }

                // Check if the last Split Cabinet was found in the Media Table
                if (!lastSplitCabinetFound)
                {
                    throw new WixException(WixErrors.SplitCabinetInsertionFailed(newCabinetName, firstCabinetName, lastCabinetOfThisSequence));
                }

                // The new Row has to be inserted just after the last cab in this cabinet split chain according to DiskID Sort
                // This is because the FDI Extract requires DiskID of Split Cabinets to be continuous. It Fails otherwise with
                // Error 2350 (FDI Server Error) as next DiskID did not have the right split cabinet during extraction
                MediaRow newMediaRow = (MediaRow)mediaTable.CreateRow(null);
                newMediaRow.Cabinet = newCabinetName;
                newMediaRow.DiskId = diskIDForLastSplitCabAdded + 1; // When Sorted with DiskID, this new Cabinet Row is an Insertion
                newMediaRow.LastSequence = lastSequenceForLastSplitCabAdded;

                // Now increment the DiskID for all rows that come after the newly inserted row to Ensure that DiskId is unique
                foreach (MediaRow mediaRow in mediaTable.Rows)
                {
                    // Check if this row comes after inserted row and it is not the new cabinet inserted row
                    if (mediaRow.DiskId >= newMediaRow.DiskId && !newCabinetName.Equals(mediaRow.Cabinet, StringComparison.InvariantCultureIgnoreCase))
                    {
                        mediaRow.DiskId++; // Increment DiskID
                    }
                }

                // Now Increment DiskID for All files Rows so that the refer to the right Media Row
                foreach (FileRow fileRow in fileTable.Rows)
                {
                    // Check if this row comes after inserted row and if this row is not the file that has to go into the current cabinet
                    // This check will work as we have only one large file in every splitting cabinet
                    // If we want to support splitting cabinet with more large files we need to update this code
                    if (fileRow.DiskId >= newMediaRow.DiskId && !fileRow.File.Equals(fileToken, StringComparison.InvariantCultureIgnoreCase))
                    {
                        fileRow.DiskId++; // Increment DiskID
                    }
                }

                // Update the Last Cabinet Added in the Split Sequence in Dictionary for future callback
                this.lastCabinetAddedToMediaTable[firstCabinetName] = newCabinetName;

                mediaTable.ValidateRows(); // Valdiates DiskDIs, throws Exception as Wix Error if validation fails
            }
            finally
            {
                // Releasing the Mutex here
                mutex.ReleaseMutex();
            }
        }
    }
}
