#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


HRESULT ScaScheduleIIS7Configuration();

HRESULT ScaIIS7ConfigTransaction(__in_z LPCWSTR wzBackup);

HRESULT ScaCreateApp7(__in_z LPCWSTR wzWebRoot, DWORD dwIsolation);

HRESULT ScaDeleteConfigElement(IIS_CONFIG_ACTION emElement, LPCWSTR wzSubKey);

HRESULT ScaWriteConfigString(__in_z const LPCWSTR wzValue);

HRESULT ScaWriteConfigID(IIS_CONFIG_ACTION emID);

HRESULT ScaWriteConfigInteger(DWORD dwValue);
