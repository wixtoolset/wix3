// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Specialization of a row for the Component table.
    /// </summary>
    public sealed class ComponentRow : Row
    {
        private string sourceFile;

        /// <summary>
        /// Creates a Control row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Component row belongs to and should get its column definitions from.</param>
        public ComponentRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets or sets the identifier for this Component row.
        /// </summary>
        /// <value>Identifier for this Component row.</value>
        public string Component
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets or sets the ComponentId for this Component row.
        /// </summary>
        /// <value>guid for this Component row.</value>
        public string Guid
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets or sets the Directory_ of the Component.
        /// </summary>
        /// <value>Directory of the Component.</value>
        public string Directory
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets the local only attribute of the Component.
        /// </summary>
        /// <value>Local only attribute of the component.</value>
        public bool IsLocalOnly
        {
            get { return MsiInterop.MsidbComponentAttributesLocalOnly == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesLocalOnly); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesLocalOnly;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesLocalOnly;
                }
            }
        }

        /// <summary>
        /// Gets or sets the source only attribute of the Component.
        /// </summary>
        /// <value>Source only attribute of the component.</value>
        public bool IsSourceOnly
        {
            get { return MsiInterop.MsidbComponentAttributesSourceOnly == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesSourceOnly); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesSourceOnly;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesSourceOnly;
                }
            }
        }

        /// <summary>
        /// Gets or sets the optional attribute of the Component.
        /// </summary>
        /// <value>Optional attribute of the component.</value>
        public bool IsOptional
        {
            get { return MsiInterop.MsidbComponentAttributesOptional == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesOptional); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesOptional;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesOptional;
                }
            }
        }

        /// <summary>
        /// Gets or sets the registry key path attribute of the Component.
        /// </summary>
        /// <value>Registry key path attribute of the component.</value>
        public bool IsRegistryKeyPath
        {
            get { return MsiInterop.MsidbComponentAttributesRegistryKeyPath == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesRegistryKeyPath); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesRegistryKeyPath;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesRegistryKeyPath;
                }
            }
        }

        /// <summary>
        /// Gets or sets the shared dll ref count attribute of the Component.
        /// </summary>
        /// <value>Shared dll ref countattribute of the component.</value>
        public bool IsSharedDll
        {
            get { return MsiInterop.MsidbComponentAttributesSharedDllRefCount == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesSharedDllRefCount); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesSharedDllRefCount;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesSharedDllRefCount;
                }
            }
        }

        /// <summary>
        /// Gets or sets the permanent attribute of the Component.
        /// </summary>
        /// <value>Permanent attribute of the component.</value>
        public bool IsPermanent
        {
            get { return MsiInterop.MsidbComponentAttributesPermanent == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesPermanent); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesPermanent;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesPermanent;
                }
            }
        }

        /// <summary>
        /// Gets or sets the ODBC data source key path attribute of the Component.
        /// </summary>
        /// <value>ODBC data source key path attribute of the component.</value>
        public bool IsOdbcDataSourceKeyPath
        {
            get { return MsiInterop.MsidbComponentAttributesODBCDataSource == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributesODBCDataSource); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributesODBCDataSource;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributesODBCDataSource;
                }
            }
        }

        /// <summary>
        /// Gets or sets the 64 bit attribute of the Component.
        /// </summary>
        /// <value>64-bitness of the component.</value>
        public bool Is64Bit
        {
            get { return MsiInterop.MsidbComponentAttributes64bit == ((int)this.Fields[3].Data & MsiInterop.MsidbComponentAttributes64bit); }
            set
            {
                if (value)
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data | MsiInterop.MsidbComponentAttributes64bit;
                }
                else
                {
                    this.Fields[3].Data = (int)this.Fields[3].Data & ~MsiInterop.MsidbComponentAttributes64bit;
                }
            }
        }

        /// <summary>
        /// Gets or sets the condition of the Component.
        /// </summary>
        /// <value>Condition of the Component.</value>
        public string Condition
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the key path of the Component.
        /// </summary>
        /// <value>Key path of the Component.</value>
        public string KeyPath
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets or sets the source location to the file to fill in the Text of the control.
        /// </summary>
        /// <value>Source location to the file to fill in the Text of the control.</value>
        public string SourceFile
        {
            get { return this.sourceFile; }
            set { this.sourceFile = value; }
        }
    }
}
