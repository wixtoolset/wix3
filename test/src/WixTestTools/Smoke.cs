// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
