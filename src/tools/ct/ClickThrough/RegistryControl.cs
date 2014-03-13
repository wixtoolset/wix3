//-------------------------------------------------------------------------------------------------
// <copyright file="RegistryControl.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// User control for selecting registry keys.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;
    using Scraper = Microsoft.Tools.WindowsInstallerXml.ApplicationModel;
//    using Serialize = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// User control for selecting registry keys.
    /// </summary>
    public class RegistryControl : System.Windows.Forms.UserControl
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.GroupBox registryGroupBox;
        private System.Windows.Forms.Label registryLabel;
        private System.Windows.Forms.TreeView registryTreeView;
        private System.Windows.Forms.TextBox registryTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button registryButton;
        private System.Windows.Forms.ImageList registryImageList;
        private Scraper.Application application;

        private AddTreeNodeCallback addTreeNodeCallback;

        /// <summary>
        /// Instantiate a new RegistryControl class.
        /// </summary>
        /// <param name="application">Application to use as a backing store.</param>
        public RegistryControl(Scraper.Application application)
        {
            this.application = application;
            // This call is required by the Windows.Forms Form Designer.
            this.InitializeComponent();

            this.addTreeNodeCallback = new AddTreeNodeCallback(this.AddTreeNode);
        }

        /// <summary>
        /// Delegate for adding a new node to the tree view.
        /// </summary>
        /// <param name="nodes">The NodeCollection to add the new node to.</param>
        /// <param name="icon">The icon of the new tree node.</param>
        /// <param name="text">The label Text of the new tree node.</param>
        /// <param name="tag">The tag of the new tree node.</param>
        /// <returns>The new node.</returns>
        private delegate TreeNode AddTreeNodeCallback(TreeNodeCollection nodes, Icon icon, string text, object tag);

        /// <summary>
        /// Event for status changes.
        /// </summary>
        public event StatusChangingHandler StatusChanging;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.components != null)
                {
                    this.components.Dispose();
                }
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
            this.registryGroupBox = new System.Windows.Forms.GroupBox();
            this.registryLabel = new System.Windows.Forms.Label();
            this.registryButton = new System.Windows.Forms.Button();
            this.registryTreeView = new System.Windows.Forms.TreeView();
            this.registryImageList = new System.Windows.Forms.ImageList(this.components);
            this.registryTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.registryGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // registryGroupBox
            // 
            this.registryGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.registryGroupBox.Controls.Add(this.registryLabel);
            this.registryGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.registryGroupBox.Location = new System.Drawing.Point(368, 48);
            this.registryGroupBox.Name = "registryGroupBox";
            this.registryGroupBox.Size = new System.Drawing.Size(256, 416);
            this.registryGroupBox.TabIndex = 9;
            this.registryGroupBox.TabStop = false;
            this.registryGroupBox.Text = "Registry Value Information";
            // 
            // registryLabel
            // 
            this.registryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.registryLabel.Location = new System.Drawing.Point(16, 24);
            this.registryLabel.Name = "registryLabel";
            this.registryLabel.Size = new System.Drawing.Size(224, 376);
            this.registryLabel.TabIndex = 4;
            // 
            // registryButton
            // 
            this.registryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.registryButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.registryButton.Location = new System.Drawing.Point(536, 16);
            this.registryButton.Name = "registryButton";
            this.registryButton.TabIndex = 2;
            this.registryButton.Text = "Get Keys";
            this.registryButton.Click += new System.EventHandler(this.RegistryButton_Click);
            // 
            // registryTreeView
            // 
            this.registryTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.registryTreeView.ImageList = this.registryImageList;
            this.registryTreeView.Location = new System.Drawing.Point(16, 48);
            this.registryTreeView.Name = "registryTreeView";
            this.registryTreeView.Size = new System.Drawing.Size(336, 416);
            this.registryTreeView.TabIndex = 3;
            this.registryTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.RegistryTreeView_AfterSelect);
            // 
            // registryImageList
            // 
            this.registryImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.registryImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // registryTextBox
            // 
            this.registryTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.registryTextBox.Location = new System.Drawing.Point(184, 14);
            this.registryTextBox.Name = "registryTextBox";
            this.registryTextBox.Size = new System.Drawing.Size(336, 20);
            this.registryTextBox.TabIndex = 1;
            this.registryTextBox.Text = "";
            this.registryTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.RegistryTextBox_KeyPress);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(32, 16);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(160, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "Path under HKCU\\Software:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // RegistryControl
            // 
            this.Controls.Add(this.registryGroupBox);
            this.Controls.Add(this.registryButton);
            this.Controls.Add(this.registryTreeView);
            this.Controls.Add(this.registryTextBox);
            this.Controls.Add(this.label3);
            this.Name = "RegistryControl";
            this.Size = new System.Drawing.Size(640, 480);
            this.registryGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// Called when the "Get Keys" button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void RegistryButton_Click(object sender, System.EventArgs e)
        {
            if (this.registryTextBox.Text.Length > 0)
            {
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.ScrapeRegistryCallback), this.registryTextBox.Text);
            }
        }

        /// <summary>
        /// Called when a key is pressed in the registry path text box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void RegistryTextBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                this.registryButton.PerformClick();
            }
        }

        /// <summary>
        /// Called after a node in the tree view is selected.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void RegistryTreeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
//            Serialize.Registry registryKey = this.registryTreeView.SelectedNode.Tag as Serialize.Registry;
//            if (registryValue == null)
//            {
//                Serialize.Registry registryKey = this.registryTreeView.SelectedNode.Tag as Serialize.Registry;
//                foreach (Serialize.ISchemaElement child in registryKey.Children)
//                {
//                    Serialize.RegistryValue subRegistryValue = child as Serialize.RegistryValue;
//                    if (subRegistryValue != null && subRegistryValu
//                }

//                foreach (Serialize.RegistryValue subRegistryValue in registryKey.Values)
//                {
//                    if (subRegistryValue.Name.Length == 0)
//                    {
//                        registryValue = subRegistryValue;
//                    }
//                }
//            }

//            if (registryKey != null)
//            {
//                switch (registryKey.Type)
//                {
//                    case Serialize.Registry.TypeType.binary:
//                        this.registryLabel.Text = "Binary Value";
//                        break;
//                    case Serialize.Registry.TypeType.integer:
//                        this.registryLabel.Text = String.Concat("DWORD Value", Environment.NewLine, registryKey.Value);
//                        break;
//                    case Serialize.Registry.TypeType.multiString:
//                        this.registryLabel.Text = "Multi-string Value";
//
//                        foreach (Serialize.ISchemaElement child in registryKey.Children)
//                        {
//                            Serialize.RegistryValue registryValue = child as Serialize.RegistryValue;
//                            if (registryValue != null)
//                            {
//                                this.registryLabel.Text = String.Concat(this.registryLabel.Text, Environment.NewLine, "\"", registryValue.Content, "\"");
//                            }
//                        }
//                        break;
//                    case Serialize.Registry.TypeType.@string:
//                        this.registryLabel.Text = String.Concat("String Value", Environment.NewLine, "\"", registryKey.Value, "\"");
//                        break;
//                }
//            }
//            else
            {
                this.registryLabel.Text = String.Empty;
            }
        }

        /// <summary>
        /// Callback for scraping the registry.
        /// </summary>
        /// <param name="obj">The path under HKCU\Software to scrape.</param>
        private void ScrapeRegistryCallback(object obj)
        {
            string path = obj as string;
            Debug.Assert(path != null);
            
            // disable UI
            this.Invoke(new MethodInvoker(this.DisableUI));

            // scrape
//            Serialize.Component regKeyComponent = Scraper.Application.ScrapeRegistry(Serialize.Registry.RootType.HKCU, String.Concat("Software\\", path));

            // prepare tree view for inserting data
            this.Invoke(new MethodInvoker(this.PrepareUI));

//            foreach (Serialize.Registry registryKey in regKeyComponent[typeof(Serialize.Registry)])
//            {
//                // insert data
//                this.AddRegistryKey(this.registryTreeView.Nodes, registryKey, true);
//            }

            // end data insertion and enable UI
            this.Invoke(new MethodInvoker(this.EnableUI));
        }

        /// <summary>
        /// Disable UI after the user has begun an expensive scrape operation.
        /// </summary>
        private void DisableUI()
        {
            this.registryTextBox.Enabled = false;
            this.registryButton.Enabled = false;

            if (this.StatusChanging != null)
            {
                this.StatusChanging(this, new StatusChangingEventArgs("Getting registry keys..."));
            }
        }

        /// <summary>
        /// Prepare the UI to receive data after a scrape operation.
        /// </summary>
        private void PrepareUI()
        {
            this.registryTreeView.BeginUpdate();
            this.registryTreeView.Nodes.Clear();
            this.registryTreeView.ImageList.Images.Clear();
        }

        /// <summary>
        /// Enable the UI after the scrape data is ready to be displayed.
        /// </summary>
        private void EnableUI()
        {
            this.registryTreeView.ExpandAll();
            if (this.registryTreeView.Nodes.Count > 0)
            {
                this.registryTreeView.SelectedNode = this.registryTreeView.Nodes[0];
                this.registryTreeView.SelectedNode.EnsureVisible();
            }
            this.registryTreeView.EndUpdate();

            this.registryTextBox.Enabled = true;
            this.registryButton.Enabled = true;

            if (this.StatusChanging != null)
            {
                this.StatusChanging(this, new StatusChangingEventArgs());
            }
        }

        /// <summary>
        /// Add a scraped key to the UI.
        /// </summary>
        /// <param name="nodes">The NodeCollection under which the new directory should be added.</param>
        /// <param name="registryKey">The scraped registry key to add.</param>
        /// <param name="skip">true if the directory itself shouldn't be added; false otherwise.</param>
//        private void AddRegistryKey(TreeNodeCollection nodes, Serialize.Registry registryKey, bool skip)
//        {
//            if (!skip)
//            {
//                Icon folderIcon = NativeMethods.GetDirectoryIcon(true, false);
//                TreeNode node = (TreeNode)this.Invoke(this.addTreeNodeCallback, new object[] { nodes, folderIcon, registryKey.Name, registryKey });
//                folderIcon.Dispose();
//
//                nodes = node.Nodes;
//            }
//
//            // add the sub-keys and values
//            foreach (Serialize.ISchemaElement child in registryKey.Children)
//            {
//                Serialize.Registry subKey = child as Serialize.Registry;
//                if (subKey != null)
//                {
//                    this.AddRegistryKey(nodes, subKey, false);
//                }
//
////                Serialize.RegistryValue registryValue = child as Serialize.RegistryValue;
////                if (registryValue != null)
////                {
////                    Icon fileIcon = NativeMethods.GetFileIcon(String.Empty, true, false);
////                    this.Invoke(this.addTreeNodeCallback, new object[] { nodes, fileIcon, registryValue, registryValue });
////                }
//            }
//        }

        /// <summary>
        /// Add a new node to the tree view.
        /// </summary>
        /// <param name="nodes">The NodeCollection to add the new node to.</param>
        /// <param name="icon">The icon of the new tree node.</param>
        /// <param name="text">The label Text of the new tree node.</param>
        /// <param name="tag">The tag of the new tree node.</param>
        /// <returns>The new node.</returns>
        private TreeNode AddTreeNode(TreeNodeCollection nodes, Icon icon, string text, object tag)
        {
            // add the image to the image list
            this.registryTreeView.ImageList.Images.Add(icon);
            int imageIndex = this.registryTreeView.ImageList.Images.Count - 1;

            // create the node
            TreeNode node = new TreeNode(text, imageIndex, imageIndex);
            node.Tag = tag;

            // add the node
            nodes.Add(node);

            return node;
        }
    }
}
