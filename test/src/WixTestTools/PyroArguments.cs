// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Pyro arguments
    /// </summary>
    public partial class Pyro
    {
        #region Private Members

        /// <summary>
        /// [-t baseline wixTransform]
        /// </summary>
        private StringDictionary baselines;

        /// <summary>
        /// -cc
        /// </summary>
        private List<string> cachedCabs;

        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;

        /// <summary>
        /// inputFile
        /// </summary>
        private string inputFile;

        /// <summary>
        /// -notidy
        /// </summary>
        private bool noTidy;

        /// <summary>
        /// -reusecab
        /// </summary>
        private bool reuseCab;

        /// <summary>
        /// -sa
        /// </summary>
        private bool suppressAssemblies;

        /// <summary>
        /// -sf
        /// </summary>
        private bool suppressFiles;

        /// <summary>
        /// -sh
        /// </summary>
        private bool suppressFileInfo;

        /// <summary>
        //  -sw<N>
        /// </summary>
        private List<string> suppressWarnings;

        /// <summary>
        /// -wx
        /// </summary>
        private bool treatWarningsAsErrors;

        /// <summary>
        /// -v
        /// </summary>
        private bool verboseOutput;

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

                // Baselines
                foreach (string key in this.Baselines.Keys)
                {
                    arguments.AppendFormat(@" -t ""{0}"" ""{1}""", this.Baselines[key], key);
                }

                // CacheCabs
                foreach (string cab in this.CachedCabs)
                {
                    arguments.AppendFormat(" -cc {0}", cab);
                }

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // InputFile
                if (!String.IsNullOrEmpty(this.InputFile))
                {
                    arguments.AppendFormat(@" ""{0}"" ", this.InputFile);
                }

                // NoTidy
                if (this.NoTidy)
                {
                    arguments.Append(" -notidy");
                }

                // OutputFile
                if (!String.IsNullOrEmpty(this.OutputFile))
                {
                    arguments.AppendFormat(@" -out ""{0}""", this.OutputFile);
                }

                // ReuseCab
                if (this.ReuseCab)
                {
                    arguments.Append(" -reusecab");
                }

                // SuppressAssemblies
                if (this.SuppressAssemblies)
                {
                    arguments.Append(" -sa");
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

                // SuppressWarnings
                foreach (string suppressWarning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(" -sw{0}", suppressWarning);
                }

                // TreatWarningsAsErrors
                if (this.TreatWarningsAsErrors)
                {
                    arguments.Append(" -wx");
                }

                // VerboseOutput
                if (this.VerboseOutput)
                {
                    arguments.Append(" -v");
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// [-t baseline wixTransform]
        /// </summary>
        /// <remarks>
        /// The key is the path to the transform file, the value is the baseline Id
        /// </remarks>
        public StringDictionary Baselines
        {
            get { return this.baselines; }
            set { this.baselines = value; }
        }

        /// <summary>
        /// -cc
        /// </summary>
        public List<string> CachedCabs
        {
            get { return this.cachedCabs; }
            set { this.cachedCabs = value; }
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
        /// inputFile
        /// </summary>
        public string InputFile
        {
            get { return this.inputFile; }
            set { this.inputFile = value; }
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
        /// -reusecab
        /// </summary>
        public bool ReuseCab
        {
            get { return this.reuseCab; }
            set { this.reuseCab = value; }
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
        //  -sw<N>
        /// </summary>
        public List<string> SuppressWarnings
        {
            get { return this.suppressWarnings; }
            set { this.suppressWarnings = value; }
        }

        /// <summary>
        /// -wx
        /// </summary>
        public bool TreatWarningsAsErrors
        {
            get { return this.treatWarningsAsErrors; }
            set { this.treatWarningsAsErrors = value; }
        }

        /// <summary>
        /// -v
        /// </summary>
        public bool VerboseOutput
        {
            get { return this.verboseOutput; }
            set { this.verboseOutput = value; }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values
        /// </summary>
        public override void SetDefaultArguments()
        {
            this.Baselines = new StringDictionary();
            this.CachedCabs = new List<string>();
            this.Extensions = new List<string>();
            this.InputFile = String.Empty;
            this.NoTidy = false;
            this.OutputFile = String.Empty;
            this.ReuseCab = false;
            this.SuppressAssemblies = false;
            this.SuppressFiles = false;
            this.SuppressFileInfo = false;
            this.SuppressWarnings = new List<string>();
            this.TreatWarningsAsErrors = false;
            this.VerboseOutput = false;
        }
    }
}
