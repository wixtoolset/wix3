//-------------------------------------------------------------------------------------------------
// <copyright file="RowCollection.cs" company="Outercurve Foundation">
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

    /// <summary>
    /// Array collection of rows.
    /// </summary>
    public sealed class RowCollection : ICollection
    {
        private ArrayList collection;

        /// <summary>
        /// Instantiate a new RowCollection class.
        /// </summary>
        public RowCollection()
        {
            this.collection = new ArrayList();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>Number of items in collection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Gets if the collection has been synchronized.
        /// </summary>
        /// <value>True if the collection has been synchronized.</value>
        public bool IsSynchronized
        {
            get { return this.collection.IsSynchronized; }
        }

        /// <summary>
        /// Gets the object used to synchronize the collection.
        /// </summary>
        /// <value>Oject used the synchronize the collection.</value>
        public object SyncRoot
        {
            get { return this.collection.SyncRoot; }
        }

        /// <summary>
        /// Gets the row at the specified index.
        /// </summary>
        /// <param name="index">The index of the row to retrieve.</param>
        /// <value>The row at the specified index.</value>
        public Row this[int index]
        {
            get { return (Row)this.collection[index]; }
        }

        /// <summary>
        /// Adds a row to the collection.
        /// </summary>
        /// <param name="row">Row to add to collection.</param>
        public void Add(Row row)
        {
            this.collection.Add(row);
        }

        /// <summary>
        /// Adds the rows in the RowCollection to the end of this collection.
        /// </summary>
        /// <param name="rowCollection">The RowCollection to add.</param>
        public void AddRange(RowCollection rowCollection)
        {
            this.collection.AddRange(rowCollection);
        }

        /// <summary>
        /// Sorts the rows in the RowCollection.
        /// </summary>
        /// <param name="comparer">The IComparer to use in sorting.</param>
        public void Sort(IComparer comparer)
        {
            this.collection.Sort(comparer);
        }

        /// <summary>
        /// Removes all rows from the RowCollection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// Clone the RowCollection.
        /// </summary>
        /// <returns>The cloned RowCollection.</returns>
        public RowCollection Clone()
        {
            RowCollection rowCollection = new RowCollection();

            rowCollection.collection.AddRange(this.collection);

            return rowCollection;
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.CopyTo(array, index);
        }

        /// <summary>
        /// Gets enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        /// <summary>
        /// Removes the row at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the row to remove.</param>
        public void RemoveAt(int index)
        {
            this.collection.RemoveAt(index);
        }
    }
}
