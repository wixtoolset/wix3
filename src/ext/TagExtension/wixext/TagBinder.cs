//-------------------------------------------------------------------------------------------------
// <copyright file="TagBinder.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Binder for the Windows Installer XML Toolset Software Id Tag Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The Binder for the Windows Installer XML Toolset Software Id Tag Extension.
    /// </summary>
    public sealed class TagBinder : BinderExtensionEx
    {
        private RowDictionary<Row> swidRows = new RowDictionary<Row>();

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public override void BundleFinalize(Output output)
        {
            Table tagTable = output.Tables["WixBundleTag"];
            if (null != tagTable)
            {
                Table table = output.Tables["WixBundle"];
                WixBundleRow bundleInfo = (WixBundleRow)table.Rows[0];
                string bundleId = bundleInfo.BundleId.ToString("D").ToUpperInvariant();
                Version bundleVersion = TagBinder.CreateFourPartVersion(bundleInfo.Version);
                string upgradeCode = NormalizeGuid(bundleInfo.UpgradeCode);

                string uniqueId = String.Concat("wix:bundle/", bundleId);

                string persistentId = String.Concat("wix:bundle.upgrade/", upgradeCode);

                // Try to collect all the software id tags from all the child packages.
                IList<SoftwareTag> containedTags = TagBinder.CollectPackageTags(output);

                foreach (Row tagRow in tagTable.Rows)
                {
                    string regid = (string)tagRow[1];
                    string name = (string)tagRow[2];

                    using (MemoryStream ms = new MemoryStream())
                    {
                        TagBinder.CreateTagFile(ms, uniqueId, bundleInfo.Name, bundleVersion, regid, bundleInfo.Publisher, persistentId, containedTags);
                        tagRow[5] = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Called before database binding occurs.
        /// </summary>
        public override void DatabaseInitialize(Output output)
        {
            // Ensure the tag files are generated to be imported by the MSI.
            this.CreateProductTagFiles(output);
        }

        /// <summary>
        /// Called after database variable resolution occurs.
        /// </summary>
        public override void DatabaseAfterResolvedFields(Output output)
        {
            Table wixBindUpdateFilesTable = output.Tables["WixBindUpdatedFiles"];

            // We'll end up re-writing the tag files but this time we may have the ProductCode
            // now to use as the unique id.
            List<WixFileRow> updatedFileRows = this.CreateProductTagFiles(output);
            foreach (WixFileRow updateFileRow in updatedFileRows)
            {
                Row row = wixBindUpdateFilesTable.CreateRow(updateFileRow.SourceLineNumbers);
                row[0] = updateFileRow.File;
            }
        }

        private List<WixFileRow> CreateProductTagFiles(Output output)
        {
            List<WixFileRow> updatedFileRows = new List<WixFileRow>();

            Table tagTable = output.Tables["WixProductTag"];
            if (null != tagTable)
            {
                string packageCode = null;
                string productName = null;
                Version productVersion = null;
                string manufacturer = null;
                string upgradeCode = null;

                Table summaryInformationTable = output.Tables["_SummaryInformation"];
                foreach (Row summaryInformationRow in summaryInformationTable.Rows)
                {
                    // PID_REVNUMBER
                    if (9 == (int)summaryInformationRow[0])
                    {
                        packageCode = (string)summaryInformationRow[1];
                        break;
                    }
                }

                packageCode = NormalizeGuid(packageCode);

                Table properties = output.Tables["Property"];
                foreach (Row property in properties.Rows)
                {
                    switch ((string)property[0])
                    {
                        case "ProductName":
                            productName = (string)property[1];
                            break;
                        case "ProductVersion":
                            productVersion = TagBinder.CreateFourPartVersion((string)property[1]);
                            break;
                        case "Manufacturer":
                            manufacturer = (string)property[1];
                            break;
                        case "UpgradeCode":
                            upgradeCode = (string)property[1];
                            break;
                    }
                }

                upgradeCode = NormalizeGuid(upgradeCode);

                Table wixFileTable = output.Tables["WixFile"];
                foreach (Row tagRow in tagTable.Rows)
                {
                    string fileId = (string)tagRow[0];
                    string regid = (string)tagRow[1];
                    string name = (string)tagRow[2];

                    string uniqueId = String.Concat("msi:package/", packageCode);
                    string persistentId = String.IsNullOrEmpty(upgradeCode) ? null : String.Concat("msi:upgrade/", upgradeCode);

                    // Find the WixFileRow that matches for this WixProductTag.
                    foreach (WixFileRow wixFileRow in wixFileTable.Rows)
                    {
                        if (fileId == wixFileRow.File)
                        {
                            string source = this.Core.GetProperty<string>(BinderCore.IntermediateFolder);

                            if (String.IsNullOrEmpty(source))
                            {
                                source = Path.GetTempFileName();
                            }
                            else
                            {
                                source = Path.Combine(source, String.Concat(wixFileRow.File, ".swidtag"));
                            }

                            // Write the tag file.
                            wixFileRow.Source = source;
                            using (FileStream fs = new FileStream(wixFileRow.Source, FileMode.Create))
                            {
                                TagBinder.CreateTagFile(fs, uniqueId, productName, productVersion, regid, manufacturer, persistentId, null);
                            }

                            updatedFileRows.Add(wixFileRow); // remember that we modified this file.

                            // Ensure the matching "SoftwareIdentificationTag" row exists and
                            // is populated correctly.
                            Row swidRow;
                            if (!this.swidRows.TryGet(fileId, out swidRow))
                            {
                                Table swid = output.Tables["SoftwareIdentificationTag"];
                                swidRow = swid.CreateRow(wixFileRow.SourceLineNumbers);
                                swidRow[0] = fileId;
                                swidRow[1] = regid;

                                this.swidRows.Add(swidRow);
                            }

                            // Always rewrite.
                            swidRow[2] = uniqueId;
                            swidRow[3] = persistentId;
                        }
                    }
                }
            }

            return updatedFileRows;
        }

        private static Version CreateFourPartVersion(string versionString)
        {
            Version version = new Version(versionString);
            return new Version(version.Major,
                               -1 < version.Minor ? version.Minor : 0,
                               -1 < version.Build ? version.Build : 0,
                               -1 < version.Revision ? version.Revision : 0);
        }

        private static string NormalizeGuid(string guidString)
        {
            try
            {
                Guid guid = new Guid(guidString);
                return guid.ToString("D").ToUpperInvariant();
            }
            catch // not a GUID, erase it.
            {
                return null;
            }
        }

        private static IList<SoftwareTag> CollectPackageTags(Output bundle)
        {
            List<SoftwareTag> tags = new List<SoftwareTag>();
            Table packageTable = bundle.Tables["ChainPackageInfo"];
            if (null != packageTable)
            {
                Table payloadTable = bundle.Tables["PayloadInfo"];
                RowDictionary<PayloadInfoRow> payloads = new RowDictionary<PayloadInfoRow>(payloadTable);

                foreach (Row row in packageTable.Rows)
                {
                    Compiler.ChainPackageType packageType = (Compiler.ChainPackageType)Enum.Parse(typeof(Compiler.ChainPackageType), (string)row[1], true);
                    if (Compiler.ChainPackageType.Msi == packageType)
                    {
                        string packagePayloadId = (string)row[2];
                        PayloadInfoRow payload = (PayloadInfoRow)payloads[packagePayloadId];

                        using (Database db = new Database(payload.FullFileName))
                        {
                            if (db.Tables.Contains("SoftwareIdentificationTag"))
                            {
                                using (View view = db.OpenView("SELECT `TagId` FROM `SoftwareIdentificationTag`"))
                                {
                                    view.Execute();
                                    while (true)
                                    {
                                        using (Record record = view.Fetch())
                                        {
                                            if (null == record)
                                            {
                                                break;
                                            }

                                            tags.Add(new SoftwareTag() { Id = record.GetString(1), });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return tags;
        }

        private static void CreateTagFile(Stream stream, string uniqueId, string name, Version version, string regid, string manufacturer, string persistendId, IList<SoftwareTag> containedTags)
        {
            using (XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;

                writer.WriteStartDocument();
                writer.WriteStartElement("SoftwareIdentity", "http://standards.iso.org/iso/19770/-2/2015/schema.xsd");
                writer.WriteAttributeString("tagId", uniqueId);
                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("version", version.ToString());
                writer.WriteAttributeString("versionScheme", "multipartnumeric");

                writer.WriteStartElement("Entity");
                writer.WriteAttributeString("name", manufacturer);
                writer.WriteAttributeString("regid", regid);
                writer.WriteAttributeString("role", "softwareCreator tagCreator");
                writer.WriteEndElement(); // </Entity>

                if (!String.IsNullOrEmpty(persistendId))
                {
                    writer.WriteStartElement("Meta");
                    writer.WriteAttributeString("persistentId", persistendId);
                    writer.WriteEndElement(); // </Meta>
                }

                if (null != containedTags)
                {
                    foreach (SoftwareTag tag in containedTags)
                    {
                        writer.WriteStartElement("Link");
                        writer.WriteAttributeString("rel", "component");
                        writer.WriteAttributeString("href", String.Concat("swid:", tag.Id));
                        writer.WriteEndElement(); // </Link>
                    }
                }

                writer.WriteEndElement(); // </SoftwareIdentity>
            }
        }

        private class SoftwareTag
        {
            public string Id { get; set; }
        }
    }
}
