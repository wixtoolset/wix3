// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
HRESULT ScaWriteWebApplication7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRootOfWeb,
    SCA_WEB_APPLICATION* pswapp,
    SCA_APPPOOL * /*psapList*/
    )
{
    HRESULT hr = S_OK;

    //all go to same web/root location tag
    hr = ScaWriteConfigID(IIS_ASP_BEGIN);
    ExitOnFailure(hr, "Failed to write WebApp ASP begin id");
    hr = ScaWriteConfigString(wzWebName);                //site name key
    ExitOnFailure(hr, "Failed to write app web key");
    hr = ScaWriteConfigString(wzRootOfWeb);               //app path key
    ExitOnFailure(hr, "Failed to write app web root");

    // IIS7 Not Supported: Isolation
    if (MSI_NULL_INTEGER != pswapp->iIsolation)
    {
        WcaLog(LOGMSG_TRACEONLY, "Not supported by IIS7: Isolation Mode, ignoring");
    }

    // allow session state
    if (MSI_NULL_INTEGER != pswapp->fAllowSessionState)
    {
        //system.webServer/asp /session | allowSessionState
        hr = ScaWriteConfigID(IIS_ASP_SESSIONSTATE);
        ExitOnFailure(hr, "Failed to write WebApp ASP sessionstate id");
        hr = ScaWriteConfigInteger(pswapp->fAllowSessionState);
        ExitOnFailure1(hr, "Failed to write allow session information for App: '%ls'", pswapp->wzName);
    }

    // session timeout
    if (MSI_NULL_INTEGER != pswapp->iSessionTimeout)
    {
        //system.webServer/asp /session | timeout
        hr = ScaWriteConfigID(IIS_ASP_SESSIONTIMEOUT);
        ExitOnFailure(hr, "Failed to write WebApp ASP sessiontimepot id");
        hr = ScaWriteConfigInteger(pswapp->iSessionTimeout);
        ExitOnFailure1(hr, "Failed to write session timeout for App: '%ls'", pswapp->wzName);
    }

    // asp buffering
    if (MSI_NULL_INTEGER != pswapp->fBuffer)
    {
        //system.webServer/asp | bufferingOn
        hr = ScaWriteConfigID(IIS_ASP_BUFFER);
        ExitOnFailure(hr, "Failed to write WebApp ASP buffer id");
        hr = ScaWriteConfigInteger(pswapp->fBuffer);
        ExitOnFailure1(hr, "Failed to write buffering flag for App: '%ls'", pswapp->wzName);
    }

    // asp parent paths
    if (MSI_NULL_INTEGER != pswapp->fParentPaths)
    {
        //system.webServer/asp | enableParentPaths
        hr = ScaWriteConfigID(IIS_ASP_PARENTPATHS);
        ExitOnFailure(hr, "Failed to write WebApp ASP parentpaths id");
        hr = ScaWriteConfigInteger(pswapp->fParentPaths);
        ExitOnFailure1(hr, "Failed to write parent paths flag for App: '%ls'", pswapp->wzName);
    }

    // default scripting language
    if (*pswapp->wzDefaultScript)
    {
        //system.webServer/asp | scriptLanguage
        hr = ScaWriteConfigID(IIS_ASP_SCRIPTLANG);
        ExitOnFailure(hr, "Failed to write WebApp ASP script lang id");
        hr = ScaWriteConfigString(pswapp->wzDefaultScript);
        ExitOnFailure1(hr, "Failed to write default scripting language for App: '%ls'", pswapp->wzName);
    }

    // asp script timeout
    if (MSI_NULL_INTEGER != pswapp->iScriptTimeout)
    {
        //system.webServer/asp /limits | scriptTimeout
        hr = ScaWriteConfigID(IIS_ASP_SCRIPTTIMEOUT);
        ExitOnFailure(hr, "Failed to write WebApp ASP script timeout id");
        hr = ScaWriteConfigInteger(pswapp->iScriptTimeout);
        ExitOnFailure1(hr, "Failed to write script timeout for App: '%ls'", pswapp->wzName);
    }

    // asp server-side script debugging
    if (MSI_NULL_INTEGER != pswapp->fServerDebugging)
    {
        //system.webServer/asp | appAllowDebugging
        hr = ScaWriteConfigID(IIS_ASP_SCRIPTSERVERDEBUG);
        ExitOnFailure(hr, "Failed to write WebApp ASP script debug id");
        hr = ScaWriteConfigInteger(pswapp->fServerDebugging);
        ExitOnFailure1(hr, "Failed to write ASP server-side script debugging flag for App: '%ls'", pswapp->wzName);
    }

    // asp client-side script debugging
    if (MSI_NULL_INTEGER != pswapp->fClientDebugging)
    {
        //system.webServer/asp | appAllowClientDebug
        hr = ScaWriteConfigID(IIS_ASP_SCRIPTCLIENTDEBUG);
        ExitOnFailure(hr, "Failed to write WebApp ASP script debug id");
        hr = ScaWriteConfigInteger(pswapp->fClientDebugging);
        ExitOnFailure1(hr, "Failed to write ASP client-side script debugging flag for App: '%ls'", pswapp->wzName);
    }

    //done with ASP application properties
    hr = ScaWriteConfigID(IIS_ASP_END);
    ExitOnFailure(hr, "Failed to write WebApp ASP begin id");

    //write out app estensions
    if (pswapp->pswappextList)
    {
        hr = ScaWebAppExtensionsWrite7(wzWebName, wzRootOfWeb, pswapp->pswappextList);
        ExitOnFailure1(hr, "Failed to write AppExtensions for App: '%ls'", pswapp->wzName);
    }

LExit:
    return hr;
}
