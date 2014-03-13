//-------------------------------------------------------------------------------------------------
// <copyright file="cryputil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Cryptography helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// function definitions

extern "C" HRESULT DAPI CrypDecodeObject(
    __in_z LPCSTR szStructType,
    __in_ecount(cbData) const BYTE* pbData,
    __in DWORD cbData,
    __in DWORD dwFlags,
    __out LPVOID* ppvObject,
    __out_opt DWORD* pcbObject
    )
{
    HRESULT hr = S_OK;
    LPVOID pvObject = NULL;
    DWORD cbObject = 0;

    if (!::CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, szStructType, pbData, cbData, dwFlags, NULL, &cbObject))
    {
        ExitWithLastError(hr, "Failed to decode object to determine size.");
    }

    pvObject = MemAlloc(cbObject, TRUE);
    ExitOnNull(pvObject, hr, E_OUTOFMEMORY, "Failed to allocate memory for decoded object.");

    if (!::CryptDecodeObject(X509_ASN_ENCODING | PKCS_7_ASN_ENCODING, szStructType, pbData, cbData, dwFlags, pvObject, &cbObject))
    {
        ExitWithLastError(hr, "Failed to decode object.");
    }

    *ppvObject = pvObject;
    pvObject = NULL;

    if (pcbObject)
    {
        *pcbObject = cbObject;
    }

LExit:
    ReleaseMem(pvObject);

    return hr;
}


extern "C" HRESULT DAPI CrypMsgGetParam(
    __in HCRYPTMSG hCryptMsg,
    __in DWORD dwType,
    __in DWORD dwIndex,
    __out LPVOID* ppvData,
    __out_opt DWORD* pcbData
    )
{
    HRESULT hr = S_OK;
    LPVOID pv = NULL;
    DWORD cb = 0;

    if (!::CryptMsgGetParam(hCryptMsg, dwType, dwIndex, NULL, &cb))
    {
        ExitWithLastError(hr, "Failed to get crypt message parameter data size.");
    }

    pv = MemAlloc(cb, TRUE);
    ExitOnNull(pv, hr, E_OUTOFMEMORY, "Failed to allocate memory for crypt message parameter.");

    if (!::CryptMsgGetParam(hCryptMsg, dwType, dwIndex, pv, &cb))
    {
        ExitWithLastError(hr, "Failed to get crypt message parameter.");
    }

    *ppvData = pv;
    pv = NULL;

    if (pcbData)
    {
        *pcbData = cb;
    }

LExit:
    ReleaseMem(pv);

    return hr;
}


extern "C" HRESULT DAPI CrypHashFile(
    __in LPCWSTR wzFilePath,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash,
    __out_opt DWORD64* pqwBytesHashed
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // open input file
    hFile = ::CreateFileW(wzFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open input file.");
    }

    hr = CrypHashFileHandle(hFile, dwProvType, algid, pbHash, cbHash, pqwBytesHashed);
    ExitOnFailure1(hr, "Failed to hash file: %ls", wzFilePath);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}


extern "C" HRESULT DAPI CrypHashFileHandle(
    __in HANDLE hFile,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash,
    __out_opt DWORD64* pqwBytesHashed
    )
{
    HRESULT hr = S_OK;
    HCRYPTPROV hProv = NULL;
    HCRYPTHASH hHash = NULL;
    DWORD cbRead = 0;
    BYTE rgbBuffer[4096] = { };
    const LARGE_INTEGER liZero = { };

    // get handle to the crypto provider
    if (!::CryptAcquireContextW(&hProv, NULL, NULL, dwProvType, CRYPT_VERIFYCONTEXT | CRYPT_SILENT))
    {
        ExitWithLastError(hr, "Failed to acquire crypto context.");
    }

    // initiate hash
    if (!::CryptCreateHash(hProv, algid, 0, 0, &hHash))
    {
        ExitWithLastError(hr, "Failed to initiate hash.");
    }

    for (;;)
    {
        // read data block
        if (!::ReadFile(hFile, rgbBuffer, sizeof(rgbBuffer), &cbRead, NULL))
        {
            ExitWithLastError(hr, "Failed to read data block.");
        }

        if (!cbRead)
        {
            break; // end of file
        }

        // hash data block
        if (!::CryptHashData(hHash, rgbBuffer, cbRead, 0))
        {
            ExitWithLastError(hr, "Failed to hash data block.");
        }
    }

    // get hash value
    if (!::CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
    {
        ExitWithLastError(hr, "Failed to get hash value.");
    }

    if (pqwBytesHashed)
    {
        if (!::SetFilePointerEx(hFile, liZero, (LARGE_INTEGER*)pqwBytesHashed, FILE_CURRENT))
        {
            ExitWithLastError(hr, "Failed to get file pointer.");
        }
    }

LExit:
    if (hHash)
    {
        ::CryptDestroyHash(hHash);
    }
    if (hProv)
    {
        ::CryptReleaseContext(hProv, 0);
    }

    return hr;
}

HRESULT DAPI CrypHashBuffer(
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in DWORD dwProvType,
    __in ALG_ID algid,
    __out_bcount(cbHash) BYTE* pbHash,
    __in DWORD cbHash
    )
{
    HRESULT hr = S_OK;
    HCRYPTPROV hProv = NULL;
    HCRYPTHASH hHash = NULL;

    // get handle to the crypto provider
    if (!::CryptAcquireContextW(&hProv, NULL, NULL, dwProvType, CRYPT_VERIFYCONTEXT | CRYPT_SILENT))
    {
        ExitWithLastError(hr, "Failed to acquire crypto context.");
    }

    // initiate hash
    if (!::CryptCreateHash(hProv, algid, 0, 0, &hHash))
    {
        ExitWithLastError(hr, "Failed to initiate hash.");
    }

    if (!::CryptHashData(hHash, pbBuffer, static_cast<DWORD>(cbBuffer), 0))
    {
        ExitWithLastError(hr, "Failed to hash data.");
    }

    // get hash value
    if (!::CryptGetHashParam(hHash, HP_HASHVAL, pbHash, &cbHash, 0))
    {
        ExitWithLastError(hr, "Failed to get hash value.");
    }

LExit:
    if (hHash)
    {
        ::CryptDestroyHash(hHash);
    }
    if (hProv)
    {
        ::CryptReleaseContext(hProv, 0);
    }

    return hr;
}

