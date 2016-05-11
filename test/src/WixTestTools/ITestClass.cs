// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
