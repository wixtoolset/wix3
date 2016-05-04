// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Security;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// Contains useful helper methods.
    /// </summary>
    internal static class WixReferenceValidator
    {
        /// <summary>
        /// Checks to see if an assemlby to see if an assembly is a valid WixExtension.
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly.</param>
        /// <param name="extensionDependencyFolders">Dependent assembly path</param>/> 
        /// <returns>True if it is a valid Wix extension</returns>
        /// <remarks>
        /// <paramref name="assemblyPath"/> can be in several different forms:
        /// <list type="number">
        /// <item><term>Absolute path to an assembly (C:\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// <item><term>Relative path to an assembly (..\..\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// </list>
        /// </remarks>
        internal static bool IsValidWixExtension(string assemblyPath, params string[] extensionDependencyFolders)
        {
            if (String.IsNullOrEmpty(assemblyPath))
            {
                return false;
            }

            // Use a separate AppDomain so the extension assembly doesn't remain locked by this process.
            using (IsolatedDomain domain = new IsolatedDomain())
            {
                WixExtensionValidator extensionValidator = domain.CreateInstance<WixExtensionValidator>((object)extensionDependencyFolders);
                return extensionValidator.IsValid(assemblyPath);
            }
        }

        /// <summary>
        /// Validates the Wix library.
        /// </summary>
         /// <param name="filePath">Path to the Wix library.</param>
        /// <returns>Returns true if it is a valid Wix Library.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static bool IsValidWixLibrary(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // it is possible that cabinet is embeded at the beginning of file
            // if that is the case, skip the cabinet and jump to the start of xml.
            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    // look for the Microsoft cabinet file header and skip past the cabinet data if found
                    if ('M' == stream.ReadByte() && 'S' == stream.ReadByte() && 'C' == stream.ReadByte() && 'F' == stream.ReadByte())
                    {
                        long offset = 0;
                        byte[] offsetBuffer = new byte[4];

                        // skip the header checksum
                        stream.Seek(4, SeekOrigin.Current);

                        // read the cabinet file size
                        stream.Read(offsetBuffer, 0, 4);
                        offset = BitConverter.ToInt32(offsetBuffer, 0);

                        // seek past the cabinet file to the xml
                        stream.Seek(offset, SeekOrigin.Begin);
                    }
                    else // plain xml file - start reading xml at the beginning of the stream
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    // read the xml
                    using (XmlReader reader = new XmlTextReader(stream))
                    {
                        reader.MoveToContent();

                        if (reader.LocalName == "wixLibrary")
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Helper class to load an assembly and its dependents to inspect the types in the assembly.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        internal class WixExtensionValidator : MarshalByRefObject
        {
            private string[] extensionDependencyFolders;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="dependencyProbeFolders">Folders to probe for dependencies</param>
            public WixExtensionValidator(string[] dependencyProbeFolders)
            {
                this.extensionDependencyFolders = dependencyProbeFolders;
            }

            /// <summary>
            /// Checks to see if there is a single type that is a sub class of
            /// Microsoft.Tools.WindowsInstallerXml.WixExtension
            /// </summary>
            /// <param name="assemblyPath">Path of the assembly.</param>
            /// <returns>True if it is a valid Wix extension</returns>
            /// <remarks>
            /// <paramref name="assemblyPath"/> can be in several different forms:
            /// <list type="number">
            /// <item><term>Absolute path to an assembly (C:\MyExtensions\ExtensionAssembly.dll)</term></item>
            /// <item><term>Relative path to an assembly (..\..\MyExtensions\ExtensionAssembly.dll)</term></item>
            /// </list>
            /// </remarks>
            [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
            public bool IsValid(string assemblyPath)
            {
                if (String.IsNullOrEmpty(assemblyPath))
                {
                    return false;
                }

                bool result = false;

                AppDomain.CurrentDomain.AssemblyResolve += this.OnCurrentDomainAssemblyResolve;

                try
                {
                    // Use LoadFrom rather than ReflectionOnlyLoadFrom to try to validate that the extension is a valid
                    // and working WiX extension. By fully loading the assembly, we validate that all of its dependencies
                    // can be located and loaded. Since this is done in an isolated AppDomain that gets unloaded, there is
                    // no long-term cost to the working-set caused by loading all the dependencies. 
                    Assembly extensionAssembly = Assembly.LoadFrom(assemblyPath);

                    AssemblyDefaultWixExtensionAttribute extensionAttribute = (AssemblyDefaultWixExtensionAttribute)
                        Attribute.GetCustomAttribute(extensionAssembly, typeof(AssemblyDefaultWixExtensionAttribute));

                    Type extensionType = null;
                    if (extensionAttribute != null)
                    {
                        extensionType = extensionAttribute.ExtensionType;
                    }

                    if (extensionType != null && extensionType.IsSubclassOf(typeof(WixExtension)))
                    {
                        result = true;
                    }
                }
                catch
                {
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= this.OnCurrentDomainAssemblyResolve;
                }

                return result;
            }

            /// <summary>
            /// This event handler is invoked when a required assembly is not in the current folder.
            /// </summary>
            /// <param name="sender">Event originator</param>
            /// <param name="args">Resolve event arguments</param>
            /// <returns>Returns loaded assembly or null when it can't resolve/find the dependent assembly</returns>
            [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
            private Assembly OnCurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                if (args.Name == currentAssembly.GetName().FullName)
                {
                    return currentAssembly;
                }

                if (this.extensionDependencyFolders == null || this.extensionDependencyFolders.Length == 0)
                {
                    return null;
                }

                AssemblyName referenceAssemblyName = new AssemblyName(args.Name);

                foreach (string folder in this.extensionDependencyFolders)
                {
                    string assemblyPathWithoutExtension = Path.Combine(folder, referenceAssemblyName.Name);
                    string assemblyPath = assemblyPathWithoutExtension + ".dll";

                    if (!File.Exists(assemblyPath))
                    {
                        assemblyPath = assemblyPathWithoutExtension + ".exe";

                        if (!File.Exists(assemblyPath))
                        {
                            continue;
                        }
                    }

                    try
                    {
                        AssemblyName definitionAssemblyName = AssemblyName.GetAssemblyName(assemblyPath);
                        if (AssemblyName.ReferenceMatchesDefinition(referenceAssemblyName, definitionAssemblyName))
                        {
                            return Assembly.LoadFrom(assemblyPath);
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                    catch (SecurityException)
                    {
                    }
                    catch (FileLoadException)
                    {
                    }
                }

                return null;
            }
        }
    }
}
