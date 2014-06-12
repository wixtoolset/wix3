// <copyright file="DescriptionAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

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
