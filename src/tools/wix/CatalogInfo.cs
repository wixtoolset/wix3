// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.IO;

    /// <summary>
    /// Catalog information for binding Bundles.
    /// </summary>
    internal class CatalogInfo
    {
        private WixCatalogRow wixCatalogRow;

        public CatalogInfo(WixCatalogRow wixCatalogRow, string payloadId)
        {
            this.wixCatalogRow = wixCatalogRow;
            this.PayloadId = payloadId;

            this.Initialize();            
        }

        private void Initialize()
        {
            FileInfo fileInfo = this.FileInfo;
            if (null == fileInfo)
            {
                //We need these statements because the old code created the FileInfo here,
                //which could have thrown an exception.
            }
        }

        /// <summary>
        /// Gets or sets the catalog identifier.
        /// </summary>
        /// <value>The catalog identifier.</value>
        public string Id
        {
            get { return this.wixCatalogRow.Id; }
            private set { this.wixCatalogRow.Id = value; }
        }

        /// <summary>
        /// Gets or sets the source file.
        /// </summary>
        /// <value>The source file.</value>
        public FileInfo FileInfo
        {
            get
            {
                return new FileInfo(this.wixCatalogRow.SourceFile);
            }
            private set
            {
                this.wixCatalogRow.SourceFile = value.FullName;
            }
        }

        /// <summary>
        /// Gets or sets the payload identifier.
        /// </summary>
        /// <value>The payload identifier.</value>
        public string PayloadId { get; private set; }
    }
}
