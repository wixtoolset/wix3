//-------------------------------------------------------------------------------------------------
// <copyright file="WixInstallerPropertyPagePanel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixBuildPropertyPagePanel class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    /// <summary>
    /// Property page contents for the Wix Project Build Settings page.
    /// </summary>
    internal partial class WixInstallerPropertyPagePanel : WixPropertyPagePanel
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixInstallerPropertyPagePanel"/> class.
        /// </summary>
        /// <param name="parentPropertyPage">The parent property page to which this is bound.</param>
        public WixInstallerPropertyPagePanel(WixPropertyPage parentPropertyPage)
            : base(parentPropertyPage)
        {
            this.InitializeComponent();

            this.outputNameTextBox.Tag = WixProjectFileConstants.OutputName;
            this.comboOutputType.Tag = WixProjectFileConstants.OutputType;
        }
    }
}
