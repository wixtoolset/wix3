// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;

    /// <summary>
    /// Denotes that a particular test case is a runtime test and may modify machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class RuntimeTestAttribute : Attribute // TODO: Implement ITraitAttribute when Xunit releases it.
    {
        /// <summary>
        /// The environment variable name to determine if runtime tests are enabled.
        /// </summary>
        internal static readonly string RuntimeTestsEnabledEnvironmentVariable = "RuntimeTestsEnabled";

        /// <summary>
        /// Gets or sets whether the runtime test can run without elevated privileges.
        /// </summary>
        public bool NonPrivileged { get; set; }

        /// <summary>
        /// Gets whether runtime tests are enabled.
        /// </summary>
        public static bool RuntimeTestsEnabled
        {
            get
            {
                string runtimeTestsEnabled = Environment.GetEnvironmentVariable(RuntimeTestAttribute.RuntimeTestsEnabledEnvironmentVariable);
                return "true".Equals(runtimeTestsEnabled, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
