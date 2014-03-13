//-------------------------------------------------------------------------------------------------
// <copyright file="CFrostCore.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    CFrostCore defines the proxy engine's core.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"
#include "dutil.h"
#include "strutil.h"
#include "shim.h"
#include "CFrostCore.h"
#include "CFrostEngine.h"

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

    /*
    HRESULT __stdcall CFrostCore::QueryInterface(
        __in const IID& riid,
        __out void** ppvObject
        )
    {
        HRESULT hr = S_OK;

        if(ppvObject) return E_INVALIDARG;

        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBurnCore), riid))
        {
            *ppvObject = static_cast<IBurnCore*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            return E_NOINTERFACE;
        }

        AddRef();

        return hr;
    }


    ULONG __stdcall CFrostCore::AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }


    ULONG __stdcall CFrostCore::Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }


    */
    STDMETHODIMP CFrostCore::GetPackageCount(
        __out DWORD* pcPackages
        )
    {
        HRESULT hr = S_OK;

        if (pcPackages == NULL)
        {
            hr = E_INVALIDARG;
        }
        else
        {
            *pcPackages = 0;

            System::UInt32 count;
            HRESULTS result = CFrostEngine::GetPackageCount(count);
            if (result == HRESULTS::HR_S_OK)
            {
                *pcPackages = (DWORD)count;
                hr = S_OK;
            }
            else if (result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::GetCommandLineParameters(
        __out_ecount_opt(*pcchCommandLine) LPWSTR psczCommandLine,
        __inout DWORD* pcchCommandLine
        )
    {
        HRESULT hr = S_OK;

        System::String^ cmdLine = gcnew String( psczCommandLine ); 

        HRESULTS result = CFrostEngine::GetCommandLineParameters(cmdLine, (UInt32%)pcchCommandLine);
        if (result == HRESULTS::HR_S_OK)
        {
            IntPtr p = Marshal::StringToHGlobalUni(cmdLine);
            hr = StrAllocString(&psczCommandLine, static_cast<wchar_t*>(p.ToPointer()), (DWORD_PTR)pcchCommandLine);
            Marshal::FreeHGlobal(p);
        }
        else
        {
            StrAllocString(&psczCommandLine, L"", 0);
            if (result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }


    STDMETHODIMP CFrostCore::GetVariableNumeric(
        __in_z LPCWSTR wzProperty,
        __out LONGLONG* pllValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzProperty);
        Int64 TargetPropertyValue = 0;

        HRESULTS result = CFrostEngine::GetPropertyNumeric(TargetPropertyName,TargetPropertyValue);
        if(result == HRESULTS::HR_S_OK)
        {
            *pllValue = TargetPropertyValue;
        }
        else
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR pwzValue,
        __inout DWORD* pcchValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzVariable);
        String^ TargetPropertyValue = String::Empty;
        UInt64 TargetValueSize = (UInt64)*pcchValue;

        HRESULTS result = CFrostEngine::GetPropertyString(TargetPropertyName,TargetPropertyValue,TargetValueSize);
        if(result == HRESULTS::HR_S_OK)
        {
            IntPtr p = Marshal::StringToHGlobalUni(TargetPropertyValue);
            pwzValue = new WCHAR[TargetValueSize+1];
            hr = StringCchCopy(pwzValue, TargetValueSize + 1, static_cast<wchar_t*>(p.ToPointer()));
            Marshal::FreeHGlobal(p);

            *pcchValue = (DWORD)TargetValueSize;
        }
        else
        {
            pwzValue = new WCHAR[1];
            StringCchCopy(pwzValue, 1, L"");
            *pcchValue = 0;

            if (result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::GetVariableVersion(
        __in_z LPCWSTR wzProperty,
        __out DWORD64* pqwValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzProperty);
        UInt64 TargetPropertyValue = 0;

        HRESULTS result = CFrostEngine::GetPropertyVersion(TargetPropertyName,TargetPropertyValue);
        if(result == HRESULTS::HR_S_OK)
        {
            *pqwValue = (DWORD64)TargetPropertyValue;
        }
        else
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::SetVariableNumeric(
        __in_z LPCWSTR wzProperty,
        __in LONGLONG llValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzProperty);
        Int64 TargetPropertyValue = (Int64)llValue;

        HRESULTS result = CFrostEngine::SetPropertyNumeric(TargetPropertyName,TargetPropertyValue);
        
        if(result != HRESULTS::HR_S_OK)
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::SetVariableString(
        __in_z LPCWSTR wzProperty,
        __in_z_opt LPCWSTR wzValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzProperty);
        String^ TargetPropertyValue = gcnew String(wzValue);

        HRESULTS result = CFrostEngine::SetPropertyString(TargetPropertyName,TargetPropertyValue);
        
        if(result != HRESULTS::HR_S_OK)
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::SetVariableVersion(
        __in_z LPCWSTR wzProperty,
        __in DWORD64 qwValue
        )
    {
        HRESULT hr = S_OK;

        String^ TargetPropertyName = gcnew String(wzProperty);
        UInt64 TargetPropertyValue = (UInt64)qwValue;

        HRESULTS result = CFrostEngine::SetPropertyVersion(TargetPropertyName,TargetPropertyValue);

        if(result != HRESULTS::HR_S_OK)
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr; 
    }

    STDMETHODIMP CFrostCore::FormatString(
        __in_z LPCWSTR wzIn,
            __out_ecount_opt(*pcchOut) LPWSTR pwzOut,
            __inout DWORD* pcchOut
        )
    {
        HRESULT hr = S_OK;

        String^ InputString = gcnew String(wzIn);
        String^ OutputString = gcnew String(pwzOut);
        UInt64 StringSize = *pcchOut;

        HRESULTS result = CFrostEngine::FormatPropertyString(InputString,OutputString,StringSize);

        if(result == HRESULTS::HR_S_OK)
        {
            IntPtr p = Marshal::StringToHGlobalUni(OutputString);
            pwzOut = new WCHAR[(int)StringSize+1];
            StringCchCopy(pwzOut,StringSize+1,static_cast<wchar_t*>(p.ToPointer()));
            Marshal::FreeHGlobal(p);

            *pcchOut = (DWORD)StringSize;
        }
        else
        {
            pwzOut = new WCHAR[1];
            StringCchCopy(pwzOut, 1, L"");
            *pcchOut = 0;

            if (result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::EscapeString( __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut)
    {
        // TODO: CALL EVENT TO DO SOMETHING...NOT SURE WHAT THIS FUNCTION DOES

        return E_NOTIMPL;
    }


    STDMETHODIMP CFrostCore::EvaluateCondition(
        __in_z LPCWSTR wzCondition,
        __out BOOL* pf
        )
    {
        HRESULT hr = S_OK;

        String^ Conditional = gcnew String(wzCondition);
        bool Output = false;

        HRESULTS result = CFrostEngine::EvaluateCondition(Conditional,Output);
        if(result != HRESULTS::HR_S_OK)
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Elevate(
        __in_opt HWND hwndParent
        )
    {
        // TODO: FOR NOW hwndParent BEING IGNORED, PASSING nullptr INSTEAD
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Elevate(nullptr);
        if (result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Detect()
    {
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Detect();
        if (result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Plan(
        __in BURN_ACTION action
        )
    {
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Plan(((SETUP_ACTION)((int)action)));
        if (result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Apply(
        __in_opt HWND hwndParent
        )
    {
        // TODO: FOR NOW hwndParent BEING IGNORED, PASSING nullptr INSTEAD
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Apply(nullptr);
        if (result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Suspend()
    {
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Suspend(nullptr);
        if(result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::Reboot()
    {
        HRESULT hr = S_OK;

        HRESULTS result = CFrostEngine::Reboot(nullptr);
        if(result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    STDMETHODIMP CFrostCore::SetSource(__in LPCWSTR wzSourcePath)
    {
        HRESULT hr = S_OK;
        
        String^ SourcePath = gcnew String(wzSourcePath);

        HRESULTS result = CFrostEngine::SetSource(SourcePath);

        if(result != HRESULTS::HR_S_OK)
        {
            if(result == HRESULTS::HR_S_FALSE)
            {
                hr = S_FALSE;
            }
            else
            {
                hr = E_FAIL;
            }
        }

        return hr;
    }
    
    STDMETHODIMP CFrostCore::Log(__in BURN_LOG_LEVEL level, __in_z LPCWSTR wzMessage)
    {
        HRESULT hr = S_OK;
        
        String^ ConvertedString = gcnew String(wzMessage);

        HRESULTS result = CFrostEngine::Log((ENGINE_LOG_LEVEL)((int)level), ConvertedString);

        if(result == HRESULTS::HR_S_OK)
        {
            hr = S_OK;
        }
        else if (result == HRESULTS::HR_S_FALSE)
        {
            hr = S_FALSE;
        }
        else
        {
            hr = E_FAIL;
        }

        return hr;
    }

    CFrostCore::CFrostCore()
    {
        m_cReferences = 1;
    }

    CFrostCore::~CFrostCore()
    {
    }

}
}
}
}
}