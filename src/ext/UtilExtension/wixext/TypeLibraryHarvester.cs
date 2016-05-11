// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring from a type library file.
    /// </summary>
    public sealed class TypeLibraryHarvester
    {
        /// <summary>
        /// Harvest the registry values written by RegisterTypeLib.
        /// </summary>
        /// <param name="path">The file to harvest registry values from.</param>
        /// <returns>The harvested registry values.</returns>
        public Wix.RegistryValue[] HarvestRegistryValues(string path)
        {
            using (RegistryHarvester registryHarvester = new RegistryHarvester(true))
            {
                NativeMethods.RegisterTypeLibrary(path);

                return registryHarvester.HarvestRegistry();
            }
        }

        /// <summary>
        /// Parses a hexadecimal version string into a Version object.
        /// </summary>
        /// <param name="versionString">Hexadecimal version string, for example "1.A.3C.F241"</param>
        /// <returns>Version object, or null if versionString is not a valid hex version.</returns>
        public static Version ParseHexVersion(string versionString)
        {
            if (String.IsNullOrEmpty(versionString))
            {
                return null;
            }

            int[] versionNumbers = new int[4];
            string[] versionNumberStrings = versionString.Split('.');

            for (int i = 0; i < versionNumbers.Length && i < versionNumberStrings.Length; i++)
            {
                if (!Int32.TryParse(versionNumberStrings[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out versionNumbers[i]))
                {
                    return null;
                }
            }

            return new Version(versionNumbers[0], versionNumbers[1], versionNumbers[2], versionNumbers[3]);
        }

        /// <summary>
        /// Native methods for registering type libraries.
        /// </summary>
        private sealed class NativeMethods
        {
            /// <summary>
            /// Registers a type library.
            /// </summary>
            /// <param name="typeLibraryFile">The type library file to register.</param>
            internal static void RegisterTypeLibrary(string typeLibraryFile)
            {
                IntPtr ptlib;

                LoadTypeLib(typeLibraryFile, out ptlib);

                RegisterTypeLib(ptlib, typeLibraryFile, null);
            }

            /// <summary>
            /// Loads and registers a type library.
            /// </summary>
            /// <param name="szFile">Contains the name of the file from which LoadTypeLib should attempt to load a type library.</param>
            /// <param name="pptlib">On return, contains a pointer to a pointer to the loaded type library.</param>
            /// <remarks>LoadTypeLib will not register the type library if the path of the type library is specified.</remarks>
            [DllImport("oleaut32.dll", PreserveSig = false)]
            private static extern void LoadTypeLib([MarshalAs(UnmanagedType.BStr)] string szFile, out IntPtr pptlib);

            /// <summary>
            /// Adds information about a type library to the system registry.
            /// </summary>
            /// <param name="ptlib">Pointer to the type library being registered.</param>
            /// <param name="szFullPath">Fully qualified path specification for the type library being registered.</param>
            /// <param name="szHelpDir">Directory in which the Help file for the library being registered can be found. Can be Null.</param>
            [DllImport("oleaut32.dll", PreserveSig = false)]
            private static extern void RegisterTypeLib(IntPtr ptlib, [MarshalAs(UnmanagedType.BStr)] string szFullPath, [MarshalAs(UnmanagedType.BStr)] string szHelpDir);
        }
    }
}
