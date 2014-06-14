//-----------------------------------------------------------------------
// <copyright file="LightArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with Light arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Light arguments
    /// </summary>
    public partial class Light
    {
        #region Private Members

        /// <summary>
        /// -ai
        /// </summary>
        private bool allowIdenticalRows;

        /// <summary>
        /// -au
        /// </summary>
        private bool allowUnresolvedVariables;

        /// <summary>
        /// -b
        /// </summary>
        private string bindPath;

        /// <summary>
        /// -bf
        /// </summary>
        private bool bindFiles;

        /// <summary>
        //  -ct <N>
        /// </summary>
        private int cabbingThreads;

        /// <summary>
        /// -cc
        /// </summary>
        private string cachedCabsPath;

        /// <summary>
        //  -cultures:<cultures>
        /// </summary>
        private string cultures;

        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;

        /// <summary>
        /// -fv
        /// </summary>
        private bool fileVersion;

        /// <summary>
        //  -ice:<ICE>
        /// </summary>
        private List<string> ices;

        /// <summary>
        //  -loc <loc.wxl>
        /// </summary>
        private List<string> locFiles;

        /// <summary>
        /// -notidy
        /// </summary>
        private bool noTidy;

        /// <summary>
        /// objectFile [objectFile ...]
        /// </summary>
        private List<string> objectFiles;

        /// <summary>
        /// -pedantic
        /// </summary>
        private bool pedantic;

        /// <summary>
        /// -reusecab
        /// </summary>
        private bool reuseCab;

        /// <summary>
        /// -sacl
        /// </summary>
        private bool suppressACL;

        /// <summary>
        /// -sadmin
        /// </summary>
        private bool suppressAdmin;

        /// <summary>
        ///  Suppress all warnings
        /// </summary>
        private bool suppressAllWarnings;

        /// <summary>
        /// -sadv
        /// </summary>
        private bool suppressADV;

        /// <summary>
        /// -sa
        /// </summary>
        private bool suppressAssemblies;

        /// <summary>
        /// -sdut
        /// </summary>
        private bool suppressDroppingUnrealTables;

        /// <summary>
        //  -sice:<ICE>
        /// </summary>
        private List<string> suppressedICEs;

        /// <summary>
        /// -sf
        /// </summary>
        private bool suppressFiles;

        /// <summary>
        /// -sh
        /// </summary>
        private bool suppressFileInfo;

        /// <summary>
        /// -sv
        /// </summary>
        private bool suppressIntermediateFileVersionCheck;

        /// <summary>
        /// -sl
        /// </summary>
        private bool suppressLayout;

        /// <summary>
        /// -sval
        /// </summary>
        private bool suppressMSIAndMSMValidation;

        /// <summary>
        /// -spsd
        /// </summary>
        private bool suppressPatchSequenceData;

        /// <summary>
        /// -sma
        /// </summary>
        private bool suppressProcessingMSIAsmTable;

        /// <summary>
        /// -ss
        /// </summary>
        private bool suppressSchemaValidation;

        /// <summary>
        /// -sui
        /// </summary>
        private bool suppressUI;

        /// <summary>
        //  -sw<N>
        /// </summary>
        private List<string> suppressWarnings;

        /// <summary>
        /// -ts
        /// </summary>
        private bool tagSectionId;

        /// <summary>
        /// -tsa
        /// </summary>
        private bool tagSectionIdAndGenerateWhenNull;

        /// <summary>
        /// Treat all warnings as errors
        /// </summary>
        private bool treatAllWarningsAsErrors;

        /// <summary>
        /// -wx [N]
        /// </summary>
        private List<int> treatWarningsAsErrors;

        /// <summary>
        //  -usf <output.xml>
        /// </summary>
        private string unreferencedSymbolsFile;

        /// <summary>
        /// -v
        /// </summary>
        private bool verbose;

        /// <summary>
        // -d<name>=<value>
        /// </summary>
        private StringDictionary wixVariables;

        /// <summary>
        /// -xo
        /// </summary>
        private bool xmlOutput;

        #endregion

        #region Public Properties

        /// <summary>
        /// The arguments as they would be passed on the command line
        /// </summary>
        /// <remarks>
        /// To allow for negative testing, checking for invalid combinations
        /// of arguments is not performed.
        /// </remarks>
        public override string Arguments
        {
            get
            {
                StringBuilder arguments = new StringBuilder(base.Arguments);

                // AllowIdenticalRows
                if (this.AllowIdenticalRows)
                {
                    arguments.Append(" -ai");
                }

                // AllowUnresolvedVariables
                if (this.AllowUnresolvedVariables)
                {
                    arguments.Append(" -au");
                }

                // BindPath
                if (!String.IsNullOrEmpty(this.BindPath))
                {
                    arguments.AppendFormat(@" -b ""{0}""", this.BindPath);
                }

                // BindFiles
                if (this.BindFiles)
                {
                    arguments.Append(" -bf");
                }

                // CabbingThreads
                if (0 < this.CabbingThreads)
                {
                    arguments.AppendFormat(" -ct {0}", this.CabbingThreads);
                }

                // CachedCabsPath
                if (!String.IsNullOrEmpty(this.CachedCabsPath))
                {
                    arguments.AppendFormat(@" -cc ""{0}""", this.CachedCabsPath);
                }

                // Cultures
                if (!String.IsNullOrEmpty(this.Cultures))
                {
                    arguments.AppendFormat(" -cultures:{0}", this.Cultures);
                }

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // FileVersion
                if (this.FileVersion)
                {
                    arguments.Append(" -fv");
                }

                // ICEs
                foreach (string ice in this.ICEs)
                {
                    arguments.AppendFormat(" -ice:{0}", ice);
                }

                // LocFiles
                foreach (string locFile in this.LocFiles)
                {
                    arguments.AppendFormat(@" -loc ""{0}""", locFile);
                }

                // NoTidy
                if (this.NoTidy)
                {
                    arguments.Append(" -notidy");
                }

                // ObjectFiles
                foreach (string objectFile in this.ObjectFiles)
                {
                    arguments.AppendFormat(@" ""{0}""", objectFile);
                }

                // OutputFile
                if (!String.IsNullOrEmpty(this.OutputFile))
                {
                    arguments.AppendFormat(@" -out ""{0}""", this.OutputFile);
                }

                // Pedantic
                if (true == this.Pedantic)
                {
                    arguments.Append(" -pedantic");
                }

                // ReuseCab
                if (this.ReuseCab)
                {
                    arguments.Append(" -reusecab");
                }

                // SuppressACL
                if (this.SuppressACL)
                {
                    arguments.Append(" -sacl");
                }

                // SuppressAdmin
                if (this.SuppressAdmin)
                {
                    arguments.Append(" -sadmin");
                }

                // SuppressADV
                if (this.SuppressADV)
                {
                    arguments.Append(" -sadv");
                }

                // SuppressAllWarnings
                if (this.SuppressAllWarnings)
                {
                    arguments.Append(" -sw");
                }

                // SuppressAssemblies
                if (this.SuppressAssemblies)
                {
                    arguments.Append(" -sa");
                }

                // SuppressDroppingUnrealTables
                if (this.SuppressDroppingUnrealTables)
                {
                    arguments.Append(" -sdut");
                }

                // SuppressedICEs
                foreach (string suppressedICE in this.SuppressedICEs)
                {
                    arguments.AppendFormat(" -sice:{0}", suppressedICE);
                }

                // SuppressFiles
                if (this.SuppressFiles)
                {
                    arguments.Append(" -sf");
                }

                // SuppressFileInfo
                if (this.SuppressFileInfo)
                {
                    arguments.Append(" -sh");
                }

                // SuppressIntermediateFileVersionCheck
                if (this.SuppressIntermediateFileVersionCheck)
                {
                    arguments.Append(" -sv");
                }

                // SuppressLayout
                if (this.SuppressLayout)
                {
                    arguments.Append(" -sl");
                }

                // SuppressPatchSequenceData
                if (this.SuppressPatchSequenceData)
                {
                    arguments.Append(" -spsd");
                }

                // SuppressMSIAndMSMValidation
                if (this.SuppressMSIAndMSMValidation)
                {
                    arguments.Append(" -sval");
                }

                // SuppressProcessingMSIAsmTable
                if (this.SuppressProcessingMSIAsmTable)
                {
                    arguments.Append(" -sma");
                }

                // SuppressSchemaValidation
                if (this.SuppressSchemaValidation)
                {
                    arguments.Append(" -ss");
                }

                // SuppressUI
                if (this.SuppressUI)
                {
                    arguments.Append(" -sui");
                }

                // SuppressWarnings
                foreach (string suppressWarning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(" -sw{0}", suppressWarning);
                }

                // TagSectionId
                if (this.TagSectionId)
                {
                    arguments.Append(" -ts");
                }

                // TagSectionIdAndGenerateWhenNull
                if (this.TagSectionIdAndGenerateWhenNull)
                {
                    arguments.Append(" -tsa");
                }

                // TreatAllWarningsAsErrors
                if (this.TreatAllWarningsAsErrors)
                {
                    arguments.Append(" -wx");
                }

                // Treat specific warnings as errors
                foreach (int warning in this.TreatWarningsAsErrors)
                {
                    arguments.AppendFormat(" -wx{0}", warning.ToString());
                }

                // UnreferencedSymbolsFile
                if (!String.IsNullOrEmpty(this.UnreferencedSymbolsFile))
                {
                    arguments.AppendFormat(@" -usf ""{0}""", this.UnreferencedSymbolsFile);
                }

                // VerboseOutput
                if (this.Verbose)
                {
                    arguments.Append(" -v");
                }

                // WixVariables
                foreach (string key in this.WixVariables.Keys)
                {
                    arguments.AppendFormat(" -d{0}={1}", key, this.WixVariables[key]);
                }

                // XmlOutput
                if (this.XmlOutput)
                {
                    arguments.Append(" -xo");
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// -ai
        /// </summary>
        public bool AllowIdenticalRows
        {
            get { return this.allowIdenticalRows; }
            set { this.allowIdenticalRows = value; }
        }

        /// <summary>
        /// -au
        /// </summary>
        public bool AllowUnresolvedVariables
        {
            get { return this.allowUnresolvedVariables; }
            set { this.allowUnresolvedVariables = value; }
        }

        /// <summary>
        /// -b
        /// </summary>
        public string BindPath
        {
            get { return this.bindPath; }
            set { this.bindPath = value; }
        }

        /// <summary>
        /// -bf
        /// </summary>
        public bool BindFiles
        {
            get { return this.bindFiles; }
            set { this.bindFiles = value; }
        }

        /// <summary>
        //  -ct <N>
        /// </summary>
        public int CabbingThreads
        {
            get { return this.cabbingThreads; }
            set { this.cabbingThreads = value; }
        }

        /// <summary>
        /// -cc
        /// </summary>
        public string CachedCabsPath
        {
            get { return this.cachedCabsPath; }
            set { this.cachedCabsPath = value; }
        }

        /// <summary>
        //  -cultures:<cultures>
        /// </summary>
        public string Cultures
        {
            get { return this.cultures; }
            set { this.cultures = value; }
        }

        /// <summary>
        /// -ext
        /// </summary>
        public List<string> Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        /// <summary>
        /// -fv
        /// </summary>
        public bool FileVersion
        {
            get { return this.fileVersion; }
            set { this.fileVersion = value; }
        }

        /// <summary>
        //  -ice:<ICE>
        /// </summary>
        public List<string> ICEs
        {
            get { return this.ices; }
            set { this.ices = value; }
        }

        /// <summary>
        //  -loc <loc.wxl>
        /// </summary>
        public List<string> LocFiles
        {
            get { return this.locFiles; }
            set { this.locFiles = value; }
        }

        /// <summary>
        /// -notidy
        /// </summary>
        public bool NoTidy
        {
            get { return this.noTidy; }
            set { this.noTidy = value; }
        }

        /// <summary>
        /// objectFile [objectFile ...]
        /// </summary>
        public List<string> ObjectFiles
        {
            get { return this.objectFiles; }
            set { this.objectFiles = value; }
        }

        /// <summary>
        /// -pedantic
        /// </summary>
        public bool Pedantic
        {
            get { return this.pedantic; }
            set { this.pedantic = value; }
        }

        /// <summary>
        /// -reusecab
        /// </summary>
        public bool ReuseCab
        {
            get { return this.reuseCab; }
            set { this.reuseCab = value; }
        }

        /// <summary>
        /// -sacl
        /// </summary>
        public bool SuppressACL
        {
            get { return this.suppressACL; }
            set { this.suppressACL = value; }
        }

        /// <summary>
        /// -sadmin
        /// </summary>
        public bool SuppressAdmin
        {
            get { return this.suppressAdmin; }
            set { this.suppressAdmin = value; }
        }

        /// <summary>
        /// -sadv
        /// </summary>
        public bool SuppressADV
        {
            get { return this.suppressADV; }
            set { this.suppressADV = value; }
        }

        /// <summary>
        ///  Suppress all warnings.
        /// </summary>
        public bool SuppressAllWarnings
        {
            get { return this.suppressAllWarnings; }
            set { this.suppressAllWarnings = value; }
        }

        /// <summary>
        /// -sa
        /// </summary>
        public bool SuppressAssemblies
        {
            get { return this.suppressAssemblies; }
            set { this.suppressAssemblies = value; }
        }

        /// <summary>
        /// -sdut
        /// </summary>
        public bool SuppressDroppingUnrealTables
        {
            get { return this.suppressDroppingUnrealTables; }
            set { this.suppressDroppingUnrealTables = value; }
        }

        /// <summary>
        //  -sice:<ICE>
        /// </summary>
        public List<string> SuppressedICEs
        {
            get { return this.suppressedICEs; }
            set { this.suppressedICEs = value; }
        }

        /// <summary>
        /// -sf
        /// </summary>
        public bool SuppressFiles
        {
            get { return this.suppressFiles; }
            set { this.suppressFiles = value; }
        }

        /// <summary>
        /// -sh
        /// </summary>
        public bool SuppressFileInfo
        {
            get { return this.suppressFileInfo; }
            set { this.suppressFileInfo = value; }
        }

        /// <summary>
        /// -sv
        /// </summary>
        public bool SuppressIntermediateFileVersionCheck
        {
            get { return this.suppressIntermediateFileVersionCheck; }
            set { this.suppressIntermediateFileVersionCheck = value; }
        }

        /// <summary>
        /// -sl
        /// </summary>
        public bool SuppressLayout
        {
            get { return this.suppressLayout; }
            set { this.suppressLayout = value; }
        }

        /// <summary>
        /// -sval
        /// </summary>
        public bool SuppressMSIAndMSMValidation
        {
            get { return this.suppressMSIAndMSMValidation; }
            set { this.suppressMSIAndMSMValidation = value; }
        }

        /// <summary>
        /// -spsd
        /// </summary>
        public bool SuppressPatchSequenceData
        {
            get { return this.suppressPatchSequenceData; }
            set { this.suppressPatchSequenceData = value; }
        }

        /// <summary>
        /// -sma
        /// </summary>
        public bool SuppressProcessingMSIAsmTable
        {
            get { return this.suppressProcessingMSIAsmTable; }
            set { this.suppressProcessingMSIAsmTable = value; }
        }

        /// <summary>
        /// -ss
        /// </summary>
        public bool SuppressSchemaValidation
        {
            get { return this.suppressSchemaValidation; }
            set { this.suppressSchemaValidation = value; }
        }

        /// <summary>
        /// -sui
        /// </summary>
        public bool SuppressUI
        {
            get { return this.suppressUI; }
            set { this.suppressUI = value; }
        }

        /// <summary>
        //  -sw<N>
        /// </summary>
        public List<string> SuppressWarnings
        {
            get { return this.suppressWarnings; }
            set { this.suppressWarnings = value; }
        }

        /// <summary>
        /// -ts
        /// </summary>
        public bool TagSectionId
        {
            get { return this.tagSectionId; }
            set { this.tagSectionId = value; }
        }

        /// <summary>
        /// -tsa
        /// </summary>
        public bool TagSectionIdAndGenerateWhenNull
        {
            get { return this.tagSectionIdAndGenerateWhenNull; }
            set { this.tagSectionIdAndGenerateWhenNull = value; }
        }

        /// <summary>
        /// Treat all warnings as errors.
        /// </summary>
        public bool TreatAllWarningsAsErrors
        {
            get { return this.treatAllWarningsAsErrors; }
            set { this.treatAllWarningsAsErrors = value; }
        }

        /// <summary>
        /// -wx [N]
        /// </summary>
        public List<int> TreatWarningsAsErrors
        {
            get { return this.treatWarningsAsErrors; }
            set { this.treatWarningsAsErrors = value; }
        }

        /// <summary>
        //  -usf <output.xml>
        /// </summary>
        public string UnreferencedSymbolsFile
        {
            get { return this.unreferencedSymbolsFile; }
            set { this.unreferencedSymbolsFile = value; }
        }

        /// <summary>
        /// -v
        /// </summary>
        public bool Verbose
        {
            get { return this.verbose; }
            set { this.verbose = value; }
        }

        /// <summary>
        // -d<name>=<value>
        /// </summary>
        public StringDictionary WixVariables
        {
            get { return this.wixVariables; }
            set { this.wixVariables = value; }
        }

        /// <summary>
        /// -xo
        /// </summary>
        public bool XmlOutput
        {
            get { return this.xmlOutput; }
            set { this.xmlOutput = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values
        /// </summary>
        public override void SetDefaultArguments()
        {
            this.AllowIdenticalRows = false;
            this.AllowUnresolvedVariables = false;
            this.BindPath = String.Empty;
            this.BindFiles = false;
            this.CabbingThreads = 0;
            this.CachedCabsPath = String.Empty;
            this.Cultures = String.Empty;
            this.Extensions = new List<string>();
            this.FileVersion = false;
            this.ICEs = new List<string>();
            this.LocFiles = new List<string>();
            this.NoTidy = false;
            this.ObjectFiles = new List<string>();
            this.OutputFile = String.Empty;
            this.Pedantic = false;
            this.ReuseCab = false;
            this.SuppressACL = false;
            this.SuppressAdmin = false;
            this.SuppressADV = false;
            this.SuppressAllWarnings = false;
            this.SuppressAssemblies = false;
            this.SuppressDroppingUnrealTables = false;
            this.SuppressedICEs = new List<string>();
            this.SuppressFiles = false;
            this.SuppressFileInfo = false;
            this.SuppressIntermediateFileVersionCheck = false;
            this.SuppressLayout = false;
            this.SuppressPatchSequenceData = false;
            this.SuppressMSIAndMSMValidation = false;
            this.SuppressProcessingMSIAsmTable = false;
            this.SuppressSchemaValidation = false;
            this.SuppressUI = false;
            this.SuppressWarnings = new List<string>();
            this.TagSectionId = false;
            this.TagSectionIdAndGenerateWhenNull = false;
            this.TreatAllWarningsAsErrors = false;
            this.TreatWarningsAsErrors = new List<int>();
            this.UnreferencedSymbolsFile = String.Empty;
            this.Verbose = false;
            this.WixVariables = new StringDictionary();
            this.XmlOutput = false;
        }
    }
}
