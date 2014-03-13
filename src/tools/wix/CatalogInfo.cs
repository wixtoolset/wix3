//-------------------------------------------------------------------------------------------------
// <copyright file="CatalogInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Catalog information for binding Bundles.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.IO;

    /// <summary>
    /// Catalog information for binding Bundles.
    /// </summary>
    internal class CatalogInfo
    {
        public CatalogInfo(Row row, string payloadId)
        {
            this.Initialize((string)row[0], (string)row[1], payloadId);
        }

        private void Initialize(string id, string sourceFile, string payloadId)
        {
            this.Id = id;
            this.FileInfo = new FileInfo(sourceFile);
            this.PayloadId = payloadId;
        }

        public string Id { get; private set; }
        public FileInfo FileInfo { get; private set; }
        public string PayloadId { get; private set; }
    }
}
