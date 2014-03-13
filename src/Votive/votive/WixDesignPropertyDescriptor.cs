//--------------------------------------------------------------------------------------------------
// <copyright file="WixDesignPropertyDescriptor.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixDesignPropertyDescriptor class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Subclasses <see cref="DesignPropertyDescriptor"/> to allow for non-bolded text in the property grid.
    /// </summary>
    public class WixDesignPropertyDescriptor : DesignPropertyDescriptor
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixDesignPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="propertyDescriptor">The <see cref="PropertyDescriptor"/> to wrap.</param>
        public WixDesignPropertyDescriptor(PropertyDescriptor propertyDescriptor)
            : base(propertyDescriptor)
        {
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// By returning false here, we're always going to show the property as non-bolded.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>Always returns false.</returns>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}