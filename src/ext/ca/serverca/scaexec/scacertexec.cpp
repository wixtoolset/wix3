// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define SIXTY_FOUR_MEG 64 * 1024 * 1024

// prototypes
static HRESULT ExecuteCertificateOperation(
    __in MSIHANDLE hInstall,
    __in SCA_ACTION saAction,
    __in DWORD dwStoreRoot
    );

static HRESULT ReadCertificateFile(
    __in LPCWSTR wzPath,
    __out BYTE** prgbData,
    __out DWORD* pcbData
    );

static HRESULT InstallCertificatePackage(
    __in HCERTSTORE hStore,
    __in BOOL fUserCertificateStore,
    __in LPCWSTR wzName,
    __in_opt BYTE* rgbData,
    __in DWORD cbData,
    __in_opt LPCWSTR wzPFXPassword
    );

static HRESULT UninstallCertificatePackage(
    __in HCERTSTORE hStore,
    __in BOOL fUserCertificateStore,
    __in LPCWSTR wzName
    );


/* ****************************************************************
 AddUserCertificate - CUSTOM ACTION ENTRY POINT for adding per-user
                      certificates

 * ***************************************************************/
extern "C" UINT __stdcall AddUserCertificate(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "AddUserCertificate");
    ExitOnFailure(hr, "Failed to initialize AddUserCertificate.");

    hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_INSTALL, CERT_SYSTEM_STORE_CURRENT_USER);
    ExitOnFailure(hr, "Failed to install per-user certificate.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/* ****************************************************************
 AddMachineCertificate - CUSTOM ACTION ENTRY POINT for adding 
                         per-machine certificates

 * ***************************************************************/
extern "C" UINT __stdcall AddMachineCertificate(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "AddMachineCertificate");
    ExitOnFailure(hr, "Failed to initialize AddMachineCertificate.");

    hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_INSTALL, CERT_SYSTEM_STORE_LOCAL_MACHINE);
    ExitOnFailure(hr, "Failed to install per-machine certificate.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/* ****************************************************************
 DeleteUserCertificate - CUSTOM ACTION ENTRY POINT for deleting 
                         per-user certificates

 * ***************************************************************/
extern "C" UINT __stdcall DeleteUserCertificate(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "DeleteUserCertificate");
    ExitOnFailure(hr, "Failed to initialize DeleteUserCertificate.");

    hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_UNINSTALL, CERT_SYSTEM_STORE_CURRENT_USER);
    ExitOnFailure(hr, "Failed to uninstall per-user certificate.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


/* ****************************************************************
 DeleteMachineCertificate - CUSTOM ACTION ENTRY POINT for deleting
                            per-machine certificates

 * ***************************************************************/
extern "C" UINT __stdcall DeleteMachineCertificate(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;

    hr = WcaInitialize(hInstall, "DeleteMachineCertificate");
    ExitOnFailure(hr, "Failed to initialize DeleteMachineCertificate.");

    hr = ExecuteCertificateOperation(hInstall, SCA_ACTION_UNINSTALL, CERT_SYSTEM_STORE_LOCAL_MACHINE);
    ExitOnFailure(hr, "Failed to uninstall per-machine certificate.");

LExit:
    er = SUCCEEDED(hr) ? ERROR_SUCCESS : ERROR_INSTALL_FAILURE;
    return WcaFinalize(er);
}


static HRESULT ExecuteCertificateOperation(
    __in MSIHANDLE /*hInstall*/,
    __in SCA_ACTION saAction,
    __in DWORD dwStoreLocation
    )
{
    //AssertSz(FALSE, "Debug ExecuteCertificateOperation() here.");
    Assert(saAction & SCA_ACTION_INSTALL || saAction & SCA_ACTION_UNINSTALL);

    HRESULT hr = S_OK;
    LPWSTR pwzCaData = NULL;
    LPWSTR pwz;
    LPWSTR pwzName = NULL;
    LPWSTR pwzStore = NULL;
    int iAttributes = 0;
    LPWSTR pwzPFXPassword = NULL;
    LPWSTR pwzFilePath = NULL;
    BYTE* pbData = NULL;
    DWORD cbData = 0;
    DWORD cbPFXPassword = 0;

    BOOL fUserStoreLocation = (CERT_SYSTEM_STORE_CURRENT_USER == dwStoreLocation);
    HCERTSTORE hCertStore = NULL;

    hr = WcaGetProperty(L"CustomActionData", &pwzCaData);
    ExitOnFailure(hr, "Failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCaData);

    pwz = pwzCaData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "Failed to parse certificate name.");
    hr = WcaReadStringFromCaData(&pwz, &pwzStore);
    ExitOnFailure(hr, "Failed to parse CustomActionData, StoreName");
    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "Failed to parse certificate attribute");
    if (SCA_ACTION_INSTALL == saAction) // install operations need more data
    {
        hr = WcaReadStreamFromCaData(&pwz, &pbData, (DWORD_PTR*)&cbData);
        ExitOnFailure(hr, "Failed to parse certificate stream.");

        hr = WcaReadStringFromCaData(&pwz, &pwzPFXPassword);
        ExitOnFailure(hr, "Failed to parse certificate password.");
    }

    // Open the right store.
    hCertStore = ::CertOpenStore(CERT_STORE_PROV_SYSTEM, 0, NULL, dwStoreLocation, pwzStore);
    MessageExitOnNullWithLastError1(hCertStore, hr, msierrCERTFailedOpen, "Failed to open certificate store: %ls", pwzStore);

    if (SCA_ACTION_INSTALL == saAction) // install operations need more data
    {
        // Uninstall existing versions of this package.  Ignore any failures
        // This is needed to clean up the private key of a cert when we replace an existing cert
        // CertAddCertificateContextToStore(CERT_STORE_ADD_REPLACE_EXISTING) does not remove the private key if the cert is replaced
        UninstallCertificatePackage(hCertStore, fUserStoreLocation, pwzName);

        hr = InstallCertificatePackage(hCertStore, fUserStoreLocation, pwzName, pbData, cbData, pwzPFXPassword);
        ExitOnFailure(hr, "Failed to install certificate.");
    }
    else
    {
        Assert(SCA_ACTION_UNINSTALL == saAction);

        hr = UninstallCertificatePackage(hCertStore, fUserStoreLocation, pwzName);
        ExitOnFailure(hr, "Failed to uninstall certificate.");
    }

LExit:
    if (NULL != pwzPFXPassword && SUCCEEDED(StrSize(pwzPFXPassword, &cbPFXPassword)))
    {
        SecureZeroMemory(pwzPFXPassword, cbPFXPassword);
    }

    if (hCertStore)
    {
        if (!::CertCloseStore(hCertStore, CERT_CLOSE_STORE_CHECK_FLAG))
        {
            WcaLog(LOGMSG_VERBOSE, "Cert store was closed but not all resources were freed.  Error 0x%x", GetLastError());
        }
    }

    ReleaseMem(pbData);
    ReleaseStr(pwzFilePath);
    ReleaseStr(pwzPFXPassword);
    ReleaseStr(pwzStore);
    ReleaseStr(pwzName);
    ReleaseStr(pwzCaData);
    return hr;
}


static HRESULT InstallCertificatePackage(
    __in HCERTSTORE hStore,
    __in BOOL fUserCertificateStore,
    __in LPCWSTR wzName,
    __in_opt BYTE* rgbData,
    __in DWORD cbData,
    __in_opt LPCWSTR wzPFXPassword
    )
{
    HRESULT hr = S_OK;

    HCERTSTORE hPfxCertStore = NULL;
    PCCERT_CONTEXT pCertContext = NULL;
    CERT_BLOB blob = { 0 };
    DWORD dwKeyset = fUserCertificateStore ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET;
    DWORD dwEncodingType;
    DWORD dwContentType;
    DWORD dwFormatType;
    LPWSTR pwzUniqueName = NULL;
    int iUniqueId = 0;

    // Figure out what type of blob (certificate or PFX) we're dealing with here.
    blob.pbData = rgbData;
    blob.cbData = cbData;

    if (!::CryptQueryObject(CERT_QUERY_OBJECT_BLOB, &blob, CERT_QUERY_CONTENT_FLAG_ALL, CERT_QUERY_FORMAT_FLAG_ALL, 0, &dwEncodingType, &dwContentType, &dwFormatType, NULL, NULL, (LPCVOID*)&pCertContext))
    {
        ExitWithLastError1(hr, "Failed to parse the certificate blob: %ls", wzName);
    }

    hr = StrAllocFormatted(&pwzUniqueName, L"%s_wixCert_%d", wzName, ++iUniqueId);
    ExitOnFailure(hr, "Failed to format unique name");

    if (!pCertContext)
    {
        // If we have a PFX blob, get the first certificate out of the PFX and use that instead of the PFX.
        if (dwContentType & CERT_QUERY_CONTENT_PFX)
        {
            ExitOnNull(wzPFXPassword, hr, E_INVALIDARG, "Failed to import PFX blob because no password was provided");

            // If we fail and our password is blank, also try passing in NULL for the password (according to the docs)
            hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, wzPFXPassword, dwKeyset);
            if (NULL == hPfxCertStore && !*wzPFXPassword)
            {
                hPfxCertStore = ::PFXImportCertStore((CRYPT_DATA_BLOB*)&blob, NULL, dwKeyset);
            }
            ExitOnNullWithLastError(hPfxCertStore, hr, "Failed to open PFX file.");

            // Install all certificates in the PFX
            for (pCertContext = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContext);
                 pCertContext;
                 pCertContext = ::CertEnumCertificatesInStore(hPfxCertStore, pCertContext))
            {
                WcaLog(LOGMSG_STANDARD, "Adding certificate: %ls", pwzUniqueName);
                hr = CertInstallSingleCertificate(hStore, pCertContext, pwzUniqueName);
                MessageExitOnFailure(hr, msierrCERTFailedAdd, "Failed to add certificate to the store.");

                hr = StrAllocFormatted(&pwzUniqueName, L"%s_wixCert_%d", wzName, ++iUniqueId);
                ExitOnFailure(hr, "Failed to format unique name");
            }
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected certificate type processed.");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "Adding certificate: %ls", pwzUniqueName);
        hr = CertInstallSingleCertificate(hStore, pCertContext, pwzUniqueName);
        MessageExitOnFailure(hr, msierrCERTFailedAdd, "Failed to add certificate to the store.");
    }

    hr = WcaProgressMessage(COST_CERT_ADD, FALSE);
    ExitOnFailure(hr, "Failed to send install progress message.");

LExit:
    ReleaseStr(pwzUniqueName);

    if (pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    // Close the stores after the context's are released.
    if (hPfxCertStore)
    {
        if (!::CertCloseStore(hPfxCertStore, CERT_CLOSE_STORE_CHECK_FLAG))
        {
            WcaLog(LOGMSG_VERBOSE, "PFX cert store was closed but not all resources were freed.  Error 0x%x", GetLastError());
        }
    }

    return hr;
}


static HRESULT UninstallCertificatePackage(
    __in HCERTSTORE hStore,
    __in BOOL fUserCertificateStore,
    __in LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    PCCERT_CONTEXT pCertContext = NULL;
    CRYPT_KEY_PROV_INFO* pPrivateKeyInfo = NULL;
    LPWSTR pwzUniquePrefix = NULL;
    int ccUniquePrefix = 0;

    hr = StrAllocFormatted(&pwzUniquePrefix, L"%s_wixCert_", wzName);
    ExitOnFailure(hr, "Failed to format unique name");
    ccUniquePrefix = ::lstrlenW(pwzUniquePrefix);

    WcaLog(LOGMSG_STANDARD, "Deleting certificate that begin with friendly name: %ls", pwzUniquePrefix);

    // Loop through all certificates in the store, deleting the ones that begin with our prefix.
    while (NULL != (pCertContext = ::CertFindCertificateInStore(hStore, PKCS_7_ASN_ENCODING | X509_ASN_ENCODING, 0, CERT_FIND_ANY, NULL, pCertContext)))
    {
        WCHAR wzFriendlyName[256] = { 0 };
        DWORD cbFriendlyName = sizeof(wzFriendlyName);

        if (::CertGetCertificateContextProperty(pCertContext, CERT_FRIENDLY_NAME_PROP_ID, reinterpret_cast<BYTE*>(wzFriendlyName), &cbFriendlyName) &&
            lstrlenW(wzFriendlyName) >= ccUniquePrefix &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_SYSTEM_DEFAULT, 0, pwzUniquePrefix, ccUniquePrefix, wzFriendlyName, ccUniquePrefix))
        {
            PCCERT_CONTEXT pCertContextDelete = ::CertDuplicateCertificateContext(pCertContext); // duplicate the context so we can delete it with out disrupting the looping
            if(pCertContextDelete)
            {
                // Delete the certificate and if successful delete the matching private key as well.
                if (::CertDeleteCertificateFromStore(pCertContextDelete))
                {
                    // If we found private key info, delete it.
                    hr = CertReadProperty(pCertContextDelete, CERT_KEY_PROV_INFO_PROP_ID, &pPrivateKeyInfo, NULL);
                    if (SUCCEEDED(hr))
                    {
                        HCRYPTPROV hProvIgnored = NULL; // ignored on deletes.
                        DWORD dwKeyset = fUserCertificateStore ? CRYPT_USER_KEYSET : CRYPT_MACHINE_KEYSET;

                        if (!::CryptAcquireContextW(&hProvIgnored, pPrivateKeyInfo->pwszContainerName, pPrivateKeyInfo->pwszProvName, pPrivateKeyInfo->dwProvType, dwKeyset | CRYPT_DELETEKEYSET | CRYPT_SILENT))
                        {
                            er = ::GetLastError();
                            hr = HRESULT_FROM_WIN32(er);
                        }

                        ReleaseNullMem(pPrivateKeyInfo);
                    }
                    else // don't worry about failures to delete private keys.
                    {
                        hr = S_OK;
                    }
                }
                else
                {
                    er = ::GetLastError();
                    hr = HRESULT_FROM_WIN32(er);
                }

                if (FAILED(hr))
                {
                    WcaLog(LOGMSG_STANDARD, "Failed to delete certificate with friendly name: %ls, continuing anyway.  Error: 0x%x", wzFriendlyName, hr);
                }

                pCertContextDelete = NULL;
            }
        }
    }

    hr = WcaProgressMessage(COST_CERT_DELETE, FALSE);
    ExitOnFailure(hr, "Failed to send uninstall progress message.");

LExit:
    ReleaseStr(pwzUniquePrefix);
    ReleaseMem(pPrivateKeyInfo);
    if(pCertContext)
    {
        ::CertFreeCertificateContext(pCertContext);
    }

    return hr;
}
