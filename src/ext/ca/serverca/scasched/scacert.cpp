// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT ConfigureCertificates(
    __in SCA_ACTION saAction
    );

static LPCWSTR StoreMapping(
    __in int iStore
    );

static HRESULT FindExistingCertificate(
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStore,
    __out BYTE** prgbCertificate,
    __out DWORD* pcbCertificate
    );

static HRESULT ResolveCertificate(
    __in LPCWSTR wzId,
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStoreName,
    __in DWORD dwAttributess,
    __in LPCWSTR wzData,
    __in LPCWSTR wzPFXPassword,
    __out BYTE** ppbCertificate,
    __out DWORD* pcbCertificate
    );

static HRESULT ReadCertificateFile(
    __in LPCWSTR wzPath,
    __out BYTE** prgbData,
    __out DWORD* pcbData
    );

static HRESULT CertificateToHash(
    __in BYTE* pbCertificate,
    __in DWORD cbCertificate,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzPFXPassword,
    __in BYTE rgbHash[],
    __in DWORD cbHash
    );

/*
HRESULT ScaGetCertificateByPath(LPCWSTR pwzName, BOOL fIsInstalling,
                                BOOL fIsUninstalling, INT iStore,
                                INT iStoreLocation, LPCWSTR wzSslCertificate,
                                LPCWSTR wzPFXPassword, BSTR* pbstrCertificate,
                                DWORD* pcbCertificate, BYTE* pbaHashBuffer);

HRESULT ScaGetCertificateByRequest(LPCWSTR pwzName, BOOL fIsInstalling,
                                   BOOL fIsUninstalling, INT iStore,
                                   INT iStoreLocation, LPCWSTR wzDistinguishedName,
                                   LPCWSTR wzCA, BSTR* pbstrCertificate,
                                   DWORD* pcbCertificate, BYTE* pbaHashBuffer);

HRESULT ScaSslNewCertificate(LPCWSTR pwzName, INT iStore,
                            INT iStoreLocation, LPCWSTR wzComputerName,
                            LPCWSTR wzDistinguishedName, LPCWSTR wzCertificateAuthorityOrig,
                            BSTR* pbstrCertificate, DWORD* pcbCertificate,
                            BYTE* pbaHashBuffer);

HRESULT ScaSslExistingCertificateByName(LPCWSTR pwzName, INT iStore,
                                       INT iStoreLocation, BSTR* pbstrCertificate,
                                       DWORD* pcbCertificate, BYTE* pbaHashBuffer);

HRESULT ScaSslExistingCertificateByBinaryData(INT iStore, INT iStoreLocation,
                                             BYTE* pwzData, DWORD cchData);

HRESULT CreateEnroll(ICEnroll2 **hEnroll, INT iStore,
                     INT iStoreLocation);

HRESULT RequestCertificate(LPCWSTR pwzName, INT iStore,
                           INT iStoreLocation, LPCWSTR wzComputerName,
                           LPCWSTR wzDistinguishedName, LPCWSTR wzCertificateAuthority,
                           BSTR *pbstrCertificate);

VOID ParseCertificateAuthority(__in LPCWSTR wzCertificateAuthorityOrig, __out LPWSTR *pwzBuffer,
                               __out LPWSTR **hwzCAArray, __out int *piCAArray);
*/


LPCWSTR vcsCertQuery = L"SELECT `Certificate`, `Name`, `Component_`, `StoreLocation`, `StoreName`, `Attributes`, `Binary_`, `CertificatePath`, `PFXPassword` FROM `Certificate`";
enum eCertQuery { cqCertificate = 1, cqName, cqComponent, cqStoreLocation, cqStoreName, cqAttributes, cqCertificateBinary, cqCertificatePath, cqPFXPassword };


/********************************************************************
InstallCertificates - CUSTOM ACTION ENTRY POINT for installing
                      certificates

********************************************************************/
extern "C" UINT __stdcall InstallCertificates(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "InstallCertificates");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ConfigureCertificates(SCA_ACTION_INSTALL);

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/********************************************************************
UninstallCertificates - CUSTOM ACTION ENTRY POINT for uninstalling
                        certificates

********************************************************************/
extern "C" UINT __stdcall UninstallCertificates(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    // initialize
    hr = WcaInitialize(hInstall, "UninstallCertificates");
    ExitOnFailure(hr, "Failed to initialize");

    hr = ConfigureCertificates(SCA_ACTION_UNINSTALL);

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


static HRESULT ConfigureCertificates(
    __in SCA_ACTION saAction
    )
{
    //AssertSz(FALSE, "debug ConfigureCertificates().");

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    PMSIHANDLE hViewCertificate;
    PMSIHANDLE hRecCertificate;
    INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
    INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;

    WCHAR* pwzId = NULL;
    WCHAR* pwzName = NULL;
    WCHAR* pwzComponent = NULL;
    int iData = 0;
    DWORD dwStoreLocation = 0;
    LPWSTR pwzStoreName = 0;
    DWORD dwAttributes = 0;
    WCHAR* pwzData = NULL;
    WCHAR* pwzPFXPassword = NULL;
    WCHAR* pwzCaData = NULL;
    WCHAR* pwzRollbackCaData = NULL;

    BYTE* pbCertificate = NULL;
    DWORD cbCertificate = 0;
    DWORD_PTR cbPFXPassword = 0;

    // Bail quickly if the Certificate table isn't around.
    if (S_OK != WcaTableExists(L"Certificate"))
    {
        WcaLog(LOGMSG_VERBOSE, "Skipping ConfigureCertificates() - required table not present.");
        ExitFunction1(hr = S_FALSE);
    }

    // Process the Certificate table.
    hr = WcaOpenExecuteView(vcsCertQuery, &hViewCertificate);
    ExitOnFailure(hr, "failed to open view on Certificate table");

    while (SUCCEEDED(hr = WcaFetchRecord(hViewCertificate, &hRecCertificate)))
    {
        hr = WcaGetRecordString(hRecCertificate, cqCertificate, &pwzId); // the id is just useful to have up front
        ExitOnFailure(hr, "failed to get Certificate.Certificate");

        hr = WcaGetRecordString(hRecCertificate, cqComponent, &pwzComponent);
        ExitOnFailure(hr, "failed to get Certificate.Component_");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzComponent, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure1(hr, "failed to get state for component: %ls", pwzComponent);

        if (!(WcaIsInstalling(isInstalled, isAction) && SCA_ACTION_INSTALL == saAction) &&
            !(WcaIsUninstalling(isInstalled, isAction) && SCA_ACTION_UNINSTALL == saAction) &&
            !(WcaIsReInstalling(isInstalled, isAction)))
        {
            WcaLog(LOGMSG_VERBOSE, "Skipping non-action certificate: %ls", pwzId);
            continue;
        }

        // extract the rest of the data from the Certificate table
        hr = WcaGetRecordFormattedString(hRecCertificate, cqName, &pwzName);
        ExitOnFailure(hr, "failed to get Certificate.Name");

        hr = WcaGetRecordInteger(hRecCertificate, cqStoreLocation, &iData);
        ExitOnFailure(hr, "failed to get Certificate.StoreLocation");

        switch (iData)
        {
        case SCA_CERTSYSTEMSTORE_CURRENTUSER:
            dwStoreLocation = CERT_SYSTEM_STORE_CURRENT_USER;
            break;
        case SCA_CERTSYSTEMSTORE_LOCALMACHINE:
            dwStoreLocation = CERT_SYSTEM_STORE_LOCAL_MACHINE;
            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure1(hr, "Invalid store location value: %d", iData);
        }

        hr = WcaGetRecordString(hRecCertificate, cqStoreName, &pwzStoreName);
        ExitOnFailure(hr, "failed to get Certificate.StoreName");

        hr = WcaGetRecordInteger(hRecCertificate, cqAttributes, reinterpret_cast<int*>(&dwAttributes));
        ExitOnFailure(hr, "failed to get Certificate.Attributes");

        if (dwAttributes & SCA_CERT_ATTRIBUTE_BINARYDATA)
        {
            hr = WcaGetRecordString(hRecCertificate, cqCertificateBinary, &pwzData);
            ExitOnFailure(hr, "failed to get Certificate.Binary_");
        }
        else
        {
            hr = WcaGetRecordFormattedString(hRecCertificate, cqCertificatePath, &pwzData);
            ExitOnFailure(hr, "failed to get Certificate.CertificatePath");
        }

        hr = WcaGetRecordFormattedString(hRecCertificate, cqPFXPassword, &pwzPFXPassword);
        ExitOnFailure(hr, "failed to get Certificate.PFXPassword");

        // Write the common data (for both install and uninstall) to the CustomActionData
        // to pass data to the deferred CustomAction.
        hr = StrAllocString(&pwzCaData, pwzName, 0);
        ExitOnFailure(hr, "Failed to pass Certificate.Certificate to deferred CustomAction.");
        hr = WcaWriteStringToCaData(pwzStoreName, &pwzCaData);
        ExitOnFailure(hr, "Failed to pass Certificate.StoreName to deferred CustomAction.");
        hr = WcaWriteIntegerToCaData(SCA_CERT_ATTRIBUTE_BINARYDATA, &pwzCaData);
        ExitOnFailure(hr, "Failed to pass Certificate.Attributes to deferred CustomAction.");

        // Copy the rollback data from the deferred data because it's the same up to this point.
        hr = StrAllocString(&pwzRollbackCaData, pwzCaData, 0);
        ExitOnFailure(hr, "Failed to allocate string for rollback CustomAction.");

        // Finally, schedule the correct deferred CustomAction to actually do work.
        LPCWSTR wzAction = NULL;
        LPCWSTR wzRollbackAction = NULL;
        DWORD dwCost = 0;
        if (SCA_ACTION_UNINSTALL == saAction)
        {
            // Find an existing certificate one (if there is one) to so we have it for rollback.
            hr = FindExistingCertificate(pwzName, dwStoreLocation, pwzStoreName, &pbCertificate, &cbCertificate);
            ExitOnFailure1(hr, "Failed to search for existing certificate with friendly name: %ls", pwzName);

            if (pbCertificate)
            {
                hr = WcaWriteStreamToCaData(pbCertificate, cbCertificate, &pwzRollbackCaData);
                ExitOnFailure(hr, "Failed to pass Certificate.Data to rollback CustomAction.");

                hr = WcaWriteStringToCaData(pwzPFXPassword, &pwzRollbackCaData);
                ExitOnFailure(hr, "Failed to pass Certificate.PFXPassword to rollback CustomAction.");
            }

            // Pick the right action to run based on what store we're uninstalling from.
            if (CERT_SYSTEM_STORE_LOCAL_MACHINE == dwStoreLocation)
            {
                wzAction = L"DeleteMachineCertificate";
                if (pbCertificate)
                {
                    wzRollbackAction = L"RollbackDeleteMachineCertificate";
                }
            }
            else
            {
                wzAction = L"DeleteUserCertificate";
                if (pbCertificate)
                {
                    wzRollbackAction = L"RollbackDeleteUserCertificate";
                }
            }
            dwCost = COST_CERT_DELETE;
        }
        else
        {
            // Actually get the certificate, resolve it to a blob, and get the blob's hash.
            hr = ResolveCertificate(pwzId, pwzName, dwStoreLocation, pwzStoreName, dwAttributes, pwzData, pwzPFXPassword, &pbCertificate, &cbCertificate);
            ExitOnFailure1(hr, "Failed to resolve certificate: %ls", pwzId);

            hr = WcaWriteStreamToCaData(pbCertificate, cbCertificate, &pwzCaData);
            ExitOnFailure(hr, "Failed to pass Certificate.Data to deferred CustomAction.");

            hr = WcaWriteStringToCaData(pwzPFXPassword, &pwzCaData);
            ExitOnFailure(hr, "Failed to pass Certificate.PFXPassword to deferred CustomAction.");

            // Pick the right action to run based on what store we're installing into.
            if (CERT_SYSTEM_STORE_LOCAL_MACHINE == dwStoreLocation)
            {
                wzAction = L"AddMachineCertificate";
                wzRollbackAction = L"RollbackAddMachineCertificate";
            }
            else
            {
                wzAction = L"AddUserCertificate";
                wzRollbackAction = L"RollbackAddUserCertificate";
            }
            dwCost = COST_CERT_ADD;
        }

        if (wzRollbackAction)
        {
            hr = WcaDoDeferredAction(wzRollbackAction, pwzRollbackCaData, dwCost);
            ExitOnFailure2(hr, "Failed to schedule rollback certificate action '%ls' for: %ls", wzRollbackAction, pwzId);
        }

        hr = WcaDoDeferredAction(wzAction, pwzCaData, dwCost);
        ExitOnFailure2(hr, "Failed to schedule certificate action '%ls' for: %ls", wzAction, pwzId);

        // Clean up for the next certificate.
        ReleaseNullMem(pbCertificate);
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }

LExit:
    if (NULL != pwzPFXPassword && SUCCEEDED(StrSize(pwzPFXPassword, &cbPFXPassword)))
    {
        SecureZeroMemory(pwzPFXPassword, cbPFXPassword);
    }

    ReleaseMem(pbCertificate);
    ReleaseStr(pwzCaData);
    ReleaseStr(pwzPFXPassword);
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzStoreName);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzId);

    return hr;
}


static HRESULT ResolveCertificate(
    __in LPCWSTR wzId,
    __in LPCWSTR /*wzName*/,
    __in DWORD dwStoreLocation,
    __in LPCWSTR /*wzStoreName*/,
    __in DWORD dwAttributes,
    __in LPCWSTR wzData,
    __in LPCWSTR wzPFXPassword,
    __out BYTE** ppbCertificate,
    __out DWORD* pcbCertificate
    )
{
    HRESULT hr = S_OK;

    LPWSTR pwzSql = NULL;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    MSIHANDLE hCertificateHashView = NULL;
    MSIHANDLE hCertificateHashColumns = NULL;

    BYTE rgbCertificateHash[CB_CERTIFICATE_HASH] = { 0 };
    WCHAR wzEncodedCertificateHash[CB_CERTIFICATE_HASH * 2 + 1] = { 0 };

    PMSIHANDLE hViewCertificateRequest, hRecCertificateRequest;

    WCHAR* pwzDistinguishedName = NULL;
    WCHAR* pwzCA = NULL;

    BYTE* pbData = NULL;
    DWORD cbData = 0;

    if (dwAttributes & SCA_CERT_ATTRIBUTE_REQUEST)
    {
        hr = E_NOTIMPL;
        ExitOnFailure(hr, "Installing certificates by requesting them from a certificate authority is not currently supported");
        //if (dwAttributes & SCA_CERT_ATTRIBUTE_OVERWRITE)
        //{
        //    // try to overwrite with the patch to a cert file
        //    WcaLog(LOGMSG_VERBOSE, "ConfigureCertificates - Overwrite with SSLCERTIFICATE");
        //    hr = ScaGetCertificateByPath(pwzName, fIsInstalling, fIsUninstalling,
        //        iStore, iStoreLocation, pwzData, wzPFXPassword, pbstrCertificate, pcbCertificate, pbaHashBuffer);
        //}
        //if (hr != S_OK)
        //{
        //    if (fIsUninstalling && !fIsInstalling)
        //    {
        //        // for uninstall, we just want to find the existing certificate
        //        hr = ScaSslExistingCertificateByName(pwzName, iStore, iStoreLocation, pbstrCertificate, pcbCertificate, pbaHashBuffer);
        //        ExitOnFailure(hr, "Failed Retrieving existing certificate during uninstall");
        //        // ok if no existing cert
        //        if (S_OK != hr)
        //            hr = S_OK;
        //    }
        //    else
        //    {
        //        // still no certificate
        //        // user has request this certificate, try to locate DistinguishedName and CA
        //        hr = WcaTableExists(L"CertificateRequest");
        //        ExitOnFailure(hr, "CertificateRequest is referenced but not found");
        //        WcaLog(LOGMSG_VERBOSE, "ConfigureCertificates - CertificateRequest table present");
        //        cchSQLView = 255 + lstrlenW(pwzName);
        //        pwzSQLView = new WCHAR[cchSQLView];
        //        if (pwzSQLView)
        //        {
        //            hr = ::StringCchPrintfW(pwzSQLView, cchSQLView, L"SELECT `DistinguishedName`, `CA` FROM `CertificateRequest` WHERE `Certificate_`=\'%s\'", pwzName);
        //            ExitOnFailure(hr, "::StringCchPrintfW failed");
        //            hr = WcaOpenExecuteView(pwzSQLView, &hViewCertificateRequest);
        //            ExitOnFailure(hr, "failed to open view on CertificateRequest table");
        //            hr = WcaFetchSingleRecord(hViewCertificateRequest, &hRecCertificateRequest);
        //            ExitOnFailure(hr, "failed to retrieve request from CertificateRequest table");
        //            hr = WcaGetRecordString(hRecCertificateRequest,  1, &pwzDistinguishedName);
        //            ExitOnFailure(hr, "failed to get DistinguishedName");
        //            hr = WcaGetRecordString(hRecCertificateRequest,  2, &pwzCA);
        //            ExitOnFailure(hr, "failed to get CA");
        //            if (pwzDistinguishedName && pwzCA && *pwzDistinguishedName && *pwzCA)
        //            {
        //                hr = ScaGetCertificateByRequest(pwzName, fIsInstalling, fIsUninstalling, iStore, iStoreLocation, pwzDistinguishedName, pwzCA, pbstrCertificate, pcbCertificate, pbaHashBuffer);
        //            }
        //            else
        //            {
        //                hr = E_FAIL;
        //                ExitOnFailure(hr, "CertificateRequest entry is empty");
        //            }
        //        }
        //        else
        //        {
        //            hr = E_FAIL;
        //            ExitOnFailure(hr, "Out of memory");
        //        }
        //    }
        //}
    }
    else if (dwAttributes & SCA_CERT_ATTRIBUTE_BINARYDATA)
    {
        // get the binary stream in Binary
        hr = WcaTableExists(L"Binary");
        if (S_OK != hr)
        {
            if (SUCCEEDED(hr))
            {
                hr = E_UNEXPECTED;
            }
            ExitOnFailure(hr, "Binary was referenced but there is no Binary table.");
        }

        hr = StrAllocFormatted(&pwzSql, L"SELECT `Data` FROM `Binary` WHERE `Name`=\'%s\'", wzData);
        ExitOnFailure(hr, "Failed to allocate Binary table query.");

        hr = WcaOpenExecuteView(pwzSql, &hView);
        ExitOnFailure(hr, "Failed to open view on Binary table");

        hr = WcaFetchSingleRecord(hView, &hRec);
        ExitOnFailure(hr, "Failed to retrieve request from Binary table");

        hr = WcaGetRecordStream(hRec, 1, &pbData, &cbData);
        ExitOnFailure(hr, "Failed to ready Binary.Data for certificate.");
    }
    else if (dwAttributes == SCA_CERT_ATTRIBUTE_DEFAULT)
    {
        hr = ReadCertificateFile(wzData, &pbData, &cbData);
        ExitOnFailure(hr, "Failed to read certificate from file path.");
    }
    else
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid Certificate.Attributes.");
    }

    // If we have loaded a certificate, update the Certificate.Hash column.
    if (pbData)
    {
        hr = CertificateToHash(pbData, cbData, dwStoreLocation, wzPFXPassword, rgbCertificateHash, countof(rgbCertificateHash));
        ExitOnFailure(hr, "Failed to get SHA1 hash of certificate.");

        hr = StrHexEncode(rgbCertificateHash, countof(rgbCertificateHash), wzEncodedCertificateHash, countof(wzEncodedCertificateHash));
        ExitOnFailure(hr, "Failed to hex encode SHA1 hash of certificate.");

        // Update the CertificateHash table.
        hr = WcaAddTempRecord(&hCertificateHashView, &hCertificateHashColumns, L"CertificateHash", NULL, 0, 2, wzId, wzEncodedCertificateHash);
        ExitOnFailure1(hr, "Failed to add encoded has for certificate: %ls", wzId);
    }

    *ppbCertificate = pbData;
    *pcbCertificate = cbData;
    pbData = NULL;

LExit:
    if (hCertificateHashColumns)
    {
        ::MsiCloseHandle(hCertificateHashColumns);
    }

    if (hCertificateHashView)
    {
        ::MsiCloseHandle(hCertificateHashView);
    }

    ReleaseStr(pwzDistinguishedName);
    ReleaseStr(pwzCA);
    ReleaseMem(pbData);
    ReleaseStr(pwzSql);

    return hr;
}


static HRESULT ReadCertificateFile(
    __in LPCWSTR wzPath,
    __out BYTE** prgbData,
    __out DWORD* pcbData
    )
{
    HRESULT hr = S_OK;

    PCCERT_CONTEXT pCertContext = NULL;
    DWORD dwContentType;
    BYTE* pbData = NULL;
    DWORD cbData = 0;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_FILE, reinterpret_cast<LPCVOID>(wzPath), CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, NULL, &dwContentType, NULL, NULL, NULL, (LPCVOID*)&pCertContext))
    {
        ExitOnFailure1(hr, "Failed to read certificate from file: %ls", wzPath);
    }

    if (pCertContext)
    {
        cbData = pCertContext->cbCertEncoded;
        pbData = static_cast<BYTE*>(MemAlloc(cbData, FALSE));
        ExitOnNull1(pbData, hr, E_OUTOFMEMORY, "Failed to allocate memory to read certificate from file: %ls", wzPath);

        CopyMemory(pbData, pCertContext->pbCertEncoded, pCertContext->cbCertEncoded);
    }
    else
    {
        // If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            hr = FileRead(&pbData, &cbData, wzPath);
            ExitOnFailure1(hr, "Failed to read PFX file: %ls", wzPath);
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected certificate type read from disk.");
        }
    }

    *pcbData = cbData;
    *prgbData = pbData;
    pbData = NULL;

LExit:
    ReleaseMem(pbData);
    return hr;
}


static HRESULT CertificateToHash(
    __in BYTE* pbCertificate,
    __in DWORD cbCertificate,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzPFXPassword,
    __in BYTE rgbHash[],
    __in DWORD cbHash
    )
{
    HRESULT hr = S_OK;

    HCERTSTORE hPfxCertStore = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    PCCERT_CONTEXT pCertContextEnum = NULL;
    CRYPT_DATA_BLOB blob = { 0 };
    CRYPT_KEY_PROV_INFO* pPfxInfo = NULL;
    DWORD dwKeyset = (CERT_SYSTEM_STORE_CURRENT_USER == dwStoreLocation) ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET;
    DWORD dwEncodingType;
    DWORD dwContentType;
    DWORD dwFormatType;

    blob.pbData = pbCertificate;
    blob.cbData = cbCertificate;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertContext))
    {
        ExitWithLastError(hr, "Failed to process certificate as a valid certificate.");
    }

    if (!pCertContext)
    {
        // If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            // If we fail and our password is blank, also try passing in NULL for the password (according to the docs)
            hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, wzPFXPassword, dwKeyset);
            if (NULL == hPfxCertStore && !*wzPFXPassword)
            {
                hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, NULL, dwKeyset);
            }
            ExitOnNullWithLastError(hPfxCertStore, hr, "Failed to open PFX file.");

            // Find the first cert with a private key, or just use the last one
            for (pCertContextEnum = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContextEnum);
                 pCertContextEnum;
                 pCertContextEnum = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContextEnum))
            {
                pCertContext = pCertContextEnum;

                if (pCertContext && CertHasPrivateKey(pCertContext, NULL))
                {
                    break;
                }
            }

            ExitOnNullWithLastError(pCertContext, hr, "Failed to read first certificate out of PFX file.");

            // Ignore failures, the worst that happens is some parts of the PFX get left behind.
            CertReadProperty(pCertContext, CERT_KEY_PROV_INFO_PROP_ID, &pPfxInfo, NULL);
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected certificate type processed.");
        }
    }

    DWORD cb = cbHash;
    if (!::CertGetCertificateContextProperty(pCertContext, CERT_SHA1_HASH_PROP_ID, static_cast<LPVOID>(rgbHash), &cb))
    {
        ExitWithLastError(hr, "Failed to get certificate SHA1 hash property.");
    }
    AssertSz(cb == cbHash, "Did not correctly read certificate SHA1 hash.");

LExit:
    if (pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    if (hPfxCertStore)
    {
        ::CertCloseStore(hPfxCertStore, 0);
    }

    if (pPfxInfo)
    {
        HCRYPTPROV hProvIgnored = NULL; // ignored on deletes.
        ::CryptAcquireContextW(&hProvIgnored, pPfxInfo->pwszContainerName, pPfxInfo->pwszProvName, pPfxInfo->dwProvType, dwKeyset | CRYPT_DELETEKEYSET | CRYPT_SILENT);

        MemFree(pPfxInfo);
    }

    return hr;
}


static HRESULT FindExistingCertificate(
    __in LPCWSTR wzName,
    __in DWORD dwStoreLocation,
    __in LPCWSTR wzStore,
    __out BYTE** prgbCertificate,
    __out DWORD* pcbCertificate
    )
{
    HRESULT hr = S_OK;
    HCERTSTORE hCertStore = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    BYTE* pbCertificate = NULL;
    DWORD cbCertificate = 0;

    hCertStore = ::CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwStoreLocation | CERT_STORE_READONLY_FLAG, wzStore);
    MessageExitOnNullWithLastError(hCertStore, hr, msierrCERTFailedOpen, "Failed to open certificate store.");

    // Loop through the certificate, looking for certificates that match our friendly name.
    pCertContext = CertFindCertificateInStore(hCertStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, NULL);
    while (pCertContext)
    {
        WCHAR wzFriendlyName[256] = { 0 };
        DWORD cbFriendlyName = sizeof(wzFriendlyName);

        if (::CertGetCertificateContextProperty(pCertContext, CERT_FRIENDLY_NAME_PROP_ID, reinterpret_cast<BYTE*>(wzFriendlyName), &cbFriendlyName) &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, 0, wzName, -1, wzFriendlyName, -1))
        {
            // If the certificate with matching friendly name is valid, let's use that.
            long lVerify = ::CertVerifyTimeValidity(NULL, pCertContext->pCertInfo);
            if (0 == lVerify)
            {
                cbCertificate = pCertContext->cbCertEncoded;
                pbCertificate = static_cast<BYTE*>(MemAlloc(cbCertificate, FALSE));
                ExitOnNull(pbCertificate, hr, E_OUTOFMEMORY, "Failed to allocate memory to copy out exist certificate.");

                CopyMemory(pbCertificate, pCertContext->pbCertEncoded, cbCertificate);
                break; // found a matching certificate, no more searching necessary
            }
        }

         // Next certificate in the store.
        PCCERT_CONTEXT pNext = ::CertFindCertificateInStore(hCertStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, pCertContext);
        // old pCertContext is freed by CertFindCertificateInStore
        pCertContext = pNext;
    }

    *prgbCertificate = pbCertificate;
    *pcbCertificate = cbCertificate;
    pbCertificate = NULL;

LExit:
    ReleaseMem(pbCertificate);

    if (pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    if (hCertStore)
    {
        ::CertCloseStore(hCertStore, 0);
    }

    return hr;
}

/*
HRESULT CreateEnroll(ICEnroll2 **hEnroll, INT iStore, INT iStoreLocation)
{
    ICEnroll2 *pEnroll = NULL;
    HRESULT hr = S_OK;
    LONG lFlags;
    DWORD dwFlags = iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT;

    // create IEntroll
    hr = CoCreateInstance( CLSID_CEnroll, NULL, CLSCTX_INPROC_SERVER, IID_ICEnroll2, (void **)&pEnroll );
    if (FAILED(hr))
        return hr;

    switch (iStore)
    {
    case SCA_CERT_STORENAME_MY:
        pEnroll->get_MyStoreFlags(&lFlags);
        lFlags &= ~CERT_SYSTEM_STORE_LOCATION_MASK;
        lFlags |= dwFlags;
        // following call will change Request store flags also
        pEnroll->put_MyStoreFlags(lFlags);
        break;
    case SCA_CERT_STORENAME_CA:
        pEnroll->get_CAStoreFlags(&lFlags);
        lFlags &= ~CERT_SYSTEM_STORE_LOCATION_MASK;
        lFlags |= dwFlags;
        // following call will change Request store flags also
        pEnroll->put_CAStoreFlags(lFlags);
        break;
    case SCA_CERT_STORENAME_REQUEST:
        pEnroll->get_RequestStoreFlags(&lFlags);
        lFlags &= ~CERT_SYSTEM_STORE_LOCATION_MASK;
        lFlags |= dwFlags;
        // following call will change Request store flags also
        pEnroll->put_RequestStoreFlags(lFlags);
        break;
    case SCA_CERT_STORENAME_ROOT:
        pEnroll->get_RootStoreFlags(&lFlags);
        lFlags &= ~CERT_SYSTEM_STORE_LOCATION_MASK;
        lFlags |= dwFlags;
        // following call will change Request store flags also
        pEnroll->put_RootStoreFlags(lFlags);
        break;
    default:
        hr = E_FAIL;
        return hr;
    }

    pEnroll->get_GenKeyFlags(&lFlags);
    lFlags |= CRYPT_EXPORTABLE;
    pEnroll->put_GenKeyFlags(lFlags);

    pEnroll->put_KeySpec(AT_KEYEXCHANGE);
    pEnroll->put_ProviderType(PROV_RSA_SCHANNEL);
    pEnroll->put_DeleteRequestCert(TRUE);

    *hEnroll = pEnroll;
    return hr;
}


HRESULT RequestCertificate(LPCWSTR pwzName, INT iStore, INT iStoreLocation,
                           LPCWSTR wzComputerName, LPCWSTR wzDistinguishedName, LPCWSTR wzCertificateAuthority,
                           BSTR *pbstrCertificate)
{
    if (pbstrCertificate == NULL)
        return E_INVALIDARG;

    HRESULT hr;
    ICEnroll2 *pEnroll = NULL;
    ICertRequest *pCertRequest = NULL;
    BSTR bstrRequest = NULL;
    LONG nDisposition;

    BSTR bstrCertificateUsage = NULL;
    BSTR bstrCertificateAttributes = NULL;
    BSTR bstrCertificateAuthority = NULL;

    // equivalent to: sprintf(bstrDistinguishedName, L"%s,CN=%s", wzDistinguishedName, wzComputerName);
    DWORD cchComputerName = lstrlenW(wzComputerName);
    DWORD cchDistinguishedName = lstrlenW(wzDistinguishedName);
    CONST DWORD cchbstrDistinguishedName = 5 + cchComputerName + cchDistinguishedName;
    BSTR bstrDistinguishedName = SysAllocStringLen(NULL, cchbstrDistinguishedName);
    ExitOnNull(bstrDistinguishedName, hr, E_OUTOFMEMORY, "Failed to allocate space for distinguished name.");
    ::StringCchCopyW((WCHAR*) bstrDistinguishedName, cchbstrDistinguishedName, wzDistinguishedName);
    ::StringCchCatW((WCHAR*) bstrDistinguishedName, cchbstrDistinguishedName, L",CN=");
    ::StringCchCatW((WCHAR*) bstrDistinguishedName, cchbstrDistinguishedName, wzComputerName);

    bstrCertificateUsage = SysAllocString(WIDE(szOID_PKIX_KP_SERVER_AUTH));
    ExitOnNull(bstrCertificateUsage, hr, E_OUTOFMEMORY, "Failed to allocate space for Certificate Usage.");
    bstrCertificateAttributes = SysAllocString(L"CertificateTemplate:WebServer");
    bstrCertificateAuthority = SysAllocString(wzCertificateAuthority);
    ExitOnNull(bstrCertificateAuthority, hr, E_OUTOFMEMORY, "Failed to allocate space for Certificate Authority.");

    hr = CreateEnroll(&pEnroll, iStore, iStoreLocation);
    ExitOnFailure(hr, "failed CoCreateInstance IEnroll");

    hr = pEnroll->createPKCS10(bstrDistinguishedName, bstrCertificateUsage, &bstrRequest);
    ExitOnFailure(hr, "failed createPKCS10");

    hr = CoCreateInstance(CLSID_CCertRequest, NULL, CLSCTX_INPROC_SERVER, IID_ICertRequest, (void **)&pCertRequest);
    ExitOnFailure(hr, "failed CoCreateInstance ICertRequest");

    hr = pCertRequest->Submit(CR_IN_BASE64 | CR_IN_PKCS10, bstrRequest, bstrCertificateAttributes, bstrCertificateAuthority, &nDisposition);
    ExitOnFailure(hr, "failed ICertRequest.Submit");

    hr = (nDisposition == CR_DISP_ISSUED) ? S_OK : E_FAIL;
    ExitOnFailure(hr, "failed CR_DISP_ISSUED");

    hr = pCertRequest->GetCertificate(CR_OUT_BASE64, pbstrCertificate);
    ExitOnFailure(hr, "failed ICertRequest.GetCertificate");

    // save the certificate in place, cannot be passed to a deferred custom action
    hr = pEnroll->acceptPKCS7(*pbstrCertificate);
    ExitOnFailure(hr, "failed accept certificate into MY store");

LExit:
    ReleaseObject(pCertRequest);
    ReleaseBSTR(bstrRequest);
    ReleaseObject(pEnroll);
    ReleaseBSTR(bstrCertificateAuthority);
    ReleaseBSTR(bstrCertificateAttributes);
    ReleaseBSTR(bstrDistinguishedName);

    return hr;
}


VOID ParseCertificateAuthority(__in LPCWSTR wzCertificateAuthorityOrig, __out LPWSTR *pwzBuffer, __out LPWSTR **hwzCAArray, __out int *piCAArray)
{
    // @asAuthorities = split /;/, $sAuthority;
    CONST WCHAR wchDelimiter = L';';

    // copy constant into a buffer
    Assert(wzCertificateAuthorityOrig);

    INT cchCA = lstrlenW(wzCertificateAuthorityOrig) + 1;
    WCHAR* wzBuffer = new WCHAR[cchCA];
    if (!wzBuffer)
        return;

    ::StringCchCopyW(wzBuffer, cchCA, wzCertificateAuthorityOrig);

    // determine the number of strings in the field
    int iCAArray = 1;
    int i;
    for (i = 0; i < cchCA; ++i)
    {
        if (wzBuffer[i] == wchDelimiter)
            ++iCAArray;
    }
    LPWSTR *pwzCAArray = (LPWSTR*) new BYTE[iCAArray * sizeof(LPWSTR)];
    if (!pwzCAArray)
    {
        return;
    }

    pwzCAArray[0] = wzBuffer;
    iCAArray = 0;
    for (i = 0; i < cchCA; ++i)
    {
        if (wzBuffer[i] != wchDelimiter)
            continue;
        wzBuffer[i] = 0; // convert buffer into MULTISZ
        pwzCAArray[iCAArray] = &wzBuffer[i+1];
        ++iCAArray;
    }

    *pwzBuffer = wzBuffer;
    *hwzCAArray = pwzCAArray;
    *piCAArray = iCAArray;
}


HRESULT ScaSslExistingCertificateByBinaryData(INT iStore, INT iStoreLocation, BYTE* pwzData, DWORD cchData)
{
    HRESULT hr = S_FALSE;
    HCERTSTORE hCertStore = NULL;
    PCCERT_CONTEXT pCertCtx = NULL, pCertCtxExisting = NULL;
    DWORD dwFlags = 0;
    LPCWSTR wzStore = StoreMapping(iStore);
    CERT_BLOB blob;

    dwFlags = iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT;
    hCertStore = CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwFlags, wzStore);
    MessageExitOnNullWithLastError(hCertStore, hr, msierrCERTFailedOpen, "failed to open certificate store, OK on uninstall");

    blob.pbData = pwzData;
    blob.cbData = cchData;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL,
        0, NULL, NULL, NULL, NULL, NULL, (LPCVOID*)&pCertCtx))
        ExitOnLastError(hr, "failed to parse the certificate blob, OK on uninstall");

    pCertCtxExisting = CertFindCertificateInStore(
        hCertStore,
        PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
        0,
        CERT_FIND_EXISTING,
        pCertCtx,
        NULL);

    if (pCertCtxExisting)
    {
        hr = S_OK;
    }

LExit:
    if (pCertCtx)
    {
        CertFreeCertificateContext(pCertCtx);
        pCertCtx = NULL;
    }
    if (pCertCtxExisting)
    {
        CertFreeCertificateContext(pCertCtxExisting);
        pCertCtxExisting = NULL;
    }
    if (hCertStore)
    {
        CertCloseStore(hCertStore, 0);
        hCertStore = NULL;
    }

    return hr;
}


HRESULT ScaSslExistingCertificateByName(LPCWSTR pwzName, INT iStore, INT iStoreLocation,
                                       BSTR* pbstrCertificate, DWORD* pcbCertificate, BYTE* pbaHashBuffer)
{
    HRESULT hr = S_FALSE;
    HCERTSTORE hSystemStore = NULL;
    PCCERT_CONTEXT pTargetCert = NULL;
    WCHAR wzFriendlyName[MAX_PATH] = {0};
    DWORD dwFriendlyNameLen = sizeof(wzFriendlyName);

    // Call CertOpenStore to open the CA store.
    hSystemStore = CertOpenStore(
        CERT_STORE_PROV_SYSTEM_REGISTRY,
        0,
        NULL,
        (iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT) | CERT_STORE_OPEN_EXISTING_FLAG,
        StoreMapping(iStore));
    if (hSystemStore == NULL)
        ExitFunction();

    // Get a particular certificate using CertFindCertificateInStore.
    pTargetCert = CertFindCertificateInStore(
        hSystemStore,
        PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
        0,
        CERT_FIND_ANY,
        NULL,
        NULL);
    while (pTargetCert != NULL)
    {
        if ((CertGetCertificateContextProperty(pTargetCert, CERT_FRIENDLY_NAME_PROP_ID,
            (BYTE*)wzFriendlyName, &dwFriendlyNameLen)) &&
            lstrcmpW(wzFriendlyName, pwzName) == 0)
        {
            // pTargetCert is a pointer to the desired certificate.
            // Check the certificate's validity.
            switch (CertVerifyTimeValidity(
                NULL,
                pTargetCert->pCertInfo))
            {
            case 1:
                // Certificate is expired
                WcaLog(LOGMSG_STANDARD, "The SSL certificate has expired");
                // always remove it
                {
                    PCCERT_CONTEXT pDupCertContext = CertDuplicateCertificateContext(pTargetCert);
                    if (pDupCertContext && CertDeleteCertificateFromStore(pDupCertContext))
                    {
                        WcaLog(LOGMSG_STANDARD, "A SSL certificate has removed");
                    }
                }
                break;
            case 0:
                // Certificate is valid
                WcaLog(LOGMSG_STANDARD, "The SSL certificate is valid");
                hr = S_OK;
                if (pbaHashBuffer)
                {
                    // if the certificate already exists and is valid, use that one
                    DWORD dwHashSize = CB_CERTIFICATE_HASH;
                    hr = CertGetCertificateContextProperty(pTargetCert, CERT_SHA1_HASH_PROP_ID, (VOID*)pbaHashBuffer, &dwHashSize)
                        ? S_OK : E_FAIL;
                    ExitOnFailure(hr, "failed CertGetCertificateContextProperty CERT_SHA1_HASH_PROP_ID");
                    Assert(pbstrCertificate);
                    Assert(pcbCertificate);
                    ReleaseBSTR(*pbstrCertificate);

                    *pbstrCertificate = SysAllocStringByteLen((LPCSTR)(pTargetCert->pbCertEncoded), pTargetCert->cbCertEncoded);
                    *pcbCertificate = pTargetCert->cbCertEncoded;
                }
                ExitFunction();
                break;
            default:
                // Certificate not valid yet, ignore it
                WcaLog(LOGMSG_STANDARD, "The SSL certificate is not valid");
                break;
            }
        }
        pTargetCert = CertFindCertificateInStore(
            hSystemStore,
            PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
            0,
            CERT_FIND_ANY,
            NULL,
            pTargetCert);
        wzFriendlyName[0] = 0;
        dwFriendlyNameLen = sizeof(wzFriendlyName);
    }

LExit:
    // Clean up memory and quit.
    if (pTargetCert)
    {
        CertFreeCertificateContext(pTargetCert);
        pTargetCert = NULL;
    }
    if (hSystemStore)
    {
        CertCloseStore(hSystemStore, CERT_CLOSE_STORE_CHECK_FLAG);
        hSystemStore = NULL;
    }

    return hr;
}


HRESULT ScaSslNewCertificate(LPCWSTR pwzName, INT iStore, INT iStoreLocation, LPCWSTR wzComputerName, LPCWSTR wzDistinguishedName, LPCWSTR wzCertificateAuthorityOrig,
                            BSTR* pbstrCertificate, DWORD* pcbCertificate, BYTE* pbaHashBuffer)
{

    if (pbstrCertificate == NULL)
        return E_INVALIDARG;

    HRESULT hr = S_OK;
    LPWSTR wzCABuffer = NULL;
    LPWSTR *wzCAArray = NULL;
    int iCAArray = 0;

    // otherwise call the CA for one
    ParseCertificateAuthority(wzCertificateAuthorityOrig, &wzCABuffer, &wzCAArray, &iCAArray);

    // try each authority three times
    for (int i = 0; i < 3 * iCAArray; ++i)
    {
        LPCWSTR wzCA = wzCAArray[i % iCAArray];
        if (NULL == wzCA || NULL == wzCA[0]) continue;
        WcaLog(LOGMSG_STANDARD, "Requesting SSL certificate from %ls", wzCA);
        hr = RequestCertificate(pwzName, iStore, iStoreLocation, wzComputerName, wzDistinguishedName, wzCA, pbstrCertificate);
        if (hr == S_OK && pbstrCertificate)
        {
            // set the friendly name
            CRYPT_HASH_BLOB hblob;
            CERT_BLOB blob;
            HCERTSTORE hCertStore = NULL;
            PCCERT_CONTEXT pCertCtxExisting = NULL;

            blob.pbData = (BYTE*)pwzName;
            blob.cbData = (lstrlenW(pwzName) + 1) * sizeof(pwzName[0]); // including terminating null

            *pcbCertificate = SysStringByteLen(*pbstrCertificate);
            hr = CertificateToHash(*pbstrCertificate, pbaHashBuffer);
            ExitOnFailure(hr, "failed to CertificateToHash for an existing certificate");

            hblob.pbData = pbaHashBuffer;
            hblob.cbData = CB_CERTIFICATE_HASH;

            hCertStore = CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, (iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT), StoreMapping(iStore));
            MessageExitOnNullWithLastError(hCertStore, hr, msierrCERTFailedOpen, "failed to open certificate store");

            pCertCtxExisting = CertFindCertificateInStore(
                hCertStore,
                PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
                0,
                CERT_FIND_HASH,
                &hblob,
                NULL);

            if (pCertCtxExisting)
            {
                CertSetCertificateContextProperty(
                    pCertCtxExisting,
                    CERT_FRIENDLY_NAME_PROP_ID,
                    0,
                    &blob);
            }

            if (pCertCtxExisting)
            {
                CertFreeCertificateContext(pCertCtxExisting);
                pCertCtxExisting = NULL;
            }
            if (hCertStore)
            {
                CertCloseStore(hCertStore, 0);
                hCertStore = NULL;
            }
            ExitFunction();
        }
        if (pbstrCertificate && *pbstrCertificate)
        {
            SysFreeString(*pbstrCertificate);
            pbstrCertificate = NULL;
        }
    }
    hr = E_FAIL;
    ExitOnFailure(hr, "failed to RequestCertificate");

LExit:
    if (wzCABuffer)
    {
        delete wzCABuffer;
    }
    if (wzCAArray)
    {
        delete wzCAArray;
    }

    return hr;
}


HRESULT ScaGetCertificateByRequest(LPCWSTR pwzName, BOOL fIsInstalling, BOOL fIsUninstalling,
                                   INT iStore, INT iStoreLocation,
                                   LPCWSTR wzDistinguishedName, LPCWSTR wzCA,
                                   BSTR* pbstrCertificate, DWORD* pcbCertificate, BYTE* pbaHashBuffer)
{
    HRESULT hr = S_OK;
    WCHAR wzComputerName[MAX_COMPUTER_NAME] = {0};
    WCHAR* pwzData = NULL;
    DWORD cchData = 0;

    // override %COMPUTERNAME% with DOMAINNAME property
    hr = WcaGetProperty( L"DOMAINNAME", &pwzData);
    ExitOnFailure(hr, "Failed to get Property DOMAINNAME");
    if (*pwzData)
    {
        // if DOMAINNAME is set, use it
        ::StringCchCopyW(wzComputerName, MAX_COMPUTER_NAME, pwzData);
    }
    else
    {
        // otherwise get the intranet name given by %COMPUTERNAME%
        GetEnvironmentVariableW(L"COMPUTERNAME", wzComputerName, MAX_COMPUTER_NAME);
    }

    hr = ScaSslExistingCertificateByName(pwzName, iStore, iStoreLocation, pbstrCertificate, pcbCertificate, pbaHashBuffer);
    ExitOnFailure(hr, "Failed ScaSslExistingCertificateByName");
    if (S_OK != hr)
    {
        if (!fIsUninstalling && fIsInstalling)
        {
            // if no existing cert and not on uninstall, hit the authority
            WcaLog(LOGMSG_STANDARD, "Adding certificate: requested, %ls", wzDistinguishedName);
            hr = ScaSslNewCertificate(pwzName, iStore, iStoreLocation, wzComputerName, wzDistinguishedName, wzCA,
                pbstrCertificate, pcbCertificate, pbaHashBuffer);
            ExitOnFailure(hr, "Failed ScaSslNewCertificate");
        }
        else
        {
            // if no existing cert and uninstall
            hr = S_OK;
        }
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}


HRESULT ScaInstallCertificateByContext(LPCWSTR pwzName, INT iStore, INT iStoreLocation,
                                       PCCERT_CONTEXT pCertContext)
{
    HRESULT hr = S_OK;
    HCERTSTORE hCertStore = NULL;
    DWORD dwFlags = iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT;
    CERT_BLOB blob;

    hCertStore = CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwFlags, StoreMapping(iStore));
    if (hCertStore == NULL)
        MessageExitOnLastError(hr, msierrCERTFailedOpen, "failed to open certificate store");

    blob.pbData = (BYTE*)pwzName;
    blob.cbData = (lstrlenW(pwzName) + 1) * sizeof(pwzName[0]); // including terminating null
    CertSetCertificateContextProperty(
        pCertContext,
        CERT_FRIENDLY_NAME_PROP_ID,
        0,
        &blob);

    if (!CertAddCertificateContextToStore(
        hCertStore,
        pCertContext,
        CERT_STORE_ADD_REPLACE_EXISTING,
        NULL))
    {
        hr = E_FAIL;
        MessageExitOnLastError(hr, msierrCERTFailedAdd, "failed to add certificate to the store");
    }

LExit:
    if (hCertStore)
    {
        CertCloseStore(hCertStore, 0);
        hCertStore = NULL;
    }

    return hr;
}


HRESULT ScaGetCertificateByPath(LPCWSTR pwzName, BOOL fIsInstalling, BOOL fIsUninstalling,
                                INT iStore, INT iStoreLocation, LPCWSTR wzSslCertificate, LPCWSTR wzPFXPassword,
                                BSTR* pbstrCertificate, DWORD* pcbCertificate, BYTE* pbaHashBuffer)
{
    Assert(wzSslCertificate);
    HRESULT hr = S_OK;
    PCCERT_CONTEXT pCertContext = NULL;
    DWORD dwEncodingType = 0;
    DWORD dwContentType = 0;
    DWORD dwFormatType = 0;
    DWORD dwHashSize = CB_CERTIFICATE_HASH;
    HANDLE hPfxFile = INVALID_HANDLE_VALUE;
    CRYPT_DATA_BLOB blob;

    blob.pbData = NULL;
    blob.cbData = 0;

    if (wzSslCertificate && wzSslCertificate[0] != 0)
    {
        if (!::CryptQueryObject(CERT_QUERY_OBJECT_FILE, (LPVOID)wzSslCertificate, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL,
            0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertContext))
            hr = fIsUninstalling ? S_FALSE : HRESULT_FROM_WIN32(::GetLastError());  // don't fail on uninstall
        ExitOnFailure(hr, "failed CryptQueryObject");
    }
    else
    {
        hr = S_FALSE;
        ExitFunction();
    }

    if (!pCertContext)
    {
        // this is a pfx?
        // make sure to exit this block of code properly for clean up blob.pbData
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            DWORD iSize = 0, iReadSize = 0;
            HCERTSTORE hPfxCertStore = NULL;

            hPfxFile = ::CreateFileW(wzSslCertificate, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
            hr = (hPfxFile != INVALID_HANDLE_VALUE) ? S_OK : E_FAIL;
            ExitOnFailure(hr, "failed CryptQueryObject, file handle is null");
            iSize = ::GetFileSize(hPfxFile, NULL);
            hr = (iSize > 0) ? S_OK : E_FAIL;
            ExitOnFailure(hr, "failed CryptQueryObject, file size is 0");
            blob.pbData = new BYTE[iSize];
            blob.cbData = iSize;
            hr = (blob.pbData) ? S_OK : E_FAIL;
            ExitOnFailure(hr, "out of memory for blob");

            if (::ReadFile(hPfxFile, (LPVOID)blob.pbData, iSize, &iReadSize, NULL))
            {
                hPfxCertStore = PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, wzPFXPassword,
                    (iStoreLocation == SCA_CERTSYSTEMSTORE_CURRENTUSER) ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET);
                if (hPfxCertStore)
                {
                    pCertContext = CertEnumCertificatesInStore(hPfxCertStore, NULL);
                    // work only with the first certificate in pfx
                    if (pCertContext)
                    {
                        hr = CertGetCertificateContextProperty(pCertContext, CERT_SHA1_HASH_PROP_ID, (VOID*)pbaHashBuffer, &dwHashSize)
                            ? S_OK : E_FAIL;
                        ExitOnFailure(hr, "failed CertGetCertificateContextProperty CERT_SHA1_HASH_PROP_ID");
                        ReleaseBSTR(*pbstrCertificate);

                        *pbstrCertificate = SysAllocStringByteLen((LPCSTR)(pCertContext->pbCertEncoded), pCertContext->cbCertEncoded);
                        *pcbCertificate = pCertContext->cbCertEncoded;
                        if (fIsInstalling)
                        {
                            // install the certificate, cannot defer because the data required cannot be passed
                            hr = ScaInstallCertificateByContext(pwzName, iStore, iStoreLocation, pCertContext);
                        }
                    }
                    else
                        hr = E_FAIL;
                }
                else
                    hr = E_FAIL;
            }
            else
                hr = E_FAIL;
        }
        else
        {
            ExitOnFailure(hr = E_FAIL, "failed CryptQueryObject, unknown data");
        }
    }
    else
    {
        // return cert and its hash
        hr = CertGetCertificateContextProperty(pCertContext, CERT_SHA1_HASH_PROP_ID, (VOID*)pbaHashBuffer, &dwHashSize)
            ? S_OK : E_FAIL;
        ExitOnFailure(hr, "failed CertGetCertificateContextProperty CERT_SHA1_HASH_PROP_ID");
        ReleaseBSTR(*pbstrCertificate);

        *pbstrCertificate = SysAllocStringByteLen((LPCSTR)(pCertContext->pbCertEncoded), pCertContext->cbCertEncoded);
        *pcbCertificate = pCertContext->cbCertEncoded;
        if (fIsInstalling)
        {
            // install the certificate, cannot defer because the data required cannot be passed
            hr = ScaInstallCertificateByContext(pwzName, iStore, iStoreLocation, pCertContext);
        }
    }

LExit:
    if (pCertContext)
    {
        CertFreeCertificateContext(pCertContext);
        pCertContext = NULL;
    }
    if (hPfxFile != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hPfxFile);
        hPfxFile = INVALID_HANDLE_VALUE;
    }
    if (blob.pbData)
    {
        delete [] blob.pbData;
        blob.pbData = NULL;
        blob.cbData = 0;
    }

    return hr;
}


HRESULT ScaInstallCertificateByBinaryData(BOOL fAddCert, INT iStore, INT iStoreLocation, LPCWSTR wzName, BYTE* pwzData, DWORD cchData,
                                          LPCWSTR wzPFXPassword)
{
    Assert(wzName);
    Assert(pwzData);
    Assert(cchData);
    HRESULT hr = S_OK;
    HCERTSTORE hCertStore = NULL, hPfxCertStore = NULL;
    PCCERT_CONTEXT pCertCtx = NULL, pCertCtxExisting = NULL;
    DWORD dwFlags, dwEncodingType, dwContentType, dwFormatType;
    CERT_BLOB blob;
    LPCWSTR wzStore = StoreMapping(iStore);

    dwFlags = iStoreLocation << CERT_SYSTEM_STORE_LOCATION_SHIFT;
    hCertStore = CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwFlags, wzStore);
    MessageExitOnNullWithLastError(hCertStore, hr, msierrCERTFailedOpen, "failed to open certificate store");

    blob.pbData = pwzData;
    blob.cbData = cchData;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL,
        0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertCtx))
        ExitOnLastError(hr, "failed to parse the certificate blob");
    ExitOnNull(pCertCtx, hr, E_UNEXPECTED, "failed to parse the certificate blob");

    blob.pbData = (BYTE*)wzName;
    blob.cbData = (lstrlenW(wzName) + 1) * sizeof(wzName[0]); // including terminating null

    CertSetCertificateContextProperty(
        pCertCtx,
        CERT_FRIENDLY_NAME_PROP_ID,
        0,
        &blob);

    if (fAddCert)
    {
        // Add
        WcaLog(LOGMSG_STANDARD, "Adding certificate: binary name, %ls", wzName);
        if (!CertAddCertificateContextToStore(
            hCertStore,
            pCertCtx,
            CERT_STORE_ADD_REPLACE_EXISTING,
            NULL))
        {
            hr = E_FAIL;
            MessageExitOnLastError(hr, msierrCERTFailedAdd, "failed to add certificate to the store");
        }
    }
    else
    {
        // Delete
        WcaLog(LOGMSG_STANDARD, "Deleting certificate provided: binary name, %ls", wzName);
        pCertCtxExisting = CertFindCertificateInStore(
            hCertStore,
            PKCS_7_ASN_ENCODING | X509_ASN_ENCODING,
            0,
            CERT_FIND_EXISTING,
            pCertCtx,
            NULL);

        if (pCertCtxExisting)
        {
            if (!CertDeleteCertificateFromStore(pCertCtxExisting))
            {
                ExitOnLastError(hr, "failed to delete certificate");
            }
            else
            {
                pCertCtxExisting = NULL;
            }
        }
    }

LExit:
    if (pCertCtx)
    {
        CertFreeCertificateContext(pCertCtx);
        pCertCtx = NULL;
    }
    if (pCertCtxExisting)
    {
        CertFreeCertificateContext(pCertCtxExisting);
        pCertCtxExisting = NULL;
    }
    // order is important for store
    if (hCertStore)
    {
        CertCloseStore(hCertStore, 0);
        hCertStore = NULL;
    }
    if (hPfxCertStore)
    {
        CertCloseStore(hPfxCertStore, 0);
        hPfxCertStore = NULL;
    }

    return hr;
}
*/
