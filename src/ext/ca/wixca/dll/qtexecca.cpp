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

HRESULT BuildCommandLine(
    __in BOOL fIs64bit,
    __out LPWSTR *ppwzCommand
    )
{
    Assert(ppwzCommand);

    HRESULT hr = S_OK;
    BOOL fScheduled = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_SCHEDULED);
    BOOL fRollback = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_ROLLBACK);
    BOOL fCommit = ::MsiGetMode(WcaGetInstallHandle(), MSIRUNMODE_COMMIT);
    LPCSTR szProperty = fIs64bit ? "QtExec64CmdLine" : "QtExecCmdLine";
    LPCWSTR wzProperty = fIs64bit ? L"QtExec64CmdLine" : L"QtExecCmdLine";

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

DWORD GetTimeout()
{
    DWORD dwTimeout = ONEMINUTE;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    if (WcaIsPropertySet("QtExecCmdTimeout"))
    {
        hr = WcaGetProperty( L"QtExecCmdTimeout", &pwzData);
        ExitOnFailure(hr, "failed to get QtExecCmdTimeout");

        if ((dwTimeout = (DWORD)_wtoi(pwzData)) == 0)
        {
            dwTimeout = ONEMINUTE;
        }
    }

LExit:
    ReleaseStr(pwzData);

    return dwTimeout;

}

extern "C" UINT __stdcall CAQuietExec(
    __in MSIHANDLE hInstall
    )
{
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzCommand = NULL;
    DWORD dwTimeout = 0;

    hr = WcaInitialize(hInstall,"CAQuietExec");
    ExitOnFailure(hr, "failed to initialize");

    hr = BuildCommandLine(FALSE, &pwzCommand);
    ExitOnFailure(hr, "failed to get Command Line");

    dwTimeout = GetTimeout();

    hr = QuietExec(pwzCommand, dwTimeout);
    ExitOnFailure(hr, "CAQuietExec Failed");

LExit:
    ReleaseStr(pwzCommand);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er); 
}

extern "C" UINT __stdcall CAQuietExec64(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "Debug here.");
    Assert(hInstall);
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzCommand = NULL;
    DWORD dwTimeout = 0;

    BOOL fIsWow64Initialized = FALSE;
    BOOL fRedirected = FALSE;

    hr = WcaInitialize(hInstall,"CAQuietExec64");
    ExitOnFailure(hr, "failed to initialize");

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

    hr = BuildCommandLine(TRUE, &pwzCommand);
    ExitOnFailure(hr, "failed to get Command Line");

    dwTimeout = GetTimeout();

    hr = QuietExec(pwzCommand, dwTimeout);
    ExitOnFailure(hr, "CAQuietExec64 Failed");

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

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}
