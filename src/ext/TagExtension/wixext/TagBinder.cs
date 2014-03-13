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
        private string overallRegid;
        private RowDictionary<Row> swidRows = new RowDictionary<Row>();

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        public override void BundleFinalize(Output output)
        {
            this.overallRegid = null; // always reset overall regid on initialize.

            Table tagTable = output.Tables["WixBundleTag"];
            if (null != tagTable)
            {
                Table table = output.Tables["WixBundle"];
                WixBundleRow bundleInfo = (WixBundleRow)table.Rows[0];
                Version bundleVersion = TagBinder.CreateFourPartVersion(bundleInfo.Version);

                // Try to collect all the software id tags from all the child packages.
                IList<SoftwareTag> allTags = TagBinder.CollectPackageTags(output);

                foreach (Row tagRow in tagTable.Rows)
                {
                    string regid = (string)tagRow[1];
                    string name = (string)tagRow[2];
                    bool licensed = (null != tagRow[3] && 0 != (int)tagRow[3]);
                    string typeString = (string)tagRow[5];

                    TagType type = String.IsNullOrEmpty(typeString) ? TagType.Unknown : (TagType)Enum.Parse(typeof(TagType), typeString);
                    IList<SoftwareTag> containedTags = TagBinder.CalculateContainedTagsAndType(allTags, ref type);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        TagBinder.CreateTagFile(ms, regid, bundleInfo.BundleId.ToString("D").ToUpperInvariant(), bundleInfo.Name, bundleVersion, bundleInfo.Publisher, licensed, type, containedTags);
                        tagRow[4] = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
        }

        /// <summary>
        /// Called before database binding occurs.
        /// </summary>
        public override void DatabaseInitialize(Output output)
        {
            this.overallRegid = null; // always reset overall regid on initialize.

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
            SourceLineNumberCollection sourceLineNumbers = null;

            Table tagTable = output.Tables["WixProductTag"];
            if (null != tagTable)
            {
                string productCode = null;
                string productName = null;
                Version productVersion = null;
                string manufacturer = null;

                Table properties = output.Tables["Property"];
                foreach (Row property in properties.Rows)
                {
                    switch ((string)property[0])
                    {
                        case "ProductCode":
                            productCode = (string)property[1];
                            break;
                        case "ProductName":
                            productName = (string)property[1];
                            break;
                        case "ProductVersion":
                            productVersion = TagBinder.CreateFourPartVersion((string)property[1]);
                            break;
                        case "Manufacturer":
                            manufacturer = (string)property[1];
                            break;
                    }
                }

                // If the ProductCode is available, only keep it if it is a GUID.
                if (!String.IsNullOrEmpty(productCode))
                {
                    try
                    {
                        Guid guid = new Guid(productCode);
                        productCode = guid.ToString("D").ToUpperInvariant();
                    }
                    catch // not a GUID, erase it.
                    {
                        productCode = null;
                    }
                }

                Table wixFileTable = output.Tables["WixFile"];
                foreach (Row tagRow in tagTable.Rows)
                {
                    string fileId = (string)tagRow[0];
                    string regid = (string)tagRow[1];
                    string name = (string)tagRow[2];
                    bool licensed = (null != tagRow[3] && 1 == (int)tagRow[3]);
                    string typeString = (string)tagRow[4];

                    TagType type = String.IsNullOrEmpty(typeString) ? TagType.Application : (TagType)Enum.Parse(typeof(TagType), typeString);
                    string uniqueId = String.IsNullOrEmpty(productCode) ? name.Replace(" ", "-") : productCode;

                    if (String.IsNullOrEmpty(this.overallRegid))
                    {
                        this.overallRegid = regid;
                        sourceLineNumbers = tagRow.SourceLineNumbers;
                    }
                    else if (!this.overallRegid.Equals(regid, StringComparison.Ordinal))
                    {
                        // TODO: display error that only one regid supported.
                    }

                    // Find the WixFileRow that matches for this WixProductTag.
                    foreach (WixFileRow wixFileRow in wixFileTable.Rows)
                    {
                        if (fileId == wixFileRow.File)
                        {
                            // Write the tag file.
                            wixFileRow.Source = Path.GetTempFileName();
                            using (FileStream fs = new FileStream(wixFileRow.Source, FileMode.Create))
                            {
                                TagBinder.CreateTagFile(fs, regid, uniqueId, productName, productVersion, manufacturer, licensed, type, null);
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
                                swidRow[1] = this.overallRegid;

                                this.swidRows.Add(swidRow);
                            }

                            // Always rewrite.
                            swidRow[2] = uniqueId;
                            swidRow[3] = type.ToString();
                        }
                    }
                }
            }

            // If we remembered the source line number for the regid, then add
            // a WixVariable to map to the regid.
            if (null != sourceLineNumbers)
            {
                Table wixVariableTable = output.Tables.EnsureTable(output.EntrySection, this.Core.TableDefinitions["WixVariable"]);
                WixVariableRow wixVariableRow = (WixVariableRow)wixVariableTable.CreateRow(sourceLineNumbers);
                wixVariableRow.Id = "WixTagRegid";
                wixVariableRow.Value = this.overallRegid;
                wixVariableRow.Overridable = false;
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
                                using (View view = db.OpenView("SELECT `Regid`, `UniqueId`, `Type` FROM `SoftwareIdentificationTag`"))
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

                                            TagType type = String.IsNullOrEmpty(record.GetString(3)) ? TagType.Unknown : (TagType)Enum.Parse(typeof(TagType), record.GetString(3));
                                            tags.Add(new SoftwareTag() { Regid = record.GetString(1), Id = record.GetString(2), Type = type });
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

        private static IList<SoftwareTag> CalculateContainedTagsAndType(IEnumerable<SoftwareTag> allTags, ref TagType type)
        {
            List<SoftwareTag> containedTags = new List<SoftwareTag>();
            foreach (SoftwareTag tag in allTags)
            {
                // If this tag type is an Application or Group then try to coerce our type to a Group.
                if (TagType.Application == tag.Type || TagType.Group == tag.Type)
                {
                    // If the type is still unknown, change our tag type and clear any contained tags that might have already
                    // been colllected.
                    if (TagType.Unknown == type)
                    {
                        type = TagType.Group;
                        containedTags = new List<SoftwareTag>();
                    }

                    // If we are a Group then we can add this as a contained tag, otherwise skip it.
                    if (TagType.Group == type)
                    {
                        containedTags.Add(tag);
                    }

                    // TODO: should we warn if this bundle is typed as a non-Group software id tag but is actually
                    // carrying Application or Group software tags?
                }
                else if (TagType.Component == tag.Type || TagType.Feature == tag.Type)
                {
                    // We collect Component and Feature tags only if the our tag is an Application or might still default to an Application.
                    if (TagType.Application == type || TagType.Unknown == type)
                    {
                        containedTags.Add(tag);
                    }
                }
            }

            // If our type was not set by now, we'll default to an Application.
            if (TagType.Unknown == type)
            {
                type = TagType.Application;
            }

            return containedTags;
        }

        private static void CreateTagFile(Stream stream, string regid, string uniqueId, string name, Version version, string manufacturer, bool licensed, TagType tagType, IList<SoftwareTag> containedTags)
        {
            using (XmlTextWriter writer = new XmlTextWriter(stream, Encoding.UTF8))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("software_identification_tag", "http://standards.iso.org/iso/19770/-2/2009/schema.xsd");
                writer.WriteElementString("entitlement_required_indicator", licensed ? "true" : "false");

                writer.WriteElementString("product_title", name);

                writer.WriteStartElement("product_version");
                writer.WriteElementString("name", version.ToString());
                writer.WriteStartElement("numeric");
                writer.WriteElementString("major", version.Major.ToString());
                writer.WriteElementString("minor", version.Minor.ToString());
                writer.WriteElementString("build", version.Build.ToString());
                writer.WriteElementString("review", version.Revision.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();

                writer.WriteStartElement("software_creator");
                writer.WriteElementString("name", manufacturer);
                writer.WriteElementString("regid", regid);
                writer.WriteEndElement();

                if (licensed)
                {
                    writer.WriteStartElement("software_licensor");
                    writer.WriteElementString("name", manufacturer);
                    writer.WriteElementString("regid", regid);
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("software_id");
                writer.WriteElementString("unique_id", uniqueId);
                writer.WriteElementString("tag_creator_regid", regid);
                writer.WriteEndElement();

                writer.WriteStartElement("tag_creator");
                writer.WriteElementString("name", manufacturer);
                writer.WriteElementString("regid", regid);
                writer.WriteEndElement();

                if (null != containedTags && 0 < containedTags.Count)
                {
                    writer.WriteStartElement("complex_of");
                    foreach (SoftwareTag tag in containedTags)
                    {
                        writer.WriteStartElement("software_id");
                        writer.WriteElementString("unique_id", tag.Id);
                        writer.WriteElementString("tag_creator_regid", tag.Regid);
                        writer.WriteEndElement(); // </software_id>
                    }
                    writer.WriteEndElement(); // </complex_of>
                }

                if (TagType.Unknown != tagType)
                {
                    writer.WriteStartElement("extended_information");
                    writer.WriteStartElement("tag_type", "http://www.tagvault.org/tv_extensions.xsd");
                    writer.WriteValue(tagType.ToString());
                    writer.WriteEndElement(); // </tag_type>
                    writer.WriteEndElement(); // </extended_information>
                }

                writer.WriteEndElement(); // </software_identification_tag>
            }
        }

        private enum TagType
        {
            Unknown,
            Application,
            Component,
            Feature,
            Group,
            Patch,
        }

        private class SoftwareTag
        {
            public string Regid { get; set; }
            public string Id { get; set; }
            public TagType Type { get; set; }
        }
    }
}
