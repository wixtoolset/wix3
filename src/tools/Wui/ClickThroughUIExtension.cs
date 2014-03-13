//-------------------------------------------------------------------------------------------------
// <copyright file="ClickThroughUIExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// ClickThrough UI extension.
// </summary>
//-------------------------------------------------------------------------------------------------
namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Windows.Forms;
    using System.Reflection;

    /// <summary>
    /// UI ClickThrough Extension.
    /// </summary>
    public abstract class ClickThroughUIExtension
    {
        /// <summary>
        /// Gets the fabricator for the extension.
        /// </summary>
        /// <value>The fabricator for the extension.</value>
        public abstract Fabricator Fabricator
        {
            get;
        }

        /// <summary>
        /// Loads a ClickThroughUIExtension from a type description string.
        /// </summary>
        /// <param name="extension">The extension type description string.</param>
        /// <returns>The loaded ClickThroughUIExtension.</returns>
        public static ClickThroughUIExtension Load(string extension)
        {
            Type extensionType;

            if (null == extension)
            {
                throw new ArgumentNullException("extension");
            }

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

                    AssemblyDefaultClickThroughUIAttribute extensionAttribute = (AssemblyDefaultClickThroughUIAttribute)Attribute.GetCustomAttribute(extensionAssembly, typeof(AssemblyDefaultClickThroughUIAttribute));

                    extensionType = extensionAttribute.ExtensionType;
                }
                catch
                {
                    throw new WixException(WixErrors.InvalidExtension(extension));
                }
            }

            if (extensionType.IsSubclassOf(typeof(ClickThroughUIExtension)))
            {
                return Activator.CreateInstance(extensionType) as ClickThroughUIExtension;
            }
            else
            {
                throw new WixException(WixErrors.InvalidExtension(extension, extensionType.ToString(), typeof(ClickThroughUIExtension).ToString()));
            }
        }

        /// <summary>
        /// Gets the UI Controls for this fabricator.
        /// </summary>
        /// <returns>Array of controls that make up the steps to feed the fabricator data.</returns>
        public virtual Control[] GetControls()
        {
            return null;
        }
    }
}
