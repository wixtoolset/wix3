//-------------------------------------------------------------------------------------------------
// <copyright file="scaweb.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Web Table functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

//Adding this because delivery doesn't have the updated specstrings.h that windows build does
#ifndef __in_xcount
#define __in_xcount(size)
#endif

// sql queries

enum eWebBaseQuery { wbqWeb = 1, wbqId, wbqIP, wbqPort, wbqHeader, wbqSecure, wbqDescription };


// prototypes for private helper functions
static SCA_WEB* NewWeb();
static void FreeWeb(SCA_WEB *pswDelete);
static SCA_WEB* AddWebToList(
    __in SCA_WEB* pswList,
    __in SCA_WEB* psw
    );
static HRESULT ScaWebFindBase(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in_z LPCWSTR wzWeb,
    __in int iSiteId,
    __in_z LPCWSTR wzIP,
    __in int iPort,
    __in_z LPCWSTR wzHeader,
    __in BOOL fSecure,
    __in_z LPCWSTR wzDescription,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase
    );
static HRESULT ScaWebFindFreeBase(
    __in IMSAdminBase* piMetabase,
    __in_xcount(unknown) SCA_WEB* pswList,
    __in int iSiteId,
    __in_z LPCWSTR wzDescription,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase
    );
static HRESULT ScaWebWrite(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* psw,
    __in SCA_APPPOOL * psapList
    );
static HRESULT ScaWebRemove(
    __in IMSAdminBase* piMetabase,
    __in const SCA_WEB* psw);
static DWORD SiteIdFromDescription(
    __in_z LPCWSTR wzDescription
    );
static void Sort(
    __in_ecount(cArray) DWORD dwArray[],
    __in int cArray
    );


HRESULT ScaWebsRead(
    __in IMSAdminBase* piMetabase,
    __in SCA_MIMEMAP** ppsmmList,
    __in SCA_WEB** ppswList,
    __in SCA_HTTP_HEADER** ppshhList,
    __in SCA_WEB_ERROR** ppsweList,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __in WCA_WRAPQUERY_HANDLE hSslCertQuery,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
    __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
    __inout LPWSTR *ppwzCustomActionData
    )
{
    Assert(piMetabase && ppswList);

    HRESULT hr = S_OK;

    MSIHANDLE hRec;
    MSIHANDLE hRecAddresses;

    SCA_WEB* psw = NULL;
    LPWSTR pwzData = NULL;
    int iSiteId;

    DWORD dwLen = 0;
    WCA_WRAPQUERY_HANDLE hQueryWebSite = NULL;
    WCA_WRAPQUERY_HANDLE hQueryWebAddress = NULL;

    hr = WcaBeginUnwrapQuery(&hQueryWebSite, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebsRead");

    hr = WcaBeginUnwrapQuery(&hQueryWebAddress, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebsRead");

    if (0 == WcaGetQueryRecords(hQueryWebSite))
    {
        WcaLog(LOGMSG_VERBOSE, "Required tables not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the webs
    while (S_OK == (hr = WcaFetchWrappedRecord(hQueryWebSite, &hRec)))
    {
        psw = NewWeb();
        ExitOnNull(psw, hr, E_OUTOFMEMORY, "Failed to allocate memory for web object in memory");

        // get the darwin information
        hr = WcaGetRecordString(hRec, wqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web");
        hr = ::StringCchCopyW(psw->wzKey, countof(psw->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to web object");

        if (*pwzData && *ppsmmList)
        {
            hr = ScaGetMimeMap(mmptWeb, pwzData, ppsmmList, &psw->psmm);
            ExitOnFailure(hr, "Failed to get mimemap for VirtualDir");
        }

        // get component install state
        hr = WcaGetRecordString(hRec, wqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Component for Web");
        hr = ::StringCchCopyW(psw->wzComponent, countof(psw->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to web object");
        if (*(psw->wzComponent))
        {
            psw->fHasComponent = TRUE;

            hr = WcaGetRecordInteger(hRec, wqInstalled, (int *)&psw->isInstalled);
            ExitOnFailure(hr, "Failed to get web Component's installed state");

            WcaGetRecordInteger(hRec, wqAction, (int *)&psw->isAction);
            ExitOnFailure(hr, "Failed to get web Component's action state");

            if (!WcaIsInstalling(psw->isInstalled, psw->isAction) && !WcaIsUninstalling(psw->isInstalled, psw->isAction)
                && !WcaIsReInstalling(psw->isInstalled, psw->isAction))
            {
                FreeWeb(psw);
                psw = NULL;

                continue; // If we aren't acting on this component, skip it
            }
        }

        hr = WcaGetRecordInteger(hRec, wqId, &iSiteId);
        ExitOnFailure(hr, "Failed to get SiteId for Web");

        // Get the web's key address.
        hr = WcaGetRecordString(hRec, wqAddress, &pwzData);
        ExitOnFailure(hr, "Failed to get Address for Web");
        hr = ::StringCchCopyW(psw->swaKey.wzKey, countof(psw->swaKey.wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to web object");

        hr = WcaGetRecordString(hRec, wqIP, &pwzData);
        ExitOnFailure(hr, "Failed to get IP for Web");
        hr = ::StringCchCopyW(psw->swaKey.wzIP, countof(psw->swaKey.wzIP), pwzData);
        ExitOnFailure(hr, "Failed to copy IP string to web object");

        hr = WcaGetRecordString(hRec, wqPort, &pwzData);
        ExitOnFailure(hr, "Failed to get Web Address port");
        psw->swaKey.iPort = wcstol(pwzData, NULL, 10);

        hr = WcaGetRecordString(hRec, wqHeader, &pwzData);
        ExitOnFailure(hr, "Failed to get Header for Web");
        hr = ::StringCchCopyW(psw->swaKey.wzHeader, countof(psw->swaKey.wzHeader), pwzData);
        ExitOnFailure(hr, "Failed to copy header string to web object");

        hr = WcaGetRecordInteger(hRec, wqSecure, &psw->swaKey.fSecure);
        ExitOnFailure(hr, "Failed to get if Web is secure");
        if (S_FALSE == hr)
        {
            psw->swaKey.fSecure = FALSE;
        }

        // Get the web's description.
        hr = WcaGetRecordString(hRec, wqDescription, &pwzData);
        ExitOnFailure(hr, "Failed to get Description for Web");
        hr = ::StringCchCopyW(psw->wzDescription, countof(psw->wzDescription), pwzData);
        ExitOnFailure(hr, "Failed to copy description string to web object");

        // Try to find the web root in case it already exists.
        dwLen = METADATA_MAX_NAME_LEN;
        hr = ScaWebFindBase(piMetabase, *ppswList,
                            psw->wzKey,
                            iSiteId,
                            psw->swaKey.wzIP,
                            psw->swaKey.iPort,
                            psw->swaKey.wzHeader,
                            psw->swaKey.fSecure,
                            psw->wzDescription,
                            psw->wzWebBase, dwLen);
        if (S_OK == hr)
        {
            psw->fBaseExists = TRUE;
        }
        else if (S_FALSE == hr) // didn't find the web site.
        {
            psw->fBaseExists = FALSE;

            // If we're actually configuring the web site.
            if (psw->fHasComponent)
            {
                if (WcaIsInstalling(psw->isInstalled, psw->isAction))
                {
                    hr = ScaWebFindFreeBase(piMetabase, *ppswList, iSiteId, psw->wzDescription, psw->wzWebBase, countof(psw->wzWebBase));
                    ExitOnFailure(hr, "Failed to find free web root.");
                }
                else if (WcaIsUninstalling(psw->isInstalled, psw->isAction))
                {
                    WcaLog(LOGMSG_VERBOSE, "Web site: '%ls' was already removed, skipping.", psw->wzKey);

                    hr = S_OK;
                    continue;
                }
            }
        }
        ExitOnFailure(hr, "Failed to find web root");

        // get any extra web addresses
        WcaFetchWrappedReset(hQueryWebAddress);
        while (S_OK == (hr = WcaFetchWrappedRecordWhereString(hQueryWebAddress, 2, psw->wzKey, &hRecAddresses)))
        {
            if (MAX_ADDRESSES_PER_WEB <= psw->cExtraAddresses)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                ExitOnFailure(hr, "Failure to get more extra web addresses, max exceeded.");
            }

            hr = WcaGetRecordString(hRecAddresses, waqAddress, &pwzData);
            ExitOnFailure(hr, "Failed to get extra web Address");

            // if this isn't the key address add it
            if (0 != lstrcmpW(pwzData, psw->swaKey.wzKey))
            {
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey), pwzData);
                ExitOnFailure(hr, "Failed to copy extra addresses key string to web object");

                hr = WcaGetRecordString(hRecAddresses, waqIP, &pwzData);
                ExitOnFailure(hr, "Failed to get extra web IP");
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP), pwzData);
                ExitOnFailure(hr, "Failed to copy extra addresses IP string to web object");

                hr = WcaGetRecordString(hRecAddresses, waqPort, &pwzData);
                ExitOnFailure(hr, "Failed to get port for extra web IP");
                psw->swaExtraAddresses[psw->cExtraAddresses].iPort= wcstol(pwzData, NULL, 10);

                hr = WcaGetRecordString(hRecAddresses, waqHeader, &pwzData);
                ExitOnFailure(hr, "Failed to get header for extra web IP");
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader), pwzData);
                ExitOnFailure(hr, "Failed to copy extra addresses header string to web object");

                hr = WcaGetRecordInteger(hRecAddresses, waqSecure, &psw->swaExtraAddresses[psw->cExtraAddresses].fSecure);
                ExitOnFailure(hr, "Failed to get if secure extra web IP");
                if (S_FALSE == hr)
                    psw->swaExtraAddresses[psw->cExtraAddresses].fSecure = FALSE;

                ++psw->cExtraAddresses;
            }
        }

        if (E_NOMOREITEMS == hr)
            hr = S_OK;
        ExitOnFailure(hr, "Failure occured while getting extra web addresses");

        hr = WcaGetRecordInteger(hRec, wqConnectionTimeout, &psw->iConnectionTimeout);
        ExitOnFailure(hr, "Failed to get connection timeout for Web");

        if (psw->fHasComponent) // If we're installing it, it needs a dir
        {
            // get the web's directory
            if (INSTALLSTATE_SOURCE == psw->isAction)
            {
                hr = WcaGetRecordString(hRec, wqSourcePath, &pwzData);
            }
            else
            {
                hr = WcaGetRecordString(hRec, wqTargetPath, &pwzData);
            }
            ExitOnFailure(hr, "Failed to get Source/TargetPath for Directory");

            // remove trailing backslashes
            while (lstrlenW(pwzData) > 0 && pwzData[lstrlenW(pwzData)-1] == L'\\')
            {
                pwzData[lstrlenW(pwzData)-1] = 0;
            }
            hr = ::StringCchCopyW(psw->wzDirectory, countof(psw->wzDirectory), pwzData);
            ExitOnFailure(hr, "Failed to copy directory string to web object");
        }

        hr = WcaGetRecordInteger(hRec, wqState, &psw->iState);
        ExitOnFailure(hr, "Failed to get state for Web");

        hr = WcaGetRecordInteger(hRec, wqAttributes, &psw->iAttributes);
        ExitOnFailure(hr, "Failed to get attributes for Web");

        // get the dir properties for this web
        hr = WcaGetRecordString(hRec, wqProperties, &pwzData);
        ExitOnFailure(hr, "Failed to get directory property record for Web");
        if (*pwzData)
        {
            hr = ScaGetWebDirProperties(pwzData, hUserQuery, hWebDirPropQuery, &psw->swp);
            ExitOnFailure(hr, "Failed to get directory properties for Web");

            psw->fHasProperties = TRUE;
        }

        // get the application information for this web
        hr = WcaGetRecordString(hRec, wqApplication, &pwzData);
        ExitOnFailure(hr, "Failed to get application identifier for Web");
        if (*pwzData)
        {
            hr = ScaGetWebApplication(NULL, pwzData, hWebAppQuery, hWebAppExtQuery, &psw->swapp);
            ExitOnFailure(hr, "Failed to get application for Web");

            psw->fHasApplication = TRUE;
        }

        // get the SSL certificates
        hr = ScaSslCertificateRead(psw->wzKey, hSslCertQuery, &(psw->pswscList));
        ExitOnFailure(hr, "Failed to get SSL Certificates.");

        // get the custom headers
        if (*ppshhList)
        {
            hr = ScaGetHttpHeader(hhptWeb, psw->wzKey, ppshhList, &(psw->pshhList));
            ExitOnFailure(hr, "Failed to get Custom HTTP Headers");
        }

        // get the errors
        if (*ppsweList)
        {
            hr = ScaGetWebError(weptWeb, psw->wzKey, ppsweList, &(psw->psweList));
            ExitOnFailure(hr, "Failed to get Custom Errors");
        }

        // get the log information for this web
        hr = WcaGetRecordString(hRec, wqLog, &pwzData);
        ExitOnFailure(hr, "Failed to get log identifier for Web");
        if (*pwzData)
        {
            hr = ScaGetWebLog(piMetabase, pwzData, hWebLogQuery, &psw->swl);
            ExitOnFailure(hr, "Failed to get Log for Web.");

            psw->fHasLog = TRUE;
        }

        *ppswList = AddWebToList(*ppswList, psw);
        psw = NULL; // set the web NULL so it doesn't accidentally get freed below
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    // if anything was left over after an error clean it all up
    WcaFinishUnwrapQuery(hQueryWebSite);
    WcaFinishUnwrapQuery(hQueryWebAddress);

    if (psw)
    {
        ScaWebsFreeList(psw);
    }

    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaWebsGetBase(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in LPCWSTR wzWeb,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase,
    __in WCA_WRAPQUERY_HANDLE hWrapQuery
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec;

    int iSiteId;
    WCHAR wzIP[MAX_PATH];
    int iPort = -1;
    WCHAR wzHeader[MAX_PATH];
    BOOL fSecure = FALSE;

    LPWSTR pwzData = NULL;

    // get the web information
    WcaFetchWrappedReset(hWrapQuery);

    hr = WcaFetchWrappedRecordWhereString(hWrapQuery, 1, wzWeb, &hRec);
    if (S_OK == hr)
    {
        // get the data to search for
        hr = WcaGetRecordInteger(hRec, wbqId, &iSiteId);
        ExitOnFailure(hr, "Failed to get SiteId for Web for VirtualDir");

        hr = WcaGetRecordString(hRec, wbqIP, &pwzData);
        ExitOnFailure(hr, "Failed to get IP for Web for VirtualDir");
        hr = ::StringCchCopyW(wzIP, countof(wzIP), pwzData);
        ExitOnFailure(hr, "Failed to copy IP for Web for VirtualDir");

        hr = WcaGetRecordString(hRec, wbqPort, &pwzData);
        ExitOnFailure(hr, "Failed to get port for extra web IP");
        iPort = wcstol(pwzData, NULL, 10);

        hr = WcaGetRecordString(hRec, wbqHeader, &pwzData);
        ExitOnFailure(hr, "Failed to get Header for Web for VirtualDir");
        hr = ::StringCchCopyW(wzHeader, countof(wzHeader), pwzData);
        ExitOnFailure(hr, "Failed to copy Header for Web for VirtualDir");

        hr = WcaGetRecordInteger(hRec, wbqSecure, &fSecure);
        if (S_FALSE == hr)
            fSecure = FALSE;

        hr = WcaGetRecordString(hRec, wbqDescription, &pwzData);
        ExitOnFailure(hr, "Failed to get Description for Web");

        // find the web or find the next free web location
        hr = ScaWebFindBase(piMetabase, pswList, wzWeb, iSiteId, wzIP, iPort, wzHeader, fSecure, pwzData, wzWebBase, cchWebBase);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        ExitOnFailure(hr, "Failed to find Web base");
    }
    else if (S_FALSE == hr)
        hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);

    // Let's check that there isn't more than one record found - if there is, throw an assert like WcaFetchSingleRecord() would
    HRESULT hrTemp = WcaFetchWrappedRecordWhereString(hWrapQuery, 1, wzWeb, &hRec);
    if (SUCCEEDED(hrTemp))
    {
        AssertSz(E_NOMOREITEMS == hrTemp, "ScaWebsGetBase found more than one record");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaWebsInstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in SCA_APPPOOL * psapList
    )
{
    HRESULT hr = S_OK;
    SCA_WEB* psw = pswList;

    while (psw)
    {
        // if we are installing the web site
        if (psw->fHasComponent && WcaIsInstalling(psw->isInstalled, psw->isAction))
        {
            hr = ScaWebWrite(piMetabase, psw, psapList);
            ExitOnFailure1(hr, "failed to write web '%ls' to metabase", psw->wzKey);
        }

        psw = psw->pswNext;
    }

LExit:
    return hr;
}


HRESULT ScaWebsUninstall(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList
    )
{
    HRESULT hr = S_OK;
    SCA_WEB* psw = pswList;

    while (psw)
    {
        // if we are uninstalling the web site
        if (psw->fHasComponent && WcaIsUninstalling(psw->isInstalled, psw->isAction))
        {
            hr = ScaWebRemove(piMetabase, psw);
            ExitOnFailure1(hr, "Failed to remove web '%ls' from metabase", psw->wzKey);
        }

        psw = psw->pswNext;
    }

LExit:
    return hr;
}


void ScaWebsFreeList(
    __in SCA_WEB* pswList
    )
{
    SCA_WEB* pswDelete = pswList;
    while (pswList)
    {
        pswDelete = pswList;
        pswList = pswList->pswNext;

        // Free the SSL, headers and errors list first
        FreeWeb(pswDelete);
    }
}


// private helper functions

static void FreeWeb(SCA_WEB *pswDelete)
{
    ScaSslCertificateFreeList(pswDelete->pswscList);
    ScaHttpHeaderFreeList(pswDelete->pshhList);
    ScaWebErrorFreeList(pswDelete->psweList);
    MemFree(pswDelete);
}

static SCA_WEB* NewWeb()
{
    SCA_WEB* psw = static_cast<SCA_WEB*>(MemAlloc(sizeof(SCA_WEB), TRUE));
    Assert(psw);
    return psw;
}


static SCA_WEB* AddWebToList(
    __in SCA_WEB* pswList,
    __in SCA_WEB* psw
    )
{
    if (pswList)
    {
        SCA_WEB* pswTemp = pswList;
        while (pswTemp->pswNext)
        {
            pswTemp = pswTemp->pswNext;
        }

        pswTemp->pswNext = psw;
    }
    else
    {
        pswList = psw;
    }

    return pswList;
}


static HRESULT ScaWebFindBase(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* pswList,
    __in_z LPCWSTR wzWeb,
    __in int iSiteId,
    __in_z LPCWSTR wzIP,
    __in int iPort,
    __in_z LPCWSTR wzHeader,
    __in BOOL fSecure,
    __in_z LPCWSTR wzDescription,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;

    WCHAR wzKey[METADATA_MAX_NAME_LEN];
    WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
    LPWSTR wzFoundKey = NULL;

    METADATA_RECORD mr;
    ::ZeroMemory(&mr, sizeof(mr));

    METADATA_RECORD mrCompare;
    ::ZeroMemory(&mrCompare, sizeof(mrCompare));

    // try to find the web in memory first
    for (SCA_WEB* psw = pswList; psw; psw = psw->pswNext)
    {
        if (0 == lstrcmpW(wzWeb, psw->wzKey))
        {
            if ((0 == lstrcmpW(wzIP, psw->swaKey.wzIP) || 0 == lstrcmpW(wzIP, L"*") || 0 == lstrcmpW(psw->swaKey.wzIP, L"*")) &&
                iPort == psw->swaKey.iPort &&
                0 == lstrcmpW(wzHeader, psw->swaKey.wzHeader) &&
                fSecure == psw->swaKey.fSecure)
            {
                if (0 == lstrlenW(psw->wzWebBase))
                {
                    WcaLog(LOGMSG_STANDARD, "A matching web object in memory was found, but the web object in memory has no associated base");
                    ExitFunction1(hr = S_FALSE);
                }

                hr = ::StringCchCopyW(wzKey, countof(wzKey), psw->wzWebBase);
                ExitOnFailure(hr, "Failed to copy web base into key.");

                wzFoundKey = wzKey;
                break;
            }
            else
            {
                WcaLog(LOGMSG_STANDARD, "Found web `%ls` but data did not match.", wzWeb);
                hr = E_UNEXPECTED;
                break;
            }
        }
    }
    ExitOnFailure(hr, "Failure occured while searching for web in memory");

    // If we didn't find a web in memory matching look in the metabase.
    if (!wzFoundKey)
    {
        mr.dwMDIdentifier = MD_KEY_TYPE;
        mr.dwMDAttributes = METADATA_INHERIT;
        mr.dwMDUserType = IIS_MD_UT_SERVER;
        mr.dwMDDataType = ALL_METADATA;
        mr.dwMDDataLen = 0;
        mr.pbMDData = NULL;

        // If we're looking based on the old WebAddress detection (NULL) or the new
        // Web description detection (-1)
        if (MSI_NULL_INTEGER == iSiteId || -1 == iSiteId)
        {
            if (MSI_NULL_INTEGER == iSiteId)
            {
                mrCompare.dwMDIdentifier = (fSecure) ? MD_SECURE_BINDINGS : MD_SERVER_BINDINGS;
            }
            else
            {
                mrCompare.dwMDIdentifier = MD_SERVER_COMMENT;
            }
            mrCompare.dwMDAttributes = METADATA_INHERIT;
            mrCompare.dwMDUserType = IIS_MD_UT_SERVER;
            mrCompare.dwMDDataType = ALL_METADATA;
            mrCompare.dwMDDataLen = 0;
            mrCompare.pbMDData = NULL;

            // Loop through the "web keys" looking for the "IIsWebServer" key that matches our search criteria.
            for (DWORD dwIndex = 0; SUCCEEDED(hr); ++dwIndex)
            {
                hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex);
                if (SUCCEEDED(hr))
                {
                    hr = ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);
                    ExitOnFailure(hr, "Failed to copy web subkey.");

                    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);
                    if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
                    {
                        hr = S_FALSE;  // didn't find anything, try next one
                        continue;
                    }
                    ExitOnFailure(hr, "Failed to get key from metabase while searching for web servers");

                    // If we have an IIsWebServer check to see if this is the site we are searching for.
                    if (0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
                    {
                        hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mrCompare);
                        if (MD_ERROR_DATA_NOT_FOUND == hr || NULL == mrCompare.pbMDData)
                        {
                            hr = S_FALSE;
                            continue;
                        }
                        ExitOnFailure(hr, "Failed to get address/description from metabase while searching for web servers");

                        LPWSTR pwzExists = (LPWSTR)mrCompare.pbMDData;
                        if (MSI_NULL_INTEGER == iSiteId)
                        {
                            // Break down the address into its constituent parts (IP:Port:Header).
                            while (S_OK == hr && *pwzExists)
                            {
                                LPCWSTR pwzIPExists = pwzExists;
                                pwzExists = const_cast<LPWSTR>(wcsstr(pwzIPExists, L":"));
                                ExitOnNull(pwzExists, hr, E_INVALIDARG, "Invalid web address. IP was not separated by a colon.");
                                *pwzExists = L'\0';

                                LPCWSTR pwzPortExists = pwzExists + 1;
                                pwzExists = const_cast<LPWSTR>(wcsstr(pwzPortExists, L":"));
                                ExitOnNull(pwzExists, hr, E_INVALIDARG, "Invalid web address. Port was not separated by a colon.");
                                *pwzExists = L'\0';
                                int iPortExists = wcstol(pwzPortExists, NULL, 10);

                                LPCWSTR pwzHeaderExists = pwzExists + 1;

                                // compare the passed in address with the address listed for this web
                                if ((0 == lstrcmpW(wzIP, pwzIPExists) || 0 == lstrcmpW(wzIP, L"*")) &&
                                    iPort == iPortExists &&
                                    0 == lstrcmpW(wzHeader, pwzHeaderExists))
                                {
                                    wzFoundKey = wzKey;
                                    break;
                                }

                                // move to the next block of data, this may move beyond the available
                                // data and exit the while loop above.
                                pwzExists = const_cast<LPWSTR>(pwzHeaderExists + lstrlenW(pwzHeaderExists) + 1);
                            }
                        }
                        else
                        {
                            if (0 == lstrcmpW(wzDescription, pwzExists))
                            {
                                wzFoundKey = wzKey;
                            }
                        }

                        // If we found the key we were looking for, no point in continuing to search.
                        if (wzFoundKey)
                        {
                            break;
                        }
                    }
                }
            }

            if (E_NOMOREITEMS == hr)
            {
                Assert(!wzFoundKey);
                hr = S_FALSE;
            }
        }
        else // we're looking a specific SiteId
        {
            hr = ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%d", iSiteId);
            ExitOnFailure(hr, "Failed to allocate metabase key string.");

            // if we have an IIsWebServer store the key
            hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);

            if (SUCCEEDED(hr) && NULL != mr.pbMDData && 0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
            {
                wzFoundKey = wzKey;
            }
            else if (MD_ERROR_DATA_NOT_FOUND == hr || NULL == mrCompare.pbMDData || NULL == mr.pbMDData)
            {
                hr = S_FALSE;
            }
        }
    }

    if (wzFoundKey)
    {
        Assert(S_OK == hr);

        hr = ::StringCchCopyW(wzWebBase, cchWebBase, wzFoundKey);
        ExitOnFailure(hr, "Buffer not larger enough for found base key.");
    }

LExit:
    MetaFreeValue(&mr);
    MetaFreeValue(&mrCompare);

    if (!wzFoundKey && SUCCEEDED(hr))
    {
        hr = S_FALSE;
    }

    return hr;
}


static HRESULT ScaWebFindFreeBase(
    __in IMSAdminBase* piMetabase,
    __in_xcount(unknown) SCA_WEB* pswList,
    __in int iSiteId,
    __in_z LPCWSTR wzDescription,
    __out_ecount(cchWebBase) LPWSTR wzWebBase,
    __in DWORD cchWebBase
    )
{
    Assert(piMetabase);

    HRESULT hr = S_OK;

    WCHAR wzKey[METADATA_MAX_NAME_LEN];
    WCHAR wzSubkey[METADATA_MAX_NAME_LEN];
    DWORD* prgdwSubKeys = NULL;
    DWORD cSubKeys = 128;
    DWORD cSubKeysFilled = 0;

    DWORD dwKey;

    METADATA_RECORD mr;
    ::ZeroMemory(&mr, sizeof(METADATA_RECORD));
    mr.dwMDIdentifier = MD_KEY_TYPE;
    mr.dwMDAttributes = 0;
    mr.dwMDUserType = IIS_MD_UT_SERVER;
    mr.dwMDDataType = STRING_METADATA;
    mr.dwMDDataLen = 0;
    mr.pbMDData = NULL;

    if (MSI_NULL_INTEGER == iSiteId || -1 == iSiteId)
    {
        prgdwSubKeys = static_cast<DWORD*>(MemAlloc(cSubKeys * sizeof(DWORD), TRUE));
        ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");

        // loop through the "web keys" looking for the "IIsWebServer" key that matches wzWeb
        for (DWORD dwIndex = 0; SUCCEEDED(hr); ++dwIndex)
        {
            hr = piMetabase->EnumKeys(METADATA_MASTER_ROOT_HANDLE, L"/LM/W3SVC", wzSubkey, dwIndex);
            if (SUCCEEDED(hr))
            {
                hr = ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/W3SVC/%s", wzSubkey);
                ExitOnFailure(hr, "Failed remember key.");

                hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mr);
                if (MD_ERROR_DATA_NOT_FOUND == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
                {
                    hr = S_FALSE;  // didn't find anything, try next one
                    continue;
                }
                ExitOnFailure(hr, "Failed to get key from metabase while searching for free web root");

                // if we have a IIsWebServer get the address information
                if (0 == lstrcmpW(L"IIsWebServer", (LPCWSTR)mr.pbMDData))
                {
                    if (cSubKeysFilled >= cSubKeys)
                    {
                        cSubKeys = cSubKeys * 2;
                        prgdwSubKeys = static_cast<DWORD*>(MemReAlloc(prgdwSubKeys, cSubKeys * sizeof(DWORD), FALSE));
                        ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");
                    }

                    prgdwSubKeys[cSubKeysFilled] = wcstol(wzSubkey, NULL, 10);
                    ++cSubKeysFilled;
                    Sort(prgdwSubKeys, cSubKeysFilled);
                }
            }
        }

        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to find free web root");

        // Add all the webs created in memory.
        CONST WCHAR *pcchSlash;
        for (SCA_WEB* psw = pswList; psw; psw = psw->pswNext)
        {
            // Don't process webs that don't have a base
            if (!*psw->wzWebBase)
            {
                continue;
            }

            // find the last slash in the web root because the root # is after it
            pcchSlash = NULL;
            for (CONST WCHAR *pcch = psw->wzWebBase; pcch && *pcch; ++pcch)
            {
                if (L'/' == *pcch)
                {
                    pcchSlash = pcch;
                }
            }
            // In case we don't find a slash, error out
            ExitOnNull1(pcchSlash, hr, E_INVALIDARG, "Failed to find a slash in the web root: %ls", psw->wzWebBase);

            prgdwSubKeys[cSubKeysFilled] = wcstol(pcchSlash + 1, NULL, 10);
            ++cSubKeysFilled;
            Sort(prgdwSubKeys, cSubKeysFilled);

            if (cSubKeysFilled >= cSubKeys)
            {
                cSubKeys = cSubKeys * 2;
                prgdwSubKeys = static_cast<DWORD*>(MemReAlloc(prgdwSubKeys, cSubKeys * sizeof(DWORD), FALSE));
                ExitOnNull(prgdwSubKeys, hr, E_OUTOFMEMORY, "failed to allocate space for web site keys");
            }
        }

        // Find the lowest free web root.
        dwKey = (-1 == iSiteId) ? SiteIdFromDescription(wzDescription) : 1;
        for (DWORD i = 0; i < cSubKeysFilled; ++i)
        {
            if (dwKey == prgdwSubKeys[i])
            {
                ++dwKey;
            }
            else if (dwKey < prgdwSubKeys[i])
            {
                break;
            }
        }
    }
    else
    {
        dwKey = iSiteId;
    }

    hr = ::StringCchPrintfW(wzWebBase, cchWebBase, L"/LM/W3SVC/%u", dwKey);
    ExitOnFailure1(hr, "failed to format web base with key: %u", dwKey);

LExit:
    MetaFreeValue(&mr);

    if (prgdwSubKeys)
    {
        MemFree(prgdwSubKeys);
    }

    return hr;
}


static HRESULT ScaWebWrite(
    __in IMSAdminBase* piMetabase,
    __in SCA_WEB* psw,
    __in SCA_APPPOOL * psapList)
{
    HRESULT hr = S_OK;

    UINT ui = 0;
    WCHAR wzIP[64];
    WCHAR wzBindings[1024];
    WCHAR wzSecureBindings[1024];
    WCHAR* pcchNext;        // used to properly create the MULTI_SZ
    DWORD cchPcchNext;
    WCHAR* pcchSecureNext ; // used to properly create the MULTI_SZ
    DWORD cchPcchSecureNext;

    // if the web root doesn't exist create it
    if (!psw->fBaseExists)
    {
        hr = ScaCreateWeb(piMetabase, psw->wzKey, psw->wzWebBase);
        ExitOnFailure(hr, "Failed to create web");
    }
    else if (psw->iAttributes & SWATTRIB_NOCONFIGUREIFEXISTS) // if we're not supposed to configure existing webs, bail
    {
        Assert(psw->fBaseExists);

        hr = S_FALSE;
        WcaLog(LOGMSG_VERBOSE, "Skipping configuration of existing web: %ls", psw->wzKey);
        ExitFunction();
    }

    // put the secure and non-secure bindings together as MULTI_SZs
    ::ZeroMemory(wzBindings, sizeof(wzBindings));
    pcchNext = wzBindings;
    cchPcchNext = countof(wzBindings);
    ::ZeroMemory(wzSecureBindings, sizeof(wzSecureBindings));
    pcchSecureNext = wzSecureBindings;
    cchPcchSecureNext = countof(wzSecureBindings);

    // set the IP address appropriately
    if (0 == lstrcmpW(psw->swaKey.wzIP, L"*"))
    {
        ::ZeroMemory(wzIP, sizeof(wzIP));
    }
    else
    {
        hr = ::StringCchCopyW(wzIP, countof(wzIP), psw->swaKey.wzIP);
        ExitOnFailure(hr, "Failed to copy IP string");
    }

    WCHAR wzBinding[256];
    hr = ::StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaKey.iPort, psw->swaKey.wzHeader);
    ExitOnFailure(hr, "Failed to format IP:Port:Header binding string");
    if (psw->swaKey.fSecure)
    {
        hr = ::StringCchCopyW(pcchSecureNext, cchPcchSecureNext, wzBinding);
        ExitOnFailure(hr, "Failed to copy binding string to securenext string");
        pcchSecureNext += lstrlenW(wzBinding) + 1;
        cchPcchSecureNext -= lstrlenW(wzBinding) + 1;
    }
    else
    {
        hr = ::StringCchCopyW(pcchNext, cchPcchNext, wzBinding);
        ExitOnFailure(hr, "Failed to copy binding string to next string");
        pcchNext += lstrlenW(wzBinding) + 1;
        cchPcchNext -= lstrlenW(wzBinding) + 1;
    }

    for (ui = 0; ui < psw->cExtraAddresses; ++ui)
    {
        // set the IP address appropriately
        if (0 == lstrcmpW(psw->swaExtraAddresses[ui].wzIP, L"*"))
        {
            ::ZeroMemory(wzIP, sizeof(wzIP));
        }
        else
        {
            hr = ::StringCchCopyW(wzIP, countof(wzIP), psw->swaExtraAddresses[ui].wzIP);
            ExitOnFailure(hr, "Failed to copy extra addresses IP to IP string");
        }

        hr = ::StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaExtraAddresses[ui].iPort, psw->swaExtraAddresses[ui].wzHeader);
        ExitOnFailure(hr, "Failed to format IP:Port:Header binding string for extra address");
        if (psw->swaExtraAddresses[ui].fSecure)
        {
            hr = ::StringCchCopyW(pcchSecureNext, cchPcchSecureNext, wzBinding);
            ExitOnFailure(hr, "Failed to copy binding string to securenext string for extra address");
            pcchSecureNext += lstrlenW(wzBinding) + 1;
            cchPcchSecureNext -= lstrlenW(wzBinding) + 1;
        }
        else
        {
            hr = ::StringCchCopyW(pcchNext, cchPcchNext, wzBinding);
            ExitOnFailure(hr, "Failed to copy binding string to next string for extra address");
            pcchNext += lstrlenW(wzBinding) + 1;
            cchPcchNext -= lstrlenW(wzBinding) + 1;
        }
    }

    // Delete the existing secure bindings metabase value, as having one while SSLCertHash and SSLStoreName aren't both set correctly can result in
    // 0x80070520 (ERROR_NO_SUCH_LOGON_SESSION) errors in some situations on IIS7. Clearing this value first and then setting it after the install has completed
    // allows the two aforementioned properties to exist in an intermediate state without errors
    hr = ScaDeleteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SECURE_BINDINGS, MULTISZ_METADATA);
    ExitOnFailure(hr, "Failed to temporarily delete secure bindings for Web");

    // now write the bindings to the metabase
    hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_BINDINGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, MULTISZ_METADATA, wzBindings);
    ExitOnFailure(hr, "Failed to write server bindings for Web");

    // write the target path for the web's directory to the metabase
    hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"/Root", MD_VR_PATH, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, psw->wzDirectory);
    ExitOnFailure(hr, "Failed to write virtual root path for Web");

    // write the description for the web to the metabase
    hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMENT, METADATA_INHERIT, IIS_MD_UT_SERVER, STRING_METADATA, psw->wzDescription);
    ExitOnFailure(hr, "Failed to write description for Web");

    ui = psw->iConnectionTimeout;
    if (MSI_NULL_INTEGER != ui)
    {
        hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_CONNECTION_TIMEOUT, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
        ExitOnFailure(hr, "Failed to write connection timeout for Web");
    }

    ui = psw->iState;
    if (MSI_NULL_INTEGER != ui)
    {
        if (2 == ui)
        {
            ui = 1;
            hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_AUTOSTART, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
            ExitOnFailure(hr, "Failed to write auto start flag for Web");
            ui = 2;
        }

        if (1 == ui || 2 == ui)
        {
            ui = 1; // start command
            hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMAND, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
            ExitOnFailure(hr, "Failed to start Web");
        }
        else if (0 == ui)
        {
            ui = 2; // stop command
            hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SERVER_COMMAND, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)ui));
            ExitOnFailure(hr, "Failed to stop Web");
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected value for Web State");
        }
    }

    WCHAR wzRootOfWeb[METADATA_MAX_NAME_LEN];
    hr = ::StringCchPrintfW(wzRootOfWeb, countof(wzRootOfWeb), L"%s/Root", psw->wzWebBase);
    ExitOnFailure(hr, "Failed to allocate WebBase/Root string for root of web");

    // write the web dirproperties information
    if (psw->fHasProperties)
    {
        hr = ScaWriteWebDirProperties(piMetabase, wzRootOfWeb, &psw->swp);
        ExitOnFailure(hr, "Failed to write web security information to metabase");
    }

    // write the application information
    if (psw->fHasApplication)
    {
        // On reinstall, we have to uninstall the old application, otherwise a duplicate will be created
        if (WcaIsReInstalling(psw->isInstalled, psw->isAction))
        {
            hr = ScaDeleteApp(piMetabase, wzRootOfWeb);
            ExitOnFailure(hr, "Failed to remove application for WebDir as part of a reinstall");
        }

        hr = ScaWriteWebApplication(piMetabase, wzRootOfWeb, &psw->swapp, psapList);
        ExitOnFailure(hr, "Failed to write web application information to metabase");
    }

    // write the SSL certificate information
    if (psw->pswscList)
    {
        hr = ScaSslCertificateWriteMetabase(piMetabase, psw->wzWebBase, psw->pswscList);
        ExitOnFailure1(hr, "Failed to write SSL certificates for Web site: %ls", psw->wzKey);
    }

    hr = ScaWriteMetabaseValue(piMetabase, psw->wzWebBase, L"", MD_SECURE_BINDINGS, METADATA_NO_ATTRIBUTES, IIS_MD_UT_SERVER, MULTISZ_METADATA, wzSecureBindings);
    ExitOnFailure(hr, "Failed to write secure bindings for Web");

    // write the headers
    if (psw->pshhList)
    {
        hr = ScaWriteHttpHeader(piMetabase, wzRootOfWeb, psw->pshhList);
        ExitOnFailure1(hr, "Failed to write custom HTTP headers for Web site: %ls", psw->wzKey);
    }

    // write the errors
    if (psw->psweList)
    {
        hr = ScaWriteWebError(piMetabase, weptWeb, psw->wzWebBase, psw->psweList);
        ExitOnFailure1(hr, "Failed to write custom web errors for Web site: %ls", psw->wzKey);
    }

    // write the mimetypes
    if (psw->psmm)
    {
        hr = ScaWriteMimeMap(piMetabase, wzRootOfWeb, psw->psmm);
        ExitOnFailure1(hr, "Failed to write mimemap for Web site: %ls", psw->wzKey);
    }

    // write the log information to the metabase
    if (psw->fHasLog)
    {
        hr = ScaWriteWebLog(piMetabase, psw->wzWebBase, &psw->swl);
        ExitOnFailure(hr, "Failed to write web log information to metabase");
    }

LExit:
    return hr;
}


static HRESULT ScaWebRemove(
    __in IMSAdminBase* piMetabase,
    __in const SCA_WEB* psw
    )
{
    HRESULT hr = S_OK;

    // simply remove the root key and everything else is pulled at the same time
    hr = ScaDeleteMetabaseKey(piMetabase, psw->wzWebBase, L"");
    ExitOnFailure1(hr, "Failed to remove web '%ls' from metabase", psw->wzKey);

LExit:
    return hr;
}


static DWORD SiteIdFromDescription(
    __in_z LPCWSTR wzDescription
    )
{
    LPCWSTR pwz = wzDescription;
    DWORD dwSiteId = 0;
    while (pwz && *pwz)
    {
        WCHAR ch = *pwz & 0xdf;
        dwSiteId = (dwSiteId * 101) + ch;
        ++pwz;
    }

    return (dwSiteId % INT_MAX) + 1;
}


// insertion sort
static void Sort(
    __in_ecount(cArray) DWORD dwArray[],
    __in int cArray
    )
{
    int i, j;
    DWORD dwData;

    for (i = 1; i < cArray; ++i)
    {
        dwData = dwArray[i];

        j = i - 1;
        while (0 <= j && dwArray[j] > dwData)
        {
            dwArray[j + 1] = dwArray[j];
            j--;
        }

        dwArray[j + 1] = dwData;
    }
}
