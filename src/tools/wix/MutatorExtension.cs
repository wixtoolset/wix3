//-------------------------------------------------------------------------------------------------
// <copyright file="MutatorExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The base mutator extension.  Any of these methods can be overridden to change
// the behavior of the mutator.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The base mutator extension.  Any of these methods can be overridden to change
    /// the behavior of the mutator.
    /// </summary>
    public abstract class MutatorExtension
    {
        private HarvesterCore core;

        /// <summary>
        /// Gets or sets the mutator core for the extension.
        /// </summary>
        /// <value>The mutator core for the extension.</value>
        public HarvesterCore Core
        {
            get { return this.core; }
            set { this.core = value; }
        }

        /// <summary>
        /// Gets the sequence of the extension.
        /// </summary>
        /// <value>The sequence of the extension.</value>
        public abstract int Sequence
        {
            get;
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        public virtual void Mutate(Wix.Wix wix)
        {
        }

        /// <summary>
        /// Mutate a WiX document as a string.
        /// </summary>
        /// <param name="wix">The Wix document element as a string.</param>
        /// <returns>The mutated Wix document as a string.</returns>
        public virtual string Mutate(string wixString)
        {
            return wixString;
        }

        /// <summary>
        /// Generate unique MSI identifiers.
        /// </summary>
        protected class IdentifierGenerator
        {
            public const int MaxProductIdentifierLength = 72;
            public const int MaxModuleIdentifierLength = 35;

            private string baseName;
            private int maxLength;
            private Dictionary<string, object> existingIdentifiers;
            private Dictionary<string, object> possibleIdentifiers;

            /// <summary>
            /// Instantiate a new IdentifierGenerator.
            /// </summary>
            /// <param name="baseName">The base resource name to use if a resource name contains no usable characters.</param>
            public IdentifierGenerator(string baseName)
            {
                this.baseName = baseName;
                this.maxLength = IdentifierGenerator.MaxProductIdentifierLength;
                this.existingIdentifiers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                this.possibleIdentifiers = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Gets or sets the maximum length for generated identifiers.
            /// </summary>
            /// <value>Maximum length for generated identifiers. (Default is 72.)</value>
            public int MaxIdentifierLength
            {
                get { return this.maxLength; }
                set { this.maxLength = value; }
            }

            /// <summary>
            /// Index an existing identifier for collision detection.
            /// </summary>
            /// <param name="identifier">The identifier.</param>
            public void IndexExistingIdentifier(string identifier)
            {
                if (null == identifier)
                {
                    throw new ArgumentNullException("identifier");
                }

                this.existingIdentifiers[identifier] = null;
            }

            /// <summary>
            /// Index a resource name for collision detection.
            /// </summary>
            /// <param name="name">The resource name.</param>
            public void IndexName(string name)
            {
                if (null == name)
                {
                    throw new ArgumentNullException("name");
                }

                string identifier = this.CreateIdentifier(name, 0);

                if (this.possibleIdentifiers.ContainsKey(identifier))
                {
                    this.possibleIdentifiers[identifier] = String.Empty;
                }
                else
                {
                    this.possibleIdentifiers.Add(identifier, null);
                }
            }

            /// <summary>
            /// Get the identifier for the given resource name.
            /// </summary>
            /// <param name="name">The resource name.</param>
            /// <returns>A legal MSI identifier.</returns>
            [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
            public string GetIdentifier(string name)
            {
                if (null == name)
                {
                    throw new ArgumentNullException("name");
                }

                for (int i = 0; i <= Int32.MaxValue; i++)
                {
                    string identifier = this.CreateIdentifier(name, i);

                    if (this.existingIdentifiers.ContainsKey(identifier) || // already used
                        (0 == i && 0 != this.possibleIdentifiers.Count && null != this.possibleIdentifiers[identifier]) || // needs an index because its duplicated
                        (0 != i && this.possibleIdentifiers.ContainsKey(identifier))) // collides with another possible identifier
                    {
                        continue;
                    }
                    else // use this identifier
                    {
                        this.existingIdentifiers.Add(identifier, null);

                        return identifier;
                    }
                }

                throw new InvalidOperationException(WixStrings.EXP_CouldnotFileUniqueIDForResourceName);
            }

            /// <summary>
            /// Create a legal MSI identifier from a resource name and an index.
            /// </summary>
            /// <param name="name">The name of the resource for which an identifier should be created.</param>
            /// <param name="index">An index to append to the end of the identifier to make it unique.</param>
            /// <returns>A legal MSI identifier.</returns>
            public string CreateIdentifier(string name, int index)
            {
                if (null == name)
                {
                    throw new ArgumentNullException("name");
                }

                StringBuilder identifier = new StringBuilder();

                // Convert the name to a standard MSI identifier
                identifier.Append(Common.GetIdentifierFromName(name));

                // no legal identifier characters were found, use the base id instead
                if (0 == identifier.Length)
                {
                    identifier.Append(this.baseName);
                }

                // truncate the identifier if it's too long (reserve 3 characters for up to 99 collisions)
                int adjustedMaxLength = this.MaxIdentifierLength - (index != 0 ? 3 : 0);
                if (adjustedMaxLength < identifier.Length)
                {
                    identifier.Length = adjustedMaxLength;
                }

                // if the index is not zero, then append it to the identifier name
                if (0 != index)
                {
                    identifier.AppendFormat("_{0}", index);
                }

                return identifier.ToString();
            }
        }
    }
}
