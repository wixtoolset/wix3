//-------------------------------------------------------------------------------------------------
// <copyright file="balinfo.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Bootstrapper Application Layer information utility.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// prototypes
static HRESULT ParsePackagesFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    );


DAPI_(HRESULT) BalInfoParseFromXml(
    __in BAL_INFO_BUNDLE* pBundle,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pNode = NULL;

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixBundleProperties", &pNode);
    if (S_OK == hr)
    {
        hr = XmlGetYesNoAttribute(pNode, L"PerMachine", &pBundle->fPerMachine);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to read bundle information per-machine.");
        }

        hr = XmlGetAttributeEx(pNode, L"DisplayName", &pBundle->sczName);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to read bundle information display name.");
        }

        hr = XmlGetAttributeEx(pNode, L"LogPathVariable", &pBundle->sczLogVariable);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to read bundle information log path variable.");
        }
    }
    ExitOnFailure(hr, "Failed to select bundle information.");

    hr = ParsePackagesFromXml(&pBundle->packages, pixdManifest);
    BalExitOnFailure(hr, "Failed to parse package information from bootstrapper application data.");

LExit:
    ReleaseObject(pNode);

    return hr;
}


DAPI_(HRESULT) BalInfoAddRelatedBundleAsPackage(
    __in BAL_INFO_PACKAGES* pPackages,
    __in LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL /*fPerMachine*/
    )
{
    HRESULT hr = S_OK;
    BAL_INFO_PACKAGE_TYPE type = BAL_INFO_PACKAGE_TYPE_UNKNOWN;
    BAL_INFO_PACKAGE* pPackage = NULL;

    // Ensure we have a supported relation type.
    switch (relationType)
    {
    case BOOTSTRAPPER_RELATION_ADDON:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_ADDON;
        break;

    case BOOTSTRAPPER_RELATION_PATCH:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_PATCH;
        break;

    case BOOTSTRAPPER_RELATION_UPGRADE:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_UPGRADE;
        break;

    default:
        ExitOnFailure1(hr = E_INVALIDARG, "Unknown related bundle type: %u", relationType);
    }

    // Check to see if the bundle is already in the list of packages.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzId, -1, pPackages->rgPackages[i].sczId, -1))
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS));
        }
    }

    // Add the related bundle as a package.
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPackages->rgPackages), pPackages->cPackages + 1, sizeof(BAL_INFO_PACKAGE), 2);
    ExitOnFailure(hr, "Failed to allocate memory for related bundle package information.");

    pPackage = pPackages->rgPackages + pPackages->cPackages;
    ++pPackages->cPackages;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy related bundle package id.");

    pPackage->type = type;

    // TODO: try to look up the DisplayName and Description in Add/Remove Programs with the wzId.

LExit:
    return hr;
}


DAPI_(HRESULT) BalInfoFindPackageById(
    __in BAL_INFO_PACKAGES* pPackages,
    __in LPCWSTR wzId,
    __out BAL_INFO_PACKAGE** ppPackage
    )
{
    *ppPackage = NULL;

    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzId, -1, pPackages->rgPackages[i].sczId, -1))
        {
            *ppPackage = pPackages->rgPackages + i;
            break;
        }
    }

    return *ppPackage ? S_OK : E_NOTFOUND;
}


DAPI_(void) BalInfoUninitialize(
    __in BAL_INFO_BUNDLE* pBundle
    )
{
    for (DWORD i = 0; i < pBundle->packages.cPackages; ++i)
    {
        ReleaseStr(pBundle->packages.rgPackages[i].sczDisplayName);
        ReleaseStr(pBundle->packages.rgPackages[i].sczDescription);
        ReleaseStr(pBundle->packages.rgPackages[i].sczId);
    }

    ReleaseMem(pBundle->packages.rgPackages);

    ReleaseStr(pBundle->sczName);
    ReleaseStr(pBundle->sczLogVariable);
    memset(pBundle, 0, sizeof(BAL_INFO_BUNDLE));
}


static HRESULT ParsePackagesFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pNodeList = NULL;
    IXMLDOMNode* pNode = NULL;
    BAL_INFO_PACKAGE* prgPackages = NULL;
    DWORD cPackages = 0;
    LPWSTR sczType = NULL;

    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixPackageProperties", &pNodeList);
    ExitOnFailure(hr, "Failed to select all packages.");

    hr = pNodeList->get_length(reinterpret_cast<long*>(&cPackages));
    ExitOnFailure(hr, "Failed to get the package count.");

    prgPackages = static_cast<BAL_INFO_PACKAGE*>(MemAlloc(sizeof(BAL_INFO_PACKAGE) * cPackages, TRUE));
    ExitOnNull(prgPackages, hr, E_OUTOFMEMORY, "Failed to allocate memory for packages.");

    DWORD iPackage = 0;
    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
    {
        hr = XmlGetAttributeEx(pNode, L"Package", &prgPackages[iPackage].sczId);
        ExitOnFailure(hr, "Failed to get package identifier for package.");

        hr = XmlGetAttributeEx(pNode, L"DisplayName", &prgPackages[iPackage].sczDisplayName);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get description for package.");
        }

        hr = XmlGetAttributeEx(pNode, L"Description", &prgPackages[iPackage].sczDescription);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get description for package.");
        }

        hr = XmlGetAttributeEx(pNode, L"PackageType", &sczType);
        ExitOnFailure(hr, "Failed to get description for package.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Exe", -1, sczType, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_EXE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msi", -1, sczType, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSI;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msp", -1, sczType, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSP;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msu", -1, sczType, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSU;
        }

        hr = XmlGetYesNoAttribute(pNode, L"Permanent", &prgPackages[iPackage].fPermanent);
        ExitOnFailure(hr, "Failed to get permanent setting for package.");

        hr = XmlGetYesNoAttribute(pNode, L"Vital", &prgPackages[iPackage].fVital);
        ExitOnFailure(hr, "Failed to get vital setting for package.");

        hr = XmlGetYesNoAttribute(pNode, L"DisplayInternalUI", &prgPackages[iPackage].fDisplayInternalUI);
        ExitOnFailure(hr, "Failed to get DisplayInternalUI setting for package.");

        ++iPackage;
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to parse all package property elements.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    pPackages->cPackages = cPackages;
    pPackages->rgPackages = prgPackages;
    prgPackages = NULL;

LExit:
    ReleaseStr(sczType);
    ReleaseMem(prgPackages);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}