//-------------------------------------------------------------------------------------------------
// <copyright file="scassl7.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    IIS SSL functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

HRESULT ScaSslCertificateWrite7(
    __in_z LPCWSTR wzWebBase,
    __in SCA_WEB_SSL_CERTIFICATE* pswscList
    )
{
    HRESULT hr = S_OK;
    WCHAR wzEncodedCertificateHash[CB_CERTIFICATE_HASH * 2 + 1] = { 0 };

    for (SCA_WEB_SSL_CERTIFICATE* pswsc = pswscList; pswsc; pswsc = pswsc->pNext)
    {
        hr = ScaWriteConfigID(IIS_SSL_BINDING);
        ExitOnFailure(hr, "Failed write SSL binding ID");
        hr = ScaWriteConfigID(IIS_CREATE);                      // Need to determine site action
        ExitOnFailure(hr, "Failed write binding action");

        hr = ScaWriteConfigString(wzWebBase);                   //site name key
        ExitOnFailure(hr, "Failed to write SSL website");
        hr = ScaWriteConfigString(pswsc->wzStoreName);          //ssl store name
        ExitOnFailure(hr, "Failed to write SSL store name");

        hr = StrHexEncode(pswsc->rgbSHA1Hash, countof(pswsc->rgbSHA1Hash), wzEncodedCertificateHash, countof(wzEncodedCertificateHash));
        ExitOnFailure(hr, "Failed to encode SSL hash");

        hr = ScaWriteConfigString(wzEncodedCertificateHash);    //ssl hash
        ExitOnFailure(hr, "Failed to write SSL hash");
    }
LExit:

    return hr;
}
