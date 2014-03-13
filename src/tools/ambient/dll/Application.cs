//-------------------------------------------------------------------------------------------------
// <copyright file="Application.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Application model for WiX.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ApplicationModel
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Base class for application. Contains a WiX root element.
    /// </summary>
    public class Application
    {
        private Wix wixRoot;
        private Product product;
        private Package package;

        /// <summary>
        /// Creates a new Application object.
        /// </summary>
        public Application()
        {
            this.wixRoot = new Wix();
            this.product = new Product();
            this.wixRoot.AddChild(this.product);
            this.package = new Package();
            this.package.Id = "????????-????-????-????-????????????";
            this.package.Compressed = Microsoft.Tools.WindowsInstallerXml.Serialize.YesNoType.yes;
            this.package.InstallerVersion = 200;
            this.product.AddChild(this.package);
        }

        /// <summary>
        /// Gets or sets the Description for this Application.
        /// </summary>
        /// <value>The Description for this Application.</value>
        public string Description
        {
            get { return this.package.Description; }
            set { this.package.Description = value; }
        }

        /// <summary>
        /// Gets or sets the Manufacturer for this Application.
        /// </summary>
        /// <value>The Manufacturer for this Application.</value>
        public string Manufacturer
        {
            get { return this.package.Manufacturer; }
            set { this.package.Manufacturer = value; }
        }

        /// <summary>
        /// Gets or sets the Name for this Application.
        /// </summary>
        /// <value>The Name for this Application.</value>
        public string Name
        {
            get { return this.product.Name; }
            set { this.product.Name = value; }
        }

        /// <summary>
        /// Gets or sets the UpgradeCode for this Application.
        /// </summary>
        /// <value>The UpgradeCode for this Application.</value>
        public string UpgradeCode
        {
            get { return this.product.UpgradeCode; }
            set { this.product.UpgradeCode = value; }
        }

        /// <summary>
        /// Gets or sets the Version for this Application.
        /// </summary>
        /// <value>The Version for this Application.</value>
        public string Version
        {
            get { return this.product.Version; }
            set { this.product.Version = value; }
        }

        /// <summary>
        /// Gets the root serialized WiX element.
        /// </summary>
        /// <value>The root serialized WiX element.</value>
        public Wix WixRoot
        {
            get { return this.wixRoot; }
        }

        /// <summary>
        /// Gets the product element.
        /// </summary>
        /// <value>The product element.</value>
        public Product Product
        {
            get { return this.product; }
        }

        /// <summary>
        /// Gets the package element.
        /// </summary>
        /// <value>The package element.</value>
        public Package Package
        {
            get { return this.package; }
        }

        /// <summary>
        /// Adds resources representing the filesystem subtree from path on down. Path is an 
        /// absolute filesystem path.
        /// </summary>
        /// <param name="path">Path from which to start scraping.</param>
        /// <returns>The scraped directory.</returns>
        public static Serialize.Directory ScrapeFileSystem(string path)
        {
            return FileSystemScraper.ScrapeFileSystem(path);
        }

        /// <summary>
        /// Serializes this application out to WiX.
        /// </summary>
        /// <param name="filePath">Path to the output file.</param>
        public void Serialize(string filePath)
        {
            XmlTextWriter writer = new XmlTextWriter(filePath, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            this.wixRoot.OutputXml(writer);
            writer.Close();
        }
    }
}