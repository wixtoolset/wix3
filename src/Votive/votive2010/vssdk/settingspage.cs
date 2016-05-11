// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Designer.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package
{

    /// <summary>
    /// The base class for property pages.
    /// </summary>
    [CLSCompliant(false), ComVisible(true)]
    public abstract class SettingsPage :
        Microsoft.VisualStudio.Package.LocalizableProperties,
        IPropertyPage
    {
        #region fields
        private Panel panel;
        private bool active;
        private bool dirty = false;
        private IPropertyPageSite site;
        private ProjectNode project;
        private ProjectConfig[] projectConfigs;
        private IVSMDPropertyGrid grid;
        private string name;
        #endregion

        #region properties

        // Remember - all properties that you don't want to appear in the properties grid
        // need to be marked with [Browsable(false)] and [AutomationBrowsable(false)]

        [Browsable(false)]
        [AutomationBrowsable(false)]
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        [Browsable(false)]
        [AutomationBrowsable(false)]
        public ProjectNode ProjectMgr
        {
            get
            {
                return this.project;
            }
        }

        [Browsable(false)]
        [AutomationBrowsable(false)]
        protected IVSMDPropertyGrid Grid
        {
            get { return this.grid; }
        }

        [Browsable(false)]
        [AutomationBrowsable(false)]
        protected bool IsDirty
        {
            get
            {
                return this.dirty;
            }
            set
            {
                if (this.dirty != value)
                {
                    this.dirty = value;
                    if (this.site != null)
                        site.OnStatusChange((uint)(this.dirty ? PropPageStatus.Dirty : PropPageStatus.Clean));
                }
            }
        }

        [Browsable(false)]
        [AutomationBrowsable(false)]
        protected Panel ThePanel
        {
            get
            {
                return this.panel;
            }
        }
        #endregion

        #region abstract methods
        protected abstract void BindProperties();
        protected abstract int ApplyChanges();
        #endregion

        #region public methods
        public object GetTypedConfigProperty(string name, Type type)
        {
            string value = GetConfigProperty(name);
            if (String.IsNullOrEmpty(value)) return null;

            TypeConverter tc = TypeDescriptor.GetConverter(type);
            return tc.ConvertFromInvariantString(value);
        }

        public object GetTypedProperty(string name, Type type)
        {
            string value = GetProperty(name);
            if (String.IsNullOrEmpty(value)) return null;

            TypeConverter tc = TypeDescriptor.GetConverter(type);
            return tc.ConvertFromInvariantString(value);
        }

        public string GetProperty(string propertyName)
        {
            if (this.ProjectMgr != null)
            {
                string property = this.ProjectMgr.BuildProject.GetPropertyValue(propertyName);

                if (property != null)
                {
                    return property;
                }
            }

            return String.Empty;
        }

        // relative to active configuration.
        public string GetConfigProperty(string propertyName)
        {
            if (this.ProjectMgr != null)
            {
                string unifiedResult = null;
                bool cacheNeedReset = true;

                for (int i = 0; i < this.projectConfigs.Length; i++)
                {
                    ProjectConfig config = projectConfigs[i];
                    string property = config.GetConfigurationProperty(propertyName, cacheNeedReset);
                    cacheNeedReset = false;

                    if (property != null)
                    {
                        string text = property.Trim();

                        if (i == 0)
                            unifiedResult = text;
                        else if (unifiedResult != text)
                            return ""; // tristate value is blank then
                    }
                }

                return unifiedResult;
            }

            return String.Empty;
        }

        /// <summary>
        /// Sets the value of a configuration dependent property.
        /// If the attribute does not exist it is created.  
        /// If value is null it will be set to an empty string.
        /// </summary>
        /// <param name="name">property name.</param>
        /// <param name="value">value of property</param>
        public void SetConfigProperty(string name, string value)
        {
            CCITracing.TraceCall();
            if (value == null)
            {
                value = String.Empty;
            }

            if (this.ProjectMgr != null)
            {
                for (int i = 0, n = this.projectConfigs.Length; i < n; i++)
                {
                    ProjectConfig config = projectConfigs[i];

                    config.SetConfigurationProperty(name, value);
                }

                this.ProjectMgr.SetProjectFileDirty(true);
            }
        }

        #endregion

        #region IPropertyPage methods.
        public virtual void Activate(IntPtr parent, RECT[] pRect, int bModal)
        {
            if (this.panel == null)
            {
                this.panel = new Panel();
                this.panel.Size = new Size(pRect[0].right - pRect[0].left, pRect[0].bottom - pRect[0].top);
                this.panel.Text = "Settings";// TODO localization
                this.panel.Visible = false;
                this.panel.Size = new Size(550, 300);
                this.panel.CreateControl();
                NativeMethods.SetParent(this.panel.Handle, parent);
            }

            if (this.grid == null && this.project != null && this.project.Site != null)
            {
                IVSMDPropertyBrowser pb = this.project.Site.GetService(typeof(IVSMDPropertyBrowser)) as IVSMDPropertyBrowser;
                this.grid = pb.CreatePropertyGrid();
            }

            if (this.grid != null)
            {
                this.active = true;


                Control cGrid = Control.FromHandle(new IntPtr(this.grid.Handle));

                cGrid.Parent = Control.FromHandle(parent);//this.panel;
                cGrid.Size = new Size(544, 294);
                cGrid.Location = new Point(3, 3);
                cGrid.Visible = true;
                this.grid.SetOption(_PROPERTYGRIDOPTION.PGOPT_TOOLBAR, false);
                this.grid.GridSort = _PROPERTYGRIDSORT.PGSORT_CATEGORIZED | _PROPERTYGRIDSORT.PGSORT_ALPHABETICAL;
                NativeMethods.SetParent(new IntPtr(this.grid.Handle), this.panel.Handle);
                UpdateObjects();
            }
        }

        public virtual int Apply()
        {
            if (IsDirty)
            {
                return this.ApplyChanges();
            }
            return VSConstants.S_OK;
        }

        public virtual void Deactivate()
        {
            if (null != this.panel)
            {
                this.panel.Dispose();
                this.panel = null;
            }
            this.active = false;
        }

        public virtual void GetPageInfo(PROPPAGEINFO[] arrInfo)
        {
            PROPPAGEINFO info = new PROPPAGEINFO();

            info.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
            info.dwHelpContext = 0;
            info.pszDocString = null;
            info.pszHelpFile = null;
            info.pszTitle = this.name;
            info.SIZE.cx = 550;
            info.SIZE.cy = 300;
            arrInfo[0] = info;
        }

        public virtual void Help(string helpDir)
        {
        }

        public virtual int IsPageDirty()
        {
            // Note this returns an HRESULT not a Bool.
            return (IsDirty ? (int)VSConstants.S_OK : (int)VSConstants.S_FALSE);
        }

        public virtual void Move(RECT[] arrRect)
        {
            RECT r = arrRect[0];

            this.panel.Location = new Point(r.left, r.top);
            this.panel.Size = new Size(r.right - r.left, r.bottom - r.top);
        }

        public virtual void SetObjects(uint count, object[] punk)
        {
            if (count > 0)
            {
                if (punk[0] is ProjectConfig)
                {
                    ArrayList configs = new ArrayList();

                    for (int i = 0; i < count; i++)
                    {
                        ProjectConfig config = (ProjectConfig)punk[i];

                        if (this.project == null)
                        {
                            this.project = config.ProjectMgr;
                        }

                        configs.Add(config);
                    }

                    this.projectConfigs = (ProjectConfig[])configs.ToArray(typeof(ProjectConfig));
                }
                else if (punk[0] is NodeProperties)
                {
                    if (this.project == null)
                    {
                        this.project = (punk[0] as NodeProperties).Node.ProjectMgr;
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
                            provider.GetCfgs(expected[0], configs, actual, null);

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
            }
            else
            {
                this.project = null;
            }

            if (this.active && this.project != null)
            {
                UpdateObjects();
            }
        }


        public virtual void SetPageSite(IPropertyPageSite theSite)
        {
            this.site = theSite;
        }

        public virtual void Show(uint cmd)
        {
            this.panel.Visible = true; // TODO: pass SW_SHOW* flags through      
            this.panel.Show();
        }

        public virtual int TranslateAccelerator(MSG[] arrMsg)
        {
            MSG msg = arrMsg[0];

            if ((msg.message < NativeMethods.WM_KEYFIRST || msg.message > NativeMethods.WM_KEYLAST) && (msg.message < NativeMethods.WM_MOUSEFIRST || msg.message > NativeMethods.WM_MOUSELAST))
                return 1;

            return (NativeMethods.IsDialogMessageA(this.panel.Handle, ref msg)) ? 0 : 1;
        }

        #endregion

        #region helper methods

        protected ProjectConfig[] GetProjectConfigurations()
        {
            return this.projectConfigs;
        }

        protected void UpdateObjects()
        {
            if (this.projectConfigs != null && this.project != null)
            {
                // Demand unmanaged permissions in order to access unmanaged memory.
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();

                IntPtr p = Marshal.GetIUnknownForObject(this);
                IntPtr ppUnk = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));
                try
                {
                    Marshal.WriteIntPtr(ppUnk, p);
                    this.BindProperties();
                    // BUGBUG -- this is really bad casting a pointer to "int"...
                    this.grid.SetSelectedObjects(1, ppUnk.ToInt32());
                    this.grid.Refresh();
                }
                finally
                {
                    if (ppUnk != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(ppUnk);
                    }
                    if (p != IntPtr.Zero)
                    {
                        Marshal.Release(p);
                    }
                }
            }
        }
        #endregion
    }
}
