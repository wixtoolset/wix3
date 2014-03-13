//-------------------------------------------------------------------------------------------------
// <copyright file="WixToolsSettingsPropertyPage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixToolsSettingsPropertyPage class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Property page for the compiler/linker/librarian command line settings
    /// </summary>
    [ComVisible(true)]
    [Guid("37DA98B6-C5F4-4ce6-867F-553EBE590ABE")]
    internal class WixToolsSettingsPropertyPage : WixPropertyPage
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of WixToolsSettingsPropertyPage class
        /// </summary>
        public WixToolsSettingsPropertyPage()
        {
            this.PageName = WixStrings.WixToolsSettingsPropertyPageName;
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
            return new WixToolsSettingsPropertyPagePanel(this);
        }
    }
}
