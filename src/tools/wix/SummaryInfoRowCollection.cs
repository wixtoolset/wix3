//-------------------------------------------------------------------------------------------------
// <copyright file="SummaryInfoRowCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Array collection of rows.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Indexed container class for summary information rows.
    /// </summary>
    internal sealed class SummaryInfoRowCollection : KeyedCollection<int, Row>
    {
        /// <summary>
        /// Creates the keyed collection from existing rows in a table.
        /// </summary>
        /// <param name="table">The summary information table to index.</param>
        internal SummaryInfoRowCollection(Table table)
        {
            if (0 != String.CompareOrdinal("_SummaryInformation", table.Name))
            {
                string message = string.Format(WixStrings.EXP_UnsupportedTable, table.Name);
                throw new ArgumentException(message, "table");
            }

            foreach (Row row in table.Rows)
            {
                this.Add(row);
            }
        }

        /// <summary>
        /// Gets the summary property ID for the <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row to index.</param>
        /// <returns>The summary property ID for the <paramref name="row"/>.
        protected override int GetKeyForItem(Row row)
        {
            return (int)row[0];
        }
    }
}
