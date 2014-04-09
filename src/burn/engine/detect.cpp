//-------------------------------------------------------------------------------------------------
// <copyright file="detect.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// internal function definitions


// function definitions

extern "C" void DetectReset(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PACKAGES* pPackages,
    __in BURN_UPDATE* /*pUpdate*/
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

// TODO: this function is an outline for what the future detection of updates by the
//       engine could look like.
extern "C" HRESULT DetectUpdate(
    __in_z LPCWSTR /*wzBundleId*/,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_UPDATE* pUpdate
    )
{
    HRESULT hr = S_OK;
    int nResult = IDNOACTION;
    BOOL fBeginCalled = FALSE;
    LPWSTR sczUpdateId = NULL;

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
        ExitFunction1(hr = E_NOTIMPL);

        // TODO: actually check that a newer version is at the update source. For now we'll just
        //       pretend that if a source is provided that an update is available.
        //pUpdate->fUpdateAvailable = (pUpdate->sczUpdateSource && *pUpdate->sczUpdateSource);

        //hr = StrAllocFormatted(&sczUpdateId, L"%ls.update", wzBundleId);
        //ExitOnFailure(hr, "Failed to allocate update id.");

        //// Update bundle is always considered per-user since we do not have a secure way to inform the elevated engine
        //// about this detected bundle's data.
        //hr = PseudoBundleInitialize(FILEMAKEVERSION(rmj, rmm, rup, 0), &pUpdate->package, FALSE, sczUpdateId, BOOTSTRAPPER_RELATION_UPDATE, BOOTSTRAPPER_PACKAGE_STATE_ABSENT, L"update.exe", pUpdate->sczUpdateSource, pUpdate->qwSize, TRUE, L"-quiet", NULL, NULL, NULL, pUpdate->pbHash, pUpdate->cbHash);
        //ExitOnFailure(hr, "Failed to initialize update bundle.");
    }

LExit:
    if (fBeginCalled)
    {
        pUX->pUserExperience->OnDetectUpdateComplete(hr, /*pUpdate->fUpdateAvailable ? pUpdate->sczUpdateSource : */NULL);
    }

    ReleaseStr(sczUpdateId);
    return hr;
}
