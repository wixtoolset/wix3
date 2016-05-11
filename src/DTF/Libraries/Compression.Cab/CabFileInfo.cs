// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Compression.Cab
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Security.Permissions;

    /// <summary>
    /// Object representing a compressed file within a cabinet package; provides operations for getting
    /// the file properties and extracting the file.
    /// </summary>
    [Serializable]
    public class CabFileInfo : ArchiveFileInfo
    {
        private int cabFolder;

        /// <summary>
        /// Creates a new CabinetFileInfo object representing a file within a cabinet in a specified path.
        /// </summary>
        /// <param name="cabinetInfo">An object representing the cabinet containing the file.</param>
        /// <param name="filePath">The path to the file within the cabinet. Usually, this is a simple file
        /// name, but if the cabinet contains a directory structure this may include the directory.</param>
        public CabFileInfo(CabInfo cabinetInfo, string filePath)
            : base(cabinetInfo, filePath)
        {
            if (cabinetInfo == null)
            {
                throw new ArgumentNullException("cabinetInfo");
            }

            this.cabFolder = -1;
        }

        /// <summary>
        /// Creates a new CabinetFileInfo object with all parameters specified,
        /// used internally when reading the metadata out of a cab.
        /// </summary>
        /// <param name="filePath">The internal path and name of the file in the cab.</param>
        /// <param name="cabFolder">The folder number containing the file.</param>
        /// <param name="cabNumber">The cabinet number where the file starts.</param>
        /// <param name="attributes">The stored attributes of the file.</param>
        /// <param name="lastWriteTime">The stored last write time of the file.</param>
        /// <param name="length">The uncompressed size of the file.</param>
        internal CabFileInfo(
            string filePath,
            int cabFolder,
            int cabNumber,
            FileAttributes attributes,
            DateTime lastWriteTime,
            long length)
            : base(filePath, cabNumber, attributes, lastWriteTime, length)
        {
            this.cabFolder = cabFolder;
        }

        /// <summary>
        /// Initializes a new instance of the CabinetFileInfo class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected CabFileInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.cabFolder = info.GetInt32("cabFolder");
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the archive.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
        /// <param name="context">The StreamingContext that contains contextual information
        /// about the source or destination.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("cabFolder", this.cabFolder);
        }

        /// <summary>
        /// Gets or sets the cabinet that contains this file.
        /// </summary>
        /// <value>
        /// The CabinetInfo instance that retrieved this file information -- this
        /// may be null if the CabinetFileInfo object was returned directly from a
        /// stream.
        /// </value>
        public CabInfo Cabinet
        {
            get
            {
                return (CabInfo) this.Archive;
            }
        }

        /// <summary>
        /// Gets the full path of the cabinet that contains this file.
        /// </summary>
        /// <value>The full path of the cabinet that contains this file.</value>
        public string CabinetName
        {
            get
            {
                return this.ArchiveName;
            }
        }

        /// <summary>
        /// Gets the number of the folder containing this file.
        /// </summary>
        /// <value>The number of the cabinet folder containing this file.</value>
        /// <remarks>A single folder or the first folder of a cabinet
        /// (or chain of cabinets) is numbered 0.</remarks>
        public int CabinetFolderNumber
        {
            get
            {
                if (this.cabFolder < 0)
                {
                    this.Refresh();
                }
                return this.cabFolder;
            }
        }

        /// <summary>
        /// Refreshes the information in this object with new data retrieved
        /// from an archive.
        /// </summary>
        /// <param name="newFileInfo">Fresh instance for the same file just
        /// read from the archive.</param>
        /// <remarks>
        /// This implementation refreshes the <see cref="CabinetFolderNumber"/>.
        /// </remarks>
        protected override void Refresh(ArchiveFileInfo newFileInfo)
        {
            base.Refresh(newFileInfo);
            this.cabFolder = ((CabFileInfo) newFileInfo).cabFolder;
        }
    }
}
