//-------------------------------------------------------------------------------------------------
// <copyright file="load.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Theme viewer load.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

struct LOAD_THREAD_CONTEXT
{
    HWND hWnd;
    LPWSTR sczThemePath;

    HANDLE hInit;
};

static DWORD WINAPI LoadThreadProc(
    __in LPVOID pvContext
    );


extern "C" HRESULT LoadStart(
    __in_z LPCWSTR wzThemePath,
    __in HWND hWnd,
    __out HANDLE* phThread
    )
{
    HRESULT hr = S_OK;
    HANDLE rgHandles[2] = { };
    LPWSTR sczThemePath = NULL;
    LOAD_THREAD_CONTEXT context = { };

    rgHandles[0] = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(rgHandles[0], hr, "Failed to create load init event.");

    hr = StrAllocString(&sczThemePath, wzThemePath, 0);
    ExitOnFailure(hr, "Failed to copy path to initial theme file.");

    context.hWnd = hWnd;
    context.sczThemePath = sczThemePath;
    context.hInit = rgHandles[0];

    rgHandles[1] = ::CreateThread(NULL, 0, LoadThreadProc, &context, 0, NULL);
    ExitOnNullWithLastError(rgHandles[1], hr, "Failed to create load thread.");

    ::WaitForMultipleObjects(countof(rgHandles), rgHandles, FALSE, INFINITE);

    *phThread = rgHandles[1];
    rgHandles[1] = NULL;
    sczThemePath = NULL;

LExit:
    ReleaseHandle(rgHandles[1]);
    ReleaseHandle(rgHandles[0]);
    ReleaseStr(sczThemePath);
    return hr;
}


static DWORD WINAPI LoadThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;

    LOAD_THREAD_CONTEXT* pContext = static_cast<LOAD_THREAD_CONTEXT*>(pvContext);
    LPWSTR sczThemePath = pContext->sczThemePath;
    HWND hWnd = pContext->hWnd;

    // We can signal the initialization event as soon as we have copied the context
    // values into local variables.
    ::SetEvent(pContext->hInit);

    BOOL fComInitialized = FALSE;
    HANDLE hDirectory = INVALID_HANDLE_VALUE;
    LPWSTR sczDirectory = NULL;
    LPWSTR wzFileName = NULL;

    THEME* pTheme = NULL;
    HANDLE_THEME* pHandle = NULL;

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM on load thread.");
    fComInitialized = TRUE;

    // Open a handle to the directory so we can put a notification on it.
    hr = PathGetDirectory(sczThemePath, &sczDirectory);
    ExitOnFailure(hr, "Failed to get path directory.");

     wzFileName = PathFile(sczThemePath);

    hDirectory = ::CreateFileW(sczDirectory, FILE_LIST_DIRECTORY, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
    if (INVALID_HANDLE_VALUE == hDirectory)
    {
        ExitWithLastError1(hr, "Failed to open directory: %ls", sczDirectory);
    }

    BOOL fUpdated = FALSE;
    do
    {
        // Get the last modified time on the file we're loading for verification that the
        // file actually gets changed down below.
        FILETIME ftModified = { };
        FileGetTime(sczThemePath, NULL, NULL, &ftModified);

        // Try to load the theme file.
        hr = ThemeLoadFromFile(sczThemePath, &pTheme);
        if (FAILED(hr))
        {
            ::SendMessageW(hWnd, WM_THMVWR_THEME_LOAD_ERROR, 0, hr);
        }
        else
        {
            hr = AllocHandleTheme(pTheme, &pHandle);
            ExitOnFailure(hr, "Failed to allocate handle to theme");

            ::SendMessageW(hWnd, WM_THMVWR_NEW_THEME, 0, reinterpret_cast<LPARAM>(pHandle));
            pHandle = NULL;
        }

        fUpdated = FALSE;
        do
        {
            DWORD rgbNotifications[1024];
            DWORD cbRead = 0;
            if (!::ReadDirectoryChangesW(hDirectory, rgbNotifications, sizeof(rgbNotifications), FALSE, FILE_NOTIFY_CHANGE_LAST_WRITE, &cbRead, NULL, NULL))
            {
                ExitWithLastError1(hr, "Failed while watching directory: %ls", sczDirectory);
            }

            // Wait for half a second to let all the file handles get closed to minimize access
            // denied errors.
            ::Sleep(500);

            FILE_NOTIFY_INFORMATION* pNotification = reinterpret_cast<FILE_NOTIFY_INFORMATION*>(rgbNotifications);
            while (pNotification)
            {
                // If our file was updated, check to see if the modified time really changed. The notifications
                // are often trigger happy thinking the file changed two or three times in a row. Maybe it's AV
                // software creating the problems but actually checking the modified date works well.
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pNotification->FileName, pNotification->FileNameLength / sizeof(WCHAR), wzFileName, -1))
                {
                    FILETIME ft = { };
                    FileGetTime(sczThemePath, NULL, NULL, &ft);

                    fUpdated = (ftModified.dwHighDateTime < ft.dwHighDateTime) || (ftModified.dwHighDateTime == ft.dwHighDateTime && ftModified.dwLowDateTime < ft.dwLowDateTime);
                    break;
                }

                pNotification = pNotification->NextEntryOffset ? reinterpret_cast<FILE_NOTIFY_INFORMATION*>(reinterpret_cast<BYTE*>(pNotification) + pNotification->NextEntryOffset) : NULL;
            }
        } while (!fUpdated);
    } while(fUpdated);

LExit:
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    ReleaseFileHandle(hDirectory);
    ReleaseStr(sczDirectory);
    ReleaseStr(sczThemePath);
    return hr;
}

extern "C" HRESULT AllocHandleTheme(
    __in THEME* pTheme,
    __out HANDLE_THEME** ppHandle
    )
{
    HRESULT hr = S_OK;
    HANDLE_THEME* pHandle = NULL;

    pHandle = static_cast<HANDLE_THEME*>(MemAlloc(sizeof(HANDLE_THEME), TRUE));
    ExitOnNull(pHandle, hr, E_OUTOFMEMORY, "Failed to allocate theme handle.");

    pHandle->cReferences = 1;
    pHandle->pTheme = pTheme;

    *ppHandle = pHandle;
    pHandle = NULL;

LExit:
    ReleaseMem(pHandle);
    return hr;
}

extern "C" void IncrementHandleTheme(
    __in HANDLE_THEME* pHandle
    )
{
    ::InterlockedIncrement(reinterpret_cast<LONG*>(&pHandle->cReferences));
}

extern "C"  void DecrementHandleTheme(
    __in HANDLE_THEME* pHandle
    )
{
    if (pHandle && 0 == ::InterlockedDecrement(reinterpret_cast<LONG*>(&pHandle->cReferences)))
    {
        ThemeFree(pHandle->pTheme);
        MemFree(pHandle);
    }
}
