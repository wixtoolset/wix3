//-------------------------------------------------------------------------------------------------
// <copyright file="netfxca.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    NetFx custom action code.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define NGEN_DEBUG   0x0001
#define NGEN_NODEP  0x0002
#define NGEN_PROFILE 0x0004
#define NGEN_32BIT  0x0008
#define NGEN_64BIT  0x0010

#define NGEN_TIMEOUT 60000 // 60 seconds

// If you change one of these strings, be sure to change the appropriate EmptyFormattedLength variable right below
LPCWSTR vpwzUnformattedQuotedFile = L"\"[#%s]\"";
LPCWSTR vpwzUnformattedQuotedDirectory = L"\"[%s]\\\"";

// These represent the length of the above strings in the case that the property resolves to an empty string
const DWORD EMPTY_FORMATTED_LENGTH_QUOTED_FILE = 2;
const DWORD EMPTY_FORMATTED_LENGTH_QUOTED_DIRECTORY = 3;

LPCWSTR vcsFileId =
    L"SELECT `File` FROM `File` WHERE `File`=?";
enum eFileId { fiFile = 1 };

LPCWSTR vcsNgenQuery =
    L"SELECT `NetFxNativeImage`.`File_`, `NetFxNativeImage`.`NetFxNativeImage`, `NetFxNativeImage`.`Priority`, `NetFxNativeImage`.`Attributes`, `NetFxNativeImage`.`File_Application`, `NetFxNativeImage`.`Directory_ApplicationBase`, `File`.`Component_` "
    L"FROM `NetFxNativeImage`, `File` WHERE `File`.`File`=`NetFxNativeImage`.`File_`";
enum eNgenQuery { ngqFile = 1, ngqId, ngqPriority, ngqAttributes, ngqFileApp, ngqDirAppBase, ngqComponent };

LPCWSTR vcsNgenGac =
    L"SELECT `MsiAssembly`.`File_Application` "
    L"FROM `File`, `MsiAssembly` WHERE `File`.`Component_`=`MsiAssembly`.`Component_` AND `File`.`File`=?";
enum eNgenGac { nggApplication = 1 };

LPCWSTR vcsNgenStrongName =
    L"SELECT `Name`,`Value` FROM `MsiAssemblyName` WHERE `Component_`=?";
enum eNgenStrongName { ngsnName = 1, ngsnValue };

// Searches subdirectories of the given path for the highest version of ngen.exe available
static HRESULT GetNgenVersion(
    __in LPWSTR pwzParentPath,
    __out LPWSTR* ppwzVersion
    )
{
    Assert(pwzParentPath);

    HRESULT hr = S_OK;
    DWORD dwError = 0;
    DWORD dwNgenFileFlags = 0;

    LPWSTR pwzVersionSearch = NULL;
    LPWSTR pwzNgen = NULL;
    LPWSTR pwzTemp = NULL;
    LPWSTR pwzTempVersion = NULL;
    DWORD dwMaxMajorVersion = 0; // This stores the highest major version we've seen so far
    DWORD dwMaxMinorVersion = 0; // This stores the minor version of the highest major version we've seen so far
    DWORD dwMajorVersion = 0; // This stores the major version of the directory we're currently considering
    DWORD dwMinorVersion = 0; // This stores the minor version of the directory we're currently considering
    BOOL fFound = TRUE;
    WIN32_FIND_DATAW wfdVersionDirectories;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    
    hr = StrAllocFormatted(&pwzVersionSearch, L"%s*", pwzParentPath);
    ExitOnFailure1(hr, "failed to create outer directory search string from string %ls", pwzParentPath);
    hFind = FindFirstFileW(pwzVersionSearch, &wfdVersionDirectories);
    if (hFind == INVALID_HANDLE_VALUE)
    {
        ExitWithLastError1(hr, "failed to call FindFirstFileW with string %ls", pwzVersionSearch);
    }

    while (fFound)
    {
        pwzTempVersion = (LPWSTR)&(wfdVersionDirectories.cFileName);

        // Explicitly exclude v1.1.4322, which isn't backwards compatible and is not supported
        if (wfdVersionDirectories.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
        {
            if (0 != lstrcmpW(L"v1.1.4322", pwzTempVersion))
            {
                // A potential candidate directory was found to run ngen from - let's make sure ngen actually exists here
                hr = StrAllocFormatted(&pwzNgen, L"%s%s\\ngen.exe", pwzParentPath, pwzTempVersion);
                ExitOnFailure2(hr, "failed to create inner ngen search string with strings %ls and %ls", pwzParentPath, pwzTempVersion);

                // If Ngen.exe does exist as a file here, then let's check the file version
                if (FileExistsEx(pwzNgen, &dwNgenFileFlags) && (0 == (dwNgenFileFlags & FILE_ATTRIBUTE_DIRECTORY)))
                {
                    hr = FileVersion(pwzNgen, &dwMajorVersion, &dwMinorVersion);

                    if (FAILED(hr))
                    {
                        WcaLog(LOGMSG_VERBOSE, "Failed to get version of %ls - continuing", pwzNgen);
                    }
                    else if (dwMajorVersion > dwMaxMajorVersion || (dwMajorVersion == dwMaxMajorVersion && dwMinorVersion > dwMaxMinorVersion))
                    {
                        // If the version we found is the highest we've seen so far in this search, it will be our new best-so-far candidate
                        hr = StrAllocString(ppwzVersion, pwzTempVersion, 0);
                        ExitOnFailure1(hr, "failed to copy temp version string %ls to version string", pwzTempVersion);
                        // Add one for the backslash after the directory name
                        WcaLog(LOGMSG_VERBOSE, "Found highest-so-far version of ngen.exe (in directory %ls, version %u.%u.%u.%u)", *ppwzVersion, (DWORD)HIWORD(dwMajorVersion), (DWORD)LOWORD(dwMajorVersion), (DWORD)HIWORD(dwMinorVersion), (DWORD)LOWORD(dwMinorVersion));

                        dwMaxMajorVersion = dwMajorVersion;
                        dwMaxMinorVersion = dwMinorVersion;
                    }
                }
                else
                {
                    WcaLog(LOGMSG_VERBOSE, "Ignoring %ls because it doesn't contain the file ngen.exe", pwzTempVersion);
                }
            }
            else
            {
                WcaLog(LOGMSG_VERBOSE, "Ignoring %ls because it is from .NET Framework v1.1, which is not backwards compatible with other versions of the Framework and thus is not supported by this custom action.", pwzTempVersion);
            }
        }
        else
        {
            WcaLog(LOGMSG_VERBOSE, "Ignoring %ls because it isn't a directory", pwzTempVersion);
        }

        fFound = FindNextFileW(hFind, &wfdVersionDirectories);

        if (!fFound)
        {
            dwError = ::GetLastError();
            hr = (ERROR_NO_MORE_FILES == dwError) ? ERROR_SUCCESS : HRESULT_FROM_WIN32(dwError);
            ExitOnFailure1(hr, "Failed to call FindNextFileW() with query %ls", pwzVersionSearch);
        }
    }

    if (NULL == *ppwzVersion)
    {
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        ExitOnRootFailure1(hr, "Searched through all subdirectories of %ls, but failed to find any version of ngen.exe", pwzParentPath);
    }
    else
    {
        WcaLog(LOGMSG_VERBOSE, "Using highest version of ngen found, located in this subdirectory: %ls, version %u.%u.%u.%u", *ppwzVersion, (DWORD)HIWORD(dwMajorVersion), (DWORD)LOWORD(dwMajorVersion), (DWORD)HIWORD(dwMinorVersion), (DWORD)LOWORD(dwMinorVersion));
    }

LExit:
    if (hFind != INVALID_HANDLE_VALUE)
    {
        if (0 == FindClose(hFind))
        {
            dwError = ::GetLastError();
            hr = HRESULT_FROM_WIN32(dwError);
            WcaLog(LOGMSG_STANDARD, "Failed to close handle created by outer FindFirstFile with error %x - continuing", hr);
        }
        hFind = INVALID_HANDLE_VALUE;
    }

    ReleaseStr(pwzVersionSearch);
    ReleaseStr(pwzNgen);
    ReleaseStr(pwzTemp);
    // Purposely don't release pwzTempVersion, because it wasn't allocated in this function, it's just a pointer to a string inside wfdVersionDirectories

    return hr;
}

// Gets the path to ngen.exe
static HRESULT GetNgenPath(
    __out LPWSTR* ppwzNgenPath,
    __in BOOL f64BitFramework
    )
{
    Assert(ppwzNgenPath);
    HRESULT hr = S_OK;

    LPWSTR pwzVersion = NULL;
    LPWSTR pwzWindowsFolder = NULL;

    hr = WcaGetProperty(L"WindowsFolder", &pwzWindowsFolder);
    ExitOnFailure(hr, "failed to get WindowsFolder property");

    hr = StrAllocString(ppwzNgenPath, pwzWindowsFolder, 0);
    ExitOnFailure1(hr, "failed to copy to NgenPath windows folder: %ls", pwzWindowsFolder);

    if (f64BitFramework)
    {
        WcaLog(LOGMSG_VERBOSE, "Searching for ngen under 64-bit framework path");

        hr = StrAllocConcat(ppwzNgenPath, L"Microsoft.NET\\Framework64\\", 0);
        ExitOnFailure(hr, "failed to copy platform portion of ngen path");
    }
    else
    {
        WcaLog(LOGMSG_VERBOSE, "Searching for ngen under 32-bit framework path");

        hr = StrAllocConcat(ppwzNgenPath, L"Microsoft.NET\\Framework\\", 0);
        ExitOnFailure(hr, "failed to copy platform portion of ngen path");
    }

    // We want to run the highest version of ngen possible, because they should be backwards compatible - so let's find the most appropriate directory now
    hr = GetNgenVersion(*ppwzNgenPath, &pwzVersion);
    ExitOnFailure1(hr, "failed to search for ngen under path %ls", *ppwzNgenPath);

    hr = StrAllocConcat(ppwzNgenPath, pwzVersion, 0);
    ExitOnFailure(hr, "failed to copy version portion of ngen path");

    hr = StrAllocConcat(ppwzNgenPath, L"\\ngen.exe", 0);
    ExitOnFailure(hr, "failed to copy \"\\ngen.exe\" portion of ngen path");

LExit:
    ReleaseStr(pwzVersion);
    ReleaseStr(pwzWindowsFolder);

    return hr;
}


static HRESULT GetStrongName(
    __out LPWSTR* ppwzStrongName,
    __in LPCWSTR pwzComponent
    )
{
    Assert(ppwzStrongName);
    HRESULT hr = S_OK;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hComponentRec = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR pwzData = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzVersion = NULL;
    LPWSTR pwzCulture = NULL;
    LPWSTR pwzPublicKeyToken = NULL;

    hComponentRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hComponentRec, 1, pwzComponent);
    ExitOnFailure1(hr, "failed to set component value in record to: %ls", pwzComponent);

    // get the name value records for this component
    hr = WcaOpenView(vcsNgenStrongName, &hView);
    ExitOnFailure(hr, "failed to open view on NetFxNativeImage table");

    hr = WcaExecuteView(hView, hComponentRec);
    ExitOnFailure(hr, "failed to execute strong name view");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, ngsnName, &pwzData);
        ExitOnFailure1(hr, "failed to get MsiAssemblyName.Name for component: %ls", pwzComponent);

        if (0 == lstrcmpW(L"name", pwzData))
        {
            hr = WcaGetRecordString(hRec, ngsnValue, &pwzName);
            ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %ls Name: %ls", pwzComponent, pwzData);
        }
        else if (0 == lstrcmpW(L"version", pwzData))
        {
            hr = WcaGetRecordString(hRec, ngsnValue, &pwzVersion);
            ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %ls Name: %ls", pwzComponent, pwzData);
        }
        else if (0 == lstrcmpW(L"culture", pwzData))
        {
            hr = WcaGetRecordString(hRec, ngsnValue, &pwzCulture);
            ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %ls Name: %ls", pwzComponent, pwzData);
        }
        else if (0 == lstrcmpW(L"publicKeyToken", pwzData))
        {
            hr = WcaGetRecordString(hRec, ngsnValue, &pwzPublicKeyToken);
            ExitOnFailure2(hr, "failed to get MsiAssemblyName.Value for component: %ls Name: %ls", pwzComponent, pwzData);
        }
    }
    if (E_NOMOREITEMS == hr)
        hr = S_OK;
    ExitOnFailure1(hr, "failed while looping through all names and values in MsiAssemblyName table for component: %ls", pwzComponent);

    hr = StrAllocFormatted(ppwzStrongName, L"\"%s, Version=%s, Culture=%s, PublicKeyToken=%s\"", pwzName, pwzVersion, pwzCulture, pwzPublicKeyToken);
    ExitOnFailure1(hr, "failed to format strong name for component: %ls", pwzComponent);

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzVersion);
    ReleaseStr(pwzCulture);
    ReleaseStr(pwzPublicKeyToken);

    return hr;
}

static HRESULT CreateInstallCommand(
    __out LPWSTR* ppwzCommandLine,
    __in LPCWSTR pwzNgenPath,
    __in LPCWSTR pwzFile,
    __in int iPriority,
    __in int iAttributes,
    __in LPCWSTR pwzFileApp,
    __in LPCWSTR pwzDirAppBase
    )
{
    Assert(ppwzCommandLine && pwzNgenPath && *pwzNgenPath && pwzFile && *pwzFile&& pwzFileApp && pwzDirAppBase);
    HRESULT hr = S_OK;

    LPWSTR pwzQueueString = NULL;

    hr = StrAllocFormatted(ppwzCommandLine, L"%s install %s", pwzNgenPath, pwzFile);
    ExitOnFailure(hr, "failed to assemble install command line");

    if (iPriority > 0)
    {
        hr = StrAllocFormatted(&pwzQueueString, L" /queue:%d", iPriority);
        ExitOnFailure(hr, "failed to format queue string");

        hr = StrAllocConcat(ppwzCommandLine, pwzQueueString, 0);
        ExitOnFailure(hr, "failed to add queue string to NGEN command line");
    }

    if (NGEN_DEBUG & iAttributes)
    {
        hr = StrAllocConcat(ppwzCommandLine, L" /Debug", 0);
        ExitOnFailure(hr, "failed to add debug to NGEN command line");
    }

    if (NGEN_PROFILE & iAttributes)
    {
        hr = StrAllocConcat(ppwzCommandLine, L" /Profile", 0);
        ExitOnFailure(hr, "failed to add profile to NGEN command line");
    }

    if (NGEN_NODEP & iAttributes)
    {
        hr = StrAllocConcat(ppwzCommandLine, L" /NoDependencies", 0);
        ExitOnFailure(hr, "failed to add no dependencies to NGEN command line");
    }

    // If it's more than just two quotes around an empty string
    if (EMPTY_FORMATTED_LENGTH_QUOTED_FILE < lstrlenW(pwzFileApp))
    {
        hr = StrAllocConcat(ppwzCommandLine, L" /ExeConfig:", 0);
        ExitOnFailure(hr, "failed to add exe config to NGEN command line");

        hr = StrAllocConcat(ppwzCommandLine, pwzFileApp, 0);
        ExitOnFailure(hr, "failed to add file app to NGEN command line");
    }

    // If it's more than just two quotes around a backslash
    if (EMPTY_FORMATTED_LENGTH_QUOTED_DIRECTORY < lstrlenW(pwzDirAppBase))
    {
        hr = StrAllocConcat(ppwzCommandLine, L" /AppBase:", 0);
        ExitOnFailure(hr, "failed to add app base to NGEN command line");

        hr = StrAllocConcat(ppwzCommandLine, pwzDirAppBase, 0);
        ExitOnFailure(hr, "failed to add dir app base to NGEN command line");
    }

LExit:
    return hr;
}

/******************************************************************
 FileIdExists - checks if the file ID is found in the File table

 returns S_OK if the file exists; S_FALSE if not; otherwise, error
********************************************************************/
static HRESULT FileIdExists(
    __in_opt LPCWSTR wzFile
    )
{
    HRESULT hr = S_OK;
    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    if (!wzFile)
    {
        hr = S_FALSE;
        ExitFunction();
    }

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, fiFile, wzFile);
    ExitOnFailure1(hr, "failed to create a record with the file: %ls", wzFile);

    hr = WcaTableExists(L"File");
    if (S_OK == hr)
    {
        hr = WcaOpenView(vcsFileId, &hView);
        ExitOnFailure(hr, "failed to open view on File table");

        hr = WcaExecuteView(hView, hRec);
        ExitOnFailure(hr, "failed to execute view on File table");

        // Reuse the same record; the handle will be released.
        hr = WcaFetchSingleRecord(hView, &hRec);
        ExitOnFailure(hr, "failed to fetch File from File table");
    }

LExit:

    return hr;
}

/******************************************************************
 SchedNetFx - entry point for NetFx Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedNetFx(
    __in MSIHANDLE hInstall
    )
{
    // AssertSz(FALSE, "debug SchedNetFx");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzInstallCustomActionData = NULL;
    LPWSTR pwzUninstallCustomActionData = NULL;
    UINT uiCost = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;
    PMSIHANDLE hViewGac = NULL;
    PMSIHANDLE hRecGac = NULL;

    LPWSTR pwzId = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzTemp = NULL;
    LPWSTR pwzFile = NULL;
    int iPriority = 0;
    int iAssemblyCost = 0;
    int iAttributes = 0;
    LPWSTR pwzFileApp = NULL;
    LPWSTR pwzDirAppBase = NULL;
    LPWSTR pwzComponent = NULL;

    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    LPWSTR pwz32Ngen = NULL;
    LPWSTR pwz64Ngen = NULL;

    BOOL f32NgenExeExists = FALSE;
    BOOL f64NgenExeExists = FALSE;

    BOOL fNeedInstallUpdate32 = FALSE;
    BOOL fNeedUninstallUpdate32 = FALSE;
    BOOL fNeedInstallUpdate64 = FALSE;
    BOOL fNeedUninstallUpdate64 = FALSE;

    // initialize
    hr = WcaInitialize(hInstall, "SchedNetFx");
    ExitOnFailure(hr, "failed to initialize");

    hr = GetNgenPath(&pwz32Ngen, FALSE);
    f32NgenExeExists = SUCCEEDED(hr);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = ERROR_SUCCESS;
        WcaLog(LOGMSG_STANDARD, "Failed to find 32bit ngen. No actions will be scheduled to create native images for 32bit.");
    }
    ExitOnFailure(hr, "failed to get 32bit ngen.exe path");

    hr = GetNgenPath(&pwz64Ngen, TRUE);
    f64NgenExeExists = SUCCEEDED(hr);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr || HRESULT_FROM_WIN32(ERROR_PATH_NOT_FOUND) == hr)
    {
        hr = ERROR_SUCCESS;
        WcaLog(LOGMSG_STANDARD, "Failed to find 64bit ngen. No actions will be scheduled to create native images for 64bit.");
    }
    ExitOnFailure(hr, "failed to get 64bit ngen.exe path");

    // loop through all the NetFx records
    hr = WcaOpenExecuteView(vcsNgenQuery, &hView);
    ExitOnFailure(hr, "failed to open view on NetFxNativeImage table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        // Get Id
        hr = WcaGetRecordString(hRec, ngqId, &pwzId);
        ExitOnFailure(hr, "failed to get NetFxNativeImage.NetFxNativeImage");

        // Get File
        hr = WcaGetRecordString(hRec, ngqFile, &pwzData);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.File_ for record: %ls", pwzId);
        hr = StrAllocFormatted(&pwzTemp, vpwzUnformattedQuotedFile, pwzData);
        ExitOnFailure1(hr, "failed to format file string for file: %ls", pwzData);
        hr = WcaGetFormattedString(pwzTemp, &pwzFile);
        ExitOnFailure1(hr, "failed to get formatted string for file: %ls", pwzData);

        // Get Priority
        hr = WcaGetRecordInteger(hRec, ngqPriority, &iPriority);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.Priority for record: %ls", pwzId);

        if (0 == iPriority)
            iAssemblyCost = COST_NGEN_BLOCKING;
        else
            iAssemblyCost = COST_NGEN_NONBLOCKING;

        // Get Attributes
        hr = WcaGetRecordInteger(hRec, ngqAttributes, &iAttributes);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.Attributes for record: %ls", pwzId);

        // Get File_Application or leave pwzFileApp NULL.
        hr = WcaGetRecordFormattedString(hRec, ngqFileApp, &pwzData);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.File_Application for record: %ls", pwzId);

        // Check if the value resolves to a valid file ID.
        if (S_OK == FileIdExists(pwzData))
        {
            // Resolve the file ID to a path.
            hr = StrAllocFormatted(&pwzTemp, vpwzUnformattedQuotedFile, pwzData);
            ExitOnFailure1(hr, "failed to format file application string for file: %ls", pwzData);

            hr = WcaGetFormattedString(pwzTemp, &pwzFileApp);
            ExitOnFailure1(hr, "failed to get formatted string for file application: %ls", pwzData);
        }
        else
        {
            // Assume record formatted to a path already.
            hr = StrAllocString(&pwzFileApp, pwzData, 0);
            ExitOnFailure1(hr, "failed to allocate string for file path: %ls", pwzData);

            hr = PathEnsureQuoted(&pwzFileApp, FALSE);
            ExitOnFailure1(hr, "failed to quote file path: %ls", pwzData);
        }

        // Get Directory_ApplicationBase or leave pwzDirAppBase NULL.
        hr = WcaGetRecordFormattedString(hRec, ngqDirAppBase, &pwzData);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.Directory_ApplicationBase for record: %ls", pwzId);

        if (WcaIsUnicodePropertySet(pwzData))
        {
            // Resolve the directory ID to a path.
            hr = StrAllocFormatted(&pwzTemp, vpwzUnformattedQuotedDirectory, pwzData);
            ExitOnFailure1(hr, "failed to format directory application base string for property: %ls", pwzData);

            hr = WcaGetFormattedString(pwzTemp, &pwzDirAppBase);
            ExitOnFailure1(hr, "failed to get formatted string for directory application base: %ls", pwzData);
        }
        else
        {
            // Assume record formatted to a path already.
            hr = StrAllocString(&pwzDirAppBase, pwzData, 0);
            ExitOnFailure1(hr, "failed to allocate string for directory path: %ls", pwzData);

            hr = PathEnsureQuoted(&pwzDirAppBase, TRUE);
            ExitOnFailure1(hr, "failed to quote and backslashify directory: %ls", pwzData);
        }

        // Get Component
        hr = WcaGetRecordString(hRec, ngqComponent, &pwzComponent);
        ExitOnFailure1(hr, "failed to get NetFxNativeImage.Directory_ApplicationBase for record: %ls", pwzId);
        er = ::MsiGetComponentStateW(hInstall, pwzComponent, &isInstalled, &isAction);
        ExitOnWin32Error1(er, hr, "failed to get install state for Component: %ls", pwzComponent);

        //
        // Figure out if it's going to be GAC'd.  The possibility exists that no assemblies are going to be GAC'd 
        // so we have to check for the MsiAssembly table first.
        //
        if (S_OK == WcaTableExists(L"MsiAssembly"))
        {
            hr = WcaOpenView(vcsNgenGac, &hViewGac);
            ExitOnFailure(hr, "failed to open view on File/MsiAssembly table");

            hr = WcaExecuteView(hViewGac, hRec);
            ExitOnFailure(hr, "failed to execute view on File/MsiAssembly table");

            hr = WcaFetchSingleRecord(hViewGac, &hRecGac);
            ExitOnFailure(hr, "failed to fetch File_Assembly from File/MsiAssembly table");

            if (S_FALSE != hr)
            {
                hr = WcaGetRecordString(hRecGac, nggApplication, &pwzData);
                ExitOnFailure(hr, "failed to get MsiAssembly.File_Application");

                // If it's in the GAC replace the file name with the strong name
                if (L'\0' == pwzData[0])
                {
                    hr = GetStrongName(&pwzFile, pwzComponent);
                    ExitOnFailure1(hr, "failed to get strong name for component: %ls", pwzData);
                }
            }
        }

        //
        // Schedule the work
        //
        if (!(iAttributes & NGEN_32BIT) && !(iAttributes & NGEN_64BIT))
            ExitOnFailure1(hr = E_INVALIDARG, "Neither 32bit nor 64bit is specified for NGEN of file: %ls", pwzFile);

        if (WcaIsInstalling(isInstalled, isAction) || WcaIsReInstalling(isInstalled, isAction))
        {
            if (iAttributes & NGEN_32BIT && f32NgenExeExists)
            {
                // Assemble the install command line
                hr = CreateInstallCommand(&pwzData, pwz32Ngen, pwzFile, iPriority, iAttributes, pwzFileApp, pwzDirAppBase);
                ExitOnFailure(hr, "failed to create install command line");

                hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData);
                ExitOnFailure1(hr, "failed to add install command to custom action data: %ls", pwzData);

                hr = WcaWriteIntegerToCaData(iAssemblyCost, &pwzInstallCustomActionData);
                ExitOnFailure1(hr, "failed to add cost to custom action data: %ls", pwzData);

                uiCost += iAssemblyCost;

                fNeedInstallUpdate32 = TRUE;
            }

            if (iAttributes & NGEN_64BIT && f64NgenExeExists)
            {
                // Assemble the install command line
                hr = CreateInstallCommand(&pwzData, pwz64Ngen, pwzFile, iPriority, iAttributes, pwzFileApp, pwzDirAppBase);
                ExitOnFailure(hr, "failed to create install command line");

                hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
                ExitOnFailure1(hr, "failed to add install command to custom action data: %ls", pwzData);

                hr = WcaWriteIntegerToCaData(iAssemblyCost, &pwzInstallCustomActionData); // cost
                ExitOnFailure1(hr, "failed to add cost to custom action data: %ls", pwzData);

                uiCost += iAssemblyCost;

                fNeedInstallUpdate64 = TRUE;
            }
        }
        else if (WcaIsUninstalling(isInstalled, isAction))
        {
            if (iAttributes & NGEN_32BIT && f32NgenExeExists)
            {
                hr = StrAllocFormatted(&pwzData, L"%s uninstall %s", pwz32Ngen, pwzFile);
                ExitOnFailure(hr, "failed to create update 32 command line");

                hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
                ExitOnFailure1(hr, "failed to add install command to custom action data: %ls", pwzData);

                hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
                ExitOnFailure1(hr, "failed to add cost to custom action data: %ls", pwzData);

                uiCost += COST_NGEN_NONBLOCKING;

                fNeedUninstallUpdate32 = TRUE;
            }

            if (iAttributes & NGEN_64BIT && f64NgenExeExists)
            {
                hr = StrAllocFormatted(&pwzData, L"%s uninstall %s", pwz64Ngen, pwzFile);
                ExitOnFailure(hr, "failed to create update 64 command line");

                hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
                ExitOnFailure1(hr, "failed to add install command to custom action data: %ls", pwzData);

                hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
                ExitOnFailure1(hr, "failed to add cost to custom action data: %ls", pwzData);

                uiCost += COST_NGEN_NONBLOCKING;

                fNeedUninstallUpdate64 = TRUE;
            }
        }
    }
    if (E_NOMOREITEMS == hr)
        hr = S_OK;
    ExitOnFailure(hr, "failed while looping through all files to create native images for");

    // If we need 32 bit install update
    if (fNeedInstallUpdate32)
    {
        hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz32Ngen);
        ExitOnFailure(hr, "failed to create install update 32 command line");

        hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
        ExitOnFailure1(hr, "failed to add install command to install custom action data: %ls", pwzData);

        hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzInstallCustomActionData); // cost
        ExitOnFailure1(hr, "failed to add cost to install custom action data: %ls", pwzData);

        uiCost += COST_NGEN_NONBLOCKING;
    }

    // If we need 32 bit uninstall update
    if (fNeedUninstallUpdate32)
    {
        hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz32Ngen);
        ExitOnFailure(hr, "failed to create uninstall update 32 command line");

        hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
        ExitOnFailure1(hr, "failed to add install command to uninstall custom action data: %ls", pwzData);

        hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
        ExitOnFailure1(hr, "failed to add cost to uninstall custom action data: %ls", pwzData);

        uiCost += COST_NGEN_NONBLOCKING;
    }

    // If we need 64 bit install update
    if (fNeedInstallUpdate64)
    {
        hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz64Ngen);
        ExitOnFailure(hr, "failed to create install update 64 command line");

        hr = WcaWriteStringToCaData(pwzData, &pwzInstallCustomActionData); // command
        ExitOnFailure1(hr, "failed to add install command to install custom action data: %ls", pwzData);

        hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzInstallCustomActionData); // cost
        ExitOnFailure1(hr, "failed to add cost to install custom action data: %ls", pwzData);

        uiCost += COST_NGEN_NONBLOCKING;
    }

    // If we need 64 bit install update
    if (fNeedUninstallUpdate64)
    {
        hr = StrAllocFormatted(&pwzData, L"%s update /queue", pwz64Ngen);
        ExitOnFailure(hr, "failed to create uninstall update 64 command line");

        hr = WcaWriteStringToCaData(pwzData, &pwzUninstallCustomActionData); // command
        ExitOnFailure1(hr, "failed to add install command to uninstall custom action data: %ls", pwzData);

        hr = WcaWriteIntegerToCaData(COST_NGEN_NONBLOCKING, &pwzUninstallCustomActionData); // cost
        ExitOnFailure1(hr, "failed to add cost to uninstall custom action data: %ls", pwzData);

        uiCost += COST_NGEN_NONBLOCKING;
    }

    // Add to progress bar
    if ((pwzInstallCustomActionData && *pwzInstallCustomActionData) || (pwzUninstallCustomActionData && *pwzUninstallCustomActionData))
    {
        hr = WcaProgressMessage(uiCost, TRUE);
        ExitOnFailure(hr, "failed to extend progress bar for NetFxExecuteNativeImage");
    }

    // Schedule the install custom action
    if (pwzInstallCustomActionData && *pwzInstallCustomActionData)
    {
        hr = WcaSetProperty(L"NetFxExecuteNativeImageInstall", pwzInstallCustomActionData);
        ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageInstall action");

        hr = WcaSetProperty(L"NetFxExecuteNativeImageCommitInstall", pwzInstallCustomActionData);
        ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageCommitInstall action");
    }

    // Schedule the uninstall custom action
    if (pwzUninstallCustomActionData && *pwzUninstallCustomActionData)
    {
        hr = WcaSetProperty(L"NetFxExecuteNativeImageUninstall", pwzUninstallCustomActionData);
        ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageUninstall action");

        hr = WcaSetProperty(L"NetFxExecuteNativeImageCommitUninstall", pwzUninstallCustomActionData);
        ExitOnFailure(hr, "failed to schedule NetFxExecuteNativeImageCommitUninstall action");
    }


LExit:
    ReleaseStr(pwzInstallCustomActionData);
    ReleaseStr(pwzUninstallCustomActionData);
    ReleaseStr(pwzId);
    ReleaseStr(pwzData);
    ReleaseStr(pwzTemp);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzFileApp);
    ReleaseStr(pwzDirAppBase);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwz32Ngen);
    ReleaseStr(pwz64Ngen);

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/******************************************************************
 ExecNetFx - entry point for NetFx Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecNetFx(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug ExecNetFx");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    int iCost = 0;

    // initialize
    hr = WcaInitialize(hInstall, "ExecNetFx");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    // loop through all the passed in data
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzData);
        ExitOnFailure(hr, "failed to read command line from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &iCost);
        ExitOnFailure(hr, "failed to read cost from custom action data");

        hr = QuietExec(pwzData, NGEN_TIMEOUT, TRUE, TRUE);
        // If we fail here it isn't critical - keep looping through to try to act on the other assemblies on our list
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "failed to execute Ngen command (with error 0x%x): %ls, continuing anyway", hr, pwzData);
            hr = S_OK;
        }

        // Tick the progress bar along for this assembly
        hr = WcaProgressMessage(iCost, FALSE);
        ExitOnFailure1(hr, "failed to tick progress bar for command line: %ls", pwzData);
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzData);

    if (FAILED(hr))
        er = ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

