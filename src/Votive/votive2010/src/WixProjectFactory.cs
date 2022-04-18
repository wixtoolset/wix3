// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Xml;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

    /// <summary>
    /// Implements the IVsProjectFactory and IVsOwnedProjectFactory interfaces, which handle
    /// the creation of our custom WiX projects.
    /// </summary>
    [Guid("930C7802-8A8C-48f9-8165-68863BCCD9DD")]
    [CLSCompliant(false)]
    public class WixProjectFactory : ProjectFactory, IVsProjectUpgradeViaFactory
    {
        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixProjectFactory"/> class.
        /// </summary>
        /// <param name="package">The <see cref="WixPackage"/> to which this project factory belongs.</param>
        public WixProjectFactory(WixPackage package)
            : base(package)
        {
        }

        // =========================================================================================
        // Methods
        // =========================================================================================

        /// <summary>
        /// Upgrades the project.  This is called before the project is loaded.
        /// </summary>
        /// <param name="fileName">Project file name</param>
        /// <param name="upgradeFlag">Upgrade flag</param>
        /// <param name="copyLocation">Place to copy the new project - not supported</param>
        /// <param name="upgradedFullyQualifiedFileName">New full name for the project - not supported</param>
        /// <param name="logger">Logger for messages during the upgrade</param>
        /// <param name="upgradeRequired">Is upgrade required?</param>
        /// <param name="newProjectFactory">GUID of the project factory</param>
        /// <returns>HRESULT</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "3#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "4#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "5#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "6#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults", Scope = "member")]
        public override int UpgradeProject(string fileName, uint upgradeFlag, string copyLocation, out string upgradedFullyQualifiedFileName, IVsUpgradeLogger logger, out int upgradeRequired, out Guid newProjectFactory)
        {
            uint ignore;
            string projectName = Path.GetFileNameWithoutExtension(fileName);
            upgradedFullyQualifiedFileName = fileName;

            this.UpgradeProject_CheckOnly(fileName, logger, out upgradeRequired, out newProjectFactory, out ignore);
            if (upgradeRequired == 0)
            {
                upgradedFullyQualifiedFileName = fileName;
                return VSConstants.S_OK;
            }

            IVsQueryEditQuerySave2 queryEditQuerySave = WixHelperMethods.GetService<IVsQueryEditQuerySave2, SVsQueryEditQuerySave>(this.Site);
            
            int qef = (int)tagVSQueryEditFlags.QEF_ReportOnly | (int)__VSQueryEditFlags2.QEF_AllowUnopenedProjects;
            uint verdict;
            uint moreInfo;
            string[] files = new string[1];
            files[0] = fileName;

            bool continueUpgrade = false;
            ErrorHandler.ThrowOnFailure(queryEditQuerySave.QueryEditFiles((uint)qef, 1, files, null, null, out verdict, out moreInfo));
            if (verdict == (uint)tagVSQueryEditResult.QER_EditOK)
            {
                continueUpgrade = true;
            }

            if (verdict == (uint)tagVSQueryEditResult.QER_EditNotOK)
            {
                logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, fileName, WixStrings.ReadOnlyFile);
                if ((moreInfo & (uint)tagVSQueryEditResultFlags.QER_ReadOnlyUnderScc) != 0)
                {
                    qef = (int)tagVSQueryEditFlags.QEF_DisallowInMemoryEdits | (int)__VSQueryEditFlags2.QEF_AllowUnopenedProjects | (int)tagVSQueryEditFlags.QEF_ForceEdit_NoPrompting;
                    ErrorHandler.ThrowOnFailure(queryEditQuerySave.QueryEditFiles((uint)qef, 1, files, null, null, out verdict, out moreInfo));
                    if (verdict == (uint)tagVSQueryEditResult.QER_EditOK)
                    {
                        continueUpgrade = true;
                    }
                }
                
                if (continueUpgrade)
                {
                    logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, fileName, WixStrings.CheckoutSuccess);
                }
                else
                {
                    logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_ERROR, projectName, fileName, WixStrings.FailedToCheckoutProject);
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, WixStrings.FailedToCheckoutFile, fileName));
                }
            }

            // If file was modified during the checkout, maybe upgrade is not needed
            if ((moreInfo & (uint)tagVSQueryEditResultFlags.QER_MaybeChanged) != 0)
            {
                this.UpgradeProject_CheckOnly(fileName, logger, out upgradeRequired, out newProjectFactory, out ignore);
                if (upgradeRequired == 0)
                {
                    if (logger != null)
                    {
                        logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, fileName, WixStrings.UpgradeNoNeedToUpgradeAfterCheckout);
                    }

                    return VSConstants.S_OK;
                }
            }

            if (continueUpgrade)
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                xmlDoc.DocumentElement.SetAttribute(WixProjectFileConstants.ToolsVersion, "4.0");

                bool targetsPathUpdated = false;
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    if (WixProjectFileConstants.PropertyGroup == node.Name)
                    {
                        foreach (XmlNode propertyNode in node.ChildNodes)
                        {
                            if (WixProjectFileConstants.WixTargetsPath == propertyNode.Name)
                            {
                                if (propertyNode.InnerText.Contains("\\Microsoft\\WiX\\v3.0\\"))
                                {
                                    targetsPathUpdated = true;
                                    propertyNode.InnerText = propertyNode.InnerText.Replace("\\Microsoft\\WiX\\v3.0\\", "\\Microsoft\\WiX\\v3.x\\");
                                }
                                else if (propertyNode.InnerText.Contains("\\Microsoft\\WiX\\v3.5\\"))
                                {
                                    targetsPathUpdated = true;
                                    propertyNode.InnerText = propertyNode.InnerText.Replace("\\Microsoft\\WiX\\v3.5\\", "\\Microsoft\\WiX\\v3.x\\");
                                }

                                if (propertyNode.InnerText.Contains("\\Wix2010.targets"))
                                {
                                    targetsPathUpdated = true;
                                    propertyNode.InnerText = propertyNode.InnerText.Replace("\\Wix2010.targets", "\\Wix.targets");
                                }
                            }
                        }
                    }
                }

                if (targetsPathUpdated)
                {
                    logger.LogMessage((uint)__VSUL_ERRORLEVEL.VSUL_INFORMATIONAL, projectName, fileName, WixStrings.WixTargetsPathUpdated);
                }

                xmlDoc.Save(fileName);
                upgradedFullyQualifiedFileName = fileName;
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries the project file to see if an upgrade is required.
        /// </summary>
        /// <param name="fileName">Name of the project file</param>
        /// <param name="logger">Logger for upgrade messages</param>
        /// <param name="upgradeRequired">Is upgrade required</param>
        /// <param name="newProjectFactory">GUID of the project factory</param>
        /// <param name="upgradeProjectCapabilityFlags">Upgrade capabilities - we have none</param>
        /// <returns>HRESULT</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "3#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "4#", Justification = "Suppressing to avoid conflict with style cop.")]
        public override int UpgradeProject_CheckOnly(string fileName, IVsUpgradeLogger logger, out int upgradeRequired, out Guid newProjectFactory, out uint upgradeProjectCapabilityFlags)
        {
            upgradeRequired = 0;
            newProjectFactory = this.GetType().GUID;
            upgradeProjectCapabilityFlags = 0;

            string toolsVersion = String.Empty;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            XmlNode toolVersionNode = xmlDoc.DocumentElement.Attributes.GetNamedItem(WixProjectFileConstants.ToolsVersion);
            if (toolVersionNode != null)
            {
                toolsVersion = toolVersionNode.Value;
            }

            try
            {
                if (!String.IsNullOrEmpty(toolsVersion))
                {
                    string[] version = toolsVersion.Split('.');
                    if (version.GetLength(0) >= 1)
                    {
                        int high = Convert.ToInt32(version[0], CultureInfo.InvariantCulture);
                        if (high < 4)
                        {
                            upgradeRequired = 1;
                        }
                    }
                }
                else
                {
                    upgradeRequired = 1;
                }
            }
            catch (FormatException)
            {
                // Unknown version, we don't want to touch it
            }
            catch (OverflowException)
            {
                // Unknown version, we don't want to touch it
            }

            foreach(XmlNode node in xmlDoc.DocumentElement.ChildNodes)
            {
                if (WixProjectFileConstants.PropertyGroup == node.Name)
                {
                    foreach (XmlNode propertyNode in node.ChildNodes)
                    {
                        if (WixProjectFileConstants.WixTargetsPath == propertyNode.Name)
                        {
                            if (propertyNode.InnerText.Contains("\\Microsoft\\WiX\\v3.0\\") || propertyNode.InnerText.Contains("\\Microsoft\\WiX\\v3.5\\"))
                            {
                                upgradeRequired = 1;
                            }

                            if (propertyNode.InnerText.Contains("\\Wix2010.targets"))
                            {
                                upgradeRequired = 1;
                            }
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the source ontrol info for the project being upgraded
        /// </summary>
        /// <param name="projectFileName">Path to the project name</param>
        /// <param name="sccProjectName">Source control data from the project</param>
        /// <param name="sccAuxPath">Source control data from the project</param>
        /// <param name="sccLocalPath">Source control data from the project</param>
        /// <param name="sccProvider">Source control data from the project</param>
        /// <returns>HRESULT</returns>
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "0#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "1#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "2#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "3#", Justification = "Suppressing to avoid conflict with style cop.")]
        [SuppressMessage("Microsoft.Naming", "CA1725:ParameterNamesShouldMatchBaseDeclaration", MessageId = "4#", Justification = "Suppressing to avoid conflict with style cop.")]
        public override int GetSccInfo(string projectFileName, out string sccProjectName, out string sccAuxPath, out string sccLocalPath, out string sccProvider)
        {
            sccProjectName = String.Empty;
            sccAuxPath = String.Empty;
            sccLocalPath = String.Empty;
            sccProvider = String.Empty;

            if (!String.IsNullOrEmpty(projectFileName) && File.Exists(projectFileName))
            {
                using (StreamReader streamReader = new StreamReader(File.OpenRead(projectFileName)))
                {
                    XmlReaderSettings readerSettings = new XmlReaderSettings();
                    readerSettings.IgnoreWhitespace = true;
                    using (XmlReader xmlReader = XmlReader.Create(streamReader, readerSettings))
                    {
                        while (xmlReader.Read())
                        {
                            if (WixProjectFactory.CheckXmlNode(xmlReader, WixProjectFileConstants.SccAuxPath, out sccAuxPath))
                            {
                                continue;
                            }

                            if (WixProjectFactory.CheckXmlNode(xmlReader, WixProjectFileConstants.SccLocalPath, out sccLocalPath))
                            {
                                continue;
                            }

                            if (WixProjectFactory.CheckXmlNode(xmlReader, WixProjectFileConstants.SccProjectName, out sccProjectName))
                            {
                                continue;
                            }

                            if (WixProjectFactory.CheckXmlNode(xmlReader, WixProjectFileConstants.SccProvider, out sccProvider))
                            {
                                continue;
                            }
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Creates a new <see cref="WixProjectNode"/>.
        /// </summary>
        /// <returns>A new <see cref="WixProjectNode"/> object.</returns>
        protected override ProjectNode CreateProject()
        {
            WixProjectNode project = new WixProjectNode(this.Package as WixPackage);
            project.SetSite((IOleServiceProvider)((IServiceProvider)this.Package).GetService(typeof(IOleServiceProvider)));
            return project;
        }

        private static bool CheckXmlNode(XmlReader xmlReader, string propertyName, out string propertyValue)
        {
            bool match = false;
            propertyValue = String.Empty;
            
            if (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (String.Compare(xmlReader.Name, propertyName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Text)
                        {
                            propertyValue = xmlReader.Value;
                            match = true;
                        }
                    }
                }
            }

            return match;
        }
    }
}
