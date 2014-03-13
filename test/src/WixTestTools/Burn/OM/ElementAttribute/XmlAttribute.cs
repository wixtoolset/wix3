//-----------------------------------------------------------------------
// <copyright file="XmlAttribute.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>property attribute to define xml attribute name and default value</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixTest.Burn.OM.ElementAttribute
{
    [System.AttributeUsage(AttributeTargets.Property)]
    public class BurnXmlAttribute : System.Attribute
    {
        public string Name;
        public object DefaultValue;

        public BurnXmlAttribute(string name)
        {
            this.Name = name;
        }

        public BurnXmlAttribute(string name, object defaultValue)
        {
            this.Name = name;
            this.DefaultValue = defaultValue;
        }
    }
}