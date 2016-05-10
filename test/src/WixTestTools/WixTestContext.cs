// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
