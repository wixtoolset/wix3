//-------------------------------------------------------------------------------------------------
// <copyright file="UIDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset UI Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

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
