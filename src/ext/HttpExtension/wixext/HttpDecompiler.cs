//-------------------------------------------------------------------------------------------------
// <copyright file="HttpDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset Http Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.Tools.WindowsInstallerXml;
    using Http = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.Http;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Http Extension.
    /// </summary>
    public sealed class HttpDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "WixHttpUrlReservation":
                    this.DecompileWixHttpUrlReservationTable(table);
                    break;
                case "WixHttpUrlAce":
                    this.DecompileWixHttpUrlAceTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the WixHttpUrlReservation table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixHttpUrlReservationTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Http.UrlReservation urlReservation = new Http.UrlReservation();
                urlReservation.Id = (string)row[0];
                switch((int)row[1])
                {
                    case HttpConstants.heReplace:
                    default:
                        urlReservation.HandleExisting = Http.UrlReservation.HandleExistingType.replace;
                        break;
                    case HttpConstants.heIgnore:
                        urlReservation.HandleExisting = Http.UrlReservation.HandleExistingType.ignore;
                        break;
                    case HttpConstants.heFail:
                        urlReservation.HandleExisting = Http.UrlReservation.HandleExistingType.fail;
                        break;
                }
                urlReservation.Sddl = (string)row[2];
                urlReservation.Url = (string)row[3];

                Wix.Component component = (Wix.Component)this.Core.GetIndexedElement("Component", (string)row[4]);
                if (null != component)
                {
                    component.AddChild(urlReservation);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
                }
                this.Core.IndexElement(row, urlReservation);
            }
        }


        /// <summary>
        /// Decompile the WixHttpUrlAce table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileWixHttpUrlAceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                Http.UrlAce urlace = new Http.UrlAce();
                urlace.Id = (string)row[0];
                urlace.SecurityPrincipal = (string)row[2];
                switch (Convert.ToInt32(row[3]))
                {
                    case HttpConstants.GENERIC_ALL:
                    default:
                        urlace.Rights = Http.UrlAce.RightsType.all;
                        break;
                    case HttpConstants.GENERIC_EXECUTE:
                        urlace.Rights = Http.UrlAce.RightsType.register;
                        break;
                    case HttpConstants.GENERIC_WRITE:
                        urlace.Rights = Http.UrlAce.RightsType.@delegate;
                        break;
                }

                string reservationId = (string)row[1];
                Http.UrlReservation urlReservation = (Http.UrlReservation)this.Core.GetIndexedElement("WixHttpUrlReservation", reservationId);
                if (null != urlReservation)
                {
                    urlReservation.AddChild(urlace);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, urlace.Id, "WixHttpUrlReservation_", reservationId, "WixHttpUrlReservation"));
                }
            }
        }
    }
}
