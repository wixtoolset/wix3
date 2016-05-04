// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Related packages. Typically represents Upgrade table from an MsiPackage.
    /// </summary>
    internal class RelatedPackage
    {
        private List<string> languages = new List<string>();

        public string Id { get; set; }
        public string MinVersion { get; set; }
        public string MaxVersion { get; set; }
        public List<string> Languages { get { return this.languages; } }
        public bool MinInclusive { get; set; }
        public bool MaxInclusive { get; set; }
        public bool LangInclusive { get; set; }
        public bool OnlyDetect { get; set; }
    }
}
