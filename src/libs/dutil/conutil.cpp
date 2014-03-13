//-------------------------------------------------------------------------------------------------
// <copyright file="conutil.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Console helper functions.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


static HANDLE vhStdIn = INVALID_HANDLE_VALUE;
static HANDLE vhStdOut = INVALID_HANDLE_VALUE;
static BOOL vfConsoleIn = FALSE;
static BOOL vfConsoleOut = FALSE;
static CONSOLE_SCREEN_BUFFER_INFO vcsbiInfo;


extern "C" HRESULT DAPI ConsoleInitialize()
{
    Assert(INVALID_HANDLE_VALUE == vhStdOut);
    HRESULT hr = S_OK;
    UINT er;

    vhStdIn = ::GetStdHandle(STD_INPUT_HANDLE);
    if (INVALID_HANDLE_VALUE == vhStdIn)
    {
        ExitOnLastError(hr, "failed to open stdin");
    }

    vhStdOut = ::GetStdHandle(STD_OUTPUT_HANDLE);
    if (INVALID_HANDLE_VALUE == vhStdOut)
    {
        ExitOnLastError(hr, "failed to open stdout");
    }

    // check if we have a std in on the console
    if (::GetConsoleScreenBufferInfo(vhStdIn, &vcsbiInfo))
    {
        vfConsoleIn = TRUE;
    }
    else
    {
        er = ::GetLastError();
        if (ERROR_INVALID_HANDLE == er)
        {
            vfConsoleIn= FALSE;
            hr = S_OK;
        }
        else
        {
            ExitOnWin32Error(er, hr, "failed to get input console screen buffer info");
        }
    }

    if (::GetConsoleScreenBufferInfo(vhStdOut, &vcsbiInfo))
    {
        vfConsoleOut = TRUE;
    }
    else   // no console
    {
        memset(&vcsbiInfo, 0, sizeof(vcsbiInfo));
        er = ::GetLastError();
        if (ERROR_INVALID_HANDLE == er)
        {
            vfConsoleOut = FALSE;
            hr = S_OK;
        }
        else
        {
            ExitOnWin32Error(er, hr, "failed to get output console screen buffer info");
        }
    }

LExit:
    if (FAILED(hr))
    {
        if (INVALID_HANDLE_VALUE != vhStdOut)
        {
            ::CloseHandle(vhStdOut);
        }

        if (INVALID_HANDLE_VALUE != vhStdIn && vhStdOut != vhStdIn)
        {
            ::CloseHandle(vhStdIn);
        }

        vhStdOut = INVALID_HANDLE_VALUE;
        vhStdIn = INVALID_HANDLE_VALUE;
    }

    return hr;
}


extern "C" void DAPI ConsoleUninitialize()
{
    memset(&vcsbiInfo, 0, sizeof(vcsbiInfo));

    if (INVALID_HANDLE_VALUE != vhStdOut)
    {
        ::CloseHandle(vhStdOut);
    }

    if (INVALID_HANDLE_VALUE != vhStdIn && vhStdOut != vhStdIn)
    {
        ::CloseHandle(vhStdIn);
    }

    vhStdOut = INVALID_HANDLE_VALUE;
    vhStdIn = INVALID_HANDLE_VALUE;
}


extern "C" void DAPI ConsoleGreen()
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    if (vfConsoleOut)
    {
        ::SetConsoleTextAttribute(vhStdOut, FOREGROUND_GREEN | FOREGROUND_INTENSITY);
    }
}


extern "C" void DAPI ConsoleRed()
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    if (vfConsoleOut)
    {
        ::SetConsoleTextAttribute(vhStdOut, FOREGROUND_RED | FOREGROUND_INTENSITY);
    }
}


extern "C" void DAPI ConsoleYellow()
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    if (vfConsoleOut)
    {
        ::SetConsoleTextAttribute(vhStdOut, FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY);
    }
}


extern "C" void DAPI ConsoleNormal()
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    if (vfConsoleOut)
    {
        ::SetConsoleTextAttribute(vhStdOut, vcsbiInfo.wAttributes);
    }
}


/********************************************************************
 ConsoleWrite - full color printfA without libc

 NOTE: use FormatMessage formatting ("%1" or "%1!d!") not plain printf formatting ("%ls" or "%d")
       assumes already in normal color and resets the screen to normal color
********************************************************************/
extern "C" HRESULT DAPI ConsoleWrite(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    LPSTR pszOutput = NULL;
    DWORD cchOutput = 0;
    DWORD cbWrote = 0;
    DWORD cbTotal = 0;

    // set the color
    switch (cc)
    {
    case CONSOLE_COLOR_NORMAL: break;   // do nothing
    case CONSOLE_COLOR_RED: ConsoleRed(); break;
    case CONSOLE_COLOR_YELLOW: ConsoleYellow(); break;
    case CONSOLE_COLOR_GREEN: ConsoleGreen(); break;
    }

    va_list args;
    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(&pszOutput, szFormat, args);
    va_end(args);
    ExitOnFailure1(hr, "failed to format message: \"%s\"", szFormat);

    cchOutput = lstrlenA(pszOutput);
    while (cbTotal < (sizeof(*pszOutput) * cchOutput))
    {
        if (!::WriteFile(vhStdOut, reinterpret_cast<BYTE*>(pszOutput) + cbTotal, cchOutput * sizeof(*pszOutput) - cbTotal, &cbWrote, NULL))
        {
            ExitOnLastError1(hr, "failed to write output to console: %s", pszOutput);
        }

        cbTotal += cbWrote;
    }

    // reset the color to normal
    if (CONSOLE_COLOR_NORMAL != cc)
    {
        ConsoleNormal();
    }

LExit:
    ReleaseStr(pszOutput);
    return hr;
}


/********************************************************************
 ConsoleWriteLine - full color printfA plus newline without libc

 NOTE: use FormatMessage formatting ("%1" or "%1!d!") not plain printf formatting ("%ls" or "%d")
       assumes already in normal color and resets the screen to normal color
********************************************************************/
extern "C" HRESULT DAPI ConsoleWriteLine(
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdOut, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    LPSTR pszOutput = NULL;
    DWORD cchOutput = 0;
    DWORD cbWrote = 0;
    DWORD cbTotal = 0;
    LPCSTR szNewLine = "\r\n";

    // set the color
    switch (cc)
    {
    case CONSOLE_COLOR_NORMAL: break;   // do nothing
    case CONSOLE_COLOR_RED: ConsoleRed(); break;
    case CONSOLE_COLOR_YELLOW: ConsoleYellow(); break;
    case CONSOLE_COLOR_GREEN: ConsoleGreen(); break;
    }

    va_list args;
    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(&pszOutput, szFormat, args);
    va_end(args);
    ExitOnFailure1(hr, "failed to format message: \"%s\"", szFormat);

    //
    // write the string
    //
    cchOutput = lstrlenA(pszOutput);
    while (cbTotal < (sizeof(*pszOutput) * cchOutput))
    {
        if (!::WriteFile(vhStdOut, reinterpret_cast<BYTE*>(pszOutput) + cbTotal, cchOutput * sizeof(*pszOutput) - cbTotal, &cbWrote, NULL))
            ExitOnLastError1(hr, "failed to write output to console: %s", pszOutput);

        cbTotal += cbWrote;
    }

    //
    // write the newline
    //
    if (!::WriteFile(vhStdOut, reinterpret_cast<const BYTE*>(szNewLine), 2, &cbWrote, NULL))
    {
        ExitOnLastError(hr, "failed to write newline to console");
    }

    // reset the color to normal
    if (CONSOLE_COLOR_NORMAL != cc)
    {
        ConsoleNormal();
    }

LExit:
    ReleaseStr(pszOutput);
    return hr;
}


/********************************************************************
 ConsoleWriteError - display an error to the screen

 NOTE: use FormatMessage formatting ("%1" or "%1!d!") not plain printf formatting ("%s" or "%d")
********************************************************************/
HRESULT ConsoleWriteError(
    HRESULT hrError,
    CONSOLE_COLOR cc,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr  = S_OK;
    LPSTR pszMessage = NULL;

    va_list args;
    va_start(args, szFormat);
    hr = StrAnsiAllocFormattedArgs(&pszMessage, szFormat, args);
    va_end(args);
    ExitOnFailure1(hr, "failed to format error message: \"%s\"", szFormat);

    if (FAILED(hrError))
    {
        hr = ConsoleWriteLine(cc, "Error 0x%x: %s", hrError, pszMessage);
    }
    else
    {
        hr = ConsoleWriteLine(cc, "Error: %s", pszMessage);
    }

LExit:
    ReleaseStr(pszMessage);
    return hr;
}


/********************************************************************
 ConsoleReadW - get console input without libc

 NOTE: only supports reading ANSI characters
********************************************************************/
extern "C" HRESULT DAPI ConsoleReadW(
    __deref_out_z LPWSTR* ppwzBuffer
    )
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdIn, "ConsoleInitialize() has not been called");
    Assert(ppwzBuffer);

    HRESULT hr = S_OK;
    LPSTR psz = NULL;
    DWORD cch = 0;
    DWORD cchRead = 0;
    DWORD cchTotalRead = 0;

    cch  = 64;
    hr = StrAnsiAlloc(&psz, cch);
    ExitOnFailure(hr, "failed to allocate memory to read from console");

    // loop until we read the \r\n from the console
    for (;;)
    {
        // read one character at a time, since that seems to be the only way to make this work
        if (!::ReadFile(vhStdIn, psz + cchTotalRead, 1, &cchRead, NULL))
            ExitOnLastError(hr, "failed to read string from console");

        cchTotalRead += cchRead;
        if (1 < cchTotalRead && '\r' == psz[cchTotalRead - 2] || '\n' == psz[cchTotalRead - 1])
        {
            psz[cchTotalRead - 2] = '\0';  // chop off the \r\n
            break;
        }
        else if (0 == cchRead)  // nothing more was read
        {
            psz[cchTotalRead] = '\0';  // null termintate and bail
            break;
        }

        if (cchTotalRead == cch)
        {
            cch *= 2;   // double everytime we run out of space
            hr = StrAnsiAlloc(&psz, cch);
            ExitOnFailure(hr, "failed to allocate memory to read from console");
        }
    }

    hr = StrAllocStringAnsi(ppwzBuffer, psz, 0, CP_ACP);

LExit:
    ReleaseStr(psz);
    return hr;
}


/********************************************************************
 ConsoleReadNonBlockingW - Read from the console without blocking
 Won't work for redirected files (exe < txtfile), but will work for stdin redirected to 
 an anonymous or named pipe

 if (fReadLine), stop reading immediately when \r\n is found
*********************************************************************/
extern "C" HRESULT DAPI ConsoleReadNonBlockingW(
    __deref_out_ecount_opt(*pcchSize) LPWSTR* ppwzBuffer,
    __out DWORD* pcchSize,
    BOOL fReadLine
    )
{
    Assert(INVALID_HANDLE_VALUE != vhStdIn && pcchSize);
    HRESULT hr = S_OK;

    LPSTR psz = NULL;

    ExitOnNull(ppwzBuffer, hr, E_INVALIDARG, "Failed to read from console because buffer was not provided");

    DWORD dwRead;
    DWORD dwNumInput;

    DWORD cchTotal = 0;
    DWORD cch = 8;

    DWORD cchRead = 0;
    DWORD cchTotalRead = 0;
    
    DWORD dwIndex = 0;
    DWORD er;

    INPUT_RECORD ir;
    WCHAR chIn;

    *ppwzBuffer = NULL;
    *pcchSize = 0;

    // If we really have a handle to stdin, and not the end of a pipe
    if (!PeekNamedPipe(vhStdIn, NULL, 0, NULL, &dwRead, NULL))
    {
        er = ::GetLastError();
        if (ERROR_INVALID_HANDLE != er)
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(er));
        }

        if (!GetNumberOfConsoleInputEvents(vhStdIn, &dwRead))
        {
            ExitOnLastError(hr, "failed to peek at console input");
        }

        if (0 == dwRead)
        {
            ExitFunction1(hr = S_FALSE);
        }

        for (/* dwRead from num of input events */; dwRead > 0; dwRead--)
        {
            if (!ReadConsoleInputW(vhStdIn, &ir, 1, &dwNumInput))
            {
                ExitOnLastError(hr, "Failed to read input from console");
            }

            // If what we have is a KEY_EVENT, and that event signifies keyUp, we're interested
            if (KEY_EVENT == ir.EventType && FALSE == ir.Event.KeyEvent.bKeyDown)
            {
                chIn = ir.Event.KeyEvent.uChar.UnicodeChar;

                if (0 == cchTotal)
                {
                    cchTotal = cch;
                    cch *= 2;
                    StrAlloc(ppwzBuffer, cch);
                }

                (*ppwzBuffer)[dwIndex] = chIn;

                if (fReadLine && (L'\r' == (*ppwzBuffer)[dwIndex - 1] && L'\n' == (*ppwzBuffer)[dwIndex]))
                {
                    *ppwzBuffer[dwIndex - 1] = L'\0';
                    dwIndex -= 1;
                    break;
                }

                ++dwIndex;
                cchTotal--;
            }
        }

        *pcchSize = dwIndex;
    }
    else
    {
        // otherwise, the peek worked, and we have the end of a pipe
        if (0 == dwRead)
            ExitFunction1(hr = S_FALSE);

        cch = 8;
        hr = StrAnsiAlloc(&psz, cch);
        ExitOnFailure(hr, "failed to allocate memory to read from console");

        for (/*dwRead from PeekNamedPipe*/; dwRead > 0; dwRead--)
        {
            // read one character at a time, since that seems to be the only way to make this work
            if (!::ReadFile(vhStdIn, psz + cchTotalRead, 1, &cchRead, NULL))
            {
                ExitOnLastError(hr, "failed to read string from console");
            }

            cchTotalRead += cchRead;
            if (fReadLine && '\r' == psz[cchTotalRead - 1] && '\n' == psz[cchTotalRead])
            {
                psz[cchTotalRead - 1] = '\0';  // chop off the \r\n
                cchTotalRead -= 1;
                break;
            }
            else if (0 == cchRead)  // nothing more was read
            {
                psz[cchTotalRead] = '\0';  // null termintate and bail
                break;
            }

            if (cchTotalRead == cch)
            {
                cch *= 2;   // double everytime we run out of space
                hr = StrAnsiAlloc(&psz, cch);
                ExitOnFailure(hr, "failed to allocate memory to read from console");
            }
        }

        *pcchSize = cchTotalRead;
        hr = StrAllocStringAnsi(ppwzBuffer, psz, cchTotalRead, CP_ACP);
    }
    
LExit:
    ReleaseStr(psz);

    return hr;
}


/********************************************************************
 ConsoleReadStringA - get console input without libc

*********************************************************************/
extern "C" HRESULT DAPI ConsoleReadStringA(
    __deref_out_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPSTR* ppszCharBuffer,
    CONST DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    )
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdIn, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    if (ppszCharBuffer && (pcchNumCharReturn || cchCharBuffer < 2))
    {
        DWORD iRead = 1;
        DWORD iReadCharTotal = 0;
        if (ppszCharBuffer && *ppszCharBuffer == NULL)
        {
            do
            {
                hr = StrAnsiAlloc(ppszCharBuffer, cchCharBuffer * iRead);
                ExitOnFailure(hr, "failed to allocate memory for ConsoleReadStringW");
                // ReadConsoleW will not return until <Return>, the last two chars are 13 and 10.
                if (!::ReadConsoleA(vhStdIn, *ppszCharBuffer + iReadCharTotal, cchCharBuffer, pcchNumCharReturn, NULL) || *pcchNumCharReturn == 0)
                {
                    ExitOnLastError(hr, "failed to read string from console");
                }
                iReadCharTotal += *pcchNumCharReturn;
                iRead += 1;
            }
            while((*ppszCharBuffer)[iReadCharTotal - 1] != 10 || (*ppszCharBuffer)[iReadCharTotal - 2] != 13);
            *pcchNumCharReturn = iReadCharTotal;
        }
        else
        {
            if (!::ReadConsoleA(vhStdIn, *ppszCharBuffer, cchCharBuffer, pcchNumCharReturn, NULL) ||
                *pcchNumCharReturn > cchCharBuffer || *pcchNumCharReturn == 0)
            {
                ExitOnLastError(hr, "failed to read string from console");
            }
            if ((*ppszCharBuffer)[*pcchNumCharReturn - 1] != 10 ||
                (*ppszCharBuffer)[*pcchNumCharReturn - 2] != 13)
            {
                // need read more
                hr = ERROR_MORE_DATA;
            }
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

LExit:
    return hr;
}

/********************************************************************
 ConsoleReadStringW - get console input without libc

*********************************************************************/
extern "C" HRESULT DAPI ConsoleReadStringW(
    __deref_out_ecount_part(cchCharBuffer,*pcchNumCharReturn) LPWSTR* ppwzCharBuffer,
    const DWORD cchCharBuffer,
    __out DWORD* pcchNumCharReturn
    )
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdIn, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    if (ppwzCharBuffer && (pcchNumCharReturn || cchCharBuffer < 2))
    {
        DWORD iRead = 1;
        DWORD iReadCharTotal = 0;
        if (*ppwzCharBuffer == NULL)
        {
            do
            {
                hr = StrAlloc(ppwzCharBuffer, cchCharBuffer * iRead);
                ExitOnFailure(hr, "failed to allocate memory for ConsoleReadStringW");
                // ReadConsoleW will not return until <Return>, the last two chars are 13 and 10.
                if (!::ReadConsoleW(vhStdIn, *ppwzCharBuffer + iReadCharTotal, cchCharBuffer, pcchNumCharReturn, NULL) || *pcchNumCharReturn == 0)
                {
                    ExitOnLastError(hr, "failed to read string from console");
                }
                iReadCharTotal += *pcchNumCharReturn;
                iRead += 1;
            }
            while((*ppwzCharBuffer)[iReadCharTotal - 1] != 10 || (*ppwzCharBuffer)[iReadCharTotal - 2] != 13);
            *pcchNumCharReturn = iReadCharTotal;
        }
        else
        {
            if (!::ReadConsoleW(vhStdIn, *ppwzCharBuffer, cchCharBuffer, pcchNumCharReturn, NULL) ||
                *pcchNumCharReturn > cchCharBuffer || *pcchNumCharReturn == 0)
            {
                ExitOnLastError(hr, "failed to read string from console");
            }
            if ((*ppwzCharBuffer)[*pcchNumCharReturn - 1] != 10 ||
                (*ppwzCharBuffer)[*pcchNumCharReturn - 2] != 13)
            {
                // need read more
                hr = ERROR_MORE_DATA;
            }
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

LExit:
    return hr;
}

/********************************************************************
 ConsoleSetReadHidden - set console input no echo

*********************************************************************/
extern "C" HRESULT DAPI ConsoleSetReadHidden(void)
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdIn, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    ::FlushConsoleInputBuffer(vhStdIn);
    if (!::SetConsoleMode(vhStdIn, ENABLE_LINE_INPUT | ENABLE_PROCESSED_INPUT))
    {
        ExitOnLastError(hr, "failed to set console input mode to be hidden");
    }

LExit:
    return hr;
}

/********************************************************************
 ConsoleSetReadNormal - reset to echo

*********************************************************************/
extern "C" HRESULT DAPI ConsoleSetReadNormal(void)
{
    AssertSz(INVALID_HANDLE_VALUE != vhStdIn, "ConsoleInitialize() has not been called");
    HRESULT hr = S_OK;
    if (!::SetConsoleMode(vhStdIn, ENABLE_LINE_INPUT | ENABLE_ECHO_INPUT | ENABLE_PROCESSED_INPUT | ENABLE_MOUSE_INPUT))
    {
        ExitOnLastError(hr, "failed to set console input mode to be normal");
    }

LExit:
    return hr;
}

