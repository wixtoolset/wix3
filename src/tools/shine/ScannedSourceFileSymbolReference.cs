// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
