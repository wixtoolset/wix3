// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

//Adding this because delivery doesn't have the updated specstrings.h that windows build does
#ifndef __in_xcount
#define __in_xcount(size)
#endif

// prototypes for private helper functions
static SCA_WEB7* NewWeb7();
static SCA_WEB7* AddWebToList7(
    __in SCA_WEB7* pswList,
    __in SCA_WEB7* psw
    );

static HRESULT ScaWebFindBase7(
    __in SCA_WEB7* pswList,
    LPCWSTR wzDescription
    );

static HRESULT ScaWebWrite7(
    __in SCA_WEB7* psw,
    __in SCA_APPPOOL * psapList
    );

static HRESULT ScaWebRemove7(__in const SCA_WEB7* psw);


HRESULT ScaWebsRead7(
    __in SCA_WEB7** ppswList,
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
    Assert(ppswList);
    WcaLog(LOGMSG_VERBOSE, "Entering ScaWebsRead7()");

    HRESULT hr = S_OK;

    MSIHANDLE hRec;
    MSIHANDLE hRecAddresses;

    WCA_WRAPQUERY_HANDLE hQueryWebSite = NULL;
    WCA_WRAPQUERY_HANDLE hQueryWebAddress = NULL;

    SCA_WEB7* psw = NULL;
    LPWSTR pwzData = NULL;

    DWORD dwLen = 0;
    errno_t error = EINVAL;

    // check to see what tables are available
    hr = WcaBeginUnwrapQuery(&hQueryWebSite, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebsRead");

    hr = WcaBeginUnwrapQuery(&hQueryWebAddress, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap query for ScaWebsRead");


    if (0 == WcaGetQueryRecords(hQueryWebSite) || 0 == WcaGetQueryRecords(hQueryWebAddress))
    {
        WcaLog(LOGMSG_VERBOSE, "Required tables not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the webs
    while (S_OK == (hr = WcaFetchWrappedRecord(hQueryWebSite, &hRec)))
    {
        psw = NewWeb7();
        if (!psw)
        {
            hr = E_OUTOFMEMORY;
            break;
        }

        // get the darwin information
        hr = WcaGetRecordString(hRec, wqWeb, &pwzData);
        ExitOnFailure(hr, "Failed to get Web");
        hr = ::StringCchCopyW(psw->wzKey, countof(psw->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to web object");

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
        }

        // Get the web's description.
        hr = WcaGetRecordString(hRec, wqDescription, &pwzData);
        ExitOnFailure(hr, "Failed to get Description for Web");
        hr = ::StringCchCopyW(psw->wzDescription, countof(psw->wzDescription), pwzData);
        ExitOnFailure(hr, "Failed to copy description string to web object");

        //get web's site Id
        hr = WcaGetRecordInteger(hRec, wqId, &psw->iSiteId);
        ExitOnFailure(hr, "Failed to get SiteId for Web");

        // get the web's key address (Bindings)
        hr = WcaGetRecordString(hRec, wqAddress, &pwzData);
        ExitOnFailure(hr, "Failed to get Address for Web");
        hr = ::StringCchCopyW(psw->swaBinding.wzKey, countof(psw->swaBinding.wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy web binding key");

        hr = WcaGetRecordString(hRec, wqIP, &pwzData);
        ExitOnFailure(hr, "Failed to get IP for Web");
        hr = ::StringCchCopyW(psw->swaBinding.wzIP, countof(psw->swaBinding.wzIP), pwzData);
        ExitOnFailure(hr, "Failed to copy web IP");

        hr = WcaGetRecordString(hRec, wqPort, &pwzData);
        ExitOnFailure(hr, "Failed to get Web Address port");
        psw->swaBinding.iPort = wcstol(pwzData, NULL, 10);

        hr = WcaGetRecordString(hRec, wqHeader, &pwzData);
        ExitOnFailure(hr, "Failed to get Header for Web");
        hr = ::StringCchCopyW(psw->swaBinding.wzHeader, countof(psw->swaBinding.wzHeader), pwzData);
        ExitOnFailure(hr, "Failed to copy web header");

        hr = WcaGetRecordInteger(hRec, wqSecure, &psw->swaBinding.fSecure);
        ExitOnFailure(hr, "Failed to get if Web is secure");
        if (S_FALSE == hr)
        {
            psw->swaBinding.fSecure = FALSE;
        }

        // look to see if site exists
        dwLen = METADATA_MAX_NAME_LEN;
        hr = ScaWebFindBase7(*ppswList, psw->wzDescription);

        // If we didn't find a web in memory, ignore it - during execute CA
        // if the site truly does not exist then there will be an error.
        if (S_OK == hr)
        {
            // site exists in config
            psw->fBaseExists = TRUE;
        }
        else if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        {
            hr = S_OK;

            // site does not exists in config
            psw->fBaseExists = FALSE;
        }
        ExitOnFailure(hr, "Failed to find web site");

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
            if (0 != lstrcmpW(pwzData, psw->swaBinding.wzKey))
            {
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey,
                    countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzKey), pwzData);
                ExitOnFailure(hr, "Failed to copy web binding key");

                hr = WcaGetRecordString(hRecAddresses, waqIP, &pwzData);
                ExitOnFailure(hr, "Failed to get extra web IP");
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzIP), pwzData);
                ExitOnFailure(hr, "Failed to copy web binding IP");

                hr = WcaGetRecordString(hRecAddresses, waqPort, &pwzData);
                ExitOnFailure(hr, "Failed to get port for extra web IP");
                psw->swaExtraAddresses[psw->cExtraAddresses].iPort= wcstol(pwzData, NULL, 10);

                // errno is set to ERANGE if overflow or underflow occurs
                _get_errno(&error);

                if (ERANGE == error)
                {
                    hr = E_INVALIDARG;
                    ExitOnFailure(hr, "Failed to convert web Port address");
                }

                hr = WcaGetRecordString(hRecAddresses, waqHeader, &pwzData);
                ExitOnFailure(hr, "Failed to get header for extra web IP");
                hr = ::StringCchCopyW(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader, countof(psw->swaExtraAddresses[psw->cExtraAddresses].wzHeader), pwzData);
                ExitOnFailure(hr, "Failed to copy web binding header");

                hr = WcaGetRecordInteger(hRecAddresses, waqSecure, &psw->swaExtraAddresses[psw->cExtraAddresses].fSecure);
                ExitOnFailure(hr, "Failed to get if secure extra web IP");
                if (S_FALSE == hr)
                {
                    psw->swaExtraAddresses[psw->cExtraAddresses].fSecure = FALSE;
                }

                ++psw->cExtraAddresses;
            }
        }

        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failure occured while getting extra web addresses");

        //
        // Connection time out
        //
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

            dwLen = lstrlenW(pwzData);
            // remove trailing backslash
            if (dwLen > 0 && pwzData[dwLen-1] == L'\\')
            {
                pwzData[dwLen-1] = 0;
            }
            hr = ::StringCchCopyW(psw->wzDirectory, countof(psw->wzDirectory), pwzData);
            ExitOnFailure1(hr, "Failed to copy web dir: '%ls'", pwzData);

        }

        hr = WcaGetRecordInteger(hRec, wqState, &psw->iState);
        ExitOnFailure(hr, "Failed to get state for Web");

        hr = WcaGetRecordInteger(hRec, wqAttributes, &psw->iAttributes);
        ExitOnFailure(hr, "Failed to get attributes for Web");

        // get the dir properties for this web
        hr = WcaGetRecordString(hRec, wqProperties, &pwzData);
        ExitOnFailure(hr, "Failed to get directory properties for Web");
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
            hr = ScaGetWebLog7(pwzData, hWebLogQuery, &psw->swl);
            ExitOnFailure(hr, "Failed to get Log for Web.");
            psw->fHasLog = TRUE;
        }

        *ppswList = AddWebToList7(*ppswList, psw);
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

    ScaWebsFreeList7(psw);

    ReleaseStr(pwzData);
    WcaLog(LOGMSG_VERBOSE, "Exiting ScaWebsRead7()");

    return hr;
}

BOOL CompareBinding(
    __in IAppHostElement* pBinding,
    __in LPVOID pContext
    )
{
    BOOL fFound = FALSE;
    HRESULT hr = S_OK;
    LPWSTR pwzBindingInfo = NULL;
    SCA_WEB7* psw = (SCA_WEB7*)pContext;

    hr = Iis7GetPropertyString(pBinding, IIS_CONFIG_BINDINGINFO, &pwzBindingInfo);
    ExitOnFailure(hr, "Failed to get bindinginfo for binding element");

    LPWSTR pwzExists = pwzBindingInfo;
    // Break down the address into its constituent parts (IP:Port:Header).
    // Taken from IIS6 CA code for compatibility
    while (S_OK == hr && *pwzExists)
    {
        LPCWSTR pwzIPExists = pwzExists;
        pwzExists = const_cast<LPWSTR>(wcsstr(pwzIPExists, L":"));
        if (NULL == pwzExists)
        {
            ExitFunction();
        }
        *pwzExists = L'\0';

        LPCWSTR pwzPortExists = pwzExists + 1;
        pwzExists = const_cast<LPWSTR>(wcsstr(pwzPortExists, L":"));
        if (NULL == pwzExists)
        {
            ExitFunction();
        }
        *pwzExists = L'\0';
        int iPortExists = wcstol(pwzPortExists, NULL, 10);

        LPCWSTR pwzHeaderExists = pwzExists + 1;

        BOOL fIpMatches = (0 == lstrcmpW(psw->swaBinding.wzIP, pwzIPExists));   // Explicit IP match
        fIpMatches |= (0 == lstrcmpW(psw->swaBinding.wzIP, L"*"));              // Authored * matches any IP
        fIpMatches |= ('\0' != psw->swaBinding.wzIP) &&                         // Unauthored IP
                      (0 == lstrcmpW(pwzIPExists, L"*"));                       // matches the All Unassigned IP : '*'

        // compare the passed in address with the address listed for this web
        if (fIpMatches && psw->swaBinding.iPort == iPortExists &&
            0 == lstrcmpW(psw->swaBinding.wzHeader, pwzHeaderExists))
        {
            fFound = TRUE;
            break;
        }

        // move to the next block of data, this may move beyond the available
        // data and exit the while loop above.
        pwzExists = const_cast<LPWSTR>(pwzHeaderExists + lstrlenW(pwzHeaderExists));
    }

LExit:
    WcaLog(LOGMSG_VERBOSE, "Site with binding %ls %s a match", pwzBindingInfo, fFound ? "is" : "is not");
    ReleaseNullStr(pwzBindingInfo);
    return fFound;
}

BOOL EnumSiteCompareBinding(
    __in IAppHostElement* pSite,
    __in LPVOID pContext
    )
{
    BOOL fFound = FALSE;
    HRESULT hr = S_OK;
    SCA_WEB7* psw = (SCA_WEB7*)pContext;
    IAppHostChildElementCollection *pSiteChildren = NULL;
    IAppHostElement *pBindings = NULL;
    IAppHostElementCollection *pBindingsCollection = NULL;
    IAppHostElement *pBinding = NULL;
    VARIANT vtProp;
    VariantInit(&vtProp);

    hr = pSite->get_ChildElements(&pSiteChildren);
    ExitOnFailure(hr, "Failed get site child elements collection");

    vtProp.vt = VT_BSTR;
    vtProp.bstrVal = ::SysAllocString(IIS_CONFIG_BINDINGS);
    hr = pSiteChildren->get_Item(vtProp, &pBindings);
    ExitOnFailure(hr, "Failed get bindings element");

    hr = pBindings->get_Collection(&pBindingsCollection);
    ExitOnFailure(hr, "Failed get bindings collection");

    WcaLog(LOGMSG_VERBOSE, "Searching for site with binding %ls:%d:%ls", psw->swaBinding.wzIP, psw->swaBinding.iPort, psw->swaBinding.wzHeader);

    hr = Iis7EnumAppHostElements(pBindingsCollection, CompareBinding, psw, &pBinding, NULL);
    ExitOnFailure(hr, "Failed search bindings collection");

    fFound = NULL != pBinding;
LExit:
    VariantClear(&vtProp);
    ReleaseNullObject(pSiteChildren);
    ReleaseNullObject(pBindings);
    ReleaseNullObject(pBindingsCollection);
    ReleaseNullObject(pBinding);
    return fFound;
}

HRESULT ScaWebSearch7(
    __in SCA_WEB7* psw,
    __deref_out_z_opt LPWSTR* pswWeb,
    __out_opt BOOL* pfFound
    )
{
    HRESULT hr = S_OK;
    BOOL fInitializedCom = FALSE;
    BSTR bstrSites = NULL;
    BSTR bstrAppHostRoot = NULL;
    IAppHostAdminManager *pAdminMgr = NULL;
    IAppHostElement *pSites = NULL;
    IAppHostElementCollection *pCollection = NULL;
    IAppHostElement *pSite = NULL;

    if (NULL != pswWeb)
    {
        ReleaseNullStr(*pswWeb);
    }

    if (NULL != pfFound)
    {
        *pfFound = FALSE;
    }

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM");
    fInitializedCom = TRUE;

    hr = CoCreateInstance(__uuidof(AppHostAdminManager), NULL, CLSCTX_INPROC_SERVER, __uuidof(IAppHostAdminManager), reinterpret_cast<void**> (&pAdminMgr));
    if (REGDB_E_CLASSNOTREG == hr)
    {
        WcaLog(LOGMSG_VERBOSE, "AppHostAdminManager was not registered, cannot find site.");
        hr = S_OK;
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to CoCreate IAppHostAdminManager");

    bstrSites = ::SysAllocString(IIS_CONFIG_SITES_SECTION);
    ExitOnNull(bstrSites, hr, E_OUTOFMEMORY, "Failed to allocate sites string.");

    bstrAppHostRoot = ::SysAllocString(IIS_CONFIG_APPHOST_ROOT);
    ExitOnNull(bstrAppHostRoot, hr, E_OUTOFMEMORY, "Failed to allocate host root string.");

    hr = pAdminMgr->GetAdminSection(bstrSites, bstrAppHostRoot, &pSites);
    ExitOnFailure(hr, "Failed get sites section");
    ExitOnNull(pSites, hr, ERROR_FILE_NOT_FOUND, "Failed get sites section object");

    hr = pSites->get_Collection(&pCollection);
    ExitOnFailure(hr, "Failed get sites collection");

    // not explicitly doing a Description search
    if (-1 != psw->iSiteId)
    {
        if (MSI_NULL_INTEGER == psw->iSiteId)
        {
            // Enumerate sites & determine if the binding matches
            hr = Iis7EnumAppHostElements(pCollection, EnumSiteCompareBinding, psw, &pSite, NULL);
            ExitOnFailure(hr, "Failed locate site by ID");
        }
        else
        {
            // Find a site with ID matches
            hr = Iis7FindAppHostElementInteger(pCollection, IIS_CONFIG_SITE, IIS_CONFIG_ID, psw->iSiteId, &pSite, NULL);
            ExitOnFailure(hr, "Failed locate site by ID");
        }
    }

    if (NULL == pSite)
    {
        // Find a site with Name that matches
        hr = Iis7FindAppHostElementString(pCollection, IIS_CONFIG_SITE, IIS_CONFIG_NAME, psw->wzDescription, &pSite, NULL);
        ExitOnFailure(hr, "Failed locate site by ID");
    }

    if (NULL != pSite)
    {
        if (NULL != pfFound)
        {
            *pfFound = TRUE;
        }

        if (NULL != pswWeb)
        {
            // We found a site, return its description
            hr = Iis7GetPropertyString(pSite, IIS_CONFIG_NAME, pswWeb);
            ExitOnFailure(hr, "Failed get site name");
        }
    }
LExit:
    ReleaseNullObject(pAdminMgr);
    ReleaseNullObject(pSites);
    ReleaseNullObject(pCollection);
    ReleaseNullObject(pSite);
    ReleaseBSTR(bstrAppHostRoot);
    ReleaseBSTR(bstrSites);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }
    return hr;
}


HRESULT ScaWebsGetBase7(
    __in SCA_WEB7* pswList,
    __in LPCWSTR pswWebKey,
    __out_ecount(cchDest) LPWSTR pswWeb,
    __in DWORD_PTR cchDest
    )
{
    HRESULT hr = S_OK;
    BOOL fFound = FALSE;
    SCA_WEB7* psw = pswList;
    LPWSTR wzSiteName = NULL;

    *pswWeb = '/0';

    //looking for psw->wzKey == pswWebKey
    while(psw)
    {
        if (0 == wcscmp(pswWebKey, psw->wzKey))
        {
            fFound = TRUE;
            break;
        }
        psw = psw->pswNext;
    }

    if (!fFound)
    {
        ExitFunction1(hr = S_FALSE);
    }

    // Search if we're not installing the site
    if (!psw->fHasComponent || (psw->iAttributes & SWATTRIB_NOCONFIGUREIFEXISTS))
    {
        // We are not installing this website.  Search for it in IIS config
        hr = ScaWebSearch7(psw, &wzSiteName, NULL);
        ExitOnFailure(hr, "Failed to search for Website");

        if (NULL != wzSiteName)
        {
            hr = ::StringCchCopyW(pswWeb, cchDest, wzSiteName);
            ExitOnFailure(hr, "Failed to set Website description for located Website");
        }
    }

    if ('/0' == *pswWeb)
    {
        WcaLog(LOGMSG_VERBOSE, "Could not find Web: %ls, defaulting to %ls", psw->wzKey, psw->wzDescription);
        // Default to the provided description, the Exec CA will locate by description
        hr = ::StringCchCopyW(pswWeb, cchDest, psw->wzDescription);
        ExitOnFailure(hr, "Failed to set Website description to default");
    }
LExit:
    ReleaseNullStr(wzSiteName);
    return hr;
}

HRESULT ScaWebsInstall7(
    __in SCA_WEB7* pswList,
    __in SCA_APPPOOL * psapList
    )
{
    HRESULT hr = S_OK;
    SCA_WEB7* psw = pswList;

    while (psw)
    {
        // if we are installing the web site
        if (psw->fHasComponent && WcaIsInstalling(psw->isInstalled, psw->isAction))
        {
            hr = ScaWebWrite7(psw, psapList);
            ExitOnFailure1(hr, "failed to write web '%ls' to metabase", psw->wzKey);
        }

        psw = psw->pswNext;
    }

LExit:
    return hr;
}


HRESULT ScaWebsUninstall7(
    __in SCA_WEB7* pswList
    )
{
    HRESULT hr = S_OK;
    SCA_WEB7* psw = pswList;

    while (psw)
    {
        if (psw->fHasComponent && WcaIsUninstalling(psw->isInstalled, psw->isAction))
        {
            // If someone
            hr = ScaWebRemove7(psw);
            ExitOnFailure1(hr, "Failed to remove web '%ls' ", psw->wzKey);
        }

        psw = psw->pswNext;
    }

LExit:
    return hr;
}


void ScaWebsFreeList7(__in SCA_WEB7* pswList)
{
    SCA_WEB7* pswDelete = pswList;
    while (pswList)
    {
        pswDelete = pswList;
        pswList = pswList->pswNext;

        // Free the SSL, headers and errors list first
        ScaSslCertificateFreeList(pswDelete->pswscList);
        ScaHttpHeaderFreeList(pswDelete->pshhList);
        ScaWebErrorFreeList(pswDelete->psweList);

        MemFree(pswDelete);
    }
}


// private helper functions

static SCA_WEB7* NewWeb7()
{
    SCA_WEB7* psw = (SCA_WEB7*)MemAlloc(sizeof(SCA_WEB7), TRUE);
    Assert(psw);
    return psw;
}


static SCA_WEB7* AddWebToList7(
    __in SCA_WEB7* pswList,
    __in SCA_WEB7* psw
    )
{
    if (pswList)
    {
        SCA_WEB7* pswTemp = pswList;
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


static HRESULT ScaWebFindBase7(
    __in SCA_WEB7* pswList,
    __in_z LPCWSTR wzDescription
    )
{
    HRESULT hr = S_OK;
    BOOL fFound = FALSE;

    // try to find the web in memory first
    for (SCA_WEB7* psw = pswList; psw; psw = psw->pswNext)
    {
        if (0 == wcscmp(wzDescription, psw->wzDescription))
        {
            fFound = TRUE;
            break;
        }
    }

    if (!fFound)
    {
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    return hr;
}


static HRESULT ScaWebWrite7(
    __in SCA_WEB7* psw,
    __in SCA_APPPOOL * psapList
    )
{
    HRESULT hr = S_OK;

    BOOL fExists = FALSE;
    UINT ui = 0;
    WCHAR wzIP[64];
    WCHAR wzBinding[1024];
    WCHAR wzAppPoolName[MAX_PATH];

    //
    // determine if site must be new
    //
    if (psw->iAttributes & SWATTRIB_NOCONFIGUREIFEXISTS)
    {
        // Check if site already exists.
        hr = ScaWebSearch7(psw, NULL, &fExists);
        ExitOnFailure1(hr, "Failed to search for web: %ls", psw->wzKey);

        if (fExists)
        {
            hr = S_FALSE;
            WcaLog(LOGMSG_VERBOSE, "Skipping configuration of existing web: %ls", psw->wzKey);
            ExitFunction();
        }
    }

    //create a site
    hr = ScaWriteConfigID(IIS_SITE);
    ExitOnFailure(hr, "Failed write site ID");

    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "Failed write site ID create action");

    //Site Name
    hr = ScaWriteConfigString(psw->wzDescription);                  //Site Name
    ExitOnFailure(hr, "Failed write site desc");

    //Site Id -- value is MSI_NULL_INTEGER if not set in WIX
    hr = ScaWriteConfigInteger(psw->iSiteId);                        //SiteID
    ExitOnFailure(hr, "Failed write site id value");

    //Site Auto Start -- value is MSI_NULL_INTEGER if not set in WIX
    hr = ScaWriteConfigInteger(psw->iState);                        // serverAutoStart
    ExitOnFailure(hr, "Failed write site autostart");

    hr = ScaWriteConfigInteger(psw->iConnectionTimeout);            //limits/connectionTimeout
    ExitOnFailure(hr, "Failed write site timeout");

    //create default application
    hr = ScaWriteConfigID(IIS_APPLICATION);
    ExitOnFailure(hr, "Failed write app ID");
    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "Failed write app action ID");
    hr = ScaWriteConfigString(psw->wzDescription);      //site name key
    ExitOnFailure(hr, "Failed write app desc");
    hr = ScaWriteConfigString(L"/");                    //  App Path (default)
    ExitOnFailure(hr, "Failed write app def path /");

    if (psw->fHasApplication && *psw->swapp.wzAppPool)
    {
        hr = ScaFindAppPool7(psw->swapp.wzAppPool, wzAppPoolName, countof(wzAppPoolName), psapList);
        ExitOnFailure(hr, "Failed to read app pool from application");

        hr = ScaWriteConfigString(wzAppPoolName);
        ExitOnFailure(hr, "Failed write app appPool");
    }
    else
    {
        hr = ScaWriteConfigString(L"");
        ExitOnFailure(hr, "Failed write app appPool");
    }

    //create vdir for default application
    hr = ScaWriteConfigID(IIS_VDIR);
    ExitOnFailure(hr, "Failed write vdir ID");
    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "Failed write vdir action");
    hr = ScaWriteConfigString(psw->wzDescription);      //site name key
    ExitOnFailure(hr, "Failed write vdir desc");
    hr = ScaWriteConfigString(L"/");                    //vdir path (default)
    ExitOnFailure(hr, "Failed write vdir app");
    hr = ScaWriteConfigString(psw->wzDirectory);         //physical dir
    ExitOnFailure(hr, "Failed write vdir dir");

    //create bindings for site
    hr = ScaWriteConfigID(IIS_BINDING);
    ExitOnFailure(hr, "Failed write binding ID");
    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "Failed write binding action ID");
    hr = ScaWriteConfigString(psw->wzDescription);      //site name key
    ExitOnFailure(hr, "Failed write binding site key");

    if (psw->swaBinding.fSecure)
    {
        hr = ScaWriteConfigString(L"https");            // binding protocol
        ExitOnFailure(hr, "Failed write binding https");
    }
    else
    {
        hr = ScaWriteConfigString(L"http");             // binding protocol
        ExitOnFailure(hr, "Failed write binding http");
    }

    // set the IP address appropriately
    if (0 == wcscmp(psw->swaBinding.wzIP, L"*"))
    {
        ::ZeroMemory(wzIP, sizeof(wzIP));
    }
    else
    {
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
        hr = ::StringCchCopyW(wzIP, countof(wzIP), psw->swaBinding.wzIP);
        ExitOnFailure(hr, "Failed to copy IP string");
    }

    hr = ::StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaBinding.iPort, psw->swaBinding.wzHeader);
    ExitOnFailure(hr, "Failed to format IP:Port:Header binding string");

    // write bindings CAData
    hr = ScaWriteConfigString(wzBinding) ;            //binding info
    ExitOnFailure(hr, "Failed to create web bindings");

    for (ui = 0; (ui < MAX_ADDRESSES_PER_WEB) && (ui < psw->cExtraAddresses); ++ui)
    {
        // set the IP address appropriately
        if (0 == wcscmp(psw->swaExtraAddresses[ui].wzIP, L"*"))
        {
            ::ZeroMemory(wzIP, sizeof(wzIP));
        }
        else
        {
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
            hr = ::StringCchCopyW(wzIP, countof(wzIP), psw->swaExtraAddresses[ui].wzIP);
            ExitOnFailure(hr, "Failed to copy web IP");
        }
        hr = ::StringCchPrintfW(wzBinding, countof(wzBinding), L"%s:%d:%s", wzIP, psw->swaExtraAddresses[ui].iPort, psw->swaExtraAddresses[ui].wzHeader);
        ExitOnFailure(hr, "Failed to copy web IP");

        //create bindings for site
        hr = ScaWriteConfigID(IIS_BINDING);
        ExitOnFailure(hr, "Failed write binding ID");
        hr = ScaWriteConfigID(IIS_CREATE);
        ExitOnFailure(hr, "Failed write binding action");
        hr = ScaWriteConfigString(psw->wzDescription);      //site name key
        ExitOnFailure(hr, "Failed write binding web name");

        if (psw->swaExtraAddresses[ui].fSecure)
        {
            hr = ScaWriteConfigString(L"https");            // binding protocol
        }
        else
        {
            hr = ScaWriteConfigString(L"http");             // binding protocol
        }
        ExitOnFailure(hr, "Failed write binding http(s)");

        // write bindings CAData
        hr = ScaWriteConfigString(wzBinding) ;              //binding info
        ExitOnFailure(hr, "Failed write binding info");
    }

    // write the web dirproperties information
    if (psw->fHasProperties)
    {
        // dir properties are for the default application of the web
        // with location '/'
        hr = ScaWriteWebDirProperties7(psw->wzDescription, L"/", &psw->swp);
        ExitOnFailure(hr, "Failed to write web security information to metabase");
    }

    //// write the application information
    if (psw->fHasApplication)
    {
        hr = ScaWriteWebApplication7(psw->wzDescription, L"/", &psw->swapp, psapList);
        ExitOnFailure(hr, "Failed to write web application information to metabase");
    }

    // write the SSL certificate information
    if (psw->pswscList)
    {
        hr = ScaSslCertificateWrite7(psw->wzDescription, psw->pswscList);
        ExitOnFailure1(hr, "Failed to write SSL certificates for Web site: %ls", psw->wzKey);
    }

    // write the headers
    if (psw->pshhList)
    {
        hr = ScaWriteHttpHeader7(psw->wzDescription, L"/", psw->pshhList);
        ExitOnFailure1(hr, "Failed to write custom HTTP headers for Web site: %ls", psw->wzKey);
    }

    // write the errors
    if (psw->psweList)
    {
        hr = ScaWriteWebError7(psw->wzDescription, L"/", psw->psweList);
        ExitOnFailure1(hr, "Failed to write custom web errors for Web site: %ls", psw->wzKey);
    }

    // write the log information to the metabase
    if (psw->fHasLog)
    {
        hr = ScaWriteWebLog7(psw->wzDescription, &psw->swl);
        ExitOnFailure(hr, "Failed to write web log information to metabase");
    }

LExit:
    return hr;
}


static HRESULT ScaWebRemove7(
    __in const SCA_WEB7* psw
    )
{
    HRESULT hr = S_OK;

    hr = ScaWriteConfigID(IIS_SITE);
    ExitOnFailure(hr, "Failed write site ID");
    hr = ScaWriteConfigID(IIS_DELETE);
    ExitOnFailure(hr, "Failed write site action");
    hr = ScaWriteConfigString(psw->wzDescription);  //Site Name
    ExitOnFailure(hr, "Failed write site name");

LExit:
    return hr;
}
