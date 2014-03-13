//-------------------------------------------------------------------------------------------------
// <copyright file="ConnectToFeatureCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Hash collection of connect to feature objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;

    /// <summary>
    /// Hash collection of connect to feature objects.
    /// </summary>
    public sealed class ConnectToFeatureCollection : ICollection
    {
        private Hashtable collection;

        /// <summary>
        /// Instantiate a new ConnectToFeatureCollection class.
        /// </summary>
        public ConnectToFeatureCollection()
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
        /// Gets a feature connection by child id.
        /// </summary>
        /// <param name="childId">Identifier of child to locate.</param>
        public ConnectToFeature this[string childId]
        {
            get { return (ConnectToFeature)this.collection[childId]; }
        }

        /// <summary>
        /// Adds a feature connection to the collection.
        /// </summary>
        /// <param name="connection">Feature connection to add.</param>
        public void Add(ConnectToFeature connection)
        {
            if (null == connection)
            {
                throw new ArgumentNullException("connection");
            }

            this.collection.Add(connection.ChildId, connection);
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
