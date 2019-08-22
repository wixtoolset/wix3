// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using WixTest.Utilities;
    using Microsoft.Win32;
    
    /// <summary>
    /// Uninstalls all packages/payloads in a given layout and deletes registry keys and files that the Burn Engine could leave behind.
    /// The Burn engine is not used to perform the uninstall (in case it is broken).
    /// This is to be used by test code that needs to ensure the machine is in a clean state after a test is performed.
    /// </summary>
    public class EngineCleanup
    {
        // contains all the data to know what to delete
        private LayoutManager.LayoutManager m_Layout;
        private string RegistrationId;
        private List<string> UpdateIds;

        #region Constructors

        /// <summary>
        /// Creates an EngineCleanup object that can be used to clean a machine of all files and regkeys a Burn bundle may have installed.
        /// </summary>
        /// <param name="Layout">Layout to be removed from the machine</param>
        public EngineCleanup(LayoutManager.LayoutManager Layout)
        {
            UpdateIds = new List<string>();
            m_Layout = Layout;
            RegistrationId = this.m_Layout.ActualBundleId;

            try
            {
                //// BUGBUG This needs to be replace by the new WiX authoring that supports this.  It doesn't exist yet. 
                //foreach (WixTest.Burn.OM.BurnManifestOM.Registration.RegistrationElement.UpdateElement updateElement in m_Layout.BurnManifest.Registration.UpdateElements)
                //{
                //    UpdateIds.Add(updateElement.BundleId);
                //}
            }
            catch
            {
                // don't die if this isn't a Bundle that Updates other bundles.
            }
        }

        #endregion

        public void CleanupEverything()
        {
            UninstallAllPayloads();
            DeleteEngineRunRegKeys();
            DeleteBurnArpEntry();
            DeleteEngineDownloadCache();
            DeleteBundleExtractionTempCache();
            DeleteEngineCache();
            DeletePayloadCache();
        }

        /// <summary>
        /// Deletes this regkey if it exists (this key causes setup to resume after a reboot):
        /// HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run  guid (where guid = what is in parameterinfo.xml for  Registration Id="{4C9C8A65-1159-414A-96D0-1815992D0A7F}")
        /// </summary>
        public void DeleteEngineRunRegKeys()
        {
            // clean up the current user
            if (!String.IsNullOrEmpty(RegistrationId))
            {
                try
                {
                    Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue(RegistrationId);
                }
                catch
                {
                    // don't throw if the key couldn't be deleted.  It probably didn't exist.
                }
            }

            // try to remove it for all users on the machine cause another user may have installed this layout and left this key behind
            if (!String.IsNullOrEmpty(RegistrationId))
            {
                foreach (string sid in Registry.Users.GetSubKeyNames())
                {
                    try
                    {
                        Registry.Users.OpenSubKey(sid + @"\Software\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue(RegistrationId);
                    }
                    catch
                    {
                        // don't throw if the key couldn't be deleted.  It probably didn't exist.
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the ARP entry (if it exists) by deleting this regkey:
        /// HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{RegistrationGUID} (where RegistrationGUID = what is in parameterinfo.xml for  Registration Id="{4C9C8A65-1159-414A-96D0-1815992D0A7F}")
        /// </summary>
        public void DeleteBurnArpEntry()
        {
            if (!String.IsNullOrEmpty(RegistrationId))
            {
                string[] uninstallSubKeys = new string[] { 
                                             @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\", 
                                             @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\" };
                foreach (string uninstallSubKey in uninstallSubKeys)
                {
                    // delete the per-machine ARP entry for this RegistrationId if it exists
                    RegistryKey rkUninstall = Registry.LocalMachine.OpenSubKey(uninstallSubKey, true);
                    if (null != rkUninstall)
                    {
                        try
                        {
                            RegistryKey rkRegistrationId = Registry.LocalMachine.OpenSubKey(uninstallSubKey + RegistrationId, true);
                            if (null != rkRegistrationId)
                            {
                                rkUninstall.DeleteSubKeyTree(RegistrationId);
                            }
                        }
                        catch
                        {
                            // don't throw if the key couldn't be deleted.  It probably didn't exist.
                        }
                        foreach (string id in UpdateIds)
                        {
                            try
                            {
                                RegistryKey rkRegistrationId = Registry.LocalMachine.OpenSubKey(uninstallSubKey + RegistrationId + "_" + id, true);
                                if (null != rkRegistrationId)
                                {
                                    rkUninstall.DeleteSubKeyTree(RegistrationId + "_" + id);
                                }
                            }
                            catch
                            {
                                // don't throw if the key couldn't be deleted.  It probably didn't exist.
                            }
                        }
                    }
                }

                // delete the per-user ARP entry(s) for this RegistrationId if they exists
                // For example:
                // HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\{b12dfdfb-9ef3-471e-b2a0-b960cc00106c}
                // HKEY_USERS\S-1-5-21-2127521184-1604012920-1887927527-1450143\Software\Microsoft\Windows\CurrentVersion\Uninstall\{b12dfdfb-9ef3-471e-b2a0-b960cc00106c}
                foreach (string sid in Registry.Users.GetSubKeyNames())
                {
                    string uninstallSubKey = sid + @"\Software\Microsoft\Windows\CurrentVersion\Uninstall\";
                    RegistryKey rkUserUninstall = Registry.Users.OpenSubKey(uninstallSubKey, true);
                    if (null != rkUserUninstall)
                    {
                        try
                        {
                            RegistryKey rkRegistrationId = Registry.Users.OpenSubKey(uninstallSubKey + RegistrationId, true);
                            if (null != rkRegistrationId)
                            {
                                rkUserUninstall.DeleteSubKeyTree(RegistrationId);
                            }
                        }
                        catch
                        {
                            // don't throw if the key couldn't be deleted.  It probably didn't exist.
                        }
                        foreach (string id in UpdateIds)
                        {
                            try
                            {
                                RegistryKey rkRegistrationId = Registry.Users.OpenSubKey(uninstallSubKey + RegistrationId + "_" + id, true);
                                if (null != rkRegistrationId)
                                {
                                    rkUserUninstall.DeleteSubKeyTree(RegistrationId + "_" + id);
                                }
                            }
                            catch
                            {
                                // don't throw if the key couldn't be deleted.  It probably didn't exist.
                            }
                        }
                    }
                }

            }

        }

        /// <summary>
        /// Deletes %temp%\PackageName_PackageVersion (i.e. %temp%\BurnTestSKU_v0.1) and all of it's contents.
        /// This will be done for all users temp folders on the machine cause another user may have installed this layout
        /// </summary>
        public void DeleteEngineDownloadCache()
        {
            string cacheFolder;

            // delete the cache for current user (local admin)
            cacheFolder = m_Layout.GetDownloadCachePath();
            WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(cacheFolder);

            // delete the cache for all other users if it exists
            foreach (string directory in UserUtilities.GetAllUserTempPaths())
            {
                cacheFolder = Path.Combine(directory, m_Layout.GetDownloadCacheFolderName());
                WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(cacheFolder);
            }
        }

        /// <summary>
        /// Deletes %temp%\RegistrationId (i.e. %temp%\{00000000-0000-0000-4F85-B939234AE44F}) and all of it's contents.
        /// This will be done for all users temp folders on the machine cause another user may have installed this layout
        /// </summary>
        public void DeleteBundleExtractionTempCache()
        {
            if (!String.IsNullOrEmpty(this.RegistrationId))
            {
                string cacheFolder;

                // delete the cache for all users if it exists
                foreach (string directory in UserUtilities.GetAllUserTempPaths())
                {
                    cacheFolder = Path.Combine(directory, this.RegistrationId);
                    WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(cacheFolder);
                }
            }
        }

        /// <summary>
        /// Deletes %ProgramData%\Package Cache\guid and all of it's contents if it exists. (where guid = what is in parameterinfo.xml for  Registration Id="{4C9C8A65-1159-414A-96D0-1815992D0A7F}") i.e.: C:\ProgramData\{4C9C8A65-1159-414A-96D0-1815992D0A7F}\setup.exe
        /// </summary>
        public void DeleteEngineCache()
        {
            if (!String.IsNullOrEmpty(this.RegistrationId))
            {
                // Delete the per-machine cached engine folder if it exists
                // per-machine installs need to be deleted from %ProgramData%\\Package Cache\\RegistrationId
                string cacheFolder = System.Environment.ExpandEnvironmentVariables("%ProgramData%\\Package Cache\\" + RegistrationId);
                WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(cacheFolder);

                // Delete the per-user cached engine folder(s) if they exist (for all users)
                // per-user installs need to be deleted from %LOCALAPPDATA%\\Package Cache\\RegistrationId
                foreach (string directory in UserUtilities.GetAllUserLocalAppDataPaths())
                {
                    // delete the cache for all the other users non-admin user (normal user)
                    cacheFolder = Path.Combine(directory, "Package Cache\\" + RegistrationId);
                    WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(cacheFolder);
                }
            }
        }

        /// <summary>
        /// Removes the payload cache for all items in the layout.  Be sure to uninstall all the payload before removing the payload cache as it might be needed during uninstall.
        /// </summary>
        public void DeletePayloadCache()
        {
            // Files are cached in:
            // EXE:  // %ProgramData%\Package Cache\filehash\filename.exe
            // MSI:  // %ProgramData%\Package Cache\productCode_version\filename.msi
            // MSP:  // %ProgramData%\Package Cache\packageCode\filename.msp

            List<string> cacheRootFolders = new List<string>();

            // delete payload cache for each item from both per-machine and per-user caches
            string cacheRootFolderPerMachine = Path.Combine("%ProgramData%", "Package Cache");
            cacheRootFolders.Add(System.Environment.ExpandEnvironmentVariables(cacheRootFolderPerMachine));

            foreach (string directory in UserUtilities.GetAllUserLocalAppDataPaths())
            {
                // delete the cache for all the other users non-admin user (normal user)
                string cacheRootFolderPerUser = Path.Combine(directory, "Package Cache");
                cacheRootFolders.Add(cacheRootFolderPerUser);
            }

            foreach (OM.WixAuthoringOM.Bundle.Chain.Package package in m_Layout.Wix.Bundle.Chain.Packages)
            {
                string packageCacheFoldername = null;
                if (!String.IsNullOrEmpty(package.CacheId))
                {
                    packageCacheFoldername = package.CacheId;

                    foreach (string cacheRootFolder in cacheRootFolders)
                    {
                        string fullPackageCachePath = Path.Combine(cacheRootFolder, packageCacheFoldername);
                        WixTest.Burn.LayoutManager.LayoutManager.RemoveDirectory(fullPackageCachePath);
                    }
                }
                else
                {
                    // TODO handle cases where CacheId is not defined.  Currently all tests are setting this.
                }
            }
        }

        /// <summary>
        /// Uninstalls all the payload items in a layout
        /// </summary>
        public void UninstallAllPayloads()
        {
            // loop thru all the items in a layout and uninstall them.
            foreach (WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.Package package in m_Layout.Wix.Bundle.Chain.Packages)
            {
                Type t = package.GetType();
                switch (t.FullName)
                {
                    case "WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.ExePackageElement":
                        // run the exe with the UninstallCommandLine
                        // hopefully the Exe does the right thing
                        // per-user installs that were performed by another user will probably not be removed properly.
                        string exeFile = null;
                        if (!String.IsNullOrEmpty(package.SourceFilePathT))
                        {
                            exeFile = package.SourceFilePathT;
                        }
                        else if (m_Layout != null &&
                            (!String.IsNullOrEmpty(m_Layout.LayoutFolder)) &&
                            (!String.IsNullOrEmpty(((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.ExePackageElement)package).Name)))
                        {
                            exeFile = Path.Combine(m_Layout.LayoutFolder, ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.ExePackageElement)package).Name);
                        }

                        if (!String.IsNullOrEmpty(exeFile))
                        {
                            Process proc = new Process();
                            proc.StartInfo.Arguments = ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.ExePackageElement)package).UninstallCommand;
                            proc.StartInfo.FileName = exeFile;
                            proc.Start();
                            proc.WaitForExit();
                        }
                        else
                        {
                            System.Diagnostics.Trace.TraceError("Unable to find Exe to uninstall: {0}", ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.ExePackageElement)package).Name);
                        }
                        break;
                    case "WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement":
                        // unintall the Msi from the SourceFile if it is set
                        // otherwise, use the Msi in the layout if it can be found
                        string msiFile = null;
                        if (!String.IsNullOrEmpty(package.SourceFilePathT))
                        {
                            msiFile = package.SourceFilePathT;
                        }
                        else if (m_Layout != null &&
                            (!String.IsNullOrEmpty(m_Layout.LayoutFolder)) &&
                            (!String.IsNullOrEmpty(((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement)package).Name)))
                        {
                            msiFile = Path.Combine(m_Layout.LayoutFolder, ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement)package).Name);
                        }

                        if (!String.IsNullOrEmpty(msiFile))
                        {
                            MsiUtils.RemoveMSI(msiFile);
                        }
                        else
                        {
                            System.Diagnostics.Trace.TraceError("Unable to find Msi to uninstall: {0}", ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement)package).Name);
                        }
                        break;
                    case "WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MspPackageElement":
                        string mspFile = null;
                        if (!String.IsNullOrEmpty(package.SourceFilePathT))
                        {
                            mspFile = package.SourceFilePathT;
                        }
                        else if (m_Layout != null &&
                            (!String.IsNullOrEmpty(m_Layout.LayoutFolder)) &&
                            (!String.IsNullOrEmpty(((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MspPackageElement)package).Name)))
                        {
                            mspFile = Path.Combine(m_Layout.LayoutFolder, ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MspPackageElement)package).Name);
                        }

                        if (!String.IsNullOrEmpty(mspFile))
                        {
                            MsiUtils.RemovePatchFromProducts(MsiUtils.GetPatchCode(mspFile), MsiUtils.GetTargetProductCodes(mspFile));
                        }
                        else
                        {
                            System.Diagnostics.Trace.TraceError("Unable to find Msp file to uninstall: {0}", ((WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.MspPackageElement)package).Name);
                        }
                        break;
                    // BUGBUG TODO: handle Item Groups (i.e. Patches) once support for testing those is enabled in the LayoutManager
                    default:
                        System.Diagnostics.Trace.TraceError("Unknown item type to uninstall.  type = {0}", t.FullName);
                        break;
                }
            }
        }
    }
}
