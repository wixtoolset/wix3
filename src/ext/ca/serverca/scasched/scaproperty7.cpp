//-------------------------------------------------------------------------------------------------
// <copyright file="scaproperty7.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS Property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

HRESULT ScaPropertyInstall7(
    SCA_PROPERTY* pspList
    )
{
    HRESULT hr = S_OK;

    for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
    {
        // if we are installing the web site
        if (WcaIsInstalling(psp->isInstalled, psp->isAction))
        {
            hr = ScaWriteProperty7(psp);
            ExitOnFailure1(hr, "failed to write Property '%ls' ", psp->wzProperty);
        }
    }

LExit:
    return hr;
}


HRESULT ScaPropertyUninstall7(
    SCA_PROPERTY* pspList
    )
{
    HRESULT hr = S_OK;

    for (SCA_PROPERTY* psp = pspList; psp; psp = psp->pspNext)
    {
        // if we are uninstalling the web site
        if (WcaIsUninstalling(psp->isInstalled, psp->isAction))
        {
            hr = ScaRemoveProperty7(psp);
            ExitOnFailure1(hr, "Failed to remove Property '%ls'", psp->wzProperty);
        }
    }

LExit:
    return hr;
}


HRESULT ScaWriteProperty7(
    const SCA_PROPERTY* psp
    )
{
    HRESULT hr = S_OK;
    DWORD dwValue;
    LPWSTR wz = NULL;

    ExitOnNull(psp, hr, E_INVALIDARG, "Failed to write property because no property to write was given");
    //
    // Figure out what setting we're writing and write it
    //
    if (0 == wcscmp(psp->wzProperty, wzIISPROPERTY_IIS5_ISOLATION_MODE))
    {
        // IIs5IsolationMode not supported
        WcaLog(LOGMSG_VERBOSE, "Not supported by IIS7: IIs5IsolationMode, ignoring");
    }
    else if (0 == wcscmp(psp->wzProperty, wzIISPROPERTY_MAX_GLOBAL_BANDWIDTH))
    {
        dwValue = wcstoul(psp->wzValue, &wz, 10) * 1024; // remember, the value shown is in kilobytes, the value saved is in bytes
        hr = ScaWriteConfigID(IIS_PROPERTY);
        ExitOnFailure(hr, "failed to set Property ID");
        hr = ScaWriteConfigID(IIS_PROPERTY_MAXBAND);
        ExitOnFailure(hr, "failed to set Property MSXBAND ID");
        hr = ScaWriteConfigInteger(dwValue);
        ExitOnFailure(hr, "failed to set Property MSXBAND value");
    }
    else if (0 == wcscmp(psp->wzProperty, wzIISPROPERTY_LOG_IN_UTF8))
    {
        dwValue = 1;
        hr = ScaWriteConfigID(IIS_PROPERTY);
        ExitOnFailure(hr, "failed to set Property ID");
        hr = ScaWriteConfigID(IIS_PROPERTY_LOGUTF8);
        ExitOnFailure(hr, "failed to set Property LOG ID");
        hr = ScaWriteConfigInteger(dwValue);
        ExitOnFailure(hr, "failed to set Property Log value");
    }
    else if (0 == wcscmp(psp->wzProperty, wzIISPROPERTY_ETAG_CHANGENUMBER))
    {
        //EtagChangenumber not supported
        WcaLog(LOGMSG_VERBOSE, "Not supported by IIS7: EtagChangenumber, ignoring");
    }

LExit:
    return hr;
}

HRESULT ScaRemoveProperty7(
    __in SCA_PROPERTY* /*psp*/
    )
{

    // NOP function for now
    //The two global values being set by WebProperty:
    //    <iis:WebProperty Id="MaxGlobalBandwidth" Value="1024" />
    //    <iis:WebProperty Id ="LogInUTF8" />
    // should should not be removed on uninstall.

    HRESULT hr = S_OK;

    return hr;
}
