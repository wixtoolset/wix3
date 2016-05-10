// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    public class ScannedUnresolvedReference : IComparable
    {
        public ScannedUnresolvedReference(string typeName, string id, ScannedSourceFile sourceFile, ScannedSymbol symbol)
        {
            this.Id = id;
            this.SourceFile = sourceFile;
            this.SourceSymbol = symbol;
            this.Type = (ScannedSymbolType)Enum.Parse(typeof(ScannedSymbolType), typeName);

            this.TargetSymbol = String.Concat(this.Type, ":", this.Id);
        }

        public string Id { get; private set; }

        public ScannedSourceFile SourceFile { get; private set; }

        public ScannedSymbol SourceSymbol { get; private set; }

        public string TargetSymbol { get; private set; }

        public ScannedSymbolType Type { get; private set; }

        public int CompareTo(object obj)
        {
            ScannedUnresolvedReference r = (ScannedUnresolvedReference)obj;
            int result = this.TargetSymbol.CompareTo(r.TargetSymbol);
            return result;
        }
    }
}
