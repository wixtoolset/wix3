//-----------------------------------------------------------------------
// <copyright file="Candle.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Candle</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using WixTest.Utilities;

    /// <summary>
    /// A class that wraps Candle.
    /// </summary>
    public partial class Candle : WixTool
    {
        /// <summary>
        /// Constructor that uses the current directory as the working directory.
        /// </summary>
        public Candle()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor that uses the default WiX tool directory as the tools location.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the tool.</param>
        public Candle(string workingDirectory)
            : base("candle.exe", workingDirectory)
        {
        }
       
        /// <summary>
        /// Based on  current arguments, returns a list of files that Candle is expected to generate.
        /// </summary>
        /// <remarks>
        /// This is an expected list and files are not guaranteed to exist.
        /// </remarks>
        public List<string> ExpectedOutputFiles
        {
            get {
                List<string> expectedOutputFiles = new List<string>();

                if (this.OutputFile.EndsWith(@"\") || this.OutputFile.EndsWith(@"/") || String.IsNullOrEmpty(this.OutputFile))
                {
                    // Create list of expected files based on how Candle would do it.
                    // Candle would change the extension of each .wxs file to .wixobj.

                    string outputDirectory = (this.OutputFile ?? String.Empty);

                    foreach (string sourceFile in this.sourceFiles)
                    {
                        string outputFile = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(sourceFile) + ".wixobj");
                        expectedOutputFiles.Add(outputFile);
                    }
                }
                else
                {
                    expectedOutputFiles.Add(this.OutputFile);
                }

                return expectedOutputFiles;
            }
        }

        /// <summary>
        /// The default file extension of an output file
        /// </summary>
        protected override string OutputFileExtension
        {
            get { return ".wixobj"; }
        }

        /// <summary>
        /// Functional name of the tool
        /// </summary>
        public override string ToolDescription
        {
            get { return "Compiler"; }
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

                if (null != this.SourceFiles && this.SourceFiles.Count == 1)
                {

                    outputFileName = String.Concat(Path.GetFileNameWithoutExtension(this.SourceFiles[0]), this.OutputFileExtension);
                    this.OutputFile = Path.Combine(outputDirectoryName, outputFileName);
                }
                else if (null != this.SourceFiles && this.SourceFiles.Count > 1)    // if more than one file, output directory should be specified
                {
                    this.OutputFile = string.Concat(outputDirectoryName, @"\");
                }
            }
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

            // If candle returns success then verify that expected wixobj files are created
            if (0 == result.ExitCode)
            {
                foreach (string file in this.ExpectedOutputFiles)
                {
                    if (!File.Exists(file))
                    {
                        errors.Add(String.Format("Expected wixobj file {0} was not created", file));
                    }
                }
            }

            return errors;
        }
    }
}
