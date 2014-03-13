//-------------------------------------------------------------------------------------------------
// <copyright file="wcalog.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Windows Installer XML CustomAction utility library logging functions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

/********************************************************************
 IsVerboseLoggingPolicy() - internal helper function to detect if 
                            policy is set for verbose logging.  Does 
                            not require database access.
********************************************************************/
static BOOL IsVerboseLoggingPolicy()
{
    BOOL fVerbose = FALSE;
    HKEY hkey = NULL;
    WCHAR rgwc[16] = { 0 };
    DWORD cb = sizeof(rgwc);
    if (ERROR_SUCCESS == ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"Software\\Policies\\Microsoft\\Windows\\Installer", 0, KEY_QUERY_VALUE, &hkey))
    {
        if (ERROR_SUCCESS == ::RegQueryValueExW(hkey, L"Logging", 0, NULL, reinterpret_cast<BYTE*>(rgwc), &cb))
        {
            for (LPCWSTR pwc = rgwc; (cb / sizeof(WCHAR)) > static_cast<DWORD>(pwc - rgwc) && *pwc; pwc++)
            {
                if (L'v' == *pwc || L'V' == *pwc)
                {
                    fVerbose = TRUE;
                    break; 
                }
            }
        }

        ::RegCloseKey(hkey);
    }
    return fVerbose;
}

/********************************************************************
 IsVerboseLogging() - internal helper function to detect if doing
                      verbose logging.  Checks:
                      1. LOGVERBOSE property.
                      2. MsiLogging property contains 'v'
                      3. Policy from registry.
                      
                      Requires database access.
********************************************************************/
BOOL WIXAPI IsVerboseLogging()
{
    static int iVerbose = -1;
    LPWSTR pwzMsiLogging = NULL;

    if (0 > iVerbose)
    {
        iVerbose = WcaIsPropertySet("LOGVERBOSE");
        if (0 == iVerbose) 
        {
            // if the property wasn't set, check the MsiLogging property (MSI 4.0+)
            HRESULT hr = WcaGetProperty(L"MsiLogging", &pwzMsiLogging);
            ExitOnFailure(hr, "failed to get MsiLogging property");
            int cchMsiLogging = lstrlenW(pwzMsiLogging);
            if (0 < cchMsiLogging)
            {
                for (int i = 0; i < cchMsiLogging; i++)
                {
                    if (L'v' == pwzMsiLogging[i] || L'V' == pwzMsiLogging[i])
                    {
                        iVerbose = 1;
                        break;
                    }
                }
            }

            // last chance: Check the registry to see if the logging policy was turned on
            if (0 == iVerbose && IsVerboseLoggingPolicy()) 
            {
               iVerbose = 1;
            }
        }
    }

LExit:
    ReleaseStr(pwzMsiLogging);
    Assert(iVerbose >= 0);
    return (BOOL)iVerbose;
}

/********************************************************************
 SetVerboseLoggingAtom() - Sets one of two global Atoms to specify
                           if the install should do verbose logging.
                           Communicates the verbose setting to 
                           deferred CAs.
                           Set a negative case atom so that we can
                           distinguish between an unset atom and the
                           non-verbose case.  This helps prevent the
                           expensive regkey lookup for non-verbose.
********************************************************************/
HRESULT WIXAPI SetVerboseLoggingAtom(BOOL bValue)
{
    HRESULT hr = S_OK;
    ATOM atomVerbose = 0;

    atomVerbose = ::GlobalFindAtomW(L"WcaVerboseLogging");
    if (0 == atomVerbose &&  bValue)
    {
        atomVerbose = ::GlobalAddAtomW(L"WcaVerboseLogging");
        ExitOnNullWithLastError(atomVerbose, hr, "Failed to create WcaVerboseLogging global atom.");
    }
    else if (0 != atomVerbose && !bValue)
    {
        ::SetLastError(ERROR_SUCCESS);
        ::GlobalDeleteAtom(atomVerbose);
        ExitOnLastError(hr, "Failed to delete WcaVerboseLogging global atom.");
    }

    atomVerbose = ::GlobalFindAtomW(L"WcaNotVerboseLogging");
    if (0 == atomVerbose && !bValue)
    {
        atomVerbose = ::GlobalAddAtomW(L"WcaNotVerboseLogging");
        ExitOnNullWithLastError(atomVerbose, hr, "Failed to create WcaNotVerboseLogging global atom.");
    }
    else if (0 != atomVerbose && bValue)
    {
        ::SetLastError(ERROR_SUCCESS);
        ::GlobalDeleteAtom(atomVerbose);
        ExitOnLastError(hr, "Failed to delete WcaNotVerboseLogging global atom.");
    }

LExit:
    return hr;
}

/********************************************************************
 IsVerboseLoggingLite() - internal helper function to detect if atom was
                          previously set to specify verbose logging.
                          Falls back on policy for an installer that is
                          unable to set the atom (no immediate CAs).
                      
                          Does not require database access.
********************************************************************/
static BOOL IsVerboseLoggingLite()
{
    ATOM atomVerbose = ::GlobalFindAtomW(L"WcaVerboseLogging");
    if (0 != atomVerbose)
    {
        return TRUE;
    }

    atomVerbose = ::GlobalFindAtomW(L"WcaNotVerboseLogging");
    if (0 != atomVerbose)
    {
        return FALSE;
    }

    return IsVerboseLoggingPolicy();
}

/********************************************************************
 WcaLog() - outputs trace and log info

*******************************************************************/
extern "C" void __cdecl WcaLog(
    __in LOGLEVEL llv,
    __in_z __format_string PCSTR fmt, 
    ...
    )
{
    static char szFmt[LOG_BUFFER];
    static char szBuf[LOG_BUFFER];
    static bool fInLogPrint = false;

    // prevent re-entrant logprints.  (recursion issues between assert/logging code)
    if (fInLogPrint)
        return;
    fInLogPrint = true;

    if (LOGMSG_STANDARD == llv || 
        (LOGMSG_VERBOSE == llv && IsVerboseLoggingLite())
#ifdef DEBUG
        || LOGMSG_TRACEONLY == llv
#endif
        )
    {
        va_list args;
        va_start(args, fmt);

        LPCSTR szLogName = WcaGetLogName();
        if (szLogName[0] != 0)
            StringCchPrintfA(szFmt, countof(szFmt), "%s:  %s", szLogName, fmt);
        else
            StringCchCopyA(szFmt, countof(szFmt), fmt);

        StringCchVPrintfA(szBuf, countof(szBuf), szFmt, args);
        va_end(args);

#ifdef DEBUG
        // always write to the log in debug
#else
        if (llv == LOGMSG_STANDARD || (llv == LOGMSG_VERBOSE && IsVerboseLoggingLite()))
#endif
        {
            PMSIHANDLE hrec = MsiCreateRecord(1);

            ::MsiRecordSetStringA(hrec, 0, szBuf);
            // TODO:  Recursion on failure.  May not be safe to assert from here.
            WcaProcessMessage(INSTALLMESSAGE_INFO, hrec);
        }

#if DEBUG
        StringCchCatA(szBuf, countof(szBuf), "\n");
        OutputDebugStringA(szBuf);
#endif
    }

    fInLogPrint = false;
    return;
}


/********************************************************************
 WcaDisplayAssert() - called before Assert() dialog shows

 NOTE: writes the assert string to the MSI log
********************************************************************/
extern "C" BOOL WIXAPI WcaDisplayAssert(
    __in LPCSTR sz
    )
{
    WcaLog(LOGMSG_STANDARD, "Debug Assert Message: %s", sz);
    return TRUE;
}


/********************************************************************
 WcaLogError() - called before ExitOnXXX() macro exists the function

 NOTE: writes the hresult and error string to the MSI log
********************************************************************/
extern "C" void WcaLogError(
    __in HRESULT hr,
    __in LPCSTR szMessage,
    ...
    )
{
    char szBuffer[LOG_BUFFER];
    va_list dots;

    va_start(dots, szMessage);
    StringCchVPrintfA(szBuffer, countof(szBuffer), szMessage, dots);
    va_end(dots);

    // log the message if using Wca common layer
    if (WcaIsInitialized())
        WcaLog(LOGMSG_STANDARD, "Error 0x%x: %s", hr, szBuffer);
}