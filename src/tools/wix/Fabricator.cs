//-------------------------------------------------------------------------------------------------
// <copyright file="Fabricator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// An extension for the Windows Installer XML Toolset fabricator extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Reflection;

    /// <summary>
    /// An extension for the Windows Installer XML Toolset fabricator extension.
    /// </summary>
    public abstract class Fabricator
    {
        private FabricatorCore core;

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Message handler for fabricator.
        /// </summary>
        /// <value>Message handler.</value>
        public MessageEventHandler MessageHandler
        {
            get { return this.Message; }
        }

        /// <summary>
        /// Gets the string name of this extension.
        /// </summary>
        /// <value>The name of this extension.</value>
        public virtual string Title
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the string namespace of this extension.
        /// </summary>
        /// <value>The namespace of this extension.</value>
        public virtual string Namespace
        {
            get { return null; }
        }

        /// <summary>
        /// Gets or sets the core for the extension.
        /// </summary>
        /// <value>The fabricator core for the extension.</value>
        protected FabricatorCore Core
        {
            get { return this.core; }
            set { this.core = value; }
        }

        /// <summary>
        /// Loads a FabricatorExtension from a type description string.
        /// </summary>
        /// <param name="extension">The extension type description string.</param>
        /// <returns>The loaded FabricatorExtension.</returns>
        public static Fabricator Load(string extension)
        {
            Type extensionType;

            if (2 == extension.Split(',').Length)
            {
                extensionType = System.Type.GetType(extension);

                if (null == extensionType)
                {
                    throw new WixException(WixErrors.InvalidExtension(extension));
                }
            }
            else
            {
                try
                {
                    Assembly extensionAssembly = Assembly.Load(extension);

                    AssemblyDefaultFabricatorExtensionAttribute extensionAttribute = (AssemblyDefaultFabricatorExtensionAttribute)Attribute.GetCustomAttribute(extensionAssembly, typeof(AssemblyDefaultFabricatorExtensionAttribute));

                    extensionType = extensionAttribute.ExtensionType;
                }
                catch
                {
                    throw new WixException(WixErrors.InvalidExtension(extension));
                }
            }

            if (extensionType.IsSubclassOf(typeof(Fabricator)))
            {
                return Activator.CreateInstance(extensionType) as Fabricator;
            }
            else
            {
                throw new WixException(WixErrors.InvalidExtension(extension, extensionType.ToString(), typeof(Fabricator).ToString()));
            }
        }

        /// <summary>
        /// Builds the setup package and feed.
        /// </summary>
        /// <param name="outputFeed">Path to the file where feed will be generated.</param>
        /// <returns>True if fabrication was successful, false if anything goes wrong.</returns>
        public virtual bool Fabricate(string outputFeed)
        {
            return false;
        }

        /// <summary>
        /// Gets the UI Controls for this fabricator.
        /// </summary>
        /// <returns>Array of controls that make up the steps to feed the fabricator data.</returns>
        public virtual System.Windows.Forms.Control[] GetControls()
        {
            return null;
        }

        /// <summary>
        /// Loads the fabricator data from disk.
        /// </summary>
        /// <param name="path">Path to load the facbricator information from.</param>
        public virtual void Open(string path)
        {
        }

        /// <summary>
        /// Saves the package builder data to disk.
        /// </summary>
        /// <param name="path">Path to save the fabricator information.</param>
        public virtual void Save(string path)
        {
        }
    }
}
