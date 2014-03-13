//-----------------------------------------------------------------------
// <copyright file="TorchArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Fields, properties and methods for working with Torch arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Fields, properties and methods for working with Torch arguments
    /// </summary>
    public partial class Torch
    {
        #region Private Members

        /// <summary>
        /// -notidy
        /// </summary>
        private bool noTidy;

        /// <summary>
        /// -pedantic
        /// </summary>
        private bool pedantic;

        /// <summary>
        /// -p
        /// </summary>
        private bool preserveUnmodified;

        /// <summary>
        //   -sw<N>
        /// </summary>
        private List<string> suppressWarnings;

        /// <summary>
        /// targetInput
        /// </summary>
        private string targetInput;

        /// <summary>
        /// -wx
        /// </summary>
        private bool treatWarningsAsErrors;

        /// <summary>
        /// updatedInput
        /// </summary>
        private string updatedInput;

        /// <summary>
        /// -v
        /// </summary>
        private bool verboseOutput;

        /// <summary>
        /// -xi
        /// </summary>
        private bool xmlInput;

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

                // pedantic
                if (this.Pedantic)
                {
                    arguments.AppendFormat(" -pedantic");
                }

                // PreserveUnmodified
                if (this.PreserveUnmodified)
                {
                    arguments.AppendFormat(" -p");
                }

                // SuppressWarnings
                foreach (string suppressWarning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(" -sw{0}", suppressWarning);
                }

                // TargetInput
                if (!String.IsNullOrEmpty(this.TargetInput))
                {
                    arguments.AppendFormat(@" ""{0}""", this.TargetInput);
                }

                // TreatWarningsAsErrors
                if (this.TreatWarningsAsErrors)
                {
                    arguments.Append(" -wx");
                }

                // UpdatedInput
                if (!String.IsNullOrEmpty(this.UpdatedInput))
                {
                    arguments.AppendFormat(@" ""{0}""", this.UpdatedInput);
                }

                // VerboseOutput
                if (this.VerboseOutput)
                {
                    arguments.Append(" -v");
                }

                // XmlInput
                if (this.XmlInput)
                {
                    arguments.Append(" -xi");
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
        /// -notidy
        /// </summary>
        public bool NoTidy
        {
            get { return this.noTidy; }
            set { this.noTidy = value; }
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
        /// -p
        /// </summary>
        public bool PreserveUnmodified
        {
            get { return this.preserveUnmodified; }
            set { this.preserveUnmodified = value; }
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
        /// targetInput
        /// </summary>
        public string TargetInput
        {
            get { return this.targetInput; }
            set { this.targetInput = value; }
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
        /// updatedInput
        /// </summary>
        public string UpdatedInput
        {
            get { return this.updatedInput; }
            set { this.updatedInput = value; }
        }

        /// <summary>
        /// -v
        /// </summary>
        public bool VerboseOutput
        {
            get { return this.verboseOutput; }
            set { this.verboseOutput = value; }
        }

        /// <summary>
        /// -xi
        /// </summary>
        public bool XmlInput
        {
            get { return this.xmlInput; }
            set { this.xmlInput = value; }
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
            this.NoTidy = false;
            this.OutputFile = String.Empty;
            this.PreserveUnmodified = false;
            this.SuppressWarnings = new List<string>();
            this.TargetInput = String.Empty;
            this.TreatWarningsAsErrors = false;
            this.UpdatedInput = String.Empty;
            this.VerboseOutput = false;
            this.XmlInput = false;
            this.XmlOutput = false;
        }
    }
}

