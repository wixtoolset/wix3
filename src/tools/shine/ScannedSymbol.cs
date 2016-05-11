// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public enum ScannedSymbolType
    {
        Bundle,
        PackageGroup,
        PayloadGroup,
        Payload,
        MsiPackage,
        ExePackage,
        MspPackage,
        MsuPackage,
        Product,
        Module,
        Feature,
        ComponentGroup,
        Component,
        File,
        Shortcut,
        ServiceInstall,
    }

    public class ScannedSymbol
    {
        public ScannedSymbol(string typeName, string id)
        {
            this.Id = id;
            this.Type = (ScannedSymbolType)Enum.Parse(typeof(ScannedSymbolType), typeName);

            this.Key = String.Concat(this.Type, ":", this.Id);
            this.SourceFiles = new List<ScannedSourceFile>();
            this.SourceSymbols = new List<ScannedSymbol>();
            this.TargetSymbols = new List<ScannedSymbol>();
        }

        public string Key { get; private set; }

        public string Id { get; private set; }

        public ScannedSymbolType Type { get; private set; }

        public IList<ScannedSourceFile> SourceFiles { get; private set; }

        public IList<ScannedSymbol> SourceSymbols { get; private set; }

        public IList<ScannedSymbol> TargetSymbols { get; private set; }

        public bool Excluded { get; set; }

        public static string CalculateKey(string typeName, string id)
        {
            ScannedSymbolType type = (ScannedSymbolType)Enum.Parse(typeof(ScannedSymbolType), typeName);
            return String.Concat(type, ":", id);
        }
    }
}
