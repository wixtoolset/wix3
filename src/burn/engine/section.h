#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// structs

typedef struct _BURN_SECTION
{
    HANDLE hEngineFile;
    HANDLE hSourceEngineFile;

    DWORD cbStub;
    DWORD cbEngineSize;     // stub + UX container + original certficiate
    DWORD64 qwBundleSize;   // stub + UX container + original certificate [+ attached containers* + final certificate]

    DWORD dwChecksumOffset;
    DWORD dwCertificateTableOffset;
    DWORD dwOriginalChecksumAndSignatureOffset;

    DWORD dwOriginalChecksum;
    DWORD dwOriginalSignatureOffset;
    DWORD dwOriginalSignatureSize;

    DWORD dwFormat;
    DWORD cContainers;
    DWORD* rgcbContainers;
} BURN_SECTION;


HRESULT SectionInitialize(
    __in BURN_SECTION* pSection,
    __in HANDLE hEngineFile,
    __in HANDLE hSourceEngineFile
    );
void SectionUninitialize(
    __in BURN_SECTION* pSection
    );
HRESULT SectionGetAttachedContainerInfo(
    __in BURN_SECTION* pSection,
    __in DWORD iContainerIndex,
    __in DWORD dwExpectedType,
    __out DWORD64* pqwOffset,
    __out DWORD64* pqwSize,
    __out BOOL* pfPresent
    );

#if defined(__cplusplus)
}
#endif
