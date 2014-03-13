// <copyright file="ConnectProxy.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  Connection proxy header.
// </summary>
//
#pragma once

class CConnectProxy : public CUnknownImpl<IDTExtensibility2>
{
public:
    static HRESULT Create(CConnectProxy** ppNewProxy);

public: // IDispatch
    HRESULT __stdcall GetTypeInfoCount(
        __out unsigned int FAR* pctinfo
        );

    HRESULT __stdcall GetTypeInfo(
        __in unsigned int iTInfo,
        __in LCID lcid,
        __out ITypeInfo FAR* FAR*  ppTInfo
        );

    HRESULT __stdcall GetIDsOfNames(
        __in REFIID  riid, 
        __in OLECHAR FAR* FAR*  rgszNames,
        __in unsigned int  cNames,
        __in LCID lcid,
        __in DISPID FAR*  rgDispId
        );

    HRESULT __stdcall Invoke(
        __in DISPID dispIdMember,
        __in REFIID riid,
        __in LCID lcid,
        __in WORD wFlags,
        __in DISPPARAMS FAR* pDispParams,
        __out VARIANT FAR* pVarResult,
        __out EXCEPINFO FAR* pExcepInfo,
        __out unsigned int FAR* puArgErr
        );

public: // IDTExtensibility2
    HRESULT __stdcall OnConnection(
        __in IDispatch* Application,
        __in ext_ConnectMode ConnectMode,
        __in IDispatch* AddInInst,
        __in SAFEARRAY** custom
        );

    HRESULT __stdcall OnDisconnection(
        __in ext_DisconnectMode RemoveMode,
        __in SAFEARRAY** custom
        );
    
    HRESULT __stdcall OnAddInsUpdate(
        __in SAFEARRAY** custom
        );

    HRESULT __stdcall OnStartupComplete(
        __in SAFEARRAY** custom
        );

    HRESULT __stdcall OnBeginShutdown(
        __in SAFEARRAY** custom
        );

protected:
    CConnectProxy();

    ~CConnectProxy();

    HRESULT Initialize();

protected:
    LPWSTR m_pwzAppId;
    ITypeInfo* m_pTypeInfo;

    BOOL m_fInstanceCreated;
    IDTExtensibility2* m_pConnect; // cached pointer to managed add-in
};
