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
            this.SourceFile = wixApprovedExeForElevationRow.SourceFile;
        }

        public void CalculateHash()
        {
            FileInfo fileInfo = new FileInfo(this.SourceFile);
            this.FileSize = fileInfo.Length;
            this.Hash = Common.GetFileHash(fileInfo);
        }

        public string Id { get; set; }

        public string SourceFile { get; set; }

        public long FileSize { get; set; }

        public string Hash { get; set; }
    }
}
