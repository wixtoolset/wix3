//-----------------------------------------------------------------------
// <copyright file="Pyro.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Pyro</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A class that wraps Pyro
    /// </summary>
    public partial class Pyro : WixTool
    {
        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Patch Builder"; }
        }

        /// <summary>
        /// Constructor that uses the current directory as the working directory
        /// </summary>
        public Pyro()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool</param>
        public Pyro(string workingDirectory)
            : base("pyro.exe", workingDirectory)
        {
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return ".msp"; }
        }
    }
}
