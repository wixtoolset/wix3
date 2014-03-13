//-----------------------------------------------------------------------
// <copyright file="WixTools.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Contains an enumeration of the Wix Tools</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;

    /// <summary>
    /// Contains an enumeration of the Wix Tools
    /// </summary>
    public abstract partial class WixTool : TestTool
    {
        /// <summary>
        /// The Wix Tools
        /// </summary>
        public enum WixTools
        {
            Any,
            Candle,
            Dark,
            Heat,
            Insignia,
            Lit,
            Light,
            Melt,
            Pyro,
            Smoke,
            Torch,
            Wixunit
        }
    }
}
