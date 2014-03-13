//-------------------------------------------------------------------------------------------------
// <copyright file="DependencyDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML toolset dependency extension decompiler.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Dependency;
    
    using Dependency = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Dependency;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML toolset dependency extension.
    /// </summary>
    public sealed class DependencyDecompiler : DecompilerExtension
    {
        private RegistryKeyValueCollection registryValues;
        private Dictionary<string, string> keyCache;

        /// <summary>
        /// Creates a new instance of the <see cref="DependencyDecompiler"/> class.
        /// </summary>
        public DependencyDecompiler()
        {
            this.registryValues = new RegistryKeyValueCollection();
            this.keyCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixDependencyProvider":
                    this.DecompileWixDependencyProviderTable(table);
                    break;

                case "WixDependency":
                    this.DecompileWixDependencyTable(table);
                    break;

                case "WixDependencyRef":
                    this.DecompileWixDependencyRefTable(table);
                    break;

                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Finalize decompilation by removing registry values that the compiler writes.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void FinalizeDecompile(TableCollection tables)
        {
            // Remove generated registry rows.
            this.FinalizeRegistryTable(tables);

            // Remove extension properties.
            this.FinalizeProperties();
        }

        /// <summary>
        /// Decompiles the WixDependencyProvider table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixDependencyProviderTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Provides provides = new Provides();

                provides.Id = (string)row[0];
                provides.Key = (string)row[2];

                if (null != row[3])
                {
                    provides.Version = (string)row[3];
                }

                if (null != row[4])
                {
                    provides.DisplayName = (string)row[4];
                }

                // Nothing to parse for attributes currently.

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[1]);
                if (null != component)
                {
                    component.AddChild(provides);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[1], "Component"));
                }

                // Index the provider to parent the RequiresRef elements.
                this.Core.IndexElement(row, provides);

                // Add the provider-specific registry keys to be removed during finalization.
                // Only remove specific keys that the compiler writes.
                string keyProvides = String.Concat(DependencyCommon.RegistryRoot, provides.Key);

                this.registryValues.Add(keyProvides, null);
                this.registryValues.Add(keyProvides, "Version");
                this.registryValues.Add(keyProvides, "DisplayName");
                this.registryValues.Add(keyProvides, "Attributes");

                // Cache the provider key.
                this.keyCache[provides.Id] = provides.Key;
            }
        }

        /// <summary>
        /// Decompiles the WixDependency table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixDependencyTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Requires requires = new Requires();

                requires.Id = (string)row[0];
                requires.ProviderKey = (string)row[1];

                if (null != row[2])
                {
                    requires.Minimum = (string)row[2];
                }

                if (null != row[3])
                {
                    requires.Maximum = (string)row[3];
                }

                if (null != row[4])
                {
                    int attributes = (int)row[4];

                    if (0 != (attributes & DependencyCommon.RequiresAttributesMinVersionInclusive))
                    {
                        requires.IncludeMinimum = Dependency.YesNoType.yes;
                    }

                    if (0 != (attributes & DependencyCommon.RequiresAttributesMaxVersionInclusive))
                    {
                        requires.IncludeMaximum = Dependency.YesNoType.yes;
                    }
                }

                this.Core.RootElement.AddChild(requires);

                // Cache the requires key.
                this.keyCache[requires.Id] = requires.ProviderKey;
            }
        }

        /// <summary>
        /// Decompiles the WixDependencyRef table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixDependencyRefTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                RequiresRef requiresRef = new RequiresRef();

                requiresRef.Id = (string)row[1];

                Provides provides = (Provides)this.Core.GetIndexedElement("WixDependencyProvider", (string)row[0]);
                if (null != provides)
                {
                    provides.AddChild(requiresRef);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "WixDependencyProvider_", (string)row[0], "WixDependencyProvider"));
                }

                // Get the cached keys for the provider and dependency IDs and generate registry rows.
                string providesKey = null;
                string requiresKey = null;

                if (null != provides && this.keyCache.ContainsKey(provides.Id))
                {
                    providesKey = this.keyCache[provides.Id];
                }
                else
                {
                    this.Core.OnMessage(DependencyWarnings.ProvidesKeyNotFound(row.SourceLineNumbers, provides.Id));
                }

                if (this.keyCache.ContainsKey(requiresRef.Id))
                {
                    requiresKey = this.keyCache[requiresRef.Id];
                }
                else
                {
                    this.Core.OnMessage(DependencyWarnings.RequiresKeyNotFound(row.SourceLineNumbers, requiresRef.Id));
                }

                if (!this.Core.EncounteredError)
                {
                    // Add the dependency-specific registry keys to be removed during finalization.
                    // Only remove specific keys that the compiler writes.
                    string keyRequires = String.Format(@"{0}{1}\{2}\{3}", DependencyCommon.RegistryRoot, requiresKey, DependencyCommon.RegistryDependents, providesKey);

                    this.registryValues.Add(keyRequires, "*");
                    this.registryValues.Add(keyRequires, "MinVersion");
                    this.registryValues.Add(keyRequires, "MaxVersion");
                    this.registryValues.Add(keyRequires, "Attributes");
                }
            }
        }

        /// <summary>
        /// Removes rows from the Registry table that are generated by this extension.
        /// </summary>
        /// <param name="tables">The collection of tables.</param>
        private void FinalizeRegistryTable(TableCollection tables)
        {
            Table registryTable = tables["Registry"];
            if (null != registryTable)
            {
                foreach (Row registryRow in registryTable.Rows)
                {
                    // Check if the compiler writes this registry value; if so, it should be removed.
                    if (this.registryValues.Contains(registryRow))
                    {
                        Wix.ISchemaElement elem = this.Core.GetIndexedElement(registryRow);

                        // If the registry row was found, remove it from its parent.
                        if (null != elem && null != elem.ParentElement)
                        {
                            Wix.IParentElement elemParent = elem.ParentElement as Wix.IParentElement;
                            if (null != elemParent)
                            {
                                elemParent.RemoveChild(elem);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes properties defined by this extension.
        /// </summary>
        /// <param name="tables">The collection of tables.</param>
        private void FinalizeProperties()
        {
            string[] properties = new string[] { "DISABLEDEPENDENCYCHECK", "IGNOREDEPENDENCIES" };
            foreach (string property in properties)
            {
                Wix.Property elem = this.Core.GetIndexedElement("Property", property) as Wix.Property;
                if (null != elem)
                {
                    // If a value is defined, log a warning we're removing it.
                    if (!String.IsNullOrEmpty(elem.Value))
                    {
                        this.Core.OnMessage(DependencyWarnings.PropertyRemoved(elem.Id));
                    }

                    // If the property row was found, remove it from its parent.
                    if (null != elem.ParentElement)
                    {
                        Wix.IParentElement elemParent = elem.ParentElement as Wix.IParentElement;
                        if (null != elemParent)
                        {
                            elemParent.RemoveChild(elem);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Provides an O(1) lookup for registry key and value name pairs for use in the decompiler.
        /// </summary>
        private sealed class RegistryKeyValueCollection : KeyedCollection<int, KeyValuePair<string, string>>
        {
            /// <summary>
            /// Adds the registry key and value name pair to the collection if it doesn't already exist.
            /// </summary>
            /// <param name="key">The registry key to add.</param>
            /// <param name="name">The registry value name to add.</param>
            internal void Add(string key, string name)
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(key, name);
                if (!this.Contains(pair))
                {
                    this.Add(pair);
                }
            }

            /// <summary>
            /// Returns whether the collection contains the registry key and value name pair from the <see cref="Row"/>.
            /// </summary>
            /// <param name="row">The registry <see cref="Row"/> to search for.</param>
            /// <returns>True if the collection contains the registry key and value name pair from the <see cref="Row"/>; otherwise, false.</returns>
            internal bool Contains(Row row)
            {
                if (null == row)
                {
                    return false;
                }

                KeyValuePair<string, string> pair = new KeyValuePair<string, string>((string)row[2], (string)row[3]);
                return this.Contains(pair);
            }

            /// <summary>
            /// Return the hash code of the key and value pair concatenated with a colon as a delimiter.
            /// </summary>
            /// <param name="pair">The registry key and value name pair.</param>
            /// <returns></returns>
            protected override int GetKeyForItem(KeyValuePair<string, string> pair)
            {
                return String.Concat(pair.Key, ":", pair.Value).GetHashCode();
            }
        }
    }
}
