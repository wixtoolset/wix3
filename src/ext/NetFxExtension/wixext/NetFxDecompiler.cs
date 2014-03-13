//-------------------------------------------------------------------------------------------------
// <copyright file="NetFxDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset .NET Framework Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using NetFx = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.NetFx;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetFxDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "NetFxNativeImage":
                    this.DecompileNetFxNativeImageTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the NetFxNativeImage table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileNetFxNativeImageTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                NetFx.NativeImage nativeImage = new NetFx.NativeImage();

                nativeImage.Id = (string)row[0];

                switch ((int)row[2])
                {
                    case 0:
                        nativeImage.Priority = NetFx.NativeImage.PriorityType.Item0;
                        break;
                    case 1:
                        nativeImage.Priority = NetFx.NativeImage.PriorityType.Item1;
                        break;
                    case 2:
                        nativeImage.Priority = NetFx.NativeImage.PriorityType.Item2;
                        break;
                    case 3:
                        nativeImage.Priority = NetFx.NativeImage.PriorityType.Item3;
                        break;
                }

                if (null != row[3])
                {
                    int attributes = (int)row[3];

                    if (0x1 == (attributes & 0x1))
                    {
                        nativeImage.Debug = NetFx.YesNoType.yes;
                    }

                    if (0x2 == (attributes & 0x2))
                    {
                        nativeImage.Dependencies = NetFx.YesNoType.no;
                    }

                    if (0x4 == (attributes & 0x4))
                    {
                        nativeImage.Profile = NetFx.YesNoType.yes;
                    }

                    if (0x8 == (attributes & 0x8) && 0x10 == (attributes & 0x10))
                    {
                        nativeImage.Platform = NetFx.NativeImage.PlatformType.all;
                    }
                    else if (0x8 == (attributes & 0x8))
                    {
                        nativeImage.Platform = NetFx.NativeImage.PlatformType.Item32bit;
                    }
                    else if (0x10 == (attributes & 0x10))
                    {
                        nativeImage.Platform = NetFx.NativeImage.PlatformType.Item64bit;
                    }
                }

                if (null != row[4])
                {
                    nativeImage.AssemblyApplication = (string)row[4];
                }

                if (null != row[5])
                {
                    nativeImage.AppBaseDirectory = (string)row[5];
                }

                Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", (string)row[1]);
                if (null != file)
                {
                    file.AddChild(nativeImage);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File_", (string)row[1], "File"));
                }
            }
        }
    }
}
