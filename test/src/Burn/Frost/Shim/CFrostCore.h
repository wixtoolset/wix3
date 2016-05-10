#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Frost
{
    class CFrostCore : public IBurnCore
    {
    public:
        CFrostCore();
        virtual ~CFrostCore();

        // IUnknown
        /*
        virtual HRESULT __stdcall QueryInterface(__in const IID& riid,  __out void** ppvObject);
        virtual ULONG __stdcall AddRef();
        virtual ULONG __stdcall Release();
        */

        // IBurnCore

        STDMETHODIMP Elevate(__in_opt HWND hwndParent);
        STDMETHODIMP Detect();
        STDMETHODIMP Plan(__in BURN_ACTION action);
        STDMETHODIMP Apply(__in_opt HWND hwndParent);
        STDMETHODIMP Suspend();
        STDMETHODIMP Reboot();
        STDMETHODIMP GetPackageCount(__out DWORD* pcPackages);
        STDMETHODIMP GetCommandLineParameters(__out_ecount_opt(*pcchCommandLine) LPWSTR psczCommandLine, __inout DWORD* pcchCommandLine);
        STDMETHODIMP GetVariableNumeric(__in_z LPCWSTR wzProperty, __out LONGLONG* pllValue);
        STDMETHODIMP GetVariableString( __in_z LPCWSTR wzVariable, __out_ecount_opt(*pcchValue) LPWSTR wzValue, __inout DWORD* pcchValue);
        STDMETHODIMP GetVariableVersion(__in_z LPCWSTR wzProperty, __in DWORD64* pqwValue);
        STDMETHODIMP SetVariableNumeric(__in_z LPCWSTR wzProperty, __in LONGLONG llValue);
        STDMETHODIMP SetVariableString(__in_z LPCWSTR wzProperty, __in_z_opt LPCWSTR wzValue);
        STDMETHODIMP SetVariableVersion(__in_z LPCWSTR wzProperty, __in DWORD64 qwValue);
        STDMETHODIMP FormatString(__in_z LPCWSTR wzIn, __out_ecount_opt(*pcchOut) LPWSTR wzOut, __inout DWORD* pcchOut);
        STDMETHODIMP EscapeString(__in_z LPCWSTR wzIn, __out_ecount_opt(*pcchOut) LPWSTR wzOut, __inout DWORD* pcchOut);
        STDMETHODIMP EvaluateCondition(__in_z LPCWSTR wzCondition, __out BOOL* pf);
        STDMETHODIMP Log(__in BURN_LOG_LEVEL level, __in_z LPCWSTR wzMessage);
        STDMETHODIMP SetSource(__in LPCWSTR wzSourcePath);

    private:
        long m_cReferences;
    };
}
}
}
}
}
