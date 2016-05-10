// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Base class for creating a decompiler extension.
    /// </summary>
    public abstract class DecompilerExtension
    {
        private DecompilerCore decompilerCore;

        /// <summary>
        /// Gets or sets the decompiler core for the extension.
        /// </summary>
        /// <value>The decompiler core for the extension.</value>
        public DecompilerCore Core
        {
            get { return this.decompilerCore; }
            set { this.decompilerCore = value; }
        }

        /// <summary>
        /// Gets the option to remove the rows from this extension's library.
        /// </summary>
        /// <value>The option to remove the rows from this extension's library.</value>
        public virtual bool RemoveLibraryRows
        {
            get { return true; }
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public virtual void InitializeDecompile(TableCollection tables)
        {
        }

        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public virtual void DecompileTable(Table table)
        {
            this.Core.OnMessage(WixErrors.TableDecompilationUnimplemented(table.Name));
        }

        /// <summary>
        /// Finalize decompilation.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public virtual void FinalizeDecompile(TableCollection tables)
        {
        }
    }
}
