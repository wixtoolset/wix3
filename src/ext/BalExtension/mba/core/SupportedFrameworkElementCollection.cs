// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
