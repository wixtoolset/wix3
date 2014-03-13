//-------------------------------------------------------------------------------------------------
// <copyright file="ProvidesDependency.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Represents an authored or imported dependency provider.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Xml;

    /// <summary>
    /// Represents an authored or imported dependency provider.
    /// </summary>
    internal sealed class ProvidesDependency
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ProviderDependency"/> class from a <see cref="Row"/>.
        /// </summary>
        /// <param name="row">The <see cref="Row"/> from which data is imported.</param>
        internal ProvidesDependency(Row row)
            : this((string)row[2], (string)row[3], (string)row[4], (int?)row[5])
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProviderDependency"/> class.
        /// </summary>
        /// <param name="key">The unique key of the dependency.</param>
        /// <param name="attributes">Additional attributes for the dependency.</param>
        internal ProvidesDependency(string key, string version, string displayName, int? attributes)
        {
            this.Key = key;
            this.Version = version;
            this.DisplayName = displayName;
            this.Attributes = attributes;
        }

        /// <summary>
        /// Gets or sets the unique key of the package provider.
        /// </summary>
        internal string Key { get; set; }

        /// <summary>
        /// Gets or sets the version of the package provider.
        /// </summary>
        internal string Version { get; set; }

        /// <summary>
        /// Gets or sets the display name of the package provider.
        /// </summary>
        internal string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the attributes for the dependency.
        /// </summary>
        internal int? Attributes { get; set; }

        /// <summary>
        /// Gets or sets whether the dependency was imported from the package.
        /// </summary>
        internal bool Imported { get; set; }

        /// <summary>
        /// Gets whether certain properties are the same.
        /// </summary>
        /// <param name="other">Another <see cref="ProvidesDependency"/> to compare.</param>
        /// <remarks>This is not the same as object equality, but only checks a subset of properties
        /// to determine if the objects are similar and could be merged into a collection.</remarks>
        /// <returns>True if certain properties are the same.</returns>
        internal bool Equals(ProvidesDependency other)
        {
            if (null != other)
            {
                return this.Key == other.Key &&
                       this.Version == other.Version &&
                       this.DisplayName == other.DisplayName;
            }

            return false;
        }

        /// <summary>
        /// Writes the dependency to the bundle XML manifest.
        /// </summary>
        /// <param name="writer">The <see cref="XmlTextWriter"/> for the bundle XML manifest.</param>
        internal void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("Provides");
            writer.WriteAttributeString("Key", this.Key);

            if (!String.IsNullOrEmpty(this.Version))
            {
                writer.WriteAttributeString("Version", this.Version);
            }

            if (!String.IsNullOrEmpty(this.DisplayName))
            {
                writer.WriteAttributeString("DisplayName", this.DisplayName);
            }

            if (this.Imported)
            {
                // The package dependency was explicitly authored into the manifest.
                writer.WriteAttributeString("Imported", "yes");
            }

            writer.WriteEndElement();
        }
    }
}
