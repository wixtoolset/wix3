// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT ScaWebSvcExtInstall(
    const SCA_WEBSVCEXT* psWseList
    );

static HRESULT ScaWebSvcExtUninstall(
    const SCA_WEBSVCEXT* psWseList
    );

// functions
// Commit does both install and uninstall
HRESULT __stdcall ScaWebSvcExtCommit7(
    __in SCA_WEBSVCEXT* psWseList
    )
{
    HRESULT hr = S_OK;

    if (!psWseList)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaWebSvcExtCommit() because there are no web service extensions in the list");
        ExitFunction();
    }

    // Make changes to local copy of metabase
    while (psWseList)
    {
        if (WcaIsInstalling(psWseList->isInstalled, psWseList->isAction))
        {
            hr = ScaWebSvcExtInstall(psWseList);
            ExitOnFailure(hr, "Failed to install Web Service extension");
        }
        else if (WcaIsUninstalling(psWseList->isInstalled, psWseList->isAction))
        {
            hr = ScaWebSvcExtUninstall(psWseList);
            ExitOnFailure(hr, "Failed to uninstall Web Service extension");
        }

        psWseList = psWseList->psWseNext;
    }


LExit:

    return hr;
}


static HRESULT ScaWebSvcExtInstall(
    const SCA_WEBSVCEXT* psWseList
    )
{
    HRESULT hr = S_OK;
    int iAllow;

    //Write CAData actions
    hr = ScaWriteConfigID(IIS_WEB_SVC_EXT);
    ExitOnFailure(hr, "failed add web svc ext ID");
    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "failed add web svc ext action");

    // write File path
    hr = ScaWriteConfigString(psWseList->wzFile);
    ExitOnFailure(hr, "failed add web svc ext file path");

    // write allowed
    // unDeleatable n/a in IIS7
    iAllow = (psWseList->iAttributes & 1);
    hr = ScaWriteConfigInteger(iAllow);
    ExitOnFailure(hr, "failed add web svc ext Allowed");

    //write group
    hr = ScaWriteConfigString(psWseList->wzGroup);
    ExitOnFailure(hr, "failed add web svc ext group");

    //write description
    hr = ScaWriteConfigString(psWseList->wzDescription);
    ExitOnFailure(hr, "failed add web svc ext description");

LExit:

    return hr;
}


static HRESULT ScaWebSvcExtUninstall(
    const SCA_WEBSVCEXT* psWseList
    )
{
    HRESULT hr = S_OK;

    //Write CAData actions
    hr = ScaWriteConfigID(IIS_WEB_SVC_EXT);
    ExitOnFailure(hr, "failed add web svc ext ID");
    hr = ScaWriteConfigID(IIS_DELETE);
    ExitOnFailure(hr, "failed add web svc ext action");

    // write File path (Key)
    hr = ScaWriteConfigString(psWseList->wzFile);
    ExitOnFailure(hr, "failed add web svc ext file path");

LExit:
    return hr;
}
