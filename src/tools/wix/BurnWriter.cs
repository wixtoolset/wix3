//-------------------------------------------------------------------------------------------------
// <copyright file="BurnWriter.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Burn PE writer for the Windows Installer Xml toolset.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Burn PE writer for the Windows Installer Xml toolset.
    /// </summary>
    /// <remarks>This class encapsulates reading/writing to a stub EXE for
    /// creating bundled/chained setup packages.</remarks>
    /// <example>
    /// using (BurnWriter writer = new BurnWriter(fileExe, this.core, guid))
    /// {
    ///     writer.AppendContainer(file1, BurnWriter.Container.UX);
    ///     writer.AppendContainer(file2, BurnWriter.Container.Attached);
    /// }
    /// </example>
    internal class BurnWriter : BurnCommon
    {
        private bool disposed;
        private bool invalidBundle;
        private BinaryWriter binaryWriter;

        /// <summary>
        /// Creates a BurnWriter for re-writing a PE file.
        /// </summary>
        /// <param name="fileExe">File to modify in-place.</param>
        /// <param name="messageHandler">The messagehandler to report warnings/errors to.</param>
        /// <param name="bundleGuid">GUID for the bundle.</param>
        private BurnWriter(string fileExe, IMessageHandler messageHandler)
            : base(fileExe, messageHandler)
        {
        }

        /// <summary>
        /// Opens a Burn writer.
        /// </summary>
        /// <param name="fileExe">Path to file.</param>
        /// <param name="messageHandler">Message handler.</param>
        /// <returns>Burn writer.</returns>
        public static BurnWriter Open(string fileExe, IMessageHandler messageHandler)
        {
            BurnWriter writer = new BurnWriter(fileExe, messageHandler);

            using (BinaryReader binaryReader = new BinaryReader(File.Open(fileExe, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete)))
            {
                if (!writer.Initialize(binaryReader))
                {
                    writer.invalidBundle = true;
                }
            }

            if (!writer.invalidBundle)
            {
                writer.binaryWriter = new BinaryWriter(File.Open(fileExe, FileMode.Open, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete));
            }

            return writer;
        }

        /// <summary>
        /// Update the ".wixburn" section data.
        /// </summary>
        /// <param name="stubSize">Size of the stub engine "burn.exe".</param>
        /// <param name="bundleId">Unique identifier for this bundle.</param>
        /// <returns></returns>
        public bool InitializeBundleSectionData(long stubSize, Guid bundleId)
        {
            if (this.invalidBundle)
            {
                return false;
            }

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_MAGIC, BURN_SECTION_MAGIC);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_VERSION, BURN_SECTION_VERSION);

            this.messageHandler.OnMessage(WixVerboses.BundleGuid(bundleId.ToString("B")));
            this.binaryWriter.BaseStream.Seek(this.wixburnDataOffset + BURN_SECTION_OFFSET_BUNDLEGUID, SeekOrigin.Begin);
            this.binaryWriter.Write(bundleId.ToByteArray());

            this.StubSize = (uint)stubSize;

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_STUBSIZE, this.StubSize);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALCHECKSUM, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATUREOFFSET, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATURESIZE, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_FORMAT, 1); // Hard-coded to CAB for now.
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_COUNT, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_UXSIZE, 0);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ATTACHEDCONTAINERSIZE, 0);
            this.binaryWriter.BaseStream.Flush();

            this.EngineSize = this.StubSize;

            return true;
        }

        /// <summary>
        /// Appends a UX or Attached container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="fileContainer">File path to append to the current exe.</param>
        /// <param name="container">Container section represented by the fileContainer.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        public bool AppendContainer(string fileContainer, BurnCommon.Container container)
        {
            using (FileStream reader = File.OpenRead(fileContainer))
            {
                return this.AppendContainer(reader, reader.Length, container);
            }
        }

        /// <summary>
        /// Appends a UX or Attached container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="containerStream">File stream to append to the current exe.</param>
        /// <param name="containerSize">Size of container to append.</param>
        /// <param name="container">Container section represented by the fileContainer.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        public bool AppendContainer(Stream containerStream, long containerSize, BurnCommon.Container container)
        {
            UInt32 burnSectionCount = 0;
            UInt32 burnSectionOffsetSize = 0;

            switch (container)
            {
                case Container.UX:
                    burnSectionCount = 1;
                    burnSectionOffsetSize = BURN_SECTION_OFFSET_UXSIZE;
                    // TODO: verify that the size in the section data is 0 or the same size.
                    this.EngineSize += (uint)containerSize;
                    this.UXSize = (uint)containerSize;
                    break;

                case Container.Attached:
                    burnSectionCount = 2;
                    burnSectionOffsetSize = BURN_SECTION_OFFSET_ATTACHEDCONTAINERSIZE;
                    // TODO: verify that the size in the section data is 0 or the same size.
                    this.AttachedContainerSize = (uint)containerSize;
                    break;

                default:
                    Debug.Assert(false);
                    return false;
            }

            return AppendContainer(containerStream, (UInt32)containerSize, burnSectionOffsetSize, burnSectionCount);
        }

        public void RememberThenResetSignature()
        {
            if (this.invalidBundle)
            {
                return;
            }

            this.OriginalChecksum = this.Checksum;
            this.OriginalSignatureOffset = this.SignatureOffset;
            this.OriginalSignatureSize = this.SignatureSize;

            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALCHECKSUM, this.OriginalChecksum);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATUREOFFSET, this.OriginalSignatureOffset);
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_ORIGINALSIGNATURESIZE, this.OriginalSignatureSize);

            this.Checksum = 0;
            this.SignatureOffset = 0;
            this.SignatureSize = 0;

            this.WriteToOffset(this.checksumOffset, this.Checksum);
            this.WriteToOffset(this.certificateTableSignatureOffset, this.SignatureOffset);
            this.WriteToOffset(this.certificateTableSignatureSize, this.SignatureSize);
        }

        /// <summary>
        /// Dispose object.
        /// </summary>
        /// <param name="disposing">True when releasing managed objects.</param>
        protected override void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing && this.binaryWriter != null)
                {
                    this.binaryWriter.Close();
                    this.binaryWriter = null;
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Appends a container to the exe and updates the ".wixburn" section data to point to it.
        /// </summary>
        /// <param name="containerStream">File stream to append to the current exe.</param>
        /// <param name="burnSectionOffsetSize">Offset of size field for this container in ".wixburn" section data.</param>
        /// <returns>true if the container data is successfully appended; false otherwise</returns>
        private bool AppendContainer(Stream containerStream, UInt32 containerSize, UInt32 burnSectionOffsetSize, UInt32 burnSectionCount)
        {
            if (this.invalidBundle)
            {
                return false;
            }

            // Update the ".wixburn" section data
            this.WriteToBurnSectionOffset(BURN_SECTION_OFFSET_COUNT, burnSectionCount);
            this.WriteToBurnSectionOffset(burnSectionOffsetSize, containerSize);

            // Append the container to the end of the existing bits.
            this.binaryWriter.BaseStream.Seek(0, SeekOrigin.End);
            BurnCommon.CopyStream(containerStream, this.binaryWriter.BaseStream, (int)containerSize);
            this.binaryWriter.BaseStream.Flush();

            return true;
        }

        /// <summary>
        /// Writes the value to an offset in the Burn section data.
        /// </summary>
        /// <param name="offset">Offset in to the Burn section data.</param>
        /// <param name="value">Value to write.</param>
        private void WriteToBurnSectionOffset(uint offset, uint value)
        {
            this.WriteToOffset(this.wixburnDataOffset + offset, value);
        }

        /// <summary>
        /// Writes the value to an offset in the Burn stub.
        /// </summary>
        /// <param name="offset">Offset in to the Burn stub.</param>
        /// <param name="value">Value to write.</param>
        private void WriteToOffset(uint offset, uint value)
        {
            this.binaryWriter.BaseStream.Seek((int)offset, SeekOrigin.Begin);
            this.binaryWriter.Write(value);
        }
    }
}
