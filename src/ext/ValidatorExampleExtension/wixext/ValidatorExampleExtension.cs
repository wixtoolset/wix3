// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
