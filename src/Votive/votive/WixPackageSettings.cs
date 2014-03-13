//-------------------------------------------------------------------------------------------------
// <copyright file="WixPackageSettings.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixPackageSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Security.AccessControl;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.Win32;

    /// <summary>
    /// Helper class for setting and retrieving registry settings for the package. All machine
    /// settings are cached on first use, so only one registry read is performed.
    /// </summary>
    public class WixPackageSettings
    {
        // =========================================================================================
        // Member Variables
        // =========================================================================================

        private static readonly Version DefaultVersion = new Version(8, 0, 50727, 42);

        private string devEnvPath;
        private string visualStudioRegistryRoot;
        private Version visualStudioVersion;
        private MachineSettingString toolsDirectory;

        // =========================================================================================
        // Constructors
        // =========================================================================================

        /// <summary>
        /// Initializes a new instance of the <see cref="WixPackageSettings"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
        public WixPackageSettings(IServiceProvider serviceProvider)
        {
            WixHelperMethods.VerifyNonNullArgument(serviceProvider, "serviceProvider");

            if (serviceProvider != null)
            {
                // get the Visual Studio registry root
                ILocalRegistry3 localRegistry = WixHelperMethods.GetService<ILocalRegistry3, SLocalRegistry>(serviceProvider);
                ErrorHandler.ThrowOnFailure(localRegistry.GetLocalRegistryRoot(out this.visualStudioRegistryRoot));
            }
        }

        // =========================================================================================
        // Properties
        // =========================================================================================

        /// <summary>
        /// Gets the path to the directory where the WiX tools reside.
        /// </summary>
        /// <value>The path to the directory where the WiX tools reside.</value>
        public virtual string ToolsDirectory
        {
            get
            {
                if (this.toolsDirectory == null && this.visualStudioRegistryRoot != null)
                {
                    string machineRootPath = WixHelperMethods.RegistryPathCombine(this.visualStudioRegistryRoot, @"InstalledProducts\WiX");

                    // initialize all of the machine settings
                    this.toolsDirectory = new MachineSettingString(machineRootPath, KeyNames.ToolsDirectory, String.Empty);
                }

                return (this.toolsDirectory != null ? this.toolsDirectory.Value : null);
            }
        }

        /// <summary>
        /// Gets the absolute path to the devenv.exe that we're currently running in.
        /// </summary>
        /// <value>The absolute path to the devenv.exe that we're currently running in.</value>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Env")]
        public virtual string DevEnvPath
        {
            get
            {
                if (this.devEnvPath == null && this.visualStudioRegistryRoot != null)
                {
                    string regPath = WixHelperMethods.RegistryPathCombine(this.visualStudioRegistryRoot, @"Setup\VS");
                    using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
                    {
                        this.devEnvPath = regKey.GetValue("EnvironmentPath", String.Empty) as string;
                    }
                }

                return this.devEnvPath;
            }
        }

        /// <summary>
        /// Gets the version of the currently running instance of Visual Studio.
        /// </summary>
        /// <value>The version of the currently running instance of Visual Studio.</value>
        public virtual Version VisualStudioVersion
        {
            get
            {
                if (this.visualStudioVersion == null && this.visualStudioRegistryRoot != null)
                {
                    string regPath = WixHelperMethods.RegistryPathCombine(this.visualStudioRegistryRoot, @"Setup\VS\BuildNumber");
                    using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(regPath, RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey))
                    {
                        string lcid = CultureInfo.CurrentUICulture.LCID.ToString(CultureInfo.InvariantCulture);
                        string versionString = regKey.GetValue(lcid) as string;
                        if (versionString == null)
                        {
                            WixHelperMethods.TraceFail("Cannot find the Visual Studio environment version in the registry path '{0}'.", WixHelperMethods.RegistryPathCombine(regPath, lcid));
                            this.visualStudioVersion = DefaultVersion;
                        }
                        else
                        {
                            try
                            {
                                this.visualStudioVersion = new Version(versionString);
                            }
                            catch (ArgumentException e)
                            {
                                WixHelperMethods.TraceFail("Invalid Visual Studio environment version string {0}: {1}", versionString, e);
                                this.visualStudioVersion = DefaultVersion;
                            }
                            catch (OverflowException e)
                            {
                                WixHelperMethods.TraceFail("Invalid Visual Studio environment version string {0}: {1}", versionString, e);
                                this.visualStudioVersion = DefaultVersion;
                            }
                            catch (FormatException e)
                            {
                                WixHelperMethods.TraceFail("Cannot parse the Visual Studio environment version string {0}: {1}", versionString, e);
                                this.visualStudioVersion = DefaultVersion;
                            }
                        }
                    }
                }

                return this.visualStudioVersion;
            }
        }

        // =========================================================================================
        // Classes
        // =========================================================================================

        /// <summary>
        /// Names of the various registry keys that store our settings.
        /// </summary>
        private static class KeyNames
        {
            public const string ToolsDirectory = "ToolsDirectory";
        }

        /// <summary>
        /// Abstract base class for a strongly-typed machine-level setting.
        /// </summary>
        /// <typeparam name="T">The type of the machine-level setting.</typeparam>
        private abstract class MachineSetting<T>
        {
            private T defaultValue;
            private bool initialized;
            private string name;
            private string rootPath;
            private T settingValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="MachineSetting&lt;T&gt;"/> class.
            /// </summary>
            /// <param name="rootPath">The root path of the registry key.</param>
            /// <param name="name">The name of the registry value to query.</param>
            /// <param name="defaultValue">The value to use if the registry key is not present.</param>
            public MachineSetting(string rootPath, string name, T defaultValue)
            {
                this.rootPath = rootPath;
                this.name = name;
                this.defaultValue = defaultValue;
            }

            /// <summary>
            /// Gets the name of the registry value to query.
            /// </summary>
            public string Name
            {
                get { return this.name; }
            }

            /// <summary>
            /// Gets or sets the value of the registry key element.
            /// </summary>
            public T Value
            {
                get
                {
                    if (!this.initialized)
                    {
                        this.Refresh();
                    }

                    return this.settingValue;
                }

                protected set
                {
                    this.settingValue = value;
                }
            }

            /// <summary>
            /// Gets the default value if the registry path is not present.
            /// </summary>
            protected T DefaultValue
            {
                get { return this.defaultValue; }
            }

            /// <summary>
            /// Refreshes the cached value by reading from the registry.
            /// </summary>
            public void Refresh()
            {
                using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey(this.rootPath, false))
                {
                    object regValue = regKey.GetValue(this.name, this.defaultValue, RegistryValueOptions.None);
                    this.initialized = true;
                    this.settingValue = (T) regValue;
                }
            }

            /// <summary>
            /// Casts the value read from the registry to the appropriate type and caches it.
            /// </summary>
            /// <param name="value">The value read from the registry.</param>
            protected abstract void CastAndStoreValue(object value);
        }

        /// <summary>
        /// Represents a strongly-typed integer machine setting.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        private sealed class MachineSettingInt32 : MachineSetting<int>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MachineSettingInt32"/> class.
            /// </summary>
            /// <param name="rootPath">The root path of the registry key.</param>
            /// <param name="name">The name of the registry value to query.</param>
            /// <param name="defaultValue">The value to use if the registry key is not present.</param>
            public MachineSettingInt32(string rootPath, string name, int defaultValue)
                : base(rootPath, name, defaultValue)
            {
            }

            /// <summary>
            /// Casts the value read from the registry to the appropriate type and caches it.
            /// </summary>
            /// <param name="value">The value read from the registry.</param>
            protected override void CastAndStoreValue(object value)
            {
                try
                {
                    this.Value = (int)value;
                }
                catch (InvalidCastException)
                {
                    this.Value = this.DefaultValue;
                    WixHelperMethods.TraceFail("Cannot convert '{0}' to an Int32.", value);
                }
            }
        }

        /// <summary>
        /// Represents a strongly-typed string machine setting.
        /// </summary>
        private sealed class MachineSettingString : MachineSetting<string>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MachineSettingString"/> class.
            /// </summary>
            /// <param name="rootPath">The root path of the registry key.</param>
            /// <param name="name">The name of the registry value to query.</param>
            /// <param name="defaultValue">The value to use if the registry key is not present.</param>
            public MachineSettingString(string rootPath, string name, string defaultValue)
                : base(rootPath, name, defaultValue)
            {
            }

            /// <summary>
            /// Casts the value read from the registry to the appropriate type and caches it.
            /// </summary>
            /// <param name="value">The value read from the registry.</param>
            protected override void CastAndStoreValue(object value)
            {
                try
                {
                    this.Value = (string)value;
                }
                catch (InvalidCastException)
                {
                    this.Value = this.DefaultValue;
                    WixHelperMethods.TraceFail("Cannot convert '{0}' to a string.", value);
                }
            }
        }

        /// <summary>
        /// Represents a strongly-typed enum machine setting.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        private class MachineSettingEnum<T> : MachineSetting<T> where T: struct
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MachineSettingEnum&lt;T&gt;"/> class.
            /// </summary>
            /// <param name="rootPath">The root path of the registry key.</param>
            /// <param name="name">The name of the registry value to query.</param>
            /// <param name="defaultValue">The value to use if the registry key is not present.</param>
            public MachineSettingEnum(string rootPath, string name, T defaultValue)
                : base(rootPath, name, defaultValue)
            {
            }

            /// <summary>
            /// Casts the value read from the registry to the appropriate type and caches it.
            /// </summary>
            /// <param name="value">The value read from the registry.</param>
            protected override void CastAndStoreValue(object value)
            {
                try
                {
                    this.Value = (T)Enum.Parse(typeof(T), value.ToString(), true);
                }
                catch (Exception e)
                {
                    if (e is FormatException || e is InvalidCastException)
                    {
                        this.Value = this.DefaultValue;
                        WixHelperMethods.TraceFail("Cannot convert '{0}' to an enum of type '{1}'.", value, typeof(T).Name);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
