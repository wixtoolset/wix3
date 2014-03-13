//-------------------------------------------------------------------------------------------------
// <copyright file="ClickThroughForm.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//   Form for ClickThrough UI.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools.ClickThrough
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Xml;

    /// <summary>
    /// Form for ClickThrough.
    /// </summary>
    internal partial class ClickThroughForm : Form
    {
        private ClickThroughUIExtension[] extensions;
        private WorkPage workPage;

        /// <summary>
        /// Creates a new form.
        /// </summary>
        public ClickThroughForm()
        {
            this.InitializeComponent();

            this.LoadExtensions();
            this.welcomePage.AddExtensions(this.extensions);
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler Message;

        /// <summary>
        /// Gets the current array of extensions.
        /// </summary>
        public ClickThroughUIExtension[] Extensions
        {
            get { return this.extensions; }
        }

        /// <summary>
        /// Opens a click through file.
        /// </summary>
        /// <param name="sender">Control that sent the open request.</param>
        internal void Open(object sender)
        {
            DialogResult result = this.openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string filePath = this.openFileDialog.FileName;
                string extensionNamespace;
                try
                {
                    using (XmlTextReader xml = new XmlTextReader(filePath))
                    {
                        xml.MoveToContent();
                        extensionNamespace = xml.NamespaceURI;

                        foreach (ClickThroughUIExtension extension in this.Extensions)
                        {
                            Fabricator f = extension.Fabricator;
                            if (f.Namespace == extensionNamespace)
                            {
                                this.ShowWorkPage(this, extension);
                                f.Open(filePath);
                                break;
                            }
                        }
                    }
                }
                finally
                {
                }
            }
        }

        /// <summary>
        /// Shows the work page for the specified extension.
        /// </summary>
        /// <param name="sender">Control that sent the work page request.</param>
        /// <param name="extension">Extension to show the work page for.</param>
        internal void ShowWorkPage(object sender, ClickThroughUIExtension extension)
        {
            this.SuspendLayout();

            this.workPage = new WorkPage();
            this.workPage.Dock = DockStyle.Fill;
            this.workPage.Extension = extension;

            this.Controls.Clear();
            this.Controls.Add(this.workPage);
            this.Controls.Add(this.statusStrip);

            this.ResumeLayout(true);
            this.workPage.Focus();
        }

        /// <summary>
        /// Shows the welcome page.
        /// </summary>
        /// <param name="sender">Control that sent the welcome page request.</param>
        internal void ShowWelcomePage(object sender)
        {
            this.SuspendLayout();

            this.workPage = null;

            this.Controls.Clear();
            this.Controls.Add(this.welcomePage);
            this.Controls.Add(this.statusStrip);

            this.ResumeLayout(true);
            this.welcomePage.Focus();
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        internal void OnMessage(MessageEventArgs mea)
        {
            if (null != this.Message)
            {
                this.Message(this, mea);
            }
        }

        /// <summary>
        /// Loads the fabricator extensions from the app.config
        /// </summary>
        private void LoadExtensions()
        {
            try
            {
                StringCollection extensionList = new StringCollection();

                // read the configuration file (ClickThrough.exe.config)
                AppCommon.ReadConfiguration(extensionList);

                this.extensions = new ClickThroughUIExtension[extensionList.Count];

                // load all extensions
                for (int i = 0; i < extensionList.Count; ++i)
                {
                    string extensionType = extensionList[i];
                    this.extensions[i] = ClickThroughUIExtension.Load(extensionType);
                }
            }
            catch (Exception e)
            {
                this.OnMessage(WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }
        }
    }
}
