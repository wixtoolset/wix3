//--------------------------------------------------------------------------------------------------
// <copyright file="WixLocalizedDescriptionAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixLocalizedDescritpionAttribute class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Subclasses <see cref="DescriptionAttribute"/> to allow for localized strings retrieved
    /// from the resource assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class WixLocalizedDescriptionAttribute : DescriptionAttribute
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private bool initialized;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixLocalizedDescriptionAttribute"/> class.
        /// </summary>
        /// <param name="descriptionId">The string identifier to get.</param>
        public WixLocalizedDescriptionAttribute(string descriptionId)
            : base(descriptionId)
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the identifier for the description.
        /// </summary>
        /// <value>The identifier for the description.</value>
        public string DescriptionId
        {
            get { return this.DescriptionValue; }
        }

        /// <summary>
        /// Gets the description stored in this attribute.
        /// </summary>
        /// <value>The description stored in this attribute.</value>
        public override string Description
        {
            get
            {
                if (!this.initialized)
                {
                    string localizedDescription = WixStrings.ResourceManager.GetString(this.DescriptionValue);
                    if (localizedDescription != null)
                    {
                        this.DescriptionValue = localizedDescription;
                    }

                    this.initialized = true;
                }

                return this.DescriptionValue;
            }
        }
    }
}