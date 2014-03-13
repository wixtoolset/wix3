0//-------------------------------------------------------------------------------------------------
// <copyright file="scawebapp.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IIS Web Application functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
enum eWebApplicationQuery { wappqName = 1, wappqIsolation, wappqAllowSession,
                            wappqSessionTimeout, wappqBuffer, wappqParentPaths,
                            wappqDefaultScript, wappqScriptTimeout,
                            wappqServerDebugging, wappqClientDebugging, wappqAppPool, wappqApplication};


HRESULT ScaGetWebApplication(MSIHANDLE /*hViewApplications*/, 
                             LPCWSTR pwzApplication,
                             __in WCA_WRAPQUERY_HANDLE hWebAppQuery,
                             __in WCA_WRAPQUERY_HANDLE hWebAppExtQuery,
                             SCA_WEB_APPLICATION* pswapp)
{
    HRESULT hr = S_OK;

    MSIHANDLE hRec;
    LPWSTR pwzData = NULL;

    // Reset back to the first record
    WcaFetchWrappedReset(hWebAppQuery);

    // get the application information
    hr = WcaFetchWrappedRecordWhereString(hWebAppQuery, wappqApplication, pwzApplication, &hRec);
    if (S_OK == hr)
    {
        // application name
        hr = WcaGetRecordString(hRec, wappqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Name of App");
        hr = ::StringCchCopyW(pswapp->wzName, countof(pswapp->wzName), pwzData);
        if (HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) == hr)
        {
            // The application name is sometimes truncated to IIS's supported length, so ignore insufficient buffer errors here
            WcaLog(LOGMSG_VERBOSE, "Application name \"%ls\" truncated to fit IIS's supported %d-character length", pwzData, MAX_APP_NAME);
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to copy name string to webapp object");

        hr = WcaGetRecordInteger(hRec, wappqIsolation, &pswapp->iIsolation);
        ExitOnFailure1(hr, "Failed to get App isolation: '%ls'", pswapp->wzName);

        hr = WcaGetRecordInteger(hRec, wappqAllowSession, &pswapp->fAllowSessionState);

        hr = WcaGetRecordInteger(hRec, wappqSessionTimeout, &pswapp->iSessionTimeout);

        hr = WcaGetRecordInteger(hRec, wappqBuffer, &pswapp->fBuffer);

        hr = WcaGetRecordInteger(hRec, wappqParentPaths, &pswapp->fParentPaths);

        hr = WcaGetRecordString(hRec, wappqDefaultScript, &pwzData);
        ExitOnFailure1(hr, "Failed to get default scripting language for App: '%ls'", pswapp->wzName);
        hr = ::StringCchCopyW(pswapp->wzDefaultScript, countof(pswapp->wzDefaultScript), pwzData);
        ExitOnFailure(hr, "Failed to copy default script string to webapp object");

        // asp script timeout
        hr = WcaGetRecordInteger(hRec, wappqScriptTimeout, &pswapp->iScriptTimeout);
        ExitOnFailure1(hr, "Failed to get scripting timeout for App: '%ls'", pswapp->wzName);

        // asp server-side script debugging
        hr = WcaGetRecordInteger(hRec, wappqServerDebugging, &pswapp->fServerDebugging);

        // asp client-side script debugging
        hr = WcaGetRecordInteger(hRec, wappqClientDebugging, &pswapp->fClientDebugging);

        hr = WcaGetRecordString(hRec, wappqAppPool, &pwzData);
        ExitOnFailure1(hr, "Failed to get AppPool for App: '%ls'", pswapp->wzName);
        hr = ::StringCchCopyW(pswapp->wzAppPool, countof(pswapp->wzAppPool), pwzData);
        ExitOnFailure2(hr, "failed to copy AppPool: '%ls' for App: '%ls'", pwzData, pswapp->wzName);

        // app extensions
         hr = ScaWebAppExtensionsRead(pwzApplication, hWebAppExtQuery, &pswapp->pswappextList);
        ExitOnFailure1(hr, "Failed to read AppExtensions for App: '%ls'", pswapp->wzName);

        hr = S_OK;
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebApplication.Application='%ls'", pwzApplication);
        hr = E_FAIL;
    }
    else
        ExitOnFailure(hr, "Error matching Application rows");

    // Let's check that there isn't more than one record found - if there is, throw an assert like WcaFetchSingleRecord() would
    HRESULT hrTemp = WcaFetchWrappedRecordWhereString(hWebAppQuery, wappqApplication, pwzApplication, &hRec);
    if (SUCCEEDED(hrTemp))
    {
        AssertSz(E_NOMOREITEMS == hrTemp, "ScaGetWebApplication found more than one record");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaWriteWebApplication(IMSAdminBase* piMetabase, LPCWSTR wzRootOfWeb, 
                               SCA_WEB_APPLICATION* pswapp, SCA_APPPOOL * psapList)
{
    HRESULT hr = S_OK;
    WCHAR wzAppPoolName[MAX_PATH];

    hr = ScaCreateApp(piMetabase, wzRootOfWeb, pswapp->iIsolation);
    ExitOnFailure(hr, "Failed to create ASP App");

    // Medium Isolation seems to have to be set through the metabase
    if (2 == pswapp->iIsolation)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_ISOLATED, METADATA_INHERIT, IIS_MD_UT_WAM, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iIsolation));
        ExitOnFailure1(hr, "Failed to write isolation value for App: '%ls'", pswapp->wzName);
    }

    // application name
    hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_FRIENDLY_NAME, METADATA_INHERIT, IIS_MD_UT_WAM, STRING_METADATA, pswapp->wzName);
    ExitOnFailure1(hr, "Failed to write Name of App: '%ls'", pswapp->wzName);

    // allow session state
    if (MSI_NULL_INTEGER != pswapp->fAllowSessionState)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ALLOWSESSIONSTATE, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fAllowSessionState));
        ExitOnFailure1(hr, "Failed to write allow session information for App: '%ls'", pswapp->wzName);
    }

    // session timeout
    if (MSI_NULL_INTEGER != pswapp->iSessionTimeout)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SESSIONTIMEOUT, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iSessionTimeout));
        ExitOnFailure1(hr, "Failed to write session timeout for App: '%ls'", pswapp->wzName);
    }

    // asp buffering
    if (MSI_NULL_INTEGER != pswapp->fBuffer)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_BUFFERINGON, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fBuffer));
        ExitOnFailure1(hr, "Failed to write buffering flag for App: '%ls'", pswapp->wzName);
    }

    // asp parent paths
    if (MSI_NULL_INTEGER != pswapp->fParentPaths)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLEPARENTPATHS, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fParentPaths));
        ExitOnFailure1(hr, "Failed to write parent paths flag for App: '%ls'", pswapp->wzName);
    }

    // default scripting language
    if (*pswapp->wzDefaultScript)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTLANGUAGE, METADATA_INHERIT, ASP_MD_UT_APP, STRING_METADATA, pswapp->wzDefaultScript);
        ExitOnFailure1(hr, "Failed to write default scripting language for App: '%ls'", pswapp->wzName);
    }

    // asp script timeout
    if (MSI_NULL_INTEGER != pswapp->iScriptTimeout)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTTIMEOUT, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->iScriptTimeout));
        ExitOnFailure1(hr, "Failed to write script timeout for App: '%ls'", pswapp->wzName);
    }

    // asp server-side script debugging
    if (MSI_NULL_INTEGER != pswapp->fServerDebugging)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLESERVERDEBUG, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fServerDebugging));
        ExitOnFailure1(hr, "Failed to write ASP server-side script debugging flag for App: '%ls'", pswapp->wzName);
    }

    // asp server-side script debugging
    if (MSI_NULL_INTEGER != pswapp->fClientDebugging)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_ENABLECLIENTDEBUG, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswapp->fClientDebugging));
        ExitOnFailure1(hr, "Failed to write ASP client-side script debugging flag for App: '%ls'", pswapp->wzName);
    }

    // AppPool
    if (*pswapp->wzAppPool && NULL != psapList)
    {
        hr = ScaFindAppPool(piMetabase, pswapp->wzAppPool, wzAppPoolName, countof(wzAppPoolName), psapList);
        ExitOnFailure1(hr, "failed to find app pool: %ls", pswapp->wzAppPool);
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_APP_APPPOOL_ID, METADATA_INHERIT, IIS_MD_UT_SERVER, STRING_METADATA, wzAppPoolName);
        ExitOnFailure1(hr, "Failed to write default AppPool for App: '%ls'", pswapp->wzName);
    }

    if (pswapp->pswappextList)
    {
        hr = ScaWebAppExtensionsWrite(piMetabase, wzRootOfWeb, pswapp->pswappextList);
        ExitOnFailure1(hr, "Failed to write AppExtensions for App: '%ls'", pswapp->wzName);
    }

LExit:
    return hr;
}
