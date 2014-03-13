//-------------------------------------------------------------------------------------------------
// <copyright file="scawebprop7.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Web directory property functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

HRESULT ScaWriteWebDirProperties7(
    __in_z LPCWSTR wzWebName,
    __in_z LPCWSTR wzRootOfWeb,
    __in const SCA_WEB_PROPERTIES* pswp
    )
{
    HRESULT hr = S_OK;
    WCHAR wz[METADATA_MAX_NAME_LEN + 1];

    //all go to same web/root location tag
    hr = ScaWriteConfigID(IIS_DIRPROP_BEGIN);
    ExitOnFailure(hr, "Failed to write DirProp begin id");
    hr = ScaWriteConfigString(wzWebName);                //site name key
    ExitOnFailure(hr, "Failed to write DirProp web key");
    hr = ScaWriteConfigString(wzRootOfWeb);               //app path key
    ExitOnFailure(hr, "Failed to write DirProp app key");

    // write the access permissions to the metabase
    if (MSI_NULL_INTEGER != pswp->iAccess)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_ACCESS);
        ExitOnFailure(hr, "Failed to write DirProp access id");
        hr = ScaWriteConfigInteger(pswp->iAccess);
        ExitOnFailure(hr, "Failed to write access permissions for Web");
    }

    if (MSI_NULL_INTEGER != pswp->iAuthorization)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_AUTH);
        ExitOnFailure(hr, "Failed to write DirProp auth id");
        hr = ScaWriteConfigInteger(pswp->iAuthorization);
        ExitOnFailure(hr, "Failed to write authorization for Web");
    }

    if (pswp->fHasUser)
    {
        Assert(pswp->scau.wzName);
        // write the user name
        if (*pswp->scau.wzDomain)
        {
            hr = ::StringCchPrintfW(wz, countof(wz), L"%s\\%s", pswp->scau.wzDomain, pswp->scau.wzName);
            ExitOnFailure(hr, "Failed to format domain\\username string");
        }
        else
        {
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
            hr = ::StringCchCopyW(wz, countof(wz), pswp->scau.wzName);
            ExitOnFailure(hr, "Failed to copy user name");
        }
        hr = ScaWriteConfigID(IIS_DIRPROP_USER);
        ExitOnFailure(hr, "Failed to write DirProp user id");
        hr = ScaWriteConfigString(wz);
        ExitOnFailure(hr, "Failed to write anonymous user name for Web");

        // write the password
        hr = ScaWriteConfigID(IIS_DIRPROP_PWD);
        ExitOnFailure(hr, "Failed to write DirProp pwd id");
        hr = ScaWriteConfigString(pswp->scau.wzPassword);
        ExitOnFailure(hr, "Failed to write anonymous user password for Web");

        if (pswp->fIIsControlledPassword)
        {
            //Not Supported by IIS7 : pswp->fIIsControlledPassword
            WcaLog(LOGMSG_VERBOSE, "Not supported by IIS7: WebDirProperties.IIsControlledPassword, ignoring");
        }
    }

    if (MSI_NULL_INTEGER != pswp->fLogVisits)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_LOGVISITS);
        ExitOnFailure(hr, "Failed to write DirProp logVisits id");
        hr = ScaWriteConfigInteger(pswp->fLogVisits ? FALSE : TRUE); // we capture "should log" but IIS7 wants "should not log"
        ExitOnFailure(hr, "Failed to write DirProp logVisits");
    }

    if (MSI_NULL_INTEGER != pswp->fIndex)
    {
        //Not Supported by IIS7 : pswp->fIndex
        WcaLog(LOGMSG_VERBOSE, "Not supported by IIS7: WebDirProperties.Index, ignoring");
    }

    if (pswp->fHasDefaultDoc)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_DEFDOCS);
        ExitOnFailure(hr, "Failed to write DirProp defdocs id");
        hr = ScaWriteConfigString(pswp->wzDefaultDoc);
        ExitOnFailure(hr, "Failed to write default documents for Web");
    }

    if (MSI_NULL_INTEGER != pswp->fAspDetailedError)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_ASPERROR);
        ExitOnFailure(hr, "Failed to write ASP script error id");
        hr = ScaWriteConfigInteger(pswp->fAspDetailedError);
        ExitOnFailure(hr, "Failed to write ASP script error for Web");
    }

    if (pswp->fHasHttpExp)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_HTTPEXPIRES);
        ExitOnFailure(hr, "Failed to write DirProp HttpExpires id");
        hr = ScaWriteConfigString(pswp->wzHttpExp);
        ExitOnFailure(hr, "Failed to write DirProp HttpExpires value");
    }

    if (MSI_NULL_INTEGER != pswp->iCacheControlMaxAge)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_MAXAGE);
        ExitOnFailure(hr, "Failed to write DirProp MaxAge id");
        hr = ScaWriteConfigInteger(pswp->iCacheControlMaxAge);
        ExitOnFailure(hr, "Failed to write DirProp MaxAge value");
    }

    if (pswp->fHasCacheControlCustom)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_CACHECUST);
        ExitOnFailure(hr, "Failed to write DirProp Cache Control Custom id");
        hr = ScaWriteConfigString(pswp->wzCacheControlCustom);
        ExitOnFailure(hr, "Failed to write Cache Control Custom for Web");
    }

    if (pswp->fNoCustomError)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_NOCUSTERROR);
        ExitOnFailure(hr, "Failed to write DirProp clear Cust Errors id");
    }

    if (MSI_NULL_INTEGER != pswp->iAccessSSLFlags)
    {
        hr = ScaWriteConfigID(IIS_DIRPROP_SSLFLAGS);
        ExitOnFailure(hr, "Failed to write DirProp sslFlags id");
        hr = ScaWriteConfigInteger(pswp->iAccessSSLFlags);
        ExitOnFailure(hr, "Failed to write AccessSSLFlags for Web");
    }

    if (*pswp->wzAuthenticationProviders)
    {
        hr = ::StringCchCopyW(wz, countof(wz), pswp->wzAuthenticationProviders);
        ExitOnFailure(hr, "Failed to copy authentication providers string");
        hr = ScaWriteConfigID(IIS_DIRPROP_AUTHPROVID);
        ExitOnFailure(hr, "Failed to write DirProp AuthProvid id");
        hr = ScaWriteConfigString(wz);
        ExitOnFailure(hr, "Failed to write AuthenticationProviders for Web");
    }
    //End of Dir Properties
    hr = ScaWriteConfigID(IIS_DIRPROP_END);
    ExitOnFailure(hr, "Failed to write DirProp end id");

LExit:
    return hr;
}
