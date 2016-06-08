// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// structs

struct BURN_CACHE_THREAD_CONTEXT
{
    BURN_ENGINE_STATE* pEngineState;
    DWORD* pcOverallProgressTicks;
    BOOL* pfRollback;
};


// internal function declarations

static HRESULT ParseCommandLine(
    __in int argc,
    __in LPWSTR* argv,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_PIPE_CONNECTION* pCompanionConnection,
    __in BURN_PIPE_CONNECTION* pEmbeddedConnection,
    __in BURN_VARIABLES* pVariables,
    __out BURN_MODE* pMode,
    __out BURN_AU_PAUSE_ACTION* pAutomaticUpdates,
    __out BOOL* pfDisableSystemRestore,
    __out_z LPWSTR* psczSourceProcessPath,
    __out_z LPWSTR* psczOriginalSource,
    __out BOOL* pfDisableUnelevate,
    __out DWORD *pdwLoggingAttributes,
    __out_z LPWSTR* psczLogFile,
    __out_z LPWSTR* psczActiveParent,
    __out_z LPWSTR* psczIgnoreDependencies,
    __out_z LPWSTR* psczAncestors,
    __out_z LPWSTR* psczSanitizedCommandLine
    );
static HRESULT ParsePipeConnection(
    __in LPWSTR* rgArgs,
    __in BURN_PIPE_CONNECTION* pConnection
    );
static HRESULT DetectPackagePayloadsCached(
    __in BURN_PACKAGE* pPackage
    );
static DWORD WINAPI CacheThreadProc(
    __in LPVOID lpThreadParameter
    );
static HRESULT WaitForCacheThread(
    __in HANDLE hCacheThread
    );
static void LogPackages(
    __in_opt const BURN_PACKAGE* pUpgradeBundlePackage,
    __in_opt const BURN_PACKAGE* pForwardCompatibleBundlePackage,
    __in const BURN_PACKAGES* pPackages,
    __in const BURN_RELATED_BUNDLES* pRelatedBundles,
    __in const BOOTSTRAPPER_ACTION action
    );


// function definitions

extern "C" HRESULT CoreInitialize(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSanitizedCommandLine = NULL;
    LPWSTR sczStreamName = NULL;
    BYTE* pbBuffer = NULL;
    SIZE_T cbBuffer = 0;
    BURN_CONTAINER_CONTEXT containerContext = { };
    BOOL fElevated = FALSE;
    LPWSTR sczSourceProcessPath = NULL;
    LPWSTR sczSourceProcessFolder = NULL;
    LPWSTR sczOriginalSource = NULL;

    // Initialize variables.
    hr = VariableInitialize(&pEngineState->variables);
    ExitOnFailure(hr, "Failed to initialize variables.");

    // Open attached UX container.
    hr = ContainerOpenUX(&pEngineState->section, &containerContext);
    ExitOnFailure(hr, "Failed to open attached UX container.");

    // Load manifest.
    hr = ContainerNextStream(&containerContext, &sczStreamName);
    ExitOnFailure(hr, "Failed to open manifest stream.");

    hr = ContainerStreamToBuffer(&containerContext, &pbBuffer, &cbBuffer);
    ExitOnFailure(hr, "Failed to get manifest stream from container.");

    hr = ManifestLoadXmlFromBuffer(pbBuffer, cbBuffer, pEngineState);
    ExitOnFailure(hr, "Failed to load manifest.");

    // Parse command line.
    hr = ParseCommandLine(pEngineState->argc, pEngineState->argv, &pEngineState->command, &pEngineState->companionConnection, &pEngineState->embeddedConnection, &pEngineState->variables, &pEngineState->mode, &pEngineState->automaticUpdates, &pEngineState->fDisableSystemRestore, &sczSourceProcessPath, &sczOriginalSource, &pEngineState->fDisableUnelevate, &pEngineState->log.dwAttributes, &pEngineState->log.sczPath, &pEngineState->registration.sczActiveParent, &pEngineState->sczIgnoreDependencies, &pEngineState->registration.sczAncestors, &sczSanitizedCommandLine);
    ExitOnFailure(hr, "Failed to parse command line.");

    LogId(REPORT_STANDARD, MSG_BURN_COMMAND_LINE, sczSanitizedCommandLine ? sczSanitizedCommandLine : L"");

    // Retain whether bundle was initially run elevated.
    ProcElevated(::GetCurrentProcess(), &fElevated);

    hr = VariableSetNumeric(&pEngineState->variables, BURN_BUNDLE_ELEVATED, fElevated, TRUE);
    ExitOnFailure(hr, "Failed to overwrite the %ls built-in variable.", BURN_BUNDLE_ELEVATED);

    hr = VariableSetNumeric(&pEngineState->variables, BURN_BUNDLE_UILEVEL, pEngineState->command.display, TRUE);
    ExitOnFailure(hr, "Failed to overwrite the %ls built-in variable.", BURN_BUNDLE_UILEVEL);

    if (sczSourceProcessPath)
    {
        hr = VariableSetLiteralString(&pEngineState->variables, BURN_BUNDLE_SOURCE_PROCESS_PATH, sczSourceProcessPath, TRUE);
        ExitOnFailure(hr, "Failed to set source process path variable.");

        hr = PathGetDirectory(sczSourceProcessPath, &sczSourceProcessFolder);
        ExitOnFailure(hr, "Failed to get source process folder from path.");

        hr = VariableSetLiteralString(&pEngineState->variables, BURN_BUNDLE_SOURCE_PROCESS_FOLDER, sczSourceProcessFolder, TRUE);
        ExitOnFailure(hr, "Failed to set source process folder variable.");
    }

    // Set BURN_BUNDLE_ORIGINAL_SOURCE, if it was passed in on the command line.
    // Needs to be done after ManifestLoadXmlFromBuffer.
    if (sczOriginalSource)
    {
        hr = VariableSetLiteralString(&pEngineState->variables, BURN_BUNDLE_ORIGINAL_SOURCE, sczOriginalSource, FALSE);
        ExitOnFailure(hr, "Failed to set original source variable.");
    }

    if (BURN_MODE_UNTRUSTED == pEngineState->mode || BURN_MODE_NORMAL == pEngineState->mode || BURN_MODE_EMBEDDED == pEngineState->mode)
    {
        hr = CacheInitialize(&pEngineState->registration, &pEngineState->variables, sczSourceProcessPath);
        ExitOnFailure(hr, "Failed to initialize internal cache functionality.");
    }

    // If we're not elevated then we'll be loading the bootstrapper application, so extract
    // the payloads from the BA container.
    if (BURN_MODE_NORMAL == pEngineState->mode || BURN_MODE_EMBEDDED == pEngineState->mode)
    {
        // Extract all UX payloads to working folder.
        hr = UserExperienceEnsureWorkingFolder(pEngineState->registration.sczId, &pEngineState->userExperience.sczTempDirectory);
        ExitOnFailure(hr, "Failed to get unique temporary folder for bootstrapper application.");

        hr = PayloadExtractFromContainer(&pEngineState->userExperience.payloads, NULL, &containerContext, pEngineState->userExperience.sczTempDirectory);
        ExitOnFailure(hr, "Failed to extract bootstrapper application payloads.");

        // Load the catalog files as soon as they are extracted.
        hr = CatalogLoadFromPayload(&pEngineState->catalogs, &pEngineState->userExperience.payloads);
        ExitOnFailure(hr, "Failed to load catalog files.");
    }

LExit:
    ReleaseStr(sczOriginalSource);
    ReleaseStr(sczSourceProcessFolder);
    ReleaseStr(sczSourceProcessPath);
    ContainerClose(&containerContext);
    ReleaseStr(sczStreamName);
    ReleaseStr(sczSanitizedCommandLine);
    ReleaseMem(pbBuffer);

    return hr;
}

extern "C" HRESULT CoreSerializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;

    hr = VariableSerialize(&pEngineState->variables, TRUE, ppbBuffer, piBuffer);
    ExitOnFailure(hr, "Failed to serialize variables.");

LExit:
    return hr;
}

extern "C" HRESULT CoreQueryRegistration(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    BYTE* pbBuffer = NULL;
    SIZE_T cbBuffer = 0;
    SIZE_T iBuffer = 0;

    // Detect if bundle is already installed.
    RegistrationDetectInstalled(&pEngineState->registration, &pEngineState->registration.fInstalled);

    // detect resume type
    hr = RegistrationDetectResumeType(&pEngineState->registration, &pEngineState->command.resumeType);
    ExitOnFailure(hr, "Failed to detect resume type.");

    // If we have a resume mode that suggests the bundle might already be present, try to load any
    // previously stored state.
    if (BOOTSTRAPPER_RESUME_TYPE_INVALID < pEngineState->command.resumeType)
    {
        // load resume state
        hr = RegistrationLoadState(&pEngineState->registration, &pbBuffer, &cbBuffer);
        if (SUCCEEDED(hr))
        {
            hr = VariableDeserialize(&pEngineState->variables, TRUE, pbBuffer, cbBuffer, &iBuffer);
        }

        // Log any failures and continue.
        if (FAILED(hr))
        {
            LogId(REPORT_STANDARD, MSG_CANNOT_LOAD_STATE_FILE, hr, pEngineState->registration.sczStateFile);
            hr = S_OK;
        }
    }

LExit:
    ReleaseBuffer(pbBuffer);

    return hr;
}

extern "C" HRESULT CoreDetect(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated = FALSE;
    BURN_PACKAGE* pPackage = NULL;
    HRESULT hrFirstPackageFailure = S_OK;

    LogId(REPORT_STANDARD, MSG_DETECT_BEGIN, pEngineState->packages.cPackages);

    hr = UserExperienceActivateEngine(&pEngineState->userExperience, &fActivated);
    ExitOnFailure(hr, "Engine cannot start detect because it is busy with another action.");

    int nResult = pEngineState->userExperience.pUserExperience->OnDetectBegin(pEngineState->registration.fInstalled, pEngineState->packages.cPackages);
    hr = UserExperienceInterpretResult(&pEngineState->userExperience, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted detect begin.");

    pEngineState->userExperience.hwndDetect = hwndParent;

    // Always reset the detect state which means the plan should be reset too.
    DetectReset(&pEngineState->registration, &pEngineState->packages);
    PlanReset(&pEngineState->plan, &pEngineState->packages);

    hr = SearchesExecute(&pEngineState->searches, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to execute searches.");

    // Load all of the related bundles.
    hr = RegistrationDetectRelatedBundles(&pEngineState->registration);
    ExitOnFailure(hr, "Failed to detect related bundles.");

    hr = DependencyDetectProviderKeyBundleId(&pEngineState->registration);
    if (SUCCEEDED(hr))
    {
        hr = DetectForwardCompatibleBundle(&pEngineState->userExperience, &pEngineState->command, &pEngineState->registration);
        ExitOnFailure(hr, "Failed to detect forward compatible bundle.");

        // If a forward compatible bundle was detected, skip rest of bundle detection
        // since we will passthrough.
        if (pEngineState->registration.fEnabledForwardCompatibleBundle)
        {
            ExitFunction();
        }
    }
    else if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to detect provider key bundle id.");

    // Report the related bundles.
    hr = DetectReportRelatedBundles(&pEngineState->userExperience, &pEngineState->registration, pEngineState->command.relationType, pEngineState->command.action);
    ExitOnFailure(hr, "Failed to report detected related bundles.");

    // Do update detection.
    hr = DetectUpdate(pEngineState->registration.sczId, &pEngineState->userExperience, &pEngineState->update);
    ExitOnFailure(hr, "Failed to detect update.");

    // Detecting MSPs requires special initialization before processing each package but
    // only do the detection if there are actually patch packages to detect because it
    // can be expensive.
    if (pEngineState->packages.cPatchInfo)
    {
        hr = MspEngineDetectInitialize(&pEngineState->packages);
        ExitOnFailure(hr, "Failed to initialize MSP engine detection.");
    }

    for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
    {
        pPackage = pEngineState->packages.rgPackages + i;

        nResult = pEngineState->userExperience.pUserExperience->OnDetectPackageBegin(pPackage->sczId);
        hr = UserExperienceInterpretResult(&pEngineState->userExperience, MB_OKCANCEL, nResult);
        ExitOnRootFailure(hr, "UX aborted detect package begin.");

        // Detect the cache state of the package.
        hr = DetectPackagePayloadsCached(pPackage);
        ExitOnFailure(hr, "Failed to detect if payloads are all cached for package: %ls", pPackage->sczId);

        // Use the correct engine to detect the package.
        switch (pPackage->type)
        {
        case BURN_PACKAGE_TYPE_EXE:
            hr = ExeEngineDetectPackage(pPackage, &pEngineState->variables);
            break;

        case BURN_PACKAGE_TYPE_MSI:
            hr = MsiEngineDetectPackage(pPackage, &pEngineState->userExperience);
            break;

        case BURN_PACKAGE_TYPE_MSP:
            hr = MspEngineDetectPackage(pPackage, &pEngineState->userExperience);
            break;

        case BURN_PACKAGE_TYPE_MSU:
            hr = MsuEngineDetectPackage(pPackage, &pEngineState->variables);
            break;

        default:
            hr = E_NOTIMPL;
            ExitOnRootFailure(hr, "Package type not supported by detect yet.");
        }

        // If the package detection failed, ensure the package state is set to unknown.
        if (FAILED(hr))
        {
            if (SUCCEEDED(hrFirstPackageFailure))
            {
                hrFirstPackageFailure = hr;
            }

            pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN;
            LogErrorId(hr, MSG_FAILED_DETECT_PACKAGE, pPackage->sczId, NULL, NULL);
        }
        // TODO: consider how to notify the UX that a package is cached.
        //else if (BOOTSTRAPPER_PACKAGE_STATE_CACHED > pPackage->currentState && pPackage->fCached)
        //{
        //     pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_CACHED;
        //}

        pEngineState->userExperience.pUserExperience->OnDetectPackageComplete(pPackage->sczId, hr, pPackage->currentState);
    }

    // Log the detected states.
    for (DWORD iPackage = 0; iPackage < pEngineState->packages.cPackages; ++iPackage)
    {
        pPackage = pEngineState->packages.rgPackages + iPackage;

        LogId(REPORT_STANDARD, MSG_DETECTED_PACKAGE, pPackage->sczId, LoggingPackageStateToString(pPackage->currentState), LoggingCacheStateToString(pPackage->cache));

        if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
        {
            for (DWORD iFeature = 0; iFeature < pPackage->Msi.cFeatures; ++iFeature)
            {
                const BURN_MSIFEATURE* pFeature = pPackage->Msi.rgFeatures + iFeature;
                LogId(REPORT_STANDARD, MSG_DETECTED_MSI_FEATURE, pPackage->sczId, pFeature->sczId, LoggingMsiFeatureStateToString(pFeature->currentState));
            }
        }
        else if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            for (DWORD iTargetProduct = 0; iTargetProduct < pPackage->Msp.cTargetProductCodes; ++iTargetProduct)
            {
                const BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + iTargetProduct;
                LogId(REPORT_STANDARD, MSG_DETECTED_MSP_TARGET, pPackage->sczId, pTargetProduct->wzTargetProductCode, LoggingPackageStateToString(pTargetProduct->patchPackageState));
            }
        }
    }

LExit:
    if (SUCCEEDED(hr))
    {
        hr = hrFirstPackageFailure;
    }

    if (fActivated)
    {
        UserExperienceDeactivateEngine(&pEngineState->userExperience);
    }

    pEngineState->userExperience.pUserExperience->OnDetectComplete(hr);
    pEngineState->userExperience.hwndDetect = NULL;

    LogId(REPORT_STANDARD, MSG_DETECT_COMPLETE, hr);

    return hr;
}

extern "C" HRESULT CorePlan(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOTSTRAPPER_ACTION action
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated = FALSE;
    LPWSTR sczLayoutDirectory = NULL;
    HANDLE hSyncpointEvent = NULL;
    BURN_PACKAGE* pUpgradeBundlePackage = NULL;
    BURN_PACKAGE* pForwardCompatibleBundlePackage = NULL;

    LogId(REPORT_STANDARD, MSG_PLAN_BEGIN, pEngineState->packages.cPackages, LoggingBurnActionToString(action));

    hr = UserExperienceActivateEngine(&pEngineState->userExperience, &fActivated);
    ExitOnFailure(hr, "Engine cannot start plan because it is busy with another action.");

    int nResult = pEngineState->userExperience.pUserExperience->OnPlanBegin(pEngineState->packages.cPackages);
    hr = UserExperienceInterpretResult(&pEngineState->userExperience, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted plan begin.");

    // Always reset the plan.
    PlanReset(&pEngineState->plan, &pEngineState->packages);

    // Remember the overall action state in the plan since it shapes the changes
    // we make everywhere.
    pEngineState->plan.action = action;
    pEngineState->plan.wzBundleId = pEngineState->registration.sczId;
    pEngineState->plan.wzBundleProviderKey = pEngineState->registration.sczId;

    hr = PlanSetVariables(action, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to update action.");

    // Set resume commandline
    hr = PlanSetResumeCommand(&pEngineState->registration, action, &pEngineState->command, &pEngineState->log);
    ExitOnFailure(hr, "Failed to set resume command");

    hr = DependencyPlanInitialize(pEngineState, &pEngineState->plan);
    ExitOnFailure(hr, "Failed to initialize the dependencies for the plan.");

    if (BOOTSTRAPPER_ACTION_LAYOUT == action)
    {
        Assert(!pEngineState->plan.fPerMachine);

        // Plan the bundle's layout.
        hr = PlanLayoutBundle(&pEngineState->plan, pEngineState->registration.sczExecutableName, pEngineState->section.qwBundleSize, &pEngineState->variables, &pEngineState->payloads, &sczLayoutDirectory);
        ExitOnFailure(hr, "Failed to plan the layout of the bundle.");

        // Plan the packages' layout.
        hr = PlanPackages(&pEngineState->registration, &pEngineState->userExperience, &pEngineState->packages, &pEngineState->plan, &pEngineState->log, &pEngineState->variables, FALSE, pEngineState->command.display, pEngineState->command.relationType, sczLayoutDirectory, &hSyncpointEvent);
        ExitOnFailure(hr, "Failed to plan packages.");
    }
    else if (BOOTSTRAPPER_ACTION_UPDATE_REPLACE == action || BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED == action)
    {
        Assert(!pEngineState->plan.fPerMachine);

        pUpgradeBundlePackage = &pEngineState->update.package;

        hr = PlanUpdateBundle(&pEngineState->userExperience, pUpgradeBundlePackage, &pEngineState->plan, &pEngineState->log, &pEngineState->variables, pEngineState->command.display, pEngineState->command.relationType, &hSyncpointEvent);
        ExitOnFailure(hr, "Failed to plan update.");
    }
    else if (pEngineState->registration.fEnabledForwardCompatibleBundle)
    {
        Assert(!pEngineState->plan.fPerMachine);

        pForwardCompatibleBundlePackage = &pEngineState->registration.forwardCompatibleBundle;

        hr = PlanPassThroughBundle(&pEngineState->userExperience, pForwardCompatibleBundlePackage, &pEngineState->plan, &pEngineState->log, &pEngineState->variables, pEngineState->command.display, pEngineState->command.relationType, &hSyncpointEvent);
        ExitOnFailure(hr, "Failed to plan passthrough.");
    }
    else // doing an action that modifies the machine state.
    {
        BOOL fContinuePlanning = TRUE; // assume we'll be able to keep planning after registration.
        pEngineState->plan.fPerMachine = pEngineState->registration.fPerMachine; // default the scope of the plan to the per-machine state of the bundle.

        hr = PlanRegistration(&pEngineState->plan, &pEngineState->registration, pEngineState->command.resumeType, pEngineState->command.relationType, pEngineState->sczIgnoreDependencies, &fContinuePlanning);
        ExitOnFailure(hr, "Failed to plan registration.");

        if (fContinuePlanning)
        {
            // Remember the early index, because we want to be able to insert some related bundles
            // into the plan before other executed packages. This particularly occurs for uninstallation
            // of addons and patches, which should be uninstalled before the main product.
            DWORD dwExecuteActionEarlyIndex = pEngineState->plan.cExecuteActions;

            // Plan the related bundles first to support downgrades with ref-counting.
            hr = PlanRelatedBundlesBegin(&pEngineState->userExperience, &pEngineState->registration, pEngineState->command.relationType, &pEngineState->plan);
            ExitOnFailure(hr, "Failed to plan related bundles.");

            hr = PlanPackages(&pEngineState->registration, &pEngineState->userExperience, &pEngineState->packages, &pEngineState->plan, &pEngineState->log, &pEngineState->variables, pEngineState->registration.fInstalled, pEngineState->command.display, pEngineState->command.relationType, NULL, &hSyncpointEvent);
            ExitOnFailure(hr, "Failed to plan packages.");

            // Schedule the update of related bundles last.
            hr = PlanRelatedBundlesComplete(&pEngineState->registration, &pEngineState->plan, &pEngineState->log, &pEngineState->variables, &hSyncpointEvent, dwExecuteActionEarlyIndex);
            ExitOnFailure(hr, "Failed to schedule related bundles.");
        }
    }

    // Remove unnecessary actions.
    hr = PlanFinalizeActions(&pEngineState->plan);
    ExitOnFailure(hr, "Failed to remove unnecessary actions from plan.");

    // Finally, display all packages and related bundles in the log.
    LogPackages(pUpgradeBundlePackage, pForwardCompatibleBundlePackage, &pEngineState->packages, &pEngineState->registration.relatedBundles, action);

#ifdef DEBUG
    PlanDump(&pEngineState->plan);
#endif

LExit:
    if (fActivated)
    {
        UserExperienceDeactivateEngine(&pEngineState->userExperience);
    }

    pEngineState->userExperience.pUserExperience->OnPlanComplete(hr);

    LogId(REPORT_STANDARD, MSG_PLAN_COMPLETE, hr);
    ReleaseStr(sczLayoutDirectory);

    return hr;
}

extern "C" HRESULT CoreElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    )
{
    HRESULT hr = S_OK;

    // If the elevated companion pipe isn't created yet, let's make that happen.
    if (INVALID_HANDLE_VALUE == pEngineState->companionConnection.hPipe)
    {
        if (!pEngineState->sczBundleEngineWorkingPath)
        {
            hr = CacheBundleToWorkingDirectory(pEngineState->registration.sczId, pEngineState->registration.sczExecutableName, &pEngineState->userExperience.payloads, &pEngineState->section, &pEngineState->sczBundleEngineWorkingPath);
            ExitOnFailure(hr, "Failed to cache engine to working directory.");
        }

        hr = ElevationElevate(pEngineState, hwndParent);
        ExitOnFailure(hr, "Failed to actually elevate.");

        hr = VariableSetNumeric(&pEngineState->variables, BURN_BUNDLE_ELEVATED, TRUE, TRUE);
        ExitOnFailure(hr, "Failed to overwrite the %ls built-in variable.", BURN_BUNDLE_ELEVATED);
    }

LExit:
    return hr;
}

extern "C" HRESULT CoreApply(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated = FALSE;
    HANDLE hLock = NULL;
    DWORD cOverallProgressTicks = 0;
    HANDLE hCacheThread = NULL;
    BOOL fElevated = FALSE;
    BOOL fRegistered = FALSE;
    BOOL fKeepRegistration = pEngineState->plan.fKeepRegistrationDefault;
    BOOL fRollback = FALSE;
    BOOL fSuspend = FALSE;
    BOOTSTRAPPER_APPLY_RESTART restart = BOOTSTRAPPER_APPLY_RESTART_NONE;
    BURN_CACHE_THREAD_CONTEXT cacheThreadContext = { };
    DWORD dwPhaseCount = 0;

    LogId(REPORT_STANDARD, MSG_APPLY_BEGIN);

    hr = UserExperienceActivateEngine(&pEngineState->userExperience, &fActivated);
    ExitOnFailure(hr, "Engine cannot start apply because it is busy with another action.");

    // Ensure any previous attempts to execute are reset.
    ApplyReset(&pEngineState->userExperience, &pEngineState->packages);

    if (pEngineState->plan.cCacheActions)
    {
        ++dwPhaseCount;
    }
    if (pEngineState->plan.cExecuteActions)
    {
        ++dwPhaseCount;
    }
    pEngineState->userExperience.pUserExperience->OnApplyPhaseCount(dwPhaseCount);

    int nResult = pEngineState->userExperience.pUserExperience->OnApplyBegin();
    hr = UserExperienceInterpretResult(&pEngineState->userExperience, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted apply begin.");

    // Abort if this bundle already requires a restart.
    if (BOOTSTRAPPER_RESUME_TYPE_REBOOT_PENDING == pEngineState->command.resumeType)
    {
        restart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;

        hr = HRESULT_FROM_WIN32(ERROR_FAIL_NOACTION_REBOOT);
        UserExperienceSendError(pEngineState->userExperience.pUserExperience, BOOTSTRAPPER_ERROR_TYPE_APPLY, NULL, hr, NULL, MB_ICONERROR | MB_OK, IDNOACTION);
        ExitFunction();
    }

    hr = ApplyLock(FALSE, &hLock);
    ExitOnFailure(hr, "Another per-user setup is already executing.");

    // Initialize only after getting a lock.
    ApplyInitialize();

    pEngineState->userExperience.hwndApply = hwndParent;

    hr = ApplySetVariables(&pEngineState->variables);
    ExitOnFailure(hr, "Failed to set initial apply variables.");

    // If the plan is empty of work to do, skip everything.
    if (!(pEngineState->plan.cRegistrationActions || pEngineState->plan.cCacheActions || pEngineState->plan.cExecuteActions || pEngineState->plan.cCleanActions))
    {
        LogId(REPORT_STANDARD, MSG_APPLY_SKIPPED);
        ExitFunction();
    }

    // Ensure the engine is cached to the working path.
    if (!pEngineState->sczBundleEngineWorkingPath)
    {
        hr = CacheBundleToWorkingDirectory(pEngineState->registration.sczId, pEngineState->registration.sczExecutableName, &pEngineState->userExperience.payloads, &pEngineState->section, &pEngineState->sczBundleEngineWorkingPath);
        ExitOnFailure(hr, "Failed to cache engine to working directory.");
    }

    // Elevate.
    if (pEngineState->plan.fPerMachine)
    {
        hr = CoreElevate(pEngineState, pEngineState->userExperience.hwndApply);
        ExitOnFailure(hr, "Failed to elevate.");

        hr = ElevationApplyInitialize(pEngineState->companionConnection.hPipe, &pEngineState->variables, pEngineState->plan.action, pEngineState->automaticUpdates, !pEngineState->fDisableSystemRestore);
        ExitOnFailure(hr, "Another per-machine setup is already executing.");

        fElevated = TRUE;
    }

    // Register.
    if (pEngineState->plan.fRegister)
    {
        hr = ApplyRegister(pEngineState);
        ExitOnFailure(hr, "Failed to register bundle.");
        fRegistered = TRUE;
    }

    // Cache.
    if (pEngineState->plan.cCacheActions)
    {
        // Launch the cache thread.
        cacheThreadContext.pEngineState = pEngineState;
        cacheThreadContext.pcOverallProgressTicks = &cOverallProgressTicks;
        cacheThreadContext.pfRollback = &fRollback;

        hCacheThread = ::CreateThread(NULL, 0, CacheThreadProc, &cacheThreadContext, 0, NULL);
        ExitOnNullWithLastError(hCacheThread, hr, "Failed to create cache thread.");

        // If we're not caching in parallel, wait for the cache thread to terminate.
        if (!pEngineState->fParallelCacheAndExecute)
        {
            hr = WaitForCacheThread(hCacheThread);
            ExitOnFailure(hr, "Failed while caching, aborting execution.");

            ReleaseHandle(hCacheThread);
        }
    }

    // Execute.
    if (pEngineState->plan.cExecuteActions)
    {
        hr = ApplyExecute(pEngineState, hCacheThread, &cOverallProgressTicks, &fKeepRegistration, &fRollback, &fSuspend, &restart);
        UserExperienceExecutePhaseComplete(&pEngineState->userExperience, hr); // signal that execute completed.
    }

    // Wait for cache thread to terminate, this should return immediately unless we're waiting for layout to complete.
    if (hCacheThread)
    {
        HRESULT hrCached = WaitForCacheThread(hCacheThread);
        if (SUCCEEDED(hr))
        {
            hr = hrCached;
        }
    }

    // If something went wrong or force restarted, skip cleaning.
    if (FAILED(hr) || fRollback || fSuspend || BOOTSTRAPPER_APPLY_RESTART_INITIATED == restart)
    {
        ExitFunction();
    }

    // Clean.
    if (pEngineState->plan.cCleanActions)
    {
        ApplyClean(&pEngineState->userExperience, &pEngineState->plan, pEngineState->companionConnection.hPipe);
    }

LExit:
    // Unregister.
    if (fRegistered)
    {
        ApplyUnregister(pEngineState, FAILED(hr) || fRollback, fKeepRegistration || pEngineState->plan.fDisallowRemoval, fSuspend, restart);
    }

    if (fElevated)
    {
        ElevationApplyUninitialize(pEngineState->companionConnection.hPipe);
    }

    pEngineState->userExperience.hwndApply = NULL;

    ApplyUninitialize();

    if (hLock)
    {
        ::ReleaseMutex(hLock);
        ::CloseHandle(hLock);
    }

    if (fActivated)
    {
        UserExperienceDeactivateEngine(&pEngineState->userExperience);
    }

    ReleaseHandle(hCacheThread);

    nResult = pEngineState->userExperience.pUserExperience->OnApplyComplete(hr, restart);
    if (IDRESTART == nResult)
    {
        pEngineState->fRestart = TRUE;
    }

    LogId(REPORT_STANDARD, MSG_APPLY_COMPLETE, hr, LoggingRestartToString(restart), LoggingBoolToString(pEngineState->fRestart));

    return hr;
}

extern "C" HRESULT CoreLaunchApprovedExe(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    )
{
    HRESULT hr = S_OK;
    BOOL fActivated = FALSE;
    DWORD dwProcessId = 0;

    LogId(REPORT_STANDARD, MSG_LAUNCH_APPROVED_EXE_BEGIN, pLaunchApprovedExe->sczId);

    hr = UserExperienceActivateEngine(&pEngineState->userExperience, &fActivated);
    ExitOnFailure(hr, "Engine cannot start LaunchApprovedExe because it is busy with another action.");

    int nResult = pEngineState->userExperience.pUserExperience->OnLaunchApprovedExeBegin();
    hr = UserExperienceInterpretResult(&pEngineState->userExperience, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted LaunchApprovedExe begin.");

    // Elevate.
    hr = CoreElevate(pEngineState, pLaunchApprovedExe->hwndParent);
    ExitOnFailure(hr, "Failed to elevate.");

    // Launch.
    hr = ElevationLaunchApprovedExe(pEngineState->companionConnection.hPipe, pLaunchApprovedExe, &dwProcessId);

LExit:
    if (fActivated)
    {
        UserExperienceDeactivateEngine(&pEngineState->userExperience);
    }

    pEngineState->userExperience.pUserExperience->OnLaunchApprovedExeComplete(hr, dwProcessId);

    LogId(REPORT_STANDARD, MSG_LAUNCH_APPROVED_EXE_COMPLETE, hr, dwProcessId);

    ApprovedExesUninitializeLaunch(pLaunchApprovedExe);

    return hr;
}

extern "C" HRESULT CoreQuit(
    __in BURN_ENGINE_STATE* pEngineState,
    __in int nExitCode
    )
{
    HRESULT hr = S_OK;

    // Save engine state if resume mode is unequal to "none".
    if (BURN_RESUME_MODE_NONE != pEngineState->resumeMode)
    {
        hr = CoreSaveEngineState(pEngineState);
        if (FAILED(hr))
        {
            LogErrorId(hr, MSG_STATE_NOT_SAVED, NULL, NULL, NULL);
            hr = S_OK;
        }
    }

    LogId(REPORT_STANDARD, MSG_QUIT, nExitCode);

    ::PostQuitMessage(nExitCode); // go bye-bye.

    return hr;
}

extern "C" HRESULT CoreSaveEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    BYTE* pbBuffer = NULL;
    SIZE_T cbBuffer = 0;

    // serialize engine state
    hr = CoreSerializeEngineState(pEngineState, &pbBuffer, &cbBuffer);
    ExitOnFailure(hr, "Failed to serialize engine state.");

    // write to registration store
    if (pEngineState->registration.fPerMachine)
    {
        hr = ElevationSaveState(pEngineState->companionConnection.hPipe, pbBuffer, cbBuffer);
        ExitOnFailure(hr, "Failed to save engine state in per-machine process.");
    }
    else
    {
        hr = RegistrationSaveState(&pEngineState->registration, pbBuffer, cbBuffer);
        ExitOnFailure(hr, "Failed to save engine state.");
    }

LExit:
    ReleaseBuffer(pbBuffer);

    return hr;
}

extern "C" LPCWSTR CoreRelationTypeToCommandLineString(
    __in BOOTSTRAPPER_RELATION_TYPE relationType
    )
{
    LPCWSTR wzRelationTypeCommandLine = NULL;
    switch (relationType)
    {
    case BOOTSTRAPPER_RELATION_DETECT:
        wzRelationTypeCommandLine = BURN_COMMANDLINE_SWITCH_RELATED_DETECT;
        break;
    case BOOTSTRAPPER_RELATION_UPGRADE:
        wzRelationTypeCommandLine = BURN_COMMANDLINE_SWITCH_RELATED_UPGRADE;
        break;
    case BOOTSTRAPPER_RELATION_ADDON:
        wzRelationTypeCommandLine = BURN_COMMANDLINE_SWITCH_RELATED_ADDON;
        break;
    case BOOTSTRAPPER_RELATION_PATCH:
        wzRelationTypeCommandLine = BURN_COMMANDLINE_SWITCH_RELATED_PATCH;
        break;
    case BOOTSTRAPPER_RELATION_UPDATE:
        wzRelationTypeCommandLine = BURN_COMMANDLINE_SWITCH_RELATED_UPDATE;
        break;
    case BOOTSTRAPPER_RELATION_DEPENDENT:
        break;
    case BOOTSTRAPPER_RELATION_NONE: __fallthrough;
    default:
        wzRelationTypeCommandLine = NULL;
        break;
    }

    return wzRelationTypeCommandLine;
}

extern "C" HRESULT CoreRecreateCommandLine(
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
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;
    LPCWSTR wzRelationTypeCommandLine = CoreRelationTypeToCommandLineString(relationType);

    hr = StrAllocString(psczCommandLine, L"", 0);
    ExitOnFailure(hr, "Failed to empty command line.");

    switch (display)
    {
    case BOOTSTRAPPER_DISPLAY_NONE:
        hr = StrAllocConcat(psczCommandLine, L" /quiet", 0);
        break;
    case BOOTSTRAPPER_DISPLAY_PASSIVE:
        hr = StrAllocConcat(psczCommandLine, L" /passive", 0);
        break;
    }
    ExitOnFailure(hr, "Failed to append display state to command-line");

    switch (action)
    {
    case BOOTSTRAPPER_ACTION_MODIFY:
        hr = StrAllocConcat(psczCommandLine, L" /modify", 0);
        break;
    case BOOTSTRAPPER_ACTION_REPAIR:
        hr = StrAllocConcat(psczCommandLine, L" /repair", 0);
        break;
    case BOOTSTRAPPER_ACTION_UNINSTALL:
        hr = StrAllocConcat(psczCommandLine, L" /uninstall", 0);
        break;
    }
    ExitOnFailure(hr, "Failed to append action state to command-line");

    switch (restart)
    {
    case BOOTSTRAPPER_RESTART_ALWAYS:
        hr = StrAllocConcat(psczCommandLine, L" /forcerestart", 0);
        break;
    case BOOTSTRAPPER_RESTART_NEVER:
        hr = StrAllocConcat(psczCommandLine, L" /norestart", 0);
        break;
    }
    ExitOnFailure(hr, "Failed to append restart state to command-line");

    if (wzActiveParent)
    {
        if (*wzActiveParent)
        {
            hr = StrAllocFormatted(&scz, L" /%ls \"%ls\"", BURN_COMMANDLINE_SWITCH_PARENT, wzActiveParent);
            ExitOnFailure(hr, "Failed to format active parent command-line for command-line.");
        }
        else
        {
            hr = StrAllocFormatted(&scz, L" /%ls", BURN_COMMANDLINE_SWITCH_PARENT_NONE);
            ExitOnFailure(hr, "Failed to format parent:none command-line for command-line.");
        }

        hr = StrAllocConcat(psczCommandLine, scz, 0);
        ExitOnFailure(hr, "Failed to append active parent command-line to command-line.");
    }

    if (wzAncestors)
    {
        hr = StrAllocFormatted(&scz, L" /%ls=%ls", BURN_COMMANDLINE_SWITCH_ANCESTORS, wzAncestors);
        ExitOnFailure(hr, "Failed to format ancestors for command-line.");

        hr = StrAllocConcat(psczCommandLine, scz, 0);
        ExitOnFailure(hr, "Failed to append ancestors to command-line.");
    }

    if (wzRelationTypeCommandLine)
    {
        hr = StrAllocFormatted(&scz, L" /%ls", wzRelationTypeCommandLine);
        ExitOnFailure(hr, "Failed to format relation type for command-line.");

        hr = StrAllocConcat(psczCommandLine, scz, 0);
        ExitOnFailure(hr, "Failed to append relation type to command-line.");
    }

    if (fPassthrough)
    {
        hr = StrAllocFormatted(&scz, L" /%ls", BURN_COMMANDLINE_SWITCH_PASSTHROUGH);
        ExitOnFailure(hr, "Failed to format passthrough for command-line.");

        hr = StrAllocConcat(psczCommandLine, scz, 0);
        ExitOnFailure(hr, "Failed to append passthrough to command-line.");
    }

    if (wzAppendLogPath && *wzAppendLogPath)
    {
        hr = StrAllocFormatted(&scz, L" /%ls \"%ls\"", BURN_COMMANDLINE_SWITCH_LOG_APPEND, wzAppendLogPath);
        ExitOnFailure(hr, "Failed to format append log command-line for command-line.");

        hr = StrAllocConcat(psczCommandLine, scz, 0);
        ExitOnFailure(hr, "Failed to append log command-line to command-line");
    }

    if (wzAdditionalCommandLineArguments && *wzAdditionalCommandLineArguments)
    {
        hr = StrAllocConcat(psczCommandLine, L" ", 0);
        ExitOnFailure(hr, "Failed to append space to command-line.");

        hr = StrAllocConcat(psczCommandLine, wzAdditionalCommandLineArguments, 0);
        ExitOnFailure(hr, "Failed to append command-line to command-line.");
    }

LExit:
    ReleaseStr(scz);

    return hr;
}

extern "C" HRESULT CoreAppendFileHandleAttachedToCommandLine(
    __in HANDLE hFileWithAttachedContainer,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine
    )
{
    HRESULT hr = S_OK;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;

    *phExecutableFile = INVALID_HANDLE_VALUE;

    if (!::DuplicateHandle(::GetCurrentProcess(), hFileWithAttachedContainer, ::GetCurrentProcess(), &hExecutableFile, 0, TRUE, DUPLICATE_SAME_ACCESS))
    {
        ExitWithLastError(hr, "Failed to duplicate file handle for attached container.");
    }

    hr = StrAllocFormattedSecure(psczCommandLine, L"%ls -%ls=%u", *psczCommandLine, BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED, hExecutableFile);
    ExitOnFailure(hr, "Failed to append the file handle to the command line.");

    *phExecutableFile = hExecutableFile;
    hExecutableFile = INVALID_HANDLE_VALUE;

LExit:
    ReleaseFileHandle(hExecutableFile);

    return hr;
}

extern "C" HRESULT CoreAppendFileHandleSelfToCommandLine(
    __in LPCWSTR wzExecutablePath,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine,
    __deref_inout_z_opt LPWSTR* psczObfuscatedCommandLine
    )
{
    HRESULT hr = S_OK;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;
    SECURITY_ATTRIBUTES securityAttributes = { };
    securityAttributes.bInheritHandle = TRUE;
    *phExecutableFile = INVALID_HANDLE_VALUE;

    hExecutableFile = ::CreateFileW(wzExecutablePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, &securityAttributes, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE != hExecutableFile)
    {
        hr = StrAllocFormattedSecure(psczCommandLine, L"%ls -%ls=%u", *psczCommandLine, BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF, hExecutableFile);
        ExitOnFailure(hr, "Failed to append the file handle to the command line.");

        if (psczObfuscatedCommandLine)
        {
            hr = StrAllocFormatted(psczObfuscatedCommandLine, L"%ls -%ls=%u", *psczObfuscatedCommandLine, BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF, hExecutableFile);
            ExitOnFailure(hr, "Failed to append the file handle to the obfuscated command line.");
        }

        *phExecutableFile = hExecutableFile;
        hExecutableFile = INVALID_HANDLE_VALUE;
    }

LExit:
    ReleaseFileHandle(hExecutableFile);

    return hr;
}

// internal helper functions

static HRESULT ParseCommandLine(
    __in int argc,
    __in LPWSTR* argv,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_PIPE_CONNECTION* pCompanionConnection,
    __in BURN_PIPE_CONNECTION* pEmbeddedConnection,
    __in BURN_VARIABLES* pVariables,
    __out BURN_MODE* pMode,
    __out BURN_AU_PAUSE_ACTION* pAutomaticUpdates,
    __out BOOL* pfDisableSystemRestore,
    __out_z LPWSTR* psczSourceProcessPath,
    __out_z LPWSTR* psczOriginalSource,
    __out BOOL* pfDisableUnelevate,
    __out DWORD *pdwLoggingAttributes,
    __out_z LPWSTR* psczLogFile,
    __out_z LPWSTR* psczActiveParent,
    __out_z LPWSTR* psczIgnoreDependencies,
    __out_z LPWSTR* psczAncestors,
    __out_z LPWSTR* psczSanitizedCommandLine
    )
{
    HRESULT hr = S_OK;
    BOOL fUnknownArg = FALSE;
    BOOL fHidden = FALSE;
    LPWSTR sczCommandLine = NULL;
    LPWSTR sczSanitizedArgument = NULL;
    LPWSTR sczVariableName = NULL;

    for (int i = 0; i < argc; ++i)
    {
        fUnknownArg = FALSE;
        int originalIndex = i;
        ReleaseNullStr(sczSanitizedArgument);

        if (argv[i][0] == L'-' || argv[i][0] == L'/')
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"l", -1) ||
                CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"log", -1))
            {
                *pdwLoggingAttributes &= ~BURN_LOGGING_ATTRIBUTE_APPEND;

                if (i + 1 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a path for log.");
                }

                ++i;

                hr = StrAllocString(psczLogFile, argv[i], 0);
                ExitOnFailure(hr, "Failed to copy log file path.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"?", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"h", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"help", -1))
            {
                pCommand->action = BOOTSTRAPPER_ACTION_HELP;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"q", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"quiet", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"s", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"silent", -1))
            {
                pCommand->display = BOOTSTRAPPER_DISPLAY_NONE;

                if (BOOTSTRAPPER_RESTART_UNKNOWN == pCommand->restart)
                {
                    pCommand->restart = BOOTSTRAPPER_RESTART_AUTOMATIC;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"passive", -1))
            {
                pCommand->display = BOOTSTRAPPER_DISPLAY_PASSIVE;

                if (BOOTSTRAPPER_RESTART_UNKNOWN == pCommand->restart)
                {
                    pCommand->restart = BOOTSTRAPPER_RESTART_AUTOMATIC;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"norestart", -1))
            {
                pCommand->restart = BOOTSTRAPPER_RESTART_NEVER;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"forcerestart", -1))
            {
                pCommand->restart = BOOTSTRAPPER_RESTART_ALWAYS;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"promptrestart", -1))
            {
                pCommand->restart = BOOTSTRAPPER_RESTART_PROMPT;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"layout", -1))
            {
                if (BOOTSTRAPPER_ACTION_HELP != pCommand->action)
                {
                    pCommand->action = BOOTSTRAPPER_ACTION_LAYOUT;
                }

                // If there is another command line argument and it is not a switch, use that as the layout directory.
                if (i + 1 < argc && argv[i + 1][0] != L'-' && argv[i + 1][0] != L'/')
                {
                    ++i;

                    hr = PathExpand(&pCommand->wzLayoutDirectory, argv[i], PATH_EXPAND_ENVIRONMENT | PATH_EXPAND_FULLPATH);
                    ExitOnFailure(hr, "Failed to copy path for layout directory.");
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"uninstall", -1))
            {
                if (BOOTSTRAPPER_ACTION_HELP != pCommand->action)
                {
                    pCommand->action = BOOTSTRAPPER_ACTION_UNINSTALL;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"repair", -1))
            {
                if (BOOTSTRAPPER_ACTION_HELP != pCommand->action)
                {
                    pCommand->action = BOOTSTRAPPER_ACTION_REPAIR;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"modify", -1))
            {
                if (BOOTSTRAPPER_ACTION_HELP != pCommand->action)
                {
                    pCommand->action = BOOTSTRAPPER_ACTION_MODIFY;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"package", -1) ||
                     CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"update", -1))
            {
                if (BOOTSTRAPPER_ACTION_UNKNOWN == pCommand->action)
                {
                    pCommand->action = BOOTSTRAPPER_ACTION_INSTALL;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"noaupause", -1))
            {
                *pAutomaticUpdates = BURN_AU_PAUSE_ACTION_NONE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"keepaupaused", -1))
            {
                // Switch /noaupause takes precedence.
                if (BURN_AU_PAUSE_ACTION_NONE != *pAutomaticUpdates)
                {
                    *pAutomaticUpdates = BURN_AU_PAUSE_ACTION_IFELEVATED_NORESUME;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"disablesystemrestore", -1))
            {
                *pfDisableSystemRestore = TRUE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"originalsource", -1))
            {
                if (i + 1 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a path for original source.");
                }

                ++i;
                hr = StrAllocString(psczOriginalSource, argv[i], 0);
                ExitOnFailure(hr, "Failed to copy last used source.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_PARENT, -1))
            {
                if (i + 1 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a value for parent.");
                }

                ++i;

                hr = StrAllocString(psczActiveParent, argv[i], 0);
                ExitOnFailure(hr, "Failed to copy parent.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_PARENT_NONE, -1))
            {
                hr = StrAllocString(psczActiveParent, L"", 0);
                ExitOnFailure(hr, "Failed to initialize parent to none.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_LOG_APPEND, -1))
            {
                if (i + 1 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a path for append log.");
                }

                ++i;

                hr = StrAllocString(psczLogFile, argv[i], 0);
                ExitOnFailure(hr, "Failed to copy append log file path.");

                *pdwLoggingAttributes |= BURN_LOGGING_ATTRIBUTE_APPEND;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_ELEVATED, -1))
            {
                if (i + 3 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify the elevated name, token and parent process id.");
                }

                if (BURN_MODE_UNTRUSTED != *pMode)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Multiple mode command-line switches were provided.");
                }

                *pMode = BURN_MODE_ELEVATED;

                ++i;

                hr = ParsePipeConnection(argv + i, pCompanionConnection);
                ExitOnFailure(hr, "Failed to parse elevated connection.");

                i += 2;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_CLEAN_ROOM), BURN_COMMANDLINE_SWITCH_CLEAN_ROOM, lstrlenW(BURN_COMMANDLINE_SWITCH_CLEAN_ROOM)))
            {
                // Get a pointer to the next character after the switch.
                LPCWSTR wzParam = &argv[i][1 + lstrlenW(BURN_COMMANDLINE_SWITCH_CLEAN_ROOM)];
                if (L'=' != wzParam[0] || L'\0' == wzParam[1])
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Missing required parameter for switch: %ls", BURN_COMMANDLINE_SWITCH_CLEAN_ROOM);
                }

                if (BURN_MODE_UNTRUSTED != *pMode)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Multiple mode command-line switches were provided.");
                }

                *pMode = BURN_MODE_NORMAL;

                hr = StrAllocString(psczSourceProcessPath, wzParam + 1, 0);
                ExitOnFailure(hr, "Failed to copy source process path.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_EMBEDDED, -1))
            {
                if (i + 3 >= argc)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Must specify the embedded name, token and parent process id.");
                }

                switch (*pMode)
                {
                case BURN_MODE_UNTRUSTED:
                    // Leave mode as UNTRUSTED to launch the clean room process.
                    break;
                case BURN_MODE_NORMAL:
                    // The initialization code already assumes that the
                    // clean room switch is at the beginning of the command line,
                    // so it's safe to assume that the mode is NORMAL in the clean room.
                    *pMode = BURN_MODE_EMBEDDED;
                    break;
                default:
                    ExitOnRootFailure(hr = E_INVALIDARG, "Multiple mode command-line switches were provided.");
                }

                ++i;

                hr = ParsePipeConnection(argv + i, pEmbeddedConnection);
                ExitOnFailure(hr, "Failed to parse embedded connection.");

                i += 2;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RELATED_DETECT, -1))
            {
                pCommand->relationType = BOOTSTRAPPER_RELATION_DETECT;

                LogId(REPORT_STANDARD, MSG_BURN_RUN_BY_RELATED_BUNDLE, LoggingRelationTypeToString(pCommand->relationType));
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RELATED_UPGRADE, -1))
            {
                pCommand->relationType = BOOTSTRAPPER_RELATION_UPGRADE;

                LogId(REPORT_STANDARD, MSG_BURN_RUN_BY_RELATED_BUNDLE, LoggingRelationTypeToString(pCommand->relationType));
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RELATED_ADDON, -1))
            {
                pCommand->relationType = BOOTSTRAPPER_RELATION_ADDON;

                LogId(REPORT_STANDARD, MSG_BURN_RUN_BY_RELATED_BUNDLE, LoggingRelationTypeToString(pCommand->relationType));
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RELATED_PATCH, -1))
            {
                pCommand->relationType = BOOTSTRAPPER_RELATION_PATCH;

                LogId(REPORT_STANDARD, MSG_BURN_RUN_BY_RELATED_BUNDLE, LoggingRelationTypeToString(pCommand->relationType));
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RELATED_UPDATE, -1))
            {
                pCommand->relationType = BOOTSTRAPPER_RELATION_UPDATE;

                LogId(REPORT_STANDARD, MSG_BURN_RUN_BY_RELATED_BUNDLE, LoggingRelationTypeToString(pCommand->relationType));
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_PASSTHROUGH, -1))
            {
                pCommand->fPassthrough = TRUE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_DISABLE_UNELEVATE, -1))
            {
                *pfDisableUnelevate = TRUE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, BURN_COMMANDLINE_SWITCH_RUNONCE, -1))
            {
                if (BURN_MODE_UNTRUSTED != *pMode)
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Multiple mode command-line switches were provided.");
                }

                *pMode = BURN_MODE_RUNONCE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES), BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, lstrlenW(BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES)))
            {
                // Get a pointer to the next character after the switch.
                LPCWSTR wzParam = &argv[i][1 + lstrlenW(BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES)];
                if (L'=' != wzParam[0] || L'\0' == wzParam[1])
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Missing required parameter for switch: %ls", BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES);
                }

                hr = StrAllocString(psczIgnoreDependencies, &wzParam[1], 0);
                ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_ANCESTORS), BURN_COMMANDLINE_SWITCH_ANCESTORS, lstrlenW(BURN_COMMANDLINE_SWITCH_ANCESTORS)))
            {
                // Get a pointer to the next character after the switch.
                LPCWSTR wzParam = &argv[i][1 + lstrlenW(BURN_COMMANDLINE_SWITCH_ANCESTORS)];
                if (L'=' != wzParam[0] || L'\0' == wzParam[1])
                {
                    ExitOnRootFailure(hr = E_INVALIDARG, "Missing required parameter for switch: %ls", BURN_COMMANDLINE_SWITCH_ANCESTORS);
                }

                hr = StrAllocString(psczAncestors, &wzParam[1], 0);
                ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED), BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED, lstrlenW(BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED)))
            {
                // Already processed in InitializeEngineState.
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF), BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF, lstrlenW(BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF)))
            {
                // Already processed in InitializeEngineState.
            }
            else if (lstrlenW(&argv[i][1]) >= lstrlenW(BURN_COMMANDLINE_SWITCH_PREFIX) &&
                CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], lstrlenW(BURN_COMMANDLINE_SWITCH_PREFIX), BURN_COMMANDLINE_SWITCH_PREFIX, lstrlenW(BURN_COMMANDLINE_SWITCH_PREFIX)))
            {
                // Skip (but log) any other private burn switches we don't recognize, so that
                // adding future private variables doesn't break old bundles
                LogId(REPORT_STANDARD, MSG_BURN_UNKNOWN_PRIVATE_SWITCH, &argv[i][1]);
            }
            else
            {
                fUnknownArg = TRUE;
            }
        }
        else
        {
            fUnknownArg = TRUE;

            const wchar_t* pwc = wcschr(argv[i], L'=');
            if (pwc)
            {
                hr = StrAllocString(&sczVariableName, argv[i], pwc - argv[i]);
                ExitOnFailure(hr, "Failed to copy variable name.");

                hr = VariableIsHidden(pVariables, sczVariableName, &fHidden);
                ExitOnFailure(hr, "Failed to determine whether variable is hidden.");

                if (fHidden)
                {
                    hr = StrAllocFormatted(&sczSanitizedArgument, L"%ls=*****", sczVariableName);
                    ExitOnFailure(hr, "Failed to copy sanitized argument.");
                }
            }
        }

        // Remember command-line switch to pass off to UX.
        if (fUnknownArg)
        {
            PathCommandLineAppend(&pCommand->wzCommandLine, argv[i]);
        }

        if (sczSanitizedArgument)
        {
            PathCommandLineAppend(psczSanitizedCommandLine, sczSanitizedArgument);
        }
        else
        {
            for (; originalIndex <= i; ++originalIndex)
            {
                PathCommandLineAppend(psczSanitizedCommandLine, argv[originalIndex]);
            }
        }
    }

    // If embedded, ensure the display goes embedded as well.
    if (BURN_MODE_EMBEDDED == *pMode)
    {
        pCommand->display = BOOTSTRAPPER_DISPLAY_EMBEDDED;
    }

    // Set the defaults if nothing was set above.
    if (BOOTSTRAPPER_ACTION_UNKNOWN == pCommand->action)
    {
        pCommand->action = BOOTSTRAPPER_ACTION_INSTALL;
    }

    if (BOOTSTRAPPER_DISPLAY_UNKNOWN == pCommand->display)
    {
        pCommand->display = BOOTSTRAPPER_DISPLAY_FULL;
    }

    if (BOOTSTRAPPER_RESTART_UNKNOWN == pCommand->restart)
    {
        pCommand->restart = BOOTSTRAPPER_RESTART_PROMPT;
    }

LExit:
    ReleaseStr(sczVariableName);
    ReleaseStr(sczSanitizedArgument);
    ReleaseStr(sczCommandLine);

    return hr;
}

static HRESULT ParsePipeConnection(
    __in_ecount(3) LPWSTR* rgArgs,
    __in BURN_PIPE_CONNECTION* pConnection
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocString(&pConnection->sczName, rgArgs[0], 0);
    ExitOnFailure(hr, "Failed to copy connection name from command line.");

    hr = StrAllocString(&pConnection->sczSecret, rgArgs[1], 0);
    ExitOnFailure(hr, "Failed to copy connection secret from command line.");

    hr = StrStringToUInt32(rgArgs[2], 0, reinterpret_cast<UINT*>(&pConnection->dwProcessId));
    ExitOnFailure(hr, "Failed to copy parent process id from command line.");

LExit:
    return hr;
}

static HRESULT DetectPackagePayloadsCached(
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachePath = NULL;
    BURN_CACHE_STATE cache = BURN_CACHE_STATE_NONE; // assume the package will not be cached.
    LPWSTR sczPayloadCachePath = NULL;
    LONGLONG llSize = 0;

    if (pPackage->sczCacheId && *pPackage->sczCacheId)
    {
        hr = CacheGetCompletedPath(pPackage->fPerMachine, pPackage->sczCacheId, &sczCachePath);
        ExitOnFailure(hr, "Failed to get completed cache path.");

        // If the cached directory exists, we have something.
        if (DirExists(sczCachePath, NULL))
        {
            cache = BURN_CACHE_STATE_COMPLETE; // assume all payloads are cached.

            // Check all payloads to see if any are missing or not the right size.
            for (DWORD i = 0; i < pPackage->cPayloads; ++i)
            {
                BURN_PACKAGE_PAYLOAD* pPackagePayload = pPackage->rgPayloads + i;

                hr = PathConcat(sczCachePath, pPackagePayload->pPayload->sczFilePath, &sczPayloadCachePath);
                ExitOnFailure(hr, "Failed to concat payload cache path.");

                hr = FileSize(sczPayloadCachePath, &llSize);
                if (SUCCEEDED(hr) && static_cast<DWORD64>(llSize) != pPackagePayload->pPayload->qwFileSize)
                {
                    hr = HRESULT_FROM_WIN32(ERROR_FILE_CORRUPT); // size did not match expectations, so cache must have the wrong file.
                }

                if (SUCCEEDED(hr))
                {
                    // TODO: should we do a full on hash verification on the file to ensure
                    //       the exact right file is cached?

                    pPackagePayload->fCached = TRUE;
                }
                else
                {
                    LogId(REPORT_STANDARD, MSG_DETECT_PACKAGE_NOT_FULLY_CACHED, pPackage->sczId, pPackagePayload->pPayload->sczKey, hr);

                    cache = BURN_CACHE_STATE_PARTIAL; // found a payload that was not cached so we are partial.
                    hr = S_OK;
                }
            }
        }
    }

    pPackage->cache = cache;

LExit:
    ReleaseStr(sczPayloadCachePath);
    ReleaseStr(sczCachePath);
    return hr;
}

static DWORD WINAPI CacheThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_THREAD_CONTEXT* pContext = reinterpret_cast<BURN_CACHE_THREAD_CONTEXT*>(lpThreadParameter);
    BURN_ENGINE_STATE* pEngineState = pContext->pEngineState;
    DWORD* pcOverallProgressTicks = pContext->pcOverallProgressTicks;
    BOOL* pfRollback = pContext->pfRollback;
    BOOL fComInitialized = FALSE;

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM on cache thread.");
    fComInitialized = TRUE;

    // cache packages
    hr = ApplyCache(pEngineState->section.hSourceEngineFile, &pEngineState->userExperience, &pEngineState->variables, &pEngineState->plan, pEngineState->companionConnection.hCachePipe, pcOverallProgressTicks, pfRollback);

LExit:
    UserExperienceExecutePhaseComplete(&pEngineState->userExperience, hr); // signal that cache completed.

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return (DWORD)hr;
}

static HRESULT WaitForCacheThread(
    __in HANDLE hCacheThread
    )
{
    HRESULT hr = S_OK;

    if (WAIT_OBJECT_0 != ::WaitForSingleObject(hCacheThread, INFINITE))
    {
        ExitWithLastError(hr, "Failed to wait for cache thread to terminate.");
    }

    if (!::GetExitCodeThread(hCacheThread, (DWORD*)&hr))
    {
        ExitWithLastError(hr, "Failed to get cache thread exit code.");
    }

LExit:
    return hr;
}

static void LogPackages(
    __in_opt const BURN_PACKAGE* pUpgradeBundlePackage,
    __in_opt const BURN_PACKAGE* pForwardCompatibleBundlePackage,
    __in const BURN_PACKAGES* pPackages,
    __in const BURN_RELATED_BUNDLES* pRelatedBundles,
    __in const BOOTSTRAPPER_ACTION action
    )
{
    if (pUpgradeBundlePackage)
    {
        LogId(REPORT_STANDARD, MSG_PLANNED_UPGRADE_BUNDLE, pUpgradeBundlePackage->sczId, LoggingRequestStateToString(pUpgradeBundlePackage->defaultRequested), LoggingRequestStateToString(pUpgradeBundlePackage->requested), LoggingActionStateToString(pUpgradeBundlePackage->execute), LoggingActionStateToString(pUpgradeBundlePackage->rollback), LoggingDependencyActionToString(pUpgradeBundlePackage->dependencyExecute));
    }
    else if (pForwardCompatibleBundlePackage)
    {
        LogId(REPORT_STANDARD, MSG_PLANNED_FORWARD_COMPATIBLE_BUNDLE, pForwardCompatibleBundlePackage->sczId, LoggingRequestStateToString(pForwardCompatibleBundlePackage->defaultRequested), LoggingRequestStateToString(pForwardCompatibleBundlePackage->requested), LoggingActionStateToString(pForwardCompatibleBundlePackage->execute), LoggingActionStateToString(pForwardCompatibleBundlePackage->rollback), LoggingDependencyActionToString(pForwardCompatibleBundlePackage->dependencyExecute));
    }
    else
    {
        // Display related bundles first if uninstalling.
        if (BOOTSTRAPPER_ACTION_UNINSTALL == action && 0 < pRelatedBundles->cRelatedBundles)
        {
            for (int i = pRelatedBundles->cRelatedBundles - 1; 0 <= i; --i)
            {
                const BURN_RELATED_BUNDLE* pRelatedBundle = &pRelatedBundles->rgRelatedBundles[i];
                const BURN_PACKAGE* pPackage = &pRelatedBundle->package;

                LogId(REPORT_STANDARD, MSG_PLANNED_RELATED_BUNDLE, pPackage->sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), LoggingRequestStateToString(pPackage->defaultRequested), LoggingRequestStateToString(pPackage->requested), LoggingActionStateToString(pPackage->execute), LoggingActionStateToString(pPackage->rollback), LoggingDependencyActionToString(pPackage->dependencyExecute));
            }
        }

        // Display all the packages in the log.
        for (DWORD i = 0; i < pPackages->cPackages; ++i)
        {
            const DWORD iPackage = (BOOTSTRAPPER_ACTION_UNINSTALL == action) ? pPackages->cPackages - 1 - i : i;
            const BURN_PACKAGE* pPackage = &pPackages->rgPackages[iPackage];

            LogId(REPORT_STANDARD, MSG_PLANNED_PACKAGE, pPackage->sczId, LoggingPackageStateToString(pPackage->currentState), LoggingRequestStateToString(pPackage->defaultRequested), LoggingRequestStateToString(pPackage->requested), LoggingActionStateToString(pPackage->execute), LoggingActionStateToString(pPackage->rollback), LoggingBoolToString(pPackage->fAcquire), LoggingBoolToString(pPackage->fUncache), LoggingDependencyActionToString(pPackage->dependencyExecute));
        }

        for (DWORD i = 0; i < pPackages->cCompatiblePackages; ++i)
        {
            const DWORD iPackage = (BOOTSTRAPPER_ACTION_UNINSTALL == action) ? pPackages->cCompatiblePackages - 1 - i : i;
            const BURN_PACKAGE* pPackage = &pPackages->rgCompatiblePackages[iPackage];

            LogId(REPORT_STANDARD, MSG_PLANNED_PACKAGE, pPackage->sczId, LoggingPackageStateToString(pPackage->currentState), LoggingRequestStateToString(pPackage->defaultRequested), LoggingRequestStateToString(pPackage->requested), LoggingActionStateToString(pPackage->execute), LoggingActionStateToString(pPackage->rollback), LoggingBoolToString(pPackage->fAcquire), LoggingBoolToString(pPackage->fUncache), LoggingDependencyActionToString(pPackage->dependencyExecute));
        }

        // Display related bundles last if caching, installing, modifying, or repairing.
        if (BOOTSTRAPPER_ACTION_UNINSTALL < action && 0 < pRelatedBundles->cRelatedBundles)
        {
            for (DWORD i = 0; i < pRelatedBundles->cRelatedBundles; ++i)
            {
                const BURN_RELATED_BUNDLE* pRelatedBundle = &pRelatedBundles->rgRelatedBundles[i];
                const BURN_PACKAGE* pPackage = &pRelatedBundle->package;

                LogId(REPORT_STANDARD, MSG_PLANNED_RELATED_BUNDLE, pPackage->sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), LoggingRequestStateToString(pPackage->defaultRequested), LoggingRequestStateToString(pPackage->requested), LoggingActionStateToString(pPackage->execute), LoggingActionStateToString(pPackage->rollback), LoggingDependencyActionToString(pPackage->dependencyExecute));
            }
        }
    }
}
