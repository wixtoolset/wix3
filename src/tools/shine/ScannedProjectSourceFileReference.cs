//-------------------------------------------------------------------------------------------------
// <copyright file="ScannedProjectSourceFileReference.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Tools.WindowsInstallerXml
{
    public class ScannedProjectSourceFileReference : IComparable
    {
        public ScannedProject SourceProject { get; set; }

        public ScannedSourceFile TargetSourceFile { get; set; }

        public int CompareTo(object obj)
        {
            ScannedProjectSourceFileReference r = (ScannedProjectSourceFileReference)obj;
            int result = this.SourceProject.Key.CompareTo(r.SourceProject.Key);
            if (result == 0)
            {
                result = this.TargetSourceFile.Key.CompareTo(r.TargetSourceFile.Key);
            }

            return result;
        }
    }
}
