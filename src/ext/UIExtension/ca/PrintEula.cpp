// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// Constants
LPCWSTR vcsEulaQuery = L"SELECT `Text` FROM `Control` WHERE `Control`='LicenseText'";


enum eEulaQuery { eqText = 1};
const int IDM_POPULATE = 100;
const int IDM_PRINT = 101;
const int CONTROL_X_COORDINATE = 0;
const int CONTROL_Y_COORDINATE = 0;
const int CONTROL_WIDTH = 500;
const int CONTROL_HEIGHT = 500;
const int ONE_INCH = 1440; // 1440 TWIPS = 1 inch.
const int TEXT_RECORD_POS = 1;
const int STRING_CAPACITY = 512;
const int NO_OF_COPIES = 1;
const LPWSTR WINDOW_CLASS = L"PrintEulaRichText";

//Forward declarations of functions, check the function definitions for the comments
static LRESULT CALLBACK WndProc(__in HWND hWnd, __in UINT message, __in WPARAM wParam, __in LPARAM lParam);
static HRESULT ReadEulaText(__in MSIHANDLE hInstall, __out LPSTR* ppszEulaText);
static DWORD CALLBACK ReadStreamCallback(__in DWORD Cookie, __out LPBYTE pbBuff, __in LONG cb, __out LONG FAR *pcb);
static HRESULT CreateRichTextWindow(__out HWND* phWndMain, __out BOOL* pfRegisteredClass);
static HRESULT PrintRichText(__in HWND hWndMain);
static void Print(__in_opt HWND hWnd);
static void LoadEulaText(__in_opt HWND hWnd);
static void ShowErrorMessage(__in HRESULT hr);

//Global variables
PRINTDLGEXW* vpPrintDlg = NULL; //Parameters for print (needed on both sides of WndProc callbacks)
LPSTR vpszEulaText = NULL;
HRESULT vhr = S_OK; //Global hr, used by the functions called from WndProc to set errorcode


/********************************************************************
 PrintEula - Custom Action entry point

********************************************************************/
extern "C" UINT __stdcall PrintEula(MSIHANDLE hInstall)
{
    //AssertSz(FALSE, "Debug PrintEula");

    HRESULT hr = S_OK;
    HWND hWndMain = NULL;
    HMODULE hRichEdit = NULL;
    BOOL fRegisteredClass = FALSE;

    hr = WcaInitialize(hInstall, "PrintEula");
    ExitOnFailure(hr, "failed to initialize");

    // Initialize then display print dialog.
    vpPrintDlg = (PRINTDLGEXW*)GlobalAlloc(GPTR, sizeof(PRINTDLGEXW)); // MSDN says to allocate on heap.
    ExitOnNullWithLastError(vpPrintDlg, hr, "Failed to allocate memory for print dialog struct.");

    vpPrintDlg->lStructSize = sizeof(PRINTDLGEX);
    vpPrintDlg->hwndOwner = ::FindWindowW(L"MsiDialogCloseClass", NULL);
    vpPrintDlg->Flags = PD_RETURNDC | PD_COLLATE | PD_NOCURRENTPAGE | PD_ALLPAGES | PD_NOPAGENUMS | PD_NOSELECTION;
    vpPrintDlg->nCopies = NO_OF_COPIES;
    vpPrintDlg->nStartPage = START_PAGE_GENERAL;

    hr = ::PrintDlgExW(vpPrintDlg);
    ExitOnFailure(hr, "Failed to show print dialog");

    // If user said they want to print.
    if (PD_RESULT_PRINT == vpPrintDlg->dwResultAction)
    {
        // Get the stream for Eula
        hr = ReadEulaText(hInstall, &vpszEulaText);
        ExitOnFailure(hr, "failed to read Eula text from MSI database");

        // Have to load Rich Edit since we'll be creating a Rich Edit control in the window
        hr = LoadSystemLibrary(L"Riched20.dll", &hRichEdit);
        ExitOnFailure(hr, "failed to load rich edit 2.0 library");

        hr = CreateRichTextWindow(&hWndMain, &fRegisteredClass);
        ExitOnFailure(hr, "failed to create rich text window for printing");

        hr = PrintRichText(hWndMain);
        if (FAILED(hr)) // Since we've already shown the print dialog, we better show them a dialog explaining why it didn't print
        {
            ShowErrorMessage(hr);
        }
    }

LExit:
    ReleaseNullStr(vpszEulaText);
    if (vpPrintDlg)
    {
        if (vpPrintDlg->hDevMode)
        {
            ::GlobalFree(vpPrintDlg->hDevMode);
        }

        if (vpPrintDlg->hDevNames)
        {
            ::GlobalFree(vpPrintDlg->hDevNames);
        }

        if (vpPrintDlg->hDC)
        {
            ::DeleteDC(vpPrintDlg->hDC);
        }

        ::GlobalFree(vpPrintDlg);
        vpPrintDlg = NULL;
    }

    if (fRegisteredClass)
    {
        ::UnregisterClassW(WINDOW_CLASS, NULL);
    }

    if (NULL != hRichEdit)
    {
        ::FreeLibrary(hRichEdit);
    }

    // Always return success since we dont want to stop the
    // installation even if the Eula printing fails.
    return WcaFinalize(ERROR_SUCCESS);
}



/********************************************************************
CreateRichTextWindow - Creates Window and Child RichText control.

********************************************************************/
HRESULT CreateRichTextWindow(
    __out HWND* phWndMain,
    __out BOOL* pfRegisteredClass
    )
{
    HRESULT hr = S_OK;
    HWND hWndMain = NULL;
    WNDCLASSEXW wcex;

    //
    // Register the window class
    //
    wcex.cbSize = sizeof(WNDCLASSEXW);
    wcex.style = CS_HREDRAW | CS_VREDRAW;
    wcex.lpfnWndProc = (WNDPROC)WndProc;
    wcex.cbClsExtra = 0;
    wcex.cbWndExtra = 0;
    wcex.hInstance = NULL;
    wcex.hIcon = NULL;
    wcex.hCursor = LoadCursor(NULL, IDC_ARROW);
    wcex.hbrBackground = (HBRUSH)(COLOR_BACKGROUND+1);
    wcex.lpszMenuName = NULL;
    wcex.lpszClassName = WINDOW_CLASS;
    wcex.hIconSm = NULL;

    if (0 == ::RegisterClassExW(&wcex))
    {
        DWORD  dwResult = ::GetLastError();

        // If we get "Class already exists" error ignore it. We might
        // encounter this when the user tries to print more than once
        // in the same setup instance and we are unable to clean up fully.
        if (dwResult != ERROR_CLASS_ALREADY_EXISTS)
        {
            ExitOnFailure(hr = HRESULT_FROM_WIN32(dwResult), "failed to register window class");
        }
    }

    *pfRegisteredClass = TRUE;

    // Perform application initialization:
    hWndMain = ::CreateWindowW(WINDOW_CLASS, NULL, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, 0, CW_USEDEFAULT, 0, NULL, NULL, NULL, NULL);
    ExitOnNullWithLastError(hWndMain, hr, "failed to create window for printing");

    ::ShowWindow(hWndMain, SW_HIDE);
    if (!::UpdateWindow(hWndMain))
    {
        ExitWithLastError(hr, "failed to update window");
    }

    *phWndMain = hWndMain;

LExit:
    return hr;
}


/********************************************************************
 PrintRichText - Sends messages to load the Eula text, print it, and
 close the window.

 NOTE: Returns errors that have occured while attempting to print,
 which were saved in vhr by the print callbacks.
********************************************************************/
HRESULT PrintRichText(
    __in HWND hWndMain
    )
{
    MSG msg;

    // Populate the RichEdit control
    ::SendMessageW(hWndMain, WM_COMMAND, IDM_POPULATE, 0);

    // Print Eula
    ::SendMessageW(hWndMain, WM_COMMAND, IDM_PRINT, 0);

    // Done! Lets close the Window
    ::SendMessage(hWndMain, WM_CLOSE, 0, 0);
    // Main message loop:
    while (::GetMessageW(&msg, NULL, 0, 0))
    {
//        if (!::TranslateAcceleratorW(msg.hwnd, NULL, &msg))
//        {
//            ::TranslateMessage(&msg);
//            ::DispatchMessageW(&msg);
//        }
    }


    // return any errors encountered in the print callbacks
    return vhr;
}


/********************************************************************
 WndProc - Windows callback procedure

********************************************************************/
LRESULT CALLBACK WndProc(
    __in HWND hWnd,
    __in UINT message,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    static HWND hWndRichEdit = NULL;
    int wmId, wmEvent;
    PAINTSTRUCT ps;
    HDC hdc;

    switch (message)
    {
    case WM_CREATE:
        hWndRichEdit = ::CreateWindowExW(WS_EX_CLIENTEDGE, RICHEDIT_CLASSW, L"", ES_MULTILINE | WS_CHILD | WS_VISIBLE | WS_VSCROLL, CONTROL_X_COORDINATE, CONTROL_Y_COORDINATE, CONTROL_WIDTH, CONTROL_HEIGHT, hWnd, NULL, NULL, NULL);
        break;
    case WM_COMMAND:
        wmId = LOWORD(wParam);
        wmEvent = HIWORD(wParam);
        switch (wmId)
        {
        case IDM_POPULATE:
            LoadEulaText(hWndRichEdit);
            break;
        case IDM_PRINT:
            Print(hWndRichEdit);
            break;
        default:
            return ::DefWindowProcW(hWnd, message, wParam, lParam);
            break;
        }
        break;
    case WM_PAINT:
        hdc = ::BeginPaint(hWnd, &ps);
        ::EndPaint(hWnd, &ps);
        break;
    case WM_DESTROY:
        ::PostQuitMessage(0);
        break;
    default:
        return ::DefWindowProcW(hWnd, message, wParam, lParam);
    }

    return 0;
}


/********************************************************************
 ReadStreamCallback - Callback function to read data to the RichText control

 NOTE: Richtext control uses this function to read data from the buffer
********************************************************************/
DWORD CALLBACK ReadStreamCallback(
    __in DWORD /*Cookie*/,
    __out LPBYTE pbBuff,
    __in LONG cb,
    __out LONG FAR *pcb
    )
{
    static LPCSTR pszTextBuf = NULL;
    DWORD er = ERROR_SUCCESS;

    // If it's null set it to the beginning of the EULA buffer
    if (pszTextBuf == NULL)
    {
        pszTextBuf = vpszEulaText;
    }

    LONG lTextLength = (LONG)lstrlen(pszTextBuf);

    if (cb < 0)
    {
        *pcb = 0;
        er = 1;
    }
    else if (lTextLength < cb ) // If the size to be written is less than then length of the buffer, write the rest
    {
        *pcb = lTextLength;
        memcpy(pbBuff, pszTextBuf, *pcb);
        pszTextBuf = NULL;
    }
    else // Only write the amount being asked for and move the pointer along
    {
        *pcb = cb;
        memcpy(pbBuff, pszTextBuf, *pcb);
        pszTextBuf = pszTextBuf +  cb;
    }

    return er;
}


/********************************************************************
 LoadEulaText - Reads data for Richedit control

********************************************************************/
void LoadEulaText(
    __in HWND hWnd
    )
{
    HRESULT hr = S_OK;

    ExitOnNull(hWnd, hr, ERROR_INVALID_HANDLE, "Invalid Handle passed to LoadEulaText");

    // Docs say this doesn't return any value
    ::SendMessageW(hWnd, EM_LIMITTEXT, static_cast<WPARAM>(lstrlen(vpszEulaText)), 0);

    EDITSTREAM es;
    ::ZeroMemory(&es, sizeof(es));
    es.pfnCallback = (EDITSTREAMCALLBACK)ReadStreamCallback;
    es.dwCookie = (DWORD)0;
    ::SendMessageW(hWnd, EM_STREAMIN, SF_RTF, (LPARAM)&es);

    if (0 != es.dwError)
    {
        ExitOnLastError(hr, "failed to load the EULA into the control");
    }

LExit:
    vhr = hr;
}


/********************************************************************
 ReadEulaText - Reads Eula text from the MSI

********************************************************************/
HRESULT ReadEulaText(
    __in MSIHANDLE /*hInstall*/,
    __out LPSTR* ppszEulaText
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hDB;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    LPWSTR pwzEula = NULL;

    hr = WcaOpenExecuteView(vcsEulaQuery, &hView);
    ExitOnFailure(hr, "failed to open and execute view for PrintEula query");

    hr = WcaFetchSingleRecord(hView, &hRec);
    ExitOnFailure(hr, "failed to fetch the row containing the LicenseText");

    hr = WcaGetRecordString(hRec, 1, &pwzEula);
    ExitOnFailure(hr, "failed to get LicenseText in PrintEula");

    hr = StrAnsiAllocString(ppszEulaText, pwzEula, 0, CP_ACP);
    ExitOnFailure(hr, "failed to convert LicenseText to ANSI code page");

LExit:
    return hr;
}


/********************************************************************
 Print - Function that sends the data from richedit control to the printer

 NOTE: Any errors encountered are saved to the vhr variable
********************************************************************/
void Print(
    __in_opt HWND hRtfWnd
    )
{
    HRESULT hr = S_OK;
    FORMATRANGE fRange;
    RECT rcPage;
    RECT rcPrintablePage;
    GETTEXTLENGTHEX gTxex;
    HDC hPrinterDC = vpPrintDlg->hDC;
    int nHorizRes = ::GetDeviceCaps(hPrinterDC, HORZRES);
    int nVertRes = ::GetDeviceCaps(hPrinterDC, VERTRES);
    int nLogPixelsX = ::GetDeviceCaps(hPrinterDC, LOGPIXELSX);
    //int nLogPixelsY = ::GetDeviceCaps(hPrinterDC, LOGPIXELSY);
    LONG_PTR lTextLength = 0; // Length of document.
    LONG_PTR lTextPrinted = 0; // Amount of document printed.
    DOCINFOW dInfo;
    LPDEVNAMES pDevnames;
    LPWSTR sczProductName = NULL;
    BOOL fStartedDoc = FALSE;
    BOOL fPrintedSomething = FALSE;

    // Ensure the printer DC is in MM_TEXT mode.
    if (0 == ::SetMapMode(hPrinterDC, MM_TEXT))
    {
        ExitWithLastError(hr, "failed to set map mode");
    }

    // Rendering to the same DC we are measuring.
    ::ZeroMemory(&fRange, sizeof(fRange));
    fRange.hdc = fRange.hdcTarget = hPrinterDC;

    // Set up the page.
    rcPage.left = rcPage.top = 0;
    rcPage.right = MulDiv(nHorizRes, ONE_INCH, nLogPixelsX);
    rcPage.bottom = MulDiv(nVertRes, ONE_INCH, nLogPixelsX);

    // Set up 1" margins all around.
    rcPrintablePage.left = rcPage.left + ONE_INCH;  
    rcPrintablePage.top = rcPage.top + ONE_INCH;
    rcPrintablePage.right = rcPage.right - ONE_INCH;
    rcPrintablePage.bottom = rcPage.bottom - ONE_INCH;

    // Set up the print job (standard printing stuff here).
    ::ZeroMemory(&dInfo, sizeof(dInfo));
    dInfo.cbSize = sizeof(DOCINFO);
    hr = WcaGetProperty(L"ProductName", &sczProductName);
    if (FAILED(hr))
    {
        // If we fail to get the product name, don't fail, just leave it blank;
        dInfo.lpszDocName = L"";
        hr = S_OK;
    }
    else
    {
        dInfo.lpszDocName = sczProductName;
    }

    pDevnames = (LPDEVNAMES)::GlobalLock(vpPrintDlg->hDevNames);
    ExitOnNullWithLastError(pDevnames, hr, "failed to get global lock");

    dInfo.lpszOutput  = (LPWSTR)pDevnames + pDevnames->wOutputOffset;

    if (0 == ::GlobalUnlock(pDevnames))
    {
        ExitWithLastError(hr, "failed to release global lock");
    }

    // Start the document.
    if (0 >= ::StartDocW(hPrinterDC, &dInfo))
    {
        ExitWithLastError(hr, "failed to start print document");
    }

    fStartedDoc = TRUE;

    ::ZeroMemory(&gTxex, sizeof(gTxex));
    gTxex.flags = GTL_NUMCHARS | GTL_PRECISE;
    lTextLength = ::SendMessageW(hRtfWnd, EM_GETTEXTLENGTHEX, (LONG_PTR)&gTxex, 0);

    while (lTextPrinted < lTextLength)
    {
        // Start the page.
        if (0 >= ::StartPage(hPrinterDC))
        {
            ExitWithLastError(hr, "failed to start print page");
        }

        // Always reset to the full printable page and start where the
        // last text left off (or zero at the beginning).
        fRange.rc = rcPrintablePage;
        fRange.rcPage = rcPage;
        fRange.chrg.cpMin = (LONG)lTextPrinted;
        fRange.chrg.cpMax = -1;

        // Print as much text as can fit on a page. The return value is
        // the index of the first character on the next page. Using TRUE
        // for the wParam parameter causes the text to be printed.
        lTextPrinted = ::SendMessageW(hRtfWnd, EM_FORMATRANGE, TRUE, (LPARAM)&fRange);
        fPrintedSomething = TRUE;

        // If text wasn't printed (i.e. we didn't move past the point we started) then
        // something must have gone wrong.
        if (lTextPrinted <= fRange.chrg.cpMin)
        {
            hr = E_FAIL;
            ExitOnFailure(hr, "failed to print some text");
        }

        // Print last page.
        if (0 >= ::EndPage(hPrinterDC))
        {
            ExitWithLastError(hr, "failed to end print page");
        }
    }

LExit:
    // Tell the control to release cached information, if we actually tried to
    // print something.
    if (fPrintedSomething)
    {
        ::SendMessageW(hRtfWnd, EM_FORMATRANGE, 0, (LPARAM)NULL);
    }

    if (fStartedDoc)
    {
        ::EndDoc(hPrinterDC);
    }

    ReleaseStr(sczProductName);

    vhr = hr;
}


/********************************************************************
 ShowErrorMessage - Display MessageBox showing the message for hr.

********************************************************************/
void ShowErrorMessage(
    __in HRESULT hr
    )
{
    WCHAR wzMsg[STRING_CAPACITY];

#pragma prefast(push)
#pragma prefast(disable:25028)
    if (0 != ::FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM, 0, hr, 0, wzMsg, countof(wzMsg), 0))
#pragma prefast(pop)
    {
        HWND hWnd = ::GetForegroundWindow();
        ::MessageBoxW(hWnd, wzMsg, L"PrintEULA", MB_OK | MB_ICONWARNING);
    }
}
