// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// constants


// structs



// internal function declarations

static HRESULT ParseRelatedMsiFromXml(
    __in IXMLDOMNode* pixnRelatedMsi,
    __in BURN_RELATED_MSI* pRelatedMsi
    );
static HRESULT EvaluateActionStateConditions(
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR sczAddLocalCondition,
    __in_z_opt LPCWSTR sczAddSourceCondition,
    __in_z_opt LPCWSTR sczAdvertiseCondition,
    __out BOOTSTRAPPER_FEATURE_STATE* pState
    );
static HRESULT CalculateFeatureAction(
    __in BOOTSTRAPPER_FEATURE_STATE currentState,
    __in BOOTSTRAPPER_FEATURE_STATE requestedState,
    __in BOOL fRepair,
    __out BOOTSTRAPPER_FEATURE_ACTION* pFeatureAction,
    __inout BOOL* pfDelta
    );
static HRESULT EscapePropertyArgumentString(
    __in LPCWSTR wzProperty,
    __inout_z LPWSTR* psczEscapedValue,
    __in BOOL fZeroOnRealloc
    );
static HRESULT ConcatFeatureActionProperties(
    __in BURN_PACKAGE* pPackage,
    __in BOOTSTRAPPER_FEATURE_ACTION* rgFeatureActions,
    __inout_z LPWSTR* psczArguments
    );
static HRESULT ConcatPatchProperty(
    __in BURN_PACKAGE* pPackage,
    __in_opt BOOTSTRAPPER_ACTION_STATE* rgSlipstreamPatchActions,
    __inout_z LPWSTR* psczArguments
    );
static void RegisterSourceDirectory(
    __in BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzCacheDirectory
    );


// function definitions

extern "C" HRESULT MsiEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnMsiPackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // @ProductCode
    hr = XmlGetAttributeEx(pixnMsiPackage, L"ProductCode", &pPackage->Msi.sczProductCode);
    ExitOnFailure(hr, "Failed to get @ProductCode.");

    // @Language
    hr = XmlGetAttributeNumber(pixnMsiPackage, L"Language", &pPackage->Msi.dwLanguage);
    ExitOnFailure(hr, "Failed to get @Language.");

    // @Version
    hr = XmlGetAttributeEx(pixnMsiPackage, L"Version", &scz);
    ExitOnFailure(hr, "Failed to get @Version.");

    hr = FileVersionFromStringEx(scz, 0, &pPackage->Msi.qwVersion);
    ExitOnFailure1(hr, "Failed to parse @Version: %ls", scz);

    // @DisplayInternalUI
    hr = XmlGetYesNoAttribute(pixnMsiPackage, L"DisplayInternalUI", &pPackage->Msi.fDisplayInternalUI);
    ExitOnFailure(hr, "Failed to get @DisplayInternalUI.");

    // select feature nodes
    hr = XmlSelectNodes(pixnMsiPackage, L"MsiFeature", &pixnNodes);
    ExitOnFailure(hr, "Failed to select feature nodes.");

    // get feature node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get feature node count.");

    if (cNodes)
    {
        // allocate memory for features
        pPackage->Msi.rgFeatures = (BURN_MSIFEATURE*)MemAlloc(sizeof(BURN_MSIFEATURE) * cNodes, TRUE);
        ExitOnNull(pPackage->Msi.rgFeatures, hr, E_OUTOFMEMORY, "Failed to allocate memory for MSI feature structs.");

        pPackage->Msi.cFeatures = cNodes;

        // parse feature elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Id
            hr = XmlGetAttributeEx(pixnNode, L"Id", &pFeature->sczId);
            ExitOnFailure(hr, "Failed to get @Id.");

            // @AddLocalCondition
            hr = XmlGetAttributeEx(pixnNode, L"AddLocalCondition", &pFeature->sczAddLocalCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @AddLocalCondition.");
            }

            // @AddSourceCondition
            hr = XmlGetAttributeEx(pixnNode, L"AddSourceCondition", &pFeature->sczAddSourceCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @AddSourceCondition.");
            }

            // @AdvertiseCondition
            hr = XmlGetAttributeEx(pixnNode, L"AdvertiseCondition", &pFeature->sczAdvertiseCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @AdvertiseCondition.");
            }

            // @RollbackAddLocalCondition
            hr = XmlGetAttributeEx(pixnNode, L"RollbackAddLocalCondition", &pFeature->sczRollbackAddLocalCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @RollbackAddLocalCondition.");
            }

            // @RollbackAddSourceCondition
            hr = XmlGetAttributeEx(pixnNode, L"RollbackAddSourceCondition", &pFeature->sczRollbackAddSourceCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @RollbackAddSourceCondition.");
            }

            // @RollbackAdvertiseCondition
            hr = XmlGetAttributeEx(pixnNode, L"RollbackAdvertiseCondition", &pFeature->sczRollbackAdvertiseCondition);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @RollbackAdvertiseCondition.");
            }

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    ReleaseNullObject(pixnNodes); // done with the MsiFeature elements.

    hr = MsiEngineParsePropertiesFromXml(pixnMsiPackage, &pPackage->Msi.rgProperties, &pPackage->Msi.cProperties);
    ExitOnFailure(hr, "Failed to parse properties from XML.");

    // select related MSI nodes
    hr = XmlSelectNodes(pixnMsiPackage, L"RelatedPackage", &pixnNodes);
    ExitOnFailure(hr, "Failed to select related MSI nodes.");

    // get related MSI node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get related MSI node count.");

    if (cNodes)
    {
        // allocate memory for related MSIs
        pPackage->Msi.rgRelatedMsis = (BURN_RELATED_MSI*)MemAlloc(sizeof(BURN_RELATED_MSI) * cNodes, TRUE);
        ExitOnNull(pPackage->Msi.rgRelatedMsis, hr, E_OUTOFMEMORY, "Failed to allocate memory for related MSI structs.");

        pPackage->Msi.cRelatedMsis = cNodes;

        // parse related MSI elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // parse related MSI element
            hr = ParseRelatedMsiFromXml(pixnNode, &pPackage->Msi.rgRelatedMsis[i]);
            ExitOnFailure(hr, "Failed to parse related MSI element.");

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    ReleaseNullObject(pixnNodes); // done with the RelatedPackage elements.

    // Select slipstream MSP nodes.
    hr = XmlSelectNodes(pixnMsiPackage, L"SlipstreamMsp", &pixnNodes);
    ExitOnFailure(hr, "Failed to select related MSI nodes.");

    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get related MSI node count.");

    if (cNodes)
    {
        pPackage->Msi.rgpSlipstreamMspPackages = reinterpret_cast<BURN_PACKAGE**>(MemAlloc(sizeof(BURN_PACKAGE*) * cNodes, TRUE));
        ExitOnNull(pPackage->Msi.rgpSlipstreamMspPackages, hr, E_OUTOFMEMORY, "Failed to allocate memory for slipstream MSP packages.");

        pPackage->Msi.rgsczSlipstreamMspPackageIds = reinterpret_cast<LPWSTR*>(MemAlloc(sizeof(LPWSTR*) * cNodes, TRUE));
        ExitOnNull(pPackage->Msi.rgsczSlipstreamMspPackageIds, hr, E_OUTOFMEMORY, "Failed to allocate memory for slipstream MSP ids.");

        pPackage->Msi.cSlipstreamMspPackages = cNodes;

        // Parse slipstream MSP Ids.
        for (DWORD i = 0; i < cNodes; ++i)
        {
            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next slipstream MSP node.");

            hr = XmlGetAttributeEx(pixnNode, L"Id", pPackage->Msi.rgsczSlipstreamMspPackageIds + i);
            ExitOnFailure(hr, "Failed to parse slipstream MSP ids.");

            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" HRESULT MsiEngineParsePropertiesFromXml(
    __in IXMLDOMNode* pixnPackage,
    __out BURN_MSIPROPERTY** prgProperties,
    __out DWORD* pcProperties
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;

    BURN_MSIPROPERTY* pProperties = NULL;

    // select property nodes
    hr = XmlSelectNodes(pixnPackage, L"MsiProperty", &pixnNodes);
    ExitOnFailure(hr, "Failed to select property nodes.");

    // get property node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get property node count.");

    if (cNodes)
    {
        // allocate memory for properties
        pProperties = (BURN_MSIPROPERTY*)MemAlloc(sizeof(BURN_MSIPROPERTY) * cNodes, TRUE);
        ExitOnNull(pProperties, hr, E_OUTOFMEMORY, "Failed to allocate memory for MSI property structs.");

        // parse property elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_MSIPROPERTY* pProperty = &pProperties[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Id
            hr = XmlGetAttributeEx(pixnNode, L"Id", &pProperty->sczId);
            ExitOnFailure(hr, "Failed to get @Id.");

            // @Value
            hr = XmlGetAttributeEx(pixnNode, L"Value", &pProperty->sczValue);
            ExitOnFailure(hr, "Failed to get @Value.");

            // @RollbackValue
            hr = XmlGetAttributeEx(pixnNode, L"RollbackValue", &pProperty->sczRollbackValue);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to get @RollbackValue.");
            }

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    *pcProperties = cNodes;
    *prgProperties = pProperties;
    pProperties = NULL;

    hr = S_OK;

LExit:
    ReleaseNullObject(pixnNodes);
    ReleaseMem(pProperties);

    return hr;
}

extern "C" void MsiEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->Msi.sczProductCode);
    ReleaseStr(pPackage->Msi.sczInstalledProductCode);

    // free features
    if (pPackage->Msi.rgFeatures)
    {
        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            ReleaseStr(pFeature->sczId);
            ReleaseStr(pFeature->sczAddLocalCondition);
            ReleaseStr(pFeature->sczAddSourceCondition);
            ReleaseStr(pFeature->sczAdvertiseCondition);
            ReleaseStr(pFeature->sczRollbackAddLocalCondition);
            ReleaseStr(pFeature->sczRollbackAddSourceCondition);
            ReleaseStr(pFeature->sczRollbackAdvertiseCondition);
        }
        MemFree(pPackage->Msi.rgFeatures);
    }

    // free properties
    if (pPackage->Msi.rgProperties)
    {
        for (DWORD i = 0; i < pPackage->Msi.cProperties; ++i)
        {
            BURN_MSIPROPERTY* pProperty = &pPackage->Msi.rgProperties[i];

            ReleaseStr(pProperty->sczId);
            ReleaseStr(pProperty->sczValue);
            ReleaseStr(pProperty->sczRollbackValue);
        }
        MemFree(pPackage->Msi.rgProperties);
    }

    // free related MSIs
    if (pPackage->Msi.rgRelatedMsis)
    {
        for (DWORD i = 0; i < pPackage->Msi.cRelatedMsis; ++i)
        {
            BURN_RELATED_MSI* pRelatedMsi = &pPackage->Msi.rgRelatedMsis[i];

            ReleaseStr(pRelatedMsi->sczUpgradeCode);
            ReleaseMem(pRelatedMsi->rgdwLanguages);
        }
        MemFree(pPackage->Msi.rgRelatedMsis);
    }

    // free slipstream MSPs
    if (pPackage->Msi.rgsczSlipstreamMspPackageIds)
    {
        for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
        {
            ReleaseStr(pPackage->Msi.rgsczSlipstreamMspPackageIds[i]);
        }

        MemFree(pPackage->Msi.rgsczSlipstreamMspPackageIds);
    }

    if (pPackage->Msi.rgpSlipstreamMspPackages)
    {
        MemFree(pPackage->Msi.rgpSlipstreamMspPackages);
    }

    // clear struct
    memset(&pPackage->Msi, 0, sizeof(pPackage->Msi));
}

extern "C" HRESULT MsiEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    Trace1(REPORT_STANDARD, "Detecting MSI package 0x%p", pPackage);

    HRESULT hr = S_OK;
    LPWSTR sczInstalledVersion = NULL;
    LPWSTR sczInstalledLanguage = NULL;
    LPWSTR sczInstalledProductCode = NULL;
    LPWSTR sczInstalledProviderKey = NULL;
    INSTALLSTATE installState = INSTALLSTATE_UNKNOWN;
    BOOTSTRAPPER_RELATED_OPERATION operation = BOOTSTRAPPER_RELATED_OPERATION_NONE;
    BOOTSTRAPPER_RELATED_OPERATION relatedMsiOperation = BOOTSTRAPPER_RELATED_OPERATION_NONE;
    WCHAR wzProductCode[MAX_GUID_CHARS + 1] = { };
    DWORD64 qwVersion = 0;
    UINT uLcid = 0;
    BOOL fPerMachine = FALSE;
    int nResult = 0;

    // detect self by product code
    // TODO: what to do about MSIINSTALLCONTEXT_USERMANAGED?
    hr = WiuGetProductInfoEx(pPackage->Msi.sczProductCode, NULL, pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
    if (SUCCEEDED(hr))
    {
        hr = FileVersionFromStringEx(sczInstalledVersion, 0, &pPackage->Msi.qwInstalledVersion);
        ExitOnFailure2(hr, "Failed to convert version: %ls to DWORD64 for ProductCode: %ls", sczInstalledVersion, pPackage->Msi.sczProductCode);

        // compare versions
        if (pPackage->Msi.qwVersion < pPackage->Msi.qwInstalledVersion)
        {
            operation = BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE;
            pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED;
        }
        else
        {
            if (pPackage->Msi.qwVersion > pPackage->Msi.qwInstalledVersion)
            {
                operation = BOOTSTRAPPER_RELATED_OPERATION_MINOR_UPDATE;
            }

            pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
        }

        // report related MSI package to UX
        if (BOOTSTRAPPER_RELATED_OPERATION_NONE != operation)
        {
            LogId(REPORT_STANDARD, MSG_DETECTED_RELATED_PACKAGE, pPackage->Msi.sczProductCode, LoggingPerMachineToString(pPackage->fPerMachine), LoggingVersionToString(pPackage->Msi.qwInstalledVersion), pPackage->Msi.dwLanguage, LoggingRelatedOperationToString(operation));

            nResult = pUserExperience->pUserExperience->OnDetectRelatedMsiPackage(pPackage->sczId, pPackage->Msi.sczProductCode, pPackage->fPerMachine, pPackage->Msi.qwInstalledVersion, operation);
            hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
            ExitOnRootFailure(hr, "UX aborted detect related MSI package.");
        }
    }
    else if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr || HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) == hr) // package not present.
    {
        // Check for newer, compatible packages based on a fixed provider key.
        hr = DependencyDetectProviderKeyPackageId(pPackage, &sczInstalledProviderKey, &sczInstalledProductCode);
        if (SUCCEEDED(hr))
        {
            hr = WiuGetProductInfoEx(sczInstalledProductCode, NULL, pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
            if (SUCCEEDED(hr))
            {
                hr = FileVersionFromStringEx(sczInstalledVersion, 0, &qwVersion);
                ExitOnFailure2(hr, "Failed to convert version: %ls to DWORD64 for ProductCode: %ls", sczInstalledVersion, sczInstalledProductCode);

                if (pPackage->Msi.qwVersion < qwVersion)
                {
                    LogId(REPORT_STANDARD, MSG_DETECTED_COMPATIBLE_PACKAGE_FROM_PROVIDER, pPackage->sczId, sczInstalledProviderKey, sczInstalledProductCode, sczInstalledVersion, pPackage->Msi.sczProductCode);

                    nResult = pUserExperience->pUserExperience->OnDetectCompatiblePackage(pPackage->sczId, sczInstalledProductCode);
                    hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
                    ExitOnRootFailure(hr, "UX aborted detect compatible MSI package.");

                    hr = StrAllocString(&pPackage->Msi.sczInstalledProductCode, sczInstalledProductCode, 0);
                    ExitOnFailure(hr, "Failed to copy the installed ProductCode to the package.");

                    pPackage->Msi.qwInstalledVersion = qwVersion;
                    pPackage->Msi.fCompatibleInstalled = TRUE;
                }
            }
        }

        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
        hr = S_OK;
    }
    else
    {
        ExitOnFailure1(hr, "Failed to get product information for ProductCode: %ls", pPackage->Msi.sczProductCode);
    }

    // detect related packages by upgrade code
    for (DWORD i = 0; i < pPackage->Msi.cRelatedMsis; ++i)
    {
        BURN_RELATED_MSI* pRelatedMsi = &pPackage->Msi.rgRelatedMsis[i];

        for (DWORD iProduct = 0; ; ++iProduct)
        {
            // get product
            hr = WiuEnumRelatedProducts(pRelatedMsi->sczUpgradeCode, iProduct, wzProductCode);
            if (E_NOMOREITEMS == hr)
            {
                hr = S_OK;
                break;
            }
            ExitOnFailure(hr, "Failed to enum related products.");

            // If we found ourselves, skip because saying that a package is related to itself is nonsensical.
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pPackage->Msi.sczProductCode, -1, wzProductCode, -1))
            {
                continue;
            }

            // get product version
            hr = WiuGetProductInfoEx(wzProductCode, NULL, MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
            if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) != hr && HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) != hr)
            {
                ExitOnFailure1(hr, "Failed to get version for product in user unmanaged context: %ls", wzProductCode);
                fPerMachine = FALSE;
            }
            else
            {
                hr = WiuGetProductInfoEx(wzProductCode, NULL, MSIINSTALLCONTEXT_MACHINE, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
                if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) != hr && HRESULT_FROM_WIN32(ERROR_UNKNOWN_PROPERTY) != hr)
                {
                    ExitOnFailure1(hr, "Failed to get version for product in machine context: %ls", wzProductCode);
                    fPerMachine = TRUE;
                }
                else
                {
                    hr = S_OK;
                    continue;
                }
            }

            hr = FileVersionFromStringEx(sczInstalledVersion, 0, &qwVersion);
            ExitOnFailure2(hr, "Failed to convert version: %ls to DWORD64 for ProductCode: %ls", sczInstalledVersion, wzProductCode);

            // compare versions
            if (pRelatedMsi->fMinProvided && (pRelatedMsi->fMinInclusive ? (qwVersion < pRelatedMsi->qwMinVersion) : (qwVersion <= pRelatedMsi->qwMinVersion)))
            {
                continue;
            }

            if (pRelatedMsi->fMaxProvided && (pRelatedMsi->fMaxInclusive ? (qwVersion > pRelatedMsi->qwMaxVersion) : (qwVersion >= pRelatedMsi->qwMaxVersion)))
            {
                continue;
            }

            // Filter by language if necessary.
            uLcid = 0; // always reset the found language.
            if (pRelatedMsi->cLanguages)
            {
                // If there is a language to get, convert it into an LCID.
                hr = WiuGetProductInfoEx(wzProductCode, NULL, fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_LANGUAGE, &sczInstalledLanguage);
                if (SUCCEEDED(hr))
                {
                    hr = StrStringToUInt32(sczInstalledLanguage, 0, &uLcid);
                }

                // Ignore related product where we can't read the language.
                if (FAILED(hr))
                {
                    LogErrorId(hr, MSG_FAILED_READ_RELATED_PACKAGE_LANGUAGE, wzProductCode, sczInstalledLanguage, NULL);

                    hr = S_OK;
                    continue;
                }

                BOOL fMatchedLcid = FALSE;
                for (DWORD iLanguage = 0; iLanguage < pRelatedMsi->cLanguages; ++iLanguage)
                {
                    if (uLcid == pRelatedMsi->rgdwLanguages[iLanguage])
                    {
                        fMatchedLcid = TRUE;
                        break;
                    }
                }

                // Skip the product if the language did not meet the inclusive/exclusive criteria.
                if ((pRelatedMsi->fLangInclusive && !fMatchedLcid) || (!pRelatedMsi->fLangInclusive && fMatchedLcid))
                {
                    continue;
                }
            }

            // If this is a detect-only related package and we're not installed yet, then we'll assume a downgrade
            // would take place since that is the overwhelmingly common use of detect-only related packages. If
            // not detect-only then it's easy; we're clearly doing a major upgrade.
            if (pRelatedMsi->fOnlyDetect)
            {
                // If we've already detected a major upgrade that trumps any guesses that the detect is a downgrade
                // or even something else.
                if (BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE == operation)
                {
                    relatedMsiOperation = BOOTSTRAPPER_RELATED_OPERATION_NONE;
                }
                else if (BOOTSTRAPPER_PACKAGE_STATE_ABSENT == pPackage->currentState)
                {
                    relatedMsiOperation = BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE;
                    operation = BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE;
                    pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE;
                }
                else // we're already on the machine so the detect-only *must* be for detection purposes only.
                {
                    relatedMsiOperation = BOOTSTRAPPER_RELATED_OPERATION_NONE;
                }
            }
            else
            {
                relatedMsiOperation = BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE;
                operation = BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE;
            }

            LogId(REPORT_STANDARD, MSG_DETECTED_RELATED_PACKAGE, wzProductCode, LoggingPerMachineToString(fPerMachine), LoggingVersionToString(qwVersion), uLcid, LoggingRelatedOperationToString(relatedMsiOperation));

            // pass to UX
            nResult = pUserExperience->pUserExperience->OnDetectRelatedMsiPackage(pPackage->sczId, wzProductCode, fPerMachine, qwVersion, relatedMsiOperation);
            hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
            ExitOnRootFailure(hr, "UX aborted detect related MSI package.");
        }
    }

    // detect features
    if (pPackage->Msi.cFeatures)
    {
        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            // Try to detect features state if the product is present on the machine.
            if (BOOTSTRAPPER_PACKAGE_STATE_PRESENT <= pPackage->currentState)
            {
                hr = WiuQueryFeatureState(pPackage->Msi.sczProductCode, pFeature->sczId, &installState);
                ExitOnFailure(hr, "Failed to query feature state.");

                if (INSTALLSTATE_UNKNOWN == installState) // in case of an upgrade a feature could be removed.
                {
                    installState = INSTALLSTATE_ABSENT;
                }
            }
            else // MSI not installed then the features can't be either.
            {
                installState = INSTALLSTATE_ABSENT;
            }

            // set current state
            switch (installState)
            {
            case INSTALLSTATE_ABSENT:
                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_ABSENT;
                break;
            case INSTALLSTATE_ADVERTISED:
                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_ADVERTISED;
                break;
            case INSTALLSTATE_LOCAL:
                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_LOCAL;
                break;
            case INSTALLSTATE_SOURCE:
                pFeature->currentState = BOOTSTRAPPER_FEATURE_STATE_SOURCE;
                break;
            default:
                hr = E_UNEXPECTED;
                ExitOnRootFailure(hr, "Invalid state value.");
            }

            // pass to UX
            nResult = pUserExperience->pUserExperience->OnDetectMsiFeature(pPackage->sczId, pFeature->sczId, pFeature->currentState);
            hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
            ExitOnRootFailure(hr, "UX aborted detect.");
        }
    }

LExit:
    ReleaseStr(sczInstalledProviderKey);
    ReleaseStr(sczInstalledProductCode);
    ReleaseStr(sczInstalledLanguage);
    ReleaseStr(sczInstalledVersion);

    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT MsiEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __out BOOL* pfBARequestedCache
    )
{
    Trace1(REPORT_STANDARD, "Planning MSI package 0x%p", pPackage);

    HRESULT hr = S_OK;
    DWORD64 qwVersion = pPackage->Msi.qwVersion;
    DWORD64 qwInstalledVersion = pPackage->Msi.qwInstalledVersion;
    BOOTSTRAPPER_ACTION_STATE execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOTSTRAPPER_ACTION_STATE rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOL fFeatureActionDelta = FALSE;
    BOOL fRollbackFeatureActionDelta = FALSE;
    int nResult = 0;
    BOOL fBARequestedCache = FALSE;

    if (pPackage->Msi.cFeatures)
    {
        // If the package is present and we're repairing it.
        BOOL fRepairingPackage = (BOOTSTRAPPER_PACKAGE_STATE_CACHED < pPackage->currentState && BOOTSTRAPPER_REQUEST_STATE_REPAIR == pPackage->requested);

        LogId(REPORT_STANDARD, MSG_PLAN_MSI_FEATURES, pPackage->Msi.cFeatures, pPackage->sczId);

        // plan features
        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];
            BOOTSTRAPPER_FEATURE_STATE defaultFeatureRequestedState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            BOOTSTRAPPER_FEATURE_STATE featureRequestedState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            BOOTSTRAPPER_FEATURE_STATE featureExpectedState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;

            // evaluate feature conditions
            hr = EvaluateActionStateConditions(pVariables, pFeature->sczAddLocalCondition, pFeature->sczAddSourceCondition, pFeature->sczAdvertiseCondition, &defaultFeatureRequestedState);
            ExitOnFailure(hr, "Failed to evaluate requested state conditions.");

            hr = EvaluateActionStateConditions(pVariables, pFeature->sczRollbackAddLocalCondition, pFeature->sczRollbackAddSourceCondition, pFeature->sczRollbackAdvertiseCondition, &featureExpectedState);
            ExitOnFailure(hr, "Failed to evaluate expected state conditions.");

            // Remember the default feature requested state so the engine doesn't get blamed for planning the wrong thing if the UX changes it.
            featureRequestedState = defaultFeatureRequestedState;

            // send MSI feature plan message to UX
            nResult = pUserExperience->pUserExperience->OnPlanMsiFeature(pPackage->sczId, pFeature->sczId, &featureRequestedState);
            hr = UserExperienceInterpretResult(pUserExperience, MB_OKCANCEL, nResult);
            ExitOnRootFailure(hr, "UX aborted plan MSI feature.");

            // calculate feature actions
            hr = CalculateFeatureAction(pFeature->currentState, featureRequestedState, fRepairingPackage, &pFeature->execute, &fFeatureActionDelta);
            ExitOnFailure(hr, "Failed to calculate execute feature state.");

            hr = CalculateFeatureAction(featureRequestedState, BOOTSTRAPPER_FEATURE_ACTION_NONE == pFeature->execute ? featureExpectedState : pFeature->currentState, FALSE, &pFeature->rollback, &fRollbackFeatureActionDelta);
            ExitOnFailure(hr, "Failed to calculate rollback feature state.");

            LogId(REPORT_STANDARD, MSG_PLANNED_MSI_FEATURE, pFeature->sczId, LoggingMsiFeatureStateToString(pFeature->currentState), LoggingMsiFeatureStateToString(defaultFeatureRequestedState), LoggingMsiFeatureStateToString(featureRequestedState), LoggingMsiFeatureActionToString(pFeature->execute), LoggingMsiFeatureActionToString(pFeature->rollback));
        }
    }

    // execute action
    switch (pPackage->currentState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT: __fallthrough;
    case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
        if (BOOTSTRAPPER_REQUEST_STATE_PRESENT == pPackage->requested || BOOTSTRAPPER_REQUEST_STATE_REPAIR == pPackage->requested)
        {
            // Take a look at the version and determine if this is a potential
            // minor upgrade (same ProductCode newer ProductVersion), otherwise,
            // there is a newer version so no work necessary.
            if (qwVersion > qwInstalledVersion)
            {
                execute = BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE;
            }
            else if (BOOTSTRAPPER_REQUEST_STATE_REPAIR == pPackage->requested)
            {
                execute = BOOTSTRAPPER_ACTION_STATE_REPAIR;
            }
            else
            {
                execute = fFeatureActionDelta ? BOOTSTRAPPER_ACTION_STATE_MODIFY : BOOTSTRAPPER_ACTION_STATE_NONE;
            }
        }
        else if ((BOOTSTRAPPER_REQUEST_STATE_ABSENT == pPackage->requested || BOOTSTRAPPER_REQUEST_STATE_CACHE == pPackage->requested) &&
                 pPackage->fUninstallable) // removing a package that can be removed.
        {
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
        }
        else if (BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT == pPackage->requested)
        {
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
        }
        else
        {
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
        }
        break;

    case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;

        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    case BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE: __fallthrough;
    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;

        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            fBARequestedCache = TRUE;
            break;

        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure1(hr, "Invalid package current state result encountered during plan: %d", pPackage->currentState);
    }

    // Calculate the rollback action if there is an execute action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != execute)
    {
        switch (BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN != pPackage->expected ? pPackage->expected : pPackage->currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT:
                rollback = fRollbackFeatureActionDelta ? BOOTSTRAPPER_ACTION_STATE_MODIFY : BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                rollback = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                break;
            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE: __fallthrough;
        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT: __fallthrough;
        case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
            // If we requested to put the package on the machine then remove the package during rollback
            // if the package is uninstallable.
            if ((BOOTSTRAPPER_REQUEST_STATE_PRESENT == pPackage->requested || BOOTSTRAPPER_REQUEST_STATE_REPAIR == pPackage->requested) &&
                pPackage->fUninstallable)
            {
                rollback = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
            }
            else
            {
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
            }
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnRootFailure(hr, "Invalid package detection result encountered.");
        }
    }

    // return values
    pPackage->execute = execute;
    pPackage->rollback = rollback;

    if (pfBARequestedCache)
    {
        *pfBARequestedCache = fBARequestedCache;
    }

LExit:
    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the package.
//
extern "C" HRESULT MsiEnginePlanAddPackage(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;
    BOOTSTRAPPER_FEATURE_ACTION* rgFeatureActions = NULL;
    BOOTSTRAPPER_FEATURE_ACTION* rgRollbackFeatureActions = NULL;

    if (pPackage->Msi.cFeatures)
    {
        // Allocate and populate array for feature actions.
        rgFeatureActions = (BOOTSTRAPPER_FEATURE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_FEATURE_ACTION) * pPackage->Msi.cFeatures, TRUE);
        ExitOnNull(rgFeatureActions, hr, E_OUTOFMEMORY, "Failed to allocate memory for feature actions.");

        rgRollbackFeatureActions = (BOOTSTRAPPER_FEATURE_ACTION*)MemAlloc(sizeof(BOOTSTRAPPER_FEATURE_ACTION) * pPackage->Msi.cFeatures, TRUE);
        ExitOnNull(rgRollbackFeatureActions, hr, E_OUTOFMEMORY, "Failed to allocate memory for rollback feature actions.");

        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            // calculate feature actions
            rgFeatureActions[i] = pFeature->execute;
            rgRollbackFeatureActions[i] = pFeature->rollback;
        }
    }

    // add wait for cache
    if (hCacheEvent)
    {
        hr = PlanExecuteCacheSyncAndRollback(pPlan, pPackage, hCacheEvent, fPlanPackageCacheRollback);
        ExitOnFailure(hr, "Failed to plan package cache syncpoint");
    }

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    // add rollback action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE;
        pAction->msiPackage.pPackage = pPackage;
        pAction->msiPackage.action = pPackage->rollback;
        pAction->msiPackage.uiLevel = MsiEngineCalculateInstallUiLevel(pPackage->Msi.fDisplayInternalUI, display, pAction->msiPackage.action);
        pAction->msiPackage.rgFeatures = rgRollbackFeatureActions;
        rgRollbackFeatureActions = NULL;

        LoggingSetPackageVariable(pPackage, NULL, TRUE, pLog, pVariables, &pAction->msiPackage.sczLogPath); // ignore errors.
        pAction->msiPackage.dwLoggingAttributes = pLog->dwAttributes;

        // Plan a checkpoint between rollback and execute so that we always attempt
        // rollback in the case that the MSI was not able to rollback itself (e.g.
        // user pushes cancel after InstallFinalize).
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append execute checkpoint.");
    }

    // add execute action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute)
    {
        hr = PlanAppendExecuteAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append execute action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE;
        pAction->msiPackage.pPackage = pPackage;
        pAction->msiPackage.action = pPackage->execute;
        pAction->msiPackage.uiLevel = MsiEngineCalculateInstallUiLevel(pPackage->Msi.fDisplayInternalUI, display, pAction->msiPackage.action);
        pAction->msiPackage.rgFeatures = rgFeatureActions;
        rgFeatureActions = NULL;

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, &pAction->msiPackage.sczLogPath); // ignore errors.
        pAction->msiPackage.dwLoggingAttributes = pLog->dwAttributes;
    }

    // Update any slipstream patches' state.
    for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
    {
        BURN_PACKAGE* pMspPackage = pPackage->Msi.rgpSlipstreamMspPackages[i];
        AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Only MSP packages can be slipstream patches.");

        MspEngineSlipstreamUpdateState(pMspPackage, pPackage->execute, pPackage->rollback);
    }

LExit:
    ReleaseMem(rgFeatureActions);
    ReleaseMem(rgRollbackFeatureActions);

    return hr;
}

extern "C" HRESULT MsiEngineAddCompatiblePackage(
    __in BURN_PACKAGES* pPackages,
    __in const BURN_PACKAGE* pPackage,
    __out_opt BURN_PACKAGE** ppCompatiblePackage
    )
{
    Assert(BURN_PACKAGE_TYPE_MSI == pPackage->type);

    HRESULT hr = S_OK;
    BURN_PACKAGE* pCompatiblePackage = NULL;
    LPWSTR sczInstalledVersion = NULL;

    // Allocate enough memory all at once so pointers to packages within
    // aren't invalidated if we otherwise reallocated.
    hr = PackageEnsureCompatiblePackagesArray(pPackages);
    ExitOnFailure(hr, "Failed to allocate memory for compatible MSI package.");

    pCompatiblePackage = pPackages->rgCompatiblePackages + pPackages->cCompatiblePackages;
    ++pPackages->cCompatiblePackages;

    pCompatiblePackage->type = BURN_PACKAGE_TYPE_MSI;

    // Read in the compatible ProductCode if not already available.
    if (pPackage->Msi.sczInstalledProductCode)
    {
        hr = StrAllocString(&pCompatiblePackage->Msi.sczProductCode, pPackage->Msi.sczInstalledProductCode, 0);
        ExitOnFailure(hr, "Failed to copy installed ProductCode to compatible package.");
    }
    else
    {
        hr = DependencyDetectProviderKeyPackageId(pPackage, NULL, &pCompatiblePackage->Msi.sczProductCode);
        ExitOnFailure(hr, "Failed to detect compatible package from provider key.");
    }

    // Read in the compatible ProductVersion if not already available.
    if (pPackage->Msi.qwInstalledVersion)
    {
        pCompatiblePackage->Msi.qwVersion = pPackage->Msi.qwInstalledVersion;

        hr = FileVersionToStringEx(pCompatiblePackage->Msi.qwVersion, &sczInstalledVersion);
        ExitOnFailure(hr, "Failed to format version number string.");
    }
    else
    {
        hr = WiuGetProductInfoEx(pCompatiblePackage->Msi.sczProductCode, NULL, pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
        ExitOnFailure(hr, "Failed to read version from compatible package.");

        hr = FileVersionFromStringEx(sczInstalledVersion, 0, &pCompatiblePackage->Msi.qwVersion);
        ExitOnFailure2(hr, "Failed to convert version: %ls to DWORD64 for ProductCode: %ls", sczInstalledVersion, pCompatiblePackage->Msi.sczProductCode);
    }

    // For now, copy enough information to support uninstalling the newer, compatible package.
    hr = StrAllocString(&pCompatiblePackage->sczId, pCompatiblePackage->Msi.sczProductCode, 0);
    ExitOnFailure(hr, "Failed to copy installed ProductCode as compatible package ID.");

    pCompatiblePackage->fPerMachine = pPackage->fPerMachine;
    pCompatiblePackage->fUninstallable = pPackage->fUninstallable;
    pCompatiblePackage->cacheType = pPackage->cacheType;

    // Removing compatible packages is best effort.
    pCompatiblePackage->fVital = FALSE;

    // Format a suitable log path variable from the original package.
    hr = StrAllocFormatted(&pCompatiblePackage->sczLogPathVariable, L"%ls_Compatible", pPackage->sczLogPathVariable);
    ExitOnFailure(hr, "Failed to format log path variable for compatible package.");

    // Use the default cache ID generation from the binder.
    hr = StrAllocFormatted(&pCompatiblePackage->sczCacheId, L"%lsv%ls", pCompatiblePackage->sczId, sczInstalledVersion);
    ExitOnFailure(hr, "Failed to format cache ID for compatible package.");

    pCompatiblePackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
    pCompatiblePackage->cache = BURN_CACHE_STATE_PARTIAL; // Cannot know if it's complete or not.

    // Copy all the providers to ensure no dependents.
    if (pPackage->cDependencyProviders)
    {
        pCompatiblePackage->rgDependencyProviders = (BURN_DEPENDENCY_PROVIDER*)MemAlloc(sizeof(BURN_DEPENDENCY_PROVIDER) * pPackage->cDependencyProviders, TRUE);
        ExitOnNull(pCompatiblePackage->rgDependencyProviders, hr, E_OUTOFMEMORY, "Failed to allocate for compatible package providers.");

        for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
        {
            BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + i;
            BURN_DEPENDENCY_PROVIDER* pCompatibleProvider = pCompatiblePackage->rgDependencyProviders + i;

            // Only need to copy the key for uninstall.
            hr = StrAllocString(&pCompatibleProvider->sczKey, pProvider->sczKey, 0);
            ExitOnFailure(hr, "Failed to copy the compatible provider key.");

            // Assume the package version is the same as the provider version.
            hr = StrAllocString(&pCompatibleProvider->sczVersion, sczInstalledVersion, 0);
            ExitOnFailure(hr, "Failed to copy the compatible provider version.");

            // Assume provider keys are similarly authored for this package.
            pCompatibleProvider->fImported = pProvider->fImported;
        }

        pCompatiblePackage->cDependencyProviders = pPackage->cDependencyProviders;
    }

    pCompatiblePackage->type = BURN_PACKAGE_TYPE_MSI;
    pCompatiblePackage->Msi.fDisplayInternalUI = pPackage->Msi.fDisplayInternalUI;

    if (ppCompatiblePackage)
    {
        *ppCompatiblePackage = pCompatiblePackage;
    }

LExit:
    ReleaseStr(sczInstalledVersion);

    return hr;
}

extern "C" HRESULT MsiEngineExecutePackage(
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    WIU_MSI_EXECUTE_CONTEXT context = { };
    WIU_RESTART restart = WIU_RESTART_NONE;

    LPWSTR sczInstalledVersion = NULL;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczMsiPath = NULL;
    LPWSTR sczProperties = NULL;
    LPWSTR sczObfuscatedProperties = NULL;

    // During rollback, if the package is already in the rollback state we expect don't
    // touch it again.
    if (fRollback)
    {
        if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pExecuteAction->msiPackage.action)
        {
            hr = WiuGetProductInfoEx(pExecuteAction->msiPackage.pPackage->Msi.sczProductCode, NULL, pExecuteAction->msiPackage.pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
            if (FAILED(hr))  // package not present.
            {
                LogId(REPORT_STANDARD, MSG_ROLLBACK_PACKAGE_SKIPPED, pExecuteAction->msiPackage.pPackage->sczId, LoggingActionStateToString(pExecuteAction->msiPackage.action), LoggingPackageStateToString(BOOTSTRAPPER_PACKAGE_STATE_ABSENT));

                hr = S_OK;
                ExitFunction();
            }
        }
        else if (BOOTSTRAPPER_ACTION_STATE_INSTALL == pExecuteAction->msiPackage.action)
        {
            hr = WiuGetProductInfoEx(pExecuteAction->msiPackage.pPackage->Msi.sczProductCode, NULL, pExecuteAction->msiPackage.pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, INSTALLPROPERTY_VERSIONSTRING, &sczInstalledVersion);
            if (SUCCEEDED(hr))  // package present.
            {
                LogId(REPORT_STANDARD, MSG_ROLLBACK_PACKAGE_SKIPPED, pExecuteAction->msiPackage.pPackage->sczId, LoggingActionStateToString(pExecuteAction->msiPackage.action), LoggingPackageStateToString(BOOTSTRAPPER_PACKAGE_STATE_PRESENT));

                hr = S_OK;
                ExitFunction();
            }

            hr = S_OK;
        }
    }

    // Default to "verbose" logging and set extra debug mode only if explicitly required.
    DWORD dwLogMode = WIU_LOG_DEFAULT | INSTALLLOGMODE_VERBOSE;

    if (pExecuteAction->msiPackage.dwLoggingAttributes & BURN_LOGGING_ATTRIBUTE_EXTRADEBUG)
    {
        dwLogMode |= INSTALLLOGMODE_EXTRADEBUG;
    }

    if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL != pExecuteAction->msiPackage.action)
    {
        // get cached MSI path
        hr = CacheGetCompletedPath(pExecuteAction->msiPackage.pPackage->fPerMachine, pExecuteAction->msiPackage.pPackage->sczCacheId, &sczCachedDirectory);
        ExitOnFailure1(hr, "Failed to get cached path for package: %ls", pExecuteAction->msiPackage.pPackage->sczId);

        // Best effort to set the execute package cache folder variable.
        VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE);

        hr = PathConcat(sczCachedDirectory, pExecuteAction->msiPackage.pPackage->rgPayloads[0].pPayload->sczFilePath, &sczMsiPath);
        ExitOnFailure(hr, "Failed to build MSI path.");
    }

    // Best effort to set the execute package action variable.
    VariableSetNumeric(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, pExecuteAction->msiPackage.action, TRUE);
    
    // Wire up the external UI handler and logging.
    hr = WiuInitializeExternalUI(pfnMessageHandler, pExecuteAction->msiPackage.uiLevel, hwndParent, pvContext, fRollback, &context);
    ExitOnFailure(hr, "Failed to initialize external UI handler.");

    if (pExecuteAction->msiPackage.sczLogPath && *pExecuteAction->msiPackage.sczLogPath)
    {
        hr = WiuEnableLog(dwLogMode, pExecuteAction->msiPackage.sczLogPath, 0);
        ExitOnFailure2(hr, "Failed to enable logging for package: %ls to: %ls", pExecuteAction->msiPackage.pPackage->sczId, pExecuteAction->msiPackage.sczLogPath);
    }

    // set up properties
    hr = MsiEngineConcatProperties(pExecuteAction->msiPackage.pPackage->Msi.rgProperties, pExecuteAction->msiPackage.pPackage->Msi.cProperties, pVariables, fRollback, &sczProperties, FALSE);
    ExitOnFailure(hr, "Failed to add properties to argument string.");

    hr = MsiEngineConcatProperties(pExecuteAction->msiPackage.pPackage->Msi.rgProperties, pExecuteAction->msiPackage.pPackage->Msi.cProperties, pVariables, fRollback, &sczObfuscatedProperties, TRUE);
    ExitOnFailure(hr, "Failed to add obfuscated properties to argument string.");

    // add feature action properties
    hr = ConcatFeatureActionProperties(pExecuteAction->msiPackage.pPackage, pExecuteAction->msiPackage.rgFeatures, &sczProperties);
    ExitOnFailure(hr, "Failed to add feature action properties to argument string.");
    
    hr = ConcatFeatureActionProperties(pExecuteAction->msiPackage.pPackage, pExecuteAction->msiPackage.rgFeatures, &sczObfuscatedProperties);
    ExitOnFailure(hr, "Failed to add feature action properties to obfuscated argument string.");

    // add slipstream patch properties
    hr = ConcatPatchProperty(pExecuteAction->msiPackage.pPackage, pExecuteAction->msiPackage.rgSlipstreamPatches, &sczProperties);
    ExitOnFailure(hr, "Failed to add patch properties to argument string.");

    hr = ConcatPatchProperty(pExecuteAction->msiPackage.pPackage, pExecuteAction->msiPackage.rgSlipstreamPatches, &sczObfuscatedProperties);
    ExitOnFailure(hr, "Failed to add patch properties to obfuscated argument string.");

    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pExecuteAction->msiPackage.pPackage->sczId, LoggingActionStateToString(pExecuteAction->msiPackage.action), sczMsiPath, sczObfuscatedProperties ? sczObfuscatedProperties : L"");

    //
    // Do the actual action.
    //
    switch (pExecuteAction->msiPackage.action)
    {
    case BOOTSTRAPPER_ACTION_STATE_ADMIN_INSTALL:
        hr = StrAllocConcatSecure(&sczProperties, L" ACTION=ADMIN", 0);
        ExitOnFailure(hr, "Failed to add ADMIN property on admin install.");
         __fallthrough;

    case BOOTSTRAPPER_ACTION_STATE_MAJOR_UPGRADE: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        hr = StrAllocConcatSecure(&sczProperties, L" REBOOT=ReallySuppress", 0);
        ExitOnFailure(hr, "Failed to add reboot suppression property on install.");

        hr = WiuInstallProduct(sczMsiPath, sczProperties, &restart);
        ExitOnFailure(hr, "Failed to install MSI package.");

        RegisterSourceDirectory(pExecuteAction->msiPackage.pPackage, sczMsiPath);
        break;

    case BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE:
        // If feature selection is not enabled, then reinstall the existing features to ensure they get
        // updated.
        if (0 == pExecuteAction->msiPackage.pPackage->Msi.cFeatures)
        {
            hr = StrAllocConcatSecure(&sczProperties, L" REINSTALL=ALL", 0);
            ExitOnFailure(hr, "Failed to add reinstall all property on minor upgrade.");
        }

        hr = StrAllocConcatSecure(&sczProperties, L" REINSTALLMODE=\"vomus\" REBOOT=ReallySuppress", 0);
        ExitOnFailure(hr, "Failed to add reinstall mode and reboot suppression properties on minor upgrade.");

        hr = WiuInstallProduct(sczMsiPath, sczProperties, &restart);
        ExitOnFailure(hr, "Failed to perform minor upgrade of MSI package.");

        RegisterSourceDirectory(pExecuteAction->msiPackage.pPackage, sczMsiPath);
        break;

    case BOOTSTRAPPER_ACTION_STATE_MODIFY: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        {
        LPCWSTR wzReinstallAll = (BOOTSTRAPPER_ACTION_STATE_MODIFY == pExecuteAction->msiPackage.action ||
                                  pExecuteAction->msiPackage.pPackage->Msi.cFeatures) ? L"" : L" REINSTALL=ALL";
        LPCWSTR wzReinstallMode = (BOOTSTRAPPER_ACTION_STATE_MODIFY == pExecuteAction->msiPackage.action) ? L"o" : L"e";

        hr = StrAllocFormattedSecure(&sczProperties, L"%ls%ls REINSTALLMODE=\"cmus%ls\" REBOOT=ReallySuppress", sczProperties ? sczProperties : L"", wzReinstallAll, wzReinstallMode);
        ExitOnFailure(hr, "Failed to add reinstall mode and reboot suppression properties on repair.");
        }

        // Ignore all dependencies, since the Burn engine already performed the check.
        hr = StrAllocFormattedSecure(&sczProperties, L"%ls %ls=ALL", sczProperties, DEPENDENCY_IGNOREDEPENDENCIES);
        ExitOnFailure(hr, "Failed to add the list of dependencies to ignore to the properties.");

        hr = WiuInstallProduct(sczMsiPath, sczProperties, &restart);
        ExitOnFailure(hr, "Failed to run maintanance mode for MSI package.");
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        hr = StrAllocConcatSecure(&sczProperties, L" REBOOT=ReallySuppress", 0);
        ExitOnFailure(hr, "Failed to add reboot suppression property on uninstall.");

        // Ignore all dependencies, since the Burn engine already performed the check.
        hr = StrAllocFormattedSecure(&sczProperties, L"%ls %ls=ALL", sczProperties, DEPENDENCY_IGNOREDEPENDENCIES);
        ExitOnFailure(hr, "Failed to add the list of dependencies to ignore to the properties.");

        hr = WiuConfigureProductEx(pExecuteAction->msiPackage.pPackage->Msi.sczProductCode, INSTALLLEVEL_DEFAULT, INSTALLSTATE_ABSENT, sczProperties, &restart);
        if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
        {
            LogId(REPORT_STANDARD, MSG_ATTEMPTED_UNINSTALL_ABSENT_PACKAGE, pExecuteAction->msiPackage.pPackage->sczId);
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to uninstall MSI package.");
        break;
    }

LExit:
    WiuUninitializeExternalUI(&context);

    StrSecureZeroFreeString(sczProperties);
    ReleaseStr(sczObfuscatedProperties);
    ReleaseStr(sczMsiPath);
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczInstalledVersion);

    switch (restart)
    {
        case WIU_RESTART_NONE:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
            break;

        case WIU_RESTART_REQUIRED:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;
            break;

        case WIU_RESTART_INITIATED:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_INITIATED;
            break;
    }

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE);

    return hr;
}

// The contents of psczProperties may be sensitive, should keep encrypted and SecureZeroFree.
extern "C" HRESULT MsiEngineConcatProperties(
    __in_ecount(cProperties) BURN_MSIPROPERTY* rgProperties,
    __in DWORD cProperties,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __deref_out_z LPWSTR* psczProperties,
    __in BOOL fObfuscateHiddenVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    LPWSTR sczEscapedValue = NULL;
    LPWSTR sczProperty = NULL;

    for (DWORD i = 0; i < cProperties; ++i)
    {
        BURN_MSIPROPERTY* pProperty = &rgProperties[i];

        // format property value
        if (fObfuscateHiddenVariables)
        {
            hr = VariableFormatStringObfuscated(pVariables, (fRollback && pProperty->sczRollbackValue) ? pProperty->sczRollbackValue : pProperty->sczValue, &sczValue, NULL);
        }
        else
        {
            hr = VariableFormatString(pVariables, (fRollback && pProperty->sczRollbackValue) ? pProperty->sczRollbackValue : pProperty->sczValue, &sczValue, NULL);
            ExitOnFailure(hr, "Failed to format property value.");
        }
        ExitOnFailure(hr, "Failed to format property value.");

        // escape property value
        hr = EscapePropertyArgumentString(sczValue, &sczEscapedValue, !fObfuscateHiddenVariables);
        ExitOnFailure(hr, "Failed to escape string.");

        // build part
        hr = VariableStrAllocFormatted(!fObfuscateHiddenVariables, &sczProperty, L" %s%=\"%s\"", pProperty->sczId, sczEscapedValue);
        ExitOnFailure(hr, "Failed to format property string part.");

        // append to property string
        hr = VariableStrAllocConcat(!fObfuscateHiddenVariables, psczProperties, sczProperty, 0);
        ExitOnFailure(hr, "Failed to append property string part.");
    }

LExit:
    StrSecureZeroFreeString(sczValue);
    StrSecureZeroFreeString(sczEscapedValue);
    StrSecureZeroFreeString(sczProperty);
    return hr;
}

extern "C" INSTALLUILEVEL MsiEngineCalculateInstallUiLevel(
    __in BOOL fDisplayInternalUI,
    __in BOOTSTRAPPER_DISPLAY display,
    __in BOOTSTRAPPER_ACTION_STATE actionState
    )
{
    // Assume there will be no internal UI displayed.
    INSTALLUILEVEL uiLevel = static_cast<INSTALLUILEVEL>(INSTALLUILEVEL_NONE | INSTALLUILEVEL_SOURCERESONLY);

    // suppress internal UI during uninstall to mimic ARP and "msiexec /x" behavior
    if (fDisplayInternalUI && BOOTSTRAPPER_ACTION_STATE_UNINSTALL != actionState && BOOTSTRAPPER_ACTION_STATE_REPAIR != actionState)
    {
        switch (display)
        {
        case BOOTSTRAPPER_DISPLAY_FULL:
            uiLevel = INSTALLUILEVEL_FULL;
            break;

        case BOOTSTRAPPER_DISPLAY_PASSIVE:
            uiLevel = INSTALLUILEVEL_REDUCED;
            break;
        }
    }

    return uiLevel;
}


// internal helper functions

static HRESULT ParseRelatedMsiFromXml(
    __in IXMLDOMNode* pixnRelatedMsi,
    __in BURN_RELATED_MSI* pRelatedMsi
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // @Id
    hr = XmlGetAttributeEx(pixnRelatedMsi, L"Id", &pRelatedMsi->sczUpgradeCode);
    ExitOnFailure(hr, "Failed to get @Id.");

    // @MinVersion
    hr = XmlGetAttributeEx(pixnRelatedMsi, L"MinVersion", &scz);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get @MinVersion.");

        hr = FileVersionFromStringEx(scz, 0, &pRelatedMsi->qwMinVersion);
        ExitOnFailure1(hr, "Failed to parse @MinVersion: %ls", scz);

        // flag that we have a min version
        pRelatedMsi->fMinProvided = TRUE;

        // @MinInclusive
        hr = XmlGetYesNoAttribute(pixnRelatedMsi, L"MinInclusive", &pRelatedMsi->fMinInclusive);
        ExitOnFailure(hr, "Failed to get @MinInclusive.");
    }

    // @MaxVersion
    hr = XmlGetAttributeEx(pixnRelatedMsi, L"MaxVersion", &scz);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get @MaxVersion.");

        hr = FileVersionFromStringEx(scz, 0, &pRelatedMsi->qwMaxVersion);
        ExitOnFailure1(hr, "Failed to parse @MaxVersion: %ls", scz);

        // flag that we have a max version
        pRelatedMsi->fMaxProvided = TRUE;

        // @MaxInclusive
        hr = XmlGetYesNoAttribute(pixnRelatedMsi, L"MaxInclusive", &pRelatedMsi->fMaxInclusive);
        ExitOnFailure(hr, "Failed to get @MaxInclusive.");
    }

    // @OnlyDetect
    hr = XmlGetYesNoAttribute(pixnRelatedMsi, L"OnlyDetect", &pRelatedMsi->fOnlyDetect);
    ExitOnFailure(hr, "Failed to get @OnlyDetect.");

    // select language nodes
    hr = XmlSelectNodes(pixnRelatedMsi, L"Language", &pixnNodes);
    ExitOnFailure(hr, "Failed to select language nodes.");

    // get language node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get language node count.");

    if (cNodes)
    {
        // @LangInclusive
        hr = XmlGetYesNoAttribute(pixnRelatedMsi, L"LangInclusive", &pRelatedMsi->fLangInclusive);
        ExitOnFailure(hr, "Failed to get @LangInclusive.");

        // allocate memory for language IDs
        pRelatedMsi->rgdwLanguages = (DWORD*)MemAlloc(sizeof(DWORD) * cNodes, TRUE);
        ExitOnNull(pRelatedMsi->rgdwLanguages, hr, E_OUTOFMEMORY, "Failed to allocate memory for language IDs.");

        pRelatedMsi->cLanguages = cNodes;

        // parse language elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Id
            hr = XmlGetAttributeNumber(pixnNode, L"Id", &pRelatedMsi->rgdwLanguages[i]);
            ExitOnFailure(hr, "Failed to get Language/@Id.");

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

static HRESULT EvaluateActionStateConditions(
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR sczAddLocalCondition,
    __in_z_opt LPCWSTR sczAddSourceCondition,
    __in_z_opt LPCWSTR sczAdvertiseCondition,
    __out BOOTSTRAPPER_FEATURE_STATE* pState
    )
{
    HRESULT hr = S_OK;
    BOOL fCondition = FALSE;

    // if no condition was set, return no feature state
    if (!sczAddLocalCondition && !sczAddSourceCondition && !sczAdvertiseCondition)
    {
        *pState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
        ExitFunction();
    }

    if (sczAddLocalCondition)
    {
        hr = ConditionEvaluate(pVariables, sczAddLocalCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate add local condition.");

        if (fCondition)
        {
            *pState = BOOTSTRAPPER_FEATURE_STATE_LOCAL;
            ExitFunction();
        }
    }

    if (sczAddSourceCondition)
    {
        hr = ConditionEvaluate(pVariables, sczAddSourceCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate add source condition.");

        if (fCondition)
        {
            *pState = BOOTSTRAPPER_FEATURE_STATE_SOURCE;
            ExitFunction();
        }
    }

    if (sczAdvertiseCondition)
    {
        hr = ConditionEvaluate(pVariables, sczAdvertiseCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate advertise condition.");

        if (fCondition)
        {
            *pState = BOOTSTRAPPER_FEATURE_STATE_ADVERTISED;
            ExitFunction();
        }
    }

    // if no condition was true, set to absent
    *pState = BOOTSTRAPPER_FEATURE_STATE_ABSENT;

LExit:
    return hr;
}

static HRESULT CalculateFeatureAction(
    __in BOOTSTRAPPER_FEATURE_STATE currentState,
    __in BOOTSTRAPPER_FEATURE_STATE requestedState,
    __in BOOL fRepair,
    __out BOOTSTRAPPER_FEATURE_ACTION* pFeatureAction,
    __inout BOOL* pfDelta
    )
{
    HRESULT hr = S_OK;

    *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_NONE;
    switch (requestedState)
    {
    case BOOTSTRAPPER_FEATURE_STATE_UNKNOWN:
        *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_NONE;
        break;

    case BOOTSTRAPPER_FEATURE_STATE_ABSENT:
        if (BOOTSTRAPPER_FEATURE_STATE_ABSENT != currentState)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_REMOVE;
        }
        break;

    case BOOTSTRAPPER_FEATURE_STATE_ADVERTISED:
        if (BOOTSTRAPPER_FEATURE_STATE_ADVERTISED != currentState)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE;
        }
        else if (fRepair)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_REINSTALL;
        }
        break;

    case BOOTSTRAPPER_FEATURE_STATE_LOCAL:
        if (BOOTSTRAPPER_FEATURE_STATE_LOCAL != currentState)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL;
        }
        else if (fRepair)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_REINSTALL;
        }
        break;

    case BOOTSTRAPPER_FEATURE_STATE_SOURCE:
        if (BOOTSTRAPPER_FEATURE_STATE_SOURCE != currentState)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE;
        }
        else if (fRepair)
        {
            *pFeatureAction = BOOTSTRAPPER_FEATURE_ACTION_REINSTALL;
        }
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnRootFailure(hr, "Invalid state value.");
    }

    if (BOOTSTRAPPER_FEATURE_ACTION_NONE != *pFeatureAction)
    {
        *pfDelta = TRUE;
    }

LExit:
    return hr;
}

static HRESULT EscapePropertyArgumentString(
    __in LPCWSTR wzProperty,
    __inout_z LPWSTR* psczEscapedValue,
    __in BOOL fZeroOnRealloc
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;
    DWORD cchEscape = 0;
    LPCWSTR wzSource = NULL;
    LPWSTR wzTarget = NULL;

    // count characters to escape
    wzSource = wzProperty;
    while (*wzSource)
    {
        ++cch;
        if (L'\"' == *wzSource)
        {
            ++cchEscape;
        }
        ++wzSource;
    }

    // allocate target buffer
    hr = VariableStrAlloc(fZeroOnRealloc, psczEscapedValue, cch + cchEscape + 1); // character count, plus escape character count, plus null terminator
    ExitOnFailure(hr, "Failed to allocate string buffer.");

    // write to target buffer
    wzSource = wzProperty;
    wzTarget = *psczEscapedValue;
    while (*wzSource)
    {
        *wzTarget = *wzSource;
        if (L'\"' == *wzTarget)
        {
            ++wzTarget;
            *wzTarget = L'\"';
        }

        ++wzSource;
        ++wzTarget;
    }

    *wzTarget = L'\0'; // add null terminator

LExit:
    return hr;
}

static HRESULT ConcatFeatureActionProperties(
    __in BURN_PACKAGE* pPackage,
    __in BOOTSTRAPPER_FEATURE_ACTION* rgFeatureActions,
    __inout_z LPWSTR* psczArguments
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;
    LPWSTR sczAddLocal = NULL;
    LPWSTR sczAddSource = NULL;
    LPWSTR sczAddDefault = NULL;
    LPWSTR sczReinstall = NULL;
    LPWSTR sczAdvertise = NULL;
    LPWSTR sczRemove = NULL;

    // features
    for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
    {
        BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

        switch (rgFeatureActions[i])
        {
        case BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL:
            if (sczAddLocal)
            {
                hr = StrAllocConcat(&sczAddLocal, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczAddLocal, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;

        case BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE:
            if (sczAddSource)
            {
                hr = StrAllocConcat(&sczAddSource, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczAddSource, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;

        case BOOTSTRAPPER_FEATURE_ACTION_ADDDEFAULT:
            if (sczAddDefault)
            {
                hr = StrAllocConcat(&sczAddDefault, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczAddDefault, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;

        case BOOTSTRAPPER_FEATURE_ACTION_REINSTALL:
            if (sczReinstall)
            {
                hr = StrAllocConcat(&sczReinstall, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczReinstall, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;

        case BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE:
            if (sczAdvertise)
            {
                hr = StrAllocConcat(&sczAdvertise, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczAdvertise, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;

        case BOOTSTRAPPER_FEATURE_ACTION_REMOVE:
            if (sczRemove)
            {
                hr = StrAllocConcat(&sczRemove, L",", 0);
                ExitOnFailure(hr, "Failed to concat separator.");
            }
            hr = StrAllocConcat(&sczRemove, pFeature->sczId, 0);
            ExitOnFailure(hr, "Failed to concat feature.");
            break;
        }
    }

    if (sczAddLocal)
    {
        hr = StrAllocFormatted(&scz, L" ADDLOCAL=\"%s\"", sczAddLocal, 0);
        ExitOnFailure(hr, "Failed to format ADDLOCAL string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

    if (sczAddSource)
    {
        hr = StrAllocFormatted(&scz, L" ADDSOURCE=\"%s\"", sczAddSource, 0);
        ExitOnFailure(hr, "Failed to format ADDSOURCE string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

    if (sczAddDefault)
    {
        hr = StrAllocFormatted(&scz, L" ADDDEFAULT=\"%s\"", sczAddDefault, 0);
        ExitOnFailure(hr, "Failed to format ADDDEFAULT string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

    if (sczReinstall)
    {
        hr = StrAllocFormatted(&scz, L" REINSTALL=\"%s\"", sczReinstall, 0);
        ExitOnFailure(hr, "Failed to format REINSTALL string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

    if (sczAdvertise)
    {
        hr = StrAllocFormatted(&scz, L" ADVERTISE=\"%s\"", sczAdvertise, 0);
        ExitOnFailure(hr, "Failed to format ADVERTISE string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

    if (sczRemove)
    {
        hr = StrAllocFormatted(&scz, L" REMOVE=\"%s\"", sczRemove, 0);
        ExitOnFailure(hr, "Failed to format REMOVE string.");

        hr = StrAllocConcatSecure(psczArguments, scz, 0);
        ExitOnFailure(hr, "Failed to concat argument string.");
    }

LExit:
    ReleaseStr(scz);
    ReleaseStr(sczAddLocal);
    ReleaseStr(sczAddSource);
    ReleaseStr(sczAddDefault);
    ReleaseStr(sczReinstall);
    ReleaseStr(sczAdvertise);
    ReleaseStr(sczRemove);

    return hr;
}

static HRESULT ConcatPatchProperty(
    __in BURN_PACKAGE* pPackage,
    __in_opt BOOTSTRAPPER_ACTION_STATE* rgSlipstreamPatchActions,
    __inout_z LPWSTR* psczArguments
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczMspPath = NULL;
    LPWSTR sczPatches = NULL;

    // If there are slipstream patch actions, build up their patch action.
    if (rgSlipstreamPatchActions)
    {
        for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
        {
            BURN_PACKAGE* pMspPackage = pPackage->Msi.rgpSlipstreamMspPackages[i];
            AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Only MSP packages can be slipstream patches.");

            BOOTSTRAPPER_ACTION_STATE patchExecuteAction = rgSlipstreamPatchActions[i];
            if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL < patchExecuteAction)
            {
                hr = CacheGetCompletedPath(pMspPackage->fPerMachine, pMspPackage->sczCacheId, &sczCachedDirectory);
                ExitOnFailure1(hr, "Failed to get cached path for MSP package: %ls", pMspPackage->sczId);

                hr = PathConcat(sczCachedDirectory, pMspPackage->rgPayloads[0].pPayload->sczFilePath, &sczMspPath);
                ExitOnFailure(hr, "Failed to build MSP path.");

                if (!sczPatches)
                {
                    hr = StrAllocConcat(&sczPatches, L" PATCH=\"", 0);
                    ExitOnFailure(hr, "Failed to prefix with PATCH property.");
                }
                else
                {
                    hr = StrAllocConcat(&sczPatches, L";", 0);
                    ExitOnFailure(hr, "Failed to semi-colon delimit patches.");
                }

                hr = StrAllocConcat(&sczPatches, sczMspPath, 0);
                ExitOnFailure(hr, "Failed to append patch path.");
            }
        }

        if (sczPatches)
        {
            hr = StrAllocConcat(&sczPatches, L"\"", 0);
            ExitOnFailure(hr, "Failed to close the quoted PATCH property.");

            hr = StrAllocConcatSecure(psczArguments, sczPatches, 0);
            ExitOnFailure(hr, "Failed to append PATCH property.");
        }
    }

LExit:
    ReleaseStr(sczMspPath);
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczPatches);
    return hr;
}

static void RegisterSourceDirectory(
    __in BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzMsiPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczMsiDirectory = NULL;
    MSIINSTALLCONTEXT dwContext = pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED;

    hr = PathGetDirectory(wzMsiPath, &sczMsiDirectory);
    ExitOnFailure1(hr, "Failed to get directory for path: %ls", wzMsiPath);

    hr = WiuSourceListAddSourceEx(pPackage->Msi.sczProductCode, NULL, dwContext, MSICODE_PRODUCT, sczMsiDirectory, 1);
    if (FAILED(hr))
    {
        LogId(REPORT_VERBOSE, MSG_SOURCELIST_REGISTER, sczMsiDirectory, pPackage->Msi.sczProductCode, hr);
        ExitFunction();
    }

LExit:
    ReleaseStr(sczMsiDirectory);

    return;
}
