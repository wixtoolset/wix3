//-------------------------------------------------------------------------------------------------
// <copyright file="SummaryControl.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// User control for summary and details.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Windows.Forms;

    /// <summary>
    /// User control for summary and details.
    /// </summary>
    public class SummaryControl : System.Windows.Forms.UserControl
    {
        private IContainer components;
        private System.Windows.Forms.Label summaryLabel;
        private System.Windows.Forms.TextBox versionTextBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button iconButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.PickIconDialog pickIconDialog1;
        private System.Windows.Forms.Label label1;

        private PackageBuilder packageBuilder;
        private ProgressDialog progressDialog;

        private bool saveWxsFile;
        private Icon icon;
        private string iconFile;
        private Microsoft.Tools.WindowsInstallerXml.ClickThrough.Header header1;
        private int iconIndex;

        private bool versionModified;
        private bool manufacturerModified;
        private bool descriptionModified;
        private bool productModified;
        private bool previousPackageModified;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox manufacturerTextBox;
        private System.Windows.Forms.TextBox applicationNameTextBox;
        private System.Windows.Forms.TextBox updateFeedTextBox;
        private System.Windows.Forms.ErrorProvider applicationNameErrorProvider;
        private System.Windows.Forms.ErrorProvider manufacturerErrorProvider;
        private System.Windows.Forms.ErrorProvider versionErrorProvider;
        private System.Windows.Forms.ErrorProvider updateFeedErrorProvider;
        private System.Windows.Forms.Button buildButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button downloadButton;
        private System.Windows.Forms.TextBox previousPackageTextBox;
        private System.Windows.Forms.Button browseButton;
        private System.Windows.Forms.ErrorProvider previousPackageErrorProvider;
        private System.Windows.Forms.OpenFileDialog previousPackageFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;

        /// <summary>
        /// Instantiate a new SummaryControl class.
        /// </summary>
        public SummaryControl()
        {
            // This call is required by the Windows.Forms Form Designer.
            this.InitializeComponent();

            this.header1.Index = 1;

            this.progressDialog = new ProgressDialog();
            this.progressDialog.Owner = this.FindForm();

            // TODO: Change this back to the path of the assembly when more icons are available via Clickthrough.exe
            // this.iconFile = Assembly.GetExecutingAssembly().Location;
            this.iconFile = String.Empty;
            this.iconIndex = 0;
        }

        /// <summary>
        /// Sets the package builder for the summary information.
        /// </summary>
        public PackageBuilder PackageBuilder
        {
            set
            {
                this.packageBuilder = value;
                this.packageBuilder.Changed += new PropertyChangedEventHandler(packageBuilder_Changed);
                this.packageBuilder.Message += new MessageEventHandler(packageBuilder_Message);
                this.packageBuilder.Progress += new ProgressEventHandler(this.progressDialog.packageBuilder_Progress);
            }
        }

        /// <summary>
        /// Sets the boolean whether the .wxs file should be saved for the summary information.
        /// </summary>
        public bool SaveWxsFile
        {
            set { this.saveWxsFile = value; }
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
        /// Override the background painting to handle display of the program icon with alpha channels.
        /// </summary>
        /// <param name="pevent">The event data.</param>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);

            if (this.icon != null)
            {
                // create the rectangle that will contain the icon, but limit its size to 40x40
                Rectangle iconRectangle = new Rectangle(
                    this.iconButton.Left - 48,
                    this.iconButton.Top,
                    Math.Min(40, this.icon.Width),
                    Math.Min(40, this.icon.Height));

                // clear the surface and draw the icon to avoid losing its alpha channel
                Graphics grfx = Graphics.FromHwnd(this.Handle);
                grfx.FillRectangle(new SolidBrush(this.BackColor), iconRectangle);
                grfx.DrawIcon(this.icon, iconRectangle);
            }
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.summaryLabel = new System.Windows.Forms.Label();
            this.versionTextBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.manufacturerTextBox = new System.Windows.Forms.TextBox();
            this.iconButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.applicationNameTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.pickIconDialog1 = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.PickIconDialog();
            this.header1 = new Microsoft.Tools.WindowsInstallerXml.ClickThrough.Header();
            this.updateFeedTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.applicationNameErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.manufacturerErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.versionErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.updateFeedErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.buildButton = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.downloadButton = new System.Windows.Forms.Button();
            this.previousPackageTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.browseButton = new System.Windows.Forms.Button();
            this.previousPackageErrorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.previousPackageFileDialog = new System.Windows.Forms.OpenFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.applicationNameErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.manufacturerErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.versionErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.updateFeedErrorProvider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.previousPackageErrorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // summaryLabel
            // 
            this.summaryLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.summaryLabel.Location = new System.Drawing.Point(16, 24);
            this.summaryLabel.Name = "summaryLabel";
            this.summaryLabel.Size = new System.Drawing.Size(224, 296);
            this.summaryLabel.TabIndex = 0;
            // 
            // versionTextBox
            // 
            this.versionTextBox.Location = new System.Drawing.Point(104, 121);
            this.versionTextBox.Name = "versionTextBox";
            this.versionTextBox.Size = new System.Drawing.Size(115, 20);
            this.versionTextBox.TabIndex = 7;
            this.versionTextBox.Text = "1.0.0.0";
            this.versionTextBox.TextChanged += new System.EventHandler(this.VersionTextBoxTextChanged);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(52, 121);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(48, 23);
            this.label7.TabIndex = 4;
            this.label7.Text = "&Version:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(337, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(80, 23);
            this.label6.TabIndex = 2;
            this.label6.Text = "&Manufacturer:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // manufacturerTextBox
            // 
            this.manufacturerTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.manufacturerTextBox.Location = new System.Drawing.Point(421, 89);
            this.manufacturerTextBox.Name = "manufacturerTextBox";
            this.manufacturerTextBox.Size = new System.Drawing.Size(104, 20);
            this.manufacturerTextBox.TabIndex = 3;
            this.manufacturerTextBox.TextChanged += new System.EventHandler(this.CompanyNameTextBoxTextChanged);
            // 
            // iconButton
            // 
            this.iconButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.iconButton.Location = new System.Drawing.Point(400, 121);
            this.iconButton.Name = "iconButton";
            this.iconButton.Size = new System.Drawing.Size(77, 27);
            this.iconButton.TabIndex = 9;
            this.iconButton.Text = "&Change...";
            this.iconButton.Click += new System.EventHandler(this.IconButton_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(240, 121);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(97, 23);
            this.label5.TabIndex = 8;
            this.label5.Text = "Add/Remove &Icon:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(36, 153);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 23);
            this.label4.TabIndex = 10;
            this.label4.Text = "&Description:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.descriptionTextBox.Location = new System.Drawing.Point(104, 161);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.Size = new System.Drawing.Size(421, 87);
            this.descriptionTextBox.TabIndex = 11;
            this.descriptionTextBox.TextChanged += new System.EventHandler(this.DescriptionTextBoxTextChanged);
            // 
            // applicationNameTextBox
            // 
            this.applicationNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.applicationNameTextBox.Location = new System.Drawing.Point(104, 89);
            this.applicationNameTextBox.Name = "applicationNameTextBox";
            this.applicationNameTextBox.Size = new System.Drawing.Size(223, 20);
            this.applicationNameTextBox.TabIndex = 1;
            this.applicationNameTextBox.TextChanged += new System.EventHandler(this.ProductNameTextBoxTextChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(4, 89);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 23);
            this.label1.TabIndex = 0;
            this.label1.Text = "Application &Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pickIconDialog1
            // 
            this.pickIconDialog1.IconFile = null;
            this.pickIconDialog1.IconIndex = -1;
            // 
            // header1
            // 
            this.header1.Dock = System.Windows.Forms.DockStyle.Top;
            this.header1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.header1.Index = 0;
            this.header1.Location = new System.Drawing.Point(0, 0);
            this.header1.Name = "header1";
            this.header1.Size = new System.Drawing.Size(540, 88);
            this.header1.TabIndex = 17;
            // 
            // updateFeedTextBox
            // 
            this.updateFeedTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.updateFeedTextBox.Location = new System.Drawing.Point(104, 264);
            this.updateFeedTextBox.Name = "updateFeedTextBox";
            this.updateFeedTextBox.Size = new System.Drawing.Size(421, 20);
            this.updateFeedTextBox.TabIndex = 13;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.Location = new System.Drawing.Point(27, 264);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 18);
            this.label2.TabIndex = 12;
            this.label2.Text = "&Update Feed:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // applicationNameErrorProvider
            // 
            this.applicationNameErrorProvider.ContainerControl = this;
            // 
            // manufacturerErrorProvider
            // 
            this.manufacturerErrorProvider.ContainerControl = this;
            // 
            // versionErrorProvider
            // 
            this.versionErrorProvider.ContainerControl = this;
            // 
            // updateFeedErrorProvider
            // 
            this.updateFeedErrorProvider.ContainerControl = this;
            // 
            // buildButton
            // 
            this.buildButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buildButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buildButton.Location = new System.Drawing.Point(437, 292);
            this.buildButton.Name = "buildButton";
            this.buildButton.Size = new System.Drawing.Size(88, 54);
            this.buildButton.TabIndex = 0;
            this.buildButton.Text = "&Build...";
            this.buildButton.Click += new System.EventHandler(this.buildButton_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "exe";
            this.saveFileDialog.FileName = "setup.exe";
            this.saveFileDialog.Filter = "Setup Bootstrap Executable (*.exe)|*.exe";
            // 
            // downloadButton
            // 
            this.downloadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.downloadButton.Enabled = false;
            this.downloadButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.downloadButton.Location = new System.Drawing.Point(212, 323);
            this.downloadButton.Name = "downloadButton";
            this.downloadButton.Size = new System.Drawing.Size(136, 24);
            this.downloadButton.TabIndex = 16;
            this.downloadButton.Text = "D&ownload from Feed...";
            // 
            // previousPackageTextBox
            // 
            this.previousPackageTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.previousPackageTextBox.Location = new System.Drawing.Point(104, 296);
            this.previousPackageTextBox.Name = "previousPackageTextBox";
            this.previousPackageTextBox.ReadOnly = true;
            this.previousPackageTextBox.Size = new System.Drawing.Size(325, 20);
            this.previousPackageTextBox.TabIndex = 15;
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.Enabled = false;
            this.label3.Location = new System.Drawing.Point(1, 298);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 19);
            this.label3.TabIndex = 14;
            this.label3.Text = "Previous Package:";
            // 
            // browseButton
            // 
            this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.browseButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.browseButton.Location = new System.Drawing.Point(356, 323);
            this.browseButton.Name = "browseButton";
            this.browseButton.Size = new System.Drawing.Size(73, 23);
            this.browseButton.TabIndex = 17;
            this.browseButton.Text = "B&rowse...";
            this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
            // 
            // previousPackageErrorProvider
            // 
            this.previousPackageErrorProvider.ContainerControl = this;
            // 
            // SummaryControl
            // 
            this.Controls.Add(this.browseButton);
            this.Controls.Add(this.previousPackageTextBox);
            this.Controls.Add(this.downloadButton);
            this.Controls.Add(this.buildButton);
            this.Controls.Add(this.updateFeedTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.header1);
            this.Controls.Add(this.versionTextBox);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.manufacturerTextBox);
            this.Controls.Add(this.iconButton);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.descriptionTextBox);
            this.Controls.Add(this.applicationNameTextBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label3);
            this.Name = "SummaryControl";
            this.Size = new System.Drawing.Size(540, 360);
            this.Validated += new System.EventHandler(this.SummaryControl_Validated);
            this.Validating += new System.ComponentModel.CancelEventHandler(this.SummaryControl_Validating);
            ((System.ComponentModel.ISupportInitialize)(this.applicationNameErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.manufacturerErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.versionErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.updateFeedErrorProvider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.previousPackageErrorProvider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        /// <summary>
        /// Called when the change icon button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void IconButton_Click(object sender, System.EventArgs e)
        {
            this.pickIconDialog1.IconFile = this.iconFile;
            this.pickIconDialog1.IconIndex = this.iconIndex;

            if (DialogResult.OK == this.pickIconDialog1.ShowDialog(this))
            {
                this.icon = this.pickIconDialog1.Icon;
                this.iconFile = this.pickIconDialog1.IconFile;
                this.iconIndex = this.pickIconDialog1.IconIndex;
            }
        }

        /// <summary>
        /// Event handler for when the version text box changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void VersionTextBoxTextChanged(object sender, EventArgs e)
        {
            this.versionErrorProvider.SetError(this.versionTextBox, String.Empty);
            this.versionModified = true;
        }

        /// <summary>
        /// Event handler for when the company name text box changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void CompanyNameTextBoxTextChanged(object sender, EventArgs e)
        {
            this.manufacturerErrorProvider.SetError(this.manufacturerTextBox, String.Empty);
            this.manufacturerModified = true;
        }

        /// <summary>
        /// Event handler for when the description text box changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void DescriptionTextBoxTextChanged(object sender, EventArgs e)
        {
            this.descriptionModified = true;
        }

        /// <summary>
        /// Event handler for when the product name text box changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void ProductNameTextBoxTextChanged(object sender, EventArgs e)
        {
            this.applicationNameErrorProvider.SetError(this.applicationNameTextBox, String.Empty);
            this.productModified = true;
        }

        /// <summary>
        /// Event handler for when the Build button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void buildButton_Click(object sender, System.EventArgs e)
        {
            ClickThroughForm parentForm = this.FindForm() as ClickThroughForm;
            if (!parentForm.VerifyRequiredInformation())
            {
                MessageBox.Show(this, "Missing required information.  Be sure to check both tabs for missing information.");
                return;
            }

            string workingDirectory = Environment.CurrentDirectory;
            if (DialogResult.OK == this.saveFileDialog.ShowDialog(this))
            {
                Environment.CurrentDirectory = workingDirectory;
                Cursor savedCursor = Cursor.Current;

                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    this.progressDialog.Reset();
                    this.progressDialog.Show();

                    if (this.saveWxsFile)
                    {
                        this.packageBuilder.Build(this.saveFileDialog.FileName, Path.ChangeExtension(this.saveFileDialog.FileName, "wxs"));
                    }
                    else
                    {
                        this.packageBuilder.Build(this.saveFileDialog.FileName);
                    }
                }
                catch (InvalidOperationException)
                {
                }
                finally
                {
                    this.progressDialog.Hide();
                    this.Cursor = savedCursor;
                }
            }
        }

        /// <summary>
        /// Event handler for when the Browse button is clicked.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void browseButton_Click(object sender, System.EventArgs e)
        {
            if (this.previousPackageFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                this.previousPackageTextBox.Text = this.previousPackageFileDialog.FileName;
                this.previousPackageErrorProvider.SetError(this.previousPackageTextBox, String.Empty);

                this.previousPackageModified = true;
            }
        }

        /// <summary>
        /// Event handler for when the package bulider ("model" for the "view") changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void packageBuilder_Changed(object sender, PropertyChangedEventArgs e)
        {
            if (!this.manufacturerModified || !this.versionModified || !this.productModified || !this.descriptionModified)
            {
                if (this.packageBuilder.ApplicationRoot != null && this.packageBuilder.ApplicationEntry != null)
                {
                    string filePath = Path.Combine(this.packageBuilder.ApplicationRoot, this.packageBuilder.ApplicationEntry);
                    FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(filePath);

                    if (!this.manufacturerModified)
                    {
                        this.manufacturerTextBox.Text = fileVersionInfo.CompanyName;
                        this.manufacturerModified = false;
                    }

                    if (!this.descriptionModified)
                    {
                        this.descriptionTextBox.Text = fileVersionInfo.FileDescription;
                        this.descriptionModified = false;
                    }

                    if (!this.productModified)
                    {
                        this.applicationNameTextBox.Text = fileVersionInfo.ProductName;
                        this.productModified = true;
                    }

                    if (!this.versionModified)
                    {
                        try
                        {
                            // Always set the revision number to zero because Windows Installer 
                            // only expects three part version numbers for Products.
                            Version version = new Version(fileVersionInfo.ProductVersion);
                            version = new Version(version.Major, version.Minor, version.Build, 0);

                            this.versionTextBox.Text = version.ToString();
                            this.versionModified = true;
                        }
                        catch (ArgumentException)
                        {
                        }
                        catch (FormatException)
                        {
                        }
                    }
                }

                if (this.packageBuilder.PreviousPackage != null && !this.previousPackageModified)
                {
                    this.previousPackageTextBox.Text = this.packageBuilder.PreviousPackage;
                }
            }
        }

        /// <summary>
        /// Event handler for when the form is being validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void SummaryControl_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.VerifyRequiredInformation(false);
        }

        /// <summary>
        /// Event handler after the form is validated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void SummaryControl_Validated(object sender, System.EventArgs e)
        {
            this.VerifyRequiredInformation(true);
        }

        /// <summary>
        /// Verify that all required information has been filled out.
        /// </summary>
        /// <param name="assign">Assigns the values in the form to the package builder.</param>
        /// <returns>True if required information is present; false otherwise.</returns>
        internal bool VerifyRequiredInformation(bool assign)
        {
            bool success = true; // assume all of the data is good to go

            if (this.updateFeedTextBox.Text == null || this.updateFeedTextBox.Text.Length == 0)
            {
                this.updateFeedErrorProvider.SetError(this.updateFeedTextBox, "A valid URL must be specified for the Application's update feed.");
                success = false;
            }
            else
            {
                try
                {
                    Uri uri = new Uri(this.updateFeedTextBox.Text);

                    this.updateFeedErrorProvider.SetError(this.updateFeedTextBox, String.Empty);
                    if (assign)
                    {
                        this.packageBuilder.UpdateUrl = uri;
                    }
                }
                catch (UriFormatException)
                {
                    this.updateFeedErrorProvider.SetError(this.updateFeedTextBox, "Invalid update feed URI.");
                }
            }

            if (this.applicationNameTextBox.Text == null || this.applicationNameTextBox.Text.Length == 0)
            {
                this.applicationNameErrorProvider.SetError(this.applicationNameTextBox, "An application name must be specified.");
                success = false;
            }
            else
            {
                this.applicationNameErrorProvider.SetError(this.applicationNameTextBox, String.Empty);
                if (assign)
                {
                    this.packageBuilder.ApplicationName = this.applicationNameTextBox.Text;
                }
            }

            if (this.manufacturerTextBox.Text == null || this.manufacturerTextBox.Text.Length == 0)
            {
                this.manufacturerErrorProvider.SetError(this.manufacturerTextBox, "A manufacturer must be specified.");
                success = false;
            }
            else
            {
                this.manufacturerErrorProvider.SetError(this.manufacturerTextBox, String.Empty);
                if (assign)
                {
                    this.packageBuilder.ManufacturerName = this.manufacturerTextBox.Text;
                }
            }

            if (assign)
            {
                this.packageBuilder.Description = this.descriptionTextBox.Text;
            }

            if (this.versionTextBox.Text == null || this.versionTextBox.Text.Length == 0)
            {
                this.versionErrorProvider.SetError(this.versionTextBox, "A version must be specified.");
                success = false;
            }
            else
            {
                try
                {
                    Version version = new Version(this.versionTextBox.Text);
                    if (version.Revision == -1)
                    {
                        version = new Version(version.Major, version.Minor, version.Build, 0);
                    }

                    if (version.Revision != 0)
                    {
                        this.versionErrorProvider.SetError(this.versionTextBox, "The version must end with a zero for the revision.  The Windows Installer expects 3 part versions, #.#.# so the last part (the revision) must be zero.");
                    }
                    else
                    {
                        this.versionErrorProvider.SetError(this.versionTextBox, String.Empty);
                        if (assign)
                        {
                            this.packageBuilder.Version = version;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    this.versionErrorProvider.SetError(this.versionTextBox, "Invalid version number.  Ensure your version is '#.#.#.#'");
                }
                catch (FormatException)
                {
                    this.versionErrorProvider.SetError(this.versionTextBox, "Invalid version number.  Ensure your version is '#.#.#.#'");
                }
            }

            if (this.previousPackageTextBox.Text == null || this.previousPackageTextBox.Text.Length == 0)
            {
                if (assign)
                {
                    this.packageBuilder.PreviousPackage = null;
                }
            }
            else
            {
                if (File.Exists(this.previousPackageTextBox.Text))
                {
                    this.previousPackageErrorProvider.SetError(this.versionTextBox, String.Empty);
                    if (assign)
                    {
                        this.packageBuilder.PreviousPackage = this.previousPackageTextBox.Text;
                    }
                }
                else
                {
                    this.previousPackageErrorProvider.SetError(this.previousPackageTextBox, "Previous package could not be found.  Please verify that the package exists.");
                }
            }

            return success;
        }

        /// <summary>
        /// Event handler for when the package builder has a message.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void packageBuilder_Message(object sender, MessageEventArgs mea)
        {
            if (mea is ClickThroughError)
            {
                MessageBox.Show(this, String.Format(mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs), "ClickThrough Package Build Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else if (mea is ClickThroughWarning)
            {
                MessageBox.Show(this, String.Format(mea.ResourceManager.GetString(mea.ResourceName), mea.MessageArgs), "ClickThrough Package Build Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
