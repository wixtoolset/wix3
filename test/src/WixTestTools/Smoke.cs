//-----------------------------------------------------------------------
// <copyright file="Smoke.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Wraps the WiX Smoke tool</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    /// <summary>
    /// Wraps the WiX Smoke tool
    /// </summary>
    public partial class Smoke : WixTool
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Smoke()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool</param>
        public Smoke(string workingDirectory)
            : base("smoke.exe", workingDirectory)
        {
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return string.Empty; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Validator"; }
        }
    }
}
