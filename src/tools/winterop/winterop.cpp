// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


HRESULT HashPublicKeyInfo(
    __in PCERT_CONTEXT pCertContext,
    __in_ecount(*pcbSubjectKeyIndentifier) BYTE* rgbSubjectKeyIdentifier,
    __inout DWORD* pcbSubjectKeyIndentifier
    )
{
    HRESULT hr = S_OK;

    if (!::CryptHashPublicKeyInfo(NULL, CALG_SHA1, 0, X509_ASN_ENCODING, &pCertContext->pCertInfo->SubjectPublicKeyInfo, rgbSubjectKeyIdentifier, pcbSubjectKeyIndentifier))
    {
        ExitWithLastError(hr, "Failed to hash public key information.");
    }

LExit:
    return hr;
}

HRESULT ResetAcls(
    __in LPCWSTR pwzFiles[],
    __in DWORD cFiles
    )
{
    HRESULT hr = S_OK;
    ACL* pacl = NULL;
    DWORD cbAcl = sizeof(ACL);

    OSVERSIONINFO osvi;

    osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFO);
#pragma warning(push)
#pragma warning(disable:4996)
    if (!::GetVersionExA(&osvi))
#pragma warning(pop)
    {
        ExitOnLastError(hr, "failed to get OS version");
    }

    // If we're running on NT 4 or earlier, or ME or earlier, don't reset ACLs.
    if (4 >= osvi.dwMajorVersion)
    {
        ExitFunction1(hr = S_FALSE);
    }

    // create an empty (not NULL!) ACL to use on all the files
    pacl = static_cast<ACL*>(MemAlloc(cbAcl, FALSE));
    ExitOnNull(pacl, hr, E_OUTOFMEMORY, "failed to allocate ACL");

#pragma prefast(push)
#pragma prefast(disable:25029)
    if (!::InitializeAcl(pacl, cbAcl, ACL_REVISION))
#pragma prefast(op)
    {
        ExitOnLastError(hr, "failed to initialize ACL");
    }

    // reset the existing security permissions on each file
    for (DWORD i = 0; i < cFiles; ++i)
    {
        hr = ::SetNamedSecurityInfoW(const_cast<LPWSTR>(pwzFiles[i]), SE_FILE_OBJECT, DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION, NULL, NULL, pacl, NULL);
        if (ERROR_FILE_NOT_FOUND != hr && ERROR_PATH_NOT_FOUND != hr)
        {
            ExitOnFailure1(hr = HRESULT_FROM_WIN32(hr), "failed to set security descriptor for file: %S", pwzFiles[i]);
        }
    }

    // Setting to S_OK because we could end with ERROR_FILE_NOT_FOUND or ERROR_PATH_NOT_FOUND as valid return values.
    hr = S_OK;

    AssertSz(::IsValidAcl(pacl), "ResetAcls() - created invalid ACL");

LExit:
    if (pacl)
    {
        MemFree(pacl);
    }

    return hr;
}


HRESULT CreateCabBegin(
    __in LPCWSTR wzCab,
    __in LPCWSTR wzCabDir,
    __in DWORD dwMaxFiles,
    __in DWORD dwMaxSize,
    __in DWORD dwMaxThresh,
    __in COMPRESSION_TYPE ct,
    __out HANDLE *phContext
    )
{
    return CabCBegin(wzCab, wzCabDir, dwMaxFiles, dwMaxSize, dwMaxThresh, ct, phContext);
}


HRESULT CreateCabAddFile(
    __in LPCWSTR wzFile,
    __in_opt LPCWSTR wzToken,
    __in_opt PMSIFILEHASHINFO pmfHash,
    __in HANDLE hContext
    )
{
    return CabCAddFile(wzFile, wzToken, pmfHash, hContext);
}


HRESULT CreateCabAddFiles(
    __in LPCWSTR pwzFiles[],
    __in LPCWSTR pwzTokens[],
    __in PMSIFILEHASHINFO pmfHash[],
    __in DWORD cFiles,
    __in HANDLE hContext
    )
{
    HRESULT hr = S_OK;
    DWORD i;

    Assert(pwzFiles);
    Assert(hContext);

    for (i = 0; i < cFiles; i++)
    {
        hr = CreateCabAddFile(
            pwzFiles[i], 
            pwzTokens ? pwzTokens[i] : NULL, 
            pmfHash[i],
            hContext
            );
        ExitOnFailure1(hr, "Failed to add file %S to cab", pwzFiles[i]);
    }

LExit:
    return hr;
}


HRESULT CreateCabFinish(
    __in HANDLE hContext,
    __in_opt FileSplitCabNamesCallback newCabNamesCallBackAddress
    )
{
    // Convert address into Binder callback function
    return CabCFinish(hContext, newCabNamesCallBackAddress);
}


void CreateCabCancel(
    __in HANDLE hContext
    )
{
    CabCCancel(hContext);
}


HRESULT ExtractCabBegin()
{
    return CabInitialize(FALSE);
}


HRESULT ExtractCab(
    __in LPCWSTR wzCabinet,
    __in LPCWSTR wzExtractDir
    )
{
    return CabExtract(wzCabinet, L"*", wzExtractDir, NULL, NULL, 0);
}


void ExtractCabFinish()
{
    CabUninitialize();
    return;
}


HRESULT EnumerateCabBegin()
{
    return CabInitialize(FALSE);
}


HRESULT EnumerateCab(
    __in LPCWSTR wzCabinet,
    __in STDCALL_PFNFDINOTIFY pfnNotify
    )
{
    return CabEnumerate(wzCabinet, L"*", pfnNotify, 0);
}


void EnumerateCabFinish()
{
    CabUninitialize();
    return;
}


BOOL WINAPI DllMain(
    __in HINSTANCE /*hInstance*/,
    __in DWORD dwReason,
    __in LPVOID /*lpvReserved*/
    )
{
    switch(dwReason)
    {
        case DLL_PROCESS_ATTACH:
        case DLL_PROCESS_DETACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
            break;
    }

    return TRUE;
}
