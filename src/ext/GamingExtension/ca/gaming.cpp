//-------------------------------------------------------------------------------------------------
// <copyright file="gaming.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Game Explorer custom action code.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

LPCWSTR vcsGameuxQuery =
    L"SELECT `WixGameExplorer`.`InstanceId`,   `WixGameExplorer`.`File_`,   `File`.`Component_` "
    L"FROM `WixGameExplorer`, `File` "
    L"WHERE `WixGameExplorer`.`File_` = `File`.`File`";
enum eGameuxQuery { egqInstanceId = 1, egqFile, egqComponent };


/******************************************************************
 ExtractXMLFromGDFBinary - extract gdf xml from the gdf binary
 
********************************************************************/
extern "C" HRESULT ExtractXMLFromGDFBinary( 
    __in LPCWSTR sczGDFBinPath,  
    __out IXMLDOMNode** ppIXMLDOMNode
    )
{
    Assert(sczGDFBinPath);
    Assert(ppIXMLDOMNode);

    HRESULT hr = S_OK;
    HMODULE hGDFDll = NULL;
    HGLOBAL hgResourceCopy = NULL;
    HGLOBAL hgResource = NULL;
    DWORD dwGDFXMLSize = 0;
    IPersistStreamInit* pPersistStreamInit = NULL;
    IStream* piStream = NULL;
    IXMLDOMDocument* pDoc = NULL;
    const BYTE* pResourceBuffer = NULL;

    // Extract the GDF XML from the GDF binary 
    hGDFDll = ::LoadLibraryW(sczGDFBinPath);
    if (NULL == hGDFDll)
    {
        ExitWithLastError(hr, "failed to load GDF binary");
    }

    // Find resource will pick the right ID_GDF_XML_STR based on the current language
    HRSRC hrsrc = ::FindResourceExW(hGDFDll, L"DATA", ID_GDF_XML_STR, MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL));
    ExitOnNullWithLastError(hrsrc, hr, "Failed to find resource.");

    hgResource = ::LoadResource(hGDFDll, hrsrc);
    ExitOnNullWithLastError(hgResource, hr, "Failed to LoadResource.");

    pResourceBuffer = (const BYTE*)::LockResource(hgResource);
    ExitOnNullWithLastError(pResourceBuffer, hr, "Failed to LockResource.");

    dwGDFXMLSize = ::SizeofResource(hGDFDll, hrsrc);
    if (0 == dwGDFXMLSize)
    {
        ExitWithLastError(hr, "failed to SizeofResource");
    }

    // HGLOBAL from LoadResource() needs to be copied for CreateStreamOnHGlobal() to work
    hgResourceCopy = ::GlobalAlloc(GMEM_MOVEABLE, dwGDFXMLSize);
    ExitOnNullDebugTrace1(hgResourceCopy, hr, E_OUTOFMEMORY, "failed to GlobalAlloc %ls", sczGDFBinPath);

    LPVOID pCopy = ::GlobalLock(hgResourceCopy);
    ExitOnNullWithLastError(pCopy, hr, "failed to global lock");

    memcpy(pCopy, pResourceBuffer, dwGDFXMLSize);
    ::GlobalUnlock(hgResourceCopy);

    hr = ::CreateStreamOnHGlobal(hgResourceCopy, TRUE, &piStream);
    ExitOnFailure(hr, "Failed to allocate stream from global memory.");

    // Load the XML into a IXMLDOMDocument object
    hr = ::CoCreateInstance(CLSID_DOMDocument, NULL, CLSCTX_INPROC_SERVER, IID_IXMLDOMDocument, (void**)&pDoc);
    ExitOnFailure(hr, "failed to CoCreateInstance for CLSID_DOMDocument");

    hr = pDoc->QueryInterface(IID_IPersistStreamInit, (void**)&pPersistStreamInit);
    ExitOnFailure(hr, "failed to QueryInterface IID_IPersistStreamInit");

    hr = pPersistStreamInit->Load(piStream);
    ExitOnFailure(hr, "failed to Load IStream");

    // Get the root node to the XML doc and store it 
    hr = pDoc->QueryInterface(IID_IXMLDOMNode, (void**)ppIXMLDOMNode);
    ExitOnFailure(hr, "failed to QueryInterface for IID_IXMLDOMNode");

LExit:
    if (NULL != hgResourceCopy)
    {
        ::GlobalFree(hgResourceCopy);
    }

    if (NULL != hGDFDll)
    {
        ::FreeLibrary(hGDFDll);
    }

    ReleaseObject(pDoc);
    ReleaseObject(piStream);
    ReleaseObject(pPersistStreamInit);

    return hr;
}

/******************************************************************
 IsV2GDF - test the GDF version
 
********************************************************************/
extern "C" HRESULT IsV2GDF (
    __in LPCWSTR sczGDFBinPath, 
    __out BOOL* pfV2GDF
    )
{
    Assert(pfV2GDF);

    *pfV2GDF = FALSE;
    IXMLDOMNode* pIXMLDOMNode = NULL;
    IXMLDOMNode* pPrimaryPlayTasksNode = NULL;

    HRESULT hr = ExtractXMLFromGDFBinary(sczGDFBinPath, &pIXMLDOMNode);
    ExitOnFailure(hr, "failed to ExtractXMLFromGDFBinary");

    hr = XmlSelectSingleNode(pIXMLDOMNode, L"//GameDefinitionFile/GameDefinition/ExtendedProperties/GameTasks/Play/Primary", &pPrimaryPlayTasksNode);
    if (S_OK == hr)
    {
        *pfV2GDF = TRUE;
    }
    else if (S_FALSE == hr)
    {
        *pfV2GDF = FALSE;
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
   }

LExit:
    ReleaseObject(pPrimaryPlayTasksNode);
    ReleaseObject(pIXMLDOMNode);

    return hr;
}

/******************************************************************
 GetBaseKnownFolderCsidl - get known folder Csidl from guid
 
********************************************************************/
extern "C" HRESULT GetBaseKnownFolderCsidl(
    __in LPCWSTR sczBaseKnownFolder, 
    __out int* pcsidl
    )
{
    Assert(sczBaseKnownFolder);
    Assert(pcsidl);

    HRESULT hr = S_OK;

    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{905e63b6-c1bf-494e-b29c-65b732d3d21a}", -1))
    {
        *pcsidl =  CSIDL_PROGRAM_FILES;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{F7F1ED05-9F6D-47A2-AAAE-29D317C6F066}", -1))
    {
        *pcsidl =  CSIDL_PROGRAM_FILES_COMMON;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", -1))
    {
        *pcsidl =  CSIDL_DESKTOP;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{FDD39AD0-238F-46AF-ADB4-6C85480369C7}", -1))
    {
        *pcsidl =  CSIDL_MYDOCUMENTS;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{C4AA340D-F20F-4863-AFEF-F87EF2E6BA25}", -1))
    {
        *pcsidl =  CSIDL_COMMON_DESKTOPDIRECTORY;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{ED4824AF-DCE4-45A8-81E2-FC7965083634}", -1))
    {
        *pcsidl =  CSIDL_COMMON_DOCUMENTS;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", -1))
    {
        *pcsidl =  CSIDL_SYSTEM;
    }
    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, sczBaseKnownFolder, -1, L"{F38BF404-1D43-42F2-9305-67DE0B28FC23}", -1))
    {
        *pcsidl =  CSIDL_WINDOWS;
    }
    else
    {
        hr = E_INVALIDARG ;
    }

    return hr;
}

/******************************************************************
 SaveShortcut - save a shortcut for one play task or support task
 
********************************************************************/
extern "C" HRESULT SaveShortcut(
    __in_z LPCWSTR sczLaunchPath, 
    __in_z_opt LPCWSTR sczCommandLineArgs,
    __in LPCWSTR sczShortcutFilePath, 
    __in BOOL bFileTask)
{
    Assert(sczLaunchPath);
    Assert(sczShortcutFilePath);

    DWORD cch = 0;
    IShellLinkW* psl = NULL;
    IPersistFile* ppf = NULL;

    HRESULT hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to ::CoInitialize");

    hr = ::CoCreateInstance(CLSID_ShellLink, NULL, CLSCTX_INPROC_SERVER, IID_IShellLinkW, (LPVOID*)&psl);
    ExitOnFailure(hr, "failed to CoCreateInstance for IID_IShellLinkW");

    // Setup shortcut
    hr = psl->SetPath(sczLaunchPath);
    ExitOnFailure(hr, "failed to set shorcut path");

    if (sczCommandLineArgs)
    {
        hr = psl->SetArguments(sczCommandLineArgs);
        ExitOnFailure(hr, "failed to set commandline rguments");
    }

    if (bFileTask)
    {
        // Set working dir to path of launch exe
        WCHAR strFullPath[MAX_PATH];
        WCHAR* strExePart;
        cch = ::GetFullPathNameW(sczLaunchPath, countof(strFullPath), strFullPath, &strExePart);
        if (0 == cch)
        {
            ExitWithLastError1(hr, "Failed to get full path for string: %ls", sczLaunchPath);
        }

        if (strExePart) 
        {
            *strExePart = L'\0';
        }

        hr = psl->SetWorkingDirectory(strFullPath);
        ExitOnFailure(hr, "failed to set working directory");
    }

    // Save shortcut to file
    hr = psl->QueryInterface(IID_IPersistFile, (LPVOID*)&ppf);
    ExitOnFailure(hr, "failed to QueryInterface for IID_IPersistFile");

    hr = ppf->Save(sczShortcutFilePath, TRUE);
    ExitOnFailure(hr, "failed to Save shortcut");
    
LExit:
    ReleaseObject(ppf);
    ReleaseObject(psl);

    ::CoUninitialize();

    return hr;
}

/******************************************************************
 SubCreateSingleShorcut - sub function for CreateSingleShorcut
 
********************************************************************/
extern "C" HRESULT SubCreateSingleShortcut(
    __in GAME_INSTALL_SCOPE gisInstallScope,         // Either GIS_CURRENT_USER or GIS_ALL_USERS 
    __in LPCWSTR /*sczGDFBinPath*/,                    // valid GameInstance GUID that was passed to AddGame()
    __in LPCWSTR sczInstanceId,
    __in LPCWSTR sczTaskName,                      // Name of task.  Ex "Play"
    __in LPCWSTR sczLaunchPath,                    // Path to exe.  Example: "C:\Program Files\Microsoft\MyGame.exe"
    __in_opt LPCWSTR sczCommandLineArgs,               // Can be NULL.  Example: "-windowed"
    __in int nTaskID,                             // ID of task
    __in BOOL bSupportTask,                       // if TRUE, this is a support task otherwise it is a play task
    __in BOOL bFileTask)                          // if TRUE, this is a file task otherwise it is a URL task
{
    Assert(sczTaskName);
    Assert(sczLaunchPath);

    WCHAR wzPath[MAX_PATH];
    WCHAR wzCommonFolder[MAX_PATH];
    WCHAR wzShortcutFilePath[MAX_PATH];
    HRESULT hr = S_OK;

    hr = ::SHGetFolderPathW(NULL, GIS_CURRENT_USER == gisInstallScope ? CSIDL_LOCAL_APPDATA : CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, wzCommonFolder);
    ExitOnFailure(hr, "failed to SHGetFolderPathW for APP data");

    // Create dir path for shortcut
    hr = ::StringCchPrintfW(
        wzPath, 
        MAX_PATH, 
        L"%s\\Microsoft\\Windows\\GameExplorer\\%s\\%s\\%d",
        wzCommonFolder, 
        sczInstanceId, 
        (bSupportTask) ? L"SupportTasks" : L"PlayTasks", 
        nTaskID);
    ExitOnFailure(hr, "failed to set dir path for shortcut");

    // Create the directory and all intermediate directories
    if (ERROR_SUCCESS == ::SHCreateDirectoryExW(NULL, wzPath, NULL))
    {
        // Create full file path to shortcut 
        hr = ::StringCchPrintfW(wzShortcutFilePath, MAX_PATH, L"%s\\%s.lnk", wzPath, sczTaskName);
        ExitOnFailure(hr, "failed to set full file path for shortcut");

        // Save shortcut
        hr = SaveShortcut(sczLaunchPath, sczCommandLineArgs, wzShortcutFilePath, bFileTask);
        ExitOnFailure(hr, "failed to save shortcut");
    }

LExit:

    return hr;
}

/******************************************************************
 CreateSingleShortcut - create a shortcut for one task
 
********************************************************************/
extern "C" HRESULT CreateSingleShortcut(
    __in IXMLDOMNode* pTaskNode, 
    __in LPCWSTR sczGDFBinPath, 
    __in LPCWSTR sczInstanceId,
    __in LPCWSTR sczGameInstallPath, 
    __in GAME_INSTALL_SCOPE gisInstallScope,
    __in BOOL bPrimaryTask, 
    __in BOOL bSupportTask)
{
    Assert(pTaskNode);
    Assert(sczGDFBinPath);
    Assert(sczGameInstallPath);

    HRESULT hr = S_OK;
    IXMLDOMNode* pFileTaskNode = NULL;
    IXMLDOMNode* pURLTaskNode = NULL;
    BSTR bstrTaskName = NULL;
    BSTR bstrTaskID = NULL;
    BSTR bstrPath = NULL;
    BSTR bstrCommandLineArgs = NULL;
    BSTR bstrBaseKnownFolderID = NULL;
    BSTR bstrURLPath = NULL;

    if (bPrimaryTask)
    {
        bstrTaskName = SysAllocString(L"Play");
        ExitOnNull(bstrTaskName, hr, E_OUTOFMEMORY, "Failed to allocate string.");
    }
    else
    {
        hr = XmlGetAttribute(pTaskNode, L"name", &bstrTaskName);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnRootFailure(hr, "failed to get name attribute");
    }

    if (bPrimaryTask)
    {
        bstrTaskID = SysAllocString(L"0");
        ExitOnNull(bstrTaskID, hr, E_OUTOFMEMORY, "Failed to allocate string.");
    }
    else
    {
        hr = XmlGetAttribute(pTaskNode, L"index", &bstrTaskID);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnRootFailure(hr, "failed to get index attribute");
    }

    int nTaskID = _wtoi(bstrTaskID);

    hr = XmlSelectSingleNode(pTaskNode, L"FileTask", &pFileTaskNode);
    if (S_OK == hr)
    {
        hr = XmlGetAttribute(pFileTaskNode, L"path", &bstrPath);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnRootFailure(hr, "failed to get path attribute");

        hr = XmlGetAttribute(pFileTaskNode, L"arguments", &bstrCommandLineArgs);
        ExitOnRootFailure(hr, "failed to get arguments attribute");

        hr = XmlGetAttribute(pFileTaskNode, L"baseKnownFolderID", &bstrBaseKnownFolderID);
        if (S_OK == hr)
        {
            int nCsidl;
            hr = GetBaseKnownFolderCsidl(bstrBaseKnownFolderID, &nCsidl);
            if (S_FALSE == hr)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
            }
            ExitOnFailure(hr, "Failed to get baseKnown folder Csidl");

            WCHAR wzFolderPath[MAX_PATH] = {0};
            hr = ::SHGetFolderPathW(NULL, nCsidl, NULL, SHGFP_TYPE_CURRENT, wzFolderPath);
            ExitOnFailure(hr, "failed to get folder path from nCsidl");

            WCHAR wzLaunchPath[MAX_PATH] = {0};
            hr = ::StringCchPrintfW(wzLaunchPath, MAX_PATH, L"%s\\%s", wzFolderPath, bstrPath);
            ExitOnFailure(hr, "failed to set launch path");

            hr = SubCreateSingleShortcut(gisInstallScope, sczGDFBinPath, sczInstanceId, bstrTaskName,wzLaunchPath, bstrCommandLineArgs, nTaskID, bSupportTask, true);
            ExitOnFailure(hr, "failed to create a shortcut for one  single task");
        }
        else if (S_FALSE == hr)         // no 'baseKnownFolderID'
        {
            WCHAR wzLaunchPath[MAX_PATH] = {0};
            ::StringCchPrintfW(wzLaunchPath, MAX_PATH, L"%s%s", sczGameInstallPath, bstrPath);
            hr = SubCreateSingleShortcut(gisInstallScope, sczGDFBinPath, sczInstanceId, bstrTaskName, wzLaunchPath, bstrCommandLineArgs, nTaskID, bSupportTask, true);
        }

        ExitOnRootFailure(hr, "failed to get baseKnownFolderID Attribute");
    }
    else if (S_FALSE == hr)         // no 'FileTask'
    {
        hr = XmlSelectSingleNode(pTaskNode, L"URLTask", &pURLTaskNode);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnRootFailure(hr, "Failed to find URLTask element.");

        hr = XmlGetAttribute(pURLTaskNode, L"Link", &bstrURLPath);
        if (S_FALSE == hr)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        }
        ExitOnRootFailure(hr, "failed to get link attribute");

        hr = SubCreateSingleShortcut(gisInstallScope, sczGDFBinPath, sczInstanceId, bstrTaskName, bstrURLPath, NULL, nTaskID, bSupportTask, false);
    }

LExit:
    ReleaseObject(pURLTaskNode);
    ReleaseObject(pFileTaskNode);
    ReleaseBSTR(bstrURLPath);
    ReleaseBSTR(bstrBaseKnownFolderID);
    ReleaseBSTR(bstrCommandLineArgs);
    ReleaseBSTR(bstrPath);
    ReleaseBSTR(bstrTaskID);
    ReleaseBSTR(bstrTaskName);

    return hr;
}

/******************************************************************
 CreateShorcuts - create shortcuts for game tasks. This is for the
   case of V2 GDF using IGameExplorer
********************************************************************/
extern "C" HRESULT CreateShorcuts(
    __in LPCWSTR sczGDFBinPath,
    __in LPCWSTR sczInstanceId,
    __in LPCWSTR sczGameInstallPath, 
    __in GAME_INSTALL_SCOPE gisInstallScope)
{
    Assert(sczGDFBinPath);
    Assert(sczInstanceId);
    Assert(sczGameInstallPath);

    IXMLDOMNode* pIXMLDOMNode = NULL;
    IXMLDOMNode* pPlayTasksNode = NULL;
    IXMLDOMNode* pPrimaryPlayTaskNode = NULL;
    IXMLDOMNode* pSecondaryPlayTaskNode = NULL;
    IXMLDOMNode* pSupportTasksNode = NULL;
    IXMLDOMNode* pTaskNode = NULL;

    HRESULT hr = ExtractXMLFromGDFBinary(sczGDFBinPath, &pIXMLDOMNode);
    ExitOnFailure(hr, "failed to ExtractXMLFromGDFBinary");

    hr = XmlSelectSingleNode(pIXMLDOMNode, L"//GameDefinitionFile/GameDefinition/ExtendedProperties/GameTasks/Play", &pPlayTasksNode);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }
    ExitOnRootFailure(hr, "Failed to find play task node element.");

    // Primary play tasks
    hr = XmlSelectSingleNode(pPlayTasksNode, L"Primary", &pPrimaryPlayTaskNode);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }
    ExitOnRootFailure(hr, "Failed to find the primary play task element.");

    hr = CreateSingleShortcut(pPrimaryPlayTaskNode, sczGDFBinPath, sczInstanceId, sczGameInstallPath, gisInstallScope, true, false);
    ExitOnFailure(hr, "Failed to CreateSingleTask for primary play task");

    // Secondary play tasks
    hr = pPrimaryPlayTaskNode->get_nextSibling(&pSecondaryPlayTaskNode);
    ExitOnRootFailure(hr, "Failed to get_nextSibling node");
    while (NULL != pSecondaryPlayTaskNode)
    {
        hr = CreateSingleShortcut(pSecondaryPlayTaskNode, sczGDFBinPath, sczInstanceId, sczGameInstallPath, gisInstallScope, false, false);
        ExitOnFailure(hr, "Failed to CreateSingleShortcut for secondary play task");
        hr = pSecondaryPlayTaskNode->get_nextSibling(&pSecondaryPlayTaskNode);
        ExitOnRootFailure(hr, "Failed to get next sibling node");
    }

    hr = XmlSelectSingleNode(pIXMLDOMNode, L"//GameDefinitionFile/GameDefinition/ExtendedProperties/GameTasks/Support", &pSupportTasksNode);
    ExitOnRootFailure(hr, "Failed to find SupportTasksNode element.");

    hr = XmlSelectSingleNode(pSupportTasksNode, L"Task", &pTaskNode);
    ExitOnRootFailure(hr, "Failed to find support task element.");

    while (NULL != pTaskNode)
    {
        hr = CreateSingleShortcut(pTaskNode, sczGDFBinPath, sczInstanceId, sczGameInstallPath, gisInstallScope, false, true);
        ExitOnFailure(hr, "Failed to CreateSingleTask for support tasks");
        hr = pTaskNode->get_nextSibling(&pTaskNode);
        ExitOnRootFailure(hr, "Failed to get next sibling for support tasks");
    }

LExit:
    ReleaseObject(pTaskNode);
    ReleaseObject(pSupportTasksNode);
    ReleaseObject(pSecondaryPlayTaskNode);
    ReleaseObject(pPrimaryPlayTaskNode);
    ReleaseObject(pPlayTasksNode);
    ReleaseObject(pIXMLDOMNode);

    return hr;
}

/******************************************************************
 RemoveShorcuts - delete shortcuts for game tasks during uninstall. This is for the
   case of V2 GDF using IGameExplorer
********************************************************************/
extern "C" HRESULT RemoveShorcuts(
    __in LPCWSTR sczInstanceId,
    __in GAME_INSTALL_SCOPE gisInstallScope)
{
    Assert(sczInstanceId);

    HRESULT hr;
    WCHAR wzPath[MAX_PATH] = {0};
    WCHAR wzAppData[MAX_PATH];

    // Get base path based on install scope
    hr = ::SHGetFolderPathW(NULL, GIS_CURRENT_USER == gisInstallScope ? CSIDL_LOCAL_APPDATA : CSIDL_COMMON_APPDATA, NULL, SHGFP_TYPE_CURRENT, wzAppData);
    ExitOnFailure(hr, "Failed to SHGetFolderPathW for APP data");

    hr = ::StringCchPrintfW(wzPath, MAX_PATH, L"%s\\Microsoft\\Windows\\GameExplorer\\%s", wzAppData, sczInstanceId);
    ExitOnFailure(hr, "Failed to set shortcut path in removing the shortcuts");

    SHFILEOPSTRUCTW fileOp;
    ZeroMemory(&fileOp, sizeof(SHFILEOPSTRUCTW));
    fileOp.wFunc = FO_DELETE;
    fileOp.pFrom = wzPath;
    fileOp.fFlags = FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT;
    ::SHFileOperationW(&fileOp);

LExit:

    return hr;
}

/******************************************************************
 WriteGameExplorerRegistry - write temporary rows to the Registry
   table that an upgrade to Windows Vista looks for to migrate the
   game registration to Game Explorer.
********************************************************************/
extern "C" HRESULT WriteGameExplorerRegistry(
    __in LPCWSTR wzInstanceId,
    __in LPCWSTR wzComponentId,
    __in LPCWSTR wzGdfPath,
    __in LPCWSTR wzInstallDir
    )
{
    LPWSTR wzRegKey = NULL;
    MSIHANDLE hView = NULL;
    MSIHANDLE hColumns = NULL;

    // both strings go under this new key
    HRESULT hr = StrAllocFormatted(&wzRegKey, L"Software\\Microsoft\\Windows\\CurrentVersion\\GameUX\\GamesToFindOnWindowsUpgrade\\%ls", wzInstanceId);
    ExitOnFailure(hr, "failed to allocate GameUX registry key");

    hr = WcaAddTempRecord(&hView, &hColumns, 
        L"Registry",            // the table
        NULL,                   // we don't care about detailed error codes
        1,                      // the column number of the key we want "uniquified" (uniqued?)
        6,                      // the number of columns we're adding
        L"WixGameExplorer",     // primary key (uniquified)
        msidbRegistryRootLocalMachine,
        wzRegKey,
        L"GDFBinaryPath",
        wzGdfPath,
        wzComponentId);
    ExitOnFailure(hr, "failed to add temporary registry row for GDFBinaryPath");

    hr = WcaAddTempRecord(&hView, &hColumns, 
        L"Registry",
        NULL,
        1,
        6,
        L"WixGameExplorer",
        msidbRegistryRootLocalMachine,
        wzRegKey,
        L"GameInstallPath",
        wzInstallDir,
        wzComponentId);
    ExitOnFailure(hr, "failed to add temporary registry row for GameInstallPath");

LExit:
    ::MsiCloseHandle(hView);
    ::MsiCloseHandle(hColumns);
    ReleaseStr(wzRegKey);

    return hr;
}

/******************************************************************
 SchedGameExplorer - entry point for the Game Explorer Custom Action

********************************************************************/
extern "C" HRESULT __stdcall SchedGameExplorer(
    __in BOOL fInstall
    )
{
    HRESULT hr = S_OK;
    int cGames = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    IGameExplorer* piGameExplorer = NULL;
    IGameExplorer2* piGameExplorer2 = NULL;
    LPWSTR pwzInstanceId = NULL;
    LPWSTR pwzFileId = NULL;
    LPWSTR pwzComponentId = NULL;
    LPWSTR pwzFormattedFile = NULL;
    LPWSTR pwzGamePath = NULL;
    LPWSTR pwzGameDir = NULL;
    LPWSTR pwzCustomActionData = NULL;

    // anything to do?
    if (S_OK != WcaTableExists(L"WixGameExplorer"))
    {
        WcaLog(LOGMSG_STANDARD, "WixGameExplorer table doesn't exist, so there are no games to register with Game Explorer");
        goto LExit;
    }

    // try to create an IGameExplorer; if it fails, assume we're on a pre-Vista OS
    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    hr = ::CoCreateInstance(__uuidof(GameExplorer), NULL, CLSCTX_ALL, __uuidof(IGameExplorer), (LPVOID*)&piGameExplorer); 
    BOOL fHasGameExplorer = SUCCEEDED(hr);
    WcaLog(LOGMSG_STANDARD, "IGameExplorer %ls available", fHasGameExplorer ? L"is" : L"is NOT");

    hr = ::CoCreateInstance(__uuidof(GameExplorer), NULL, CLSCTX_ALL, __uuidof(IGameExplorer2), (LPVOID*)&piGameExplorer2); 
    BOOL fHasGameExplorer2 = SUCCEEDED(hr);
    WcaLog(LOGMSG_STANDARD, "IGameExplorer2 %ls available", fHasGameExplorer2 ? L"is" : L"is NOT");

    // query and loop through all the games
    hr = WcaOpenExecuteView(vcsGameuxQuery, &hView);
    ExitOnFailure(hr, "failed to open view on WixGameExplorer table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        ++cGames;

        // start with the instance guid
        hr = WcaGetRecordString(hRec, egqInstanceId, &pwzInstanceId);
        ExitOnFailure(hr, "failed to get game instance id");

        // get file id 
        hr = WcaGetRecordString(hRec, egqFile, &pwzFileId);
        ExitOnFailure(hr, "failed to get game file id");

        // turn that into the path to the target file
        hr = StrAllocFormatted(&pwzFormattedFile, L"[#%s]", pwzFileId);
        ExitOnFailure1(hr, "failed to format file string for file: %ls", pwzFileId);
        hr = WcaGetFormattedString(pwzFormattedFile, &pwzGamePath);
        ExitOnFailure1(hr, "failed to get formatted string for file: %ls", pwzFileId);

        // and then get the directory part of the path
        hr = PathGetDirectory(pwzGamePath, &pwzGameDir);
        ExitOnFailure1(hr, "failed to get path for file: %ls", pwzGamePath);

        // get component and its install/action states
        hr = WcaGetRecordString(hRec, egqComponent, &pwzComponentId);
        ExitOnFailure(hr, "failed to get game component id");

        // we need to know if the component's being installed, uninstalled, or reinstalled
        WCA_TODO todo = WcaGetComponentToDo(pwzComponentId);

        // skip this entry if this is the install CA and we are uninstalling the component
        if (fInstall && WCA_TODO_UNINSTALL == todo)
        {
            continue;
        }

        // skip this entry if this is an uninstall CA and we are not uninstalling the component
        if (!fInstall && WCA_TODO_UNINSTALL != todo)
        {
            continue;
        }

        // if we got a Game Explorer, write the CA data; otherwise,
        // just write the registry values for an XP-to-Vista upgrade
        if (fHasGameExplorer || fHasGameExplorer2)
        {
            // write custom action data: operation, instance guid, path, directory
            hr = WcaWriteIntegerToCaData(todo, &pwzCustomActionData);
            ExitOnFailure1(hr, "failed to write Game Explorer operation to custom action data for instance id: %ls", pwzInstanceId);

            hr = WcaWriteStringToCaData(pwzInstanceId, &pwzCustomActionData);
            ExitOnFailure1(hr, "failed to write custom action data for instance id: %ls", pwzInstanceId);

            hr = WcaWriteStringToCaData(pwzGamePath, &pwzCustomActionData);
            ExitOnFailure1(hr, "failed to write game path to custom action data for instance id: %ls", pwzInstanceId);

            hr = WcaWriteStringToCaData(pwzGameDir, &pwzCustomActionData);
            ExitOnFailure1(hr, "failed to write game install directory to custom action data for instance id: %ls", pwzInstanceId);
        }
        else
        {
            hr = WriteGameExplorerRegistry(pwzInstanceId, pwzComponentId, pwzGamePath, pwzGameDir);
            ExitOnFailure1(hr, "failed to write registry rows for game id: %ls", pwzInstanceId);
        }
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occured while processing WixGameExplorer table");

    // schedule ExecGameExplorer if there's anything to do
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling Game Explorer (%ls)", pwzCustomActionData);
        hr = WcaDoDeferredAction(L"WixRollbackGameExplorer", pwzCustomActionData, cGames * COST_GAMEEXPLORER);
        ExitOnFailure(hr, "Failed to schedule Game Explorer rollback");
        hr = WcaDoDeferredAction(L"WixExecGameExplorer", pwzCustomActionData, cGames * COST_GAMEEXPLORER);
        ExitOnFailure(hr, "Failed to schedule Game Explorer execution");
    }

LExit:
    ReleaseStr(pwzInstanceId);
    ReleaseStr(pwzFileId);
    ReleaseStr(pwzComponentId);
    ReleaseStr(pwzFormattedFile);
    ReleaseStr(pwzGamePath);
    ReleaseStr(pwzCustomActionData);
    ReleaseObject(piGameExplorer2);
    ReleaseObject(piGameExplorer);

    ::CoUninitialize();

    return hr;
}

/******************************************************************
 SchedGameExplorerUninstall - entry point for the Game Explorer uninstall Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedGameExplorerUninstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "SchedGameExplorerUninstall");
    ExitOnFailure(hr, "failed to initialize");

    hr = SchedGameExplorer(FALSE);

LExit:
    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

/******************************************************************
 SchedGameExplorer - entry point for the Game Explorer install Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedGameExplorerInstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "SchedGameExplorerInstall");
    ExitOnFailure(hr, "failed to initialize");

    hr = SchedGameExplorer(TRUE);

LExit:
    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

/******************************************************************
 ExecGameExplorer - entry point for Game Explorer Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecGameExplorer(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    BOOL fHasAccess = FALSE;
    GUID guidInstanceId = {0};

    IGameExplorer* piGameExplorer = NULL;
    IGameExplorer2* piGameExplorer2 = NULL;
    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwz = NULL;
    int iOperation = 0;
    LPWSTR pwzInstanceId = NULL;
    LPWSTR pwzGamePath = NULL;
    LPWSTR pwzGameDir = NULL;
    BSTR bstrGamePath = NULL;
    BSTR bstrGameDir = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ExecGameExplorer");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    // try to create an IGameExplorer
    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    hr = ::CoCreateInstance(__uuidof(GameExplorer), NULL, CLSCTX_ALL, __uuidof(IGameExplorer), (LPVOID*)&piGameExplorer);
    BOOL fHasGameExplorer = SUCCEEDED(hr);
    WcaLog(LOGMSG_STANDARD, "IGameExplorer %ls available", fHasGameExplorer ? L"is" : L"is NOT");

    hr = ::CoCreateInstance(__uuidof(GameExplorer), NULL, CLSCTX_ALL, __uuidof(IGameExplorer2), (LPVOID*)&piGameExplorer2); 
    BOOL fHasGameExplorer2 = SUCCEEDED(hr);
    WcaLog(LOGMSG_STANDARD, "IGameExplorer2 %ls available", fHasGameExplorer2 ? L"is" : L"is NOT");

    // nothing to do if there's no Game Explorer (though we should have been scheduled only if
    // there was a Game Explorer when we started the install).
    if (fHasGameExplorer || fHasGameExplorer2)
    {
        // loop through all the passed in data
        pwz = pwzCustomActionData;
        while (pwz && *pwz)
        {
            // extract the custom action data
            hr = WcaReadIntegerFromCaData(&pwz, &iOperation);
            ExitOnFailure(hr, "failed to read operation from custom action data");
            hr = WcaReadStringFromCaData(&pwz, &pwzInstanceId);
            ExitOnFailure(hr, "failed to read instance ID from custom action data");
            hr = WcaReadStringFromCaData(&pwz, &pwzGamePath);
            ExitOnFailure(hr, "failed to read GDF path from custom action data");
            hr = WcaReadStringFromCaData(&pwz, &pwzGameDir);
            ExitOnFailure(hr, "failed to read game installation directory from custom action data");

            // convert from LPWSTRs to BSTRs and GUIDs, which are what IGameExplorer wants
            hr = ::CLSIDFromString(pwzInstanceId, &guidInstanceId);
            ExitOnFailure1(hr, "couldn't convert invalid GUID string '%ls'", pwzInstanceId);

            bstrGamePath = ::SysAllocString(pwzGamePath);
            ExitOnNull(bstrGamePath, hr, E_OUTOFMEMORY, "failed SysAllocString for bstrGamePath");
            bstrGameDir = ::SysAllocString(pwzGameDir);
            ExitOnNull(bstrGameDir, hr, E_OUTOFMEMORY, "failed SysAllocString for bstrGameDir");

            // if rolling back, swap INSTALL and UNINSTALL
            if (::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK))
            {
                if (WCA_TODO_INSTALL == iOperation)
                {
                    iOperation = WCA_TODO_UNINSTALL;
                }
                else if (WCA_TODO_UNINSTALL == iOperation)
                {
                    iOperation = WCA_TODO_INSTALL;
                }
            }

            BOOL fIsV2GDF = FALSE;
            hr = IsV2GDF(pwzGamePath, &fIsV2GDF);
            ExitOnFailure(hr, "failed to get game GDF version");
            WcaLog(LOGMSG_STANDARD, "The GDF of this game is %ls", fIsV2GDF ? L"V2" : L"V1");

            if (fIsV2GDF && fHasGameExplorer2)
            {
                switch (iOperation)
                {
                case WCA_TODO_INSTALL:
                case WCA_TODO_REINSTALL:
                    hr = piGameExplorer2->InstallGame(bstrGamePath, bstrGameDir, GIS_ALL_USERS);
                    ExitOnFailure1(hr, "failed to install game: %ls", bstrGamePath);
                    break;
                case WCA_TODO_UNINSTALL:
                    hr = piGameExplorer2->UninstallGame(bstrGamePath);
                    ExitOnFailure1(hr, "failed to remove game instance: %ls", pwzInstanceId);
                    break;
                }
            }
            else if (fHasGameExplorer)
            {
                switch (iOperation)
                {
                case WCA_TODO_INSTALL:
                    hr = piGameExplorer->VerifyAccess(bstrGamePath, &fHasAccess);
                    ExitOnFailure1(hr, "failed to verify game access: %ls", pwzInstanceId);

                    if (SUCCEEDED(hr) && fHasAccess)
                    {
                        WcaLog(LOGMSG_STANDARD, "Adding game: %ls, %ls", bstrGamePath, bstrGameDir);
                        hr = piGameExplorer->AddGame(bstrGamePath, bstrGameDir, GIS_ALL_USERS, &guidInstanceId);
                    }
                    ExitOnFailure1(hr, "failed to add game instance: %ls", pwzInstanceId);

                    if (fIsV2GDF)
                    {
                        hr = CreateShorcuts(pwzGamePath, pwzInstanceId, pwzGameDir, GIS_ALL_USERS);
                        ExitOnFailure(hr, "failed to add shortcuts for game tasks");
                    }
                    break;
                case WCA_TODO_REINSTALL:
                    hr = piGameExplorer->UpdateGame(guidInstanceId);
                    ExitOnFailure1(hr, "failed to update game instance: %ls", pwzInstanceId);
                    break;
                case WCA_TODO_UNINSTALL:
                    hr = piGameExplorer->RemoveGame(guidInstanceId);
                    ExitOnFailure1(hr, "failed to remove game instance: %ls", pwzInstanceId);
                    if (fIsV2GDF)
                    {
                        hr = RemoveShorcuts(pwzInstanceId, GIS_ALL_USERS);
                        ExitOnFailure(hr, "failed to remove shortcuts for game tasks");
                    }
                    break;
                }
            }

            // Tick the progress bar along for this game
            hr = WcaProgressMessage(COST_GAMEEXPLORER, FALSE);
            ExitOnFailure1(hr, "failed to tick progress bar for game instance: %ls", pwzInstanceId);
        }
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzInstanceId);
    ReleaseStr(pwzGamePath);
    ReleaseStr(pwzGameDir);
    ReleaseBSTR(bstrGamePath);
    ReleaseBSTR(bstrGameDir);
    ReleaseObject(piGameExplorer2);
    ReleaseObject(piGameExplorer);
    ::CoUninitialize();

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}
