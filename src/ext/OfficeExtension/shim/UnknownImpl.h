//-------------------------------------------------------------------------------------------------
// <copyright file="UnknownImpl.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

#pragma once

template<typename T>
class CUnknownImpl : public T
{
public:
    CUnknownImpl() : m_cReferences(1)
    {
    }


    virtual ~CUnknownImpl()
    {
    }


    virtual HRESULT __stdcall QueryInterface(
        __in const IID& riid,
        __out void** ppvObject
        )
    {
        HRESULT hr = S_OK;

        ExitOnNull(ppvObject, hr, E_INVALIDARG, "Invalid argument ppvObject");
        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(T), riid))
        {
            *ppvObject = static_cast<T*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            ExitFunction1(hr = E_NOINTERFACE);
        }

        AddRef();
    LExit:
        return hr;
    }


    virtual ULONG __stdcall AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }


    virtual ULONG __stdcall Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }


protected:
    long m_cReferences;
};
