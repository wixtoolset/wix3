// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;

namespace WixTest
{
    /// <summary>
    /// Description trait attribute denotes the test case description.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class DescriptionAttribute : Attribute // TODO: Implement ITraitAttribute when Xunit releases it.
    {
        /// <summary>
        /// Creates a new instance <see cref="DescriptionAttribute"/> class.
        /// </summary>
        /// <param name="description">The test case description.</param>
        public DescriptionAttribute(string description)
        {
            this.Description = description;
        }

        /// <summary>
        /// Gets the test case description.
        /// </summary>
        public string Description { get; private set; }
    }
}
