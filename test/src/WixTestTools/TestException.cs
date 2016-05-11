// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
