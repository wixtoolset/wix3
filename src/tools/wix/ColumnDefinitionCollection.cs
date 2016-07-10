// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A collection of definitions.
    /// </summary>
    public sealed class ColumnDefinitionCollection : ICollection, ICollection<ColumnDefinition>
    {
        private List<ColumnDefinition> collection;
        private Dictionary<string, int> indexHashtable;
        private Dictionary<string, ColumnDefinition> nameHashtable;

        /// <summary>
        /// Instantiate a new ColumnDefinitionCollection class.
        /// </summary>
        public ColumnDefinitionCollection()
        {
            this.collection = new List<ColumnDefinition>();
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
        /// Gets if the collection is read-only. This collection type is not read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets if the collection has been synchronized.
        /// </summary>
        /// <value>True if the collection has been synchronized.</value>
        public bool IsSynchronized
        {
            get { return ((ICollection) this.collection).IsSynchronized; }
        }

        /// <summary>
        /// Gets the object used to synchronize the collection.
        /// </summary>
        /// <value>Oject used the synchronize the collection.</value>
        public object SyncRoot
        {
            get { return ((ICollection) this.collection).SyncRoot; }
        }

        /// <summary>
        /// Gets a column definition by index.
        /// </summary>
        /// <param name="index">Index into array.</param>
        /// <value>Column definition at index location.</value>
        public ColumnDefinition this[int index]
        {
            get { return this.collection[index]; }
        }

        /// <summary>
        /// Gets a column definition by name.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>Column definition for the named column.</returns>
        public ColumnDefinition this[string columnName]
        {
            get
            {
                if (null == this.nameHashtable)
                {
                    this.nameHashtable = new Dictionary<string, ColumnDefinition>();

                    foreach (ColumnDefinition columnDefinition in this.collection)
                    {
                        this.nameHashtable.Add(columnDefinition.Name, columnDefinition);
                    }
                }

                return this.nameHashtable[columnName];
            }
        }

        /// <summary>
        /// Adds a column definition to the collection.
        /// </summary>
        /// <param name="item">Column definition to add to array.</param>
        public void Add(ColumnDefinition item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            this.collection.Add(item);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        void ICollection.CopyTo(System.Array array, int index)
        {
            ((ICollection) this.collection).CopyTo(array, index);
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        public void CopyTo(ColumnDefinition[] array, int arrayIndex)
        {
            this.collection.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        /// <summary>
        /// Gets enumerator for the collection.
        /// </summary>
        /// <returns>Enumerator for the collection.</returns>
        public IEnumerator<ColumnDefinition> GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        /// <summary>
        /// Returns the zero-based index of the named column.
        /// </summary>
        /// <param name="columnName">The name of the column.</param>
        /// <returns>The zero-based index of the named column.</returns>
        public int IndexOf(string columnName)
        {
            if (null == this.indexHashtable)
            {
                this.indexHashtable = new Dictionary<string, int>();

                for (int i = 0; i < this.collection.Count; i++)
                {
                    this.indexHashtable.Add(this.collection[i].Name, i);
                }
            }

            return this.indexHashtable[columnName];
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
            this.indexHashtable = null;
            this.nameHashtable = null;
        }

        /// <summary>
        /// Tests whether the collection contains an item.
        /// </summary>
        /// <param name="item">The column to look for.</param>
        /// <returns></returns>
        public bool Contains(ColumnDefinition item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return this.IndexOf(item.Name) >= 0;
        }

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="item">The column to remove.</param>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(ColumnDefinition item)
        {
            if (this.collection.Remove(item))
            {
                this.indexHashtable = null;
                this.nameHashtable = null;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
