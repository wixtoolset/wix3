//-------------------------------------------------------------------------------------------------
// <copyright file="scaweblog.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Custom Actions for handling log settings for a particular IIS Website
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

enum eWebLogQuery { wlqLog = 1, wlqFormat };

/* ****************************************************************
 * LookupLogFormatGUID -Looks up a given IIS Log format type in 
 * the metabase and returns the GUID for it.
 * ****************************************************************/
static HRESULT LookupLogFormatGUID(
    __in IMSAdminBase* piMetabase,
    __in LPCWSTR wzLogFormat,
    __out_ecount(cchGUID) LPWSTR wzGUID,
    __in int cchGUID
    )
{
    WCHAR wzKey[METADATA_MAX_NAME_LEN];
    HRESULT hr = S_OK;

    METADATA_RECORD mrKeyType;
    ::ZeroMemory(&mrKeyType, sizeof(mrKeyType));

    mrKeyType.dwMDIdentifier = MD_KEY_TYPE;
    mrKeyType.dwMDAttributes = METADATA_NO_ATTRIBUTES;
    mrKeyType.dwMDUserType = IIS_MD_UT_SERVER;
    mrKeyType.dwMDDataType = ALL_METADATA;
    mrKeyType.dwMDDataLen = 0;
    mrKeyType.pbMDData = NULL;

    METADATA_RECORD mrPluginId;
    ::ZeroMemory(&mrPluginId, sizeof(mrPluginId));

    mrPluginId.dwMDIdentifier = MD_LOG_PLUGIN_MOD_ID;
    mrPluginId.dwMDAttributes = METADATA_INHERIT;
    mrPluginId.dwMDUserType = IIS_MD_UT_SERVER;
    mrPluginId.dwMDDataType = ALL_METADATA;
    mrPluginId.dwMDDataLen = 0;
    mrPluginId.pbMDData = NULL;
    
    hr = ::StringCchPrintfW(wzKey, countof(wzKey), L"/LM/Logging/%s", wzLogFormat);
    ExitOnFailure(hr, "failed to format logging metabase key name");

    // verify that we have this log format available in IIS
    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mrKeyType);
    ExitOnFailure1(hr, "Failed to find specified Log format key in IIS for log format: %ls", wzLogFormat);
    ExitOnNull(mrKeyType.pbMDData, hr, E_POINTER, "Request for Log format key in IIS for log format returned success, but value was NULL");

    if (0 != lstrcmpW(L"IIsLogModule", (LPCWSTR)mrKeyType.pbMDData))
    {
        ExitOnFailure1(hr = E_UNEXPECTED, "Found invalid log format in IIS: %ls", (LPCWSTR)mrKeyType.pbMDData);
    }

    // find the GUID for that log format
    hr = MetaGetValue(piMetabase, METADATA_MASTER_ROOT_HANDLE, wzKey, &mrPluginId);
    ExitOnFailure1(hr, "Failed to retrieve IISLog format GUID. Key: %ls", wzKey);
    ExitOnNull(mrPluginId.pbMDData, hr, E_POINTER, "Retrieval of IISLog format GUID returned success, but value was NULL");

    hr = ::StringCchCopyW(wzGUID, cchGUID, (LPCWSTR)mrPluginId.pbMDData);
    ExitOnFailure1(hr, "failed to copy metabase value: %ls", (LPCWSTR)mrPluginId.pbMDData);

LExit:

    if (mrKeyType.pbMDData)
    {
        MetaFreeValue(&mrKeyType);
    }

    if (mrPluginId.pbMDData)
    {
        MetaFreeValue(&mrPluginId);
    }

    return hr;
}


/* ****************************************************************
 * ScaGetWebLog -Retrieves Log table data for the specified Log key
 *
 * ****************************************************************/
HRESULT ScaGetWebLog(
    IMSAdminBase* piMetabase,
    LPCWSTR wzLog,
    __in WCA_WRAPQUERY_HANDLE hWebLogQuery,
    SCA_WEB_LOG* pswl
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzData = NULL;
    MSIHANDLE hRec;

    if (0 == WcaGetQueryRecords(hWebLogQuery))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaGetWebLog() - no records to process");
        ExitFunction1(hr = S_FALSE);
    }

    WcaFetchWrappedReset(hWebLogQuery);

    hr = WcaFetchWrappedRecordWhereString(hWebLogQuery, wlqLog, wzLog, &hRec);
    if (E_NOMOREITEMS == hr)
    {
        ExitOnFailure1(hr, "cannot locate IIsWebLog.Log='%ls'", wzLog);
    }
    HRESULT hrTemp = WcaFetchWrappedRecordWhereString(hWebLogQuery, wlqLog, wzLog, &hRec);

    if (SUCCEEDED(hrTemp))
    {
        ExitOnFailure(hr, "error - found multiple matching IIsWebLog rows");
    }

    ::ZeroMemory(pswl, sizeof(SCA_WEB_LOG));

    // check that log key matches
    hr = WcaGetRecordString(hRec, wlqLog, &pwzData);
    ExitOnFailure1(hr, "failed to get IIsWebLog.Log for Log: %ls", wzLog);
    hr = ::StringCchCopyW(pswl->wzLog, countof(pswl->wzLog), pwzData);
    ExitOnFailure1(hr, "failed to copy log name: %ls", pwzData);

    hr = WcaGetRecordString(hRec, wlqFormat, &pwzData);
    ExitOnFailure1(hr, "failed to get IIsWebLog.Format for Log:", wzLog);
    hr = ::StringCchCopyW(pswl->wzFormat, countof(pswl->wzFormat), pwzData);
    ExitOnFailure1(hr, "failed to copy log format: %ls", pwzData);

    // if they specified a log format, look up its GUID in the metabase
    if (*pswl->wzFormat && 0 != lstrcmpW(pswl->wzFormat, L"none"))
    {
        hr = LookupLogFormatGUID(piMetabase, pswl->wzFormat, pswl->wzFormatGUID, countof(pswl->wzFormatGUID));
        ExitOnFailure1(hr, "Failed to get Log Format GUID for Log: %ls", wzLog);
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


/* ****************************************************************
 * ScaWriteWebLog -Writes the IIS log values to the metabase.
 *
 * ****************************************************************/
HRESULT ScaWriteWebLog(
    IMSAdminBase* piMetabase,
    LPCWSTR wzWebBase,
    SCA_WEB_LOG *pswl
    )
{
    HRESULT hr = S_OK;

    if (*pswl->wzFormat)
    {
        if (0 == lstrcmpW(pswl->wzFormat, L"none"))
        {
            // user wishes for Logging to be turned 'off'
            hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"", MD_LOG_TYPE, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)0));
            ExitOnFailure1(hr, "Failed to write Log Type for Web: %ls", wzWebBase);
        }
        else
        {
            Assert(*pswl->wzFormatGUID);
            
            // write the GUID for the log format for the web to the metabase
            hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"", MD_LOG_PLUGIN_ORDER, METADATA_INHERIT, IIS_MD_UT_SERVER, STRING_METADATA, pswl->wzFormatGUID);
            ExitOnFailure1(hr, "Failed to write Log GUID for Web: %ls", wzWebBase);

            hr = ScaWriteMetabaseValue(piMetabase, wzWebBase, L"", MD_LOG_TYPE, METADATA_INHERIT, IIS_MD_UT_SERVER, DWORD_METADATA, (LPVOID)((DWORD_PTR)1));
            ExitOnFailure1(hr, "Failed to write Log Type for Web: %ls", wzWebBase);
        }
    }

LExit:
    return hr;
}


