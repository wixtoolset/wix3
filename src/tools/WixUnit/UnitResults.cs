//-------------------------------------------------------------------------------------------------
// <copyright file="UnitResults.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Container for the results from a single unit test run.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.Xml;

    /// <summary>
    /// Container for the results from a single unit test run.
    /// </summary>
    internal sealed class UnitResults
    {
        private ArrayList errors;
        private ArrayList output;
        private ArrayList outputFiles;

        /// <summary>
        /// Instantiate a new UnitResults.
        /// </summary>
        public UnitResults()
        {
            this.errors = new ArrayList();
            this.output = new ArrayList();
            this.outputFiles = new ArrayList();
        }

        /// <summary>
        /// Gets the ArrayList of error strings.
        /// </summary>
        /// <value>The ArrayList of error strings.</value>
        public ArrayList Errors
        {
            get { return this.errors; }
        }

        /// <summary>
        /// Gets the ArrayList of output strings.
        /// </summary>
        /// <value>The ArrayList of output strings.</value>
        public ArrayList Output
        {
            get { return this.output; }
        }

        /// <summary>
        /// Gets the ArrayList of output files.
        /// </summary>
        /// <value>The ArrayList of output files.</value>
        public ArrayList OutputFiles
        {
            get { return this.outputFiles; }
        }
    }
}
