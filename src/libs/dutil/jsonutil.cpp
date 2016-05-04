// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const DWORD JSON_STACK_INCREMENT = 5;

// Prototypes
static HRESULT DoStart(
    __in JSON_WRITER* pWriter,
    __in JSON_TOKEN tokenStart,
    __in_z LPCWSTR wzStartString
    );
static HRESULT DoEnd(
    __in JSON_WRITER* pWriter,
    __in JSON_TOKEN tokenEnd,
    __in_z LPCWSTR wzEndString
    );
static HRESULT DoKey(
    __in JSON_WRITER* pWriter,
    __in_z LPCWSTR wzKey
    );
static HRESULT DoValue(
    __in JSON_WRITER* pWriter,
    __in_z_opt LPCWSTR wzValue
    );
static HRESULT EnsureTokenStack(
    __in JSON_WRITER* pWriter
    );
static HRESULT SerializeJsonString(
    __out_z LPWSTR* psczJsonString,
    __in_z LPCWSTR wzString
    );



DAPI_(HRESULT) JsonInitializeReader(
    __in_z LPCWSTR wzJson,
    __in JSON_READER* pReader
    )
{
    HRESULT hr = S_OK;

    memset(pReader, 0, sizeof(JSON_READER));
    ::InitializeCriticalSection(&pReader->cs);

    hr = StrAllocString(&pReader->sczJson, wzJson, 0);
    ExitOnFailure(hr, "Failed to allocate json string.");

    pReader->pwz = pReader->sczJson;

LExit:
    return hr;
}


DAPI_(void) JsonUninitializeReader(
    __in JSON_READER* pReader
    )
{
    ReleaseStr(pReader->sczJson);

    ::DeleteCriticalSection(&pReader->cs);
    memset(pReader, 0, sizeof(JSON_READER));
}


static HRESULT NextToken(
    __in JSON_READER* pReader,
    __out JSON_TOKEN* pToken
    )
{
    HRESULT hr = S_OK;

    // Skip whitespace.
    while (L' ' == *pReader->pwz ||
        L'\t' == *pReader->pwz ||
        L'\r' == *pReader->pwz ||
        L'\n' == *pReader->pwz ||
        L',' == *pReader->pwz)
    {
        ++pReader->pwz;
    }

    switch (*pReader->pwz)
    {
    case L'{':
        *pToken = JSON_TOKEN_OBJECT_START;
        break;

    case L':': // begin object value
        *pToken = JSON_TOKEN_OBJECT_VALUE;
        break;

    case L'}': // end object
        *pToken = JSON_TOKEN_OBJECT_END;
        break;

    case L'[': // begin array
        *pToken = JSON_TOKEN_ARRAY_START;
        break;

    case L']': // end array
        *pToken = JSON_TOKEN_ARRAY_END;
        break;

    case L'"': // string
        *pToken = JSON_TOKEN_OBJECT_START == *pToken ? JSON_TOKEN_VALUE : JSON_TOKEN_OBJECT_KEY;
        break;

    case L't': // true
    case L'f': // false
    case L'n': // null
    case L'-': // number
    case L'0':
    case L'1':
    case L'2':
    case L'3':
    case L'4':
    case L'5':
    case L'6':
    case L'7':
    case L'8':
    case L'9':
        *pToken = JSON_TOKEN_VALUE;
        break;

    case L'\0':
        hr = E_NOMOREITEMS;
        break;

    default:
        hr = E_INVALIDSTATE;
    }

    return hr;
}


DAPI_(HRESULT) JsonReadNext(
    __in JSON_READER* pReader,
    __out JSON_TOKEN* pToken,
    __out JSON_VALUE* pValue
    )
{
    HRESULT hr = S_OK;
    //WCHAR wz;
    //JSON_TOKEN token = JSON_TOKEN_NONE;

    ::EnterCriticalSection(&pReader->cs);

    hr = NextToken(pReader, pToken);
    if (E_NOMOREITEMS == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to get next token.");

    if (JSON_TOKEN_VALUE == *pToken)
    {
        hr = JsonReadValue(pReader, pValue);
    }
    else
    {
        ++pReader->pwz;
    }

LExit:
    ::LeaveCriticalSection(&pReader->cs);
    return hr;
}


DAPI_(HRESULT) JsonReadValue(
    __in JSON_READER* /*pReader*/,
    __in JSON_VALUE* /*pValue*/
    )
{
    HRESULT hr = S_OK;

//LExit:
    return hr;
}


DAPI_(HRESULT) JsonInitializeWriter(
    __in JSON_WRITER* pWriter
    )
{
    memset(pWriter, 0, sizeof(JSON_WRITER));
    ::InitializeCriticalSection(&pWriter->cs);

    return S_OK;
}


DAPI_(void) JsonUninitializeWriter(
    __in JSON_WRITER* pWriter
    )
{
    ReleaseMem(pWriter->rgTokenStack);
    ReleaseStr(pWriter->sczJson);

    ::DeleteCriticalSection(&pWriter->cs);
    memset(pWriter, 0, sizeof(JSON_WRITER));
}


DAPI_(HRESULT) JsonWriteBool(
    __in JSON_WRITER* pWriter,
    __in BOOL fValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    hr = StrAllocString(&sczValue, fValue ? L"true" : L"false", 0);
    ExitOnFailure(hr, "Failed to convert boolean to string.");

    hr = DoValue(pWriter, sczValue);
    ExitOnFailure(hr, "Failed to add boolean to JSON.");

LExit:
    ReleaseStr(sczValue);
    return hr;
}


DAPI_(HRESULT) JsonWriteNumber(
    __in JSON_WRITER* pWriter,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    hr = StrAllocFormatted(&sczValue, L"%u", dwValue);
    ExitOnFailure(hr, "Failed to convert number to string.");

    hr = DoValue(pWriter, sczValue);
    ExitOnFailure(hr, "Failed to add number to JSON.");

LExit:
    ReleaseStr(sczValue);
    return hr;
}


DAPI_(HRESULT) JsonWriteString(
    __in JSON_WRITER* pWriter,
    __in_z LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczJsonString = NULL;

    hr = SerializeJsonString(&sczJsonString, wzValue);
    ExitOnFailure(hr, "Failed to allocate string JSON.");

    hr = DoValue(pWriter, sczJsonString);
    ExitOnFailure(hr, "Failed to add string to JSON.");

LExit:
    ReleaseStr(sczJsonString);
    return hr;
}


DAPI_(HRESULT) JsonWriteArrayStart(
    __in JSON_WRITER* pWriter
    )
{
    HRESULT hr = S_OK;

    hr = DoStart(pWriter, JSON_TOKEN_ARRAY_START, L"[");
    ExitOnFailure(hr, "Failed to start JSON array.");

LExit:
    return hr;
}


DAPI_(HRESULT) JsonWriteArrayEnd(
    __in JSON_WRITER* pWriter
    )
{
    HRESULT hr = S_OK;

    hr = DoEnd(pWriter, JSON_TOKEN_ARRAY_END, L"]");
    ExitOnFailure(hr, "Failed to end JSON array.");

LExit:
    return hr;
}


DAPI_(HRESULT) JsonWriteObjectStart(
    __in JSON_WRITER* pWriter
    )
{
    HRESULT hr = S_OK;

    hr = DoStart(pWriter, JSON_TOKEN_OBJECT_START, L"{");
    ExitOnFailure(hr, "Failed to start JSON object.");

LExit:
    return hr;
}


DAPI_(HRESULT) JsonWriteObjectKey(
    __in JSON_WRITER* pWriter,
    __in_z LPCWSTR wzKey
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczObjectKey = NULL;

    hr = StrAllocFormatted(&sczObjectKey, L"\"%ls\":", wzKey);
    ExitOnFailure(hr, "Failed to allocate JSON object key.");

    hr = DoKey(pWriter, sczObjectKey);
    ExitOnFailure(hr, "Failed to add object key to JSON.");

LExit:
    ReleaseStr(sczObjectKey);
    return hr;
}


DAPI_(HRESULT) JsonWriteObjectEnd(
    __in JSON_WRITER* pWriter
    )
{
    HRESULT hr = S_OK;

    hr = DoEnd(pWriter, JSON_TOKEN_OBJECT_END, L"}");
    ExitOnFailure(hr, "Failed to end JSON object.");

LExit:
    return hr;
}


static HRESULT DoStart(
    __in JSON_WRITER* pWriter,
    __in JSON_TOKEN tokenStart,
    __in_z LPCWSTR wzStartString
    )
{
    Assert(JSON_TOKEN_ARRAY_START == tokenStart || JSON_TOKEN_OBJECT_START == tokenStart);

    HRESULT hr = S_OK;
    JSON_TOKEN token = JSON_TOKEN_NONE;
    BOOL fNeedComma = FALSE;
    BOOL fPushToken = TRUE;

    ::EnterCriticalSection(&pWriter->cs);

    hr = EnsureTokenStack(pWriter);
    ExitOnFailure(hr, "Failed to ensure token stack for start.");

    token = pWriter->rgTokenStack[pWriter->cTokens - 1];
    switch (token)
    {
    case JSON_TOKEN_NONE:
        token = tokenStart;
        fPushToken = FALSE;
        break;

    case JSON_TOKEN_ARRAY_START: // array start changes to array value.
        token = JSON_TOKEN_ARRAY_VALUE;
        break;

    case JSON_TOKEN_ARRAY_VALUE:
    case JSON_TOKEN_ARRAY_END:
    case JSON_TOKEN_OBJECT_END:
        fNeedComma = TRUE;
        break;

    default: // everything else is not allowed.
        hr = E_UNEXPECTED;
        break;
    }
    ExitOnRootFailure(hr, "Cannot start array or object to JSON serializer now.");

    if (fNeedComma)
    {
        hr = StrAllocConcat(&pWriter->sczJson, L",", 0);
        ExitOnFailure(hr, "Failed to add comma for start array or object to JSON.");
    }

    hr = StrAllocConcat(&pWriter->sczJson, wzStartString, 0);
    ExitOnFailure(hr, "Failed to start JSON array or object.");

    pWriter->rgTokenStack[pWriter->cTokens - 1] = token;
    if (fPushToken)
    {
        pWriter->rgTokenStack[pWriter->cTokens] = tokenStart;
        ++pWriter->cTokens;
    }

LExit:
    ::LeaveCriticalSection(&pWriter->cs);
    return hr;
}


static HRESULT DoEnd(
    __in JSON_WRITER* pWriter,
    __in JSON_TOKEN tokenEnd,
    __in_z LPCWSTR wzEndString
    )
{
    HRESULT hr = S_OK;

    ::EnterCriticalSection(&pWriter->cs);

    if (!pWriter->rgTokenStack || 0 == pWriter->cTokens)
    {
        hr = E_UNEXPECTED;
        ExitOnRootFailure(hr, "Failure to pop token because the stack is empty.");
    }
    else
    {
        JSON_TOKEN token = pWriter->rgTokenStack[pWriter->cTokens - 1];
        if ((JSON_TOKEN_ARRAY_END == tokenEnd && JSON_TOKEN_ARRAY_START != token && JSON_TOKEN_ARRAY_VALUE != token) ||
            (JSON_TOKEN_OBJECT_END == tokenEnd && JSON_TOKEN_OBJECT_START != token && JSON_TOKEN_OBJECT_VALUE != token))
        {
            hr = E_UNEXPECTED;
            ExitOnRootFailure1(hr, "Failure to pop token because the stack did not match the expected token: %d", tokenEnd);
        }
    }

    hr = StrAllocConcat(&pWriter->sczJson, wzEndString, 0);
    ExitOnFailure(hr, "Failed to end JSON array or object.");

    --pWriter->cTokens;

LExit:
    ::LeaveCriticalSection(&pWriter->cs);
    return hr;
}


static HRESULT DoKey(
    __in JSON_WRITER* pWriter,
    __in_z LPCWSTR wzKey
    )
{
    HRESULT hr = S_OK;
    JSON_TOKEN token = JSON_TOKEN_NONE;
    BOOL fNeedComma = FALSE;

    ::EnterCriticalSection(&pWriter->cs);

    hr = EnsureTokenStack(pWriter);
    ExitOnFailure(hr, "Failed to ensure token stack for key.");

    token = pWriter->rgTokenStack[pWriter->cTokens - 1];
    switch (token)
    {
    case JSON_TOKEN_OBJECT_START:
        token = JSON_TOKEN_OBJECT_KEY;
        break;

    case JSON_TOKEN_OBJECT_VALUE:
        token = JSON_TOKEN_OBJECT_KEY;
        fNeedComma = TRUE;
        break;

    default: // everything else is not allowed.
        hr = E_UNEXPECTED;
        break;
    }
    ExitOnRootFailure(hr, "Cannot add key to JSON serializer now.");

    if (fNeedComma)
    {
        hr = StrAllocConcat(&pWriter->sczJson, L",", 0);
        ExitOnFailure(hr, "Failed to add comma for key to JSON.");
    }

    hr = StrAllocConcat(&pWriter->sczJson, wzKey, 0);
    ExitOnFailure(hr, "Failed to add key to JSON.");

    pWriter->rgTokenStack[pWriter->cTokens - 1] = token;

LExit:
    ::LeaveCriticalSection(&pWriter->cs);
    return hr;
}


static HRESULT DoValue(
    __in JSON_WRITER* pWriter,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    JSON_TOKEN token = JSON_TOKEN_NONE;
    BOOL fNeedComma = FALSE;

    ::EnterCriticalSection(&pWriter->cs);

    hr = EnsureTokenStack(pWriter);
    ExitOnFailure(hr, "Failed to ensure token stack for value.");

    token = pWriter->rgTokenStack[pWriter->cTokens - 1];
    switch (token)
    {
    case JSON_TOKEN_ARRAY_START:
        token = JSON_TOKEN_ARRAY_VALUE;
        break;

    case JSON_TOKEN_ARRAY_VALUE:
        fNeedComma = TRUE;
        break;

    case JSON_TOKEN_OBJECT_KEY:
        token = JSON_TOKEN_OBJECT_VALUE;
        break;

    case JSON_TOKEN_NONE:
        token = JSON_TOKEN_VALUE;
        break;

    default: // everything else is not allowed.
        hr = E_UNEXPECTED;
        break;
    }
    ExitOnRootFailure(hr, "Cannot add value to JSON serializer now.");

    if (fNeedComma)
    {
        hr = StrAllocConcat(&pWriter->sczJson, L",", 0);
        ExitOnFailure(hr, "Failed to add comma for value to JSON.");
    }

    if (wzValue)
    {
        hr = StrAllocConcat(&pWriter->sczJson, wzValue, 0);
        ExitOnFailure(hr, "Failed to add value to JSON.");
    }
    else
    {
        hr = StrAllocConcat(&pWriter->sczJson, L"null", 0);
        ExitOnFailure(hr, "Failed to add null value to JSON.");
    }

    pWriter->rgTokenStack[pWriter->cTokens - 1] = token;

LExit:
    ::LeaveCriticalSection(&pWriter->cs);
    return hr;
}


static HRESULT EnsureTokenStack(
    __in JSON_WRITER* pWriter
    )
{
    HRESULT hr = S_OK;
    DWORD cNumAlloc = pWriter->cTokens != 0 ? pWriter->cTokens : 0;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pWriter->rgTokenStack), cNumAlloc, sizeof(JSON_TOKEN), JSON_STACK_INCREMENT);
    ExitOnFailure(hr, "Failed to allocate JSON token stack.");

    if (0 == pWriter->cTokens)
    {
        pWriter->rgTokenStack[0] = JSON_TOKEN_NONE;
        ++pWriter->cTokens;
    }

LExit:
    return hr;
}


static HRESULT SerializeJsonString(
    __out_z LPWSTR* psczJsonString,
    __in_z LPCWSTR wzString
    )
{
    HRESULT hr = S_OK;
    DWORD cchRequired = 3; // start with enough space for null terminated empty quoted string (aka: ""\0)

    for (LPCWSTR pch = wzString; *pch; ++pch)
    {
        // If it is a special JSON character, add space for the escape backslash.
        if (L'"' == *pch || L'\\' == *pch || L'/' == *pch || L'\b' == *pch || L'\f' == *pch || L'\n' == *pch || L'\r' == *pch || L'\t' == *pch)
        {
            ++cchRequired;
        }

        ++cchRequired;
    }

    hr = StrAlloc(psczJsonString, cchRequired);
    ExitOnFailure(hr, "Failed to allocate space for JSON string.");

    LPWSTR pchTarget = *psczJsonString;

    *pchTarget = L'\"';
    ++pchTarget;

    for (LPCWSTR pch = wzString; *pch; ++pch, ++pchTarget)
    {
        // If it is a special JSON character, handle it or just add the character as is.
        switch (*pch)
        {
        case L'"':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'"';
            break;

        case L'\\':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'\\';
            break;

        case L'/':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'/';
            break;

        case L'\b':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'b';
            break;

        case L'\f':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'f';
            break;

        case L'\n':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'n';
            break;

        case L'\r':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L'r';
            break;

        case L'\t':
            *pchTarget = L'\\';
            ++pchTarget;
            *pchTarget = L't';
            break;

        default:
            *pchTarget = *pch;
            break;
        }

    }

    *pchTarget = L'\"';
    ++pchTarget;
    *pchTarget = L'\0';

LExit:
    return hr;
}
