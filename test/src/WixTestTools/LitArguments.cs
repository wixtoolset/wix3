// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Lit arguments
    /// </summary>
    public partial class Lit
    {
        #region Private Members

        /// <summary>
        /// -b
        /// </summary>
        private string bindPath;

        /// <summary>
        /// -bf
        /// </summary>
        private bool bindFiles;

        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;

        /// <summary>
        //  -loc <loc.wxl>
        /// </summary>
        private List<string> locFiles;

        /// <summary>
        /// objectFile [objectFile ...]
        /// </summary>
        private List<string> objectFiles;

        /// <summary>
        /// -pedantic
        /// </summary>
        private bool pedantic;

        /// <summary>
        /// @responsefile
        /// </summary>
        private string responseFile;

        /// <summary>
        /// -ss
        /// </summary>
        private bool suppressSchemaValidation;

        /// <summary>
        /// -sv
        /// </summary>
        private bool suppressIntermediateFileVersionCheck;

        /// <summary>
        //  -sw[N]
        /// </summary>
        private List<int> suppressWarnings;

        /// <summary>
        ///  Suppress all warnings
        /// </summary>
        private bool suppressAllWarnings;

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

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // LocFile
                foreach (string locFile in this.LocFiles)
                {
                    arguments.AppendFormat(@" -loc ""{0}""", locFile);
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

                // Response File
                if (!String.IsNullOrEmpty(this.ResponseFile))
                {
                    arguments.AppendFormat(" @{0}", this.ResponseFile);
                }

                // SuppressSchemaValidation
                if (this.SuppressSchemaValidation)
                {
                    arguments.Append(" -ss");
                }

                // SuppressVersionChecking
                if (this.SuppressIntermediateFileVersionCheck)
                {
                    arguments.Append(" -sv");
                }

                // SuppressAllWarnings
                if (this.SuppressAllWarnings)
                {
                    arguments.Append(" -sw");
                }

                // SuppressWarnings
                foreach (int warning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(" -sw{0}", warning.ToString());
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
                if (this.VerboseOutput)
                {
                    arguments.Append(" -v");
                }

                return arguments.ToString();
            }
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
        /// -ext
        /// </summary>
        public List<string> Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
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
        /// @responsefile
        /// </summary>
        public string ResponseFile
        {
            get { return this.responseFile; }
            set { this.responseFile = value; }
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
        /// -sv
        /// </summary>
        public bool SuppressIntermediateFileVersionCheck
        {
            get { return this.suppressIntermediateFileVersionCheck; }
            set { this.suppressIntermediateFileVersionCheck = value; }
        }

        /// <summary>
        /// Suppress all warnings
        /// </summary>
        public bool SuppressAllWarnings
        {
            get { return this.suppressAllWarnings; }
            set { this.suppressAllWarnings = value; }
        }

        /// <summary>
        //  -sw[N]
        /// </summary>
        public List<int> SuppressWarnings
        {
            get { return this.suppressWarnings; }
            set { this.suppressWarnings = value; }
        }

        /// <summary>
        /// Treat all warnings as errors
        /// </summary>
        public bool TreatAllWarningsAsErrors
        {
            get { return this.treatAllWarningsAsErrors; }
            set { this.treatAllWarningsAsErrors = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public List<int> TreatWarningsAsErrors
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
            this.BindPath = String.Empty;
            this.BindFiles = false;
            this.Extensions = new List<string>();
            this.LocFiles = new List<string>();
            this.ObjectFiles = new List<string>();
            this.OutputFile = String.Empty;
            this.Pedantic = false;
            this.SuppressIntermediateFileVersionCheck = false;
            this.SuppressSchemaValidation = false;
            this.SuppressAllWarnings = false;
            this.SuppressWarnings = new List<int>();
            this.TreatAllWarningsAsErrors = false;
            this.TreatWarningsAsErrors = new List<int>();
            this.VerboseOutput = false;
        }
    }
}
