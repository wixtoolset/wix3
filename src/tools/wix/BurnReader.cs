// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using Microsoft.Tools.WindowsInstallerXml.Cab;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Burn PE reader for the Windows Installer Xml toolset.
    /// </summary>
    /// <remarks>This class encapsulates reading from a stub EXE with containers attached
    /// for dissecting bundled/chained setup packages.</remarks>
    /// <example>
    /// using (BurnReader reader = BurnReader.Open(fileExe, this.core, guid))
    /// {
    ///     reader.ExtractUXContainer(file1, tempFolder);
    /// }
    /// </example>
    internal class BurnReader : BurnCommon
    {
        private bool disposed;

        private bool invalidBundle;
        private BinaryReader binaryReader;
        private List<DictionaryEntry> attachedContainerPayloadNames;

        /// <summary>
        /// Creates a BurnReader for reading a PE file.
        /// </summary>
        /// <param name="fileExe">File to read.</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        private BurnReader(string fileExe, IMessageHandler messageHandler)
            : base(fileExe, messageHandler)
        {
            this.attachedContainerPayloadNames = new List<DictionaryEntry>();
        }

        /// <summary>
        /// Gets the underlying stream.
        /// </summary>
        public Stream Stream
        {
            get
            {
                return (null != this.binaryReader) ? this.binaryReader.BaseStream : null;
            }
        }

        /// <summary>
        /// Opens a Burn reader.
        /// </summary>
        /// <param name="fileExe">Path to file.</param>
        /// <param name="messageHandler">Message handler.</param>
        /// <returns>Burn reader.</returns>
        public static BurnReader Open(string fileExe, IMessageHandler messageHandler)
        {
            BurnReader reader = new BurnReader(fileExe, messageHandler);

            reader.binaryReader = new BinaryReader(File.Open(fileExe, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete));
            if (!reader.Initialize(reader.binaryReader))
            {
                reader.invalidBundle = true;
            }

            return reader;
        }

        /// <summary>
        /// Gets the UX container from the exe and extracts its contents to the output directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to write extracted files to.</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ExtractUXContainer(string outputDirectory, string tempDirectory)
        {
            // No UX container to extract
            if (this.UXAddress == 0 || this.UXSize == 0)
            {
                return false;
            }

            if (this.invalidBundle)
            {
                return false;
            }

            Directory.CreateDirectory(outputDirectory);
            string tempCabPath = Path.Combine(tempDirectory, "ux.cab");
            string manifestOriginalPath = Path.Combine(outputDirectory, "0");
            string manifestPath = Path.Combine(outputDirectory, "manifest.xml");

            this.binaryReader.BaseStream.Seek(this.UXAddress, SeekOrigin.Begin);
            using (Stream tempCab = File.Open(tempCabPath, FileMode.Create, FileAccess.Write))
            {
                BurnCommon.CopyStream(this.binaryReader.BaseStream, tempCab, (int)this.UXSize);
            }

            using (WixExtractCab extract = new WixExtractCab())
            {
                extract.Extract(tempCabPath, outputDirectory);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath));
            File.Delete(manifestPath);
            File.Move(manifestOriginalPath, manifestPath);

            XmlDocument document = new XmlDocument();
            document.Load(manifestPath);
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("burn", BurnCommon.BurnNamespace);
            XmlNodeList uxPayloads = document.SelectNodes("/burn:BurnManifest/burn:UX/burn:Payload", namespaceManager);
            XmlNodeList payloads = document.SelectNodes("/burn:BurnManifest/burn:Payload", namespaceManager);

            foreach (XmlNode uxPayload in uxPayloads)
            {
                XmlNode sourcePathNode = uxPayload.Attributes.GetNamedItem("SourcePath");
                XmlNode filePathNode = uxPayload.Attributes.GetNamedItem("FilePath");

                string sourcePath = Path.Combine(outputDirectory, sourcePathNode.Value);
                string destinationPath = Path.Combine(outputDirectory, filePathNode.Value);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
            }

            foreach (XmlNode payload in payloads)
            {
                XmlNode sourcePathNode = payload.Attributes.GetNamedItem("SourcePath");
                XmlNode filePathNode = payload.Attributes.GetNamedItem("FilePath");
                XmlNode packagingNode = payload.Attributes.GetNamedItem("Packaging");

                string sourcePath = sourcePathNode.Value;
                string destinationPath = filePathNode.Value;
                string packaging = packagingNode.Value;

                if (packaging.Equals("embedded", StringComparison.OrdinalIgnoreCase))
                {
                    this.attachedContainerPayloadNames.Add(new DictionaryEntry(sourcePath, destinationPath));
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the attached container from the exe and extracts its contents to the output directory.
        /// </summary>
        /// <param name="outputDirectory">Directory to write extracted files to.</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool ExtractAttachedContainer(string outputDirectory, string tempDirectory)
        {
            // No attached container to extract
            if (this.AttachedContainers.Count == 0)
            {
                return false;
            }

            if (this.invalidBundle)
            {
                return false;
            }

            Directory.CreateDirectory(outputDirectory);
            foreach (ContainerSlot cntnr in AttachedContainers)
            {
                string tempCabPath = Path.GetTempFileName();

                this.binaryReader.BaseStream.Seek(cntnr.Address, SeekOrigin.Begin);
                using (Stream tempCab = File.Open(tempCabPath, FileMode.Create, FileAccess.Write))
                {
                    BurnCommon.CopyStream(this.binaryReader.BaseStream, tempCab, (int)cntnr.Size);
                }

                using (WixExtractCab extract = new WixExtractCab())
                {
                    extract.Extract(tempCabPath, outputDirectory);
                }
            }

            foreach (DictionaryEntry entry in this.attachedContainerPayloadNames)
            {
                string sourcePath = Path.Combine(outputDirectory, (string)entry.Key);
                string destinationPath = Path.Combine(outputDirectory, (string)entry.Value);

                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                File.Delete(destinationPath);
                File.Move(sourcePath, destinationPath);
            }

            return true;
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        /// <param name="disposing">True when releasing managed objects.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing && this.binaryReader != null)
                {
                    this.binaryReader.Close();
                    this.binaryReader = null;
                }

                this.disposed = true;
            }
        }
    }
}
