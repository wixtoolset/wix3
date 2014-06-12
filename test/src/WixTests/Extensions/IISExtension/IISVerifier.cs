//-------------------------------------------------------------------------------------------------
// <copyright file="IISVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification for IIS Extension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using Microsoft.Web.Administration;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Contains methods for IIS Extension test verification
    /// </summary>
    public static class IISVerifier
    {
        /// <summary>
        /// URL to the server used
        /// </summary>
        public const string ServerURL = "IIS://localhost/W3SVC";

        /// <summary>
        /// Will return an object containing the value of the property under the root path.
        /// </summary>
        /// <param name="propertyName">Name of the Property</param>
        /// <returns>The value of the property specified</returns>
        public static object GetMetaBasePropertyValue(string propertyName)
        {
            string metabasePath = IISVerifier.ServerURL;
            return GetMetaBasePropertyValue(metabasePath, propertyName);
        }

        /// <summary>
        /// Will return an object containing the value of the property.
        /// </summary>
        /// <param name="metabasePath">The metabase path to use</param>
        /// <param name="propertyName">Name of the Property</param>
        /// <returns>The value of the property specified</returns>
        public static object GetMetaBasePropertyValue(string metabasePath, string propertyName)
        {
            PropertyValueCollection propValues = GetMetaBaseProperty(metabasePath, propertyName);
            if (null != propValues)
            {
                return propValues.Value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Will return an object containing the property.
        /// </summary>
        /// <param name="metabasePath">The metabase path to use</param>
        /// <param name="propertyName">Name of the Property</param>
        /// <returns>The property specified if found, null otherwise</returns>
        public static PropertyValueCollection GetMetaBaseProperty(string metabasePath, string propertyName)
        {
            DirectoryEntry path = new DirectoryEntry(metabasePath);
            PropertyValueCollection propValues = path.Properties[propertyName];

            return propValues;
        }

        /// <summary>
        /// Checks if a web directory exsts.
        /// </summary>
        /// <param name="webdirname">Web directory path to find</param>
        /// <param name="sitename">Web Site name</param>
        /// <returns> Return true/false </returns>
        public static bool WebDirExist(string webdirName, string siteName)
        {
            string siteId = GetWebSiteId(siteName);
            if (string.IsNullOrEmpty(siteId))
            {
                return false;
            }

            string metabasePath = string.Format("{0}/{1}/{2}", IISVerifier.ServerURL , siteId, "root");
            return WebObjectExist(metabasePath, webdirName, "IISWebDirectory");
        }

        /// <summary>
        /// Checks if a Filter exsts.
        /// </summary>
        /// <param name="filterName">Filter to find</param>
        /// <param name="siteName">Web Site name</param>
        /// <param name="isGlobal">Is the filter a global filter, SiteName is igonered if true</param>
        /// <returns> Return true/false </returns>
        public static bool FilterExists(string filterName, string siteName, bool isGlobal)
        {
            string metabasePath;
            if (isGlobal)
            {
                metabasePath = string.Format("{0}/{1}", IISVerifier.ServerURL, "filters");
            }
            else
            {
                string siteId = GetWebSiteId(siteName);
                if (! string.IsNullOrEmpty(siteId))
                {
                    metabasePath = string.Format("{0}/{1}/{2}", IISVerifier.ServerURL, siteId, "filters");
                }
                else
                {
                    return false;
                }
            }
            return WebObjectExist(metabasePath, filterName, "IIsFilter");
        }

        /// <summary>
        /// Will return True\False if the virtual directory exists.
        /// </summary>
        /// <param name="siteName">Site name</param>
        /// <param name="siteName">virtual Directory nameWebSite name to find</param>
        /// <returns>Return true/false</returns>
        public static bool VirtualDirExist(string virtualDirectoryName, string siteName)
        {
            string siteId = GetWebSiteId(siteName);
            if (string.IsNullOrEmpty(siteId))
            {
                return false;
            }

            string metabasePath = string.Format("{0}/{1}/{2}", IISVerifier.ServerURL, siteId, "root");
            return WebObjectExist(metabasePath, virtualDirectoryName, "IISWebVirtualDir");
        }

        /// <summary>
        /// Checks if a WebApplication exsts.
        /// </summary>
        /// <param name="applicationName">Application to find</param>
        /// <param name="virtualDirectoryName">VirtualDir name to look under</param>
        /// <param name="siteName">Web Site name</param>
        /// <returns> Return true/false </returns>
        public static bool WebApplicationExist(string applicationName, string virtualDirectoryName, string siteName)
        {
            string siteId = GetWebSiteId(siteName);
            if (string.IsNullOrEmpty(siteId))
            {
                return false;
            }

            string metabasePath = string.Format("{0}/{1}/{2}/{3}", IISVerifier.ServerURL, siteId, "root", virtualDirectoryName);
            string propertyValue = (string) GetMetaBasePropertyValue(metabasePath, "AppFriendlyName");

            return applicationName.StartsWith(propertyValue); // application names are truncated
        }

        /// <summary>
        /// Checks if a CustomHeadder exsts.
        /// </summary>
        /// <param name="customHeadder">CustomHeadder to find</param>
        /// <param name="virtualDirectoryName">VirtualDir name to look under</param>
        /// <param name="siteName">Web Site name</param>
        /// <returns> Return true/false </returns>
        public static bool CustomHeadderExist(string customHeadder, string virtualDirectoryName, string siteName)
        {
            string siteId = GetWebSiteId(siteName);
            if (string.IsNullOrEmpty(siteId))
            {
                return false;
            }

            string metabasePath = string.Format("{0}/{1}/{2}/{3}", IISVerifier.ServerURL, siteId, "root", virtualDirectoryName);
            string propertyValue = (string)GetMetaBasePropertyValue(metabasePath, "HttpCustomHeaders");

            return customHeadder.Equals(propertyValue); 
        }

        /// <summary>
        /// Checks if a Web Service Extension exsts.
        /// </summary>
        /// <param name="serviceExtensionName">Web Service Extension to find</param>
        /// <returns> Return true/false </returns>
        public static bool WebServiceExtensionExists(string serviceExtensionName)
        {
            object[] serviceExtensionList = (object[])GetMetaBasePropertyValue("WebSvcExtRestrictionList");

            foreach (string serviceExtension in serviceExtensionList)
            {
                // each entry is of the form {Enabled},{ServiceExtensionName} -- where Enables is either 0 or 1
                if (serviceExtension.Contains(serviceExtensionName)) 
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a Web Service Extension is enabled exsts.
        /// </summary>
        /// <param name="serviceExtensionName">Web Service Extension to find</param>
        /// <returns> Return true if the extension is enabled, false if disables, or the extension does not exist </returns>
        public static bool WebServiceExtensionEnabled(string serviceExtensionName)
        {
            object[] serviceExtensionList = (object[])GetMetaBasePropertyValue("WebSvcExtRestrictionList");

            foreach (string serviceExtension in serviceExtensionList)
            {
                // each entry is of the form {Enabled},{ServiceExtensionName} -- where Enables is either 0 or 1
                if (serviceExtension.Contains(serviceExtensionName))
                {
                    string[] parts = serviceExtension.Split(new char[] { ',' });
                    return Convert.ToInt32(parts[0]) == 1;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a Web Site exsts.
        /// </summary>
        /// <param name="siteName">Web Site to find</param>
        /// <returns> Return true/false </returns>
        public static bool WebSiteExists(string siteName)
        {
            return null != IISVerifier.GetWebSite(siteName);
        }

        /// <summary>
        /// Checks if a Web Site is started.
        /// </summary>
        /// <param name="siteName">Web Site to find</param>
        /// <returns> Return true if the website is started, false if not, or the website does not exist </returns>
        public static bool WebSiteStarted(string siteName)
        {
            Site site = IISVerifier.GetWebSite(siteName);
            if (null != site)
            {
                return site.State == Microsoft.Web.Administration.ObjectState.Started;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a AppPool exsts.
        /// </summary>
        /// <param name="appPoolName">AppPool to find</param>
        /// <returns> Return true/false </returns>
        public static bool AppPoolExists(string appPoolName)
        {
            return null != IISVerifier.GetAppPool(appPoolName);
        }

        /// <summary>
        /// Finds the queue length of an AppPool.
        /// </summary>
        /// <param name="appPoolName">AppPool to find</param>
        /// <returns> The size of the AppPool queue length; or -1 if the apppool does not exist</returns>
        public static long AppPoolQueueLength(string appPoolName)
        {
            ApplicationPool appPool = IISVerifier.GetAppPool(appPoolName);
            if (null != appPool)
            {
                return appPool.QueueLength;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Finds the proecess identity type of an AppPool.
        /// </summary>
        /// <param name="appPoolName">AppPool to find</param>
        /// <returns> The proecess identity type of the AppPool; or null if the apppool does not exist</returns>
        public static string AppPoolProcessIdentity(string appPoolName)
        {
            ApplicationPool appPool = IISVerifier.GetAppPool(appPoolName);
            if (null != appPool)
            {
                return appPool.ProcessModel.IdentityType.ToString();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a Certificate exists
        /// </summary>
        /// <param name="certificateName">The certificate to look for</param>
        /// <param name="storeLocation">The strore location to look under</param>
        /// <returns> Return true/false </returns>
        public static bool CertificateExists(string certificateName, StoreLocation storeLocation)
        {
            X509Certificate certificate = IISVerifier.GetCertificate(certificateName, storeLocation);
            return certificate != null;
        }

        /// <summary>
        /// Will return a WebSite object if it exists in the system.
        /// </summary>
        /// <param name="siteName">WebSite name to find</param>
        /// <returns>WebSite Object if found, null otherwise</returns>
        private static Site GetWebSite(string siteName)
        {
            ServerManager siteManager = new ServerManager();

            foreach (Site site in siteManager.Sites)
            {
                if (site.Name == siteName)
                {
                    return site;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the Id of the website if found
        /// </summary>
        /// <param name="siteName">WebSite name to find</param>
        /// <returns>Web site id if found, null otherwise</returns>
        private static string GetWebSiteId(string siteName)
        {
            Site site = GetWebSite(siteName);

            if (null != site)
            {
                IDictionary<string, string> siteAttributes = site.RawAttributes;
                return siteAttributes["id"];
            }
            return null;
        }

        /// <summary>
        /// Will return a AppPool object if it exists in the system.
        /// </summary>
        /// <param name="appPoolName">AppPool name to find</param>
        /// <returns>AppPool Object if found, null otherwise</returns>
        private static ApplicationPool GetAppPool(string appPoolName)
        {
            ServerManager siteManager = new ServerManager();

            foreach (ApplicationPool app in siteManager.ApplicationPools)
            {
                if (app.Name == appPoolName)
                {
                    return app;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds out if a web object (e.g. WebDir, filter, VirtualDir .. etc) exists
        /// </summary>
        /// <param name="metabasePath">Metabase path to look under</param>
        /// <param name="objectName">Name of the object to find</param>
        /// <param name="keyType">The type of the object</param>
        /// <returns>true if the look up was successful found, false otherwise</returns>
        private static bool WebObjectExist(string metabasePath, string objectName, string keyType)
        {
            DirectoryEntry metabaseDirectoryEntry = new DirectoryEntry(metabasePath);
            DirectoryEntries children = metabaseDirectoryEntry.Children;

            foreach (DirectoryEntry directoryEntry in children)
            {
                if (directoryEntry.Name.Equals(objectName) &&
                    null != directoryEntry.Properties["KeyType"] &&
                    directoryEntry.Properties["KeyType"].Value.ToString().ToLowerInvariant().Equals(keyType.ToLowerInvariant()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Will return a certificate object.
        /// </summary>
        /// <param name="name">Name of the certificate</param>
        /// <param name="storelocation">Name of the Store</param>
        /// <returns>X509Certificate2 certificate object</returns>
        private static X509Certificate2 GetCertificate(string name, StoreLocation storeLocation)
        {
            X509Store certificateStore = new X509Store(storeLocation);
            certificateStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificateCollection = (X509Certificate2Collection)certificateStore.Certificates;
            foreach (X509Certificate2 certificate in certificateCollection)
            {
                if (certificate.FriendlyName == name)
                {
                    return certificate;
                }
            }
            return null;
        }
    }
}