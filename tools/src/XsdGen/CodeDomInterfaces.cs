// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Serialize
{
    using System;
    using System.Collections;
    using System.Xml;

    /// <summary>
    /// Interface for generated schema elements.
    /// </summary>
    public interface ISchemaElement
    {
        /// <summary>
        /// Gets and sets the parent of this element. May be null.
        /// </summary>
        /// <value>An ISchemaElement that has this element as a child.</value>
        ISchemaElement ParentElement
        {
            get;
            set;
        }
    
        /// <summary>
        /// Outputs xml representing this element, including the associated attributes
        /// and any nested elements.
        /// </summary>
        /// <param name="writer">XmlTextWriter to be used when outputting the element.</param>
        void OutputXml(XmlWriter writer);
    }

    /// <summary>
    /// Interface for generated schema elements. Implemented by elements that have child
    /// elements.
    /// </summary>
    public interface IParentElement
    {
        /// <summary>
        /// Gets an enumerable collection of the children of this element.
        /// </summary>
        /// <value>An enumerable collection of the children of this element.</value>
        IEnumerable Children
        {
            get;
        }

        /// <summary>
        /// Gets an enumerable collection of the children of this element, filtered
        /// by the passed in type.
        /// </summary>
        /// <param name="childType">The type of children to retrieve.</param>
        IEnumerable this[Type childType]
        {
            get;
        }

        /// <summary>
        /// Adds a child to this element.
        /// </summary>
        /// <param name="child">Child to add.</param>
        void AddChild(ISchemaElement child);

        /// <summary>
        /// Removes a child from this element.
        /// </summary>
        /// <param name="child">Child to remove.</param>
        void RemoveChild(ISchemaElement child);
    }

    /// <summary>
    /// Interface for generated schema elements. Implemented by classes with attributes.
    /// </summary>
    public interface ISetAttributes
    {
        /// <summary>
        /// Sets the attribute with the given name to the given value. The value here is
        /// a string, and is converted to the strongly-typed version inside this method.
        /// </summary>
        /// <param name="name">The name of the attribute to set.</param>
        /// <param name="value">The value to assign to the attribute.</param>
        void SetAttribute(string name, string value);
    }

    /// <summary>
    /// Interface for generated schema elements. Implemented by classes with children.
    /// </summary>
    public interface ICreateChildren
    {
        /// <summary>
        /// Creates an instance of the child with the passed in name.
        /// </summary>
        /// <param name="childName">String matching the element name of the child when represented in XML.</param>
        /// <returns>An instance of that child.</returns>
        ISchemaElement CreateChild(string childName);
    }
}
