// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT AppendUrlAce(
    __in LPWSTR wzSecurityPrincipal,
    __in int iRights,
    __in LPWSTR* psczSDDL
    );
static HRESULT WriteHttpUrlReservation(
    __in WCA_TODO action,
    __in LPWSTR wzUrl,
    __in LPWSTR wzSDDL,
    __in int iHandleExisting,
    __in LPWSTR* psczCustomActionData
    );
static HRESULT AddUrlReservation(
    __in LPWSTR wzUrl,
    __in LPWSTR wzSddl
    );
static HRESULT GetUrlReservation(
    __in LPWSTR wzUrl,
    __deref_out_z LPWSTR* psczSddl
    );
static HRESULT RemoveUrlReservation(
    __in LPWSTR wzUrl
    );

HTTPAPI_VERSION vcHttpVersion = HTTPAPI_VERSION_1;
ULONG vcHttpFlags = HTTP_INITIALIZE_CONFIG;

LPCWSTR vcsHttpUrlReservationQuery =
    L"SELECT `WixHttpUrlReservation`.`WixHttpUrlReservation`, `WixHttpUrlReservation`.`HandleExisting`, `WixHttpUrlReservation`.`Sddl`, `WixHttpUrlReservation`.`Url`, `WixHttpUrlReservation`.`Component_` "
    L"FROM `WixHttpUrlReservation`";
enum eHttpUrlReservationQuery { hurqId = 1, hurqHandleExisting, hurqSDDL, hurqUrl, hurqComponent };

LPCWSTR vcsHttpUrlAceQuery =
    L"SELECT `WixHttpUrlAce`.`SecurityPrincipal`, `WixHttpUrlAce`.`Rights` "
    L"FROM `WixHttpUrlAce` "
    L"WHERE `WixHttpUrlAce`.`WixHttpUrlReservation_`=?";
enum eHttpUrlAceQuery { huaqSecurityPrincipal = 1, huaqRights };

enum eHandleExisting { heReplace = 0, heIgnore = 1, heFail = 2 };

/******************************************************************
 SchedHttpUrlReservations - immediate custom action worker to 
   prepare configuring URL reservations.

********************************************************************/
static UINT SchedHttpUrlReservations(
    __in MSIHANDLE hInstall,
    __in WCA_TODO todoSched
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fAceTableExists = FALSE;
    BOOL fHttpInitialized = FALSE;
    DWORD cUrlReservations = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    PMSIHANDLE hQueryReq = NULL;
    PMSIHANDLE hAceView = NULL;

    LPWSTR sczCustomActionData = NULL;
    LPWSTR sczRollbackCustomActionData = NULL;

    LPWSTR sczId = NULL;
    LPWSTR sczComponent = NULL;
    WCA_TODO todoComponent = WCA_TODO_UNKNOWN;
    LPWSTR sczUrl = NULL;
    LPWSTR sczSecurityPrincipal = NULL;
    int iRights = 0;
    int iHandleExisting = 0;

    LPWSTR sczExistingSDDL = NULL;
    LPWSTR sczSDDL = NULL;

    // Initialize.
    hr = WcaInitialize(hInstall, "SchedHttpUrlReservations");
    ExitOnFailure(hr, "Failed to initialize.");

    // Anything to do?
    hr = WcaTableExists(L"WixHttpUrlReservation");
    ExitOnFailure(hr, "Failed to check if the WixHttpUrlReservation table exists.");
    if (S_FALSE == hr)
    {
        WcaLog(LOGMSG_STANDARD, "WixHttpUrlReservation table doesn't exist, so there are no URL reservations to configure.");
        ExitFunction();
    }

    hr = WcaTableExists(L"WixHttpUrlAce");
    ExitOnFailure(hr, "Failed to check if the WixHttpUrlAce table exists.");
    fAceTableExists = S_OK == hr;

    // Query and loop through all the URL reservations.
    hr = WcaOpenExecuteView(vcsHttpUrlReservationQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on the WixHttpUrlReservation table.");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(vcHttpVersion, vcHttpFlags, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration.");

    fHttpInitialized = TRUE;

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, hurqId, &sczId);
        ExitOnFailure(hr, "Failed to get WixHttpUrlReservation.WixHttpUrlReservation");

        hr = WcaGetRecordString(hRec, hurqComponent, &sczComponent);
        ExitOnFailure(hr, "Failed to get WixHttpUrlReservation.Component_");

        // Figure out what we're doing for this reservation, treating reinstall the same as install.
        todoComponent = WcaGetComponentToDo(sczComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d) for UrlReservation '%ls'.", sczComponent, todoComponent, todoSched, sczId);
            continue;
        }

        hr = WcaGetRecordFormattedString(hRec, hurqUrl, &sczUrl);
        ExitOnFailure(hr, "Failed to get WixHttpUrlReservation.Url");

        hr = WcaGetRecordInteger(hRec, hurqHandleExisting, &iHandleExisting);
        ExitOnFailure(hr, "Failed to get WixHttpUrlReservation.HandleExisting");

        if (::MsiRecordIsNull(hRec, hurqSDDL))
        {
            hr = StrAllocString(&sczSDDL, L"D:", 2);
            ExitOnFailure(hr, "Failed to allocate SDDL string.");

            // Skip creating the SDDL on uninstall, since it's never used and the lookup(s) could fail.
            if (fAceTableExists && WCA_TODO_UNINSTALL != todoComponent)
            {
                hQueryReq = ::MsiCreateRecord(1);
                hr = WcaSetRecordString(hQueryReq, 1, sczId);
                ExitOnFailure1(hr, "Failed to create record for querying WixHttpUrlAce table for reservation %ls", sczId);

                hr = WcaOpenView(vcsHttpUrlAceQuery, &hAceView);
                ExitOnFailure1(hr, "Failed to open view on WixHttpUrlAce table for reservation %ls", sczId);
                hr = WcaExecuteView(hAceView, hQueryReq);
                ExitOnFailure1(hr, "Failed to execute view on WixHttpUrlAce table for reservation %ls", sczId);

                while (S_OK == (hr = WcaFetchRecord(hAceView, &hRec)))
                {
                    hr = WcaGetRecordFormattedString(hRec, huaqSecurityPrincipal, &sczSecurityPrincipal);
                    ExitOnFailure(hr, "Failed to get WixHttpUrlAce.SecurityPrincipal");

                    hr = WcaGetRecordInteger(hRec, huaqRights, &iRights);
                    ExitOnFailure(hr, "Failed to get WixHttpUrlAce.Rights");

                    hr = AppendUrlAce(sczSecurityPrincipal, iRights, &sczSDDL);
                    ExitOnFailure(hr, "Failed to append URL ACE.");
                }

                if (E_NOMOREITEMS == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to enumerate selected rows from WixHttpUrlAce table.");
            }
        }
        else
        {
            hr = WcaGetRecordFormattedString(hRec, hurqSDDL, &sczSDDL);
            ExitOnFailure(hr, "Failed to get WixHttpUrlReservation.SDDL");
        }

        hr = GetUrlReservation(sczUrl, &sczExistingSDDL);
        ExitOnFailure1(hr, "Failed to get the existing SDDL for %ls", sczUrl);

        hr = WriteHttpUrlReservation(todoComponent, sczUrl, sczExistingSDDL ? sczExistingSDDL : L"", iHandleExisting, &sczRollbackCustomActionData);
        ExitOnFailure(hr, "Failed to write URL Reservation to rollback custom action data.");

        hr = WriteHttpUrlReservation(todoComponent, sczUrl, sczSDDL, iHandleExisting, &sczCustomActionData);
        ExitOnFailure(hr, "Failed to write URL reservation to custom action data.");
        ++cUrlReservations;
    }

    // Reaching the end of the list is not an error.
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occurred while processing WixHttpUrlReservation table.");

    // Schedule ExecHttpUrlReservations if there's anything to do.
    if (cUrlReservations)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling URL reservations (%ls)", sczCustomActionData);
        WcaLog(LOGMSG_STANDARD, "Scheduling rollback URL reservations (%ls)", sczRollbackCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(L"WixRollbackHttpUrlReservationsInstall", sczRollbackCustomActionData, cUrlReservations * COST_HTTP_URL_ACL);
            ExitOnFailure(hr, "Failed to schedule install URL reservations rollback.");
            hr = WcaDoDeferredAction(L"WixExecHttpUrlReservationsInstall", sczCustomActionData, cUrlReservations * COST_HTTP_URL_ACL);
            ExitOnFailure(hr, "Failed to schedule install URL reservations execution.");
        }
        else
        {
            hr = WcaDoDeferredAction(L"WixRollbackHttpUrlReservationsUninstall", sczRollbackCustomActionData, cUrlReservations * COST_HTTP_URL_ACL);
            ExitOnFailure(hr, "Failed to schedule uninstall URL reservations rollback.");
            hr = WcaDoDeferredAction(L"WixExecHttpUrlReservationsUninstall", sczCustomActionData, cUrlReservations * COST_HTTP_URL_ACL);
            ExitOnFailure(hr, "Failed to schedule uninstall URL reservations execution.");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No URL reservations scheduled.");
    }
LExit:
    ReleaseStr(sczSDDL);
    ReleaseStr(sczExistingSDDL);
    ReleaseStr(sczSecurityPrincipal);
    ReleaseStr(sczUrl)
    ReleaseStr(sczComponent);
    ReleaseStr(sczId);
    ReleaseStr(sczRollbackCustomActionData);
    ReleaseStr(sczCustomActionData);

    if (fHttpInitialized)
    {
        ::HttpTerminate(vcHttpFlags, NULL);
    }

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

static HRESULT AppendUrlAce(
    __in LPWSTR wzSecurityPrincipal,
    __in int iRights,
    __in LPWSTR* psczSDDL
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSid = NULL;
    LPWSTR sczSid = NULL;

    Assert(wzSecurityPrincipal && *wzSecurityPrincipal);
    Assert(psczSDDL && *psczSDDL);

    // As documented in the xsd, if the first char is '*', then the rest of the string is a SID string, e.g. *S-1-5-18.
    if (L'*' == wzSecurityPrincipal[0])
    {
        wzSid = &wzSecurityPrincipal[1];
    }
    else
    {
        hr = AclGetAccountSidStringEx(NULL, wzSecurityPrincipal, &sczSid);
        ExitOnFailure1(hr, "Failed to lookup the SID for account %ls", wzSecurityPrincipal);

        wzSid = sczSid;
    }

    hr = StrAllocFormatted(psczSDDL, L"%ls(A;;%#x;;;%ls)", *psczSDDL, iRights, wzSid);

LExit:
    ReleaseStr(sczSid);

    return hr;
}

static HRESULT WriteHttpUrlReservation(
    __in WCA_TODO action,
    __in LPWSTR wzUrl,
    __in LPWSTR wzSDDL,
    __in int iHandleExisting,
    __in LPWSTR* psczCustomActionData
    )
{
    HRESULT hr = S_OK;

    hr = WcaWriteIntegerToCaData(action, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write action to custom action data.");

    hr = WcaWriteStringToCaData(wzUrl, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write URL to custom action data.");

    hr = WcaWriteStringToCaData(wzSDDL, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write SDDL to custom action data.");

    hr = WcaWriteIntegerToCaData(iHandleExisting, psczCustomActionData);
    ExitOnFailure(hr, "Failed to write HandleExisting to custom action data.")

LExit:
    return hr;
}

/******************************************************************
 SchedHttpUrlReservationsInstall - immediate custom action entry
   point to prepare adding URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpUrlReservationsInstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedHttpUrlReservations(hInstall, WCA_TODO_INSTALL);
}

/******************************************************************
 SchedHttpUrlReservationsUninstall - immediate custom action entry
   point to prepare removing URL reservations.

********************************************************************/
extern "C" UINT __stdcall SchedHttpUrlReservationsUninstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedHttpUrlReservations(hInstall, WCA_TODO_UNINSTALL);
}

/******************************************************************
 ExecHttpUrlReservations - deferred custom action entry point to 
   register and remove URL reservations.

********************************************************************/
extern "C" UINT __stdcall ExecHttpUrlReservations(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    BOOL fHttpInitialized = FALSE;
    LPWSTR sczCustomActionData = NULL;
    LPWSTR wz = NULL;
    int iTodo = WCA_TODO_UNKNOWN;
    LPWSTR sczUrl = NULL;
    LPWSTR sczSDDL = NULL;
    eHandleExisting handleExisting = heIgnore;
    BOOL fRollback = ::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK);
    BOOL fRemove = FALSE;
    BOOL fAdd = FALSE;
    BOOL fFailOnExisting = FALSE;

    // Initialize.
    hr = WcaInitialize(hInstall, "ExecHttpUrlReservations");
    ExitOnFailure(hr, "Failed to initialize.");

    hr = HRESULT_FROM_WIN32(::HttpInitialize(vcHttpVersion, vcHttpFlags, NULL));
    ExitOnFailure(hr, "Failed to initialize HTTP Server configuration.");

    fHttpInitialized = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &sczCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData.");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", sczCustomActionData);

    wz = sczCustomActionData;
    while (wz && *wz)
    {
        // Extract the custom action data and if rolling back, swap INSTALL and UNINSTALL.
        hr = WcaReadIntegerFromCaData(&wz, &iTodo);
        ExitOnFailure(hr, "Failed to read todo from custom action data.");

        hr = WcaReadStringFromCaData(&wz, &sczUrl);
        ExitOnFailure(hr, "Failed to read Url from custom action data.");

        hr = WcaReadStringFromCaData(&wz, &sczSDDL);
        ExitOnFailure(hr, "Failed to read SDDL from custom action data.");

        hr = WcaReadIntegerFromCaData(&wz, reinterpret_cast<int*>(&handleExisting));
        ExitOnFailure(hr, "Failed to read HandleExisting from custom action data.");

        switch (iTodo)
        {
        case WCA_TODO_INSTALL:
        case WCA_TODO_REINSTALL:
            fRemove = heReplace == handleExisting || fRollback;
            fAdd = !fRollback || *sczSDDL;
            fFailOnExisting = heFail == handleExisting && !fRollback;
            break;

        case WCA_TODO_UNINSTALL:
            fRemove = !fRollback;
            fAdd = fRollback && *sczSDDL;
            fFailOnExisting = FALSE;
            break;
        }

        if (fRemove)
        {
            WcaLog(LOGMSG_STANDARD, "Removing reservation for URL '%ls'", sczUrl);
            hr = RemoveUrlReservation(sczUrl);
            if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to remove reservation for URL '%ls'", sczUrl);
                }
                else
                {
                    ExitOnFailure1(hr, "Failed to remove reservation for URL '%ls'", sczUrl);
                }
            }
        }
        if (fAdd)
        {
            WcaLog(LOGMSG_STANDARD, "Adding reservation for URL '%ls' with SDDL '%ls'", sczUrl, sczSDDL);
            hr = AddUrlReservation(sczUrl, sczSDDL);
            if (S_FALSE == hr && fFailOnExisting)
            {
                hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
            }
            if (FAILED(hr))
            {
                if (fRollback)
                {
                    WcaLogError(hr, "Failed to add reservation for URL '%ls' with SDDL '%ls'", sczUrl, sczSDDL);
                }
                else
                {
                    ExitOnFailure2(hr, "Failed to add reservation for URL '%ls' with SDDL '%ls'", sczUrl, sczSDDL);
                }
            }
        }
    }

LExit:
    ReleaseStr(sczSDDL);
    ReleaseStr(sczUrl);
    ReleaseStr(sczCustomActionData);

    if (fHttpInitialized)
    {
        ::HttpTerminate(vcHttpFlags, NULL);
    }

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}

static HRESULT AddUrlReservation(
    __in LPWSTR wzUrl,
    __in LPWSTR wzSddl
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_URLACL_SET set = { };

    set.KeyDesc.pUrlPrefix = wzUrl;
    set.ParamDesc.pStringSecurityDescriptor = wzSddl;

    er = ::HttpSetServiceConfiguration(NULL, HttpServiceConfigUrlAclInfo, &set, sizeof(set), NULL);
    if (ERROR_ALREADY_EXISTS == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }
    ExitOnFailure2(hr, "Failed to add URL reservation: %ls, ACL: %ls", wzUrl, wzSddl);

LExit:
    return hr;
}

static HRESULT GetUrlReservation(
    __in LPWSTR wzUrl,
    __deref_out_z LPWSTR* psczSddl
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_URLACL_QUERY query = { };
    HTTP_SERVICE_CONFIG_URLACL_SET* pSet = NULL;
    ULONG cbSet = 0;

    query.QueryDesc = HttpServiceConfigQueryExact;
    query.KeyDesc.pUrlPrefix = wzUrl;

    er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigUrlAclInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    if (ERROR_INSUFFICIENT_BUFFER == er)
    {
        pSet = reinterpret_cast<HTTP_SERVICE_CONFIG_URLACL_SET*>(MemAlloc(cbSet, TRUE));
        ExitOnNull(pSet, hr, E_OUTOFMEMORY, "Failed to allocate query URLACL buffer.");

        er = ::HttpQueryServiceConfiguration(NULL, HttpServiceConfigUrlAclInfo, &query, sizeof(query), pSet, cbSet, &cbSet, NULL);
    }
    
    if (ERROR_SUCCESS == er)
    {
        hr = StrAllocString(psczSddl, pSet->ParamDesc.pStringSecurityDescriptor, 0);
    }
    else if (ERROR_FILE_NOT_FOUND == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    ReleaseMem(pSet);

    return hr;
}

static HRESULT RemoveUrlReservation(
    __in LPWSTR wzUrl
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HTTP_SERVICE_CONFIG_URLACL_SET set = { };

    set.KeyDesc.pUrlPrefix = wzUrl;

    er = ::HttpDeleteServiceConfiguration(NULL, HttpServiceConfigUrlAclInfo, &set, sizeof(set), NULL);
    if (ERROR_FILE_NOT_FOUND == er)
    {
        hr = S_FALSE;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }
    ExitOnFailure1(hr, "Failed to remove URL reservation: %ls", wzUrl);

LExit:
    return hr;
}
