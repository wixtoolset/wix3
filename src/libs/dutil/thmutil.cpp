// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include <cmath>

#ifndef BCM_SETSHIELD
#define BCM_SETSHIELD       (BCM_FIRST + 0x000C)
#endif

#ifndef LWS_NOPREFIX
#define LWS_NOPREFIX        0x0004
#endif

const DWORD THEME_INVALID_ID = 0xFFFFFFFF;
const COLORREF THEME_INVISIBLE_COLORREF = 0xFFFFFFFF;
const DWORD GROW_WINDOW_TEXT = 250;
const LPCWSTR THEME_WC_HYPERLINK = L"ThemeHyperLink";

static Gdiplus::GdiplusStartupInput vgsi;
static Gdiplus::GdiplusStartupOutput vgso = { };
static ULONG_PTR vgdiToken = 0;
static ULONG_PTR vgdiHookToken = 0;
static HMODULE vhHyperlinkRegisteredModule = NULL;
static HMODULE vhModuleRichEd = NULL;
static HCURSOR vhCursorHand = NULL;

enum INTERNAL_CONTROL_STYLE
{
    INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED = 0x0001,
    INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE = 0x0002,
    INTERNAL_CONTROL_STYLE_DISABLED = 0x0004,
    INTERNAL_CONTROL_STYLE_HIDDEN = 0x0008,
    INTERNAL_CONTROL_STYLE_OWNER_DRAW = 0x0010,
};

struct MEMBUFFER_FOR_RICHEDIT
{
    BYTE* rgbData;
    DWORD cbData;

    DWORD iData;
};


// prototypes
static HRESULT ParseTheme(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMDocument* pixd,
    __out THEME** ppTheme
    );
static HRESULT LocalizeTheme(
    __in THEME *pTheme,
    __in const WIX_LOCALIZATION *pWixLoc
    );
static HRESULT ParseImage(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __out HBITMAP* phImage
    );
static HRESULT ParseApplication(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseFonts(
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParsePages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseImageLists(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseControls(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in_opt THEME_PAGE* pPage
    );
static HRESULT ParseControl(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL_TYPE type,
    __in THEME* pTheme,
    __in DWORD iControl
    );
static HRESULT ParseBillboards(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseColumns(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseTabs(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT FindImageList(
    __in THEME* pTheme,
    __in_z LPCWSTR wzImageListName,
    __out HIMAGELIST *phImageList
    );
static HRESULT DrawBillboard(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawButton(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawHyperlink(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawImage(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawProgressBar(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static BOOL DrawHoverControl(
    __in THEME* pTheme,
    __in BOOL fHover
    );
static DWORD CALLBACK RichEditStreamFromFileHandleCallback(
    __in DWORD_PTR dwCookie,
    __in_bcount(cb) LPBYTE pbBuff,
    __in LONG cb,
    __in LONG *pcb
    );
static DWORD CALLBACK RichEditStreamFromMemoryCallback(
    __in DWORD_PTR dwCookie,
    __in_bcount(cb) LPBYTE pbBuff,
    __in LONG cb,
    __in LONG *pcb
    );
static void FreeFont(
    __in THEME_FONT* pFont
    );
static void FreePage(
    __in THEME_PAGE* pPage
    );
static void FreeControl(
    __in THEME_CONTROL* pControl
    );
static void FreeImageList(
    __in THEME_IMAGELIST* pImageList
    );
static void FreeBillboard(
    __in THEME_BILLBOARD* pBillboard
    );
static void FreeColumn(
    __in THEME_COLUMN* pColumn
    );
static void FreeTab(
    __in THEME_TAB* pTab
    );
static void CALLBACK OnBillboardTimer(
    __in const THEME* pTheme,
    __in HWND hwnd,
    __in UINT_PTR idEvent
    );
static HRESULT OnRichEditEnLink(
    __in LPARAM lParam,
    __in HWND hWndRichEdit,
    __in HWND hWnd
    );
static BOOL ControlIsType(
    __in const THEME* pTheme,
    __in DWORD dwControl,
    __in THEME_CONTROL_TYPE type
    );
static const THEME_CONTROL* FindControlFromHWnd(
    __in const THEME* pTheme,
    __in HWND hWnd
    );
static void GetControlDimensions(
    __in const RECT* prcParent,
    __in const THEME_CONTROL* pControl,
    __out int* piWidth,
    __out int* piHeight,
    __out int* piX,
    __out int* piY
    );
// Using iWidth as total width of listview, base width of columns, and "Expands" flag on columns
// calculates final width of each column (storing result in each column's nWidth value)
static HRESULT SizeListViewColumns(
    __inout THEME_CONTROL* pControl
    );
// DPI scaling functions
static void ScaleApplication(
    __in THEME* pTheme
    );
static void ScaleControl(
    __in const THEME* pTheme,
    __in THEME_CONTROL* pControl
    );
static void ScaleFont(
    __in const THEME* pTheme,
    __in LOGFONTW* lf
    );

DAPI_(HRESULT) ThemeInitialize(
    __in_opt HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    INITCOMMONCONTROLSEX icex = { };
    WNDCLASSW wcHyperlink = { };

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML.");

    vhCursorHand = ::LoadCursorA(NULL, IDC_HAND);

    // Base the theme hyperlink class on a button but give it the "hand" icon.
    if (!::GetClassInfoW(NULL, WC_BUTTONW, &wcHyperlink))
    {
        ExitWithLastError(hr, "Failed to get button window class.");
    }

    wcHyperlink.lpszClassName = THEME_WC_HYPERLINK;
#pragma prefast(push)
#pragma prefast(disable:25068)
    wcHyperlink.hCursor = vhCursorHand;
#pragma prefast(pop)

    if (!::RegisterClassW(&wcHyperlink))
    {
        ExitWithLastError(hr, "Failed to get button window class.");
    }
    vhHyperlinkRegisteredModule = hModule;

    // Initialize GDI+ and common controls.
    vgsi.SuppressBackgroundThread = TRUE;

    hr = GdipInitialize(&vgsi, &vgdiToken, &vgso);
    ExitOnFailure(hr, "Failed to initialize GDI+.");

    icex.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icex.dwICC = ICC_STANDARD_CLASSES | ICC_PROGRESS_CLASS | ICC_LISTVIEW_CLASSES | ICC_TREEVIEW_CLASSES | ICC_TAB_CLASSES | ICC_LINK_CLASS;
    ::InitCommonControlsEx(&icex);

    (*vgso.NotificationHook)(&vgdiHookToken);

LExit:
    return hr;
}


DAPI_(void) ThemeUninitialize()
{
    if (vhModuleRichEd)
    {
        ::FreeLibrary(vhModuleRichEd);
        vhModuleRichEd = NULL;
    }

    if (vhHyperlinkRegisteredModule)
    {
        ::UnregisterClassW(THEME_WC_HYPERLINK, vhHyperlinkRegisteredModule);
        vhHyperlinkRegisteredModule = NULL;
    }

    if (vgdiToken)
    {
        GdipUninitialize(vgdiToken);
        vgdiToken = 0;
    }

    XmlUninitialize();
}


DAPI_(HRESULT) ThemeLoadFromFile(
    __in_z LPCWSTR wzThemeFile,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixd = NULL;
    LPWSTR sczRelativePath = NULL;

    hr = XmlLoadDocumentFromFile(wzThemeFile, &pixd);
    ExitOnFailure(hr, "Failed to load theme resource as XML document.");

    hr = PathGetDirectory(wzThemeFile, &sczRelativePath);
    ExitOnFailure(hr, "Failed to get relative path from theme file.");

    hr = ParseTheme(NULL, sczRelativePath, pixd, ppTheme);
    ExitOnFailure(hr, "Failed to parse theme.");

LExit:
    ReleaseStr(sczRelativePath);
    ReleaseObject(pixd);

    return hr;
}


DAPI_(HRESULT) ThemeLoadFromResource(
    __in_opt HMODULE hModule,
    __in_z LPCSTR szResource,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;
    LPVOID pvResource = NULL;
    DWORD cbResource = 0;
    LPWSTR sczXml = NULL;
    IXMLDOMDocument* pixd = NULL;

    hr = ResReadData(hModule, szResource, &pvResource, &cbResource);
    ExitOnFailure(hr, "Failed to read theme from resource.");

    // Ensure returned resource buffer is null-terminated.
    reinterpret_cast<BYTE *>(pvResource)[cbResource - 1] = '\0';

    hr = StrAllocStringAnsi(&sczXml, reinterpret_cast<LPCSTR>(pvResource), cbResource, CP_UTF8);
    ExitOnFailure(hr, "Failed to convert XML document data from UTF-8 to unicode string.");

    hr = XmlLoadDocument(sczXml, &pixd);
    ExitOnFailure(hr, "Failed to load theme resource as XML document.");

    hr = ParseTheme(hModule, NULL, pixd, ppTheme);
    ExitOnFailure(hr, "Failed to parse theme.");

LExit:
    ReleaseObject(pixd);
    ReleaseStr(sczXml);

    return hr;
}


DAPI_(void) ThemeFree(
    __in THEME* pTheme
    )
{
    if (pTheme)
    {
        for (DWORD i = 0; i < pTheme->cFonts; ++i)
        {
            FreeFont(pTheme->rgFonts + i);
        }

        for (DWORD i = 0; i < pTheme->cPages; ++i)
        {
            FreePage(pTheme->rgPages + i);
        }

        for (DWORD i = 0; i < pTheme->cImageLists; ++i)
        {
            FreeImageList(pTheme->rgImageLists + i);
        }

        for (DWORD i = 0; i < pTheme->cControls; ++i)
        {
            FreeControl(pTheme->rgControls + i);
        }

        ReleaseMem(pTheme->rgControls);
        ReleaseMem(pTheme->rgPages);
        ReleaseMem(pTheme->rgFonts);

        if (pTheme->hImage)
        {
            ::DeleteBitmap(pTheme->hImage);
        }

        ReleaseStr(pTheme->sczCaption);
        ReleaseMem(pTheme);
    }
}


DAPI_(HRESULT) ThemeLoadControls(
    __in THEME* pTheme,
    __in HWND hwndParent,
    __in_ecount_opt(cAssignControlIds) const THEME_ASSIGN_CONTROL_ID* rgAssignControlIds,
    __in DWORD cAssignControlIds
    )
{
    AssertSz(!pTheme->hwndParent, "Theme already loaded controls because it has a parent window.");

    HRESULT hr = S_OK;
    RECT rcParent = { };

    pTheme->hwndParent = hwndParent;

    ::GetClientRect(pTheme->hwndParent, &rcParent);

    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        THEME_CONTROL* pControl = pTheme->rgControls + i;
        THEME_FONT* pControlFont = (pTheme->cFonts > pControl->dwFontId) ? pTheme->rgFonts + pControl->dwFontId : NULL;
        LPCWSTR wzWindowClass = NULL;
        DWORD dwWindowBits = WS_CHILD;
        DWORD dwWindowExBits = 0;

        switch (pControl->type)
        {
        case THEME_CONTROL_TYPE_BILLBOARD: // billboards are basically just owner drawn static controls (where we draw different images).
            if (pControl->cBillboards)
            {
                wzWindowClass = WC_STATICW;
                dwWindowBits |= SS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                ExitOnRootFailure(hr, "Billboard was unexpected.");
            }
            break;

        case THEME_CONTROL_TYPE_CHECKBOX:
            dwWindowBits |= BS_AUTOCHECKBOX | BS_MULTILINE; // checkboxes are basically buttons with an extra bit tossed in.
            __fallthrough;
        case THEME_CONTROL_TYPE_BUTTON:
            wzWindowClass = WC_BUTTONW;
            if (pControl->hImage || (pTheme->hImage && 0 <= pControl->nSourceX && 0 <= pControl->nSourceY))
            {
                dwWindowBits |= BS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            break;

        case THEME_CONTROL_TYPE_LISTVIEW:
            // If thmutil is handling the image list for this listview, tell Windows not to free it when the control is destroyed.
            if (pControl->rghImageList[0] || pControl->rghImageList[1] || pControl->rghImageList[2] || pControl->rghImageList[3])
            {
                pControl->dwStyle |= LVS_SHAREIMAGELISTS;
            }
            wzWindowClass = WC_LISTVIEWW;
            break;

        case THEME_CONTROL_TYPE_TREEVIEW:
            wzWindowClass = WC_TREEVIEWW;
            break;

        case THEME_CONTROL_TYPE_TAB:
            wzWindowClass = WC_TABCONTROLW;
            break;

        case THEME_CONTROL_TYPE_EDITBOX:
            wzWindowClass = WC_EDITW;
            dwWindowBits |= ES_LEFT | ES_AUTOHSCROLL;
            dwWindowExBits = WS_EX_CLIENTEDGE;
            break;

        case THEME_CONTROL_TYPE_HYPERLINK: // hyperlinks are basically just owner drawn buttons.
            wzWindowClass = THEME_WC_HYPERLINK;
            dwWindowBits |= BS_OWNERDRAW | BTNS_NOPREFIX;
            break;

        case THEME_CONTROL_TYPE_HYPERTEXT:
            wzWindowClass = WC_LINK;
            dwWindowBits |= LWS_NOPREFIX;
            break;

        case THEME_CONTROL_TYPE_IMAGE: // images are basically just owner drawn static controls (so we can draw .jpgs and .pngs instead of just bitmaps).
            if (pControl->hImage || (pTheme->hImage && 0 <= pControl->nSourceX && 0 <= pControl->nSourceY))
            {
                wzWindowClass = WC_STATICW;
                dwWindowBits |= SS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                ExitOnRootFailure(hr, "Invalid image or image list coordinates.");
            }
            break;

        case THEME_CONTROL_TYPE_PROGRESSBAR:
            if (pControl->hImage || (pTheme->hImage && 0 <= pControl->nSourceX && 0 <= pControl->nSourceY))
            {
                wzWindowClass = WC_STATICW; // no such thing as an owner drawn progress bar so we'll make our own out of a static control.
                dwWindowBits |= SS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            else
            {
                wzWindowClass = PROGRESS_CLASSW;
            }
            break;

        case THEME_CONTROL_TYPE_RICHEDIT:
            if (!vhModuleRichEd)
            {
                hr = LoadSystemLibrary(L"Riched20.dll", &vhModuleRichEd);
                ExitOnFailure(hr, "Failed to load Rich Edit control library.");
            }
            wzWindowClass = RICHEDIT_CLASSW;
            dwWindowBits |= ES_AUTOVSCROLL | ES_MULTILINE | WS_VSCROLL | ES_READONLY;
            break;

        case THEME_CONTROL_TYPE_STATIC:
            wzWindowClass = WC_STATICW;
            dwWindowBits |= SS_ETCHEDHORZ;
            break;

        case THEME_CONTROL_TYPE_TEXT:
            wzWindowClass = WC_STATICW;
            break;
            
        case THEME_CONTROL_TYPE_COMBOBOX:
            wzWindowClass = WC_COMBOBOXW;
            dwWindowBits |= CBS_DROPDOWNLIST | CBS_HASSTRINGS;
            break;
        }
        ExitOnNull(wzWindowClass, hr, E_INVALIDDATA, "Failed to configure control %u because of unknown type: %u", i, pControl->type);

        // Default control ids to the theme id and its index in the control array, unless there
        // is a specific id to assign to a named control.
        WORD wControlId = MAKEWORD(i, pTheme->wId);
        for (DWORD iAssignControl = 0; pControl->sczName && iAssignControl < cAssignControlIds; ++iAssignControl)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pControl->sczName, -1, rgAssignControlIds[iAssignControl].wzName, -1))
            {
                wControlId = rgAssignControlIds[iAssignControl].wId;
                break;
            }
        }

        pControl->wId = wControlId;

        int w, h, x, y;
        GetControlDimensions(&rcParent, pControl, &w, &h, &x, &y);

        // Disable paged controls so their shortcut keys don't trigger when their page isn't being shown.
        dwWindowBits |= 0 < pControl->wPageId ? WS_DISABLED : 0;
        pControl->hWnd = ::CreateWindowExW(dwWindowExBits, wzWindowClass, pControl->sczText, pControl->dwStyle | dwWindowBits, x, y, w, h, pTheme->hwndParent, reinterpret_cast<HMENU>(wControlId), NULL, NULL);
        ExitOnNullWithLastError(pControl->hWnd, hr, "Failed to create window.");

        if (THEME_CONTROL_TYPE_EDITBOX == pControl->type)
        {
            if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE)
            {
                hr = ::SHAutoComplete(pControl->hWnd, SHACF_FILESYS_ONLY);
            }
        }
        else if (THEME_CONTROL_TYPE_LISTVIEW == pControl->type)
        {
            ::SendMessageW(pControl->hWnd, LVM_SETEXTENDEDLISTVIEWSTYLE, 0, pControl->dwExtendedStyle);

            hr = SizeListViewColumns(pControl);
            ExitOnFailure(hr, "Failed to get size of list view columns.");

            for (DWORD j = 0; j < pControl->cColumns; ++j)
            {
                LVCOLUMNW lvc = { };
                lvc.mask = LVCF_FMT | LVCF_WIDTH | LVCF_TEXT | LVCF_SUBITEM;
                lvc.cx = pControl->ptcColumns[j].nWidth;
                lvc.iSubItem = j;
                lvc.pszText = pControl->ptcColumns[j].pszName;
                lvc.fmt = LVCFMT_LEFT;
                lvc.cchTextMax = 4;

                if (-1 == ::SendMessageW(pControl->hWnd, LVM_INSERTCOLUMNW, (WPARAM)(int)(j), (LPARAM)(const LV_COLUMNW *)(&lvc)))
                {
                    ExitWithLastError(hr, "Failed to insert listview column %u into tab control.", j);
                }

                // Return value tells us the old image list, we don't care.
                if (pControl->rghImageList[0])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_NORMAL), reinterpret_cast<LPARAM>(pControl->rghImageList[0]));
                }
                else if (pControl->rghImageList[1])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_SMALL), reinterpret_cast<LPARAM>(pControl->rghImageList[1]));
                }
                else if (pControl->rghImageList[2])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_STATE), reinterpret_cast<LPARAM>(pControl->rghImageList[2]));
                }
                else if (pControl->rghImageList[3])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_GROUPHEADER), reinterpret_cast<LPARAM>(pControl->rghImageList[3]));
                }
            }
        }
        else if (THEME_CONTROL_TYPE_RICHEDIT == pControl->type)
        {
            ::SendMessageW(pControl->hWnd, EM_AUTOURLDETECT, static_cast<WPARAM>(TRUE), 0);
            ::SendMessageW(pControl->hWnd, EM_SETEVENTMASK, 0, ENM_KEYEVENTS | ENM_LINK);
        }
        else if (THEME_CONTROL_TYPE_TAB == pControl->type)
        {
            ULONG_PTR hbrBackground = 0;
            if (THEME_INVALID_ID != pControl->dwFontId)
            {
                hbrBackground = reinterpret_cast<ULONG_PTR>(pTheme->rgFonts[pControl->dwFontId].hBackground);
            }
            else
            {
                hbrBackground = ::GetClassLongPtr(pTheme->hwndParent, GCLP_HBRBACKGROUND);
            }
            ::SetClassLongPtr(pControl->hWnd, GCLP_HBRBACKGROUND, hbrBackground);

            for (DWORD j = 0; j < pControl->cTabs; ++j)
            {
                TCITEMW tci = { };
                tci.mask = TCIF_TEXT | TCIF_IMAGE;
                tci.iImage = -1;
                tci.pszText = pControl->pttTabs[j].pszName;

                if (-1 == ::SendMessageW(pControl->hWnd, TCM_INSERTITEMW, (WPARAM)(int)(j), (LPARAM)(const TC_ITEMW *)(&tci)))
                {
                    ExitWithLastError(hr, "Failed to insert tab %u into tab control.", j);
                }
            }
        }

        if (pControlFont)
        {
            ::SendMessageW(pControl->hWnd, WM_SETFONT, (WPARAM)pControlFont->hFont, FALSE);
        }
    }

LExit:
    return hr;
}


DAPI_(void) ThemeUnloadControls(
    __in THEME* pTheme
    )
{
    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        // TODO: Should the control id get reset as well?
        pTheme->rgControls[i].hWnd = NULL;
    }

    pTheme->hwndHover = NULL;
    pTheme->hwndParent = NULL;
}


DAPI_(HRESULT) ThemeLocalize(
    __in THEME *pTheme,
    __in const WIX_LOCALIZATION *pWixLoc
    )
{
    HRESULT hr = S_OK;
    LOC_CONTROL* pLocControl = NULL;

    hr = LocLocalizeString(pWixLoc, &pTheme->sczCaption);
    ExitOnFailure(hr, "Failed to localize theme caption.");

    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        THEME_CONTROL* pControl = pTheme->rgControls + i;

        hr = LocLocalizeString(pWixLoc, &pControl->sczText);
        ExitOnFailure(hr, "Failed to localize control text.");

        for (DWORD j = 0; j < pControl->cColumns; ++j)
        {
            hr = LocLocalizeString(pWixLoc, &pControl->ptcColumns[j].pszName);
            ExitOnFailure(hr, "Failed to localize column text.");
        }

        for (DWORD j = 0; j < pControl->cTabs; ++j)
        {
            hr = LocLocalizeString(pWixLoc, &pControl->pttTabs[j].pszName);
            ExitOnFailure(hr, "Failed to localize tab text.");
        }

        // Localize controls size, location, and text.
        hr = LocGetControl(pWixLoc, pControl->sczName, &pLocControl);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to localize control.");

        if (LOC_CONTROL_NOT_SET != pLocControl->nX)
        {
            pControl->nX = pLocControl->nX;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nY)
        {
            pControl->nY = pLocControl->nY;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nWidth)
        {
            pControl->nWidth = pLocControl->nWidth;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nHeight)
        {
            pControl->nHeight = pLocControl->nHeight;
        }

        if (pLocControl->wzText && *pLocControl->wzText)
        {
            hr = StrAllocString(&pControl->sczText, pLocControl->wzText, 0);
            ExitOnFailure(hr, "Failed to localize control text.");
        }
    }

LExit:
    return hr;
}


/********************************************************************
 ThemeLoadStrings - Loads string resources.
 Must be called after loading a theme and before calling
 ThemeLoadControls.
*******************************************************************/
DAPI_(HRESULT) ThemeLoadStrings(
    __in THEME* pTheme,
    __in HMODULE hResModule
    )
{
    HRESULT hr = S_OK;
    ExitOnNull(pTheme, hr, S_FALSE, "Theme must be loaded first.");

    if (UINT_MAX != pTheme->uStringId)
    {
        hr = ResReadString(hResModule, pTheme->uStringId, &pTheme->sczCaption);
        ExitOnFailure(hr, "Failed to load theme caption.");
    }

    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        THEME_CONTROL* pControl = pTheme->rgControls + i;

        if (UINT_MAX != pControl->uStringId)
        {
            hr = ResReadString(hResModule, pControl->uStringId, &pControl->sczText);
            ExitOnFailure(hr, "Failed to load control text.");

            for (DWORD j = 0; j < pControl->cColumns; ++j)
            {
                if (UINT_MAX != pControl->ptcColumns[j].uStringId)
                {
                    hr = ResReadString(hResModule, pControl->ptcColumns[j].uStringId, &pControl->ptcColumns[j].pszName);
                    ExitOnFailure(hr, "Failed to load column text.");
                }
            }

            for (DWORD j = 0; j < pControl->cTabs; ++j)
            {
                if (UINT_MAX != pControl->pttTabs[j].uStringId)
                {
                    hr = ResReadString(hResModule, pControl->pttTabs[j].uStringId, &pControl->pttTabs[j].pszName);
                    ExitOnFailure(hr, "Failed to load tab text.");
                }
            }
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeLoadRichEditFromFile(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in_z LPCWSTR wzFileName,
    __in HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFile = NULL;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    hr = PathRelativeToModule(&sczFile, wzFileName, hModule);
    ExitOnFailure(hr, "Failed to read resource data.");

    hFile = ::CreateFileW(sczFile, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open RTF file.");
    }
    else
    {
        LONGLONG llRtfSize;
        hr = FileSizeByHandle(hFile, &llRtfSize);
        if (SUCCEEDED(hr))
        {
            ::SendMessageW(hWnd, EM_EXLIMITTEXT, 0, static_cast<LPARAM>(llRtfSize));
        }

        EDITSTREAM es = { };
        es.pfnCallback = RichEditStreamFromFileHandleCallback;
        es.dwCookie = reinterpret_cast<DWORD_PTR>(hFile);

        ::SendMessageW(hWnd, EM_STREAMIN, SF_RTF, reinterpret_cast<LPARAM>(&es));
        hr = es.dwError;
        ExitOnFailure(hr, "Failed to update RTF stream.");
    }

LExit:
    ReleaseStr(sczFile);
    ReleaseFile(hFile);

    return hr;
}


DAPI_(HRESULT) ThemeLoadRichEditFromResource(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in_z LPCSTR szResourceName,
    __in HMODULE hModule
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    return ThemeLoadRichEditFromResourceToHWnd(hWnd, szResourceName, hModule);
}

DAPI_(HRESULT) ThemeLoadRichEditFromResourceToHWnd(
    __in HWND hWnd,
    __in_z LPCSTR szResourceName,
    __in HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    MEMBUFFER_FOR_RICHEDIT buffer = { };
    EDITSTREAM es = { };

    hr = ResReadData(hModule, szResourceName, reinterpret_cast<LPVOID*>(&buffer.rgbData), &buffer.cbData);
    ExitOnFailure(hr, "Failed to read resource data.");

    es.pfnCallback = RichEditStreamFromMemoryCallback;
    es.dwCookie = reinterpret_cast<DWORD_PTR>(&buffer);

    ::SendMessageW(hWnd, EM_STREAMIN, SF_RTF, reinterpret_cast<LPARAM>(&es));
    hr = es.dwError;
    ExitOnFailure(hr, "Failed to update RTF stream.");

LExit:
    return hr;
}


DAPI_(BOOL) ThemeHandleKeyboardMessage(
    __in_opt THEME* pTheme,
    __in HWND /*hWnd*/,
    __in MSG* pMsg
    )
{
    return pTheme ? ::IsDialogMessageW(pTheme->hwndParent, pMsg) : FALSE;
}


extern "C" LRESULT CALLBACK ThemeDefWindowProc(
    __in_opt THEME* pTheme,
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    int w,h,x,y;
    RECT rcParent = { };
    RECT *pRect = NULL;

    if (pTheme)
    {
        switch (uMsg)
        {
        case WM_NCHITTEST:
            if (pTheme->dwStyle & WS_POPUP)
            {
                return HTCAPTION; // allow pop-up windows to be moved by grabbing any non-control.
            }
            break;

        case WM_WINDOWPOSCHANGED:
            {
                //WINDOWPOS* pos = reinterpret_cast<LPWINDOWPOS>(lParam);
                //ThemeWindowPositionChanged(pTheme, pos);
            }
            break;

        case WM_DRAWITEM:
            ThemeDrawControl(pTheme, reinterpret_cast<LPDRAWITEMSTRUCT>(lParam));
            return TRUE;

        case WM_CTLCOLORSTATIC:
            {
            HBRUSH hBrush = NULL;
            if (ThemeSetControlColor(pTheme, reinterpret_cast<HDC>(wParam), reinterpret_cast<HWND>(lParam), &hBrush))
            {
                return reinterpret_cast<LRESULT>(hBrush);
            }
            }
            break;

        case WM_SETCURSOR:
            if (ThemeHoverControl(pTheme, hWnd, reinterpret_cast<HWND>(wParam)))
            {
                return TRUE;
            }
            break;

        case WM_PAINT:
            if (::GetUpdateRect(hWnd, NULL, FALSE))
            {
                PAINTSTRUCT ps;
                ::BeginPaint(hWnd, &ps);
                ThemeDrawBackground(pTheme, &ps);
                ::EndPaint(hWnd, &ps);
            }
            return 0;

        case WM_SIZING:
            if (pTheme->fAutoResize)
            {
                pRect = reinterpret_cast<RECT *>(lParam);
                if (pRect->right - pRect->left < pTheme->nMinimumWidth)
                {
                    if (wParam == WMSZ_BOTTOMLEFT || wParam == WMSZ_LEFT || wParam == WMSZ_TOPLEFT)
                    {
                        pRect->left = pRect->right - pTheme->nMinimumWidth;
                    }
                    else
                    {
                        pRect->right = pRect->left + pTheme->nMinimumWidth;
                    }
                }
                if (pRect->bottom - pRect->top < pTheme->nMinimumHeight)
                {
                    if (wParam == WMSZ_BOTTOM || wParam == WMSZ_BOTTOMLEFT || wParam == WMSZ_BOTTOMRIGHT)
                    {
                        pRect->bottom = pRect->top + pTheme->nMinimumHeight;
                    }
                    else
                    {
                        pRect->top = pRect->bottom - pTheme->nMinimumHeight;
                    }
                }

                return TRUE;
            }
            break;

        case WM_SIZE:
            if (pTheme->fAutoResize)
            {
                ::GetClientRect(pTheme->hwndParent, &rcParent);
                for (DWORD i = 0; i < pTheme->cControls; ++i)
                {
                    GetControlDimensions(&rcParent, pTheme->rgControls + i, &w, &h, &x, &y);
                    ::MoveWindow(pTheme->rgControls[i].hWnd, x, y, w, h, TRUE);
                    if (THEME_CONTROL_TYPE_LISTVIEW == pTheme->rgControls[i].type)
                    {
                        SizeListViewColumns(pTheme->rgControls + i);

                        for (DWORD j = 0; j < pTheme->rgControls[i].cColumns; ++j)
                        {
                            if (-1 == ::SendMessageW(pTheme->rgControls[i].hWnd, LVM_SETCOLUMNWIDTH, (WPARAM)(int)(j), (LPARAM)(pTheme->rgControls[i].ptcColumns[j].nWidth)))
                            {
                                Trace(REPORT_DEBUG, "Failed to resize listview column %u with error %u", j, ::GetLastError());
                                return 0;
                            }
                        }
                    }
                }

                return 0;
            }
            break;

        case WM_TIMER:
            OnBillboardTimer(pTheme, hWnd, wParam);
            break;

        case WM_NOTIFY:
            if (lParam)
            {
                LPNMHDR pnmhdr = reinterpret_cast<LPNMHDR>(lParam);
                switch (pnmhdr->code)
                {
                // Tab/Shift+Tab support for rich-edit control.
                case EN_MSGFILTER:
                    {
                    MSGFILTER* msgFilter = reinterpret_cast<MSGFILTER*>(lParam);
                    if (WM_KEYDOWN == msgFilter->msg && VK_TAB == msgFilter->wParam)
                    {
                        BOOL fShift = 0x8000 & ::GetKeyState(VK_SHIFT);
                        HWND hwndFocus = ::GetNextDlgTabItem(hWnd, msgFilter->nmhdr.hwndFrom, fShift);
                        ::SetFocus(hwndFocus);
                        return 1;
                    }
                    break;
                    }

                // Hyperlink clicks from rich-edit control.
                case EN_LINK:
                    return SUCCEEDED(OnRichEditEnLink(lParam, pnmhdr->hwndFrom, hWnd));

                // Clicks on a hypertext/syslink control.
                case NM_CLICK: __fallthrough;
                case NM_RETURN:
                    if (ControlIsType(pTheme, static_cast<DWORD>(pnmhdr->idFrom), THEME_CONTROL_TYPE_HYPERTEXT))
                    {
                        PNMLINK pnmlink = reinterpret_cast<PNMLINK>(lParam);
                        LITEM litem = pnmlink->item;
                        ShelExec(litem.szUrl, NULL, L"open", NULL, SW_SHOWDEFAULT, hWnd, NULL);
                        return 1;
                    }

                    return 0;
                }
            }
            break;
        }
    }

    return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}


DAPI_(void) ThemeGetPageIds(
    __in const THEME* pTheme,
    __in_ecount(cGetPages) LPCWSTR* rgwzFindNames,
    __inout_ecount(cGetPages) DWORD* rgdwPageIds,
    __in DWORD cGetPages
    )
{
    for (DWORD i = 0; i < cGetPages; ++i)
    {
        LPCWSTR wzFindName = rgwzFindNames[i];
        for (DWORD j = 0; j < pTheme->cPages; ++j)
        {
            LPCWSTR wzPageName = pTheme->rgPages[j].sczName;
            if (wzPageName && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPageName, -1, wzFindName, -1))
            {
                rgdwPageIds[i] = j + 1; // add one to make the page ids 1-based (so zero is invalid).
                break;
            }
        }
    }
}


DAPI_(THEME_PAGE*) ThemeGetPage(
    __in const THEME* pTheme,
    __in DWORD dwPage
    )
{
    DWORD iPage = dwPage - 1;
    THEME_PAGE* pPage = NULL;

    if (iPage < pTheme->cPages)
    {
        pPage = pTheme->rgPages + iPage;
    }

    return pPage;
}


DAPI_(void) ThemeShowPage(
    __in THEME* pTheme,
    __in DWORD dwPage,
    __in int nCmdShow
    )
{
    DWORD iPage = dwPage - 1;
    HWND hwndFocus = NULL;

    if (iPage < pTheme->cPages)
    {
        const THEME_PAGE* pPage = pTheme->rgPages + iPage;
        for (DWORD i = 0; i < pPage->cControlIndices; ++i)
        {
            const THEME_CONTROL* pControl = pTheme->rgControls + pPage->rgdwControlIndices[i];
            HWND hWnd = pControl->hWnd;

            if (((pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED) && (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED)) 
                || pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDDEN)
            {
                ::ShowWindow(hWnd, SW_HIDE);
            }
            else
            {
                ::EnableWindow(hWnd, SW_HIDE != nCmdShow && !(pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED));

                if (!hwndFocus && SW_HIDE != nCmdShow && pControl->dwStyle & WS_TABSTOP)
                {
                    hwndFocus = hWnd;
                }

                ::ShowWindow(hWnd, nCmdShow);
            }

            if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
            {
                if (SW_HIDE == nCmdShow || (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED))
                {
                    ThemeStopBillboard(pTheme, pControl->wId);
                }
                else
                {
                    ThemeStartBillboard(pTheme, pControl->wId, 0xFFFF);
                }
            }
        }

        if (hwndFocus)
        {
            ::SetFocus(hwndFocus);
        }
    }
}


DAPI_(BOOL) ThemeControlExists(
    __in THEME* pTheme,
    __in DWORD dwControl
    )
{
    BOOL fExists = FALSE;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    if (hWnd)
    {
        const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);
        fExists = (pControl && hWnd == pControl->hWnd);
    }

    return fExists;
}


DAPI_(void) ThemeControlEnable(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in BOOL fEnable
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hWnd));
    if (pControl)
    {
        pControl->dwInternalStyle = fEnable ? (pControl->dwInternalStyle & ~INTERNAL_CONTROL_STYLE_DISABLED) : (pControl->dwInternalStyle | INTERNAL_CONTROL_STYLE_DISABLED);
        ::EnableWindow(hWnd, fEnable);

        if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED)
        {
            ::ShowWindow(hWnd, fEnable ? SW_SHOW : SW_HIDE);
        }
    }
}


DAPI_(BOOL) ThemeControlEnabled(
    __in THEME* pTheme,
    __in DWORD dwControl
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);
    return pControl && !(pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED);
}


DAPI_(void) ThemeControlElevates(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in BOOL fElevates
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    ::SendMessageW(hWnd, BCM_SETSHIELD, 0, fElevates);
}


DAPI_(void) ThemeShowControl(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in int nCmdShow
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    ::ShowWindow(hWnd, nCmdShow);

    // Save the controls visible state.
    THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hWnd));
    if (pControl)
    {
        pControl->dwInternalStyle = (SW_HIDE == nCmdShow) ? (pControl->dwInternalStyle | INTERNAL_CONTROL_STYLE_HIDDEN) : (pControl->dwInternalStyle & ~INTERNAL_CONTROL_STYLE_HIDDEN);
    }
}


DAPI_(BOOL) ThemeControlVisible(
    __in THEME* pTheme,
    __in DWORD dwControl
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    return ::IsWindowVisible(hWnd);
}


DAPI_(BOOL) ThemePostControlMessage(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in UINT Msg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    if (!::PostMessageW(hWnd, Msg, wParam, lParam))
    {
        er = ::GetLastError();
        hr = HRESULT_FROM_WIN32(er);
    }

    return SUCCEEDED(hr);
}


DAPI_(LRESULT) ThemeSendControlMessage(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in UINT Msg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    return ::SendMessageW(hWnd, Msg, wParam, lParam);
}


DAPI_(HRESULT) ThemeDrawBackground(
    __in THEME* pTheme,
    __in PAINTSTRUCT* pps
    )
{
    HRESULT hr = S_FALSE;

    if (pTheme->hImage && 0 <= pTheme->nSourceX && 0 <= pTheme->nSourceY && pps->fErase)
    {
        HDC hdcMem = ::CreateCompatibleDC(pps->hdc);
        HBITMAP hDefaultBitmap = static_cast<HBITMAP>(::SelectObject(hdcMem, pTheme->hImage));

        ::StretchBlt(pps->hdc, 0, 0, pTheme->nWidth, pTheme->nHeight, hdcMem, pTheme->nSourceX, pTheme->nSourceY, pTheme->nWidth, pTheme->nHeight, SRCCOPY);

        ::SelectObject(hdcMem, hDefaultBitmap);
        ::DeleteDC(hdcMem);

        hr = S_OK;
    }

    return hr;
}


DAPI_(HRESULT) ThemeDrawControl(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis
    )
{
    HRESULT hr = S_OK;
    const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, pdis->hwndItem);

    AssertSz(pControl, "Expected control window from owner draw window.");
    AssertSz(pControl->hWnd == pdis->hwndItem, "Expected control window to match owner draw window.");
    AssertSz(pControl->nWidth < 1 || pControl->nWidth == pdis->rcItem.right - pdis->rcItem.left, "Expected control window width to match owner draw window width.");
    AssertSz(pControl->nHeight < 1 || pControl->nHeight == pdis->rcItem.bottom - pdis->rcItem.top, "Expected control window height to match owner draw window height.");

    switch (pControl->type)
    {
    case THEME_CONTROL_TYPE_BILLBOARD:
        hr = DrawBillboard(pTheme, pdis, pControl);
        ExitOnFailure(hr, "Failed to draw billboard.");
        break;

    case THEME_CONTROL_TYPE_BUTTON:
        hr = DrawButton(pTheme, pdis, pControl);
        ExitOnFailure(hr, "Failed to draw button.");
        break;

    case THEME_CONTROL_TYPE_HYPERLINK:
        hr = DrawHyperlink(pTheme, pdis, pControl);
        ExitOnFailure(hr, "Failed to draw hyperlink.");
        break;

    case THEME_CONTROL_TYPE_IMAGE:
        hr = DrawImage(pTheme, pdis, pControl);
        ExitOnFailure(hr, "Failed to draw image.");
        break;

    case THEME_CONTROL_TYPE_PROGRESSBAR:
        hr = DrawProgressBar(pTheme, pdis, pControl);
        ExitOnFailure(hr, "Failed to draw progress bar.");
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnRootFailure(hr, "Did not specify an owner draw control to draw.");
    }

LExit:
    return hr;
}


DAPI_(BOOL) ThemeHoverControl(
    __in THEME* pTheme,
    __in HWND hwndParent,
    __in HWND hwndControl
    )
{
    BOOL fHovered = FALSE;
    if (hwndControl != pTheme->hwndHover)
    {
        if (pTheme->hwndHover && pTheme->hwndHover != hwndParent)
        {
            DrawHoverControl(pTheme, FALSE);
        }

        pTheme->hwndHover = hwndControl;

        if (pTheme->hwndHover && pTheme->hwndHover != hwndParent)
        {
            fHovered = DrawHoverControl(pTheme, TRUE);
        }
    }

    return fHovered;
}


DAPI_(BOOL) ThemeIsControlChecked(
    __in THEME* pTheme,
    __in DWORD dwControl
    )
{
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    return BST_CHECKED == ::SendMessageW(hWnd, BM_GETCHECK, 0, 0);
}


DAPI_(BOOL) ThemeSetControlColor(
    __in THEME* pTheme,
    __in HDC hdc,
    __in HWND hWnd,
    __out HBRUSH* phBackgroundBrush
    )
{
    THEME_FONT* pFont = NULL;
    BOOL fHasBackground = FALSE;

    if (hWnd == pTheme->hwndParent)
    {
        pFont = (THEME_INVALID_ID == pTheme->dwFontId) ? NULL : pTheme->rgFonts + pTheme->dwFontId;
    }
    else
    {
        const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);
        pFont = (!pControl || THEME_INVALID_ID == pControl->dwFontId) ? NULL : pTheme->rgFonts + pControl->dwFontId;
    }

    if (pFont)
    {
        if (pFont->hForeground)
        {
            ::SetTextColor(hdc, pFont->crForeground);
        }

        if (pFont->hBackground)
        {
            ::SetBkColor(hdc, pFont->crBackground);

            *phBackgroundBrush = pFont->hBackground;
            fHasBackground = TRUE;
        }
        else
        {
            ::SetBkMode(hdc, TRANSPARENT);
            *phBackgroundBrush = static_cast<HBRUSH>(::GetStockObject(NULL_BRUSH));
            fHasBackground = TRUE;
        }
    }

    return fHasBackground;
}


DAPI_(HRESULT) ThemeStartBillboard(
    __in const THEME* pTheme,
    __in DWORD dwControl,
    __in WORD iImage
    )
{
    HRESULT hr = E_NOTFOUND;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    if (hWnd)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hWnd));
        if (pControl && THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
        {
            WORD wStart = static_cast<WORD>((iImage < pControl->cBillboards) ? iImage : (pControl->dwData < pControl->cBillboards) ? pControl->dwData : 0);

            pControl->dwData = wStart;
            if (!::SetTimer(pTheme->hwndParent, pControl->wId, pControl->wBillboardInterval, NULL))
            {
                ExitWithLastError(hr, "Failed to start billboard.");
            }

            hr = S_OK;
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeStopBillboard(
    __in const THEME* pTheme,
    __in DWORD dwControl
    )
{
    HRESULT hr = E_NOTFOUND;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    if (hWnd)
    {
        const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);
        if (pControl && THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
        {
            if (::KillTimer(pTheme->hwndParent, pControl->wId))
            {
                hr = S_OK;
            }
        }
    }

    return hr;
}


DAPI_(HRESULT) ThemeSetProgressControl(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in DWORD dwProgressPercentage
    )
{
    HRESULT hr = E_NOTFOUND;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    if (hWnd)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hWnd));
        if (pControl)
        {
            DWORD dwCurrentProgress = LOWORD(pControl->dwData);

            if (dwCurrentProgress != dwProgressPercentage)
            {
                DWORD dwColor = HIWORD(pControl->dwData);
                pControl->dwData = MAKEDWORD(dwProgressPercentage, dwColor);

                if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW)
                {
                    if (!::InvalidateRect(hWnd, NULL, FALSE))
                    {
                        ExitWithLastError(hr, "Failed to invalidate progress bar window.");
                    }
                }
                else
                {
                    ::SendMessageW(hWnd, PBM_SETPOS, dwProgressPercentage, 0);
                }

                hr = S_OK;
            }
            else
            {
                hr = S_FALSE;
            }
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeSetProgressControlColor(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in DWORD dwColorIndex
    )
{
    HRESULT hr = S_FALSE;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    if (hWnd)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hWnd));

        // Only set color on owner draw progress bars.
        if (pControl && (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW))
        {
            DWORD dwCurrentColor = HIWORD(pControl->dwData);

            if (dwCurrentColor != dwColorIndex)
            {
                DWORD dwCurrentProgress =  LOWORD(pControl->dwData);
                pControl->dwData = MAKEDWORD(dwCurrentProgress, dwColorIndex);

                if (!::InvalidateRect(hWnd, NULL, FALSE))
                {
                    ExitWithLastError(hr, "Failed to invalidate progress bar window.");
                }

                hr = S_OK;
            }
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeSetTextControl(
    __in THEME* pTheme,
    __in DWORD dwControl,
    __in_z LPCWSTR wzText
    )
{
    HRESULT hr = S_OK;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);

    if (hWnd && !::SetWindowTextW(hWnd, wzText))
    {
        ExitWithLastError(hr, "Failed to set control text.");
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeGetTextControl(
    __in const THEME* pTheme,
    __in DWORD dwControl,
    __out LPWSTR* psczText
    )
{
    HRESULT hr = S_OK;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    DWORD cchText = 0;
    DWORD cchTextRead = 0;

    // Ensure the string has room for at least one character.
    hr = StrMaxLength(*psczText, reinterpret_cast<DWORD_PTR*>(&cchText));
    ExitOnFailure(hr, "Failed to get text buffer length.");

    if (!cchText)
    {
        cchText = GROW_WINDOW_TEXT;

        hr = StrAlloc(psczText, cchText);
        ExitOnFailure(hr, "Failed to grow text buffer.");
    }

    // Read (and keep growing buffer) until we finally read less than there
    // is room in the buffer.
    for (;;)
    {
        cchTextRead = ::GetWindowTextW(hWnd, *psczText, cchText);
        if (cchTextRead + 1 < cchText)
        {
            break;
        }
        else
        {
            cchText = cchTextRead + GROW_WINDOW_TEXT;

            hr = StrAlloc(psczText, cchText);
            ExitOnFailure(hr, "Failed to grow text buffer again.");
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeUpdateCaption(
    __in THEME* pTheme,
    __in_z LPCWSTR wzCaption
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocString(&pTheme->sczCaption, wzCaption, 0);
    ExitOnFailure(hr, "Failed to update theme caption.");

LExit:
    return hr;
}


DAPI_(void) ThemeSetFocus(
    __in THEME* pTheme,
    __in DWORD dwControl
    )
{
    HWND hwndFocus = ::GetDlgItem(pTheme->hwndParent, dwControl); 
    if (hwndFocus && !ThemeControlEnabled(pTheme, dwControl)) 
    { 
        hwndFocus = ::GetNextDlgTabItem(pTheme->hwndParent, hwndFocus, FALSE); 
    }

    if (hwndFocus) 
    { 
        ::SetFocus(hwndFocus); 
    }
}


// Internal functions.

static HRESULT ParseTheme(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMDocument* pixd,
    __out THEME** ppTheme
    )
{
    static WORD wThemeId = 0;

    HRESULT hr = S_OK;
    THEME* pTheme = NULL;
    IXMLDOMElement *pThemeElement = NULL;

    UINT nDpiX;
    UINT nDpiY;

    hr = pixd->get_documentElement(&pThemeElement);
    ExitOnFailure(hr, "Failed to get theme element.");

    pTheme = static_cast<THEME*>(MemAlloc(sizeof(THEME), TRUE));
    ExitOnNull(pTheme, hr, E_OUTOFMEMORY, "Failed to allocate memory for theme.");

    pTheme->wId = ++wThemeId;

    GetDpiForMonitor(NULL, &nDpiX, &nDpiY);
    pTheme->fScaleFactorX = GetScaleFactorForDpi(nDpiX);
    pTheme->fScaleFactorY = GetScaleFactorForDpi(nDpiY);

    // Parse the optional background resource image.
    hr = ParseImage(hModule, wzRelativePath, pThemeElement, &pTheme->hImage);
    ExitOnFailure(hr, "Failed while parsing theme image.");

    // Parse the optional window style.
    hr = XmlGetAttributeNumberBase(pThemeElement, L"HexStyle", 16, &pTheme->dwStyle);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumberBase(pThemeElement, L"s", 16, &pTheme->dwStyle);
    }
    ExitOnFailure(hr, "Failed to get theme window style (t@s) attribute.");

    // Parse the application element.
    hr = ParseApplication(hModule, wzRelativePath, pThemeElement, pTheme);
    ExitOnFailure(hr, "Failed to parse theme application element.");

    // Parse the fonts.
    hr = ParseFonts(pThemeElement, pTheme);
    ExitOnFailure(hr, "Failed to parse theme fonts.");

    // Parse any imagelists.
    hr = ParseImageLists(hModule, wzRelativePath, pThemeElement, pTheme);
    ExitOnFailure(hr, "Failed to parse image lists.");

    // Parse the pages.
    hr = ParsePages(hModule, wzRelativePath, pThemeElement, pTheme);
    ExitOnFailure(hr, "Failed to parse theme pages.");

    // Parse the non-paged controls.
    hr = ParseControls(hModule, wzRelativePath, pThemeElement, pTheme, NULL);
    ExitOnFailure(hr, "Failed to parse theme controls.");

    *ppTheme = pTheme;
    pTheme = NULL;

LExit:
    ReleaseObject(pThemeElement);

    if (pTheme)
    {
        ThemeFree(pTheme);
    }

    return hr;
}

static HRESULT ParseImage(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __out HBITMAP* phImage
    )
{
    HRESULT hr = S_OK;
    BSTR bstr = NULL;
    LPWSTR sczImageFile = NULL;
    int iResourceId = 0;
    Gdiplus::Bitmap* pBitmap = NULL;

    hr = XmlGetAttribute(pElement, L"ImageResource", &bstr);
    ExitOnFailure(hr, "Failed to get image resource attribute.");

    if (S_OK == hr)
    {
        iResourceId = wcstol(bstr, NULL, 10);

        hr = GdipBitmapFromResource(hModule, MAKEINTRESOURCE(iResourceId), &pBitmap);
        // Don't fail.
    }

    ReleaseNullBSTR(bstr);

    // Parse the optional background image from a given file.
    if (!pBitmap)
    {
        hr = XmlGetAttribute(pElement, L"ImageFile", &bstr);
        ExitOnFailure(hr, "Failed to get image file attribute.");

        if (S_OK == hr)
        {
            if (wzRelativePath)
            {
                hr = PathConcat(wzRelativePath, bstr, &sczImageFile);
                ExitOnFailure(hr, "Failed to combine image file path.");
            }
            else
            {
                hr = PathRelativeToModule(&sczImageFile, bstr, hModule);
                ExitOnFailure(hr, "Failed to get image filename.");
            }

            hr = GdipBitmapFromFile(sczImageFile, &pBitmap);
            // Don't fail.
        }
    }

    // If there is an image, convert it into a bitmap handle.
    if (pBitmap)
    {
        Gdiplus::Color black;
        Gdiplus::Status gs = pBitmap->GetHBITMAP(black, phImage);
        ExitOnGdipFailure(gs, hr, "Failed to convert GDI+ bitmap into HBITMAP.");
    }

    hr = S_OK;

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    ReleaseStr(sczImageFile);
    ReleaseBSTR(bstr);

    return hr;
}

static HRESULT ParseApplication(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixn = NULL;
    BSTR bstr = NULL;
    LPWSTR sczIconFile = NULL;

    hr = XmlSelectSingleNode(pElement, L"Window|Application|App|a", &pixn);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }
    ExitOnFailure(hr, "Failed to find application element.");

    hr = XmlGetYesNoAttribute(pixn, L"AutoResize", &pTheme->fAutoResize);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get AutoResize attribute.");

    hr = XmlGetAttributeNumber(pixn, L"Width", reinterpret_cast<DWORD*>(&pTheme->nWidth));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"w", reinterpret_cast<DWORD*>(&pTheme->nWidth));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Failed to find application width attribute.");
        }
    }
    ExitOnFailure(hr, "Failed to get application width attribute.");

    hr = XmlGetAttributeNumber(pixn, L"Height", reinterpret_cast<DWORD*>(&pTheme->nHeight));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"h", reinterpret_cast<DWORD*>(&pTheme->nHeight));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Failed to find application height attribute.");
        }
    }
    ExitOnFailure(hr, "Failed to get application height attribute.");

    hr = XmlGetAttributeNumber(pixn, L"MinimumWidth", reinterpret_cast<DWORD*>(&pTheme->nMinimumWidth));
    if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get application minimum width attribute.");

    hr = XmlGetAttributeNumber(pixn, L"MinimumHeight", reinterpret_cast<DWORD*>(&pTheme->nMinimumHeight));
    if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to get application minimum height attribute.");

    hr = XmlGetAttributeNumber(pixn, L"FontId", &pTheme->dwFontId);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"f", &pTheme->dwFontId);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Failed to find application font attribute.");
        }
    }
    ExitOnFailure(hr, "Failed to get application font attribute.");

    // Get the optional application icon from a resource.
    hr = XmlGetAttribute(pixn, L"IconResource", &bstr);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttribute(pixn, L"i", &bstr);
    }
    ExitOnFailure(hr, "Failed to get application icon resource attribute.");

    if (S_OK == hr)
    {
        pTheme->hIcon = ::LoadIconW(hModule, bstr);
        ExitOnNullWithLastError(pTheme->hIcon, hr, "Failed to load application icon.");

        ReleaseNullBSTR(bstr);
    }

    // Get the optional application icon from a file.
    hr = XmlGetAttribute(pixn, L"IconFile", &bstr);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttribute(pixn, L"if", &bstr);
    }
    ExitOnFailure(hr, "Failed to get application icon file attribute.");

    if (S_OK == hr)
    {
        if (wzRelativePath)
        {
            hr = PathConcat(wzRelativePath, bstr, &sczIconFile);
            ExitOnFailure(hr, "Failed to combine icon file path.");
        }
        else
        {
            hr = PathRelativeToModule(&sczIconFile, bstr, hModule);
            ExitOnFailure(hr, "Failed to get icon filename.");
        }
        ReleaseNullBSTR(bstr);

        pTheme->hIcon = ::LoadImageW(NULL, sczIconFile, IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_LOADFROMFILE);
        ExitOnNullWithLastError(pTheme->hIcon, hr, "Failed to load application icon: %ls.", bstr);
    }

    hr = XmlGetAttributeNumber(pixn, L"SourceX", reinterpret_cast<DWORD*>(&pTheme->nSourceX));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sx", reinterpret_cast<DWORD*>(&pTheme->nSourceX));
        if (S_FALSE == hr)
        {
            pTheme->nSourceX = -1;
        }
    }
    ExitOnFailure(hr, "Failed to get application source X attribute.");

    hr = XmlGetAttributeNumber(pixn, L"SourceY", reinterpret_cast<DWORD*>(&pTheme->nSourceY));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sy", reinterpret_cast<DWORD*>(&pTheme->nSourceY));
        if (S_FALSE == hr)
        {
            pTheme->nSourceY = -1;
        }
    }
    ExitOnFailure(hr, "Failed to get application source Y attribute.");

    // Parse the optional window style.
    hr = XmlGetAttributeNumberBase(pixn, L"HexStyle", 16, &pTheme->dwStyle);
    ExitOnFailure(hr, "Failed to get theme window style (Window@HexStyle) attribute.");

    if (S_FALSE == hr)
    {
        pTheme->dwStyle = WS_VISIBLE | WS_MINIMIZEBOX | WS_SYSMENU;
        pTheme->dwStyle |= (0 <= pTheme->nSourceX && 0 <= pTheme->nSourceY) ? WS_POPUP : WS_OVERLAPPED;
    }

    hr = XmlGetAttributeNumber(pixn, L"StringId", reinterpret_cast<DWORD*>(&pTheme->uStringId));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sid", reinterpret_cast<DWORD*>(&pTheme->uStringId));
    }
    ExitOnFailure(hr, "Failed to get application caption id attribute.");

    if (S_FALSE == hr)
    {
        pTheme->uStringId = UINT_MAX;

        hr = XmlGetText(pixn, &bstr);
        ExitOnFailure(hr, "Failed to get application caption.");

        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Failed to find application caption.");
        }

        hr = StrAllocString(&pTheme->sczCaption, bstr, 0);
        ExitOnFailure(hr, "Failed to copy application caption.");
    }

    ScaleApplication(pTheme);

LExit:
    ReleaseStr(sczIconFile);
    ReleaseBSTR(bstr);
    ReleaseObject(pixn);

    return hr;
}


static HRESULT ParseFonts(
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    BSTR bstrName = NULL;
    DWORD dwId = 0;
    LOGFONTW lf = { };
    COLORREF crForeground = THEME_INVISIBLE_COLORREF;
    COLORREF crBackground = THEME_INVISIBLE_COLORREF;

    hr = XmlSelectNodes(pElement, L"Font|f", &pixnl);
    ExitOnFailure(hr, "Failed to find font elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pTheme->cFonts));
    ExitOnFailure(hr, "Failed to count the number of theme fonts.");

    if (!pTheme->cFonts)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgFonts = static_cast<THEME_FONT*>(MemAlloc(sizeof(THEME_FONT) * pTheme->cFonts, TRUE));
    ExitOnNull(pTheme->rgFonts, hr, E_OUTOFMEMORY, "Failed to allocate theme fonts.");

    lf.lfQuality = CLEARTYPE_QUALITY;

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, NULL)))
    {
        hr = XmlGetAttributeNumber(pixn, L"Id", &dwId);
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumber(pixn, L"id", &dwId);
            if (S_FALSE == hr)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            }
        }
        ExitOnFailure(hr, "Failed to find font id.");

        if (pTheme->cFonts <= dwId)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Invalid theme font id.");
        }

        hr = XmlGetText(pixn, &bstrName);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnFailure(hr, "Failed to get font name.");

        hr = ::StringCchCopyW(lf.lfFaceName, countof(lf.lfFaceName), bstrName);
        ExitOnFailure(hr, "Failed to copy font name.");

        hr = XmlGetAttributeNumber(pixn, L"Height", reinterpret_cast<DWORD*>(&lf.lfHeight));
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumber(pixn, L"h", reinterpret_cast<DWORD*>(&lf.lfHeight));
            if (S_FALSE == hr)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            }
        }
        ExitOnFailure(hr, "Failed to find font height attribute.");

        hr = XmlGetAttributeNumber(pixn, L"Weight", reinterpret_cast<DWORD*>(&lf.lfWeight));
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumber(pixn, L"w", reinterpret_cast<DWORD*>(&lf.lfWeight));
            if (S_FALSE == hr)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            }
        }
        ExitOnFailure(hr, "Failed to find font weight attribute.");

        hr = XmlGetYesNoAttribute(pixn, L"Underline", reinterpret_cast<BOOL*>(&lf.lfUnderline));
        if (E_NOTFOUND == hr)
        {
            hr = XmlGetYesNoAttribute(pixn, L"u", reinterpret_cast<BOOL*>(&lf.lfUnderline));
            if (E_NOTFOUND == hr)
            {
                lf.lfUnderline = FALSE;
                hr = S_OK;
            }
        }
        ExitOnFailure(hr, "Failed to find font underline attribute.");

        hr = XmlGetAttributeNumberBase(pixn, L"Foreground", 16, &crForeground);
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumberBase(pixn, L"f", 16, &crForeground);
            if (S_FALSE == hr)
            {
                crForeground = THEME_INVISIBLE_COLORREF;
                hr = S_OK;
            }
        }
        ExitOnFailure(hr, "Failed to find font foreground color.");

        hr = XmlGetAttributeNumberBase(pixn, L"Background", 16, &crBackground);
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumberBase(pixn, L"b", 16, &crBackground);
            if (S_FALSE == hr)
            {
                crBackground = THEME_INVISIBLE_COLORREF;
                hr = S_OK;
            }
        }
        ExitOnFailure(hr, "Failed to find font background color.");

        THEME_FONT* pFont = pTheme->rgFonts + dwId;
        if (pFont->hFont)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            ExitOnRootFailure(hr, "Theme font id duplicated.");
        }

        ScaleFont(pTheme, &lf);

        pFont->hFont = ::CreateFontIndirectW(&lf);
        ExitOnNullWithLastError(pFont->hFont, hr, "Failed to create product title font.");

        pFont->crForeground = crForeground;
        if (THEME_INVISIBLE_COLORREF != pFont->crForeground)
        {
            pFont->hForeground = ::CreateSolidBrush(pFont->crForeground);
            ExitOnNullWithLastError(pFont->hForeground, hr, "Failed to create text foreground brush.");
        }

        pFont->crBackground = crBackground;
        if (THEME_INVISIBLE_COLORREF != pFont->crBackground)
        {
            pFont->hBackground = ::CreateSolidBrush(pFont->crBackground);
            ExitOnNullWithLastError(pFont->hBackground, hr, "Failed to create text background brush.");
        }

        ReleaseNullBSTR(bstrName);
        ReleaseNullObject(pixn);
    }
    ExitOnFailure(hr, "Failed to enumerate all fonts.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT ParsePages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    BSTR bstrType = NULL;
    THEME_PAGE* pPage = NULL;
    DWORD iPage = 0;

    hr = XmlSelectNodes(pElement, L"Page", &pixnl);
    ExitOnFailure(hr, "Failed to find page elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pTheme->cPages));
    ExitOnFailure(hr, "Failed to count the number of theme pages.");

    if (!pTheme->cPages)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgPages = static_cast<THEME_PAGE*>(MemAlloc(sizeof(THEME_PAGE) * pTheme->cPages, TRUE));
    ExitOnNull(pTheme->rgPages, hr, E_OUTOFMEMORY, "Failed to allocate theme pages.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, &bstrType)))
    {
        pPage = pTheme->rgPages + iPage;

        pPage->wId = static_cast<WORD>(iPage + 1);

        hr = XmlGetAttributeEx(pixn, L"Name", &pPage->sczName);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed when querying page Name.");

        hr = ParseControls(hModule, wzRelativePath, pixn, pTheme, pPage);
        ExitOnFailure(hr, "Failed to parse page controls.");

        ++iPage;

        ReleaseNullBSTR(bstrType);
        ReleaseNullObject(pixn);
    }
    ExitOnFailure(hr, "Failed to enumerate all pages.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrType);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT ParseImageLists(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlImageLists = NULL;
    IXMLDOMNode* pixnImageList = NULL;
    IXMLDOMNodeList* pixnlImages = NULL;
    IXMLDOMNode* pixnImage = NULL;
    DWORD dwImageListIndex = 0;
    DWORD dwImageCount = 0;
    HBITMAP hBitmap = NULL;
    BITMAP bm = { };
    BSTR bstr = NULL;
    DWORD i = 0;
    int iRetVal = 0;

    hr = XmlSelectNodes(pElement, L"ImageList", &pixnlImageLists);
    ExitOnFailure(hr, "Failed to find ImageList elements.");

    hr = pixnlImageLists->get_length(reinterpret_cast<long*>(&pTheme->cImageLists));
    ExitOnFailure(hr, "Failed to count the number of image lists.");

    if (!pTheme->cImageLists)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgImageLists = static_cast<THEME_IMAGELIST*>(MemAlloc(sizeof(THEME_IMAGELIST) * pTheme->cImageLists, TRUE));
    ExitOnNull(pTheme->rgImageLists, hr, E_OUTOFMEMORY, "Failed to allocate theme image lists.");

    while (S_OK == (hr = XmlNextElement(pixnlImageLists, &pixnImageList, NULL)))
    {
        hr = XmlGetAttribute(pixnImageList, L"Name", &bstr);
        ExitOnFailure(hr, "Failed to get ImageList/@Name attribute.");

        hr = StrAllocString(&pTheme->rgImageLists[dwImageListIndex].sczName, bstr, 0);
        ExitOnFailure(hr, "Failed to make copy of ImageList name");

        hr = XmlSelectNodes(pixnImageList, L"Image|i", &pixnlImages);
        ExitOnFailure(hr, "Failed to select child Image|i nodes");

        hr = pixnlImages->get_length(reinterpret_cast<long*>(&dwImageCount));
        ExitOnFailure(hr, "Failed to count the number of images in list.");

        if (0 < dwImageCount)
        {
            i = 0;
            while (S_OK == (hr = XmlNextElement(pixnlImages, &pixnImage, NULL)))
            {
                if (hBitmap)
                {
                    ::DeleteObject(hBitmap);
                    hBitmap = NULL;
                }
                hr = ParseImage(hModule, wzRelativePath, pixnImage, &hBitmap);
                ExitOnFailure(hr, "Failed to parse image: %u", i);

                if (0 == i)
                {
                    ::GetObjectW(hBitmap, sizeof(BITMAP), &bm);

                    pTheme->rgImageLists[dwImageListIndex].hImageList = ImageList_Create(bm.bmWidth, bm.bmHeight, ILC_COLOR24, dwImageCount, 0);
                    ExitOnNullWithLastError(pTheme->rgImageLists[dwImageListIndex].hImageList, hr, "Failed to create image list");
                }

                iRetVal = ImageList_Add(pTheme->rgImageLists[dwImageListIndex].hImageList, hBitmap, NULL);
                if (-1 == iRetVal)
                {
                    ExitWithLastError(hr, "Failed to add image %u to image list.", i);
                }

                ++i;
            }
        }
        ++dwImageListIndex;
    }

LExit:
    if (hBitmap)
    {
        ::DeleteObject(hBitmap);
    }
    ReleaseBSTR(bstr);
    ReleaseObject(pixnlImageLists);
    ReleaseObject(pixnImageList);
    ReleaseObject(pixnlImages);
    ReleaseObject(pixnImage);

    return hr;
}


static HRESULT ParseControls(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in_opt THEME_PAGE* pPage
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    BSTR bstrType = NULL;
    DWORD cNewControls = 0;
    DWORD iControl = 0;
    DWORD iPageControl = 0;

    hr = XmlSelectNodes(pElement, L"*", &pixnl);
    ExitOnFailure(hr, "Failed to find control elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&cNewControls));
    ExitOnFailure(hr, "Failed to count the number of theme controls.");

    // If we are creating top level controls (no page provided), subtract the font and
    // page elements and "application" element since they are all siblings and inflate
    // the count.
    if (!pPage)
    {
        cNewControls = cNewControls - pTheme->cFonts - pTheme->cPages - pTheme->cImageLists - 1;
    }

    if (!cNewControls)
    {
        ExitFunction1(hr = S_OK);
    }

    if (pPage)
    {
        hr = ::SizeTMult(sizeof(DWORD), cNewControls, &cbAllocSize);
        ExitOnFailure(hr, "Overflow while calculating allocation size for %u control indices.", cNewControls);

        pPage->rgdwControlIndices = static_cast<DWORD*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pPage->rgdwControlIndices, hr, E_OUTOFMEMORY, "Failed to allocate theme page controls.");

        pPage->cControlIndices = cNewControls;
    }

    iControl = pTheme->cControls;
    pTheme->cControls += cNewControls;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pTheme->rgControls), pTheme->cControls, sizeof(THEME_CONTROL), cNewControls);
    ExitOnFailure(hr, "Failed to allocate theme controls.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, &bstrType)))
    {
        THEME_CONTROL_TYPE type = THEME_CONTROL_TYPE_UNKNOWN;

        if (!bstrType)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Null element encountered!");
        }

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Billboard", -1))
        {
            type = THEME_CONTROL_TYPE_BILLBOARD;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Button", -1) ||
            CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"b", 1))
        {
            type = THEME_CONTROL_TYPE_BUTTON;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Checkbox", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"cb", 2))
        {
            type = THEME_CONTROL_TYPE_CHECKBOX;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Editbox", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"eb", 2))
        {
            type = THEME_CONTROL_TYPE_EDITBOX;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Hyperlink", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"l", 1))
        {
            type = THEME_CONTROL_TYPE_HYPERLINK;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Hypertext", -1))
        {
            type = THEME_CONTROL_TYPE_HYPERTEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Image", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"i", 1))
        {
            type = THEME_CONTROL_TYPE_IMAGE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Progressbar", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"pb", 2))
        {
            type = THEME_CONTROL_TYPE_PROGRESSBAR;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Richedit", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"rt", 2))
        {
            type = THEME_CONTROL_TYPE_RICHEDIT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Static", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"s", 1))
        {
            type = THEME_CONTROL_TYPE_STATIC;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Text", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"t", 1))
        {
            type = THEME_CONTROL_TYPE_TEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ListView", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Listview", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"lv", 2))
        {
            type = THEME_CONTROL_TYPE_LISTVIEW;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"TreeView", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Treeview", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"tv", 2))
        {
            type = THEME_CONTROL_TYPE_TREEVIEW;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Tab", -1) ||
                 CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"tb", 2))
        {
            type = THEME_CONTROL_TYPE_TAB;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Combobox", -1))
        {
            type = THEME_CONTROL_TYPE_COMBOBOX;
        }

        if (THEME_CONTROL_TYPE_UNKNOWN != type)
        {
            hr = ParseControl(hModule, wzRelativePath, pixn, type, pTheme, iControl);
            ExitOnFailure(hr, "Failed to parse control.");

            if (pPage)
            {
                pTheme->rgControls[iControl].wPageId = pPage->wId;
                pPage->rgdwControlIndices[iPageControl] = iControl;
                ++iPageControl;
            }

            ++iControl;
        }

        ReleaseNullBSTR(bstrType);
        ReleaseNullObject(pixn);
    }
    ExitOnFailure(hr, "Failed to enumerate all controls.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrType);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT ParseControl(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL_TYPE type,
    __in THEME* pTheme,
    __in DWORD iControl
    )
{
    HRESULT hr = S_OK;
    DWORD dwId = 0;
    THEME_CONTROL* pControl = NULL;
    DWORD dwValue = 0;
    BOOL fValue = FALSE;
    BSTR bstrText = NULL;

    hr = XmlGetAttributeNumber(pixn, L"id", &dwId);
    if (S_FALSE == hr)
    {
        dwId = iControl;
    }
    ExitOnFailure(hr, "Failed to find control id.");

    if (pTheme->cControls <= dwId)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Invalid theme control id.");
    }

    pControl = pTheme->rgControls + dwId;
    if (THEME_CONTROL_TYPE_UNKNOWN != pControl->type)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Theme control id duplicated.");
    }

    pControl->type = type;

    hr = XmlGetAttributeEx(pixn, L"Name", &pControl->sczName);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed when querying control Name.");

    hr = XmlGetAttributeNumber(pixn, L"X", reinterpret_cast<DWORD*>(&pControl->nX));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"x", reinterpret_cast<DWORD*>(&pControl->nX));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
    }
    ExitOnFailure(hr, "Failed to find control X attribute.");

    hr = XmlGetAttributeNumber(pixn, L"Y", reinterpret_cast<DWORD*>(&pControl->nY));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"y", reinterpret_cast<DWORD*>(&pControl->nY));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
    }
    ExitOnFailure(hr, "Failed to find control Y attribute.");

    hr = XmlGetAttributeNumber(pixn, L"Height", reinterpret_cast<DWORD*>(&pControl->nHeight));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"h", reinterpret_cast<DWORD*>(&pControl->nHeight));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
    }
    ExitOnFailure(hr, "Failed to find control height attribute.");

    hr = XmlGetAttributeNumber(pixn, L"Width", reinterpret_cast<DWORD*>(&pControl->nWidth));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"w", reinterpret_cast<DWORD*>(&pControl->nWidth));
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
    }
    ExitOnFailure(hr, "Failed to find control width attribute.");

    // Parse the optional background resource image.
    hr = ParseImage(hModule, wzRelativePath, pixn, &pControl->hImage);
    ExitOnFailure(hr, "Failed while parsing control image.");

    hr = XmlGetAttributeNumber(pixn, L"SourceX", reinterpret_cast<DWORD*>(&pControl->nSourceX));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sx", reinterpret_cast<DWORD*>(&pControl->nSourceX));
        if (S_FALSE == hr)
        {
            pControl->nSourceX = -1;
        }
    }
    ExitOnFailure(hr, "Failed to find control source X attribute.");

    hr = XmlGetAttributeNumber(pixn, L"SourceY", reinterpret_cast<DWORD*>(&pControl->nSourceY));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sy", reinterpret_cast<DWORD*>(&pControl->nSourceY));
        if (S_FALSE == hr)
        {
            pControl->nSourceY = -1;
        }
    }
    ExitOnFailure(hr, "Failed to find control source Y attribute.");

    hr = XmlGetAttributeNumber(pixn, L"FontId", &pControl->dwFontId);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"f", &pControl->dwFontId);
        if (S_FALSE == hr)
        {
            pControl->dwFontId = THEME_INVALID_ID;
        }
    }
    ExitOnFailure(hr, "Failed to find font for control.");

    hr = XmlGetAttributeNumber(pixn, L"HoverFontId", &pControl->dwFontHoverId);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"fh", &pControl->dwFontHoverId);
        if (S_FALSE == hr)
        {
            pControl->dwFontHoverId = THEME_INVALID_ID;
        }
    }
    ExitOnFailure(hr, "Failed to find hover font for control.");

    hr = XmlGetAttributeNumber(pixn, L"SelectedFontId", &pControl->dwFontSelectedId);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"fs", &pControl->dwFontSelectedId);
        if (S_FALSE == hr)
        {
            pControl->dwFontSelectedId = THEME_INVALID_ID;
        }
    }
    ExitOnFailure(hr, "Failed to find selected font for control.");

    // Parse the optional window style.
    hr = XmlGetAttributeNumberBase(pixn, L"HexStyle", 16, &pControl->dwStyle);
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumberBase(pixn, L"s", 16, &pControl->dwStyle);
        ExitOnFailure(hr, "Failed to get control window style (@s) attribute.");
    }

    // Parse the tabstop bit "shortcut nomenclature", this could have been set with the style above.
    hr = XmlGetYesNoAttribute(pixn, L"TabStop", &fValue);
    if (E_NOTFOUND == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"t", &dwValue);
        if (S_OK == hr && dwValue)
        {
            fValue = TRUE;
        }
    }
    ExitOnFailure(hr, "Failed to tell if the control is a tab stop.");

    if (S_OK == hr && fValue)
    {
        pControl->dwStyle |= WS_TABSTOP;
    }

    hr = XmlGetYesNoAttribute(pixn, L"Visible", &fValue);
    if (E_NOTFOUND == hr)
    {
        hr = S_OK;
    }
    else if (fValue)
    {
        pControl->dwStyle |= WS_VISIBLE;
    }
    ExitOnFailure(hr, "Failed to tell if the control is visible.");

    hr = XmlGetYesNoAttribute(pixn, L"HideWhenDisabled", &fValue);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to parse if the control should be hidden when disabled.");

        if (fValue)
        {
            pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED;
        }
    }

    hr = XmlGetYesNoAttribute(pixn, L"FileSystemAutoComplete", &fValue);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to parse if the control autocomplete.");

        if (fValue)
        {
            pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE;
        }
    }

    hr = XmlGetAttributeNumber(pixn, L"StringId", reinterpret_cast<DWORD*>(&pControl->uStringId));
    if (S_FALSE == hr)
    {
        hr = XmlGetAttributeNumber(pixn, L"sid", reinterpret_cast<DWORD*>(&pControl->uStringId));
    }
    ExitOnFailure(hr, "Failed to get control text id attribute.");

    if (S_FALSE == hr)
    {
        pControl->uStringId = UINT_MAX;

        hr = XmlGetText(pixn, &bstrText);
        ExitOnFailure(hr, "Failed to get control text.");

        if (S_OK == hr)
        {
            hr = StrAllocString(&pControl->sczText, bstrText, 0);
            ExitOnFailure(hr, "Failed to copy control text.");

            ReleaseNullBSTR(bstrText);
        }
        else if (S_FALSE == hr)
        {
            hr = S_OK;
        }
    }

    if (THEME_CONTROL_TYPE_BILLBOARD == type)
    {
        hr = XmlGetYesNoAttribute(pixn, L"Loop", &pControl->fBillboardLoops);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Billboard/@Loop attribute.");
        }

        pControl->wBillboardInterval = 5000;
        hr = XmlGetAttributeNumber(pixn, L"Interval", &dwValue);
        if (S_OK == hr && dwValue)
        {
            pControl->wBillboardInterval = static_cast<WORD>(dwValue & 0xFFFF);
        }
        ExitOnFailure(hr, "Failed to get Billboard/@Interval.");

        hr = ParseBillboards(hModule, wzRelativePath, pixn, pControl);
        ExitOnFailure(hr, "Failed to parse billboards.");
    }
    else if (THEME_CONTROL_TYPE_TEXT == type)
    {
        hr = XmlGetYesNoAttribute(pixn, L"Center", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= SS_CENTER;
        }
        ExitOnFailure(hr, "Failed to tell if the text control should be centered.");

        hr = XmlGetYesNoAttribute(pixn, L"DisablePrefix", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= SS_NOPREFIX;
        }
        ExitOnFailure(hr, "Failed to tell if the text control should disable prefix.");
    }
    else if (THEME_CONTROL_TYPE_LISTVIEW == type)
    {
        // Parse the optional extended window style.
        hr = XmlGetAttributeNumberBase(pixn, L"HexExtendedStyle", 16, &pControl->dwExtendedStyle);
        if (S_FALSE == hr)
        {
            hr = XmlGetAttributeNumberBase(pixn, L"xs", 16, &pControl->dwExtendedStyle);
        }
        ExitOnFailure(hr, "Failed to get theme ListView extended style (ListView/@HexExtendedStyle) attribute.");

        hr = XmlGetAttribute(pixn, L"ImageList", &bstrText);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to get theme ListView ImageList (ListView/@ImageList) attribute.");

            hr = FindImageList(pTheme, bstrText, &pControl->rghImageList[0]);
            ExitOnFailure(hr, "Failed to find image list %ls while setting imagelist for ListView");
        }

        hr = XmlGetAttribute(pixn, L"ImageListSmall", &bstrText);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to get theme ListView ImageList (ListView/@ImageListSmall) attribute.");

            hr = FindImageList(pTheme, bstrText, &pControl->rghImageList[1]);
            ExitOnFailure(hr, "Failed to find image list %ls while setting imagelistsmall for ListView");
        }

        hr = XmlGetAttribute(pixn, L"ImageListState", &bstrText);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to get theme ListView ImageList (ListView/@ImageListState) attribute.");

            hr = FindImageList(pTheme, bstrText, &pControl->rghImageList[2]);
            ExitOnFailure(hr, "Failed to find image list %ls while setting imageliststate for ListView");
        }

        hr = XmlGetAttribute(pixn, L"ImageListGroupHeader", &bstrText);
        if (S_FALSE != hr)
        {
            ExitOnFailure(hr, "Failed to get theme ListView ImageList (ListView/@ImageListGroupHeader) attribute.");

            hr = FindImageList(pTheme, bstrText, &pControl->rghImageList[3]);
            ExitOnFailure(hr, "Failed to find image list %ls while setting imagelistgroupheader for ListView");
        }

        hr = ParseColumns(pixn, pControl);
        ExitOnFailure(hr, "Failed to parse columns");
    }
    else if (THEME_CONTROL_TYPE_TREEVIEW == type)
    {
        pControl->dwStyle |= TVS_DISABLEDRAGDROP;

        hr = XmlGetYesNoAttribute(pixn, L"EnableDragDrop", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle &= ~TVS_DISABLEDRAGDROP;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control control enables drag and drop.");

        hr = XmlGetYesNoAttribute(pixn, L"FullRowSelect", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= TVS_FULLROWSELECT;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control enables full row select.");

        hr = XmlGetYesNoAttribute(pixn, L"HasButtons", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= TVS_HASBUTTONS;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control show buttons.");

        hr = XmlGetYesNoAttribute(pixn, L"AlwaysShowSelect", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= TVS_SHOWSELALWAYS;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control always displays the selection.");

        hr = XmlGetYesNoAttribute(pixn, L"LinesAtRoot", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= TVS_LINESATROOT;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control shows lines at the root.");

        hr = XmlGetYesNoAttribute(pixn, L"HasLines", &fValue);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        else if (fValue)
        {
            pControl->dwStyle |= TVS_HASLINES;
        }
        ExitOnFailure(hr, "Failed to tell if the tree control shows lines.");
    }
    else if (THEME_CONTROL_TYPE_TAB == type)
    {
        hr = ParseTabs(pixn, pControl);
        ExitOnFailure(hr, "Failed to parse tabs");
    }

    ScaleControl(pTheme, pControl);

LExit:
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseBillboards(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Image", &pixnl);
    ExitOnFailure(hr, "Failed to select child billboard image nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cBillboards));
    ExitOnFailure(hr, "Failed to count the number of billboard images.");

    if (0 < pControl->cBillboards)
    {
        hr = ::SizeTMult(sizeof(THEME_BILLBOARD), pControl->cBillboards, &cbAllocSize);
        ExitOnFailure(hr, "Overflow while calculating allocation size for %u THEME_BILLBOARD structs.", pControl->cBillboards);

        pControl->ptbBillboards = static_cast<THEME_BILLBOARD*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pControl->ptbBillboards, hr, E_OUTOFMEMORY, "Failed to allocate billboard image structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            hr = ParseImage(hModule, wzRelativePath, pixnChild, &pControl->ptbBillboards[i].hImage);
            ExitOnFailure(hr, "Failed to get billboard image.");

            hr = XmlGetText(pixnChild, &bstrText);
            ExitOnFailure(hr, "Failed to get inner text of the Billboard/@Image element.");

            if (S_OK == hr && bstrText && *bstrText)
            {
                hr = StrAllocString(&(pControl->ptbBillboards[i].sczUrl), bstrText, 0);
                ExitOnFailure(hr, "Failed to copy image URL.");

                pControl->wBillboardUrls |= (1 << i);
            }

            ++i;
            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseColumns(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Column|c", &pixnl);
    ExitOnFailure(hr, "Failed to select child column nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cColumns));
    ExitOnFailure(hr, "Failed to count the number of control columns.");

    if (0 < pControl->cColumns)
    {
        hr = ::SizeTMult(sizeof(THEME_COLUMN), pControl->cColumns, &cbAllocSize);
        ExitOnFailure(hr, "Overflow while calculating allocation size for %u THEME_COLUMN structs.", pControl->cColumns);

        pControl->ptcColumns = static_cast<THEME_COLUMN*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pControl->ptcColumns, hr, E_OUTOFMEMORY, "Failed to allocate column structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            hr = XmlGetText(pixnChild, &bstrText);
            ExitOnFailure(hr, "Failed to get inner text of column element.");

            hr = XmlGetAttributeNumber(pixnChild, L"Width", reinterpret_cast<DWORD*>(&pControl->ptcColumns[i].nBaseWidth));
            if (S_FALSE == hr)
            {
                hr = XmlGetAttributeNumber(pixnChild, L"w", reinterpret_cast<DWORD*>(&pControl->ptcColumns[i].nBaseWidth));
                if (S_FALSE == hr)
                {
                    pControl->ptcColumns[i].nBaseWidth = 100;
                }
            }
            ExitOnFailure(hr, "Failed to get column width attribute.");

            hr = XmlGetYesNoAttribute(pixnChild, L"Expands", reinterpret_cast<BOOL*>(&pControl->ptcColumns[i].fExpands));
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to find expands attribute.");

            hr = StrAllocString(&(pControl->ptcColumns[i].pszName), bstrText, 0);
            ExitOnFailure(hr, "Failed to copy column name.");

            ++i;
            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseTabs(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    size_t cbAllocSize = 0;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Tab|t", &pixnl);
    ExitOnFailure(hr, "Failed to select child tab nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cTabs));
    ExitOnFailure(hr, "Failed to count the number of tabs.");

    if (0 < pControl->cTabs)
    {
        hr = ::SizeTMult(sizeof(THEME_TAB), pControl->cTabs, &cbAllocSize);
        ExitOnFailure(hr, "Overflow while calculating allocation size for %u THEME_TAB structs.", pControl->cTabs);

        pControl->pttTabs = static_cast<THEME_TAB*>(MemAlloc(cbAllocSize, TRUE));
        ExitOnNull(pControl->pttTabs, hr, E_OUTOFMEMORY, "Failed to allocate tab structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            hr = XmlGetText(pixnChild, &bstrText);
            ExitOnFailure(hr, "Failed to get inner text of tab element.");

            hr = StrAllocString(&(pControl->pttTabs[i].pszName), bstrText, 0);
            ExitOnFailure(hr, "Failed to copy tab name.");

            ++i;
            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT FindImageList(
    __in THEME* pTheme,
    __in_z LPCWSTR wzImageListName,
    __out HIMAGELIST *phImageList
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pTheme->cImageLists; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pTheme->rgImageLists[i].sczName, -1, wzImageListName, -1))
        {
            *phImageList = pTheme->rgImageLists[i].hImageList;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}


static HRESULT DrawBillboard(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    HBITMAP hImage = pControl->ptbBillboards[pControl->dwData].hImage;
    DWORD dwHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    DWORD dwWidth = pdis->rcItem.right - pdis->rcItem.left;
    int nSourceX = hImage ? 0 : pControl->nSourceX;
    int nSourceY = hImage ? 0 : pControl->nSourceY;

    HDC hdcMem = ::CreateCompatibleDC(pdis->hDC);
    HBITMAP hDefaultBitmap = static_cast<HBITMAP>(::SelectObject(hdcMem, hImage ? hImage : pTheme->hImage));

    ::StretchBlt(pdis->hDC, 0, 0, dwWidth, dwHeight, hdcMem, nSourceX, nSourceY, dwWidth, dwHeight, SRCCOPY);

    ::SelectObject(hdcMem, hDefaultBitmap);
    ::DeleteDC(hdcMem);
    return S_OK;
}


static HRESULT DrawButton(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD_PTR dwStyle = ::GetWindowLongPtrW(pdis->hwndItem, GWL_STYLE);
    int nSourceX = pControl->hImage ? 0 : pControl->nSourceX;
    int nSourceY = pControl->hImage ? 0 : pControl->nSourceY;

    HDC hdcMem = ::CreateCompatibleDC(pdis->hDC);
    HBITMAP hDefaultBitmap = static_cast<HBITMAP>(::SelectObject(hdcMem, pControl->hImage ? pControl->hImage : pTheme->hImage));

    if (ODS_SELECTED & pdis->itemState)
    {
        nSourceY += pControl->nHeight * 2;
    }
    else if (pControl->dwData & THEME_CONTROL_DATA_HOVER)
    {
        nSourceY += pControl->nHeight;
    }

    ::StretchBlt(pdis->hDC, 0, 0, pControl->nWidth, pControl->nHeight, hdcMem, nSourceX, nSourceY, pControl->nWidth, pControl->nHeight, SRCCOPY);

    if (WS_TABSTOP & dwStyle && ODS_FOCUS & pdis->itemState)
    {
        ::DrawFocusRect(pdis->hDC, &pdis->rcItem);
    }

    ::SelectObject(hdcMem, hDefaultBitmap);
    ::DeleteDC(hdcMem);
    return hr;
}


static HRESULT DrawHyperlink(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    WCHAR wzText[256] = { };
    DWORD cchText = 0;
    THEME_FONT* pFont = NULL;
    HFONT hfPrev = NULL;
    COLORREF clrForePrev;
    COLORREF clrBackPrev;

    if (0 == (cchText = ::GetWindowTextW(pdis->hwndItem, wzText, countof(wzText))))
    {
        ExitWithLastError(hr, "Failed to get text of link.");
    }

    if (ODS_SELECTED & pdis->itemState)
    {
        pFont = pTheme->rgFonts + pControl->dwFontSelectedId;
    }
    else if (pControl->dwData & THEME_CONTROL_DATA_HOVER)
    {
        pFont = pTheme->rgFonts + pControl->dwFontHoverId;
    }
    else
    {
        pFont = pTheme->rgFonts + pControl->dwFontId;
    }

    hfPrev = SelectFont(pdis->hDC, pFont->hFont);

    clrForePrev = ::SetTextColor(pdis->hDC, pFont->crForeground);
    clrBackPrev = ::SetBkColor(pdis->hDC, pFont->crBackground);

#pragma prefast(push)
#pragma prefast(disable:26010) // OACR doesn't know this, but GetWindowText won't return a number larger than the buffer.
    ::ExtTextOutW(pdis->hDC, 0, 0, ETO_CLIPPED | ETO_OPAQUE, &pdis->rcItem, wzText, cchText, NULL);
#pragma prefast(pop)

    if (ODS_FOCUS & pdis->itemState)
    {
        ::DrawFocusRect(pdis->hDC, &pdis->rcItem);
    }

    ::SetBkColor(pdis->hDC, clrBackPrev);
    ::SetTextColor(pdis->hDC, clrForePrev);

    SelectFont(pdis->hDC, hfPrev);

LExit:
    return hr;
}


static HRESULT DrawImage(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    DWORD dwHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    DWORD dwWidth = pdis->rcItem.right - pdis->rcItem.left;
    int nSourceX = pControl->hImage ? 0 : pControl->nSourceX;
    int nSourceY = pControl->hImage ? 0 : pControl->nSourceY;

    BLENDFUNCTION bf = { };
    bf.BlendOp = AC_SRC_OVER;
    bf.SourceConstantAlpha = 255;
    bf.AlphaFormat = AC_SRC_ALPHA;

    HDC hdcMem = ::CreateCompatibleDC(pdis->hDC);
    HBITMAP hDefaultBitmap = static_cast<HBITMAP>(::SelectObject(hdcMem, pControl->hImage ? pControl->hImage : pTheme->hImage));

    // Try to draw the image with transparency and if that fails (usually because the image has no
    // alpha channel) then draw the image as is.
    if (!::AlphaBlend(pdis->hDC, 0, 0, dwWidth, dwHeight, hdcMem, nSourceX, nSourceY, dwWidth, dwHeight, bf))
    {
        ::StretchBlt(pdis->hDC, 0, 0, dwWidth, dwHeight, hdcMem, nSourceX, nSourceY, dwWidth, dwHeight, SRCCOPY);
    }

    ::SelectObject(hdcMem, hDefaultBitmap);
    ::DeleteDC(hdcMem);
    return S_OK;
}


static HRESULT DrawProgressBar(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    DWORD dwProgressColor = HIWORD(pControl->dwData);
    DWORD dwProgressPercentage = LOWORD(pControl->dwData);
    DWORD dwHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    DWORD dwCenter = (pdis->rcItem.right - 2) * dwProgressPercentage / 100;
    int nSourceX = pControl->hImage ? 0 : pControl->nSourceX;
    int nSourceY = (pControl->hImage ? 0 : pControl->nSourceY) + (dwProgressColor * pControl->nHeight);

    HDC hdcMem = ::CreateCompatibleDC(pdis->hDC);
    HBITMAP hDefaultBitmap = static_cast<HBITMAP>(::SelectObject(hdcMem, pControl->hImage ? pControl->hImage : pTheme->hImage));

    // Draw the left side of the progress bar.
    ::StretchBlt(pdis->hDC, 0, 0, 1, dwHeight, hdcMem, nSourceX, nSourceY, 1, dwHeight, SRCCOPY);

    // Draw the filled side of the progress bar, if there is any.
    if (0 < dwCenter)
    {
        ::StretchBlt(pdis->hDC, 1, 0, dwCenter, dwHeight, hdcMem, nSourceX + 1, nSourceY, 1, dwHeight, SRCCOPY);
    }

    // Draw the unfilled side of the progress bar, if there is any.
    if (dwCenter < static_cast<DWORD>(pdis->rcItem.right - 2))
    {
        ::StretchBlt(pdis->hDC, 1 + dwCenter, 0, pdis->rcItem.right - dwCenter - 1, dwHeight, hdcMem, nSourceX + 2, nSourceY, 1, dwHeight, SRCCOPY);
    }

    // Draw the right side of the progress bar.
    ::StretchBlt(pdis->hDC, pdis->rcItem.right - 1, 0, 1, dwHeight, hdcMem, nSourceX, nSourceY, 1, dwHeight, SRCCOPY);

    ::SelectObject(hdcMem, hDefaultBitmap);
    ::DeleteDC(hdcMem);
    return S_OK;
}


static BOOL DrawHoverControl(
    __in THEME* pTheme,
    __in BOOL fHover
    )
{
    BOOL fChangedHover = FALSE;
    THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, pTheme->hwndHover));

    // Only hyperlinks and owner-drawn buttons have hover states.
    if (pControl && (THEME_CONTROL_TYPE_HYPERLINK == pControl->type ||
        (THEME_CONTROL_TYPE_BUTTON == pControl->type && (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW))))
    {
        if (fHover)
        {
            pControl->dwData |= THEME_CONTROL_DATA_HOVER;
        }
        else
        {
            pControl->dwData &= ~THEME_CONTROL_DATA_HOVER;
        }

        ::InvalidateRect(pControl->hWnd, NULL, FALSE);
        fChangedHover = TRUE;
    }

    return fChangedHover;
}


static void FreePage(
    __in THEME_PAGE* pPage
    )
{
    if (pPage)
    {
        ReleaseStr(pPage->sczName);
        ReleaseMem(pPage->rgdwControlIndices);
    }
}


static void FreeImageList(
    __in THEME_IMAGELIST* pImageList
    )
{
    if (pImageList)
    {
        ReleaseStr(pImageList->sczName);
        ImageList_Destroy(pImageList->hImageList);
    }
}

static void FreeControl(
    __in THEME_CONTROL* pControl
    )
{
    if (pControl)
    {
        if (::IsWindow(pControl->hWnd))
        {
            ::CloseWindow(pControl->hWnd);
            pControl->hWnd = NULL;
        }

        ReleaseStr(pControl->sczName);
        ReleaseStr(pControl->sczText);

        if (pControl->hImage)
        {
            ::DeleteBitmap(pControl->hImage);
        }

        for (DWORD i = 0; i < pControl->cBillboards; ++i)
        {
            FreeBillboard(&(pControl->ptbBillboards[i]));
        }

        for (DWORD i = 0; i < pControl->cColumns; ++i)
        {
            FreeColumn(&(pControl->ptcColumns[i]));
        }

        for (DWORD i = 0; i < pControl->cTabs; ++i)
        {
            FreeTab(&(pControl->pttTabs[i]));
        }

        ReleaseMem(pControl->ptbBillboards)
        ReleaseMem(pControl->ptcColumns);
        ReleaseMem(pControl->pttTabs);
    }
}


static void FreeBillboard(
    __in THEME_BILLBOARD* pBillboard
    )
{
    ReleaseStr(pBillboard->sczUrl);
    if (pBillboard->hImage)
    {
        ::DeleteBitmap(pBillboard->hImage);
    }
}


static void FreeColumn(
    __in THEME_COLUMN* pColumn
    )
{
    ReleaseStr(pColumn->pszName);
}


static void FreeTab(
    __in THEME_TAB* pTab
    )
{
    ReleaseStr(pTab->pszName);
}


static void FreeFont(
    __in THEME_FONT* pFont
    )
{
    if (pFont)
    {
        if (pFont->hBackground)
        {
            ::DeleteObject(pFont->hBackground);
            pFont->hBackground = NULL;
        }

        if (pFont->hForeground)
        {
            ::DeleteObject(pFont->hForeground);
            pFont->hForeground = NULL;
        }

        if (pFont->hFont)
        {
            ::DeleteObject(pFont->hFont);
            pFont->hFont = NULL;
        }
    }
}


static DWORD CALLBACK RichEditStreamFromFileHandleCallback(
    __in DWORD_PTR dwCookie,
    __in_bcount(cb) LPBYTE pbBuff,
    __in LONG cb,
    __in LONG* pcb
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = reinterpret_cast<HANDLE>(dwCookie);

    if (!::ReadFile(hFile, pbBuff, cb, reinterpret_cast<DWORD*>(pcb), NULL))
    {
        ExitWithLastError(hr, "Failed to read file");
    }

LExit:
    return hr;
}


static DWORD CALLBACK RichEditStreamFromMemoryCallback(
    __in DWORD_PTR dwCookie,
    __in_bcount(cb) LPBYTE pbBuff,
    __in LONG cb,
    __in LONG* pcb
    )
{
    HRESULT hr = S_OK;
    MEMBUFFER_FOR_RICHEDIT* pBuffer = reinterpret_cast<MEMBUFFER_FOR_RICHEDIT*>(dwCookie);
    DWORD cbCopy = 0;

    if (pBuffer->iData < pBuffer->cbData)
    {
        cbCopy = min(static_cast<DWORD>(cb), pBuffer->cbData - pBuffer->iData);
        memcpy(pbBuff, pBuffer->rgbData + pBuffer->iData, cbCopy);

        pBuffer->iData += cbCopy;
        Assert(pBuffer->iData <= pBuffer->cbData);
    }

    *pcb = cbCopy;
    return hr;
}


static void CALLBACK OnBillboardTimer(
    __in const THEME* pTheme,
    __in HWND hwnd,
    __in UINT_PTR idEvent
    )
{
    HWND hwndControl = ::GetDlgItem(hwnd, static_cast<int>(idEvent));

    if (hwndControl)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, hwndControl));
        AssertSz(pControl && THEME_CONTROL_TYPE_BILLBOARD == pControl->type, "Only billboard controls should have the BillboardTimerProc().");

        if (pControl)
        {
            ++pControl->dwData;
            if (pControl->dwData < pControl->cBillboards)
            {
                ::InvalidateRect(hwndControl, NULL, FALSE);
            }
            else if (pControl->fBillboardLoops)
            {
                pControl->dwData = 0;
                ::InvalidateRect(hwndControl, NULL, FALSE);
            }
            else // no more looping
            {
                ::KillTimer(hwnd, idEvent);
            }
        }
    }
}

static HRESULT OnRichEditEnLink(
    __in LPARAM lParam,
    __in HWND hWndRichEdit,
    __in HWND hWnd
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLink = NULL;
    ENLINK* link = reinterpret_cast<ENLINK*>(lParam);

    switch (link->msg)
    {
    case WM_LBUTTONDOWN:
        {
        hr = StrAlloc(&sczLink, link->chrg.cpMax - link->chrg.cpMin + 2);
        ExitOnFailure(hr, "Failed to allocate string for link.");

        TEXTRANGEW tr;
        tr.chrg.cpMin = link->chrg.cpMin;
        tr.chrg.cpMax = link->chrg.cpMax;
        tr.lpstrText = sczLink;

        if (0 < ::SendMessageW(hWndRichEdit, EM_GETTEXTRANGE, 0, reinterpret_cast<LPARAM>(&tr)))
        {
            hr = ShelExec(sczLink, NULL, L"open", NULL, SW_SHOWDEFAULT, hWnd, NULL);
            ExitOnFailure(hr, "Failed to launch link: %ls", sczLink);
        }
        
        break;
        }

    case WM_SETCURSOR:
        ::SetCursor(vhCursorHand);
        break;
    }

LExit:
    ReleaseStr(sczLink);

    return hr;
}

static BOOL ControlIsType(
    __in const THEME* pTheme,
    __in DWORD dwControl,
    __in const THEME_CONTROL_TYPE type
    )
{
    BOOL fIsType = FALSE;
    HWND hWnd = ::GetDlgItem(pTheme->hwndParent, dwControl);
    if (hWnd)
    {
        const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);
        fIsType = (pControl && type == pControl->type);
    }

    return fIsType;
}

static const THEME_CONTROL* FindControlFromHWnd(
    __in const THEME* pTheme,
    __in HWND hWnd
    )
{
    // As we can't use GWLP_USERDATA (SysLink controls on Windows XP uses it too)...
    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        if (hWnd == pTheme->rgControls[i].hWnd)
        {
            return pTheme->rgControls + i;
        }
    }

    return NULL;
}

static void GetControlDimensions(
    __in const RECT* prcParent,
    __in const THEME_CONTROL* pControl,
    __out int* piWidth,
    __out int* piHeight,
    __out int* piX,
    __out int* piY
    )
{
    *piWidth  = pControl->nWidth < 1  ? pControl->nX < 0 ? prcParent->right  + pControl->nWidth  : prcParent->right  + pControl->nWidth  - pControl->nX : pControl->nWidth;
    *piHeight = pControl->nHeight < 1 ? pControl->nY < 0 ? prcParent->bottom + pControl->nHeight : prcParent->bottom + pControl->nHeight - pControl->nY : pControl->nHeight;
    *piX = pControl->nX < 0 ? prcParent->right  + pControl->nX - *piWidth  : pControl->nX;
    *piY = pControl->nY < 0 ? prcParent->bottom + pControl->nY - *piHeight : pControl->nY;
}

static HRESULT SizeListViewColumns(
    __inout THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    RECT rcParent = { };
    int cNumExpandingColumns = 0;
    int iExtraAvailableSize;

    if (!::GetWindowRect(pControl->hWnd, &rcParent))
    {
        ExitWithLastError(hr, "Failed to get window rect of listview control.");
    }

    iExtraAvailableSize = rcParent.right - rcParent.left;

    for (DWORD i = 0; i < pControl->cColumns; ++i)
    {
        if (pControl->ptcColumns[i].fExpands)
        {
            ++cNumExpandingColumns;
        }

        iExtraAvailableSize -= pControl->ptcColumns[i].nBaseWidth;
    }

    // Leave room for a vertical scroll bar just in case.
    iExtraAvailableSize -= ::GetSystemMetrics(SM_CXVSCROLL);

    for (DWORD i = 0; i < pControl->cColumns; ++i)
    {
        if (pControl->ptcColumns[i].fExpands)
        {
            pControl->ptcColumns[i].nWidth = pControl->ptcColumns[i].nBaseWidth + (iExtraAvailableSize / cNumExpandingColumns);
            // In case there is any remainder, use it up the first chance we get.
            pControl->ptcColumns[i].nWidth += iExtraAvailableSize % cNumExpandingColumns;
            iExtraAvailableSize -= iExtraAvailableSize % cNumExpandingColumns;
        }
        else
        {
            pControl->ptcColumns[i].nWidth = pControl->ptcColumns[i].nBaseWidth;
        }
    }

LExit:
    return hr;
}

DAPI_(int) ScaleByFactor(
    int pixels,
    FLOAT fScaleFactor
    )
{
    if (pixels == -1)
    {
        return pixels;
    }

    float x = pixels * fScaleFactor;
    return static_cast<int>(std::rintf(x));
}

static void ScaleApplication(
    __in THEME* pTheme
    )
{
    pTheme->nWidth = ScaleByFactor(pTheme->nWidth, pTheme->fScaleFactorX);
    pTheme->nHeight = ScaleByFactor(pTheme->nHeight, pTheme->fScaleFactorY);

    pTheme->nMinimumWidth = ScaleByFactor(pTheme->nMinimumWidth, pTheme->fScaleFactorX);
    pTheme->nMinimumHeight = ScaleByFactor(pTheme->nMinimumHeight, pTheme->fScaleFactorY);

    pTheme->nSourceX = ScaleByFactor(pTheme->nSourceX, pTheme->fScaleFactorX);
    pTheme->nSourceY = ScaleByFactor(pTheme->nSourceY, pTheme->fScaleFactorY);
}

static void ScaleControl(
    __in const THEME* pTheme,
    __in THEME_CONTROL* pControl
    )
{
    pControl->nX = ScaleByFactor(pControl->nX, pTheme->fScaleFactorX);
    pControl->nY = ScaleByFactor(pControl->nY, pTheme->fScaleFactorY);

    pControl->nWidth = ScaleByFactor(pControl->nWidth, pTheme->fScaleFactorX);
    pControl->nHeight = ScaleByFactor(pControl->nHeight, pTheme->fScaleFactorY);

    pControl->nSourceX = ScaleByFactor(pControl->nSourceX, pTheme->fScaleFactorX);
    pControl->nSourceY = ScaleByFactor(pControl->nSourceY, pTheme->fScaleFactorY);
}

static void ScaleFont(
    __in const THEME* pTheme,
    __in LOGFONTW* lf
    )
{
    if (lf->lfHeight < 0)
    {
        lf->lfHeight = ScaleByFactor(lf->lfHeight, pTheme->fScaleFactorX);
    }
}