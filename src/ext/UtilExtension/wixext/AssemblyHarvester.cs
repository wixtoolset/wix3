// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring from an assembly file.
    /// </summary>
    public sealed class AssemblyHarvester
    {
        /// <summary>
        /// Harvest the registry values written by RegisterAssembly.
        /// </summary>
        /// <param name="path">The file to harvest registry values from.</param>
        /// <returns>The harvested registry values.</returns>
        public Wix.RegistryValue[] HarvestRegistryValues(string path)
        {
            RegistrationServices regSvcs = new RegistrationServices();
            Assembly assembly = Assembly.LoadFrom(path);

            // must call this before overriding registry hives to prevent binding failures
            // on exported types during RegisterAssembly
            assembly.GetExportedTypes();

            using (RegistryHarvester registryHarvester = new RegistryHarvester(true))
            {
                regSvcs.RegisterAssembly(assembly, AssemblyRegistrationFlags.SetCodeBase);

                return registryHarvester.HarvestRegistry();
            }
        }
    }
}
