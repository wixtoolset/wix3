// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
