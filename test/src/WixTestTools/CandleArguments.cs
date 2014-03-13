//-----------------------------------------------------------------------
// <copyright file="CandleArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with Candle arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Candle arguments.
    /// </summary>
    public partial class Candle
    {
        #region Private Members

        /// <summary>
        /// -fips
        /// </summary>
        private bool fips;

        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;

        /// <summary>
        /// -zs
        /// </summary>
        private bool onlyValidateDocuments;

        /// <summary>
        /// -pedantic
        /// </summary>
        private bool pedantic;

        /// <summary>
        /// <![CDATA[-p<file>]]>
        /// </summary>
        private string preProcessFile;

        /// <summary>
        /// <![CDATA[-d<name>=<value>]]>
        /// </summary>
        private Dictionary<string, string> preProcessorParams;

        /// <summary>
        ///  <![CDATA[-I<dir>]]>
        /// </summary>
        private List<string> includeSearchPaths;

        /// <summary>
        /// sourceFile [sourceFile ...]
        /// </summary>
        private List<string> sourceFiles;

        /// <summary>
        ///  Suppress all warnings
        /// </summary>
        private bool suppressAllWarnings;

        /// <summary>
        /// -sfdvital
        /// </summary>
        private bool suppressMarkingVitalDefault;

        /// <summary>
        /// -ss
        /// </summary>
        private bool suppressSchemaValidation;

        /// <summary>
        ///  -sw[N]
        /// </summary>
        private List<int> suppressWarnings;

        /// <summary>
        /// -trace
        /// </summary>
        private bool trace;

        /// <summary>
        /// -wx[N]
        /// </summary>
        private List<int> treatWarningsAsErrors;

        /// <summary>
        /// Treat all warnings as errors
        /// </summary>
        private bool treatAllWarningsAsErrors;

        /// <summary>
        /// -v
        /// </summary>
        private bool verbose;

        #endregion

        #region Public Properties

        /// <summary>
        /// The arguments as they would be passed on the command line.
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

                // FIPS
                if (this.FIPS)
                {
                    arguments.Append(" -fips");
                }

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // OnlyValidateDocuments
                if (this.OnlyValidateDocuments)
                {
                    arguments.Append(" -zs");
                }

                // OutputPath
                if (!String.IsNullOrEmpty(this.OutputFile))
                {
                    // WiX requires that we add extra backslashes to the end of a directory path
                    if (this.OutputFile.EndsWith(@"\") && !this.OutputFile.EndsWith(@"\\"))
                    {
                        arguments.AppendFormat(@" -out ""{0}\""", this.OutputFile);
                    }
                    else
                    {
                        arguments.AppendFormat(@" -out ""{0}""", this.OutputFile);
                    }
                }

                // Pedantic
                if (this.Pedantic)
                {
                    arguments.Append(" -pedantic");
                }

                // PreProcessFile
                if (!String.IsNullOrEmpty(this.PreProcessFile))
                {
                    arguments.AppendFormat(@" -p""{0}""", this.PreProcessFile);
                }

                // PreProcessorParams
                foreach (string key in this.PreProcessorParams.Keys)
                {
                    arguments.AppendFormat(" -d{0}={1}", key, this.PreProcessorParams[key]);
                }

                // IncludeSearchPaths
                foreach (string searchPath in this.IncludeSearchPaths)
                {
                    arguments.AppendFormat(@" -I""{0}""", searchPath);
                }

                // SourceFiles
                foreach (string sourceFile in this.SourceFiles)
                {
                    arguments.AppendFormat(@" ""{0}""", sourceFile);
                }

                // SuppressMarkingVitalDefault
                if (this.SuppressMarkingVitalDefault)
                {
                    arguments.Append(" -sfdvital");
                }

                // SuppressAllWarnings
                if (this.SuppressAllWarnings)
                {
                    arguments.Append(" -sw");
                }

                // SuppressSchemaValidation
                if (true == this.suppressSchemaValidation)
                {
                    arguments.Append(" -ss");
                }

                // SuppressWarnings
                foreach (int warning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(" -sw{0}", warning.ToString());
                }

                // Trace
                if (this.Trace)
                {
                    arguments.Append(" -trace");
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
                
                // VerboseOutput
                if (this.Verbose)
                {
                    arguments.Append(" -v");
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// Enable FIPS compliant algorithms.
        /// </summary>
        public bool FIPS
        {
            get { return this.fips; }
            set { this.fips = value; }
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
        /// -zs
        /// </summary>
        public bool OnlyValidateDocuments
        {
            get { return this.onlyValidateDocuments; }
            set { this.onlyValidateDocuments = value; }
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
        /// <![CDATA[-p<file>]]>
        /// </summary>
        public string PreProcessFile
        {
            get { return this.preProcessFile; }
            set { this.preProcessFile = value; }
        }

        /// <summary>
        /// <![CDATA[-d<name>=<value>]]>  
        /// </summary>
        public Dictionary<string, string> PreProcessorParams
        {
            get { return this.preProcessorParams; }
            set { this.preProcessorParams = value; }
        }

        /// <summary>
        /// <![CDATA[-I<dir>]]> 
        /// </summary>
        public List<string> IncludeSearchPaths
        {
            get { return this.includeSearchPaths; }
            set { this.includeSearchPaths = value; }
        }

        /// <summary>
        /// sourceFile [sourceFile ...]
        /// </summary>
        public List<string> SourceFiles
        {
            get { return this.sourceFiles; }
            set { this.sourceFiles = value; }
        }

        /// <summary>
        ///  Suppress Marking files as vital by default.
        /// </summary>
        public bool SuppressMarkingVitalDefault
        {
            get { return this.suppressMarkingVitalDefault; }
            set { this.suppressMarkingVitalDefault = value; }
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
        /// -ss
        /// </summary>
        public bool SuppressSchemaValidation
        {
            get { return this.suppressSchemaValidation; }
            set { this.suppressSchemaValidation = value; }
        }

        /// <summary>
        /// -sw[N]
        /// </summary>
        public List<int> SuppressWarnings
        {
            get { return this.suppressWarnings; }
            set { this.suppressWarnings = value; }
        }

        /// <summary>
        /// -trace
        /// </summary>
        public bool Trace
        {
            get { return this.trace; }
            set { this.trace = value; }
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
        /// -wx[N]
        /// </summary>
        public List<int> TreatWarningsAsErrors
        {
            get { return this.treatWarningsAsErrors; }
            set { this.treatWarningsAsErrors = value; }
        }

        /// <summary>
        /// -v
        /// </summary>
        public bool Verbose
        {
            get { return this.verbose; }
            set { this.verbose = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values.
        /// </summary>
        public override void SetDefaultArguments()
        {
            this.FIPS = false;
            this.Extensions = new List<string>();
            this.OnlyValidateDocuments = false;
            this.OutputFile = String.Empty;
            this.Pedantic = false;
            this.PreProcessFile = String.Empty;
            this.PreProcessorParams = new Dictionary<string, string>();
            this.IncludeSearchPaths = new List<string>();
            this.SourceFiles = new List<string>();
            this.SuppressAllWarnings = false;
            this.SuppressSchemaValidation = false;
            this.SuppressWarnings = new List<int>();
            this.Trace = false;
            this.TreatAllWarningsAsErrors = false;
            this.TreatWarningsAsErrors = new List<int>();
            this.Verbose = false;
        }
    }
}
