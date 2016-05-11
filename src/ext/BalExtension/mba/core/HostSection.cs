// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Handler for the Host configuration section.
    /// </summary>
    public sealed class HostSection : ConfigurationSection
    {
        private static readonly ConfigurationProperty assemblyNameProperty = new ConfigurationProperty("assemblyName", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationProperty supportedFrameworksProperty = new ConfigurationProperty("", typeof(SupportedFrameworkElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);

        /// <summary>
        /// Creates a new instance of the <see cref="HostSection"/> class.
        /// </summary>
        public HostSection()
        {
        }

        /// <summary>
        /// Gets the name of the assembly that contians the <see cref="BootstrapperApplication"/> child class.
        /// </summary>
        /// <remarks>
        /// The assembly specified by this name must contain the <see cref="BootstrapperApplicationAttribute"/> to identify
        /// the type of the <see cref="BootstrapperApplication"/> child class.
        /// </remarks>
        [ConfigurationProperty("assemblyName", IsRequired = true)]
        public string AssemblyName
        {
            get { return (string)base[assemblyNameProperty]; }
            set { base[assemblyNameProperty] = value; }
        }

        /// <summary>
        /// Gets the <see cref="SupportedFrameworkElementCollection"/> of supported frameworks for the host configuration.
        /// </summary>
        [ConfigurationProperty("", IsDefaultCollection = true)]
        [ConfigurationCollection(typeof(SupportedFrameworkElement))]
        public SupportedFrameworkElementCollection SupportedFrameworks
        {
            get { return (SupportedFrameworkElementCollection)base[supportedFrameworksProperty]; }
        }
    }
}
