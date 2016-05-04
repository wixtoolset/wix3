// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;

    /// <summary>
    /// Identifies the bootstrapper application class.
    /// </summary>
    /// <remarks>
    /// This required assembly attribute identifies the bootstrapper application class.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class BootstrapperApplicationAttribute : Attribute
    {
        private Type bootstrapperApplicationType;

        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperApplicationAttribute"/> class.
        /// </summary>
        /// <param name="bootstrapperApplicationType">The <see cref="Type"/> of the user experience, or null for the default user experience.</param>
        public BootstrapperApplicationAttribute(Type bootstrapperApplicationType)
        {
            this.bootstrapperApplicationType = bootstrapperApplicationType;
        }

        /// <summary>
        /// Gets the type of the bootstrapper application class to create.
        /// </summary>
        public Type BootstrapperApplicationType
        {
            get { return this.bootstrapperApplicationType; }
        }
    }
}
