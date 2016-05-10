// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
