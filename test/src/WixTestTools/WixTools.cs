// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
