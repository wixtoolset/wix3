//-----------------------------------------------------------------------
// <copyright file="Lit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Lit</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest.Utilities;

    /// <summary>
    /// A class that wraps Lit
    /// </summary>
    public partial class Lit : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory
        /// </summary>
        public Lit()
            : this((string)null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool</param>
        public Lit(string workingDirectory)
            : base("lit.exe", workingDirectory)
        {
        }

        /// <summary>
        /// Constructor that uses data from a Candle object to create a Lit object
        /// </summary>
        /// <param name="candle">A Candle object</param>
        public Lit(Candle candle)
            : this()
        {
            // The output of Candle is the input for Lit
            this.ObjectFiles = candle.ExpectedOutputFiles;
        }

        /// <summary>
        /// Checks that the result from a run matches the expected results.
        /// </summary>
        /// <param name="result">A result from a run.</param>
        /// <returns>A list of errors.</returns>
        public override List<string> CheckResult(Result result)
        {
            List<string> errors = new List<string>();
            errors.AddRange(base.CheckResult(result));

            // If Lit returns success then verify that expected wix file is created
            if (result.ExitCode == 0)
            {
                if (null != this.ObjectFiles && this.ObjectFiles.Count > 0)
                {
                    if (false == this.Help && !File.Exists(this.ExpectedOutputFile))
                    {
                        errors.Add(String.Format("Expected wix file {0} was not created", this.ExpectedOutputFile));
                    }
                }
            }
            return errors;
        }

        /// <summary>
        /// The expected output file of Lit that is guaranteed to exist
        /// </summary>
        public string ExpectedOutputFile
        {
            get
            {
                if (!string.IsNullOrEmpty(this.OutputFile))
                {
                    return this.OutputFile;
                }
                else
                {
                    if (null != this.ObjectFiles && 1 == this.ObjectFiles.Count)
                    {
                        return Path.Combine(this.WorkingDirectory, String.Concat(Path.GetFileNameWithoutExtension(this.ObjectFiles[0]), this.OutputFileExtension));
                    }
                    else
                    {
                        return Path.Combine(this.WorkingDirectory, String.Concat("product", this.OutputFileExtension));
                    }
                }
            }
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return ".wixlib"; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Library Tool"; }
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
                
                // make sure the directory exists
                if (!Directory.Exists(outputDirectoryName))
                {
                    Directory.CreateDirectory(outputDirectoryName);
                }

                if (null != this.ObjectFiles && this.ObjectFiles.Count == 1)
                {
                    outputFileName = String.Concat(Path.GetFileNameWithoutExtension(this.ObjectFiles[0]), this.OutputFileExtension);
                }
                else
                {
                    outputFileName = String.Concat("test", this.OutputFileExtension);
                }

                this.OutputFile = Path.Combine(outputDirectoryName, outputFileName);
            }
        }
    }
}
