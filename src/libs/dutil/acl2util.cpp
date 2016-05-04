// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

/********************************************************************
AclCalculateServiceSidString - gets the SID string for the given service name

NOTE: psczSid should be freed with StrFree()
********************************************************************/
extern "C" HRESULT DAPI AclCalculateServiceSidString(
    __in LPCWSTR wzServiceName,
    __in int cchServiceName,
    __deref_out_z LPWSTR* psczSid
    )
{
    // TODO: use undocumented RtlCreateServiceSid function?
    // http://blogs.technet.com/b/voy/archive/2007/03/22/per-service-sid.aspx
    // Assume little endian.
    HRESULT hr = S_OK;
    LPWSTR sczUpperServiceName = NULL;
    DWORD cbHash = SHA1_HASH_LEN;
    BYTE* pbHash = NULL;

    Assert(psczSid);

    if (0 == cchServiceName)
    {
        hr = ::StringCchLengthW(wzServiceName, INT_MAX, reinterpret_cast<size_t*>(&cchServiceName));
        ExitOnFailure(hr, "Failed to get the length of the service name.");
    }

    hr = StrAllocStringToUpperInvariant(&sczUpperServiceName, wzServiceName, cchServiceName);
    ExitOnFailure(hr, "Failed to upper case the service name.");

    pbHash = reinterpret_cast<BYTE*>(MemAlloc(cbHash, TRUE));
    ExitOnNull(pbHash, hr, E_OUTOFMEMORY, "Failed to allocate hash byte array.");

    hr = CrypHashBuffer(reinterpret_cast<BYTE*>(sczUpperServiceName), cchServiceName * 2, PROV_RSA_FULL, CALG_SHA1, pbHash, cbHash);
    ExitOnNull(pbHash, hr, E_OUTOFMEMORY, "Failed to hash the service name.");

    hr = StrAllocFormatted(psczSid, L"S-1-5-80-%u-%u-%u-%u-%u",
                           MAKEDWORD(MAKEWORD(pbHash[0], pbHash[1]), MAKEWORD(pbHash[2], pbHash[3])),
                           MAKEDWORD(MAKEWORD(pbHash[4], pbHash[5]), MAKEWORD(pbHash[6], pbHash[7])),
                           MAKEDWORD(MAKEWORD(pbHash[8], pbHash[9]), MAKEWORD(pbHash[10], pbHash[11])),
                           MAKEDWORD(MAKEWORD(pbHash[12], pbHash[13]), MAKEWORD(pbHash[14], pbHash[15])),
                           MAKEDWORD(MAKEWORD(pbHash[16], pbHash[17]), MAKEWORD(pbHash[18], pbHash[19])));

LExit:
    ReleaseMem(pbHash);
    ReleaseStr(sczUpperServiceName);

    return hr;
}


/********************************************************************
AclGetAccountSidStringEx - gets a string version of the account's SID
                           calculates a service's SID if lookup fails

NOTE: psczSid should be freed with StrFree()
********************************************************************/
extern "C" HRESULT DAPI AclGetAccountSidStringEx(
    __in_z LPCWSTR wzSystem,
    __in_z LPCWSTR wzAccount,
    __deref_out_z LPWSTR* psczSid
    )
{
    HRESULT hr = S_OK;
    int cchAccount = 0;
    PSID psid = NULL;
    LPWSTR pwz = NULL;
    LPWSTR sczSid = NULL;

    Assert(psczSid);

    hr = AclGetAccountSid(wzSystem, wzAccount, &psid);
    if (SUCCEEDED(hr))
    {
        Assert(::IsValidSid(psid));

        if (!::ConvertSidToStringSidW(psid, &pwz))
        {
            ExitWithLastError1(hr, "Failed to convert SID to string for Account: %ls", wzAccount);
        }

        hr = StrAllocString(psczSid, pwz, 0);
    }
    else
    {
        if (HRESULT_FROM_WIN32(ERROR_NONE_MAPPED) == hr)
        {
            HRESULT hrLength = ::StringCchLengthW(wzAccount, INT_MAX, reinterpret_cast<size_t*>(&cchAccount));
            ExitOnFailure(hrLength, "Failed to get the length of the account name.");

            if (11 < cchAccount && CSTR_EQUAL == CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, L"NT SERVICE\\", 11, wzAccount, 11))
            {
                // If the service is not installed then LookupAccountName doesn't resolve the SID, but we can calculate it.
                LPCWSTR wzServiceName = &wzAccount[11];
                hr = AclCalculateServiceSidString(wzServiceName, cchAccount - 11, &sczSid);
                ExitOnFailure1(hr, "Failed to calculate the service SID for %ls", wzServiceName);

                *psczSid = sczSid;
                sczSid = NULL;
            }
        }
        ExitOnFailure1(hr, "Failed to get SID for account: %ls", wzAccount);
    }

LExit:
    ReleaseStr(sczSid);
    if (pwz)
    {
        ::LocalFree(pwz);
    }
    if (psid)
    {
        AclFreeSid(psid);
    }

    return hr;
}
