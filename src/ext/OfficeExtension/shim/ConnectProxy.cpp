// <copyright file="ConnectProxy.cpp" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
//  Connection proxy implementation.
// </summary>
//
#include "precomp.h"

// external globals
extern HMODULE vhModule;


HRESULT CConnectProxy::Create(
    __out CConnectProxy** ppNewProxy
    )
{
    HRESULT hr = S_OK;
    CConnectProxy* pProxy = NULL;

    pProxy = new CConnectProxy();
    ExitOnNull(pProxy, hr, E_OUTOFMEMORY, "Failed to allocate proxy.");

    hr = pProxy->Initialize();
    ExitOnFailure(hr, "Failed to initialize proxy.");

    *ppNewProxy = pProxy;
    pProxy = NULL;

LExit:
    ReleaseObject(pProxy);
    return hr;
}


CConnectProxy::CConnectProxy() : 
    m_pwzAppId(NULL),
    m_pTypeInfo(NULL),
    m_fInstanceCreated(FALSE),
    m_pConnect(NULL)
{
}


CConnectProxy::~CConnectProxy()
{
    ReleaseObject(m_pConnect);
    ReleaseObject(m_pTypeInfo);
    ReleaseStr(m_pwzAppId);

    if (m_fInstanceCreated)
    {
        ClrLoaderDestroyInstance();
    }
}


HRESULT CConnectProxy::Initialize()
{
    HRESULT hr = S_OK;
    ITypeLib* pTypeLib = NULL;
    ITypeInfo* pTypeInfo = NULL;
    LPWSTR pwzAppId = NULL;
    LPWSTR pwzClrVersion = NULL;
    LPWSTR pwzAssemblyName = NULL;
    LPWSTR pwzClassName = NULL;

    // Kick off the auto-update.
    hr = ResReadString(vhModule, IDS_APPLICATIONID, &pwzAppId);
    ExitOnFailure(hr, "Failed to load application id from resources.");

    UpdateThreadCheck(pwzAppId, FALSE); // launch off the auto-update check, ignore any failures

    // Get the TypeLib information loaded.
    hr = ::LoadRegTypeLib(LIBID_AddinNamespace, 1, 0, 0, &pTypeLib);
    ExitOnFailure(hr, "Failed to load LIBID_AddinNamespace");

    hr = pTypeLib->GetTypeInfoOfGuid(IID__IDTExtensibility2, &pTypeInfo);
    ExitOnFailure(hr, "Failed to get IID__IDTExtensibility2 from TypeLib.");

/*
    // Get the CLR instance created.
    hr = ResReadString(vhModule, IDS_CLRVERSION, &pwzClrVersion);
    if (FAILED(hr) || (pwzClrVersion && L'v' != *pwzClrVersion))
    {
        // Don't specify a CLR version if we failed to read the string table or
        // the string table contained a string that didn't start with "v" (for "version").
        ReleaseNullStr(pwzClrVersion);
        hr = S_OK;
    }

    hr = ResReadString(vhModule, IDS_ASSEMBLYNAME, &pwzAssemblyName);
    ExitOnFailure(hr, "Failed to load assembly name from resources.");

    hr = ResReadString(vhModule, IDS_CLASSNAME, &pwzClassName);
    ExitOnFailure(hr, "Failed to load class name from resources.");

    hr = ClrLoaderCreateInstance(pwzClrVersion, pwzAssemblyName, pwzClassName, __uuidof(IDTExtensibility2), (void **)&m_pConnect);
    ExitOnFailure3(hr, "Failed to create instance of managed assembly: %S, class: %S, clr version: %S", pwzAssemblyName, pwzClassName, pwzClrVersion);
    m_fInstanceCreated = TRUE;
*/
    pTypeInfo->AddRef();
    m_pTypeInfo = pTypeInfo;
    pTypeInfo = NULL;

    m_pwzAppId = pwzAppId;
    pwzAppId = NULL;

LExit:
    ReleaseStr(pwzClassName);
    ReleaseStr(pwzAssemblyName);
    ReleaseStr(pwzClrVersion);
    ReleaseStr(pwzAppId);
    ReleaseObject(pTypeInfo);
    ReleaseObject(pTypeLib);
    return hr;
}


HRESULT __stdcall CConnectProxy::GetTypeInfoCount(
    __out unsigned int FAR* pctinfo
    )
{
    if (!pctinfo)
    {
        return E_INVALIDARG;
    }

   *pctinfo = 1;
    return S_OK;
}


HRESULT __stdcall CConnectProxy::GetTypeInfo(
    __in unsigned int iTInfo,
    __in LCID lcid,
    __out ITypeInfo FAR* FAR*  ppTInfo
    )
{
    if (!ppTInfo)
    {
        return E_INVALIDARG;
    }

    *ppTInfo = NULL;

    if(iTInfo != 0)
    {
        return DISP_E_BADINDEX;
    }

    // AddRef and return pointer to cached typeinfo for this object.
    m_pTypeInfo->AddRef();
    *ppTInfo = m_pTypeInfo;

    return S_OK;
}


HRESULT __stdcall CConnectProxy::GetIDsOfNames(
    __in REFIID  riid, 
    __in OLECHAR FAR* FAR*  rgszNames,
    __in unsigned int  cNames,
    __in LCID lcid,
    __in DISPID FAR*  rgDispId
    )
{
   return ::DispGetIDsOfNames(m_pTypeInfo, rgszNames, cNames, rgDispId);
}


HRESULT __stdcall CConnectProxy::Invoke(
    __in DISPID dispIdMember,
    __in REFIID riid,
    __in LCID lcid,
    __in WORD wFlags,
    __in DISPPARAMS FAR* pDispParams,
    __out VARIANT FAR* pvarResult,
    __out EXCEPINFO FAR* pExcepInfo,
    __out unsigned int FAR* puArgErr
    )
{
   return ::DispInvoke(this, m_pTypeInfo, dispIdMember, wFlags, pDispParams, pvarResult, pExcepInfo, puArgErr); 
}


HRESULT __stdcall CConnectProxy::OnConnection(
    __in IDispatch* Application,
    __in ext_ConnectMode ConnectMode,
    __in IDispatch* AddInInst,
    __in SAFEARRAY** custom
    )
{
    HRESULT hr = S_OK;
    LPWSTR pwzClrVersion = NULL;
    LPWSTR pwzAssemblyName = NULL;
    LPWSTR pwzClassName = NULL;

    // Get the CLR instance created.
    hr = ResReadString(vhModule, IDS_CLRVERSION, &pwzClrVersion);
    if (FAILED(hr) || (pwzClrVersion && L'v' != *pwzClrVersion))
    {
        // Don't specify a CLR version if we failed to read the string table or
        // the string table contained a string that didn't start with "v" (for "version").
        ReleaseNullStr(pwzClrVersion);
        hr = S_OK;
    }

    hr = ResReadString(vhModule, IDS_ASSEMBLYNAME, &pwzAssemblyName);
    ExitOnFailure(hr, "Failed to load assembly name from resources.");

    hr = ResReadString(vhModule, IDS_CLASSNAME, &pwzClassName);
    ExitOnFailure(hr, "Failed to load class name from resources.");

    hr = ClrLoaderCreateInstance(pwzClrVersion, pwzAssemblyName, pwzClassName, __uuidof(IDTExtensibility2), (void **)&m_pConnect);
    ExitOnFailure3(hr, "Failed to create instance of managed assembly: %S, class: %S, clr version: %S", pwzAssemblyName, pwzClassName, pwzClrVersion);
    m_fInstanceCreated = TRUE;

    hr = m_pConnect->OnConnection(Application, ConnectMode, AddInInst, custom);
    ExitOnFailure(hr, "Failed to call OnConnection in assembly.");

LExit:
    ReleaseStr(pwzClassName);
    ReleaseStr(pwzAssemblyName);
    ReleaseStr(pwzClrVersion);

    return hr;
}


HRESULT __stdcall CConnectProxy::OnDisconnection(
    __in ext_DisconnectMode RemoveMode,
    __in SAFEARRAY** custom
    )
{
    HRESULT hr = S_OK;

    if (m_pwzAppId)
    {
        UpdateThreadCheck(m_pwzAppId, TRUE); // launch off the auto-update check, ignore any failures
    }

    hr = m_pConnect->OnDisconnection(RemoveMode, custom);

    ReleaseNullObject(m_pConnect);
    ClrLoaderDestroyInstance();
    m_fInstanceCreated = FALSE;

//LExit:
    return hr;
}


HRESULT __stdcall CConnectProxy::OnAddInsUpdate(
    __in SAFEARRAY** custom
    )
{
    return m_pConnect->OnAddInsUpdate(custom);
}


HRESULT __stdcall CConnectProxy::OnStartupComplete(
    __in SAFEARRAY** custom
    )
{
    return m_pConnect->OnStartupComplete(custom);
}


HRESULT __stdcall CConnectProxy::OnBeginShutdown(SAFEARRAY** custom)
{
    return m_pConnect->OnBeginShutdown(custom);
}
