//-------------------------------------------------------------------------------------------------
// <copyright file="DllHarvester.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Harvest WiX authoring from a native DLL file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.InteropServices;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring from a native DLL file.
    /// </summary>
    public sealed class DllHarvester
    {
        /// <summary>
        /// Harvest the registry values written by calling DllRegisterServer on the specified file.
        /// </summary>
        /// <param name="file">The file to harvest registry values from.</param>
        /// <returns>The harvested registry values.</returns>
        public Wix.RegistryValue[] HarvestRegistryValues(string file)
        {
            // load the DLL
            NativeMethods.LoadLibrary(file);

            using (RegistryHarvester registryHarvester = new RegistryHarvester(true))
            {
                try
                {
                    DynamicPInvoke(file, "DllRegisterServer", typeof(int), null, null);

                    return registryHarvester.HarvestRegistry();
                }
                catch (TargetInvocationException e)
                {
                    e.Data["file"] = file;
                    throw;
                }
            }
        }

        /// <summary>
        /// Dynamically PInvokes into a DLL.
        /// </summary>
        /// <param name="dll">Dynamic link library containing the entry point.</param>
        /// <param name="entryPoint">Entry point into dynamic link library.</param>
        /// <param name="returnType">Return type of entry point.</param>
        /// <param name="parameterTypes">Type of parameters to entry point.</param>
        /// <param name="parameterValues">Value of parameters to entry point.</param>
        /// <returns>Value from invoked code.</returns>
        private static object DynamicPInvoke(string dll, string entryPoint, Type returnType, Type[] parameterTypes, object[] parameterValues)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "wixTempAssembly";

            AssemblyBuilder dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder dynamicModule = dynamicAssembly.DefineDynamicModule("wixTempModule");

            MethodBuilder dynamicMethod = dynamicModule.DefinePInvokeMethod(entryPoint, dll, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.PinvokeImpl, CallingConventions.Standard, returnType, parameterTypes, CallingConvention.Winapi, CharSet.Ansi);
            dynamicModule.CreateGlobalFunctions();

            MethodInfo methodInfo = dynamicModule.GetMethod(entryPoint);
            return methodInfo.Invoke(null, parameterValues);
        }

        /// <summary>
        /// Native methods for loading libraries.
        /// </summary>
        private sealed class NativeMethods
        {
            private const UInt32 LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

            /// <summary>
            /// Load a DLL library.
            /// </summary>
            /// <param name="file">The file name of the executable module.</param>
            /// <returns>If the function succeeds, the return value is a handle to the mapped executable module.</returns>
            internal static IntPtr LoadLibrary(string file)
            {
                IntPtr dllHandle = LoadLibraryEx(file, IntPtr.Zero, NativeMethods.LOAD_WITH_ALTERED_SEARCH_PATH);

                if (IntPtr.Zero == dllHandle)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    throw new Exception(String.Format("Unable to load file: {0}, error: {1}", file, lastError));
                }

                return dllHandle;
            }

            /// <summary>
            /// Maps the specified executable module into the address space of the calling process.
            /// </summary>
            /// <param name="file">The file name of the executable module.</param>
            /// <param name="fileHandle">This parameter is reserved for future use. It must be NULL.</param>
            /// <param name="flags">Action to take when loading the module.</param>
            /// <returns>If the function succeeds, the return value is a handle to the mapped executable module.</returns>
            [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr LoadLibraryEx(string file, IntPtr fileHandle, UInt32 flags);
        }
    }
}
