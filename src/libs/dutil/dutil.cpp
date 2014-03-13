//-------------------------------------------------------------------------------------------------
// <copyright file="dutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Utility layer that provides standard support for asserts, exit macros
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// No need for OACR to warn us about using non-unicode APIs in this file.
#pragma prefast(disable:25068)

// Asserts & Tracing

const int DUTIL_STRING_BUFFER = 1024;
static HMODULE Dutil_hAssertModule = NULL;
static DUTIL_ASSERTDISPLAYFUNCTION Dutil_pfnDisplayAssert = NULL;
static BOOL Dutil_fNoAsserts = FALSE;
static REPORT_LEVEL Dutil_rlCurrentTrace = REPORT_STANDARD;
static BOOL Dutil_fTraceFilenames = FALSE;


/*******************************************************************
Dutil_SetAssertModule

*******************************************************************/
extern "C" void DAPI Dutil_SetAssertModule(
    __in HMODULE hAssertModule
    )
{
    Dutil_hAssertModule = hAssertModule;
}


/*******************************************************************
Dutil_SetAssertDisplayFunction

*******************************************************************/
extern "C" void DAPI Dutil_SetAssertDisplayFunction(
    __in DUTIL_ASSERTDISPLAYFUNCTION pfn
    )
{
    Dutil_pfnDisplayAssert = pfn;
}


/*******************************************************************
Dutil_AssertMsg

*******************************************************************/
extern "C" void DAPI Dutil_AssertMsg(
    __in_z LPCSTR szMessage
    )
{
    static BOOL fInAssert = FALSE; // TODO: make this thread safe (this is a cheap hack to prevent re-entrant Asserts)

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    int id = IDRETRY;
    HKEY hkDebug = NULL;
    HANDLE hAssertFile = INVALID_HANDLE_VALUE;
    char szPath[MAX_PATH] = { };
    DWORD cch = 0;

    if (fInAssert)
    {
        return;
    }
    fInAssert = TRUE;

    char szMsg[DUTIL_STRING_BUFFER];
    hr = ::StringCchCopyA(szMsg, countof(szMsg), szMessage);
    ExitOnFailure(hr, "failed to copy message while building assert message");

    if (Dutil_pfnDisplayAssert)
    {
        // call custom function to display the assert string
        if (!Dutil_pfnDisplayAssert(szMsg))
        {
            ExitFunction();
        }
    }
    else
    {
        OutputDebugStringA(szMsg);
    }

    if (!Dutil_fNoAsserts)
    {
        er = ::RegOpenKeyExW(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Delivery\\Debug", 0, KEY_QUERY_VALUE, &hkDebug);
        if (ERROR_SUCCESS == er)
        {
            cch = countof(szPath);
            er = ::RegQueryValueExA(hkDebug, "DeliveryAssertsLog", NULL, NULL, reinterpret_cast<BYTE*>(szPath), &cch);
            szPath[countof(szPath) - 1] = '\0'; // ensure string is null terminated since registry won't guarantee that.
            if (ERROR_SUCCESS == er)
            {
                hAssertFile = ::CreateFileA(szPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
                if (INVALID_HANDLE_VALUE != hAssertFile)
                {
                    ::SetFilePointer(hAssertFile, 0, 0, FILE_END);
                    ::StringCchCatA(szMsg, countof(szMsg), "\r\n");
                    ::WriteFile(hAssertFile, szMsg, lstrlenA(szMsg), &cch, NULL);
                }
            }
        }

        // if anything went wrong while fooling around with the registry, just show the usual assert dialog box
        if (ERROR_SUCCESS != er)
        {
            hr = ::StringCchCatA(szMsg, countof(szMsg), "\nAbort=Debug, Retry=Skip, Ignore=Skip all");
            ExitOnFailure(hr, "failed to concat string while building assert message");

            id = ::MessageBoxA(0, szMsg, "Debug Assert Message",
                MB_SERVICE_NOTIFICATION | MB_TOPMOST | 
                MB_DEFBUTTON2 | MB_ABORTRETRYIGNORE);
        }
    }

    if (id == IDABORT)
    {
        if (Dutil_hAssertModule)
        {
            ::GetModuleFileNameA(Dutil_hAssertModule, szPath, countof(szPath));

            hr = ::StringCchPrintfA(szMsg, countof(szMsg), "Module is running from: %s\nIf you are not using pdb-stamping, place your PDB near the module and attach to process id: %d (0x%x)", szPath, ::GetCurrentProcessId(), ::GetCurrentProcessId());
            if (SUCCEEDED(hr))
            {
                ::MessageBoxA(0, szMsg, "Debug Assert Message", MB_SERVICE_NOTIFICATION | MB_TOPMOST | MB_OK);
            }
        }

        ::DebugBreak();
    }
    else if (id == IDIGNORE)
    {
        Dutil_fNoAsserts = TRUE;
    }

LExit:
    ReleaseFileHandle(hAssertFile);
    ReleaseRegKey(hkDebug);
    fInAssert = FALSE;
}


/*******************************************************************
Dutil_Assert

*******************************************************************/
extern "C" void DAPI Dutil_Assert(
    __in_z LPCSTR szFile, 
    __in int iLine
    )
{
    HRESULT hr = S_OK;
    char szMessage[DUTIL_STRING_BUFFER] = { };
    hr = ::StringCchPrintfA(szMessage, countof(szMessage), "Assertion failed in %s, %i", szFile, iLine);
    if (SUCCEEDED(hr))
    {
        Dutil_AssertMsg(szMessage);
    }
    else
    {
        Dutil_AssertMsg("Assert failed to build string");
    }
}


/*******************************************************************
Dutil_AssertSz

*******************************************************************/
extern "C" void DAPI Dutil_AssertSz(
    __in_z LPCSTR szFile, 
    __in int iLine, 
    __in_z __format_string LPCSTR szMsg
    )
{
    HRESULT hr = S_OK;
    char szMessage[DUTIL_STRING_BUFFER] = { };

    hr = ::StringCchPrintfA(szMessage, countof(szMessage), "Assertion failed in %s, %i\n%s", szFile, iLine, szMsg);
    if (SUCCEEDED(hr))
    {
        Dutil_AssertMsg(szMessage);
    }
    else
    {
        Dutil_AssertMsg("Assert failed to build string");
    }
}


/*******************************************************************
Dutil_TraceSetLevel

*******************************************************************/
extern "C" void DAPI Dutil_TraceSetLevel(
    __in REPORT_LEVEL rl,
    __in BOOL fTraceFilenames
    )
{
    Dutil_rlCurrentTrace = rl;
    Dutil_fTraceFilenames = fTraceFilenames;
}


/*******************************************************************
Dutil_TraceGetLevel

*******************************************************************/
extern "C" REPORT_LEVEL DAPI Dutil_TraceGetLevel()
{
    return Dutil_rlCurrentTrace;
}


/*******************************************************************
Dutil_Trace

*******************************************************************/
extern "C" void DAPI Dutil_Trace(
    __in_z LPCSTR szFile, 
    __in int iLine, 
    __in REPORT_LEVEL rl, 
    __in_z __format_string LPCSTR szFormat, 
    ...
    )
{
    AssertSz(REPORT_NONE != rl, "REPORT_NONE is not a valid tracing level");

    HRESULT hr = S_OK;
    char szOutput[DUTIL_STRING_BUFFER] = { };
    char szMsg[DUTIL_STRING_BUFFER] = { };

    if (Dutil_rlCurrentTrace < rl)
    {
        return;
    }

    va_list args;
    va_start(args, szFormat);
    hr = ::StringCchVPrintfA(szOutput, countof(szOutput), szFormat, args);
    va_end(args);

    if (SUCCEEDED(hr))
    {
        LPCSTR szPrefix = "Trace/u";
        switch (rl)
        {
        case REPORT_STANDARD:
            szPrefix = "Trace/s";
            break;
        case REPORT_VERBOSE:
            szPrefix = "Trace/v";
            break;
        case REPORT_DEBUG:
            szPrefix = "Trace/d";
            break;
        }

        if (Dutil_fTraceFilenames)
        {
            hr = ::StringCchPrintfA(szMsg, countof(szMsg), "%s [%s,%d]: %s\r\n", szPrefix, szFile, iLine, szOutput);
        }
        else
        {
            hr = ::StringCchPrintfA(szMsg, countof(szMsg), "%s: %s\r\n", szPrefix, szOutput);
        }

        if (SUCCEEDED(hr))
        {
            OutputDebugStringA(szMsg);
        }
        // else fall through to the case below
    }

    if (FAILED(hr))
    {
        if (Dutil_fTraceFilenames)
        {
            ::StringCchPrintfA(szMsg, countof(szMsg), "Trace [%s,%d]: message too long, skipping\r\n", szFile, iLine);
        }
        else
        {
            ::StringCchPrintfA(szMsg, countof(szMsg), "Trace: message too long, skipping\r\n");
        }

        szMsg[countof(szMsg)-1] = '\0';
        OutputDebugStringA(szMsg);
    }
}


/*******************************************************************
Dutil_TraceError

*******************************************************************/
extern "C" void DAPI Dutil_TraceError(
    __in_z LPCSTR szFile, 
    __in int iLine, 
    __in REPORT_LEVEL rl, 
    __in HRESULT hrError, 
    __in_z __format_string LPCSTR szFormat, 
    ...
    )
{
    HRESULT hr = S_OK;
    char szOutput[DUTIL_STRING_BUFFER] = { };
    char szMsg[DUTIL_STRING_BUFFER] = { };

    // if this is NOT an error report and we're not logging at this level, bail
    if (REPORT_ERROR != rl && Dutil_rlCurrentTrace < rl)
    {
        return;
    }

    va_list args;
    va_start(args, szFormat);
    hr = ::StringCchVPrintfA(szOutput, countof(szOutput), szFormat, args);
    va_end(args);

    if (SUCCEEDED(hr))
    {
        if (Dutil_fTraceFilenames)
        {
            if (FAILED(hrError))
            {
                hr = ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x [%s,%d]: %s\r\n", hrError, szFile, iLine, szOutput);
            }
            else
            {
                hr = ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError [%s,%d]: %s\r\n", szFile, iLine, szOutput);
            }
        }
        else
        {
            if (FAILED(hrError))
            {
                hr = ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x: %s\r\n", hrError, szOutput);
            }
            else
            {
                hr = ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError: %s\r\n", szOutput);
            }
        }

        if (SUCCEEDED(hr))
        {
            OutputDebugStringA(szMsg);
        }
        // else fall through to the failure case below
    }

    if (FAILED(hr))
    {
        if (Dutil_fTraceFilenames)
        {
            if (FAILED(hrError))
            {
                ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x [%s,%d]: message too long, skipping\r\n", hrError, szFile, iLine);
            }
            else
            {
                ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError [%s,%d]: message too long, skipping\r\n", szFile, iLine);
            }
        }
        else
        {
            if (FAILED(hrError))
            {
                ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError 0x%x: message too long, skipping\r\n", hrError);
            }
            else
            {
                ::StringCchPrintfA(szMsg, countof(szMsg), "TraceError: message too long, skipping\r\n");
            }
        }

        szMsg[countof(szMsg)-1] = '\0';
        OutputDebugStringA(szMsg);
    }
}



/*******************************************************************
Dutil_RootFailure

*******************************************************************/
extern "C" void DAPI Dutil_RootFailure(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in HRESULT hrError
    )
{
#ifndef DEBUG
    UNREFERENCED_PARAMETER(szFile);
    UNREFERENCED_PARAMETER(iLine);
    UNREFERENCED_PARAMETER(hrError);
#endif // DEBUG

    TraceError2(hrError, "Root failure at %s:%d", szFile, iLine);
}

/*******************************************************************
 LoadSystemLibrary - Fully qualifies the path to a module in the
                     Windows system directory and loads it.

 Returns
   E_MODNOTFOUND - The module could not be found.
   * - Another error occured.
********************************************************************/
extern "C" HRESULT DAPI LoadSystemLibrary(
    __in_z LPCWSTR wzModuleName,
    __out HMODULE *phModule
    )
{
    HRESULT hr = LoadSystemLibraryWithPath(wzModuleName, phModule, NULL);
    return hr;
}

/*******************************************************************
 LoadSystemLibraryWithPath - Fully qualifies the path to a module in
                             the Windows system directory and loads it
                             and returns the path

 Returns
   E_MODNOTFOUND - The module could not be found.
   * - Another error occured.
********************************************************************/
extern "C" HRESULT DAPI LoadSystemLibraryWithPath(
    __in_z LPCWSTR wzModuleName,
    __out HMODULE *phModule,
    __deref_out_z_opt LPWSTR* psczPath
    )
{
    HRESULT hr = S_OK;
    DWORD cch = 0;
    WCHAR wzPath[MAX_PATH] = { };

    cch = ::GetSystemDirectoryW(wzPath, MAX_PATH);
    ExitOnNullWithLastError(cch, hr, "Failed to get the Windows system directory.");

    if (L'\\' != wzPath[cch - 1])
    {
        hr = ::StringCchCatNW(wzPath, MAX_PATH, L"\\", 1);
        ExitOnRootFailure(hr, "Failed to terminate the string with a backslash.");
    }

    hr = ::StringCchCatW(wzPath, MAX_PATH, wzModuleName);
    ExitOnRootFailure1(hr, "Failed to create the fully-qualified path to %ls.", wzModuleName);

    *phModule = ::LoadLibraryW(wzPath);
    ExitOnNullWithLastError1(*phModule, hr, "Failed to load the library %ls.", wzModuleName);

    if (psczPath)
    {
        hr = StrAllocString(psczPath, wzPath, MAX_PATH);
        ExitOnFailure(hr, "Failed to copy the path to library.");
    }

LExit:
    return hr;
}
