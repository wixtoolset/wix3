//-------------------------------------------------------------------------------------------------
// <copyright file="ConnectToModule.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Object that connects things to modules.
// </summary>
//-------------------------------------------------------------------------------------------------

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
