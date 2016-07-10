// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Object that connects things to modules.
    /// </summary>
    public sealed class ConnectToModule
    {
        private string childId;
        private string module;
        private string moduleLanguage;

        /// <summary>
        /// Creates a new connect to module.
        /// </summary>
        /// <param name="childId">Id of the child.</param>
        /// <param name="module">Id of the module.</param>
        /// <param name="moduleLanguage">Language of the module.</param>
        public ConnectToModule(string childId, string module, string moduleLanguage)
        {
            this.childId = childId;
            this.module = module;
            this.moduleLanguage = moduleLanguage;
        }

        /// <summary>
        /// Gets the id of the child.
        /// </summary>
        /// <value>Child identifier.</value>
        public string ChildId
        {
            get { return this.childId; }
        }

        /// <summary>
        /// Gets the id of the module.
        /// </summary>
        /// <value>The id of the module.</value>
        public string Module
        {
            get { return this.module; }
        }

        /// <summary>
        /// Gets the language of the module.
        /// </summary>
        /// <value>The language of the module.</value>
        public string ModuleLanguage
        {
            get { return this.moduleLanguage; }
        }
    }
}
