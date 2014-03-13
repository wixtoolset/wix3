//--------------------------------------------------------------------------------------------------
// <copyright file="WixLocalizedControlTextAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixLocalizedControlTextAttribute class.
// </summary>
//--------------------------------------------------------------------------------------------------

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