//-------------------------------------------------------------------------------------------------
// <copyright file="IBootstrapperEngineSecure.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// COM interop interface for IBootstrapperEngine.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Allows calls into the bootstrapper engine without having to use managed strings for everything.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6480D616-27A0-44D7-905B-81512C29C2FB")]
    [GeneratedCodeAttribute("Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperEngineSecure
    {
        void GetPackageCount(
            [MarshalAs(UnmanagedType.U4)] out int pcPackages
            );

        [PreserveSig]
        int GetVariableNumeric(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            out long pllValue
            );

        [PreserveSig]
        int GetVariableString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue,
            [MarshalAs(UnmanagedType.U4)] ref int pcchValue
            );

        [PreserveSig]
        int GetVariableVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            [MarshalAs(UnmanagedType.U8)] out long pqwValue
            );

        [PreserveSig]
        int FormatString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzIn,
                                              IntPtr wzOut,
            [MarshalAs(UnmanagedType.U4)] ref int pcchOut
            );

        [PreserveSig]
        int EscapeString(
                                          IntPtr wzIn,
                                          IntPtr wzOut,
            [MarshalAs(UnmanagedType.U4)] ref int pcchOut
            );

        void EvaluateCondition(
                                            IntPtr wzCondition,
            [MarshalAs(UnmanagedType.Bool)] out bool pf
            );

        void Log(
            [MarshalAs(UnmanagedType.U4)] LogLevel level,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage
            );

        void SendEmbeddedError(
            [MarshalAs(UnmanagedType.U4)] int dwErrorCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage,
            [MarshalAs(UnmanagedType.U4)] int dwUIHint,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        void SendEmbeddedProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallProgressPercentage,
            [MarshalAs(UnmanagedType.I4)] out int pnResult
            );

        void SetUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzLocalSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadSource,
            [MarshalAs(UnmanagedType.U8)] long qwValue,
            [MarshalAs(UnmanagedType.U4)] UpdateHashType hashType,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=4)] byte[] rgbHash,
            [MarshalAs(UnmanagedType.U4)] int cbHash
            );

        void SetLocalSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPath
            );

        void SetDownloadSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUrl,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUser,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPassword
            );

        void SetVariableNumeric(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            long llValue
            );

        void SetVariableString(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
                                              IntPtr wzValue
            );

        void SetVariableVersion(
            [MarshalAs(UnmanagedType.LPWStr)] string wzVariable,
            [MarshalAs(UnmanagedType.U8)] long qwValue
            );

        void CloseSplashScreen();

        void Detect();

        void Plan(
            [MarshalAs(UnmanagedType.U4)] LaunchAction action
            );

        [PreserveSig]
        int Elevate(
            IntPtr hwndParent
            );

        void Apply(
            IntPtr hwndParent
            );

        void Quit(
            [MarshalAs(UnmanagedType.U4)] int dwExitCode
            );
    }
}
