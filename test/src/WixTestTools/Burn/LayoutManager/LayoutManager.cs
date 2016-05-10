// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.LayoutManager
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixTest.Burn.OM.WixAuthoringOM;

    public partial class LayoutManager
    {
        #region private member variables

        private WixElement m_WixElement;
        private string m_LayoutFolder;
        private string m_BurnstubExeFilename;
        private string m_SetupBundleFilename;
        private UX.UxBase m_Ux;
        private bool? m_ActualPerMachineBundle = null;
        private string m_ActualBundleId;

        #endregion

        #region public Properties

        public string LayoutFolder
        {
            get
            {
                return m_LayoutFolder;
            }
            set
            {
                m_LayoutFolder = value;
            }
        }

        public string BurnstubExeFilename
        {
            get
            {
                if (String.IsNullOrEmpty(m_BurnstubExeFilename))
                {
                    m_BurnstubExeFilename = "Burnstub.exe";
                }
                return m_BurnstubExeFilename;
            }
            set
            {
                m_BurnstubExeFilename = value;
            }
        }

        public string SetupBundleFilename
        {
            get
            {
                if (String.IsNullOrEmpty(m_SetupBundleFilename))
                {
                    m_SetupBundleFilename = "BurnSetupBundle.exe";
                }
                return m_SetupBundleFilename;
            }
            set
            {
                m_SetupBundleFilename = value;
            }
        }

        public WixElement Wix
        {
            get
            {
                return m_WixElement;
            }
            set
            {
                m_WixElement = value;
            }
        }
        
        public UX.UxBase Ux
        {
            get
            {
                return m_Ux;
            }
            set
            {
                m_Ux = value;
            }
        }

        /// <summary>
        /// The BundleId from a built bundle (it is dynamically generated at build time, not authored)
        /// Null if it has not been built and read from the bundle.
        /// </summary>
        public string ActualBundleId
        {
            get
            {
                return m_ActualBundleId;
            }
            set
            {
                m_ActualBundleId = value;
            }
        }

        /// <summary>
        /// Indicates what the built bundle contains for the Bundles Registration\@PerMachine  (this is calculated at bundle build time)
        /// Null if it has not been built and read from the bundle.  True if the value is yes. False if the value is no.
        /// </summary>
        public bool? ActualPerMachineBundle
        {
            get
            {
                return m_ActualPerMachineBundle;
            }
            set
            {
                m_ActualPerMachineBundle = value;
            }
        }
        #endregion

        #region constructors

        public LayoutManager()
        {
            initializeProperties(null, null);
        }

        public LayoutManager(string layoutFolder)
        {
            initializeProperties(layoutFolder, null);
        }

        public LayoutManager(UX.UxBase ux)
        {
            initializeProperties(null, ux);
        }

        public LayoutManager(string layoutFolder, UX.UxBase ux)
        {
            initializeProperties(layoutFolder, ux);
        }

        private void initializeProperties(string layoutFolder, UX.UxBase ux)
        {
            if (String.IsNullOrEmpty(layoutFolder))
            {
                LayoutFolder = System.Environment.ExpandEnvironmentVariables("%WIX_ROOT%\\test\\sandbox");
            }
            else
            {
                LayoutFolder = System.Environment.ExpandEnvironmentVariables(layoutFolder);
            }

            if (ux != null)
            {
                this.Ux = ux;
            }

            CreateAndInitializeWixBundleManifestWithMinimumDefaults();
        }

        #endregion

        public void CreateAndInitializeWixBundleManifestWithMinimumDefaults()
        {
            Wix = new OM.WixAuthoringOM.WixElement();
            Wix.Xmlns = "http://schemas.microsoft.com/wix/2006/wi";
            Wix.Bundle = new OM.WixAuthoringOM.Bundle.BundleElement();
            Wix.Bundle.Compressed = "yes";
            Wix.Bundle.Version = "1.0.0.0";
            Wix.Bundle.Name = "Burn Chainer Test";
            Wix.Bundle.AboutUrl = "http://burn/about.html";
            Wix.Bundle.DisableModify = "no";
            Wix.Bundle.DisableRemove = "no";
            Wix.Bundle.DisableRepair = "no";
            Wix.Bundle.HelpTelephone = "555-555-5555";
            Wix.Bundle.HelpUrl = "http://burn/help.html";
            Wix.Bundle.Manufacturer = "BurnPublisher";
            Wix.Bundle.UpdateUrl = "http://burn/update.html";
            Wix.Bundle.UpgradeCode = "{3A882C10-C361-4bdf-91A6-D13C4DB71F7B}";

            if (null != this.Ux) Wix.Bundle.UX = this.Ux.GetWixBundleUXElement();

            Wix.Bundle.Chain = new OM.WixAuthoringOM.Bundle.Chain.ChainElement();
        }
        
        private void CopyEngineAndUxFiles()
        {
            // copy all of the Burn engine files
            string srcBinDir = WixTest.Settings.WixToolsDirectory;
            string srcBurnEngineFile = Path.Combine(srcBinDir, this.BurnstubExeFilename);
            string destBurnEngineFile = Path.Combine(this.LayoutFolder, this.BurnstubExeFilename);
            CopyFile(srcBurnEngineFile, destBurnEngineFile);

            // TODO: Figure out if we should copy the PDBs too.  Might be nice to have them in the layout...

            // copy the Burn UX files.  
            if (null != this.Ux) this.Ux.CopyAndConfigureUx(this.LayoutFolder, this.Wix);

        }

        /// <summary>
        /// Creates a burn bundle layout by running the candle.exe and light.exe build tools.
        /// Dynamically generates parameterinfo.xml from BurnManifest, cabs and attaches the UX files, parameterinfo.xml, embedded payloads in an attached container.
        /// </summary>
        public void BuildBundle()
        {
            BuildBundle(true);
        }

        /// <summary>
        /// Creates a burn bundle layout by running the candle.exe and light.exe build tools.
        /// Dynamically generates parameterinfo.xml from BurnManifest, cabs and attaches the UX files, parameterinfo.xml, embedded payloads in an attached container.
        /// </summary>
        /// <param name="removeAllOtherFiles">determines if everything except the bundle is deleted from the bundle folder after the bundle is created.  It is a good idea to delete it if you want to verify stuff isn't picked up from this folder and is downloaded.</param>
        public void BuildBundle(bool removeAllOtherFiles)
        {
            BuildBundle(removeAllOtherFiles, null, String.Empty, String.Empty);
        }

        /// <summary>
        /// Creates a burn bundle layout by running the candle.exe and light.exe build tools.
        /// Dynamically generates parameterinfo.xml from BurnManifest, cabs and attaches the UX files, parameterinfo.xml, embedded payloads in an attached container.
        /// </summary>
        /// <param name="removeAllOtherFiles">determines if everything except the bundle is deleted from the bundle folder after the bundle is created.  It is a good idea to delete it if you want to verify stuff isn't picked up from this folder and is downloaded.</param>
        /// <param name="wixExtensions">list of additional wixExtensions to use to build the bundle</param>
        public void BuildBundle(bool removeAllOtherFiles, List<string>wixExtensions, string additionalCandleArgs, string additionalLightArgs)
        {
            string xmlManifestContent = WixTest.Burn.OM.Generator.XMLGenerator.GetXmlString(Wix);

            string bundleManifestFile = Path.Combine(this.LayoutFolder, Path.GetFileNameWithoutExtension(this.SetupBundleFilename) + ".xml");
            if (!Directory.Exists(this.LayoutFolder))
            {
                Directory.CreateDirectory(this.LayoutFolder);
            }
            // write the xml to bundle.xml
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlManifestContent);
            //Save the bundle.xml in Encoding.UTF8 since that is what the xml says is the encoding.
            XmlTextWriter wrtr = new XmlTextWriter(bundleManifestFile, Encoding.UTF8);
            xmlDoc.WriteTo(wrtr);
            wrtr.Close();

            CopyEngineAndUxFiles();
            if (null == wixExtensions) wixExtensions = new List<string>();
            wixExtensions.Add("wixutilextension");
            string bundlePath = Builder.BuildBundlePackage(this.LayoutFolder, bundleManifestFile, wixExtensions.ToArray(), additionalCandleArgs, additionalLightArgs, true);

            ActualBundleId = GetRegistrationId();
            if (GetRegistrationPerMachine() == "no") ActualPerMachineBundle = false;
            if (GetRegistrationPerMachine() == "yes") ActualPerMachineBundle = true;

            if (removeAllOtherFiles)
            {
                List<string> bundleFilesToKeep = new List<string>();
                bundleFilesToKeep.Add(Path.Combine(this.LayoutFolder, this.SetupBundleFilename));
                // BUGBUG TODO: read thru the burn manifest and keep all the "external" files too.  This could be UX resources files and/or Payload files and Payload resources in the Chain

                foreach (string file in Directory.GetFiles(this.LayoutFolder, "*", SearchOption.AllDirectories))
                {
                    if (!bundleFilesToKeep.Contains(file))
                    {
                        File.SetAttributes(file, System.IO.FileAttributes.Normal);
                        File.Delete(file);
                    }
                }

            }

        }

        private XmlNode GetRegistrationNode()
        {
            string burnManifestFile = Path.Combine(this.LayoutFolder, Builder.BurnManifestFileName);
            string registrationXpath = @"//burn:Registration";

            XmlNodeList registrationNodes = Verifier.QueryBurnManifest(burnManifestFile, registrationXpath);
            if (null == registrationNodes || registrationNodes.Count == 0 )
            {
                throw new ApplicationException(string.Format("Could not load Registration information from Burn Manifest file in '{0}'.", burnManifestFile));
            }

            return registrationNodes[0];
        }

        private string GetRegistrationId()
        {
            XmlNode registrationNode = this.GetRegistrationNode();
            if (null == registrationNode.Attributes["Id"])
            {
                throw new ApplicationException("Could not load Registration Id from Burn Manifest.");
            }

            return registrationNode.Attributes["Id"].Value;
        }

        private string GetRegistrationPerMachine()
        {
            XmlNode registrationNode = this.GetRegistrationNode();
            if (null == registrationNode.Attributes["PerMachine"])
            {
                throw new ApplicationException("Could not load PerMachine Registration from Burn Manifest.");
            }

            return registrationNode.Attributes["PerMachine"].Value;
        }

        public static void CopyFile(string srcFile, string destFile)
        {
            string destDir = Path.GetDirectoryName(destFile);

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (File.Exists(destFile))
            {
                // remove read-only attributes from files that exist so they can be over-written.
                File.SetAttributes(destFile, System.IO.FileAttributes.Normal);
            }

            File.Copy(srcFile, destFile, true);
        }

        public static void RemoveFile(string file)
        {
            if (File.Exists(file))
            {
                // remove read-only attributes from files that exist so they can be deleted.
                File.SetAttributes(file, System.IO.FileAttributes.Normal);

                File.Delete(file);
            }
        }

        public static void RemoveDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                // remove read-only attributes from all files that exist so they can be deleted.
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, System.IO.FileAttributes.Normal);
                }
                Directory.Delete(directory, true);
            }
        }

        #region methods for adding EXEs, MSIs, MSPs, Variables

        public void AddMsi(string file, string newFileName, string url, bool includeInLayout)
        {
            AddMsi(file, newFileName, url, includeInLayout, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        /// <summary>
        /// Add an MSI to the Chain of payloads this bundle will install
        /// </summary>
        /// <param name="file">full path to the MSI file to add</param>
        /// <param name="newFileName">New filename for the MSI, null if you don't want to rename it</param>
        /// <param name="url">URL where the MSI can be downloaded from, null if you don't want to download it</param>
        /// <param name="includeInLayout">true if the MSI should be included in the bundle or as an external file, false if it should not exist.</param>
        /// <param name="msiPropertyName">name of a property to be sent to the MSI at install time</param>
        /// <param name="msiPropertyValue">value of the named property to be sent to the MSI at install time</param>
        /// <param name="installCondition">condition that this MSI will be installed</param>
        /// <param name="rollbackInstallCondition">UNUSED (BUGBUG, rip this out)</param>
        public void AddMsi(string file, string newFileName, string url, bool includeInLayout, string msiPropertyName, string msiPropertyValue
            , string installCondition, string rollbackInstallCondition)
        {
            OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement msi = new OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement();
            msi.SourceFilePathT = file;
            msi.Name = Path.GetFileName(file);
            if (!String.IsNullOrEmpty(newFileName))
            {
                msi.Name = newFileName;
            }
            msi.Id = "Id_" + msi.Name;
            msi.Vital = "no";
            msi.Cache = "yes";
            msi.CacheId = msi.Id;
            msi.SourceFile = msi.Name;
            // if you include a URL, the item will be downloaded.
            if (!includeInLayout) msi.DownloadUrl = url;

            if (!string.IsNullOrEmpty(msiPropertyName) && !string.IsNullOrEmpty(msiPropertyValue))
            {
                OM.WixAuthoringOM.Bundle.Chain.MsiPropertyElement msiProperty = new OM.WixAuthoringOM.Bundle.Chain.MsiPropertyElement();
                msiProperty.Name = msiPropertyName;
                msiProperty.Value = msiPropertyValue;

                msi.MsiProperty = msiProperty;
            }

            if (!string.IsNullOrEmpty(installCondition))
            {
                msi.InstallCondition = installCondition;
            }
            
            Wix.Bundle.Chain.Packages.Add(msi);

            CopyPayloadToLayout(file, newFileName, true);
        }

        public void AddVariable(string id, string value, OM.WixAuthoringOM.Bundle.Variable.VariableElement.VariableDataType type)
        {
            OM.WixAuthoringOM.Bundle.Variable.VariableElement varElement = new OM.WixAuthoringOM.Bundle.Variable.VariableElement();
            varElement.Name = id;
            varElement.Value = value;
            varElement.Type = type;

            Wix.Bundle.Variables.Add(varElement);
        }

        public class ExternalFile
        {
            public string File;
            public string Url;
        }

        public void AddMsiAndExternalFiles(string msiFile, string newMsiFileName, string url, bool includeInLayout, List<ExternalFile> extFiles)
        {
            OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement msi = new OM.WixAuthoringOM.Bundle.Chain.MsiPackageElement();
            msi.Name = Path.GetFileName(msiFile);
            if (!String.IsNullOrEmpty(newMsiFileName)) msi.Name = newMsiFileName;
            msi.SourceFilePathT = msiFile;
            msi.Id = "Id_" + msi.Name;
            msi.Vital = "no";
            msi.Cache = "yes";
            msi.CacheId = msi.Id;
            msi.SourceFile = msi.Name;
            if (!includeInLayout)
            {
                msi.DownloadUrl = url;
                // explicitly author Payloads for each of the external files with the specified URL for each file.
                // You don't have to do this explicitly if everything is in an attached container.  You only have to do this when you need to specify each URL.
                foreach (ExternalFile file in extFiles)
                {
                    OM.WixAuthoringOM.Bundle.PayloadElement payloadElement = new OM.WixAuthoringOM.Bundle.PayloadElement();
                    payloadElement.SourceFilePathT = file.File;
                    payloadElement.Name = Path.GetFileName(file.File);
                    payloadElement.SourceFile = payloadElement.Name;
                    payloadElement.DownloadUrl = file.Url;
                    msi.Payloads.Add(payloadElement);
                }
            }
            Wix.Bundle.Chain.Packages.Add(msi);

            // copy all the files to the layout directory so the bundle will build
            // copy the MSI file
            CopyPayloadToLayout(msiFile, newMsiFileName, true);
            // copy the external files (i.e. external CABs) 
            foreach (ExternalFile file in extFiles)
            {
                CopyPayloadToLayout(file.File, null, true);
            }
        }

        public void AddMsp(string file, bool includeInLayout)
        {
            AddMsp(file, null, null, includeInLayout, null);
        }

        /// <summary>
        /// Add an MspPackage to the Chain of payloads this bundle will install
        /// </summary>
        /// <param name="file">full path to the MSP file to add</param>
        /// <param name="newFileName">New filename for the MSP, null if you don't want to rename it</param>
        /// <param name="url">URL where the MSP can be downloaded from, null if you don't want to download it</param>
        /// <param name="includeInLayout">true if the MSP should be included in the bundle or as an external file, false if it should not exist.</param>
        /// <param name="installCondition">condition that this MSI will be installed</param>
        public void AddMsp(string file, string newFileName, string url, bool includeInLayout, string installCondition)
        {
            OM.WixAuthoringOM.Bundle.Chain.MspPackageElement msp = new OM.WixAuthoringOM.Bundle.Chain.MspPackageElement();
            msp.SourceFilePathT = file;
            msp.Name = Path.GetFileName(file);
            if (!String.IsNullOrEmpty(newFileName))
            {
                msp.Name = newFileName;
            }
            msp.Id = "Id_" + msp.Name;
            msp.Vital = "no";
            msp.Cache = "yes";
            msp.CacheId = msp.Id;
            msp.SourceFile = msp.Name;
            // if you include a URL, the item will be downloaded.
            if (!includeInLayout) msp.DownloadUrl = url;

            if (!string.IsNullOrEmpty(installCondition))
            {
                msp.InstallCondition = installCondition;
            }

            Wix.Bundle.Chain.Packages.Add(msp);

            CopyPayloadToLayout(file, newFileName, true);
        }

        public void AddExe(string file, bool includeInLayout)
        {
            AddExe(file, null, null, includeInLayout, string.Empty, string.Empty, string.Empty);
        }

        public void AddExe(string file, string newFileName, string url, bool includeInLayout)
        {
            AddExe(file, newFileName, url, includeInLayout, string.Empty, string.Empty, string.Empty);
        }

        public void AddExe(string file, string newFileName, string url, bool includeInLayout, string installCondition, string rollbackInstallCondition
            , string installArguments)
        {
            OM.WixAuthoringOM.Bundle.Chain.ExePackageElement exe = new OM.WixAuthoringOM.Bundle.Chain.ExePackageElement();
            exe.Name = System.IO.Path.GetFileName(file);
            if (!String.IsNullOrEmpty(newFileName)) exe.Name = newFileName;
            exe.SourceFilePathT = file;
            exe.Id = "Id_" + exe.Name;
            exe.Vital = "no";
            exe.Cache = "yes";
            exe.CacheId = exe.Id;
            exe.SourceFile = exe.Name;
            if (!includeInLayout) exe.DownloadUrl = url;
            exe.PerMachine = "yes";
            exe.InstallCommand = " ";
            exe.RepairCommand = " ";
            exe.UninstallCommand = " ";
            exe.DetectCondition = "1=2";

            if (!string.IsNullOrEmpty(installArguments))
            {
                exe.InstallCommand = installArguments;
            }

            if (!string.IsNullOrEmpty(installCondition))
            {
                exe.InstallCondition = installCondition;
            }

            Wix.Bundle.Chain.Packages.Add(exe);

            CopyPayloadToLayout(file, newFileName, true);
        }

        /// <summary>
        /// Adds a Payload to an existing package
        /// </summary>
        /// <param name="packageToAddTo">package to add the payload to</param>
        /// <param name="file">full path to the file to be added</param>
        /// <param name="newFileName">new filename, null if you don't want it renamed</param>
        /// <param name="url">Url for the file if it is to be downloaded at install time</param>
        /// <param name="includeInLayout">true of the file should be included in an attached container or as external file, false if it should not exist.</param>
        public void AddSubFile(WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain.Package packageToAddTo, string file, string newFileName, string url, bool includeInLayout)
        {
            WixTest.Burn.OM.WixAuthoringOM.Bundle.PayloadElement payload = new OM.WixAuthoringOM.Bundle.PayloadElement();

            payload.SourceFile = file;
            payload.SourceFilePathT = file;
            payload.Name = System.IO.Path.GetFileName(file);
            if (!String.IsNullOrEmpty(newFileName))
            {
                payload.Name = newFileName;
            }
            if (!String.IsNullOrEmpty(url) && !includeInLayout) // BUGBUG: if a URL is provided, WiX will not put the file in the attached container.
            {
                payload.DownloadUrl = url;
            }

            packageToAddTo.Payloads.Add(payload);

            CopyPayloadToLayout(file, newFileName, true);
        }
        #endregion

        public void CopyPayloadToLayout(string srcFile, string newFileName, bool includeInLayout)
        {
            string destFile = Path.Combine(this.LayoutFolder, Path.GetFileName(srcFile));
            if (!String.IsNullOrEmpty(newFileName))
            {
                destFile = Path.Combine(this.LayoutFolder, newFileName);
            }

            if (includeInLayout)
            {
                CopyFile(srcFile, destFile);
            }
            else
            {
                RemoveFile(destFile);
            }
        }

        /// <summary>
        /// Gets a path to the download cache folder for the current layout and current user.
        /// </summary>
        /// <returns>path to the download cache folder</returns>
        public string GetDownloadCachePath()
        {
            return Path.Combine(System.Environment.ExpandEnvironmentVariables(@"%temp%"), GetDownloadCacheFolderName());
        }

        /// <summary>
        /// Gets a folder name of the download cache folder for the current layout and current user.
        /// </summary>
        /// <returns>path to the download cache folder</returns>
        public string GetDownloadCacheFolderName()
        {
            return "UX_1.0.0.0";
        }

        /// <summary>
        /// Gets a path to the download cache folder for the current layout and current user.
        /// </summary>
        /// <param name="username">username who's download cache path to return.</param>
        /// <returns>path to the download cache folder</returns>
        public string GetDownloadCachePath(string username)
        {
            string currentUserPath = GetDownloadCachePath();
            return currentUserPath.Replace(System.Environment.ExpandEnvironmentVariables("%USERNAME%"), username);
        }

    }
}
