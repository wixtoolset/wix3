// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef struct _DETECT_AUTHENTICATION_REQUIRED_DATA
{
    BURN_USER_EXPERIENCE* pUX;
    LPCWSTR wzPackageOrContainerId;
} DETECT_AUTHENTICATION_REQUIRED_DATA;

// internal function definitions
static HRESULT AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    );

static HRESULT DetectAtomFeedUpdate(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    );

static HRESULT DownloadUpdateFeed(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate,
    __deref_inout_z LPWSTR* psczTempFile
    );

// function definitions

extern "C" void DetectReset(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages
    )
{
    RelatedBundlesUninitialize(&pRegistration->relatedBundles);
    ReleaseNullStr(pRegistration->sczDetectedProviderKeyBundleId);
    pRegistration->fEnabledForwardCompatibleBundle = FALSE;
    PackageUninitialize(&pRegistration->forwardCompatibleBundle);

    for (DWORD iPackage = 0; iPackage < pPackages->cPackages; ++iPackage)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPackages + iPackage;

        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN;

        pPackage->cache = BURN_CACHE_STATE_NONE;
        for (DWORD iPayload = 0; iPayload < pPackage->cPayloads; ++iPayload)
        {
            BURN_PACKAGE_PAYLOAD* pPayload = pPackage->rgPayloads + iPayload;
            pPayload->fCached = FALSE;
        }

        if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
        {
            for (DWORD iFeature = 0; iFeature < pPackage->Msi.cFeatures; ++iFeature)
            {
                BURN_MSIFEATURE* pFeature = pPackage->Msi.rgFeatures + iFeature;

                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            }
        }
        else if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            ReleaseNullMem(pPackage->Msp.rgTargetProducts);
            pPackage->Msp.cTargetProductCodes = 0;
        }
    }

    for (DWORD iPatchInfo = 0; iPatchInfo < pPackages->cPatchInfo; ++iPatchInfo)
    {
        MSIPATCHSEQUENCEINFOW* pPatchInfo = pPackages->rgPatchInfo + iPatchInfo;
        pPatchInfo->dwOrder = 0;
        pPatchInfo->uStatus = 0;
    }
}

extern "C" HRESULT DetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    int nRecommendation = IDNOACTION;

    if (pRegistration->sczDetectedProviderKeyBundleId &&
        CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczDetectedProviderKeyBundleId, -1, pRegistration->sczId, -1))
    {
        // Only change the recommendation if an parent was provided.
        if (pRegistration->sczActiveParent && *pRegistration->sczActiveParent)
        {
            // On install, recommend running the forward compatible bundle because there is an active parent. This
            // will essentially register the parent with the forward compatible bundle.
            if (BOOTSTRAPPER_ACTION_INSTALL == pCommand->action)
            {
                nRecommendation = IDOK;
            }
            else if (BOOTSTRAPPER_ACTION_UNINSTALL == pCommand->action ||
                     BOOTSTRAPPER_ACTION_MODIFY == pCommand->action ||
                     BOOTSTRAPPER_ACTION_REPAIR == pCommand->action)
            {
                // When modifying the bundle, only recommend running the forward compatible bundle if the parent
                // is already registered as a dependent of the provider key.
                if (DependencyDependentExists(pRegistration, pRegistration->sczActiveParent))
                {
                    nRecommendation = IDOK;
                }
            }
        }

        for (DWORD iRelatedBundle = 0; iRelatedBundle < pRegistration->relatedBundles.cRelatedBundles; ++iRelatedBundle)
        {
            BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + iRelatedBundle;

            if (BOOTSTRAPPER_RELATION_UPGRADE == pRelatedBundle->relationType &&
                pRegistration->qwVersion <= pRelatedBundle->qwVersion &&
                CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczDetectedProviderKeyBundleId, -1, pRelatedBundle->package.sczId, -1))
            {
                int nResult = pUX->pUserExperience->OnDetectForwardCompatibleBundle(pRelatedBundle->package.sczId, pRelatedBundle->relationType, pRelatedBundle->sczTag, pRelatedBundle->package.fPerMachine, pRelatedBundle->qwVersion, nRecommendation);
                hr = UserExperienceInterpretResult(pUX, MB_OKCANCEL, nResult);
                ExitOnRootFailure(hr, "BA aborted detect forward compatible bundle.");

                if (IDOK == nResult)
                {
                    hr = PseudoBundleInitializePassthrough(&pRegistration->forwardCompatibleBundle, pCommand, NULL, pRegistration->sczActiveParent, pRegistration->sczAncestors, &pRelatedBundle->package);
                    ExitOnFailure(hr, "Failed to initialize update bundle.");

                    pRegistration->fEnabledForwardCompatibleBundle = TRUE;
                }

                LogId(REPORT_STANDARD, MSG_DETECTED_FORWARD_COMPATIBLE_BUNDLE, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), LoggingPerMachineToString(pRelatedBundle->package.fPerMachine), LoggingVersionToString(pRelatedBundle->qwVersion), LoggingBoolToString(pRegistration->fEnabledForwardCompatibleBundle));
                break;
            }
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT DetectReportRelatedBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOTSTRAPPER_ACTION action
    )
{
    HRESULT hr = S_OK;

    for (DWORD iRelatedBundle = 0; iRelatedBundle < pRegistration->relatedBundles.cRelatedBundles; ++iRelatedBundle)
    {
        const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + iRelatedBundle;
        BOOTSTRAPPER_RELATED_OPERATION operation = BOOTSTRAPPER_RELATED_OPERATION_NONE;

        switch (pRelatedBundle->relationType)
        {
        case BOOTSTRAPPER_RELATION_UPGRADE:
            if (BOOTSTRAPPER_RELATION_UPGRADE != relationType && BOOTSTRAPPER_ACTION_UNINSTALL < action)
            {
                if (pRegistration->qwVersion > pRelatedBundle->qwVersion)
                {
                    operation = BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE;
                }
                else if (pRegistration->qwVersion < pRelatedBundle->qwVersion)
                {
                    operation = BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE;
                }
            }
            break;

        case BOOTSTRAPPER_RELATION_PATCH: __fallthrough;
        case BOOTSTRAPPER_RELATION_ADDON:
            if (BOOTSTRAPPER_ACTION_UNINSTALL == action)
            {
                operation = BOOTSTRAPPER_RELATED_OPERATION_REMOVE;
            }
            else if (BOOTSTRAPPER_ACTION_INSTALL == action || BOOTSTRAPPER_ACTION_MODIFY == action)
            {
                operation = BOOTSTRAPPER_RELATED_OPERATION_INSTALL;
            }
            else if (BOOTSTRAPPER_ACTION_REPAIR == action)
            {
                operation = BOOTSTRAPPER_RELATED_OPERATION_REPAIR;
            }
            break;

        case BOOTSTRAPPER_RELATION_DETECT: __fallthrough;
        case BOOTSTRAPPER_RELATION_DEPENDENT:
            break;

        default:
            hr = E_FAIL;
            ExitOnRootFailure1(hr, "Unexpected relation type encountered: %d", pRelatedBundle->relationType);
            break;
        }

        LogId(REPORT_STANDARD, MSG_DETECTED_RELATED_BUNDLE, pRelatedBundle->package.sczId, LoggingRelationTypeToString(pRelatedBundle->relationType), LoggingPerMachineToString(pRelatedBundle->package.fPerMachine), LoggingVersionToString(pRelatedBundle->qwVersion), LoggingRelatedOperationToString(operation));

        int nResult = pUX->pUserExperience->OnDetectRelatedBundle(pRelatedBundle->package.sczId, pRelatedBundle->relationType, pRelatedBundle->sczTag, pRelatedBundle->package.fPerMachine, pRelatedBundle->qwVersion, operation);
        hr = UserExperienceInterpretResult(pUX, MB_OKCANCEL, nResult);
        ExitOnRootFailure(hr, "BA aborted detect related bundle.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DetectUpdate(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    )
{
    HRESULT hr = S_OK;
    int nResult = IDNOACTION;
    BOOL fBeginCalled = FALSE;

    // If no update source was specified, skip update detection.
    if (!pUpdate->sczUpdateSource || !*pUpdate->sczUpdateSource)
    {
        ExitFunction();
    }

    fBeginCalled = TRUE;

    nResult = pUX->pUserExperience->OnDetectUpdateBegin(pUpdate->sczUpdateSource, IDNOACTION);
    hr = UserExperienceInterpretResult(pUX, MB_OKCANCEL, nResult);
    ExitOnRootFailure(hr, "UX aborted detect update begin.");

    if (IDNOACTION == nResult)
    {
        //pUpdate->fUpdateAvailable = FALSE;
    }
    else if (IDOK == nResult)
    {
        hr = DetectAtomFeedUpdate(wzBundleId, pUX, pUpdate);
        ExitOnFailure(hr, "Failed to detect atom feed update.");
    }

LExit:
    if (fBeginCalled)
    {
        pUX->pUserExperience->OnDetectUpdateComplete(hr, pUpdate->fUpdateAvailable ? pUpdate->sczUpdateSource : NULL);
    }

    return hr;
}

static HRESULT AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    )
{
    Assert(401 == lHttpCode || 407 == lHttpCode);

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    BOOTSTRAPPER_ERROR_TYPE errorType = (401 == lHttpCode) ? BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER : BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY;
    LPWSTR sczError = NULL;
    DETECT_AUTHENTICATION_REQUIRED_DATA* pAuthenticationData = reinterpret_cast<DETECT_AUTHENTICATION_REQUIRED_DATA*>(pData);

    *pfRetrySend = FALSE;
    *pfRetry = FALSE;

    hr = StrAllocFromError(&sczError, HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED), NULL);
    ExitOnFailure(hr, "Failed to allocation error string.");

    int nResult = pAuthenticationData->pUX->pUserExperience->OnError(errorType, pAuthenticationData->wzPackageOrContainerId, ERROR_ACCESS_DENIED, sczError, MB_RETRYTRYAGAIN, 0, NULL, IDNOACTION);
    nResult = UserExperienceCheckExecuteResult(pAuthenticationData->pUX, FALSE, MB_RETRYTRYAGAIN, nResult);
    if (IDTRYAGAIN == nResult && pAuthenticationData->pUX->hwndDetect)
    {
        er = ::InternetErrorDlg(pAuthenticationData->pUX->hwndDetect, hUrl, ERROR_INTERNET_INCORRECT_PASSWORD, FLAGS_ERROR_UI_FILTER_FOR_ERRORS | FLAGS_ERROR_UI_FLAGS_CHANGE_OPTIONS | FLAGS_ERROR_UI_FLAGS_GENERATE_DATA, NULL);
        if (ERROR_SUCCESS == er || ERROR_CANCELLED == er)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }
        else if (ERROR_INTERNET_FORCE_RETRY == er)
        {
            *pfRetrySend = TRUE;
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
        }
    }
    else if (IDRETRY == nResult)
    {
        *pfRetry = TRUE;
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
    }

LExit:
    ReleaseStr(sczError);

    return hr;
}

static HRESULT DownloadUpdateFeed(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate,
    __deref_inout_z LPWSTR* psczTempFile
    )
{
    HRESULT hr = S_OK;
    DOWNLOAD_SOURCE downloadSource = { };
    DOWNLOAD_CACHE_CALLBACK cacheCallback = { };
    DOWNLOAD_AUTHENTICATION_CALLBACK authenticationCallback = { };
    DETECT_AUTHENTICATION_REQUIRED_DATA authenticationData = { };
    DWORD64 qwDownloadSize = 0;

    // Always do our work in the working folder, even if cached.
    hr = PathCreateTimeBasedTempFile(NULL, L"UpdateFeed", NULL, L"xml", psczTempFile, NULL);
    ExitOnFailure(hr, "Failed to create UpdateFeed based on current system time.");

    // Do we need a means of the BA to pass in a username and password? If so, we should copy it to downloadSource here
    hr = StrAllocString(&downloadSource.sczUrl, pUpdate->sczUpdateSource, 0);
    ExitOnFailure(hr, "Failed to copy update url.");

    cacheCallback.pfnProgress = NULL; //UpdateProgressRoutine;
    cacheCallback.pfnCancel = NULL; // TODO: set this
    cacheCallback.pv = NULL; //pProgress;

    authenticationData.pUX = pUX;
    authenticationData.wzPackageOrContainerId = wzBundleId;

    authenticationCallback.pv =  static_cast<LPVOID>(&authenticationData);
    authenticationCallback.pfnAuthenticate = &AuthenticationRequired;

    hr = DownloadUrl(&downloadSource, qwDownloadSize, *psczTempFile, &cacheCallback, &authenticationCallback);
    ExitOnFailure2(hr, "Failed attempt to download update feed from URL: '%ls' to: '%ls'", downloadSource.sczUrl, *psczTempFile);

LExit:
    if (FAILED(hr))
    {
        if (*psczTempFile)
        {
            FileEnsureDelete(*psczTempFile);
        }

        ReleaseNullStr(*psczTempFile);
    }

    ReleaseStr(downloadSource.sczUrl);
    ReleaseStr(downloadSource.sczUser);
    ReleaseStr(downloadSource.sczPassword);
    return hr;
}


static HRESULT DetectAtomFeedUpdate(
    __in_z LPCWSTR wzBundleId,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    )
{
    Assert(pUpdate && pUpdate->sczUpdateSource && *pUpdate->sczUpdateSource);
#ifdef DEBUG
    LogLine(REPORT_STANDARD, "DetectAtomFeedUpdate() - update location: %ls", pUpdate->sczUpdateSource);
#endif


    HRESULT hr = S_OK;
    int nResult = IDNOACTION;
    LPWSTR sczUpdateFeedTempFile = NULL;

    ATOM_FEED* pAtomFeed = NULL;
    APPLICATION_UPDATE_CHAIN* pApupChain = NULL;

    hr = AtomInitialize();
    ExitOnFailure(hr, "Failed to initialize Atom.");

    hr = DownloadUpdateFeed(wzBundleId, pUX, pUpdate, &sczUpdateFeedTempFile);
    ExitOnFailure(hr, "Failed to download update feed.");

    hr = AtomParseFromFile(sczUpdateFeedTempFile, &pAtomFeed);
    ExitOnFailure1(hr, "Failed to parse update atom feed: %ls.", sczUpdateFeedTempFile);

    hr = ApupAllocChainFromAtom(pAtomFeed, &pApupChain);
    ExitOnFailure(hr, "Failed to allocate update chain from atom feed.");

    if (0 < pApupChain->cEntries)
    {
        for (DWORD i = 0; i < pApupChain->cEntries; ++i)
        {
            APPLICATION_UPDATE_ENTRY* pAppUpdateEntry = &pApupChain->rgEntries[i];

            nResult = pUX->pUserExperience->OnDetectUpdate(pAppUpdateEntry->rgEnclosures ? pAppUpdateEntry->rgEnclosures->wzUrl : NULL, 
                pAppUpdateEntry->rgEnclosures ? pAppUpdateEntry->rgEnclosures->dw64Size : 0, 
                pAppUpdateEntry->dw64Version, pAppUpdateEntry->wzTitle,
                pAppUpdateEntry->wzSummary, pAppUpdateEntry->wzContentType, pAppUpdateEntry->wzContent, IDNOACTION);

            switch (nResult)
            {
                case IDNOACTION:
                    hr = S_OK;
                    continue;
                case IDOK:   
                    pUpdate->fUpdateAvailable = TRUE;
                    hr = S_OK;
                    break;
                case IDCANCEL:
                    hr = S_FALSE;
                    break;
                default:
                    ExitOnRootFailure(hr = E_INVALIDDATA, "UX aborted detect update begin.");
            }
            break;
        }
    }

LExit:
    if (sczUpdateFeedTempFile && *sczUpdateFeedTempFile)
    {
        FileEnsureDelete(sczUpdateFeedTempFile);
    }

    ApupFreeChain(pApupChain);
    AtomFreeFeed(pAtomFeed);
    ReleaseStr(sczUpdateFeedTempFile);
    AtomUninitialize();

    return hr;
}
