//-----------------------------------------------------------------------
// <copyright file="PriorityAttribute.cs" company="Outercurve Foundation">
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
    /// Priority trait attribute denotes the test case priority.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class PriorityAttribute : Attribute // TODO: Implement ITraitAttribute when Xunit releases it.
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PriorityAttribute"/> class.
        /// </summary>
        /// <param name="priority">The test case priority.</param>
        public PriorityAttribute(int priority)
        {
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the test case priority.
        /// </summary>
        public int Priority { get; private set; }
    }
}
