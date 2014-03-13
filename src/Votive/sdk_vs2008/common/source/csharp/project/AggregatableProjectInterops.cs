//-------------------------------------------------------------------------------------------------
// <copyright file="AggregatableProjectInterops.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.VisualStudio.Shell.Interop
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.OLE.Interop;
    using Microsoft.VisualStudio.Shell;

    // We need to define a corrected version of the IA for this interface so that IUnknown pointers are passed
    // as "IntPtr" instead of "object". This ensures that we get the actual IUnknown pointer and not a wrapped
    // managed proxy pointer.
    [ComImport]
    [Guid("ffb2e715-7312-4b93-83d7-d37bcc561c90")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsAggregatableProjectCorrected
    {
        [PreserveSig]
        int SetInnerProject(IntPtr punkInnerIUnknown);
        [PreserveSig]
        int InitializeForOuter([MarshalAs(UnmanagedType.LPWStr)]string pszFilename, 
                               [MarshalAs(UnmanagedType.LPWStr)]string pszLocation, 
                               [MarshalAs(UnmanagedType.LPWStr)]string pszName, 
                               uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled);
        [PreserveSig]
        int OnAggregationComplete();
        [PreserveSig]
        int GetAggregateProjectTypeGuids([MarshalAs(UnmanagedType.BStr)]out string pbstrProjTypeGuids);
        [PreserveSig]
        int SetAggregateProjectTypeGuids([MarshalAs(UnmanagedType.LPWStr)]string lpstrProjTypeGuids);
    }

    // We need to define a corrected version of the IA for this interface so that IUnknown pointers are passed
    // as "IntPtr" instead of "object". This ensures that we get the actual IUnknown pointer and not a wrapped
    // managed proxy pointer.
    [ComImport()]
    [Guid("6d5140d3-7436-11ce-8034-00aa006009fa")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ILocalRegistryCorrected
    {
        [PreserveSig]
        int CreateInstance(Guid clsid, IntPtr punkOuterIUnknown, ref Guid riid, uint dwFlags, out IntPtr ppvObj);
        [PreserveSig]
        int GetClassObjectOfClsid(ref Guid clsid, uint dwFlags, IntPtr lpReserved, ref Guid riid, out IntPtr ppvClassObject);
        [PreserveSig]
        int GetTypeLibOfClsid(Guid clsid, out ITypeLib pptLib);
    }

    // We need to define a corrected version of the IA for this interface so that IUnknown pointers are passed
    // as "IntPtr" instead of "object". This ensures that we get the actual IUnknown pointer and not a wrapped
    // managed proxy pointer.
    [ComImport]
    [Guid("44569501-2ad0-4966-9bac-12b799a1ced6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVsAggregatableProjectFactoryCorrected
    {
        [PreserveSig]
        int GetAggregateProjectType([MarshalAs(UnmanagedType.LPWStr)]string fileName, [MarshalAs(UnmanagedType.BStr)]out string projectTypeGuid);
        [PreserveSig]
        int PreCreateForOuter(IntPtr outerProjectIUnknown, out IntPtr projectIUnknown);
    }
}
