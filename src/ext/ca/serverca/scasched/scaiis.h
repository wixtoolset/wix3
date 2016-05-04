#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


HRESULT ScaMetabaseTransaction(__in_z LPCWSTR wzBackup);

HRESULT ScaCreateWeb(IMSAdminBase* piMetabase, LPCWSTR wzWeb, LPCWSTR wzWebBase);

HRESULT ScaDeleteApp(IMSAdminBase* piMetabase, LPCWSTR wzWebRoot);

HRESULT ScaCreateApp(IMSAdminBase* piMetabase, LPCWSTR wzWebRoot,
                     DWORD dwIsolation);

HRESULT ScaCreateMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
                             LPCWSTR wzSubKey);

HRESULT ScaDeleteMetabaseKey(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
                             LPCWSTR wzSubKey);

HRESULT ScaWriteMetabaseValue(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
                              LPCWSTR wzSubKey, DWORD dwIdentifier,
                              DWORD dwAttributes, DWORD dwUserType,
                              DWORD dwDataType, LPVOID pvData);

HRESULT ScaDeleteMetabaseValue(IMSAdminBase* piMetabase, LPCWSTR wzRootKey,
                              LPCWSTR wzSubKey, DWORD dwIdentifier,
                              DWORD dwDataType);

HRESULT ScaWriteConfigurationScript(LPCWSTR pwzCaScriptKey);

HRESULT ScaAddToIisConfiguration(LPCWSTR pwzData, DWORD dwCost);

HRESULT ScaLoadMetabase(IMSAdminBase** piMetabase);
