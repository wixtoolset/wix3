//-------------------------------------------------------------------------------------------------
// <copyright file="RelatedPackage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Related packages.
// </summary>
//-------------------------------------------------------------------------------------------------

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
