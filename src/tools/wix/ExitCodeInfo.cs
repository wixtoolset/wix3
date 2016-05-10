// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Utility class for Burn ExitCode information.
    /// </summary>
    internal class ExitCodeInfo
    {
        public ExitCodeInfo(Row row)
            : this((string)row[0], (int)row[1], (string)row[2])
        {
        }

        public ExitCodeInfo(string packageId, int value, string behavior)
        {
            this.PackageId = packageId;
            // null value means wildcard
            if (CompilerCore.IntegerNotSet == value)
            {
                this.Code = "*";
            }
            else
            {
                this.Code = unchecked((uint)value).ToString();
            }
            this.Type = behavior;
        }

        public string PackageId { get; private set; }
        public string Code { get; private set; }
        public string Type { get; private set; }
    }
}
