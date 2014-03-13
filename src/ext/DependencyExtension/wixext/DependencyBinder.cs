//-------------------------------------------------------------------------------------------------
// <copyright file="DependencyBinder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML toolset dependency extension binder.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The compiler for the Windows Installer XML toolset dependency extension.
    /// </summary>
    public sealed class DependencyBinder : BinderExtension
    {
        private Output output = null;

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public override void DatabaseFinalize(Output output)
        {
            this.output = output;

            Table wixDependencyTable = output.Tables["WixDependency"];
            Table wixDependencyProviderTable = output.Tables["WixDependencyProvider"];
            Table wixDependencyRefTable = output.Tables["WixDependencyRef"];

            // Make sure there's something to do.
            if (null != wixDependencyRefTable)
            {
                KeyedRowCollection wixDependencyRows = new KeyedRowCollection(wixDependencyTable);
                KeyedRowCollection wixDependencyProviderRows = new KeyedRowCollection(wixDependencyProviderTable);

                // For each relationship, get the provides and requires rows to generate registry values.
                foreach (Row wixDependencyRefRow in wixDependencyRefTable.Rows)
                {
                    string providesId = (string)wixDependencyRefRow[0];
                    string requiresId = (string)wixDependencyRefRow[1];

                    Row wixDependencyRow = null;
                    if (wixDependencyRows.Contains(requiresId))
                    {
                        wixDependencyRow = wixDependencyRows[requiresId];
                    }

                    Row wixDependencyProviderRow = null;
                    if (wixDependencyProviderRows.Contains(providesId))
                    {
                        wixDependencyProviderRow = wixDependencyProviderRows[providesId];
                    }

                    // If we found both rows, generate the registry values.
                    if (null != wixDependencyRow && null != wixDependencyProviderRow)
                    {
                        // Format the root registry key using the required provider key and the current provider key.
                        string requiresKey = (string)wixDependencyRow[1];
                        string providesKey = (string)wixDependencyProviderRow[2];
                        string keyRequires = String.Format(@"{0}{1}\{2}\{3}", DependencyCommon.RegistryRoot, requiresKey, DependencyCommon.RegistryDependents, providesKey);

                        // Get the component ID from the provider.
                        string componentId = (string)wixDependencyProviderRow[1];

                        Row row = this.CreateRegistryRow(wixDependencyRow);
                        row[0] = this.Core.GenerateIdentifier("reg", providesId, requiresId, "(Default)");
                        row[1] = -1;
                        row[2] = keyRequires;
                        row[3] = "*";
                        row[4] = null;
                        row[5] = componentId;

                        string minVersion = (string)wixDependencyRow[2];
                        if (!String.IsNullOrEmpty(minVersion))
                        {
                            row = this.CreateRegistryRow(wixDependencyRow);
                            row[0] = this.Core.GenerateIdentifier("reg", providesId, requiresId, "MinVersion");
                            row[1] = -1;
                            row[2] = keyRequires;
                            row[3] = "MinVersion";
                            row[4] = minVersion;
                            row[5] = componentId;
                        }

                        string maxVersion = (string)wixDependencyRow[3];
                        if (!String.IsNullOrEmpty(minVersion))
                        {
                            row = this.CreateRegistryRow(wixDependencyRow);
                            row[0] = this.Core.GenerateIdentifier("reg", providesId, requiresId, "MaxVersion");
                            row[1] = -1;
                            row[2] = keyRequires;
                            row[3] = "MaxVersion";
                            row[4] = maxVersion;
                            row[5] = componentId;
                        }

                        if (null != wixDependencyRow[4])
                        {
                            int attributes = (int)wixDependencyRow[4];

                            row = this.CreateRegistryRow(wixDependencyRow);
                            row[0] = this.Core.GenerateIdentifier("reg", providesId, requiresId, "Attributes");
                            row[1] = -1;
                            row[2] = keyRequires;
                            row[3] = "Attributes";
                            row[4] = String.Concat("#", attributes.ToString(CultureInfo.InvariantCulture.NumberFormat));
                            row[5] = componentId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a registry row using source information from the given <see cref="Row"/>.
        /// </summary>
        /// <param name="referenceRow">The <see cref="Row"/> from which the section and source line information are retrieved.</param>
        /// <returns>A new Registry row.</returns>
        private Row CreateRegistryRow(Row referenceRow)
        {
            TableDefinition tableDefinition = this.Core.TableDefinitions["Registry"];

            // Create the row from the main tables, which were populated during link anyway.
            // We still associate the table with the dependency row's section to maintain servicing.
            Table table = this.output.Tables.EnsureTable(referenceRow.Table.Section, tableDefinition);
            Row row = table.CreateRow(referenceRow.SourceLineNumbers);
            
            // Set the section ID for patching and return the new row.
            row.SectionId = referenceRow.SectionId;
            return row;
        }

        /// <summary>
        /// A keyed collection of <see cref="Row"/> instances for O(1) lookup.
        /// </summary>
        private sealed class KeyedRowCollection : KeyedCollection<string, Row>
        {
            /// <summary>
            /// Initializes the <see cref="KeyedRowCollection"/> class with all rows from the specified <paramref name="table"/>.
            /// </summary>
            /// <param name="table">The <see cref="Table"/> containing rows to index.</param>
            internal KeyedRowCollection(Table table)
            {
                if (null != table)
                {
                    foreach (Row row in table.Rows)
                    {
                        this.Add(row);
                    }
                }
            }

            /// <summary>
            /// Gets the primary key for the <see cref="Row"/>.
            /// </summary>
            /// <param name="row">The <see cref="Row"/> to index.</param>
            /// <returns>The primary key for the <see cref="Row"/>.</returns>
            protected override string GetKeyForItem(Row row)
            {
                return row.GetPrimaryKey('/');
            }
        }
    }
}
