//-------------------------------------------------------------------------------------------------
// <copyright file="ElementCollection.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Element collections used by generated strongly-typed schema objects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.ClickThrough.Serialize
{
    using System;
    using System.Collections;
    using System.Globalization;

    /// <summary>
    /// Collection used in the CodeDOM for the children of a given element. Provides type-checking
    /// on the allowed children to ensure that only allowed types are added.
    /// </summary>
    public class ElementCollection : ICollection, IEnumerable
    {
        private CollectionType collectionType;
        private int minimum = 1;
        private int maximum = 1;
        private int totalContainedItems;
        private int containersUsed;
        private ArrayList items;

        /// <summary>
        /// Creates a new element collection.
        /// </summary>
        /// <param name="collectionType">Type of the collection to create.</param>
        public ElementCollection(CollectionType collectionType)
        {
            this.collectionType = collectionType;
            this.items = new ArrayList();
        }

        /// <summary>
        /// Creates a new element collection.
        /// </summary>
        /// <param name="collectionType">Type of the collection to create.</param>
        /// <param name="minimum">When used with a type 'Choice', specifies a minimum number of allowed children.</param>
        /// <param name="maximum">When used with a type 'Choice', specifies a maximum number of allowed children.</param>
        public ElementCollection(CollectionType collectionType, int minimum, int maximum) : this(collectionType)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        /// <summary>
        /// Enum representing types of XML collections.
        /// </summary>
        public enum CollectionType
        {
            /// <summary>
            /// A choice type, corresponding to the XSD choice element.
            /// </summary>
            Choice,

            /// <summary>
            /// A sequence type, corresponding to the XSD sequence element.
            /// </summary>
            Sequence
        }

        /// <summary>
        /// Gets the type of collection.
        /// </summary>
        /// <value>The type of collection.</value>
        public CollectionType Type
        {
            get { return this.collectionType; }
        }

        /// <summary>
        /// Gets the count of child elements in this collection (counts ISchemaElements, not nested collections).
        /// </summary>
        /// <value>The count of child elements in this collection (counts ISchemaElements, not nested collections).</value>
        public int Count
        {
            get { return this.totalContainedItems; }
        }

        /// <summary>
        /// Gets the flag specifying whether this collection is synchronized. Always returns false.
        /// </summary>
        /// <value>The flag specifying whether this collection is synchronized. Always returns false.</value>
        public bool IsSynchronized
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an object external callers can synchronize on.
        /// </summary>
        /// <value>An object external callers can synchronize on.</value>
        public object SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Adds a child element to this collection.
        /// </summary>
        /// <param name="element">The element to add.</param>
        /// <exception cref="ArgumentException">Thrown if the child is not of an allowed type.</exception>
        public void AddElement(ISchemaElement element)
        {
            foreach (object obj in this.items)
            {
                bool containerUsed;

                CollectionItem collectionItem = obj as CollectionItem;
                if (collectionItem != null)
                {
                    containerUsed = collectionItem.Elements.Count != 0;
                    if (collectionItem.ElementType.IsAssignableFrom(element.GetType()))
                    {
                        collectionItem.AddElement(element);
                        
                        if (!containerUsed)
                        {
                            this.containersUsed++;
                        }

                        this.totalContainedItems++;
                        return;
                    }

                    continue;
                }

                ElementCollection collection = obj as ElementCollection;
                if (collection != null)
                {
                    containerUsed = collection.Count != 0;

                    try
                    {
                        collection.AddElement(element);

                        if (!containerUsed)
                        {
                            this.containersUsed++;
                        }

                        this.totalContainedItems++;
                        return;
                    }
                    catch (ArgumentException)
                    {
                        // Eat the exception and keep looking. We'll throw our own if we can't find its home.
                    }

                    continue;
                }
            }

            throw new ArgumentException(String.Format(
                CultureInfo.InvariantCulture,
                "Element of type {0} is not valid for this collection.",
                element.GetType().Name));
        }

        /// <summary>
        /// Removes a child element from this collection.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        /// <exception cref="ArgumentException">Thrown if the element is not of an allowed type.</exception>
        public void RemoveElement(ISchemaElement element)
        {
            foreach (object obj in this.items)
            {
                CollectionItem collectionItem = obj as CollectionItem;
                if (collectionItem != null)
                {
                    if (collectionItem.ElementType.IsAssignableFrom(element.GetType()))
                    {
                        if (collectionItem.Elements.Count == 0)
                        {
                            return;
                        }

                        collectionItem.RemoveElement(element);
                        
                        if (collectionItem.Elements.Count == 0)
                        {
                            this.containersUsed--;
                        }

                        this.totalContainedItems--;
                        return;
                    }

                    continue;
                }

                ElementCollection collection = obj as ElementCollection;
                if (collection != null)
                {
                    if (collection.Count == 0)
                    {
                        continue;
                    }

                    try
                    {
                        collection.RemoveElement(element);

                        if (collection.Count == 0)
                        {
                            this.containersUsed--;
                        }

                        this.totalContainedItems--;
                        return;
                    }
                    catch (ArgumentException)
                    {
                        // Eat the exception and keep looking. We'll throw our own if we can't find its home.
                    }

                    continue;
                }
            }

            throw new ArgumentException(String.Format(
                CultureInfo.InvariantCulture,
                "Element of type {0} is not valid for this collection.",
                element.GetType().Name));
        }

        /// <summary>
        /// Copies this collection to an array.
        /// </summary>
        /// <param name="array">Array to copy to.</param>
        /// <param name="index">Offset into the array.</param>
        public void CopyTo(Array array, int index)
        {
            int item = 0;
            foreach (ISchemaElement element in this)
            {
                array.SetValue(element, (long)(item + index));
                item++;
            }
        }

        /// <summary>
        /// Creates an enumerator for walking the elements in this collection.
        /// </summary>
        /// <returns>A newly created enumerator.</returns>
        public IEnumerator GetEnumerator()
        {
            return new ElementCollectionEnumerator(this);
        }

        /// <summary>
        /// Gets an enumerable collection of children of a given type.
        /// </summary>
        /// <param name="childType">Type of children to get.</param>
        /// <returns>A collection of children.</returns>
        /// <exception cref="ArgumentException">Thrown if the type isn't a valid child type.</exception>
        public IEnumerable Filter(Type childType)
        {
            foreach (object container in this.items)
            {
                CollectionItem collectionItem = container as CollectionItem;
                if (collectionItem != null)
                {
                    if (collectionItem.ElementType.IsAssignableFrom(childType))
                    {
                        return collectionItem.Elements;
                    }

                    continue;
                }

                ElementCollection elementCollection = container as ElementCollection;
                if (elementCollection != null)
                {
                    IEnumerable nestedFilter = elementCollection.Filter(childType);
                    if (nestedFilter != null)
                    {
                        return nestedFilter;
                    }
                    
                    continue;
                }
            }

            throw new ArgumentException(String.Format(
                CultureInfo.InvariantCulture,
                "Type {0} is not valid for this collection.",
                childType.Name));
        }

        /// <summary>
        /// Adds a type to this collection.
        /// </summary>
        /// <param name="collectionItem">CollectionItem representing the type to add.</param>
        internal void AddItem(CollectionItem collectionItem)
        {
            this.items.Add(collectionItem);
        }

        /// <summary>
        /// Adds a nested collection to this collection.
        /// </summary>
        /// <param name="collection">ElementCollection to add.</param>
        internal void AddCollection(ElementCollection collection)
        {
            this.items.Add(collection);
        }

        /// <summary>
        /// Class used to represent a given type in the child collection of an element. Abstract,
        /// has subclasses for choice and sequence (which can do cardinality checks).
        /// </summary>
        internal abstract class CollectionItem
        {
            private Type elementType;
            private ArrayList elements;

            /// <summary>
            /// Creates a new CollectionItem for the given element type.
            /// </summary>
            /// <param name="elementType">Type of the element for this collection item.</param>
            public CollectionItem(Type elementType)
            {
                this.elementType = elementType;
                this.elements = new ArrayList();
            }

            /// <summary>
            /// Gets the type of this collection's items.
            /// </summary>
            public Type ElementType
            {
                get { return this.elementType; }
            }

            /// <summary>
            /// Gets the elements of this collection.
            /// </summary>
            internal ArrayList Elements
            {
                get { return this.elements; }
            }

            /// <summary>
            /// Adds an element to this collection. Must be of an assignable type to the collection's
            /// type.
            /// </summary>
            /// <param name="element">The element to add.</param>
            /// <exception cref="ArgumentException">Thrown if the type isn't assignable to the collection's type.</exception>
            public void AddElement(ISchemaElement element)
            {
                if (!this.elementType.IsAssignableFrom(element.GetType()))
                {
                    throw new ArgumentException(
                        String.Format(
                            CultureInfo.InvariantCulture, 
                            "Element must be a subclass of {0}, but was of type {1}.", 
                            this.elementType.Name, 
                            element.GetType().Name), 
                        "element");
                }

                this.elements.Add(element);
            }

            /// <summary>
            /// Removes an element from this collection.
            /// </summary>
            /// <param name="element">The element to remove.</param>
            /// <exception cref="ArgumentException">Thrown if the element's type isn't assignable to the collection's type.</exception>
            public void RemoveElement(ISchemaElement element)
            {
                if (!this.elementType.IsAssignableFrom(element.GetType()))
                {
                    throw new ArgumentException(
                        String.Format(
                            CultureInfo.InvariantCulture, 
                            "Element must be a subclass of {0}, but was of type {1}.", 
                            this.elementType.Name, 
                            element.GetType().Name), 
                        "element");
                }

                this.elements.Remove(element);
            }
        }

        /// <summary>
        /// Class representing a choice item. Doesn't do cardinality checks.
        /// </summary>
        internal class ChoiceItem : CollectionItem
        {
            /// <summary>
            /// Creates a new choice item.
            /// </summary>
            /// <param name="elementType">Type of the created item.</param>
            public ChoiceItem(Type elementType) : base(elementType)
            {
            }
        }

        /// <summary>
        /// Class representing a sequence item. Can do cardinality checks, if required.
        /// </summary>
        internal class SequenceItem : CollectionItem
        {
            private int minimum = 1;
            private int maximum = 1;

            /// <summary>
            /// Creates a new sequence item.
            /// </summary>
            /// <param name="elementType">Type of the created item.</param>
            public SequenceItem(Type elementType) : base(elementType)
            {
            }

            /// <summary>
            /// Creates a new sequence item with the specified minimum and maximum.
            /// </summary>
            /// <param name="elementType">Type of the created item.</param>
            /// <param name="minimum">Minimum number of elements.</param>
            /// <param name="maximum">Maximum number of elements.</param>
            public SequenceItem(Type elementType, int minimum, int maximum) : base(elementType)
            {
                this.minimum = minimum;
                this.maximum = maximum;
            }
        }

        /// <summary>
        /// Enumerator for the ElementCollection.
        /// </summary>
        private class ElementCollectionEnumerator : IEnumerator
        {
            private ElementCollection collection;
            private Stack collectionStack;

            /// <summary>
            /// Creates a new ElementCollectionEnumerator.
            /// </summary>
            /// <param name="collection">The collection to create an enumerator for.</param>
            public ElementCollectionEnumerator(ElementCollection collection)
            {
                this.collection = collection;
            }

            /// <summary>
            /// Gets the current object from the enumerator.
            /// </summary>
            public object Current
            {
                get
                {
                    if (this.collectionStack != null && this.collectionStack.Count > 0)
                    {
                        CollectionTuple tuple = (CollectionTuple)this.collectionStack.Peek();
                        object container = tuple.Collection.items[tuple.ContainerIndex];
                        
                        CollectionItem collectionItem = container as CollectionItem;
                        if (collectionItem != null)
                        {
                            return collectionItem.Elements[tuple.ItemIndex];
                        }

                        throw new InvalidOperationException(String.Format(
                            CultureInfo.InvariantCulture,
                            "Element of type {0} found in enumerator. Must be ChoiceItem or SequenceItem.",
                            container.GetType().Name));
                    }

                    return null;
                }
            }

            /// <summary>
            /// Resets the enumerator to the beginning.
            /// </summary>
            public void Reset()
            {
                if (this.collectionStack != null)
                {
                    this.collectionStack.Clear();
                    this.collectionStack = null;
                }
            }

            /// <summary>
            /// Moves the enumerator to the next item.
            /// </summary>
            /// <returns>True if there is a next item, false otherwise.</returns>
            public bool MoveNext()
            {
                if (this.collectionStack == null)
                {
                    if (this.collection.Count == 0)
                    {
                        return false;
                    }

                    this.collectionStack = new Stack();
                    this.collectionStack.Push(new CollectionTuple(this.collection));
                }

                CollectionTuple tuple = (CollectionTuple)this.collectionStack.Peek();

                if (this.FindNext(tuple))
                {
                    return true;
                }

                this.collectionStack.Pop();
                if (this.collectionStack.Count == 0)
                {
                    return false;
                }

                return this.MoveNext();
            }

            /// <summary>
            /// Pushes a collection onto the stack.
            /// </summary>
            /// <param name="collection">The collection to push.</param>
            private void PushCollection(ElementCollection collection)
            {
                if (collection.Count <= 0)
                {
                    throw new ArgumentException(String.Format(
                        CultureInfo.InvariantCulture,
                        "Collection has {0} elements. Must have at least one.",
                        collection.Count));
                }

                CollectionTuple tuple = new CollectionTuple(collection);
                this.collectionStack.Push(tuple);
                this.FindNext(tuple);
            }

            /// <summary>
            /// Finds the next item from a given tuple.
            /// </summary>
            /// <param name="tuple">The tuple to start looking from.</param>
            /// <returns>True if a next element is found, false otherwise.</returns>
            private bool FindNext(CollectionTuple tuple)
            {
                object container = tuple.Collection.items[tuple.ContainerIndex];
                        
                CollectionItem collectionItem = container as CollectionItem;
                if (collectionItem != null)
                {
                    if (tuple.ItemIndex + 1 < collectionItem.Elements.Count)
                    {
                        tuple.ItemIndex++;
                        return true;
                    }
                }

                ElementCollection elementCollection = container as ElementCollection;
                if (elementCollection != null && elementCollection.Count > 0 && tuple.ItemIndex == -1)
                {
                    tuple.ItemIndex++;
                    this.PushCollection(elementCollection);
                    return true;
                }

                tuple.ItemIndex = 0;

                for (int i = tuple.ContainerIndex + 1; i < tuple.Collection.items.Count; ++i)
                {
                    object nestedContainer = tuple.Collection.items[i];
                        
                    CollectionItem nestedCollectionItem = nestedContainer as CollectionItem;
                    if (nestedCollectionItem != null)
                    {
                        if (nestedCollectionItem.Elements.Count > 0)
                        {
                            tuple.ContainerIndex = i;
                            return true;
                        }
                    }

                    ElementCollection nestedElementCollection = nestedContainer as ElementCollection;
                    if (nestedElementCollection != null && nestedElementCollection.Count > 0)
                    {
                        tuple.ContainerIndex = i;
                        this.PushCollection(nestedElementCollection);
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Class representing a single point in the collection. Consists of an ElementCollection,
            /// a container index, and an index into the container.
            /// </summary>
            private class CollectionTuple
            {
                private ElementCollection collection;
                private int containerIndex;
                private int itemIndex = -1;

                /// <summary>
                /// Creates a new CollectionTuple.
                /// </summary>
                /// <param name="collection">The collection for the tuple.</param>
                public CollectionTuple(ElementCollection collection)
                {
                    this.collection = collection;
                }

                /// <summary>
                /// Gets the collection for the tuple.
                /// </summary>
                public ElementCollection Collection
                {
                    get { return this.collection; }
                }

                /// <summary>
                /// Gets and sets the index of the container in the collection.
                /// </summary>
                public int ContainerIndex
                {
                    get { return this.containerIndex; }
                    set { this.containerIndex = value; }
                }

                /// <summary>
                /// Gets and sets the index of the item in the container.
                /// </summary>
                public int ItemIndex
                {
                    get { return this.itemIndex; }
                    set { this.itemIndex = value; }
                }
            }
        }
    }
}
