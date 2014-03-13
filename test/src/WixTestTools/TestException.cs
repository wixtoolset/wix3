//-----------------------------------------------------------------------
// <copyright file="TestException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Exception that occurs during a TestTool run</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;

    /// <summary>
    /// Exception that occurs during a TestTool run
    /// </summary>
    public class TestException : Exception
    {
        /// <summary>
        /// The result of executing the tool
        /// </summary>
        private Result result = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="result">The result of executing the tool</param>
        public TestException(string message, Result result)
            : base(message)
        {
            this.result = result;
        }

        /// <summary>
        /// The result of executing the tool
        /// </summary>
        public Result Result
        {
            get { return this.result; }
        }
    }
}
