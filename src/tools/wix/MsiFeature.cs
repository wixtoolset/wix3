// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Msi Feature Information.
    /// </summary>
    internal class MsiFeature
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Parent { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Display { get; set; }
        public int Level { get; set; }
        public string Directory { get; set; }
        public int Attributes { get; set; }
    }
}
