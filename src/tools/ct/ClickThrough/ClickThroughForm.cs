//-------------------------------------------------------------------------------------------------
// <copyright file="ClickThroughForm.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The main form for the ClickThrough application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Xml;
    using System.Windows.Forms;
    using Microsoft.Tools.WindowsInstallerXml;
    using Scraper = Microsoft.Tools.WindowsInstallerXml.ApplicationModel;

    /// <summary>
    /// The main form for the ClickThrough application.
    /// </summary>
    public class ClickThroughForm : System.Windows.Forms.Form
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.MenuItem menuItemFile;
        private System.Windows.Forms.MenuItem menuItemSave;
        private System.Windows.Forms.MenuItem menuItemExit;
        private System.Windows.Forms.MenuItem menuItemHelp;
        private System.Windows.Forms.MenuItem menuItemAbout;
        private System.Windows.Forms.MenuItem menuItemOpen;
        private System.Windows.Forms.MainMenu mainMenu;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.StatusBar statusBar;
        private System.Windows.Forms.TabPage filesTabPage;
        private System.Windows.Forms.TabPage packageTabPage;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.FilesControl filesControl;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.SummaryControl summaryControl;
        private System.Windows.Forms.MenuItem menuItemSaveWxsFile;
        private System.Windows.Forms.MenuItem menuItemOptions;

        private PackageBuilder packageBuilder;

        /// <summary>
        /// Instantiate a new ClickThroughForm.
        /// </summary>
        public ClickThroughForm()
        {
            // Required for Windows Form Designer support
            this.InitializeComponent();

            this.packageBuilder = new PackageBuilder();

            this.filesControl.PackageBuilder = this.packageBuilder;
            this.summaryControl.PackageBuilder = this.packageBuilder;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // required to enable 32-bit icon support
            Application.EnableVisualStyles();
            Application.DoEvents();

            Application.Run(new ClickThroughForm());
        }

        /// <summary>
        /// Verify that all required information has been filled out.
        /// </summary>
        /// <returns>true if required information is present; false otherwise.</returns>
        internal bool VerifyRequiredInformation()
        {
            bool success = true;

            if (!this.filesControl.VerifyRequiredInformation(true))
            {
                success = false;
            }

            if (!this.summaryControl.VerifyRequiredInformation(true))
            {
                success = false;
            }

            return success;
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

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu = new System.Windows.Forms.MainMenu();
            this.menuItemFile = new System.Windows.Forms.MenuItem();
            this.menuItemOpen = new System.Windows.Forms.MenuItem();
            this.menuItemSave = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuItemExit = new System.Windows.Forms.MenuItem();
            this.menuItemOptions = new System.Windows.Forms.MenuItem();
            this.menuItemSaveWxsFile = new System.Windows.Forms.MenuItem();
            this.menuItemHelp = new System.Windows.Forms.MenuItem();
            this.menuItemAbout = new System.Windows.Forms.MenuItem();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.filesTabPage = new System.Windows.Forms.TabPage();
            this.filesControl = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.FilesControl();
            this.packageTabPage = new System.Windows.Forms.TabPage();
            this.summaryControl = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.SummaryControl();
            this.tabControl.SuspendLayout();
            this.filesTabPage.SuspendLayout();
            this.packageTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                     this.menuItemFile,
                                                                                     this.menuItemOptions,
                                                                                     this.menuItemHelp});
            // 
            // menuItemFile
            // 
            this.menuItemFile.Index = 0;
            this.menuItemFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                         this.menuItemOpen,
                                                                                         this.menuItemSave,
                                                                                         this.menuItem4,
                                                                                         this.menuItemExit});
            this.menuItemFile.Text = "&File";
            // 
            // menuItemOpen
            // 
            this.menuItemOpen.Enabled = false;
            this.menuItemOpen.Index = 0;
            this.menuItemOpen.Text = "&Open";
            this.menuItemOpen.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // menuItemSave
            // 
            this.menuItemSave.Index = 1;
            this.menuItemSave.Text = "&Save";
            this.menuItemSave.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // menuItem4
            // 
            this.menuItem4.Index = 2;
            this.menuItem4.Text = "-";
            // 
            // menuItemExit
            // 
            this.menuItemExit.Index = 3;
            this.menuItemExit.Text = "E&xit";
            this.menuItemExit.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // menuItemOptions
            // 
            this.menuItemOptions.Index = 1;
            this.menuItemOptions.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                            this.menuItemSaveWxsFile});
            this.menuItemOptions.Text = "&Options";
            // 
            // menuItemSaveWxsFile
            // 
            this.menuItemSaveWxsFile.Index = 0;
            this.menuItemSaveWxsFile.RadioCheck = true;
            this.menuItemSaveWxsFile.Text = "&Export .wxs during build";
            this.menuItemSaveWxsFile.Click += new System.EventHandler(this.MenuItemSaveWxsFile_Click);
            // 
            // menuItemHelp
            // 
            this.menuItemHelp.Index = 2;
            this.menuItemHelp.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                         this.menuItemAbout});
            this.menuItemHelp.Text = "&Help";
            // 
            // menuItemAbout
            // 
            this.menuItemAbout.Index = 0;
            this.menuItemAbout.Text = "&About";
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 384);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(552, 22);
            this.statusBar.TabIndex = 0;
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.Filter = "ClickThrough Build Data (*.cbd)|*.cbd";
            // 
            // openFileDialog
            // 
            this.openFileDialog.Filter = "ClickThrough Build Data (*.cbd)|*.cbd";
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.filesTabPage);
            this.tabControl.Controls.Add(this.packageTabPage);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(552, 384);
            this.tabControl.TabIndex = 1;
            // 
            // filesTabPage
            // 
            this.filesTabPage.Controls.Add(this.filesControl);
            this.filesTabPage.Location = new System.Drawing.Point(4, 22);
            this.filesTabPage.Name = "filesTabPage";
            this.filesTabPage.Size = new System.Drawing.Size(544, 358);
            this.filesTabPage.TabIndex = 0;
            this.filesTabPage.Text = "Files";
            // 
            // filesControl
            // 
            this.filesControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filesControl.Location = new System.Drawing.Point(0, 0);
            this.filesControl.Name = "filesControl";
            this.filesControl.Size = new System.Drawing.Size(544, 358);
            this.filesControl.TabIndex = 0;
            // 
            // packageTabPage
            // 
            this.packageTabPage.Controls.Add(this.summaryControl);
            this.packageTabPage.Location = new System.Drawing.Point(4, 22);
            this.packageTabPage.Name = "packageTabPage";
            this.packageTabPage.Size = new System.Drawing.Size(544, 337);
            this.packageTabPage.TabIndex = 1;
            this.packageTabPage.Text = "Package";
            // 
            // summaryControl
            // 
            this.summaryControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.summaryControl.Location = new System.Drawing.Point(0, 0);
            this.summaryControl.Name = "summaryControl";
            this.summaryControl.Size = new System.Drawing.Size(544, 337);
            this.summaryControl.TabIndex = 0;
            // 
            // ClickThroughForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(552, 406);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.statusBar);
            this.Menu = this.mainMenu;
            this.MinimumSize = new System.Drawing.Size(560, 440);
            this.Name = "ClickThroughForm";
            this.Text = "ClickThrough for Isolated Applications";
            this.tabControl.ResumeLayout(false);
            this.filesTabPage.ResumeLayout(false);
            this.packageTabPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        /// <summary>
        /// Called when the File->Open menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OpenMenuItem_Click(object sender, System.EventArgs e)
        {
            if (DialogResult.OK == this.openFileDialog.ShowDialog(this))
            {
                this.packageBuilder.Load(this.openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Called when the File->Save menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void SaveMenuItem_Click(object sender, System.EventArgs e)
        {
            if (DialogResult.OK == this.saveFileDialog.ShowDialog(this))
            {
                this.packageBuilder.Save(this.saveFileDialog.FileName);
            }
        }

        /// <summary>
        /// Called when the Options->Save .wxs File menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void MenuItemSaveWxsFile_Click(object sender, System.EventArgs e)
        {
            this.menuItemSaveWxsFile.Checked = !this.menuItemSaveWxsFile.Checked;
            this.summaryControl.SaveWxsFile = this.menuItemSaveWxsFile.Checked;
        }

        /// <summary>
        /// Called when the File->Exit menu item is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ExitMenuItem_Click(object sender, System.EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// Called when the status change is occurring.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ChangeStatus(object sender, StatusChangingEventArgs e)
        {
            if (e.Message != null)
            {
                this.statusBar.Text = e.Message;
            }
            else
            {
                this.statusBar.Text = String.Empty;
            }
        }
    }
}
