//-------------------------------------------------------------------------------------------------
// <copyright file="RowDictionary.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// A collection of rows indexed by their primary key.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.ObjectModel;

    /// <summary>
    /// A collection of rows indexed by their primary key.
    /// </summary>
    public sealed class RowDictionary<T> : KeyedCollection<string, T> where T : Row
    {
        /// <summary>
        /// Creates an empty <see cref="RowDictionary"/>.
        /// </summary>
        public RowDictionary() : base(StringComparer.InvariantCulture)
        {
        }

        /// <summary>
        /// Creates and populates a <see cref="RowDictionary"/> with the rows from the given <see cref="Table"/>.
        /// </summary>
        /// <param name="table">The table to index.</param>
        /// <remarks>
        /// Rows added to the index are not automatically added to the given <paramref name="table"/>.
        /// </remarks>
        public RowDictionary(Table table) : this()
        {
            if (null != table && 0 < table.Rows.Count)
            {
                foreach (T row in table.Rows)
                {
                    this.TryAdd(row);
                }
            }
        }

        /// <summary>
        /// Tries to add a row if the primary key doesn't already exist.
        /// </summary>
        /// <param name="row">The row to add.</param>
        /// <returns>
        /// True if the row was added; otherwise, false if the primary key already existed.
        /// </returns>
        public bool TryAdd(T row)
        {
            string key = this.GetKeyForItem(row);

            if (!base.Contains(key))
            {
                base.Add(row);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a row for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the row to retrieve.</param>
        /// <param name="row">The row that was retrieved.</param>
        /// <returns>
        /// True if the row was retrieved; otherwise, false if the row does not exist.
        /// </returns>
        public bool TryGet(string key, out T row)
        {
            if (base.Contains(key))
            {
                row = base[key];
                return true;
            }

            row = null;
            return false;
        }

        /// <summary>
        /// Gets the key to index from the <paramref name="row"/>.
        /// </summary>
        /// <param name="row">The row to be indexed.</param>
        /// <returns>
        /// The key to index.
        /// </returns>
        protected override string GetKeyForItem(T row)
        {
            if (null == row)
            {
                throw new ArgumentNullException("row");
            }

            return row.GetPrimaryKey('/');
        }
    }
}
