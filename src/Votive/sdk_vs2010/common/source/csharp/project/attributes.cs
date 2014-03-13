//-------------------------------------------------------------------------------------------------
// <copyright file="attributes.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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

[assembly: InternalsVisibleTo("votive2010, PublicKey=0024000004800000940000000602000000240000525341310004000001000100d94aacc567fbd9fd7d60e04aca0817b2059b62d69630f24a07d699c4d5363ba24083a8ac9433b62c04cc6016bf7d528033ce655c1a0ee1b58f9cec3ad603525c9ca5319518ad2aeecf1a1263727e246db0603b72f2d49e65dc21a0e4d5ab65b846759d5f49529014b8766cb5a8b8daf987f25a49839dd3e538954d3a4ed938c4")]
[assembly: InternalsVisibleTo("votive2010, PublicKey=002400000480000094000000060200000024000052534131000400000100010041ecee86db7cfdf093df13b246788e7b95e711e23d50976aa64a2251000e8c7668a3c8dd01684d4d0c1738dbac8cc205871df7ce666aa0f569a96011ee7c85341ae5d9ac307129f06013e902202a1115f2c70ef6ddefd8ab0a3fd1151911b35f02f6b589bc96f78f11e0dceddbddbf57ba09e512306823b4e94de51f87b4ddc6")]
[assembly: CLSCompliant(true)]

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
