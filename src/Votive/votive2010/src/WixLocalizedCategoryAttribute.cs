// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Subclasses <see cref="CategoryAttribute"/> to allow for localized strings retrieved
    /// from the resource assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class WixLocalizedCategoryAttribute : CategoryAttribute
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixLocalizedCategoryAttribute"/> class.
        /// </summary>
        /// <param name="categoryId">The string identifier to get.</param>
        public WixLocalizedCategoryAttribute(string categoryId)
            : base(categoryId)
        {
        }

        /// <summary>
        /// Gets the identifier for the category.
        /// </summary>
        /// <value>The identifier for the category.</value>
        public string CategoryId
        {
            get { return this.Category; }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Looks up the localized name of the specified category.
        /// </summary>
        /// <param name="value">The identifer for the category to look up.</param>
        /// <returns>The localized name of the category, or null if a localized name does not exist.</returns>
        protected override string GetLocalizedString(string value)
        {
            return WixStrings.ResourceManager.GetString(value);
        }
    }
}
