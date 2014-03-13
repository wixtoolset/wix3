//-----------------------------------------------------------------------
// <copyright file="XmlChildElement.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>property attribute to define xml attributes</summary>
//-----------------------------------------------------------------------

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
