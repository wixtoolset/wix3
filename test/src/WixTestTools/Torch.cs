//-----------------------------------------------------------------------
// <copyright file="Torch.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Torch</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    /// <summary>
    /// A class that wraps Torch
    /// </summary>
    public partial class Torch : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory
        /// </summary>
        public Torch()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool</param>
        public Torch(string workingDirectory)
            : base("torch.exe", workingDirectory)
        {
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return ".mst"; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Transform Builder"; }
        }
    }
}
