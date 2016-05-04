// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Designer.Interfaces;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Package
{ 
    /// <summary>
    /// Defines a type converter.
    /// </summary>
    /// <remarks>This is needed to get rid of the type TypeConverter type that could not give back the Type we were passing to him.
    /// We do not want to use reflection to get the type back from the  ConverterTypeName. Also the GetType methos does not spwan converters from other assemblies.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class PropertyPageTypeConverterAttribute : Attribute
    {
        #region fields
        Type converterType;
        #endregion

        #region ctors
        public PropertyPageTypeConverterAttribute(Type type)
        {
            this.converterType = type;
        } 
        #endregion

        #region properties
        public Type ConverterType
        {
            get
            {
                return this.converterType;
            }
        } 
        #endregion
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LocDisplayNameAttribute : DisplayNameAttribute
    {
        #region fields
        string name;
        #endregion

        #region ctors
        public LocDisplayNameAttribute(string name)
        {
            this.name = name;
        } 
        #endregion

        #region properties
        public override string DisplayName
        {
            get
            {
                string result = SR.GetString(this.name, CultureInfo.CurrentUICulture);
                if (result == null)
                {
                    Debug.Assert(false, "String resource '" + this.name + "' is missing");
                    result = this.name;
                }
                return result;
            }
        } 
        #endregion
    }
}
