// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio.PropertyPages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.Build.BuildEngine;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Abstract base class for a WiX project property page.
    /// </summary>
    [ComVisible(true)]
    [Guid("9F78788F-0BE9-4962-A4D5-2BE8A4C78DF7")]
    internal abstract class WixPropertyPage : IPropertyPage2
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private const int Win32SwHide = 0;

        private bool active;
        private bool isDirty;
        private WixPropertyPagePanel propertyPagePanel;
        private string pageName;
        private IPropertyPageSite pageSite;
        private WixProjectNode project;
        private ProjectConfig[] projectConfigs;
        private PROPPAGEINFO propPageInfo;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixPropertyPage"/> class.
        /// </summary>
        public WixPropertyPage()
        {
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets or sets a value indicating whether this property page has changed its state since
        /// the last call to <see cref="IPropertyPage2.Apply" />.
        /// </summary>
        [Browsable(false)]
        [AutomationBrowsable(false)]
        public bool IsDirty
        {
            get
            {
                return this.isDirty;
            }

            set
            {
                if (this.isDirty != value)
                {
                    this.isDirty = value;
                    if (this.pageSite != null)
                    {
                        this.pageSite.OnStatusChange((uint)(this.isDirty ? PropPageStatus.Dirty : PropPageStatus.Clean));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the name of the property page that will be displayed in the left hand
        /// navigation bar on the VS property page dialog.
        /// </summary>
        [Browsable(false)]
        [AutomationBrowsable(false)]
        public string PageName
        {
            get { return this.pageName; }
            set { this.pageName = value; }
        }

        /// <summary>
        /// Gets the project associated with this property page.
        /// </summary>
        [Browsable(false)]
        [AutomationBrowsable(false)]
        public WixProjectNode ProjectMgr
        {
            get { return this.project; }
        }

        /// <summary>
        /// Returns the project Service Provider (needed to show error messages)
        /// </summary>
        internal System.IServiceProvider Site
        {
            get 
            {
                if (this.project != null)
                {
                    return this.project.Site;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the main control that hosts the property page.
        /// </summary>
        protected WixPropertyPagePanel PropertyPagePanel
        {
            get { return this.propertyPagePanel; }
            private set { this.propertyPagePanel = value; }
        }

        /// <summary>
        /// Gets the currently active collection of configurations for the page.
        /// </summary>
        protected IList<ProjectConfig> ProjectConfigs
        {
            get { return this.projectConfigs; }
        }

        // =========================================================================================
        // IPropertyPage Members
        // =========================================================================================

        /// <summary>
        /// Called when the environment wants us to create our property page.
        /// </summary>
        /// <param name="hwndParent">The HWND of the parent window.</param>
        /// <param name="rects">The bounds of the area that we should fill.</param>
        /// <param name="modal">Indicates whether the dialog box is shown modally or not.</param>
        void IPropertyPage.Activate(IntPtr hwndParent, RECT[] rects, int modal)
        {
            // create the panel control
            this.PropertyPagePanel = this.CreatePropertyPagePanel();
            this.PropertyPagePanel.HookupEvents();

            // we need to create the control so the handle is valid
            this.PropertyPagePanel.CreateControl();

            this.PropertyPagePanel.HelpRequested += new HelpEventHandler(this.PropertyPagePanel_HelpRequested);

            // set our parent
            NativeMethods.SetParent(this.PropertyPagePanel.Handle, hwndParent);

            // set our initial size
            this.ResizeContents(rects[0]);

            this.PropertyPagePanel.BindProperties();
            this.active = true;
            this.IsDirty = false;
        }

        /// <summary>
        /// Called when the environment wants us to create our property page.
        /// </summary>
        /// <param name="hwndParent">The HWND of the parent window.</param>
        /// <param name="rects">The bounds of the area that we should fill.</param>
        /// <param name="modal">Indicates whether the dialog box is shown modally or not.</param>
        void IPropertyPage2.Activate(IntPtr hwndParent, RECT[] rects, int modal)
        {
            ((IPropertyPage)this).Activate(hwndParent, rects, modal);
        }

        /// <summary>
        /// Applies the changes made on the property page to the bound objects.
        /// </summary>
        /// <returns>
        /// <b>S_OK</b> if the changes were successfully applied and the property page is current with the bound objects;
        /// <b>S_FALSE</b> if the changes were applied, but the property page cannot determine if its state is current with the objects.
        /// </returns>
        int IPropertyPage.Apply()
        {
            if (this.PropertyPagePanel != null)
            {
                this.PropertyPagePanel.Apply();
            }

            // Changes are all applied to the build project in real-time, so nothing to do here.
            this.IsDirty = false;
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Applies the changes made on the property page to the bound objects.
        /// </summary>
        void IPropertyPage2.Apply()
        {
            ErrorHandler.ThrowOnFailure(((IPropertyPage)this).Apply());
        }

        /// <summary>
        /// The environment calls this to notify us that we should clean up our resources.
        /// </summary>
        void IPropertyPage.Deactivate()
        {
            if (this.PropertyPagePanel != null)
            {
                this.PropertyPagePanel.Dispose();
                this.PropertyPagePanel = null;
            }

            this.active = false;
        }

        /// <summary>
        /// The environment calls this to notify us that we should clean up our resources.
        /// </summary>
        void IPropertyPage2.Deactivate()
        {
            ((IPropertyPage)this).Deactivate();
        }

        /// <summary>
        /// Specifies which field is to receive the focus when the property page is activated.
        /// </summary>
        /// <param name="DISPID">The property that is to receive the focus.</param>
        void IPropertyPage2.EditProperty(int DISPID)
        {
        }

        /// <summary>
        /// The environment calls this to get the parameters to describe the property page.
        /// </summary>
        /// <param name="pageInfos">The parameters are returned in this one-sized array.</param>
        void IPropertyPage.GetPageInfo(PROPPAGEINFO[] pageInfos)
        {
            WixHelperMethods.VerifyNonNullArgument(pageInfos, "pageInfos");

            if (this.PropertyPagePanel == null)
            {
                this.PropertyPagePanel = this.CreatePropertyPagePanel();
            }

            PROPPAGEINFO info = new PROPPAGEINFO();

            info.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
            info.dwHelpContext = 0;
            info.pszDocString = null;
            info.pszHelpFile = null;
            info.pszTitle = this.PageName;
            info.SIZE.cx = this.PropertyPagePanel.Width;
            info.SIZE.cy = this.PropertyPagePanel.Height;
            pageInfos[0] = info;
            this.propPageInfo = info;
        }

        /// <summary>
        /// The environment calls this to get the parameters to describe the property page.
        /// </summary>
        /// <param name="pageInfos">The parameters are returned in this one-sized array.</param>
        void IPropertyPage2.GetPageInfo(PROPPAGEINFO[] pageInfos)
        {
            ((IPropertyPage)this).GetPageInfo(pageInfos);
        }

        /// <summary>
        /// Invokes the property page help in response to an end-user request.
        /// </summary>
        /// <param name="pszHelpDir">
        /// String under the HelpDir key in the property page's CLSID information in the registry.
        /// If HelpDir does not exist, this will be the path found in the InProcServer32 entry
        /// minus the server file name. (Note that LocalServer32 is not checked in the current
        /// implementation, since local property pages are not currently supported). 
        /// </param>
        void IPropertyPage.Help(string pszHelpDir)
        {
        }

        /// <summary>
        /// Invokes the property page help in response to an end-user request.
        /// </summary>
        /// <param name="pszHelpDir">
        /// String under the HelpDir key in the property page's CLSID information in the registry.
        /// If HelpDir does not exist, this will be the path found in the InProcServer32 entry
        /// minus the server file name. (Note that LocalServer32 is not checked in the current
        /// implementation, since local property pages are not currently supported). 
        /// </param>
        void IPropertyPage2.Help(string pszHelpDir)
        {
            ((IPropertyPage)this).Help(pszHelpDir);
        }

        /// <summary>
        /// Indicates whether this property page has changed its state since the last call to
        /// <see cref="IPropertyPage2.Apply"/>. The property sheet uses this information to enable
        /// or disable the Apply button in the dialog box.
        /// </summary>
        /// <returns>
        /// <b>S_OK</b> if the value state of the property page is dirty, that is, it has changed
        /// and is different from the state of the bound objects;
        /// <b>S_FALSE</b> if the value state of the page has not changed and is current with that
        /// of the bound objects.
        /// </returns>
        int IPropertyPage.IsPageDirty()
        {
            return (this.IsDirty ? VSConstants.S_OK : VSConstants.S_FALSE);
        }

        /// <summary>
        /// Indicates whether this property page has changed its state since the last call to
        /// <see cref="IPropertyPage2.Apply"/>. The property sheet uses this information to enable
        /// or disable the Apply button in the dialog box.
        /// </summary>
        /// <returns>
        /// <b>S_OK</b> if the value state of the property page is dirty, that is, it has changed
        /// and is different from the state of the bound objects;
        /// <b>S_FALSE</b> if the value state of the page has not changed and is current with that
        /// of the bound objects.
        /// </returns>
        int IPropertyPage2.IsPageDirty()
        {
            return ((IPropertyPage)this).IsPageDirty();
        }

        /// <summary>
        /// Repositions and resizes the property page dialog box according to the contents of
        /// <paramref name="pRect"/>. The rectangle specified by <paramref name="pRect"/> is
        /// treated identically to that passed to <see cref="IPropertyPage2.Activate"/>.
        /// </summary>
        /// <param name="rects">The bounds of the area that we should fill.</param>
        void IPropertyPage.Move(RECT[] rects)
        {
            if (rects.Length != 0)
            {
                this.ResizeContents(rects[0]);
            }
        }

        /// <summary>
        /// Repositions and resizes the property page dialog box according to the contents of
        /// <paramref name="pRect"/>. The rectangle specified by <paramref name="pRect"/> is
        /// treated identically to that passed to <see cref="IPropertyPage2.Activate"/>.
        /// </summary>
        /// <param name="rects">The bounds of the area that we should fill.</param>
        void IPropertyPage2.Move(RECT[] rects)
        {
            ((IPropertyPage)this).Move(rects);
        }

        /// <summary>
        /// The environment calls this to set the currently selected objects that the property page should show.
        /// </summary>
        /// <param name="count">The count of elements in <paramref name="punk"/>.</param>
        /// <param name="punk">An array of <b>IUnknown</b> objects to show in the property page.</param>
        /// <remarks>
        /// We are supposed to cache these objects until we get another call with <paramref name="count"/> = 0.
        /// Also, the environment is supposed to call this before calling <see cref="IPropertyPage2.Activate"/>,
        /// but like all things when interacting with Visual Studio, don't trust that and code defensively.
        /// </remarks>
        void IPropertyPage.SetObjects(uint count, object[] punk)
        {
            if (count == 0)
            {
                if (this.project != null)
                {
                    this.project.OutputTypeChanged -= this.HandleOutputTypeChanged;
                    this.project = null;
                }

                return;
            }

            if (punk[0] is ProjectConfig)
            {
                List<ProjectConfig> configs = new List<ProjectConfig>();

                for (int i = 0; i < count; i++)
                {
                    ProjectConfig config = (ProjectConfig)punk[i];

                    if (this.project == null)
                    {
                        this.project = config.ProjectMgr as WixProjectNode;
                        this.project.OutputTypeChanged += this.HandleOutputTypeChanged;
                    }

                    configs.Add(config);
                }

                this.projectConfigs = configs.ToArray();
            }
            else if (punk[0] is NodeProperties)
            {
                if (this.project == null)
                {
                    this.project = (punk[0] as NodeProperties).Node.ProjectMgr as WixProjectNode;
                    this.project.OutputTypeChanged += this.HandleOutputTypeChanged;
                }

                Dictionary<string, ProjectConfig> configsMap = new Dictionary<string, ProjectConfig>();

                for (int i = 0; i < count; i++)
                {
                    NodeProperties property = (NodeProperties)punk[i];
                    IVsCfgProvider provider;
                    ErrorHandler.ThrowOnFailure(property.Node.ProjectMgr.GetCfgProvider(out provider));
                    uint[] expected = new uint[1];
                    ErrorHandler.ThrowOnFailure(provider.GetCfgs(0, null, expected, null));
                    if (expected[0] > 0)
                    {
                        ProjectConfig[] configs = new ProjectConfig[expected[0]];
                        uint[] actual = new uint[1];
                        int hr = provider.GetCfgs(expected[0], configs, actual, null);
                        if (hr != 0)
                        {
                            Marshal.ThrowExceptionForHR(hr);
                        }

                        foreach (ProjectConfig config in configs)
                        {
                            if (!configsMap.ContainsKey(config.ConfigName))
                            {
                                configsMap.Add(config.ConfigName, config);
                            }
                        }
                    }
                }

                if (configsMap.Count > 0)
                {
                    if (this.projectConfigs == null)
                    {
                        this.projectConfigs = new ProjectConfig[configsMap.Keys.Count];
                    }

                    configsMap.Values.CopyTo(this.projectConfigs, 0);
                }
            }

            if (this.active && this.project != null)
            {
                this.PropertyPagePanel.BindProperties();
                this.IsDirty = false;
            }
        }

        /// <summary>
        /// The environment calls this to set the currently selected objects that the property page should show.
        /// </summary>
        /// <param name="count">The count of elements in <paramref name="punk"/>.</param>
        /// <param name="punk">An array of <b>IUnknown</b> objects to show in the property page.</param>
        /// <remarks>
        /// We are supposed to cache these objects until we get another call with <paramref name="count"/> = 0.
        /// Also, the environment is supposed to call this before calling <see cref="IPropertyPage2.Activate"/>,
        /// but like all things when interacting with Visual Studio, don't trust that and code defensively.
        /// </remarks>
        void IPropertyPage2.SetObjects(uint count, object[] punk)
        {
            ((IPropertyPage)this).SetObjects(count, punk);
        }

        /// <summary>
        /// Initializes a property page and provides the property page object with the
        /// <see cref="IPropertyPageSite"/> interface through which the property page communicates
        /// with the property frame.
        /// </summary>
        /// <param name="site">
        /// The <see cref="IPropertyPageSite"/> that manages and provides services to this property
        /// page within the entire property sheet.
        /// </param>
        void IPropertyPage.SetPageSite(IPropertyPageSite site)
        {
            // pPageSite can be null (on deactivation)
            this.pageSite = site;
        }

        /// <summary>
        /// Initializes a property page and provides the property page object with the
        /// <see cref="IPropertyPageSite"/> interface through which the property page communicates
        /// with the property frame.
        /// </summary>
        /// <param name="site">
        /// The <see cref="IPropertyPageSite"/> that manages and provides services to this property
        /// page within the entire property sheet.
        /// </param>
        void IPropertyPage2.SetPageSite(IPropertyPageSite site)
        {
            ((IPropertyPage)this).SetPageSite(site);
        }

        /// <summary>
        /// Makes the property page dialog box visible or invisible according to the <paramref name="nCmdShow"/>
        /// parameter. If the page is made visible, the page should set the focus to itself, specifically to the
        /// first property on the page.
        /// </summary>
        /// <param name="cmdShow">
        /// Command describing whether to become visible (SW_SHOW or SW_SHOWNORMAL) or hidden (SW_HIDE). No other values are valid for this parameter.
        /// </param>
        void IPropertyPage.Show(uint cmdShow)
        {
            if (this.PropertyPagePanel != null)
            {
                if (cmdShow == Win32SwHide)
                {
                    this.PropertyPagePanel.Hide();
                }
                else
                {
                    this.PropertyPagePanel.Show();
                    this.SetHelpContext();
                }
            }
        }

        /// <summary>
        /// Makes the property page dialog box visible or invisible according to the <paramref name="nCmdShow"/>
        /// parameter. If the page is made visible, the page should set the focus to itself, specifically to the
        /// first property on the page.
        /// </summary>
        /// <param name="cmdShow">
        /// Command describing whether to become visible (SW_SHOW or SW_SHOWNORMAL) or hidden (SW_HIDE). No other values are valid for this parameter.
        /// </param>
        void IPropertyPage2.Show(uint cmdShow)
        {
            ((IPropertyPage)this).Show(cmdShow);
        }

        /// <summary>
        /// Instructs the property page to process the keystroke described in <paramref name="pMsg"/>.
        /// </summary>
        /// <param name="msg">Describes the keystroke to process.</param>
        /// <returns>
        /// <list type="table">
        /// <item><term>S_OK</term><description>The property page handles the accelerator.</description></item>
        /// <item><term>S_FALSE</term><description>The property page handles accelerators, but this one was not useful to it.</description></item>
        /// <item><term>E_NOTIMPL</term><description>The proeprty page does not handle accelerators.</description></item>
        /// </list>
        /// </returns>
        int IPropertyPage.TranslateAccelerator(MSG[] msg)
        {
            WixHelperMethods.VerifyNonNullArgument(msg, "msg");

            // Always return S_FALSE otherwise we hijack all of the accelerators even for the menus
            return VSConstants.S_FALSE;
        }

        /// <summary>
        /// Instructs the property page to process the keystroke described in <paramref name="pMsg"/>.
        /// </summary>
        /// <param name="msg">Describes the keystroke to process.</param>
        /// <returns>
        /// <list type="table">
        /// <item><term>S_OK</term><description>The property page handles the accelerator.</description></item>
        /// <item><term>S_FALSE</term><description>The property page handles accelerators, but this one was not useful to it.</description></item>
        /// <item><term>E_NOTIMPL</term><description>The proeprty page does not handle accelerators.</description></item>
        /// </list>
        /// </returns>
        int IPropertyPage2.TranslateAccelerator(MSG[] msg)
        {
            return ((IPropertyPage)this).TranslateAccelerator(msg);
        }

        // =========================================================================================
        // Public Methods
        // =========================================================================================

        /// <summary>
        /// Gets a project property.
        /// </summary>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>
        /// Value of the property, or null if the property is unset or inconsistent across configurations.
        /// </returns>
        public virtual string GetProperty(string propertyName)
        {
            ProjectProperty property = new ProjectProperty(this.ProjectMgr, propertyName);
            return property.GetValue(false, this.projectConfigs);
        }

        /// <summary>
        /// Gets a property value as a tri-state checkbox value.
        /// </summary>
        /// <param name="propertyName">Name of the property to get.</param>
        /// <returns>Boolean check state, or Indeterminate if the property is
        /// inconsistent across configurations.</returns>
        public virtual CheckState GetPropertyCheckState(string propertyName)
        {
            ProjectProperty property = new ProjectProperty(this.ProjectMgr, propertyName);
            bool? value = property.GetBooleanValue(this.projectConfigs);

            if (!value.HasValue)
            {
                return CheckState.Indeterminate;
            }
            else
            {
                return value.Value ? CheckState.Checked : CheckState.Unchecked;
            }
        }

        /// <summary>
        /// Sets a project property.
        /// </summary>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <param name="value">Value of the property.</param>
        public virtual void SetProperty(string propertyName, string value)
        {
            ProjectProperty property = new ProjectProperty(this.ProjectMgr, propertyName);

            string oldValue = this.GetProperty(propertyName);
            if (!String.Equals(value, oldValue, StringComparison.Ordinal))
            {
                property.SetValue(value, this.projectConfigs);
                this.IsDirty = true;
            }
        }

        /// <summary>
        /// Normalizes a text-field property value by trimming whitespace.
        /// Subclasses may override to do additional normalization.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">Property value entered by the user.</param>
        /// <returns>Normalized property value.</returns>
        public virtual string Normalize(string propertyName, string value)
        {
            WixHelperMethods.VerifyStringArgument(propertyName, "propertyName");
            WixHelperMethods.VerifyNonNullArgument(value, "value");

            return value.Trim();
        }

        /// <summary>
        /// Creates the controls that constitute the property page. This should be safe to re-entrancy.
        /// </summary>
        /// <returns>The newly created main control that hosts the property page.</returns>
        protected abstract WixPropertyPagePanel CreatePropertyPagePanel();

        /// <summary>
        /// Updates the page as necessary when the project output type changed.
        /// </summary>
        /// <param name="source">ProjectNode which sent the event.</param>
        /// <param name="e">Name of the property that changed.</param>
        protected virtual void HandleOutputTypeChanged(object source, PropertyChangedEventArgs e)
        {
            this.PropertyPagePanel.BindProperties();
        }

        /// <summary>
        /// Resizes the property grid to the specified bounds.
        /// </summary>
        /// <param name="newBounds">The total area of the property page.</param>
        private void ResizeContents(RECT newBounds)
        {
            if (this.PropertyPagePanel != null && this.PropertyPagePanel.IsHandleCreated)
            {
                int minimumWidth = 0;
                int minimumHeight = 0;

                if (this.propPageInfo.SIZE.cx != 0)
                {
                    minimumWidth = this.propPageInfo.SIZE.cx;
                    minimumHeight = this.propPageInfo.SIZE.cy;
                }

                // Visual Studio sends us the size of the area in which it wants us to size.
                // However, we don't want to size smaller than the property page's minimum
                // size, which scales according to the screen DPI.
                this.PropertyPagePanel.Bounds = new Rectangle(
                    newBounds.left,
                    newBounds.top,
                    Math.Max(newBounds.right - newBounds.left, minimumWidth),
                    Math.Max(newBounds.bottom - newBounds.top, minimumHeight));
            }
        }

        /// <summary>
        /// Sets the help context into the help service for this property page.
        /// </summary>
        private void SetHelpContext()
        {
            if (this.pageSite != null)
            {
                System.IServiceProvider sp = this.pageSite as System.IServiceProvider;
                if (sp != null)
                {
                    IHelpService helpService = sp.GetService(typeof(IHelpService)) as IHelpService;
                    if (helpService != null)
                    {
                        helpService.AddContextAttribute("Keyword", String.Empty, HelpKeywordType.F1Keyword);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the help event by displaying the property pages help from WiX.chm.
        /// </summary>
        /// <param name="sender">The control sending the event.</param>
        /// <param name="hlpevent">Event parameters.</param>
        private void PropertyPagePanel_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            WixHelperMethods.ShowWixHelp(this.PropertyPagePanel, @"html\votive_property_pages.htm");
        }
    }
}
