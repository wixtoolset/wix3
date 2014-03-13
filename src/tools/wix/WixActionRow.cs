//-------------------------------------------------------------------------------------------------
// <copyright file="WixActionRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the sequence tables.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Schema;

    using Microsoft.Tools.WindowsInstallerXml.Msi;

    /// <summary>
    /// The Sequence tables that actions may belong to.
    /// </summary>
    public enum SequenceTable
    {
        /// <summary>AdminUISequence</summary>
        AdminUISequence,

        /// <summary>AdminExecuteSequence</summary>
        AdminExecuteSequence,

        /// <summary>AdvtExecuteSequence</summary>
        AdvtExecuteSequence,

        /// <summary>InstallUISequence</summary>
        InstallUISequence,

        /// <summary>InstallExecuteSequence</summary>
        InstallExecuteSequence
    }

    /// <summary>
    /// Specialization of a row for the sequence tables.
    /// </summary>
    public sealed class WixActionRow : Row, IComparable
    {
        private WixActionRowCollection previousActionRows;
        private WixActionRowCollection nextActionRows;

        /// <summary>
        /// Instantiates an ActionRow that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Action row belongs to and should get its column definitions from.</param>
        public WixActionRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Instantiates a standard ActionRow.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the standard action.</param>
        /// <param name="action">The name of the standard action.</param>
        /// <param name="condition">The condition of the standard action.</param>
        /// <param name="sequence">The suggested sequence number of the standard action.</param>
        private WixActionRow(SequenceTable sequenceTable, string action, string condition, int sequence) :
            base(null, Installer.GetTableDefinitions()["WixAction"])
        {
            this.SequenceTable = sequenceTable;
            this.Action = action;
            this.Condition = condition;
            this.Sequence = sequence;
            this.Overridable = true; // all standard actions are overridable by default
        }

        /// <summary>
        /// Instantiates an ActionRow by copying data from another ActionRow.
        /// </summary>
        /// <param name="source">The row the data is copied from.</param>
        /// <remarks>The previous and next action collections are not copied.</remarks>
        private WixActionRow(WixActionRow source)
            : base(source)
        {
        }

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        /// <value>The name of the action.</value>
        public string Action
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        /// <summary>
        /// Gets the name of the action this action should be scheduled after.
        /// </summary>
        /// <value>The name of the action this action should be scheduled after.</value>
        public string After
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        /// <summary>
        /// Gets the name of the action this action should be scheduled before.
        /// </summary>
        /// <value>The name of the action this action should be scheduled before.</value>
        public string Before
        {
            get { return (string)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        /// <summary>
        /// Gets or sets the condition of the action.
        /// </summary>
        /// <value>The condition of the action.</value>
        public string Condition
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets or sets whether this action is overridable.
        /// </summary>
        /// <value>Whether this action is overridable.</value>
        public bool Overridable
        {
            get { return (1 == Convert.ToInt32(this.Fields[6].Data, CultureInfo.InvariantCulture)); }
            set { this.Fields[6].Data = (value ? 1 : 0); }
        }

        /// <summary>
        /// Gets or sets the sequence number of this action.
        /// </summary>
        /// <value>The sequence number of this action.</value>
        public int Sequence
        {
            get { return Convert.ToInt32(this.Fields[3].Data, CultureInfo.InvariantCulture); }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets of sets the sequence table of this action.
        /// </summary>
        /// <value>The sequence table of this action.</value>
        public SequenceTable SequenceTable
        {
            get { return (SequenceTable)Enum.Parse(typeof(SequenceTable), (string)this.Fields[0].Data); }
            set { this.Fields[0].Data = value.ToString(); }
        }

        /// <summary>
        /// Gets the actions that should be scheduled after this action.
        /// </summary>
        /// <value>The actions that should be scheduled after this action.</value>
        internal WixActionRowCollection NextActionRows
        {
            get
            {
                if (null == this.nextActionRows)
                {
                    this.nextActionRows = new WixActionRowCollection();
                }

                return this.nextActionRows;
            }
        }

        /// <summary>
        /// Gets the actions that should be scheduled before this action.
        /// </summary>
        /// <value>The actions that should be scheduled before this action.</value>
        internal WixActionRowCollection PreviousActionRows
        {
            get
            {
                if (null == this.previousActionRows)
                {
                    this.previousActionRows = new WixActionRowCollection();
                }

                return this.previousActionRows;
            }
        }

        /// <summary>
        /// Creates a clone of the action row.
        /// </summary>
        /// <returns>A shallow copy of the source object.</returns>
        /// <remarks>The previous and next action collections are not copied.</remarks>
        public WixActionRow Clone()
        {
            return new WixActionRow(this);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type.
        /// </summary>
        /// <param name="obj">Other reference to compare this one to.</param>
        /// <returns>Returns less than 0 for less than, 0 for equals, and greater than 0 for greater.</returns>
        public int CompareTo(object obj)
        {
            WixActionRow otherActionRow = (WixActionRow)obj;

            return this.Sequence.CompareTo(otherActionRow.Sequence);
        }

        /// <summary>
        /// Parses ActionRows from the Xml reader.
        /// </summary>
        /// <param name="reader">Xml reader that contains serialized ActionRows.</param>
        /// <returns>The parsed ActionRows.</returns>
        internal static WixActionRow[] Parse(XmlReader reader)
        {
            Debug.Assert("action" == reader.LocalName);

            string id = null;
            string condition = null;
            bool empty = reader.IsEmptyElement;
            int sequence = int.MinValue;
            int sequenceCount = 0;
            SequenceTable[] sequenceTables = new SequenceTable[Enum.GetValues(typeof(SequenceTable)).Length];

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "name":
                        id = reader.Value;
                        break;
                    case "AdminExecuteSequence":
                        if (Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name, reader.Value))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdminExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "AdminUISequence":
                        if (Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name, reader.Value))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdminUISequence;
                            ++sequenceCount;
                        }
                        break;
                    case "AdvtExecuteSequence":
                        if (Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name, reader.Value))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdvtExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "condition":
                        condition = reader.Value;
                        break;
                    case "InstallExecuteSequence":
                        if (Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name, reader.Value))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.InstallExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "InstallUISequence":
                        if (Common.IsYes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name, reader.Value))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.InstallUISequence;
                            ++sequenceCount;
                        }
                        break;
                    case "sequence":
                        sequence = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    default:
                        if (!reader.NamespaceURI.StartsWith("http://www.w3.org/", StringComparison.Ordinal))
                        {
                            throw new WixException(WixErrors.UnexpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.Name));
                        }
                        break;
                }
            }

            if (null == id)
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", "name"));
            }

            if (int.MinValue == sequence)
            {
                throw new WixException(WixErrors.ExpectedAttribute(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", "sequence"));
            }
            else if (1 > sequence)
            {
                throw new WixException(WixErrors.IntegralValueOutOfRange(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", "sequence", sequence, 1, int.MaxValue));
            }

            if (0 == sequenceCount)
            {
                throw new WixException(WixErrors.ExpectedAttributes(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", "AdminExecuteSequence", "AdminUISequence", "AdvtExecuteSequence", "InstallExecuteSequence", "InstallUISequence"));
            }

            if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
            {
                throw new WixException(WixErrors.UnexpectedContentNode(SourceLineNumberCollection.FromUri(reader.BaseURI), "action", reader.NodeType.ToString()));
            }

            // create the actions
            WixActionRow[] actionRows = new WixActionRow[sequenceCount];
            for (int i = 0; i < sequenceCount; i++)
            {
                WixActionRow actionRow = new WixActionRow(sequenceTables[i], id, condition, sequence);
                actionRows[i] = actionRow;
            }

            return actionRows;
        }

        /// <summary>
        /// Determines whether this ActionRow contains the specified ActionRow as a child in its dependency tree.
        /// </summary>
        /// <param name="actionRow">The possible child ActionRow.</param>
        /// <returns>true if the ActionRow is a child of this ActionRow; false otherwise.</returns>
        internal bool ContainsChildActionRow(WixActionRow actionRow)
        {
            if (null != this.previousActionRows)
            {
                if (this.previousActionRows.Contains(actionRow.SequenceTable, actionRow.Action))
                {
                    return true;
                }
            }

            if (null != this.nextActionRows)
            {
                if (this.nextActionRows.Contains(actionRow.SequenceTable, actionRow.Action))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get all the actions scheduled before this one in a particular sequence table.
        /// </summary>
        /// <param name="sequenceTable">The sequence table.</param>
        /// <param name="allPreviousActionRows">A RowCollection which will contain all the previous actions.</param>
        internal void GetAllPreviousActionRows(SequenceTable sequenceTable, RowCollection allPreviousActionRows)
        {
            if (null != this.previousActionRows)
            {
                foreach (WixActionRow actionRow in this.previousActionRows)
                {
                    if (sequenceTable == actionRow.SequenceTable)
                    {
                        actionRow.GetAllPreviousActionRows(sequenceTable, allPreviousActionRows);
                        allPreviousActionRows.Add(actionRow);
                        actionRow.GetAllNextActionRows(sequenceTable, allPreviousActionRows);
                    }
                }
            }
        }

        /// <summary>
        /// Get all the actions scheduled after this one in a particular sequence table.
        /// </summary>
        /// <param name="sequenceTable">The sequence table.</param>
        /// <param name="allNextActionRows">A RowCollection which will contain all the next actions.</param>
        internal void GetAllNextActionRows(SequenceTable sequenceTable, RowCollection allNextActionRows)
        {
            if (null != this.nextActionRows)
            {
                foreach (WixActionRow actionRow in this.nextActionRows)
                {
                    if (sequenceTable == actionRow.SequenceTable)
                    {
                        actionRow.GetAllPreviousActionRows(sequenceTable, allNextActionRows);
                        allNextActionRows.Add(actionRow);
                        actionRow.GetAllNextActionRows(sequenceTable, allNextActionRows);
                    }
                }
            }
        }
    }
}
