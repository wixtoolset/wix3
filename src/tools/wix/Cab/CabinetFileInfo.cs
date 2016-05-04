// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Properties of a file in a cabinet.
    /// </summary>
    internal sealed class CabinetFileInfo
    {
        private string fileId;
        private ushort date;
        private ushort time;
        private int size;

        /// <summary>
        /// Constructs CabinetFileInfo
        /// </summary>
        /// <param name="fileId">File Id</param>
        /// <param name="date">Last modified date (MS-DOS time)</param>
        /// <param name="time">Last modified time (MS-DOS time)</param>
        public CabinetFileInfo(string fileId, ushort date, ushort time, int size)
        {
            this.fileId = fileId;
            this.date = date;
            this.time = time;
            this.size = size;
        }

        /// <summary>
        /// Gets the file Id of the file.
        /// </summary>
        /// <value>file Id</value>
        public string FileId
        {
            get { return this.fileId; }
        }

        /// <summary>
        /// Gets modified date (DOS format).
        /// </summary>
        public ushort Date
        {
            get { return this.date; }
        }

        /// <summary>
        /// Gets modified time (DOS format).
        /// </summary>
        public ushort Time
        {
            get { return this.time; }
        }

        /// <summary>
        /// Gets the size of the file in bytes.
        /// </summary>
        public int Size
        {
            get { return this.size; }
        }
    }
}
