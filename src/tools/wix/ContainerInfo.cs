//-------------------------------------------------------------------------------------------------
// <copyright file="ContainerInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Container info for binding Bundles.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// Container info for binding Bundles.
    /// </summary>
    internal class ContainerInfo
    {
        private List<PayloadInfoRow> payloads = new List<PayloadInfoRow>();

        public ContainerInfo(Row row, BinderFileManager fileManager)
            : this((string)row[0], (string)row[1], (string)row[2], (string)row[3], fileManager)
        {
            this.SourceLineNumbers = row.SourceLineNumbers;
        }

        public ContainerInfo(string id, string name, string type, string downloadUrl, BinderFileManager fileManager)
        {
            this.Id = id;
            this.Name = name;
            this.Type = type;
            this.DownloadUrl = downloadUrl;
            this.FileManager = fileManager;
            this.TempPath = Path.Combine(fileManager.TempFilesLocation, name);
            this.FileInfo = new FileInfo(this.TempPath);
        }

        public SourceLineNumberCollection SourceLineNumbers { get; private set; }
        public BinderFileManager FileManager { get; private set; }
        public string DownloadUrl { get; private set; }
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string TempPath { get; private set; }
        public FileInfo FileInfo { get; set; }
        public long FileSize { get { return this.FileInfo.Length; } }

        public List<PayloadInfoRow> Payloads
        {
            get
            {
                return this.payloads;
            }
            set
            {
                this.payloads = value;
            }
        }
    }
}
