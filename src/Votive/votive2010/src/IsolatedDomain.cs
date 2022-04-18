// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Manages a separate AppDomain which is unloaded when the object is disposed.
    /// Objects can be loaded in an IsolatedDomain to do work with assemblies
    /// that need to be isolated or unloaded.
    /// </summary>
    internal class IsolatedDomain : IDisposable
    {
        /// <summary>
        /// AppDomain created for this IsolatedDomain that will be unloaded when the IsolatedDomain is disposed.
        /// </summary>
        private AppDomain appDomain;

        /// <summary>
        /// Creates a new isolated domain.
        /// </summary>
        public IsolatedDomain()
        {
            AppDomainSetup appDomainSetup = new AppDomainSetup();
            appDomainSetup.ApplicationBase = Path.GetDirectoryName(typeof(IsolatedDomain).Assembly.Location);
            this.appDomain = AppDomain.CreateDomain(typeof(IsolatedDomain).Name, null, appDomainSetup);
            AppDomain.CurrentDomain.AssemblyResolve += this.OnCurrentDomainAssemblyResolve;
        }

        /// <summary>
        /// Gets the AppDomain that will be unloaded when the IsolatedDomain is disposed.
        /// </summary>
        public AppDomain AppDomain
        {
            get { return this.appDomain; }
        }

#pragma warning disable 618
        /// <summary>
        /// Creates an instance of a type in the isolated domain.
        /// </summary>
        /// <typeparam name="T">Type of the isolated worker instance to create.</typeparam>
        /// <param name="args">Parameters to be passed to the constructor.</param>
        /// <returns>A transparent proxy to the instance, remoted across the AppDomain boundary.</returns>
        public T CreateInstance<T>(params object[] args) where T: MarshalByRefObject
        {
            return (T)this.AppDomain.CreateInstanceAndUnwrap(
                typeof(T).Assembly.FullName,
                typeof(T).FullName,
                false,
                BindingFlags.CreateInstance,
                null,
                args,
                CultureInfo.InvariantCulture,
                null,
                null);
        }

        /// <summary>
        /// Shuts down the isolated domain, unloading any assemblies it loaded.
        /// </summary>
        /// <remarks>Any proxy objects returned by CreateInstance become invalid
        /// after the IsolatedDomain is disposed.</remarks>
        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this.OnCurrentDomainAssemblyResolve;
            if (this.AppDomain != null)
            {
                AppDomain.Unload(this.AppDomain);
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Resolves the current assembly in the current domain when returning types across the appdomain boundary;
        /// necessary because the current domain does not include the assembly location in its application base.
        /// </summary>
        /// <param name="sender">Originating appdomain.</param>
        /// <param name="args">Resolve event arguments including the name of the assembly to resolve.</param>
        /// <returns>The current assembly if the name to be resolved matches, else null.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
        private Assembly OnCurrentDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name == typeof(IsolatedDomain).Assembly.FullName)
            {
                return Assembly.LoadFrom(typeof(IsolatedDomain).Assembly.Location);
            }

            return null;
        }
    }
}
