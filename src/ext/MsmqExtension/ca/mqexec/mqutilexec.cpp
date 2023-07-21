// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// private structs

struct PCA_WELLKNOWN_SID
{
    LPCWSTR pwzName;
    SID_IDENTIFIER_AUTHORITY iaIdentifierAuthority;
    BYTE nSubAuthorityCount;
    DWORD dwSubAuthority[8];
};


// well known SIDs

PCA_WELLKNOWN_SID wsWellKnownSids[] = {
    {L"\\Everyone",          SECURITY_WORLD_SID_AUTHORITY, 1, {SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\Administrators",    SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0}},
    {L"\\LocalSystem",       SECURITY_NT_AUTHORITY,        1, {SECURITY_LOCAL_SYSTEM_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\LocalService",      SECURITY_NT_AUTHORITY,        1, {SECURITY_LOCAL_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\NetworkService",    SECURITY_NT_AUTHORITY,        1, {SECURITY_NETWORK_SERVICE_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\AuthenticatedUser", SECURITY_NT_AUTHORITY,        1, {SECURITY_AUTHENTICATED_USER_RID, 0, 0, 0, 0, 0, 0, 0}},
    {L"\\Guests",            SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_GUESTS, 0, 0, 0, 0, 0, 0}},
    {L"\\Users",             SECURITY_NT_AUTHORITY,        2, {SECURITY_BUILTIN_DOMAIN_RID, DOMAIN_ALIAS_RID_USERS, 0, 0, 0, 0, 0, 0}},
    {L"\\CREATOR OWNER",     SECURITY_NT_AUTHORITY,        1, {SECURITY_CREATOR_OWNER_RID, 0, 0, 0, 0, 0, 0, 0}},
    {NULL,                   SECURITY_NULL_SID_AUTHORITY,  0, {0, 0, 0, 0, 0, 0, 0, 0}}
};


// prototypes for private helper functions

static HRESULT CreateSidFromDomainRidPair(
    PSID pDomainSid,
    DWORD dwRid,
    PSID* ppSid
    );
static HRESULT InitLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr,
    LPCWSTR pwzStr,
    SIZE_T dwLen
    );
static void FreeLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr
    );


// function definitions

HRESULT PcaActionDataMessage(
    DWORD cArgs,
    ...
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hRec;
    va_list args;

    // record
    hRec = ::MsiCreateRecord(cArgs);
    ExitOnNull(hRec, hr, E_OUTOFMEMORY, "Failed to create record");

    va_start(args, cArgs);
    for (DWORD i = 1; i <= cArgs; i++)
    {
        LPCWSTR pwzArg = va_arg(args, WCHAR*);
        if (pwzArg && *pwzArg)
        {
            er = ::MsiRecordSetStringW(hRec, i, pwzArg);
            ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to set record field string");
        }
    }
    va_end(args);

    // message
    er = WcaProcessMessage(INSTALLMESSAGE_ACTIONDATA, hRec);
    if (0 == er || IDOK == er || IDYES == er)
    {
        hr = S_OK;
    }
    else if (ERROR_INSTALL_USEREXIT == er || IDABORT == er || IDCANCEL == er)
    {
        WcaSetReturnValue(ERROR_INSTALL_USEREXIT); // note that the user said exit
        hr = S_FALSE;
    }
    else
        hr = E_UNEXPECTED;

LExit:
    return hr;
}

HRESULT PcaAccountNameToSid(
    LPCWSTR pwzAccountName,
    PSID* ppSid
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    NTSTATUS st = 0;

    PSID pSid = NULL;
    LSA_OBJECT_ATTRIBUTES loaAttributes;
    LSA_HANDLE lsahPolicy = NULL;
    LSA_UNICODE_STRING lusName;
    PLSA_REFERENCED_DOMAIN_LIST plrdsDomains = NULL;
    PLSA_TRANSLATED_SID pltsSid = NULL;

    ::ZeroMemory(&loaAttributes, sizeof(loaAttributes));
    ::ZeroMemory(&lusName, sizeof(lusName));

    // identify well known SIDs
    for (PCA_WELLKNOWN_SID* pWS = wsWellKnownSids; pWS->pwzName; pWS++)
    {
        if (0 == lstrcmpiW(pwzAccountName, pWS->pwzName))
        {
            // allocate SID buffer
            pSid = (PSID)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, ::GetSidLengthRequired(pWS->nSubAuthorityCount));
            ExitOnNull(pSid, hr, E_OUTOFMEMORY, "Failed to allocate buffer for SID");

            // initialize SID
            ::InitializeSid(pSid, &pWS->iaIdentifierAuthority, pWS->nSubAuthorityCount);

            // copy sub autorities
            for (DWORD i = 0; i < pWS->nSubAuthorityCount; i++)
                *::GetSidSubAuthority(pSid, i) = pWS->dwSubAuthority[i];

            break;
        }
    }

    // lookup name
    if (!pSid)
    {
        // open policy handle
        st = ::LsaOpenPolicy(NULL, &loaAttributes, POLICY_ALL_ACCESS, &lsahPolicy);
        er = ::LsaNtStatusToWinError(st);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to open policy handle");

        // create account name lsa unicode string
        hr = InitLsaUnicodeString(&lusName, pwzAccountName, wcslen(pwzAccountName));
        ExitOnFailure(hr, "Failed to initialize account name string");

        // lookup name
        st = ::LsaLookupNames(lsahPolicy, 1, &lusName, &plrdsDomains, &pltsSid);
        er = ::LsaNtStatusToWinError(st);
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to lookup account names");

        if (SidTypeDomain == pltsSid->Use)
            ExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED), "Domain SIDs not supported");

        // convert sid
        hr = CreateSidFromDomainRidPair(plrdsDomains->Domains[pltsSid->DomainIndex].Sid, pltsSid->RelativeId, &pSid);
        ExitOnFailure(hr, "Failed to convert SID");
    }

    *ppSid = pSid;
    pSid = NULL;

    hr = S_OK;

LExit:
    // clean up
    if (pSid)
        ::HeapFree(::GetProcessHeap(), 0, pSid);
    if (lsahPolicy)
        ::LsaClose(lsahPolicy);
    if (plrdsDomains)
        ::LsaFreeMemory(plrdsDomains);
    if (pltsSid)
        ::LsaFreeMemory(pltsSid);
    FreeLsaUnicodeString(&lusName);

    return hr;
}

HRESULT PcaSidToAccountName(
    PSID pSid,
    LPWSTR* ppwzAccountName
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    NTSTATUS st = 0;

    LSA_OBJECT_ATTRIBUTES loaAttributes;
    LSA_HANDLE lsahPolicy = NULL;
    PLSA_REFERENCED_DOMAIN_LIST plrdsDomains = NULL;
    PLSA_TRANSLATED_NAME pltnName = NULL;

    LPWSTR pwzDomain = NULL;
    LPWSTR pwzName = NULL;

    ::ZeroMemory(&loaAttributes, sizeof(loaAttributes));

    // open policy handle
    st = ::LsaOpenPolicy(NULL, &loaAttributes, POLICY_ALL_ACCESS, &lsahPolicy);
    er = ::LsaNtStatusToWinError(st);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to open policy handle");

    // lookup SID
    st = ::LsaLookupSids(lsahPolicy, 1, &pSid, &plrdsDomains, &pltnName);
    er = ::LsaNtStatusToWinError(st);
    ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "Failed to lookup SID");

    if (SidTypeDomain == pltnName->Use)
        ExitOnFailure(hr = HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED), "Domain SIDs not supported");

    // format account name string
    if (SidTypeWellKnownGroup != pltnName->Use)
    {
        PLSA_UNICODE_STRING plusDomain = &plrdsDomains->Domains[pltnName->DomainIndex].Name;
        hr = StrAllocString(&pwzDomain, plusDomain->Buffer, plusDomain->Length / sizeof(WCHAR));
        ExitOnFailure(hr, "Failed to allocate name string");
    }

    hr = StrAllocString(&pwzName, pltnName->Name.Buffer, pltnName->Name.Length / sizeof(WCHAR));
    ExitOnFailure(hr, "Failed to allocate domain string");

    hr = StrAllocFormatted(ppwzAccountName, L"%s\\%s", pwzDomain ? pwzDomain : L"", pwzName);
    ExitOnFailure(hr, "Failed to format account name string");

    hr = S_OK;

LExit:
    // clean up
    if (lsahPolicy)
        ::LsaClose(lsahPolicy);
    if (plrdsDomains)
        ::LsaFreeMemory(plrdsDomains);
    if (pltnName)
        ::LsaFreeMemory(pltnName);

    ReleaseStr(pwzDomain);
    ReleaseStr(pwzName);

    return hr;
}

HRESULT PcaBuildAccountName(
    LPCWSTR pwzDomain,
    LPCWSTR pwzName,
    LPWSTR* ppwzAccount
    )
{
    HRESULT hr = S_OK;

    WCHAR wzComputerName[MAX_COMPUTERNAME_LENGTH + 1];
    ::ZeroMemory(wzComputerName, sizeof(wzComputerName));

    // if domain is '.', get computer name
    if (0 == lstrcmpW(pwzDomain, L"."))
    {
        DWORD dwSize = countof(wzComputerName);
        if (!::GetComputerNameW(wzComputerName, &dwSize))
            ExitOnFailure(hr = HRESULT_FROM_WIN32(::GetLastError()), "Failed to get computer name");
    }

    // build account name
    hr = StrAllocFormatted(ppwzAccount, L"%s\\%s", *wzComputerName ? wzComputerName : pwzDomain, pwzName);
    ExitOnFailure(hr, "Failed to build domain user name");

    hr = S_OK;

LExit:
    return hr;
}

HRESULT PcaGuidFromString(
    LPCWSTR pwzGuid,
    LPGUID pGuid
    )
{
    HRESULT hr = S_OK;

    int cch = 0;

    WCHAR wz[39];
    ::ZeroMemory(wz, sizeof(wz));

    cch = lstrlenW(pwzGuid);

    if (38 == cch && L'{' == pwzGuid[0] && L'}' == pwzGuid[37])
        StringCchCopyW(wz, countof(wz), pwzGuid);
    else if (36 == cch)
        StringCchPrintfW(wz, countof(wz), L"{%s}", pwzGuid);
    else
        ExitFunction1(hr = E_INVALIDARG);

    hr = ::CLSIDFromString(wz, pGuid);

LExit:
    return hr;
}


// helper function definitions

static HRESULT CreateSidFromDomainRidPair(
    PSID pDomainSid,
    DWORD dwRid,
    PSID* ppSid
    )
{
    HRESULT hr = S_OK;

    PSID pSid = NULL;

    // get domain SID sub authority count
    UCHAR ucSubAuthorityCount = *::GetSidSubAuthorityCount(pDomainSid);

    // allocate SID buffer
    DWORD dwLengthRequired = ::GetSidLengthRequired(ucSubAuthorityCount + (UCHAR)1);
    if (*ppSid)
    {
        SIZE_T ccb = ::HeapSize(::GetProcessHeap(), 0, *ppSid);
        if (-1 == ccb)
            ExitOnFailure(hr = E_FAIL, "Failed to get size of SID buffer");

        if (ccb < dwLengthRequired)
        {
            pSid = (PSID)::HeapReAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, *ppSid, dwLengthRequired);
            ExitOnNull1(pSid, hr, E_OUTOFMEMORY, "Failed to reallocate buffer for SID, len: %d", dwLengthRequired);
            *ppSid = pSid;
        }
    }
    else
    {
        *ppSid = (PSID)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, dwLengthRequired);
        ExitOnNull1(*ppSid, hr, E_OUTOFMEMORY, "Failed to allocate buffer for SID, len: %d", dwLengthRequired);
    }

    ::InitializeSid(*ppSid, ::GetSidIdentifierAuthority(pDomainSid), ucSubAuthorityCount + (UCHAR)1);

    // copy sub autorities
    DWORD i = 0;
    for (; i < ucSubAuthorityCount; i++)
        *::GetSidSubAuthority(*ppSid, i) = *::GetSidSubAuthority(pDomainSid, i);
    *::GetSidSubAuthority(*ppSid, i) = dwRid;

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT InitLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr,
    LPCWSTR pwzStr,
    SIZE_T dwLen
    )
{
    HRESULT hr = S_OK;

    plusStr->Length = (USHORT)dwLen * sizeof(WCHAR);
    plusStr->MaximumLength = (USHORT)(dwLen + 1) * sizeof(WCHAR);

    plusStr->Buffer = (WCHAR*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(WCHAR) * (dwLen + 1));
    ExitOnNull(plusStr->Buffer, hr, E_OUTOFMEMORY, "Failed to allocate account name string");

    hr = StringCchCopyW(plusStr->Buffer, dwLen + 1, pwzStr);
    ExitOnFailure(hr, "Failed to copy buffer");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeLsaUnicodeString(
    PLSA_UNICODE_STRING plusStr
    )
{
    if (plusStr->Buffer)
        ::HeapFree(::GetProcessHeap(), 0, plusStr->Buffer);
}
