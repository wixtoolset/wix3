// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Utility class for Burn CommandLine information.
    /// </summary>
    internal class CommandLineInfo
    {
        public CommandLineInfo(Row row)
            : this((string)row[0], (string)row[1], (string)row[2], (string)row[3], (string)row[4])
        {
        }

        public CommandLineInfo(string packageId, string installCommand, string uninstallCommand, string repairCommand, string condition)
        {
            this.PackageId = packageId;
            this.InstallArgument = installCommand;
            this.UninstallArgument = uninstallCommand;
            this.RepairArgument = repairCommand;
            this.Condition = condition;
        }

        public string PackageId { get; private set; }
        public string InstallArgument { get; private set; }
        public string UninstallArgument { get; private set; }
        public string RepairArgument { get; private set; }
        public string Condition { get; private set; }
    }
}
