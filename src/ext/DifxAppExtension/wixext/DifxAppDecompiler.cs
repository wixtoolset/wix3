//-------------------------------------------------------------------------------------------------
// <copyright file="DifxAppDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset Driver Install Frameworks for Applications Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Globalization;

    using DifxApp = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.DifxApp;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Driver Install Frameworks for Applications Extension.
    /// </summary>
    public sealed class DifxAppDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "MsiDriverPackages":
                    this.DecompileMsiDriverPackagesTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the MsiDriverPackages table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileMsiDriverPackagesTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                DifxApp.Driver driver = new DifxApp.Driver();

                int attributes = (int)row[1];
                if (0x1 == (attributes & 0x1))
                {
                    driver.ForceInstall = DifxApp.YesNoType.yes;
                }

                if (0x2 == (attributes & 0x2))
                {
                    driver.PlugAndPlayPrompt = DifxApp.YesNoType.no;
                }

                if (0x4 == (attributes & 0x4))
                {
                    driver.AddRemovePrograms = DifxApp.YesNoType.no;
                }

                if (0x8 == (attributes & 0x8))
                {
                    driver.Legacy = DifxApp.YesNoType.yes;
                }

                if (0x10 == (attributes & 0x10))
                {
                    driver.DeleteFiles = DifxApp.YesNoType.yes;
                }

                if (null != row[2])
                {
                    driver.Sequence = (int)row[2];
                }

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[0]);
                if (null != component)
                {
                    component.AddChild(driver);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component", (string)row[0], "Component"));
                }
            }
        }
    }
}
