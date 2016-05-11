// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Utility class for Burn MsiProperty information.
    /// </summary>
    internal class MsiPropertyInfo
    {
        public MsiPropertyInfo(Row row)
            : this((string)row[0], (string)row[1], (string)row[2])
        {
        }

        public MsiPropertyInfo(string packageId, string name, string value)
        {
            this.PackageId = packageId;
            this.Name = name;
            this.Value = value;
        }

        public string PackageId { get; private set; }
        public string Name { get; private set; }
        public string Value { get; set; }
    }
}
