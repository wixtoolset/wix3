// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT AppPoolExists(
    __in LPCWSTR wzAppPool
    );

// functions
HRESULT ScaFindAppPool7(
    __in LPCWSTR wzAppPool,
    __out_ecount(cchName) LPWSTR wzName,
    __in DWORD cchName,
    __in SCA_APPPOOL *psapList
    )
{
    Assert(wzAppPool && *wzAppPool && wzName && *wzName);

    HRESULT hr = S_OK;

    // check memory first
    SCA_APPPOOL* psap = psapList;
    for (; psap; psap = psap->psapNext)
    {
        if (0 == wcscmp(psap->wzAppPool, wzAppPool))
        {
            break;
        }
    }
    ExitOnNull1(psap, hr, HRESULT_FROM_WIN32(ERROR_NOT_FOUND), "Could not find the app pool: %ls", wzAppPool);

    // copy the web app pool name
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
    hr = ::StringCchCopyW(wzName, cchName, psap->wzName);
    ExitOnFailure1(hr, "failed to copy app pool name while finding app pool: %ls", psap->wzName);

    // if it's not being installed now, check if it exists already
    if (!psap->fHasComponent)
    {
        hr = AppPoolExists(psap->wzName);
        ExitOnFailure1(hr, "failed to check for existence of app pool: %ls", psap->wzName);
    }

LExit:
    return hr;
}


static HRESULT AppPoolExists(
    __in LPCWSTR /*wzAppPool*/
    )
{
    HRESULT hr = S_OK;

    //this function checks for existance of app pool in IIS7 config
    //at schedule time, we will defer this to execute time.

    return hr;
}


HRESULT ScaAppPoolInstall7(
    __in SCA_APPPOOL* psapList
    )
{
    HRESULT hr = S_OK;

    for (SCA_APPPOOL* psap = psapList; psap; psap = psap->psapNext)
    {
        // if we are installing the app pool
        if (psap->fHasComponent && WcaIsInstalling(psap->isInstalled, psap->isAction))
        {
            hr = ScaWriteAppPool7(psap);
            ExitOnFailure1(hr, "failed to write AppPool '%ls' to metabase", psap->wzAppPool);
        }
    }

LExit:
    return hr;
}


HRESULT ScaAppPoolUninstall7(
    __in SCA_APPPOOL* psapList
    )
{

    HRESULT hr = S_OK;

    for (SCA_APPPOOL* psap = psapList; psap; psap = psap->psapNext)
    {
        // if we are uninstalling the app pool
        if (psap->fHasComponent && WcaIsUninstalling(psap->isInstalled, psap->isAction))
        {
            hr = ScaRemoveAppPool7(psap);
            ExitOnFailure1(hr, "Failed to remove AppPool '%ls' from metabase", psap->wzAppPool);
        }
    }

LExit:
    return hr;
}


HRESULT ScaWriteAppPool7(
    __in const SCA_APPPOOL* psap
    )
{
    Assert(psap);

    HRESULT hr = S_OK;
    DWORD dwIdentity = 0xFFFFFFFF;
    LPWSTR pwzValue = NULL;
    LPWSTR wz = NULL;

    //create the app pool
    hr = ScaWriteConfigID(IIS_APPPOOL);
    ExitOnFailure(hr, "failed to write AppPool key.");

    hr = ScaWriteConfigID(IIS_CREATE);
    ExitOnFailure(hr, "failed to write AppPool create action.");

    hr = ScaWriteConfigString(psap->wzName);
    ExitOnFailure1(hr, "failed to write AppPool name: %ls", psap->wzName);

    // Now do all the optional stuff

    // Set the AppPool Recycling Tab
    if (MSI_NULL_INTEGER != psap->iRecycleMinutes)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_MIN);
        ExitOnFailure(hr, "failed to set periodic restart time id");
        hr = ScaWriteConfigInteger(psap->iRecycleMinutes);
        ExitOnFailure(hr, "failed to set periodic restart time");
    }

    if (MSI_NULL_INTEGER != psap->iRecycleRequests)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_REQ);
        ExitOnFailure(hr, "failed to set periodic restart request count id");
        hr = ScaWriteConfigInteger(psap->iRecycleRequests);
        ExitOnFailure(hr, "failed to set periodic restart request count");
    }

    if (*psap->wzRecycleTimes)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_TIMES);
        ExitOnFailure(hr, "failed to set periodic restart schedule id");
        hr = ScaWriteConfigString(psap->wzRecycleTimes);
        ExitOnFailure(hr, "failed to set periodic restart schedule");
    }

    if (MSI_NULL_INTEGER != psap->iVirtualMemory)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_VIRMEM);
        ExitOnFailure(hr, "failed to set periodic restart memory count id");
        hr = ScaWriteConfigInteger(psap->iVirtualMemory);
        ExitOnFailure(hr, "failed to set periodic restart memory count");
    }

    if (MSI_NULL_INTEGER != psap->iPrivateMemory)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_PRIVMEM);
        ExitOnFailure(hr, "failed to set periodic restart private memory count id");
        hr = ScaWriteConfigInteger(psap->iPrivateMemory);
        ExitOnFailure(hr, "failed to set periodic restart private memory count");
    }

    // Set AppPool Performance Tab
    if (MSI_NULL_INTEGER != psap->iIdleTimeout)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_IDLTIMEOUT);
        ExitOnFailure(hr, "failed to set idle timeout value id");
        hr = ScaWriteConfigInteger(psap->iIdleTimeout);
        ExitOnFailure(hr, "failed to set idle timeout value");
    }

    if (MSI_NULL_INTEGER != psap->iQueueLimit)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_QUEUELIMIT);
        ExitOnFailure(hr, "failed to set request queue limit value id");
        hr = ScaWriteConfigInteger(psap->iQueueLimit);
        ExitOnFailure(hr, "failed to set request queue limit value");
    }
    if (*psap->wzCpuMon)
    {
#pragma prefast(suppress:26037, "Source string is null terminated - it is populated as target of ::StringCchCopyW")
        hr = ::StrAllocString(&pwzValue, psap->wzCpuMon, 0);
        ExitOnFailure(hr, "failed to allocate CPUMonitor string");

        DWORD dwPercent = 0;
        DWORD dwRefreshMinutes = 0;
        DWORD dwAction = 0;

        dwPercent = wcstoul(pwzValue, &wz, 10);
        if (100  < dwPercent)
        {
            ExitOnFailure1(hr = E_INVALIDARG, "invalid maximum cpu percentage value: %d", dwPercent);
        }
        if (wz && L',' == *wz)
        {
            ++wz;
            dwRefreshMinutes = wcstoul(wz, &wz, 10);
            if (wz && L',' == *wz)
            {
                ++wz;
                dwAction = wcstoul(wz, &wz, 10);
            }
        }
        if (dwPercent)
        {
            hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_CPU_PCT);
            ExitOnFailure(hr, "failed to set recycle pct id");
            hr = ScaWriteConfigInteger(dwPercent);
            ExitOnFailure(hr, "failed to set CPU percentage max");
        }
        if (dwRefreshMinutes)
        {
            hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_CPU_REFRESH);
            ExitOnFailure(hr, "failed to set recycle refresh id");
            hr = ScaWriteConfigInteger(dwRefreshMinutes);
            ExitOnFailure(hr, "failed to set refresh CPU minutes");
        }
        if (dwAction)
        {
            // 0 = No Action
            // 1 = Shutdown
            hr = ScaWriteConfigID(IIS_APPPOOL_RECYCLE_CPU_ACTION);
            ExitOnFailure(hr, "failed to set recycle refresh id");
            hr = ScaWriteConfigInteger(dwAction);
            ExitOnFailure(hr, "failed to set CPU action");
        }
    }

    if (MSI_NULL_INTEGER != psap->iMaxProcesses)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_MAXPROCESS);
        ExitOnFailure(hr, "Failed to write max processes config ID");

        hr = ScaWriteConfigInteger(psap->iMaxProcesses);
        ExitOnFailure(hr, "failed to set web garden maximum worker processes");
    }

    hr = ScaWriteConfigID(IIS_APPPOOL_32BIT);
    ExitOnFailure(hr, "Failed to write 32 bit app pool config ID");
    hr = ScaWriteConfigInteger(psap->iCompAttributes & msidbComponentAttributes64bit ? 0 : 1);
    ExitOnFailure(hr, "Failed to write 32 bit app pool config value");

    //
    // Set the AppPool Identity tab
    //
    if (psap->iAttributes & APATTR_APPPOOLIDENTITY)
    {
        dwIdentity = 4;
    }
    else if (psap->iAttributes & APATTR_NETSERVICE)
    {
        dwIdentity = 2;
    }
    else if (psap->iAttributes & APATTR_LOCSERVICE)
    {
        dwIdentity = 1;
    }
    else if (psap->iAttributes & APATTR_LOCSYSTEM)
    {
        dwIdentity = 0;
    }
    else if (psap->iAttributes & APATTR_OTHERUSER)
    {
        if (!*psap->suUser.wzDomain || 0 == _wcsicmp(psap->suUser.wzDomain, L"."))
        {
            if (0 == _wcsicmp(psap->suUser.wzName, L"NetworkService"))
            {
                dwIdentity = 2;
            }
            else if (0 == _wcsicmp(psap->suUser.wzName, L"LocalService"))
            {
                dwIdentity = 1;
            }
            else if (0 == _wcsicmp(psap->suUser.wzName, L"LocalSystem"))
            {
                dwIdentity = 0;
            }
            else
            {
                dwIdentity = 3;
            }
        }
        else if (0 == _wcsicmp(psap->suUser.wzDomain, L"NT AUTHORITY"))
        {
            if (0 == _wcsicmp(psap->suUser.wzName, L"NETWORK SERVICE"))
            {
                dwIdentity = 2;
            }
            else if (0 == _wcsicmp(psap->suUser.wzName, L"SERVICE"))
            {
                dwIdentity = 1;
            }
            else if (0 == _wcsicmp(psap->suUser.wzName, L"SYSTEM"))
            {
                dwIdentity = 0;
            }
            else
            {
                dwIdentity = 3;
            }
        }
        else
        {
            dwIdentity = 3;
        }
    }

    if (-1 != dwIdentity)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_IDENTITY);
        ExitOnFailure(hr, "failed to set app pool identity id");
        hr = ScaWriteConfigInteger(dwIdentity);
        ExitOnFailure(hr, "failed to set app pool identity");

        if (3 == dwIdentity)
        {
            if (*psap->suUser.wzDomain)
            {
                hr = StrAllocFormatted(&pwzValue, L"%s\\%s", psap->suUser.wzDomain, psap->suUser.wzName);
                ExitOnFailure2(hr, "failed to format user name: %ls domain: %ls", psap->suUser.wzName, psap->suUser.wzDomain);
            }
            else
            {
                hr = StrAllocFormatted(&pwzValue, L"%s", psap->suUser.wzName);
                ExitOnFailure1(hr, "failed to format user name: %ls", psap->suUser.wzName);
            }

            hr = ScaWriteConfigID(IIS_APPPOOL_USER);
            ExitOnFailure(hr, "failed to set app pool identity name id");
            hr = ScaWriteConfigString(pwzValue);
            ExitOnFailure(hr, "failed to set app pool identity name");

            hr = ScaWriteConfigID(IIS_APPPOOL_PWD);
            ExitOnFailure(hr, "failed to set app pool identity password id");
            hr = ScaWriteConfigString(psap->suUser.wzPassword);
            ExitOnFailure(hr, "failed to set app pool identity password");
        }
    }
    
    if (*psap->wzManagedPipelineMode)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_MANAGED_PIPELINE_MODE);
        ExitOnFailure(hr, "failed to set app pool integrated mode");
        hr = ScaWriteConfigString(psap->wzManagedPipelineMode);
        ExitOnFailure(hr, "failed to set app pool managed pipeline mode value");
    }

    if (*psap->wzManagedRuntimeVersion)
    {
        hr = ScaWriteConfigID(IIS_APPPOOL_MANAGED_RUNTIME_VERSION);
        ExitOnFailure(hr, "failed to set app pool managed runtime version mode");
        hr = ScaWriteConfigString(psap->wzManagedRuntimeVersion);
        ExitOnFailure(hr, "failed to set app pool managed runtime version value");
    }

    //
    //The number of properties above is variable so we put an end tag in so the
    //execute CA will know when to stop looking for AppPool properties
    //
    hr = ScaWriteConfigID(IIS_APPPOOL_END);
    ExitOnFailure(hr, "failed to set app pool end of properties id");

LExit:
    ReleaseStr(pwzValue);

    return hr;
}


HRESULT ScaRemoveAppPool7(
    __in const SCA_APPPOOL* psap
    )
{
    Assert(psap);

    HRESULT hr = S_OK;

    //do not delete the default App Pool
    if (0 != _wcsicmp(psap->wzAppPool, L"DefaultAppPool"))
    {
        //delete the app pool
        hr = ScaWriteConfigID(IIS_APPPOOL);
        ExitOnFailure(hr, "failed to write AppPool key.");

        hr = ScaWriteConfigID(IIS_DELETE);
        ExitOnFailure(hr, "failed to write AppPool delete action.");

        hr = ScaWriteConfigString(psap->wzName);
        ExitOnFailure1(hr, "failed to delete AppPool: %ls", psap->wzName);
    }

LExit:
    return hr;
}
