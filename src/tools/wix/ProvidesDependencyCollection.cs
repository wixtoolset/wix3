// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// A case-insensitive collection of unique <see cref="ProvidesDependency"/> objects.
    /// </summary>
    internal sealed class ProvidesDependencyCollection : KeyedCollection<string, ProvidesDependency>
    {
        /// <summary>
        /// Creates a case-insensitive collection of unique <see cref="ProvidesDependency"/> objects.
        /// </summary>
        internal ProvidesDependencyCollection()
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }

        /// <summary>
        /// Adds the <see cref="ProvidesDependency"/> to the collection if it doesn't already exist.
        /// </summary>
        /// <param name="dependency">The <see cref="ProvidesDependency"/> to add to the collection.</param>
        /// <returns>True if the <see cref="ProvidesDependency"/> was added to the collection; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="dependency"/> parameter is null.</exception>
        internal bool Merge(ProvidesDependency dependency)
        {
            if (null == dependency)
            {
                throw new ArgumentNullException("dependency");
            }

            // If the dependency key is already in the collection, verify equality for a subset of properties.
            if (this.Contains(dependency.Key))
            {
                ProvidesDependency current = this[dependency.Key];
                if (!current.Equals(dependency))
                {
                    return false;
                }
            }

            base.Add(dependency);
            return true;
        }

        /// <summary>
        /// Gets the <see cref="ProvidesDependency.Key"/> for the <paramref name="dependency"/>.
        /// </summary>
        /// <param name="dependency">The dependency to index.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="dependency"/> parameter is null.</exception>
        /// <returns>The <see cref="ProvidesDependency.Key"/> for the <paramref name="dependency"/>.</returns>
        protected override string GetKeyForItem(ProvidesDependency dependency)
        {
            if (null == dependency)
            {
                throw new ArgumentNullException("dependency");
            }

            return dependency.Key;
        }
    }
}
