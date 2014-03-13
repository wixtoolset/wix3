//-------------------------------------------------------------------------------------------------
// <copyright file="ValidatorExampleExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset Validator Example Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The Windows Installer XML Toolset Validator Example Extension.
    /// </summary>
    public sealed class ValidatorExampleExtension : WixExtension
    {
        private InspectorExtension inspectorExtension;
        private ValidatorExtension validatorExtension;

        /// <summary>
        /// Gets the optional inspector extension.
        /// </summary>
        /// <value>The optional inspector extension.</value>
        public override InspectorExtension InspectorExtension
        {
            get
            {
                if (null == this.inspectorExtension)
                {
                    this.inspectorExtension = new ExampleInspectorExtension();
                }

                return this.inspectorExtension;
            }
        }

        /// <summary>
        /// Gets the optional validator extension.
        /// </summary>
        /// <value>The optional validator extension.</value>
        public override ValidatorExtension ValidatorExtension
        {
            get
            {
                if (null == this.validatorExtension)
                {
                    this.validatorExtension = new ValidatorXmlExtension();
                }

                return this.validatorExtension;
            }
        }
    }
}
