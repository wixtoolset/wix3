// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define COST_IIS_WRITEKEY 10

HRESULT ScaIIS7ConfigTransaction(LPCWSTR wzBackup)
{
    HRESULT hr = S_OK;

    hr = WcaDoDeferredAction(L"StartIIS7ConfigTransaction", wzBackup, COST_IIS_TRANSACTIONS);
    ExitOnFailure(hr, "Failed to schedule StartIIS7ConfigTransaction");

    hr = WcaDoDeferredAction(L"RollbackIIS7ConfigTransaction", wzBackup, 0);   // rollback cost is irrelevant
    ExitOnFailure(hr, "Failed to schedule RollbackIIS7ConfigTransaction");

    hr = WcaDoDeferredAction(L"CommitIIS7ConfigTransaction", wzBackup, 0);  // commit is free
    ExitOnFailure(hr, "Failed to schedule StartIIS7ConfigTransaction");

LExit:
    return hr;
}

HRESULT ScaWriteConfigString(const LPCWSTR wzValue)
{
    HRESULT hr = S_OK;
    WCHAR* pwzCustomActionData = NULL;

    hr = WcaWriteStringToCaData(wzValue, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add metabase delete key directive to CustomActionData");

    hr = ScaAddToIisConfiguration(pwzCustomActionData, COST_IIS_WRITEKEY);
    ExitOnFailure2(hr, "Failed to add ScaWriteMetabaseValue action data: %ls, cost: %d", pwzCustomActionData, COST_IIS_WRITEKEY);

LExit:
    ReleaseStr(pwzCustomActionData);

    return hr;
}

HRESULT ScaWriteConfigInteger(DWORD dwValue)
{
    HRESULT hr = S_OK;
    WCHAR* pwzCustomActionData = NULL;

    hr = WcaWriteIntegerToCaData(dwValue, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add metabase delete key directive to CustomActionData");

    hr = ScaAddToIisConfiguration(pwzCustomActionData, COST_IIS_WRITEKEY);
    ExitOnFailure2(hr, "Failed to add ScaWriteMetabaseValue action data: %ls, cost: %d", pwzCustomActionData, COST_IIS_WRITEKEY);

LExit:
    ReleaseStr(pwzCustomActionData);

    return hr;
}

HRESULT ScaWriteConfigID(IIS_CONFIG_ACTION emID)
{
    HRESULT hr = S_OK;
    WCHAR* pwzCustomActionData = NULL;

    hr = WcaWriteIntegerToCaData(emID, &pwzCustomActionData);
    ExitOnFailure(hr, "Failed to add metabase delete key directive to CustomActionData");

    hr = ScaAddToIisConfiguration(pwzCustomActionData, COST_IIS_WRITEKEY);
    ExitOnFailure2(hr, "Failed to add ScaWriteMetabaseValue action data: %ls, cost: %d", pwzCustomActionData, COST_IIS_WRITEKEY);

LExit:
    ReleaseStr(pwzCustomActionData);

    return hr;
}

