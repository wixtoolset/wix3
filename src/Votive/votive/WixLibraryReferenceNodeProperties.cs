//-------------------------------------------------------------------------------------------------
// <copyright file="WixLibraryReferenceNodeProperties.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixLibraryReferenceNodeProperties class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Package;

    /// <summary>
    /// Represents Wixlib reference node properties.
    /// </summary>
    /// <remarks>This class must be public and marked as ComVisible in order for the DispatchWrapper to work correctly.</remarks>
    [CLSCompliant(false)]
    [ComVisible(true)]
    [Guid("C69F4413-4204-4ef1-B195-A729F85F9285")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1409:ComVisibleTypesShouldBeCreatable")]
    public class WixLibraryReferenceNodeProperties : WixReferenceNodeProperties
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixLibraryReferenceNodeProperties"/> class.
        /// </summary>
        /// <param name="node">The node that contains the properties to expose via the Property Browser.</param>
        public WixLibraryReferenceNodeProperties(WixLibraryReferenceNode node)
            : base(node)
        {
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
        /// </summary>
        /// <returns>The class name of the object, or null if the class does not have a name.</returns>
        public override string GetClassName()
        {
            return WixStrings.WixLibraryReferenceProperties;
        }
    }
}
