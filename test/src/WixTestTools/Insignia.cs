//-----------------------------------------------------------------------
// <copyright file="Insignia.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Insignia</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// A class that wraps Insignia
    /// </summary>
    public partial class Insignia : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory
        /// </summary>
        public Insignia()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool</param>
        public Insignia(string workingDirectory)
            : base("insignia.exe", workingDirectory)
        {
        }


        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return String.Empty; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Inscriber"; }
        }
    }
}
