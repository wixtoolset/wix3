// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const LPCWSTR wzSectionSeparator = L"\\";

struct INI_STRUCT
{
    LPWSTR sczPath; // the path to the INI file to be parsed

    LPWSTR sczOpenTagPrefix; // For regular ini, this would be '['
    LPWSTR sczOpenTagPostfix; // For regular ini, this would be ']'

    LPWSTR sczValuePrefix; // for regular ini, this would be NULL
    LPWSTR sczValueSeparator; // for regular ini, this would be '='

    LPWSTR sczCommentLinePrefix; // for regular ini, this would be ';'

    INI_VALUE *rgivValues;
    DWORD cValues;

    LPWSTR *rgsczLines;
    DWORD cLines;

    FILE_ENCODING feEncoding;
    BOOL fModified;
};

const int INI_HANDLE_BYTES = sizeof(INI_STRUCT);

static HRESULT GetSectionPrefixFromName(
    __in_z LPCWSTR wzName,
    __deref_out_z LPWSTR* psczOutput
    );
static void UninitializeIniValue(
    INI_VALUE *pivValue
    );

extern "C" HRESULT DAPI IniInitialize(
    __out_bcount(INI_HANDLE_BYTES) INI_HANDLE* piHandle
    )
{
    HRESULT hr = S_OK;

    // Allocate the handle
    *piHandle = static_cast<INI_HANDLE>(MemAlloc(sizeof(INI_STRUCT), TRUE));
    ExitOnNull(*piHandle, hr, E_OUTOFMEMORY, "Failed to allocate ini object");

LExit:
    return hr;
}

extern "C" void DAPI IniUninitialize(
    __in_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle
    )
{
    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    ReleaseStr(pi->sczPath);
    ReleaseStr(pi->sczOpenTagPrefix);
    ReleaseStr(pi->sczOpenTagPostfix);
    ReleaseStr(pi->sczValuePrefix);
    ReleaseStr(pi->sczValueSeparator);
    ReleaseStr(pi->sczCommentLinePrefix);

    for (DWORD i = 0; i < pi->cValues; ++i)
    {
        UninitializeIniValue(pi->rgivValues + i);
    }
    ReleaseMem(pi->rgivValues);

    ReleaseStrArray(pi->rgsczLines, pi->cLines);

    ReleaseMem(pi);
}

extern "C" HRESULT DAPI IniSetOpenTag(
    __inout_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in_z_opt LPCWSTR wzOpenTagPrefix,
    __in_z_opt LPCWSTR wzOpenTagPostfix
    )
{
    HRESULT hr = S_OK;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    if (wzOpenTagPrefix)
    {
        hr = StrAllocString(&pi->sczOpenTagPrefix, wzOpenTagPrefix, 0);
        ExitOnFailure1(hr, "Failed to copy open tag prefix to ini struct: %ls", wzOpenTagPrefix);
    }
    else
    {
        ReleaseNullStr(pi->sczOpenTagPrefix);
    }

    if (wzOpenTagPostfix)
    {
        hr = StrAllocString(&pi->sczOpenTagPostfix, wzOpenTagPostfix, 0);
        ExitOnFailure1(hr, "Failed to copy open tag postfix to ini struct: %ls", wzOpenTagPostfix);
    }
    else
    {
        ReleaseNullStr(pi->sczOpenTagPrefix);
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI IniSetValueStyle(
    __inout_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in_z_opt LPCWSTR wzValuePrefix,
    __in_z_opt LPCWSTR wzValueSeparator
    )
{
    HRESULT hr = S_OK;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    if (wzValuePrefix)
    {
        hr = StrAllocString(&pi->sczValuePrefix, wzValuePrefix, 0);
        ExitOnFailure1(hr, "Failed to copy value prefix to ini struct: %ls", wzValuePrefix);
    }
    else
    {
        ReleaseNullStr(pi->sczValuePrefix);
    }

    if (wzValueSeparator)
    {
        hr = StrAllocString(&pi->sczValueSeparator, wzValueSeparator, 0);
        ExitOnFailure1(hr, "Failed to copy value separator to ini struct: %ls", wzValueSeparator);
    }
    else
    {
        ReleaseNullStr(pi->sczValueSeparator);
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI IniSetCommentStyle(
    __inout_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in_z_opt LPCWSTR wzLinePrefix
    )
{
    HRESULT hr = S_OK;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    if (wzLinePrefix)
    {
        hr = StrAllocString(&pi->sczCommentLinePrefix, wzLinePrefix, 0);
        ExitOnFailure1(hr, "Failed to copy comment line prefix to ini struct: %ls", wzLinePrefix);
    }
    else
    {
        ReleaseNullStr(pi->sczCommentLinePrefix);
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI IniParse(
    __inout_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in LPCWSTR wzPath,
    __out_opt FILE_ENCODING *pfeEncodingFound
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczContents = NULL;
    LPWSTR sczCurrentSection = NULL;
    LPWSTR sczName = NULL;
    LPWSTR sczNameTrimmed = NULL;
    LPWSTR sczValue = NULL;
    LPWSTR sczValueTrimmed = NULL;
    LPWSTR wzOpenTagPrefix = NULL;
    LPWSTR wzOpenTagPostfix = NULL;
    LPWSTR wzValuePrefix = NULL;
    LPWSTR wzValueSeparator = NULL;
    LPWSTR wzCommentLinePrefix = NULL;
    LPWSTR wzValueBegin = NULL;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    BOOL fSections = (NULL != pi->sczOpenTagPrefix) && (NULL != pi->sczOpenTagPostfix);
    BOOL fValuePrefix = (NULL != pi->sczValuePrefix);

    hr = StrAllocString(&pi->sczPath, wzPath, 0);
    ExitOnFailure1(hr, "Failed to copy path to ini struct: %ls", wzPath);

    hr = FileToString(pi->sczPath, &sczContents, &pi->feEncoding);
    ExitOnFailure1(hr, "Failed to convert file to string: %ls", pi->sczPath);

    if (pfeEncodingFound)
    {
        *pfeEncodingFound = pi->feEncoding;
    }

    if (!sczContents || !*sczContents)
    {
        // Empty string, nothing to parse
        ExitFunction1(hr = S_OK);
    }

    hr = StrSplitAllocArray(&pi->rgsczLines, reinterpret_cast<UINT *>(&pi->cLines), sczContents, L"\n");
    ExitOnFailure(hr, "Failed to split INI file into lines");

    for (DWORD i = 0; i < pi->cLines; ++i)
    {
        if (!*pi->rgsczLines[i])
        {
            continue;
        }

        if (pi->sczCommentLinePrefix)
        {
            wzCommentLinePrefix = wcsstr(pi->rgsczLines[i], pi->sczCommentLinePrefix);

            if (wzCommentLinePrefix && wzCommentLinePrefix <= pi->rgsczLines[i] + 1)
            {
                continue;
            }
        }

        if (pi->sczOpenTagPrefix)
        {
            wzOpenTagPrefix = wcsstr(pi->rgsczLines[i], pi->sczOpenTagPrefix);
        }

        if (pi->sczOpenTagPostfix)
        {
            wzOpenTagPostfix = wcsstr(pi->rgsczLines[i], pi->sczOpenTagPostfix);
        }

        if (pi->sczValuePrefix)
        {
            wzValuePrefix = wcsstr(pi->rgsczLines[i], pi->sczValuePrefix);
        }

        if (pi->sczValueSeparator)
        {
            if (wzValuePrefix)
            {
                wzValueSeparator = wcsstr(wzValuePrefix + lstrlenW(pi->sczValuePrefix), pi->sczValueSeparator);
            }
            else
            {
                wzValueSeparator = wcsstr(pi->rgsczLines[i], pi->sczValueSeparator);
            }
        }

        // Don't keep the '\r' before every '\n'
        if (pi->rgsczLines[i][lstrlenW(pi->rgsczLines[i])-1] == L'\r')
        {
            pi->rgsczLines[i][lstrlenW(pi->rgsczLines[i])-1] = L'\0';
        }

        if (fSections && wzOpenTagPrefix && wzOpenTagPostfix && wzOpenTagPrefix < wzOpenTagPostfix && (NULL == wzCommentLinePrefix || wzOpenTagPrefix < wzCommentLinePrefix))
        {
            // There is an section starting here, let's keep track of it and move on
            hr = StrAllocString(&sczCurrentSection, wzOpenTagPrefix + lstrlenW(pi->sczOpenTagPrefix), wzOpenTagPostfix - (wzOpenTagPrefix + lstrlenW(pi->sczOpenTagPrefix)));
            ExitOnFailure2(hr, "Failed to record section name for line: %ls of INI file: %ls", pi->rgsczLines[i], pi->sczPath);

            // Sections will be calculated dynamically after any set operations, so don't include this in the list of lines to remember for output
            ReleaseNullStr(pi->rgsczLines[i]);
        }
        else if (wzValueSeparator && (NULL == wzCommentLinePrefix || wzValueSeparator < wzCommentLinePrefix)
            && (!fValuePrefix || wzValuePrefix))
        {
            if (fValuePrefix)
            {
                wzValueBegin = wzValuePrefix + lstrlenW(pi->sczValuePrefix);
            }
            else
            {
                wzValueBegin = pi->rgsczLines[i];
            }

            hr = MemEnsureArraySize(reinterpret_cast<void **>(&pi->rgivValues), pi->cValues + 1, sizeof(INI_VALUE), 100);
            ExitOnFailure(hr, "Failed to increase array size for value array");

            if (sczCurrentSection)
            {
                hr = StrAllocString(&sczName, sczCurrentSection, 0);
                ExitOnFailure(hr, "Failed to copy current section name");

                hr = StrAllocConcat(&sczName, wzSectionSeparator, 0);
                ExitOnFailure(hr, "Failed to copy current section name");
            }

            hr = StrAllocConcat(&sczName, wzValueBegin, wzValueSeparator - wzValueBegin);
            ExitOnFailure(hr, "Failed to copy name");

            hr = StrAllocString(&sczValue, wzValueSeparator + lstrlenW(pi->sczValueSeparator), 0);
            ExitOnFailure(hr, "Failed to copy value");

            hr = StrTrimWhitespace(&sczNameTrimmed, sczName);
            ExitOnFailure(hr, "Failed to trim whitespace from name");

            hr = StrTrimWhitespace(&sczValueTrimmed, sczValue);
            ExitOnFailure(hr, "Failed to trim whitespace from value");

            pi->rgivValues[pi->cValues].wzName = const_cast<LPCWSTR>(sczNameTrimmed);
            sczNameTrimmed = NULL;
            pi->rgivValues[pi->cValues].wzValue = const_cast<LPCWSTR>(sczValueTrimmed);
            sczValueTrimmed = NULL;
            pi->rgivValues[pi->cValues].dwLineNumber = i + 1;

            ++pi->cValues;

            // Values will be calculated dynamically after any set operations, so don't include this in the list of lines to remember for output
            ReleaseNullStr(pi->rgsczLines[i]);
        }
        else
        {
            // Must be a comment, so ignore it and keep it in the list to output
        }

        ReleaseNullStr(sczName);
    }

LExit:
    ReleaseStr(sczCurrentSection);
    ReleaseStr(sczContents);
    ReleaseStr(sczName);
    ReleaseStr(sczNameTrimmed);
    ReleaseStr(sczValue);
    ReleaseStr(sczValueTrimmed);

    return hr;
}

extern "C" HRESULT DAPI IniGetValueList(
    __in_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __deref_out_ecount_opt(pcValues) INI_VALUE** prgivValues,
    __out DWORD *pcValues
    )
{
    HRESULT hr = S_OK;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    *prgivValues = pi->rgivValues;
    *pcValues = pi->cValues;

    return hr;
}

extern "C" HRESULT DAPI IniGetValue(
    __in_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in LPCWSTR wzValueName,
    __deref_out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);
    INI_VALUE *pValue = NULL;

    for (DWORD i = 0; i < pi->cValues; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pi->rgivValues[i].wzName, -1, wzValueName, -1))
        {
            pValue = pi->rgivValues + i;
            break;
        }
    }

    if (NULL == pValue)
    {
        hr = E_NOTFOUND;
        ExitOnFailure1(hr, "Failed to check for INI value: %ls", wzValueName);
    }

    if (NULL == pValue->wzValue)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = StrAllocString(psczValue, pValue->wzValue, 0);
    ExitOnFailure1(hr, "Failed to make copy of value while looking up INI value named: %ls", wzValueName);

LExit:
    return hr;
}

extern "C" HRESULT DAPI IniSetValue(
    __in_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in LPCWSTR wzValueName,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSectionPrefix = NULL; // includes section name and backslash
    LPWSTR sczName = NULL;
    LPWSTR sczValue = NULL;
    DWORD dwInsertIndex = DWORD_MAX;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);
    INI_VALUE *pValue = NULL;

    for (DWORD i = 0; i < pi->cValues; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pi->rgivValues[i].wzName, -1, wzValueName, -1))
        {
            pValue = pi->rgivValues + i;
            break;
        }
    }

    // We're killing the value
    if (NULL == wzValue)
    {
        if (pValue && pValue->wzValue)
        {
            pi->fModified = TRUE;
            sczValue = const_cast<LPWSTR>(pValue->wzValue);
            pValue->wzValue = NULL;
            ReleaseNullStr(sczValue);
        }

        ExitFunction();
    }
    else
    {
        if (pValue)
        {
            if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, pValue->wzValue, -1, wzValue, -1))
            {
                pi->fModified = TRUE;
                hr = StrAllocString(const_cast<LPWSTR *>(&pValue->wzValue), wzValue, 0);
                ExitOnFailure1(hr, "Failed to update value INI value named: %ls", wzValueName);
            }

            ExitFunction1(hr = S_OK);
        }
        else
        {
            if (wzValueName)
            {
                hr = GetSectionPrefixFromName(wzValueName, &sczSectionPrefix);
                ExitOnFailure1(hr, "Failed to get section prefix from value name: %ls", wzValueName);
            }

            // If we have a section prefix, figure out the index to insert it (at the end of the section it belongs in)
            if (sczSectionPrefix)
            {
                for (DWORD i = 0; i < pi->cValues; ++i)
                {
                    if (0 == wcsncmp(pi->rgivValues[i].wzName, sczSectionPrefix, lstrlenW(sczSectionPrefix)))
                    {
                        dwInsertIndex = i;
                    }
                    else if (DWORD_MAX != dwInsertIndex)
                    {
                        break;
                    }
                }
            }
            else
            {
                for (DWORD i = 0; i < pi->cValues; ++i)
                {
                    if (NULL == wcsstr(pi->rgivValues[i].wzName, wzSectionSeparator))
                    {
                        dwInsertIndex = i;
                    }
                    else if (DWORD_MAX != dwInsertIndex)
                    {
                        break;
                    }
                }
            }

            // Otherwise, just add it to the end
            if (DWORD_MAX == dwInsertIndex)
            {
                dwInsertIndex = pi->cValues;
            }

            pi->fModified = TRUE;
            hr = MemInsertIntoArray(reinterpret_cast<void **>(&pi->rgivValues), dwInsertIndex, 1, pi->cValues + 1, sizeof(INI_VALUE), 100);
            ExitOnFailure(hr, "Failed to insert value into array");

            hr = StrAllocString(&sczName, wzValueName, 0);
            ExitOnFailure(hr, "Failed to copy name");

            hr = StrAllocString(&sczValue, wzValue, 0);
            ExitOnFailure(hr, "Failed to copy value");

            pi->rgivValues[dwInsertIndex].wzName = const_cast<LPCWSTR>(sczName);
            sczName = NULL;
            pi->rgivValues[dwInsertIndex].wzValue = const_cast<LPCWSTR>(sczValue);
            sczValue = NULL;

            ++pi->cValues;
        }
    }

LExit:
    ReleaseStr(sczName);
    ReleaseStr(sczValue);

    return hr;
}

extern "C" HRESULT DAPI IniWriteFile(
    __in_bcount(INI_HANDLE_BYTES) INI_HANDLE piHandle,
    __in_z_opt LPCWSTR wzPath,
    __in FILE_ENCODING feOverrideEncoding
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCurrentSectionPrefix = NULL;
    LPWSTR sczNewSectionPrefix = NULL;
    LPWSTR sczContents = NULL;
    LPCWSTR wzName = NULL;
    DWORD dwLineArrayIndex = 1;
    FILE_ENCODING feEncoding;

    INI_STRUCT *pi = static_cast<INI_STRUCT *>(piHandle);

    if (FILE_ENCODING_UNSPECIFIED == feOverrideEncoding)
    {
        feEncoding = pi->feEncoding;
    }
    else
    {
        feEncoding = feOverrideEncoding;
    }

    if (FILE_ENCODING_UNSPECIFIED == feEncoding)
    {
        feEncoding = FILE_ENCODING_UTF16_WITH_BOM;
    }

    if (!pi->fModified)
    {
        ExitFunction1(hr = S_OK);
    }
    if (NULL == wzPath && NULL == pi->sczPath)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    BOOL fSections = (pi->sczOpenTagPrefix) && (pi->sczOpenTagPostfix);

    // Insert any beginning lines we didn't understand like comments
    if (0 < pi->cLines)
    {
        while (pi->rgsczLines[dwLineArrayIndex])
        {
            hr = StrAllocConcat(&sczContents, pi->rgsczLines[dwLineArrayIndex], 0);
            ExitOnFailure(hr, "Failed to add previous line to ini output buffer in-memory");

            hr = StrAllocConcat(&sczContents, L"\r\n", 2);
            ExitOnFailure(hr, "Failed to add endline to ini output buffer in-memory");

            ++dwLineArrayIndex;
        }
    }

    for (DWORD i = 0; i < pi->cValues; ++i)
    {
        // Skip if this value was killed off
        if (NULL == pi->rgivValues[i].wzValue)
        {
            continue;
        }

        // Now generate any lines for the current value like value line and maybe also a new section line before it

        // First see if we need to write a section line
        hr = GetSectionPrefixFromName(pi->rgivValues[i].wzName, &sczNewSectionPrefix);
        ExitOnFailure1(hr, "Failed to get section prefix from name: %ls", pi->rgivValues[i].wzName);

        // If the new section prefix is different, write a section out for it
        if (fSections && sczNewSectionPrefix && (NULL == sczCurrentSectionPrefix || CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, sczNewSectionPrefix, -1, sczCurrentSectionPrefix, -1)))
        {
            hr = StrAllocConcat(&sczContents, pi->sczOpenTagPrefix, 0);
            ExitOnFailure(hr, "Failed to concat open tag prefix to string");

            // Exclude section separator (i.e. backslash) from new section prefix
            hr = StrAllocConcat(&sczContents, sczNewSectionPrefix, lstrlenW(sczNewSectionPrefix)-lstrlenW(wzSectionSeparator));
            ExitOnFailure(hr, "Failed to concat section name to string");

            hr = StrAllocConcat(&sczContents, pi->sczOpenTagPostfix, 0);
            ExitOnFailure(hr, "Failed to concat open tag postfix to string");

            hr = StrAllocConcat(&sczContents, L"\r\n", 2);
            ExitOnFailure(hr, "Failed to add endline to ini output buffer in-memory");
            
            ReleaseNullStr(sczCurrentSectionPrefix);
            sczCurrentSectionPrefix = sczNewSectionPrefix;
            sczNewSectionPrefix = NULL;
        }

        // Inserting lines we read before the current value if appropriate
        while (pi->rgivValues[i].dwLineNumber > dwLineArrayIndex)
        {
            // Skip any lines were purposely forgot
            if (NULL == pi->rgsczLines[dwLineArrayIndex])
            {
                ++dwLineArrayIndex;
                continue;
            }

            hr = StrAllocConcat(&sczContents, pi->rgsczLines[dwLineArrayIndex++], 0);
            ExitOnFailure(hr, "Failed to add previous line to ini output buffer in-memory");

            hr = StrAllocConcat(&sczContents, L"\r\n", 2);
            ExitOnFailure(hr, "Failed to add endline to ini output buffer in-memory");
        }

        wzName = pi->rgivValues[i].wzName;
        if (fSections)
        {
            wzName += lstrlenW(sczCurrentSectionPrefix);
        }

        // OK, now just write the name/value pair, if it isn't deleted
        if (pi->sczValuePrefix)
        {
            hr = StrAllocConcat(&sczContents, pi->sczValuePrefix, 0);
            ExitOnFailure(hr, "Failed to concat value prefix to ini output buffer");
        }

        hr = StrAllocConcat(&sczContents, wzName, 0);
        ExitOnFailure(hr, "Failed to concat value name to ini output buffer");

        hr = StrAllocConcat(&sczContents, pi->sczValueSeparator, 0);
        ExitOnFailure(hr, "Failed to concat value separator to ini output buffer");

        hr = StrAllocConcat(&sczContents, pi->rgivValues[i].wzValue, 0);
        ExitOnFailure(hr, "Failed to concat value to ini output buffer");

        hr = StrAllocConcat(&sczContents, L"\r\n", 2);
        ExitOnFailure(hr, "Failed to add endline to ini output buffer in-memory");
    }

    // If no path was specified, use the path to the file we parsed
    if (NULL == wzPath)
    {
        wzPath = pi->sczPath;
    }

    hr = FileFromString(wzPath, 0, sczContents, feEncoding);
    ExitOnFailure1(hr, "Failed to write INI contents out to file: %ls", wzPath);

LExit:
    ReleaseStr(sczContents);
    ReleaseStr(sczCurrentSectionPrefix);
    ReleaseStr(sczNewSectionPrefix);

    return hr;
}

static void UninitializeIniValue(
    INI_VALUE *pivValue
    )
{
    ReleaseStr(const_cast<LPWSTR>(pivValue->wzName));
    ReleaseStr(const_cast<LPWSTR>(pivValue->wzValue));
}

static HRESULT GetSectionPrefixFromName(
    __in_z LPCWSTR wzName,
    __deref_out_z LPWSTR* psczOutput
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSectionDelimiter = NULL;

    ReleaseNullStr(*psczOutput);

    wzSectionDelimiter = wcsstr(wzName, wzSectionSeparator);
    if (wzSectionDelimiter && wzSectionDelimiter != wzName)
    {
        hr = StrAllocString(psczOutput, wzName, wzSectionDelimiter - wzName + 1);
        ExitOnFailure(hr, "Failed to copy section prefix");
    }

LExit:
    return hr;
}
