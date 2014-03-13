//-------------------------------------------------------------------------------------------------
// <copyright file="WixComplexReferenceRow.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Specialization of a row for the WixComplexReference table.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;

    /// <summary>
    /// Types of parents in complex reference.
    /// </summary>
    public enum ComplexReferenceParentType
    {
        /// <summary>Unknown complex reference type, default and invalid.</summary>
        Unknown,

        /// <summary>Feature parent of complex reference.</summary>
        Feature,

        /// <summary>ComponentGroup parent of complex reference.</summary>
        ComponentGroup,

        /// <summary>FeatureGroup parent of complex reference.</summary>
        FeatureGroup,

        /// <summary>Module parent of complex reference.</summary>
        Module,

        /// <summary>Product parent of complex reference.</summary>
        Product,

        /// <summary>PayloadGroup parent of complex reference.</summary>
        PayloadGroup,

        /// <summary>Package parent of complex reference.</summary>
        Package,

        /// <summary>PackageGroup parent of complex reference.</summary>
        PackageGroup,

        /// <summary>Container parent of complex reference.</summary>
        Container,

        /// <summary>Layout parent of complex reference.</summary>
        Layout,

        /// <summary>Patch parent of complex reference.</summary>
        Patch,

        /// <summary>PatchFamilyGroup parent of complex reference.</summary>
        PatchFamilyGroup,
    }

    /// <summary>
    /// Types of children in complex refernece.
    /// </summary>
    public enum ComplexReferenceChildType
    {
        /// <summary>Unknown complex reference type, default and invalid.</summary>
        Unknown,

        /// <summary>Component child of complex reference.</summary>
        Component,

        /// <summary>Feature child of complex reference.</summary>
        Feature,

        /// <summary>ComponentGroup child of complex reference.</summary>
        ComponentGroup,

        /// <summary>FeatureGroup child of complex reference.</summary>
        FeatureGroup,

        /// <summary>Module child of complex reference.</summary>
        Module,

        /// <summary>Payload child of complex reference.</summary>
        Payload,

        /// <summary>PayloadGroup child of complex reference.</summary>
        PayloadGroup,

        /// <summary>Package child of complex reference.</summary>
        Package,

        /// <summary>PackageGroup child of complex reference.</summary>
        PackageGroup,

        /// <summary>PatchFamily child of complex reference.</summary>
        PatchFamily,

        /// <summary>PatchFamilyGroup child of complex reference.</summary>
        PatchFamilyGroup,
    }

    /// <summary>
    /// Specialization of a row for the WixComplexReference table.
    /// </summary>
    public sealed class WixComplexReferenceRow : Row, IComparable
    {
        /// <summary>
        /// Creates a WixComplexReferenceRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this row belongs to and should get its column definitions from.</param>
        public WixComplexReferenceRow(SourceLineNumberCollection sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        /// <summary>
        /// Gets the parent type of the complex reference.
        /// </summary>
        /// <value>Parent type of the complex reference.</value>
        public ComplexReferenceParentType ParentType
        {
            get { return (ComplexReferenceParentType)Enum.ToObject(typeof(ComplexReferenceParentType), (int)this.Fields[1].Data); }
            set { this.Fields[1].Data = (int)value; }
        }

        /// <summary>
        /// Gets or sets the parent identifier of the complex reference.
        /// </summary>
        /// <value>Parent identifier of the complex reference.</value>
        public string ParentId
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        /// <summary>
        /// Gets the parent language of the complex reference.
        /// </summary>
        /// <value>Parent language of the complex reference.</value>
        public string ParentLanguage
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        /// <summary>
        /// Gets the child type of the complex reference.
        /// </summary>
        /// <value>Child type of the complex reference.</value>
        public ComplexReferenceChildType ChildType
        {
            get { return (ComplexReferenceChildType)Enum.ToObject(typeof(ComplexReferenceChildType), (int)this.Fields[4].Data); }
            set { this.Fields[4].Data = (int)value; }
        }

        /// <summary>
        /// Gets the child identifier of the complex reference.
        /// </summary>
        /// <value>Child identifier of the complex reference.</value>
        public string ChildId
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        /// <summary>
        /// Gets if this is the primary complex reference.
        /// </summary>
        /// <value>true if primary complex reference.</value>
        public bool IsPrimary
        {
            get
            {
                return (0x1 == ((int)this.Fields[5].Data & 0x1));
            }

            set
            {
                if (null == this.Fields[5].Data)
                {
                    this.Fields[5].Data = 0;
                }

                if (value)
                {
                    this.Fields[5].Data = (int)this.Fields[5].Data | 0x1;
                }
                else
                {
                    this.Fields[5].Data = (int)this.Fields[5].Data & ~0x1;
                }
            }
        }

        /// <summary>
        /// Determines if two complex references are equivalent.
        /// </summary>
        /// <param name="obj">Complex reference to compare.</param>
        /// <returns>True if complex references are equivalent.</returns>
        public override bool Equals(object obj)
        {
            return 0 == this.CompareTo(obj);
        }

        /// <summary>
        /// Gets the hash code for the complex reference.
        /// </summary>
        /// <returns>Hash code for the complex reference.</returns>
        public override int GetHashCode()
        {
            return this.ChildType.GetHashCode() ^ this.ChildId.GetHashCode() ^ this.ParentType.GetHashCode() ^ this.ParentLanguage.GetHashCode() ^ this.ParentId.GetHashCode() ^ this.IsPrimary.GetHashCode();
        }

        /// <summary>
        /// Compares two complex references.
        /// </summary>
        /// <param name="obj">Complex reference to compare to.</param>
        /// <returns>Zero if the objects are equivalent, negative number if the provided object is less, positive if greater.</returns>
        public int CompareTo(object obj)
        {
            int comparison = this.CompareToWithoutConsideringPrimary(obj);
            if (0 == comparison)
            {
                comparison = ((WixComplexReferenceRow)obj).IsPrimary.CompareTo(this.IsPrimary); // Note: the order of these is purposely switched to ensure that "Yes" is lower than "No" and "NotSet"
            }
            return comparison;
        }

        /// <summary>
        /// Compares two complex references without considering the primary bit.
        /// </summary>
        /// <param name="obj">Complex reference to compare to.</param>
        /// <returns>Zero if the objects are equivalent, negative number if the provided object is less, positive if greater.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String,System.String)")]
        public int CompareToWithoutConsideringPrimary(object obj)
        {
            WixComplexReferenceRow other = obj as WixComplexReferenceRow;
            if (null == other)
            {
                throw new ArgumentException(WixStrings.EXP_ExpectedComplexReferenceType, "obj");
            }

            int comparison = this.ChildType - other.ChildType;
            if (0 == comparison)
            {
                comparison = String.Compare(this.ChildId, other.ChildId, StringComparison.Ordinal);
                if (0 == comparison)
                {
                    comparison = this.ParentType - other.ParentType;
                    if (0 == comparison)
                    {
                        string thisParentLanguage = null == this.ParentLanguage ? String.Empty : this.ParentLanguage;
                        string otherParentLanguage = null == other.ParentLanguage ? String.Empty : other.ParentLanguage;
                        comparison = String.Compare(thisParentLanguage, otherParentLanguage, StringComparison.Ordinal);
                        if (0 == comparison)
                        {
                            comparison = String.Compare(this.ParentId, other.ParentId, StringComparison.Ordinal);
                        }
                    }
                }
            }

            return comparison;
        }

        /// <summary>
        /// Creates a shallow copy of the ComplexReference.
        /// </summary>
        /// <returns>A shallow copy of the ComplexReference.</returns>
        internal WixComplexReferenceRow Clone()
        {
            WixComplexReferenceRow wixComplexReferenceRow = new WixComplexReferenceRow(this.SourceLineNumbers, this.Table);
            wixComplexReferenceRow.ParentType = this.ParentType;
            wixComplexReferenceRow.ParentId = this.ParentId;
            wixComplexReferenceRow.ParentLanguage = this.ParentLanguage;
            wixComplexReferenceRow.ChildType = this.ChildType;
            wixComplexReferenceRow.ChildId = this.ChildId;
            wixComplexReferenceRow.IsPrimary = this.IsPrimary;

            return wixComplexReferenceRow;
        }

        /// <summary>
        /// Changes all of the parent references to point to the passed in parent reference.
        /// </summary>
        /// <param name="parent">New parent complex reference.</param>
        internal void Reparent(WixComplexReferenceRow parent)
        {
            this.ParentId = parent.ParentId;
            this.ParentLanguage = parent.ParentLanguage;
            this.ParentType = parent.ParentType;

            if (!this.IsPrimary)
            {
                this.IsPrimary = parent.IsPrimary;
            }
        }
    }
}
