//-------------------------------------------------------------------------------------------------
// <copyright file="CommandLineInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for Burn ExePackage CommandLine information.
// </summary>
//-------------------------------------------------------------------------------------------------

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
