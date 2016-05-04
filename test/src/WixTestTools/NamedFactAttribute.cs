// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using WixTest.Utilities;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    /// Attribute used to decorate test case methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NamedFactAttribute : FactAttribute
    {
        /// <summary>
        /// Gets the test case methods.
        /// </summary>
        /// <param name="method">The <see cref="IMethodInfo"/> describing the test case method.</param>
        /// <returns>A <see cref="FactCommand"/>-derivative containing information about the test case.</returns>
        protected override IEnumerable<ITestCommand> EnumerateTestCommands(IMethodInfo method)
        {
            // Skip the test cases if requirements are not met.
            if (this.ValidateRequirements(method))
            {
                yield return new TestMethodCommand(method);
            }
            else
            {
                yield return new SkipCommand(method, null, this.Skip);
            }
        }

        private bool ValidateRequirements(IMethodInfo method)
        {
            if (method.HasAttribute(typeof(RuntimeTestAttribute)))
            {
                RuntimeTestAttribute attr = method.GetCustomAttributes(typeof(RuntimeTestAttribute)).First().GetInstance<RuntimeTestAttribute>();
                if (!(RuntimeTestAttribute.RuntimeTestsEnabled || Debugger.IsAttached))
                {
                    this.Skip = String.Format("Runtime tests are not enabled on this test environment. To enable runtime tests set the environment variable '{0}'=true or run the tests under a debugger.", RuntimeTestAttribute.RuntimeTestsEnabledEnvironmentVariable);
                    return false;
                }
                else if (!(attr.NonPrivileged || UacUtilities.IsProcessElevated))
                {
                    this.Skip = String.Format("The runtime test '{0}' requires that the test process be elevated.", method.Name);
                    return false;
                }
            }
            else if (method.HasAttribute(typeof(Is64BitSpecificTestAttribute)) && !Is64BitSpecificTestAttribute.Is64BitOperatingSystem)
            {
                this.Skip = "64-bit specific tests are not enabled on 32-bit machines.";
                return false;
            }

            return true;
        }

        private class TestMethodCommand : FactCommand
        {
            /// <summary>
            /// Gets the containing namespace of the test case method.
            /// </summary>
            public string Namespace { get; private set; }

            /// <summary>
            /// Gets the containing class name of the test case method.
            /// </summary>
            public string Class { get; private set; }

            /// <summary>
            /// Gets the name of the test case method.
            /// </summary>
            public string Method { get; set; }

            /// <summary>
            /// Creates a new instance of the <see cref="TestMethodCommand"/> class.
            /// </summary>
            /// <param name="method">The <see cref="IMethodInfo"/> describing the test case method.</param>
            public TestMethodCommand(IMethodInfo method)
                : base(method)
            {
                this.Namespace = method.Class.Type.Namespace;
                this.Class = method.Class.Type.Name;
                this.Method = method.Name;
            }

            /// <summary>
            /// Executes the test case method.
            /// </summary>
            /// <param name="testClass">An instance of the class that contains the test case method.</param>
            /// <returns>The <see cref="MethodResult"/> of executing the test case method.</returns>
            public override MethodResult Execute(object testClass)
            {
                ITestClass theTestClass = testClass as ITestClass;

                if (null != theTestClass)
                {
                    MethodResult result = null;

                    try
                    {
                        theTestClass.TestInitialize(this.Namespace, this.Class, this.Method);
                        result = base.Execute(testClass);
                    }
                    catch (Exception ex)
                    {
                        // Return test failure to avoid extra break when debugging.
                        result = new FailedResult(this.testMethod, ex, null);
                    }
                    finally
                    {
                        theTestClass.TestUninitialize(result);
                    }

                    return result;
                }

                return base.Execute(testClass);
            }
        }
    }
}
