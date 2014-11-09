//-------------------------------------------------------------------------------------------------
// <copyright file="HttpDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the WiX Toolset Http Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using Http = WixToolset.Extensions.Serialize.Http;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The decompiler for the WiX Toolset Http Extension.
    /// </summary>
    public sealed class HttpDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Creates a decompiler for Http Extension.
        /// </summary>
        public HttpDecompiler()
        {
            this.TableDefinitions = HttpExtensionData.GetExtensionTableDefinitions();
        }

        /// <summary>
        /// Get the extensions library to be removed.
        /// </summary>
        /// <param name="tableDefinitions">Table definitions for library.</param>
        /// <returns>Library to remove from decompiled output.</returns>
        public override Library GetLibraryToRemove(TableDefinitionCollection tableDefinitions)
        {
            return HttpExtensionData.GetExtensionLibrary(tableDefinitions);
        }

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
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerConstants.PrimaryKeyDelimiter), "Component_", (string)row[2], "Component"));
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
