//-------------------------------------------------------------------------------------------------
// <copyright file="ProvidesDependencyCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// A case-insensitive collection of unique ProvidesDependency objects.
// </summary>
//-------------------------------------------------------------------------------------------------

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
