// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static HRESULT ParsePayloadRefsFromXml(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PAYLOADS* pPayloads,
    __in IXMLDOMNode* pixnPackage
    );
static HRESULT ParsePatchTargetCode(
    __in BURN_PACKAGES* pPackages,
    __in IXMLDOMNode* pixnBundle
    );
static HRESULT FindRollbackBoundaryById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    );


// function definitions

extern "C" HRESULT PackagesParseFromXml(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeName = NULL;
    DWORD cMspPackages = 0;
    LPWSTR scz = NULL;

    // select rollback boundary nodes
    hr = XmlSelectNodes(pixnBundle, L"RollbackBoundary", &pixnNodes);
    ExitOnFailure(hr, "Failed to select rollback boundary nodes.");

    // get rollback boundary node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get rollback bundary node count.");

    if (cNodes)
    {
        // allocate memory for rollback boundaries
        pPackages->rgRollbackBoundaries = (BURN_ROLLBACK_BOUNDARY*)MemAlloc(sizeof(BURN_ROLLBACK_BOUNDARY) * cNodes, TRUE);
        ExitOnNull(pPackages->rgRollbackBoundaries, hr, E_OUTOFMEMORY, "Failed to allocate memory for rollback boundary structs.");

        pPackages->cRollbackBoundaries = cNodes;

        // parse rollback boundary elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = &pPackages->rgRollbackBoundaries[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Id
            hr = XmlGetAttributeEx(pixnNode, L"Id", &pRollbackBoundary->sczId);
            ExitOnFailure(hr, "Failed to get @Id.");

            // @Vital
            hr = XmlGetYesNoAttribute(pixnNode, L"Vital", &pRollbackBoundary->fVital);
            ExitOnFailure(hr, "Failed to get @Vital.");

            // prepare next iteration
            ReleaseNullObject(pixnNode);
            ReleaseNullBSTR(bstrNodeName);
        }
    }

    ReleaseNullObject(pixnNodes); // done with the RollbackBoundary elements.

    // select package nodes
    hr = XmlSelectNodes(pixnBundle, L"Chain/ExePackage|Chain/MsiPackage|Chain/MspPackage|Chain/MsuPackage", &pixnNodes);
    ExitOnFailure(hr, "Failed to select package nodes.");

    // get package node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get package node count.");

    if (!cNodes)
    {
        ExitFunction1(hr = S_OK);
    }

    // allocate memory for packages
    pPackages->rgPackages = (BURN_PACKAGE*)MemAlloc(sizeof(BURN_PACKAGE) * cNodes, TRUE);
    ExitOnNull(pPackages->rgPackages, hr, E_OUTOFMEMORY, "Failed to allocate memory for package structs.");

    pPackages->cPackages = cNodes;

    // parse package elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_PACKAGE* pPackage = &pPackages->rgPackages[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, &bstrNodeName);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pPackage->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Cache
        hr = XmlGetAttributeEx(pixnNode, L"Cache", &scz);
        if (SUCCEEDED(hr))
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"no", -1))
            {
                pPackage->cacheType = BURN_CACHE_TYPE_NO;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"yes", -1))
            {
                pPackage->cacheType = BURN_CACHE_TYPE_YES;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"always", -1))
            {
                pPackage->cacheType = BURN_CACHE_TYPE_ALWAYS;
            }
            else
            {
                hr = E_UNEXPECTED;
                ExitOnFailure1(hr, "Invalid cache type: %ls", scz);
            }
        }
        ExitOnFailure(hr, "Failed to get @Cache.");

        // @CacheId
        hr = XmlGetAttributeEx(pixnNode, L"CacheId", &pPackage->sczCacheId);
        ExitOnFailure(hr, "Failed to get @CacheId.");

        // @Size
        hr = XmlGetAttributeLargeNumber(pixnNode, L"Size", &pPackage->qwSize);
        ExitOnFailure(hr, "Failed to get @Size.");

        // @InstallSize
        hr = XmlGetAttributeLargeNumber(pixnNode, L"InstallSize", &pPackage->qwInstallSize);
        ExitOnFailure(hr, "Failed to get @InstallSize.");

        // @PerMachine
        hr = XmlGetYesNoAttribute(pixnNode, L"PerMachine", &pPackage->fPerMachine);
        ExitOnFailure(hr, "Failed to get @PerMachine.");

        // @Permanent
        hr = XmlGetYesNoAttribute(pixnNode, L"Permanent", &pPackage->fUninstallable);
        ExitOnFailure(hr, "Failed to get @Permanent.");
        pPackage->fUninstallable = !pPackage->fUninstallable; // TODO: change "Uninstallable" variable name to permanent, until then Uninstallable is the opposite of Permanent so fix the variable.

        // @Vital
        hr = XmlGetYesNoAttribute(pixnNode, L"Vital", &pPackage->fVital);
        ExitOnFailure(hr, "Failed to get @Vital.");

        // @LogPathVariable
        hr = XmlGetAttributeEx(pixnNode, L"LogPathVariable", &pPackage->sczLogPathVariable);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @LogPathVariable.");
        }

        // @RollbackLogPathVariable
        hr = XmlGetAttributeEx(pixnNode, L"RollbackLogPathVariable", &pPackage->sczRollbackLogPathVariable);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @RollbackLogPathVariable.");
        }

        // @InstallCondition
        hr = XmlGetAttributeEx(pixnNode, L"InstallCondition", &pPackage->sczInstallCondition);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @InstallCondition.");
        }

        // @RollbackBoundaryForward
        hr = XmlGetAttributeEx(pixnNode, L"RollbackBoundaryForward", &scz);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @RollbackBoundaryForward.");

            hr =  FindRollbackBoundaryById(pPackages, scz, &pPackage->pRollbackBoundaryForward);
            ExitOnFailure1(hr, "Failed to find forward transaction boundary: %ls", scz);
        }

        // @RollbackBoundaryBackward
        hr = XmlGetAttributeEx(pixnNode, L"RollbackBoundaryBackward", &scz);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @RollbackBoundaryBackward.");

            hr =  FindRollbackBoundaryById(pPackages, scz, &pPackage->pRollbackBoundaryBackward);
            ExitOnFailure1(hr, "Failed to find backward transaction boundary: %ls", scz);
        }

        // read type specific attributes
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"ExePackage", -1))
        {
            pPackage->type = BURN_PACKAGE_TYPE_EXE;

            hr = ExeEngineParsePackageFromXml(pixnNode, pPackage); // TODO: Modularization
            ExitOnFailure(hr, "Failed to parse EXE package.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MsiPackage", -1))
        {
            pPackage->type = BURN_PACKAGE_TYPE_MSI;

            hr = MsiEngineParsePackageFromXml(pixnNode, pPackage); // TODO: Modularization
            ExitOnFailure(hr, "Failed to parse MSI package.");
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MspPackage", -1))
        {
            pPackage->type = BURN_PACKAGE_TYPE_MSP;

            hr = MspEngineParsePackageFromXml(pixnNode, pPackage); // TODO: Modularization
            ExitOnFailure(hr, "Failed to parse MSP package.");

            ++cMspPackages;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrNodeName, -1, L"MsuPackage", -1))
        {
            pPackage->type = BURN_PACKAGE_TYPE_MSU;

            hr = MsuEngineParsePackageFromXml(pixnNode, pPackage); // TODO: Modularization
            ExitOnFailure(hr, "Failed to parse MSU package.");
        }
        else
        {
            // ignore other package types for now
        }

        // parse payload references
        hr = ParsePayloadRefsFromXml(pPackage, pPayloads, pixnNode);
        ExitOnFailure(hr, "Failed to parse payload references.");

        // parse dependency providers
        hr = DependencyParseProvidersFromXml(pPackage, pixnNode);
        ExitOnFailure(hr, "Failed to parse dependency providers.");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullBSTR(bstrNodeName);
    }

    if (cMspPackages)
    {
        pPackages->rgPatchInfo = static_cast<MSIPATCHSEQUENCEINFOW*>(MemAlloc(sizeof(MSIPATCHSEQUENCEINFOW) * cMspPackages, TRUE));
        ExitOnNull(pPackages->rgPatchInfo, hr, E_OUTOFMEMORY, "Failed to allocate memory for MSP patch sequence information.");

        pPackages->rgPatchInfoToPackage = static_cast<BURN_PACKAGE**>(MemAlloc(sizeof(BURN_PACKAGE*) * cMspPackages, TRUE));
        ExitOnNull(pPackages->rgPatchInfoToPackage, hr, E_OUTOFMEMORY, "Failed to allocate memory for patch sequence information to package lookup.");

        for (DWORD i = 0; i < pPackages->cPackages; ++i)
        {
            BURN_PACKAGE* pPackage = &pPackages->rgPackages[i];

            if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
            {
                pPackages->rgPatchInfo[pPackages->cPatchInfo].szPatchData = pPackage->Msp.sczApplicabilityXml;
                pPackages->rgPatchInfo[pPackages->cPatchInfo].ePatchDataType = MSIPATCH_DATATYPE_XMLBLOB;
                pPackages->rgPatchInfoToPackage[pPackages->cPatchInfo] = pPackage;
                ++pPackages->cPatchInfo;

                // Loop through all MSI packages seeing if any of them slipstream this MSP.
                for (DWORD j = 0; j < pPackages->cPackages; ++j)
                {
                    BURN_PACKAGE* pMsiPackage = &pPackages->rgPackages[j];

                    if (BURN_PACKAGE_TYPE_MSI == pMsiPackage->type)
                    {
                        for (DWORD k = 0; k < pMsiPackage->Msi.cSlipstreamMspPackages; ++k)
                        {
                            if (pMsiPackage->Msi.rgsczSlipstreamMspPackageIds[k] && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPackage->sczId, -1, pMsiPackage->Msi.rgsczSlipstreamMspPackageIds[k], -1))
                            {
                                pMsiPackage->Msi.rgpSlipstreamMspPackages[k] = pPackage;

                                ReleaseNullStr(pMsiPackage->Msi.rgsczSlipstreamMspPackageIds[k]); // we don't need the slipstream package id any longer so free it.
                            }
                        }
                    }
                }
            }
        }
    }

    AssertSz(pPackages->cPatchInfo == cMspPackages, "Count of packages patch info should be equal to the number of MSP packages.");

    hr = ParsePatchTargetCode(pPackages, pixnBundle);
    ExitOnFailure(hr, "Failed to parse target product codes.");

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseBSTR(bstrNodeName);
    ReleaseStr(scz);

    return hr;
}

extern "C" void PackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->sczId);
    ReleaseStr(pPackage->sczLogPathVariable);
    ReleaseStr(pPackage->sczRollbackLogPathVariable);
    ReleaseStr(pPackage->sczInstallCondition);
    ReleaseStr(pPackage->sczRollbackInstallCondition);
    ReleaseStr(pPackage->sczCacheId);

    if (pPackage->rgDependencyProviders)
    {
        for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
        {
            DependencyUninitialize(pPackage->rgDependencyProviders + i);
        }
        MemFree(pPackage->rgDependencyProviders);
    }

    ReleaseMem(pPackage->rgPayloads);

    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_EXE:
        ExeEnginePackageUninitialize(pPackage); // TODO: Modularization
        break;
    case BURN_PACKAGE_TYPE_MSI:
        MsiEnginePackageUninitialize(pPackage); // TODO: Modularization
        break;
    case BURN_PACKAGE_TYPE_MSP:
        MspEnginePackageUninitialize(pPackage); // TODO: Modularization
        break;
    case BURN_PACKAGE_TYPE_MSU:
        MsuEnginePackageUninitialize(pPackage); // TODO: Modularization
        break;
    }
}

extern "C" void PackagesUninitialize(
    __in BURN_PACKAGES* pPackages
    )
{
    if (pPackages->rgRollbackBoundaries)
    {
        for (DWORD i = 0; i < pPackages->cRollbackBoundaries; ++i)
        {
            ReleaseStr(pPackages->rgRollbackBoundaries[i].sczId);
        }
        MemFree(pPackages->rgRollbackBoundaries);
    }

    if (pPackages->rgPackages)
    {
        for (DWORD i = 0; i < pPackages->cPackages; ++i)
        {
            PackageUninitialize(pPackages->rgPackages + i);
        }
        MemFree(pPackages->rgPackages);
    }

    if (pPackages->rgCompatiblePackages)
    {
        for (DWORD i = 0; i < pPackages->cCompatiblePackages; ++i)
        {
            PackageUninitialize(pPackages->rgCompatiblePackages + i);
        }
        MemFree(pPackages->rgCompatiblePackages);
    }

    if (pPackages->rgPatchTargetCodes)
    {
        for (DWORD i = 0; i < pPackages->cPatchTargetCodes; ++i)
        {
            ReleaseStr(pPackages->rgPatchTargetCodes[i].sczTargetCode);
        }
        MemFree(pPackages->rgPatchTargetCodes);
    }

    ReleaseMem(pPackages->rgPatchInfo);
    ReleaseMem(pPackages->rgPatchInfoToPackage);

    // clear struct
    memset(pPackages, 0, sizeof(BURN_PACKAGES));
}

extern "C" HRESULT PackageFindById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    )
{
    HRESULT hr = S_OK;
    BURN_PACKAGE* pPackage = NULL;

    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        pPackage = &pPackages->rgPackages[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPackage->sczId, -1, wzId, -1))
        {
            *ppPackage = pPackage;
            ExitFunction1(hr = S_OK);
        }
    }

    for (DWORD i = 0; i < pPackages->cCompatiblePackages; ++i)
    {
        pPackage = &pPackages->rgCompatiblePackages[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPackage->sczId, -1, wzId, -1))
        {
            *ppPackage = pPackage;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}


extern "C" HRESULT PackageFindRelatedById(
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    )
{
    HRESULT hr = S_OK;
    BURN_PACKAGE* pPackage = NULL;

    for (DWORD i = 0; i < pRelatedBundles->cRelatedBundles; ++i)
    {
        pPackage = &pRelatedBundles->rgRelatedBundles[i].package;

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPackage->sczId, -1, wzId, -1))
        {
            *ppPackage = pPackage;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

/********************************************************************
 PackageGetProperty - Determines if the property is defined
  and optionally copies the property value.

 Note: The caller must free psczValue if requested.

 Note: Returns E_NOTFOUND if the property was not defined or if the
  package does not support properties.

*********************************************************************/
extern "C" HRESULT PackageGetProperty(
    __in const BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzProperty,
    __out_z_opt LPWSTR* psczValue
    )
{
    HRESULT hr = E_NOTFOUND;
    BURN_MSIPROPERTY* rgProperties = NULL;
    DWORD cProperties = 0;

    // For MSIs and MSPs, enumerate the properties looking for wzProperty.
    if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
    {
        rgProperties = pPackage->Msi.rgProperties;
        cProperties = pPackage->Msi.cProperties;
    }
    else if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
    {
        rgProperties = pPackage->Msp.rgProperties;
        cProperties = pPackage->Msp.cProperties;
    }

    for (DWORD i = 0; i < cProperties; ++i)
    {
        const BURN_MSIPROPERTY* pProperty = &rgProperties[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pProperty->sczId, -1, wzProperty, -1))
        {
            if (psczValue)
            {
                hr = StrAllocString(psczValue, pProperty->sczValue, 0);
                ExitOnFailure(hr, "Failed to copy the property value.");
            }

            ExitFunction1(hr = S_OK);
        }
    }

LExit:
    return hr;
}

HRESULT PackageEnsureCompatiblePackagesArray(
    __in BURN_PACKAGES* pPackages
    )
{
    HRESULT hr = S_OK;

    if (!pPackages->rgCompatiblePackages)
    {
        pPackages->rgCompatiblePackages = (BURN_PACKAGE*)MemAlloc(sizeof(BURN_PACKAGE) * pPackages->cPackages, TRUE);
        ExitOnNull(pPackages->rgCompatiblePackages, hr, E_OUTOFMEMORY, "Failed to allocate memory for compatible packages.");
    }

LExit:
    return hr;
}


// internal function declarations

static HRESULT ParsePayloadRefsFromXml(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PAYLOADS* pPayloads,
    __in IXMLDOMNode* pixnPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR sczId = NULL;

    // select package nodes
    hr = XmlSelectNodes(pixnPackage, L"PayloadRef", &pixnNodes);
    ExitOnFailure(hr, "Failed to select package nodes.");

    // get package node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get package node count.");

    if (!cNodes)
    {
        ExitFunction1(hr = S_OK);
    }

    // allocate memory for payload pointers
    pPackage->rgPayloads = (BURN_PACKAGE_PAYLOAD*)MemAlloc(sizeof(BURN_PACKAGE_PAYLOAD) * cNodes, TRUE);
    ExitOnNull(pPackage->rgPayloads, hr, E_OUTOFMEMORY, "Failed to allocate memory for package payloads.");

    pPackage->cPayloads = cNodes;

    // parse package elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_PACKAGE_PAYLOAD* pPackagePayload = &pPackage->rgPayloads[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &sczId);
        ExitOnFailure(hr, "Failed to get Id attribute.");

        // find payload
        hr = PayloadFindById(pPayloads, sczId, &pPackagePayload->pPayload);
        ExitOnFailure(hr, "Failed to find payload.");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(sczId);

    return hr;
}

static HRESULT ParsePatchTargetCode(
    __in BURN_PACKAGES* pPackages,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    BSTR bstrNodeText = NULL;
    BOOL fProduct;

    hr = XmlSelectNodes(pixnBundle, L"PatchTargetCode", &pixnNodes);
    ExitOnFailure(hr, "Failed to select PatchTargetCode nodes.");

    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get PatchTargetCode node count.");

    if (!cNodes)
    {
        ExitFunction1(hr = S_OK);
    }

    pPackages->rgPatchTargetCodes = (BURN_PATCH_TARGETCODE*)MemAlloc(sizeof(BURN_PATCH_TARGETCODE) * cNodes, TRUE);
    ExitOnNull(pPackages->rgPatchTargetCodes, hr, E_OUTOFMEMORY, "Failed to allocate memory for patch targetcodes.");

    pPackages->cPatchTargetCodes = cNodes;

    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_PATCH_TARGETCODE* pTargetCode = pPackages->rgPatchTargetCodes + i;

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        hr = XmlGetAttributeEx(pixnNode, L"TargetCode", &pTargetCode->sczTargetCode);
        ExitOnFailure(hr, "Failed to get @TargetCode attribute.");

        hr = XmlGetYesNoAttribute(pixnNode, L"Product", &fProduct);
        if (E_NOTFOUND == hr)
        {
            fProduct = FALSE;
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to get @Product.");

        pTargetCode->type = fProduct ? BURN_PATCH_TARGETCODE_TYPE_PRODUCT : BURN_PATCH_TARGETCODE_TYPE_UPGRADE;

        // prepare next iteration
        ReleaseNullBSTR(bstrNodeText);
        ReleaseNullObject(pixnNode);
    }

LExit:
    ReleaseBSTR(bstrNodeText);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);

    return hr;
}

static HRESULT FindRollbackBoundaryById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;

    for (DWORD i = 0; i < pPackages->cRollbackBoundaries; ++i)
    {
        pRollbackBoundary = &pPackages->rgRollbackBoundaries[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pRollbackBoundary->sczId, -1, wzId, -1))
        {
            *ppRollbackBoundary = pRollbackBoundary;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
