// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Microsoft.Tools.WindowsInstallerXml;

    using IIs = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.IIs;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The web site harvester for the Windows Installer XML Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsWebSiteHarvester : HarvesterExtension
    {
        /// <summary>
        /// Harvest a WiX document.
        /// </summary>
        /// <param name="argument">The argument for harvesting.</param>
        /// <returns>The harvested Fragment.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            DirectoryHarvester directoryHarvester = new DirectoryHarvester();
            directoryHarvester.Core = this.Core;
            directoryHarvester.KeepEmptyDirectories = true;

            IIsWebSiteHarvester iisWebSiteHarvester = new IIsWebSiteHarvester();
            iisWebSiteHarvester.Core = this.Core;

            IIs.WebSite webSite = iisWebSiteHarvester.HarvestWebSite(argument);

            Wix.Component component = new Wix.Component();
            component.AddChild(new Wix.CreateFolder());
            component.AddChild(webSite);

            this.Core.RootDirectory = webSite.Directory;
            Wix.Directory directory = directoryHarvester.HarvestDirectory(webSite.Directory, true);
            directory.AddChild(component);

            Wix.Fragment fragment = new Wix.Fragment();
            fragment.AddChild(directory);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a web site.
        /// </summary>
        /// <param name="name">The name of the web site.</param>
        /// <returns>The harvested web site.</returns>
        public IIs.WebSite HarvestWebSite(string name)
        {
            try
            {
                DirectoryEntry directoryEntry = new DirectoryEntry("IIS://localhost/W3SVC");

                foreach (DirectoryEntry childEntry in directoryEntry.Children)
                {
                    if ("IIsWebServer" == childEntry.SchemaClassName)
                    {
                        if (String.Equals((string)childEntry.Properties["ServerComment"].Value, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return this.HarvestWebSite(childEntry);
                        }
                    }
                }
            }
            catch (COMException ce)
            {
                // 0x8007005 - access denied
                // If we don't have permission to harvest a website, it's likely because we're on
                // Vista or higher and aren't an Admin.
                if ((0x80070005 == unchecked((uint)ce.ErrorCode)))
                {
                    throw new WixException(IIsErrors.InsufficientPermissionHarvestWebSite());
                }
                // 0x80005000 - unknown error
                else if ((0x80005000 == unchecked((uint)ce.ErrorCode)))
                {
                    throw new WixException(IIsErrors.CannotHarvestWebSite());
                }
            }

            throw new WixException(IIsErrors.WebSiteNotFound(name));
        }

        /// <summary>
        /// Harvest a web site.
        /// </summary>
        /// <param name="webSiteEntry">The web site directory entry.</param>
        /// <returns>The harvested web site.</returns>
        private IIs.WebSite HarvestWebSite(DirectoryEntry webSiteEntry)
        {
            IIs.WebSite webSite = new IIs.WebSite();

            foreach (string propertyName in webSiteEntry.Properties.PropertyNames)
            {
                PropertyValueCollection property = webSiteEntry.Properties[propertyName];
                PropertyValueCollection parentProperty = webSiteEntry.Parent.Properties[propertyName];

                if (null == parentProperty.Value || parentProperty.Value.ToString() != property.Value.ToString())
                {
                    switch (propertyName)
                    {
                        case "SecureBindings":
                            IIs.WebAddress secureWebAddress = this.HarvestBindings(propertyName, property);
                            if (null != secureWebAddress)
                            {
                                webSite.AddChild(secureWebAddress);
                            }
                            break;
                        case "ServerBindings":
                            IIs.WebAddress webAddress = this.HarvestBindings(propertyName, property);
                            if (null != webAddress)
                            {
                                webSite.AddChild(webAddress);
                            }
                            break;
                        case "ServerComment":
                            webSite.Description = (string)property.Value;
                            break;
                    }
                }
            }

            foreach (DirectoryEntry childEntry in webSiteEntry.Children)
            {
                switch (childEntry.SchemaClassName)
                {
                    case "IIsFilters":
                        string loadOrder = (string)childEntry.Properties["FilterLoadOrder"].Value;
                        if (loadOrder.Length > 0)
                        {
                            string[] filterNames = loadOrder.Split(",".ToCharArray());

                            for (int i = 0; i < filterNames.Length; i++)
                            {
                                using (DirectoryEntry webFilterEntry = new DirectoryEntry(String.Concat(childEntry.Path, '/', filterNames[i])))
                                {
                                    IIs.WebFilter webFilter = this.HarvestWebFilter(webFilterEntry);

                                    webFilter.LoadOrder = (i + 1).ToString(CultureInfo.InvariantCulture);

                                    webSite.AddChild(webFilter);
                                }
                            }
                        }
                        break;
                    case "IIsWebDirectory":
                        this.HarvestWebDirectory(childEntry, webSite);
                        break;
                    case "IIsWebVirtualDir":
                        foreach (string propertyName in childEntry.Properties.PropertyNames)
                        {
                            PropertyValueCollection property = childEntry.Properties[propertyName];

                            switch (propertyName)
                            {
                                case "Path":
                                    webSite.Directory = (string)property.Value;
                                    break;
                            }
                        }

                        IIs.WebDirProperties webDirProps = this.HarvestWebDirProperties(childEntry);
                        if (null != webDirProps)
                        {
                            webSite.AddChild(webDirProps);
                        }

                        foreach (DirectoryEntry child2Entry in childEntry.Children)
                        {
                            switch (child2Entry.SchemaClassName)
                            {
                                case "IIsWebDirectory":
                                    this.HarvestWebDirectory(child2Entry, webSite);
                                    break;
                                case "IIsWebVirtualDir":
                                    this.HarvestWebVirtualDir(child2Entry, webSite);
                                    break;
                            }
                        }
                        break;
                }
            }

            return webSite;
        }

        /// <summary>
        /// Harvest bindings.
        /// </summary>
        /// <param name="propertyName">The property name of the bindings property.</param>
        /// <param name="bindingsProperty">The bindings property.</param>
        /// <returns>The harvested bindings.</returns>
        private IIs.WebAddress HarvestBindings(string propertyName, PropertyValueCollection bindingsProperty)
        {
            if (1 == bindingsProperty.Count)
            {
                IIs.WebAddress webAddress = new IIs.WebAddress();

                string[] bindings = ((string)bindingsProperty[0]).Split(":".ToCharArray());

                if (0 < bindings[0].Length)
                {
                    webAddress.IP = bindings[0];
                }

                if (0 < bindings[1].Length)
                {
                    webAddress.Port = bindings[1];
                }

                if (0 < bindings[2].Length)
                {
                    webAddress.Header = bindings[2];
                }

                if ("SecureBindings" == propertyName)
                {
                    webAddress.Secure = IIs.YesNoType.yes;
                }

                return webAddress;
            }

            return null;
        }

        /// <summary>
        /// Harvest a web directory.
        /// </summary>
        /// <param name="webDirectoryEntry">The web directory directory entry.</param>
        /// <param name="webSite">The parent web site.</param>
        private void HarvestWebDirectory(DirectoryEntry webDirectoryEntry, IIs.WebSite webSite)
        {
            foreach (DirectoryEntry childEntry in webDirectoryEntry.Children)
            {
                switch (childEntry.SchemaClassName)
                {
                    case "IIsWebDirectory":
                        this.HarvestWebDirectory(childEntry, webSite);
                        break;
                    case "IIsWebVirtualDir":
                        this.HarvestWebVirtualDir(childEntry, webSite);
                        break;
                }
            }

            IIs.WebDirProperties webDirProperties = this.HarvestWebDirProperties(webDirectoryEntry);

            if (null != webDirProperties)
            {
                IIs.WebDir webDir = new IIs.WebDir();

                int indexOfRoot = webDirectoryEntry.Path.IndexOf("ROOT/", StringComparison.OrdinalIgnoreCase);
                webDir.Path = webDirectoryEntry.Path.Substring(indexOfRoot + 5);

                webDir.AddChild(webDirProperties);

                webSite.AddChild(webDir);
            }
        }

        /// <summary>
        /// Harvest a web filter.
        /// </summary>
        /// <param name="webFilterEntry">The web filter directory entry.</param>
        /// <returns>The harvested web filter.</returns>
        private IIs.WebFilter HarvestWebFilter(DirectoryEntry webFilterEntry)
        {
            IIs.WebFilter webFilter = new IIs.WebFilter();

            webFilter.Name = webFilterEntry.Name;

            foreach (string propertyName in webFilterEntry.Properties.PropertyNames)
            {
                PropertyValueCollection property = webFilterEntry.Properties[propertyName];

                switch (propertyName)
                {
                    case "FilterDescription":
                        webFilter.Description = (string)property.Value;
                        break;
                    case "FilterFlags":
                        webFilter.Flags = (int)property.Value;
                        break;
                    case "FilterPath":
                        webFilter.Path = (string)property.Value;
                        break;
                }
            }

            return webFilter;
        }

        /// <summary>
        /// Harvest a web directory's properties.
        /// </summary>
        /// <param name="directoryEntry">The web directory directory entry.</param>
        /// <returns>The harvested web directory's properties.</returns>
        private IIs.WebDirProperties HarvestWebDirProperties(DirectoryEntry directoryEntry)
        {
            bool foundProperties = false;
            IIs.WebDirProperties webDirProperties = new IIs.WebDirProperties();

            // Cannot read properties for "iisadmin" site.
            if (String.Equals("iisadmin", directoryEntry.Name, StringComparison.OrdinalIgnoreCase) &&
                String.Equals("ROOT", directoryEntry.Parent.Name, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            foreach (string propertyName in directoryEntry.Properties.PropertyNames)
            {
                PropertyValueCollection property = directoryEntry.Properties[propertyName];
                PropertyValueCollection parentProperty = directoryEntry.Parent.Properties[propertyName];

                if (null == parentProperty.Value || parentProperty.Value.ToString() != property.Value.ToString())
                {
                    switch (propertyName)
                    {
                        case "AccessFlags":
                            int access = (int)property.Value;

                            if (0x1 == (access & 0x1))
                            {
                                webDirProperties.Read = IIs.YesNoType.yes;
                            }

                            if (0x2 == (access & 0x2))
                            {
                                webDirProperties.Write = IIs.YesNoType.yes;
                            }

                            if (0x4 == (access & 0x4))
                            {
                                webDirProperties.Execute = IIs.YesNoType.yes;
                            }

                            if (0x200 == (access & 0x200))
                            {
                                webDirProperties.Script = IIs.YesNoType.yes;
                            }

                            foundProperties = true;
                            break;
                        case "AuthFlags":
                            int authorization = (int)property.Value;

                            if (0x1 == (authorization & 0x1))
                            {
                                webDirProperties.AnonymousAccess = IIs.YesNoType.yes;
                            }

                            if (0x2 == (authorization & 0x2))
                            {
                                webDirProperties.BasicAuthentication = IIs.YesNoType.yes;
                            }

                            if (0x4 == (authorization & 0x4))
                            {
                                webDirProperties.WindowsAuthentication = IIs.YesNoType.yes;
                            }

                            if (0x10 == (authorization & 0x10))
                            {
                                webDirProperties.DigestAuthentication = IIs.YesNoType.yes;
                            }

                            if (0x40 == (authorization & 0x40))
                            {
                                webDirProperties.PassportAuthentication = IIs.YesNoType.yes;
                            }

                            foundProperties = true;
                            break;
                    }
                }
            }

            return foundProperties ? webDirProperties : null;
        }

        /// <summary>
        /// Harvest a web virtual directory.
        /// </summary>
        /// <param name="webVirtualDirEntry">The web virtual directory directory entry.</param>
        /// <param name="webSite">The parent web site.</param>
        private void HarvestWebVirtualDir(DirectoryEntry webVirtualDirEntry, IIs.WebSite webSite)
        {
            IIs.WebVirtualDir webVirtualDir = new IIs.WebVirtualDir();

            foreach (string propertyName in webVirtualDirEntry.Properties.PropertyNames)
            {
                PropertyValueCollection property = webVirtualDirEntry.Properties[propertyName];
                PropertyValueCollection parentProperty = webVirtualDirEntry.Parent.Properties[propertyName];

                if (null == parentProperty.Value || parentProperty.Value.ToString() != property.Value.ToString())
                {
                    switch (propertyName)
                    {
                        case "Path":
                            webVirtualDir.Directory = (string)property.Value;
                            break;
                    }
                }
            }

            int indexOfRoot = webVirtualDirEntry.Path.IndexOf("ROOT/", StringComparison.OrdinalIgnoreCase);
            webVirtualDir.Alias = webVirtualDirEntry.Path.Substring(indexOfRoot + 5);

            IIs.WebDirProperties webDirProps = this.HarvestWebDirProperties(webVirtualDirEntry);
            if (webDirProps != null)
            {
                webVirtualDir.AddChild(webDirProps);
            }

            foreach (DirectoryEntry childEntry in webVirtualDirEntry.Children)
            {
                switch (childEntry.SchemaClassName)
                {
                    case "IIsWebDirectory":
                        this.HarvestWebDirectory(childEntry, webSite);
                        break;
                    case "IIsWebVirtualDir":
                        this.HarvestWebVirtualDir(childEntry, webSite);
                        break;
                }
            }

            webSite.AddChild(webVirtualDir);
        }
    }
}
