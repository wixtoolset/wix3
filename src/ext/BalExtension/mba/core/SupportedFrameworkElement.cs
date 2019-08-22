// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Configuration;

    /// <summary>
    /// Handler for the supportedFramework configuration section.
    /// </summary>
    public sealed class SupportedFrameworkElement : ConfigurationElement
    {
        private static readonly ConfigurationProperty versionProperty = new ConfigurationProperty("version", typeof(string), null, ConfigurationPropertyOptions.IsRequired);
        private static readonly ConfigurationProperty runtimeVersionProperty = new ConfigurationProperty("runtimeVersion", typeof(string));

        /// <summary>
        /// Creates a new instance of the <see cref="SupportedFrameworkElement"/> class.
        /// </summary>
        public SupportedFrameworkElement()
        {
        }

        /// <summary>
        /// Gets the version of the supported framework.
        /// </summary>
        /// <remarks>
        /// The assembly specified by this name must contain a value matching the NETFX version registry key under
        /// "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP".
        /// </remarks>
        [ConfigurationProperty("version", IsRequired = true)]
        public string Version
        {
            get { return (string)base[versionProperty]; }
            set { base[versionProperty] = value; }
        }

        /// <summary>
        /// Gets the runtime version required by this supported framework.
        /// </summary>
        [ConfigurationProperty("runtimeVersion", IsRequired = false)]
        public string RuntimeVersion
        {
            get { return (string)base[runtimeVersionProperty]; }
            set { base[runtimeVersionProperty] = value; }
        }
    }
}
