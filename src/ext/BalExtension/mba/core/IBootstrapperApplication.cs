// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Allows customization of the bootstrapper application.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("53C31D56-49C0-426B-AB06-099D717C67FE")]
    [GeneratedCodeAttribute("Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperApplication
    {
        void OnStartup();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnShutdown();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnSystemShutdown(
            [MarshalAs(UnmanagedType.U4)] EndSessionReasons dwEndSession,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectBegin(
            [MarshalAs(UnmanagedType.Bool)] bool fInstalled,
            [MarshalAs(UnmanagedType.U4)] int cPackages
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectUpdateBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
             [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.U8)] long dw64Size,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTitle,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSummary,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContentType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContent,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        void OnDetectUpdateComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectCompatiblePackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectRelatedMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectTargetMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] PackageState patchState
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnDetectMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] FeatureState state
            );

        void OnDetectPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state
            );

        void OnDetectComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanBegin(
            [MarshalAs(UnmanagedType.U4)] int cPackages
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanCompatiblePackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanTargetMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnPlanMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] ref FeatureState pRequestedState
            );

        void OnPlanPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state,
            [MarshalAs(UnmanagedType.U4)] RequestState requested,
            [MarshalAs(UnmanagedType.U4)] ActionState execute,
            [MarshalAs(UnmanagedType.U4)] ActionState rollback
            );

        void OnPlanComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnApplyBegin();

        void OnApplyPhaseCount(
            int dwPhaseCount
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnElevate();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnError(
            [MarshalAs(UnmanagedType.U4)] ErrorType errorType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int dwCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzError,
            [MarshalAs(UnmanagedType.U4)] int uiFlags,
            [MarshalAs(UnmanagedType.U4)] int cData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzData,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnRegisterBegin();

        void OnRegisterComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheBegin();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCachePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cCachePayloads,
            [MarshalAs(UnmanagedType.U8)] long dw64PackageCacheSize
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheAcquireBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U4)] CacheOperation operation,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSource
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheAcquireProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U8)] long dw64Progress,
            [MarshalAs(UnmanagedType.U8)] long dw64Total,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnResolveSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzLocalSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadSource
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheAcquireComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheVerifyBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCacheVerifyComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnCachePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        void OnCacheComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecuteBegin(
            [MarshalAs(UnmanagedType.U4)] int cExecutingPackages
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecutePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fExecute
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecutePatchTarget(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTargetProductCode
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecuteProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecuteMsiMessage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] InstallMessage mt,
            [MarshalAs(UnmanagedType.U4)] int uiFlags,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage,
            [MarshalAs(UnmanagedType.U4)] int cData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzData,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecuteFilesInUse(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cFiles,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzFiles
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnExecutePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrExitCode,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] int nRecommendation
            );

        void OnExecuteComplete(
            int hrStatus
            );

        void OnUnregisterBegin();

        void OnUnregisterComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnApplyComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        Result OnLaunchApprovedExeBegin();

        void OnLaunchApprovedExeComplete(
            int hrStatus,
            int processId
            );
    }

    /// <summary>
    /// The display level for the UX.
    /// </summary>
    public enum Display
    {
        Unknown,
        Embedded,
        None,
        Passive,
        Full,
    }

    /// <summary>
    /// Messages from Windows Installer.
    /// </summary>
    public enum InstallMessage
    {
        FatalExit,
        Error = 0x01000000,
        Warning = 0x02000000,
        User = 0x03000000,
        Info = 0x04000000,
        FilesInUse = 0x05000000,
        ResolveSource = 0x06000000,
        OutOfDiskSpace = 0x07000000,
        ActionStart = 0x08000000,
        ActionData = 0x09000000,
        Progress = 0x0a000000,
        CommonData = 0x0b000000,
        Initialize = 0x0c000000,
        Terminate = 0x0d000000,
        ShowDialog = 0x0e000000,
        RMFilesInUse = 0x19000000,
    }

    /// <summary>
    /// The action to perform when a reboot is necessary.
    /// </summary>
    public enum Restart
    {
        Unknown,
        Never,
        Prompt,
        Automatic,
        Always,
    }

    /// <summary>
    /// Result codes.
    /// </summary>
    public enum Result
    {
        Error = -1,
        None,
        Ok,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No,
        Close,
        Help,
        TryAgain,
        Continue,
        Download = 101,
        Restart,
        Suspend,
        Timeout = 32000,
    }

    /// <summary>
    /// Describes why a bundle or packaged is being resumed.
    /// </summary>
    public enum ResumeType
    {
        None,

        /// <summary>
        /// Resume information exists but is invalid.
        /// </summary>
        Invalid,

        /// <summary>
        /// The bundle was re-launched after an unexpected interruption.
        /// </summary>
        Interrupted,

        /// <summary>
        /// A reboot is pending.
        /// </summary>
        RebootPending,

        /// <summary>
        /// The bundle was re-launched after a reboot.
        /// </summary>
        Reboot,

        /// <summary>
        /// The bundle was re-launched after being suspended.
        /// </summary>
        Suspend,

        /// <summary>
        /// The bundle was launched from Add/Remove Programs.
        /// </summary>
        Arp,
    }

    /// <summary>
    /// Indicates what caused the error.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// The error occurred trying to elevate.
        /// </summary>
        Elevate,

        /// <summary>
        /// The error came from the Windows Installer.
        /// </summary>
        WindowsInstaller,

        /// <summary>
        /// The error came from an EXE Package.
        /// </summary>
        ExePackage,

        /// <summary>
        /// The error came while trying to authenticate with an HTTP server.
        /// </summary>
        HttpServerAuthentication,

        /// <summary>
        /// The error came while trying to authenticate with an HTTP proxy.
        /// </summary>
        HttpProxyAuthentication,

        /// <summary>
        /// The error occurred during apply.
        /// </summary>
        Apply,
    };

    public enum RelatedOperation
    {
        None,

        /// <summary>
        /// The related bundle or package will be downgraded.
        /// </summary>
        Downgrade,

        /// <summary>
        /// The related package will be upgraded as a minor revision.
        /// </summary>
        MinorUpdate,

        /// <summary>
        /// The related bundle or package will be upgraded as a major revision.
        /// </summary>
        MajorUpgrade,

        /// <summary>
        /// The related bundle will be removed.
        /// </summary>
        Remove,

        /// <summary>
        /// The related bundle will be installed.
        /// </summary>
        Install,

        /// <summary>
        /// The related bundle will be repaired.
        /// </summary>
        Repair,
    };

    /// <summary>
    /// The cache operation used to acquire a container or payload.
    /// </summary>
    public enum CacheOperation
    {
        /// <summary>
        /// Container or payload is being copied.
        /// </summary>
        Copy,

        /// <summary>
        /// Container or payload is being downloaded.
        /// </summary>
        Download,

        /// <summary>
        /// Container or payload is being extracted.
        /// </summary>
        Extract
    }

    /// <summary>
    /// The restart state after a package or all packages were applied.
    /// </summary>
    public enum ApplyRestart
    {
        /// <summary>
        /// Package or chain does not require a restart.
        /// </summary>
        None,

        /// <summary>
        /// Package or chain requires a restart but it has not been initiated yet.
        /// </summary>
        RestartRequired,

        /// <summary>
        /// Package or chain has already initiated the restart.
        /// </summary>
        RestartInitiated
    }

    /// <summary>
    /// The relation type for related bundles.
    /// </summary>
    public enum RelationType
    {
        None,
        Detect,
        Upgrade,
        Addon,
        Patch,
        Dependent,
        Update,
    }

    /// <summary>
    /// One or more reasons why the application is requested to be closed or is being closed.
    /// </summary>
    [Flags]
    public enum EndSessionReasons
    {
        /// <summary>
        /// The system is shutting down or restarting (it is not possible to determine which event is occurring).
        /// </summary>
        Unknown,

        /// <summary>
        /// The application is using a file that must be replaced, the system is being serviced, or system resources are exhausted.
        /// </summary>
        CloseApplication,

        /// <summary>
        /// The application is forced to shut down.
        /// </summary>
        Critical = 0x40000000,

        /// <summary>
        /// The user is logging off.
        /// </summary>
        Logoff = unchecked((int)0x80000000)
    }
}
