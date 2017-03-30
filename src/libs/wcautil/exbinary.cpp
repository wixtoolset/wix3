//-------------------------------------------------------------------------------------------------
// <copyright file="exbinary.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Extract streams from the Binary table.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

//
// Extracts the data from the Binary table row with the given ID into a buffer. 
//
HRESULT WIXAPI WcaExtractBinaryToBuffer(
    __in LPCWSTR wzBinaryId,
    __out BYTE** pbData,
    __out DWORD* pcbData
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzSql = NULL;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;

    // make sure we're not horked from the get-go
    hr = WcaTableExists(L"Binary");
    if (S_OK != hr)
    {
        if (SUCCEEDED(hr))
        {
            hr = E_UNEXPECTED;
        }
        ExitOnFailure(hr, "There is no Binary table.");
    }

    ExitOnNull(wzBinaryId, hr, E_INVALIDARG, "Binary ID cannot be null");
    ExitOnNull(*wzBinaryId, hr, E_INVALIDARG, "Binary ID cannot be empty string");

    hr = StrAllocFormatted(&pwzSql, L"SELECT `Data` FROM `Binary` WHERE `Name`=\'%ls\'", wzBinaryId);
    ExitOnFailure(hr, "Failed to allocate Binary table query.");

    hr = WcaOpenExecuteView(pwzSql, &hView);
    ExitOnFailure(hr, "Failed to open view on Binary table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    ExitOnFailure(hr, "Failed to retrieve request from Binary table");

    hr = WcaGetRecordStream(hRec, 1, pbData, pcbData);
    ExitOnFailure(hr, "Failed to read Binary.Data.");

LExit:
    ReleaseStr(pwzSql);

    return hr;
}

//
// Extracts the data from the Binary table row with the given ID into a file.
//
HRESULT WIXAPI WcaExtractBinaryToFile(
    __in LPCWSTR wzBinaryId,
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    DWORD cbData = 0;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // grab the bits
    hr = WcaExtractBinaryToBuffer(wzBinaryId, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to extract binary data: %ls", wzBinaryId);

    // write 'em to the file
    hFile = ::CreateFileW(wzPath, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to create file: %ls", wzPath);
    }

    DWORD cbWritten = 0;
    if (!::WriteFile(hFile, pbData, cbData, &cbWritten, NULL))
    {
        ExitWithLastError(hr, "Failed to write data to file: %ls", wzPath);
    }

LExit:
    ReleaseFile(hFile);
    ReleaseMem(pbData);

    return hr;
}

//
// Extracts the data from the Binary table row with the given ID into a string.
//
HRESULT WIXAPI WcaExtractBinaryToString(
    __in LPCWSTR wzBinaryId,
    __deref_out_z LPWSTR* psczOutput,
    __out WCA_ENCODING* encoding
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    DWORD cbData = 0;

    // grab the bits
    hr = WcaExtractBinaryToBuffer(wzBinaryId, &pbData, &cbData);
    ExitOnFailure(hr, "Failed to extract binary data: %ls", wzBinaryId);

    // expand by a NULL character (or two) to make sure the buffer is null-terminated
    cbData += 2;
    pbData = reinterpret_cast<LPBYTE>(MemReAlloc(pbData, cbData, TRUE));
    ExitOnNull(pbData, hr, E_OUTOFMEMORY, "Failed to expand binary buffer");

    // Check for BOMs.
    if (2 < cbData)
    {
        if ((0xFF == *pbData) && (0xFE == *(pbData + 1)))
        {
            *encoding = WCA_ENCODING_UTF_16;
            hr = StrAllocString(psczOutput, reinterpret_cast<LPWSTR>(pbData), 0);
        }
        else if ((0xEF == *pbData) && (0xBB == *(pbData + 1)) && (0xBF == *(pbData + 2)))
        {
            *encoding = WCA_ENCODING_UTF_8;
            hr = StrAllocStringAnsi(psczOutput, reinterpret_cast<LPCSTR>(pbData), 0, CP_UTF8);
        }
        else
        {
            *encoding = WCA_ENCODING_ANSI;
            hr = StrAllocStringAnsi(psczOutput, reinterpret_cast<LPCSTR>(pbData), 0, CP_ACP);
        }
        ExitOnFailure(hr, "Failed to allocate string for binary buffer.");
    }

    // Free the byte buffer since it has been converted to a new UNICODE string, one way or another.
    if (pbData)
    {
        WcaFreeStream(pbData);
        pbData = NULL;
    }

LExit:
    ReleaseMem(pbData);

    return hr;
}
