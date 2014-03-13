//-------------------------------------------------------------------------------------------------
// <copyright file="HeatExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// An extension for the Windows Installer XML Toolset Harvester application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.Tools.WindowsInstallerXml;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// A command line option.
    /// </summary>
    public struct HeatCommandLineOption
    {
        public string Option;
        public string Description;

        /// <summary>
        /// Instantiates a new CommandLineOption.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <param name="description">The description of the option.</param>
        public HeatCommandLineOption(string option, string description)
        {
            this.Option = option;
            this.Description = description;
        }
    }

    /// <summary>
    /// An extension for the Windows Installer XML Toolset Harvester application.
    /// </summary>
    public abstract class HeatExtension
    {
        private HeatCore core;
        private ConsoleMessageHandler messageHandler;

        /// <summary>
        /// Gets or sets the heat core for the extension.
        /// </summary>
        /// <value>The heat core for the extension.</value>
        public HeatCore Core
        {
            get { return this.core; }
            set { this.core = value; }
        }

        /// <summary>
        /// Gets or sets the message handler for the extension.
        /// </summary>
        /// <value>The message handler for the extension.</value>
        public ConsoleMessageHandler MessageHandler
        {
            get { return this.messageHandler; }
            set { this.messageHandler = value; }
        }

        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        public virtual HeatCommandLineOption[] CommandLineTypes
        {
            get { return null; }
        }

        /// <summary>
        /// Loads a HeatExtension from a type description string.
        /// </summary>
        /// <param name="extension">The extension type description string.</param>
        /// <returns>The loaded HeatExtension.</returns>
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
        public static HeatExtension Load(string extension)
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
                    AssemblyDefaultHeatExtensionAttribute extensionAttribute = (AssemblyDefaultHeatExtensionAttribute)Attribute.GetCustomAttribute(extensionAssembly, typeof(AssemblyDefaultHeatExtensionAttribute));

                    if (null != extensionAttribute)
                    {
                        extensionType = extensionAttribute.ExtensionType;
                    }
                    else
                    {
                        throw new WixException(WixErrors.InvalidExtensionType(assemblyName, typeof(AssemblyDefaultHeatExtensionAttribute).ToString()));
                    }
                }
            }

            if (extensionType.IsSubclassOf(typeof(HeatExtension)))
            {
                return Activator.CreateInstance(extensionType) as HeatExtension;
            }
            else
            {
                throw new WixException(WixErrors.InvalidExtensionType(extension, extensionType.ToString(), typeof(HeatExtension).ToString()));
            }
        }

        /// <summary>
        /// Parse the command line options for this extension.
        /// </summary>
        /// <param name="type">The active harvester type.</param>
        /// <param name="args">The option arguments.</param>
        public virtual void ParseOptions(string type, string[] args)
        {
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
