// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.IO;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a payload from the file system.
    /// </summary>
    public sealed class DirPayloadsHarvester : PayloadHarvester
    {
        private static readonly string PayloadPrefix = "pld";

        /// <summary>
        /// Instantiate a new DirPayloadsHarvester.
        /// </summary>
        public DirPayloadsHarvester()
        {
        }

        public string BaseDownloadUrl { get; internal set; }

        /// <summary>
        /// Harvest a folder hierarchy as payloads with RemotePayload information.
        /// </summary>
        /// <param name="argument">The path of the payload.</param>
        /// <returns>A harvested payload.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }
            if (string.IsNullOrEmpty(BaseDownloadUrl))
            {
                throw new ArgumentNullException("url");
            }

            string dir = Path.GetFullPath(argument);
            string[] files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            Wix.Fragment fragment = new Wix.Fragment();
            foreach (string f in files)
            {
                if (!File.Exists(f))
                {
                    continue;
                }

                Wix.Payload payload = this.HarvestPayload(dir, f);
                fragment.AddChild(payload);
            }

            return new Wix.Fragment[] { fragment };
        }

        private Wix.Payload HarvestPayload(string baseDir, string path)
        {
            Wix.Payload payload = new Wix.Payload();

            // baseDir is absolute so we can simplify sub-name retrieval
            payload.Name = path.Substring(baseDir.Length + 1);
            payload.Id = this.Core.GenerateIdentifier(PayloadPrefix, BaseDownloadUrl, payload.Name);
            payload.DownloadUrl = BaseDownloadUrl + "/" + payload.Name.Replace("\\", "/");

            // Harvest remote payload
            Wix.RemotePayload remotePayload = this.HarvestRemotePayload(path);
            payload.AddChild(remotePayload);

            return payload;
        }
    }
}
