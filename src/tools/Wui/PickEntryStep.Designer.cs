// <copyright file="PickEntryStep.Designer.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Step to pick the entry point for application.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    /// <summary>
    /// Step to pick the entry point for application.
    /// </summary>
    public sealed partial class PickEntryStep
    {
        private System.Windows.Forms.TreeView treeView;
        private System.Windows.Forms.ImageList treeImages;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PickEntryStep));
            this.treeView = new System.Windows.Forms.TreeView();
            this.treeImages = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // treeView
            // 
            resources.ApplyResources(this.treeView, "treeView");
            this.treeView.FullRowSelect = true;
            this.treeView.HideSelection = false;
            this.treeView.ImageList = this.treeImages;
            this.treeView.Name = "treeView";
            this.treeView.DoubleClick += new System.EventHandler(this.TreeView_DoubleClick);
            // 
            // treeImages
            // 
            this.treeImages.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            resources.ApplyResources(this.treeImages, "treeImages");
            this.treeImages.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // PickEntryStep
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.treeView);
            this.Name = "PickEntryStep";
            this.Tag = "Double click on the file in the directory tree below to be the entry point for yo" +
                "ur application.  This should be an executable.";
            this.ResumeLayout(false);

        }

        #endregion
    }
}
