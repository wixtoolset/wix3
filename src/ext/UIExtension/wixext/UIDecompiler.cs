// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset UI Extension.
    /// </summary>
    public sealed class UIDecompiler : DecompilerExtension
    {
        private bool removeLibraryRows;

        /// <summary>
        /// Gets the option to remove the rows from this extension's library.
        /// </summary>
        /// <value>The option to remove the rows from this extension's library.</value>
        public override bool RemoveLibraryRows
        {
            get { return this.removeLibraryRows; }
        }

        /// <summary>
        /// Called at the beginning of the decompilation of a database.
        /// </summary>
        /// <param name="tables">The collection of all tables.</param>
        public override void InitializeDecompile(TableCollection tables)
        {
            Table propertyTable = tables["Property"];

            if (null != propertyTable)
            {
                foreach (Row row in propertyTable.Rows)
                {
                    if ("WixUI_Mode" == (string)row[0])
                    {
                        Wix.UIRef uiRef = new Wix.UIRef();

                        uiRef.Id = String.Concat("WixUI_", (string)row[1]);

                        this.Core.RootElement.AddChild(uiRef);
                        this.removeLibraryRows = true;

                        break;
                    }
                }
            }
        }
    }
}
