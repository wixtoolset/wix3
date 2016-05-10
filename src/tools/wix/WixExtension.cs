// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// The main class for a WiX extension.
    /// </summary>
    public class WixExtension
    {
        /// <summary>
        /// Allows the extension to parse custom command-line arguments.
        /// </summary>
        /// <param name="args">Collection of custom command-line arguments.</param>
        /// <param name="messageHandler">Message handler to send errors if any occur.</param>
        /// <returns>Collection of unknown command-line arguments for other extensions to possibly parse.</returns>
        public virtual StringCollection ParseCommandLine(StringCollection args, IMessageHandler messageHandler)
        {
            return args;
        }

        /// <summary>
        /// Gets the optional binder extension.
        /// </summary>
        /// <value>The optional binder extension.</value>
        public virtual BinderExtension BinderExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional binder file manager.
        /// </summary>
        /// <value>The optional binder file manager.</value>
        public virtual BinderFileManager BinderFileManager
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional compiler extension.
        /// </summary>
        /// <value>The optional compiler extension.</value>
        public virtual CompilerExtension CompilerExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional inspector extension.
        /// </summary>
        /// <value>The optional inspector extension.</value>
        public virtual InspectorExtension InspectorExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional binder replacement.
        /// </summary>
        /// <value>The optional binder replacement.</value>
        public virtual WixBinder CustomBinder
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional decompiler extension.
        /// </summary>
        /// <value>The optional decompiler extension.</value>
        public virtual DecompilerExtension DecompilerExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional preprocessor extension.
        /// </summary>
        /// <value>The optional preprocessor extension.</value>
        public virtual PreprocessorExtension PreprocessorExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional table definitions for this extension.
        /// </summary>
        /// <value>The optional table definitions for this extension.</value>
        public virtual TableDefinitionCollection TableDefinitions
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional unbinder extension.
        /// </summary>
        /// <value>The optional unbinder extension.</value>
        public virtual UnbinderExtension UnbinderExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional validator extension.
        /// </summary>
        /// <value>The optional validator extension.</value>
        public virtual ValidatorExtension ValidatorExtension
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the optional default culture.
        /// </summary>
        /// <value>The optional default culture.</value>
        public virtual string DefaultCulture
        {
            get { return null; }
        }

        /// <summary>
        /// Loads a WixExtension from a type description string.
        /// </summary>
        /// <param name="extension">The extension type description string.</param>
        /// <returns>The loaded WixExtension.</returns>
        /// <remarks>
        /// <paramref name="extension"/> can be in several different forms:
        /// <list type="number">
        /// <item><term>AssemblyQualifiedName (TopNamespace.SubNameSpace.ContainingClass+NestedClass, MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089)</term></item>
        /// <item><term>AssemblyName (MyAssembly, Version=1.3.0.0, Culture=neutral, PublicKeyToken=b17a5c561934e089)</term></item>
        /// <item><term>Absolute path to an assembly (C:\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// <item><term>Filename of an assembly in the application directory (ExtensionAssembly.dll)</term></item>
        /// <item><term>Relative path to an assembly (..\..\MyExtensions\ExtensionAssembly.dll)</term></item>
        /// </list>
        /// To specify a particular class to use, prefix the fully qualified class name to the assembly and separate them with a comma.
        /// For example: "TopNamespace.SubNameSpace.ContainingClass+NestedClass, C:\MyExtensions\ExtensionAssembly.dll"
        /// </remarks>
        public static WixExtension Load(string extension)
        {
            Type extensionType = null;
            int commaIndex = extension.IndexOf(',');
            string className = String.Empty;
            string assemblyName = extension;

            if (0 <= commaIndex)
            {
                className = extension.Substring(0, commaIndex);
                assemblyName = (extension.Length <= commaIndex + 1 ? String.Empty : extension.Substring(commaIndex + 1));
            }

            className = className.Trim();
            assemblyName = assemblyName.Trim();

            if (null == extensionType && 0 < assemblyName.Length)
            {

                Assembly extensionAssembly;

                // case 3: Absolute path to an assembly
                if (Path.IsPathRooted(assemblyName))
                {
                    extensionAssembly = ExtensionLoadFrom(assemblyName);
                }
                else
                {
                    try
                    {
                        // case 2: AssemblyName
                        extensionAssembly = Assembly.Load(assemblyName);
                    }
                    catch (IOException e)
                    {
                        if (e is FileLoadException || e is FileNotFoundException)
                        {
                            try
                            {
                                // case 4: Filename of an assembly in the application directory
                                extensionAssembly = Assembly.Load(Path.GetFileNameWithoutExtension(assemblyName));
                            }
                            catch (IOException innerE)
                            {
                                if (innerE is FileLoadException || innerE is FileNotFoundException)
                                {
                                    // case 5: Relative path to an assembly

                                    // we want to use Assembly.Load when we can because it has some benefits over Assembly.LoadFrom
                                    // (see the documentation for Assembly.LoadFrom). However, it may fail when the path is a relative
                                    // path, so we should try Assembly.LoadFrom one last time. We could have detected a directory
                                    // separator character and used Assembly.LoadFrom directly, but dealing with path canonicalization
                                    // issues is something we don't want to deal with if we don't have to.
                                    extensionAssembly = ExtensionLoadFrom(assemblyName);
                                }
                                else
                                {
                                    throw new WixException(WixErrors.InvalidExtension(assemblyName, innerE.Message));
                                }
                            }
                        }
                        else
                        {
                            throw new WixException(WixErrors.InvalidExtension(assemblyName, e.Message));
                        }
                    }
                }

                if (0 < className.Length)
                {
                    try
                    {
                        // case 1: AssemblyQualifiedName
                        extensionType = extensionAssembly.GetType(className, true /* throwOnError */, true /* ignoreCase */);
                    }
                    catch(Exception e)
                    {
                        throw new WixException(WixErrors.InvalidExtensionType(assemblyName, className, e.GetType().ToString(), e.Message));
                    }
                }
                else
                {
                    // if no class name was specified, then let's hope the assembly defined a default WixExtension
                    AssemblyDefaultWixExtensionAttribute extensionAttribute = (AssemblyDefaultWixExtensionAttribute)Attribute.GetCustomAttribute(extensionAssembly, typeof(AssemblyDefaultWixExtensionAttribute));

                    if (null != extensionAttribute)
                    {
                        extensionType = extensionAttribute.ExtensionType;
                    }
                    else
                    {
                        throw new WixException(WixErrors.InvalidExtensionType(assemblyName, typeof(AssemblyDefaultWixExtensionAttribute).ToString()));
                    }
                }
            }

            if (extensionType.IsSubclassOf(typeof(WixExtension)))
            {
                return Activator.CreateInstance(extensionType) as WixExtension;
            }
            else
            {
                throw new WixException(WixErrors.InvalidExtensionType(extension, extensionType.ToString(), typeof(WixExtension).ToString()));
            }
        }

        /// <summary>
        /// Gets the library associated with this extension.
        /// </summary>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The library for this extension.</returns>
        public virtual Library GetLibrary(TableDefinitionCollection tableDefinitions)
        {
            return null;
        }

        /// <summary>
        /// Help for loading a library from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <param name="tableDefinitions">The table definitions to use while loading the library.</param>
        /// <returns>The loaded library.</returns>
        protected static Library LoadLibraryHelper(Assembly assembly, string resourceName, TableDefinitionCollection tableDefinitions)
        {
            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                UriBuilder uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "embeddedresource";
                uriBuilder.Path = assembly.Location;
                uriBuilder.Fragment = resourceName;

                return Library.Load(resourceStream, uriBuilder.Uri, tableDefinitions, false, true);
            }
        }

        /// <summary>
        /// Helper for loading table definitions from an embedded resource.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resource.</param>
        /// <param name="resourceName">The name of the embedded resource being requested.</param>
        /// <returns>The loaded table definitions.</returns>
        protected static TableDefinitionCollection LoadTableDefinitionHelper(Assembly assembly, string resourceName)
        {
            XmlReader reader = null;

            try
            {
                using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    reader = new XmlTextReader(resourceStream);

                    return TableDefinitionCollection.Load(reader, false);
                }
            }
            finally
            {
                if (null != reader)
                {
                    reader.Close();
                }
            }
        }

        private static Assembly ExtensionLoadFrom(string assemblyName)
        {
            Assembly extensionAssembly = null;

            try
            {
                extensionAssembly = Assembly.LoadFrom(assemblyName);
            }
            catch (Exception e)
            {
                throw new WixException(WixErrors.InvalidExtension(assemblyName, e.Message));
            }

            return extensionAssembly;
        }
    }
}
