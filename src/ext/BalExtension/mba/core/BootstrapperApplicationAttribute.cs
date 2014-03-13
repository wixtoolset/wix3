//-------------------------------------------------------------------------------------------------
// <copyright file="BootstrapperApplicationAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Identifies the bootstrapper application class.
// </summary>
//-------------------------------------------------------------------------------------------------

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
