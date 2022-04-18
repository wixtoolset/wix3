// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Attribute to denote the localized text that should be displayed on the control for the
    /// property page settings.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class WixLocalizedControlTextAttribute : Attribute
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private string id;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixLocalizedControlTextAttribute"/> class.
        /// </summary>
        /// <param name="controlTextId">The string identifier to get.</param>
        public WixLocalizedControlTextAttribute(string controlTextId)
        {
            this.id = controlTextId;
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the identifier for the associated control or control's label.
        /// </summary>
        /// <value>The identifier for the associated control or control's label.</value>
        public string ControlTextId
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the text to display for the associated control or control's label.
        /// </summary>
        /// <value>The text to display for the associated control or control's label.</value>
        public string ControlText
        {
            get
            {
                return WixStrings.ResourceManager.GetString(this.id);
            }
        }
    }
}
