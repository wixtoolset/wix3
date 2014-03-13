//--------------------------------------------------------------------------------------------------
// <copyright file="WixConfigProvider.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixConfigProvider class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Build.BuildEngine;
	using MSBuild = Microsoft.Build.Evaluation;
	using MSBuildConstruction = Microsoft.Build.Construction;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    /// <summary>
    /// Provides support for adding and removing project configurations and platforms.
    /// </summary>
    internal class WixConfigProvider : ConfigProvider
    {
        internal const string IntermediateBaseRelativePath = "obj";
        internal const string ConfigPath = "$(Platform)\\$(Configuration)\\";

		private WixProjectNode wixProjectNode;

        /// <summary>
        /// Creates a new config provider for WiX projects.
        /// </summary>
        /// <param name="project">Parent project node</param>
        public WixConfigProvider(WixProjectNode project)
            : base(project)
        {
        	this.wixProjectNode = project;
        }

        /// <summary>
        /// Copies an existing configuration name or creates a new one. 
        /// </summary>
        /// <param name="name">The name of the new configuration.</param>
        /// <param name="cloneName">the name of the configuration to copy, or a null reference, indicating that AddCfgsOfCfgName should create a new configuration.</param>
        /// <param name="isPrivate">Flag indicating whether or not the new configuration is private. If fPrivate is set to true, the configuration is private. If set to false, the configuration is public. This flag can be ignored.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code. </returns>
        public override int AddCfgsOfCfgName(string name, string cloneName, int isPrivate)
        {
            if (!this.ProjectMgr.QueryEditProjectFile(false))
            {
                throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
            }

            string[] platforms = this.GetPropertiesConditionedOn(ProjectFileConstants.Platform);
            foreach (string platform in platforms)
            {
                string cloneCondition = String.IsNullOrEmpty(cloneName) ? null
                    : String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, cloneName, platform);
                this.AddNewConfigPropertyGroup(name, platform, cloneCondition);
            }

            this.NotifyOnCfgNameAdded(name);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Copies an existing platform name or creates a new one. 
        /// </summary>
        /// <param name="platformName">The name of the new platform.</param>
        /// <param name="clonePlatformName">The name of the platform to copy, or a null reference, indicating that AddCfgsOfPlatformName should create a new platform.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int AddCfgsOfPlatformName(string platformName, string clonePlatformName)
        {
            if (!this.ProjectMgr.QueryEditProjectFile(false))
            {
                throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
            }

            string[] configs = this.GetPropertiesConditionedOn(ProjectFileConstants.Configuration);
            foreach (string config in configs)
            {
                string cloneCondition = String.IsNullOrEmpty(clonePlatformName) ? null
                    : String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, config, clonePlatformName);
                this.AddNewConfigPropertyGroup(config, platformName, cloneCondition);
            }

            this.NotifyOnPlatformNameAdded(platformName);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Deletes a specified configuration name. 
        /// </summary>
        /// <param name="name">The name of the configuration to be deleted.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code. </returns>
        public override int DeleteCfgsOfCfgName(string name)
        {
            if (name != null)
            {
                if (!this.ProjectMgr.QueryEditProjectFile(false))
                {
                    throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
                }

                string[] configs = this.GetPropertiesConditionedOn(ProjectFileConstants.Configuration);
                foreach (string config in configs)
                {
                    if (String.Compare(config, name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string condition = String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigConditionString, config);
                        this.RemovePropertyGroupsWithMatchingCondition(condition);

                        string[] platforms = this.GetPropertiesConditionedOn(ProjectFileConstants.Configuration);
                        foreach (string platform in platforms)
                        {
                            condition = String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, config, platform);
                            this.RemovePropertyGroupsWithMatchingCondition(condition);
                        }

                        this.NotifyOnCfgNameDeleted(name);
                    }
                }
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Deletes a specified platform name. 
        /// </summary>
        /// <param name="platName">The platform name to delete.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int DeleteCfgsOfPlatformName(string platName)
        {
            if (platName != null)
            {
                if (!this.ProjectMgr.QueryEditProjectFile(false))
                {
                    throw Marshal.GetExceptionForHR(VSConstants.OLE_E_PROMPTSAVECANCELLED);
                }

                string[] platforms = this.GetPropertiesConditionedOn(ProjectFileConstants.Platform);
                foreach (string platform in platforms)
                {
                    if (String.Compare(platform, platName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string condition = String.Format(CultureInfo.InvariantCulture, WixProjectConfig.PlatformConditionString, platform);
                        this.RemovePropertyGroupsWithMatchingCondition(condition);

                        string[] configs = this.GetPropertiesConditionedOn(ProjectFileConstants.Configuration);
                        foreach (string config in configs)
                        {
                            condition = String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, config, platform);
                            this.RemovePropertyGroupsWithMatchingCondition(condition);
                        }

                        this.NotifyOnPlatformNameDeleted(platform);
                    }
                }
            }

            return VSConstants.S_OK;
        }

		private void RemovePropertyGroupsWithMatchingCondition(string condition)
		{
		}

        /// <summary>
        /// Returns the configuration associated with a specified configuration or platform name. 
        /// </summary>
        /// <param name="name">The name of the configuration to be returned.</param>
        /// <param name="platName">The name of the platform for the configuration to be returned.</param>
        /// <param name="cfg">The implementation of the IVsCfg interface.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetCfgOfName(string name, string platName, out IVsCfg cfg)
        {
            cfg = this.GetProjectConfiguration(name, platName);

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the per-configuration objects for this object. 
        /// </summary>
        /// <param name="celt">Number of configuration objects to be returned or zero, indicating a request for an unknown number of objects.</param>
        /// <param name="a">On input, pointer to an interface array or a null reference. On output, this parameter points to an array of IVsCfg interfaces belonging to the requested configuration objects.</param>
        /// <param name="actual">The number of configuration objects actually returned or a null reference, if this information is not necessary.</param>
        /// <param name="flags">Flags that specify settings for project configurations, or a null reference (Nothing in Visual Basic) if no additional flag settings are required. For valid prgrFlags values, see __VSCFGFLAGS.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetCfgs(uint celt, IVsCfg[] a, uint[] actual, uint[] flags)
        {
            if (flags != null)
            {
                flags[0] = 0;
            }

            int i = 0;
            string[] configList = this.GetConfigurations();
            string[] platformList = this.GetPlatforms();

            if (a != null)
            {
                foreach (string config in configList)
                {
                    foreach (string platform in platformList)
                    {
                        if (i < celt)
                        {
                            a[i] = this.GetProjectConfiguration(config, platform);
                            i++;
                        }
                    }
                }
            }
            else
            {
                i = configList.Length * platformList.Length;
            }

            if (actual != null)
            {
                actual[0] = (uint)i;
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns a specified configuration property. 
        /// </summary>
        /// <param name="propid">Specifies the property identifier for the property to return. For valid propid values, see __VSCFGPROPID.</param>
        /// <param name="var">The value of the property.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetCfgProviderProperty(int propid, out object var)
        {
            var = false;
            switch ((__VSCFGPROPID)propid)
            {
                case __VSCFGPROPID.VSCFGPROPID_SupportsPlatformAdd:
                    var = true;
                    break;

                case __VSCFGPROPID.VSCFGPROPID_SupportsPlatformDelete:
                    var = true;
                    break;

                default:
                    return base.GetCfgProviderProperty(propid, out var);
            }

            return VSConstants.S_OK;
        }

        /// <summary>
        /// Returns the existing configurations stored in the project file.
        /// </summary>
        /// <param name="celt">Specifies the requested number of property names. If this number is unknown, celt can be zero.</param>
        /// <param name="names">On input, an allocated array to hold the number of configuration property names specified by celt. This parameter can also be a null reference if the celt parameter is zero. 
        /// On output, names contains configuration property names.</param>
        /// <param name="actual">The actual number of property names returned.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetCfgNames(uint celt, string[] names, uint[] actual)
        {
            string[] configs = this.GetConfigurations();

            // This GetPlatforms method actually just deals with filling in the array,
            // so it works fine for the configurations array also.
            return ConfigProvider.GetPlatforms(celt, names, actual, configs);
        }

        /// <summary>
        /// Returns the list of platform names supported by the current WiX project.
        /// </summary>
        /// <param name="celt">Specifies the requested number of platform names. If this number is unknown, celt can be zero.</param>
        /// <param name="names">On input, an allocated array to hold the number of platform names specified by celt. This parameter can also be a null reference if the celt parameter is zero. On output, names contains platform names.</param>
        /// <param name="actual">The actual number of platform names returned.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetPlatformNames(uint celt, string[] names, uint[] actual)
        {
            string[] platforms = this.GetPlatforms();
            return ConfigProvider.GetPlatforms(celt, names, actual, platforms);
        }

        /// <summary>
        /// Gets the list of supported platform names for WiX projects.
        /// </summary>
        /// <param name="celt">Specifies the requested number of supported platform names. If this number is unknown, celt can be zero.</param>
        /// <param name="names">On input, an allocated array to hold the number of names specified by celt. This parameter can also be a null reference (Nothing in Visual Basic)if the celt parameter is zero. On output, names contains the names of supported platforms</param>
        /// <param name="actual">The actual number of platform names returned.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code.</returns>
        public override int GetSupportedPlatformNames(uint celt, string[] names, uint[] actual)
        {
            string[] supportedPlatforms = new string[] { WixProjectConfig.X86Platform, WixProjectConfig.X64Platform, WixProjectConfig.IA64Platform };
            return ConfigProvider.GetPlatforms(celt, names, actual, supportedPlatforms);
        }

        /// <summary>
        /// Provides access to the IVsProjectCfg interface implemented on a project's configuration object. 
        /// </summary>
        /// <param name="projectCfgCanonicalName">The canonical name of the configuration to access.</param>
        /// <param name="projectCfg">The IVsProjectCfg interface of the configuration identified by szProjectCfgCanonicalName.</param>
        /// <returns>If the method succeeds, it returns S_OK. If it fails, it returns an error code. </returns>
        public override int OpenProjectCfg(string projectCfgCanonicalName, out IVsProjectCfg projectCfg)
        {
            if (String.IsNullOrEmpty(projectCfgCanonicalName))
            {
                throw new ArgumentNullException("projectCfgCanonicalName");
            }

            ConfigCanonicalName config = new ConfigCanonicalName(projectCfgCanonicalName);
            projectCfg = this.GetProjectConfiguration(config);
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Adds a new property group for a new configuration or platform.
        /// </summary>
        /// <param name="config">Configuration name of the full configuration being added.</param>
        /// <param name="platform">Platform name of the full configuration being added.</param>
        /// <param name="cloneCondition">Condition of property group to clone, if it exists.</param>
        private void AddNewConfigPropertyGroup(string config, string platform, string cloneCondition)
        {
            MSBuildConstruction.ProjectPropertyGroupElement newPropertyGroup = null;

            if (!String.IsNullOrEmpty(cloneCondition))
            {
                foreach (MSBuildConstruction.ProjectPropertyGroupElement propertyGroup in this.ProjectMgr.BuildProject.Xml.PropertyGroups)
                {
                    if (String.Equals(propertyGroup.Condition.Trim(), cloneCondition.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        newPropertyGroup = this.ProjectMgr.ClonePropertyGroup(propertyGroup);
						foreach (MSBuildConstruction.ProjectPropertyElement property in newPropertyGroup.Properties)
						{
							if (property.Name.Equals(WixProjectFileConstants.OutputPath) ||
								property.Name.Equals(WixProjectFileConstants.IntermediateOutputPath))
							{
								property.Parent.RemoveChild(property);
							}
						}
                        break;
                    }
                }
            }

            if (newPropertyGroup == null)
            {
                newPropertyGroup = this.ProjectMgr.BuildProject.Xml.AddPropertyGroup();
                IList<KeyValuePair<KeyValuePair<string, string>, string>> propVals = this.NewConfigProperties;
                foreach (KeyValuePair<KeyValuePair<string, string>, string> data in propVals)
                {
                    KeyValuePair<string, string> propData = data.Key;
                    string value = data.Value;
                    MSBuildConstruction.ProjectPropertyElement newProperty = newPropertyGroup.AddProperty(propData.Key, value);
                    if (!String.IsNullOrEmpty(propData.Value))
                    {
                        newProperty.Condition = propData.Value;
                    }
                }
            }

            string outputBasePath = this.ProjectMgr.OutputBaseRelativePath;
            string outputPath = Path.Combine(outputBasePath, WixConfigProvider.ConfigPath);
            newPropertyGroup.AddProperty(WixProjectFileConstants.OutputPath, outputPath);

            string intermediateBasePath = WixConfigProvider.IntermediateBaseRelativePath;
            string intermediatePath = Path.Combine(intermediateBasePath, WixConfigProvider.ConfigPath);
            newPropertyGroup.AddProperty(WixProjectFileConstants.IntermediateOutputPath, intermediatePath);

            string newCondition = String.Format(CultureInfo.InvariantCulture, WixProjectConfig.ConfigAndPlatformConditionString, config, platform);
            newPropertyGroup.Condition = newCondition;
        }

        private string[] GetConfigurations()
        {
            string[] configs = this.GetPropertiesConditionedOn(ProjectFileConstants.Configuration);
            if (configs.Length == 0 ||
                (configs.Length == 1 && (configs[0] == WixProjectConfig.DebugConfiguration || configs[0] == WixProjectConfig.ReleaseConfiguration)))
            {
                // If there are no configuration conditions, or only Debug or only Release is defined, then
                // assume both the standard Debug and Release configs are available.
                return new string[] { WixProjectConfig.DebugConfiguration, WixProjectConfig.ReleaseConfiguration };
            }
            else
            {
                return configs;
            }
        }

        private string[] GetPlatforms()
        {
            string[] platforms = this.GetPropertiesConditionedOn(ProjectFileConstants.Platform);
            List<string> platformsList = new List<string>(platforms);

            // MSBuild always adds AnyCPU to the platforms list. Remove it because it is not supported by WiX projects.
            platformsList.Remove("AnyCPU");
            platformsList.Remove("Any CPU");

            if (platformsList.Count == 0)
            {
                // If there are no platform conditions, assume the platform is x86.
                platformsList.Add(WixProjectConfig.X86Platform);
            }

            return platformsList.ToArray();
        }

        private ProjectConfig GetProjectConfiguration(string config, string platform)
        {
            ConfigCanonicalName configCanonicalName = new ConfigCanonicalName(config, platform);
            if (this.configurationsList.ContainsKey(configCanonicalName))
            {
                return this.configurationsList[configCanonicalName];
            }

            ProjectConfig requestedConfiguration = new WixProjectConfig((WixProjectNode) this.ProjectMgr, config, platform);
            this.configurationsList.Add(configCanonicalName, requestedConfiguration);

            return requestedConfiguration;
        }
    }
}
