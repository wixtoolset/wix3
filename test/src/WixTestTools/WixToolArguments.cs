//-----------------------------------------------------------------------
// <copyright file="WixToolArguments.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Generic Fields, properties and methods for working with WixTool arguments
// </summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Generic Fields, properties and methods for working with WixTool arguments
    /// </summary>
    public abstract partial class WixTool
    {
        #region Private Members

        /// <summary>
        /// -?
        /// </summary>
        private bool help;

        /// <summary>
        /// -nologo
        /// </summary>
        private bool noLogo;

        /// <summary>
        /// Any other arguments
        /// </summary>
        private string otherArguments;

        /// <summary>
        /// -out outputFile
        /// </summary>
        private string outputFile;

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
                StringBuilder arguments = new StringBuilder();

                // Help
                if (this.Help)
                {
                    arguments.Append(" -?");
                }

                // NoLogo
                if (this.NoLogo)
                {
                    arguments.Append(" -nologo");
                }

                // OutputPath can't be generated here because the tools have different ways of
                // specifying the output path on the command line

                // OtherArguments
                if (!String.IsNullOrEmpty(this.OtherArguments))
                {
                    arguments.AppendFormat(" {0}", this.otherArguments);
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// -?
        /// </summary>
        public virtual bool Help
        {
            get { return this.help; }
            set { this.help = value; }
        }

        /// <summary>
        /// -nologo
        /// </summary>
        public virtual bool NoLogo
        {
            get { return this.noLogo; }
            set { this.noLogo = value; }
        }

        /// <summary>
        /// -out outputFile
        /// </summary>
        public string OutputFile
        {
            get { return (this.outputFile ?? String.Empty); }
            set { this.outputFile = value; }
        }

        /// <summary>
        /// Any other arguments
        /// </summary>
        public string OtherArguments
        {
            get { return this.otherArguments; }
            set { this.otherArguments = value; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public abstract string ToolDescription
        {
            get;
        }

        #endregion


        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values
        /// </summary>
        public abstract void SetDefaultArguments();

        /// <summary>
        /// Clears all of the common arguments and resets them to the default values
        /// </summary>
        private void SetBaseDefaultArguments()
        {
            this.Help = false;
            this.NoLogo = true;
            this.OtherArguments = String.Empty;
        }
    }
}
