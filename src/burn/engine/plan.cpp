// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// internal struct definitions

struct PLAN_NONPERMANENT_PACKAGE_INDICES
{
    DWORD iAfterExecuteFirstNonPermanentPackage;
    DWORD iBeforeRollbackFirstNonPermanentPackage;
    DWORD iAfterExecuteLastNonPermanentPackage;
    DWORD iAfterRollbackLastNonPermanentPackage;
};

// internal function definitions

static void UninitializeRegistrationAction(
    __in BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    );
static void UninitializeCacheAction(
    __in BURN_CACHE_ACTION* pCacheAction
    );
static void ResetPlannedPackageState(
    __in BURN_PACKAGE* pPackage
    );
static HRESULT ProcessPackage(
    __in BOOL fBundlePerMachine,
    __in BOOL fPlanCompatible,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __inout HANDLE* phSyncpointEvent,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary,
    __in_opt PLAN_NONPERMANENT_PACKAGE_INDICES* pNonpermanentPackageIndices
    );
static HRESULT ProcessPackageRollbackBoundary(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    );
static HRESULT GetActionDefaultRequestState(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOL fPermanent,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    );
static HRESULT AddRegistrationAction(
    __in BURN_PLAN* pPlan,
    __in BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type,
    __in_z LPCWSTR wzDependentProviderKey,
    __in_z LPCWSTR wzOwnerBundleId
    );
static HRESULT AddCachePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __out HANDLE* phSyncpointEvent
    );
static HRESULT AddCachePackageHelper(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __out HANDLE* phSyncpointEvent
    );
static HRESULT AddCacheSlipstreamMsps(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
static BOOL AlreadyPlannedCachePackage(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzPackageId,
    __out HANDLE* phSyncpointEvent
    );
static DWORD GetNextCheckpointId();
static HRESULT AppendCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    );
static HRESULT AppendRollbackCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    );
static HRESULT AppendLayoutContainerAction(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_PACKAGE* pPackage,
    __in DWORD iPackageStartAction,
    __in BURN_CONTAINER* pContainer,
    __in BOOL fContainerCached,
    __in_z LPCWSTR wzLayoutDirectory
    );
static HRESULT AppendCacheOrLayoutPayloadAction(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_PACKAGE* pPackage,
    __in DWORD iPackageStartAction,
    __in BURN_PAYLOAD* pPayload,
    __in BOOL fPayloadCached,
    __in_z_opt LPCWSTR wzLayoutDirectory
    );
static BOOL FindContainerCacheAction(
    __in BURN_CACHE_ACTION_TYPE type,
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __in DWORD iSearchStart,
    __in DWORD iSearchEnd,
    __out_opt BURN_CACHE_ACTION** ppCacheAction,
    __out_opt DWORD* piCacheAction
    );
static HRESULT CreateContainerAcquireAndExtractAction(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __in DWORD iPackageStartAction,
    __in BOOL fPayloadCached,
    __out BURN_CACHE_ACTION** ppContainerExtractAction,
    __out DWORD* piContainerTryAgainAction
    );
static HRESULT AddAcquireContainer(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __out_opt BURN_CACHE_ACTION** ppCacheAction,
    __out_opt DWORD* piCacheAction
    );
static HRESULT AddExtractPayload(
    __in BURN_CACHE_ACTION* pCacheAction,
    __in_opt BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzPayloadWorkingPath
    );
static BURN_CACHE_ACTION* ProcessSharedPayload(
    __in BURN_PLAN* pPlan,
    __in BURN_PAYLOAD* pPayload
    );
static HRESULT RemoveUnnecessaryActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    );
static HRESULT FinalizeSlipstreamPatchActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    );
static HRESULT PlanDependencyActions(
    __in BOOL fBundlePerMachine,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
static HRESULT CalculateExecuteActions(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __out_opt BOOL* pfBARequestedCache
    );
static BOOL NeedsCache(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );

// function definitions

extern "C" void PlanReset(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGES* pPackages
    )
{
    if (pPlan->rgRegistrationActions)
    {
        for (DWORD i = 0; i < pPlan->cRegistrationActions; ++i)
        {
            UninitializeRegistrationAction(&pPlan->rgRegistrationActions[i]);
        }
        MemFree(pPlan->rgRegistrationActions);
    }

    if (pPlan->rgRollbackRegistrationActions)
    {
        for (DWORD i = 0; i < pPlan->cRollbackRegistrationActions; ++i)
        {
            UninitializeRegistrationAction(&pPlan->rgRollbackRegistrationActions[i]);
        }
        MemFree(pPlan->rgRollbackRegistrationActions);
    }

    if (pPlan->rgCacheActions)
    {
        for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
        {
            UninitializeCacheAction(&pPlan->rgCacheActions[i]);
        }
        MemFree(pPlan->rgCacheActions);
    }

    if (pPlan->rgExecuteActions)
    {
        for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
        {
            PlanUninitializeExecuteAction(&pPlan->rgExecuteActions[i]);
        }
        MemFree(pPlan->rgExecuteActions);
    }

    if (pPlan->rgRollbackActions)
    {
        for (DWORD i = 0; i < pPlan->cRollbackActions; ++i)
        {
            PlanUninitializeExecuteAction(&pPlan->rgRollbackActions[i]);
        }
        MemFree(pPlan->rgRollbackActions);
    }

    if (pPlan->rgCleanActions)
    {
        // Nothing needs to be freed inside clean actions today.
        MemFree(pPlan->rgCleanActions);
    }

    if (pPlan->rgPlannedProviders)
    {
        ReleaseDependencyArray(pPlan->rgPlannedProviders, pPlan->cPlannedProviders);
    }

    memset(pPlan, 0, sizeof(BURN_PLAN));

    // Reset the planned actions for each package.
    if (pPackages->rgPackages)
    {
        for (DWORD i = 0; i < pPackages->cPackages; ++i)
        {
            ResetPlannedPackageState(&pPackages->rgPackages[i]);
        }
    }
}

extern "C" void PlanUninitializeExecuteAction(
    __in BURN_EXECUTE_ACTION* pExecuteAction
    )
{
    switch (pExecuteAction->type)
    {
    case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
        ReleaseStr(pExecuteAction->exePackage.sczIgnoreDependencies);
        ReleaseStr(pExecuteAction->exePackage.sczAncestors);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
        ReleaseStr(pExecuteAction->msiPackage.sczLogPath);
        ReleaseMem(pExecuteAction->msiPackage.rgFeatures);
        ReleaseMem(pExecuteAction->msiPackage.rgSlipstreamPatches);
        ReleaseMem(pExecuteAction->msiPackage.rgOrderedPatches);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
        ReleaseStr(pExecuteAction->mspTarget.sczTargetProductCode);
        ReleaseStr(pExecuteAction->mspTarget.sczLogPath);
        ReleaseMem(pExecuteAction->mspTarget.rgOrderedPatches);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
        ReleaseStr(pExecuteAction->msuPackage.sczLogPath);
        break;

    case BURN_EXECUTE_ACTION_TYPE_SERVICE_STOP: __fallthrough;
    case BURN_EXECUTE_ACTION_TYPE_SERVICE_START:
        ReleaseStr(pExecuteAction->service.sczServiceName);
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
        ReleaseStr(pExecuteAction->packageDependency.sczBundleProviderKey);
        break;

    case BURN_EXECUTE_ACTION_TYPE_COMPATIBLE_PACKAGE:
        ReleaseStr(pExecuteAction->compatiblePackage.sczInstalledProductCode);
        break;
    }
}

extern "C" HRESULT PlanSetVariables(
    __in BOOTSTRAPPER_ACTION action,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    hr = VariableSetNumeric(pVariables, BURN_BUNDLE_ACTION, action, TRUE);
    ExitOnFailure(hr, "Failed to set the bundle action built-in variable.");

LExit:
    return hr;
}

extern "C" HRESULT PlanDefaultPackageRequestState(
    __in BURN_PACKAGE_TYPE packageType,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __in BOOL fPermanent,
    __in BOOTSTRAPPER_ACTION action,
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR wzInstallCondition,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_REQUEST_STATE defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
    BOOL fCondition = FALSE;

    // If doing layout, then always default to requesting the file be cached.
    if (BOOTSTRAPPER_ACTION_LAYOUT == action)
    {
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_CACHE;
    }
    else if (BOOTSTRAPPER_RELATION_PATCH == relationType && BURN_PACKAGE_TYPE_MSP == packageType)
    {
        // For patch related bundles, only install a patch if currently absent during install, modify, or repair.
        if (BOOTSTRAPPER_PACKAGE_STATE_ABSENT == currentState && BOOTSTRAPPER_ACTION_INSTALL <= action)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
        }
        else
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
        }
    }
    else if (BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED == currentState && BOOTSTRAPPER_ACTION_UNINSTALL != action)
    {
        // Superseded means the package is on the machine but not active, so only uninstall operations are allowed.
        // All other operations do nothing.
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
    }
    else if (BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE == currentState && !(BOOTSTRAPPER_ACTION_UNINSTALL == action && BURN_PACKAGE_TYPE_MSP == packageType))
    {
        // Obsolete means the package is not on the machine and should not be installed, *except* patches can be obsolete
        // and present so allow them to be removed during uninstall. Everyone else, gets nothing.
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
    }
    else // pick the best option for the action state and install condition.
    {
        hr = GetActionDefaultRequestState(action, fPermanent, currentState, &defaultRequestState);
        ExitOnFailure(hr, "Failed to get default request state for action.");

        // If there is an install condition (and we're doing an install) evaluate the condition
        // to determine whether to use the default request state or make the package absent.
        if (BOOTSTRAPPER_ACTION_UNINSTALL != action && wzInstallCondition && *wzInstallCondition)
        {
            hr = ConditionEvaluate(pVariables, wzInstallCondition, &fCondition);
            ExitOnFailure(hr, "Failed to evaluate install condition.");

            *pRequestState = fCondition ? defaultRequestState : BOOTSTRAPPER_REQUEST_STATE_ABSENT;
        }
        else // just set the package to the default request state.
        {
            *pRequestState = defaultRequestState;
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanLayoutBundle(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzExecutableName,
    __in DWORD64 qwBundleSize,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PAYLOADS* pPayloads,
    __out_z LPWSTR* psczLayoutDirectory
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    LPWSTR sczExecutablePath = NULL;
    LPWSTR sczLayoutDirectory = NULL;

    // Get the layout directory.
    hr = VariableGetString(pVariables, BURN_BUNDLE_LAYOUT_DIRECTORY, &sczLayoutDirectory);
    if (E_NOTFOUND == hr) // if not set, use the current directory as the layout directory.
    {
        hr = VariableGetString(pVariables, BURN_BUNDLE_SOURCE_PROCESS_FOLDER, &sczLayoutDirectory);
        if (E_NOTFOUND == hr) // if not set, use the current directory as the layout directory.
        {
            hr = PathForCurrentProcess(&sczExecutablePath, NULL);
            ExitOnFailure(hr, "Failed to get path for current executing process as layout directory.");

            hr = PathGetDirectory(sczExecutablePath, &sczLayoutDirectory);
            ExitOnFailure(hr, "Failed to get executing process as layout directory.");
        }
    }
    ExitOnFailure(hr, "Failed to get bundle layout directory property.");

    hr = PathBackslashTerminate(&sczLayoutDirectory);
    ExitOnFailure(hr, "Failed to ensure layout directory is backslash terminated.");

    // Plan the layout of the bundle engine itself.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append bundle start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE;

    hr = StrAllocString(&pCacheAction->bundleLayout.sczExecutableName, wzExecutableName, 0);
    ExitOnFailure(hr, "Failed to to copy executable name for bundle.");

    hr = StrAllocString(&pCacheAction->bundleLayout.sczLayoutDirectory, sczLayoutDirectory, 0);
    ExitOnFailure(hr, "Failed to to copy layout directory for bundle.");

    hr = CacheCalculateBundleLayoutWorkingPath(pPlan->wzBundleId, &pCacheAction->bundleLayout.sczUnverifiedPath);
    ExitOnFailure(hr, "Failed to calculate bundle layout working path.");

    pCacheAction->bundleLayout.qwBundleSize = qwBundleSize;

    pPlan->qwCacheSizeTotal += qwBundleSize;

    ++pPlan->cOverallProgressTicksTotal;

    // Plan the layout of layout-only payloads.
    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = pPayloads->rgPayloads + i;
        if (pPayload->fLayoutOnly)
        {
            // TODO: determine if a payload already exists in the layout and pass appropriate value fPayloadCached
            // (instead of always FALSE).
            hr = AppendCacheOrLayoutPayloadAction(pPlan, NULL, BURN_PLAN_INVALID_ACTION_INDEX, pPayload, FALSE, sczLayoutDirectory);
            ExitOnFailure(hr, "Failed to plan layout payload.");
        }
    }

    *psczLayoutDirectory = sczLayoutDirectory;
    sczLayoutDirectory = NULL;

LExit:
    ReleaseStr(sczLayoutDirectory);
    ReleaseStr(sczExecutablePath);

    return hr;
}

extern "C" HRESULT PlanPackages(
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
    )
{
    HRESULT hr = S_OK;
    BOOL fBundlePerMachine = pPlan->fPerMachine; // bundle is per-machine if plan starts per-machine.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    PLAN_NONPERMANENT_PACKAGE_INDICES nonpermanentPackageIndices;
    nonpermanentPackageIndices.iAfterExecuteFirstNonPermanentPackage = BURN_PLAN_INVALID_ACTION_INDEX;
    nonpermanentPackageIndices.iBeforeRollbackFirstNonPermanentPackage = BURN_PLAN_INVALID_ACTION_INDEX;
    nonpermanentPackageIndices.iAfterExecuteLastNonPermanentPackage = BURN_PLAN_INVALID_ACTION_INDEX;
    nonpermanentPackageIndices.iAfterRollbackLastNonPermanentPackage = BURN_PLAN_INVALID_ACTION_INDEX;

    // Plan the packages.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        DWORD iPackage = (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action) ? pPackages->cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = pPackages->rgPackages + iPackage;

        // Support passing Ancestors to embedded burn bundles
        if (BURN_PACKAGE_TYPE_EXE == pPackage->type && BURN_EXE_PROTOCOL_TYPE_BURN == pPackage->Exe.protocol)
        {
            // Pass along any ancestors and ourself to prevent infinite loops.
            if (pRegistration->sczAncestors && *pRegistration->sczAncestors)
            {
                hr = StrAllocFormatted(&pPackage->Exe.sczAncestors, L"%ls;%ls", pRegistration->sczAncestors, pRegistration->sczId);
                ExitOnFailure(hr, "Failed to copy ancestors and self to related bundle ancestors.");
            }
            else
            {
                hr = StrAllocString(&pPackage->Exe.sczAncestors, pRegistration->sczId, 0);
                ExitOnFailure(hr, "Failed to copy self to related bundle ancestors.");
            }
        }

        hr = ProcessPackage(fBundlePerMachine, FALSE, pUX, pPlan, pPackage, pLog, pVariables, display, relationType, wzLayoutDirectory, phSyncpointEvent, &pRollbackBoundary, &nonpermanentPackageIndices);
        ExitOnFailure(hr, "Failed to process package.");

        // Attempt to remove orphaned packages during uninstall. Currently only MSI packages are supported and should not require source.
        if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action && BURN_PACKAGE_TYPE_MSI == pPackage->type && pPackage->Msi.fCompatibleInstalled)
        {
            BURN_PACKAGE* pCompatiblePackage = NULL;
            BURN_EXECUTE_ACTION* pAction = NULL;

            // Add the compatible package to the list.
            hr = MsiEngineAddCompatiblePackage(pPackages, pPackage, &pCompatiblePackage);
            ExitOnFailure(hr, "Failed to add compatible package for package: %ls", pPackage->sczId);

            // Plan to load the compatible package into the elevated engine before its needed.
            hr = PlanAppendExecuteAction(pPlan, &pAction);
            ExitOnFailure(hr, "Failed to append execute action.");

            pAction->type = BURN_EXECUTE_ACTION_TYPE_COMPATIBLE_PACKAGE;
            pAction->compatiblePackage.pReferencePackage = pPackage;
            pAction->compatiblePackage.qwInstalledVersion = pCompatiblePackage->Msi.qwVersion;

            hr = StrAllocString(&pAction->compatiblePackage.sczInstalledProductCode, pCompatiblePackage->Msi.sczProductCode, 0);
            ExitOnFailure(hr, "Failed to copy installed ProductCode");

            // Process the compatible MSI package like any other.
            hr = ProcessPackage(fBundlePerMachine, TRUE, pUX, pPlan, pCompatiblePackage, pLog, pVariables, display, relationType, wzLayoutDirectory, phSyncpointEvent, &pRollbackBoundary, &nonpermanentPackageIndices);
            ExitOnFailure(hr, "Failed to process compatible package.");

            if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pCompatiblePackage->execute)
            {
                LogId(REPORT_STANDARD, MSG_PLANNED_ORPHAN_PACKAGE_FROM_PROVIDER, pPackage->sczId, pCompatiblePackage->Msi.sczProductCode, pPackage->Msi.sczProductCode);
            }
        }
    }

    // Insert the "keep registration" and "remove registration" actions in the plan when installing the first time and anytime we are uninstalling respectively.
    if (!fBundleInstalled && (BOOTSTRAPPER_ACTION_INSTALL == pPlan->action || BOOTSTRAPPER_ACTION_MODIFY == pPlan->action || BOOTSTRAPPER_ACTION_REPAIR == pPlan->action))
    {
        if (BURN_PLAN_INVALID_ACTION_INDEX == nonpermanentPackageIndices.iAfterExecuteFirstNonPermanentPackage)
        {
            nonpermanentPackageIndices.iAfterExecuteFirstNonPermanentPackage = pPlan->cExecuteActions;
            nonpermanentPackageIndices.iBeforeRollbackFirstNonPermanentPackage = pPlan->cRollbackActions;
        }

        hr = PlanKeepRegistration(pPlan, nonpermanentPackageIndices.iAfterExecuteFirstNonPermanentPackage, nonpermanentPackageIndices.iBeforeRollbackFirstNonPermanentPackage);
        ExitOnFailure(hr, "Failed to plan install keep registration.");
    }
    else if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
    {
        if (BURN_PLAN_INVALID_ACTION_INDEX == nonpermanentPackageIndices.iAfterExecuteLastNonPermanentPackage)
        {
            nonpermanentPackageIndices.iAfterExecuteLastNonPermanentPackage = pPlan->cExecuteActions;
            nonpermanentPackageIndices.iAfterRollbackLastNonPermanentPackage = pPlan->cRollbackActions;
        }

        hr = PlanRemoveRegistration(pPlan, nonpermanentPackageIndices.iAfterExecuteLastNonPermanentPackage, nonpermanentPackageIndices.iAfterRollbackLastNonPermanentPackage);
        ExitOnFailure(hr, "Failed to plan uninstall remove registration.");
    }

    // If we still have an open rollback boundary, complete it.
    if (pRollbackBoundary)
    {
        hr = PlanRollbackBoundaryComplete(pPlan);
        ExitOnFailure(hr, "Failed to plan rollback boundary begin.");

        pRollbackBoundary = NULL;
    }

    // Plan clean up of packages.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        DWORD iPackage = (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action) ? pPackages->cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = pPackages->rgPackages + iPackage;

        hr = PlanCleanPackage(pPlan, pPackage);
        ExitOnFailure(hr, "Failed to plan clean package.");
    }

    // Plan best-effort clean up of compatible packages.
    for (DWORD i = 0; i < pPackages->cCompatiblePackages; ++i)
    {
        DWORD iPackage = (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action) ? pPackages->cCompatiblePackages - 1 - i : i;
        BURN_PACKAGE* pCompatiblePackage = pPackages->rgCompatiblePackages + iPackage;

        PlanCleanPackage(pPlan, pCompatiblePackage);
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRegistration(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RESUME_TYPE resumeType,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z_opt LPCWSTR wzIgnoreDependencies,
    __out BOOL* pfContinuePlanning
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSelfDependent = NULL;
    STRINGDICT_HANDLE sdIgnoreDependents = NULL;
    DEPENDENCY* rgDependencies = NULL;
    UINT cDependencies = 0;

    pPlan->fRegister = TRUE; // register the bundle since we're modifying machine state.

    // Keep the registration if the bundle was already installed or we are planning after a restart.
    pPlan->fKeepRegistrationDefault = (pRegistration->fInstalled || BOOTSTRAPPER_RESUME_TYPE_REBOOT == resumeType);

    pPlan->fDisallowRemoval = FALSE; // by default the bundle can be planned to be removed

    // If no parent was specified at all, use the bundle id as the self dependent.
    if (!pRegistration->sczActiveParent)
    {
        wzSelfDependent = pRegistration->sczId;
    }
    else if (*pRegistration->sczActiveParent) // if parent was specified use that as the self dependent.
    {
        wzSelfDependent = pRegistration->sczActiveParent;
    }
    // else parent:none was used which means we should not register a dependency on ourself.

    if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
    {
        // If our provider key was detected and it points to our current bundle then we can
        // unregister the bundle dependency.
        if (pRegistration->sczDetectedProviderKeyBundleId &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczId, -1, pRegistration->sczDetectedProviderKeyBundleId, -1))
        {
            pPlan->dependencyRegistrationAction = BURN_DEPENDENCY_REGISTRATION_ACTION_UNREGISTER;
        }
        else // log that another bundle already owned our registration, hopefully this only happens when a newer version
        {    // of a bundle installed and is in the process of upgrading us.
            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_PROVIDER_KEY_REMOVAL, pRegistration->sczProviderKey, pRegistration->sczDetectedProviderKeyBundleId);
        }

        // Create the dictionary of dependents that should be ignored.
        hr = DictCreateStringList(&sdIgnoreDependents, 5, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create the string dictionary.");

        // If the self-dependent dependent exists, plan its removal. If we did not do this, we
        // would prevent self-removal.
        if (wzSelfDependent && DependencyDependentExists(pRegistration, wzSelfDependent))
        {
            hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER, wzSelfDependent, pRegistration->sczId);
            ExitOnFailure(hr, "Failed to allocate registration action.");

            hr = DependencyAddIgnoreDependencies(sdIgnoreDependents, wzSelfDependent);
            ExitOnFailure(hr, "Failed to add self-dependent to ignore dependents.");
        }

        // If we are not doing an upgrade, we check to see if there are still dependents on us and if so we skip planning.
        // However, when being upgraded, we always execute our uninstall because a newer version of us is probably
        // already on the machine and we need to clean up the stuff specific to this bundle.
        if (BOOTSTRAPPER_RELATION_UPGRADE != relationType)
        {
            // If there were other dependencies to ignore, add them.
            if (wzIgnoreDependencies && *wzIgnoreDependencies)
            {
                hr = DependencyAddIgnoreDependencies(sdIgnoreDependents, wzIgnoreDependencies);
                ExitOnFailure(hr, "Failed to add dependents ignored from command-line.");
            }

            // For addon or patch bundles, dependent related bundles should be ignored. This allows
            // that addon or patch to be removed even though bundles it targets still are registered.
            for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
            {
                const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

                if (BOOTSTRAPPER_RELATION_DEPENDENT == pRelatedBundle->relationType)
                {
                    for (DWORD j = 0; j < pRelatedBundle->package.cDependencyProviders; ++j)
                    {
                        const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders + j;

                        hr = DependencyAddIgnoreDependencies(sdIgnoreDependents, pProvider->sczKey);
                        ExitOnFailure(hr, "Failed to add dependent bundle provider key to ignore dependents.");
                    }
                }
            }

            // If there are any (non-ignored and not-planned-to-be-removed) dependents left, uninstall.
            hr = DepCheckDependents(pRegistration->hkRoot, pRegistration->sczProviderKey, 0, sdIgnoreDependents, &rgDependencies, &cDependencies);
            if (E_FILENOTFOUND == hr)
            {
                hr = S_OK;
            }
            else if (SUCCEEDED(hr) && cDependencies)
            {
                // TODO: callback to the BA and let it have the option to ignore any of these dependents?

                 pPlan->fDisallowRemoval = TRUE; // ensure the registration stays
                 *pfContinuePlanning = FALSE; // skip the rest of planning.

                 LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_DUE_TO_DEPENDENTS, cDependencies);
            }
            ExitOnFailure(hr, "Failed to check for remaining dependents during planning.");
        }
    }
    else
    {
        BOOL fAddonOrPatchBundle = (pRegistration->cAddonCodes || pRegistration->cPatchCodes);

        // If the bundle is not cached or will not be cached after restart, ensure the bundle is cached.
        if (!FileExistsAfterRestart(pRegistration->sczCacheExecutablePath, NULL))
        {
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE;
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION;
        }
        else if (BOOTSTRAPPER_ACTION_REPAIR == pPlan->action && !CacheBundleRunningFromCache()) // repairing but not not running from the cache.
        {
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE;
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION;
        }
        else if (BOOTSTRAPPER_ACTION_REPAIR == pPlan->action) // just repair, make sure the registration is "fixed up".
        {
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_REGISTRATION;
        }

        // Always update our estimated size registration when installing/modify/repair since things
        // may have been added or removed or it just needs to be "fixed up".
        pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_UPDATE_SIZE;

        // Always plan to write our provider key registration when installing/modify/repair to "fix it"
        // if broken.
        pPlan->dependencyRegistrationAction = BURN_DEPENDENCY_REGISTRATION_ACTION_REGISTER;

        // Register each dependent related bundle. The ensures that addons and patches are reference
        // counted and stick around until the last targeted bundle is removed.
        for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
        {
            const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

            if (BOOTSTRAPPER_RELATION_DEPENDENT == pRelatedBundle->relationType)
            {
                for (DWORD j = 0; j < pRelatedBundle->package.cDependencyProviders; ++j)
                {
                    const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders + j;

                    if (!DependencyDependentExists(pRegistration, pProvider->sczKey))
                    {
                        hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER, pProvider->sczKey, pRelatedBundle->package.sczId);
                        ExitOnFailure(hr, "Failed to add registration action for dependent related bundle.");
                    }
                }
            }
        }

        // Only do the following if we decided there was a dependent self to register. If so and and an explicit parent was
        // provided, register dependent self. Otherwise, if this bundle is not an addon or patch bundle then self-regisiter
        // as our own dependent.
        if (wzSelfDependent && (pRegistration->sczActiveParent || !fAddonOrPatchBundle))
        {
            if (!DependencyDependentExists(pRegistration, wzSelfDependent))
            {
                hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER, wzSelfDependent, pRegistration->sczId);
                ExitOnFailure(hr, "Failed to add registration action for self dependent.");
            }
        }
    }

LExit:
    ReleaseDict(sdIgnoreDependents);
    ReleaseDependencyArray(rgDependencies, cDependencies);

    return hr;
}

extern "C" HRESULT PlanPassThroughBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout HANDLE* phSyncpointEvent
    )
{
    HRESULT hr = S_OK;
    BOOL fBundlePerMachine = pPlan->fPerMachine; // bundle is per-machine if plan starts per-machine.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    // Plan passthrough package.
    hr = ProcessPackage(fBundlePerMachine, FALSE, pUX, pPlan, pPackage, pLog, pVariables, display, relationType, NULL, phSyncpointEvent, &pRollbackBoundary, NULL);
    ExitOnFailure(hr, "Failed to process passthrough package.");

    // If we still have an open rollback boundary, complete it.
    if (pRollbackBoundary)
    {
        hr = PlanRollbackBoundaryComplete(pPlan);
        ExitOnFailure(hr, "Failed to plan rollback boundary for passthrough package.");
    }

    // Notice that the PlanCleanPackage() function is purposefully missing here. Passthrough packages
    // are never cleaned up by the calling bundle (they delete themselves when appropriate) so we don't
    // need to plan clean up.

LExit:
    return hr;
}

extern "C" HRESULT PlanUpdateBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout HANDLE* phSyncpointEvent
    )
{
    HRESULT hr = S_OK;
    BOOL fBundlePerMachine = pPlan->fPerMachine; // bundle is per-machine if plan starts per-machine.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    // Plan update package.
    hr = ProcessPackage(fBundlePerMachine, FALSE, pUX, pPlan, pPackage, pLog, pVariables, display, relationType, NULL, phSyncpointEvent, &pRollbackBoundary, NULL);
    ExitOnFailure(hr, "Failed to process update package.");

    // If we still have an open rollback boundary, complete it.
    if (pRollbackBoundary)
    {
        hr = PlanRollbackBoundaryComplete(pPlan);
        ExitOnFailure(hr, "Failed to plan rollback boundary for update package.");
    }

    // Plan clean up of update package.
    hr = PlanCleanPackage(pPlan, pPackage);
    ExitOnFailure(hr, "Failed to plan clean of update package.");

LExit:
    return hr;
}

static HRESULT ProcessPackage(
    __in BOOL fBundlePerMachine,
    __in BOOL fPlanCompatible,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __inout HANDLE* phSyncpointEvent,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary,
    __in_opt PLAN_NONPERMANENT_PACKAGE_INDICES* pNonpermanentPackageIndices
    )
{
    HRESULT hr = S_OK;
    BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary = NULL;

    // Remember the default requested state so the engine doesn't get blamed for planning the wrong thing if the UX changes it.
    hr = PlanDefaultPackageRequestState(pPackage->type, pPackage->currentState, !pPackage->fUninstallable, pPlan->action, pVariables, pPackage->sczInstallCondition, relationType, &pPackage->defaultRequested);
    ExitOnFailure(hr, "Failed to set default package state.");

    pPackage->requested = pPackage->defaultRequested;

    int nResult = IDNOACTION;
    if (fPlanCompatible)
    {
        nResult = pUX->pUserExperience->OnPlanCompatiblePackage(pPackage->sczId, &pPackage->requested);
    }
    else
    {
        nResult = pUX->pUserExperience->OnPlanPackageBegin(pPackage->sczId, &pPackage->requested);
    }

    hr = UserExperienceInterpretResult(pUX, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted plan package begin.");

    pEffectiveRollbackBoundary = (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action) ? pPackage->pRollbackBoundaryBackward : pPackage->pRollbackBoundaryForward;
    hr = ProcessPackageRollbackBoundary(pPlan, pEffectiveRollbackBoundary, ppRollbackBoundary);
    ExitOnFailure(hr, "Failed to process package rollback boundary.");

    // If the package is in a requested state, plan it.
    if (BOOTSTRAPPER_REQUEST_STATE_NONE != pPackage->requested)
    {
        if (BOOTSTRAPPER_ACTION_LAYOUT == pPlan->action)
        {
            hr = PlanLayoutPackage(pPlan, pPackage, wzLayoutDirectory);
            ExitOnFailure(hr, "Failed to plan layout package.");
        }
        else
        {
            if (pPackage->fUninstallable && pNonpermanentPackageIndices)
            {
                if (BURN_PLAN_INVALID_ACTION_INDEX == pNonpermanentPackageIndices->iBeforeRollbackFirstNonPermanentPackage)
                {
                    pNonpermanentPackageIndices->iBeforeRollbackFirstNonPermanentPackage = pPlan->cRollbackActions;
                }
            }

            hr = PlanExecutePackage(fBundlePerMachine, display, pUX, pPlan, pPackage, pLog, pVariables, phSyncpointEvent);
            ExitOnFailure(hr, "Failed to plan execute package.");

            if (pPackage->fUninstallable && pNonpermanentPackageIndices)
            {
                if (BURN_PLAN_INVALID_ACTION_INDEX == pNonpermanentPackageIndices->iAfterExecuteFirstNonPermanentPackage)
                {
                    pNonpermanentPackageIndices->iAfterExecuteFirstNonPermanentPackage = pPlan->cExecuteActions - 1;
                }

                pNonpermanentPackageIndices->iAfterExecuteLastNonPermanentPackage = pPlan->cExecuteActions;
                pNonpermanentPackageIndices->iAfterRollbackLastNonPermanentPackage = pPlan->cRollbackActions;
            }
        }
    }
    else if (BOOTSTRAPPER_ACTION_LAYOUT != pPlan->action)
    {
        // All packages that have cacheType set to always should be cached if the bundle is going to be present.
        if (BURN_CACHE_TYPE_ALWAYS == pPackage->cacheType && BOOTSTRAPPER_ACTION_INSTALL <= pPlan->action)
        {
            hr = PlanCachePackage(fBundlePerMachine, pUX, pPlan, pPackage, pVariables, phSyncpointEvent);
            ExitOnFailure(hr, "Failed to plan cache package.");
        }
        else
        {
            // Make sure the package is properly ref-counted even if no plan is requested.
            hr = PlanDependencyActions(fBundlePerMachine, pPlan, pPackage);
            ExitOnFailure(hr, "Failed to plan dependency actions for package: %ls", pPackage->sczId);
        }
    }

    // Add the checkpoint after each package and dependency registration action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute || BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback || BURN_DEPENDENCY_ACTION_NONE != pPackage->dependencyExecute)
    {
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append execute checkpoint.");
    }

LExit:
    pUX->pUserExperience->OnPlanPackageComplete(pPackage->sczId, hr, pPackage->currentState, pPackage->requested, pPackage->execute, pPackage->rollback);
    return hr;
}

static HRESULT ProcessPackageRollbackBoundary(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    )
{
    HRESULT hr = S_OK;

    // If the package marks the start of a rollback boundary, start a new one.
    if (pEffectiveRollbackBoundary)
    {
        // Complete previous rollback boundary.
        if (*ppRollbackBoundary)
        {
            hr = PlanRollbackBoundaryComplete(pPlan);
            ExitOnFailure(hr, "Failed to plan rollback boundary complete.");
        }

        // Start new rollback boundary.
        hr = PlanRollbackBoundaryBegin(pPlan, pEffectiveRollbackBoundary);
        ExitOnFailure(hr, "Failed to plan rollback boundary begin.");

        *ppRollbackBoundary = pEffectiveRollbackBoundary;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanLayoutPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzLayoutDirectory
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    DWORD iPackageStartAction = 0;

    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append package start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_PACKAGE_START;
    pCacheAction->packageStart.pPackage = pPackage;

    // Remember the index for the package start action (which is now the last in the cache
    // actions array) because the array may be resized later and move around in memory.
    iPackageStartAction = pPlan->cCacheActions - 1;

    // If any of the package payloads are not cached, add them to the plan.
    for (DWORD i = 0; i < pPackage->cPayloads; ++i)
    {
        BURN_PACKAGE_PAYLOAD* pPackagePayload = &pPackage->rgPayloads[i];

        // If doing layout and the package is in a container.
        if (wzLayoutDirectory && pPackagePayload->pPayload->pContainer)
        {
            // TODO: determine if a container already exists in the layout and pass appropriate value fPayloadCached (instead of always FALSE).
            hr = AppendLayoutContainerAction(pPlan, pPackage, iPackageStartAction, pPackagePayload->pPayload->pContainer, FALSE, wzLayoutDirectory);
            ExitOnFailure(hr, "Failed to append layout container action.");
        }
        else
        {
            // TODO: determine if a payload already exists in the layout and pass appropriate value fPayloadCached (instead of always FALSE).
            hr = AppendCacheOrLayoutPayloadAction(pPlan, pPackage, iPackageStartAction, pPackagePayload->pPayload, FALSE, wzLayoutDirectory);
            ExitOnFailure(hr, "Failed to append cache/layout payload action.");
        }

        Assert(BURN_CACHE_ACTION_TYPE_PACKAGE_START == pPlan->rgCacheActions[iPackageStartAction].type);
        ++pPlan->rgCacheActions[iPackageStartAction].packageStart.cCachePayloads;
        pPlan->rgCacheActions[iPackageStartAction].packageStart.qwCachePayloadSizeTotal += pPackagePayload->pPayload->qwFileSize;
    }

    // Create package stop action.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append cache action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_PACKAGE_STOP;
    pCacheAction->packageStop.pPackage = pPackage;

    // Update the start action with the location of the complete action.
    pPlan->rgCacheActions[iPackageStartAction].packageStart.iPackageCompleteAction = pPlan->cCacheActions - 1;

    ++pPlan->cOverallProgressTicksTotal;

LExit:
    return hr;
}

extern "C" HRESULT PlanCachePackage(
    __in BOOL fPerMachine,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __out HANDLE* phSyncpointEvent
    )
{
    HRESULT hr = S_OK;
    BOOL fBARequestedCache = FALSE;

    // Calculate the execute actions because we need them to decide whether the package should be cached.
    hr = CalculateExecuteActions(pUserExperience, pPackage, pVariables, &fBARequestedCache);
    ExitOnFailure(hr, "Failed to calculate execute actions for package: %ls", pPackage->sczId);

    if (fBARequestedCache || NeedsCache(pPlan, pPackage))
    {
        // The behavior for cache only mode is to do nothing on rollback, e.g. for subsequent install on demand scenarios.
        pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

        hr = AddCachePackage(pPlan, pPackage, phSyncpointEvent);
        ExitOnFailure(hr, "Failed to plan cache package.");

        // Update plan state to account for the package being cached.
        pPlan->qwEstimatedSize += pPackage->qwSize;
        if (pPackage->fPerMachine)
        {
            pPlan->fPerMachine = TRUE;
        }
    }

    // Make sure the package is properly ref-counted.
    hr = PlanDependencyActions(fPerMachine, pPlan, pPackage);
    ExitOnFailure(hr, "Failed to plan dependency actions for package: %ls", pPackage->sczId);

LExit:
    return hr;
}

extern "C" HRESULT PlanExecutePackage(
    __in BOOL fPerMachine,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout HANDLE* phSyncpointEvent
    )
{
    HRESULT hr = S_OK;
    BOOL fBARequestedCache = FALSE;

    hr = CalculateExecuteActions(pUserExperience, pPackage, pVariables, &fBARequestedCache);
    ExitOnFailure(hr, "Failed to calculate plan actions for package: %ls", pPackage->sczId);

    // Calculate package states based on reference count and plan certain dependency actions prior to planning the package execute action.
    hr = DependencyPlanPackageBegin(fPerMachine, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to begin plan dependency actions for package: %ls", pPackage->sczId);

    if (fBARequestedCache || NeedsCache(pPlan, pPackage))
    {
        hr = AddCachePackage(pPlan, pPackage, phSyncpointEvent);
        ExitOnFailure(hr, "Failed to plan cache package.");
    }
    else if (BURN_CACHE_STATE_COMPLETE != pPackage->cache && // if the package is not in the cache, disable any rollback that would require the package from the cache.
             (BOOTSTRAPPER_ACTION_STATE_UNINSTALL < pPackage->rollback || (BURN_PACKAGE_TYPE_EXE == pPackage->type && BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback))
            )
    {
        LogId(REPORT_STANDARD, MSG_PLAN_DISABLING_ROLLBACK_NO_CACHE, pPackage->sczId, LoggingCacheStateToString(pPackage->cache), LoggingActionStateToString(pPackage->rollback));
        pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
    }


    // Add the cache and install size to estimated size if it will be on the machine at the end of the install
    if (BOOTSTRAPPER_REQUEST_STATE_PRESENT == pPackage->requested || 
        (BOOTSTRAPPER_PACKAGE_STATE_PRESENT == pPackage->currentState && BOOTSTRAPPER_REQUEST_STATE_ABSENT < pPackage->requested) || 
        BURN_CACHE_TYPE_ALWAYS == pPackage->cacheType
       )
    {
        // If the package will remain in the cache, add the package size to the estimated size
        if (BURN_CACHE_TYPE_YES <= pPackage->cacheType)
        {
            pPlan->qwEstimatedSize += pPackage->qwSize;
        }

        // If the package will end up installed on the machine, add the install size to the estimated size.
        if (BOOTSTRAPPER_REQUEST_STATE_CACHE < pPackage->requested)
        {
            // MSP packages get cached automatically by windows installer with any embedded cabs, so include that in the size as well
            if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
            {
                pPlan->qwEstimatedSize += pPackage->qwSize;
            }

            pPlan->qwEstimatedSize += pPackage->qwInstallSize;
        }
    }

    // Add execute actions.
    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_EXE:
        hr = ExeEnginePlanAddPackage(NULL, pPackage, pPlan, pLog, pVariables, *phSyncpointEvent, pPackage->fAcquire);
        break;

    case BURN_PACKAGE_TYPE_MSI:
        hr = MsiEnginePlanAddPackage(display, pPackage, pPlan, pLog, pVariables, *phSyncpointEvent, pPackage->fAcquire);
        break;

    case BURN_PACKAGE_TYPE_MSP:
        hr = MspEnginePlanAddPackage(display, pPackage, pPlan, pLog, pVariables, *phSyncpointEvent, pPackage->fAcquire);
        break;

    case BURN_PACKAGE_TYPE_MSU:
        hr = MsuEnginePlanAddPackage(pPackage, pPlan, pLog, pVariables, *phSyncpointEvent, pPackage->fAcquire);
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid package type.");
    }
    ExitOnFailure(hr, "Failed to add plan actions for package: %ls", pPackage->sczId);

    // Plan certain dependency actions after planning the package execute action.
    hr = DependencyPlanPackageComplete(pPackage, pPlan);
    ExitOnFailure(hr, "Failed to complete plan dependency actions for package: %ls", pPackage->sczId);

    // If we are going to take any action on this package, add progress for it.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute || BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        LoggingIncrementPackageSequence();

        ++pPlan->cExecutePackagesTotal;
        ++pPlan->cOverallProgressTicksTotal;

        // If package is per-machine and is being executed, flag the plan to be per-machine as well.
        if (pPackage->fPerMachine)
        {
            pPlan->fPerMachine = TRUE;
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRelatedBundlesBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    LPWSTR* rgsczAncestors = NULL;
    UINT cAncestors = 0;
    STRINGDICT_HANDLE sdAncestors = NULL;

    if (pRegistration->sczAncestors)
    {
        hr = StrSplitAllocArray(&rgsczAncestors, &cAncestors, pRegistration->sczAncestors, L";");
        ExitOnFailure(hr, "Failed to create string array from ancestors.");

        hr = DictCreateStringListFromArray(&sdAncestors, rgsczAncestors, cAncestors, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create dictionary from ancestors array.");
    }

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;
        pRelatedBundle->package.defaultRequested = BOOTSTRAPPER_REQUEST_STATE_NONE;
        pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_NONE;

        // Do not execute the same bundle twice.
        if (sdAncestors)
        {
            hr = DictKeyExists(sdAncestors, pRelatedBundle->package.sczId);
            if (SUCCEEDED(hr))
            {
                LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_SCHEDULED, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType));
                continue;
            }
            else if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to lookup the bundle ID in the ancestors dictionary.");
            }
        }
        else if (BOOTSTRAPPER_RELATION_DEPENDENT == pRelatedBundle->relationType && BOOTSTRAPPER_RELATION_NONE != relationType)
        {
            // Avoid repair loops for older bundles that do not handle ancestors.
            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_DEPENDENT, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), LoggingRelationTypeToString(relationType));
            continue;
        }

        // Pass along any ancestors and ourself to prevent infinite loops.
        if (pRegistration->sczAncestors && *pRegistration->sczAncestors)
        {
            hr = StrAllocFormatted(&pRelatedBundle->package.Exe.sczAncestors, L"%ls;%ls", pRegistration->sczAncestors, pRegistration->sczId);
            ExitOnFailure(hr, "Failed to copy ancestors and self to related bundle ancestors.");
        }
        else
        {
            hr = StrAllocString(&pRelatedBundle->package.Exe.sczAncestors, pRegistration->sczId, 0);
            ExitOnFailure(hr, "Failed to copy self to related bundle ancestors.");
        }

        switch (pRelatedBundle->relationType)
        {
        case BOOTSTRAPPER_RELATION_UPGRADE:
            if (BOOTSTRAPPER_RELATION_UPGRADE != relationType && BOOTSTRAPPER_ACTION_UNINSTALL < pPlan->action)
            {
                pRelatedBundle->package.requested = (pRegistration->qwVersion > pRelatedBundle->qwVersion) ? BOOTSTRAPPER_REQUEST_STATE_ABSENT : BOOTSTRAPPER_REQUEST_STATE_NONE;
            }
            break;
        case BOOTSTRAPPER_RELATION_PATCH: __fallthrough;
        case BOOTSTRAPPER_RELATION_ADDON:
            if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
            {
                pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
            }
            else if (BOOTSTRAPPER_ACTION_INSTALL == pPlan->action || BOOTSTRAPPER_ACTION_MODIFY == pPlan->action)
            {
                pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            }
            else if (BOOTSTRAPPER_ACTION_REPAIR == pPlan->action)
            {
                pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
            }
            break;
        case BOOTSTRAPPER_RELATION_DEPENDENT:
            // Automatically repair dependent bundles to restore missing
            // packages after uninstall unless we're being upgraded with the
            // assumption that upgrades are cumulative (as intended).
            if (BOOTSTRAPPER_RELATION_UPGRADE != relationType && BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
            {
                pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
            }
            break;
        case BOOTSTRAPPER_RELATION_DETECT:
            break;
        default:
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected relation type encountered during plan: %d", pRelatedBundle->relationType);
            break;
        }

        pRelatedBundle->package.defaultRequested = pRelatedBundle->package.requested;

        int nResult = pUserExperience->pUserExperience->OnPlanRelatedBundle(pRelatedBundle->package.sczId, &pRelatedBundle->package.requested);
        hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
        ExitOnRootFailure(hr, "UX aborted plan related bundle.");

        // Log when the UX changed the bundle state so the engine doesn't get blamed for planning the wrong thing.
        if (pRelatedBundle->package.requested != pRelatedBundle->package.defaultRequested)
        {
            LogId(REPORT_STANDARD, MSG_PLANNED_BUNDLE_UX_CHANGED_REQUEST, pRelatedBundle->package.sczId, LoggingRequestStateToString(pRelatedBundle->package.requested), LoggingRequestStateToString(pRelatedBundle->package.defaultRequested));
        }

        // If uninstalling and the dependent related bundle may be executed, ignore its provider key to allow for downgrades with ref-counting.
        if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action && BOOTSTRAPPER_RELATION_DEPENDENT == pRelatedBundle->relationType && BOOTSTRAPPER_REQUEST_STATE_NONE != pRelatedBundle->package.requested)
        {
            if (0 < pRelatedBundle->package.cDependencyProviders)
            {
                // Bundles only support a single provider key.
                const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders;

                hr = DepDependencyArrayAlloc(&pPlan->rgPlannedProviders, &pPlan->cPlannedProviders, pProvider->sczKey, pProvider->sczDisplayName);
                ExitOnFailure(hr, "Failed to add the package provider key \"%ls\" to the planned list.", pProvider->sczKey);
            }
        }
    }

LExit:
    ReleaseDict(sdAncestors);
    ReleaseStrArray(rgsczAncestors, cAncestors);

    return hr;
}

extern "C" HRESULT PlanRelatedBundlesComplete(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout HANDLE* phSyncpointEvent,
    __in DWORD dwExecuteActionEarlyIndex
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczIgnoreDependencies = NULL;
    STRINGDICT_HANDLE sdProviderKeys = NULL;

    // Get the list of dependencies to ignore to pass to related bundles.
    hr = DependencyAllocIgnoreDependencies(pPlan, &sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to get the list of dependencies to ignore.");

    hr = DictCreateStringList(&sdProviderKeys, pPlan->cExecuteActions, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary for planned packages.");

    BOOL fExecutingAnyPackage = FALSE;

    for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
    {
        if (BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE == pPlan->rgExecuteActions[i].type && BOOTSTRAPPER_ACTION_STATE_NONE != pPlan->rgExecuteActions[i].exePackage.action)
        {
            fExecutingAnyPackage = TRUE;

            BURN_PACKAGE* pPackage = pPlan->rgExecuteActions[i].packageProvider.pPackage;
            if (BURN_PACKAGE_TYPE_EXE == pPackage->type && BURN_EXE_PROTOCOL_TYPE_BURN == pPackage->Exe.protocol)
            {
                if (0 < pPackage->cDependencyProviders)
                {
                    // Bundles only support a single provider key.
                    const BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders;
                    DictAddKey(sdProviderKeys, pProvider->sczKey);
                }
            }
        }
        else
        {
            switch (pPlan->rgExecuteActions[i].type)
            {
            case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
                fExecutingAnyPackage |= (BOOTSTRAPPER_ACTION_STATE_NONE != pPlan->rgExecuteActions[i].msiPackage.action);
                break;

            case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
                fExecutingAnyPackage |= (BOOTSTRAPPER_ACTION_STATE_NONE != pPlan->rgExecuteActions[i].mspTarget.action);
                break;

            case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
                fExecutingAnyPackage |= (BOOTSTRAPPER_ACTION_STATE_NONE != pPlan->rgExecuteActions[i].msuPackage.action);
                break;
            }
        }
    }

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        DWORD *pdwInsertIndex = NULL;
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

        // Do not execute if a major upgrade to the related bundle is an embedded bundle (Provider keys are the same)
        if (0 < pRelatedBundle->package.cDependencyProviders)
        {
            // Bundles only support a single provider key.
            const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders;
            hr = DictKeyExists(sdProviderKeys, pProvider->sczKey);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to check the dictionary for a related bundle provider key: \"%ls\".", pProvider->sczKey);
                // Key found, so there is an embedded bundle with the same provider key that will be executed.  So this related bundle should not be added to the plan
                LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_EMBEDDED_BUNDLE_NEWER, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), pProvider->sczKey);
                continue;
            }
            else
            {
                hr = S_OK;
            }
        }

        // For an uninstall, there is no need to repair dependent bundles if no packages are executing.
        if (!fExecutingAnyPackage && BOOTSTRAPPER_RELATION_DEPENDENT == pRelatedBundle->relationType && BOOTSTRAPPER_REQUEST_STATE_REPAIR == pRelatedBundle->package.requested && BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
        {
            pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_DEPENDENT_BUNDLE_REPAIR, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType));
        }

        if (BOOTSTRAPPER_RELATION_ADDON == pRelatedBundle->relationType || BOOTSTRAPPER_RELATION_PATCH == pRelatedBundle->relationType)
        {
            // Addon and patch bundles will be passed a list of dependencies to ignore for planning.
            hr = StrAllocString(&pRelatedBundle->package.Exe.sczIgnoreDependencies, sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to copy the list of dependencies to ignore.");

            // Uninstall addons and patches early in the chain, before other packages are uninstalled.
            if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
            {
                pdwInsertIndex = &dwExecuteActionEarlyIndex;
            }
        }

        if (BOOTSTRAPPER_REQUEST_STATE_NONE != pRelatedBundle->package.requested)
        {
            hr = ExeEnginePlanCalculatePackage(&pRelatedBundle->package, NULL);
            ExitOnFailure(hr, "Failed to calcuate plan for related bundle: %ls", pRelatedBundle->package.sczId);

            // Calculate package states based on reference count for addon and patch related bundles.
            if (BOOTSTRAPPER_RELATION_ADDON == pRelatedBundle->relationType || BOOTSTRAPPER_RELATION_PATCH == pRelatedBundle->relationType)
            {
                hr = DependencyPlanPackageBegin(pRegistration->fPerMachine, &pRelatedBundle->package, pPlan);
                ExitOnFailure(hr, "Failed to begin plan dependency actions to  package: %ls", pRelatedBundle->package.sczId);

                // If uninstalling a related bundle, make sure the bundle is uninstalled after removing registration.
                if (pdwInsertIndex && BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action)
                {
                    ++(*pdwInsertIndex);
                }
            }

            hr = ExeEnginePlanAddPackage(pdwInsertIndex, &pRelatedBundle->package, pPlan, pLog, pVariables, *phSyncpointEvent, FALSE);
            ExitOnFailure(hr, "Failed to add to plan related bundle: %ls", pRelatedBundle->package.sczId);

            // Calculate package states based on reference count for addon and patch related bundles.
            if (BOOTSTRAPPER_RELATION_ADDON == pRelatedBundle->relationType || BOOTSTRAPPER_RELATION_PATCH == pRelatedBundle->relationType)
            {
                hr = DependencyPlanPackageComplete(&pRelatedBundle->package, pPlan);
                ExitOnFailure(hr, "Failed to complete plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);
            }

            // If we are going to take any action on this package, add progress for it.
            if (BOOTSTRAPPER_ACTION_STATE_NONE != pRelatedBundle->package.execute || BOOTSTRAPPER_ACTION_STATE_NONE != pRelatedBundle->package.rollback)
            {
                LoggingIncrementPackageSequence();

                ++pPlan->cExecutePackagesTotal;
                ++pPlan->cOverallProgressTicksTotal;
            }

            // If package is per-machine and is being executed, flag the plan to be per-machine as well.
            if (pRelatedBundle->package.fPerMachine)
            {
                pPlan->fPerMachine = TRUE;
            }
        }
        else if (BOOTSTRAPPER_RELATION_ADDON == pRelatedBundle->relationType || BOOTSTRAPPER_RELATION_PATCH == pRelatedBundle->relationType)
        {
            // Make sure the package is properly ref-counted even if no plan is requested.
            hr = DependencyPlanPackageBegin(pRegistration->fPerMachine, &pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to begin plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);

            hr = DependencyPlanPackage(pdwInsertIndex, &pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to plan related bundle package provider actions.");

            hr = DependencyPlanPackageComplete(&pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to complete plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);
        }
    }

LExit:
    ReleaseDict(sdProviderKeys);
    ReleaseStr(sczIgnoreDependencies);

    return hr;
}

extern "C" HRESULT PlanFinalizeActions(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;

    hr = RemoveUnnecessaryActions(TRUE, pPlan->rgExecuteActions, pPlan->cExecuteActions);
    ExitOnFailure(hr, "Failed to remove unnecessary execute actions.");

    hr = RemoveUnnecessaryActions(FALSE, pPlan->rgRollbackActions, pPlan->cRollbackActions);
    ExitOnFailure(hr, "Failed to remove unnecessary execute actions.");

    hr = FinalizeSlipstreamPatchActions(TRUE, pPlan->rgExecuteActions, pPlan->cExecuteActions);
    ExitOnFailure(hr, "Failed to finalize slipstream execute actions.");

    hr = FinalizeSlipstreamPatchActions(FALSE, pPlan->rgRollbackActions, pPlan->cRollbackActions);
    ExitOnFailure(hr, "Failed to finalize slipstream rollback actions.");

LExit:
    return hr;
}

extern "C" HRESULT PlanCleanPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOL fPlanCleanPackage = FALSE;
    BURN_CLEAN_ACTION* pCleanAction = NULL;

    // The following is a complex set of logic that determines when a package should be cleaned
    // from the cache. Start by noting that we only clean if the package is being acquired or
    // already cached and the package is not supposed to always be cached.
    if ((pPackage->fAcquire || BURN_CACHE_STATE_PARTIAL == pPackage->cache || BURN_CACHE_STATE_COMPLETE == pPackage->cache) && 
        (BURN_CACHE_TYPE_ALWAYS > pPackage->cacheType || BOOTSTRAPPER_ACTION_INSTALL > pPlan->action))
    {
        // The following are all different reasons why the package should be cleaned from the cache.
        // The else-ifs are used to make the conditions easier to see (rather than have them combined
        // in one huge condition).
        if (BURN_CACHE_TYPE_YES > pPackage->cacheType)  // easy, package is not supposed to stay cached.
        {
            fPlanCleanPackage = TRUE;
        }
        else if ((BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT == pPackage->requested ||
                  BOOTSTRAPPER_REQUEST_STATE_ABSENT == pPackage->requested) &&      // requested to be removed and
                 BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute)          // actually being removed.
        {
            fPlanCleanPackage = TRUE;
        }
        else if ((BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT == pPackage->requested ||
                  BOOTSTRAPPER_REQUEST_STATE_ABSENT == pPackage->requested) &&      // requested to be removed but
                 BOOTSTRAPPER_ACTION_STATE_NONE == pPackage->execute &&             // execute is do nothing and
                 !pPackage->fDependencyManagerWasHere &&                            // dependency manager didn't change execute and
                 BOOTSTRAPPER_PACKAGE_STATE_PRESENT > pPackage->currentState)       // currently not installed.
        {
            fPlanCleanPackage = TRUE;
        }
        else if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action &&                  // uninstalling and
                 BOOTSTRAPPER_REQUEST_STATE_NONE == pPackage->requested &&          // requested do nothing (aka: default) and
                 BOOTSTRAPPER_ACTION_STATE_NONE == pPackage->execute &&             // execute is still do nothing and
                 !pPackage->fDependencyManagerWasHere &&                            // dependency manager didn't change execute and
                 BOOTSTRAPPER_PACKAGE_STATE_PRESENT > pPackage->currentState)       // currently not installed.
        {
            fPlanCleanPackage = TRUE;
        }
    }

    if (fPlanCleanPackage)
    {
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPlan->rgCleanActions), pPlan->cCleanActions + 1, sizeof(BURN_CLEAN_ACTION), 5);
        ExitOnFailure(hr, "Failed to grow plan's array of clean actions.");

        pCleanAction = pPlan->rgCleanActions + pPlan->cCleanActions;
        ++pPlan->cCleanActions;

        pCleanAction->pPackage = pPackage;

        pPackage->fUncache = TRUE;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanExecuteCacheSyncAndRollback(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    hr = PlanAppendExecuteAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append wait action for caching.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT;
    pAction->syncpoint.hEvent = hCacheEvent;

    if (fPlanPackageCacheRollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE;
        pAction->uncachePackage.pPackage = pPackage;

        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append execute checkpoint for cache rollback.");
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanExecuteCheckpoint(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;
    DWORD dwCheckpointId = GetNextCheckpointId();

    // execute checkpoint
    hr = PlanAppendExecuteAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append execute action.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pAction->checkpoint.dwId = dwCheckpointId;

    // rollback checkpoint
    hr = PlanAppendRollbackAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append rollback action.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pAction->checkpoint.dwId = dwCheckpointId;

LExit:
    return hr;
}

extern "C" HRESULT PlanInsertExecuteAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    )
{
    HRESULT hr = S_OK;

    hr = MemInsertIntoArray((void**)&pPlan->rgExecuteActions, dwIndex, 1, pPlan->cExecuteActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of execute actions.");

    *ppExecuteAction = pPlan->rgExecuteActions + dwIndex;
    ++pPlan->cExecuteActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanInsertRollbackAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    )
{
    HRESULT hr = S_OK;

    hr = MemInsertIntoArray((void**)&pPlan->rgRollbackActions, dwIndex, 1, pPlan->cRollbackActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback actions.");

    *ppRollbackAction = pPlan->rgRollbackActions + dwIndex;
    ++pPlan->cRollbackActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanAppendExecuteAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize((void**)&pPlan->rgExecuteActions, pPlan->cExecuteActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of execute actions.");

    *ppExecuteAction = pPlan->rgExecuteActions + pPlan->cExecuteActions;
    ++pPlan->cExecuteActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanAppendRollbackAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize((void**)&pPlan->rgRollbackActions, pPlan->cRollbackActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback actions.");

    *ppRollbackAction = pPlan->rgRollbackActions + pPlan->cRollbackActions;
    ++pPlan->cRollbackActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanKeepRegistration(
    __in BURN_PLAN* pPlan,
    __in DWORD iAfterExecutePackageAction,
    __in DWORD iBeforeRollbackPackageAction
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    if (BURN_PLAN_INVALID_ACTION_INDEX != iAfterExecutePackageAction)
    {
        hr = PlanInsertExecuteAction(iAfterExecutePackageAction, pPlan, &pAction);
        ExitOnFailure(hr, "Failed to insert keep registration execute action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_REGISTRATION;
        pAction->registration.fKeep = TRUE;
    }

    if (BURN_PLAN_INVALID_ACTION_INDEX != iBeforeRollbackPackageAction)
    {
        hr = PlanInsertRollbackAction(iBeforeRollbackPackageAction, pPlan, &pAction);
        ExitOnFailure(hr, "Failed to insert keep registration rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_REGISTRATION;
        pAction->registration.fKeep = FALSE;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRemoveRegistration(
    __in BURN_PLAN* pPlan,
    __in DWORD iAfterExecutePackageAction,
    __in DWORD iAfterRollbackPackageAction
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    if (BURN_PLAN_INVALID_ACTION_INDEX != iAfterExecutePackageAction)
    {
        hr = PlanInsertExecuteAction(iAfterExecutePackageAction, pPlan, &pAction);
        ExitOnFailure(hr, "Failed to insert remove registration execute action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_REGISTRATION;
        pAction->registration.fKeep = FALSE;
    }

    if (BURN_PLAN_INVALID_ACTION_INDEX != iAfterRollbackPackageAction)
    {
        hr = PlanInsertRollbackAction(iAfterRollbackPackageAction, pPlan, &pAction);
        ExitOnFailure(hr, "Failed to insert remove registration rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_REGISTRATION;
        pAction->registration.fKeep = TRUE;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRollbackBoundaryBegin(
    __in BURN_PLAN* pPlan,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pExecuteAction = NULL;

    // Add begin rollback boundary to execute plan.
    hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary begin action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY;
    pExecuteAction->rollbackBoundary.pRollbackBoundary = pRollbackBoundary;

    // Add begin rollback boundary to rollback plan.
    hr = PlanAppendRollbackAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary begin action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY;
    pExecuteAction->rollbackBoundary.pRollbackBoundary = pRollbackBoundary;

LExit:
    return hr;
}

extern "C" HRESULT PlanRollbackBoundaryComplete(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pExecuteAction = NULL;
    DWORD dwCheckpointId = 0;

    // Add checkpoints.
    dwCheckpointId = GetNextCheckpointId();

    hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append execute action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pExecuteAction->checkpoint.dwId = dwCheckpointId;

    hr = PlanAppendRollbackAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pExecuteAction->checkpoint.dwId = dwCheckpointId;

LExit:
    return hr;
}

/*******************************************************************
 PlanSetResumeCommand - Initializes resume command string

*******************************************************************/
extern "C" HRESULT PlanSetResumeCommand(
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_LOGGING* pLog
    )
{
    HRESULT hr = S_OK;

    // build the resume command-line.
    hr = CoreRecreateCommandLine(&pRegistration->sczResumeCommandLine, action, pCommand->display, pCommand->restart, pCommand->relationType, pCommand->fPassthrough, pRegistration->sczActiveParent, pRegistration->sczAncestors, pLog->sczPath, pCommand->wzCommandLine);
    ExitOnFailure(hr, "Failed to recreate resume command-line.");

LExit:
    return hr;
}


// internal function definitions

static void UninitializeRegistrationAction(
    __in BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    )
{
    ReleaseStr(pAction->sczDependentProviderKey);
    ReleaseStr(pAction->sczBundleId);
    memset(pAction, 0, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION));
}

static void UninitializeCacheAction(
    __in BURN_CACHE_ACTION* pCacheAction
    )
{
    switch (pCacheAction->type)
    {
    case BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT:
        ReleaseHandle(pCacheAction->syncpoint.hEvent);
        break;

    case BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE:
        ReleaseStr(pCacheAction->bundleLayout.sczExecutableName);
        ReleaseStr(pCacheAction->bundleLayout.sczLayoutDirectory);
        ReleaseStr(pCacheAction->bundleLayout.sczUnverifiedPath);
        break;

    case BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER:
        ReleaseStr(pCacheAction->resolveContainer.sczUnverifiedPath);
        break;

    case BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER:
        ReleaseStr(pCacheAction->extractContainer.sczContainerUnverifiedPath);
        ReleaseMem(pCacheAction->extractContainer.rgPayloads);
        break;

    case BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD:
        ReleaseStr(pCacheAction->resolvePayload.sczUnverifiedPath);
        break;

    case BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD:
        ReleaseStr(pCacheAction->cachePayload.sczUnverifiedPath);
        break;
    }
}

static void ResetPlannedPackageState(
    __in BURN_PACKAGE* pPackage
    )
{
    // Reset package state that is a result of planning.
    pPackage->expected = BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN;
    pPackage->defaultRequested = BOOTSTRAPPER_REQUEST_STATE_NONE;
    pPackage->requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
    pPackage->fAcquire = FALSE;
    pPackage->fUncache = FALSE;
    pPackage->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
    pPackage->providerExecute = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->providerRollback = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->dependencyExecute = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->dependencyRollback = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->fDependencyManagerWasHere = FALSE;

    if (BURN_PACKAGE_TYPE_MSI == pPackage->type && pPackage->Msi.rgFeatures)
    {
        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            pFeature->execute = BOOTSTRAPPER_FEATURE_ACTION_NONE;
            pFeature->rollback = BOOTSTRAPPER_FEATURE_ACTION_NONE;
        }
    }
    else if (BURN_PACKAGE_TYPE_MSP == pPackage->type && pPackage->Msp.rgTargetProducts)
    {
        for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
        {
            BURN_MSPTARGETPRODUCT* pTargetProduct = &pPackage->Msp.rgTargetProducts[i];

            pTargetProduct->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            pTargetProduct->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
        }
    }
}

static HRESULT GetActionDefaultRequestState(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOL fPermanent,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    )
{
    HRESULT hr = S_OK;

    switch (action)
    {
    case BOOTSTRAPPER_ACTION_CACHE:
        switch (currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            break;

        default:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_CACHE;
            break;
        }
        break;

    case BOOTSTRAPPER_ACTION_INSTALL: __fallthrough;
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE: __fallthrough;
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED:
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
        break;

    case BOOTSTRAPPER_ACTION_REPAIR:
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
        break;

    case BOOTSTRAPPER_ACTION_UNINSTALL:
        *pRequestState = fPermanent ? BOOTSTRAPPER_REQUEST_STATE_NONE : BOOTSTRAPPER_REQUEST_STATE_ABSENT;
        break;

    case BOOTSTRAPPER_ACTION_MODIFY:
        switch (currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_CACHE;
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            break;

        default:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid action state.");
    }

LExit:
        return hr;
}

static HRESULT AddRegistrationAction(
    __in BURN_PLAN* pPlan,
    __in BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type,
    __in_z LPCWSTR wzDependentProviderKey,
    __in_z LPCWSTR wzOwnerBundleId
    )
{
    HRESULT hr = S_OK;
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE rollbackType = (BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER == type) ? BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER : BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER;
    BURN_DEPENDENT_REGISTRATION_ACTION* pAction = NULL;

    // Create forward registration action.
    hr = MemEnsureArraySize((void**)&pPlan->rgRegistrationActions, pPlan->cRegistrationActions + 1, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of registration actions.");

    pAction = pPlan->rgRegistrationActions + pPlan->cRegistrationActions;
    ++pPlan->cRegistrationActions;

    pAction->type = type;

    hr = StrAllocString(&pAction->sczBundleId, wzOwnerBundleId, 0);
    ExitOnFailure(hr, "Failed to copy owner bundle to registration action.");

    hr = StrAllocString(&pAction->sczDependentProviderKey, wzDependentProviderKey, 0);
    ExitOnFailure(hr, "Failed to copy dependent provider key to registration action.");

    // Create rollback registration action.
    hr = MemEnsureArraySize((void**)&pPlan->rgRollbackRegistrationActions, pPlan->cRollbackRegistrationActions + 1, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback registration actions.");

    pAction = pPlan->rgRollbackRegistrationActions + pPlan->cRollbackRegistrationActions;
    ++pPlan->cRollbackRegistrationActions;

    pAction->type = rollbackType;

    hr = StrAllocString(&pAction->sczBundleId, wzOwnerBundleId, 0);
    ExitOnFailure(hr, "Failed to copy owner bundle to registration action.");

    hr = StrAllocString(&pAction->sczDependentProviderKey, wzDependentProviderKey, 0);
    ExitOnFailure(hr, "Failed to copy dependent provider key to rollback registration action.");

LExit:
    return hr;
}

static HRESULT AddCachePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __out HANDLE* phSyncpointEvent
    )
{
    HRESULT hr = S_OK;

    // If this is an MSI package with slipstream MSPs, ensure the MSPs are cached first.
    if (BURN_PACKAGE_TYPE_MSI == pPackage->type && 0 < pPackage->Msi.cSlipstreamMspPackages)
    {
        hr = AddCacheSlipstreamMsps(pPlan, pPackage);
        ExitOnFailure(hr, "Failed to plan slipstream patches for package.");
    }

    hr = AddCachePackageHelper(pPlan, pPackage, phSyncpointEvent);
    ExitOnFailure(hr, "Failed to plan cache package.");

LExit:
    return hr;
}

static HRESULT AddCachePackageHelper(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __out HANDLE* phSyncpointEvent
    )
{
    AssertSz(pPackage->sczCacheId && *pPackage->sczCacheId, "AddCachePackageHelper() expects the package to have a cache id.");

    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    DWORD dwCheckpoint = 0;
    DWORD iPackageStartAction = 0;

    BOOL fPlanned = AlreadyPlannedCachePackage(pPlan, pPackage->sczId, phSyncpointEvent);
    if (fPlanned)
    {
        ExitFunction();
    }

    // Cache checkpoints happen before the package is cached because downloading packages'
    // payloads will not roll themselves back the way installation packages rollback on
    // failure automatically.
    dwCheckpoint = GetNextCheckpointId();

    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append package start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_CHECKPOINT;
    pCacheAction->checkpoint.dwId = dwCheckpoint;

    // Only plan the cache rollback if the package is also going to be uninstalled;
    // otherwise, future operations like repair will not be able to locate the cached package.
    BOOL fPlanCacheRollback = (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->rollback);

    if (fPlanCacheRollback)
    {
        hr = AppendRollbackCacheAction(pPlan, &pCacheAction);
        ExitOnFailure(hr, "Failed to append rollback cache action.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_CHECKPOINT;
        pCacheAction->checkpoint.dwId = dwCheckpoint;
    }

    // Plan the package start.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append package start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_PACKAGE_START;
    pCacheAction->packageStart.pPackage = pPackage;

    // Remember the index for the package start action (which is now the last in the cache
    // actions array) because we have to update this action after processing all the payloads
    // and the array may be resized later which would move a pointer around in memory.
    iPackageStartAction = pPlan->cCacheActions - 1;

    if (fPlanCacheRollback)
    {
        // Create a package cache rollback action.
        hr = AppendRollbackCacheAction(pPlan, &pCacheAction);
        ExitOnFailure(hr, "Failed to append rollback cache action.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE;
        pCacheAction->rollbackPackage.pPackage = pPackage;
    }

    // Add all the payload cache operations to the plan for this package.
    for (DWORD i = 0; i < pPackage->cPayloads; ++i)
    {
        BURN_PACKAGE_PAYLOAD* pPackagePayload = &pPackage->rgPayloads[i];

        hr = AppendCacheOrLayoutPayloadAction(pPlan, pPackage, iPackageStartAction, pPackagePayload->pPayload, pPackagePayload->fCached, NULL);
        ExitOnFailure(hr, "Failed to append payload cache action.");

        Assert(BURN_CACHE_ACTION_TYPE_PACKAGE_START == pPlan->rgCacheActions[iPackageStartAction].type);
        ++pPlan->rgCacheActions[iPackageStartAction].packageStart.cCachePayloads;
        pPlan->rgCacheActions[iPackageStartAction].packageStart.qwCachePayloadSizeTotal += pPackagePayload->pPayload->qwFileSize;
    }

    // Create package stop action.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append cache action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_PACKAGE_STOP;
    pCacheAction->packageStop.pPackage = pPackage;

    // Update the start action with the location of the complete action.
    pPlan->rgCacheActions[iPackageStartAction].packageStart.iPackageCompleteAction = pPlan->cCacheActions - 1;

    // Create syncpoint action.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append cache action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT;
    pCacheAction->syncpoint.hEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pCacheAction->syncpoint.hEvent, hr, "Failed to create syncpoint event.");

    *phSyncpointEvent = pCacheAction->syncpoint.hEvent;

    ++pPlan->cOverallProgressTicksTotal;

    // If the package was not already fully cached then note that we planned the cache here. Otherwise, we only
    // did cache operations to verify the cache is valid so we did not plan the acquisition of the package.
    pPackage->fAcquire = (BURN_CACHE_STATE_COMPLETE != pPackage->cache);

LExit:
    return hr;
}

static HRESULT AddCacheSlipstreamMsps(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    HANDLE hIgnored = NULL;

    AssertSz(BURN_PACKAGE_TYPE_MSI == pPackage->type, "Only MSI packages can have slipstream patches.");

    for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
    {
        BURN_PACKAGE* pMspPackage = pPackage->Msi.rgpSlipstreamMspPackages[i];
        AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Only MSP packages can be slipstream patches.");

        hr = AddCachePackageHelper(pPlan, pMspPackage, &hIgnored);
        ExitOnFailure(hr, "Failed to plan slipstream MSP: %ls", pMspPackage->sczId);
    }

LExit:
    return hr;
}

static BOOL AlreadyPlannedCachePackage(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzPackageId,
    __out HANDLE* phSyncpointEvent
    )
{
    BOOL fPlanned = FALSE;

    for (DWORD iCacheAction = 0; iCacheAction < pPlan->cCacheActions; ++iCacheAction)
    {
        BURN_CACHE_ACTION* pCacheAction = pPlan->rgCacheActions + iCacheAction;

        if (BURN_CACHE_ACTION_TYPE_PACKAGE_STOP == pCacheAction->type)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pCacheAction->packageStop.pPackage->sczId, -1, wzPackageId, -1))
            {
                if (iCacheAction + 1 < pPlan->cCacheActions && BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT == pPlan->rgCacheActions[iCacheAction + 1].type)
                {
                    *phSyncpointEvent = pPlan->rgCacheActions[iCacheAction + 1].syncpoint.hEvent;
                }

                fPlanned = TRUE;
                break;
            }
        }
    }

    return fPlanned;
}

static DWORD GetNextCheckpointId()
{
    static DWORD dwCounter = 0;
    return ++dwCounter;
}

static HRESULT AppendCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPlan->rgCacheActions), pPlan->cCacheActions + 1, sizeof(BURN_CACHE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of cache actions.");

    *ppCacheAction = pPlan->rgCacheActions + pPlan->cCacheActions;
    ++pPlan->cCacheActions;

LExit:
    return hr;
}

static HRESULT AppendRollbackCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPlan->rgRollbackCacheActions), pPlan->cRollbackCacheActions + 1, sizeof(BURN_CACHE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback cache actions.");

    *ppCacheAction = pPlan->rgRollbackCacheActions + pPlan->cRollbackCacheActions;
    ++pPlan->cRollbackCacheActions;

LExit:
    return hr;
}

static HRESULT AppendLayoutContainerAction(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_PACKAGE* pPackage,
    __in DWORD iPackageStartAction,
    __in BURN_CONTAINER* pContainer,
    __in BOOL fContainerCached,
    __in_z LPCWSTR wzLayoutDirectory
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pAcquireAction = NULL;
    DWORD iAcquireAction = BURN_PLAN_INVALID_ACTION_INDEX;
    LPWSTR sczContainerWorkingPath = NULL;
    BURN_CACHE_ACTION* pCacheAction = NULL;

    // No need to do anything if the container is already cached or is attached to the bundle (since the
    // bundle itself will already have a layout action).
    if (fContainerCached || pContainer->fAttached)
    {
        ExitFunction();
    }

    // Ensure the container is being acquired.  If it is, then some earlier package already planned the layout of this container so 
    // don't do it again. Otherwise, plan away!
    if (!FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER, pPlan, pContainer, 0, iPackageStartAction, NULL, NULL))
    {
        hr = AddAcquireContainer(pPlan, pContainer, &pAcquireAction, &iAcquireAction);
        ExitOnFailure(hr, "Failed to append acquire container action for layout to plan.");

        Assert(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == pAcquireAction->type);

        // Create the layout container action.
        hr = StrAllocString(&sczContainerWorkingPath, pAcquireAction->resolveContainer.sczUnverifiedPath, 0);
        ExitOnFailure(hr, "Failed to copy container working path for layout.");

        hr = AppendCacheAction(pPlan, &pCacheAction);
        ExitOnFailure(hr, "Failed to append cache action to cache payload.");

        hr = StrAllocString(&pCacheAction->layoutContainer.sczLayoutDirectory, wzLayoutDirectory, 0);
        ExitOnFailure(hr, "Failed to copy layout directory into plan.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_LAYOUT_CONTAINER;
        pCacheAction->layoutContainer.pPackage = pPackage;
        pCacheAction->layoutContainer.pContainer = pContainer;
        pCacheAction->layoutContainer.fMove = TRUE;
        pCacheAction->layoutContainer.iTryAgainAction = iAcquireAction;
        pCacheAction->layoutContainer.sczUnverifiedPath = sczContainerWorkingPath;
        sczContainerWorkingPath = NULL;

        pPlan->qwCacheSizeTotal += pContainer->qwFileSize;
    }

LExit:
    ReleaseNullStr(sczContainerWorkingPath);

    return hr;
}

static HRESULT AppendCacheOrLayoutPayloadAction(
    __in BURN_PLAN* pPlan,
    __in_opt BURN_PACKAGE* pPackage,
    __in DWORD iPackageStartAction,
    __in BURN_PAYLOAD* pPayload,
    __in BOOL fPayloadCached,
    __in_z_opt LPCWSTR wzLayoutDirectory
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPayloadWorkingPath = NULL;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    DWORD iTryAgainAction = BURN_PLAN_INVALID_ACTION_INDEX;

    hr = CacheCalculatePayloadWorkingPath(pPlan->wzBundleId, pPayload, &sczPayloadWorkingPath);
    ExitOnFailure(hr, "Failed to calculate unverified path for payload.");

    // If the payload is in a container, ensure the container is being acquired
    // then add this payload to the list of payloads to extract already in the plan.
    if (pPayload->pContainer)
    {
        BURN_CACHE_ACTION* pPreviousPackageExtractAction = NULL;
        BURN_CACHE_ACTION* pThisPackageExtractAction = NULL;

        // If the payload is not already cached, then add it to the first extract container action in the plan. Extracting
        // all the needed payloads from the container in a single pass is the most efficient way to extract files from
        // containers. If there is not an extract container action before our package, that is okay because we'll create
        // an extract container action for our package in a second anyway.
        if (!fPayloadCached)
        {
            if (FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER, pPlan, pPayload->pContainer, 0, iPackageStartAction, &pPreviousPackageExtractAction, NULL))
            {
                hr = AddExtractPayload(pPreviousPackageExtractAction, pPackage, pPayload, sczPayloadWorkingPath);
                ExitOnFailure(hr, "Failed to add extract payload action to previous package.");
            }
        }

        // If there is already an extract container action after our package start action then try to find an acquire action
        // that is matched with it. If there is an acquire action then that is our "try again" action, otherwise we'll use the existing
        // extract action as the "try again" action.
        if (FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER, pPlan, pPayload->pContainer, iPackageStartAction, BURN_PLAN_INVALID_ACTION_INDEX, &pThisPackageExtractAction, &iTryAgainAction))
        {
            DWORD iAcquireAction = BURN_PLAN_INVALID_ACTION_INDEX;
            if (FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER, pPlan, pPayload->pContainer, iPackageStartAction, iTryAgainAction, NULL, &iAcquireAction))
            {
                iTryAgainAction = iAcquireAction;
            }
        }
        else // did not find an extract container action for our package.
        {
            // Ensure there is an extract action (and maybe an acquire action) for every package that has payloads. The
            // acquire and extract action will be skipped if the payload is already cached or was added to a previous
            // package's extract action above.
            //
            // These actions always exist (even when they are likely to be skipped) so that "try again" will not
            // jump so far back in the plan that you end up extracting payloads for other packages. With these actions
            // "try again" will only retry the extraction for payloads in this package.
            hr = CreateContainerAcquireAndExtractAction(pPlan, pPayload->pContainer, iPackageStartAction, pPreviousPackageExtractAction ? TRUE : fPayloadCached, &pThisPackageExtractAction, &iTryAgainAction);
            ExitOnFailure(hr, "Failed to create container extract action.");
        }
        ExitOnFailure(hr, "Failed while searching for package's container extract action.");

        // We *always* add the payload to this package's extract action even though the extract action
        // is probably being skipped until retry if there was a previous package extract action.
        hr = AddExtractPayload(pThisPackageExtractAction, pPackage, pPayload, sczPayloadWorkingPath);
        ExitOnFailure(hr, "Failed to add extract payload to current package.");
    }
    else // add a payload acquire action to the plan.
    {
        // Try to find an existing acquire action for this payload. If one is not found,
        // we'll create it. At the same time we will change any cache/layout payload actions
        // that would "MOVE" the file to "COPY" so that our new cache/layout action below
        // can do the move.
        pCacheAction = ProcessSharedPayload(pPlan, pPayload);
        if (!pCacheAction)
        {
            hr = AppendCacheAction(pPlan, &pCacheAction);
            ExitOnFailure(hr, "Failed to append cache action to acquire payload.");

            pCacheAction->type = BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD;
            pCacheAction->fSkipUntilRetried = fPayloadCached;
            pCacheAction->resolvePayload.pPackage = pPackage;
            pCacheAction->resolvePayload.pPayload = pPayload;
            hr = StrAllocString(&pCacheAction->resolvePayload.sczUnverifiedPath, sczPayloadWorkingPath, 0);
            ExitOnFailure(hr, "Failed to copy unverified path for payload to acquire.");
        }

        iTryAgainAction = static_cast<DWORD>(pCacheAction - pPlan->rgCacheActions);
        pCacheAction = NULL;
    }

    Assert(BURN_PLAN_INVALID_ACTION_INDEX != iTryAgainAction);
    Assert(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == pPlan->rgCacheActions[iTryAgainAction].type ||
           BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == pPlan->rgCacheActions[iTryAgainAction].type ||
           BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD == pPlan->rgCacheActions[iTryAgainAction].type);

    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append cache action to cache payload.");

    if (NULL == wzLayoutDirectory)
    {
        pCacheAction->type = BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD;
        pCacheAction->cachePayload.pPackage = pPackage;
        pCacheAction->cachePayload.pPayload = pPayload;
        pCacheAction->cachePayload.fMove = TRUE;
        pCacheAction->cachePayload.iTryAgainAction = iTryAgainAction;
        pCacheAction->cachePayload.sczUnverifiedPath = sczPayloadWorkingPath;
        sczPayloadWorkingPath = NULL;
    }
    else
    {
        hr = StrAllocString(&pCacheAction->layoutPayload.sczLayoutDirectory, wzLayoutDirectory, 0);
        ExitOnFailure(hr, "Failed to copy layout directory into plan.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_LAYOUT_PAYLOAD;
        pCacheAction->layoutPayload.pPackage = pPackage;
        pCacheAction->layoutPayload.pPayload = pPayload;
        pCacheAction->layoutPayload.fMove = TRUE;
        pCacheAction->layoutPayload.iTryAgainAction = iTryAgainAction;
        pCacheAction->layoutPayload.sczUnverifiedPath = sczPayloadWorkingPath;
        sczPayloadWorkingPath = NULL;
    }

    pCacheAction = NULL;

    pPlan->qwCacheSizeTotal += pPayload->qwFileSize;

LExit:
    ReleaseStr(sczPayloadWorkingPath);

    return hr;
}

static BOOL FindContainerCacheAction(
    __in BURN_CACHE_ACTION_TYPE type,
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __in DWORD iSearchStart,
    __in DWORD iSearchEnd,
    __out_opt BURN_CACHE_ACTION** ppCacheAction,
    __out_opt DWORD* piCacheAction
    )
{
    BOOL fFound = FALSE; // assume we won't find what we are looking for.

    Assert(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == type || BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == type);

    iSearchStart = (BURN_PLAN_INVALID_ACTION_INDEX == iSearchStart) ? 0 : iSearchStart;
    iSearchEnd = (BURN_PLAN_INVALID_ACTION_INDEX == iSearchEnd) ? pPlan->cCacheActions : iSearchEnd;

    for (DWORD iSearch = iSearchStart; iSearch < iSearchEnd; ++iSearch)
    {
        BURN_CACHE_ACTION* pCacheAction = pPlan->rgCacheActions + iSearch;
        if (pCacheAction->type == type &&
            ((BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == pCacheAction->type && pCacheAction->resolveContainer.pContainer == pContainer) ||
             (BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == pCacheAction->type && pCacheAction->extractContainer.pContainer == pContainer)))
        {
            if (ppCacheAction)
            {
                *ppCacheAction = pCacheAction;
            }

            if (piCacheAction)
            {
                *piCacheAction = iSearch;
            }

            fFound = TRUE;
            break;
        }
    }

    return fFound;
}

static HRESULT CreateContainerAcquireAndExtractAction(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __in DWORD iPackageStartAction,
    __in BOOL fPayloadCached,
    __out BURN_CACHE_ACTION** ppContainerExtractAction,
    __out DWORD* piContainerTryAgainAction
    )
{
    HRESULT hr = S_OK;
    DWORD iAcquireAction = BURN_PLAN_INVALID_ACTION_INDEX;
    BURN_CACHE_ACTION* pContainerExtractAction = NULL;
    DWORD iExtractAction = BURN_PLAN_INVALID_ACTION_INDEX;
    DWORD iTryAgainAction = BURN_PLAN_INVALID_ACTION_INDEX;
    LPWSTR sczContainerWorkingPath = NULL;

    // If the container is actually attached to the executable then we will not need an acquire
    // container action.
    if (!pContainer->fActuallyAttached)
    {
        BURN_CACHE_ACTION* pAcquireContainerAction = NULL;

        // If there is no plan to acquire the container then add acquire action since we
        // can't extract stuff out of a container until we acquire the container.
        if (!FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER, pPlan, pContainer, iPackageStartAction, BURN_PLAN_INVALID_ACTION_INDEX, &pAcquireContainerAction, &iAcquireAction))
        {
            hr = AddAcquireContainer(pPlan, pContainer, &pAcquireContainerAction, &iAcquireAction);
            ExitOnFailure(hr, "Failed to append acquire container action to plan.");

            pAcquireContainerAction->fSkipUntilRetried = TRUE; // we'll start by assuming the acquire is not necessary and the fPayloadCached below will set us straight if wrong.
        }

        Assert(BURN_PLAN_INVALID_ACTION_INDEX != iAcquireAction);
        Assert(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == pAcquireContainerAction->type);
        Assert(pContainer == pAcquireContainerAction->resolveContainer.pContainer);
    }

    Assert((pContainer->fActuallyAttached && BURN_PLAN_INVALID_ACTION_INDEX == iAcquireAction) ||
           (!pContainer->fActuallyAttached && BURN_PLAN_INVALID_ACTION_INDEX != iAcquireAction));

    // If we do not find an action for extracting payloads from this container, create it now.
    if (!FindContainerCacheAction(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER, pPlan, pContainer, (BURN_PLAN_INVALID_ACTION_INDEX == iAcquireAction) ? iPackageStartAction : iAcquireAction, BURN_PLAN_INVALID_ACTION_INDEX, &pContainerExtractAction, &iExtractAction))
    {
        // Attached containers that are actually attached use the executable path for their working path.
        if (pContainer->fActuallyAttached)
        {
            Assert(BURN_PLAN_INVALID_ACTION_INDEX == iAcquireAction);

            hr = PathForCurrentProcess(&sczContainerWorkingPath, NULL);
            ExitOnFailure(hr, "Failed to get path for executing module as attached container working path.");
        }
        else // use the acquired working path as the location of the container.
        {
            Assert(BURN_PLAN_INVALID_ACTION_INDEX != iAcquireAction);

            hr = StrAllocString(&sczContainerWorkingPath, pPlan->rgCacheActions[iAcquireAction].resolveContainer.sczUnverifiedPath, 0);
            ExitOnFailure(hr, "Failed to copy container unverified path for cache action to extract container.");
        }

        hr = AppendCacheAction(pPlan, &pContainerExtractAction);
        ExitOnFailure(hr, "Failed to append cache action to extract payloads from container.");

        iExtractAction = pPlan->cCacheActions - 1;

        pContainerExtractAction->type = BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER;
        pContainerExtractAction->fSkipUntilRetried = pContainer->fActuallyAttached; // assume we can skip the extract engine when the container is already attached and the fPayloadCached below will set us straight if wrong.
        pContainerExtractAction->extractContainer.pContainer = pContainer;
        pContainerExtractAction->extractContainer.iSkipUntilAcquiredByAction = iAcquireAction;
        pContainerExtractAction->extractContainer.sczContainerUnverifiedPath = sczContainerWorkingPath;
        sczContainerWorkingPath = NULL;
    }

    Assert(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == pContainerExtractAction->type);
    Assert(BURN_PLAN_INVALID_ACTION_INDEX != iExtractAction);

    // If there is an acquire action, that is our try again action otherwise, we'll use the extract action.
    iTryAgainAction = (BURN_PLAN_INVALID_ACTION_INDEX != iAcquireAction) ? iAcquireAction : iExtractAction;

    // If the try again action thinks it can be skipped but the payload is not cached ensure
    // the action will not be skipped. That also means we need to add this container to the
    // cache size if it is not already attached.
    BURN_CACHE_ACTION* pTryAgainAction = pPlan->rgCacheActions + iTryAgainAction;
    Assert((BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER == pTryAgainAction->type && pContainer == pTryAgainAction->resolveContainer.pContainer) ||
           (BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == pTryAgainAction->type && pContainer == pTryAgainAction->extractContainer.pContainer));
    if (pTryAgainAction->fSkipUntilRetried && !fPayloadCached)
    {
        pTryAgainAction->fSkipUntilRetried = FALSE;

        if (!pContainer->fActuallyAttached)
        {
            pPlan->qwCacheSizeTotal += pContainer->qwFileSize;
        }
    }

    *ppContainerExtractAction = pContainerExtractAction;
    *piContainerTryAgainAction = iTryAgainAction;

LExit:
    ReleaseStr(sczContainerWorkingPath);

    return hr;
}

static HRESULT AddAcquireContainer(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer,
    __out_opt BURN_CACHE_ACTION** ppCacheAction,
    __out_opt DWORD* piCacheAction
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczContainerWorkingPath = NULL;
    BURN_CACHE_ACTION* pAcquireContainerAction = NULL;

    hr = CacheCalculateContainerWorkingPath(pPlan->wzBundleId, pContainer, &sczContainerWorkingPath);
    ExitOnFailure(hr, "Failed to calculate unverified path for container.");

    hr = AppendCacheAction(pPlan, &pAcquireContainerAction);
    ExitOnFailure(hr, "Failed to append acquire container action to plan.");

    pAcquireContainerAction->type = BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER;
    pAcquireContainerAction->resolveContainer.pContainer = pContainer;
    pAcquireContainerAction->resolveContainer.sczUnverifiedPath = sczContainerWorkingPath;
    sczContainerWorkingPath = NULL;

    if (ppCacheAction)
    {
        *ppCacheAction = pAcquireContainerAction;
    }

    if (piCacheAction)
    {
        *piCacheAction = pPlan->cCacheActions - 1;
    }

LExit:
    ReleaseStr(sczContainerWorkingPath);

    return hr;
}

static HRESULT AddExtractPayload(
    __in BURN_CACHE_ACTION* pCacheAction,
    __in_opt BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzPayloadWorkingPath
    )
{
    HRESULT hr = S_OK;

    Assert(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER == pCacheAction->type);

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pCacheAction->extractContainer.rgPayloads), pCacheAction->extractContainer.cPayloads + 1, sizeof(BURN_EXTRACT_PAYLOAD), 5);
    ExitOnFailure(hr, "Failed to grow list of payloads to extract from container.");

    BURN_EXTRACT_PAYLOAD* pExtractPayload = pCacheAction->extractContainer.rgPayloads + pCacheAction->extractContainer.cPayloads;
    pExtractPayload->pPackage = pPackage;
    pExtractPayload->pPayload = pPayload;
    hr = StrAllocString(&pExtractPayload->sczUnverifiedPath, wzPayloadWorkingPath, 0);
    ExitOnFailure(hr, "Failed to copy unverified path for payload to extract.");
    ++pCacheAction->extractContainer.cPayloads;

    pCacheAction->extractContainer.qwTotalExtractSize += pPayload->qwFileSize;

LExit:
    return hr;
}

static BURN_CACHE_ACTION* ProcessSharedPayload(
    __in BURN_PLAN* pPlan,
    __in BURN_PAYLOAD* pPayload
    )
{
    BURN_CACHE_ACTION* pAcquireAction = NULL;
#ifdef DEBUG
    DWORD cMove = 0;
#endif

    for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
    {
        BURN_CACHE_ACTION* pCacheAction = pPlan->rgCacheActions + i;

        if (BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD == pCacheAction->type &&
            pCacheAction->resolvePayload.pPayload == pPayload)
        {
            AssertSz(!pAcquireAction, "There should be at most one acquire cache action per payload.");
            pAcquireAction = pCacheAction;
        }
        else if (BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD == pCacheAction->type &&
                 pCacheAction->cachePayload.pPayload == pPayload &&
                 pCacheAction->cachePayload.fMove)
        {
            // Since we found a shared payload, change its operation from MOVE to COPY.
            pCacheAction->cachePayload.fMove = FALSE;

            AssertSz(1 == ++cMove, "Shared payload should be moved once and only once.");
#ifndef DEBUG
            break;
#endif
        }
        else if (BURN_CACHE_ACTION_TYPE_LAYOUT_PAYLOAD == pCacheAction->type &&
                 pCacheAction->layoutPayload.pPayload == pPayload &&
                 pCacheAction->layoutPayload.fMove)
        {
            // Since we found a shared payload, change its operation from MOVE to COPY if necessary
            pCacheAction->layoutPayload.fMove = FALSE;

            AssertSz(1 == ++cMove, "Shared payload should be moved once and only once.");
#ifndef DEBUG
            break;
#endif
        }
    }

    return pAcquireAction;
}

static HRESULT RemoveUnnecessaryActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    )
{
    HRESULT hr = S_OK;
    LPCSTR szExecuteOrRollback = fExecute ? "execute" : "rollback";

    for (DWORD i = 0; i < cActions; ++i)
    {
        BURN_EXECUTE_ACTION* pAction = rgActions + i;

        // If this MSP targets a package in the chain, check the target's execute state
        // to see if this patch should be skipped.
        if (BURN_EXECUTE_ACTION_TYPE_MSP_TARGET == pAction->type && pAction->mspTarget.pChainedTargetPackage)
        {
            BOOTSTRAPPER_ACTION_STATE chainedTargetPackageAction = fExecute ? pAction->mspTarget.pChainedTargetPackage->execute : pAction->mspTarget.pChainedTargetPackage->rollback;
            if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == chainedTargetPackageAction)
            {
                LogId(REPORT_STANDARD, MSG_PLAN_SKIP_PATCH_ACTION, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.pChainedTargetPackage->sczId, LoggingActionStateToString(chainedTargetPackageAction), szExecuteOrRollback);
                pAction->fDeleted = TRUE;
            }
            else if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL < chainedTargetPackageAction && pAction->mspTarget.fSlipstream && BOOTSTRAPPER_ACTION_STATE_UNINSTALL < pAction->mspTarget.action)
            {
                // If the slipstream target is being installed or upgraded (not uninstalled or repaired) then we will slipstream so skip
                // this action to install the patch standalone. Also, if the slipstream target is being repaired and the patch is being
                // repaired, skip this operation since it will be redundant.
                //
                // The primary goal here is to ensure that a slipstream patch that is yet not installed is installed even if the MSI
                // is already on the machine. The slipstream must be installed standalone if the MSI is being repaired.
                if (BOOTSTRAPPER_ACTION_STATE_REPAIR != chainedTargetPackageAction || BOOTSTRAPPER_ACTION_STATE_REPAIR == pAction->mspTarget.action)
                {
                    LogId(REPORT_STANDARD, MSG_PLAN_SKIP_SLIPSTREAM_ACTION, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.pChainedTargetPackage->sczId, LoggingActionStateToString(chainedTargetPackageAction), szExecuteOrRollback);
                    pAction->fDeleted = TRUE;
                }
            }
        }
    }

    return hr;
}

static HRESULT FinalizeSlipstreamPatchActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < cActions; ++i)
    {
        BURN_EXECUTE_ACTION* pAction = rgActions + i;

        // If this MSI package contains slipstream patches store the slipstream actions.
        if (BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE == pAction->type && pAction->msiPackage.pPackage->Msi.cSlipstreamMspPackages)
        {
            BURN_PACKAGE* pPackage = pAction->msiPackage.pPackage;

            // By default all slipstream actions will be initialized to "no action" (aka: 0).
            pAction->msiPackage.rgSlipstreamPatches = (BOOTSTRAPPER_ACTION_STATE*)MemAlloc(sizeof(BOOTSTRAPPER_ACTION_STATE) * pPackage->Msi.cSlipstreamMspPackages, TRUE);
            ExitOnNull(pAction->msiPackage.rgSlipstreamPatches, hr, E_OUTOFMEMORY, "Failed to allocate memory for patch actions.");

            // If we are uninstalling or repairing the MSI, we must ignore all the slipstream patches because they cannot
            // be applied right now.
            if (BOOTSTRAPPER_ACTION_STATE_REPAIR != pAction->msiPackage.action && BOOTSTRAPPER_ACTION_STATE_UNINSTALL != pAction->msiPackage.action)
            {
                for (DWORD j = 0; j < pPackage->Msi.cSlipstreamMspPackages; ++j)
                {
                    BURN_PACKAGE* pMspPackage = pPackage->Msi.rgpSlipstreamMspPackages[j];
                    AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Only MSP packages can be slipstream patches.");

                    pAction->msiPackage.rgSlipstreamPatches[j] = fExecute ? pMspPackage->execute : pMspPackage->rollback;
                    for (DWORD k = 0; k < pMspPackage->Msp.cTargetProductCodes; ++k)
                    {
                        BURN_MSPTARGETPRODUCT* pTargetProduct = pMspPackage->Msp.rgTargetProducts + k;
                        if (pPackage == pTargetProduct->pChainedTargetPackage)
                        {
                            pAction->msiPackage.rgSlipstreamPatches[j] = fExecute ? pTargetProduct->execute : pTargetProduct->rollback;
                            break;
                        }
                    }
                }
            }
        }
    }

LExit:
    return hr;
}

static HRESULT PlanDependencyActions(
    __in BOOL fBundlePerMachine,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    hr = DependencyPlanPackageBegin(fBundlePerMachine, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to begin plan dependency actions for package: %ls", pPackage->sczId);

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    hr = DependencyPlanPackageComplete(pPackage, pPlan);
    ExitOnFailure(hr, "Failed to complete plan dependency actions for package: %ls", pPackage->sczId);

LExit:
    return hr;
}

static HRESULT CalculateExecuteActions(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __out_opt BOOL* pfBARequestedCache
    )
{
    HRESULT hr = S_OK;

    // Calculate execute actions.
    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_EXE:
        hr = ExeEnginePlanCalculatePackage(pPackage, pfBARequestedCache);
        break;

    case BURN_PACKAGE_TYPE_MSI:
        hr = MsiEnginePlanCalculatePackage(pPackage, pVariables, pUserExperience, pfBARequestedCache);
        break;

    case BURN_PACKAGE_TYPE_MSP:
        hr = MspEnginePlanCalculatePackage(pPackage, pUserExperience, pfBARequestedCache);
        break;

    case BURN_PACKAGE_TYPE_MSU:
        hr = MsuEnginePlanCalculatePackage(pPackage, pfBARequestedCache);
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid package type.");
    }

LExit:
    return hr;
}

static BOOL NeedsCache(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    // All packages that have cacheType set to always should be cached if the bundle is going to be present.
    if (BURN_CACHE_TYPE_ALWAYS == pPackage->cacheType && BOOTSTRAPPER_ACTION_INSTALL <= pPlan->action)
    {
        return TRUE;
    }    
    else if (BURN_PACKAGE_TYPE_EXE == pPackage->type) // Exe packages require the package for all operations (even uninstall).
    {
        return BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute;
    }
    else // The other package types can uninstall without the original package.
    {
        return BOOTSTRAPPER_ACTION_STATE_UNINSTALL < pPackage->execute;
    }
}


#ifdef DEBUG

static void CacheActionLog(
    __in DWORD iAction,
    __in BURN_CACHE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    LPCWSTR wzBase = fRollback ? L"   Rollback cache" : L"   Cache";
    switch (pAction->type)
    {
    case BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: ACQUIRE_CONTAINER id: %ls, source path: %ls, working path: %ls, skip until retried: %hs", wzBase, iAction, pAction->resolveContainer.pContainer->sczId, pAction->resolveContainer.pContainer->sczSourcePath, pAction->resolveContainer.sczUnverifiedPath, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_ACQUIRE_PAYLOAD:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: ACQUIRE_PAYLOAD package id: %ls, payload id: %ls, source path: %ls, working path: %ls, skip until retried: %hs", wzBase, iAction, pAction->resolvePayload.pPackage ? pAction->resolvePayload.pPackage->sczId : L"", pAction->resolvePayload.pPayload->sczKey, pAction->resolvePayload.pPayload->sczSourcePath, pAction->resolvePayload.sczUnverifiedPath, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: CACHE_PAYLOAD package id: %ls, payload id: %ls, working path: %ls, operation: %ls, skip until retried: %hs, retry action: %u", wzBase, iAction, pAction->cachePayload.pPackage->sczId, pAction->cachePayload.pPayload->sczKey, pAction->cachePayload.sczUnverifiedPath, pAction->cachePayload.fMove ? L"move" : L"copy", LoggingBoolToString(pAction->fSkipUntilRetried), pAction->cachePayload.iTryAgainAction);
        break;

    case BURN_CACHE_ACTION_TYPE_CHECKPOINT:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: CHECKPOINT id: %u", wzBase, iAction, pAction->checkpoint.dwId);
        break;

    case BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: EXTRACT_CONTAINER id: %ls, working path: %ls, skip until retried: %hs, skip until acquired by action: %u", wzBase, iAction, pAction->extractContainer.pContainer->sczId, pAction->extractContainer.sczContainerUnverifiedPath, LoggingBoolToString(pAction->fSkipUntilRetried), pAction->extractContainer.iSkipUntilAcquiredByAction);
        for (DWORD j = 0; j < pAction->extractContainer.cPayloads; j++)
        {
            LogStringLine(REPORT_STANDARD, "      extract package id: %ls, payload id: %ls, working path: %ls", pAction->extractContainer.rgPayloads[j].pPackage->sczId, pAction->extractContainer.rgPayloads[j].pPayload->sczKey, pAction->extractContainer.rgPayloads[j].sczUnverifiedPath);
        }
        break;

    case BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: LAYOUT_BUNDLE working path: %ls, layout directory: %ls, exe name: %ls, skip until retried: %hs", wzBase, iAction, pAction->bundleLayout.sczUnverifiedPath, pAction->bundleLayout.sczLayoutDirectory, pAction->bundleLayout.sczExecutableName, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_LAYOUT_CONTAINER:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: LAYOUT_CONTAINER package id: %ls, container id: %ls, working path: %ls, layout directory: %ls, operation: %ls, skip until retried: %hs, retry action: %u", wzBase, iAction, pAction->layoutContainer.pPackage ? pAction->layoutContainer.pPackage->sczId : L"", pAction->layoutContainer.pContainer->sczId, pAction->layoutContainer.sczUnverifiedPath, pAction->layoutContainer.sczLayoutDirectory, pAction->layoutContainer.fMove ? L"move" : L"copy", LoggingBoolToString(pAction->fSkipUntilRetried), pAction->layoutContainer.iTryAgainAction);
        break;

    case BURN_CACHE_ACTION_TYPE_LAYOUT_PAYLOAD:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: LAYOUT_PAYLOAD package id: %ls, payload id: %ls, working path: %ls, layout directory: %ls, operation: %ls, skip until retried: %hs, retry action: %u", wzBase, iAction, pAction->layoutPayload.pPackage ? pAction->layoutPayload.pPackage->sczId : L"", pAction->layoutPayload.pPayload->sczKey, pAction->layoutPayload.sczUnverifiedPath, pAction->layoutPayload.sczLayoutDirectory, pAction->layoutPayload.fMove ? L"move" : L"copy", LoggingBoolToString(pAction->fSkipUntilRetried), pAction->layoutPayload.iTryAgainAction);
        break;

    case BURN_CACHE_ACTION_TYPE_PACKAGE_START:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: PACKAGE_START id: %ls, plan index for skip: %u, payloads to cache: %u, bytes to cache: %llu, skip until retried: %hs", wzBase, iAction, pAction->packageStart.pPackage->sczId, pAction->packageStart.iPackageCompleteAction, pAction->packageStart.cCachePayloads, pAction->packageStart.qwCachePayloadSizeTotal, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_PACKAGE_STOP:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: PACKAGE_STOP id: %ls, skip until retried: %hs", wzBase, iAction, pAction->packageStop.pPackage->sczId, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: ROLLBACK_PACKAGE id: %ls, skip until retried: %hs", wzBase, iAction, pAction->rollbackPackage.pPackage->sczId, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: SIGNAL_SYNCPOINT event handle: 0x%x, skip until retried: %hs", wzBase, iAction, pAction->syncpoint.hEvent, LoggingBoolToString(pAction->fSkipUntilRetried));
        break;

    case BURN_CACHE_ACTION_TYPE_TRANSACTION_BOUNDARY:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: TRANSACTION_BOUNDARY id: %ls, event handle: 0x%x, vital: %ls", wzBase, iAction, pAction->rollbackBoundary.pRollbackBoundary->sczId, pAction->rollbackBoundary.hEvent, pAction->rollbackBoundary.pRollbackBoundary->fVital ? L"yes" : L"no");
        break;

    default:
        AssertSz(FALSE, "Unknown cache action type.");
        break;
    }
}

static void ExecuteActionLog(
    __in DWORD iAction,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    LPCWSTR wzBase = fRollback ? L"   Rollback" : L"   Execute";
    switch (pAction->type)
    {
    case BURN_EXECUTE_ACTION_TYPE_CHECKPOINT:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: CHECKPOINT id: %u", wzBase, iAction, pAction->checkpoint.dwId);
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: PACKAGE_PROVIDER package id: %ls, action: %u", wzBase, iAction, pAction->packageProvider.pPackage->sczId, pAction->packageProvider.action);
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: PACKAGE_DEPENDENCY package id: %ls, bundle provider key: %ls, action: %u", wzBase, iAction, pAction->packageDependency.pPackage->sczId, pAction->packageDependency.sczBundleProviderKey, pAction->packageDependency.action);
        break;

    case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: EXE_PACKAGE package id: %ls, action: %hs, ignore dependencies: %ls", wzBase, iAction, pAction->exePackage.pPackage->sczId, LoggingActionStateToString(pAction->exePackage.action), pAction->exePackage.sczIgnoreDependencies);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: MSI_PACKAGE package id: %ls, action: %hs, ui level: %u, log path: %ls, logging attrib: %u", wzBase, iAction, pAction->msiPackage.pPackage->sczId, LoggingActionStateToString(pAction->msiPackage.action), pAction->msiPackage.uiLevel, pAction->msiPackage.sczLogPath, pAction->msiPackage.dwLoggingAttributes);
        for (DWORD j = 0; j < pAction->msiPackage.cPatches; ++j)
        {
            LogStringLine(REPORT_STANDARD, "      Patch[%u]: order: %u, msp package id: %ls", j, pAction->msiPackage.rgOrderedPatches->dwOrder, pAction->msiPackage.rgOrderedPatches[j].dwOrder, pAction->msiPackage.rgOrderedPatches[j].pPackage->sczId);
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: MSP_TARGET package id: %ls, action: %hs, target product code: %ls, target per-machine: %ls, ui level: %u, log path: %ls", wzBase, iAction, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.sczTargetProductCode, pAction->mspTarget.fPerMachineTarget ? L"yes" : L"no", pAction->mspTarget.uiLevel, pAction->mspTarget.sczLogPath);
        for (DWORD j = 0; j < pAction->mspTarget.cOrderedPatches; ++j)
        {
            LogStringLine(REPORT_STANDARD, "      Patch[%u]: order: %u, msp package id: %ls", j, pAction->mspTarget.rgOrderedPatches[j].dwOrder, pAction->mspTarget.rgOrderedPatches[j].pPackage->sczId);
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: MSU_PACKAGE package id: %ls, action: %hs, log path: %ls", wzBase, iAction, pAction->msuPackage.pPackage->sczId, LoggingActionStateToString(pAction->msuPackage.action), pAction->msuPackage.sczLogPath);
        break;

    case BURN_EXECUTE_ACTION_TYPE_REGISTRATION:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: REGISTRATION keep: %ls", wzBase, iAction, pAction->registration.fKeep ? L"yes" : L"no");
        break;

    case BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: ROLLBACK_BOUNDARY id: %ls, vital: %ls", wzBase, iAction, pAction->rollbackBoundary.pRollbackBoundary->sczId, pAction->rollbackBoundary.pRollbackBoundary->fVital ? L"yes" : L"no");
        break;

    case BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: WAIT_SYNCPOINT event handle: 0x%x", wzBase, iAction, pAction->syncpoint.hEvent);
        break;

    case BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: UNCACHE_PACKAGE id: %ls", wzBase, iAction, pAction->uncachePackage.pPackage->sczId);
        break;

    case BURN_EXECUTE_ACTION_TYPE_COMPATIBLE_PACKAGE:
        LogStringLine(REPORT_STANDARD, "%ls action[%u]: COMPATIBLE_PACKAGE reference id: %ls, installed ProductCode: %ls", wzBase, iAction, pAction->compatiblePackage.pReferencePackage->sczId, pAction->compatiblePackage.sczInstalledProductCode);
        break;

    default:
        AssertSz(FALSE, "Unknown execute action type.");
        break;
    }
}

extern "C" void PlanDump(
    __in BURN_PLAN* pPlan
    )
{
    LogStringLine(REPORT_STANDARD, "--- Begin plan dump ---");

    LogStringLine(REPORT_STANDARD, "Plan action: %hs", LoggingBurnActionToString(pPlan->action));
    LogStringLine(REPORT_STANDARD, "     per-machine: %hs", LoggingTrueFalseToString(pPlan->fPerMachine));
    LogStringLine(REPORT_STANDARD, "     keep registration by default: %hs", LoggingTrueFalseToString(pPlan->fKeepRegistrationDefault));
    LogStringLine(REPORT_STANDARD, "     estimated size: %llu", pPlan->qwEstimatedSize);

    LogStringLine(REPORT_STANDARD, "Plan cache size: %llu", pPlan->qwCacheSizeTotal);
    for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
    {
        CacheActionLog(i, pPlan->rgCacheActions + i, FALSE);
    }

    for (DWORD i = 0; i < pPlan->cRollbackCacheActions; ++i)
    {
        CacheActionLog(i, pPlan->rgRollbackCacheActions + i, TRUE);
    }

    LogStringLine(REPORT_STANDARD, "Plan execute package count: %u", pPlan->cExecutePackagesTotal);
    LogStringLine(REPORT_STANDARD, "     overall progress ticks: %u", pPlan->cOverallProgressTicksTotal);
    for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
    {
        ExecuteActionLog(i, pPlan->rgExecuteActions + i, FALSE);
    }

    for (DWORD i = 0; i < pPlan->cRollbackActions; ++i)
    {
        ExecuteActionLog(i, pPlan->rgRollbackActions + i, TRUE);
    }

    for (DWORD i = 0; i < pPlan->cCleanActions; ++i)
    {
        LogStringLine(REPORT_STANDARD, "   Clean action[%u]: CLEAN_PACKAGE package id: %ls", i, pPlan->rgCleanActions[i].pPackage->sczId);
    }

    for (DWORD i = 0; i < pPlan->cPlannedProviders; ++i)
    {
        LogStringLine(REPORT_STANDARD, "   Dependency action[%u]: PLANNED_PROVIDER key: %ls, name: %ls", i, pPlan->rgPlannedProviders[i].sczKey, pPlan->rgPlannedProviders[i].sczName);
    }

    LogStringLine(REPORT_STANDARD, "--- End plan dump ---");
}

#endif
