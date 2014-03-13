//-------------------------------------------------------------------------------------------------
// <copyright file="ExitCodeInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Utility class for Burn ExitCode information.
// </summary>
//-------------------------------------------------------------------------------------------------

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
                this.Code = value.ToString();
            }
            this.Type = behavior;
        }

        public string PackageId { get; private set; }
        public string Code { get; private set; }
        public string Type { get; private set; }
    }
}
