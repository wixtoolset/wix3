// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Specialization of a row for the WixMedia table.
    /// </summary>
    public sealed class WixMediaRow : Row
    {
        /// <summary>
        /// Creates a WixMedia row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public WixMediaRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixMedia row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public WixMediaRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the compression level for this media row.
        /// </summary>
        /// <value>Compression level.</value>
        public Cab.CompressionLevel CompressionLevel
        {
            get
            {
                Cab.CompressionLevel level = Cab.CompressionLevel.Mszip;
                switch ((string)this.Fields[1].Data)
                {
                    case "low":
                        level = Cab.CompressionLevel.Low;
                        break;
                    case "medium":
                        level = Cab.CompressionLevel.Medium;
                        break;
                    case "high":
                        level = Cab.CompressionLevel.High;
                        break;
                    case "none":
                        level = Cab.CompressionLevel.None;
                        break;
                    case "mszip":
                        level = Cab.CompressionLevel.Mszip;
                        break;
                }

                return level;
            }

            set
            {
                switch (value)
                {
                    case Cab.CompressionLevel.None:
                        this.Fields[1].Data = "none";
                        break;
                    case Cab.CompressionLevel.Low:
                        this.Fields[1].Data = "low";
                        break;
                    case Cab.CompressionLevel.Medium:
                        this.Fields[1].Data = "medium";
                        break;
                    case Cab.CompressionLevel.High:
                        this.Fields[1].Data = "high";
                        break;
                    case Cab.CompressionLevel.Mszip:
                        this.Fields[1].Data = "mszip";
                        break;
                    default:
                        throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, WixStrings.EXP_UnknownCompressionLevelType, value));
                }
            }
        }

        /// <summary>
        /// Gets or sets the disk id for this media.
        /// </summary>
        /// <value>Disk id for the media.</value>
        public int DiskId
        {
            get { return Convert.ToInt32(this.Fields[0].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the compression level for this media row has been set.
        /// </summary>
        /// <value>Compression level.</value>
        public bool HasExplicitCompressionLevel
        {
            get { return !String.IsNullOrEmpty((string)this.Fields[1].Data); }
        }

        /// <summary>
        /// Gets or sets the layout location for this media row.
        /// </summary>
        /// <value>Layout location to the root of the media.</value>
        public string Layout
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }
    }
}
