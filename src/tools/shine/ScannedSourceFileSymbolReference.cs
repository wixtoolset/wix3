//-------------------------------------------------------------------------------------------------
// <copyright file="ScannedSourceFileSymbolReference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public class ScannedSourceFileSymbolReference : IComparable
    {
        public ScannedSourceFile SourceSourceFile { get; set; }

        public ScannedSymbol TargetSymbol { get; set; }

        public int CompareTo(object obj)
        {
            ScannedSourceFileSymbolReference r = (ScannedSourceFileSymbolReference)obj;
            int result = this.SourceSourceFile.Key.CompareTo(r.SourceSourceFile.Key);
            if (result == 0)
            {
                result = this.TargetSymbol.Key.CompareTo(r.TargetSymbol.Key);
            }

            return result;
        }
    }
}
