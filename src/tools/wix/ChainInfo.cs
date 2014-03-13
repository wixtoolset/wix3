//-------------------------------------------------------------------------------------------------
// <copyright file="ChainInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Chain info for binding Bundles.
// </summary>
//-------------------------------------------------------------------------------------------------

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
