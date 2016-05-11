// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

struct ESE_QUERY
{
    ESE_QUERY_TYPE qtQueryType;
    BOOL fIndexRangeSet;

    JET_SESID jsSession;
    JET_TABLEID jtTable;

    DWORD dwColumns;
    void *pvData[10]; // The data queried for for this column
    DWORD cbData[10]; // One for each column, describes the size of the corresponding entry in ppvData
};

// Todo: convert more JET_ERR to HRESULTS here
HRESULT HresultFromJetError(JET_ERR jEr)
{
    HRESULT hr = S_OK;

    switch (jEr)
    {
    case JET_errSuccess:
        return S_OK;

    case JET_wrnNyi:
        return E_NOTIMPL;
        break;

    case JET_errOutOfMemory:
        hr = E_OUTOFMEMORY;
        break;

    case JET_errInvalidParameter: __fallthrough;
    case JET_errInvalidInstance:
        hr = E_INVALIDARG;
        break;

    case JET_errDatabaseInUse:
        hr = HRESULT_FROM_WIN32(ERROR_DEVICE_IN_USE);
        break;

    case JET_errKeyDuplicate:
        hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS);
        break;

    case JET_errInvalidSystemPath: __fallthrough;
    case JET_errInvalidLogDirectory: __fallthrough;
    case JET_errInvalidPath: __fallthrough;
    case JET_errDatabaseInvalidPath:
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_NAME);
        break;

    case JET_errDatabaseLocked:
        hr = HRESULT_FROM_WIN32(ERROR_FILE_CHECKED_OUT);
        break;

    case JET_errInvalidDatabase:
        hr = HRESULT_FROM_WIN32(ERROR_INTERNAL_DB_CORRUPTION);
        break;

    case JET_errCallbackNotResolved:
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_FUNCTION);
        break;

    case JET_errNoCurrentRecord: __fallthrough;
    case JET_errRecordNotFound: __fallthrough;
    case JET_errFileNotFound: __fallthrough;
    case JET_errObjectNotFound:
        hr = E_NOTFOUND;
        break;

    case JET_wrnBufferTruncated:
        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        break;

    case JET_errFileAccessDenied:
        hr = E_ACCESSDENIED;
        break;

    default:
        hr = E_FAIL;
    }

    // Log the actual Jet error code so we have record of it before it's morphed into an HRESULT to be compatible with the rest of our code
    ExitTrace1(hr, "Encountered Jet Error: 0x%08x", jEr);

    return hr;
}

#define ExitOnJetFailure(e, x, s) { x = HresultFromJetError(e); if (S_OK != x) { ExitTrace(x, s); goto LExit; }}
#define ExitOnJetFailure1(e, x, f, s) { x = HresultFromJetError(e); if (S_OK != x) { ExitTrace1(x, f, s); goto LExit; }}
#define ExitOnJetFailure2(e, x, f, s, t) { x = HresultFromJetError(e); if (S_OK != x) { ExitTrace(x, f, s, t); goto LExit; }}
#define ExitOnJetFailure3(e, x, f, s, t, u) { x = HresultFromJetError(e); if (S_OK != x) { ExitTrace1(x, f, s, t, u); goto LExit; }}

#define ExitOnRootJetFailure(e, x, s) { x = HresultFromJetError(e); if (S_OK != x) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace(x, s); goto LExit; }}
#define ExitOnRootJetFailure1(e, x, f, s) { x = HresultFromJetError(e); if (S_OK != x) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace1(x, f, s); goto LExit; }}
#define ExitOnRootJetFailure2(e, x, f, s, t) { x = HresultFromJetError(e); if (S_OK != x) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace(x, f, s, t); goto LExit; }}
#define ExitOnRootJetFailure3(e, x, f, s, t, u) { x = HresultFromJetError(e); if (S_OK != x) { Dutil_RootFailure(__FILE__, __LINE__, x); ExitTrace1(x, f, s, t, u); goto LExit; }}

HRESULT DAPI EseBeginSession(
    __out JET_INSTANCE *pjiInstance,
    __out JET_SESID *pjsSession,
    __in_z LPCWSTR pszInstance,
    __in_z LPCWSTR pszPath
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    LPSTR pszAnsiInstance = NULL;
    LPSTR pszAnsiPath = NULL;

    hr = DirEnsureExists(pszPath, NULL);
    ExitOnFailure(hr, "Failed to ensure database directory exists");

    // Sigh. JETblue requires Vista and up for the wide character version of this function, so we'll convert to ANSI before calling,
    // likely breaking everyone with unicode characters in their path.
    hr = StrAnsiAllocString(&pszAnsiInstance, pszInstance, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting instance name to ansi");

    hr = StrAnsiAllocString(&pszAnsiPath, pszPath, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting session path name to ansi");

    jEr = JetCreateInstanceA(pjiInstance, pszAnsiInstance);
    ExitOnJetFailure(jEr, hr, "Failed to create instance");

    jEr = JetSetSystemParameter(pjiInstance, NULL, JET_paramSystemPath, NULL, pszAnsiPath);
    ExitOnJetFailure1(jEr, hr, "Failed to set jet system path to: %s", pszAnsiPath);

    // This makes sure log files that are created are created next to the database, not next to our EXE (note they last after execution)
    jEr = JetSetSystemParameter(pjiInstance, NULL, JET_paramLogFilePath, NULL, pszAnsiPath);
    ExitOnJetFailure1(jEr, hr, "Failed to set jet log file path to: %s", pszAnsiPath);

    jEr = JetSetSystemParameter(pjiInstance, NULL, JET_paramMaxOpenTables, 10, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to set jet max open tables parameter");

    // TODO: Use callback hooks so that Jet Engine uses our memory allocation methods, etc.? (search docs for "JET_PFNREALLOC" - there are other callbacks too)

    jEr = JetInit(pjiInstance);
    ExitOnJetFailure(jEr, hr, "Failed to initialize jet engine instance");

    jEr = JetBeginSession(*pjiInstance, pjsSession, NULL, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to begin jet session");

LExit:
    ReleaseStr(pszAnsiInstance);
    ReleaseStr(pszAnsiPath);

    return hr;
}

HRESULT DAPI EseEndSession(
    __in JET_INSTANCE jiInstance,
    __in JET_SESID jsSession
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetEndSession(jsSession, 0);
    ExitOnJetFailure(jEr, hr, "Failed to end jet session");

    jEr = JetTerm(jiInstance);
    ExitOnJetFailure(jEr, hr, "Failed to uninitialize jet engine instance");

LExit:
    return hr;
}

// Utility function used by EnsureSchema()
HRESULT AllocColumnCreateStruct(
    __in const ESE_TABLE_SCHEMA *ptsSchema,
    __deref_out JET_COLUMNCREATE **ppjccColumnCreate
    )
{
    HRESULT hr = S_OK;
    DWORD_PTR i;
    size_t cbAllocSize = 0;

    hr = ::SizeTMult(ptsSchema->dwColumns, sizeof(JET_COLUMNCREATE), &(cbAllocSize));
    ExitOnFailure(hr, "Maximum allocation exceeded.");

    *ppjccColumnCreate = static_cast<JET_COLUMNCREATE*>(MemAlloc(cbAllocSize, TRUE));
    ExitOnNull(*ppjccColumnCreate, hr, E_OUTOFMEMORY, "Failed to allocate column create structure for database");

    for (i = 0; i < ptsSchema->dwColumns; ++i)
    {
        (*ppjccColumnCreate)[i].cbStruct = sizeof(JET_COLUMNCREATE);

        hr = StrAnsiAllocString(&(*ppjccColumnCreate)[i].szColumnName, ptsSchema->pcsColumns[i].pszName, 0, CP_ACP);
        ExitOnFailure1(hr, "Failed to allocate ansi column name: %ls", ptsSchema->pcsColumns[i].pszName);

        (*ppjccColumnCreate)[i].coltyp = ptsSchema->pcsColumns[i].jcColumnType;

        if (JET_coltypText == (*ppjccColumnCreate)[i].coltyp)
        {
            (*ppjccColumnCreate)[i].cbMax = 256;
        }
        else if (JET_coltypLongText == (*ppjccColumnCreate)[i].coltyp)
        {
            (*ppjccColumnCreate)[i].cbMax = 2147483648;
            (*ppjccColumnCreate)[i].grbit = JET_bitColumnTagged; // LongText columns must be tagged
            ptsSchema->pcsColumns[i].fNullable = TRUE;
        }
        else if (JET_coltypLong == (*ppjccColumnCreate)[i].coltyp)
        {
            (*ppjccColumnCreate)[i].cbMax = 4;

            if (ptsSchema->pcsColumns[i].fAutoIncrement)
            {
                (*ppjccColumnCreate)[i].grbit |= JET_bitColumnAutoincrement;
            }
        }

        if (!(ptsSchema->pcsColumns[i].fNullable))
        {
            (*ppjccColumnCreate)[i].grbit |= JET_bitColumnNotNULL;
        }

        (*ppjccColumnCreate)[i].pvDefault = NULL;
        (*ppjccColumnCreate)[i].cbDefault = 0;
        (*ppjccColumnCreate)[i].cp = 1200;
        (*ppjccColumnCreate)[i].columnid = 0;
        (*ppjccColumnCreate)[i].err = 0;
    }

LExit:
    return hr;
}

HRESULT FreeColumnCreateStruct(
    __in_ecount(dwColumns) JET_COLUMNCREATE *pjccColumnCreate,
    __in DWORD dwColumns
    )
{
    HRESULT hr = S_OK;
    DWORD i;

    for (i = 0; i < dwColumns; ++i)
    {
        ReleaseStr((pjccColumnCreate[i]).szColumnName);
    }

    hr = MemFree(pjccColumnCreate);
    ExitOnFailure(hr, "Failed to release core column create struct");

LExit:
    return hr;
}

// Utility function used by EnsureSchema()
HRESULT AllocIndexCreateStruct(
    __in const ESE_TABLE_SCHEMA *ptsSchema,
    __deref_out JET_INDEXCREATE **ppjicIndexCreate
    )
{
    HRESULT hr = S_OK;
    LPSTR pszMultiSzKeys = NULL;
    LPSTR pszIndexName = NULL;
    LPSTR pszTempString = NULL;
    BOOL fKeyColumns = FALSE;
    DWORD_PTR i;

    for (i=0; i < ptsSchema->dwColumns; ++i)
    {
        if (ptsSchema->pcsColumns[i].fKey)
        {
            hr = StrAnsiAllocString(&pszTempString, ptsSchema->pcsColumns[i].pszName, 0, CP_ACP);
            ExitOnFailure1(hr, "Failed to convert string to ansi: %ls", ptsSchema->pcsColumns[i].pszName);

            hr = StrAnsiAllocConcat(&pszMultiSzKeys, "+", 0);
            ExitOnFailure1(hr, "Failed to append plus sign to multisz string: %s", pszTempString);

            hr = StrAnsiAllocConcat(&pszMultiSzKeys, pszTempString, 0);
            ExitOnFailure1(hr, "Failed to append column name to multisz string: %s", pszTempString);

            ReleaseNullStr(pszTempString);

            // All question marks will be converted to null characters later; this is just to trick dutil
            // into letting us create an ansi, double-null-terminated list of single-null-terminated strings
            hr = StrAnsiAllocConcat(&pszMultiSzKeys, "?", 0);
            ExitOnFailure1(hr, "Failed to append placeholder character to multisz string: %ls", pszMultiSzKeys);

            // Record that at least one key column was found
            fKeyColumns = TRUE;
        }
    }

    // If no key columns were found, don't create an index - just return
    if (!fKeyColumns)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = StrAnsiAllocString(&pszIndexName, ptsSchema->pszName, 0, CP_ACP);
    ExitOnFailure1(hr, "Failed to allocate ansi string version of %ls", ptsSchema->pszName);

    hr = StrAnsiAllocConcat(&pszIndexName, "_Index", 0);
    ExitOnFailure1(hr, "Failed to append table name string version of %ls", ptsSchema->pszName);

    *ppjicIndexCreate = static_cast<JET_INDEXCREATE*>(MemAlloc(sizeof(JET_INDEXCREATE), TRUE));
    ExitOnNull(*ppjicIndexCreate, hr, E_OUTOFMEMORY, "Failed to allocate index create structure for database");

    // Record the size including both null terminators - the struct requires this
    DWORD dwSize = 0;
    dwSize = lstrlen(pszMultiSzKeys) + 1; // add 1 to include null character at the end
    ExitOnFailure(hr, "Failed to get size of keys string");

    // At this point convert all question marks to null characters
    for (i = 0; i < dwSize; ++i)
    {
        if ('?' == pszMultiSzKeys[i])
        {
            pszMultiSzKeys[i] = '\0';
        }
    }

    (*ppjicIndexCreate)->cbStruct = sizeof(JET_INDEXCREATE);
    (*ppjicIndexCreate)->szIndexName = pszIndexName;
    (*ppjicIndexCreate)->szKey = pszMultiSzKeys;
    (*ppjicIndexCreate)->cbKey = dwSize;
    (*ppjicIndexCreate)->grbit = JET_bitIndexUnique | JET_bitIndexPrimary;
    (*ppjicIndexCreate)->ulDensity = 80;
    (*ppjicIndexCreate)->lcid = 1033;
    (*ppjicIndexCreate)->pidxunicode = NULL;
    (*ppjicIndexCreate)->cbVarSegMac = 0;
    (*ppjicIndexCreate)->rgconditionalcolumn = NULL;
    (*ppjicIndexCreate)->cConditionalColumn = 0;
    (*ppjicIndexCreate)->err = 0;

LExit:
    ReleaseStr(pszTempString);

    return hr;
}

HRESULT EnsureSchema(
    __in JET_DBID jdbDb,
    __in JET_SESID jsSession,
    __in ESE_DATABASE_SCHEMA *pdsSchema
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    BOOL fTransaction = FALSE;
    DWORD dwTable;
    DWORD dwColumn;
    JET_TABLECREATE jtTableCreate = { };

    // Set parameters which apply to all tables here
    jtTableCreate.cbStruct = sizeof(jtTableCreate);
    jtTableCreate.ulPages = 100;
    jtTableCreate.ulDensity = 0; // per the docs, 0 means "use the default value"
    jtTableCreate.cIndexes = 1;

    hr = EseBeginTransaction(jsSession);
    ExitOnFailure(hr, "Failed to begin transaction to create tables");
    fTransaction = TRUE;
   
    for (dwTable = 0;dwTable < pdsSchema->dwTables; ++dwTable)
    {
        // Don't free this pointer - it's just a shortcut to the current table's name within the struct
        LPCWSTR pwzTableName = pdsSchema->ptsTables[dwTable].pszName;

        // Ensure table exists
        hr = EseOpenTable(jsSession, jdbDb, pwzTableName, &pdsSchema->ptsTables[dwTable].jtTable);
        if (E_NOTFOUND == hr) // if the table is missing, create it
        {
            // Fill out the JET_TABLECREATE struct
            hr = StrAnsiAllocString(&jtTableCreate.szTableName, pdsSchema->ptsTables[dwTable].pszName, 0, CP_ACP);
            ExitOnFailure(hr, "Failed converting table name to ansi");

            hr = AllocColumnCreateStruct(&(pdsSchema->ptsTables[dwTable]), &jtTableCreate.rgcolumncreate);
            ExitOnFailure(hr, "Failed to allocate column create struct");

            hr = AllocIndexCreateStruct(&(pdsSchema->ptsTables[dwTable]), &jtTableCreate.rgindexcreate);
            ExitOnFailure(hr, "Failed to allocate index create struct");

            jtTableCreate.cColumns = pdsSchema->ptsTables[dwTable].dwColumns;
            jtTableCreate.tableid = NULL;

            // TODO: Investigate why we can't create a table without a key column?
            // Actually create the table using our JET_TABLECREATE struct
            jEr = JetCreateTableColumnIndex(jsSession, jdbDb, &jtTableCreate);
            ExitOnJetFailure1(jEr, hr, "Failed to create %ls table", pwzTableName);

            // Record the table ID in our cache
            pdsSchema->ptsTables[dwTable].jtTable = jtTableCreate.tableid;

            // Record the column IDs in our cache
            for (dwColumn = 0; dwColumn < pdsSchema->ptsTables[dwTable].dwColumns; ++dwColumn)
            {
                pdsSchema->ptsTables[dwTable].pcsColumns[dwColumn].jcColumn = jtTableCreate.rgcolumncreate[dwColumn].columnid;
            }

            // Free and NULL things we allocated in this struct
            ReleaseNullStr(jtTableCreate.szTableName);

            hr = FreeColumnCreateStruct(jtTableCreate.rgcolumncreate, jtTableCreate.cColumns);
            ExitOnFailure(hr, "Failed to free column create struct");
            jtTableCreate.rgcolumncreate = NULL;
        }
        else
        {
            // If the table already exists, grab the column ids and put them into our cache
            for (dwColumn = 0;dwColumn < pdsSchema->ptsTables[dwTable].dwColumns; ++dwColumn)
            {
                // Don't free this - it's just a shortcut to the current column within the struct
                ESE_COLUMN_SCHEMA *pcsColumn = &(pdsSchema->ptsTables[dwTable].pcsColumns[dwColumn]);
                ULONG ulColumnSize = 0;
                BOOL fNullable = pcsColumn->fNullable;

                // Todo: this code is nearly duplicated from AllocColumnCreateStruct - factor it out!
                if (JET_coltypText == pcsColumn->jcColumnType)
                {
                    ulColumnSize = 256;
                }
                else if (JET_coltypLongText == pcsColumn->jcColumnType)
                {
                    ulColumnSize = 2147483648;
                    fNullable = TRUE;
                }
                else if (JET_coltypLong == pcsColumn->jcColumnType)
                {
                    ulColumnSize = 4;
                    fNullable = TRUE;
                }

                hr = EseEnsureColumn(jsSession, pdsSchema->ptsTables[dwTable].jtTable, pcsColumn->pszName, pcsColumn->jcColumnType, ulColumnSize, pcsColumn->fFixed, fNullable, &pcsColumn->jcColumn);
                ExitOnFailure2(hr, "Failed to create column %u of %ls table", dwColumn, pwzTableName);
            }
        }
    }

LExit:
    ReleaseStr(jtTableCreate.szTableName);

    if (NULL != jtTableCreate.rgcolumncreate)
    {
        // Don't record the HRESULT here or it will override the return value of this function
        FreeColumnCreateStruct(jtTableCreate.rgcolumncreate, jtTableCreate.cColumns);
    }

    if (fTransaction)
    {
        EseCommitTransaction(jsSession);
    }

    return hr;
}

// Todo: support overwrite flag? Unfortunately, requires WinXP and up
// Todo: Add version parameter, and a built-in dutil table that stores the version of the database schema on disk - then allow overriding the "migrate to new schema" functionality with a callback
HRESULT DAPI EseEnsureDatabase(
    __in JET_SESID jsSession,
    __in_z LPCWSTR pszFile,
    __in ESE_DATABASE_SCHEMA *pdsSchema,
    __out JET_DBID* pjdbDb,
    __in BOOL fExclusive,
    __in BOOL fReadonly
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    JET_GRBIT jgrOptions = 0;
    LPWSTR pszDir = NULL;
    LPSTR pszAnsiFile = NULL;

    // Sigh. JETblue requires Vista and up for the wide character version of this function, so we'll convert to ANSI before calling,
    // likely breaking all those with unicode characters in their path.
    hr = StrAnsiAllocString(&pszAnsiFile, pszFile, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting database name to ansi");

    hr = PathGetDirectory(pszFile, &pszDir);
    ExitOnFailure(hr, "Failed to get directory that will contain database file");

    hr = DirEnsureExists(pszDir, NULL);
    ExitOnFailure1(hr, "Failed to ensure directory exists for database: %ls", pszDir);

    if (FileExistsEx(pszFile, NULL))
    {
        if (fReadonly)
        {
            jgrOptions = jgrOptions | JET_bitDbReadOnly;
        }

        jEr = JetAttachDatabaseA(jsSession, pszAnsiFile, jgrOptions);
        ExitOnJetFailure1(jEr, hr, "Failed to attach to database %s", pszAnsiFile);

        // This flag doesn't apply to attach, only applies to Open, so only set it after the attach
        if (fExclusive)
        {
            jgrOptions = jgrOptions | JET_bitDbExclusive;
        }

        jEr = JetOpenDatabaseA(jsSession, pszAnsiFile, NULL, pjdbDb, jgrOptions);
        ExitOnJetFailure1(jEr, hr, "Failed to open database %s", pszAnsiFile);
    }
    else
    {
        jEr = JetCreateDatabase2A(jsSession, pszAnsiFile, 0, pjdbDb, 0);
        ExitOnJetFailure1(jEr, hr, "Failed to create database %ls", pszFile);
    }

    hr = EnsureSchema(*pjdbDb, jsSession, pdsSchema);
    ExitOnFailure(hr, "Failed to ensure database schema matches expectations");
        
LExit:
    ReleaseStr(pszDir);
    ReleaseStr(pszAnsiFile);

    return hr;
}

HRESULT DAPI EseCloseDatabase(
    __in JET_SESID jsSession,
    __in JET_DBID jdbDb
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    JET_GRBIT jgrOptions = 0;

    jEr = JetCloseDatabase(jsSession, jdbDb, jgrOptions);
    ExitOnJetFailure(jEr, hr, "Failed to close database");

LExit:
    return hr;
}

HRESULT DAPI EseCreateTable(
    __in JET_SESID jsSession,
    __in JET_DBID jdbDb,
    __in_z LPCWSTR pszTable,
    __out JET_TABLEID *pjtTable
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    LPSTR pszAnsiTable = NULL;

    hr = StrAnsiAllocString(&pszAnsiTable, pszTable, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting table name to ansi");

    jEr = JetCreateTableA(jsSession, jdbDb, pszAnsiTable, 100, 0, pjtTable);
    ExitOnJetFailure1(jEr, hr, "Failed to create table %s", pszAnsiTable);

LExit:
    ReleaseStr(pszAnsiTable);

    return hr;
}

HRESULT DAPI EseOpenTable(
    __in JET_SESID jsSession,
    __in JET_DBID jdbDb,
    __in_z LPCWSTR pszTable,
    __out JET_TABLEID *pjtTable
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    LPSTR pszAnsiTable = NULL;

    hr = StrAnsiAllocString(&pszAnsiTable, pszTable, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting table name to ansi");

    jEr = JetOpenTableA(jsSession, jdbDb, pszAnsiTable, NULL, 0, 0, pjtTable);
    ExitOnJetFailure1(jEr, hr, "Failed to open table %s", pszAnsiTable);

LExit:
    ReleaseStr(pszAnsiTable);

    return hr;
}

HRESULT DAPI EseCloseTable(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetCloseTable(jsSession, jtTable);
    ExitOnJetFailure(jEr, hr, "Failed to close table");

LExit:
    return hr;
}

HRESULT DAPI EseEnsureColumn(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in_z LPCWSTR pszColumnName,
    __in JET_COLTYP jcColumnType,
    __in ULONG ulColumnSize,
    __in BOOL fFixed,
    __in BOOL fNullable,
    __out_opt JET_COLUMNID *pjcColumn
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    LPSTR pszAnsiColumnName = NULL;
    JET_COLUMNDEF jcdColumnDef = { sizeof(JET_COLUMNDEF) };
    JET_COLUMNBASE jcdTempBase = { sizeof(JET_COLUMNBASE) };

    hr = StrAnsiAllocString(&pszAnsiColumnName, pszColumnName, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting column name to ansi");

    jEr = JetGetTableColumnInfoA(jsSession, jtTable, pszAnsiColumnName, &jcdTempBase, sizeof(JET_COLUMNBASE), JET_ColInfoBase);
    if (JET_errSuccess == jEr)
    {
        // Return the found columnID
        if (NULL != pjcColumn)
        {
            *pjcColumn = jcdTempBase.columnid;
        }

        ExitFunction1(hr = S_OK);
    }
    else if (JET_errColumnNotFound == jEr)
    {
        jEr = JET_errSuccess;
    }
    ExitOnJetFailure1(jEr, hr, "Failed to check if column exists: %s", pszAnsiColumnName);

    jcdColumnDef.columnid = 0;
    jcdColumnDef.coltyp = jcColumnType;
    jcdColumnDef.wCountry = 0;
    jcdColumnDef.langid = 0;
    jcdColumnDef.cp = 1200;
    jcdColumnDef.wCollate = 0;
    jcdColumnDef.cbMax = ulColumnSize;
    jcdColumnDef.grbit = 0;

    if (fFixed)
    {
        jcdColumnDef.grbit = jcdColumnDef.grbit | JET_bitColumnFixed;
    }
    if (!fNullable)
    {
        jcdColumnDef.grbit = jcdColumnDef.grbit | JET_bitColumnNotNULL;
    }

    jEr = JetAddColumnA(jsSession, jtTable, pszAnsiColumnName, &jcdColumnDef, NULL, 0, pjcColumn);
    ExitOnJetFailure1(jEr, hr, "Failed to add column %ls", pszColumnName);

LExit:
    ReleaseStr(pszAnsiColumnName);

    return hr;
}

HRESULT DAPI EseGetColumn(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in_z LPCWSTR pszColumnName,
    __out JET_COLUMNID *pjcColumn
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    LPSTR pszAnsiColumnName = NULL;
    JET_COLUMNBASE jcdTempBase = { sizeof(JET_COLUMNBASE) };

    hr = StrAnsiAllocString(&pszAnsiColumnName, pszColumnName, 0, CP_ACP);
    ExitOnFailure(hr, "Failed converting column name to ansi");

    jEr = JetGetTableColumnInfoA(jsSession, jtTable, pszAnsiColumnName, &jcdTempBase, sizeof(JET_COLUMNBASE), JET_ColInfoBase);
    if (JET_errSuccess == jEr)
    {
        // Return the found columnID
        if (NULL != pjcColumn)
        {
            *pjcColumn = jcdTempBase.columnid;
        }

        ExitFunction1(hr = S_OK);
    }
    ExitOnJetFailure1(jEr, hr, "Failed to check if column exists: %s", pszAnsiColumnName);

LExit:
    ReleaseStr(pszAnsiColumnName);

    return hr;
}

HRESULT DAPI EseMoveCursor(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in LONG lRow
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetMove(jsSession, jtTable, lRow, 0);
    ExitOnJetFailure1(jEr, hr, "Failed to move jet cursor by amount: %d", lRow);

LExit:
    return hr;
}

HRESULT DAPI EseDeleteRow(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetDelete(jsSession, jtTable);
    ExitOnJetFailure(jEr, hr, "Failed to delete row");

LExit:
    return hr;
}

HRESULT DAPI EseBeginTransaction(
    __in JET_SESID jsSession
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetBeginTransaction(jsSession);
    ExitOnJetFailure(jEr, hr, "Failed to begin transaction");

LExit:
    return hr;
}

HRESULT DAPI EseRollbackTransaction(
    __in JET_SESID jsSession,
    __in BOOL fAll
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetRollback(jsSession, fAll ? JET_bitRollbackAll : 0);
    ExitOnJetFailure(jEr, hr, "Failed to rollback transaction");

LExit:
    return hr;
}

HRESULT DAPI EseCommitTransaction(
    __in JET_SESID jsSession
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetCommitTransaction(jsSession, 0);
    ExitOnJetFailure(jEr, hr, "Failed to commit transaction");

LExit:
    return hr;
}

HRESULT DAPI EsePrepareUpdate(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in ULONG ulPrep
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetPrepareUpdate(jsSession, jtTable, ulPrep);
    ExitOnJetFailure1(jEr, hr, "Failed to prepare for update of type: %ul", ulPrep);

LExit:
    return hr;
}

HRESULT DAPI EseFinishUpdate(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in BOOL fSeekToInsertedRecord
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    unsigned char rgbBookmark[JET_cbBookmarkMost + 1];
    DWORD cbBookmark;

    if (fSeekToInsertedRecord)
    {
        jEr = JetUpdate(jsSession, jtTable, rgbBookmark, sizeof(rgbBookmark), &cbBookmark);
        ExitOnJetFailure(jEr, hr, "Failed to run update and retrieve bookmark");

        jEr = JetGotoBookmark(jsSession, jtTable, rgbBookmark, cbBookmark);
        ExitOnJetFailure(jEr, hr, "Failed to seek to recently updated record using bookmark");
    }
    else
    {
        jEr = JetUpdate(jsSession, jtTable, NULL, 0, NULL);
        ExitOnJetFailure(jEr, hr, "Failed to run update (without retrieving bookmark)");
    }

LExit:
    // If we fail, the caller won't expect that the update wasn't finished, so we'll cancel their entire update to leave them in a good state
    if (FAILED(hr))
    {
        JetPrepareUpdate(jsSession, jtTable, JET_prepCancel);
    }

    return hr;
}

HRESULT DAPI EseSetColumnBinary(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetSetColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, pbBuffer, static_cast<unsigned long>(cbBuffer), 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to set binary value into column of database");

LExit:
    return hr;
}

HRESULT DAPI EseSetColumnDword(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __in DWORD dwValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetSetColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, &dwValue, sizeof(DWORD), 0, NULL);
    ExitOnJetFailure1(jEr, hr, "Failed to set dword value into column of database: %u", dwValue);

LExit:
    return hr;
}

HRESULT DAPI EseSetColumnBool(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __in BOOL fValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    BYTE bValue = fValue ? 0xFF : 0x00;

    jEr = JetSetColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, &bValue, 1, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to set bool value into column of database");

LExit:
    return hr;
}

HRESULT DAPI EseSetColumnString(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __in_z LPCWSTR pwzValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    ULONG cbValueSize = static_cast<ULONG>((wcslen(pwzValue) + 1) * sizeof(WCHAR)); // add 1 for null character, then multiply by size of WCHAR to get bytes

    jEr = JetSetColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, pwzValue, cbValueSize, 0, NULL);
    ExitOnJetFailure1(jEr, hr, "Failed to set string value into column of database: %ls", pwzValue);

LExit:
    return hr;
}

HRESULT DAPI EseSetColumnEmpty(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetSetColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, NULL, 0, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to set empty value into column of database");

LExit:
    return hr;
}

HRESULT DAPI EseGetColumnBinary(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __deref_out_bcount(*piBuffer) BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    ULONG ulActualSize = 0;

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, NULL, 0, &ulActualSize, 0, NULL);
    if (JET_wrnBufferTruncated == jEr)
    {
        jEr = JET_errSuccess;
    }
    ExitOnJetFailure(jEr, hr, "Failed to check size of binary value from record");

    if (NULL == *ppbBuffer)
    {
        *ppbBuffer = reinterpret_cast<BYTE *>(MemAlloc(ulActualSize, FALSE));
        ExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to allocate memory for reading binary value column");
    }
    else
    {
        *ppbBuffer = reinterpret_cast<BYTE *>(MemReAlloc(*ppbBuffer, ulActualSize, FALSE));
        ExitOnNull(*ppbBuffer, hr, E_OUTOFMEMORY, "Failed to reallocate memory for reading binary value column");
    }

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, *ppbBuffer, ulActualSize, NULL, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to retrieve binary value from record");

    *piBuffer = static_cast<SIZE_T>(ulActualSize);

LExit:
    if (FAILED(hr))
    {
        ReleaseNullMem(*ppbBuffer);
    }

    return hr;
}

HRESULT DAPI EseGetColumnDword(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __out DWORD *pdwValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, pdwValue, sizeof(DWORD), NULL, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to retrieve dword value from record");

LExit:
    return hr;
}

HRESULT DAPI EseGetColumnBool(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __out BOOL *pfValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    BYTE bValue = 0;

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, &bValue, 1, NULL, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to retrieve bool value from record");

    if (bValue == 0)
    {
        *pfValue = FALSE;
    }
    else
    {
        *pfValue = TRUE;
    }

LExit:
    return hr;
}

HRESULT DAPI EseGetColumnString(
    __in JET_SESID jsSession,
    __in ESE_TABLE_SCHEMA tsTable,
    __in DWORD dwColumn,
    __out LPWSTR *ppszValue
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    ULONG ulActualSize = 0;

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, NULL, 0, &ulActualSize, 0, NULL);
    if (JET_wrnBufferTruncated == jEr)
    {
        jEr = JET_errSuccess;
    }
    ExitOnJetFailure(jEr, hr, "Failed to check size of string value from record");

    hr = StrAlloc(ppszValue, ulActualSize);
    ExitOnFailure(hr, "Failed to allocate string while retrieving column value");

    jEr = JetRetrieveColumn(jsSession, tsTable.jtTable, tsTable.pcsColumns[dwColumn].jcColumn, *ppszValue, ulActualSize, NULL, 0, NULL);
    ExitOnJetFailure(jEr, hr, "Failed to retrieve string value from record");

LExit:
    return hr;
}

HRESULT DAPI EseBeginQuery(
    __in JET_SESID jsSession,
    __in JET_TABLEID jtTable,
    __in ESE_QUERY_TYPE qtQueryType,
    __out ESE_QUERY_HANDLE *peqhHandle
    )
{
    UNREFERENCED_PARAMETER(jsSession);
    UNREFERENCED_PARAMETER(jtTable);

    HRESULT hr = S_OK;

    *peqhHandle = static_cast<ESE_QUERY*>(MemAlloc(sizeof(ESE_QUERY), TRUE));
    ExitOnNull(*peqhHandle, hr, E_OUTOFMEMORY, "Failed to allocate new query");

    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(*peqhHandle);
    peqHandle->qtQueryType = qtQueryType;
    peqHandle->jsSession = jsSession;
    peqHandle->jtTable = jtTable;

LExit:
    return hr;
}

// Utility function used by other functions to set a query column
HRESULT DAPI SetQueryColumn(
    __in ESE_QUERY_HANDLE eqhHandle,
    __in_bcount(cbData) const void *pvData,
    __in DWORD cbData,
    __in JET_GRBIT jGrb
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);

    if (peqHandle->dwColumns == countof(peqHandle->pvData))
    {
        hr = E_NOTIMPL;
        ExitOnFailure1(hr, "Dutil hasn't implemented support for queries of more than %d columns", countof(peqHandle->pvData));
    }

    if (0 == peqHandle->dwColumns) // If it's the first column, start a new key
    {
        jGrb = jGrb | JET_bitNewKey;
    }

    jEr = JetMakeKey(peqHandle->jsSession, peqHandle->jtTable, pvData, cbData, jGrb);
    ExitOnJetFailure(jEr, hr, "Failed to begin new query");

    // If the query is wildcard, setup the cached copy of pvData
    if (ESE_QUERY_EXACT != peqHandle->qtQueryType)
    {
        peqHandle->pvData[peqHandle->dwColumns] = MemAlloc(cbData, FALSE);
        ExitOnNull(peqHandle->pvData[peqHandle->dwColumns], hr, E_OUTOFMEMORY, "Failed to allocate memory");

        memcpy(peqHandle->pvData[peqHandle->dwColumns], pvData, cbData);

        peqHandle->cbData[peqHandle->dwColumns] = cbData;
    }

    // Increment the number of total columns
    ++peqHandle->dwColumns;

LExit:
    return hr;
}

HRESULT DAPI EseSetQueryColumnBinary(
    __in ESE_QUERY_HANDLE eqhHandle,
    __in_bcount(cbBuffer) const BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in BOOL fFinal // If this is true, all other key columns in the query will be set to "*"
    )
{
    HRESULT hr = S_OK;
    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);
    JET_GRBIT jGrb = 0;

    if (cbBuffer > DWORD_MAX)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    if (fFinal)
    {
        if (ESE_QUERY_FROM_TOP == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnStartLimit;
        }
        else if (ESE_QUERY_FROM_BOTTOM == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnEndLimit;
        }
    }

    hr = SetQueryColumn(eqhHandle, reinterpret_cast<const void *>(pbBuffer), static_cast<DWORD>(cbBuffer), jGrb);
    ExitOnFailure(hr, "Failed to set value of query colum (as binary) to:");

LExit:
    return hr;
}

HRESULT DAPI EseSetQueryColumnDword(
    __in ESE_QUERY_HANDLE eqhHandle,
    __in DWORD dwData,
    __in BOOL fFinal
    )
{
    HRESULT hr = S_OK;
    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);
    JET_GRBIT jGrb = 0;

    if (fFinal)
    {
        if (ESE_QUERY_FROM_TOP == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnStartLimit;
        }
        else if (ESE_QUERY_FROM_BOTTOM == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnEndLimit;
        }
    }

    hr = SetQueryColumn(eqhHandle, (const void *)&dwData, sizeof(DWORD), jGrb);
    ExitOnFailure1(hr, "Failed to set value of query colum (as dword) to: %u", dwData);

LExit:
    return hr;
}

HRESULT DAPI EseSetQueryColumnBool(
    __in ESE_QUERY_HANDLE eqhHandle,
    __in BOOL fValue,
    __in BOOL fFinal
    )
{
    HRESULT hr = S_OK;
    BYTE bByte = fValue ? 0xFF : 0x00;
    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);
    JET_GRBIT jGrb = 0;

    if (fFinal)
    {
        if (ESE_QUERY_FROM_TOP == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnStartLimit;
        }
        else if (ESE_QUERY_FROM_BOTTOM == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnEndLimit;
        }
    }

    hr = SetQueryColumn(eqhHandle, (const void *)&bByte, 1, jGrb);
    ExitOnFailure1(hr, "Failed to set value of query colum (as bool) to: %s", fValue ? "TRUE" : "FALSE");

LExit:
    return hr;
}

HRESULT DAPI EseSetQueryColumnString(
    __in ESE_QUERY_HANDLE eqhHandle,
    __in_z LPCWSTR pszString,
    __in BOOL fFinal
    )
{
    HRESULT hr = S_OK;
    DWORD dwStringSize = 0;
    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);
    JET_GRBIT jGrb = 0;

    dwStringSize = sizeof(WCHAR) * (lstrlenW(pszString) + 1); // Add 1 for null terminator


    if (fFinal)
    {
        if (ESE_QUERY_FROM_TOP == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnStartLimit;
        }
        else if (ESE_QUERY_FROM_BOTTOM == peqHandle->qtQueryType)
        {
            jGrb = jGrb | JET_bitFullColumnEndLimit;
        }
    }

    hr = SetQueryColumn(eqhHandle, (const void *)pszString, dwStringSize, jGrb);
    ExitOnFailure1(hr, "Failed to set value of query colum (as string) to: %ls", pszString);

LExit:
    return hr;
}

HRESULT DAPI EseFinishQuery(
    __in ESE_QUERY_HANDLE eqhHandle
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;

    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);

    if (peqHandle->fIndexRangeSet)
    {
        jEr = JetSetIndexRange(peqHandle->jsSession, peqHandle->jtTable, JET_bitRangeRemove);
        ExitOnJetFailure(jEr, hr, "Failed to release index range");

        peqHandle->fIndexRangeSet = FALSE;
    }

    for (int i=0; i < countof(peqHandle->pvData); ++i)
    {
        ReleaseMem(peqHandle->pvData[i]);
    }

    ReleaseMem(peqHandle);

LExit:
    return hr;
}

HRESULT DAPI EseRunQuery(
    __in ESE_QUERY_HANDLE eqhHandle
    )
{
    HRESULT hr = S_OK;
    JET_ERR jEr = JET_errSuccess;
    JET_GRBIT jGrb = 0;
    JET_GRBIT jGrbSeekType = 0;
    DWORD i;

    ESE_QUERY *peqHandle = static_cast<ESE_QUERY *>(eqhHandle);

    if (ESE_QUERY_EXACT == peqHandle->qtQueryType)
    {
        jEr = JetSeek(peqHandle->jsSession, peqHandle->jtTable, JET_bitSeekEQ);
        ExitOnJetFailure(jEr, hr, "Failed to seek EQ within jet table");
    }
    else
    {
        if (ESE_QUERY_FROM_TOP == peqHandle->qtQueryType)
        {
            jGrbSeekType = JET_bitSeekGE;
        }
        else if (ESE_QUERY_FROM_BOTTOM == peqHandle->qtQueryType)
        {
            jGrbSeekType = JET_bitSeekLE;
        }

        jEr = JetSeek(peqHandle->jsSession, peqHandle->jtTable, jGrbSeekType);
        if (jEr == JET_wrnSeekNotEqual)
        {
            jEr = JET_errSuccess;
        }

        // At this point we've already set our cursor to the beginning of the range of records to select.
        // Now we'll make a key pointing to the end of the range of records to select, so we can call JetSetIndexRange()
        // For a semi-explanation, see this doc page: http://msdn.microsoft.com/en-us/library/aa964799%28EXCHG.10%29.aspx
        for (i = 0; i < peqHandle->dwColumns; ++i)
        {
            if (i == 0)
            {
                jGrb = JET_bitNewKey;
            }
            else
            {
                jGrb = 0;
            }

            // On the last iteration
            if (i == peqHandle->dwColumns - 1)
            {
                jGrb |= JET_bitFullColumnEndLimit;
            }

            jEr = JetMakeKey(peqHandle->jsSession, peqHandle->jtTable, peqHandle->pvData[i], peqHandle->cbData[i], jGrb);
            ExitOnJetFailure(jEr, hr, "Failed to begin new query");
        }

        jEr = JetSetIndexRange(peqHandle->jsSession, peqHandle->jtTable, JET_bitRangeUpperLimit);
        ExitOnJetFailure(jEr, hr, "Failed to set index range");

        peqHandle->fIndexRangeSet = TRUE;

        // Sometimes JetBlue doesn't check if there is a current record when calling the above function (and sometimes it does)
        // So, let's check if there is a current record before returning (by reading the first byte of one).
        jEr = JetMove(peqHandle->jsSession, peqHandle->jtTable, 0, 0);
        ExitOnJetFailure(jEr, hr, "Failed to check if there is a current record after query");
    }

LExit:
    return hr;
}
