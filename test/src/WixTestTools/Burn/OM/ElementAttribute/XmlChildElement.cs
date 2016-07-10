// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WixTest.Burn.OM.ElementAttribute
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class BurnXmlChildElement : System.Attribute
    {
        public BurnXmlChildElement()
        {
        }       
    }
}
