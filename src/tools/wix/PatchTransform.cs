// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

    public class PatchTransform : IMessageHandler
    {
        private string baseline;
        private Output transform;
        private string transformPath;

        public string Baseline
        {
            get { return this.baseline; }
        }

        public Output Transform
        {
            get
            {
                if (null == this.transform)
                {
                    this.transform = Output.Load(this.transformPath, false, false); ;
                }

                return this.transform;
            }
        }

        public string TransformPath
        {
            get { return this.transformPath; }
        }

        public PatchTransform(string transformPath, string baseline)
        {
            this.transformPath = transformPath;
            this.baseline = baseline;
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Validates that the differences in the transform are valid for patch transforms.
        /// </summary>
        public void Validate()
        {
            // Changing the ProdocutCode in a patch transform is not recommended.
            Table propertyTable = this.Transform.Tables["Property"];
            if (null != propertyTable)
            {
                foreach (Row row in propertyTable.Rows)
                {
                    // Only interested in modified rows; fast check.
                    if (RowOperation.Modify == row.Operation)
                    {
                        if (0 == String.CompareOrdinal("ProductCode", (string)row[0]))
                        {
                            this.OnMessage(WixWarnings.MajorUpgradePatchNotRecommended());
                        }
                    }
                }
            }

            // If there is nothing in the component table we can return early because the remaining checks are component based.
            Table componentTable = this.Transform.Tables["Component"];
            if (null == componentTable)
            {
                return;
            }

            // Index Feature table row operations
            Table featureTable = this.Transform.Tables["Feature"];
            Table featureComponentsTable = this.Transform.Tables["FeatureComponents"];
            Hashtable featureOps = null;
            if (null != featureTable)
            {
                int capacity = featureTable.Rows.Count;
                featureOps = new Hashtable(capacity);

                foreach (Row row in featureTable.Rows)
                {
                    featureOps[(string)row[0]] = row.Operation;
                }
            }
            else
            {
                featureOps = new Hashtable();
            }

            // Index Component table and check for keypath modifications
            Hashtable deletedComponent = new Hashtable();
            Hashtable componentKeyPath = new Hashtable();
            foreach (Row row in componentTable.Rows)
            {
                string id = row.Fields[0].Data.ToString();
                string keypath = (null == row.Fields[5].Data) ? String.Empty : row.Fields[5].Data.ToString();

                componentKeyPath.Add(id, keypath);
                if (RowOperation.Delete == row.Operation)
                {
                    deletedComponent.Add(id, row);
                }
                else if (RowOperation.Modify == row.Operation)
                {
                    if (row.Fields[1].Modified)
                    {
                        // Changing the guid of a component is equal to deleting the old one and adding a new one.
                        deletedComponent.Add(id, row);
                    }

                    // If the keypath is modified its an error
                    if (row.Fields[5].Modified)
                    {
                        this.OnMessage(WixErrors.InvalidKeypathChange(row.SourceLineNumbers, id, this.transformPath));
                    }
                }
            }

            // Verify changes in the file table
            Table fileTable = this.Transform.Tables["File"];
            if (null != fileTable)
            {
                Hashtable componentWithChangedKeyPath = new Hashtable();
                foreach (Row row in fileTable.Rows)
                {
                    if (RowOperation.None != row.Operation)
                    {
                        string fileId = row.Fields[0].Data.ToString();
                        string componentId = row.Fields[1].Data.ToString();

                        // If this file is the keypath of a component
                        if (String.Equals((string)componentKeyPath[componentId], fileId, StringComparison.Ordinal))
                        {
                            if (row.Fields[2].Modified)
                            {
                                // You cant change the filename of a file that is the keypath of a component.
                                this.OnMessage(WixErrors.InvalidKeypathChange(row.SourceLineNumbers, componentId, this.transformPath));
                            }

                            if (!componentWithChangedKeyPath.ContainsKey(componentId))
                            {
                                componentWithChangedKeyPath.Add(componentId, fileId);
                            }
                        }

                        if (RowOperation.Delete == row.Operation)
                        {
                            // If the file is removed from a component that is not deleted.
                            if (!deletedComponent.ContainsKey(componentId))
                            {
                                bool foundRemoveFileEntry = false;
                                string filename = Msi.Installer.GetName((string)row[2], false, true);

                                Table removeFileTable = this.Transform.Tables["RemoveFile"];
                                if (null != removeFileTable)
                                {
                                    foreach (Row removeFileRow in removeFileTable.Rows)
                                    {
                                        if (RowOperation.Delete == removeFileRow.Operation)
                                        {
                                            continue;
                                        }

                                        if (componentId == (string)removeFileRow[1])
                                        {
                                            // Check if there is a RemoveFile entry for this file
                                            if (null != removeFileRow[2])
                                            {
                                                string removeFileName = Msi.Installer.GetName((string)removeFileRow[2], false, true);

                                                // Convert the MSI format for a wildcard string to Regex format.
                                                removeFileName = removeFileName.Replace('.', '|').Replace('?', '.').Replace("*", ".*").Replace("|", "\\.");

                                                Regex regex = new Regex(removeFileName, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                                                if (regex.IsMatch(filename))
                                                {
                                                    foundRemoveFileEntry = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!foundRemoveFileEntry)
                                {
                                    this.OnMessage(WixWarnings.InvalidRemoveFile(row.SourceLineNumbers, fileId, componentId));
                                }
                            }
                        }
                    }
                }
            }

            if (0 < deletedComponent.Count)
            {
                // Index FeatureComponents table.
                Hashtable featureComponents = new Hashtable();

                if (null != featureComponentsTable)
                {
                    foreach (Row row in featureComponentsTable.Rows)
                    {
                        ArrayList features;
                        string componentId = row.Fields[1].Data.ToString();

                        if (featureComponents.Contains(componentId))
                        {
                            features = (ArrayList)featureComponents[componentId];
                        }
                        else
                        {
                            features = new ArrayList();
                            featureComponents.Add(componentId, features);
                        }
                        features.Add(row.Fields[0].Data.ToString());
                    }
                }

                // Check to make sure if a component was deleted, the feature was too.
                foreach (DictionaryEntry entry in deletedComponent)
                {
                    if (featureComponents.Contains(entry.Key.ToString()))
                    {
                        ArrayList features = (ArrayList)featureComponents[entry.Key.ToString()];
                        foreach (string featureId in features)
                        {
                            if (!featureOps.ContainsKey(featureId) || RowOperation.Delete != (RowOperation)featureOps[featureId])
                            {
                                // The feature was not deleted.
                                this.OnMessage(WixErrors.InvalidRemoveComponent(((Row)entry.Value).SourceLineNumbers, entry.Key.ToString(), featureId, this.transformPath));
                            }
                        }
                    }
                }
            }

            // Warn if new components are added to existing features
            if (null != featureComponentsTable)
            {
                foreach (Row row in featureComponentsTable.Rows)
                {
                    if (RowOperation.Add == row.Operation)
                    {
                        // Check if the feature is in the Feature table
                        string feature_ = (string)row[0];
                        string component_ = (string)row[1];

                        // Features may not be present if not referenced
                        if (!featureOps.ContainsKey(feature_) || RowOperation.Add != (RowOperation)featureOps[feature_])
                        {
                            this.OnMessage(WixWarnings.NewComponentAddedToExistingFeature(row.SourceLineNumbers, component_, feature_, this.transformPath));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, e);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
