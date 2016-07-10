// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT PcaActionDataMessage(
    DWORD cArgs,
    ...
    );
HRESULT PcaAccountNameToSid(
    LPCWSTR pwzAccountName,
    PSID* ppSid
    );
HRESULT PcaSidToAccountName(
    PSID pSid,
    LPWSTR* ppwzAccountName
    );
HRESULT PcaBuildAccountName(
    LPCWSTR pwzDomain,
    LPCWSTR pwzName,
    LPWSTR* ppwzAccount
    );
HRESULT PcaGuidFromString(
    LPCWSTR pwzGuid,
    GUID* pGuid
    );
