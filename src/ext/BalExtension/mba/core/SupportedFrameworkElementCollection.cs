//-------------------------------------------------------------------------------------------------
// <copyright file="SupportedFrameworkElementCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Handler for the collection of supportedFramework configuration section.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Handler for the supportedFramework collection.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1010:CollectionsShouldImplementGenericInterface")]
    [ConfigurationCollection(typeof(SupportedFrameworkElement), AddItemName = "supportedFramework", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public sealed class SupportedFrameworkElementCollection : ConfigurationElementCollection
    {
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "supportedFramework"; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new SupportedFrameworkElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as SupportedFrameworkElement).Version;
        }
    }
}
