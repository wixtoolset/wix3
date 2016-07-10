// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR THMVWR_WINDOW_CLASS_MAIN = L"ThmViewerMain";

static THEME* vpTheme = NULL;
static DWORD vdwDisplayThreadId = 0;

enum THMVWR_CONTROL
{
    // Non-paged controls
    THMVWR_CONTROL_TREE = THEME_FIRST_ASSIGN_CONTROL_ID,
};

static THEME_ASSIGN_CONTROL_ID vrgInitControls[] = {
    { THMVWR_CONTROL_TREE, L"Tree" },
};

// Internal functions

static HRESULT ProcessCommandLine(
    __in_z_opt LPCWSTR wzCommandLine,
    __out_z LPWSTR* psczThemeFile
    );
static HRESULT CreateTheme(
    __in HINSTANCE hInstance,
    __out THEME** ppTheme
    );
static HRESULT CreateMainWindowClass(
    __in HINSTANCE hInstance,
    __in THEME* pTheme,
    __out ATOM* pAtom
    );
static LRESULT CALLBACK MainWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static void OnNewTheme(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in HANDLE_THEME* pHandle
    );


int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE /* hPrevInstance */,
    __in_z_opt LPWSTR lpCmdLine,
    __in int /*nCmdShow*/
    )
{
    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    LPWSTR sczThemeFile = NULL;
    ATOM atom = 0;
    HWND hWnd = NULL;

    HANDLE hDisplayThread = NULL;
    HANDLE hLoadThread = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    hr = ProcessCommandLine(lpCmdLine, &sczThemeFile);
    ExitOnFailure(hr, "Failed to process command line.");

    hr = CreateTheme(hInstance, &vpTheme);
    ExitOnFailure(hr, "Failed to create theme.");

    hr = CreateMainWindowClass(hInstance, vpTheme, &atom);
    ExitOnFailure(hr, "Failed to create main window.");

    hWnd = ::CreateWindowExW(0, reinterpret_cast<LPCWSTR>(atom), vpTheme->sczCaption, vpTheme->dwStyle, CW_USEDEFAULT, CW_USEDEFAULT, vpTheme->nWidth, vpTheme->nHeight, HWND_DESKTOP, NULL, hInstance, NULL);
    ExitOnNullWithLastError(hWnd, hr, "Failed to create window.");

    if (!sczThemeFile)
    {
        // Prompt for a path to the theme file.
        OPENFILENAMEW ofn = { };
        WCHAR wzFile[MAX_PATH] = { };

        ofn.lStructSize = sizeof(ofn);
        ofn.hwndOwner = hWnd;
        ofn.lpstrFile = wzFile;
        ofn.nMaxFile = countof(wzFile);
        ofn.lpstrFilter = L"Theme Files\0*.thm\0XML Files\0*.xml\0All Files\0*.*\0";
        ofn.nFilterIndex = 1;
        ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST;
        ofn.lpstrTitle = vpTheme->sczCaption;

        if (::GetOpenFileNameW(&ofn))
        {
            hr = StrAllocString(&sczThemeFile, wzFile, 0);
            ExitOnFailure(hr, "Failed to copy opened file to theme file.");
        }
        else
        {
            ::MessageBoxW(hWnd, L"Must specify a path to theme file.", vpTheme->sczCaption, MB_OK | MB_ICONERROR);
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    hr = DisplayStart(hInstance, hWnd, &hDisplayThread, &vdwDisplayThreadId);
    ExitOnFailure(hr, "Failed to start display.");

    hr = LoadStart(sczThemeFile, hWnd, &hLoadThread);
    ExitOnFailure(hr, "Failed to start load.");

    // message pump
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        else if (!ThemeHandleKeyboardMessage(vpTheme, msg.hwnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    if (::IsWindow(hWnd))
    {
        ::DestroyWindow(hWnd);
    }

    if (hDisplayThread)
    {
        ::PostThreadMessageW(vdwDisplayThreadId, WM_QUIT, 0, 0);
        ::WaitForSingleObject(hDisplayThread, 10000);
        ::CloseHandle(hDisplayThread);
    }

    // TODO: come up with a good way to kill the load thread, probably need to switch
    // the ReadDirectoryW() to overlapped mode.
    ReleaseHandle(hLoadThread);

    if (atom && !::UnregisterClassW(reinterpret_cast<LPCWSTR>(atom), hInstance))
    {
        DWORD er = ::GetLastError();
        er = er;
    }

    ThemeFree(vpTheme);
    ThemeUninitialize();

    // uninitialize COM
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    ReleaseStr(sczThemeFile);
    return hr;
}


//
// ProcessCommandLine - process the provided command line arguments.
//
static HRESULT ProcessCommandLine(
    __in_z_opt LPCWSTR wzCommandLine,
    __out_z LPWSTR* psczThemeFile
    )
{
    HRESULT hr = S_OK;
    int argc = 0;
    LPWSTR* argv = NULL;

    if (wzCommandLine && *wzCommandLine)
    {
        hr = AppParseCommandLine(wzCommandLine, &argc, &argv);
        ExitOnFailure(hr, "Failed to parse command line.");

        for (int i = 0; i < argc; ++i)
        {
            if (argv[i][0] == L'-' || argv[i][0] == L'/')
            {
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"lang", -1))
                {
                    if (i + 1 >= argc)
                    {
                        ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a language.");
                    }

                    ++i;
                }
            }
            else
            {
                hr = StrAllocString(psczThemeFile, argv[i], 0);
                ExitOnFailure(hr, "Failed to copy path to theme file.");
            }
        }
    }

LExit:
    if (argv)
    {
        AppFreeCommandLineArgs(argv);
    }

    return hr;
}

static HRESULT CreateTheme(
    __in HINSTANCE hInstance,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;

    hr = ThemeInitialize(hInstance);
    ExitOnFailure(hr, "Failed to initialize theme manager.");

    hr = ThemeLoadFromResource(hInstance, MAKEINTRESOURCEA(THMVWR_RES_THEME_FILE), ppTheme);
    ExitOnFailure(hr, "Failed to load theme from thmviewer.thm.");

LExit:
    return hr;
}

static HRESULT CreateMainWindowClass(
    __in HINSTANCE hInstance,
    __in THEME* pTheme,
    __out ATOM* pAtom
    )
{
    HRESULT hr = S_OK;
    ATOM atom = 0;
    WNDCLASSW wc = { };

    wc.lpfnWndProc = MainWndProc;
    wc.hInstance = hInstance;
    wc.hIcon = reinterpret_cast<HICON>(pTheme->hIcon);
    wc.hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
    wc.hbrBackground = pTheme->rgFonts[pTheme->dwFontId].hBackground;
    wc.lpszMenuName = NULL;
    wc.lpszClassName = THMVWR_WINDOW_CLASS_MAIN;
    atom = ::RegisterClassW(&wc);
    if (!atom)
    {
        ExitWithLastError(hr, "Failed to register main windowclass .");
    }

    *pAtom = atom;

LExit:
    return hr;
}

static LRESULT CALLBACK MainWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HANDLE_THEME* pHandleTheme = reinterpret_cast<HANDLE_THEME*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        //LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
        //pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(lpcs->lpCreateParams);
        //::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBA));
        }
        break;

    case WM_NCDESTROY:
        DecrementHandleTheme(pHandleTheme);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        break;

    case WM_CREATE:
        {
        HRESULT hr = ThemeLoadControls(vpTheme, hWnd, vrgInitControls, countof(vrgInitControls));
        if (FAILED(hr))
        {
            return -1;
        }
        }
        break;

    case WM_THMVWR_NEW_THEME:
        OnNewTheme(vpTheme, hWnd, reinterpret_cast<HANDLE_THEME*>(lParam));
        return 0;

    case WM_DESTROY:
        ::PostQuitMessage(0);
        break;

    case WM_NOTIFY:
        {
        NMHDR* pnmhdr = reinterpret_cast<NMHDR*>(lParam);
        switch (pnmhdr->code)
        {
        case TVN_SELCHANGED:
            {
            NMTREEVIEWW* ptv = reinterpret_cast<NMTREEVIEWW*>(lParam);
            ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_SHOWPAGE, SW_HIDE, ptv->itemOld.lParam);
            ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_SHOWPAGE, SW_SHOW, ptv->itemNew.lParam);
            }
            break;

        //case NM_DBLCLK:
        //    TVITEM item = { };
        //    item.mask = TVIF_PARAM;
        //    item.hItem = TreeView_GetSelection(pnmhdr->hwndFrom);
        //    TreeView_GetItem(pnmhdr->hwndFrom, &item);
        //    ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_SHOWPAGE, SW_SHOW, item.lParam);
        //    return 1;
        }
        }
        break;
    }

    return ThemeDefWindowProc(vpTheme, hWnd, uMsg, wParam, lParam);
}

static void OnNewTheme(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in HANDLE_THEME* pHandle
    )
{
    HANDLE_THEME* pOldHandle = reinterpret_cast<HANDLE_THEME*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));
    THEME* pNewTheme = pHandle->pTheme;

    WCHAR wzSelectedPage[MAX_PATH] = { };
    HTREEITEM htiSelected = NULL;
    TVINSERTSTRUCTW tvi = { };
    TVITEMW item = { };

    if (pOldHandle)
    {
        DecrementHandleTheme(pOldHandle);
        pOldHandle = NULL;
    }

    // Pass the new theme handle to the display thread so it can get the display window prepared
    // to show the new theme.
    IncrementHandleTheme(pHandle);
    ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_NEW_THEME, 0, reinterpret_cast<LPARAM>(pHandle));

    ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pHandle));

    // Remember the currently selected item by name so we can try to automatically select it later.
    // Otherwise, the user would see their window destroyed after every save of their theme file and
    // have to click to get the window back.
    item.mask = TVIF_TEXT;
    item.pszText = wzSelectedPage;
    item.cchTextMax = countof(wzSelectedPage);
    item.hItem = reinterpret_cast<HTREEITEM>(ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_GETNEXTITEM, TVGN_CARET, NULL));
    ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_GETITEM, 0, reinterpret_cast<LPARAM>(&item));

    // Remove the previous items in the tree.
    ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_DELETEITEM, 0, reinterpret_cast<LPARAM>(TVI_ROOT));

    // Add the application node.
    tvi.hParent = NULL;
    tvi.hInsertAfter = TVI_ROOT;
    tvi.item.mask = TVIF_TEXT | TVIF_PARAM;
    tvi.item.lParam = 0;
    tvi.item.pszText = L"Application";

    // Add the pages.
    tvi.hParent = reinterpret_cast<HTREEITEM>(ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_INSERTITEM, 0, reinterpret_cast<LPARAM>(&tvi)));
    tvi.hInsertAfter = TVI_SORT;
    for (DWORD i = 0; i < pNewTheme->cPages; ++i)
    {
        THEME_PAGE* pPage = pNewTheme->rgPages + i;
        if (pPage->sczName && *pPage->sczName)
        {
            tvi.item.pszText = pPage->sczName;
            tvi.item.lParam = i + 1; //prgdwPageIds[i]; - TODO: do the right thing here by calling ThemeGetPageIds(), should not assume we know how the page ids will be calculated.

            HTREEITEM hti = reinterpret_cast<HTREEITEM>(ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_INSERTITEM, 0, reinterpret_cast<LPARAM>(&tvi)));
            if (*wzSelectedPage && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pPage->sczName, -1, wzSelectedPage, -1))
            {
                htiSelected = hti;
            }
        }
    }

    if (*wzSelectedPage && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Application", -1, wzSelectedPage, -1))
    {
        htiSelected = tvi.hParent;
    }

    ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_EXPAND, TVE_EXPAND, reinterpret_cast<LPARAM>(tvi.hParent));
    if (htiSelected)
    {
        ThemeSendControlMessage(pTheme, THMVWR_CONTROL_TREE, TVM_SELECTITEM, TVGN_CARET, reinterpret_cast<LPARAM>(htiSelected));
    }

    return;
}
