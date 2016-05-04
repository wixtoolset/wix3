// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Specialization of a row for the WixProperty table.
    /// </summary>
    public sealed class WixPropertyRow : Row
    {
        /// <summary>Creates a WixProperty row that belongs to a table.</summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this WixProperty row belongs to and should get its column definitions from.</param>
        public WixPropertyRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets and sets the id for this property row.
        /// </summary>
        /// <value>Id for the property.</value>
        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets and sets if this is an admin property row.
        /// </summary>
        /// <value>Flag if this is an admin property.</value>
        public bool Admin
        {
            get
            {
                return (0x1 == (Convert.ToInt32(this.Fields[1].Data, CultureInfo.InvariantCulture) & 0x1));
            }

            set
            {
                if (null == this.Fields[1].Data)
                {
                    this.Fields[1].Data = 0;
                }

                if (value)
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data | 0x1;
                }
                else
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data & ~0x1;
                }
            }
        }

        /// <summary>
        /// Gets and sets if this is a hidden property row.
        /// </summary>
        /// <value>Flag if this is a hidden property.</value>
        public bool Hidden
        {
            get
            {
                return (0x2 == (Convert.ToInt32(this.Fields[1].Data, CultureInfo.InvariantCulture) & 0x2));
            }

            set
            {
                if (null == this.Fields[1].Data)
                {
                    this.Fields[1].Data = 0;
                }

                if (value)
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data | 0x2;
                }
                else
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data & ~0x2;
                }
            }
        }

        /// <summary>
        /// Gets and sets if this is a secure property row.
        /// </summary>
        /// <value>Flag if this is a secure property.</value>
        public bool Secure
        {
            get
            {
                return (0x4 == (Convert.ToInt32(this.Fields[1].Data, CultureInfo.InvariantCulture) & 0x4));
            }

            set
            {
                if (null == this.Fields[1].Data)
                {
                    this.Fields[1].Data = 0;
                }

                if (value)
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data | 0x4;
                }
                else
                {
                    this.Fields[1].Data = (int)this.Fields[1].Data & ~0x4;
                }
            }
        }
    }
}
