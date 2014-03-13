//-------------------------------------------------------------------------------------------------
// <copyright file="ConnectToModuleCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Hash collection of connect to module objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;

    /// <summary>
    /// Hash collection of connect to module objects.
    /// </summary>
    public sealed class ConnectToModuleCollection : ICollection
    {
        private Hashtable collection;

        /// <summary>
        /// Instantiate a new ConnectToModuleCollection class.
        /// </summary>
        public ConnectToModuleCollection()
        {
            this.collection = new Hashtable();
        }

        /// <summary>
        /// Gets the number of elements actually contained in the ConnectToModuleCollection.
        /// </summary>
        /// <value>The number of elements actually contained in the ConnectToModuleCollection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the ConnectToModuleCollection is synchronized (thread-safe).
        /// </summary>
        /// <value>true if access to the ConnectToModuleCollection is synchronized (thread-safe); otherwise, false. The default is false.</value>
        public bool IsSynchronized
        {
            get { return this.collection.IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the ConnectToModuleCollection.
        /// </summary>
        /// <value>An object that can be used to synchronize access to the ConnectToModuleCollection.</value>
        public object SyncRoot
        {
            get { return this.collection.SyncRoot; }
        }

        /// <summary>
        /// Gets a module connection by child id.
        /// </summary>
        /// <param name="childId">Identifier of child to locate.</param>
        public ConnectToModule this[string childId]
        {
            get { return (ConnectToModule)this.collection[childId]; }
        }

        /// <summary>
        /// Adds a module connection to the collection.
        /// </summary>
        /// <param name="connection">Module connection to add.</param>
        public void Add(ConnectToModule connection)
        {
            if (null == connection)
            {
                throw new ArgumentNullException("connection");
            }

            this.collection.Add(connection.ChildId, connection);
        }

        /// <summary>
        /// Copies the entire ConnectToModuleCollection to a compatible one-dimensional Array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from this ConnectToModuleCollection. The Array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.Keys.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator for the entire ConnectToModuleCollection.
        /// </summary>
        /// <returns>An IEnumerator for the entire ConnectToModuleCollection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.Keys.GetEnumerator();
        }
    }
}
