//-----------------------------------------------------------------------
// <copyright file="InsigniaArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with Insignia arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Insignia arguments
    /// </summary>
    public partial class Insignia
    {
        #region Private Members

        /// <summary>
        /// databaseFile [databaseFile ...]
        /// </summary>
        private List<string> databaseFiles;

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

                // DatabaseFiles
                foreach (string databaseFile in this.DatabaseFiles)
                {
                    arguments.AppendFormat(@" ""{0}""", databaseFile);
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
        /// databaseFile [databaseFile ...]
        /// </summary>
        public List<string> DatabaseFiles
        {
            get { return this.databaseFiles; }
            set { this.databaseFiles = value; }
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
            this.DatabaseFiles = new List<string>();
            this.SuppressAllWarnings = false;
            this.SuppressWarnings = new List<int>();
            this.TreatAllWarningsAsErrors = false;
            this.TreatWarningsAsErrors = new List<int>();
            this.VerboseOutput = false;
            this.NoLogo = false;
        }
    }
}
