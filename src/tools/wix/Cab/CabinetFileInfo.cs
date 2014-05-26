//-------------------------------------------------------------------------------------------------
// <copyright file="CabinetFileInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains properties for a file inside a cabinet
// </summary>
//-------------------------------------------------------------------------------------------------

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
