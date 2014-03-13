//-------------------------------------------------------------------------------------------------
// <copyright file="XmlConfig.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Code to configure XML files.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define XMLCONFIG_ELEMENT 0x00000001
#define XMLCONFIG_VALUE 0x00000002
#define XMLCONFIG_DOCUMENT 0x00000004
#define XMLCONFIG_CREATE 0x00000010
#define XMLCONFIG_DELETE 0x00000020
#define XMLCONFIG_INSTALL 0x00000100
#define XMLCONFIG_UNINSTALL 0x00000200
#define XMLCONFIG_PRESERVE_MODIFIED 0x00001000

enum eXmlAction
{
    xaUnknown = 0,
    xaOpenFile,
    xaOpenFilex64,
    xaWriteValue,
    xaWriteDocument,
    xaDeleteValue,
    xaCreateElement,
    xaDeleteElement,
};

enum eXmlPreserveDate
{
    xdDontPreserve = 0,
    xdPreserve
};

LPCWSTR vcsXmlConfigQuery =
    L"SELECT `XmlConfig`.`XmlConfig`, `XmlConfig`.`File`, `XmlConfig`.`ElementPath`, `XmlConfig`.`VerifyPath`, `XmlConfig`.`Name`, "
    L"`XmlConfig`.`Value`, `XmlConfig`.`Flags`, `XmlConfig`.`Component_`, `Component`.`Attributes` "
    L"FROM `XmlConfig`,`Component` WHERE `XmlConfig`.`Component_`=`Component`.`Component` ORDER BY `File`, `Sequence`";
enum eXmlConfigQuery { xfqXmlConfig = 1, xfqFile, xfqElementPath, xfqVerifyPath, xfqName, xfqValue, xfqXmlFlags, xfqComponent, xfqCompAttributes  };

struct XML_CONFIG_CHANGE
{
    WCHAR wzId[MAX_DARWIN_KEY + 1];

    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    WCHAR wzFile[MAX_PATH];
    LPWSTR pwzElementPath;
    LPWSTR pwzVerifyPath;
    WCHAR wzName[MAX_DARWIN_COLUMN];
    LPWSTR pwzValue;
    BOOL fInstalledFile;

    int iXmlFlags;
    int iCompAttributes;

    XML_CONFIG_CHANGE* pxfcAdditionalChanges;
    int cAdditionalChanges;

    XML_CONFIG_CHANGE* pxfcPrev;
    XML_CONFIG_CHANGE* pxfcNext;
};

static HRESULT FreeXmlConfigChangeList(
    __in_opt XML_CONFIG_CHANGE* pxfcList
    )
{
    HRESULT hr = S_OK;

    XML_CONFIG_CHANGE* pxfcDelete;
    while(pxfcList)
    {
        pxfcDelete = pxfcList;
        pxfcList = pxfcList->pxfcNext;

        if (pxfcDelete->pwzElementPath)
        {
            hr = MemFree(pxfcDelete->pwzElementPath);
            ExitOnFailure(hr, "failed to free xml file element path in change list item");
        }

        if (pxfcDelete->pwzVerifyPath)
        {
            hr = MemFree(pxfcDelete->pwzVerifyPath);
            ExitOnFailure(hr, "failed to free xml file verify path in change list item");
        }

        if (pxfcDelete->pwzValue)
        {
            hr = MemFree(pxfcDelete->pwzValue);
            ExitOnFailure(hr, "failed to free xml file value in change list item");
        }

        hr = MemFree(pxfcDelete);
        ExitOnFailure(hr, "failed to free xml file change list item");
    }

LExit:
    return hr;
}

static HRESULT AddXmlConfigChangeToList(
    __inout XML_CONFIG_CHANGE** ppxfcHead,
    __inout XML_CONFIG_CHANGE** ppxfcTail
    )
{
    Assert(ppxfcHead && ppxfcTail);

    HRESULT hr = S_OK;

    XML_CONFIG_CHANGE* pxfc = static_cast<XML_CONFIG_CHANGE*>(MemAlloc(sizeof(XML_CONFIG_CHANGE), TRUE));
    ExitOnNull(pxfc, hr, E_OUTOFMEMORY, "failed to allocate memory for new xml file change list element");

    // Add it to the end of the list
    if (NULL == *ppxfcHead)
    {
        *ppxfcHead = pxfc;
        *ppxfcTail = pxfc;
    }
    else
    {
        Assert(*ppxfcTail && (*ppxfcTail)->pxfcNext == NULL);
        (*ppxfcTail)->pxfcNext = pxfc;
        pxfc->pxfcPrev = *ppxfcTail;
        *ppxfcTail = pxfc;
    }

LExit:
    return hr;
}


static HRESULT ReadXmlConfigTable(
    __inout XML_CONFIG_CHANGE** ppxfcHead,
    __inout XML_CONFIG_CHANGE** ppxfcTail
    )
{
    Assert(ppxfcHead && ppxfcTail);

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR pwzData = NULL;

    // loop through all the xml configurations
    hr = WcaOpenExecuteView(vcsXmlConfigQuery, &hView);
    ExitOnFailure(hr, "failed to open view on XmlConfig table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = AddXmlConfigChangeToList(ppxfcHead, ppxfcTail);
        ExitOnFailure(hr, "failed to add xml file change to list");

        // Get record Id
        hr = WcaGetRecordString(hRec, xfqXmlConfig, &pwzData);
        ExitOnFailure(hr, "failed to get XmlConfig record Id");
        hr = StringCchCopyW((*ppxfcTail)->wzId, countof((*ppxfcTail)->wzId), pwzData);
        ExitOnFailure(hr, "failed to copy XmlConfig record Id");

        // Get component name
        hr = WcaGetRecordString(hRec, xfqComponent, &pwzData);
        ExitOnFailure1(hr, "failed to get component name for XmlConfig: %ls", (*ppxfcTail)->wzId);

        // Get the component's state
        if (0 < lstrlenW(pwzData))
        {
            hr = StringCchCopyW((*ppxfcTail)->wzComponent, countof((*ppxfcTail)->wzComponent), pwzData);
            ExitOnFailure(hr, "failed to copy component id");

            er = ::MsiGetComponentStateW(WcaGetInstallHandle(), (*ppxfcTail)->wzComponent, &(*ppxfcTail)->isInstalled, &(*ppxfcTail)->isAction);
            ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "failed to get install state for component id");
        }

        // Get the xml file
        hr = WcaGetRecordFormattedString(hRec, xfqFile, &pwzData);
        ExitOnFailure1(hr, "failed to get xml file for XmlConfig: %ls", (*ppxfcTail)->wzId);
        hr = StringCchCopyW((*ppxfcTail)->wzFile, countof((*ppxfcTail)->wzFile), pwzData);
        ExitOnFailure(hr, "failed to copy xml file path");

        // Figure out if the file is already on the machine or if it's being installed
        hr = WcaGetRecordString(hRec, xfqFile, &pwzData);
        ExitOnFailure1(hr, "failed to get xml file for XmlConfig: %ls", (*ppxfcTail)->wzId);
        if (NULL != wcsstr(pwzData, L"[!") || NULL != wcsstr(pwzData, L"[#"))
        {
            (*ppxfcTail)->fInstalledFile = TRUE;
        }

        // Get the XmlConfig table flags
        hr = WcaGetRecordInteger(hRec, xfqXmlFlags, &(*ppxfcTail)->iXmlFlags);
        ExitOnFailure1(hr, "failed to get XmlConfig flags for XmlConfig: %ls", (*ppxfcTail)->wzId);

        // Get the Element Path
        hr = WcaGetRecordFormattedString(hRec, xfqElementPath, &(*ppxfcTail)->pwzElementPath);
        ExitOnFailure1(hr, "failed to get Element Path for XmlConfig: %ls", (*ppxfcTail)->wzId);

        // Get the Verify Path
        hr = WcaGetRecordFormattedString(hRec, xfqVerifyPath, &(*ppxfcTail)->pwzVerifyPath);
        ExitOnFailure1(hr, "failed to get Verify Path for XmlConfig: %ls", (*ppxfcTail)->wzId);

        // Get the name
        hr = WcaGetRecordFormattedString(hRec, xfqName, &pwzData);
        ExitOnFailure1(hr, "failed to get Name for XmlConfig: %ls", (*ppxfcTail)->wzId);
        hr = StringCchCopyW((*ppxfcTail)->wzName, countof((*ppxfcTail)->wzName), pwzData);
        ExitOnFailure(hr, "failed to copy name of element");

        // Get the value
        hr = WcaGetRecordFormattedString(hRec, xfqValue, &pwzData);
        ExitOnFailure1(hr, "failed to get Value for XmlConfig: %ls", (*ppxfcTail)->wzId);
        hr = StrAllocString(&(*ppxfcTail)->pwzValue, pwzData, 0);
        ExitOnFailure(hr, "failed to allocate buffer for value");

        // Get the component attributes
        hr = WcaGetRecordInteger(hRec, xfqCompAttributes, &(*ppxfcTail)->iCompAttributes);
        ExitOnFailure1(hr, "failed to get component attributes for XmlConfig: %ls", (*ppxfcTail)->wzId);
    }

    // if we looped through all records all is well
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed while looping through all objects to secure");

LExit:
    ReleaseStr(pwzData);

    return hr;
}

static HRESULT ProcessChanges(
    __inout XML_CONFIG_CHANGE** ppxfcHead
    )
{
    Assert(ppxfcHead && *ppxfcHead);
    HRESULT hr = S_OK;

    XML_CONFIG_CHANGE* pxfc = NULL;
    XML_CONFIG_CHANGE* pxfcNext = NULL;
    XML_CONFIG_CHANGE* pxfcCheck = NULL;
    int cAdditionalChanges = 0;
    XML_CONFIG_CHANGE* pxfcLast = NULL;

    // If there's only one item in the list, none of this matters
    if (pxfc && !pxfc->pxfcNext)
    {
        ExitFunction();
    }

    // Loop through the list
    pxfc = *ppxfcHead;
    while (pxfc)
    {
        // Keep track of where our next spot will be since our current node may be moved
        pxfcNext = pxfc->pxfcNext;

        // With each node, check to see if it's element path matches the Id of some other node in the list
        pxfcCheck = *ppxfcHead;
        while (pxfcCheck)
        {
            if (0 == lstrcmpW(pxfc->pwzElementPath, pxfcCheck->wzId) && 0 == pxfc->iXmlFlags
                && XMLCONFIG_CREATE & pxfcCheck->iXmlFlags && XMLCONFIG_ELEMENT & pxfcCheck->iXmlFlags)
            {
                // We found a match.  First, take it out of the current list
                if (pxfc->pxfcPrev)
                {
                    pxfc->pxfcPrev->pxfcNext = pxfc->pxfcNext;
                }
                else // it was the head.  Update the head
                {
                    *ppxfcHead = pxfc->pxfcNext;
                }

                if (pxfc->pxfcNext)
                {
                    pxfc->pxfcNext->pxfcPrev = pxfc->pxfcPrev;
                }

                pxfc->pxfcNext = NULL;
                pxfc->pxfcPrev = NULL;

                // Now, add this node to the end of the matched node's additional changes list
                if (!pxfcCheck->pxfcAdditionalChanges)
                {
                    pxfcCheck->pxfcAdditionalChanges = pxfc;
                    pxfcCheck->cAdditionalChanges = 1;
                }
                else
                {
                    pxfcLast = pxfcCheck->pxfcAdditionalChanges;
                    cAdditionalChanges = 1;
                    while (pxfcLast->pxfcNext)
                    {
                        pxfcLast = pxfcLast->pxfcNext;
                        ++cAdditionalChanges;
                    }
                    pxfcLast->pxfcNext = pxfc;
                    pxfc->pxfcPrev = pxfcLast;
                    pxfcCheck->cAdditionalChanges = ++cAdditionalChanges;
                }
            }

            pxfcCheck = pxfcCheck->pxfcNext;
        }

        pxfc = pxfcNext;
    }

LExit:

    return hr;
}


static HRESULT BeginChangeFile(
    __in LPCWSTR pwzFile,
    __in int iCompAttributes,
    __inout LPWSTR* ppwzCustomActionData
    )
{
    Assert(pwzFile && *pwzFile && ppwzCustomActionData);

    HRESULT hr = S_OK;
    BOOL fIs64Bit = iCompAttributes & msidbComponentAttributes64bit;

    LPBYTE pbData = NULL;
    DWORD cbData = 0;

    LPWSTR pwzRollbackCustomActionData = NULL;

    if (fIs64Bit)
    {
        hr = WcaWriteIntegerToCaData((int)xaOpenFilex64, ppwzCustomActionData);
        ExitOnFailure(hr, "failed to write 64-bit file indicator to custom action data");
    }
    else
    {
        hr = WcaWriteIntegerToCaData((int)xaOpenFile, ppwzCustomActionData);
        ExitOnFailure(hr, "failed to write file indicator to custom action data");
    }

    hr = WcaWriteStringToCaData(pwzFile, ppwzCustomActionData);
    ExitOnFailure1(hr, "failed to write file to custom action data: %ls", pwzFile);

    // If the file already exits, then we have to put it back the way it was on failure
    if (FileExistsEx(pwzFile, NULL))
    {
        hr = FileRead(&pbData, &cbData, pwzFile);
        ExitOnFailure1(hr, "failed to read file: %ls", pwzFile);

        // Set up the rollback for this file
        hr = WcaWriteIntegerToCaData((int)fIs64Bit, &pwzRollbackCustomActionData);
        ExitOnFailure(hr, "failed to write component bitness to rollback custom action data");

        hr = WcaWriteStringToCaData(pwzFile, &pwzRollbackCustomActionData);
        ExitOnFailure1(hr, "failed to write file name to rollback custom action data: %ls", pwzFile);

        hr = WcaWriteStreamToCaData(pbData, cbData, &pwzRollbackCustomActionData);
        ExitOnFailure(hr, "failed to write file contents to rollback custom action data.");

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"ExecXmlConfigRollback"), pwzRollbackCustomActionData, COST_XMLFILE);
        ExitOnFailure1(hr, "failed to schedule ExecXmlConfigRollback for file: %ls", pwzFile);

        ReleaseStr(pwzRollbackCustomActionData);
    }
LExit:
    ReleaseMem(pbData);

    return hr;
}


static HRESULT WriteChangeData(
    __in XML_CONFIG_CHANGE* pxfc,
    __in eXmlAction action,
    __inout LPWSTR* ppwzCustomActionData
    )
{
    Assert(pxfc && ppwzCustomActionData);

    HRESULT hr = S_OK;
    XML_CONFIG_CHANGE* pxfcAdditionalChanges = NULL;

    hr = WcaWriteStringToCaData(pxfc->pwzElementPath, ppwzCustomActionData);
    ExitOnFailure1(hr, "failed to write ElementPath to custom action data: %ls", pxfc->pwzElementPath);

    hr = WcaWriteStringToCaData(pxfc->pwzVerifyPath, ppwzCustomActionData);
    ExitOnFailure1(hr, "failed to write VerifyPath to custom action data: %ls", pxfc->pwzVerifyPath);

    hr = WcaWriteStringToCaData(pxfc->wzName, ppwzCustomActionData);
    ExitOnFailure1(hr, "failed to write Name to custom action data: %ls", pxfc->wzName);

    hr = WcaWriteStringToCaData(pxfc->pwzValue, ppwzCustomActionData);
    ExitOnFailure1(hr, "failed to write Value to custom action data: %ls", pxfc->pwzValue);

    if (pxfc->iXmlFlags & XMLCONFIG_CREATE && pxfc->iXmlFlags & XMLCONFIG_ELEMENT && xaCreateElement == action && pxfc->pxfcAdditionalChanges)
    {
        hr = WcaWriteIntegerToCaData(pxfc->cAdditionalChanges, ppwzCustomActionData);
        ExitOnFailure(hr, "failed to write additional changes value to custom action data");

        pxfcAdditionalChanges = pxfc->pxfcAdditionalChanges;
        while (pxfcAdditionalChanges)
        {
            Assert((0 == lstrcmpW(pxfcAdditionalChanges->wzComponent, pxfc->wzComponent)) && 0 == pxfcAdditionalChanges->iXmlFlags && (0 == lstrcmpW(pxfcAdditionalChanges->wzFile, pxfc->wzFile)));

            hr = WcaWriteStringToCaData(pxfcAdditionalChanges->wzName, ppwzCustomActionData);
            ExitOnFailure1(hr, "failed to write Name to custom action data: %ls", pxfc->wzName);

            hr = WcaWriteStringToCaData(pxfcAdditionalChanges->pwzValue, ppwzCustomActionData);
            ExitOnFailure1(hr, "failed to write Value to custom action data: %ls", pxfc->pwzValue);

            pxfcAdditionalChanges = pxfcAdditionalChanges->pxfcNext;
        }
    }
    else
    {
        hr = WcaWriteIntegerToCaData(0, ppwzCustomActionData);
        ExitOnFailure(hr, "failed to write additional changes value to custom action data");
    }

LExit:
    return hr;
}


/******************************************************************
 SchedXmlConfig - entry point for XmlConfig Custom Action

********************************************************************/
extern "C" UINT __stdcall SchedXmlConfig(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug SchedXmlConfig");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzCurrentFile = NULL;
    BOOL fCurrentFileChanged = FALSE;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    XML_CONFIG_CHANGE* pxfcHead = NULL;
    XML_CONFIG_CHANGE* pxfcTail = NULL; // TODO: do we need this any more?
    XML_CONFIG_CHANGE* pxfc = NULL;

    eXmlAction xa = xaUnknown;
    eXmlPreserveDate xd;

    LPWSTR pwzCustomActionData = NULL;

    DWORD cFiles = 0;

    // initialize
    hr = WcaInitialize(hInstall, "SchedXmlConfig");
    ExitOnFailure(hr, "failed to initialize");

    hr = ReadXmlConfigTable(&pxfcHead, &pxfcTail);
    MessageExitOnFailure(hr, msierrXmlConfigFailedRead, "failed to read XmlConfig table");

    hr = ProcessChanges(&pxfcHead);
    ExitOnFailure(hr, "failed to process XmlConfig changes");

    // loop through all the xml configurations
    for (pxfc = pxfcHead; pxfc; pxfc = pxfc->pxfcNext)
    {
        // If this is a different file, or the first file...
        if (NULL == pwzCurrentFile || 0 != lstrcmpW(pwzCurrentFile, pxfc->wzFile))
        {
            // Remember the file we're currently working on
            hr = StrAllocString(&pwzCurrentFile, pxfc->wzFile, 0);
            ExitOnFailure(hr, "failed to copy file name");

            fCurrentFileChanged = TRUE;
        }

        //
        // Figure out what action to take
        //
        xa = xaUnknown;

        // If it's being installed or reinstalled or uninstalled and that matches
        // what we are doing then calculate the right action.
        if ((XMLCONFIG_INSTALL & pxfc->iXmlFlags && (WcaIsInstalling(pxfc->isInstalled, pxfc->isAction) || WcaIsReInstalling(pxfc->isInstalled, pxfc->isAction))) ||
            (XMLCONFIG_UNINSTALL & pxfc->iXmlFlags && WcaIsUninstalling(pxfc->isInstalled, pxfc->isAction)))
        {
            if (XMLCONFIG_CREATE & pxfc->iXmlFlags && XMLCONFIG_ELEMENT & pxfc->iXmlFlags)
            {
                xa = xaCreateElement;
            }
            else if (XMLCONFIG_DELETE & pxfc->iXmlFlags && XMLCONFIG_ELEMENT & pxfc->iXmlFlags)
            {
                xa = xaDeleteElement;
            }
            else if (XMLCONFIG_DELETE & pxfc->iXmlFlags && XMLCONFIG_VALUE & pxfc->iXmlFlags)
            {
                xa = xaDeleteValue;
            }
            else if (XMLCONFIG_CREATE & pxfc->iXmlFlags && XMLCONFIG_VALUE & pxfc->iXmlFlags)
            {
                xa = xaWriteValue;
            }
            else if (XMLCONFIG_CREATE & pxfc->iXmlFlags && XMLCONFIG_DOCUMENT & pxfc->iXmlFlags)
            {
                xa = xaWriteDocument;
            }
            else if (XMLCONFIG_DELETE & pxfc->iXmlFlags && XMLCONFIG_DOCUMENT & pxfc->iXmlFlags)
            {
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid flag configuration.  Cannot delete a fragment node.");
            }
        }

        if (XMLCONFIG_PRESERVE_MODIFIED & pxfc->iXmlFlags)
        {
            xd = xdPreserve;
        }
        else
        {
            xd= xdDontPreserve;
        }

        if (xaUnknown != xa)
        {
            if (fCurrentFileChanged)
            {
                hr = BeginChangeFile(pwzCurrentFile, pxfc->iCompAttributes, &pwzCustomActionData);
                ExitOnFailure1(hr, "failed to begin file change for file: %ls", pwzCurrentFile);

                fCurrentFileChanged = FALSE;
                ++cFiles;
            }

            hr = WcaWriteIntegerToCaData((int)xa, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write action indicator custom action data");

            hr = WcaWriteIntegerToCaData((int)xd, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write Preserve Date indicator to custom action data");

            hr = WriteChangeData(pxfc, xa, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write change data");
        }
    }

    // If we looped through all records all is well
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed while looping through all objects to secure");

    // Schedule the custom action and add to progress bar
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        Assert(0 < cFiles);

        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"ExecXmlConfig"), pwzCustomActionData, cFiles * COST_XMLFILE);
        ExitOnFailure(hr, "failed to schedule ExecXmlConfig action");
    }

LExit:
    ReleaseStr(pwzCurrentFile);
    ReleaseStr(pwzCustomActionData);

    FreeXmlConfigChangeList(pxfcHead);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/******************************************************************
 ExecXmlConfig - entry point for XmlConfig Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecXmlConfig(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ExecXmlConfig");
    HRESULT hr = S_OK;
    HRESULT hrOpenFailure = S_OK;
    UINT er = ERROR_SUCCESS;

    BOOL fIsWow64Process = FALSE;
    BOOL fIsFSRedirectDisabled = FALSE;
    BOOL fPreserveDate = FALSE;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzData = NULL;
    LPWSTR pwzFile = NULL;
    LPWSTR pwzElementPath = NULL;
    LPWSTR pwzVerifyPath = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzValue = NULL;
    LPWSTR pwz = NULL;
    int cAdditionalChanges = 0;

    IXMLDOMDocument* pixd = NULL;
    IXMLDOMNode* pixn = NULL;
    IXMLDOMNode* pixnVerify = NULL;
    IXMLDOMNode* pixnNewNode = NULL;
    IXMLDOMNode* pixnRemovedChild = NULL;

    IXMLDOMDocument* pixdNew = NULL;
    IXMLDOMElement* pixeNew = NULL;

    FILETIME ft;

    int id = IDRETRY;

    eXmlAction xa;
    eXmlPreserveDate xd;

    // initialize
    hr = WcaInitialize(hInstall, "ExecXmlConfig");
    ExitOnFailure(hr, "failed to initialize");

    hr = XmlInitialize();
    ExitOnFailure(hr, "failed to initialize xml utilities");

    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    hr = WcaReadIntegerFromCaData(&pwz, (int*) &xa);
    ExitOnFailure(hr, "failed to process CustomActionData");

    // Initialize the Wow64 API - store the result in fWow64APIPresent
    // If it fails, this doesn't warrant an error yet, because we only need the Wow64 API in some cases
    WcaInitializeWow64();
    fIsWow64Process = WcaIsWow64Process();

    if (xaOpenFile != xa && xaOpenFilex64 != xa)
    {
        ExitOnFailure(hr = E_INVALIDARG, "invalid custom action data");
    }

    // loop through all the passed in data
    while (pwz && *pwz)
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzFile);
        ExitOnFailure(hr, "failed to read file name from custom action data");

        // Default to not preserve date, preserve it if any modifications require us to
        fPreserveDate = FALSE;

        // Open the file
        ReleaseNullObject(pixd);

        if (xaOpenFilex64 == xa)
        {
            if (!fIsWow64Process)
            {
                hr = E_NOTIMPL;
                ExitOnFailure(hr, "Custom action was told to act on a 64-bit component, but the custom action process is not running in WOW.");
            }

            hr = WcaDisableWow64FSRedirection();
            ExitOnFailure(hr, "Custom action was told to act on a 64-bit component, but was unable to disable filesystem redirection through the Wow64 API.");

            fIsFSRedirectDisabled = TRUE;
        }

        hr = XmlLoadDocumentFromFileEx(pwzFile, XML_LOAD_PRESERVE_WHITESPACE, &pixd);
        if (FAILED(hr))
        {
            // Ignore the return code for now.  If they try to add something, we'll fail the install.  If all they do is remove stuff then it doesn't matter.
            hrOpenFailure = hr;
            hr = S_OK;
        }
        else
        {
            hrOpenFailure = S_OK;
        }

        WcaLog(LOGMSG_VERBOSE, "Configuring Xml File: %ls", pwzFile);

        while (pwz && *pwz)
        {
            // If we skip past an element that has additional changes we need to strip them off the stream before
            // moving on to the next element. Do that now and then restart the outer loop.
            if (cAdditionalChanges > 0)
            {
                while (cAdditionalChanges > 0)
                {
                    hr = WcaReadStringFromCaData(&pwz, &pwzName);
                    ExitOnFailure(hr, "failed to process CustomActionData");
                    hr = WcaReadStringFromCaData(&pwz, &pwzValue);
                    ExitOnFailure(hr, "failed to process CustomActionData");

                    cAdditionalChanges--;
                }
                continue;
            }

            hr = WcaReadIntegerFromCaData(&pwz, (int*) &xa);
            ExitOnFailure(hr, "failed to process CustomActionData");

            // Break if we need to move on to a different file
            if (xaOpenFile == xa || xaOpenFilex64 == xa)
            {
                break;
            }

            hr = WcaReadIntegerFromCaData(&pwz, (int*) &xd);
            ExitOnFailure(hr, "failed to process CustomActionData");

            if (xdPreserve == xd)
            {
                fPreserveDate = TRUE;
            }

            // Get path, name, and value to be written
            hr = WcaReadStringFromCaData(&pwz, &pwzElementPath);
            ExitOnFailure(hr, "failed to process CustomActionData");
            hr = WcaReadStringFromCaData(&pwz, &pwzVerifyPath);
            ExitOnFailure(hr, "failed to process CustomActionData");
            hr = WcaReadStringFromCaData(&pwz, &pwzName);
            ExitOnFailure(hr, "failed to process CustomActionData");
            hr = WcaReadStringFromCaData(&pwz, &pwzValue);
            ExitOnFailure(hr, "failed to process CustomActionData");
            hr = WcaReadIntegerFromCaData(&pwz, &cAdditionalChanges);
            ExitOnFailure(hr, "failed to process CustomActionData");

            // If we failed to open the file and we're adding something to the file, we've got a problem.  Otherwise, just continue on since the file's already gone.
            if (FAILED(hrOpenFailure))
            {
                if (xaCreateElement == xa || xaWriteValue == xa || xaWriteDocument == xa)
                {
                    MessageExitOnFailure1(hr = hrOpenFailure, msierrXmlConfigFailedOpen, "failed to load XML file: %ls", pwzFile);
                }
                else
                {
                    continue;
                }
            }

            // Select the node we're about to modify
            ReleaseNullObject(pixn);

            hr = XmlSelectSingleNode(pixd, pwzElementPath, &pixn);

            // If we failed to find the node that we are going to add to, we've got a problem. Otherwise, just continue since the node's already gone.
            if (S_FALSE == hr)
            {
                if (xaCreateElement == xa || xaWriteValue == xa || xaWriteDocument == xa)
                {
                    hr = HRESULT_FROM_WIN32(ERROR_OBJECT_NOT_FOUND);
                }
                else
                {
                    hr = S_OK;
                    continue;
                }
            }

            MessageExitOnFailure2(hr, msierrXmlConfigFailedSelect, "failed to find node: %ls in XML file: %ls", pwzElementPath, pwzFile);

            // Make the modification
            switch (xa)
            {
            case xaWriteValue:
                if (pwzName && *pwzName)
                {
                    // We're setting an attribute
                    hr = XmlSetAttribute(pixn, pwzName, pwzValue);
                    ExitOnFailure2(hr, "failed to set attribute: %ls to value %ls", pwzName, pwzValue);
                }
                else
                {
                    // We're setting the text of the node
                    hr = XmlSetText(pixn, pwzValue);
                    ExitOnFailure2(hr, "failed to set text to: %ls for element %ls.  Make sure that XPath points to an element.", pwzValue, pwzElementPath);
                }
                break;
            case xaWriteDocument:
                if (NULL != pwzVerifyPath && 0 != pwzVerifyPath[0])
                {
                    hr = XmlSelectSingleNode(pixn, pwzVerifyPath, &pixnVerify);
                    if (S_OK == hr)
                    {
                        // We found the verify path which means we have no further work to do
                        continue;
                    }
                    ExitOnFailure1(hr, "failed to query verify path: %ls", pwzVerifyPath);
                }

                hr = XmlLoadDocumentEx(pwzValue, XML_LOAD_PRESERVE_WHITESPACE, &pixdNew);
                ExitOnFailure(hr, "Failed to load value as document.");

                hr = pixdNew->get_documentElement(&pixeNew);
                ExitOnFailure(hr, "Failed to get document element.");

                hr = pixn->appendChild(pixeNew, NULL);
                ExitOnFailure(hr, "Failed to append document element on to parent element.");

                ReleaseNullObject(pixeNew);
                ReleaseNullObject(pixdNew);
                break;

            case xaCreateElement:
                if (NULL != pwzVerifyPath && 0 != pwzVerifyPath[0])
                {
                    hr = XmlSelectSingleNode(pixn, pwzVerifyPath, &pixnVerify);
                    if (S_OK == hr)
                    {
                        // We found the verify path which means we have no further work to do
                        continue;
                    }
                    ExitOnFailure1(hr, "failed to query verify path: %ls", pwzVerifyPath);
                }

                hr = XmlCreateChild(pixn, pwzName, &pixnNewNode);
                ExitOnFailure1(hr, "failed to create child element: %ls", pwzName);

                if (pwzValue && *pwzValue)
                {
                    hr = XmlSetText(pixnNewNode, pwzValue);
                    ExitOnFailure2(hr, "failed to set text to: %ls for node: %ls", pwzValue, pwzName);
                }

                while (cAdditionalChanges > 0)
                {
                    hr = WcaReadStringFromCaData(&pwz, &pwzName);
                    ExitOnFailure(hr, "failed to process CustomActionData");
                    hr = WcaReadStringFromCaData(&pwz, &pwzValue);
                    ExitOnFailure(hr, "failed to process CustomActionData");

                    // Set the additional attribute
                    hr = XmlSetAttribute(pixnNewNode, pwzName, pwzValue);
                    ExitOnFailure2(hr, "failed to set attribute: %ls to value %ls", pwzName, pwzValue);

                    cAdditionalChanges--;
                }

                ReleaseNullObject(pixnNewNode);
                break;
            case xaDeleteValue:
                if (pwzName && *pwzName)
                {
                    // Delete the attribute
                    hr = XmlRemoveAttribute(pixn, pwzName);
                    ExitOnFailure1(hr, "failed to remove attribute: %ls", pwzName);
                }
                else
                {
                    // Clear the text value for the node
                    hr = XmlSetText(pixn, L"");
                    ExitOnFailure(hr, "failed to clear text value");
                }
                break;
            case xaDeleteElement:
                if (NULL != pwzVerifyPath && 0 != pwzVerifyPath[0])
                {
                    hr = XmlSelectSingleNode(pixn, pwzVerifyPath, &pixnVerify);
                    if (S_OK == hr)
                    {
                        hr = pixn->removeChild(pixnVerify, &pixnRemovedChild);
                        ExitOnFailure(hr, "failed to remove created child element");

                        ReleaseNullObject(pixnRemovedChild);
                    }
                    else
                    {
                        WcaLog(LOGMSG_VERBOSE, "Failed to select path %ls for deleting.  Skipping...", pwzVerifyPath);
                        hr = S_OK;
                    }
                }
                else
                {
                    // TODO: This requires a VerifyPath to delete an element.  Should we support not having one?
                    WcaLog(LOGMSG_VERBOSE, "No VerifyPath specified for delete element of ID: %ls", pwzElementPath);
                }
                break;
            default:
                ExitOnFailure(hr = E_UNEXPECTED, "Invalid modification specified in custom action data");
                break;
            }
        }


        // Now that we've made all of the changes to this file, save it and move on to the next
        if (S_OK == hrOpenFailure)
        {
            if (fPreserveDate)
            {
                hr = FileGetTime(pwzFile, NULL, NULL, &ft);
                ExitOnFailure1(hr, "failed to get modified time of file : %ls", pwzFile);
            }

            int iSaveAttempt = 0;

            do
            {
                hr = XmlSaveDocument(pixd, pwzFile);
                if (FAILED(hr))
                {
                    id = WcaErrorMessage(msierrXmlConfigFailedSave, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 1, pwzFile);
                    switch (id)
                    {
                    case IDABORT:
                        ExitOnFailure1(hr, "Failed to save changes to XML file: %ls", pwzFile);
                    case IDRETRY:
                        hr = S_FALSE;   // hit me, baby, one more time
                        break;
                    case IDIGNORE:
                        hr = S_OK;  // pretend everything is okay and bail
                        break;
                    case 0: // No UI case, MsiProcessMessage returns 0
                        if (STIERR_SHARING_VIOLATION == hr)
                        {
                            // Only in case of sharing violation do we retry 30 times, once a second.
                            if (iSaveAttempt < 30)
                            {
                                hr = S_FALSE;
                                ++iSaveAttempt;
                                WcaLog(LOGMSG_VERBOSE, "Unable to save changes to XML file: %ls, retry attempt: %x", pwzFile, iSaveAttempt);
                                Sleep(1000);
                            }
                            else
                            {
                                ExitOnFailure1(hr, "Failed to save changes to XML file: %ls", pwzFile);
                            }
                        }
                        break;
                    default: // Unknown error
                        ExitOnFailure1(hr, "Failed to save changes to XML file: %ls", pwzFile);
                    }
                }
            } while (S_FALSE == hr);

            if (fPreserveDate)
            {
                hr = FileSetTime(pwzFile, NULL, NULL, &ft);
                ExitOnFailure1(hr, "failed to set modified time of file : %ls", pwzFile);
            }

            if (fIsFSRedirectDisabled)
            {
                fIsFSRedirectDisabled = FALSE;
                WcaRevertWow64FSRedirection();
            }
        }
    }

LExit:
    // Make sure we revert FS Redirection if necessary before exiting
    if (fIsFSRedirectDisabled)
    {
        fIsFSRedirectDisabled = FALSE;
        WcaRevertWow64FSRedirection();
    }
    WcaFinalizeWow64();

    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzData);
    ReleaseStr(pwzFile);
    ReleaseStr(pwzElementPath);
    ReleaseStr(pwzVerifyPath);
    ReleaseStr(pwzName);
    ReleaseStr(pwzValue);

    ReleaseObject(pixeNew);
    ReleaseObject(pixdNew);

    ReleaseObject(pixn);
    ReleaseObject(pixd);
    ReleaseObject(pixnNewNode);
    ReleaseObject(pixnRemovedChild);

    XmlUninitialize();

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


/******************************************************************
 ExecXmlConfigRollback - entry point for XmlConfig rollback Custom Action

*******************************************************************/
extern "C" UINT __stdcall ExecXmlConfigRollback(
    __in MSIHANDLE hInstall
    )
{
//    AssertSz(FALSE, "debug ExecXmlConfigRollback");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    int iIs64Bit;
    BOOL fIs64Bit = FALSE;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzFileName = NULL;
    LPBYTE pbData = NULL;
    DWORD_PTR cbData = 0;
    DWORD cbDataWritten = 0;

    FILETIME ft;

    HANDLE hFile = INVALID_HANDLE_VALUE;

    // initialize
    hr = WcaInitialize(hInstall, "ExecXmlConfigRollback");
    ExitOnFailure(hr, "failed to initialize");


    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    pwz = pwzCustomActionData;

    hr = WcaReadIntegerFromCaData(&pwz, &iIs64Bit);
    ExitOnFailure(hr, "failed to read component bitness from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzFileName);
    ExitOnFailure(hr, "failed to read file name from custom action data");

    hr = WcaReadStreamFromCaData(&pwz, &pbData, &cbData);
    ExitOnFailure(hr, "failed to read file contents from custom action data");

    fIs64Bit = (BOOL)iIs64Bit;

    if (fIs64Bit)
    {
        hr = WcaInitializeWow64();
        if (S_FALSE == hr)
        {
            hr = TYPE_E_DLLFUNCTIONNOTFOUND;
        }
        ExitOnFailure(hr, "failed to initialize Wow64 API");

        if (!WcaIsWow64Process())
        {
            hr = E_NOTIMPL;
            ExitOnFailure(hr, "Custom action was told to rollback a 64-bit component, but the Wow64 API is unavailable.");
        }

        hr = WcaDisableWow64FSRedirection();
        ExitOnFailure(hr, "Custom action was told to rollback a 64-bit component, but was unable to Disable Filesystem Redirection through the Wow64 API.");
    }

    hr = FileGetTime(pwzFileName, NULL, NULL, &ft);
    ExitOnFailure1(hr, "Failed to get modified date of file %ls.", pwzFileName);

    // Open the file
    hFile = ::CreateFileW(pwzFileName, GENERIC_WRITE, NULL, NULL, TRUNCATE_EXISTING, NULL, NULL);
    ExitOnInvalidHandleWithLastError1(hFile, hr, "failed to open file: %ls", pwzFileName);

    // Write out the old data
    if (!::WriteFile(hFile, pbData, (DWORD)cbData, &cbDataWritten, NULL))
    {
        ExitOnLastError1(hr, "failed to write to file: %ls", pwzFileName);
    }

    Assert(cbData == cbDataWritten);

    ReleaseFile(hFile);

    hr = FileSetTime(pwzFileName, NULL, NULL, &ft);
    ExitOnFailure1(hr, "Failed to set modified date of file %ls.", pwzFileName);

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(pwzFileName);

    ReleaseFile(hFile);

    if (fIs64Bit)
    {
        WcaRevertWow64FSRedirection();
        WcaFinalizeWow64();
    }

    ReleaseMem(pbData);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}

