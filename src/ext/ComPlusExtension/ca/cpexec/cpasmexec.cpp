//-------------------------------------------------------------------------------------------------
// <copyright file="cpasmexec.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    COM+ assembly functions for CustomActions
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"


// GAC related declarations

typedef struct _FUSION_INSTALL_REFERENCE_
{
    DWORD cbSize;
    DWORD dwFlags;
    GUID guidScheme;
    LPCWSTR szIdentifier;
    LPCWSTR szNonCannonicalData;
} FUSION_INSTALL_REFERENCE;

typedef struct _FUSION_INSTALL_REFERENCE_ *LPFUSION_INSTALL_REFERENCE;

typedef const FUSION_INSTALL_REFERENCE *LPCFUSION_INSTALL_REFERENCE;

typedef struct _ASSEMBLY_INFO
{
    ULONG cbAssemblyInfo;
    DWORD dwAssemblyFlags;
    ULARGE_INTEGER uliAssemblySizeInKB;
    LPWSTR pszCurrentAssemblyPathBuf;
    ULONG cchBuf;
} ASSEMBLY_INFO;

typedef interface IAssemblyCacheItem IAssemblyCacheItem;

MIDL_INTERFACE("e707dcde-d1cd-11d2-bab9-00c04f8eceae")
IAssemblyCache : public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE UninstallAssembly( 
        /* [in] */ DWORD dwFlags,
        /* [in] */ LPCWSTR pszAssemblyName,
        /* [in] */ LPCFUSION_INSTALL_REFERENCE pRefData,
        /* [optional][out] */ ULONG *pulDisposition) = 0;

    virtual HRESULT STDMETHODCALLTYPE QueryAssemblyInfo( 
        /* [in] */ DWORD dwFlags,
        /* [in] */ LPCWSTR pszAssemblyName,
        /* [out][in] */ ASSEMBLY_INFO *pAsmInfo) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateAssemblyCacheItem( 
        /* [in] */ DWORD dwFlags,
        /* [in] */ PVOID pvReserved,
        /* [out] */ IAssemblyCacheItem **ppAsmItem,
        /* [optional][in] */ LPCWSTR pszAssemblyName) = 0;

    virtual HRESULT STDMETHODCALLTYPE CreateAssemblyScavenger( 
        /* [out] */ IUnknown **ppUnkReserved) = 0;

    virtual HRESULT STDMETHODCALLTYPE InstallAssembly( 
        /* [in] */ DWORD dwFlags,
        /* [in] */ LPCWSTR pszManifestFilePath,
        /* [in] */ LPCFUSION_INSTALL_REFERENCE pRefData) = 0;
};

typedef HRESULT (__stdcall *LoadLibraryShimFunc)(LPCWSTR szDllName, LPCWSTR szVersion, LPVOID pvReserved, HMODULE *phModDll);
typedef HRESULT (__stdcall *CreateAssemblyCacheFunc)(IAssemblyCache **ppAsmCache, DWORD dwReserved);


// RegistrationHelper related declarations

static const GUID CLSID_RegistrationHelper =
    { 0x89a86e7b, 0xc229, 0x4008, { 0x9b, 0xaa, 0x2f, 0x5c, 0x84, 0x11, 0xd7, 0xe0 } };

enum eInstallationFlags {
    ifConfigureComponentsOnly = 16,
    ifFindOrCreateTargetApplication = 4,
    ifExpectExistingTypeLib = 1
};


// private constants

enum eAssemblyAttributes
{
    aaEventClass     = (1 << 0),
    aaDotNetAssembly = (1 << 1),
    aaPathFromGAC    = (1 << 2),
    aaRunInCommit    = (1 << 3)
};


// private structs

struct CPI_ROLE_ASSIGNMENT
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzRoleName[MAX_DARWIN_COLUMN + 1];

    CPI_ROLE_ASSIGNMENT* pNext;
};

struct CPI_METHOD
{
    WCHAR wzIndex[11 + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];

    CPI_PROPERTY* pPropertyList;
    CPI_ROLE_ASSIGNMENT* pRoleAssignmentList;

    CPI_METHOD* pNext;
};

struct CPI_INTERFACE
{
    WCHAR wzIID[CPI_MAX_GUID + 1];

    CPI_PROPERTY* pPropertyList;
    CPI_ROLE_ASSIGNMENT* pRoleAssignmentList;
    CPI_METHOD* pMethodList;

    CPI_INTERFACE* pNext;
};

struct CPI_COMPONENT
{
    WCHAR wzCLSID[CPI_MAX_GUID + 1];

    CPI_PROPERTY* pPropertyList;
    CPI_ROLE_ASSIGNMENT* pRoleAssignmentList;
    CPI_INTERFACE* pInterfaceList;

    CPI_COMPONENT* pNext;
};

struct CPI_ASSEMBLY_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzAssemblyName;
    LPWSTR pwzDllPath;
    LPWSTR pwzTlbPath;
    LPWSTR pwzPSDllPath;
    LPWSTR pwzAppID;
    LPWSTR pwzPartID;
    int iAttributes;
    CPI_COMPONENT* pCompList;
};

struct CPI_ROLE_ASSIGNMENTS_ATTRIBUTES
{
    int iActionType;
    int iActionCost;
    LPWSTR pwzKey;
    LPWSTR pwzAppID;
    LPWSTR pwzPartID;
    int iRoleCount;
    CPI_COMPONENT* pCompList;
};


// prototypes for private helper functions

static HRESULT RegisterAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static HRESULT UnregisterAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static void InitAssemblyExec();
static void UninitAssemblyExec();
static HRESULT GetRegistrationHelper(
    IDispatch** ppiRegHlp
    );
static HRESULT GetAssemblyCacheObject(
    IAssemblyCache** ppAssemblyCache
    );
static HRESULT GetAssemblyPathFromGAC(
    LPCWSTR pwzAssemblyName,
    LPWSTR* ppwzAssemblyPath
    );
static HRESULT RegisterDotNetAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static HRESULT RegisterNativeAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static HRESULT UnregisterDotNetAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static HRESULT RemoveComponents(
    ICatalogCollection* piCompColl,
    CPI_COMPONENT* pCompList
    );
static HRESULT ReadAssemblyAttributes(
    LPWSTR* ppwzData,
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static void FreeAssemblyAttributes(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    );
static HRESULT ReadRoleAssignmentsAttributes(
    LPWSTR* ppwzData,
    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES* pAttrs
    );
static void FreeRoleAssignmentsAttributes(
    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES* pAttrs
    );
static HRESULT ConfigureComponents(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    CPI_COMPONENT* pCompList,
    BOOL fCreate,
    BOOL fProgress
    );
static HRESULT ConfigureInterfaces(
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    CPI_INTERFACE* pIntfList,
    BOOL fCreate
    );
static HRESULT ConfigureMethods(
    ICatalogCollection* piIntfColl,
    ICatalogObject* piIntfObj,
    CPI_METHOD* pMethList,
    BOOL fCreate
    );
static HRESULT ConfigureRoleAssignments(
    LPCWSTR pwzCollName,
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    CPI_ROLE_ASSIGNMENT* pRoleList,
    BOOL fCreate
    );
static HRESULT ReadComponentList(
    LPWSTR* ppwzData,
    CPI_COMPONENT** ppCompList
    );
static HRESULT ReadInterfaceList(
    LPWSTR* ppwzData,
    CPI_INTERFACE** ppIntfList
    );
static HRESULT ReadMethodList(
    LPWSTR* ppwzData,
    CPI_METHOD** ppMethList
    );
static HRESULT ReadRoleAssignmentList(
    LPWSTR* ppwzData,
    CPI_ROLE_ASSIGNMENT** ppRoleList
    );
static void FreeComponentList(
    CPI_COMPONENT* pList
    );
static void FreeInterfaceList(
    CPI_INTERFACE* pList
    );
static void FreeMethodList(
    CPI_METHOD* pList
    );
static void FreeRoleAssignmentList(
    CPI_ROLE_ASSIGNMENT* pList
    );


// variables

static IDispatch* gpiRegHlp;
static IAssemblyCache* gpAssemblyCache;
static HMODULE ghMscoree;
static HMODULE ghFusion;


// function definitions

HRESULT CpiConfigureAssemblies(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_ASSEMBLY_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // initialize
    InitAssemblyExec();

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadAssemblyAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read assembly attributes");

        // write key to rollback file
        hr = CpiWriteKeyToRollbackFile(hRollbackFile, attrs.pwzKey);
        ExitOnFailure(hr, "Failed to write key to rollback file");

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = RegisterAssembly(&attrs);
            ExitOnFailure1(hr, "Failed to register assembly, key: %S", attrs.pwzKey);
            break;
        case atRemove:
            hr = UnregisterAssembly(&attrs);
            ExitOnFailure1(hr, "Failed to unregister assembly, key: %S", attrs.pwzKey);
            break;
        default:
            hr = S_OK;
            break;
        }

        if (S_FALSE == hr)
            ExitFunction(); // aborted by user

        // write completion status to rollback file
        hr = CpiWriteIntegerToRollbackFile(hRollbackFile, 1);
        ExitOnFailure(hr, "Failed to write completion status to rollback file");

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeAssemblyAttributes(&attrs);

    // uninitialize
    UninitAssemblyExec();

    return hr;
}

HRESULT CpiRollbackConfigureAssemblies(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_ASSEMBLY_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // initialize
    InitAssemblyExec();

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadAssemblyAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read assembly attributes");

        // rollback status
        hr = CpiFindRollbackStatus(pRollbackDataList, attrs.pwzKey, &iRollbackStatus);

        if (S_FALSE == hr)
            continue; // not found, nothing to rollback

        // action
        switch (attrs.iActionType)
        {
        case atCreate:
            hr = RegisterAssembly(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to register assembly, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        case atRemove:
            hr = UnregisterAssembly(&attrs);
            if (FAILED(hr))
                WcaLog(LOGMSG_STANDARD, "Failed to unregister assembly, hr: 0x%x, key: %S", hr, attrs.pwzKey);
            break;
        }

        // check rollback status
        if (0 == iRollbackStatus)
            continue; // operation did not complete, skip progress

        // progress
        hr = WcaProgressMessage(attrs.iActionCost, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeAssemblyAttributes(&attrs);

    // uninitialize
    UninitAssemblyExec();

    return hr;
}

HRESULT CpiConfigureRoleAssignments(
    LPWSTR* ppwzData,
    HANDLE hRollbackFile
    )
{
    HRESULT hr = S_OK;

    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, FALSE);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // write count to rollback file
    hr = CpiWriteIntegerToRollbackFile(hRollbackFile, iCnt);
    ExitOnFailure(hr, "Failed to write count to rollback file");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadRoleAssignmentsAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read role assignments attributes");

        // write key to rollback file
        hr = CpiWriteKeyToRollbackFile(hRollbackFile, attrs.pwzKey);
        ExitOnFailure(hr, "Failed to write key to rollback file");

        // action
        if (atNoOp != attrs.iActionType)
        {
            hr = ConfigureComponents(attrs.pwzPartID, attrs.pwzAppID, attrs.pCompList, atCreate == attrs.iActionType, TRUE);
            ExitOnFailure(hr, "Failed to configure components");

            if (S_FALSE == hr)
                ExitFunction(); // aborted by user
        }

        // write completion status to rollback file
        hr = CpiWriteIntegerToRollbackFile(hRollbackFile, 1);
        ExitOnFailure(hr, "Failed to write completion status to rollback file");

        // progress
        hr = WcaProgressMessage(attrs.iActionCost * attrs.iRoleCount, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeRoleAssignmentsAttributes(&attrs);

    return hr;
}

HRESULT CpiRollbackConfigureRoleAssignments(
    LPWSTR* ppwzData,
    CPI_ROLLBACK_DATA* pRollbackDataList
    )
{
    HRESULT hr = S_OK;

    int iRollbackStatus;

    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES attrs;
    ::ZeroMemory(&attrs, sizeof(attrs));

    // read action text
    hr = CpiActionStartMessage(ppwzData, NULL == pRollbackDataList);
    ExitOnFailure(hr, "Failed to send action start message");

    // get count
    int iCnt = 0;
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    for (int i = 0; i < iCnt; i++)
    {
        // read attributes from CustomActionData
        hr = ReadRoleAssignmentsAttributes(ppwzData, &attrs);
        ExitOnFailure(hr, "Failed to read role assignments attributes");

        // rollback status
        hr = CpiFindRollbackStatus(pRollbackDataList, attrs.pwzKey, &iRollbackStatus);

        if (S_FALSE == hr)
            continue; // not found, nothing to rollback

        // action
        if (atNoOp != attrs.iActionType)
        {
            hr = ConfigureComponents(attrs.pwzPartID, attrs.pwzAppID, attrs.pCompList, atCreate == attrs.iActionType, TRUE);
            ExitOnFailure(hr, "Failed to configure components");

            if (S_FALSE == hr)
                ExitFunction(); // aborted by user
        }

        // check rollback status
        if (0 == iRollbackStatus)
            continue; // operation did not complete, skip progress

        // progress
        hr = WcaProgressMessage(attrs.iActionCost * attrs.iRoleCount, FALSE);
        ExitOnFailure(hr, "Failed to update progress");
    }

    hr = S_OK;

LExit:
    // clean up
    FreeRoleAssignmentsAttributes(&attrs);

    return hr;
}


// helper function definitions

static HRESULT RegisterAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    // progress message
    hr = CpiActionDataMessage(1, (pAttrs->iAttributes & aaPathFromGAC) ? pAttrs->pwzAssemblyName : pAttrs->pwzDllPath);
    ExitOnFailure(hr, "Failed to send progress messages");

    if (S_FALSE == hr)
        ExitFunction(); // aborted by user

    // log
    WcaLog(LOGMSG_VERBOSE, "Registering assembly, key: %S", pAttrs->pwzKey);

    // extract path from GAC
    if (pAttrs->iAttributes & aaPathFromGAC)
    {
        hr = GetAssemblyPathFromGAC(pAttrs->pwzAssemblyName, &pAttrs->pwzDllPath);
        if (S_FALSE == hr)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        ExitOnFailure(hr, "Failed to get path for assembly from GAC");

        // log
        WcaLog(LOGMSG_VERBOSE, "Assembly path extracted from GAC, key: %S, path: '%S'", pAttrs->pwzKey, pAttrs->pwzDllPath);
    }

    // .net assembly
    if (pAttrs->iAttributes & aaDotNetAssembly)
    {
        hr = RegisterDotNetAssembly(pAttrs);
        ExitOnFailure(hr, "Failed to register .NET assembly");
    }

    // native assembly
    else
    {
        hr = RegisterNativeAssembly(pAttrs);
        ExitOnFailure(hr, "Failed to register native assembly");
    }

    // configure components
    if (pAttrs->pCompList)
    {
        hr = ConfigureComponents(pAttrs->pwzPartID, pAttrs->pwzAppID, pAttrs->pCompList, TRUE, FALSE);
        ExitOnFailure(hr, "Failed to configure components");
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT UnregisterAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    long lChanges = 0;

    ICatalogCollection* piColl = NULL;
    ICatalogObject* piObj = NULL;

    // progress message
    hr = CpiActionDataMessage(1, (pAttrs->iAttributes & aaPathFromGAC) ? pAttrs->pwzAssemblyName : pAttrs->pwzDllPath);
    ExitOnFailure(hr, "Failed to send progress messages");

    if (S_FALSE == hr)
        ExitFunction(); // aborted by user

    // log
    WcaLog(LOGMSG_VERBOSE, "Unregistering assembly, key: %S", pAttrs->pwzKey);

    // extract path from GAC
    if (pAttrs->iAttributes & aaPathFromGAC)
    {
        hr = GetAssemblyPathFromGAC(pAttrs->pwzAssemblyName, &pAttrs->pwzDllPath);
        ExitOnFailure(hr, "Failed to get path for assembly from GAC");

        if (S_FALSE == hr)
        {
            WcaLog(LOGMSG_VERBOSE, "Unable to locate assembly in GAC, assembly will not be unregistered from COM+, key: %S", pAttrs->pwzKey);
            ExitFunction1(hr = S_OK);
        }

        // log
        WcaLog(LOGMSG_VERBOSE, "Assembly path extracted from GAC, key: %S, path: '%S'", pAttrs->pwzKey, pAttrs->pwzDllPath);
    }

    // .NET assembly
    if (pAttrs->iAttributes & aaDotNetAssembly)
    {
        if (pAttrs->pwzAppID && *pAttrs->pwzAppID)
        {
            // When unregistering a .net assembly using the RegistrationHelper class, and the application is
            // left empty after all components in the assembly are removed, the RegistrationHelper class also
            // attempts to remove the application for some reason. However, it does not handle the situation
            // when the application has its deleteable property set to false, and will simply fail if this is
            // the case. This is the reason we are clearing the deleatable property of the application here.
            //
            // TODO: handle rollbacks

            // get applications collection
            hr = CpiGetApplicationsCollection(pAttrs->pwzPartID, &piColl);
            ExitOnFailure(hr, "Failed to get applications collection");

            if (S_FALSE == hr)
            {
                // applications collection not found
                WcaLog(LOGMSG_VERBOSE, "Unable to retrieve applications collection, nothing to delete, key: %S", pAttrs->pwzKey);
                ExitFunction1(hr = S_OK);
            }

            // find application object
            hr = CpiFindCollectionObjectByStringKey(piColl, pAttrs->pwzAppID, &piObj);
            ExitOnFailure(hr, "Failed to find application object");

            if (S_FALSE == hr)
            {
                // application not found
                WcaLog(LOGMSG_VERBOSE, "Unable to find application object, nothing to delete, key: %S", pAttrs->pwzKey);
                ExitFunction1(hr = S_OK);
            }

            // reset deleteable property
            hr = CpiResetObjectProperty(piColl, piObj, L"Deleteable");
            ExitOnFailure(hr, "Failed to reset deleteable property");
        }

        // unregister assembly
        hr = UnregisterDotNetAssembly(pAttrs);
        ExitOnFailure(hr, "Failed to unregister .NET assembly");
    }

    // native assembly
    else
    {
        // get components collection
        hr = CpiGetComponentsCollection(pAttrs->pwzPartID, pAttrs->pwzAppID, &piColl);
        ExitOnFailure(hr, "Failed to get components collection");

        if (S_FALSE == hr)
        {
            // components collection not found
            WcaLog(LOGMSG_VERBOSE, "Unable to retrieve components collection, nothing to delete, key: %S", pAttrs->pwzKey);
            ExitFunction1(hr = S_OK);
        }

        // remove components
        hr = RemoveComponents(piColl, pAttrs->pCompList);
        ExitOnFailure(hr, "Failed to get remove components");

        // save changes
        hr = piColl->SaveChanges(&lChanges);
        if (COMADMIN_E_OBJECTERRORS == hr)
            CpiLogCatalogErrorInfo();
        ExitOnFailure(hr, "Failed to save changes");
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piColl);
    ReleaseObject(piObj);

    return hr;
}

static void InitAssemblyExec()
{
    gpiRegHlp = NULL;
    gpAssemblyCache = NULL;
    ghMscoree = NULL;
    ghFusion = NULL;
}

static void UninitAssemblyExec()
{
    ReleaseObject(gpiRegHlp);
    ReleaseObject(gpAssemblyCache);
    if (ghFusion)
        ::FreeLibrary(ghFusion);
    if (ghMscoree)
        ::FreeLibrary(ghMscoree);
}

static HRESULT GetRegistrationHelper(
    IDispatch** ppiRegHlp
    )
{
    HRESULT hr = S_OK;

    if (!gpiRegHlp)
    {
        // create registration helper object
        hr = ::CoCreateInstance(CLSID_RegistrationHelper, NULL, CLSCTX_ALL, IID_IDispatch, (void**)&gpiRegHlp); 
        ExitOnFailure(hr, "Failed to create registration helper object");
    }

    gpiRegHlp->AddRef();
    *ppiRegHlp = gpiRegHlp;

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT GetAssemblyCacheObject(
    IAssemblyCache** ppAssemblyCache
    )
{
    HRESULT hr = S_OK;

    if (!gpAssemblyCache)
    {
        // mscoree.dll
        if (!ghMscoree)
        {
            // load mscoree.dll
            ghMscoree = ::LoadLibraryW(L"mscoree.dll");
            ExitOnNull(ghMscoree, hr, E_FAIL, "Failed to load mscoree.dll");
        }

        // fusion.dll
        if (!ghFusion)
        {
            // get LoadLibraryShim function address
            LoadLibraryShimFunc pfnLoadLibraryShim = (LoadLibraryShimFunc)::GetProcAddress(ghMscoree, "LoadLibraryShim");
            ExitOnNull(pfnLoadLibraryShim, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for LoadLibraryShim() function");

            // load fusion.dll
            hr = pfnLoadLibraryShim(L"fusion.dll", NULL, NULL, &ghFusion);
            ExitOnFailure(hr, "Failed to load fusion.dll");
        }

        // get CreateAssemblyCache function address
        CreateAssemblyCacheFunc pfnCreateAssemblyCache = (CreateAssemblyCacheFunc)::GetProcAddress(ghFusion, "CreateAssemblyCache");
        ExitOnNull(pfnCreateAssemblyCache, hr, HRESULT_FROM_WIN32(::GetLastError()), "Failed get address for CreateAssemblyCache() function");

        // create AssemblyCache object
        hr = pfnCreateAssemblyCache(&gpAssemblyCache, 0);
        ExitOnFailure(hr, "Failed to create AssemblyCache object");
    }

    gpAssemblyCache->AddRef();
    *ppAssemblyCache = gpAssemblyCache;

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT GetAssemblyPathFromGAC(
    LPCWSTR pwzAssemblyName,
    LPWSTR* ppwzAssemblyPath
    )
{
    HRESULT hr = S_OK;

    IAssemblyCache* pAssemblyCache = NULL;

    ASSEMBLY_INFO assemblyInfo;
    WCHAR wzPathBuf[MAX_PATH];

    ::ZeroMemory(&assemblyInfo, sizeof(ASSEMBLY_INFO));
    ::ZeroMemory(wzPathBuf, countof(wzPathBuf));

    // get AssemblyCache object
    hr = GetAssemblyCacheObject(&pAssemblyCache);
    ExitOnFailure(hr, "Failed to get AssemblyCache object");

    // get assembly info
    assemblyInfo.cbAssemblyInfo = sizeof(ASSEMBLY_INFO);
    assemblyInfo.pszCurrentAssemblyPathBuf = wzPathBuf;
    assemblyInfo.cchBuf = countof(wzPathBuf);

    hr = pAssemblyCache->QueryAssemblyInfo(0, pwzAssemblyName, &assemblyInfo);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
        ExitFunction1(hr = S_FALSE);
    ExitOnFailure(hr, "Failed to get assembly info");

    // copy assembly path
    hr = StrAllocString(ppwzAssemblyPath, wzPathBuf, 0);
    ExitOnFailure(hr, "Failed to copy assembly path");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(pAssemblyCache);

    return hr;
}

static HRESULT RegisterDotNetAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    IDispatch* piRegHlp = NULL;

    DISPID dispid;
    BSTR bstrMember = NULL;

    long lInstallationFlags = 0;

    VARIANTARG rgvarg[5];
    DISPPARAMS dispparams;
    EXCEPINFO excepInfo;

    BSTR bstrPartName = NULL;
    BSTR bstrAppName = NULL;
    BSTR bstrDllPath = NULL;
    BSTR bstrTlbPath = NULL;

    ::ZeroMemory(rgvarg, sizeof(rgvarg));
    ::ZeroMemory(&dispparams, sizeof(dispparams));
    ::ZeroMemory(&excepInfo, sizeof(excepInfo));

    bstrMember = ::SysAllocString(L"InstallAssembly_2");
    ExitOnNull(bstrMember, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for method name");

    // create BSTRs for parameters
    if (pAttrs->pwzPartID && *pAttrs->pwzPartID)
    {
        bstrPartName = ::SysAllocString(pAttrs->pwzPartID);
        ExitOnNull(bstrPartName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for partition id");
    }

    if (pAttrs->pwzAppID && *pAttrs->pwzAppID)
    {
        bstrAppName = ::SysAllocString(pAttrs->pwzAppID);
        ExitOnNull(bstrAppName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for application id");
    }

    bstrDllPath = ::SysAllocString(pAttrs->pwzDllPath);
    ExitOnNull(bstrDllPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for dll path");

    if (pAttrs->pwzTlbPath && *pAttrs->pwzTlbPath)
    {
        bstrTlbPath = ::SysAllocString(pAttrs->pwzTlbPath);
        ExitOnNull(bstrTlbPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for tlb path");
    }

    // get registration helper object
    hr = GetRegistrationHelper(&piRegHlp);
    ExitOnFailure(hr, "Failed to get registration helper object");

    // get dispatch id of InstallAssembly() method
    hr = piRegHlp->GetIDsOfNames(IID_NULL, &bstrMember, 1, LOCALE_USER_DEFAULT, &dispid);
    ExitOnFailure(hr, "Failed to get dispatch id of InstallAssembly() method");

    // set installation flags
    lInstallationFlags = ifExpectExistingTypeLib;

    if (!bstrAppName)
        lInstallationFlags |= ifFindOrCreateTargetApplication;

    // invoke InstallAssembly() method
    rgvarg[0].vt = VT_I4;
    rgvarg[0].lVal = lInstallationFlags;
    rgvarg[1].vt = VT_BYREF|VT_BSTR;
    rgvarg[1].pbstrVal = &bstrTlbPath;
    rgvarg[2].vt = VT_BSTR;
    rgvarg[2].bstrVal = bstrPartName;
    rgvarg[3].vt = VT_BYREF|VT_BSTR;
    rgvarg[3].pbstrVal = &bstrAppName;
    rgvarg[4].vt = VT_BSTR;
    rgvarg[4].bstrVal = bstrDllPath;
    dispparams.rgvarg = rgvarg;
    dispparams.cArgs = 5;
    dispparams.cNamedArgs = 0;

    hr = piRegHlp->Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, &excepInfo, NULL);
    if (DISP_E_EXCEPTION == hr)
    {
        // log exception information
        if (!excepInfo.pfnDeferredFillIn || (excepInfo.pfnDeferredFillIn && SUCCEEDED(excepInfo.pfnDeferredFillIn(&excepInfo))))
        {
            WcaLog(LOGMSG_STANDARD, "ExceptionInfo: Code='%hu', Source='%S', Description='%S', HelpFile='%S', HelpContext='%u'",
                excepInfo.wCode, excepInfo.bstrSource,
                excepInfo.bstrDescription ? excepInfo.bstrDescription : L"",
                excepInfo.bstrHelpFile ? excepInfo.bstrHelpFile : L"",
                excepInfo.dwHelpContext);
        }
    }
    ExitOnFailure(hr, "Failed to invoke RegistrationHelper.InstallAssembly() method");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRegHlp);

    ReleaseBSTR(bstrMember);

    ReleaseBSTR(excepInfo.bstrSource);
    ReleaseBSTR(excepInfo.bstrDescription);
    ReleaseBSTR(excepInfo.bstrHelpFile);

    ReleaseBSTR(bstrPartName);
    ReleaseBSTR(bstrAppName);
    ReleaseBSTR(bstrDllPath);
    ReleaseBSTR(bstrTlbPath);

    return hr;
}

static HRESULT RegisterNativeAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    ICOMAdminCatalog* piCatalog = NULL;
    ICOMAdminCatalog2* piCatalog2 = NULL;
    BSTR bstrGlobPartID = NULL;

    BSTR bstrPartID = NULL;
    BSTR bstrAppID = NULL;
    BSTR bstrDllPath = NULL;
    BSTR bstrTlbPath = NULL;
    BSTR bstrPSDllPath = NULL;

    // create BSTRs for parameters
    if (pAttrs->pwzPartID && *pAttrs->pwzPartID)
    {
        bstrPartID = ::SysAllocString(pAttrs->pwzPartID);
        ExitOnNull(bstrPartID, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for partition id");
    }

    bstrAppID = ::SysAllocString(pAttrs->pwzAppID);
    ExitOnNull(bstrAppID, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for application id");

    bstrDllPath = ::SysAllocString(pAttrs->pwzDllPath);
    ExitOnNull(bstrDllPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for dll path");

    bstrTlbPath = ::SysAllocString(pAttrs->pwzTlbPath ? pAttrs->pwzTlbPath : L"");
    ExitOnNull(bstrTlbPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for tlb path");

    bstrPSDllPath = ::SysAllocString(pAttrs->pwzPSDllPath ? pAttrs->pwzPSDllPath : L"");
    ExitOnNull(bstrPSDllPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for tlb path");

    // get catalog
    hr = CpiGetAdminCatalog(&piCatalog);
    ExitOnFailure(hr, "Failed to get COM+ admin catalog");

    // get ICOMAdminCatalog2 interface
    hr = piCatalog->QueryInterface(IID_ICOMAdminCatalog2, (void**)&piCatalog2);

    // COM+ 1.5 or later
    if (E_NOINTERFACE != hr)
    {
        ExitOnFailure(hr, "Failed to get IID_ICOMAdminCatalog2 interface");

        // partition id
        if (!bstrPartID)
        {
            // get global partition id
            hr = piCatalog2->get_GlobalPartitionID(&bstrGlobPartID);
            ExitOnFailure(hr, "Failed to get global partition id");
        }

        // set current partition
        hr = piCatalog2->put_CurrentPartition(bstrPartID ? bstrPartID : bstrGlobPartID);
        ExitOnFailure(hr, "Failed to set current partition");
    }

    // COM+ pre 1.5
    else
    {
        // this version of COM+ does not support partitions, make sure a partition was not specified
        if (bstrPartID)
            ExitOnFailure(hr = E_FAIL, "Partitions are not supported by this version of COM+");
    }

    // install event classes
    if (pAttrs->iAttributes & aaEventClass)
    {
        hr = piCatalog->InstallEventClass(bstrAppID, bstrDllPath, bstrTlbPath, bstrPSDllPath);
        if (COMADMIN_E_OBJECTERRORS == hr)
            CpiLogCatalogErrorInfo();
        ExitOnFailure(hr, "Failed to install event classes");
    }

    // install components
    else
    {
        hr = piCatalog->InstallComponent(bstrAppID, bstrDllPath, bstrTlbPath, bstrPSDllPath);
        if (COMADMIN_E_OBJECTERRORS == hr)
            CpiLogCatalogErrorInfo();
        ExitOnFailure(hr, "Failed to install components");
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCatalog);
    ReleaseObject(piCatalog2);
    ReleaseBSTR(bstrGlobPartID);

    ReleaseBSTR(bstrPartID);
    ReleaseBSTR(bstrAppID);
    ReleaseBSTR(bstrDllPath);
    ReleaseBSTR(bstrTlbPath);
    ReleaseBSTR(bstrPSDllPath);

    return hr;
}

static HRESULT UnregisterDotNetAssembly(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    IDispatch* piRegHlp = NULL;

    DISPID dispid;
    BSTR bstrMember = NULL;

    VARIANTARG rgvarg[3];
    DISPPARAMS dispparams;
    EXCEPINFO excepInfo;

    BSTR bstrPartName = NULL;
    BSTR bstrAppName = NULL;
    BSTR bstrDllPath = NULL;

    ::ZeroMemory(rgvarg, sizeof(rgvarg));
    ::ZeroMemory(&dispparams, sizeof(dispparams));
    ::ZeroMemory(&excepInfo, sizeof(excepInfo));

    bstrMember = ::SysAllocString(L"UninstallAssembly_2");
    ExitOnNull(bstrMember, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for method name");

    // create BSTRs for parameters
    if (pAttrs->pwzPartID && *pAttrs->pwzPartID)
    {
        bstrPartName = ::SysAllocString(pAttrs->pwzPartID);
        ExitOnNull(bstrPartName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for partition id");
    }

    bstrAppName = ::SysAllocString(pAttrs->pwzAppID);
    ExitOnNull(bstrAppName, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for application id");

    bstrDllPath = ::SysAllocString(pAttrs->pwzDllPath);
    ExitOnNull(bstrDllPath, hr, E_OUTOFMEMORY, "Failed to allocate BSTR for dll path");

    // get registration helper object
    hr = GetRegistrationHelper(&piRegHlp);
    ExitOnFailure(hr, "Failed to get registration helper object");

    // get dispatch id of UninstallAssembly() method
    hr = piRegHlp->GetIDsOfNames(IID_NULL, &bstrMember, 1, LOCALE_USER_DEFAULT, &dispid);
    ExitOnFailure(hr, "Failed to get dispatch id of UninstallAssembly() method");

    // invoke UninstallAssembly() method
    rgvarg[0].vt = VT_BSTR;
    rgvarg[0].bstrVal = bstrPartName;
    rgvarg[1].vt = VT_BSTR;
    rgvarg[1].bstrVal = bstrAppName;
    rgvarg[2].vt = VT_BSTR;
    rgvarg[2].bstrVal = bstrDllPath;
    dispparams.rgvarg = rgvarg;
    dispparams.cArgs = 3;
    dispparams.cNamedArgs = 0;

    hr = piRegHlp->Invoke(dispid, IID_NULL, LOCALE_USER_DEFAULT, DISPATCH_METHOD, &dispparams, NULL, &excepInfo, NULL);
    if (DISP_E_EXCEPTION == hr)
    {
        // log exception information
        if (!excepInfo.pfnDeferredFillIn || (excepInfo.pfnDeferredFillIn && SUCCEEDED(excepInfo.pfnDeferredFillIn(&excepInfo))))
        {
            WcaLog(LOGMSG_STANDARD, "ExceptionInfo: Code='%hu', Source='%S', Description='%S', HelpFile='%S', HelpContext='%u'",
                excepInfo.wCode, excepInfo.bstrSource,
                excepInfo.bstrDescription ? excepInfo.bstrDescription : L"",
                excepInfo.bstrHelpFile ? excepInfo.bstrHelpFile : L"",
                excepInfo.dwHelpContext);
        }
    }
    ExitOnFailure(hr, "Failed to invoke RegistrationHelper.UninstallAssembly() method");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRegHlp);

    ReleaseBSTR(bstrMember);

    ReleaseBSTR(excepInfo.bstrSource);
    ReleaseBSTR(excepInfo.bstrDescription);
    ReleaseBSTR(excepInfo.bstrHelpFile);

    ReleaseBSTR(bstrPartName);
    ReleaseBSTR(bstrAppName);
    ReleaseBSTR(bstrDllPath);

    return hr;
}

static HRESULT RemoveComponents(
    ICatalogCollection* piCompColl,
    CPI_COMPONENT* pCompList
    )
{
    HRESULT hr = S_OK;

    for (CPI_COMPONENT* pItm = pCompList; pItm; pItm = pItm->pNext)
    {
        // remove
        hr = CpiRemoveCollectionObject(piCompColl, pItm->wzCLSID, NULL, FALSE);
        ExitOnFailure(hr, "Failed to remove component");

        if (S_FALSE == hr)
            WcaLog(LOGMSG_VERBOSE, "Component not found, nothing to delete, key: %S", pItm->wzCLSID);
    }

    hr = S_OK;

LExit:
    return hr;
}

static HRESULT ReadAssemblyAttributes(
    LPWSTR* ppwzData,
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    // read attributes
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionType);
    ExitOnFailure(hr, "Failed to read action type");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionCost);
    ExitOnFailure(hr, "Failed to read action cost");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzAssemblyName);
    ExitOnFailure(hr, "Failed to read assembly name");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzDllPath);
    ExitOnFailure(hr, "Failed to read dll path");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzTlbPath);
    ExitOnFailure(hr, "Failed to read tlb path");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPSDllPath);
    ExitOnFailure(hr, "Failed to read proxy-stub dll path");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iAttributes);
    ExitOnFailure(hr, "Failed to read attributes");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzAppID);
    ExitOnFailure(hr, "Failed to read application id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to read partition id");

    // free existing component list
    if (pAttrs->pCompList)
    {
        FreeComponentList(pAttrs->pCompList);
        pAttrs->pCompList = NULL;
    }

    // read components
    hr = ReadComponentList(ppwzData, &pAttrs->pCompList);
    ExitOnFailure(hr, "Failed to read components");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeAssemblyAttributes(
    CPI_ASSEMBLY_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzAssemblyName);
    ReleaseStr(pAttrs->pwzDllPath);
    ReleaseStr(pAttrs->pwzTlbPath);
    ReleaseStr(pAttrs->pwzPSDllPath);
    ReleaseStr(pAttrs->pwzAppID);
    ReleaseStr(pAttrs->pwzPartID);

    if (pAttrs->pCompList)
        FreeComponentList(pAttrs->pCompList);
}

static HRESULT ReadRoleAssignmentsAttributes(
    LPWSTR* ppwzData,
    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES* pAttrs
    )
{
    HRESULT hr = S_OK;

    // read attributes
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionType);
    ExitOnFailure(hr, "Failed to read action type");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iActionCost);
    ExitOnFailure(hr, "Failed to read action cost");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzKey);
    ExitOnFailure(hr, "Failed to read key");
    hr = WcaReadIntegerFromCaData(ppwzData, &pAttrs->iRoleCount);
    ExitOnFailure(hr, "Failed to read role assignments count");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzAppID);
    ExitOnFailure(hr, "Failed to read application id");
    hr = WcaReadStringFromCaData(ppwzData, &pAttrs->pwzPartID);
    ExitOnFailure(hr, "Failed to read partition id");

    // free existing component list
    if (pAttrs->pCompList)
    {
        FreeComponentList(pAttrs->pCompList);
        pAttrs->pCompList = NULL;
    }

    // read components
    hr = ReadComponentList(ppwzData, &pAttrs->pCompList);
    ExitOnFailure(hr, "Failed to read components");

    hr = S_OK;

LExit:
    return hr;
}

static void FreeRoleAssignmentsAttributes(
    CPI_ROLE_ASSIGNMENTS_ATTRIBUTES* pAttrs
    )
{
    ReleaseStr(pAttrs->pwzKey);
    ReleaseStr(pAttrs->pwzAppID);
    ReleaseStr(pAttrs->pwzPartID);

    if (pAttrs->pCompList)
        FreeComponentList(pAttrs->pCompList);
}


static HRESULT ConfigureComponents(
    LPCWSTR pwzPartID,
    LPCWSTR pwzAppID,
    CPI_COMPONENT* pCompList,
    BOOL fCreate,
    BOOL fProgress
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piCompColl = NULL;
    ICatalogObject* piCompObj = NULL;

    long lChanges = 0;

    // get components collection
    hr = CpiGetComponentsCollection(pwzPartID, pwzAppID, &piCompColl);
    if (S_FALSE == hr)
        if (fCreate)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            ExitFunction1(hr = S_OK);
    ExitOnFailure(hr, "Failed to get components collection");

    // read components
    for (CPI_COMPONENT* pItm = pCompList; pItm; pItm = pItm->pNext)
    {
        // progress message
        if (fProgress)
        {
            hr = CpiActionDataMessage(1, pItm->wzCLSID);
            ExitOnFailure(hr, "Failed to send progress messages");

            if (S_FALSE == hr)
                ExitFunction(); // aborted by user
        }

        // find component
        hr = CpiFindCollectionObjectByStringKey(piCompColl, pItm->wzCLSID, &piCompObj);
        if (S_FALSE == hr)
            if (fCreate)
                hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            else
                continue;
        ExitOnFailure(hr, "Failed to find component object");

        // properties
        hr = CpiPutCollectionObjectValues(piCompObj, pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to write properties");

        // read roles
        if (pItm->pRoleAssignmentList)
        {
            hr = ConfigureRoleAssignments(L"RolesForComponent", piCompColl, piCompObj, pItm->pRoleAssignmentList, fCreate);
            ExitOnFailure(hr, "Failed to read roles");
        }

        // read interfaces
        if (pItm->pInterfaceList)
        {
            hr = ConfigureInterfaces(piCompColl, piCompObj, pItm->pInterfaceList, fCreate);
            ExitOnFailure(hr, "Failed to read interfaces");
        }

        // clean up
        ReleaseNullObject(piCompObj);
    }

    // save changes
    hr = piCompColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piCompColl);
    ReleaseObject(piCompObj);

    return hr;
}

static HRESULT ConfigureInterfaces(
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    CPI_INTERFACE* pIntfList,
    BOOL fCreate
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piIntfColl = NULL;
    ICatalogObject* piIntfObj = NULL;

    long lChanges = 0;

    // get interfaces collection
    hr = CpiGetInterfacesCollection(piCompColl, piCompObj, &piIntfColl);
    if (S_FALSE == hr)
        if (fCreate)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            ExitFunction1(hr = S_OK);
    ExitOnFailure(hr, "Failed to get interfaces collection");

    // read interfaces
    for (CPI_INTERFACE* pItm = pIntfList; pItm; pItm = pItm->pNext)
    {
        // find interface
        hr = CpiFindCollectionObjectByStringKey(piIntfColl, pItm->wzIID, &piIntfObj);
        if (S_FALSE == hr)
            if (fCreate)
                hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            else
                continue;
        ExitOnFailure(hr, "Failed to find interface object");

        // properties
        hr = CpiPutCollectionObjectValues(piIntfObj, pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to write properties");

        // read roles
        if (pItm->pRoleAssignmentList)
        {
            hr = ConfigureRoleAssignments(L"RolesForInterface", piIntfColl, piIntfObj, pItm->pRoleAssignmentList, fCreate);
            ExitOnFailure(hr, "Failed to read roles");
        }

        // read methods
        if (pItm->pMethodList)
        {
            hr = ConfigureMethods(piIntfColl, piIntfObj, pItm->pMethodList, fCreate);
            ExitOnFailure(hr, "Failed to read methods");
        }

        // clean up
        ReleaseNullObject(piIntfObj);
    }

    // save changes
    hr = piIntfColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piIntfColl);
    ReleaseObject(piIntfObj);

    return hr;
}

static HRESULT ConfigureMethods(
    ICatalogCollection* piIntfColl,
    ICatalogObject* piIntfObj,
    CPI_METHOD* pMethList,
    BOOL fCreate
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piMethColl = NULL;
    ICatalogObject* piMethObj = NULL;

    long lChanges = 0;

    // get methods collection
    hr = CpiGetMethodsCollection(piIntfColl, piIntfObj, &piMethColl);
    if (S_FALSE == hr)
        if (fCreate)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            ExitFunction1(hr = S_OK);
    ExitOnFailure(hr, "Failed to get methods collection");

    // read methods
    for (CPI_METHOD* pItm = pMethList; pItm; pItm = pItm->pNext)
    {
        // find method
        if (*pItm->wzIndex)
            hr = CpiFindCollectionObjectByIntegerKey(piMethColl, _wtol(pItm->wzIndex), &piMethObj);
        else
            hr = CpiFindCollectionObjectByName(piMethColl, pItm->wzName, &piMethObj);

        if (S_FALSE == hr)
            if (fCreate)
                hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
            else
                continue;
        ExitOnFailure(hr, "Failed to find method object");

        // properties
        hr = CpiPutCollectionObjectValues(piMethObj, pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to write properties");

        // read roles
        if (pItm->pRoleAssignmentList)
        {
            hr = ConfigureRoleAssignments(L"RolesForMethod", piMethColl, piMethObj, pItm->pRoleAssignmentList, fCreate);
            ExitOnFailure(hr, "Failed to read roles");
        }

        // clean up
        ReleaseNullObject(piMethObj);
    }

    // save changes
    hr = piMethColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piMethColl);
    ReleaseObject(piMethObj);

    return hr;
}

static HRESULT ConfigureRoleAssignments(
    LPCWSTR pwzCollName,
    ICatalogCollection* piCompColl,
    ICatalogObject* piCompObj,
    CPI_ROLE_ASSIGNMENT* pRoleList,
    BOOL fCreate
    )
{
    HRESULT hr = S_OK;

    ICatalogCollection* piRoleColl = NULL;
    ICatalogObject* piRoleObj = NULL;

    long lChanges = 0;

    // get roles collection
    hr = CpiGetCatalogCollection(piCompColl, piCompObj, pwzCollName, &piRoleColl);
    if (S_FALSE == hr)
        if (fCreate)
            hr = HRESULT_FROM_WIN32(ERROR_NOT_FOUND);
        else
            ExitFunction1(hr = S_OK);
    ExitOnFailure(hr, "Failed to get role assignments collection");

    // read roles
    for (CPI_ROLE_ASSIGNMENT* pItm = pRoleList; pItm; pItm = pItm->pNext)
    {
        if (fCreate)
        {
            // find existing role
            hr = CpiFindCollectionObjectByName(piRoleColl, pItm->wzRoleName, NULL);
            ExitOnFailure1(hr, "Failed to find role, key: %S", pItm->wzKey);

            if (S_OK == hr)
                continue; // role already exists

            // add object
            hr = CpiAddCollectionObject(piRoleColl, &piRoleObj);
            ExitOnFailure(hr, "Failed to add role assignment to collection");

            // role name
            hr = CpiPutCollectionObjectValue(piRoleObj, L"Name", pItm->wzRoleName);
            ExitOnFailure1(hr, "Failed to set role name property, key: %S", pItm->wzKey);

            // clean up
            ReleaseNullObject(piRoleObj);
        }
        else
        {
            // remove role
            hr = CpiRemoveCollectionObject(piRoleColl, NULL, pItm->wzRoleName, FALSE);
            ExitOnFailure1(hr, "Failed to remove role, key: %S", pItm->wzKey);
        }
    }

    // save changes
    hr = piRoleColl->SaveChanges(&lChanges);
    if (COMADMIN_E_OBJECTERRORS == hr)
        CpiLogCatalogErrorInfo();
    ExitOnFailure(hr, "Failed to save changes");

    hr = S_OK;

LExit:
    // clean up
    ReleaseObject(piRoleColl);
    ReleaseObject(piRoleObj);

    return hr;
}

static HRESULT ReadComponentList(
    LPWSTR* ppwzData,
    CPI_COMPONENT** ppCompList
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    CPI_COMPONENT* pItm = NULL;

    int iCnt = 0;

    // read count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // read components
    for (int i = 0; i < iCnt; i++)
    {
        pItm = (CPI_COMPONENT*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_COMPONENT));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // read clsid
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read clsid");
        StringCchCopyW(pItm->wzCLSID, countof(pItm->wzCLSID), pwzData);

        // read properties
        hr = CpiReadPropertyList(ppwzData, &pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to read properties");

        // read role assignments
        hr = ReadRoleAssignmentList(ppwzData, &pItm->pRoleAssignmentList);
        ExitOnFailure(hr, "Failed to read role assignments");

        // read interfaces
        hr = ReadInterfaceList(ppwzData, &pItm->pInterfaceList);
        ExitOnFailure(hr, "Failed to read interfaces");

        // add to list
        if (*ppCompList)
            pItm->pNext = *ppCompList;
        *ppCompList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzData);

    if (pItm)
        FreeComponentList(pItm);

    return hr;
}

static HRESULT ReadInterfaceList(
    LPWSTR* ppwzData,
    CPI_INTERFACE** ppIntfList
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    CPI_INTERFACE* pItm = NULL;

    int iCnt = 0;

    // read count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // read interfaces
    for (int i = 0; i < iCnt; i++)
    {
        pItm = (CPI_INTERFACE*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_INTERFACE));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // read iid
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read iid");
        StringCchCopyW(pItm->wzIID, countof(pItm->wzIID), pwzData);

        // read properties
        hr = CpiReadPropertyList(ppwzData, &pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to read properties");

        // read role assignments
        hr = ReadRoleAssignmentList(ppwzData, &pItm->pRoleAssignmentList);
        ExitOnFailure(hr, "Failed to read role assignments");

        // read methods
        hr = ReadMethodList(ppwzData, &pItm->pMethodList);
        ExitOnFailure(hr, "Failed to read methods");

        // add to list
        if (*ppIntfList)
            pItm->pNext = *ppIntfList;
        *ppIntfList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzData);

    if (pItm)
        FreeInterfaceList(pItm);

    return hr;
}

static HRESULT ReadMethodList(
    LPWSTR* ppwzData,
    CPI_METHOD** ppMethList
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    CPI_METHOD* pItm = NULL;

    int iCnt = 0;

    // read count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read count");

    // read methods
    for (int i = 0; i < iCnt; i++)
    {
        pItm = (CPI_METHOD*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_METHOD));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // read index
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read index");
        StringCchCopyW(pItm->wzIndex, countof(pItm->wzIndex), pwzData);

        // read name
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read name");
        StringCchCopyW(pItm->wzName, countof(pItm->wzName), pwzData);

        // read properties
        hr = CpiReadPropertyList(ppwzData, &pItm->pPropertyList);
        ExitOnFailure(hr, "Failed to read properties");

        // read role assignments
        hr = ReadRoleAssignmentList(ppwzData, &pItm->pRoleAssignmentList);
        ExitOnFailure(hr, "Failed to read role assignments");

        // add to list
        if (*ppMethList)
            pItm->pNext = *ppMethList;
        *ppMethList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzData);

    if (pItm)
        FreeMethodList(pItm);

    return hr;
}

static HRESULT ReadRoleAssignmentList(
    LPWSTR* ppwzData,
    CPI_ROLE_ASSIGNMENT** ppRoleList
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;

    CPI_ROLE_ASSIGNMENT* pItm = NULL;

    int iCnt = 0;

    // read role count
    hr = WcaReadIntegerFromCaData(ppwzData, &iCnt);
    ExitOnFailure(hr, "Failed to read role assignments count");

    // read roles
    for (int i = 0; i < iCnt; i++)
    {
        pItm = (CPI_ROLE_ASSIGNMENT*)::HeapAlloc(::GetProcessHeap(), HEAP_ZERO_MEMORY, sizeof(CPI_ROLE_ASSIGNMENT));
        if (!pItm)
            ExitFunction1(hr = E_OUTOFMEMORY);

        // read key
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read key");
        StringCchCopyW(pItm->wzKey, countof(pItm->wzKey), pwzData);

        // read role name
        hr = WcaReadStringFromCaData(ppwzData, &pwzData);
        ExitOnFailure(hr, "Failed to read role name");
        StringCchCopyW(pItm->wzRoleName, countof(pItm->wzRoleName), pwzData);

        // add to list
        if (*ppRoleList)
            pItm->pNext = *ppRoleList;
        *ppRoleList = pItm;
        pItm = NULL;
    }

    hr = S_OK;

LExit:
    // clean up
    ReleaseStr(pwzData);

    if (pItm)
        FreeRoleAssignmentList(pItm);

    return hr;
}

static void FreeComponentList(
    CPI_COMPONENT* pList
    )
{
    while (pList)
    {
        if (pList->pPropertyList)
            CpiFreePropertyList(pList->pPropertyList);
        if (pList->pRoleAssignmentList)
            FreeRoleAssignmentList(pList->pRoleAssignmentList);
        if (pList->pInterfaceList)
            FreeInterfaceList(pList->pInterfaceList);

        CPI_COMPONENT* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void FreeInterfaceList(
    CPI_INTERFACE* pList
    )
{
    while (pList)
    {
        if (pList->pPropertyList)
            CpiFreePropertyList(pList->pPropertyList);
        if (pList->pRoleAssignmentList)
            FreeRoleAssignmentList(pList->pRoleAssignmentList);
        if (pList->pMethodList)
            FreeMethodList(pList->pMethodList);

        CPI_INTERFACE* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void FreeMethodList(
    CPI_METHOD* pList
    )
{
    while (pList)
    {
        if (pList->pPropertyList)
            CpiFreePropertyList(pList->pPropertyList);
        if (pList->pRoleAssignmentList)
            FreeRoleAssignmentList(pList->pRoleAssignmentList);

        CPI_METHOD* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}

static void FreeRoleAssignmentList(
    CPI_ROLE_ASSIGNMENT* pList
    )
{
    while (pList)
    {
        CPI_ROLE_ASSIGNMENT* pDelete = pList;
        pList = pList->pNext;
        ::HeapFree(::GetProcessHeap(), 0, pDelete);
    }
}
