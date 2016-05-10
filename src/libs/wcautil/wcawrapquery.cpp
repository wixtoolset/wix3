// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "wcawrapquery.h"

static const LPWSTR ISINSTALLEDCOLUMNNAME = L"ISInstalled";
static const LPWSTR ISACTIONCOLUMNNAME = L"ISAction";
static const LPWSTR SOURCEPATHCOLUMNNAME = L"SourcePath";
static const LPWSTR TARGETPATHCOLUMNNAME = L"TargetPath";

// This instantiates a new query object in the deferred CA, and returns the handle to the query
WCA_WRAPQUERY_HANDLE WIXAPI GetNewQueryInstance(
    DWORD dwInColumns,
    DWORD dwInRows
    )
{
    HRESULT hr = S_OK;

    WCA_WRAPQUERY_HANDLE hNewHandle = NULL;

    hNewHandle = static_cast<WCA_WRAPQUERY_HANDLE>(MemAlloc(sizeof(WCA_WRAPQUERY_STRUCT), TRUE));
    if (NULL == hNewHandle)
    {
        hr = E_OUTOFMEMORY;
        ExitOnFailure(hr, "Failed to allocate Query Instance");
    }

    // Initialize non-array members
    hNewHandle->dwColumns = dwInColumns;
    hNewHandle->dwRows = dwInRows;
    hNewHandle->dwNextIndex = 0;

    // Initialize arrays
    if (0 != hNewHandle->dwColumns)
    {
        hNewHandle->pcdtColumnType = static_cast<eColumnDataType *>(MemAlloc(hNewHandle->dwColumns * sizeof(eColumnDataType), TRUE));
        if (NULL == hNewHandle->pcdtColumnType)
        {
            hr = E_OUTOFMEMORY;
            ExitOnFailure(hr, "Failed to allocate column type array");
        }

        hNewHandle->ppwzColumnNames = static_cast<LPWSTR *>(MemAlloc(hNewHandle->dwColumns * sizeof(LPWSTR), TRUE));
        if (NULL == hNewHandle->ppwzColumnNames)
        {
            hr = E_OUTOFMEMORY;
            ExitOnFailure(hr, "Failed to allocate column names array");
        }
    }

    for (DWORD i=0;i<hNewHandle->dwColumns;i++)
    {
        hNewHandle->pcdtColumnType[i] = cdtUnknown;
        hNewHandle->ppwzColumnNames[i] = NULL;
    }

    if (0 != hNewHandle->dwRows)
    {
        hNewHandle->phRecords = static_cast<MSIHANDLE *>(MemAlloc(hNewHandle->dwRows * sizeof(MSIHANDLE), TRUE));
        if (NULL == hNewHandle->phRecords)
        {
            hr = E_OUTOFMEMORY;
            ExitOnFailure(hr, "Failed to allocate records array");
        }
    }

    for (DWORD i=0;i<hNewHandle->dwRows;i++)
    {
        hNewHandle->phRecords[i] = NULL;
    }

    return hNewHandle;

LExit:
    // The handle isn't complete, so destroy any memory it allocated before returning NULL
    if (NULL != hNewHandle)
    {
        WcaFinishUnwrapQuery(hNewHandle);
    }

    return NULL;
}

// This function takes in the column type string from MsiViewGetColumnInfo, and returns
// whether the column stores strings, ints, binary streams, or
// cdtUnknown if this information couldn't be determined.
eColumnDataType WIXAPI GetDataTypeFromString(
    LPCWSTR pwzTypeString
    )
{
    if (NULL == pwzTypeString || 0 == wcslen(pwzTypeString))
    {
        return cdtUnknown;
    }

    switch (pwzTypeString[0])
    {
    case 'v':
    case 'V':
    case 'o':
    case 'O':
        return cdtStream;

    case 'g':
    case 'G':
    case 's':
    case 'S':
    case 'l':
    case 'L':
        return cdtString;

    case 'i':
    case 'I':
    case 'j':
    case 'J':
        return cdtInt;

    default:
        return cdtUnknown;
    }
}

HRESULT WIXAPI WcaWrapEmptyQuery(
    __inout LPWSTR * ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;

    WcaLog(LOGMSG_TRACEONLY, "Wrapping result of empty query");

    hr = WcaWriteIntegerToCaData(static_cast<int>(wqaTableBegin), ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write table begin marker to custom action data");

    hr = WcaWriteIntegerToCaData(0, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write number of columns to custom action data");

    hr = WcaWriteIntegerToCaData(0, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write number of rows to custom action data");

    hr = WcaWriteIntegerToCaData(static_cast<int>(wqaTableFinish), ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write table finish marker to custom action data");

//  WcaLog(LOGMSG_TRACEONLY, "Finished wrapping result of empty query");

LExit:
    return hr;
}

/********************************************************************
WcaWrapQuery() - wraps a view and transmits it through the
                CustomActionData property

********************************************************************/
HRESULT WIXAPI WcaWrapQuery(
    __in_z LPCWSTR pwzQuery,
    __inout LPWSTR * ppwzCustomActionData,
    __in_opt DWORD dwFormatMask,
    __in_opt DWORD dwComponentColumn,
    __in_opt DWORD dwDirectoryColumn
    )
{
    HRESULT hr = S_OK;
    HRESULT hrTemp = S_OK;
    UINT er = ERROR_SUCCESS;
    UINT cViewColumns;
    eColumnDataType *pcdtColumnTypeList = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzColumnData = NULL;
    LPWSTR pwzRecordData = NULL;
    BYTE* pbData = NULL;
    DWORD dwNumRecords = 0;
    BOOL fAddComponentState = FALSE; // Add two integer columns to the right side of the query - ISInstalled, and ISAction
    BOOL fAddDirectoryPath = FALSE; // Add two string columns to the right side of the query - SourcePath, and TargetPath
    int iTempInteger = 0;

    WCHAR wzPath[MAX_PATH + 1];
    DWORD dwLen;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

    PMSIHANDLE hColumnTypes, hColumnNames;
    PMSIHANDLE hView, hRec;

    WcaLog(LOGMSG_TRACEONLY, "Wrapping result of query: \"%ls\"", pwzQuery);

    // open the view
    hr = WcaOpenExecuteView(pwzQuery, &hView);
    ExitOnFailure(hr, "Failed to execute view");

    hr = WcaWriteIntegerToCaData(static_cast<int>(wqaTableBegin), ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write table begin marker to custom action data");

//  WcaLog(LOGMSG_TRACEONLY, "Starting to wrap table's column information", pwzQuery);

    // Use GetColumnInfo to populate the names of the columns.
    er = ::MsiViewGetColumnInfo(hView, MSICOLINFO_TYPES, &hColumnTypes);
    ExitOnWin32Error(er, hr, "Failed to get column types");

    er = ::MsiViewGetColumnInfo(hView, MSICOLINFO_NAMES, &hColumnNames);
    ExitOnWin32Error(er, hr, "Failed to get column names");

    cViewColumns = ::MsiRecordGetFieldCount(hColumnTypes);

    if (0xFFFFFFFF == cViewColumns)
    {
        // According to MSDN, this return value only happens when the handle is invalid
        hr = E_HANDLE;
        ExitOnFailure(hr, "Failed to get number of fields in record");
    }

    if (cViewColumns >= dwComponentColumn)
    {
        fAddComponentState = TRUE;
    }
    else if (0xFFFFFFFF != dwComponentColumn)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Component column %d out of range", dwComponentColumn);
    }

    if (cViewColumns >= dwDirectoryColumn)
    {
        fAddDirectoryPath = TRUE;
    }
    else if (0xFFFFFFFF != dwDirectoryColumn)
    {
        hr = E_INVALIDARG;
        ExitOnFailure1(hr, "Directory column %d out of range", dwDirectoryColumn);
    }

    hr = WcaWriteIntegerToCaData(static_cast<int>(cViewColumns) + 2 * static_cast<int>(fAddComponentState) + 2 * static_cast<int>(fAddDirectoryPath), ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write number of columns to custom action data");

    pcdtColumnTypeList = new eColumnDataType[cViewColumns];
    ExitOnNull(pcdtColumnTypeList, hr, E_OUTOFMEMORY, "Failed to allocate memory to store column info types");

    // Loop through all the columns reporting information about each one
    for (DWORD i = 0; i < cViewColumns; i++)
    {
        hr = WcaGetRecordString(hColumnNames, i+1, &pwzData);
        ExitOnFailure1(hr, "Failed to get the column %d name", i+1);

        hr = WcaWriteStringToCaData(pwzData, &pwzColumnData);
        ExitOnFailure2(hr, "Failed to write column %d name %ls to custom action data", i+1, pwzData);

        hr = WcaGetRecordString(hColumnTypes, i+1, &pwzData);
        ExitOnFailure1(hr, "Failed to get the column type string for column %d", i+1);

        pcdtColumnTypeList[i] = GetDataTypeFromString(pwzData);

        if (cdtUnknown == pcdtColumnTypeList[i])
        {
            hr = E_INVALIDARG;
            ExitOnFailure2(hr, "Failed to recognize column %d type string: %ls", i+1, pwzData);
        }

        hr = WcaWriteIntegerToCaData(pcdtColumnTypeList[i], &pwzColumnData);
        ExitOnFailure1(hr, "Failed to write column %d type enumeration to custom action data", i+1);
    }

    // Add two integer columns to the right side of the query - ISInstalled, and ISAction
    if (fAddComponentState)
    {
        hr = WcaWriteStringToCaData(ISINSTALLEDCOLUMNNAME, &pwzColumnData);
        ExitOnFailure2(hr, "Failed to write extra column %d name %ls to custom action data", cViewColumns + 1, ISINSTALLEDCOLUMNNAME);

        hr = WcaWriteIntegerToCaData(cdtInt, &pwzColumnData);
        ExitOnFailure1(hr, "Failed to write extra column %d type to custom action data", cViewColumns + 1);

        hr = WcaWriteStringToCaData(ISACTIONCOLUMNNAME, &pwzColumnData);
        ExitOnFailure2(hr, "Failed to write extra column %d name %ls to custom action data", cViewColumns + 1, ISACTIONCOLUMNNAME);

        hr = WcaWriteIntegerToCaData(cdtInt, &pwzColumnData);
        ExitOnFailure1(hr, "Failed to write extra column %d type to custom action data", cViewColumns + 1);
    }

    if (fAddDirectoryPath)
    {
        hr = WcaWriteStringToCaData(SOURCEPATHCOLUMNNAME, &pwzColumnData);
        ExitOnFailure2(hr, "Failed to write extra column %d name %ls to custom action data", cViewColumns + 1, SOURCEPATHCOLUMNNAME);

        hr = WcaWriteIntegerToCaData(cdtString, &pwzColumnData);
        ExitOnFailure1(hr, "Failed to write extra column %d type to custom action data", cViewColumns + 1);

        hr = WcaWriteStringToCaData(TARGETPATHCOLUMNNAME, &pwzColumnData);
        ExitOnFailure2(hr, "Failed to write extra column %d name %ls to custom action data", cViewColumns + 1, TARGETPATHCOLUMNNAME);

        hr = WcaWriteIntegerToCaData(cdtString, &pwzColumnData);
        ExitOnFailure1(hr, "Failed to write extra column %d type to custom action data", cViewColumns + 1);
    }

    // Begin wrapping actual table data
    //WcaLog(LOGMSG_TRACEONLY, "Starting to wrap table data", pwzQuery);
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaWriteIntegerToCaData(static_cast<int>(wqaRowBegin), &pwzRecordData);
        ExitOnFailure(hr, "Failed to write row begin marker to custom action data");

        for (DWORD i = 0; i < cViewColumns; i++)
        {
            switch (pcdtColumnTypeList[i])
            {
            case cdtString:
                // If we were given a format mask, we're not past the index, and it's set to true for this column, then format the string
                if (i < (sizeof(dwFormatMask) * 8) && (dwFormatMask & (1 << i)))
                {
                    hr = WcaGetRecordFormattedString(hRec, i + 1, &pwzData);
                }
                else
                {
                    hr = WcaGetRecordString(hRec, i + 1, &pwzData);
                }
                ExitOnFailure1(hr, "Failed to get string for column %d", i + 1);

                hr = WcaWriteStringToCaData(pwzData, &pwzRecordData);
                ExitOnFailure1(hr, "Failed to write string to temporary record custom action data for column %d", i + 1);
                break;

            case cdtInt:
                if (i < (sizeof(dwFormatMask) * 8) && (dwFormatMask & (1 << i)))
                {
                    hr = WcaGetRecordFormattedInteger(hRec, i + 1, &iTempInteger);
                }
                else
                {
                    hr = WcaGetRecordInteger(hRec, i + 1, &iTempInteger);
                }
                ExitOnFailure1(hr, "Failed to get integer for column %d", i + 1);

                hr = WcaWriteIntegerToCaData(iTempInteger, &pwzRecordData);
                ExitOnFailure1(hr, "Failed to write integer to temporary record custom action data for column %d", i + 1);
                break;

            case cdtStream:
                hr = E_NOTIMPL;
                ExitOnFailure1(hr, "A query was wrapped which contained a binary stream data field in column %d - however, the ability to wrap stream data fields is not implemented at this time", i);
                break;

            case cdtUnknown:
            default:
                hr = E_INVALIDARG;
                ExitOnFailure2(hr, "Failed to recognize column type enumeration %d for column %d", pcdtColumnTypeList[i], i + 1);
            }
        }

        // Add two integer columns to the right side of the query - ISInstalled, and ISAction
        if (fAddComponentState)
        {
            // Get the component ID
            hr = WcaGetRecordString(hRec, dwComponentColumn, &pwzData);
            ExitOnFailure1(hr, "Failed to get component from column %d while adding extra columns", dwComponentColumn);

            if (0 == lstrlenW(pwzData))
            {
                // If no component was provided, set these both to zero as though a structure to store them were allocated with memory zero'd out
                isInstalled = (INSTALLSTATE)0;
                isAction = (INSTALLSTATE)0;
            }
            else
            {
                er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
                // If we don't get the component state, that may be because the component ID was invalid, but isn't necessarily an error, so write NULL's
                if (FAILED(HRESULT_FROM_WIN32(er)))
                {
                    ExitOnFailure1(hr, "Failed to get component state for component %ls", pwzData);
                }
            }

            hr = WcaWriteIntegerToCaData(isInstalled, &pwzRecordData);
            ExitOnFailure(hr, "Failed to write extra ISInstalled column to custom action data");

            hr = WcaWriteIntegerToCaData(isAction, &pwzRecordData);
            ExitOnFailure(hr, "Failed to write extra ISAction column to custom action data");
        }

        // Add two string columns to the right side of the query - SourcePath, and TargetPath
        if (fAddDirectoryPath)
        {
            hr = WcaGetRecordString(hRec, dwDirectoryColumn, &pwzData);
            // If this fails, ignore it, and just leave those columns blank
            if (SUCCEEDED(hr))
            {
                // Only get source path if the component state is INSTALLSTATE_SOURCE, or if we have no component to check the installstate of
                if (INSTALLSTATE_SOURCE == isAction || !fAddComponentState)
                {
                    dwLen = countof(wzPath);
                    er = ::MsiGetSourcePathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
                    hrTemp = HRESULT_FROM_WIN32(er);
                    if (dwLen > countof(wzPath))
                    {
                        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                        ExitOnRootFailure1(hr, "Failed to record entire Source Path for Directory %ls because its length was greater than MAX_PATH.", pwzData);
                    }

                    if (SUCCEEDED(hrTemp))
                    {
                        hr = WcaWriteStringToCaData(wzPath, &pwzRecordData);
                        ExitOnFailure(hr, "Failed to write source path string to record data string");
                    }
                    else
                    {
                        hr = WcaWriteStringToCaData(L"", &pwzRecordData);
                        ExitOnFailure(hr, "Failed to write empty source path string to record data string");
                    }
                }
                else
                {
                    hr = WcaWriteStringToCaData(L"", &pwzRecordData);
                    ExitOnFailure(hr, "Failed to write empty source path string before writing target path string to record data string");
                }

                dwLen = countof(wzPath);
                er = ::MsiGetTargetPathW(WcaGetInstallHandle(), pwzData, wzPath, &dwLen);
                hrTemp = HRESULT_FROM_WIN32(er);
                if (dwLen > countof(wzPath))
                {
                    hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                    ExitOnRootFailure1(hr, "Failed to record entire Source Path for Directory %ls because its length was greater than MAX_PATH.", pwzData);
                }
                if (SUCCEEDED(hrTemp))
                {
                    hr = WcaWriteStringToCaData(wzPath, &pwzRecordData);
                    ExitOnFailure(hr, "Failed to write target path string to record data string");
                }
                else
                {
                    hr = WcaWriteStringToCaData(L"", &pwzRecordData);
                    ExitOnFailure(hr, "Failed to write empty target path string to record data string");
                }
            }
            else
            {
                // Write both fields as blank
                hr = WcaWriteStringToCaData(L"", &pwzRecordData);
                hr = WcaWriteStringToCaData(L"", &pwzRecordData);
            }
        }

        hr = WcaWriteIntegerToCaData(static_cast<int>(wqaRowFinish), &pwzRecordData);
        ExitOnFailure(hr, "Failed to write row finish marker to custom action data");

        ++dwNumRecords;
    }

    hr = WcaWriteIntegerToCaData(dwNumRecords, ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write number of records to custom action data");

    if (NULL != pwzColumnData)
    {
        hr = WcaWriteStringToCaData(pwzColumnData, ppwzCustomActionData);
        ExitOnFailure(hr, "Failed to write column data to custom action data");
    }

    if (NULL != pwzRecordData)
    {
        hr = WcaWriteStringToCaData(pwzRecordData, ppwzCustomActionData);
        ExitOnFailure(hr, "Failed to write record data to custom action data");
    }

    hr = WcaWriteIntegerToCaData(static_cast<int>(wqaTableFinish), ppwzCustomActionData);
    ExitOnFailure(hr, "Failed to write table finish marker to custom action data");

//  WcaLog(LOGMSG_TRACEONLY, "Finished wrapping result of query: \"%ls\"", pwzQuery);

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzColumnData);
    ReleaseStr(pwzRecordData);

    ReleaseMem(pbData);

    return hr;
}


/********************************************************************
WcaBeginUnwrapQuery() - unwraps a view for direct access from the
                        CustomActionData property

********************************************************************/
HRESULT WIXAPI WcaBeginUnwrapQuery(
    __out WCA_WRAPQUERY_HANDLE * phWrapQuery,
    __inout LPWSTR * ppwzCustomActionData
    )
{
    HRESULT hr = S_OK;
    int iTempInteger = 0;
    int iColumns = 0;
    int iRows = 0;
    BYTE* pbData = NULL;
    LPWSTR pwzData = NULL;
    WCA_WRAPQUERY_HANDLE hWrapQuery = NULL;

    WcaLog(LOGMSG_TRACEONLY, "Unwrapping a query from custom action data");

    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
    if (wqaTableBegin != iTempInteger)
    {
        hr = E_INVALIDARG;
    }
    ExitOnFailure1(hr, "Failed to read table begin marker from custom action data (read %d instead)", iTempInteger);

    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iColumns);
    ExitOnFailure(hr, "Failed to read number of columns from custom action data");

    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iRows);
    ExitOnFailure(hr, "Failed to read number of rows from custom action data");

    hWrapQuery = GetNewQueryInstance(iColumns, iRows);
    if (NULL == hWrapQuery)
    {
        hr = E_POINTER;
    }
    ExitOnFailure2(hr, "Failed to get a query instance with %d columns and %d rows", iColumns, iRows);

    for (int i = 0; i < iColumns; i++)
    {
        hr = WcaReadStringFromCaData(ppwzCustomActionData, &(hWrapQuery->ppwzColumnNames[i]));
        ExitOnFailure1(hr, "Failed to read column %d's name from custom action data", i+1);

        hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
        if (cdtString != iTempInteger && cdtInt != iTempInteger && cdtStream != iTempInteger)
        {
            hr = E_INVALIDARG;
        }
        ExitOnFailure1(hr, "Failed to read column %d's type from custom action data", i+1);

        // Set the column type into the actual data structure
        hWrapQuery->pcdtColumnType[i] = (eColumnDataType)iTempInteger;
    }

    for (int i = 0; i < iRows; i++)
    {
        hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
        if (wqaRowBegin != iTempInteger)
        {
            hr = E_INVALIDARG;
        }
        ExitOnFailure1(hr, "Failed to read begin row marker from custom action data (read %d instead)", iTempInteger);

        hWrapQuery->phRecords[i] = ::MsiCreateRecord((unsigned int)iColumns);

        for (int j = 0; j < iColumns; j++)
        {
            switch (hWrapQuery->pcdtColumnType[j])
            {
            case cdtString:
                hr = WcaReadStringFromCaData(ppwzCustomActionData, &pwzData);
                ExitOnFailure(hr, "Failed to read string from custom action data");

                hr = WcaSetRecordString(hWrapQuery->phRecords[i], j+1, pwzData);
                ExitOnFailure2(hr, "Failed to write string %ls to record in column %d", pwzData, j+1);
                break;

            case cdtInt:
                WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
                ExitOnFailure(hr, "Failed to read integer from custom action data");

                WcaSetRecordInteger(hWrapQuery->phRecords[i], j+1, iTempInteger);
                ExitOnFailure2(hr, "Failed to write integer %d to record in column %d", iTempInteger, j+1);
                break;

            case cdtStream:
                hr = E_NOTIMPL;
                ExitOnFailure(hr, "A query was wrapped which contained a stream data field - however, the ability to wrap stream data fields is not implemented at this time");
                break;

            case cdtUnknown:
            default:
                hr = E_INVALIDARG;
                ExitOnFailure2(hr, "Failed to recognize column type enumeration %d for column %d", hWrapQuery->pcdtColumnType[j+1], i+1);
            }
        }

        hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
        if (wqaRowFinish != iTempInteger)
        {
            hr = E_INVALIDARG;
        }
        ExitOnFailure1(hr, "Failed to read row finish marker from custom action data (read %d instead)", iTempInteger);
    }

    hr = WcaReadIntegerFromCaData(ppwzCustomActionData, &iTempInteger);
    if (wqaTableFinish != iTempInteger)
    {
        hr = E_INVALIDARG;
    }
    ExitOnFailure1(hr, "Failed to read table finish marker from custom action data (read %d instead)", iTempInteger);

    *phWrapQuery = hWrapQuery;

//  WcaLog(LOGMSG_TRACEONLY, "Successfully finished unwrapping a query from custom action data");

LExit:
    ReleaseStr(pwzData);
    ReleaseMem(pbData);

    return hr;
}

// This function returns the total number of records in a query
DWORD WIXAPI WcaGetQueryRecords(
    __in const WCA_WRAPQUERY_HANDLE hWrapQuery
    )
{
    return hWrapQuery->dwRows;
}

// This function resets a query back to its first row, so that the next fetch returns the first record
void WIXAPI WcaFetchWrappedReset(
    __in WCA_WRAPQUERY_HANDLE hWrapQuery
    )
{
    hWrapQuery->dwNextIndex = 0;
}

// Fetches the next record in the query
// NOTE: the MSIHANDLE returned by this function should not be released, as it is the same handle used by the query object to maintain the item.
//       so, don't use this function with PMSIHANDLE objects!
HRESULT WIXAPI WcaFetchWrappedRecord(
    __in WCA_WRAPQUERY_HANDLE hWrapQuery,
    __out MSIHANDLE* phRec
    )
{
    DWORD dwNextIndex = hWrapQuery->dwNextIndex;

    if (dwNextIndex >= hWrapQuery->dwRows)
    {
        return E_NOMOREITEMS;
    }

    if (NULL == hWrapQuery->phRecords[dwNextIndex])
    {
        return E_HANDLE;
    }

    *phRec = hWrapQuery->phRecords[hWrapQuery->dwNextIndex];

    // Increment our next index variable
    ++hWrapQuery->dwNextIndex;

    return S_OK;
}

// Fetch the next record in the query where the string value in column dwComparisonColumn equals the value pwzExpectedValue
// NOTE: the MSIHANDLE returned by this function should not be released, as it is the same handle used by the query object to maintain the item.
//       so, don't use this function with PMSIHANDLE objects!
HRESULT WIXAPI WcaFetchWrappedRecordWhereString(
    __in WCA_WRAPQUERY_HANDLE hWrapQuery,
    __in DWORD dwComparisonColumn,
    __in_z LPCWSTR pwzExpectedValue,
    __out MSIHANDLE* phRec
    )
{
    HRESULT hr = S_OK;
    MSIHANDLE hRec = NULL;
    LPWSTR pwzData = NULL;

    while (S_OK == (hr = WcaFetchWrappedRecord(hWrapQuery, &hRec)))
    {
        ExitOnFailure(hr, "Failed to fetch a wrapped record");

        hr = WcaGetRecordString(hRec, dwComparisonColumn, &pwzData);
        ExitOnFailure1(hr, "Failed to get record string in column %d", dwComparisonColumn);

        if (0 == lstrcmpW(pwzData, pwzExpectedValue))
        {
            *phRec = hRec;
            ExitFunction1(hr = S_OK);
        }
    }
    // If we errored here but not because there were no records left, write an error to the log
    if (hr != E_NOMOREITEMS)
    {
        ExitOnFailure2(hr, "Failed while searching for a wrapped record where column %d is set to %ls", dwComparisonColumn, pwzExpectedValue);
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

/********************************************************************
WcaBeginUnwrapQuery() - Finishes unwrapping a view for direct access
                        from the CustomActionData property

********************************************************************/
void WIXAPI WcaFinishUnwrapQuery(
    __in_opt WCA_WRAPQUERY_HANDLE hWrapQuery
    )
{
    if (NULL == hWrapQuery)
    {
        WcaLog(LOGMSG_TRACEONLY, "Failed to finish an unwrap query - ignoring");
        return;
    }

    ReleaseMem(hWrapQuery->pcdtColumnType);

    for (DWORD i=0;i<hWrapQuery->dwColumns;i++)
    {
        ReleaseStr(hWrapQuery->ppwzColumnNames[i]);
    }
    ReleaseMem(hWrapQuery->ppwzColumnNames);

    for (DWORD i=0;i<hWrapQuery->dwRows;i++)
    {
        if (NULL != hWrapQuery->phRecords[i])
        {
            ::MsiCloseHandle(hWrapQuery->phRecords[i]);
        }
    }
    ReleaseMem(hWrapQuery->phRecords);

    ReleaseMem(hWrapQuery);
}
