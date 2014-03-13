//-------------------------------------------------------------------------------------------------
// <copyright file="scasqlstr.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    SQL string functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// sql queries
LPCWSTR vcsSqlStringQuery = L"SELECT `String`, `SqlDb_`, `Component_`,`SQL`,`User_`,`Attributes`,`Sequence` "
L"FROM `SqlString` ORDER BY `SqlDb_`,`Sequence`";
enum eSqlStringQuery { ssqSqlString = 1, ssqSqlDb, ssqComponent, ssqSQL, ssqUser, ssqAttributes, ssqSequence };

LPCWSTR vcsSqlScriptQuery = L"SELECT `ScriptBinary_`,`Script`, `SqlDb_`, `Component_`,`User_`,`Attributes`,`Sequence` "
L"FROM `SqlScript` ORDER BY `SqlDb_`,`Sequence`";
enum eSqlScriptQuery { sscrqScriptBinary=1, sscrqSqlScript, sscrqSqlDb, sscrqComponent, sscrqUser, sscrqAttributes, sscrqSequence };

LPCWSTR vcsSqlBinaryScriptQuery = L"SELECT `Data` FROM `Binary` WHERE `Name`=?";
enum eSqlBinaryScriptQuery { ssbsqData = 1 };


// prototypes for private helper functions
static HRESULT NewSqlStr(
    __out SCA_SQLSTR** ppsss
    );
static SCA_SQLSTR* AddSqlStrToList(
    __in SCA_SQLSTR* psssList,
    __in SCA_SQLSTR* psss
    );
static HRESULT ExecuteStrings(
    __in SCA_DB* psdList,
    __in SCA_SQLSTR* psssList,
    __in BOOL fInstall
    );

HRESULT ScaSqlStrsRead(
    __inout SCA_SQLSTR** ppsssList,
    __in SCA_ACTION saAction
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hView, hRec;
    PMSIHANDLE hViewUser, hRecUser;

    LPWSTR pwzComponent = NULL;
    LPWSTR pwzData = NULL;

    SCA_SQLSTR* psss = NULL;

    if (S_OK != WcaTableExists(L"SqlString") || S_OK != WcaTableExists(L"SqlDatabase"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaSqlStrsRead() - SqlString and/or SqlDatabase table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // loop through all the sql strings
    hr = WcaOpenExecuteView(vcsSqlStringQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on SqlString table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
        INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

        hr = WcaGetRecordString(hRec, ssqComponent, &pwzComponent);
        ExitOnFailure(hr, "Failed to get Component for SQL String.");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzComponent, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "Failed to get state for component: %ls", pwzComponent);

        // If we're doing install but the Component is not being installed or we're doing
        // uninstall but the Component is not being uninstalled, skip it.
        if ((WcaIsInstalling(isInstalled, isAction) && SCA_ACTION_INSTALL != saAction) ||
            (WcaIsUninstalling(isInstalled, isAction) && SCA_ACTION_UNINSTALL != saAction))
        {
            continue;
        }

        hr = NewSqlStr(&psss);
        ExitOnFailure(hr, "failed to allocation new sql string element");

        psss->isInstalled = isInstalled;
        psss->isAction = isAction;

        hr = WcaGetRecordString(hRec, ssqSqlString, &pwzData);
        ExitOnFailure(hr, "Failed to get SqlString.String");
        hr = ::StringCchCopyW(psss->wzKey, countof(psss->wzKey), pwzData);
        ExitOnFailure1(hr, "Failed to copy SqlString.String: %ls", pwzData);

        // find the database information for this string
        hr = WcaGetRecordString(hRec, ssqSqlDb, &pwzData);
        ExitOnFailure1(hr, "Failed to get SqlString.SqlDb_ for SqlString '%ls'", psss->wzKey);
        hr = ::StringCchCopyW(psss->wzSqlDb, countof(psss->wzSqlDb), pwzData);
        ExitOnFailure1(hr, "Failed to copy SqlString.SqlDb_: %ls", pwzData);

        hr = WcaGetRecordInteger(hRec, ssqAttributes, &psss->iAttributes);
        ExitOnFailure1(hr, "Failed to get SqlString.Attributes for SqlString '%ls'", psss->wzKey);

        //get the sequence number for the string (note that this will be sequenced with scripts too)
        hr = WcaGetRecordInteger(hRec, ssqSequence, &psss->iSequence);
        ExitOnFailure1(hr, "Failed to get SqlString.Sequence for SqlString '%ls'", psss->wzKey);

        // execute SQL
        hr = WcaGetRecordFormattedString(hRec, ssqSQL, &pwzData);
        ExitOnFailure1(hr, "Failed to get SqlString.SQL for SqlString '%ls'", psss->wzKey);

        Assert(!psss->pwzSql);
        hr = StrAllocString(&psss->pwzSql, pwzData, 0);
        ExitOnFailure1(hr, "Failed to alloc string for SqlString '%ls'", psss->wzKey);

        *ppsssList = AddSqlStrToList(*ppsssList, psss);
        psss = NULL; // set the sss to NULL so it doesn't get freed below
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while reading SqlString table");

LExit:
    // if anything was left over after an error clean it all up
    if (psss)
    {
        ScaSqlStrsFreeList(psss);
    }

    ReleaseStr(pwzData);
    ReleaseStr(pwzComponent);

    return hr;
}


HRESULT ScaSqlStrsReadScripts(
    __inout SCA_SQLSTR** ppsssList,
    __in SCA_ACTION saAction
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView, hRec;
    PMSIHANDLE hViewBinary, hRecBinary;
    PMSIHANDLE hViewUser, hRecUser;

    LPWSTR pwzComponent = NULL;
    LPWSTR pwzData = NULL;

    BYTE* pbScript = NULL;
    DWORD cbRead = 0;
    DWORD cbScript = 0;
    DWORD cchScript = 0;

    LPWSTR pwzScriptBuffer = NULL;
    WCHAR* pwzScript = NULL;
    WCHAR* pwz;
    DWORD cch = 0;

    SCA_SQLSTR sss;
    SCA_SQLSTR* psss = NULL;

    if (S_OK != WcaTableExists(L"SqlScript") || S_OK != WcaTableExists(L"SqlDatabase") || S_OK != WcaTableExists(L"Binary"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ScaSqlStrsReadScripts() - SqlScripts and/or SqlDatabase table not present");
        ExitFunction1(hr = S_FALSE);
    }

    // open a view on the binary table
    hr = WcaOpenView(vcsSqlBinaryScriptQuery, &hViewBinary);
    ExitOnFailure(hr, "Failed to open view on Binary table for SqlScripts");

    // loop through all the sql scripts
    hr = WcaOpenExecuteView(vcsSqlScriptQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on SqlScript table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
        INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

        hr = WcaGetRecordString(hRec, sscrqComponent, &pwzComponent);
        ExitOnFailure(hr, "Failed to get Component for SQL Script.");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzComponent, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "Failed to get state for component: %ls", pwzComponent);

        // If we're doing install but the Component is not being installed or we're doing
        // uninstall but the Component is not being uninstalled, skip it.
        if ((WcaIsInstalling(isInstalled, isAction) && SCA_ACTION_INSTALL != saAction) ||
            (WcaIsUninstalling(isInstalled, isAction) && SCA_ACTION_UNINSTALL != saAction))
        {
            continue;
        }

        ::ZeroMemory(&sss, sizeof(sss));

        sss.isInstalled = isInstalled;
        sss.isAction = isAction;

        hr = WcaGetRecordString(hRec, sscrqSqlScript, &pwzData);
        ExitOnFailure(hr, "Failed to get SqlScript.Script");
        hr = ::StringCchCopyW(sss.wzKey, countof(sss.wzKey), pwzData);
        ExitOnFailure1(hr, "Failed to copy SqlScript.Script: %ls", pwzData);

        // find the database information for this string
        hr = WcaGetRecordString(hRec, sscrqSqlDb, &pwzData);
        ExitOnFailure1(hr, "Failed to get SqlScript.SqlDb_ for SqlScript '%ls'", sss.wzKey);
        hr = ::StringCchCopyW(sss.wzSqlDb, countof(sss.wzSqlDb), pwzData);
        ExitOnFailure1(hr, "Failed to copy SqlScritp.SqlDbb: %ls", pwzData);

        hr = WcaGetRecordInteger(hRec, sscrqAttributes, &sss.iAttributes);
        ExitOnFailure1(hr, "Failed to get SqlScript.Attributes for SqlScript '%ls'", sss.wzKey);

        hr = WcaGetRecordInteger(hRec, sscrqSequence, &sss.iSequence);
        ExitOnFailure1(hr, "Failed to get SqlScript.Sequence for SqlScript '%ls'", sss.wzKey);

        // get the sql script out of the binary stream
        hr = WcaExecuteView(hViewBinary, hRec);
        ExitOnFailure1(hr, "Failed to open SqlScript.BinaryScript_ for SqlScript '%ls'", sss.wzKey);
        hr = WcaFetchSingleRecord(hViewBinary, &hRecBinary);
        ExitOnFailure1(hr, "Failed to fetch SqlScript.BinaryScript_ for SqlScript '%ls'", sss.wzKey);

        // Note: We need to allocate an extra character on the stream to NULL terminate the SQL script.
        //       The WcaGetRecordStream() function won't let us add extra space on the end of the stream
        //       so we'll read the stream "the old fashioned way".
        //hr = WcaGetRecordStream(hRecBinary, ssbsqData, (BYTE**)&pbScript, &cbScript);
        //ExitOnFailure1(hr, "Failed to read SqlScript.BinaryScript_ for SqlScript '%ls'", sss.wzKey);
        er = ::MsiRecordReadStream(hRecBinary, ssbsqData, NULL, &cbRead);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get size of stream");

        cbScript = cbRead + sizeof(WCHAR); // we may have an ANSI SQL script but leave enough to even NULL terminate a WCHAR string
        hr = WcaAllocStream(&pbScript, cbScript); // this will allocate a fully zeroed out buffer so our string will be NULL terminated
        ExitOnFailure(hr, "failed to allocate data for stream");

        er = ::MsiRecordReadStream(hRecBinary, ssbsqData, reinterpret_cast<char*>(pbScript), &cbRead); //read the buffer but leave the space for the NULL terminator
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to read from stream");

        Assert(cbRead + sizeof(WCHAR) == cbScript);

        // Check for the UNICODE BOM file marker.
        if ((0xFF == *pbScript) && (0xFE == *(pbScript + 1)))
        {
            // Copy the UNICODE string after the BOM marker (subtract one because we'll skip the BOM marker).
            cchScript = (cbScript / sizeof(WCHAR)) - 1;

            hr = StrAllocString(&pwzScriptBuffer, reinterpret_cast<LPWSTR>(pbScript) + 1, 0);
            ExitOnFailure1(hr, "Failed to allocate WCHAR string of size '%d'", cchScript);
        }
        else
        {
            // We have an ANSI string so convert it to UNICODE.
            cchScript = cbScript;

            hr = StrAllocStringAnsi(&pwzScriptBuffer, reinterpret_cast<LPCSTR>(pbScript), 0, CP_ACP);
            ExitOnFailure1(hr, "Failed to allocate WCHAR string of size '%d'", cchScript);
        }

        // Free the byte buffer since it has been converted to a new UNICODE string, one way or another.
        if (pbScript)
        {
            WcaFreeStream(pbScript);
            pbScript = NULL;
        }

        // Process the SQL script stripping out unnecessary stuff (like comments) and looking for "GO" statements.
        pwzScript = pwzScriptBuffer;
        while (cchScript && pwzScript && *pwzScript)
        {
            // strip off leading whitespace
            while (cchScript && *pwzScript && iswspace(*pwzScript))
            {
                ++pwzScript;
                --cchScript;
            }

            Assert(0 <= cchScript);

            // if there is a SQL comment remove it
            while (cchScript && L'/' == *pwzScript && L'*' == *(pwzScript + 1))
            {
                // go until end of comment
                while (cchScript && *pwzScript && *(pwzScript + 1) && !(L'*' == *pwzScript && L'/' == *(pwzScript + 1)))
                {
                    ++pwzScript;
                    --cchScript;
                }

                Assert(2 <= cchScript);

                if (2 <= cchScript)
                {
                    // to account for */ at end
                    pwzScript+=2;
                    cchScript-=2;
                }

                Assert(0 <= cchScript);

                // strip off any new leading whitespace
                while (cchScript && *pwzScript && iswspace(*pwzScript))
                {
                    ++pwzScript;
                    --cchScript;
                }
            }

            while (cchScript && L'-' == *pwzScript && L'-' == *(pwzScript + 1))
            {
                // go past the new line character
                while (cchScript && *pwzScript && L'\n' != *pwzScript)
                {
                    ++pwzScript;
                    --cchScript;
                }

                Assert(0 <= cchScript);

                if (cchScript && L'\n' == *pwzScript)
                {
                    ++pwzScript;
                    --cchScript;
                }

                Assert(0 <= cchScript);

                // strip off any new leading whitespace
                while (cchScript && *pwzScript && iswspace(*pwzScript))
                {
                    ++pwzScript;
                    --cchScript;
                }
            }

            Assert(0 <= cchScript);

            // try to isolate a "GO" keyword and count the characters in the SQL string
            pwz = pwzScript;
            cch = 0;
            while (cchScript && *pwz)
            {
                //skip past comment lines that might have "go" in them
                //note that these comments are "in the middle" of our function,
                //or possibly at the end of a line
                if (cchScript && L'-' == *pwz && L'-' == *(pwz + 1))
                {
                    // skip past chars until the new line character
                    while (cchScript && *pwz && (L'\n' != *pwz))
                    {
                        ++pwz;
                        ++cch;
                        --cchScript;
                    }
                }

                //skip past comment lines of form /* to */ that might have "go" in them
                //note that these comments are "in the middle" of our function,
                //or possibly at the end of a line
                if (cchScript && L'/' == *pwz && L'*' == *(pwz + 1))
                {
                    // skip past chars until the new line character
                    while (cchScript && *pwz && *(pwz + 1) && !((L'*' == *pwz) && (L'/' == *(pwz + 1))))
                    {
                        ++pwz;
                        ++cch;
                        --cchScript;
                    }

                    if (2 <= cchScript)
                    {
                        // to account for */ at end
                        pwz+=2;
                        cch+=2;
                        cchScript-=2;
                    }
                }

                // Skip past strings that may be part of the SQL statement that might have a "go" in them
                if ( cchScript && L'\'' == *pwz )
                {
                    ++pwz;
                    ++cch;
                    --cchScript;

                    // Skip past chars until the end of the string
                    while ( cchScript && *pwz && !(L'\'' == *pwz) )
                    {
                        ++pwz;
                        ++cch;
                        --cchScript;
                    }
                }

                // Skip past strings that may be part of the SQL statement that might have a "go" in them
                if ( cchScript && L'\"' == *pwz )
                {
                    ++pwz;
                    ++cch;
                    --cchScript;

                    // Skip past chars until the end of the string
                    while ( cchScript && *pwz && !(L'\"' == *pwz) )
                    {
                        ++pwz;
                        ++cch;
                        --cchScript;
                    }
                }

                // if "GO" is isolated
                if ((pwzScript == pwz || iswspace(*(pwz - 1))) && 
                    (L'G' == *pwz || L'g' == *pwz) && 
                    (L'O' == *(pwz + 1) || L'o' == *(pwz + 1)) &&
                    (0 == *(pwz + 2) || iswspace(*(pwz + 2))))
                {
                    *pwz = 0; // null terminate the SQL string on the "G"
                    pwz += 2;
                    cchScript -= 2;
                    break;   // found "GO" now add SQL string to list
                }

                ++pwz;
                ++cch;
                --cchScript;
            }

            Assert(0 <= cchScript);

            if (0 < cch) //don't process if there's nothing to process
            {
                // replace tabs with spaces
                for (LPWSTR pwzTab = wcsstr(pwzScript, L"\t"); pwzTab; pwzTab = wcsstr(pwzTab, L"\t"))
                    *pwzTab = ' ';

                // strip off whitespace at the end of the script string
                for (LPWSTR pwzErase = pwzScript + cch - 1; pwzScript < pwzErase && iswspace(*pwzErase); pwzErase--)
                {
                    *(pwzErase) = 0;
                    cch--;
                }
            }

            if (0 < cch)
            {
                hr = NewSqlStr(&psss);
                ExitOnFailure(hr, "failed to allocate new sql string element");

                // copy everything over
                hr = ::StringCchCopyW(psss->wzKey, countof(psss->wzKey), sss.wzKey);
                ExitOnFailure(hr, "Failed to copy key string to sqlstr object");
                hr = ::StringCchCopyW(psss->wzSqlDb, countof(psss->wzSqlDb), sss.wzSqlDb);
                ExitOnFailure(hr, "Failed to copy DB string to sqlstr object");
                hr = ::StringCchCopyW(psss->wzComponent, countof(psss->wzComponent), sss.wzComponent);
                ExitOnFailure(hr, "Failed to copy component string to sqlstr object");
                psss->isInstalled = sss.isInstalled;
                psss->isAction = sss.isAction;
                psss->iAttributes = sss.iAttributes;
                psss->iSequence = sss.iSequence;

                // cchRequired includes the NULL terminating char
                hr = StrAllocString(&psss->pwzSql, pwzScript, 0);
                ExitOnFailure1(hr, "Failed to allocate string for SQL script: '%ls'", psss->wzKey);

                *ppsssList = AddSqlStrToList(*ppsssList, psss);
                psss = NULL; // set the db NULL so it doesn't accidentally get freed below
            }

            pwzScript = pwz;
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while reading SqlString table");

LExit:
    // if anything was left over after an error clean it all up
    if (psss)
    {
        ScaSqlStrsFreeList(psss);
    }

    if (pbScript)
    {
        WcaFreeStream(pbScript);
    }

    ReleaseStr(pwzScriptBuffer);
    ReleaseStr(pwzData);
    ReleaseStr(pwzComponent);

    return hr;
}


HRESULT ScaSqlStrsInstall(
    __in SCA_DB* psdList,
    __in SCA_SQLSTR* psssList
    )
{
    HRESULT hr = ExecuteStrings(psdList, psssList, TRUE);

    return hr;
}


HRESULT ScaSqlStrsUninstall(
    __in SCA_DB* psdList,
    __in SCA_SQLSTR* psssList
    )
{
    HRESULT hr = ExecuteStrings(psdList, psssList, FALSE);

    return hr;
}


void ScaSqlStrsFreeList(
    __in SCA_SQLSTR* psssList
    )
{
    SCA_SQLSTR* psssDelete = psssList;
    while (psssList)
    {
        psssDelete = psssList;
        psssList = psssList->psssNext;

        if (psssDelete->pwzSql)
        {
            ReleaseStr(psssDelete->pwzSql);
        }

        MemFree(psssDelete);
    }
}


// private helper functions

static HRESULT NewSqlStr(
    __out SCA_SQLSTR** ppsss
    )
{
    HRESULT hr = S_OK;
    SCA_SQLSTR* psss = static_cast<SCA_SQLSTR*>(MemAlloc(sizeof(SCA_SQLSTR), TRUE));
    ExitOnNull(psss, hr, E_OUTOFMEMORY, "failed to allocate memory for new sql string element");

    *ppsss = psss;

LExit:
    return hr;
}


static SCA_SQLSTR* AddSqlStrToList(
    __in SCA_SQLSTR* psssList,
    __in SCA_SQLSTR* psss
    )
{
    Assert(psss); //just checking

    //make certain we have a valid sequence number; note that negatives are technically valid
    if (MSI_NULL_INTEGER == psss->iSequence)
    {
        psss->iSequence = 0;
    }

    if (psssList)
    {
        //list already exists, so insert psss into the list in Sequence order

        //see if we need to change the head, otherwise figure out where in the order it fits
        if (psss->iSequence < psssList->iSequence)
        {
            psss->psssNext = psssList;
            psssList = psss;
        }
        else
        {
            SCA_SQLSTR* psssT = psssList;
            //note that if Sequence numbers are duplicated, as in the case of a sqlscript,
            //we need to insert them "at the end" of the group so the sqlfile stays in order
            while (psssT->psssNext && (psssT->psssNext->iSequence <= psss->iSequence))
            {
                psssT  = psssT->psssNext;
            }

            //insert our new psss AFTER psssT
            psss->psssNext = psssT->psssNext;
            psssT->psssNext = psss;
        }
    }
    else
    {
        psssList = psss;
    }

    return psssList;
}


static HRESULT ExecuteStrings(
    __in SCA_DB* psdList,
    __in SCA_SQLSTR* psssList,
    __in BOOL fInstall
    )
{
    HRESULT hr = S_FALSE; // assume nothing will be done

    int iRollback = -1;
    int iOldRollback = iRollback;

    LPCWSTR wzOldDb = NULL;
    UINT uiCost = 0;
    WCHAR* pwzCustomActionData = NULL;
    WCHAR wzNumber[64];

    // loop through all sql strings
    for (SCA_SQLSTR* psss = psssList; psss; psss = psss->psssNext)
    {
        // if installing this component
        if ((fInstall && (psss->iAttributes & SCASQL_EXECUTE_ON_INSTALL) && WcaIsInstalling(psss->isInstalled, psss->isAction) && !WcaIsReInstalling(psss->isInstalled, psss->isAction)) ||
            (fInstall && (psss->iAttributes & SCASQL_EXECUTE_ON_REINSTALL) && WcaIsReInstalling(psss->isInstalled, psss->isAction)) ||
            (!fInstall && (psss->iAttributes & SCASQL_EXECUTE_ON_UNINSTALL) && WcaIsUninstalling(psss->isInstalled, psss->isAction)))
        {
            // determine if this is a rollback scheduling or normal deferred scheduling
            if (psss->iAttributes & SCASQL_ROLLBACK)
            {
                iRollback = 1;
            }
            else
            {
                iRollback = 0;
            }

            // if we need to create a connection to a new server\database
            if (!wzOldDb || 0 != lstrcmpW(wzOldDb, psss->wzSqlDb) || iOldRollback != iRollback)
            {
                const SCA_DB* psd = ScaDbsFindDatabase(psss->wzSqlDb, psdList);
                if (!psd)
                {
                    ExitOnFailure1(hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "failed to find data for Database: %ls", psss->wzSqlDb);
                }

                if (-1 == iOldRollback)
                {
                    iOldRollback = iRollback;
                }
                Assert(0 == iOldRollback || 1 == iOldRollback);

                // if there was custom action data before, schedule the action to write it
                if (pwzCustomActionData && *pwzCustomActionData)
                {
                    Assert(pwzCustomActionData && *pwzCustomActionData && uiCost);

                    hr = WcaDoDeferredAction(1 == iOldRollback ? L"RollbackExecuteSqlStrings" : L"ExecuteSqlStrings", pwzCustomActionData, uiCost);
                    ExitOnFailure1(hr, "failed to schedule ExecuteSqlStrings action, rollback: %d", iOldRollback);
                    iOldRollback = iRollback;

                    *pwzCustomActionData = L'\0';
                    uiCost = 0;
                }

                Assert(!pwzCustomActionData  || (pwzCustomActionData && 0 == *pwzCustomActionData) && 0 == uiCost);

                hr = WcaWriteStringToCaData(psd->wzKey, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Server Database String to CustomActionData for Database String: %ls", psd->wzKey);

                hr = WcaWriteStringToCaData(psd->wzServer, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Server to CustomActionData for Database String: %ls", psd->wzKey);

                hr = WcaWriteStringToCaData(psd->wzInstance, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Instance to CustomActionData for Database String: %ls", psd->wzKey);

                hr = WcaWriteStringToCaData(psd->wzDatabase, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Database to CustomActionData for Database String: %ls", psd->wzKey);

                hr = ::StringCchPrintfW(wzNumber, countof(wzNumber), L"%d", psd->iAttributes);
                ExitOnFailure(hr, "Failed to format attributes integer value to string");
                hr = WcaWriteStringToCaData(wzNumber, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Attributes to CustomActionData for Database String: %ls", psd->wzKey);

                hr = ::StringCchPrintfW(wzNumber, countof(wzNumber), L"%d", psd->fUseIntegratedAuth);
                ExitOnFailure(hr, "Failed to format UseIntegratedAuth integer value to string");
                hr = WcaWriteStringToCaData(wzNumber, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL IntegratedAuth flag to CustomActionData for Database String: %ls", psd->wzKey);

                hr = WcaWriteStringToCaData(psd->scau.wzName, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL UserName to CustomActionData for Database String: %ls", psd->wzKey);

                hr = WcaWriteStringToCaData(psd->scau.wzPassword, &pwzCustomActionData);
                ExitOnFailure1(hr, "Failed to add SQL Password to CustomActionData for Database String: %ls", psd->wzKey);

                uiCost += COST_SQL_CONNECTDB;

                wzOldDb = psss->wzSqlDb;
            }

            WcaLog(LOGMSG_VERBOSE, "Scheduling SQL string: %ls", psss->pwzSql);

            hr = WcaWriteStringToCaData(psss->wzKey, &pwzCustomActionData);
            ExitOnFailure1(hr, "Failed to add SQL Key to CustomActionData for SQL string: %ls", psss->wzKey);

            hr = WcaWriteIntegerToCaData(psss->iAttributes, &pwzCustomActionData);
            ExitOnFailure1(hr, "failed to add attributes to CustomActionData for SQL string: %ls", psss->wzKey);

            hr = WcaWriteStringToCaData(psss->pwzSql, &pwzCustomActionData);
            ExitOnFailure1(hr, "Failed to to add SQL Query to CustomActionData for SQL string: %ls", psss->wzKey);
            uiCost += COST_SQL_STRING;
        }
    }

    if (pwzCustomActionData && *pwzCustomActionData)
    {
        Assert(pwzCustomActionData && *pwzCustomActionData && uiCost);
        hr = WcaDoDeferredAction(1 == iRollback ? L"RollbackExecuteSqlStrings" : L"ExecuteSqlStrings", pwzCustomActionData, uiCost);
        ExitOnFailure(hr, "Failed to schedule ExecuteSqlStrings action");

        *pwzCustomActionData = L'\0';
        uiCost = 0;
    }

LExit:
    ReleaseStr(pwzCustomActionData);

    return hr;
}
