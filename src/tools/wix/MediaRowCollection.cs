//-------------------------------------------------------------------------------------------------
// <copyright file="MediaRowCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Hash table collection of specialized media rows.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;

    /// <summary>
    /// Hash table collection of specialized media rows.
    /// </summary>
    public sealed class MediaRowCollection : ICollection
    {
        private Hashtable collection;

        /// <summary>
        /// Instantiate a new RowCollection class.
        /// </summary>
        public MediaRowCollection()
        {
            this.collection = new Hashtable();
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
        /// Gets a media row by disk id.
        /// </summary>
        /// <param name="diskId">Disk identifier of media row to locate.</param>
        public MediaRow this[int diskId]
        {
            get { return (MediaRow)this.collection[diskId]; }
        }

        /// <summary>
        /// Adds the row from a Media table to the end of the collection.
        /// </summary>
        /// <param name="mediaRow">The row from the Media table to add.</param>
        public void Add(MediaRow mediaRow)
        {
            this.collection.Add(mediaRow.DiskId, mediaRow);
        }

        /// <summary>
        /// Adds the rows in the RowCollection to the end of this collection.
        /// </summary>
        /// <param name="rowCollection">The RowCollection to add.</param>
        public void AddRange(RowCollection rowCollection)
        {
            if (0 == this.collection.Count)
            {
                this.collection = new Hashtable(rowCollection.Count);
            }

            foreach (MediaRow mediaRow in rowCollection)
            {
                this.collection.Add(mediaRow.DiskId, mediaRow);
            }
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
            return this.collection.Values.GetEnumerator();
        }
    }
}
