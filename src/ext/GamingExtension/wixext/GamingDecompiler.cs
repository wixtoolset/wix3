// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using Gaming = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Gaming;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Gaming Extension.
    /// </summary>
    public sealed class GamingDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixGameExplorer":
                    this.DecompileWixGameExplorerTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the WixGameExplorer table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixGameExplorerTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Gaming.Game game = new Gaming.Game();

                game.Id = (string)row[0];

                Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", (string)row[1]);
                if (null != file)
                {
                    file.AddChild(game);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File_", (string)row[1], "File"));
                }
            }
        }
    }
}
