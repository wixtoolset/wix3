//--------------------------------------------------------------------------------------------------
// <copyright file="WixFileNodeNonMemberProperties.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixFileNodeNonMemberProperties class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Represents non-member file node properties.
    /// </summary>
    /// <remarks>This class must be public and marked as ComVisible in order for the DispatchWrapper to work correctly.</remarks>
    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("C9124B53-0D67-400A-8C8B-47A06C5DC6B7")]
    [SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    [SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NonMember")]
    public class WixFileNodeNonMemberProperties : FileNodeProperties
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixFileNodeNonMemberProperties"/> class.
        /// </summary>
        /// <param name="node">The node that contains the properties to expose via the Property Browser.</param>
        public WixFileNodeNonMemberProperties(WixFileNode node)
            : base(node)
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Overriden so that it can be make invisible for non member file items.
        /// </summary>
        /// <value>Gets / Sets the BuildAction for the item. It defines how the MS Build
        /// will treat this item at build time.</value>
        [Browsable(false)]
        [AutomationBrowsable(false)]
        public override BuildAction BuildAction
        {
            get
            {
                return base.BuildAction;
            }

            set
            {
                base.BuildAction = value;
            }
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Creates a custom property descriptor for the node properties, which affects the behavior
        /// of the property grid.
        /// </summary>
        /// <param name="propertyDescriptor">The <see cref="PropertyDescriptor"/> to wrap.</param>
        /// <returns>A custom <see cref="PropertyDescriptor"/> object.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "In the 2005 SDK, it's called p and in the 2008 SDK it's propertyDescriptor")]
        public override DesignPropertyDescriptor CreateDesignPropertyDescriptor(PropertyDescriptor propertyDescriptor)
        {
            return new WixNonMemberDesignPropertyDescriptor(propertyDescriptor);
        }
    }
}