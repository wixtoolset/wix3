// <copyright file="appsynup.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  RSS update functions header.
// </summary>
//
#pragma once

HRESULT RssUpdateTryLaunchUpdate(
    __in LPCWSTR wzAppId,
    __in DWORD64 dw64AppVersion,
    __out HANDLE* phUpdateProcess,
    __out_opt DWORD64* pdw64NextUpdateTime
    );

HRESULT RssUpdateCheckFeed(
    __in LPCWSTR wzAppId,
    __in DWORD64 dw64AppVersion,
    __in LPCWSTR wzFeedUri,
    __in DWORD64 dw64NextUpdateTime
    );

HRESULT RssUpdateGetAppInfo(
    __in LPCWSTR wzApplicationId,
    __out_opt DWORD64* pdw64Version,
    __out_opt LPWSTR* ppwzUpdateFeedUri,
    __out_opt LPWSTR* ppwzApplicationPath
    );

HRESULT RssUpdateGetUpdateInfo(
    __in LPCWSTR wzApplicationId,
    __out_opt DWORD64* pdw64NextUpdate,
    __out_opt BOOL* pfUpdateReady,
    __out_opt DWORD64* pdw64UpdateVersion,
    __out_opt LPWSTR* ppwzLocalFeedPath,
    __out_opt LPWSTR* ppwzLocalSetupPath
    );

HRESULT RssUpdateSetUpdateInfo(
    __in LPCWSTR wzApplicationId,
    __in DWORD64 dw64NextUpdate,
    __in DWORD64 dw64UpdateVersion,
    __in LPCWSTR wzLocalFeedPath,
    __in LPCWSTR wzLocalSetupPath
    );

HRESULT RssUpdateDeleteUpdateInfo(
    __in LPCWSTR wzApplicationId
    );

HRESULT RssUpdateGetFeedInfo(
    __in LPCWSTR wzRssPath,
    __out_opt DWORD* pdwTimetoLive,
    __out_opt LPWSTR* ppwzApplicationId,
    __out_opt DWORD64* pdw64Version,
    __out_opt LPWSTR* ppwzApplicationSource
    );

HRESULT Download(
    __in_opt LPCWSTR wzBasePath,
    __in LPCWSTR wzSourcePath,
    __in LPCWSTR wzDestPath
    );

