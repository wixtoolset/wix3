// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Chain info for binding Bundles.
    /// </summary>
    internal class ChainInfo
    {
        public ChainInfo(Row row)
        {
            BundleChainAttributes attributes = (null == row[0]) ? BundleChainAttributes.None : (BundleChainAttributes)row[0];

            this.DisableRollback = (BundleChainAttributes.DisableRollback == (attributes & BundleChainAttributes.DisableRollback));
            this.DisableSystemRestore = (BundleChainAttributes.DisableSystemRestore == (attributes & BundleChainAttributes.DisableSystemRestore));
            this.ParallelCache = (BundleChainAttributes.ParallelCache == (attributes & BundleChainAttributes.ParallelCache));
            this.Packages = new List<ChainPackageInfo>();
            this.RollbackBoundaries = new List<RollbackBoundaryInfo>();
            this.SourceLineNumbers = row.SourceLineNumbers;
        }

        public bool DisableRollback { get; private set; }
        public bool DisableSystemRestore { get; private set; }
        public bool ParallelCache { get; private set; }
        public List<ChainPackageInfo> Packages { get; private set; }
        public List<RollbackBoundaryInfo> RollbackBoundaries { get; private set; }
        public SourceLineNumberCollection SourceLineNumbers { get; private set; }
    }
}
