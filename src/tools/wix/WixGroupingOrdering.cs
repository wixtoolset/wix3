// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Grouping and Ordering class of the Windows Installer Xml toolset.
    /// </summary>
    public sealed class WixGroupingOrdering : IMessageHandler
    {
        private Output output;
        private IMessageHandler messageHandler;
        private List<string> groupTypes;
        private List<string> itemTypes;
        private ItemCollection items;
        private List<int> rowsUsed;
        private bool loaded;
        private bool encounteredError;

        /// <summary>
        /// Creates a WixGroupingOrdering object.
        /// </summary>
        /// <param name="output">Output from which to read the group and order information.</param>
        /// <param name="messageHandler">Handler for any error messages.</param>
        /// <param name="groupTypes">Group types to include.</param>
        /// <param name="itemTypes">Item types to include.</param>
        public WixGroupingOrdering(Output output, IMessageHandler messageHandler)
        {
            this.output = output;
            this.messageHandler = messageHandler;

            this.rowsUsed = new List<int>();
            this.loaded = false;
            this.encounteredError = false;
        }

        /// <summary>
        /// Switches a WixGroupingOrdering object to operate on a new set of groups/items.
        /// </summary>
        /// <param name="groupTypes">Group types to include.</param>
        /// <param name="itemTypes">Item types to include.</param>
        public void UseTypes(IEnumerable<string> groupTypes, IEnumerable<string> itemTypes)
        {
            this.groupTypes = new List<string>(groupTypes);
            this.itemTypes = new List<string>(itemTypes);

            this.items = new ItemCollection();
            this.loaded = false;
        }

        /// <summary>
        /// Sends a message to the message handler if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs || MessageLevel.Error == e.Level)
            {
                this.encounteredError = true;
            }

            if (null != this.messageHandler)
            {
                this.messageHandler.OnMessage(e);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }

        /// <summary>
        /// Finds all nested items under a parent group and creates new WixGroup data for them.
        /// </summary>
        /// <param name="parentType">The group type for the parent group to flatten.</param>
        /// <param name="parentId">The identifier of the parent group to flatten.</param>
        /// <param name="removeUsedRows">Whether to remove used group rows before returning.</param>
        public void FlattenAndRewriteRows(string parentType, string parentId, bool removeUsedRows)
        {
            Debug.Assert(this.groupTypes.Contains(parentType));

            List<Item> orderedItems;
            this.CreateOrderedList(parentType, parentId, out orderedItems);
            if (this.encounteredError)
            {
                return;
            }

            this.CreateNewGroupRows(parentType, parentId, orderedItems);

            if (removeUsedRows)
            {
                this.RemoveUsedGroupRows();
            }
        }

        /// <summary>
        /// Finds all items under a parent group type and creates new WixGroup data for them.
        /// </summary>
        /// <param name="parentType">The type of the parent group to flatten.</param>
        /// <param name="removeUsedRows">Whether to remove used group rows before returning.</param>
        public void FlattenAndRewriteGroups(string parentType, bool removeUsedRows)
        {
            Debug.Assert(this.groupTypes.Contains(parentType));

            this.LoadFlattenOrderGroups();
            if (this.encounteredError)
            {
                return;
            }

            foreach (Item item in this.items)
            {
                if (parentType == item.Type)
                {
                    List<Item> orderedItems;
                    this.CreateOrderedList(item.Type, item.Id, out orderedItems);
                    this.CreateNewGroupRows(item.Type, item.Id, orderedItems);
                }
            }

            if (removeUsedRows)
            {
                this.RemoveUsedGroupRows();
            }
        }


        /// <summary>
        /// Creates a flattened and ordered list of items for the given parent group.
        /// </summary>
        /// <param name="parentType">The group type for the parent group to flatten.</param>
        /// <param name="parentId">The identifier of the parent group to flatten.</param>
        /// <param name="orderedItems">The returned list of ordered items.</param>
        private void CreateOrderedList(string parentType, string parentId, out List<Item> orderedItems)
        {
            orderedItems = null;

            this.LoadFlattenOrderGroups();
            if (this.encounteredError)
            {
                return;
            }

            Item parentItem;
            if (!this.items.TryGetValue(parentType, parentId, out parentItem))
            {
                this.OnMessage(WixErrors.IdentifierNotFound(parentType, parentId));
                return;
            }

            orderedItems = new List<Item>(parentItem.ChildItems);
            orderedItems.Sort(new Item.AfterItemComparer());
        }

        /// <summary>
        /// Removes rows from WixGroup that have been used by this object.
        /// </summary>
        public void RemoveUsedGroupRows()
        {
            // Ensure we have a list of unique IDs, sorted in descending order...
            Dictionary<int, bool> uniqueRowsUsed = new Dictionary<int, bool>(this.rowsUsed.Count);
            foreach (int rowIndex in this.rowsUsed)
            {
                uniqueRowsUsed[rowIndex] = true;
            }

            List<int> sortedIndexes = new List<int>(uniqueRowsUsed.Keys);
            sortedIndexes.Sort();
            sortedIndexes.Reverse();

            Table wixGroupTable = this.output.Tables["WixGroup"];
            Debug.Assert(null != wixGroupTable);
            Debug.Assert(sortedIndexes[0] < wixGroupTable.Rows.Count);

            foreach (int rowIndex in sortedIndexes)
            {
                wixGroupTable.Rows.RemoveAt(rowIndex);
            }
        }

        /// <summary>
        /// Creates new WixGroup rows for a list of items.
        /// </summary>
        /// <param name="parentType">The group type for the parent group in the new rows.</param>
        /// <param name="parentId">The identifier of the parent group in the new rows.</param>
        /// <param name="orderedItems">The list of new items.</param>
        private void CreateNewGroupRows(string parentType, string parentId, List<Item> orderedItems)
        {
            // TODO: MSIs don't guarantee that rows stay in the same order, and technically, neither
            // does WiX (although they do, currently). We probably want to "upgrade" this to a new
            // table that includes a sequence number, and then change the code that uses ordered
            // groups to read from that table instead.
            Table wixGroupTable = this.output.Tables["WixGroup"];
            Debug.Assert(null != wixGroupTable);

            foreach (Item item in orderedItems)
            {
                Row row = wixGroupTable.CreateRow(item.Row.SourceLineNumbers);
                row[0] = parentId;
                row[1] = parentType;
                row[2] = item.Id;
                row[3] = item.Type;
            }
        }

        // Group/Ordering Flattening Logic
        //
        // What follows is potentially convoluted logic. Two somewhat orthogonal concepts are in
        // play: grouping (parent/child relationships) and ordering (before/after relationships).
        // Dealing with just one or the other is straghtforward. Groups can be flattened
        // recursively. Ordering can be propagated in either direction. When the ordering also
        // participates in the grouping constructions, however, things get trickier.  For the
        // purposes of this discussion, we're dealing with "items" and "groups", and an instance
        // of either of them can be marked as coming "after" some other instance.
        //
        // For simple item-to-item ordering, the "after" values simply propagate: if A is after B,
        // and B is after C, then we can say that A is after *both* B and C.  If a group is involved,
        // it acts as a proxy for all of its included items and any sub-groups.

        /// <summary>
        /// Internal workhorse for ensuring that group and ordering information has
        /// been loaded and applied.
        /// </summary>
        private void LoadFlattenOrderGroups()
        {
            if (!this.loaded)
            {
                this.LoadGroups();
                this.LoadOrdering();

                // It would be really nice to have a "find circular after dependencies"
                // function, but it gets much more complicated because of the way that
                // the dependencies are propagated across group boundaries. For now, we
                // just live with the dependency loop detection as we flatten the
                // dependencies. Group references, however, we can check directly.
                this.FindCircularGroupReferences();

                if (!this.encounteredError)
                {
                    this.FlattenGroups();
                    this.FlattenOrdering();
                }

                this.loaded = true;
            }
        }

        /// <summary>
        /// Loads data from the WixGroup table.
        /// </summary>
        private void LoadGroups()
        {
            Table wixGroupTable = this.output.Tables["WixGroup"];
            if (null == wixGroupTable || 0 == wixGroupTable.Rows.Count)
            {
                // TODO: Change message name to make it *not* Bundle specific?
                this.OnMessage(WixErrors.MissingBundleInformation("WixGroup"));
            }

            // Collect all of the groups
            for (int rowIndex = 0; rowIndex < wixGroupTable.Rows.Count; ++rowIndex)
            {
                Row row = wixGroupTable.Rows[rowIndex];
                string rowParentName = (string)row[0];
                string rowParentType = (string)row[1];
                string rowChildName = (string)row[2];
                string rowChildType = (string)row[3];

                // If this row specifies a parent or child type that's not in our
                // lists, we assume it's not a row that we're concerned about.
                if (!this.groupTypes.Contains(rowParentType) ||
                    !this.itemTypes.Contains(rowChildType))
                {
                    continue;
                }

                this.rowsUsed.Add(rowIndex);

                Item parentItem;
                if (!this.items.TryGetValue(rowParentType, rowParentName, out parentItem))
                {
                    parentItem = new Item(row, rowParentType, rowParentName);
                    this.items.Add(parentItem);
                }

                Item childItem;
                if (!this.items.TryGetValue(rowChildType, rowChildName, out childItem))
                {
                    childItem = new Item(row, rowChildType, rowChildName);
                    this.items.Add(childItem);
                }

                parentItem.ChildItems.Add(childItem);
            }
        }

        /// <summary>
        /// Flattens group/item information.
        /// </summary>
        private void FlattenGroups()
        {
            foreach (Item item in this.items)
            {
                item.FlattenChildItems();
            }
        }

        /// <summary>
        /// Finds and reports circular references in the group/item data.
        /// </summary>
        private void FindCircularGroupReferences()
        {
            ItemCollection itemsInKnownLoops = new ItemCollection();
            foreach (Item item in this.items)
            {
                if (itemsInKnownLoops.Contains(item))
                {
                    continue;
                }

                ItemCollection itemsSeen = new ItemCollection();
                string circularReference;
                if (this.FindCircularGroupReference(item, item, itemsSeen, out circularReference))
                {
                    itemsInKnownLoops.Add(itemsSeen);
                    this.OnMessage(WixErrors.ReferenceLoopDetected(item.Row.SourceLineNumbers, circularReference));
                }
            }
        }

        /// <summary>
        /// Recursive worker to find and report circular references in group/item data.
        /// </summary>
        /// <param name="checkItem">The sentinal item being checked.</param>
        /// <param name="currentItem">The current item in the recursion.</param>
        /// <param name="itemsSeen">A list of all items already visited (for performance).</param>
        /// <param name="circularReference">A list of items in the current circular reference, if one was found; null otherwise.</param>
        /// <returns>True if a circular reference was found; false otherwise.</returns>
        private bool FindCircularGroupReference(Item checkItem, Item currentItem, ItemCollection itemsSeen, out string circularReference)
        {
            circularReference = null;
            foreach (Item subitem in currentItem.ChildItems)
            {
                if (checkItem == subitem)
                {
                    // TODO: Even better would be to include the source lines for each reference!
                    circularReference = String.Format(CultureInfo.InvariantCulture, "{0}:{1} -> {2}:{3}",
                        currentItem.Type, currentItem.Id, subitem.Type, subitem.Id);
                    return true;
                }

                if (!itemsSeen.Contains(subitem))
                {
                    itemsSeen.Add(subitem);
                    if (this.FindCircularGroupReference(checkItem, subitem, itemsSeen, out circularReference))
                    {
                        // TODO: Even better would be to include the source lines for each reference!
                        circularReference = String.Format(CultureInfo.InvariantCulture, "{0}:{1} -> {2}",
                            currentItem.Type, currentItem.Id, circularReference);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Loads ordering dependency data from the WixOrdering table.
        /// </summary>
        private void LoadOrdering()
        {
            Table wixOrderingTable = output.Tables["WixOrdering"];
            if (null == wixOrderingTable || 0 == wixOrderingTable.Rows.Count)
            {
                // TODO: Do we need a message here?
                return;
            }

            foreach (Row row in wixOrderingTable.Rows)
            {
                string rowItemType = (string)row[0];
                string rowItemName = (string)row[1];
                string rowDependsOnType = (string)row[2];
                string rowDependsOnName = (string)row[3];

                // If this row specifies some other (unknown) type in either
                // position, we assume it's not a row that we're concerned about.
                // For ordering, we allow group and item in either position.
                if (!(this.groupTypes.Contains(rowItemType) || this.itemTypes.Contains(rowItemType)) ||
                    !(this.groupTypes.Contains(rowDependsOnType) || this.itemTypes.Contains(rowDependsOnType)))
                {
                    continue;
                }

                Item item = null;
                Item dependsOn = null;

                if (!this.items.TryGetValue(rowItemType, rowItemName, out item))
                {
                    this.OnMessage(WixErrors.IdentifierNotFound(rowItemType, rowItemName));
                }

                if (!this.items.TryGetValue(rowDependsOnType, rowDependsOnName, out dependsOn))
                {
                    this.OnMessage(WixErrors.IdentifierNotFound(rowDependsOnType, rowDependsOnName));
                }

                if (null == item || null == dependsOn)
                {
                    continue;
                }

                item.AddAfter(dependsOn, this);
            }
        }

        /// <summary>
        /// Flattens the ordering dependencies in the groups/items.
        /// </summary>
        private void FlattenOrdering()
        {
            // Because items don't know about their parent groups (and can, in fact, be
            // in more than one group at a time), we need to pre-propagate the 'afters'
            // from each parent item to its children before we attempt to flatten the
            // ordering.
            foreach (Item item in this.items)
            {
                item.PropagateAfterToChildItems(this);
            }

            foreach (Item item in this.items)
            {
                item.FlattenAfters(this);
            }
        }

        /// <summary>
        /// A variant of KeyedCollection that doesn't throw when an item is re-added.
        /// </summary>
        /// <typeparam name="TKey">Key type for the collection.</typeparam>
        /// <typeparam name="TItem">Item type for the colelction.</typeparam>
        internal abstract class EnhancedKeyCollection<TKey, TItem> : KeyedCollection<TKey, TItem>
        {
            new public void Add(TItem item)
            {
                if (!this.Contains(item))
                {
                    base.Add(item);
                }
            }

            public void Add(Collection<TItem> list)
            {
                foreach (TItem item in list)
                {
                    this.Add(item);
                }
            }

            public void Remove(Collection<TItem> list)
            {
                foreach (TItem item in list)
                {
                    this.Remove(item);
                }
            }

            public bool TryGetValue(TKey key, out TItem item)
            {
                // KeyedCollection doesn't implement the TryGetValue() method, but it's
                // a useful concept.  We can't just always pass this to the enclosed
                // Dictionary, however, because it doesn't always exist! If it does, we
                // can delegate to it as one would expect. If it doesn't, we have to
                // implement everything ourselves in terms of Contains().

                if (null != this.Dictionary)
                {
                    return this.Dictionary.TryGetValue(key, out item);
                }

                if (this.Contains(key))
                {
                    item = this[key];
                    return true;
                }

                item = default(TItem);
                return false;
            }

#if DEBUG
            // This just makes debugging easier...
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (TItem item in this)
                {
                    sb.AppendFormat("{0}, ", item);
                }
                sb.Length -= 2;
                return sb.ToString();
            }
#endif // DEBUG
        }

        /// <summary>
        /// A specialized EnhancedKeyCollection, typed to Items.
        /// </summary>
        internal class ItemCollection : EnhancedKeyCollection<string, Item>
        {
            protected override string GetKeyForItem(Item item)
            {
                return item.Key;
            }

            public bool TryGetValue(string type, string id, out Item item)
            {
                return this.TryGetValue(CreateKeyFromTypeId(type, id), out item);
            }

            public static string CreateKeyFromTypeId(string type, string id)
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}_{1}", type, id);
            }
        }

        /// <summary>
        /// An item (or group) in the grouping/ordering engine.
        /// </summary>
        /// <remarks>Encapsulates nested group membership and also before/after
        /// ordering dependencies.</remarks>
        internal class Item
        {
            private ItemCollection afterItems;
            private ItemCollection beforeItems; // for checking for circular references
            private bool flattenedAfterItems;

            public Item(Row row, string type, string id)
            {
                this.Row = row;
                this.Type = type;
                this.Id = id;

                this.Key = ItemCollection.CreateKeyFromTypeId(type, id);

                afterItems = new ItemCollection();
                beforeItems = new ItemCollection();
                flattenedAfterItems = false;
            }

            public Row Row { get; private set; }
            public string Type { get; private set; }
            public string Id { get; private set; }
            public string Key { get; private set; }

#if DEBUG
            // Makes debugging easier...
            public override string ToString()
            {
                return this.Key;
            }
#endif // DEBUG

            private ItemCollection childItems = new ItemCollection();
            public ItemCollection ChildItems { get { return childItems; } }

            /// <summary>
            /// Removes any nested groups under this item and replaces
            /// them with their child items.
            /// </summary>
            public void FlattenChildItems()
            {
                ItemCollection flattenedChildItems = new ItemCollection();

                foreach (Item childItem in this.ChildItems)
                {
                    if (0 == childItem.ChildItems.Count)
                    {
                        flattenedChildItems.Add(childItem);
                    }
                    else
                    {
                        childItem.FlattenChildItems();
                        flattenedChildItems.Add(childItem.ChildItems);
                    }
                }

                this.ChildItems.Clear();
                this.ChildItems.Add(flattenedChildItems);
            }

            /// <summary>
            /// Adds a list of items to the 'after' ordering collection.
            /// </summary>
            /// <param name="items">List of items to add.</param>
            /// <param name="messageHandler">Message handler in case a circular ordering reference is found.</param>
            public void AddAfter(ItemCollection items, IMessageHandler messageHandler)
            {
                foreach (Item item in items)
                {
                    this.AddAfter(item, messageHandler);
                }
            }

            /// <summary>
            /// Adds an item to the 'after' ordering collection.
            /// </summary>
            /// <param name="item">Items to add.</param>
            /// <param name="messageHandler">Message handler in case a circular ordering reference is found.</param>
            public void AddAfter(Item after, IMessageHandler messageHandler)
            {
                if (this.beforeItems.Contains(after))
                {
                    // We could try to chain this up (the way that group circular dependencies
                    // are reported), but since we're in the process of flattening, we may already
                    // have lost some distinction between authored and propagated ordering.
                    string circularReference = String.Format(CultureInfo.InvariantCulture, "{0}:{1} -> {2}:{3} -> {0}:{1}",
                        this.Type, this.Id, after.Type, after.Id);
                    messageHandler.OnMessage(WixErrors.OrderingReferenceLoopDetected(after.Row.SourceLineNumbers, circularReference));
                    return;
                }

                this.afterItems.Add(after);
                after.beforeItems.Add(this);
            }

            /// <summary>
            /// Propagates 'after' dependencies from an item to its child items.
            /// </summary>
            /// <param name="messageHandler">Message handler in case a circular ordering reference is found.</param>
            /// <remarks>Because items don't know about their parent groups (and can, in fact, be in more
            /// than one group at a time), we need to propagate the 'afters' from each parent item to its children
            /// before we attempt to flatten the ordering.</remarks>
            public void PropagateAfterToChildItems(IMessageHandler messageHandler)
            {
                if (this.ShouldItemPropagateChildOrdering())
                {
                    foreach (Item childItem in this.childItems)
                    {
                        childItem.AddAfter(this.afterItems, messageHandler);
                    }
                }
            }

            /// <summary>
            /// Flattens the ordering dependency for this item.
            /// </summary>
            /// <param name="messageHandler">Message handler in case a circular ordering reference is found.</param>
            public void FlattenAfters(IMessageHandler messageHandler)
            {
                if (this.flattenedAfterItems)
                {
                    return;
                }

                this.flattenedAfterItems = true;

                // Ensure that if we're after something (A), and *it's* after something (B),
                // that we list ourselved as after both (A) *and* (B).
                ItemCollection nestedAfterItems = new ItemCollection();

                foreach (Item afterItem in this.afterItems)
                {
                    afterItem.FlattenAfters(messageHandler);
                    nestedAfterItems.Add(afterItem.afterItems);

                    if (afterItem.ShouldItemPropagateChildOrdering())
                    {
                        // If we are after a group, it really means
                        // we are after all of the group's children.
                        foreach (Item childItem in afterItem.ChildItems)
                        {
                            childItem.FlattenAfters(messageHandler);
                            nestedAfterItems.Add(childItem.afterItems);
                            nestedAfterItems.Add(childItem);
                        }
                    }
                }

                this.AddAfter(nestedAfterItems, messageHandler);
            }

            // We *don't* propagate ordering information from Packages or
            // Containers to their children, because ordering doesn't matter
            // for them, and a Payload in two Packages (or Containers) can
            // cause a circular reference to occur.  We do, however, need to
            // track the ordering in the UX Container, because we need the
            // first payload to be the entrypoint.
            private bool ShouldItemPropagateChildOrdering()
            {
                if (String.Equals("Package", this.Type, StringComparison.Ordinal) ||
                    (String.Equals("Container", this.Type, StringComparison.Ordinal) &&
                    !String.Equals(Compiler.BurnUXContainerId, this.Id, StringComparison.Ordinal)))
                {
                    return false;
                }
                return true;
            }

            /// <summary>
            /// Helper IComparer class to make ordering easier.
            /// </summary>
            internal sealed class AfterItemComparer : IComparer<Item>
            {
                public int Compare(Item x, Item y)
                {
                    if (x.afterItems.Contains(y))
                    {
                        return 1;
                    }
                    else if (y.afterItems.Contains(x))
                    {
                        return -1;
                    }

                    return string.CompareOrdinal(x.Id, y.Id);
                }
            }
        }
    }
}
