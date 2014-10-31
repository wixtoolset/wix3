//-----------------------------------------------------------------------
// <copyright file="RuntimeTestAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

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
