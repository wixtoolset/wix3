// <copyright file="PickEntryStep.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//  Second step in the isolated applications UI for MSI builder for ClickThrough.
// </summary>

namespace Microsoft.Tools.WindowsInstallerXml.Extensions.ClickThrough
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;
    using System.IO;
    using System.Threading;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Second step in the isolated application UI.
    /// </summary>
    public sealed partial class PickEntryStep : UserControl
    {
        private Fabricator fabricator;
        private int folderImageIndex;
        private TreeNode entryPointNode;
        private string source;
        private string entryPoint;

        private AddTreeNodeCallback addTreeNodeCallback;

        /// <summary>
        /// Creates a pick entry step.
        /// </summary>
        public PickEntryStep()
        {
            this.InitializeComponent();

            Icon folderIcon = NativeMethods.GetDirectoryIcon(true, false);
            this.treeView.ImageList.Images.Add("folder", folderIcon);
            this.folderImageIndex = this.treeView.ImageList.Images.IndexOfKey("folder");

            this.addTreeNodeCallback = new AddTreeNodeCallback(this.AddTreeNode);
        }

        /// <summary>
        /// Delegate for adding a new node to the tree view.
        /// </summary>
        /// <param name="nodes">The NodeCollection to add the new node to.</param>
        /// <param name="text">The label Text of the new tree node.</param>
        /// <param name="tag">The tag of the new tree node.</param>
        /// <param name="selected">Flag if new node should be selected.</param>
        /// <returns>The new node.</returns>
        private delegate TreeNode AddTreeNodeCallback(TreeNodeCollection nodes, string text, object tag, bool selected);

        /// <summary>
        /// Event fired any time a change is made to the step's properties.
        /// </summary>
        public event PropertyChangedEventHandler Changed;

        /// <summary>
        /// Gets and sets the fabricator for this step.
        /// </summary>
        /// <value>Fabricator.</value>
        public Fabricator Fabricator
        {
            get { return this.fabricator; }
            set { this.fabricator = value; }
        }

        /// <summary>
        /// Gets and sets the source for this step.
        /// </summary>
        /// <value>Source from this step.</value>
        public string Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (this.source != value)
                {
                    this.source = value;
                    if (this.source != null)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback(this.GetDirectoryTreeCallback), this.source);
                    }
                    else
                    {
                        this.Invoke(new MethodInvoker(this.PrepareUI));
                        this.Invoke(new MethodInvoker(this.EnableUI));
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the entry point for this step.
        /// </summary>
        /// <value>Entry point from this step.</value>
        public string EntryPoint
        {
            get
            {
                return this.entryPoint;
            }

            set
            {
                if (this.entryPoint != value)
                {
                    this.entryPoint = value;
                    this.SelectNode(this.entryPoint);
                }
            }
        }

        /// <summary>
        /// Event handler for when an item is double clicked in the tree control.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void TreeView_DoubleClick(object sender, EventArgs e)
        {
            FileInfo fileInfo = this.treeView.SelectedNode.Tag as FileInfo;
            if (fileInfo != null && this.source != null)
            {
                this.entryPoint = fileInfo.FullName.Substring(this.source.Length + 1);
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("EntryPoint"));
                }
            }
        }

        /// <summary>
        /// Callback for scraping the file system.
        /// </summary>
        /// <param name="obj">The path to scrape.</param>
        private void GetDirectoryTreeCallback(object obj)
        {
            string path = obj as string;
            Debug.Assert(path != null);

            // disable UI
            ////this.Invoke(new MethodInvoker(this.fabricator.DisableUI));

            // prepare tree view for inserting data
            this.Invoke(new MethodInvoker(this.PrepareUI));

            // insert data
            this.AddDirectory(this.treeView.Nodes, path, null, true);

            // end data insertion and enable UI
            this.Invoke(new MethodInvoker(this.EnableUI));
        }

        /// <summary>
        /// Prepare the UI to receive data after a scrape operation.
        /// </summary>
        private void PrepareUI()
        {
            this.treeView.BeginUpdate();
            this.treeView.Nodes.Clear();
        }

        /// <summary>
        /// Enable the UI after the scrape data is ready to be displayed.
        /// </summary>
        private void EnableUI()
        {
            if (this.entryPoint != null)
            {
                this.SelectNode(this.entryPoint);
            }

            this.treeView.ExpandAll();
            this.treeView.EndUpdate();
        }

        /// <summary>
        /// Add a directory to the UI.
        /// </summary>
        /// <param name="nodes">The NodeCollection under which the new directory should be added.</param>
        /// <param name="currentPath">Path navigated to thus far.</param>
        /// <param name="directoryName">The directory to add.</param>
        /// <param name="skip">true if the directory itself shouldn't be added; false otherwise.</param>
        private void AddDirectory(TreeNodeCollection nodes, string currentPath, string directoryName, bool skip)
        {
            // get the directory icon, add it to the image list, then free it immediately
            if (!skip)
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(currentPath, directoryName));
                TreeNode node = (TreeNode)this.Invoke(this.addTreeNodeCallback, new object[] { nodes, directoryName, directoryInfo, false });

                // add sub-directories and files to this node
                nodes = node.Nodes;

                currentPath = Path.Combine(currentPath, directoryName);
            }

            foreach (string fileName in Directory.GetFiles(currentPath))
            {
                bool selected = false;
                FileInfo fileInfo = new FileInfo(fileName);

                // if there is no application entry point and we've found an executable make this the application entry point
                if (this.entryPoint == null && String.Compare(fileInfo.Extension, ".exe", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    selected = true;
                }

                this.Invoke(this.addTreeNodeCallback, new object[] { nodes, Path.GetFileName(fileName), fileInfo, selected });
            }

            foreach (string childDirectoryName in Directory.GetDirectories(currentPath))
            {
                this.AddDirectory(nodes, currentPath, Path.GetFileName(childDirectoryName), false);
            }
        }

        /// <summary>
        /// Add a new node to the tree view.
        /// </summary>
        /// <param name="nodes">The NodeCollection to add the new node to.</param>
        /// <param name="text">The label Text of the new tree node.</param>
        /// <param name="tag">The tag of the new tree node.</param>
        /// <param name="selected">Flag if new node should be selected.</param>
        /// <returns>The new node.</returns>
        private TreeNode AddTreeNode(TreeNodeCollection nodes, string text, object tag, bool selected)
        {
            int imageIndex;
            FileInfo fileInfo = tag as FileInfo;
            if (null == fileInfo)
            {
                imageIndex = this.folderImageIndex;
            }
            else
            {
                string index = fileInfo.Extension == ".exe" ? fileInfo.FullName : fileInfo.Extension; // use the full path to the file for exe's and just the extension for everything else
                imageIndex = this.treeView.ImageList.Images.IndexOfKey(index);
                if (-1 == imageIndex)
                {
                    Icon fileIcon = NativeMethods.GetFileIcon(fileInfo.FullName, true, false);
                    this.treeView.ImageList.Images.Add(fileInfo.Extension, fileIcon);

                    imageIndex = this.treeView.ImageList.Images.IndexOfKey(fileInfo.Extension);
                }
            }

            // create the node
            TreeNode node = node = new TreeNode(text, imageIndex, imageIndex);
            node.Name = text;
            node.Tag = tag;

            // add the node
            nodes.Add(node);

            if (selected)
            {
                this.entryPoint = fileInfo.FullName.Substring(this.source.Length + 1);
                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("EntryPoint"));
                }
            }

            return node;
        }

        /// <summary>
        /// Selects a node in the tree based on the entry point.
        /// </summary>
        /// <param name="entryPoint">New entry point of the application.</param>
        private void SelectNode(string entryPoint)
        {
            TreeNode newEntryPointNode = null;
            if (entryPoint != null)
            {
                newEntryPointNode = this.TryFindNode(this.treeView.Nodes, entryPoint);
                if (newEntryPointNode != null)
                {
                    newEntryPointNode.NodeFont = new Font(this.treeView.Font, FontStyle.Bold);
                }
            }

            if (this.entryPointNode != newEntryPointNode)
            {
                if (this.entryPointNode != null)
                {
                    this.entryPointNode.NodeFont = this.treeView.Font;
                }

                this.entryPointNode = newEntryPointNode;
                this.treeView.SelectedNode = this.entryPointNode;
                this.treeView.Update();
            }
        }

        /// <summary>
        /// Finds the node a the full path in the node collection
        /// </summary>
        /// <param name="nodes">Collection of nodes to search for the path in.</param>
        /// <param name="fullPath">Path in the node collection to find.</param>
        /// <returns>TreeNode if path finds a match, otherwise null.</returns>
        private TreeNode TryFindNode(TreeNodeCollection nodes, string fullPath)
        {
            string[] names = fullPath.Split("\\".ToCharArray());
            TreeNode found = null;
            for (int i = 0; i < names.Length; ++i)
            {
                found = nodes[names[i]];
                if (found == null)
                {
                    break;
                }

                nodes = found.Nodes;
            }

            return found;
        }
    }
}
