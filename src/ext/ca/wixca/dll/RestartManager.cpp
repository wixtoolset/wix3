//-------------------------------------------------------------------------------------------------
// <copyright file="RestartManager.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Restart Manager custom actions.
// </summary>
// 
// <remarks>
//    MSDN (http://msdn.microsoft.com/library/aa373680.aspx) implies we need separate
//    custom actions for different processor architectures / component bitness.
// </remarks>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include <restartmanager.h>

// Include space for the terminating null.
#define CCH_SESSION_KEY CCH_RM_SESSION_KEY + 1

enum eRmuResourceType
{
    etInvalid,
    etFilename,
    etApplication,
    etServiceName,

    // Mask types from Attributes.
    etTypeMask = 0xf,
};

LPCWSTR vcsRestartResourceQuery =
    L"SELECT `WixRestartResource`.`WixRestartResource`, `WixRestartResource`.`Component_`, `WixRestartResource`.`Resource`, `WixRestartResource`.`Attributes` "
    L"FROM `WixRestartResource`";
enum eRestartResourceQuery { rrqRestartResource = 1, rrqComponent, rrqResource, rrqAttributes };

/********************************************************************
WixRegisterRestartResources - Immediate CA to register resources with RM.

Enumerates components before InstallValidate and registers resources
to be restarted by Restart Manager if the component action
is anything other than None.

Do not disable file system redirection.

********************************************************************/
extern "C" UINT __stdcall WixRegisterRestartResources(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR wzSessionKey = NULL;
    size_t cchSessionKey = 0;
    PRMU_SESSION pSession = NULL;

    LPWSTR wzRestartResource = NULL;
    LPWSTR wzComponent = NULL;
    LPWSTR wzResource = NULL;
    int iAttributes = NULL;
    BOOL fIsComponentNull = FALSE;
    WCA_TODO todo = WCA_TODO_UNKNOWN;
    int iType = etInvalid;

    hr = WcaInitialize(hInstall, "WixRegisterRestartResources");
    ExitOnFailure(hr, "Failed to initialize.");

    // Skip if the table doesn't exist.
    if (S_OK != WcaTableExists(L"WixRestartResource"))
    {
        WcaLog(LOGMSG_STANDARD, "The RestartResource table does not exist; there are no resources to register with Restart Manager.");
        ExitFunction();
    }

    // Get the existing Restart Manager session if available.
    hr = WcaGetProperty(L"MsiRestartManagerSessionKey", &wzSessionKey);
    ExitOnFailure(hr, "Failed to get the MsiRestartManagerSessionKey property.");

    hr = ::StringCchLengthW(wzSessionKey, CCH_SESSION_KEY, &cchSessionKey);
    ExitOnFailure(hr, "Failed to get the MsiRestartManagerSessionKey string length.");

    // Skip if the property doesn't exist.
    if (0 == cchSessionKey)
    {
        WcaLog(LOGMSG_STANDARD, "The MsiRestartManagerSessionKey property is not available to join.");
        ExitFunction();
    }

    // Join the existing Restart Manager session if supported.
    hr = RmuJoinSession(&pSession, wzSessionKey);
    if (E_MODNOTFOUND == hr)
    {
        WcaLog(LOGMSG_STANDARD, "The Restart Manager is not supported on this platform. Skipping.");
        ExitFunction1(hr = S_OK);
    }
    else if (FAILED(hr))
    {
        WcaLog(LOGMSG_STANDARD, "Failed to join the existing Restart Manager session %ls.", wzSessionKey);
        ExitFunction1(hr = S_OK);
    }

    // Loop through each record in the table.
    hr = WcaOpenExecuteView(vcsRestartResourceQuery, &hView);
    ExitOnFailure(hr, "Failed to open a view on the RestartResource table.");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, rrqRestartResource, &wzRestartResource);
        ExitOnFailure(hr, "Failed to get the RestartResource field value.");

        hr = WcaGetRecordString(hRec, rrqComponent, &wzComponent);
        ExitOnFailure(hr, "Failed to get the Component_ field value.");

        hr = WcaGetRecordFormattedString(hRec, rrqResource, &wzResource);
        ExitOnFailure(hr, "Failed to get the Resource formatted field value.");

        hr = WcaGetRecordInteger(hRec, rrqAttributes, &iAttributes);
        ExitOnFailure(hr, "Failed to get the Attributes field value.");

        fIsComponentNull = ::MsiRecordIsNull(hRec, rrqComponent);
        todo = WcaGetComponentToDo(wzComponent);

        // Only register resources for components that are null, or being installed, reinstalled, or uninstalled.
        if (!fIsComponentNull && WCA_TODO_UNKNOWN == todo)
        {
            WcaLog(LOGMSG_VERBOSE, "Skipping resource %ls.", wzRestartResource);
            continue;
        }

        // Get the type from Attributes and add to the Restart Manager.
        iType = iAttributes & etTypeMask;
        switch (iType)
        {
        case etFilename:
            WcaLog(LOGMSG_VERBOSE, "Registering file name %ls with the Restart Manager.", wzResource);
            hr = RmuAddFile(pSession, wzResource);
            ExitOnFailure(hr, "Failed to register the file name with the Restart Manager session.");
            break;

        case etApplication:
            WcaLog(LOGMSG_VERBOSE, "Registering process name %ls with the Restart Manager.", wzResource);
            hr = RmuAddProcessesByName(pSession, wzResource);
            ExitOnFailure(hr, "Failed to register the process name with the Restart Manager session.");
            break;

        case etServiceName:
            WcaLog(LOGMSG_VERBOSE, "Registering service name %ls with the Restart Manager.", wzResource);
            hr = RmuAddService(pSession, wzResource);
            ExitOnFailure(hr, "Failed to register the service name with the Restart Manager session.");
            break;

        default:
            WcaLog(LOGMSG_VERBOSE, "The resource type %d for %ls is not supported and will not be registered.", iType, wzRestartResource);
            break;
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed while looping through all rows to register resources.");

    // Register the resources and unjoin the session.
    hr = RmuEndSession(pSession);
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to register the resources with the Restart Manager.");
        ExitFunction1(hr = S_OK);
    }

LExit:
    ReleaseStr(wzRestartResource);
    ReleaseStr(wzComponent);
    ReleaseStr(wzResource);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}
