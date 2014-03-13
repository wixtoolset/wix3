//-------------------------------------------------------------------------------------------------
// <copyright file="AssemblyHarvester.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Harvest WiX authoring from an assembly file.
// </summary>
//-------------------------------------------------------------------------------------------------

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
