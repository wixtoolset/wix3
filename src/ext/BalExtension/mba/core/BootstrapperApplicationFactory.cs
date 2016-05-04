// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Configuration;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class used by the MUX host to create and return the IBootstrapperApplication implementation to the engine.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    public sealed class BootstrapperApplicationFactory : MarshalByRefObject, IBootstrapperApplicationFactory
    {
        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperApplicationFactory"/> class.
        /// </summary>
        public BootstrapperApplicationFactory()
        {
        }

        /// <summary>
        /// Loads the bootstrapper application assembly and creates an instance of the IBootstrapperApplication.
        /// </summary>
        /// <param name="pEngine">IBootstrapperEngine provided for the bootstrapper application.</param>
        /// <param name="command">Command line for the bootstrapper application.</param>
        /// <returns>Bootstrapper application via <see cref="IBootstrapperApplication"/> interface.</returns>
        /// <exception cref="MissingAttributeException">The bootstrapper application assembly
        /// does not define the <see cref="BootstrapperApplicationAttribute"/>.</exception>
        public IBootstrapperApplication Create(IBootstrapperEngine pEngine, ref Command command)
        {
            // Get the wix.boostrapper section group to get the name of the bootstrapper application assembly to host.
            HostSection section = ConfigurationManager.GetSection("wix.bootstrapper/host") as HostSection;
            if (null == section)
            {
                throw new MissingAttributeException(); // TODO: throw a more specific exception than this.
            }

            // Load the BA and make sure it extends BootstrapperApplication.
            Type baType = BootstrapperApplicationFactory.GetBootstrapperApplicationTypeFromAssembly(section.AssemblyName);
            BootstrapperApplication ba = Activator.CreateInstance(baType) as BootstrapperApplication;
            if (null == ba)
            {
                throw new InvalidBootstrapperApplicationException();
            }

            ba.Engine = new Engine(pEngine);
            ba.Command = command;
            return ba;
        }

        /// <summary>
        /// Locates the <see cref="BootstrapperApplicationAttribute"/> and returns the specified type.
        /// </summary>
        /// <param name="assemblyName">The assembly that defines the user experience class.</param>
        /// <returns>The bootstrapper application <see cref="Type"/>.</returns>
        private static Type GetBootstrapperApplicationTypeFromAssembly(string assemblyName)
        {
            Type baType = null;

            // Load the requested assembly.
            Assembly asm = AppDomain.CurrentDomain.Load(assemblyName);

            // If an assembly was loaded and is not the current assembly, check for the required attribute.
            // This is done to avoid using the BootstrapperApplicationAttribute which we use at build time
            // to specify the BootstrapperApplication assembly in the manifest. This attribute is for custom
            // BootstrapperApplication assemblies.
            if (!Assembly.GetExecutingAssembly().Equals(asm))
            {
                // There must be one and only one BootstrapperApplicationAttribute. The attribute prevents multiple declarations already.
                BootstrapperApplicationAttribute[] attrs = (BootstrapperApplicationAttribute[])asm.GetCustomAttributes(typeof(BootstrapperApplicationAttribute), false);
                if (null != attrs)
                {
                    baType = attrs[0].BootstrapperApplicationType;
                }
            }

            if (null == baType)
            {
                throw new MissingAttributeException();
            }

            return baType;
        }
    }
}
