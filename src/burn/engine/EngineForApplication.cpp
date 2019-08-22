// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


class CEngineForApplication : public IBootstrapperEngine, public IMarshal
{
public: // IUnknown
    virtual STDMETHODIMP QueryInterface(
        __in const IID& riid,
        __out void** ppvObject
        )
    {
        HRESULT hr = S_OK;

        ExitOnNull(ppvObject, hr, E_INVALIDARG, "Invalid argument ppvObject");
        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBootstrapperEngine), riid))
        {
            *ppvObject = static_cast<IBootstrapperEngine*>(this);
        }
        else if (::IsEqualIID(IID_IMarshal, riid))
        {
            *ppvObject = static_cast<IMarshal*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = reinterpret_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            ExitFunction1(hr = E_NOINTERFACE);
        }

        AddRef();

    LExit:
        return hr;
    }

    virtual STDMETHODIMP_(ULONG) AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }

    virtual STDMETHODIMP_(ULONG) Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }

public: // IBootstrapperEngine
    virtual STDMETHODIMP GetPackageCount(
        __out DWORD* pcPackages
        )
    {
        HRESULT hr = S_OK;
        if (pcPackages)
        {
            *pcPackages = m_pEngineState->packages.cPackages;
        }
        else
        {
            hr = E_INVALIDARG;
        }

        return hr;
    }

    // The contents of pllValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
    virtual STDMETHODIMP GetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __out LONGLONG* pllValue
        )
    {
        HRESULT hr = S_OK;

        if (wzVariable && *wzVariable && pllValue)
        {
            hr = VariableGetNumeric(&m_pEngineState->variables, wzVariable, pllValue);
        }
        else
        {
            hr = E_INVALIDARG;
        }

        return hr;
    }

    // The contents of wzValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroFree.
    virtual STDMETHODIMP GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout DWORD* pcchValue
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;
        size_t cchRemaining = 0;

        if (wzVariable && *wzVariable && pcchValue)
        {
            hr = VariableGetString(&m_pEngineState->variables, wzVariable, &sczValue);
            if (SUCCEEDED(hr))
            {
                if (wzValue)
                {
                    hr = ::StringCchCopyExW(wzValue, *pcchValue, sczValue, NULL, &cchRemaining, STRSAFE_FILL_BEHIND_NULL);
                    if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
                    {
                        hr = E_MOREDATA;

                        ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, &cchRemaining);
                        *pcchValue = cchRemaining + 1;
                    }
                }
                else
                {
                    hr = E_MOREDATA;

                    ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, &cchRemaining);
                    *pcchValue = cchRemaining + 1;
                }
            }
        }
        else
        {
            hr = E_INVALIDARG;
        }

        StrSecureZeroFreeString(sczValue);
        return hr;
    }

    // The contents of wzValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
    virtual STDMETHODIMP GetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __out DWORD64* pqwValue
        )
    {
        HRESULT hr = S_OK;

        if (wzVariable && *wzVariable && pqwValue)
        {
            hr = VariableGetVersion(&m_pEngineState->variables, wzVariable, pqwValue);
        }
        else
        {
            hr = E_INVALIDARG;
        }

        return hr;
    }

    // The contents of wzOut may be sensitive, should keep encrypted and SecureZeroFree.
    virtual STDMETHODIMP FormatString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;
        DWORD cchValue = 0;

        if (wzIn && *wzIn && pcchOut)
        {
            hr = VariableFormatString(&m_pEngineState->variables, wzIn, &sczValue, &cchValue);
            if (SUCCEEDED(hr))
            {
                if (wzOut)
                {
                    hr = ::StringCchCopyExW(wzOut, *pcchOut, sczValue, NULL, NULL, STRSAFE_FILL_BEHIND_NULL);
                    if (FAILED(hr))
                    {
                        *pcchOut = cchValue;
                        if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
                        {
                            hr = E_MOREDATA;
                        }
                    }
                }
                else
                {
                    hr = E_MOREDATA;
                    *pcchOut = cchValue;
                }
            }
        }
        else
        {
            hr = E_INVALIDARG;
        }

        StrSecureZeroFreeString(sczValue);
        return hr;
    }

    virtual STDMETHODIMP EscapeString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczValue = NULL;
        size_t cchRemaining = 0;

        if (wzIn && *wzIn && pcchOut)
        {
            hr = VariableEscapeString(wzIn, &sczValue);
            if (SUCCEEDED(hr))
            {
                if (wzOut)
                {
                    hr = ::StringCchCopyExW(wzOut, *pcchOut, sczValue, NULL, &cchRemaining, STRSAFE_FILL_BEHIND_NULL);
                    if (STRSAFE_E_INSUFFICIENT_BUFFER == hr)
                    {
                        hr = E_MOREDATA;
                        ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, &cchRemaining);
                        *pcchOut = cchRemaining;
                    }
                }
                else
                {
                    ::StringCchLengthW(sczValue, STRSAFE_MAX_CCH, &cchRemaining);
                    *pcchOut = cchRemaining;
                }
            }
        }
        else
        {
            hr = E_INVALIDARG;
        }

        StrSecureZeroFreeString(sczValue);
        return hr;
    }

    virtual STDMETHODIMP EvaluateCondition(
        __in_z LPCWSTR wzCondition,
        __out BOOL* pf
        )
    {
        HRESULT hr = S_OK;

        if (wzCondition && *wzCondition && pf)
        {
            hr = ConditionEvaluate(&m_pEngineState->variables, wzCondition, pf);
        }
        else
        {
            hr = E_INVALIDARG;
        }

        return hr;
    }

    virtual STDMETHODIMP Log(
        __in BOOTSTRAPPER_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        )
    {
        HRESULT hr = S_OK;
        REPORT_LEVEL rl = REPORT_NONE;

        switch (level)
        {
        case BOOTSTRAPPER_LOG_LEVEL_STANDARD:
            rl = REPORT_STANDARD;
            break;

        case BOOTSTRAPPER_LOG_LEVEL_VERBOSE:
            rl = REPORT_VERBOSE;
            break;

        case BOOTSTRAPPER_LOG_LEVEL_DEBUG:
            rl = REPORT_DEBUG;
            break;

        case BOOTSTRAPPER_LOG_LEVEL_ERROR:
            rl = REPORT_ERROR;
            break;

        default:
            ExitFunction1(hr = E_INVALIDARG);
        }

        hr = LogStringLine(rl, "%ls", wzMessage);
        ExitOnFailure(hr, "Failed to log UX message.");

    LExit:
        return hr;
    }

    virtual STDMETHODIMP SendEmbeddedError(
        __in DWORD dwErrorCode,
        __in_z_opt LPCWSTR wzMessage,
        __in DWORD dwUIHint,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BYTE* pbData = NULL;
        DWORD cbData = 0;
        DWORD dwResult = 0;

        if (BURN_MODE_EMBEDDED != m_pEngineState->mode)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
            ExitOnRootFailure(hr, "Application requested to send embedded message when not in embedded mode.");
        }

        hr = BuffWriteNumber(&pbData, &cbData, dwErrorCode);
        ExitOnFailure(hr, "Failed to write error code to message buffer.");

        hr = BuffWriteString(&pbData, &cbData, wzMessage ? wzMessage : L"");
        ExitOnFailure(hr, "Failed to write message string to message buffer.");

        hr = BuffWriteNumber(&pbData, &cbData, dwUIHint);
        ExitOnFailure(hr, "Failed to write UI hint to message buffer.");

        hr = PipeSendMessage(m_pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_ERROR, pbData, cbData, NULL, NULL, &dwResult);
        ExitOnFailure(hr, "Failed to send embedded message over pipe.");

        *pnResult = static_cast<int>(dwResult);

    LExit:
        ReleaseBuffer(pbData);
        return hr;
    }

    virtual STDMETHODIMP SendEmbeddedProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BYTE* pbData = NULL;
        DWORD cbData = 0;
        DWORD dwResult = 0;

        if (BURN_MODE_EMBEDDED != m_pEngineState->mode)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INVALID_STATE);
            ExitOnRootFailure(hr, "Application requested to send embedded progress message when not in embedded mode.");
        }

        hr = BuffWriteNumber(&pbData, &cbData, dwProgressPercentage);
        ExitOnFailure(hr, "Failed to write progress percentage to message buffer.");

        hr = BuffWriteNumber(&pbData, &cbData, dwOverallProgressPercentage);
        ExitOnFailure(hr, "Failed to write overall progress percentage to message buffer.");

        hr = PipeSendMessage(m_pEngineState->embeddedConnection.hPipe, BURN_EMBEDDED_MESSAGE_TYPE_PROGRESS, pbData, cbData, NULL, NULL, &dwResult);
        ExitOnFailure(hr, "Failed to send embedded progress message over pipe.");

        *pnResult = static_cast<int>(dwResult);

    LExit:
        ReleaseBuffer(pbData);
        return hr;
    }

    virtual STDMETHODIMP SetUpdate(
        __in_z_opt LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource,
        __in DWORD64 qwSize,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
        __in_bcount_opt(cbHash) BYTE* rgbHash,
        __in DWORD cbHash
        )
    {
        HRESULT hr = S_OK;
        LPCWSTR sczId = NULL;
        LPWSTR sczLocalSource = NULL;
        LPWSTR sczCommandline = NULL;
        UUID guid = {};
        WCHAR wzGuid[39];
        RPC_STATUS rs = RPC_S_OK;

        ::EnterCriticalSection(&m_pEngineState->csActive);

        if ((!wzLocalSource || !*wzLocalSource) && (!wzDownloadSource || !*wzDownloadSource))
        {
            UpdateUninitialize(&m_pEngineState->update);
        }
        else if (BOOTSTRAPPER_UPDATE_HASH_TYPE_NONE == hashType && (0 != cbHash || rgbHash))
        {
            hr = E_INVALIDARG;
        }
        else if (BOOTSTRAPPER_UPDATE_HASH_TYPE_SHA1 == hashType && (SHA1_HASH_LEN != cbHash || !rgbHash))
        {
            hr = E_INVALIDARG;
        }
        else
        {
            UpdateUninitialize(&m_pEngineState->update);

            if (!wzLocalSource || !*wzLocalSource)
            {
                hr = StrAllocFormatted(&sczLocalSource, L"update\\%ls", m_pEngineState->registration.sczExecutableName);
                ExitOnFailure(hr, "Failed to default local update source");
            }

            hr = CoreRecreateCommandLine(&sczCommandline, BOOTSTRAPPER_ACTION_INSTALL, m_pEngineState->command.display, m_pEngineState->command.restart, BOOTSTRAPPER_RELATION_NONE, FALSE, m_pEngineState->registration.sczActiveParent, m_pEngineState->registration.sczAncestors, NULL, m_pEngineState->command.wzCommandLine);
            ExitOnFailure(hr, "Failed to recreate command-line for update bundle.");

            // Per-user bundles would fail to use the downloaded update bundle, as the existing install would already be cached 
            // at the registration id's location.  Here I am generating a random guid, but in the future it would be nice if the
            // feed would provide the ID of the update.
            if (!m_pEngineState->registration.fPerMachine)
            {
                rs = ::UuidCreate(&guid);
                hr = HRESULT_FROM_RPC(rs);
                ExitOnFailure(hr, "Failed to create bundle update guid.");

                if (!::StringFromGUID2(guid, wzGuid, countof(wzGuid)))
                {
                    hr = E_OUTOFMEMORY;
                    ExitOnRootFailure(hr, "Failed to convert bundle update guid into string.");
                }

                sczId = wzGuid;
            }
            else
            {
                sczId = m_pEngineState->registration.sczId;
            }

            hr = PseudoBundleInitialize(FILEMAKEVERSION(rmj, rmm, rup, 0), &m_pEngineState->update.package, FALSE, sczId, BOOTSTRAPPER_RELATION_UPDATE, BOOTSTRAPPER_PACKAGE_STATE_ABSENT, m_pEngineState->registration.sczExecutableName, sczLocalSource ? sczLocalSource : wzLocalSource, wzDownloadSource, qwSize, TRUE, sczCommandline, NULL, NULL, NULL, rgbHash, cbHash);
            ExitOnFailure(hr, "Failed to set update bundle.");

            m_pEngineState->update.fUpdateAvailable = TRUE;
        }

    LExit:
        ::LeaveCriticalSection(&m_pEngineState->csActive);

        ReleaseStr(sczCommandline);
        ReleaseStr(sczLocalSource);
        return hr;
    }

    virtual STDMETHODIMP SetLocalSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzPath
        )
    {
        HRESULT hr = S_OK;
        BURN_CONTAINER* pContainer = NULL;
        BURN_PAYLOAD* pPayload = NULL;

        ::EnterCriticalSection(&m_pEngineState->csActive);
        hr = UserExperienceEnsureEngineInactive(&m_pEngineState->userExperience);
        ExitOnFailure(hr, "Engine is active, cannot change engine state.");

        if (!wzPath || !*wzPath)
        {
            hr = E_INVALIDARG;
        }
        else if (wzPayloadId && * wzPayloadId)
        {
            hr = PayloadFindById(&m_pEngineState->payloads, wzPayloadId, &pPayload);
            ExitOnFailure1(hr, "UX requested unknown payload with id: %ls", wzPayloadId);

            if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_OPERATION);
                ExitOnFailure1(hr, "UX denied while trying to set source on embedded payload: %ls", wzPayloadId);
            }

            hr = StrAllocString(&pPayload->sczSourcePath, wzPath, 0);
            ExitOnFailure(hr, "Failed to set source path for payload.");
        }
        else if (wzPackageOrContainerId && *wzPackageOrContainerId)
        {
            hr = ContainerFindById(&m_pEngineState->containers, wzPackageOrContainerId, &pContainer);
            ExitOnFailure1(hr, "UX requested unknown container with id: %ls", wzPackageOrContainerId);

            hr = StrAllocString(&pContainer->sczSourcePath, wzPath, 0);
            ExitOnFailure(hr, "Failed to set source path for container.");
        }
        else
        {
            hr = E_INVALIDARG;
        }

    LExit:
        ::LeaveCriticalSection(&m_pEngineState->csActive);
        return hr;
    }

    virtual STDMETHODIMP SetDownloadSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzUrl,
        __in_z_opt LPCWSTR wzUser,
        __in_z_opt LPCWSTR wzPassword
        )
    {
        HRESULT hr = S_OK;
        BURN_CONTAINER* pContainer = NULL;
        BURN_PAYLOAD* pPayload = NULL;
        DOWNLOAD_SOURCE* pDownloadSource = NULL;

        ::EnterCriticalSection(&m_pEngineState->csActive);
        hr = UserExperienceEnsureEngineInactive(&m_pEngineState->userExperience);
        ExitOnFailure(hr, "Engine is active, cannot change engine state.");

        if (wzPayloadId && * wzPayloadId)
        {
            hr = PayloadFindById(&m_pEngineState->payloads, wzPayloadId, &pPayload);
            ExitOnFailure1(hr, "UX requested unknown payload with id: %ls", wzPayloadId);

            if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_OPERATION);
                ExitOnFailure1(hr, "UX denied while trying to set download URL on embedded payload: %ls", wzPayloadId);
            }

            pDownloadSource = &pPayload->downloadSource;
        }
        else if (wzPackageOrContainerId && *wzPackageOrContainerId)
        {
            hr = ContainerFindById(&m_pEngineState->containers, wzPackageOrContainerId, &pContainer);
            ExitOnFailure1(hr, "UX requested unknown container with id: %ls", wzPackageOrContainerId);

            pDownloadSource = &pContainer->downloadSource;
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "UX did not provide container or payload id.");
        }

        if (wzUrl && *wzUrl)
        {
            hr = StrAllocString(&pDownloadSource->sczUrl, wzUrl, 0);
            ExitOnFailure(hr, "Failed to set download URL.");

            if (wzUser && *wzUser)
            {
                hr = StrAllocString(&pDownloadSource->sczUser, wzUser, 0);
                ExitOnFailure(hr, "Failed to set download user.");

                if (wzPassword && *wzPassword)
                {
                    hr = StrAllocString(&pDownloadSource->sczPassword, wzPassword, 0);
                    ExitOnFailure(hr, "Failed to set download password.");
                }
                else // no password.
                {
                    ReleaseNullStr(pDownloadSource->sczPassword);
                }
            }
            else // no user means no password either.
            {
                ReleaseNullStr(pDownloadSource->sczUser);
                ReleaseNullStr(pDownloadSource->sczPassword);
            }
        }
        else // no URL provided means clear out the whole download source.
        {
            ReleaseNullStr(pDownloadSource->sczUrl);
            ReleaseNullStr(pDownloadSource->sczUser);
            ReleaseNullStr(pDownloadSource->sczPassword);
        }

    LExit:
        ::LeaveCriticalSection(&m_pEngineState->csActive);
        return hr;
    }

    virtual STDMETHODIMP SetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        )
    {
        HRESULT hr = S_OK;

        if (wzVariable && *wzVariable)
        {
            hr = VariableSetNumeric(&m_pEngineState->variables, wzVariable, llValue, FALSE);
            ExitOnFailure(hr, "Failed to set numeric variable.");
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "UX did not provide variable name.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP SetVariableString(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        )
    {
        HRESULT hr = S_OK;

        if (wzVariable && *wzVariable)
        {
            hr = VariableSetString(&m_pEngineState->variables, wzVariable, wzValue, FALSE);
            ExitOnFailure(hr, "Failed to set numeric variable.");
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "UX did not provide variable name.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP SetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __in DWORD64 qwValue
        )
    {
        HRESULT hr = S_OK;

        if (wzVariable && *wzVariable)
        {
            hr = VariableSetVersion(&m_pEngineState->variables, wzVariable, qwValue, FALSE);
            ExitOnFailure(hr, "Failed to set version variable.");
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "UX did not provide variable name.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP CloseSplashScreen()
    {
        // If the splash screen is still around, close it.
        if (::IsWindow(m_pEngineState->command.hwndSplashScreen))
        {
            ::PostMessageW(m_pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
        }

        return S_OK;
    }

    virtual STDMETHODIMP Detect(
        __in_opt HWND hwndParent
        )
    {
        HRESULT hr = S_OK;

        if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_DETECT, 0, reinterpret_cast<LPARAM>(hwndParent)))
        {
            ExitWithLastError(hr, "Failed to post detect message.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Plan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        HRESULT hr = S_OK;

        if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_PLAN, 0, action))
        {
            ExitWithLastError(hr, "Failed to post plan message.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Elevate(
        __in_opt HWND hwndParent
        )
    {
        HRESULT hr = S_OK;

        if (INVALID_HANDLE_VALUE != m_pEngineState->companionConnection.hPipe)
        {
            hr = HRESULT_FROM_WIN32(ERROR_ALREADY_INITIALIZED);
        }
        else if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_ELEVATE, 0, reinterpret_cast<LPARAM>(hwndParent)))
        {
            ExitWithLastError(hr, "Failed to post elevate message.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Apply(
        __in_opt HWND hwndParent
        )
    {
        HRESULT hr = S_OK;

        if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_APPLY, 0, reinterpret_cast<LPARAM>(hwndParent)))
        {
            ExitWithLastError(hr, "Failed to post apply message.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Quit(
        __in DWORD dwExitCode
        )
    {
        HRESULT hr = S_OK;

        if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_QUIT, static_cast<WPARAM>(dwExitCode), 0))
        {
            ExitWithLastError(hr, "Failed to post shutdown message.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP LaunchApprovedExe(
        __in_opt HWND hwndParent,
        __in_z LPCWSTR wzApprovedExeForElevationId,
        __in_z_opt LPCWSTR wzArguments,
        __in DWORD dwWaitForInputIdleTimeout
        )
    {
        HRESULT hr = S_OK;
        BURN_APPROVED_EXE* pApprovedExe = NULL;
        BOOL fLeaveCriticalSection = FALSE;
        BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe = (BURN_LAUNCH_APPROVED_EXE*)MemAlloc(sizeof(BURN_LAUNCH_APPROVED_EXE), TRUE);

        ::EnterCriticalSection(&m_pEngineState->csActive);
        fLeaveCriticalSection = TRUE;
        hr = UserExperienceEnsureEngineInactive(&m_pEngineState->userExperience);
        ExitOnFailure(hr, "Engine is active, cannot change engine state.");

        if (!wzApprovedExeForElevationId || !*wzApprovedExeForElevationId)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }

        hr = ApprovedExesFindById(&m_pEngineState->approvedExes, wzApprovedExeForElevationId, &pApprovedExe);
        ExitOnFailure1(hr, "UX requested unknown approved exe with id: %ls", wzApprovedExeForElevationId);

        ::LeaveCriticalSection(&m_pEngineState->csActive);
        fLeaveCriticalSection = FALSE;

        hr = StrAllocString(&pLaunchApprovedExe->sczId, wzApprovedExeForElevationId, NULL);
        ExitOnFailure(hr, "Failed to copy the id.");

        if (wzArguments)
        {
            hr = StrAllocString(&pLaunchApprovedExe->sczArguments, wzArguments, NULL);
            ExitOnFailure(hr, "Failed to copy the arguments.");
        }

        pLaunchApprovedExe->dwWaitForInputIdleTimeout = dwWaitForInputIdleTimeout;

        pLaunchApprovedExe->hwndParent = hwndParent;

        if (!::PostThreadMessageW(m_dwThreadId, WM_BURN_LAUNCH_APPROVED_EXE, 0, reinterpret_cast<LPARAM>(pLaunchApprovedExe)))
        {
            ExitWithLastError(hr, "Failed to post launch approved exe message.");
        }

    LExit:
        if (fLeaveCriticalSection)
        {
            ::LeaveCriticalSection(&m_pEngineState->csActive);
        }

        if (FAILED(hr))
        {
            ApprovedExesUninitializeLaunch(pLaunchApprovedExe);
        }

        return hr;
    }

public: // IMarshal
    virtual STDMETHODIMP GetUnmarshalClass( 
        __in REFIID /*riid*/,
        __in_opt LPVOID /*pv*/,
        __in DWORD /*dwDestContext*/,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out LPCLSID /*pCid*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP GetMarshalSizeMax(
        __in REFIID riid,
        __in_opt LPVOID /*pv*/,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out DWORD *pSize
        )
    {
        HRESULT hr = S_OK;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pSize, hr, E_FAIL, "Invalid size output parameter is NULL.");

        // Specify enough size to marshal just the interface pointer across threads.
        *pSize = sizeof(LPVOID);

    LExit:
        return hr;
    }

    virtual STDMETHODIMP MarshalInterface( 
        __in IStream* pStm,
        __in REFIID riid,
        __in_opt LPVOID pv,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/
        )
    {
        HRESULT hr = S_OK;
        IBootstrapperEngine *pThis = NULL;
        ULONG ulWritten = 0;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // "pv" may not be set, so we should us "this" otherwise.
        if (pv)
        {
            pThis = reinterpret_cast<IBootstrapperEngine*>(pv);
        }
        else
        {
            pThis = static_cast<IBootstrapperEngine*>(this);
        }

        // E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");

        // Marshal the interface pointer in-proc as is.
        hr = pStm->Write(pThis, sizeof(pThis), &ulWritten);
        if (STG_E_MEDIUMFULL == hr)
        {
            ExitOnFailure(hr, "Failed to write the stream because the stream is full.");
        }
        else if (FAILED(hr))
        {
            // All other STG error must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to write the IBootstrapperEngine interface pointer to the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP UnmarshalInterface(
        __in IStream* pStm,
        __in REFIID riid,
        __deref_out LPVOID* ppv
        )
    {
        HRESULT hr = S_OK;
        ULONG ulRead = 0;

        // We only support marshaling the engine in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");
        ExitOnNull(ppv, hr, E_FAIL, "The interface output parameter is NULL.");

        // Unmarshal the interface pointer in-proc as is.
        hr = pStm->Read(*ppv, sizeof(LPVOID), &ulRead);
        if (FAILED(hr))
        {
            // All STG errors must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to read the IBootstrapperEngine interface pointer from the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP ReleaseMarshalData(
        __in IStream* /*pStm*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP DisconnectObject(
        __in DWORD /*dwReserved*/
        )
    {
        return E_NOTIMPL;
    }

public:
    CEngineForApplication(
        __in BURN_ENGINE_STATE* pEngineState,
        __in DWORD dwThreadId
        )
    {
        m_cReferences = 1;
        m_pEngineState = pEngineState;
        m_dwThreadId = dwThreadId;
    }

private:
    long m_cReferences;
    BURN_ENGINE_STATE* m_pEngineState;
    DWORD m_dwThreadId;
};


extern "C" HRESULT EngineForApplicationCreate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in DWORD dwThreadId,
    __out IBootstrapperEngine** ppEngineForApplication
    )
{
    HRESULT hr = S_OK;

    CEngineForApplication* pEngine = new CEngineForApplication(pEngineState, dwThreadId);
    ExitOnNull(pEngine, hr, E_OUTOFMEMORY, "Failed to allocate new BootstrapperEngineForApplication object.");

    hr = pEngine->QueryInterface(IID_PPV_ARGS(ppEngineForApplication));
    ExitOnFailure(hr, "Failed to QI for IBootstrapperEngine from BootstrapperEngineForApplication object.");

LExit:
    ReleaseObject(pEngine);
    return hr;
}
