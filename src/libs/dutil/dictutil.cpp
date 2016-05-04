// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// These should all be primes, and spaced reasonably apart (currently each is about 4x the last)
const DWORD MAX_BUCKET_SIZES[] = {
    503,
    2017,
    7937,
    32779,
    131111,
    524341,
    2097709,
    8390857,
    33563437,
    134253719,
    537014927,
    2148059509
    };

// However many items are in the cab, let's keep the buckets at least 8 times that to avoid collisions
#define MAX_BUCKETS_TO_ITEMS_RATIO 8

enum DICT_TYPE
{
    DICT_INVALID = 0,
    DICT_EMBEDDED_KEY = 1,
    DICT_STRING_LIST = 2
};

struct STRINGDICT_STRUCT
{
    DICT_TYPE dtType;

    // Optional flags to control the behavior of the dictionary.
    DICT_FLAG dfFlags;

    // Index into MAX_BUCKET_SIZES (array of primes), representing number of buckets we've allocated
    DWORD dwBucketSizeIndex;

    // Number of items currently stored in the dict buckets
    DWORD dwNumItems;

    // Byte offset of key within bucket value, for collision checking - see
    // comments above DictCreateEmbeddedKey() implementation for further details
    size_t cByteOffset;

    // The actual stored buckets
    void **ppvBuckets;

    // The actual stored items in the order they were added (used for auto freeing or enumerating)
    void **ppvItemList;

    // Pointer to the array of items, so the caller is free to resize the array of values out from under us without harm
    void **ppvValueArray;
};

const int STRINGDICT_HANDLE_BYTES = sizeof(STRINGDICT_STRUCT);

static HRESULT StringHash(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwNumBuckets,
    __in_z LPCWSTR pszString,
    __out LPDWORD pdwHash
    );
static BOOL IsMatchExact(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwMatchIndex,
    __in_z LPCWSTR wzOriginalString
    );
static HRESULT GetValue(
    __in const STRINGDICT_STRUCT *psd,
    __in_z LPCWSTR pszString,
    __out_opt void **ppvValue
    );
static HRESULT GetInsertIndex(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwBucketCount,
    __in void **ppvBuckets,
    __in_z LPCWSTR pszString,
    __out DWORD *pdwOutput
    );
static HRESULT GetIndex(
    __in const STRINGDICT_STRUCT *psd,
    __in_z LPCWSTR pszString,
    __out DWORD *pdwOutput
    );
static LPCWSTR GetKey(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    );
static HRESULT GrowDictionary(
    __inout STRINGDICT_STRUCT *psd
    );
// These 2 helper functions allow us to safely handle dictutil consumers resizing
// the value array by storing "offsets" instead of raw void *'s in our buckets.
static void * TranslateOffsetToValue(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    );
static void * TranslateValueToOffset(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    );

// The dict will store a set of keys (as wide-char strings) and a set of values associated with those keys (as void *'s).
// However, to support collision checking, the key needs to be represented in the "value" object (pointed to
// by the void *). The "stByteOffset" parameter tells this dict the byte offset of the "key" string pointer
// within the "value" object. Use the offsetof() macro to fill this out.
// The "ppvArray" parameter gives dictutil the address of your value array. If you provide this parameter,
// dictutil will remember all pointer values provided as "offsets" against this array. It is only necessary to provide
// this parameter to dictutil if it is possible you will realloc the array.
//
// Use DictAddValue() and DictGetValue() with this dictionary type.
extern "C" HRESULT DAPI DictCreateWithEmbeddedKey(
    __out_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE* psdHandle,
    __in DWORD dwNumExpectedItems,
    __in_opt void **ppvArray,
    __in size_t cByteOffset,
    __in DICT_FLAG dfFlags
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(psdHandle, hr, E_INVALIDARG, "Handle not specified while creating dict");

    // Allocate the handle
    *psdHandle = static_cast<STRINGDICT_HANDLE>(MemAlloc(sizeof(STRINGDICT_STRUCT), FALSE));
    ExitOnNull(*psdHandle, hr, E_OUTOFMEMORY, "Failed to allocate dictionary object");

    STRINGDICT_STRUCT *psd = static_cast<STRINGDICT_STRUCT *>(*psdHandle);

    // Fill out the new handle's values
    psd->dtType = DICT_EMBEDDED_KEY;
    psd->dfFlags = dfFlags;
    psd->cByteOffset = cByteOffset;
    psd->dwBucketSizeIndex = 0;
    psd->dwNumItems = 0;
    psd->ppvItemList = NULL;
    psd->ppvValueArray = ppvArray;

    // Make psd->dwBucketSizeIndex point to the appropriate spot in the prime
    // array based on expected number of items and items to buckets ratio
    // Careful: the "-1" in "countof(MAX_BUCKET_SIZES)-1" ensures we don't end
    // this loop past the end of the array!
    while (psd->dwBucketSizeIndex < (countof(MAX_BUCKET_SIZES)-1) &&
           MAX_BUCKET_SIZES[psd->dwBucketSizeIndex] < dwNumExpectedItems * MAX_BUCKETS_TO_ITEMS_RATIO)
    {
        ++psd->dwBucketSizeIndex;
    }

    // Finally, allocate our initial buckets
    psd->ppvBuckets = static_cast<void**>(MemAlloc(sizeof(void *) * MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], TRUE));
    ExitOnNull(psd->ppvBuckets, hr, E_OUTOFMEMORY, "Failed to allocate buckets for dictionary");

LExit:
    return hr;
}

// The dict will store a set of keys, with no values associated with them. Use DictAddKey() and DictKeyExists() with this dictionary type.
extern "C" HRESULT DAPI DictCreateStringList(
    __out_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE* psdHandle,
    __in DWORD dwNumExpectedItems,
    __in DICT_FLAG dfFlags
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(psdHandle, hr, E_INVALIDARG, "Handle not specified while creating dict");

    // Allocate the handle
    *psdHandle = static_cast<STRINGDICT_HANDLE>(MemAlloc(sizeof(STRINGDICT_STRUCT), FALSE));
    ExitOnNull(*psdHandle, hr, E_OUTOFMEMORY, "Failed to allocate dictionary object");

    STRINGDICT_STRUCT *psd = static_cast<STRINGDICT_STRUCT *>(*psdHandle);

    // Fill out the new handle's values
    psd->dtType = DICT_STRING_LIST;
    psd->dfFlags = dfFlags;
    psd->cByteOffset = 0;
    psd->dwBucketSizeIndex = 0;
    psd->dwNumItems = 0;
    psd->ppvItemList = NULL;
    psd->ppvValueArray = NULL;

    // Make psd->dwBucketSizeIndex point to the appropriate spot in the prime
    // array based on expected number of items and items to buckets ratio
    // Careful: the "-1" in "countof(MAX_BUCKET_SIZES)-1" ensures we don't end
    // this loop past the end of the array!
    while (psd->dwBucketSizeIndex < (countof(MAX_BUCKET_SIZES)-1) &&
           MAX_BUCKET_SIZES[psd->dwBucketSizeIndex] < dwNumExpectedItems * MAX_BUCKETS_TO_ITEMS_RATIO)
    {
        ++psd->dwBucketSizeIndex;
    }

    // Finally, allocate our initial buckets
    psd->ppvBuckets = static_cast<void**>(MemAlloc(sizeof(void *) * MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], TRUE));
    ExitOnNull(psd->ppvBuckets, hr, E_OUTOFMEMORY, "Failed to allocate buckets for dictionary");

LExit:
    return hr;
}

extern "C" HRESULT DAPI DictCreateStringListFromArray(
    __out_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE* psdHandle,
    __in_ecount(cStringArray) const LPCWSTR* rgwzStringArray,
    __in const DWORD cStringArray,
    __in DICT_FLAG dfFlags
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE sd = NULL;

    hr = DictCreateStringList(&sd, cStringArray, dfFlags);
    ExitOnFailure(hr, "Failed to create the string dictionary.");

    for (DWORD i = 0; i < cStringArray; ++i)
    {
        const LPCWSTR wzKey = rgwzStringArray[i];

        hr = DictKeyExists(sd, wzKey);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the string dictionary.");
        }
        else
        {
            hr = DictAddKey(sd, wzKey);
            ExitOnFailure1(hr, "Failed to add \"%ls\" to the string dictionary.", wzKey);
        }
    }

    *psdHandle = sd;
    sd = NULL;

LExit:
    ReleaseDict(sd);

    return hr;
}

extern "C" HRESULT DAPI DictCompareStringListToArray(
    __in_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE sdStringList,
    __in_ecount(cStringArray) const LPCWSTR* rgwzStringArray,
    __in const DWORD cStringArray
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < cStringArray; ++i)
    {
        hr = DictKeyExists(sdStringList, rgwzStringArray[i]);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the string dictionary.");
            ExitFunction1(hr = S_OK);
        }
    }

    ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_NO_MATCH));

LExit:
    return hr;
}

// Todo: Dict should resize itself when (number of items) exceeds (number of buckets / MAX_BUCKETS_TO_ITEMS_RATIO)
extern "C" HRESULT DAPI DictAddKey(
    __in_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE sdHandle,
    __in_z LPCWSTR pszString
    )
{
    HRESULT hr = S_OK;
    DWORD dwIndex = 0;
    STRINGDICT_STRUCT *psd = static_cast<STRINGDICT_STRUCT *>(sdHandle);

    ExitOnNull(sdHandle, hr, E_INVALIDARG, "Handle not specified while adding value to dict");
    ExitOnNull(pszString, hr, E_INVALIDARG, "String not specified while adding value to dict");

    if (psd->dwBucketSizeIndex >= countof(MAX_BUCKET_SIZES))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid dictionary - bucket size index is out of range");
    }

    if (DICT_STRING_LIST != psd->dtType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Tried to add key without value to wrong dictionary type! This dictionary type is: %d", psd->dtType);
    }

    if ((psd->dwNumItems + 1) >= MAX_BUCKET_SIZES[psd->dwBucketSizeIndex] / MAX_BUCKETS_TO_ITEMS_RATIO)
    {
        hr = GrowDictionary(psd);
        if (HRESULT_FROM_WIN32(ERROR_DATABASE_FULL) == hr)
        {
            // If we fail to proactively grow the dictionary, don't fail unless the dictionary is completely full
            if (psd->dwNumItems < MAX_BUCKET_SIZES[psd->dwBucketSizeIndex])
            {
                hr = S_OK;
            }
        }
        ExitOnFailure(hr, "Failed to grow dictionary");
    }

    hr = GetInsertIndex(psd, MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], psd->ppvBuckets, pszString, &dwIndex);
    ExitOnFailure(hr, "Failed to get index to insert into");

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&(psd->ppvItemList)), psd->dwNumItems + 1, sizeof(void *), 1000);
    ExitOnFailure(hr, "Failed to resize list of items in dictionary");
    ++psd->dwNumItems;

    hr = StrAllocString(reinterpret_cast<LPWSTR *>(&(psd->ppvBuckets[dwIndex])), pszString, 0);
    ExitOnFailure(hr, "Failed to allocate copy of string");

    psd->ppvItemList[psd->dwNumItems-1] = psd->ppvBuckets[dwIndex];

LExit:
    return hr;
}

// Todo: Dict should resize itself when (number of items) exceeds (number of buckets / MAX_BUCKETS_TO_ITEMS_RATIO)
extern "C" HRESULT DAPI DictAddValue(
    __in_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE sdHandle,
    __in void *pvValue
    )
{
    HRESULT hr = S_OK;
    void *pvOffset = NULL;
    LPCWSTR wzKey = NULL;
    DWORD dwIndex = 0;
    STRINGDICT_STRUCT *psd = static_cast<STRINGDICT_STRUCT *>(sdHandle);

    ExitOnNull(sdHandle, hr, E_INVALIDARG, "Handle not specified while adding value to dict");
    ExitOnNull(pvValue, hr, E_INVALIDARG, "Value not specified while adding value to dict");

    if (psd->dwBucketSizeIndex >= countof(MAX_BUCKET_SIZES))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid dictionary - bucket size index is out of range");
    }

    if (DICT_EMBEDDED_KEY != psd->dtType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Tried to add key/value pair to wrong dictionary type! This dictionary type is: %d", psd->dtType);
    }

    wzKey = GetKey(psd, pvValue);
    ExitOnNull(wzKey, hr, E_INVALIDARG, "String not specified while adding value to dict");

    if ((psd->dwNumItems + 1) >= MAX_BUCKET_SIZES[psd->dwBucketSizeIndex] / MAX_BUCKETS_TO_ITEMS_RATIO)
    {
        hr = GrowDictionary(psd);
        if (HRESULT_FROM_WIN32(ERROR_DATABASE_FULL) == hr && psd->dwNumItems + 1 )
        {
            // If we fail to proactively grow the dictionary, don't fail unless the dictionary is completely full
            if (psd->dwNumItems < MAX_BUCKET_SIZES[psd->dwBucketSizeIndex])
            {
                hr = S_OK;
            }
        }
        ExitOnFailure(hr, "Failed to grow dictionary");
    }

    hr = GetInsertIndex(psd, MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], psd->ppvBuckets, wzKey, &dwIndex);
    ExitOnFailure(hr, "Failed to get index to insert into");

    hr = MemEnsureArraySize(reinterpret_cast<void **>(&(psd->ppvItemList)), psd->dwNumItems + 1, sizeof(void *), 1000);
    ExitOnFailure(hr, "Failed to resize list of items in dictionary");
    ++psd->dwNumItems;

    pvOffset = TranslateValueToOffset(psd, pvValue);
    psd->ppvBuckets[dwIndex] = pvOffset;
    psd->ppvItemList[psd->dwNumItems-1] = pvOffset;

LExit:
    return hr;
}

extern "C" HRESULT DAPI DictGetValue(
    __in_bcount(STRINGDICT_HANDLE_BYTES) C_STRINGDICT_HANDLE sdHandle,
    __in_z LPCWSTR pszString,
    __out void **ppvValue
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(sdHandle, hr, E_INVALIDARG, "Handle not specified while searching dict");
    ExitOnNull(pszString, hr, E_INVALIDARG, "String not specified while searching dict");

    const STRINGDICT_STRUCT *psd = static_cast<const STRINGDICT_STRUCT *>(sdHandle);

    if (DICT_EMBEDDED_KEY != psd->dtType)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Tried to lookup value in wrong dictionary type! This dictionary type is: %d", psd->dtType);
    }

    hr = GetValue(psd, pszString, ppvValue);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to call internal GetValue()");

LExit:
    return hr;
}

extern "C" HRESULT DAPI DictKeyExists(
    __in_bcount(STRINGDICT_HANDLE_BYTES) C_STRINGDICT_HANDLE sdHandle,
    __in_z LPCWSTR pszString
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(sdHandle, hr, E_INVALIDARG, "Handle not specified while searching dict");
    ExitOnNull(pszString, hr, E_INVALIDARG, "String not specified while searching dict");

    const STRINGDICT_STRUCT *psd = static_cast<const STRINGDICT_STRUCT *>(sdHandle);

    // This works with either type of dictionary
    hr = GetValue(psd, pszString, NULL);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to call internal GetValue()");

LExit:
    return hr;
}

extern "C" void DAPI DictDestroy(
    __in_bcount(STRINGDICT_HANDLE_BYTES) STRINGDICT_HANDLE sdHandle
    )
{
    DWORD i;

    STRINGDICT_STRUCT *psd = static_cast<STRINGDICT_STRUCT *>(sdHandle);

    if (DICT_STRING_LIST == psd->dtType)
    {
        for (i = 0; i < psd->dwNumItems; ++i)
        {
            ReleaseStr(reinterpret_cast<LPWSTR>(psd->ppvItemList[i]));
        }
    }

    ReleaseMem(psd->ppvItemList);
    ReleaseMem(psd->ppvBuckets);
    ReleaseMem(psd);
}

static HRESULT StringHash(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwNumBuckets,
    __in_z LPCWSTR pszString,
    __out DWORD *pdwHash
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzKey = NULL;
    LPWSTR sczNewKey = NULL;
    DWORD result = 0;

    if (DICT_FLAG_CASEINSENSITIVE & psd->dfFlags)
    {
        hr = StrAllocStringToUpperInvariant(&sczNewKey, pszString, 0);
        ExitOnFailure(hr, "Failed to convert the string to upper-case.");

        wzKey = sczNewKey;
    }
    else
    {
        wzKey = pszString;
    }

    while (*wzKey)
    {
        result = ~(*wzKey++ * 509) + result * 65599;
    }

    *pdwHash = result % dwNumBuckets;

LExit:
    ReleaseStr(sczNewKey);

    return hr;
}

static BOOL IsMatchExact(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwMatchIndex,
    __in_z LPCWSTR wzOriginalString
    )
{
    LPCWSTR wzMatchString = GetKey(psd, TranslateOffsetToValue(psd, psd->ppvBuckets[dwMatchIndex]));
    DWORD dwFlags = 0;

    if (DICT_FLAG_CASEINSENSITIVE & psd->dfFlags)
    {
        dwFlags |= NORM_IGNORECASE;
    }

    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, dwFlags, wzOriginalString, -1, wzMatchString, -1))
    {
        return TRUE;
    }

    return FALSE;
}

static HRESULT GetValue(
    __in const STRINGDICT_STRUCT *psd,
    __in_z LPCWSTR pszString,
    __out_opt void **ppvValue
    )
{
    HRESULT hr = S_OK;
    DWORD dwOriginalIndexCandidate = 0;
    void *pvCandidateValue = NULL;
    DWORD dwIndex = 0;

    ExitOnNull(psd, hr, E_INVALIDARG, "Handle not specified while searching dict");
    ExitOnNull(pszString, hr, E_INVALIDARG, "String not specified while searching dict");

    if (psd->dwBucketSizeIndex >= countof(MAX_BUCKET_SIZES))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid dictionary - bucket size index is out of range");
    }

    hr = StringHash(psd, MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], pszString, &dwOriginalIndexCandidate);
    ExitOnFailure(hr, "Failed to hash the string.");

    DWORD dwIndexCandidate = dwOriginalIndexCandidate;

    pvCandidateValue = TranslateOffsetToValue(psd, psd->ppvBuckets[dwIndexCandidate]);

    // If no match exists in the dict
    if (NULL == pvCandidateValue)
    {
        if (NULL != ppvValue)
        {
            *ppvValue = NULL;
        }
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = GetIndex(psd, pszString, &dwIndex);
    if (E_NOTFOUND == hr)
    {
        ExitFunction();
    }
    ExitOnFailure(hr, "Failed to find index to get");

    if (NULL != ppvValue)
    {
        *ppvValue = TranslateOffsetToValue(psd, psd->ppvBuckets[dwIndex]);
    }

LExit:
    if (FAILED(hr) && NULL != ppvValue)
    {
        *ppvValue = NULL;
    }

    return hr;
}

static HRESULT GetInsertIndex(
    __in const STRINGDICT_STRUCT *psd,
    __in DWORD dwBucketCount,
    __in void **ppvBuckets,
    __in_z LPCWSTR pszString,
    __out DWORD *pdwOutput
    )
{
    HRESULT hr = S_OK;
    DWORD dwOriginalIndexCandidate = 0;

    hr = StringHash(psd, dwBucketCount, pszString, &dwOriginalIndexCandidate);
    ExitOnFailure(hr, "Failed to hash the string.");

    DWORD dwIndexCandidate = dwOriginalIndexCandidate;

    // If we collide, keep iterating forward from our intended position, even wrapping around to zero, until we find an empty bucket
#pragma prefast(push)
#pragma prefast(disable:26007)
    while (NULL != ppvBuckets[dwIndexCandidate])
#pragma prefast(pop)
    {
        ++dwIndexCandidate;

        // If we got to the end of the array, wrap around to zero index
        if (dwIndexCandidate >= dwBucketCount)
        {
            dwIndexCandidate = 0;
        }

        // If we wrapped all the way back around to our original index, the dict is full - throw an error
        if (dwIndexCandidate == dwOriginalIndexCandidate)
        {
            // The dict table is full - this error seems to be a reasonably close match 
            hr = HRESULT_FROM_WIN32(ERROR_DATABASE_FULL);
            ExitOnRootFailure1(hr, "Failed to add item '%ls' to dict table because dict table is full of items", pszString);
        }
    }

    *pdwOutput = dwIndexCandidate;

LExit:
    return hr;
}

static HRESULT GetIndex(
    __in const STRINGDICT_STRUCT *psd,
    __in_z LPCWSTR pszString,
    __out DWORD *pdwOutput
    )
{
    HRESULT hr = S_OK;
    DWORD dwOriginalIndexCandidate = 0;

    if (psd->dwBucketSizeIndex >= countof(MAX_BUCKET_SIZES))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid dictionary - bucket size index is out of range");
    }

    hr = StringHash(psd, MAX_BUCKET_SIZES[psd->dwBucketSizeIndex], pszString, &dwOriginalIndexCandidate);
    ExitOnFailure(hr, "Failed to hash the string.");

    DWORD dwIndexCandidate = dwOriginalIndexCandidate;

    while (!IsMatchExact(psd, dwIndexCandidate, pszString))
    {
        ++dwIndexCandidate;

        // If we got to the end of the array, wrap around to zero index
        if (dwIndexCandidate >= MAX_BUCKET_SIZES[psd->dwBucketSizeIndex])
        {
            dwIndexCandidate = 0;
        }

        // If no match exists in the dict
        if (NULL == psd->ppvBuckets[dwIndexCandidate])
        {
            ExitFunction1(hr = E_NOTFOUND);
        }

        // If we wrapped all the way back around to our original index, the dict is full and we found nothing, so return as such
        if (dwIndexCandidate == dwOriginalIndexCandidate)
        {
            ExitFunction1(hr = E_NOTFOUND);
        }
    }

    *pdwOutput = dwIndexCandidate;

LExit:
    return hr;
}

static LPCWSTR GetKey(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    )
{
    const BYTE *lpByte = reinterpret_cast<BYTE *>(pvValue);

    if (DICT_EMBEDDED_KEY == psd->dtType)
    {
        void *pvKey = reinterpret_cast<void *>(reinterpret_cast<BYTE *>(pvValue) + psd->cByteOffset);

#pragma prefast(push)
#pragma prefast(disable:26010)
        return *(reinterpret_cast<LPCWSTR *>(pvKey));
#pragma prefast(pop)
    }
    else
    {
        return (reinterpret_cast<LPCWSTR>(lpByte));
    }
}

static HRESULT GrowDictionary(
    __inout STRINGDICT_STRUCT *psd
    )
{
    HRESULT hr = S_OK;
    DWORD dwInsertIndex = 0;
    LPCWSTR wzKey = NULL;
    DWORD dwNewBucketSizeIndex = 0;
    size_t cbAllocSize = 0;
    void **ppvNewBuckets = NULL;

    dwNewBucketSizeIndex = psd->dwBucketSizeIndex + 1;

    if (dwNewBucketSizeIndex >= countof(MAX_BUCKET_SIZES))
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_DATABASE_FULL));
    }

    hr = ::SizeTMult(sizeof(void *), MAX_BUCKET_SIZES[dwNewBucketSizeIndex], &cbAllocSize);
    ExitOnFailure(hr, "Overflow while calculating allocation size to grow dictionary");

    ppvNewBuckets = static_cast<void**>(MemAlloc(cbAllocSize, TRUE));
    ExitOnNull1(ppvNewBuckets, hr, E_OUTOFMEMORY, "Failed to allocate %u buckets while growing dictionary", MAX_BUCKET_SIZES[dwNewBucketSizeIndex]);

    for (DWORD i = 0; i < psd->dwNumItems; ++i)
    {
        wzKey = GetKey(psd, TranslateOffsetToValue(psd, psd->ppvItemList[i]));
        ExitOnNull(wzKey, hr, E_INVALIDARG, "String not specified in existing dict value");

        hr = GetInsertIndex(psd, MAX_BUCKET_SIZES[dwNewBucketSizeIndex], ppvNewBuckets, wzKey, &dwInsertIndex);
        ExitOnFailure(hr, "Failed to get index to insert into");

        ppvNewBuckets[dwInsertIndex] = psd->ppvItemList[i];
    }

    psd->dwBucketSizeIndex = dwNewBucketSizeIndex;
    ReleaseMem(psd->ppvBuckets);
    psd->ppvBuckets = ppvNewBuckets;
    ppvNewBuckets = NULL;

LExit:
    ReleaseMem(ppvNewBuckets);

    return hr;
}

static void * TranslateOffsetToValue(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    )
{
    if (NULL == pvValue)
    {
        return NULL;
    }

    // All offsets are stored as (real offset + 1), so subtract 1 to get back to the real value
    if (NULL != psd->ppvValueArray)
    {
        return reinterpret_cast<void *>(reinterpret_cast<DWORD_PTR>(pvValue) + reinterpret_cast<DWORD_PTR>(*psd->ppvValueArray) - 1);
    }
    else
    {
        return pvValue;
    }
}

static void * TranslateValueToOffset(
    __in const STRINGDICT_STRUCT *psd,
    __in void *pvValue
    )
{
    if (NULL != psd->ppvValueArray)
    {
        // 0 has a special meaning - we don't want offset 0 into the array to have NULL for the offset - so add 1 to avoid this issue
        return reinterpret_cast<void *>(reinterpret_cast<DWORD_PTR>(pvValue) - reinterpret_cast<DWORD_PTR>(*psd->ppvValueArray) + 1);
    }
    else
    {
        return pvValue;
    }
}
