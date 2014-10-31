//-----------------------------------------------------------------------
// <copyright file="WixTestBase.cs" company="Outercurve Foundation">
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
    /// Support interface for test cases.
    /// </summary>
    public interface ITestClass
    {
        /// <summary>
        /// Initialize the test case.
        /// </summary>
        /// <param name="testNamespace">Containing namespace of the test case.</param>
        /// <param name="testClass">Containing class name of the test case.</param>
        /// <param name="testMethod">Method name of the test case.</param>
        void TestInitialize(string testNamespace, string testClass, string testMethod);

        /// <summary>
        /// Uninitializes the test case.
        /// </summary>
        /// <param name="result">The <see cref="MethodResult"/> of executing the test case method.</param>
        void TestUninitialize(MethodResult result);
    }
}
