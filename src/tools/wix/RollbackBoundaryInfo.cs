// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Rollback boundary info for binding Bundles.
    /// </summary>
    internal class RollbackBoundaryInfo
    {
        public RollbackBoundaryInfo(string id)
        {
            this.Default = true;
            this.Id = id;
            this.Vital = YesNoType.Yes;
        }

        public RollbackBoundaryInfo(Row row)
        {
            this.Id = row[0].ToString();

            this.Vital = (null == row[10] || 1 == (int)row[10]) ? YesNoType.Yes : YesNoType.No;
            this.SourceLineNumbers = row.SourceLineNumbers;
        }

        public bool Default { get; private set; }
        public string Id { get; private set; }
        public YesNoType Vital { get; private set; }
        public SourceLineNumberCollection SourceLineNumbers { get; private set; }
    }
}
