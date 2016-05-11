// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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

        public string Id { get; private set; }

        public string Key { get; private set; }

        public string ValueName { get; private set; }

        public BundleApprovedExeForElevationAttributes Attributes { get; private set; }

        public bool Win64
        {
            get
            {
                return BundleApprovedExeForElevationAttributes.Win64 == (this.Attributes & BundleApprovedExeForElevationAttributes.Win64);
            }
        }
    }
}
