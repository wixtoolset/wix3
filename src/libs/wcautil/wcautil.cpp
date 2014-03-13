//-------------------------------------------------------------------------------------------------
// <copyright file="wcautil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Windows Installer XML CustomAction utility library.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// globals
HMODULE g_hInstCADLL;

// statics
static BOOL s_fInitialized;
static MSIHANDLE s_hInstall;
static MSIHANDLE s_hDatabase;
static char s_szCustomActionLogName[32];
static UINT s_iRetVal;


/********************************************************************
 WcaGlobalInitialize() - initializes the Wca library, should be
                         called once per custom action Dll during
                         DllMain on DLL_PROCESS_ATTACH

********************************************************************/
extern "C" void WIXAPI WcaGlobalInitialize(
    __in HINSTANCE hInst
    )
{
    g_hInstCADLL = hInst;
    MemInitialize();

    AssertSetModule(g_hInstCADLL);
    AssertSetDisplayFunction(WcaDisplayAssert);
}


/********************************************************************
 WcaGlobalFinalize() - finalizes the Wca library, should be the
                       called once per custom action Dll during
                       DllMain on DLL_PROCESS_DETACH

********************************************************************/
extern "C" void WIXAPI WcaGlobalFinalize()
{
#ifdef DEBUG
    if (WcaIsInitialized())
    {
        CHAR szBuf[2048];
        StringCchPrintfA(szBuf, countof(szBuf), "CustomAction %s called WcaInitialize() but not WcaFinalize()", WcaGetLogName());

        AssertSz(FALSE, szBuf);
    }
#endif
    MemUninitialize();
    g_hInstCADLL = NULL;
}


/********************************************************************
 WcaInitialize() - initializes the Wca framework, should be the first
                   thing called by all CustomActions

********************************************************************/
extern "C" HRESULT WIXAPI WcaInitialize(
    __in MSIHANDLE hInstall,
    __in_z PCSTR szCustomActionLogName
    )
{
    WCHAR wzCAFileName[MAX_PATH] = {0};
    DWORD dwMajorVersion = 0;
    DWORD dwMinorVersion = 0;

    // these statics should be called once per CustomAction invocation.
    // Darwin doesn't preserve DLL state across CustomAction calls so
    // these should always be initialized to NULL.  If that behavior changes
    // we would need to do a careful review of all of our module/global data.
    AssertSz(!s_fInitialized, "WcaInitialize() should only be called once per CustomAction");
    Assert(NULL == s_hInstall);
    Assert(NULL == s_hDatabase);
    Assert(0 == *s_szCustomActionLogName);

    HRESULT hr = S_OK;

    s_fInitialized = TRUE;
    s_iRetVal = ERROR_SUCCESS; // assume all will go well

    s_hInstall = hInstall;
    s_hDatabase = ::MsiGetActiveDatabase(s_hInstall); // may return null if deferred CustomAction

    hr = ::StringCchCopy(s_szCustomActionLogName, countof(s_szCustomActionLogName), szCustomActionLogName);
    ExitOnFailure1(hr, "Failed to copy CustomAction log name: %s", szCustomActionLogName);

    // If we got the database handle i.e.: immediate CA
    if (s_hDatabase)
    {
        hr = SetVerboseLoggingAtom(IsVerboseLogging());
        ExitOnFailure(hr, "Failed to set verbose logging global atom");
    }

    if (!::GetModuleFileNameW(g_hInstCADLL, wzCAFileName, countof(wzCAFileName)))
    {
        ExitWithLastError(hr, "Failed to get module filename");
    }

    FileVersion(wzCAFileName, &dwMajorVersion, &dwMinorVersion);  // Ignore failure, just log 0.0.0.0

    WcaLog(LOGMSG_VERBOSE, "Entering %s in %ls, version %u.%u.%u.%u", szCustomActionLogName, wzCAFileName, (DWORD)HIWORD(dwMajorVersion), (DWORD)LOWORD(dwMajorVersion), (DWORD)HIWORD(dwMinorVersion), (DWORD)LOWORD(dwMinorVersion));

    Assert(s_hInstall);
LExit:
    if (FAILED(hr))
    {
        if (s_hDatabase)
        {
            ::MsiCloseHandle(s_hDatabase);
            s_hDatabase = NULL;
        }

        s_hInstall = NULL;
        s_fInitialized = FALSE;
    }

    return hr;
}


/********************************************************************
 WcaFinalize() - cleans up after the Wca framework, should be the last
                 thing called by all CustomActions

********************************************************************/
extern "C" UINT WIXAPI WcaFinalize(
    __in UINT iReturnValue
    )
{
    AssertSz(!WcaIsWow64Initialized(), "WcaFinalizeWow64() should be called before calling WcaFinalize()");

    // clean up after our initialization
    if (s_hDatabase)
    {
        ::MsiCloseHandle(s_hDatabase);
        s_hDatabase = NULL;
    }

    s_hInstall = NULL;
    s_fInitialized = FALSE;
    *s_szCustomActionLogName = 0;

    // if no error occurred during the processing of the CustomAction return the passed in return value
    // otherwise return the previous failure
    return (ERROR_SUCCESS == s_iRetVal) ? iReturnValue : s_iRetVal;
}


/********************************************************************
 WcaIsInitialized() - determines if WcaInitialize() has been called

********************************************************************/
extern "C" BOOL WIXAPI WcaIsInitialized()
{
    return s_fInitialized;
}


/********************************************************************
 WcaGetInstallHandle() - gets the handle to the active install session

********************************************************************/
extern "C" MSIHANDLE WIXAPI WcaGetInstallHandle()
{
    AssertSz(s_hInstall, "WcaInitialize() should be called before attempting to access the install handle.");
    return s_hInstall;
}


/********************************************************************
 WcaGetDatabaseHandle() - gets the handle to the active database

 NOTE: this function can only be used in immediate CustomActions.
       Deferred CustomActions do not have access to the active
       database.
********************************************************************/
extern "C" MSIHANDLE WIXAPI WcaGetDatabaseHandle()
{
    AssertSz(s_hDatabase, "WcaInitialize() should be called before attempting to access the install handle.  Also note that deferred CustomActions do not have access to the active database.");
    return s_hDatabase;
}


/********************************************************************
 WcaGetLogName() - gets the name of the CustomAction used in logging

********************************************************************/
extern "C" const char* WIXAPI WcaGetLogName()
{
    return s_szCustomActionLogName;
}


/********************************************************************
 WcaSetReturnValue() - sets the value to return from the CustomAction

********************************************************************/
extern "C" void WIXAPI WcaSetReturnValue(
    __in UINT iReturnValue
    )
{
    s_iRetVal = iReturnValue;
}


/********************************************************************
 WcaCancelDetected() - determines if the user has canceled yet

 NOTE: returns true when WcaSetReturnValue() is set to ERROR_INSTALL_USEREXIT
********************************************************************/
extern "C" BOOL WIXAPI WcaCancelDetected()
{
    return ERROR_INSTALL_USEREXIT == s_iRetVal;
}
