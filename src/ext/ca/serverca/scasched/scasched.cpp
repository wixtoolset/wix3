// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const int ConfigureIIsCost = 8;
const int WriteMetabaseChangesCost = 20;
const int WriteIIS7ConfigChangesCost = 20;

// sql queries
LPCWSTR vcsUserDeferredQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Password` FROM `User`";

LPCWSTR vcsWebSvcExtQuery = L"SELECT `Component_`, `File`, `Description`, `Group`, `Attributes` FROM `IIsWebServiceExtension`";

LPCWSTR vcsAppPoolQuery = L"SELECT `AppPool`, `Name`, `Component_`, `Attributes`, `User_`, `RecycleMinutes`, `RecycleRequests`, `RecycleTimes`, `VirtualMemory`, `PrivateMemory`, `IdleTimeout`, `QueueLimit`, `CPUMon`, `MaxProc`, `ManagedRuntimeVersion`, `ManagedPipelineMode` FROM `IIsAppPool`";

LPCWSTR vcsComponentAttrQuery = L"SELECT `Component`,`Attributes` FROM `Component`";

LPCWSTR vcsMimeMapQuery = L"SELECT `MimeMap`, `ParentType`, `ParentValue`, `MimeType`, `Extension` FROM `IIsMimeMap`";

LPCWSTR vcsHttpHeaderQuery = L"SELECT `Name`, `ParentType`, `ParentValue`, `Value`, `Attributes` FROM `IIsHttpHeader` ORDER BY `Sequence`";

LPCWSTR vcsWebErrorQuery =
    L"SELECT `ErrorCode`, `SubCode`, `ParentType`, `ParentValue`, `File`, `URL` "
    L"FROM `IIsWebError` ORDER BY `ErrorCode`, `SubCode`";

LPCWSTR vcsWebDirPropertiesQuery = L"SELECT `DirProperties`, `Access`, `Authorization`, `AnonymousUser_`, `IIsControlledPassword`, `LogVisits`, `Index`, `DefaultDoc`, `AspDetailedError`, `HttpExpires`, `CacheControlMaxAge`, `CacheControlCustom`, `NoCustomError`, `AccessSSLFlags`, `AuthenticationProviders` "
    L"FROM `IIsWebDirProperties`";

LPCWSTR vcsSslCertificateQuery = L"SELECT `Certificate`.`StoreName`, `CertificateHash`.`Hash`, `IIsWebSiteCertificates`.`Web_` FROM `Certificate`, `CertificateHash`, `IIsWebSiteCertificates` WHERE `Certificate`.`Certificate`=`CertificateHash`.`Certificate_` AND `CertificateHash`.`Certificate_`=`IIsWebSiteCertificates`.`Certificate_`";

LPCWSTR vcsWebLogQuery = L"SELECT `Log`, `Format` "
    L"FROM `IIsWebLog`";

LPCWSTR vcsWebApplicationQuery = L"SELECT `Name`, `Isolation`, `AllowSessions`, `SessionTimeout`, "
    L"`Buffer`, `ParentPaths`, `DefaultScript`, `ScriptTimeout`, "
    L"`ServerDebugging`, `ClientDebugging`, `AppPool_`, `Application` "
    L"FROM `IIsWebApplication`";

LPCWSTR vcsWebAppExtensionQuery = L"SELECT `Extension`, `Verbs`, `Executable`, `Attributes`, `Application_` FROM `IIsWebApplicationExtension`";

LPCWSTR vcsWebQuery = L"SELECT `Web`, `Component_`, `Id`, `Description`, `ConnectionTimeout`, `Directory_`, `State`, `Attributes`, `DirProperties_`, `Application_`, "
    L"`Address`, `IP`, `Port`, `Header`, `Secure`, `Log_` FROM `IIsWebSite`, `IIsWebAddress` "
    L"WHERE `KeyAddress_`=`Address` ORDER BY `Sequence`";

LPCWSTR vcsWebAddressQuery = L"SELECT `Address`, `Web_`, `IP`, `Port`, `Header`, `Secure` "
    L"FROM `IIsWebAddress`";

LPCWSTR vcsWebBaseQuery = L"SELECT `Web`, `Id`, `IP`, `Port`, `Header`, `Secure`, `Description` "
    L"FROM `IIsWebSite`, `IIsWebAddress` "
    L"WHERE `KeyAddress_`=`Address`";

LPCWSTR vcsWebDirQuery = L"SELECT `Web_`, `WebDir`, `Component_`, `Path`, `DirProperties_`, `Application_` "
    L"FROM `IIsWebDir`";

LPCWSTR vcsVDirQuery = L"SELECT `Web_`, `VirtualDir`, `Component_`, `Alias`, `Directory_`, `DirProperties_`, `Application_` "
    L"FROM `IIsWebVirtualDir`";

LPCWSTR vcsFilterQuery = L"SELECT `Web_`, `Name`, `Component_`, `Path`, `Description`, `Flags`, `LoadOrder` FROM `IIsFilter` ORDER BY `Web_`";

LPCWSTR vcsPropertyQuery = L"SELECT `Property`, `Component_`, `Attributes`, `Value` "
    L"FROM `IIsProperty`";

#define IIS7CONDITION L"VersionNT >= 600"
#define USEIIS7CONDITION IIS7CONDITION L"AND NOT UseIis6Compatibility"

/********************************************************************
DllMain - standard entry point for all WiX CustomActions

********************************************************************/
extern "C" BOOL WINAPI DllMain(
    __in HINSTANCE hInst,
    __in ULONG ulReason,
    __in LPVOID
    )
{
    switch (ulReason)
    {
    case DLL_PROCESS_ATTACH:
        WcaGlobalInitialize(hInst);
        break;

    case DLL_PROCESS_DETACH:
        WcaGlobalFinalize();
        break;
    }

    return TRUE;
}

/********************************************************************
ConfigureIIs - CUSTOM ACTION ENTRY POINT for installing IIs settings

********************************************************************/
extern "C" UINT __stdcall ConfigureIIs(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ConfigureIIs here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzBackupId = NULL;
    LPWSTR pwzCustomActionData = NULL; // CustomActionData for ConfigureIIs custom action

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureIIs");
    ExitOnFailure(hr, "Failed to initialize");

    // check for the prerequsite tables
    if (S_OK != WcaTableExists(L"IIsWebSite") && S_OK != WcaTableExists(L"IIsFilter") && S_OK != WcaTableExists(L"IIsProperty") && 
        S_OK != WcaTableExists(L"IIsWebServiceExtension") && S_OK != WcaTableExists(L"IIsAppPool"))
    {
        WcaLog(LOGMSG_VERBOSE, "skipping IIs CustomAction, no IIsWebSite table, no IIsFilter table, no IIsProperty table, no IIsWebServiceExtension, and no IIsAppPool table");
        ExitFunction1(hr = S_FALSE);
    }

    // Get a CaScript key
    hr = WcaCaScriptCreateKey(&pwzScriptKey);
    ExitOnFailure(hr, "Failed to get encoding key.");

    // Generate a unique string to be used for this product's transaction
    // This prevents a name collision when doing a major upgrade
    hr = WcaGetProperty(L"ProductCode", &pwzBackupId);
    ExitOnFailure(hr, "failed to get ProductCode");

    hr = StrAllocConcat(&pwzBackupId, L"ScaConfigureIIs", 0);
    ExitOnFailure(hr, "failed to concat ScaConfigureIIs");

    // make sure the operations below are wrapped in a "transaction"
    // use IIS7 transaction logic even if using Iis6 compat because Backup/Restore don't work with metabase compatibility
    if (MSICONDITION_TRUE == ::MsiEvaluateConditionW(hInstall, IIS7CONDITION))
    {
        hr = ScaIIS7ConfigTransaction(pwzBackupId);
        MessageExitOnFailure(hr, msierrIISFailedSchedTransaction, "failed to start IIS7 transaction");
    }
    else
    {
        hr = ScaMetabaseTransaction(pwzBackupId);
        MessageExitOnFailure(hr, msierrIISFailedSchedTransaction, "failed to start IIS transaction");
    }

    // Write the CaScript key to the ConfigureIIS custom action data
    hr = WcaWriteStringToCaData(pwzScriptKey, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add encoding key to CustomActionData.");

    // Wrap vcsUserDeferredQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"User"))
    {
        hr = WcaWrapQuery(vcsUserDeferredQuery, &pwzCustomActionData, efmcColumn3 | efmcColumn4 | efmcColumn5, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap User query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap User empty query");
    }

    // Wrap vcsWebSvcExtQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebServiceExtension"))
    {
        hr = WcaWrapQuery(vcsWebSvcExtQuery, &pwzCustomActionData, efmcColumn2 | efmcColumn3 | efmcColumn4, 1, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebServiceExtension query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebServiceExtension empty query");
    }

    // Wrap vcsAppPoolQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsAppPool"))
    {
        hr = WcaWrapQuery(vcsAppPoolQuery, &pwzCustomActionData, efmcColumn2 | efmcColumn15 | efmcColumn16, 3, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsAppPool query");

        hr = WcaWrapQuery(vcsComponentAttrQuery, &pwzCustomActionData, 0, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap Component query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsAppPool empty query");
    }

    // Wrap vcsMimeMapQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsMimeMap"))
    {
        hr = WcaWrapQuery(vcsMimeMapQuery, &pwzCustomActionData, efmcColumn4 | efmcColumn5, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsMimeMap query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsMimeMap empty query");
    }

    // Wrap vcsHttpHeaderQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsHttpHeader"))
    {
        hr = WcaWrapQuery(vcsHttpHeaderQuery, &pwzCustomActionData, efmcColumn1 | efmcColumn4, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsHttpHeader query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsHttpHeader empty query");
    }

    // Wrap vcsWebErrorQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebError"))
    {
        hr = WcaWrapQuery(vcsWebErrorQuery, &pwzCustomActionData, efmcColumn5 | efmcColumn6, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebError query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebError empty query");
    }

    // Wrap vcsWebDirPropertiesQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebDirProperties"))
    {
        hr = WcaWrapQuery(vcsWebDirPropertiesQuery, &pwzCustomActionData, efmcColumn8 | efmcColumn10 | efmcColumn12 | efmcColumn15, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebDirProperties query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebDirProperties empty query");
    }

    // Wrap vcsSslCertificateQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"Certificate") && S_OK == WcaTableExists(L"CertificateHash") && S_OK == WcaTableExists(L"IIsWebSiteCertificates"))
    {
        hr = WcaWrapQuery(vcsSslCertificateQuery, &pwzCustomActionData, 0, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap SslCertificate query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap SslCertificate empty query");
    }

    // Wrap vcsWebLogQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebLog"))
    {
        hr = WcaWrapQuery(vcsWebLogQuery, &pwzCustomActionData, efmcColumn2, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebLog query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebLog empty query");
    }

    // Wrap vcsWebApplicationQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebApplication"))
    {
        hr = WcaWrapQuery(vcsWebApplicationQuery, &pwzCustomActionData, efmcColumn1, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebApplication query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebApplication empty query");
    }

    // Wrap vcsWebAppExtensionQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebApplicationExtension"))
    {
        hr = WcaWrapQuery(vcsWebAppExtensionQuery, &pwzCustomActionData, efmcColumn2 | efmcColumn3, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebApplicationExtension query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebApplicationExtension empty query");
    }

    // Wrap vcsWebQuery, vcsWebAddressQuery, and vcsWebBaseQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebAddress") && S_OK == WcaTableExists(L"IIsWebSite"))
    {
        hr = WcaWrapQuery(vcsWebQuery, &pwzCustomActionData, efmcColumn3 | efmcColumn4 | efmcColumn12 | efmcColumn13 | efmcColumn14, 2, 6);
        ExitOnFailure(hr, "Failed to wrap IIsWebSite query");

        hr = WcaWrapQuery(vcsWebAddressQuery, &pwzCustomActionData, efmcColumn3 | efmcColumn4 | efmcColumn5, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebAddress query");

        hr = WcaWrapQuery(vcsWebBaseQuery, &pwzCustomActionData, efmcColumn2 | efmcColumn3 | efmcColumn4 | efmcColumn5 | efmcColumn7, 0xFFFFFFFF, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebBase query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebSite empty query");

        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebAddress empty query");

        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebBase empty query");
    }

    // Wrap vcsWebDirQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebDir"))
    {
        hr = WcaWrapQuery(vcsWebDirQuery, &pwzCustomActionData, efmcColumn4, 3, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsWebDir query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebDir empty query");
    }

    // Wrap vcsVDirQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsWebVirtualDir"))
    {
        hr = WcaWrapQuery(vcsVDirQuery, &pwzCustomActionData, efmcColumn4, 3, 5);
        ExitOnFailure(hr, "Failed to wrap IIsWebVirtualDir query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsWebVirtualDir empty query");
    }

    // Wrap vcsFilterQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsFilter"))
    {
        hr = WcaWrapQuery(vcsFilterQuery, &pwzCustomActionData, efmcColumn4 | efmcColumn5, 3, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsFilter query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsFilter empty query");
    }

    // Wrap vcsPropertyQuery to send to deferred CA
    if (S_OK == WcaTableExists(L"IIsProperty"))
    {
        hr = WcaWrapQuery(vcsPropertyQuery, &pwzCustomActionData, efmcColumn4, 2, 0xFFFFFFFF);
        ExitOnFailure(hr, "Failed to wrap IIsProperty query");
    }
    else
    {
        hr = WcaWrapEmptyQuery(&pwzCustomActionData);
        ExitOnFailure(hr, "Failed to wrap IIsProperty empty query");
    }

    if (MSICONDITION_TRUE == ::MsiEvaluateConditionW(hInstall, USEIIS7CONDITION))
    {
        // This must remain trace only, CA data may contain password
        WcaLog(LOGMSG_TRACEONLY, "Custom Action Data for ConfigureIIS7Exec will be: %ls", pwzCustomActionData);

        hr = WcaDoDeferredAction(L"ConfigureIIs7Exec", pwzCustomActionData, ConfigureIIsCost);
        ExitOnFailure(hr, "Failed to schedule ConfigureIIs7Exec custom action");

        ReleaseNullStr(pwzCustomActionData);

        // Write the CaScript key to the ConfigureIIS custom action data
        hr = WcaWriteStringToCaData(pwzScriptKey, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add script key to CustomActionData.");

        hr = WcaDoDeferredAction(L"WriteIIS7ConfigChanges", pwzCustomActionData, WriteIIS7ConfigChangesCost);
        ExitOnFailure(hr, "Failed to schedule WriteMetabaseChanges custom action");
    }
    else
    {
        // This must remain trace only, CA data may contain password
        WcaLog(LOGMSG_TRACEONLY, "Custom Action Data for ConfigureIISExec will be: %ls", pwzCustomActionData);

        hr = WcaDoDeferredAction(L"ConfigureIIsExec", pwzCustomActionData, ConfigureIIsCost);
        ExitOnFailure(hr, "Failed to schedule ConfigureIISExec custom action");

        ReleaseNullStr(pwzCustomActionData);

        // Write the CaScript key to the ConfigureIIS custom action data
        hr = WcaWriteStringToCaData(pwzScriptKey, &pwzCustomActionData);
        ExitOnFailure(hr, "Failed to add script key to CustomActionData.");

        hr = WcaDoDeferredAction(L"WriteMetabaseChanges", pwzCustomActionData, WriteMetabaseChangesCost);
        ExitOnFailure(hr, "Failed to schedule WriteMetabaseChanges custom action");
    }

LExit:
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzBackupId);
    ReleaseStr(pwzCustomActionData);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
ConfigureIIsExec - custom action for installing IIs settings - table
data will be wrapped and passed in from immediate CA
ReadIIsTables

********************************************************************/
extern "C" UINT __stdcall ConfigureIIsExec(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ConfigureIIsExec here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    BOOL fInitializedCom = FALSE;
    IMSAdminBase* piMetabase = NULL;

    SCA_WEB* pswList = NULL;
    SCA_WEBDIR* pswdList = NULL;
    SCA_VDIR* psvdList = NULL;
    SCA_FILTER* psfList = NULL;
    SCA_APPPOOL *psapList = NULL;
    SCA_MIMEMAP* psmmList = NULL;
    SCA_HTTP_HEADER* pshhList = NULL;
    SCA_PROPERTY *pspList = NULL;
    SCA_WEBSVCEXT* psWseList = NULL;
    SCA_WEB_ERROR* psweList = NULL;

    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzCustomActionData = NULL;

    WCA_WRAPQUERY_HANDLE hUserQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebBaseQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebDirPropQuery = NULL;
    WCA_WRAPQUERY_HANDLE hSslCertQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebLogQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebAppQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebAppExtQuery = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureIIsExec");
    ExitOnFailure(hr, "Failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // Get the CaScript key
    hr = WcaReadStringFromCaData(&pwzCustomActionData, &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CaScript key from custom action data");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    // if IIS was uninstalled (thus no IID_IMSAdminBase) allow the
    // user to still uninstall this package by clicking "Ignore"
    do
    {
        hr = ::CoCreateInstance(CLSID_MSAdminBase, NULL, CLSCTX_ALL, IID_IMSAdminBase, (void**)&piMetabase);
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_STANDARD, "failed to get IID_IMSAdminBase Object");
            er = WcaErrorMessage(msierrIISCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT:
                ExitFunction();   // bail with the error result from the CoCreate to kick off a rollback
            case IDRETRY:
                hr = S_FALSE;   // hit me, baby, one more time
                break;
            case IDIGNORE:
                __fallthrough;
            default:
                WcaLog(LOGMSG_STANDARD, "ignoring absent IIS");
                // We need to write the empty script to communicate to other deferred CA that there is noting to do.
                hr = ScaWriteConfigurationScript(pwzScriptKey);
                ExitOnFailure(hr, "failed to schedule metabase configuration");

                ExitFunction1(hr = S_OK);  // pretend everything is okay
                break;
            }
        }
    } while (S_FALSE == hr);

    // read the msi tables
    hr = WcaBeginUnwrapQuery(&hUserQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap user query");

    hr = ScaWebSvcExtRead(&psWseList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebSvcExt, "failed while processing WebServiceExtensions");

    hr = ScaAppPoolRead(&psapList, hUserQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadAppPool, "failed while processing WebAppPools");

    // MimeMap, Error and HttpHeader need to be read before the virtual directory and web read
    hr = ScaMimeMapRead(&psmmList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadMimeMap, "failed while processing MimeMaps");

    hr = ScaHttpHeaderRead(&pshhList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadHttpHeader, "failed while processing HttpHeaders");

    hr = ScaWebErrorRead(&psweList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebError, "failed while processing WebErrors");

    hr = WcaBeginUnwrapQuery(&hWebDirPropQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web dir properties query");

    hr = WcaBeginUnwrapQuery(&hSslCertQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap ssl certificate query");

    hr = WcaBeginUnwrapQuery(&hWebLogQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web log query");

    hr = WcaBeginUnwrapQuery(&hWebAppQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web application query");

    hr = WcaBeginUnwrapQuery(&hWebAppExtQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web application extension query");

    hr = ScaWebsRead(piMetabase, &psmmList, &pswList, &pshhList, &psweList, hUserQuery, hWebDirPropQuery, hSslCertQuery, hWebLogQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebSite, "failed while processing WebSites");

    hr = WcaBeginUnwrapQuery(&hWebBaseQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web base query");

    hr = ScaWebDirsRead(piMetabase, pswList, hUserQuery, hWebBaseQuery, hWebDirPropQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData, &pswdList);
    MessageExitOnFailure(hr, msierrIISFailedReadWebDirs, "failed while processing WebDirs");

    hr = ScaVirtualDirsRead(piMetabase, pswList, &psvdList, &psmmList, &pshhList, &psweList, hUserQuery, hWebBaseQuery, hWebDirPropQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadVDirs, "failed while processing WebVirtualDirs");

    hr = ScaFiltersRead(piMetabase, pswList, hWebBaseQuery, &psfList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadFilters, "failed while processing WebFilters");

    hr = ScaPropertyRead(&pspList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadProp, "failed while processing WebProperties");

    // do uninstall actions (order is important!)
    hr = ScaPropertyUninstall(piMetabase, pspList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallProp, "failed to uninstall IIS properties");

    hr = ScaFiltersUninstall(piMetabase, psfList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallFilters, "failed to schedule uninstall of filters");

    hr = ScaVirtualDirsUninstall(piMetabase, psvdList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallVDirs, "failed to schedule uninstall of virtual directories");

    hr = ScaWebDirsUninstall(piMetabase, pswdList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallWebDirs, "failed to schedule uninstall of web directories");

    hr = ScaWebsUninstall(piMetabase, pswList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallWebs, "failed to schedule uninstall of webs");

    hr = ScaAppPoolUninstall(piMetabase, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallAppPool, "failed to schedule uninstall of AppPools");


    // do install actions (order is important!)
    // ScaWebSvcExtCommit contains both uninstall and install actions.
    hr = ScaWebSvcExtCommit(piMetabase, psWseList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebSvcExt, "failed to schedule install/uninstall of WebSvcExt");

    hr = ScaAppPoolInstall(piMetabase, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallAppPool, "failed to schedule install of AppPools");

    hr = ScaWebsInstall(piMetabase, pswList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebs, "failed to schedule install of webs");

    hr = ScaWebDirsInstall(piMetabase, pswdList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebDirs, "failed to schedule install of web directories");

    hr = ScaVirtualDirsInstall(piMetabase, psvdList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallVDirs, "failed to schedule install of virtual directories");

    hr = ScaFiltersInstall(piMetabase, psfList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallFilters, "failed to schedule install of filters");

    hr = ScaPropertyInstall(piMetabase, pspList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallProp, "failed to schedule install of properties");

    hr = ScaWriteConfigurationScript(pwzScriptKey);
    ExitOnFailure(hr, "failed to schedule metabase configuration");

LExit:
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzCustomActionData);

    WcaFinishUnwrapQuery(hUserQuery);
    WcaFinishUnwrapQuery(hWebBaseQuery);
    WcaFinishUnwrapQuery(hWebDirPropQuery);
    WcaFinishUnwrapQuery(hSslCertQuery);
    WcaFinishUnwrapQuery(hWebLogQuery);
    WcaFinishUnwrapQuery(hWebAppQuery);
    WcaFinishUnwrapQuery(hWebAppExtQuery);

    if (psWseList)
    {
        ScaWebSvcExtFreeList(psWseList);
    }

    if (psfList)
    {
        ScaFiltersFreeList(psfList);
    }

    if (psvdList)
    {
        ScaVirtualDirsFreeList(psvdList);
    }

    if (pswdList)
    {
        ScaWebDirsFreeList(pswdList);
    }

    if (pswList)
    {
        ScaWebsFreeList(pswList);
    }

    if (psmmList)
    {
        ScaMimeMapCheckList(psmmList);
        ScaMimeMapFreeList(psmmList);
    }

    if (pshhList)
    {
        ScaHttpHeaderCheckList(pshhList);
        ScaHttpHeaderFreeList(pshhList);
    }

    if (psweList)
    {
        ScaWebErrorCheckList(psweList);
        ScaWebErrorFreeList(psweList);
    }

    ReleaseObject(piMetabase);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
ConfigureIIs - CUSTOM ACTION ENTRY POINT for installing IIs settings

********************************************************************/
extern "C" UINT __stdcall ConfigureIIs7Exec(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(FALSE, "debug ConfigureIIs7Exec here");
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzCustomActionData = NULL;

    SCA_WEB7* pswList = NULL;
    SCA_WEBDIR7* pswdList = NULL;
    SCA_VDIR7* psvdList = NULL;
    SCA_FILTER* psfList = NULL;
    SCA_APPPOOL *psapList = NULL;
    SCA_MIMEMAP* psmmList = NULL;
    SCA_HTTP_HEADER* pshhList = NULL;
    SCA_PROPERTY *pspList = NULL;
    SCA_WEBSVCEXT* psWseList = NULL;
    SCA_WEB_ERROR* psweList = NULL;

    WCA_WRAPQUERY_HANDLE hUserQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebBaseQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebDirPropQuery = NULL;
    WCA_WRAPQUERY_HANDLE hSslCertQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebLogQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebAppQuery = NULL;
    WCA_WRAPQUERY_HANDLE hWebAppExtQuery = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureIIs7Exec");
    ExitOnFailure(hr, "Failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    // Get the CaScript key
    hr = WcaReadStringFromCaData(&pwzCustomActionData, &pwzScriptKey);
    ExitOnFailure(hr, "Failed to get CaScript key from custom action data");

    // read the msi tables
    hr = WcaBeginUnwrapQuery(&hUserQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap user query");

    hr = ScaWebSvcExtRead(&psWseList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebSvcExt, "failed while processing WebServiceExtensions");

    hr = ScaAppPoolRead(&psapList, hUserQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadAppPool, "failed while processing WebAppPools");

    // MimeMap, Error and HttpHeader need to be read before the virtual directory and web read
    hr = ScaMimeMapRead(&psmmList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadMimeMap, "failed while processing MimeMaps");

    hr = ScaHttpHeaderRead(&pshhList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadHttpHeader, "failed while processing HttpHeaders");

    hr = ScaWebErrorRead(&psweList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebError, "failed while processing WebErrors");

    hr = WcaBeginUnwrapQuery(&hWebDirPropQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web dir properties query");

    hr = WcaBeginUnwrapQuery(&hSslCertQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap ssl certificate query");

    hr = WcaBeginUnwrapQuery(&hWebLogQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web log query");

    hr = WcaBeginUnwrapQuery(&hWebAppQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web application query");

    hr = WcaBeginUnwrapQuery(&hWebAppExtQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web application extension query");

    hr = ScaWebsRead7(&pswList, &pshhList, &psweList, hUserQuery, hWebDirPropQuery, hSslCertQuery, hWebLogQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadWebSite, "failed while processing WebSites");

    hr = WcaBeginUnwrapQuery(&hWebBaseQuery, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to unwrap web base query");

    hr = ScaWebDirsRead7(pswList, hUserQuery, hWebBaseQuery, hWebDirPropQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData, &pswdList);
    MessageExitOnFailure(hr, msierrIISFailedReadWebDirs, "failed while processing WebDirs");

    hr = ScaVirtualDirsRead7(pswList, &psvdList, &psmmList, &pshhList, &psweList, hUserQuery, hWebBaseQuery, hWebDirPropQuery, hWebAppQuery, hWebAppExtQuery, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadVDirs, "failed while processing WebVirtualDirs");

    hr = ScaFiltersRead7(pswList, hWebBaseQuery, &psfList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadFilters, "failed while processing WebFilters");

    hr = ScaPropertyRead(&pspList, &pwzCustomActionData);
    MessageExitOnFailure(hr, msierrIISFailedReadProp, "failed while processing WebProperties");

    // do uninstall actions (order is important!)
    hr = ScaPropertyUninstall7(pspList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallProp, "failed to uninstall IIS properties");

    hr = ScaFiltersUninstall7(psfList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallFilters, "failed to schedule uninstall of filters");

    hr = ScaVirtualDirsUninstall7(psvdList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallVDirs, "failed to schedule uninstall of virtual directories");

    hr = ScaWebDirsUninstall7(pswdList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallWebDirs, "failed to schedule uninstall of web directories");

    hr = ScaWebsUninstall7(pswList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallWebs, "failed to schedule uninstall of webs");

    hr = ScaAppPoolUninstall7(psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedUninstallAppPool, "failed to schedule uninstall of AppPools");


    // do install actions (order is important!)
    // ScaWebSvcExtCommit contains both uninstall and install actions.
    hr = ScaWebSvcExtCommit7(psWseList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebSvcExt, "failed to schedule install/uninstall of WebSvcExt");

    hr = ScaAppPoolInstall7(psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallAppPool, "failed to schedule install of AppPools");

    hr = ScaWebsInstall7(pswList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebs, "failed to schedule install of webs");

    hr = ScaWebDirsInstall7(pswdList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallWebDirs, "failed to schedule install of web directories");

    hr = ScaVirtualDirsInstall7(psvdList, psapList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallVDirs, "failed to schedule install of virtual directories");

    hr = ScaFiltersInstall7(psfList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallFilters, "failed to schedule install of filters");

    hr = ScaPropertyInstall7(pspList);
    MessageExitOnFailure(hr, msierrIISFailedSchedInstallProp, "failed to schedule install of properties");

    hr = ScaWriteConfigurationScript(pwzScriptKey);
    ExitOnFailure(hr, "failed to schedule metabase configuration");

LExit:
    ReleaseNullStr(pwzScriptKey);
    ReleaseNullStr(pwzCustomActionData);

    WcaFinishUnwrapQuery(hUserQuery);
    WcaFinishUnwrapQuery(hWebBaseQuery);
    WcaFinishUnwrapQuery(hWebDirPropQuery);
    WcaFinishUnwrapQuery(hSslCertQuery);
    WcaFinishUnwrapQuery(hWebLogQuery);
    WcaFinishUnwrapQuery(hWebAppQuery);
    WcaFinishUnwrapQuery(hWebAppExtQuery);

    if (psWseList)
    {
        ScaWebSvcExtFreeList(psWseList);
    }

    if (psfList)
    {
        ScaFiltersFreeList(psfList);
    }

    if (psvdList)
    {
        ScaVirtualDirsFreeList7(psvdList);
    }

    if (pswdList)
    {
        ScaWebDirsFreeList7(pswdList);
    }

    if (pswList)
    {
        ScaWebsFreeList7(pswList);
    }

    if (psmmList)
    {
        ScaMimeMapFreeList(psmmList);
    }

    if (pshhList)
    {
        ScaHttpHeaderCheckList(pshhList);
        ScaHttpHeaderFreeList(pshhList);
    }

    if (psweList)
    {
        ScaWebErrorCheckList(psweList);
        ScaWebErrorFreeList(psweList);
    }

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}

/********************************************************************
ConfigureSmb - CUSTOM ACTION ENTRY POINT for installing fileshare settings

********************************************************************/
extern "C" UINT __stdcall ConfigureSmbInstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    SCA_SMB* pssList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureSmbInstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (WcaTableExists(L"FileShare") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping SMB CustomAction, no FileShare table");
        ExitFunction1(hr = S_FALSE);
    }

    hr = ScaSmbRead(&pssList);
    ExitOnFailure(hr, "failed to read FileShare table");

    hr = ScaSmbInstall(pssList);
    ExitOnFailure(hr, "failed to install FileShares");

LExit:
    if (pssList)
        ScaSmbFreeList(pssList);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
ConfigureSmb - CUSTOM ACTION ENTRY POINT for installing fileshare settings

********************************************************************/
extern "C" UINT __stdcall ConfigureSmbUninstall(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    SCA_SMB* pssList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureSmbUninstall");
    ExitOnFailure(hr, "Failed to initialize");

    // check to see if necessary tables are specified
    if (WcaTableExists(L"FileShare") != S_OK)
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping SMB CustomAction, no FileShare table");
        ExitFunction1(hr = S_FALSE);
    }

    hr = ScaSmbRead(&pssList);
    ExitOnFailure(hr, "failed to read FileShare table");

    hr = ScaSmbUninstall(pssList);
    ExitOnFailure(hr, "failed to uninstall FileShares");

LExit:
    if (pssList)
        ScaSmbFreeList(pssList);

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
ConfigureUsers - CUSTOM ACTION ENTRY POINT for installing users

********************************************************************/
extern "C" UINT __stdcall ConfigureUsers(
    __in MSIHANDLE hInstall
    )
{
    //AssertSz(0, "Debug ConfigureUsers");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    BOOL fInitializedCom = FALSE;
    SCA_USER* psuList = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "ConfigureUsers");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = ScaUserRead(&psuList);
    ExitOnFailure(hr, "failed to read User table");

    hr = ScaUserExecute(psuList);
    ExitOnFailure(hr, "failed to add/remove User actions");

LExit:
    if (psuList)
    {
        ScaUserFreeList(psuList);
    }

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}
