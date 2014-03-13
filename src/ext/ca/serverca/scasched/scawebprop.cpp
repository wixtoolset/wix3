//-------------------------------------------------------------------------------------------------
// <copyright file="scawebprop.cpp" company="Outercurve Foundation">
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

// sql queries
enum eWebDirPropertiesQuery { wpqProperties = 1, wpqAccess, wpqAuthorization, wpqUser, wpqControlledPassword, wpqLogVisits, wpqIndex, wpqDefaultDoc,  wpqAspDetailedError, wpqHttpExp, wpqCCMaxAge, wpqCCCustom, wpqNoCustomError, wpqAccessSSLFlags, wpqAuthenticationProviders };

HRESULT ScaGetWebDirProperties(
    __in LPCWSTR wzProperties,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __in WCA_WRAPQUERY_HANDLE hWebDirPropQuery,
    __inout SCA_WEB_PROPERTIES* pswp
    )
{
    Assert(*wzProperties && pswp);

    HRESULT hr = S_OK;
    MSIHANDLE hRec;
    LPWSTR pwzData = NULL;

    ExitOnNull(wzProperties, hr, E_INVALIDARG, "Failed to get web directory properties because no properties were provided to get");

    WcaFetchWrappedReset(hWebDirPropQuery);

    hr = WcaFetchWrappedRecordWhereString(hWebDirPropQuery, 1, wzProperties, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, wpqProperties, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.DirProperties");
        hr = ::StringCchCopyW(pswp->wzKey, countof(pswp->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to webdirproperties object");

        Assert(0 == lstrcmpW(pswp->wzKey, wzProperties));

        hr = WcaGetRecordInteger(hRec, wpqAccess, &pswp->iAccess);
        ExitOnFailure(hr, "Failed to get access value");

        hr = WcaGetRecordInteger(hRec, wpqAuthorization, &pswp->iAuthorization);
        ExitOnFailure(hr, "Failed to get authorization value");

        // if allow anonymous users
        if (S_OK == hr && pswp->iAuthorization & 1)
        {
            // if there is an anonymous user specified
            hr = WcaGetRecordString(hRec, wpqUser, &pwzData);
            ExitOnFailure(hr, "Failed to get AnonymousUser_");
            if (pwzData && *pwzData)
            {
                hr = WcaGetRecordInteger(hRec, wpqControlledPassword, &pswp->fIIsControlledPassword);
                ExitOnFailure(hr, "Failed to get IIsControlledPassword");
                if (S_FALSE == hr)
                {
                    pswp->fIIsControlledPassword = FALSE;
                    hr = S_OK;
                }

                hr = ScaGetUserDeferred(pwzData, hUserQuery, &pswp->scau);
                ExitOnFailure(hr, "Failed to get User information for Web");

                pswp->fHasUser = TRUE;
            }
            else
                pswp->fHasUser = FALSE;
        }

        hr = WcaGetRecordInteger(hRec, wpqLogVisits, &pswp->fLogVisits);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.LogVisits");

        hr = WcaGetRecordInteger(hRec, wpqIndex, &pswp->fIndex);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.Index");

        hr = WcaGetRecordString(hRec, wpqDefaultDoc, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.DefaultDoc");
        if (pwzData && *pwzData)
        {
            pswp->fHasDefaultDoc = TRUE;
            if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default documents by setting them blank
            {
                pswp->wzDefaultDoc[0] = L'\0';
            }
            else   // set the default documents
            {
                hr = ::StringCchCopyW(pswp->wzDefaultDoc, countof(pswp->wzDefaultDoc), pwzData);
                ExitOnFailure(hr, "Failed to copy default document string to webdirproperties object");
            }
        }
        else
        {
            pswp->fHasDefaultDoc = FALSE;
        }

        hr = WcaGetRecordInteger(hRec, wpqAspDetailedError, &pswp->fAspDetailedError);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.AspDetailedError");

        hr = WcaGetRecordString(hRec, wpqHttpExp, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.HttpExp");
        if (pwzData && *pwzData)
        {
            pswp->fHasHttpExp = TRUE;
            if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default expiration settings by setting them blank
            {
                pswp->wzHttpExp[0] = L'\0';
            }
            else   // set the expiration setting
            {
                hr = ::StringCchCopyW(pswp->wzHttpExp, countof(pswp->wzHttpExp), pwzData);
                ExitOnFailure(hr, "Failed to copy http expiration string to webdirproperties object");
            }
        }
        else
        {
            pswp->fHasHttpExp = FALSE;
        }

        hr = WcaGetRecordInteger(hRec, wpqCCMaxAge, &pswp->iCacheControlMaxAge);
        ExitOnFailure(hr, "failed to get IIsWebDirProperties.CacheControlMaxAge");

        hr = WcaGetRecordString(hRec, wpqCCCustom, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.CacheControlCustom");
        if (pwzData && *pwzData)
        {
            pswp->fHasCacheControlCustom = TRUE;
            if (0 == lstrcmpW(L"-", pwzData))   // remove any existing default cache control custom settings by setting them blank
            {
                pswp->wzCacheControlCustom[0] = L'\0';
            }
            else   // set the custom cache control setting
            {
                hr = ::StringCchCopyW(pswp->wzCacheControlCustom, countof(pswp->wzCacheControlCustom), pwzData);
                ExitOnFailure(hr, "Failed to copy cache control custom settings to webdirproperites object");
            }
        }
        else
        {
            pswp->fHasCacheControlCustom = FALSE;
        }

        hr = WcaGetRecordInteger(hRec, wpqNoCustomError, &pswp->fNoCustomError);
        ExitOnFailure(hr, "failed to get IIsWebDirProperties.NoCustomError");
        if (MSI_NULL_INTEGER == pswp->fNoCustomError)
            pswp->fNoCustomError = FALSE;

        hr = WcaGetRecordInteger(hRec, wpqAccessSSLFlags, &pswp->iAccessSSLFlags);
        ExitOnFailure(hr, "failed to get IIsWebDirProperties.AccessSSLFlags");

        hr = WcaGetRecordString(hRec, wpqAuthenticationProviders, &pwzData);
        ExitOnFailure(hr, "Failed to get IIsWebDirProperties.AuthenticationProviders");
        if (pwzData && *pwzData)
        {
            hr = ::StringCchCopyW(pswp->wzAuthenticationProviders, countof(pswp->wzAuthenticationProviders), pwzData);
            ExitOnFailure(hr, "Failed to copy authentication providers string to webdirproperties object");
        }
        else
        {
            pswp->wzAuthenticationProviders[0] = L'\0';
        }
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate IIsWebDirProperties.DirProperties='%ls'", wzProperties);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error getting appropriate webdirproperty");
    }

    // Let's check that there isn't more than one record found - if there is, throw an assert like WcaFetchSingleRecord() would
    HRESULT hrTemp = WcaFetchWrappedRecordWhereString(hWebDirPropQuery, 1, wzProperties, &hRec);
    if (SUCCEEDED(hrTemp))
    {
        AssertSz(E_NOMOREITEMS == hrTemp, "ScaGetWebDirProperties found more than one record");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaWriteWebDirProperties(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzRootOfWeb,
    __inout SCA_WEB_PROPERTIES* pswp
    )
{
    HRESULT hr = S_OK;
    DWORD dw = 0;
    WCHAR wz[METADATA_MAX_NAME_LEN + 1];

    // write the access permissions to the metabase
    if (MSI_NULL_INTEGER != pswp->iAccess)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ACCESS_PERM, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAccess));
        ExitOnFailure(hr, "Failed to write access permissions for Web");
    }

    if (MSI_NULL_INTEGER != pswp->iAuthorization)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_AUTHORIZATION, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAuthorization));
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
            hr = ::StringCchCopyW(wz, countof(wz), pswp->scau.wzName);
            ExitOnFailure(hr, "Failed to copy user name");
        }
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_USER_NAME, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)wz);
        ExitOnFailure(hr, "Failed to write anonymous user name for Web");

        // write the password
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_PWD, METADATA_INHERIT | METADATA_SECURE, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->scau.wzPassword);
        ExitOnFailure(hr, "Failed to write anonymous user password for Web");

        // store whether IIs controls password
        dw = (pswp->fIIsControlledPassword) ? TRUE : FALSE;
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ANONYMOUS_USE_SUBAUTH, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
        ExitOnFailure(hr, "Failed to write if IIs controls user password for Web");
    }

    if (MSI_NULL_INTEGER != pswp->fLogVisits)
    {
        // The sense of this boolean value is reversed - it is "don't log", not "log visits."
        dw = (pswp->fLogVisits) ? FALSE : TRUE;
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_DONT_LOG, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
        ExitOnFailure(hr, "Failed to write authorization for Web");
    }

    if (MSI_NULL_INTEGER != pswp->fIndex)
    {
        dw = (pswp->fIndex) ? TRUE : FALSE;
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_IS_CONTENT_INDEXED, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
        ExitOnFailure(hr, "Failed to write authorization for Web");
    }

    if (pswp->fHasDefaultDoc)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_DEFAULT_LOAD_FILE, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzDefaultDoc);
        ExitOnFailure(hr, "Failed to write default documents for Web");
    }

    if (MSI_NULL_INTEGER != pswp->fAspDetailedError)
    {
        dw = (pswp->fAspDetailedError) ? TRUE : FALSE;
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_ASP_SCRIPTERRORSSENTTOBROWSER, METADATA_INHERIT, ASP_MD_UT_APP, DWORD_METADATA, (LPVOID)((DWORD_PTR)dw));
        ExitOnFailure(hr, "Failed to write ASP script error for Web");
    }

    if (pswp->fHasHttpExp)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_HTTP_EXPIRES, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzHttpExp);
        ExitOnFailure(hr, "Failed to write HTTP Expiration for Web");
    }

    if (MSI_NULL_INTEGER != pswp->iCacheControlMaxAge)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CC_MAX_AGE, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iCacheControlMaxAge));
        ExitOnFailure(hr, "Failed to write Cache Control Max Age for Web");
    }

    if (pswp->fHasCacheControlCustom)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CC_OTHER, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)pswp->wzCacheControlCustom);
        ExitOnFailure(hr, "Failed to write Cache Control Custom for Web");
    }

    if (pswp->fNoCustomError)
    {
        memset(wz, 0, sizeof(wz));
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_CUSTOM_ERROR, METADATA_INHERIT, IIS_MD_UT_FILE, MULTISZ_METADATA, wz);
        ExitOnFailure(hr, "Failed to write Custom Error for Web");
    }

    if (MSI_NULL_INTEGER != pswp->iAccessSSLFlags)
    {
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_SSL_ACCESS_PERM, METADATA_INHERIT, IIS_MD_UT_FILE, DWORD_METADATA, (LPVOID)((DWORD_PTR)pswp->iAccessSSLFlags));
        ExitOnFailure(hr, "Failed to write AccessSSLFlags for Web");
    }

    if (*pswp->wzAuthenticationProviders)
    {
        hr = ::StringCchCopyW(wz, countof(wz), pswp->wzAuthenticationProviders);
        ExitOnFailure(hr, "Failed to copy authentication providers string");
        hr = ScaWriteMetabaseValue(piMetabase, wzRootOfWeb, NULL, MD_NTAUTHENTICATION_PROVIDERS, METADATA_INHERIT, IIS_MD_UT_FILE, STRING_METADATA, (LPVOID)wz);
        ExitOnFailure(hr, "Failed to write AuthenticationProviders for Web");
    }

LExit:
    return hr;
}
