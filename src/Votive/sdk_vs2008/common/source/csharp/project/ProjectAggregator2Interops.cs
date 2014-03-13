//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectAggregator2Interops.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.ProjectAggregator2.Interop
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;

    [ComImport]
    [Guid("D6CEA324-8E81-4e0e-91DE-E5D7394A45CE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsProjectAggregator2
    {
        #region IVsProjectAggregator2 Members

        [PreserveSig]
        int SetInner(IntPtr innerIUnknown);
        [PreserveSig]
        int SetMyProject(IntPtr projectIUnknown);

        #endregion
    }
}
