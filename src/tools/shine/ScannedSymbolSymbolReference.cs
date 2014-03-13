//-------------------------------------------------------------------------------------------------
// <copyright file="ScannedSymbolSymbolReference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public class ScannedSymbolSymbolReference : IComparable
    {
        public ScannedSymbol SourceSymbol { get; set; }

        public ScannedSymbol TargetSymbol { get; set; }

        public int CompareTo(object obj)
        {
            ScannedSymbolSymbolReference r = (ScannedSymbolSymbolReference)obj;
            int result = this.SourceSymbol.Key.CompareTo(r.SourceSymbol.Key);
            if (result == 0)
            {
                result = this.TargetSymbol.Key.CompareTo(r.TargetSymbol.Key);
            }

            return result;
        }
    }
}
