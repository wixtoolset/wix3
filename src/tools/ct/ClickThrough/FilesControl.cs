//-------------------------------------------------------------------------------------------------
// <copyright file="FilesControl.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// User control for selecting files.
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
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// User Control for selecting files.
    /// </summary>
    public class FilesControl : System.Windows.Forms.UserControl
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.GroupBox filesGroupBox;
        private System.Windows.Forms.ListView fileListView;
        private System.Windows.Forms.CheckBox shortcutCheckBox;
        private System.Windows.Forms.TreeView filesTreeView;
        private System.Windows.Forms.Button filesBrowseButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ColumnHeader propertyColumnHeader;
        private System.Windows.Forms.ColumnHeader valueColumnHeader;
        private System.Windows.Forms.ImageList filesImageList;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.Header header1;
        private AddTreeNodeCallback addTreeNodeCallback;

        private PackageBuilder packageBuilder;
        private string directoryPath;
        private string applicationEntry;
        private System.Windows.Forms.ErrorProvider shortcutErrorProvider;
        private System.Windows.Forms.ErrorProvider directoryErrorProvider;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.WixFolderBrowserDialog wixFolderBrowserDialog;
        private System.Windows.Forms.TextBox directoryTextBox;

        /// <summary>
        /// Instantiate a new FilesControl class.
        /// </summary>
        public FilesControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            this.InitializeComponent();

            this.header1.Index = 0;

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
        private delegate TreeNode AddTreeNodeCallback(TreeNodeCollection nodes, Icon icon, string text, object tag, bool selected);

        /// <summary>
        /// Event for status changes.
        /// </summary>
        public event StatusChangingHandler StatusChanging;

        /// <summary>
        /// Sets the package builder for the summary information.
        /// </summary>
        public PackageBuilder PackageBuilder
        {
            set
            {
                this.packageBuilder = value;
            }
        }

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

        /// <summary>
        /// Retrieve the size of a file in a nicely-formatted string.
        /// </summary>
        /// <param name="fileInfo">The file.</param>
        /// <returns>A nicely-formatted string representing the size of the file.</returns>
        private static string GetFileSize(FileInfo fileInfo)
        {
            int fileSize = (int)Math.Round(fileInfo.Length / 1024.0);

            // use a special number format provider that does not output decimal spaces
            NumberFormatInfo numberFormatInfo = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
            numberFormatInfo.NumberDecimalDigits = 0;

            return String.Format(CultureInfo.InvariantCulture, "{0} KB", fileSize.ToString("n", numberFormatInfo));
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.filesGroupBox = new System.Windows.Forms.GroupBox();
            this.fileListView = new System.Windows.Forms.ListView();
            this.propertyColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.valueColumnHeader = new System.Windows.Forms.ColumnHeader();
            this.shortcutCheckBox = new System.Windows.Forms.CheckBox();
            this.filesTreeView = new System.Windows.Forms.TreeView();
            this.filesImageList = new System.Windows.Forms.ImageList(this.components);
            this.filesBrowseButton = new System.Windows.Forms.Button();
            this.directoryTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.wixFolderBrowserDialog = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.WixFolderBrowserDialog();
            this.header1 = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.Header();
            this.directoryErrorProvider = new System.Windows.Forms.ErrorProvider();
            this.shortcutErrorProvider = new System.Windows.Forms.ErrorProvider();
            this.filesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // filesGroupBox
            // 
            this.filesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.filesGroupBox.Controls.Add(this.fileListView);
            this.filesGroupBox.Controls.Add(this.shortcutCheckBox);
            this.filesGroupBox.Enabled = false;
            this.filesGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.filesGroupBox.Location = new System.Drawing.Point(268, 121);
            this.filesGroupBox.Name = "filesGroupBox";
            this.filesGroupBox.Size = new System.Drawing.Size(256, 224);
            this.filesGroupBox.TabIndex = 6;
            this.filesGroupBox.TabStop = false;
            this.filesGroupBox.Text = "File Information";
            // 
            // fileListView
            // 
            this.fileListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.fileListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                           this.propertyColumnHeader,
                                                                                           this.valueColumnHeader});
            this.fileListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.fileListView.Location = new System.Drawing.Point(16, 46);
            this.fileListView.MultiSelect = false;
            this.fileListView.Name = "fileListView";
            this.fileListView.Size = new System.Drawing.Size(224, 162);
            this.fileListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.fileListView.TabIndex = 1;
            this.fileListView.View = System.Windows.Forms.View.Details;
            // 
            // propertyColumnHeader
            // 
            this.propertyColumnHeader.Text = "Property";
            this.propertyColumnHeader.Width = 98;
            // 
            // valueColumnHeader
            // 
            this.valueColumnHeader.Text = "Value";
            this.valueColumnHeader.Width = 122;
            // 
            // shortcutCheckBox
            // 
            this.shortcutCheckBox.Location = new System.Drawing.Point(16, 19);
            this.shortcutCheckBox.Name = "shortcutCheckBox";
            this.shortcutCheckBox.Size = new System.Drawing.Size(152, 18);
            this.shortcutCheckBox.TabIndex = 0;
            this.shortcutCheckBox.Text = "&Shortcut for Application";
            this.shortcutCheckBox.CheckedChanged += new System.EventHandler(this.ShortcutCheckBox_CheckedChanged);
            // 
            // filesTreeView
            // 
            this.filesTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.filesTreeView.HideSelection = false;
            this.filesTreeView.ImageList = this.filesImageList;
            this.filesTreeView.Location = new System.Drawing.Point(16, 121);
            this.filesTreeView.Name = "filesTreeView";
            this.filesTreeView.Size = new System.Drawing.Size(236, 224);
            this.filesTreeView.TabIndex = 5;
            this.filesTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FilesTreeView_AfterSelect);
            // 
            // filesImageList
            // 
            this.filesImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.filesImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.filesImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // filesBrowseButton
            // 
            this.filesBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.filesBrowseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.filesBrowseButton.Location = new System.Drawing.Point(460, 89);
            this.filesBrowseButton.Name = "filesBrowseButton";
            this.filesBrowseButton.Size = new System.Drawing.Size(75, 24);
            this.filesBrowseButton.TabIndex = 4;
            this.filesBrowseButton.Text = "&Browse...";
            this.filesBrowseButton.Click += new System.EventHandler(this.FilesBrowseButton_Click);
            // 
            // directoryTextBox
            // 
            this.directoryTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.directoryTextBox.Location = new System.Drawing.Point(76, 89);
            this.directoryTextBox.Name = "directoryTextBox";
            this.directoryTextBox.ReadOnly = true;
            this.directoryTextBox.Size = new System.Drawing.Size(367, 20);
            this.directoryTextBox.TabIndex = 3;
            this.directoryTextBox.Text = "Start by clicking the Browse button to select the directory with your files...";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(4, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "Root Folder:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // wixFolderBrowserDialog
            // 
            this.wixFolderBrowserDialog.Description = "Please select the directory containing the application to package.";
            this.wixFolderBrowserDialog.SelectedPath = null;
            // 
            // header1
            // 
            this.header1.Dock = System.Windows.Forms.DockStyle.Top;
            this.header1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.header1.Index = 0;
            this.header1.Location = new System.Drawing.Point(0, 0);
            this.header1.Name = "header1";
            this.header1.Size = new System.Drawing.Size(540, 88);
            this.header1.TabIndex = 7;
            // 
            // directoryErrorProvider
            // 
            this.directoryErrorProvider.ContainerControl = this;
            // 
            // shortcutErrorProvider
            // 
            this.shortcutErrorProvider.ContainerControl = this;
            // 
            // FilesControl
            // 
            this.Controls.Add(this.header1);
            this.Controls.Add(this.filesGroupBox);
            this.Controls.Add(this.filesTreeView);
            this.Controls.Add(this.filesBrowseButton);
            this.Controls.Add(this.directoryTextBox);
            this.Controls.Add(this.label2);
            this.Name = "FilesControl";
            this.Size = new System.Drawing.Size(540, 360);
            this.Validating += new System.ComponentModel.CancelEventHandler(this.FilesControl_Validating);
            this.Validated += new System.EventHandler(this.FilesControl_Validated);
            this.filesGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// Called when the Browse button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FilesBrowseButton_Click(object sender, System.EventArgs e)
        {
            if (this.wixFolderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.directoryPath = this.wixFolderBrowserDialog.SelectedPath;
                this.directoryTextBox.Text = this.directoryPath;
                this.directoryErrorProvider.SetError(this.directoryTextBox, String.Empty);

                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.ScrapeFileSystemCallback), this.wixFolderBrowserDialog.SelectedPath);
            }
        }

        /// <summary>
        /// Return true if a string is null or zero length
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static bool IsNullOrEmpty(string s)
        {
            return s == null || s.Length == 0;
        }

        /// <summary>
        /// Called after a node in the tree view is selected.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void FilesTreeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            FileInfo fileInfo = e.Node.Tag as FileInfo;

            this.fileListView.Items.Clear();

            if (fileInfo != null)
            {
                this.filesGroupBox.Enabled = true;

                // set the assembly details (if this is an assembly file)
                try
                {
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(fileInfo.FullName);
                    if (CultureInfo.InvariantCulture == assemblyName.CultureInfo)
                    {
                        this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Culture", "neutral" }));
                    }
                    else
                    {
                        this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Culture", assemblyName.CultureInfo.ToString() }));
                    }
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Hash Algorithm", assemblyName.HashAlgorithm.ToString() }));
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Name", assemblyName.Name }));
                    byte[] publicKeyToken = assemblyName.GetPublicKeyToken();
                    if (publicKeyToken != null)
                    {
                        StringBuilder publicKeyTokenString = new StringBuilder();
                        for (int i = 0; i < publicKeyToken.Length; i++)
                        {
                            publicKeyTokenString.AppendFormat("{0:x2}", publicKeyToken[i]);
                        }
                        this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Public Key Token", publicKeyTokenString.ToString() }));
                    }
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Assembly Version", assemblyName.Version.ToString() }));
                }
                catch (BadImageFormatException) // ignore files that aren't assemblies
                {
                }

                // set the file details
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(fileInfo.FullName);
                
                if (!IsNullOrEmpty(fileVersionInfo.Comments))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Comments", fileVersionInfo.Comments }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.CompanyName))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Company Name", fileVersionInfo.CompanyName }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.FileDescription))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Description", fileVersionInfo.FileDescription }));
                }

                this.fileListView.Items.Add(new ListViewItem(new string[] { "File Size", GetFileSize(fileInfo) }));

                if (!IsNullOrEmpty(fileVersionInfo.FileVersion))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "File Version", fileVersionInfo.FileVersion }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.InternalName))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Internal Name", fileVersionInfo.InternalName }));
                }

                if (fileVersionInfo.IsDebug)
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Is Debug", "yes" }));
                }

                if (fileVersionInfo.IsPatched)
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Is Patched", "yes" }));
                }

                if (fileVersionInfo.IsPreRelease)
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Is Pre-release", "yes" }));
                }

                if (fileVersionInfo.IsPrivateBuild)
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Is Private Build", "yes" }));
                }

                if (fileVersionInfo.IsSpecialBuild)
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Is Special Build", "yes" }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.Language))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Language", fileVersionInfo.Language }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.LegalCopyright))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Legal Copyright", fileVersionInfo.LegalCopyright }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.LegalTrademarks))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Legal Trademarks", fileVersionInfo.LegalTrademarks }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.OriginalFilename))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Original Filename", fileVersionInfo.OriginalFilename }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.PrivateBuild))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Private Build Version", fileVersionInfo.PrivateBuild }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.ProductName))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Product Name", fileVersionInfo.ProductName }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.ProductVersion))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Product Version", fileVersionInfo.ProductVersion }));
                }

                if (!IsNullOrEmpty(fileVersionInfo.SpecialBuild))
                {
                    this.fileListView.Items.Add(new ListViewItem(new string[] { "Special Build", fileVersionInfo.SpecialBuild }));
                }

                // set the shortcut details
                if (this.applicationEntry != null && Path.Combine(this.directoryPath, this.applicationEntry) == fileInfo.FullName)
                {
                    this.shortcutCheckBox.Checked = true;
                }
                else
                {
                    this.shortcutCheckBox.Checked = false;
                }
            }
            else
            {
                this.filesGroupBox.Enabled = false;
                this.shortcutCheckBox.Checked = false;
            }
        }

        /// <summary>
        /// Called when the shortcut checkbox becomes checked or unchecked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ShortcutCheckBox_CheckedChanged(object sender, System.EventArgs e)
        {
            FileInfo fileInfo = this.filesTreeView.SelectedNode.Tag as FileInfo;

            if (fileInfo != null)
            {
                this.shortcutErrorProvider.SetError(this.shortcutCheckBox, String.Empty);

                if (!this.shortcutCheckBox.Checked)
                {
                    this.filesTreeView.SelectedNode.NodeFont = new Font(this.filesTreeView.Font, FontStyle.Regular);
                    if (this.applicationEntry == fileInfo.FullName.Substring(this.directoryPath.Length + 1))
                    {
                        this.applicationEntry = null;
                    }
                }
                else
                {
                    this.filesTreeView.SelectedNode.NodeFont = new Font(this.filesTreeView.Font, FontStyle.Bold);
                    this.applicationEntry = fileInfo.FullName.Substring(this.directoryPath.Length + 1);
                }

                // hack to get around re-draw bug in TreeView
                this.filesTreeView.SelectedNode.Text = this.filesTreeView.SelectedNode.Text;
            }
        }

        /// <summary>
        /// Callback for scraping the file system.
        /// </summary>
        /// <param name="obj">The path to scrape.</param>
        private void ScrapeFileSystemCallback(object obj)
        {
            string path = obj as string;
            Debug.Assert(path != null);

            // disable UI
            this.Invoke(new MethodInvoker(this.DisableUI));

            // scrape
            this.packageBuilder.ApplicationRoot = path;
            Wix.Directory directory = this.packageBuilder.GetApplicationRootDirectory();

            // prepare tree view for inserting data
            this.Invoke(new MethodInvoker(this.PrepareUI));

            // insert data
            this.AddDirectory(this.filesTreeView.Nodes, path, directory, directory, true);

            // end data insertion and enable UI
            this.Invoke(new MethodInvoker(this.EnableUI));
        }

        /// <summary>
        /// Disable UI after the user has begun an expensive scrape operation.
        /// </summary>
        private void DisableUI()
        {
            this.filesBrowseButton.Enabled = false;

            if (this.StatusChanging != null)
            {
                this.StatusChanging(this, new StatusChangingEventArgs("Getting files and directories..."));
            }
        }

        /// <summary>
        /// Prepare the UI to receive data after a scrape operation.
        /// </summary>
        private void PrepareUI()
        {
            this.filesTreeView.BeginUpdate();
            this.filesTreeView.Nodes.Clear();
            this.filesTreeView.ImageList.Images.Clear();
        }

        /// <summary>
        /// Enable the UI after the scrape data is ready to be displayed.
        /// </summary>
        private void EnableUI()
        {
            this.filesTreeView.ExpandAll();
            if (this.filesTreeView.Nodes.Count > 0)
            {
                //this.filesTreeView.SelectedNode = this.filesTreeView.Nodes[0];
                //this.filesTreeView.SelectedNode.EnsureVisible();
            }
            this.filesTreeView.EndUpdate();

            this.filesBrowseButton.Enabled = true;

            if (this.StatusChanging != null)
            {
                this.StatusChanging(this, new StatusChangingEventArgs());
            }
        }

        /// <summary>
        /// Add a scraped directory to the UI.
        /// </summary>
        /// <param name="nodes">The NodeCollection under which the new directory should be added.</param>
        /// <param name="rootDirectory">Root of the scraped directory info's.</param>
        /// <param name="directory">The scraped directory to add.</param>
        /// <param name="skip">true if the directory itself shouldn't be added; false otherwise.</param>
        private void AddDirectory(TreeNodeCollection nodes, string currentPath, Wix.Directory rootDirectory, Wix.Directory directory, bool skip)
        {
            // get the directory icon, add it to the image list, then free it immediately
            if (!skip)
            {
                Icon folderIcon = NativeMethods.GetDirectoryIcon(true, false);
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(currentPath, directory.Name));

                TreeNode node = (TreeNode)this.Invoke(this.addTreeNodeCallback, new object[] { nodes, folderIcon, directory.Name, directoryInfo, false });
                folderIcon.Dispose();

                // add sub-directories and files to this node
                nodes = node.Nodes;

                currentPath = Path.Combine(currentPath, directory.Name);
            }

            foreach (Wix.ISchemaElement element in directory.Children)
            {
                Wix.Component component = element as Wix.Component;
                if (null != component)
                {
                    foreach (Wix.ISchemaElement child in component.Children)
                    {
                        Wix.File file = child as Wix.File;
                        if (null != file)
                        {
                            bool selected = false;
                            FileInfo fileInfo = new FileInfo(Path.Combine(currentPath, file.Name));

                            // if there is no application entry point and we've found an executable make this the application entry point
                            if (this.packageBuilder.ApplicationEntry == null && String.Compare(fileInfo.Extension, ".exe", true, CultureInfo.InvariantCulture) == 0)
                            {
                                //this.packageBuilder.ApplicationEntry = fileInfo.FullName.Substring(rootDirectory.FullName.Length + 1);
                                selected = true;
                            }

                            // get the file icon, add it to the image list, then free it immediately
                            Icon fileIcon = NativeMethods.GetFileIcon(fileInfo.FullName, true, false);
                            this.Invoke(this.addTreeNodeCallback, new object[] { nodes, fileIcon, file.Name, fileInfo, selected });
                            fileIcon.Dispose();
                        }
                    }
                }
                else
                {
                    Wix.Directory subDirectory = element as Wix.Directory;
                    if (null != subDirectory)
                    {
                        this.AddDirectory(nodes, currentPath, rootDirectory, subDirectory, false);
                    }
                }
            }
        }

        /// <summary>
        /// Add a new node to the tree view.
        /// </summary>
        /// <param name="nodes">The NodeCollection to add the new node to.</param>
        /// <param name="icon">The icon of the new tree node.</param>
        /// <param name="text">The label Text of the new tree node.</param>
        /// <param name="tag">The tag of the new tree node.</param>
        /// <returns>The new node.</returns>
        private TreeNode AddTreeNode(TreeNodeCollection nodes, Icon icon, string text, object tag, bool selected)
        {
            // add the image to the image list
            this.filesTreeView.ImageList.Images.Add(icon);
            int imageIndex = this.filesTreeView.ImageList.Images.Count - 1;

            // create the node
            TreeNode node = new TreeNode(text, imageIndex, imageIndex);
            node.Tag = tag;

            // add the node
            nodes.Add(node);

            if (selected)
            {
                this.filesTreeView.SelectedNode = node;
                this.shortcutCheckBox.Checked = true;
            }

            return node;
        }

        private void FilesControl_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.VerifyRequiredInformation(false);
        }

        private void FilesControl_Validated(object sender, System.EventArgs e)
        {
            this.VerifyRequiredInformation(true);
        }

        internal bool VerifyRequiredInformation(bool assign)
        {
            bool success = true; // assume all of the data is good to go

            if (this.directoryPath == null || this.directoryPath.Length == 0)
            {
                this.directoryErrorProvider.SetError(this.directoryTextBox, "A valid path must be provided for the root path.");
                success = false;
            }
            else if (assign)
            {
                this.packageBuilder.ApplicationRoot = this.directoryPath;
            }

            if (this.applicationEntry == null || this.applicationEntry.Length == 0)
            {
                this.shortcutErrorProvider.SetError(this.shortcutCheckBox, "One file must be selected as the shortcut entry point for the application.");
                success = false;
            }
            else if (assign)
            {
                this.packageBuilder.ApplicationEntry = this.applicationEntry;
            }

            return success;
        }
    }
}
