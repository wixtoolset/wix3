//-------------------------------------------------------------------------------------------------
// <copyright file="qtexecca.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Executes command line instructions without popping up a shell.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define OUTPUT_BUFFER 1024

// These old "CA" prefix names are deprecated, and intended to go away in wix 4.0, only staying now for compatibility reasons
const LPCSTR CAQUIET_TIMEOUT_PROPERTY = "QtExecCmdTimeout";
const LPCWSTR CAQUIET_TIMEOUT_PROPERTY_WIDE = L"QtExecCmdTimeout";
const LPCSTR CAQUIET_ARGUMENTS_PROPERTY = "QtExecCmdLine";
const LPCSTR CAQUIET64_ARGUMENTS_PROPERTY = "QtExec64CmdLine";
const LPCWSTR CAQUIET_ARGUMENTS_PROPERTY_WIDE = L"QtExecCmdLine";
const LPCWSTR CAQUIET64_ARGUMENTS_PROPERTY_WIDE = L"QtExec64CmdLine";
// end deprecated section

// WixCA name quiet commandline argument properties
const LPCSTR WIX_QUIET_ARGUMENTS_PROPERTY = "WixQuietExecCmdLine";
const LPCSTR WIX_QUIET64_ARGUMENTS_PROPERTY = "WixQuietExec64CmdLine";
const LPCWSTR WIX_QUIET_ARGUMENTS_PROPERTY_WIDE = L"WixQuietExecCmdLine";
const LPCWSTR WIX_QUIET64_ARGUMENTS_PROPERTY_WIDE = L"WixQuietExec64CmdLine";

// WixCA quiet timeout properties
const LPCSTR WIX_QUIET_TIMEOUT_PROPERTY = "WixQuietExecCmdTimeout";
const LPCSTR WIX_QUIET64_TIMEOUT_PROPERTY = "WixQuietExec64CmdTimeout";
const LPCWSTR WIX_QUIET_TIMEOUT_PROPERTY_WIDE = L"WixQuietExecCmdTimeout";
const LPCWSTR WIX_QUIET64_TIMEOUT_PROPERTY_WIDE = L"WixQuietExec64CmdTimeout";

// WixCA silent commandline argument properties
const LPCSTR WIX_SILENT_ARGUMENTS_PROPERTY = "WixSilentExecCmdLine";
const LPCSTR WIX_SILENT64_ARGUMENTS_PROPERTY = "WixSilentExec64CmdLine";
const LPCWSTR WIX_SILENT_ARGUMENTS_PROPERTY_WIDE = L"WixSilentExecCmdLine";
const LPCWSTR WIX_SILENT64_ARGUMENTS_PROPERTY_WIDE = L"WixSilentExec64CmdLine";

// WixCA silent timeout properties
const LPCSTR WIX_SILENT_TIMEOUT_PROPERTY = "WixSilentExecCmdTimeout";
const LPCSTR WIX_SILENT64_TIMEOUT_PROPERTY = "WixSilentExec64CmdTimeout";
const LPCWSTR WIX_SILENT_TIMEOUT_PROPERTY_WIDE = L"WixSilentExecCmdTimeout";
const LPCWSTR WIX_SILENT64_TIMEOUT_PROPERTY_WIDE = L"WixSilentExec64CmdTimeout";

HRESULT BuildCommandLine(
    __in LPCSTR szProperty,
    __in LPCWSTR wzProperty,
    __out LPWSTR *ppwzCommand
    )
{
    Assert(ppwzCommand);

    HRESULT hr = S_OK;
    BOOL fScheduled = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_SCHEDULED);
    BOOL fRollback = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK);
    BOOL fCommit = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_COMMIT);

    if (fScheduled || fRollback || fCommit)
    {
        if (WcaIsPropertySet("CustomActionData"))
        {
            hr = WcaGetProperty( L"CustomActionData", ppwzCommand);
            ExitOnFailure(hr, "failed to get CustomActionData");
        }
    }
    else if (WcaIsPropertySet(szProperty))
    {
        hr = WcaGetFormattedProperty(wzProperty, ppwzCommand);
        ExitOnFailure1(hr, "failed to get %ls", wzProperty);
        hr = WcaSetProperty(wzProperty, L""); // clear out the property now that we've read it
        ExitOnFailure1(hr, "failed to set %ls", wzProperty);
    }

    if (!*ppwzCommand)
    {
        ExitOnFailure(hr = E_INVALIDARG, "failed to get command line data");
    }

    if (L'"' != **ppwzCommand)
    {
        WcaLog(LOGMSG_STANDARD, "Command string must begin with quoted application name.");
        ExitOnFailure(hr = E_INVALIDARG, "invalid command line property value");
    }

LExit:
    return hr;
}

#define ONEMINUTE 60000

DWORD GetTimeout(LPCSTR szPropertyName, LPCWSTR wzPropertyName)
{
    DWORD dwTimeout = ONEMINUTE;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    if (WcaIsPropertySet(szPropertyName))
    {
        hr = WcaGetProperty(wzPropertyName, &pwzData);
        ExitOnFailure1(hr, "failed to get %ls", wzPropertyName);

        if ((dwTimeout = (DWORD)_wtoi(pwzData)) == 0)
        {
            dwTimeout = ONEMINUTE;
        }
    }

LExit:
    ReleaseStr(pwzData);

    return dwTimeout;

}

HRESULT ExecCommon(
    __in LPCSTR szArgumentsProperty,
    __in LPCWSTR wzArgumentsProperty,
    __in LPCSTR szTimeoutProperty,
    __in LPCWSTR wzTimeoutProperty,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzCommand = NULL;
    DWORD dwTimeout = 0;

    hr = BuildCommandLine(szArgumentsProperty, wzArgumentsProperty, &pwzCommand);
    ExitOnFailure(hr, "failed to get Command Line");

    dwTimeout = GetTimeout(szTimeoutProperty, wzTimeoutProperty);

    hr = QuietExec(pwzCommand, dwTimeout, fLogCommand, fLogOutput);
    ExitOnFailure(hr, "QuietExec Failed");

LExit:
    ReleaseStr(pwzCommand);

    return hr;
}

HRESULT ExecCommon64(
    __in LPCSTR szArgumentsProperty,
    __in LPCWSTR wzArgumentsProperty,
    __in LPCSTR szTimeoutProperty,
    __in LPCWSTR wzTimeoutProperty,
    __in BOOL fLogCommand,
    __in BOOL fLogOutput
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzCommand = NULL;
    DWORD dwTimeout = 0;
    BOOL fIsWow64Initialized = FALSE;
    BOOL fRedirected = FALSE;

    hr = WcaInitializeWow64();
    if (S_FALSE == hr)
    {
        hr = TYPE_E_DLLFUNCTIONNOTFOUND;
    }
    ExitOnFailure(hr, "failed to intialize WOW64.");
    fIsWow64Initialized = TRUE;

    hr = WcaDisableWow64FSRedirection();
    ExitOnFailure(hr, "failed to enable filesystem redirection.");
    fRedirected = TRUE;

    hr = BuildCommandLine(szArgumentsProperty, wzArgumentsProperty, &pwzCommand);
    ExitOnFailure(hr, "failed to get Command Line");

    dwTimeout = GetTimeout(szTimeoutProperty, wzTimeoutProperty);

    hr = QuietExec(pwzCommand, dwTimeout, fLogCommand, fLogOutput);
    ExitOnFailure(hr, "QuietExec64 Failed");

LExit:
    ReleaseStr(pwzCommand);

    if (fRedirected)
    {
        WcaRevertWow64FSRedirection();
    }

    if (fIsWow64Initialized)
    {
        WcaFinalizeWow64();
    }

    return hr;
}

// These two custom actions are deprecated, and should go away in wix v4.0. WixQuietExec replaces this one,
// and is not intended to have any difference in behavior apart from CA name and property names.
extern "C" UINT __stdcall CAQuietExec(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "CAQuietExec");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon(CAQUIET_ARGUMENTS_PROPERTY, CAQUIET_ARGUMENTS_PROPERTY_WIDE, CAQUIET_TIMEOUT_PROPERTY, CAQUIET_TIMEOUT_PROPERTY_WIDE, TRUE, TRUE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er); 
}

// 2nd deprecated custom action name, superseded by WixQuietExec64
extern "C" UINT __stdcall CAQuietExec64(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "CAQuietExec64");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon64(CAQUIET64_ARGUMENTS_PROPERTY, CAQUIET64_ARGUMENTS_PROPERTY_WIDE, CAQUIET_TIMEOUT_PROPERTY, CAQUIET_TIMEOUT_PROPERTY_WIDE, TRUE, TRUE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}

extern "C" UINT __stdcall WixQuietExec(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixQuietExec");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon(WIX_QUIET_ARGUMENTS_PROPERTY, WIX_QUIET_ARGUMENTS_PROPERTY_WIDE, WIX_QUIET_TIMEOUT_PROPERTY, WIX_QUIET_TIMEOUT_PROPERTY_WIDE, TRUE, TRUE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er); 
}

extern "C" UINT __stdcall WixQuietExec64(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixQuietExec64");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon64(WIX_QUIET64_ARGUMENTS_PROPERTY, WIX_QUIET64_ARGUMENTS_PROPERTY_WIDE, WIX_QUIET64_TIMEOUT_PROPERTY, WIX_QUIET64_TIMEOUT_PROPERTY_WIDE, TRUE, TRUE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}

extern "C" UINT __stdcall WixSilentExec(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixSilentExec");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon(WIX_SILENT_ARGUMENTS_PROPERTY, WIX_SILENT_ARGUMENTS_PROPERTY_WIDE, WIX_SILENT_TIMEOUT_PROPERTY, WIX_SILENT_TIMEOUT_PROPERTY_WIDE, FALSE, FALSE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er); 
}

extern "C" UINT __stdcall WixSilentExec64(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "WixSilentExec64");
    ExitOnFailure(hr, "failed to initialize");

    hr = ExecCommon64(WIX_SILENT64_ARGUMENTS_PROPERTY, WIX_SILENT64_ARGUMENTS_PROPERTY_WIDE, WIX_SILENT64_TIMEOUT_PROPERTY, WIX_SILENT64_TIMEOUT_PROPERTY_WIDE, FALSE, FALSE);
    ExitOnFailure(hr, "Failed in ExecCommon method");

LExit:
    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}
