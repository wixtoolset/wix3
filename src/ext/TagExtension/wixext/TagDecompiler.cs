// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using Microsoft.Tools.WindowsInstallerXml;
    using Tag = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Tag;

    /// <summary>
    /// The Binder for the Windows Installer XML Toolset Software Id Tag Extension.
    /// </summary>
    public sealed class TagDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "SoftwareIdentificationTag":
                    this.DecompileSoftwareIdentificationTag(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the SoftwareIdentificationTag table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileSoftwareIdentificationTag(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Tag.Tag tag = new Tag.Tag();

                tag.Regid = (string)row[1];

                this.Core.RootElement.AddChild(tag);
            }
        }
    }
}
