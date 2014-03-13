//-------------------------------------------------------------------------------------------------
// <copyright file="bitsengine.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//    Module: Core
//
//    Setup chainer/bootstrapper download engine using BITS.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

// const

const DWORD BITSENGINE_NO_PROGRESS_TIMEOUT = 2 * 60;

// functions

static HRESULT CreateJob(
    __out IBackgroundCopyJob** ppJob
    );
static HRESULT SetCredentials(
    __in IBackgroundCopyJob* pJob,
    __in_z_opt LPCWSTR wzUser,
    __in_z_opt LPCWSTR wzPassword
    );
static void SendError(
    __in BURN_CACHE_CALLBACK* pCacheCallback,
    __in IBackgroundCopyJob* pJob,
    __in HRESULT hrError,
    __in BG_ERROR_CONTEXT context,
    __out_opt BOOL* pfRetry
    );


// class

class CBurnBitsCallback : public IBackgroundCopyCallback
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

        if (::IsEqualIID(__uuidof(IBackgroundCopyCallback), riid))
        {
            *ppvObject = static_cast<IBackgroundCopyCallback*>(this);
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

public: // IBackgroundCopyCallback
    virtual STDMETHODIMP JobTransferred(
        __in IBackgroundCopyJob* pJob
        )
    {
        HRESULT hr = S_OK;

        hr = SendProgress(pJob);
        ExitOnFailure(hr, "Failure while sending progress during BITS job transferred.");

    LExit:
        if (FAILED(hr))
        {
            ProcessResult(BG_ERROR_CONTEXT_NONE, hr);
        }
        else
        {
            ::SetEvent(m_hComplete);
        }

        return S_OK; // must return S_OK otherwise BITS just keeps calling back.
    }

    virtual STDMETHODIMP JobError(
        __in IBackgroundCopyJob* /*pJob*/,
        __in IBackgroundCopyError* pError
        )
    {
        HRESULT hr = S_OK;
        BG_ERROR_CONTEXT context = BG_ERROR_CONTEXT_NONE;
        HRESULT hrError = S_OK;

        hr = pError->GetError(&context, &hrError);
        ExitOnFailure(hr, "Failed to get error context.");

        if (SUCCEEDED(hrError))
        {
            hr = E_UNEXPECTED;
        }

    LExit:
        ProcessResult(context, FAILED(hrError) ? hrError : hr);

        return S_OK; // must return S_OK otherwise BITS just keeps calling back.
    }

    virtual STDMETHODIMP JobModification(
        __in IBackgroundCopyJob* pJob,
        __in DWORD /*dwReserved*/
        )
    {
        HRESULT hr = S_OK;
        BG_JOB_STATE state = BG_JOB_STATE_ERROR;

        ::EnterCriticalSection(&m_cs);

        hr = pJob->GetState(&state);
        ExitOnFailure(hr, "Failed to get state during job modification.");

        // If we're actually downloading stuff, let's send progress.
        if (BG_JOB_STATE_TRANSFERRING == state)
        {
            hr = SendProgress(pJob);
            ExitOnFailure(hr, "Failure while sending progress during BITS job modification.");
        }

    LExit:
        ::LeaveCriticalSection(&m_cs);

        ProcessResult(BG_ERROR_CONTEXT_NONE, hr);

        return S_OK; // documentation says to always return S_OK
    }

public:
    void Reset()
    {
        m_hrError = S_OK;
        m_contextError = BG_ERROR_CONTEXT_NONE;

        ::ResetEvent(m_hComplete);
    }

    HRESULT WaitForCompletion()
    {
        HRESULT hr = S_OK;
        HANDLE rghEvents[1] = { m_hComplete };
        MSG msg = { };
        BOOL fMessageProcessed = FALSE;

        do
        {
            fMessageProcessed = FALSE;

            switch (::MsgWaitForMultipleObjects(countof(rghEvents), rghEvents, FALSE, INFINITE, QS_ALLINPUT))
            {
            case WAIT_OBJECT_0:
                break;

            case WAIT_OBJECT_0 + 1:
                ::PeekMessageW(&msg, NULL, 0, 0, PM_NOREMOVE);
                fMessageProcessed = TRUE;
                break;

            default:
                ExitWithLastError(hr, "Failed while waiting for download.");
            }
        } while(fMessageProcessed);

    LExit:
        return hr;
    }

    void GetError(
        __out HRESULT* pHR,
        __out BG_ERROR_CONTEXT* pContext
        )
    {
        *pHR = m_hrError;
        *pContext = m_contextError;
    }

private:
    HRESULT SendProgress(
        __in IBackgroundCopyJob* pJob
        )
    {
        HRESULT hr = S_OK;
        BG_JOB_PROGRESS progress = { };

        if (m_pCallback && m_pCallback->pfnProgress)
        {
            hr = pJob->GetProgress(&progress);
            ExitOnFailure(hr, "Failed to get progress when BITS job was transferred.");

            hr = CacheSendProgressCallback(m_pCallback, progress.BytesTransferred, progress.BytesTotal, INVALID_HANDLE_VALUE);
            ExitOnFailure(hr, "Failed to send progress from BITS job.");
        }

    LExit:
        return hr;
    }

    void ProcessResult(
        __in BG_ERROR_CONTEXT context,
        __in HRESULT hr
        )
    {
        if (FAILED(hr))
        {
            m_contextError = context;
            m_hrError = hr;

            ::SetEvent(m_hComplete);
        }
    }

public:
    CBurnBitsCallback(
        __in_opt BURN_CACHE_CALLBACK* pCallback,
        __out HRESULT* pHR
        )
    {
        HRESULT hr = S_OK;

        m_cReferences = 1;
        ::InitializeCriticalSection(&m_cs);

        m_hComplete = ::CreateEventW(NULL, TRUE, FALSE, NULL);
        ExitOnNullWithLastError(m_hComplete, hr, "Failed to create BITS job complete event.");

        m_contextError = BG_ERROR_CONTEXT_NONE;
        m_hrError = S_OK;

        m_pCallback = pCallback;

    LExit:
        *pHR = hr;
    }

    ~CBurnBitsCallback()
    {
        m_pCallback = NULL;
        ReleaseHandle(m_hComplete);
        ::DeleteCriticalSection(&m_cs);
    }

private:
    long m_cReferences;
    CRITICAL_SECTION m_cs;
    BG_ERROR_CONTEXT m_contextError;
    HRESULT m_hrError;

    HANDLE m_hComplete;
    BURN_CACHE_CALLBACK* m_pCallback;
};


extern "C" HRESULT BitsDownloadUrl(
    __in BURN_CACHE_CALLBACK* pCallback,
    __in BURN_DOWNLOAD_SOURCE* pDownloadSource,
    __in_z LPCWSTR wzDestinationPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDownloadUrl = NULL;
    CBurnBitsCallback* pBitsCallback = NULL;
    IBackgroundCopyJob* pJob = NULL;
    BOOL fRetry = FALSE;
    BG_ERROR_CONTEXT contextError = BG_ERROR_CONTEXT_NONE;

    // If the URL isn't at least 8 characters long (e.g.: "bits://X") then it
    // isn't going to do us any good.
    if (8 > lstrlenW(pDownloadSource->sczUrl))
    {
        hr = E_INVALIDARG;
        ExitOnRootFailure1(hr, "Invalid BITS engine URL: %ls", pDownloadSource->sczUrl);
    }

    // Fix the URL to be "http" instead of "bits".
    hr = StrAllocString(&sczDownloadUrl, pDownloadSource->sczUrl, 0);
    ExitOnFailure(hr, "Failed to copy download URL.");

    sczDownloadUrl[0] = L'h';
    sczDownloadUrl[1] = L't';
    sczDownloadUrl[2] = L't';
    sczDownloadUrl[3] = L'p';

    // Create and configure the BITS job.
    hr = CreateJob(&pJob);
    ExitOnFailure(hr, "Failed to create BITS job.");

    hr = SetCredentials(pJob, pDownloadSource->sczUser, pDownloadSource->sczPassword);
    ExitOnFailure(hr, "Failed to set credentials for BITS job.");

    hr = pJob->AddFile(sczDownloadUrl, wzDestinationPath);
    ExitOnFailure(hr, "Failed to add file to BITS job.");

    // Set the callback into the BITs job.
    pBitsCallback = new CBurnBitsCallback(pCallback, &hr);
    ExitOnNull(pBitsCallback, hr, E_OUTOFMEMORY, "Failed to create BITS job callback.");
    ExitOnFailure(hr, "Failed to initialize BITS job callback.");

    hr = pJob->SetNotifyInterface(pBitsCallback);
    ExitOnFailure(hr, "Failed to set callback interface for BITS job.");

    // Go into our retry download loop.
    do
    {
        fRetry = FALSE;

        pBitsCallback->Reset(); // ensure we are ready for the download to start (again?).

        hr = pJob->Resume();
        ExitOnFailure(hr, "Falied to start BITS job.");

        hr = pBitsCallback->WaitForCompletion();
        ExitOnFailure(hr, "Failed while waiting for BITS download.");

        // See if there are any errors.
        pBitsCallback->GetError(&hr, &contextError);
        if (HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hr)
        {
            ExitFunction();
        }
        else if (FAILED(hr))
        {
            SendError(pCallback, pJob, hr, contextError, &fRetry);
        }
    } while (fRetry);
    ExitOnFailure(hr, "Failed to download BITS job.");

    // After all that, we should have the file downloaded so complete the job to get
    // the file copied to the destination.
    hr = pJob->Complete();
    ExitOnFailure(hr, "Failed to complete BITS job.");

LExit:
    if (pJob)
    {
        pJob->SetNotifyInterface(NULL);

        // If we failed, kill the job.
        if (FAILED(hr))
        {
            pJob->Cancel(); // TODO: should we cancel if we're going to retry the package? Probably the right thing to do.
        }
    }

    ReleaseObject(pBitsCallback);
    ReleaseObject(pJob);
    ReleaseStr(sczDownloadUrl);

    return hr;
}

static HRESULT CreateJob(
    __out IBackgroundCopyJob** ppJob
    )
{
    HRESULT hr = S_OK;
    IBackgroundCopyManager* pBitsManager = NULL;
    IBackgroundCopyJob* pJob = NULL;
    GUID guidJob = { };

    hr = ::CoCreateInstance(__uuidof(BackgroundCopyManager), NULL, CLSCTX_ALL, __uuidof(IBackgroundCopyManager), reinterpret_cast<LPVOID*>(&pBitsManager));
    ExitOnFailure(hr, "Failed to create IBackgroundCopyManager.");

    hr = pBitsManager->CreateJob(L"WixBurn", BG_JOB_TYPE_DOWNLOAD, &guidJob, &pJob);
    ExitOnFailure(hr, "Failed to create BITS job.");

    hr = pJob->SetNotifyFlags(BG_NOTIFY_JOB_TRANSFERRED | BG_NOTIFY_JOB_ERROR | BG_NOTIFY_JOB_MODIFICATION);
    ExitOnFailure(hr, "Failed to set notification flags for BITS job.");

    hr = pJob->SetNoProgressTimeout(BITSENGINE_NO_PROGRESS_TIMEOUT); // use 2 minutes since default is 14 days.
    ExitOnFailure(hr, "Failed to set progress timeout.");

    hr = pJob->SetPriority(BG_JOB_PRIORITY_FOREGROUND);
    ExitOnFailure(hr, "Failed to set BITS job to foreground.");

    *ppJob = pJob;
    pJob = NULL;

LExit:
    ReleaseObject(pJob);
    ReleaseObject(pBitsManager);

    return hr;
}

static HRESULT SetCredentials(
    __in IBackgroundCopyJob* pJob,
    __in_z_opt LPCWSTR wzUser,
    __in_z_opt LPCWSTR wzPassword
    )
{
    HRESULT hr = S_OK;
    IBackgroundCopyJob2* pJob2 = NULL;
    BG_AUTH_CREDENTIALS ac = { };

    // If IBackgroundCopyJob2::SetCredentials() is supported, set the username/password.
    hr = pJob->QueryInterface(IID_PPV_ARGS(&pJob2));
    if (SUCCEEDED(hr))
    {
        ac.Target = BG_AUTH_TARGET_PROXY;
        ac.Credentials.Basic.UserName = const_cast<LPWSTR>(wzUser);
        ac.Credentials.Basic.Password = const_cast<LPWSTR>(wzPassword);

        ac.Scheme = BG_AUTH_SCHEME_NTLM;
        hr = pJob2->SetCredentials(&ac);
        ExitOnFailure(hr, "Failed to set background copy NTLM credentials");

        ac.Scheme = BG_AUTH_SCHEME_NEGOTIATE;
        hr = pJob2->SetCredentials(&ac);
        ExitOnFailure(hr, "Failed to set background copy negotiate credentials");
    }

    hr = S_OK;

LExit:
    ReleaseObject(pJob2);

    return hr;
}

static void SendError(
    __in BURN_CACHE_CALLBACK* pCacheCallback,
    __in IBackgroundCopyJob* pJob,
    __in HRESULT hrError,
    __in BG_ERROR_CONTEXT /*context*/,
    __out_opt BOOL* pfRetry
    )
{
    HRESULT hr = S_OK;
    IBackgroundCopyError* pError = NULL;
    LPWSTR pszErrorDescription = NULL;

    hr = pJob->GetError(&pError);
    if (SUCCEEDED(hr))
    {
        pError->GetErrorDescription(LANGIDFROMLCID(::GetThreadLocale()), &pszErrorDescription);
    }

    CacheSendErrorCallback(pCacheCallback, hrError, pszErrorDescription, pfRetry);

    if (pszErrorDescription)
    {
        ::CoTaskMemFree(pszErrorDescription);
    }
    ReleaseObject(pError);
}
