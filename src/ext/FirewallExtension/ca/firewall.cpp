// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsFirewallExceptionQuery =
    L"SELECT `Name`, `RemoteAddresses`, `Port`, `Protocol`, `Program`, `Attributes`, `Profile`, `Component_`, `Description` FROM `WixFirewallException`";
enum eFirewallExceptionQuery { feqName = 1, feqRemoteAddresses, feqPort, feqProtocol, feqProgram, feqAttributes, feqProfile, feqComponent, feqDescription };
enum eFirewallExceptionTarget { fetPort = 1, fetApplication, fetUnknown };
enum eFirewallExceptionAttributes { feaIgnoreFailures = 1 };

/******************************************************************
 SchedFirewallExceptions - immediate custom action worker to 
   register and remove firewall exceptions.

********************************************************************/
static UINT SchedFirewallExceptions(
    __in MSIHANDLE hInstall,
    WCA_TODO todoSched
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    int cFirewallExceptions = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzRemoteAddresses = NULL;
    LPWSTR pwzPort = NULL;
    int iProtocol = 0;
    int iAttributes = 0;
    int iProfile = 0;
    LPWSTR pwzProgram = NULL;
    LPWSTR pwzComponent = NULL;
    LPWSTR pwzFormattedFile = NULL;
    LPWSTR pwzDescription = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "SchedFirewallExceptions");
    ExitOnFailure(hr, "failed to initialize");

    // anything to do?
    if (S_OK != WcaTableExists(L"WixFirewallException"))
    {
        WcaLog(LOGMSG_STANDARD, "WixFirewallException table doesn't exist, so there are no firewall exceptions to configure.");
        ExitFunction();
    }

    // query and loop through all the firewall exceptions
    hr = WcaOpenExecuteView(vcsFirewallExceptionQuery, &hView);
    ExitOnFailure(hr, "failed to open view on WixFirewallException table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordFormattedString(hRec, feqName, &pwzName);
        ExitOnFailure(hr, "failed to get firewall exception name");

        hr = WcaGetRecordFormattedString(hRec, feqRemoteAddresses, &pwzRemoteAddresses);
        ExitOnFailure(hr, "failed to get firewall exception remote addresses");

        hr = WcaGetRecordFormattedString(hRec, feqPort, &pwzPort);
        ExitOnFailure(hr, "failed to get firewall exception port");

        hr = WcaGetRecordInteger(hRec, feqProtocol, &iProtocol);
        ExitOnFailure(hr, "failed to get firewall exception protocol");

        hr = WcaGetRecordFormattedString(hRec, feqProgram, &pwzProgram);
        ExitOnFailure(hr, "failed to get firewall exception program");

        hr = WcaGetRecordInteger(hRec, feqAttributes, &iAttributes);
        ExitOnFailure(hr, "failed to get firewall exception attributes");
        
        hr = WcaGetRecordInteger(hRec, feqProfile, &iProfile);
        ExitOnFailure(hr, "failed to get firewall exception profile");

        hr = WcaGetRecordString(hRec, feqComponent, &pwzComponent);
        ExitOnFailure(hr, "failed to get firewall exception component");

        hr = WcaGetRecordString(hRec, feqDescription, &pwzDescription);
        ExitOnFailure(hr, "failed to get firewall description");

        // figure out what we're doing for this exception, treating reinstall the same as install
        WCA_TODO todoComponent = WcaGetComponentToDo(pwzComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d)", pwzComponent, todoComponent, todoSched);
            continue;
        }

        // action :: name :: profile :: remoteaddresses :: attributes :: target :: {port::protocol | path}
        ++cFirewallExceptions;
        hr = WcaWriteIntegerToCaData(todoComponent, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception action to custom action data");

        hr = WcaWriteStringToCaData(pwzName, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception name to custom action data");

        hr = WcaWriteIntegerToCaData(iProfile, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception profile to custom action data");

        hr = WcaWriteStringToCaData(pwzRemoteAddresses, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception remote addresses to custom action data");

        hr = WcaWriteIntegerToCaData(iAttributes, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception attributes to custom action data");

        if (*pwzProgram)
        {
            // If program is defined, we have an application exception.
            hr = WcaWriteIntegerToCaData(fetApplication, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write exception target (application) to custom action data");

            hr = WcaWriteStringToCaData(pwzProgram, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write application path to custom action data");
        }
        else
        {
            // we have a port-only exception
            hr = WcaWriteIntegerToCaData(fetPort, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write exception target (port) to custom action data");
        }

        hr = WcaWriteStringToCaData(pwzPort, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write application path to custom action data");

        hr = WcaWriteIntegerToCaData(iProtocol, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception protocol to custom action data");

        hr = WcaWriteStringToCaData(pwzDescription, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write firewall rule description to custom action data");
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    } 
    ExitOnFailure(hr, "failure occured while processing WixFirewallException table");

    // schedule ExecFirewallExceptions if there's anything to do
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling firewall exception (%ls)", pwzCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(L"WixRollbackFirewallExceptionsInstall", pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions rollback");            
            hr = WcaDoDeferredAction(L"WixExecFirewallExceptionsInstall", pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions execution");
        }
        else
        {
            hr = WcaDoDeferredAction(L"WixRollbackFirewallExceptionsUninstall", pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall uninstall exceptions rollback");    
            hr = WcaDoDeferredAction(L"WixExecFirewallExceptionsUninstall", pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall uninstall exceptions execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No firewall exceptions scheduled");
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzRemoteAddresses);
    ReleaseStr(pwzPort);
    ReleaseStr(pwzProgram);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzDescription);
    ReleaseStr(pwzFormattedFile);

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

/******************************************************************
 SchedFirewallExceptionsInstall - immediate custom action entry
   point to register firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsInstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_INSTALL);
}

/******************************************************************
 SchedFirewallExceptionsUninstall - immediate custom action entry
   point to remove firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsUninstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_UNINSTALL);
}

/******************************************************************
 GetFirewallRules - Get the collection of firewall rules.

********************************************************************/
static HRESULT GetFirewallRules(
    __in BOOL fIgnoreFailures,
    __out INetFwRules** ppNetFwRules
    )
{
    HRESULT hr = S_OK;
    INetFwPolicy2* pNetFwPolicy2 = NULL;
    INetFwRules* pNetFwRules = NULL;
    *ppNetFwRules = NULL;
    
    do
    {
        ReleaseNullObject(pNetFwPolicy2);
        ReleaseNullObject(pNetFwRules);

        if (SUCCEEDED(hr = ::CoCreateInstance(__uuidof(NetFwPolicy2), NULL, CLSCTX_ALL, __uuidof(INetFwPolicy2), (void**)&pNetFwPolicy2)) &&
            SUCCEEDED(hr = pNetFwPolicy2->get_Rules(&pNetFwRules)))
        {
            break;
        }
        else if (fIgnoreFailures)
        {
            ExitFunction1(hr = S_FALSE);
        }
        else
        {
            WcaLog(LOGMSG_STANDARD, "Failed to connect to Windows Firewall");
            UINT er = WcaErrorMessage(msierrFirewallCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT: // exit with the current HRESULT
                ExitFunction();
            case IDRETRY: // clean up and retry the loop
                hr = S_FALSE;
                break;
            case IDIGNORE: // pass S_FALSE back to the caller, who knows how to ignore the failure
                ExitFunction1(hr = S_FALSE);
            default: // No UI, so default is to fail.
                ExitFunction();
            }
        }
    } while (S_FALSE == hr);

    *ppNetFwRules = pNetFwRules;
    pNetFwRules = NULL;
    
LExit:
    ReleaseObject(pNetFwPolicy2);
    ReleaseObject(pNetFwRules);

    return hr;
}

/******************************************************************
 CreateFwRuleObject - CoCreate a firewall rule, and set the common set of properties which are shared
 between port and application firewall rules

********************************************************************/
static HRESULT CreateFwRuleObject(
    __in BSTR bstrName,
    __in int iProfile,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in LPCWSTR wzDescription,
    __out INetFwRule** ppNetFwRule
    )
{
    HRESULT hr = S_OK;
    BSTR bstrRemoteAddresses = NULL;
    BSTR bstrPort = NULL;
    BSTR bstrDescription = NULL;
    INetFwRule* pNetFwRule = NULL;
    *ppNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrRemoteAddresses = ::SysAllocString(wzRemoteAddresses);
    ExitOnNull(bstrRemoteAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for remote addresses");
    bstrPort = ::SysAllocString(wzPort);
    ExitOnNull(bstrPort, hr, E_OUTOFMEMORY, "failed SysAllocString for port");
    bstrDescription = ::SysAllocString(wzDescription);
    ExitOnNull(bstrDescription, hr, E_OUTOFMEMORY, "failed SysAllocString for description");

    hr = ::CoCreateInstance(__uuidof(NetFwRule), NULL, CLSCTX_ALL, __uuidof(INetFwRule), (void**)&pNetFwRule);
    ExitOnFailure(hr, "failed to create NetFwRule object");

    hr = pNetFwRule->put_Name(bstrName);
    ExitOnFailure(hr, "failed to set exception name");

    hr = pNetFwRule->put_Profiles(static_cast<NET_FW_PROFILE_TYPE2>(iProfile));
    ExitOnFailure(hr, "failed to set exception profile");

    if (MSI_NULL_INTEGER != iProtocol)
    {
        hr = pNetFwRule->put_Protocol(static_cast<NET_FW_IP_PROTOCOL>(iProtocol));
        ExitOnFailure(hr, "failed to set exception protocol");
    }

    if (bstrPort && *bstrPort)
    {
        hr = pNetFwRule->put_LocalPorts(bstrPort);
        ExitOnFailure(hr, "failed to set exception port");
    }

    if (bstrRemoteAddresses && *bstrRemoteAddresses)
    {
        hr = pNetFwRule->put_RemoteAddresses(bstrRemoteAddresses);
        ExitOnFailure1(hr, "failed to set exception remote addresses '%ls'", bstrRemoteAddresses);
    }

    if (bstrDescription && *bstrDescription)
    {
        hr = pNetFwRule->put_Description(bstrDescription);
        ExitOnFailure1(hr, "failed to set exception description '%ls'", bstrDescription);
    }

    *ppNetFwRule = pNetFwRule;
    pNetFwRule = NULL;

LExit:
    ReleaseBSTR(bstrRemoteAddresses);
    ReleaseBSTR(bstrPort);
    ReleaseBSTR(bstrDescription);
    ReleaseObject(pNetFwRule);

    return hr;
}

/******************************************************************
 FSupportProfiles - Returns true if we support profiles on this machine.
  (Only on Vista or later)

********************************************************************/
static BOOL FSupportProfiles()
{
    BOOL fSupportProfiles = FALSE;
    INetFwRules* pNetFwRules = NULL;

    // We only support profiles if we can co-create an instance of NetFwPolicy2. 
    // This will not work on pre-vista machines.
    if (SUCCEEDED(GetFirewallRules(TRUE, &pNetFwRules)) &&  pNetFwRules != NULL)
    {
        fSupportProfiles = TRUE;
        ReleaseObject(pNetFwRules);
    }

    return fSupportProfiles;
}

/******************************************************************
 GetCurrentFirewallProfile - get the active firewall profile as an
   INetFwProfile, which owns the lists of exceptions we're 
   updating.

********************************************************************/
static HRESULT GetCurrentFirewallProfile(
    __in BOOL fIgnoreFailures,
    __out INetFwProfile** ppfwProfile
    )
{
    HRESULT hr = S_OK;
    INetFwMgr* pfwMgr = NULL;
    INetFwPolicy* pfwPolicy = NULL;
    INetFwProfile* pfwProfile = NULL;
    *ppfwProfile = NULL;
    
    do
    {
        ReleaseNullObject(pfwPolicy);
        ReleaseNullObject(pfwMgr);
        ReleaseNullObject(pfwProfile);

        if (SUCCEEDED(hr = ::CoCreateInstance(__uuidof(NetFwMgr), NULL, CLSCTX_INPROC_SERVER, __uuidof(INetFwMgr), (void**)&pfwMgr)) &&
            SUCCEEDED(hr = pfwMgr->get_LocalPolicy(&pfwPolicy)) &&
            SUCCEEDED(hr = pfwPolicy->get_CurrentProfile(&pfwProfile)))
        {
            break;
        }
        else if (fIgnoreFailures)
        {
            ExitFunction1(hr = S_FALSE);
        }
        else
        {
            WcaLog(LOGMSG_STANDARD, "Failed to connect to Windows Firewall");
            UINT er = WcaErrorMessage(msierrFirewallCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT: // exit with the current HRESULT
                ExitFunction();
            case IDRETRY: // clean up and retry the loop
                hr = S_FALSE;
                break;
            case IDIGNORE: // pass S_FALSE back to the caller, who knows how to ignore the failure
                ExitFunction1(hr = S_FALSE);
            default: // No UI, so default is to fail.
                ExitFunction();
            }
        }
    } while (S_FALSE == hr);

    *ppfwProfile = pfwProfile;
    pfwProfile = NULL;
    
LExit:
    ReleaseObject(pfwPolicy);
    ReleaseObject(pfwMgr);
    ReleaseObject(pfwProfile);

    return hr;
}

/******************************************************************
 AddApplicationException

********************************************************************/
static HRESULT AddApplicationException(
    __in LPCWSTR wzFile, 
    __in LPCWSTR wzName,
    __in int iProfile,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in LPCWSTR wzDescription
    )
{
    HRESULT hr = S_OK;
    BSTR bstrFile = NULL;
    BSTR bstrName = NULL;
    INetFwRules* pNetFwRules = NULL;
    INetFwRule* pNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrFile = ::SysAllocString(wzFile);
    ExitOnNull(bstrFile, hr, E_OUTOFMEMORY, "failed SysAllocString for path");
    bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // try to find it (i.e., support reinstall)
    hr = pNetFwRules->Item(bstrName, &pNetFwRule);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        hr = CreateFwRuleObject(bstrName, iProfile, wzRemoteAddresses, wzPort, iProtocol, wzDescription, &pNetFwRule);
        ExitOnFailure(hr, "failed to create FwRule object");

        // set edge traversal to true
        hr = pNetFwRule->put_EdgeTraversal(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to set application exception edgetraversal property");
        
        // set path
        hr = pNetFwRule->put_ApplicationName(bstrFile);
        ExitOnFailure(hr, "failed to set application name");
        
        // enable it
        hr = pNetFwRule->put_Enabled(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to to enable application exception");
     
        // add it to the list of authorized apps
        hr = pNetFwRules->Add(pNetFwRule);
        ExitOnFailure(hr, "failed to add app to the authorized apps list");
    }
    else
    {
        // we found an existing app exception (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing app");
        
        // enable it (just in case it was disabled)
        pNetFwRule->put_Enabled(VARIANT_TRUE);
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseBSTR(bstrFile);
    ReleaseObject(pNetFwRules);
    ReleaseObject(pNetFwRule);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 AddApplicationExceptionOnCurrentProfile

********************************************************************/
static HRESULT AddApplicationExceptionOnCurrentProfile(
    __in LPCWSTR wzFile, 
    __in LPCWSTR wzName, 
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;
    BSTR bstrFile = NULL;
    BSTR bstrName = NULL;
    BSTR bstrRemoteAddresses = NULL;
    INetFwProfile* pfwProfile = NULL;
    INetFwAuthorizedApplications* pfwApps = NULL;
    INetFwAuthorizedApplication* pfwApp = NULL;

    // convert to BSTRs to make COM happy
    bstrFile = ::SysAllocString(wzFile);
    ExitOnNull(bstrFile, hr, E_OUTOFMEMORY, "failed SysAllocString for path");
    bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");
    bstrRemoteAddresses = ::SysAllocString(wzRemoteAddresses);
    ExitOnNull(bstrRemoteAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for remote addresses");

    // get the firewall profile, which is our entry point for adding exceptions
    hr = GetCurrentFirewallProfile(fIgnoreFailures, &pfwProfile);
    ExitOnFailure(hr, "failed to get firewall profile");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // first, let's see if the app is already on the exception list
    hr = pfwProfile->get_AuthorizedApplications(&pfwApps);
    ExitOnFailure(hr, "failed to get list of authorized apps");

    // try to find it (i.e., support reinstall)
    hr = pfwApps->Item(bstrFile, &pfwApp);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        // not found, so we get to add it
        hr = ::CoCreateInstance(__uuidof(NetFwAuthorizedApplication), NULL, CLSCTX_INPROC_SERVER, __uuidof(INetFwAuthorizedApplication), reinterpret_cast<void**>(&pfwApp));
        ExitOnFailure(hr, "failed to create authorized app");

        // set the display name
        hr = pfwApp->put_Name(bstrName);
        ExitOnFailure(hr, "failed to set authorized app name");

        // set path
        hr = pfwApp->put_ProcessImageFileName(bstrFile);
        ExitOnFailure(hr, "failed to set authorized app path");

        // set the allowed remote addresses
        if (bstrRemoteAddresses && *bstrRemoteAddresses)
        {
            hr = pfwApp->put_RemoteAddresses(bstrRemoteAddresses);
            ExitOnFailure(hr, "failed to set authorized app remote addresses");
        }

        // add it to the list of authorized apps
        hr = pfwApps->Add(pfwApp);
        ExitOnFailure(hr, "failed to add app to the authorized apps list");
    }
    else
    {
        // we found an existing app exception (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing app");

        // enable it (just in case it was disabled)
        pfwApp->put_Enabled(VARIANT_TRUE);
    }

LExit:
    ReleaseBSTR(bstrRemoteAddresses);
    ReleaseBSTR(bstrName);
    ReleaseBSTR(bstrFile);
    ReleaseObject(pfwApp);
    ReleaseObject(pfwApps);
    ReleaseObject(pfwProfile);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 AddPortException

********************************************************************/
static HRESULT AddPortException(
    __in LPCWSTR wzName,
    __in int iProfile,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in LPCWSTR wzDescription
    )
{
    HRESULT hr = S_OK;
    BSTR bstrName = NULL;
    INetFwRules* pNetFwRules = NULL;
    INetFwRule* pNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // try to find it (i.e., support reinstall)
    hr = pNetFwRules->Item(bstrName, &pNetFwRule);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        hr = CreateFwRuleObject(bstrName, iProfile, wzRemoteAddresses, wzPort, iProtocol, wzDescription, &pNetFwRule);
        ExitOnFailure(hr, "failed to create FwRule object");

        // enable it
        hr = pNetFwRule->put_Enabled(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to to enable port exception");

        // add it to the list of authorized ports
        hr = pNetFwRules->Add(pNetFwRule);
        ExitOnFailure(hr, "failed to add app to the authorized ports list");
    }
    else
    {
        // we found an existing port exception (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing port rule");

        // enable it (just in case it was disabled)
        pNetFwRule->put_Enabled(VARIANT_TRUE);
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);
    ReleaseObject(pNetFwRule);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 AddPortExceptionOnCurrentProfile

********************************************************************/
static HRESULT AddPortExceptionOnCurrentProfile(
    __in LPCWSTR wzName,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures,
    __in int iPort,
    __in int iProtocol
    )
{
    HRESULT hr = S_OK;
    BSTR bstrName = NULL;
    BSTR bstrRemoteAddresses = NULL;
    INetFwProfile* pfwProfile = NULL;
    INetFwOpenPorts* pfwPorts = NULL;
    INetFwOpenPort* pfwPort = NULL;

    // convert to BSTRs to make COM happy
    bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");
    bstrRemoteAddresses = ::SysAllocString(wzRemoteAddresses);
    ExitOnNull(bstrRemoteAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for remote addresses");

    // create and initialize a new open port object
    hr = ::CoCreateInstance(__uuidof(NetFwOpenPort), NULL, CLSCTX_INPROC_SERVER, __uuidof(INetFwOpenPort), reinterpret_cast<void**>(&pfwPort));
    ExitOnFailure(hr, "failed to create new open port");

    hr = pfwPort->put_Port(iPort);
    ExitOnFailure(hr, "failed to set exception port");

    hr = pfwPort->put_Protocol(static_cast<NET_FW_IP_PROTOCOL>(iProtocol));
    ExitOnFailure(hr, "failed to set exception protocol");

    if (bstrRemoteAddresses && *bstrRemoteAddresses)
    {
        hr = pfwPort->put_RemoteAddresses(bstrRemoteAddresses);
        ExitOnFailure1(hr, "failed to set exception remote addresses '%ls'", bstrRemoteAddresses);
    }

    hr = pfwPort->put_Name(bstrName);
    ExitOnFailure(hr, "failed to set exception name");

    // get the firewall profile, its current list of open ports, and add ours
    hr = GetCurrentFirewallProfile(fIgnoreFailures, &pfwProfile);
    ExitOnFailure(hr, "failed to get firewall profile");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    hr = pfwProfile->get_GloballyOpenPorts(&pfwPorts);
    ExitOnFailure(hr, "failed to get open ports");

    hr = pfwPorts->Add(pfwPort);
    ExitOnFailure(hr, "failed to add exception to global list");

LExit:
    ReleaseBSTR(bstrRemoteAddresses);
    ReleaseBSTR(bstrName);
    ReleaseObject(pfwProfile);
    ReleaseObject(pfwPorts);
    ReleaseObject(pfwPort);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 RemoveException - Removes the exception rule with the given name.

********************************************************************/
static HRESULT RemoveException(
    __in LPCWSTR wzName, 
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;;
    INetFwRules* pNetFwRules = NULL;

    // convert to BSTRs to make COM happy
    BSTR bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for path");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    hr = pNetFwRules->Remove(bstrName);
    ExitOnFailure(hr, "failed to remove authorized app");

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 RemoveApplicationExceptionFromCurrentProfile

********************************************************************/
static HRESULT RemoveApplicationExceptionFromCurrentProfile(
    __in LPCWSTR wzFile, 
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;
    INetFwProfile* pfwProfile = NULL;
    INetFwAuthorizedApplications* pfwApps = NULL;

    // convert to BSTRs to make COM happy
    BSTR bstrFile = ::SysAllocString(wzFile);
    ExitOnNull(bstrFile, hr, E_OUTOFMEMORY, "failed SysAllocString for path");

    // get the firewall profile, which is our entry point for removing exceptions
    hr = GetCurrentFirewallProfile(fIgnoreFailures, &pfwProfile);
    ExitOnFailure(hr, "failed to get firewall profile");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // now get the list of app exceptions and remove the one
    hr = pfwProfile->get_AuthorizedApplications(&pfwApps);
    ExitOnFailure(hr, "failed to get list of authorized apps");

    hr = pfwApps->Remove(bstrFile);
    ExitOnFailure(hr, "failed to remove authorized app");

LExit:
    ReleaseBSTR(bstrFile);
    ReleaseObject(pfwApps);
    ReleaseObject(pfwProfile);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 RemovePortExceptionFromCurrentProfile

********************************************************************/
static HRESULT RemovePortExceptionFromCurrentProfile(
    __in int iPort,
    __in int iProtocol,
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;
    INetFwProfile* pfwProfile = NULL;
    INetFwOpenPorts* pfwPorts = NULL;

    // get the firewall profile, which is our entry point for adding exceptions
    hr = GetCurrentFirewallProfile(fIgnoreFailures, &pfwProfile);
    ExitOnFailure(hr, "failed to get firewall profile");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    hr = pfwProfile->get_GloballyOpenPorts(&pfwPorts);
    ExitOnFailure(hr, "failed to get open ports");

    hr = pfwPorts->Remove(iPort, static_cast<NET_FW_IP_PROTOCOL>(iProtocol));
    ExitOnFailure2(hr, "failed to remove open port %d, protocol %d", iPort, iProtocol);

LExit:
    return fIgnoreFailures ? S_OK : hr;
}

static HRESULT AddApplicationException(
    __in BOOL fSupportProfiles,
    __in LPCWSTR wzFile, 
    __in LPCWSTR wzName,
    __in int iProfile,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in LPCWSTR wzDescription
    )
{
    HRESULT hr = S_OK;

    if (fSupportProfiles)
    {
        hr = AddApplicationException(wzFile, wzName, iProfile, wzRemoteAddresses, fIgnoreFailures, wzPort, iProtocol, wzDescription);
    }
    else
    {
        if (0 != *wzPort || MSI_NULL_INTEGER != iProtocol)
        {
            // NOTE: This is treated as an error rather than either creating a rule based on just the application (no port), or 
            // just the port because it is unclear what is the proper fall back. For example, suppose that you have code that 
            // runs in dllhost.exe. Clearly falling back to opening all of dllhost is wrong. Because the firewall is a security
            // feature, it seems better to require the MSI author to indicate the behavior that they want.
            WcaLog(LOGMSG_STANDARD, "FirewallExtension: Cannot add firewall rule '%ls', which defines both an application and a port or protocol. Such a rule requires Microsoft Windows Vista or later.", wzName);
            return fIgnoreFailures ? S_OK : E_NOTIMPL;
        }

        hr = AddApplicationExceptionOnCurrentProfile(wzFile, wzName, wzRemoteAddresses, fIgnoreFailures);
    }

    return hr;
}

static HRESULT AddPortException(
    __in BOOL fSupportProfiles,
    __in LPCWSTR wzName,
    __in int iProfile,
    __in_opt LPCWSTR wzRemoteAddresses,
    __in BOOL fIgnoreFailures,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in LPCWSTR wzDescription
    )
{
    HRESULT hr = S_OK;

    if (fSupportProfiles)
    {
        hr = AddPortException(wzName, iProfile, wzRemoteAddresses, fIgnoreFailures, wzPort, iProtocol, wzDescription);
    }
    else
    {
        hr = AddPortExceptionOnCurrentProfile(wzName, wzRemoteAddresses, fIgnoreFailures, wcstol(wzPort, NULL, 10), iProtocol);
    }

    return hr;
}

static HRESULT RemoveApplicationException(
    __in BOOL fSupportProfiles,
    __in LPCWSTR wzName,
    __in LPCWSTR wzFile, 
    __in BOOL fIgnoreFailures,
    __in LPCWSTR wzPort,
    __in int iProtocol
    )
{
    HRESULT hr = S_OK;

    if (fSupportProfiles)
    {
        hr = RemoveException(wzName, fIgnoreFailures);
    }
    else
    {
        if (0 != *wzPort || MSI_NULL_INTEGER != iProtocol)
        {
            WcaLog(LOGMSG_STANDARD, "FirewallExtension: Cannot remove firewall rule '%ls', which defines both an application and a port or protocol. Such a rule requires Microsoft Windows Vista or later.", wzName);
            return S_OK;
        }

        hr = RemoveApplicationExceptionFromCurrentProfile(wzFile, fIgnoreFailures);
    }

    return hr;
}

static HRESULT RemovePortException(
    __in BOOL fSupportProfiles,
    __in LPCWSTR wzName,
    __in LPCWSTR wzPort,
    __in int iProtocol,
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;

    if (fSupportProfiles)
    {
        hr = RemoveException(wzName, fIgnoreFailures);
    }
    else
    {
        hr = RemovePortExceptionFromCurrentProfile(wcstol(wzPort, NULL, 10), iProtocol, fIgnoreFailures);
    }

    return hr;
}

/******************************************************************
 ExecFirewallExceptions - deferred custom action entry point to 
   register and remove firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall ExecFirewallExceptions(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    BOOL fSupportProfiles = FALSE;
    LPWSTR pwz = NULL;
    LPWSTR pwzCustomActionData = NULL;
    int iTodo = WCA_TODO_UNKNOWN;
    LPWSTR pwzName = NULL;
    LPWSTR pwzRemoteAddresses = NULL;
    int iAttributes = 0;
    int iTarget = fetUnknown;
    LPWSTR pwzFile = NULL;
    LPWSTR pwzPort = NULL;
    LPWSTR pwzDescription = NULL;
    int iProtocol = 0;
    int iProfile = 0;

    // initialize
    hr = WcaInitialize(hInstall, "ExecFirewallExceptions");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");

    // Find out if we support profiles (only on Vista or later)
    fSupportProfiles = FSupportProfiles();

    // loop through all the passed in data
    pwz = pwzCustomActionData;
    while (pwz && *pwz)
    {
        // extract the custom action data and if rolling back, swap INSTALL and UNINSTALL
        hr = WcaReadIntegerFromCaData(&pwz, &iTodo);
        ExitOnFailure(hr, "failed to read todo from custom action data");
        if (::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK))
        {
            if (WCA_TODO_INSTALL == iTodo)
            {
                iTodo = WCA_TODO_UNINSTALL;
            }
            else if (WCA_TODO_UNINSTALL == iTodo)
            {
                iTodo = WCA_TODO_INSTALL;
            }
        }

        hr = WcaReadStringFromCaData(&pwz, &pwzName);
        ExitOnFailure(hr, "failed to read name from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &iProfile);
        ExitOnFailure(hr, "failed to read profile from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &pwzRemoteAddresses);
        ExitOnFailure(hr, "failed to read remote addresses from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
        ExitOnFailure(hr, "failed to read attributes from custom action data");
        BOOL fIgnoreFailures = feaIgnoreFailures == (iAttributes & feaIgnoreFailures);

        hr = WcaReadIntegerFromCaData(&pwz, &iTarget);
        ExitOnFailure(hr, "failed to read target from custom action data");

        if (iTarget == fetApplication)
        {
            hr = WcaReadStringFromCaData(&pwz, &pwzFile);
            ExitOnFailure(hr, "failed to read file path from custom action data");
        }

        hr = WcaReadStringFromCaData(&pwz, &pwzPort);
        ExitOnFailure(hr, "failed to read port from custom action data");
        hr = WcaReadIntegerFromCaData(&pwz, &iProtocol);
        ExitOnFailure(hr, "failed to read protocol from custom action data");
        hr = WcaReadStringFromCaData(&pwz, &pwzDescription);
        ExitOnFailure(hr, "failed to read protocol from custom action data");

        switch (iTarget)
        {
        case fetPort:
            switch (iTodo)
            {
            case WCA_TODO_INSTALL:
            case WCA_TODO_REINSTALL:
                WcaLog(LOGMSG_STANDARD, "Installing firewall exception2 %ls on port %ls, protocol %d", pwzName, pwzPort, iProtocol);
                hr = AddPortException(fSupportProfiles, pwzName, iProfile, pwzRemoteAddresses, fIgnoreFailures, pwzPort, iProtocol, pwzDescription);
                ExitOnFailure3(hr, "failed to add/update port exception for name '%ls' on port %ls, protocol %d", pwzName, pwzPort, iProtocol);
                break;

            case WCA_TODO_UNINSTALL:
                WcaLog(LOGMSG_STANDARD, "Uninstalling firewall exception2 %ls on port %ls, protocol %d", pwzName, pwzPort, iProtocol);
                hr = RemovePortException(fSupportProfiles, pwzName, pwzPort, iProtocol, fIgnoreFailures);
                ExitOnFailure3(hr, "failed to remove port exception for name '%ls' on port %ls, protocol %d", pwzName, pwzPort, iProtocol);
                break;
            }
            break;

        case fetApplication:
            switch (iTodo)
            {
            case WCA_TODO_INSTALL:
            case WCA_TODO_REINSTALL:
                WcaLog(LOGMSG_STANDARD, "Installing firewall exception2 %ls (%ls)", pwzName, pwzFile);
                hr = AddApplicationException(fSupportProfiles, pwzFile, pwzName, iProfile, pwzRemoteAddresses, fIgnoreFailures, pwzPort, iProtocol, pwzDescription);
                ExitOnFailure2(hr, "failed to add/update application exception for name '%ls', file '%ls'", pwzName, pwzFile);
                break;

            case WCA_TODO_UNINSTALL:
                WcaLog(LOGMSG_STANDARD, "Uninstalling firewall exception2 %ls (%ls)", pwzName, pwzFile);
                hr = RemoveApplicationException(fSupportProfiles, pwzName, pwzFile, fIgnoreFailures, pwzPort, iProtocol);
                ExitOnFailure2(hr, "failed to remove application exception for name '%ls', file '%ls'", pwzName, pwzFile);
                break;
            }
            break;
        }
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzRemoteAddresses);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzPort);
    ReleaseStr(pwzDescription);
    ::CoUninitialize();

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}
