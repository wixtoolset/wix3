//-------------------------------------------------------------------------------------------------
// <copyright file="ApprovedExeForElevation.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// ApprovedExeForElevation dynamically generated info.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.IO;

    /// <summary>
    /// ApprovedExeForElevation dynamically generated info.
    /// </summary>
    public class ApprovedExeForElevation
    {
        public ApprovedExeForElevation(WixApprovedExeForElevationRow wixApprovedExeForElevationRow)
        {
            this.Id = wixApprovedExeForElevationRow.Id;
            this.Key = wixApprovedExeForElevationRow.Key;
            this.ValueName = wixApprovedExeForElevationRow.ValueName;
            this.Attributes = wixApprovedExeForElevationRow.Attributes;
        }

        public string Id { get; set; }

        public string Key { get; set; }

        public string ValueName { get; set; }

        public BundleApprovedExeForElevationAttributes Attributes { get; set; }

        public bool Win64
        {
            get
            {
                return BundleApprovedExeForElevationAttributes.Win64 == (this.Attributes & BundleApprovedExeForElevationAttributes.Win64);
            }
        }
    }
}
