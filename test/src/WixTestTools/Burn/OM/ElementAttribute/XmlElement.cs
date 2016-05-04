// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixTest.Burn.OM.ElementAttribute
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class BurnXmlElement : System.Attribute
    {
        public string Name;
        public string NamespacePrefix;

        public BurnXmlElement(string name)
	   : this(name, string.Empty)
        {
        }

        public BurnXmlElement(string name, string namespacePrefix)
        {
            this.Name = name;
            this.NamespacePrefix = namespacePrefix;
        }
    }
}
