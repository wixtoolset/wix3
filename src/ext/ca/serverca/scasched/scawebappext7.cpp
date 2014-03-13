//-------------------------------------------------------------------------------------------------
// <copyright file="scawebappext7.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Web Application Extension functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

HRESULT ScaWebAppExtensionsWrite7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRootOfWeb,
    __in SCA_WEB_APPLICATION_EXTENSION* pswappextList
    )
{
    HRESULT hr = S_OK;
    SCA_WEB_APPLICATION_EXTENSION* pswappext = NULL;

    if (!pswappextList)
    {
        ExitFunction1(hr = S_OK);
    }

    //create the Extension for this vdir application
    //all go to same web/root location tag
    hr = ScaWriteConfigID(IIS_APPEXT_BEGIN);
    ExitOnFailure(hr, "Failed to write webappext begin id");
    hr = ScaWriteConfigString(wzWebName);                //site name key
    ExitOnFailure(hr, "Failed to write app web key");
    hr = ScaWriteConfigString(wzRootOfWeb);               //app path key
    ExitOnFailure(hr, "Failed to write app web key");

    pswappext = pswappextList;

    while (pswappext)
    {
        //create the Extension for this vdir application
        hr = ScaWriteConfigID(IIS_APPEXT);
        ExitOnFailure(hr, "Failed to write webappext begin id");

        if (*pswappext->wzExtension)
        {
            hr = ScaWriteConfigString(pswappext->wzExtension);
        }
        else   // blank means "*" (all)
        {
            hr = ScaWriteConfigString(L"*");
        }
        ExitOnFailure(hr, "Failed to write extension");

        hr = ScaWriteConfigString(pswappext->wzExecutable);
        ExitOnFailure(hr, "Failed to write extension executable");

        hr = ScaWriteConfigString(pswappext->wzVerbs);
        ExitOnFailure(hr, "Failed to write extension verbs");

        pswappext = pswappext->pswappextNext;
    }

    hr = ScaWriteConfigID(IIS_APPEXT_END);
    ExitOnFailure(hr, "Failed to write webappext begin id");

LExit:
    return hr;
}

