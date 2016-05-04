// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
