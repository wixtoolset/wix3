// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The different command line arguments that Dark supports.
    /// </summary>
    public partial class Dark : WixTool
    {
        #region Fields
        /// <summary>
        /// The path to export the binaries to.
        /// </summary>
        private string binaryPath;

        /// <summary>
        /// The extensions to use with the �ext option.
        /// </summary>
        private List<string> extensions;

        /// <summary>
        /// -notidy option.
        /// </summary>
        private bool noTidy;

         /// <summary>
        /// The msi Input file.
        /// </summary>
        private string inputFile;

        /// <summary>
        /// -sdet option.
        /// </summary>
        private bool suppressDroppingEmptyTables;
        
        /// <summary>
        /// -sras option.
        /// </summary>
        private bool suppressRelativeActionSequences;

        /// <summary>
        /// -sui option.
        /// </summary>
        private bool suppressUITables;

        /// <summary>
        /// The list of warning IDs to be ignored.
        /// </summary>
        private List<int> suppressWarnings;

        /// <summary>
        /// -wx option.
        /// </summary>
        private bool treatWarningsAsErrors;

        /// <summary>
        /// -v option.
        /// </summary>
        private bool verbose;

        /// <summary>
        /// -xo option.
        /// </summary>
        private bool xmlOutput;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the arguments as they would be passed on the command line.
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
                
                // InputFile
                if (!string.IsNullOrEmpty(this.InputFile))
                {
                    arguments.AppendFormat(@" ""{0}""", this.InputFile);
                }

                // OutputFile
                if (!string.IsNullOrEmpty(this.OutputFile))
                {
                    arguments.AppendFormat(@" ""{0}""", this.OutputFile);
                }

                // Extensions     
                foreach (string extension in this.Extensions)
                {
                    arguments.AppendFormat(@" -ext ""{0}""", extension);
                }

                // NoTidy
                if (this.NoTidy)
                {
                    arguments.Append(" -notidy");
                }

                // SDET
                if (this.SuppressDroppingEmptyTables)
                {
                    arguments.Append(" -sdet");
                }

                // SRAS
                if (this.SuppressRelativeActionSequences)
                {
                    arguments.Append(" -sras");
                }

                // SUI
                if (this.SuppressUITables)
                {
                    arguments.Append(" -sui");
                }

                // SW
                foreach (int warning in this.SuppressWarnings)
                {
                    arguments.AppendFormat(@" -sw""{0}""", warning);
                }

                // V
                if (this.Verbose)
                {
                    arguments.Append(" -v");
                }

                // WX
                if (this.TreatWarningsAsErrors)
                {
                    arguments.Append(" -wx");
                }

                // X
                if (!string.IsNullOrEmpty(this.BinaryPath))
                {
                    arguments.AppendFormat(@" -x ""{0}""", this.BinaryPath);
                }

                // XO
                if (this.XmlOutput)
                {
                    arguments.Append(" -xo");
                }

                return arguments.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the path where the binaries will be exported.
        /// </summary>
        public string BinaryPath
        {
            get
            {
                return this.binaryPath;
            }
            set
            {
                this.binaryPath = value;
            }
        }

        /// <summary>
        /// Gets or sets the extensions to use with the �ext option.
        /// </summary>
        public List<string> Extensions
        {
            get
            {
                return this.extensions;
            }
            set
            {
                this.extensions = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -notidy option is used or not.
        /// </summary>
        public bool NoTidy
        {
            get
            {
                return this.noTidy;
            }
            set
            {
                this.noTidy = value;
            }
        }

        /// <summary>
        /// Gets or sets the msi input file.
        /// </summary>
        public string InputFile
        {
            get
            {
                return this.inputFile;
            }

            set
            {
                this.inputFile = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -sdet option is used or not.
        /// </summary>
        public bool SuppressDroppingEmptyTables
        {
            get
            {
                return this.suppressDroppingEmptyTables;
            }
            set
            {
                this.suppressDroppingEmptyTables = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -sras option is used or not.
        /// </summary>
        public bool SuppressRelativeActionSequences
        {
            get
            {
                return this.suppressRelativeActionSequences;
            }
            set
            {
                this.suppressRelativeActionSequences = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -sui option is used or not.
        /// </summary>
        public bool SuppressUITables
        {
            get
            {
                return this.suppressUITables;
            }
            set
            {
                this.suppressUITables = value;
            }
        }

        /// <summary>
        /// Gets or sets the warning numbers that should be ignored.
        /// </summary>
        public List<int> SuppressWarnings
        {
            get
            {
                return this.suppressWarnings;
            }
            set
            {
                this.suppressWarnings = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -wx option is used or not.
        /// </summary>
        public bool TreatWarningsAsErrors
        {
            get
            {
                return this.treatWarningsAsErrors;
            }
            set
            {
                this.treatWarningsAsErrors = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -v option is used or not.
        /// </summary>
        public bool Verbose
        {
            get
            {
                return this.verbose;
            }
            set
            {
                this.verbose = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether -xo option is used or not.
        /// </summary>
        public bool XmlOutput
        {
            get
            {
                return this.xmlOutput;
            }
            set
            {
                this.xmlOutput = value;
            }
        }

        #endregion

        /// <summary>
        /// Clears all of the assigned arguments and resets them to the default values.
        /// </summary>
        public override void SetDefaultArguments()
        {
            this.BinaryPath = string.Empty;
            this.Extensions = new List<string>();
            this.NoTidy = false;
            this.InputFile = string.Empty;
            this.SuppressDroppingEmptyTables = false;
            this.SuppressRelativeActionSequences = false;
            this.SuppressUITables = false;
            this.SuppressWarnings = new List<int>();
            this.TreatWarningsAsErrors = false;
            this.Verbose = false;
            this.XmlOutput = false;
        }
    }
}
