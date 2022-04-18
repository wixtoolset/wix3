// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Subclasses <see cref="DisplayNameAttribute"/> to allow for localized strings retrieved
    /// from the resource assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class WixLocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private bool initialized;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixLocalizedDisplayNameAttribute"/> class.
        /// </summary>
        /// <param name="displayNameId">The string identifier to get.</param>
        public WixLocalizedDisplayNameAttribute(string displayNameId)
            : base(displayNameId)
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the identifier for the display name.
        /// </summary>
        /// <value>The identifier for the display name.</value>
        public string DisplayNameId
        {
            get { return this.DisplayNameValue; }
        }

        /// <summary>
        /// Gets the display name for a property, event, or public void method that takes no
        /// arguments stored in this attribute.
        /// </summary>
        /// <value>The display name for a property, event, or public void method with no arguments.</value>
        public override string DisplayName
        {
            get
            {
                if (!this.initialized)
                {
                    string localizedString = WixStrings.ResourceManager.GetString(this.DisplayNameValue);
                    if (localizedString != null)
                    {
                        this.DisplayNameValue = localizedString;
                    }

                    this.initialized = true;
                }

                return this.DisplayNameValue;
            }
        }
    }
}
