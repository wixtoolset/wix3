//-------------------------------------------------------------------------------------------------
// <copyright file="Mutator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset mutator.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The Windows Installer XML Toolset mutator.
    /// </summary>
    public sealed class Mutator
    {
        private HarvesterCore core;
        private SortedList extensions;
        private string extensionArgument;

        /// <summary>
        /// Instantiate a new mutator.
        /// </summary>
        public Mutator()
        {
            this.extensions = new SortedList();
        }

        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        public HarvesterCore Core
        {
            get { return this.core; }
            set { this.core = value; }
        }

        /// <summary>
        /// Gets or sets the value of the extension argument passed to heat.
        /// </summary>
        /// <value>The extension argument.</value>
        public string ExtensionArgument
        {
            get { return this.extensionArgument; }
            set { this.extensionArgument = value; }
        }

        /// <summary>
        /// Adds a mutator extension.
        /// </summary>
        /// <param name="mutatorExtension">The mutator extension to add.</param>
        public void AddExtension(MutatorExtension mutatorExtension)
        {
            this.extensions.Add(mutatorExtension.Sequence, mutatorExtension);
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        /// <returns>true if mutation was successful</returns>
        public bool Mutate(Wix.Wix wix)
        {
            bool encounteredError = false;
            
            try
            {
                foreach (MutatorExtension mutatorExtension in this.extensions.Values)
                {
                    if (null == mutatorExtension.Core)
                    {
                        mutatorExtension.Core = this.core;
                    }

                    mutatorExtension.Mutate(wix);
                }
            }
            finally
            {
                encounteredError = this.core.EncounteredError;
            }

            // return the Wix document element only if mutation completed successfully
            return !encounteredError;
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wixString">The Wix document as a string.</param>
        /// <returns>The mutated Wix document as a string if mutation was successful, else null.</returns>
        public string Mutate(string wixString)
        {
            bool encounteredError = false;

            try
            {
                foreach (MutatorExtension mutatorExtension in this.extensions.Values)
                {
                    if (null == mutatorExtension.Core)
                    {
                        mutatorExtension.Core = this.core;
                    }

                    wixString = mutatorExtension.Mutate(wixString);

                    if (String.IsNullOrEmpty(wixString) || this.core.EncounteredError)
                    {
                        break;
                    }
                }
            }
            finally
            {
                encounteredError = this.core.EncounteredError;
            }

            return encounteredError ? null : wixString;
        }
    }
}
