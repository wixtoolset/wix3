//-------------------------------------------------------------------------------------------------
// <copyright file="WixPathsPropertyPage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixPathsPropertyPage class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Property page for the include and reference paths
    /// </summary>
    [ComVisible(true)]
    [Guid("3ED6B659-F2FA-4252-8B3A-3736B04BFC4D")]
    internal class WixPathsPropertyPage : WixPropertyPage
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of WixPathsPropertyPage
        /// </summary>
        public WixPathsPropertyPage()
        {
            this.PageName = WixStrings.WixPathsPropertyPageName;
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Creates the controls that constitute the property page. This should be safe to re-entrancy.
        /// </summary>
        /// <returns>The newly created main control that hosts the property page.</returns>
        protected override WixPropertyPagePanel CreatePropertyPagePanel()
        {
            return new WixPathsPropertyPagePanel(this);
        }
    }
}
