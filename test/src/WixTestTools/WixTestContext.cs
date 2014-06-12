//-----------------------------------------------------------------------
// <copyright file="WixTestContext.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using Xunit.Sdk;

    /// <summary>
    /// Context for a test run.
    /// </summary>
    public class WixTestContext
    {
        /// <summary>
        /// Gets the unique seed for this test run.
        /// </summary>
        public string Seed { get; internal set; }

        /// <summary>
        /// Gets the test data directory for all tests.
        /// </summary>
        public string DataDirectory { get; internal set; }

        /// <summary>
        /// Gets the unique name of the current test case.
        /// </summary>
        public string TestName { get; internal set; }

        /// <summary>
        /// Gets the test directory for the current test case.
        /// </summary>
        public string TestDirectory { get; internal set; }

        /// <summary>
        /// Gets the test data directory for the current test case.
        /// </summary>
        public string TestDataDirectory { get; internal set; }

        /// <summary>
        /// Gets the test result for the current test case.
        /// </summary>
        public ITestResult TestResult { get; internal set; }
    }
}
