#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD BURN_PLAN_INVALID_ACTION_INDEX = 0x80000000;

enum BURN_REGISTRATION_ACTION_OPERATIONS
{
    BURN_REGISTRATION_ACTION_OPERATIONS_NONE = 0x0,
    BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE = 0x1,
    BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION = 0x2,
    BURN_REGISTRATION_ACTION_OPERATIONS_UPDATE_SIZE = 0x4,
};

enum BURN_DEPENDENCY_REGISTRATION_ACTION
{
    BURN_DEPENDENCY_REGISTRATION_ACTION_NONE,
    BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER,
    BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER,
};

enum BURN_DEPENDENT_REGISTRATION_ACTION_TYPE
{
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_NONE,
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER,
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER,
};

enum BURN_CACHE_ACTION_TYPE
{
    BURN_CACHE_ACTION_TYPE_NONE,
    BURN_CACHE_ACTION_TYPE_CHECKPOINT,
    BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE,
    BURN_CACHE_ACTION_TYPE_PACKAGE_START,
    BURN_CACHE_ACTION_TYPE_PACKAGE_STOP,
    BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE,
    BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT,
    BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER,
    BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER,
    BURN_CACHE_ACTION_TYPE_LAYOUT_CONTAINER,
    BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD,
    BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD,
    BURN_CACHE_ACTION_TYPE_LAYOUT_PAYLOAD,
    BURN_CACHE_ACTION_TYPE_TRANSACTION_BOUNDARY,
};

enum BURN_EXECUTE_ACTION_TYPE
{
    BURN_EXECUTE_ACTION_TYPE_NONE,
    BURN_EXECUTE_ACTION_TYPE_CHECKPOINT,
    BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT,
    BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_MSP_TARGET,
    BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_SERVICE_STOP,
    BURN_EXECUTE_ACTION_TYPE_SERVICE_START,
    BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER,
    BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY,
    BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY,
    BURN_EXECUTE_ACTION_TYPE_REGISTRATION,
    BURN_EXECUTE_ACTION_TYPE_COMPATIBLE_PACKAGE,
};

enum BURN_CLEAN_ACTION_TYPE
{
    BURN_CLEAN_ACTION_TYPE_NONE,
    BURN_CLEAN_ACTION_TYPE_BUNDLE,
    BURN_CLEAN_ACTION_TYPE_PACKAGE,
};


// structs

typedef struct _BURN_EXTRACT_PAYLOAD
{
    BURN_PACKAGE* pPackage;
    BURN_PAYLOAD* pPayload;
    LPWSTR sczUnverifiedPath;
} BURN_EXTRACT_PAYLOAD;

typedef struct _BURN_DEPENDENT_REGISTRATION_ACTION
{
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type;
    LPWSTR sczBundleId;
    LPWSTR sczDependentProviderKey;
} BURN_DEPENDENT_REGISTRATION_ACTION;

typedef struct _BURN_CACHE_ACTION
{
    BURN_CACHE_ACTION_TYPE type;
    BOOL fSkipUntilRetried;
    union
    {
        struct
        {
            DWORD dwId;
        } checkpoint;
        struct
        {
            LPWSTR sczExecutableName;
            LPWSTR sczLayoutDirectory;
            LPWSTR sczUnverifiedPath;
            DWORD64 qwBundleSize;
        } bundleLayout;
        struct
        {
            BURN_PACKAGE* pPackage;
            DWORD cCachePayloads;
            DWORD64 qwCachePayloadSizeTotal;
            DWORD iPackageCompleteAction;
        } packageStart;
        struct
        {
            BURN_PACKAGE* pPackage;
        } packageStop;
        struct
        {
            BURN_PACKAGE* pPackage;
        } rollbackPackage;
        struct
        {
            HANDLE hEvent;
        } syncpoint;
        struct
        {
            BURN_CONTAINER* pContainer;
            LPWSTR sczUnverifiedPath;
        } resolveContainer;
        struct
        {
            BURN_CONTAINER* pContainer;
            DWORD64 qwTotalExtractSize;
            DWORD iSkipUntilAcquiredByAction;
            LPWSTR sczContainerUnverifiedPath;

            BURN_EXTRACT_PAYLOAD* rgPayloads;
            DWORD cPayloads;
        } extractContainer;
        struct
        {
            BURN_PACKAGE* pPackage;
            BURN_CONTAINER* pContainer;
            DWORD iTryAgainAction;
            DWORD cTryAgainAttempts;
            LPWSTR sczLayoutDirectory;
            LPWSTR sczUnverifiedPath;
            BOOL fMove;
        } layoutContainer;
        struct
        {
            BURN_PACKAGE* pPackage;
            BURN_PAYLOAD* pPayload;
            LPWSTR sczUnverifiedPath;
        } resolvePayload;
        struct
        {
            BURN_PACKAGE* pPackage;
            BURN_PAYLOAD* pPayload;
            DWORD iTryAgainAction;
            DWORD cTryAgainAttempts;
            LPWSTR sczUnverifiedPath;
            BOOL fMove;
        } cachePayload;
        struct
        {
            BURN_PACKAGE* pPackage;
            BURN_PAYLOAD* pPayload;
            DWORD iTryAgainAction;
            DWORD cTryAgainAttempts;
            LPWSTR sczLayoutDirectory;
            LPWSTR sczUnverifiedPath;
            BOOL fMove;
        } layoutPayload;
        struct
        {
            BURN_ROLLBACK_BOUNDARY* pRollbackBoundary;
            HANDLE hEvent;
        } rollbackBoundary;
    };
} BURN_CACHE_ACTION;

typedef struct _BURN_ORDERED_PATCHES
{
    DWORD dwOrder;
    BURN_PACKAGE* pPackage;
} BURN_ORDERED_PATCHES;

typedef struct _BURN_EXECUTE_ACTION
{
    BURN_EXECUTE_ACTION_TYPE type;
    BOOL fDeleted; // used to skip an action after it was planned since deleting actions out of the plan is too hard.
    union
    {
        struct
        {
            DWORD dwId;
        } checkpoint;
        struct
        {
            HANDLE hEvent;
        } syncpoint;
        struct
        {
            BURN_PACKAGE* pPackage;
        } uncachePackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            BOOL fFireAndForget;
            BOOTSTRAPPER_ACTION_STATE action;
            LPWSTR sczIgnoreDependencies;
            LPWSTR sczAncestors;
        } exePackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczLogPath;
            DWORD dwLoggingAttributes;
            INSTALLUILEVEL uiLevel;
            BOOTSTRAPPER_ACTION_STATE action;

            BOOTSTRAPPER_FEATURE_ACTION* rgFeatures;
            BOOTSTRAPPER_ACTION_STATE* rgSlipstreamPatches;

            BURN_ORDERED_PATCHES* rgOrderedPatches;
            DWORD cPatches;
        } msiPackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczTargetProductCode;
            BURN_PACKAGE* pChainedTargetPackage;
            BOOL fSlipstream;
            BOOL fPerMachineTarget;
            LPWSTR sczLogPath;
            INSTALLUILEVEL uiLevel;
            BOOTSTRAPPER_ACTION_STATE action;

            BURN_ORDERED_PATCHES* rgOrderedPatches;
            DWORD cOrderedPatches;
        } mspTarget;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczLogPath;
            BOOTSTRAPPER_ACTION_STATE action;
        } msuPackage;
        struct
        {
            LPWSTR sczServiceName;
        } service;
        struct
        {
            BOOL fKeep;
        } registration;
        struct
        {
            BURN_ROLLBACK_BOUNDARY* pRollbackBoundary;
        } rollbackBoundary;
        struct
        {
            BURN_PACKAGE* pPackage;
            BURN_DEPENDENCY_ACTION action;
        } packageProvider;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczBundleProviderKey;
            BURN_DEPENDENCY_ACTION action;
        } packageDependency;
        struct
        {
            BURN_PACKAGE* pReferencePackage;
            LPWSTR sczInstalledProductCode;
            DWORD64 qwInstalledVersion;
        } compatiblePackage;
    };
} BURN_EXECUTE_ACTION;

typedef struct _BURN_CLEAN_ACTION
{
    BURN_PACKAGE* pPackage;
} BURN_CLEAN_ACTION;

typedef struct _BURN_PLAN
{
    BOOTSTRAPPER_ACTION action;
    LPWSTR wzBundleId;          // points directly into parent the ENGINE_STATE.
    LPWSTR wzBundleProviderKey; // points directly into parent the ENGINE_STATE.
    BOOL fPerMachine;
    BOOL fRegister;
    DWORD dwRegistrationOperations;
    BOOL fKeepRegistrationDefault;
    BOOL fDisallowRemoval;

    DWORD64 qwCacheSizeTotal;

    DWORD64 qwEstimatedSize;

    DWORD cExecutePackagesTotal;
    DWORD cOverallProgressTicksTotal;

    BURN_DEPENDENCY_REGISTRATION_ACTION dependencyRegistrationAction;

    BURN_DEPENDENT_REGISTRATION_ACTION* rgRegistrationActions;
    DWORD cRegistrationActions;

    BURN_DEPENDENT_REGISTRATION_ACTION* rgRollbackRegistrationActions;
    DWORD cRollbackRegistrationActions;

    BURN_CACHE_ACTION* rgCacheActions;
    DWORD cCacheActions;

    BURN_CACHE_ACTION* rgRollbackCacheActions;
    DWORD cRollbackCacheActions;

    BURN_EXECUTE_ACTION* rgExecuteActions;
    DWORD cExecuteActions;

    BURN_EXECUTE_ACTION* rgRollbackActions;
    DWORD cRollbackActions;

    BURN_CLEAN_ACTION* rgCleanActions;
    DWORD cCleanActions;

    DEPENDENCY* rgPlannedProviders;
    UINT cPlannedProviders;
} BURN_PLAN;


// functions

void PlanReset(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGES* pPackages
    );
void PlanUninitializeExecuteAction(
    __in BURN_EXECUTE_ACTION* pExecuteAction
    );
HRESULT PlanSetVariables(
    __in BOOTSTRAPPER_ACTION action,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanDefaultPackageRequestState(
    __in BURN_PACKAGE_TYPE packageType,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __in BOOL fPermanent,
    __in BOOTSTRAPPER_ACTION action,
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR wzInstallCondition,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    );
HRESULT PlanLayoutBundle(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzExecutableName,
    __in DWORD64 qwBundleSize,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PAYLOADS* pPayloads,
    __out_z LPWSTR* psczLayoutDirectory
    );
HRESULT PlanPackages(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fBundleInstalled,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __inout HANDLE* phSyncpointEvent
    );
HRESULT PlanRegistration(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RESUME_TYPE resumeType,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z_opt LPCWSTR wzIgnoreDependencies,
    __out BOOL* pfContinuePlanning
    );
HRESULT PlanPassThroughBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout HANDLE* phSyncpointEvent
    );
HRESULT PlanUpdateBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout HANDLE* phSyncpointEvent
    );
HRESULT PlanLayoutPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzLayoutDirectory
    );
HRESULT PlanCachePackage(
    __in BOOL fPerMachine,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __out HANDLE* phSyncpointEvent
    );
HRESULT PlanExecutePackage(
    __in BOOL fPerMachine,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout HANDLE* phSyncpointEvent
    );
HRESULT PlanRelatedBundlesBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    );
HRESULT PlanRelatedBundlesComplete(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout HANDLE* phSyncpointEvent,
    __in DWORD dwExecuteActionEarlyIndex
    );
HRESULT PlanFinalizeActions(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanCleanPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
HRESULT PlanExecuteCacheSyncAndRollback(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT PlanExecuteCheckpoint(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanInsertExecuteAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanInsertRollbackAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    );
HRESULT PlanAppendExecuteAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanAppendRollbackAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanKeepRegistration(
    __in BURN_PLAN* pPlan,
    __in DWORD iAfterExecutePackageAction,
    __in DWORD iBeforeRollbackPackageAction
    );
HRESULT PlanRemoveRegistration(
    __in BURN_PLAN* pPlan,
    __in DWORD iAfterExecutePackageAction,
    __in DWORD iAfterRollbackPackageAction
    );
HRESULT PlanRollbackBoundaryBegin(
    __in BURN_PLAN* pPlan,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    );
HRESULT PlanRollbackBoundaryComplete(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanSetResumeCommand(
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_LOGGING* pLog
    );

#ifdef DEBUG
void PlanDump(
    __in BURN_PLAN* pPlan
    );
#endif

#if defined(__cplusplus)
}
#endif
