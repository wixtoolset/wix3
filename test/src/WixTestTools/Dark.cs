//-----------------------------------------------------------------------
// <copyright file="Dark.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Dark</summary>
//-----------------------------------------------------------------------
namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Xml;
    using WixTest.Utilities;

    /// <summary>
    /// Dark tool class.
    /// </summary>
    public partial class Dark : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory.
        /// </summary>
        public Dark()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool.</param>
        public Dark(string workingDirectory)
            : base("dark.exe", workingDirectory)
        {
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return ".wxs"; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Decompiler"; }
        }

        /// <summary>
        /// Sets the OutputFile to a default value if it is not set 
        /// </summary>
        protected override void SetDefaultOutputFile()
        {
            if (String.IsNullOrEmpty(this.OutputFile))
            {
                string outputFileName;
                string outputDirectoryName = FileUtilities.GetUniqueFileName();

                if (!string.IsNullOrEmpty(this.InputFile))
                {
                    outputFileName = String.Concat(Path.GetFileNameWithoutExtension(this.InputFile), this.OutputFileExtension);
                    this.OutputFile = Path.Combine(outputDirectoryName, outputFileName);
                }
            }
        }
    }
}
