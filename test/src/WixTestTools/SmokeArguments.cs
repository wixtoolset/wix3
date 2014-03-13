//-----------------------------------------------------------------------
// <copyright file="SmokeArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with Smoke arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Smoke arguments
    /// </summary>
    public partial class Smoke
    {
        #region Private Members

        /// <summary>
        /// -cub
        /// </summary>
        private List<string> cubFiles;

        /// <summary>
        /// databaseFile [databaseFile ...]
        /// </summary>
        private List<string> databaseFiles;
        
        /// <summary>
        /// -ext
        /// </summary>
        private List<string> extensions;
        
        /// <summary>
        /// -nodefault
        /// </summary>
        private bool noDefault;
        
        /// <summary>
        /// -notidy
        /// </summary>
        private bool noTidy;
        
        /// <summary>
        //  -sice:<ICE>
        /// </summary>
        private List<string> suppressedICEs;
        
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

                // CubFiles
                foreach (string cubFile in this.CubFiles)
                {
                    arguments.AppendFormat(@" -cub ""{0}""", cubFile);
                }

                // DatabaseFiles
                foreach (string databaseFile in this.DatabaseFiles)
                {
                    arguments.AppendFormat(" \"{0}\"", databaseFile);
                }

                // Extensions
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // NoDefault
                if (this.NoDefault)
                {
                    arguments.Append(" -nodefault");
                }

                // NoTidy
                if (this.NoTidy)
                {
                    arguments.Append(" -notidy");
                }

                // SuppressedICEs
                foreach (string suppressedICE in this.SuppressedICEs)
                {
                    arguments.AppendFormat(" -sice:{0}", suppressedICE);
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
        /// -cub
        /// </summary>
        public List<string> CubFiles
        {
            get { return this.cubFiles; }
            set { this.cubFiles = value; }
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
        /// -ext
        /// </summary>
        public List<string> Extensions
        {
            get { return this.extensions; }
            set { this.extensions = value; }
        }

        /// <summary>
        /// -nodefault
        /// </summary>
        public bool NoDefault
        {
            get { return this.noDefault; }
            set { this.noDefault = value; }
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
        //  -sice:<ICE>
        /// </summary>
        public List<string> SuppressedICEs
        {
            get { return this.suppressedICEs; }
            set { this.suppressedICEs = value; }
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
            this.CubFiles = new List<string>();
            this.DatabaseFiles = new List<string>();
            this.Extensions = new List<string>();
            this.NoDefault = false;
            this.NoTidy = false;
            this.SuppressedICEs = new List<string>();
            this.SuppressWarnings = new List<string>();
            this.TreatWarningsAsErrors = false;
            this.VerboseOutput = false;
        }
    }
}
