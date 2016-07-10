#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const LPCWSTR BURN_POLICY_REGISTRY_PATH = L"WiX\\Burn";

const LPCWSTR BURN_COMMANDLINE_SWITCH_PARENT = L"parent";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PARENT_NONE = L"parent:none";
const LPCWSTR BURN_COMMANDLINE_SWITCH_CLEAN_ROOM = L"burn.clean.room";
const LPCWSTR BURN_COMMANDLINE_SWITCH_ELEVATED = L"burn.elevated";
const LPCWSTR BURN_COMMANDLINE_SWITCH_EMBEDDED = L"burn.embedded";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RUNONCE = L"burn.runonce";
const LPCWSTR BURN_COMMANDLINE_SWITCH_LOG_APPEND = L"burn.log.append";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_DETECT = L"burn.related.detect";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_UPGRADE = L"burn.related.upgrade";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_ADDON = L"burn.related.addon";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_PATCH = L"burn.related.patch";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_UPDATE = L"burn.related.update";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PASSTHROUGH = L"burn.passthrough";
const LPCWSTR BURN_COMMANDLINE_SWITCH_DISABLE_UNELEVATE = L"burn.disable.unelevate";
const LPCWSTR BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES = L"burn.ignoredependencies";
const LPCWSTR BURN_COMMANDLINE_SWITCH_ANCESTORS = L"burn.ancestors";
const LPCWSTR BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED = L"burn.filehandle.attached";
const LPCWSTR BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF = L"burn.filehandle.self";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PREFIX = L"burn.";

const LPCWSTR BURN_BUNDLE_LAYOUT_DIRECTORY = L"WixBundleLayoutDirectory";
const LPCWSTR BURN_BUNDLE_ACTION = L"WixBundleAction";
const LPCWSTR BURN_BUNDLE_ACTIVE_PARENT = L"WixBundleActiveParent";
const LPCWSTR BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER = L"WixBundleExecutePackageCacheFolder";
const LPCWSTR BURN_BUNDLE_EXECUTE_PACKAGE_ACTION = L"WixBundleExecutePackageAction";
const LPCWSTR BURN_BUNDLE_FORCED_RESTART_PACKAGE = L"WixBundleForcedRestartPackage";
const LPCWSTR BURN_BUNDLE_INSTALLED = L"WixBundleInstalled";
const LPCWSTR BURN_BUNDLE_ELEVATED = L"WixBundleElevated";
const LPCWSTR BURN_BUNDLE_PROVIDER_KEY = L"WixBundleProviderKey";
const LPCWSTR BURN_BUNDLE_MANUFACTURER = L"WixBundleManufacturer";
const LPCWSTR BURN_BUNDLE_SOURCE_PROCESS_PATH = L"WixBundleSourceProcessPath";
const LPCWSTR BURN_BUNDLE_SOURCE_PROCESS_FOLDER = L"WixBundleSourceProcessFolder";
const LPCWSTR BURN_BUNDLE_TAG = L"WixBundleTag";
const LPCWSTR BURN_BUNDLE_UILEVEL = L"WixBundleUILevel";
const LPCWSTR BURN_BUNDLE_VERSION = L"WixBundleVersion";

// The following constants must stay in sync with src\wix\Binder.cs
const LPCWSTR BURN_BUNDLE_NAME = L"WixBundleName";
const LPCWSTR BURN_BUNDLE_ORIGINAL_SOURCE = L"WixBundleOriginalSource";
const LPCWSTR BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = L"WixBundleOriginalSourceFolder";
const LPCWSTR BURN_BUNDLE_LAST_USED_SOURCE = L"WixBundleLastUsedSource";


// enums

enum BURN_MODE
{
    BURN_MODE_UNTRUSTED,
    BURN_MODE_NORMAL,
    BURN_MODE_ELEVATED,
    BURN_MODE_EMBEDDED,
    BURN_MODE_RUNONCE,
};

enum BURN_AU_PAUSE_ACTION
{
    BURN_AU_PAUSE_ACTION_NONE,
    BURN_AU_PAUSE_ACTION_IFELEVATED,
    BURN_AU_PAUSE_ACTION_IFELEVATED_NORESUME,
};


// structs

typedef struct _BURN_ENGINE_STATE
{
    // synchronization
    CRITICAL_SECTION csActive; // Any call from the UX that reads or alters the engine state
                               // needs to be syncronized through this critical section.
                               // Note: The engine must never do a UX callback while in this critical section.

    // UX flow control
    //BOOL fSuspend;             // Is TRUE when UX made Suspend() call on core.
    //BOOL fForcedReboot;        // Is TRUE when UX made Reboot() call on core.
    //BOOL fCancelled;           // Is TRUE when UX return cancel on UX OnXXX() methods.
    //BOOL fReboot;              // Is TRUE when UX confirms OnRestartRequried().
    BOOL fRestart;               // Set TRUE when UX returns IDRESTART during Apply().

    // engine data
    BOOTSTRAPPER_COMMAND command;
    BURN_SECTION section;
    BURN_VARIABLES variables;
    BURN_CONDITION condition;
    BURN_SEARCHES searches;
    BURN_USER_EXPERIENCE userExperience;
    BURN_REGISTRATION registration;
    BURN_CONTAINERS containers;
    BURN_CATALOGS catalogs;
    BURN_PAYLOADS payloads;
    BURN_PACKAGES packages;
    BURN_UPDATE update;
    BURN_APPROVED_EXES approvedExes;

    HWND hMessageWindow;
    HANDLE hMessageWindowThread;

    BOOL fDisableRollback;
    BOOL fDisableSystemRestore;
    BOOL fParallelCacheAndExecute;

    BURN_LOGGING log;

    BURN_PLAN plan;

    BURN_MODE mode;
    BURN_AU_PAUSE_ACTION automaticUpdates;

    DWORD dwElevatedLoggingTlsId;

    LPWSTR sczBundleEngineWorkingPath;
    BURN_PIPE_CONNECTION companionConnection;
    BURN_PIPE_CONNECTION embeddedConnection;

    BURN_RESUME_MODE resumeMode;
    BOOL fDisableUnelevate;

    LPWSTR sczIgnoreDependencies;

    int argc;
    LPWSTR* argv;
} BURN_ENGINE_STATE;


// function declarations

HRESULT CoreInitialize(
    __in BURN_ENGINE_STATE* pEngineState
    );
HRESULT CoreSerializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT CoreQueryRegistration(
    __in BURN_ENGINE_STATE* pEngineState
    );
//HRESULT CoreDeserializeEngineState(
//    __in BURN_ENGINE_STATE* pEngineState,
//    __in_bcount(cbBuffer) BYTE* pbBuffer,
//    __in SIZE_T cbBuffer
//    );
HRESULT CoreDetect(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
HRESULT CorePlan(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOTSTRAPPER_ACTION action
    );
HRESULT CoreElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
HRESULT CoreApply(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
HRESULT CoreLaunchApprovedExe(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );
HRESULT CoreQuit(
    __in BURN_ENGINE_STATE* pEngineState,
    __in int nExitCode
    );
HRESULT CoreSaveEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );
LPCWSTR CoreRelationTypeToCommandLineString(
    __in BOOTSTRAPPER_RELATION_TYPE relationType
    );
HRESULT CoreRecreateCommandLine(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RESTART restart,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL fPassthrough,
    __in_z_opt LPCWSTR wzActiveParent,
    __in_z_opt LPCWSTR wzAncestors,
    __in_z_opt LPCWSTR wzAppendLogPath,
    __in_z_opt LPCWSTR wzAdditionalCommandLineArguments
    );
HRESULT CoreAppendFileHandleAttachedToCommandLine(
    __in HANDLE hFileWithAttachedContainer,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine
    );
HRESULT CoreAppendFileHandleSelfToCommandLine(
    __in LPCWSTR wzExecutablePath,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine,
    __deref_inout_z_opt LPWSTR* psczObfuscatedCommandLine
    );

#if defined(__cplusplus)
}
#endif
