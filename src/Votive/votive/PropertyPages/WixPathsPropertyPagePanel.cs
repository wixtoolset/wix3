//-------------------------------------------------------------------------------------------------
// <copyright file="WixPathsPropertyPagePanel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixPathsPropertyPagePanel class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Data;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// Property page contents for the Paths property page
    /// </summary>
    /// 
    internal partial class WixPathsPropertyPagePanel : WixPropertyPagePanel
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initilizes a new instance of the WixPathsPropertyPagePanel class
        /// </summary>
        /// <param name="parentPropertyPage">The parent property page to which this is bound</param>
        public WixPathsPropertyPagePanel(WixPropertyPage parentPropertyPage)
            : base(parentPropertyPage)
        {
            this.InitializeComponent();

            this.referencePathsFoldersSelector.Tag = WixProjectFileConstants.ReferencePaths;
            this.includePathsFolderSelector.Tag = WixProjectFileConstants.IncludeSearchPaths;
        }

        private void WixPathsPropertyPagePanel_Resize(object sender, EventArgs e)
        {
            this.mainTableLayoutPanel.Size = new Size(this.Size.Width, this.Size.Height);
        }

        private void IncludePathsGroupBox_Resize(object sender, EventArgs e)
        {
            this.includePathsFolderSelector.Size = new Size(
                this.includePathsGroupBox.Size.Width - this.includePathsFolderSelector.Location.X,
                this.includePathsGroupBox.Size.Height - this.includePathsFolderSelector.Location.Y);

            this.referencePathsFoldersSelector.Size = new Size(
                this.referencePathsGroupBox.Size.Width - this.referencePathsFoldersSelector.Location.X,
                this.includePathsGroupBox.Size.Height - this.includePathsFolderSelector.Location.Y);
        }
    }
}
