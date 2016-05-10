// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#include "CFrostCore.h"
#include "CFrostEngine.h"
#include "IFrostUserExperience.h"
#include "shim.h"

using namespace System;
using namespace System::Windows::Forms;
using namespace System::Runtime::InteropServices;


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
    CFrostEngine::CFrostEngine()
    {
    }

    CFrostEngine::~CFrostEngine()
    {
        Uninitialize();
    }

    HRESULTS CFrostEngine::CreateUX(Int32 appHwnd, SETUP_COMMAND setupCommand, IFrostUserExperience^% uxRef)
    {
        // TODO: ALSO NEED TO CREATE THE MOCK BURNCORE OBJECT 

        HRESULTS hr = HRESULTS::HR_S_OK;

        if (UxModule == nullptr)
        {
            UxModule = ::LoadLibrary(L"UXEntryPoint.dll");
            if( UxModule == nullptr )
            {
                UInt32 err = (UInt32)::GetLastError();
                String^ msg = String::Empty;
                msg->Format("LoadLibrary failed: %x", err);
                System::Windows::Forms::MessageBox::Show(msg);
                hr = HRESULTS::HR_FAILURE;
            }
            else
            {
                PFN_CREATE_USER_EXPERIENCE func = 
                    reinterpret_cast<PFN_CREATE_USER_EXPERIENCE>(::GetProcAddress(UxModule, "SetupUXCreate"));

                //FARPROC func = ::GetProcAddress(UxModule, "CreateUserExperience");
                if( func == nullptr )
                {
                    System::Windows::Forms::MessageBox::Show("GetProcAddress failed");
                    hr = HRESULTS::HR_FAILURE;
                }
                else
                {
                    // TODO: use setupCommand
                    BURN_COMMAND cmd;
                    cmd.action = BURN_ACTION_INSTALL;
                    //cmd.display = BURN_DISPLAY_NONE;
                    //cmd.display = BURN_DISPLAY_PASSIVE;
                    cmd.display = BURN_DISPLAY_FULL;
                    cmd.restart = BURN_RESTART_NEVER;

                    pin_ptr<IBurnUserExperience *> p = &pUX;
                    HRESULT result = func(&cmd, p);
                    if( S_OK != result )
                    {
                        if( ERROR_NOT_SUPPORTED == result )
                        {
                            System::Windows::Forms::MessageBox::Show(".NET was not found");
                        }
                        else
                        {
                            System::Windows::Forms::MessageBox::Show("CreateUserExperience failed");
                        }
                        hr = HRESULTS::HR_FAILURE;
                    }
                    else
                    {
                        FrostCore = gcnew BurnCoreWrapper;
                        if (FrostCore == nullptr)
                        {
                            hr = HRESULTS::HR_FAILURE;
                        }
                        else
                        {
                            FrostCore->pBurnCore = new CFrostCore;
                            burnCoreGCHandle = GCHandle::Alloc(FrostCore, GCHandleType::Pinned);
                            hr = HRESULTS::HR_S_OK;
                        }
                    }
                }
            }
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        if (hr == HRESULTS::HR_S_OK)
        {
            uxRef = UXProxy = gcnew CFrostEngine();
        }

        return hr;
    }

    HRESULTS CFrostEngine::Initialize(Int32 nCmdShow, SETUP_RESUME ResumeState)
    {
        BURN_RESUME_TYPE RealBurnInitResumeValue = BURN_RESUME_TYPE_NONE;
        switch(ResumeState)
        {
            case SETUP_RESUME::SETUP_RESUME_NONE:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_NONE;
                break;
            case SETUP_RESUME::SETUP_RESUME_INVALID:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_INVALID;
                break;
            case SETUP_RESUME::SETUP_RESUME_UNEXPECTED:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_UNEXPECTED;
                break;
            case SETUP_RESUME::SETUP_RESUME_REBOOT_PENDING:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_REBOOT_PENDING;
                break;
            case SETUP_RESUME::SETUP_RESUME_REBOOT:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_REBOOT;
                break;
            case SETUP_RESUME::SETUP_RESUME_SUSPEND:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_SUSPEND;
                break;
            case SETUP_RESUME::SETUP_RESUME_ARP:
                RealBurnInitResumeValue = BURN_RESUME_TYPE_ARP;
                break;
        }

        HRESULT hr = pUX->Initialize(FrostCore->pBurnCore, 0,RealBurnInitResumeValue);

        if (hr == S_OK)
        {
            return HRESULTS::HR_S_OK;
        }
        else
        {
            return HRESULTS::HR_FAILURE;
        }
    }

    HRESULTS CFrostEngine::Run()
    {
        HRESULT hr = pUX->Run();

        if (hr == S_OK)
        {
            return HRESULTS::HR_S_OK;
        }
        else
        {
            return HRESULTS::HR_FAILURE;
        }
    }

    void CFrostEngine::Uninitialize()
    {
        if (pUX != nullptr)
        {
            pUX->Uninitialize();
            delete pUX;
            pUX = nullptr;
        }

        if (burnCoreGCHandle.IsAllocated)
        {
            burnCoreGCHandle.Free();
        }

        if (FrostCore != nullptr)
        {
            if (FrostCore->pBurnCore != nullptr)
            {
                delete FrostCore->pBurnCore;
                FrostCore->pBurnCore = nullptr;
            }
        }

        if (UxModule != nullptr)
        {
            ::FreeLibrary(UxModule); // unload
            UxModule = nullptr;
        }

        return;
    }

    CommandID CFrostEngine::OnDectectBegin(UInt32 cPackages)
    {
        int retValue = pUX->OnDetectBegin(cPackages);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnDetectPackageBegin(String^ wzPackageId)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        int retValue = pUX->OnDetectPackageBegin(cwch);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    void CFrostEngine::OnDetectPackageComplete(String^ wzPackageId, HRESULTS hrStatus, CUR_PACKAGE_STATE state)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        PACKAGE_STATE pkgState = GetPkgState(state);

        pUX->OnDetectPackageComplete(cwch, GetHRESULT(hrStatus), pkgState);
    }

    void CFrostEngine::OnDetectComplete(HRESULTS hrStatus)
    {
        pUX->OnDetectComplete(GetHRESULT(hrStatus));
    }


    HRESULT CFrostEngine::GetHRESULT(HRESULTS hrStatus)
    {
        int temp = (int)hrStatus;

        HRESULT hr;
        switch(temp)
        {
        case 0:
            hr = S_OK;
            break;
        case 1:
            hr = S_FALSE;
            break;
        default:
            hr = E_FAIL;
            break;
        }

        return hr;
    }

    REQUEST_STATE CFrostEngine::GetReqState(PKG_REQUEST_STATE reqState)
    {
        REQUEST_STATE pkgState;

        switch(safe_cast<int>(reqState))
        {
        case REQUEST_STATE_ABSENT:
            pkgState = REQUEST_STATE_ABSENT;
            break;
        case REQUEST_STATE_CACHE:
            pkgState = REQUEST_STATE_CACHE;
            break;
        case REQUEST_STATE_PRESENT:
            pkgState = REQUEST_STATE_PRESENT;
            break;
        case REQUEST_STATE_REPAIR:
            pkgState = REQUEST_STATE_REPAIR;
            break;
        default:
            pkgState = REQUEST_STATE_NONE;
            break;
        }

        return pkgState;
    }

    PACKAGE_STATE CFrostEngine::GetPkgState(CUR_PACKAGE_STATE state)
    {
        PACKAGE_STATE pkgState;

        switch(safe_cast<int>(state))
        {
        case PACKAGE_STATE_ABSENT:
            pkgState = PACKAGE_STATE_ABSENT;
            break;
        case PACKAGE_STATE_CACHED:
            pkgState = PACKAGE_STATE_CACHED;
            break;
        case PACKAGE_STATE_PRESENT:
            pkgState = PACKAGE_STATE_PRESENT;
            break;
        default:
            pkgState = PACKAGE_STATE_UNKNOWN;
            break;
        }

        return pkgState;
    }

    ACTION_STATE CFrostEngine::GetPkgActionState(PKG_ACTION_STATE state)
    {
        ACTION_STATE actionState;

        switch(safe_cast<int>(state))
        {
        case ACTION_STATE_UNINSTALL:
            actionState = ACTION_STATE_UNINSTALL;
            break;
        case ACTION_STATE_INSTALL:
            actionState = ACTION_STATE_INSTALL;
            break;
        case ACTION_STATE_ADMIN_INSTALL:
            actionState = ACTION_STATE_ADMIN_INSTALL;
            break;
        case ACTION_STATE_MAINTENANCE:
            actionState = ACTION_STATE_MAINTENANCE;
            break;
        case ACTION_STATE_RECACHE:
            actionState = ACTION_STATE_RECACHE;
            break;
        case ACTION_STATE_MINOR_UPGRADE:
            actionState = ACTION_STATE_MINOR_UPGRADE;
            break;
        case ACTION_STATE_MAJOR_UPGRADE:
            actionState = ACTION_STATE_MAJOR_UPGRADE;
            break;
        case ACTION_STATE_PATCH:
            actionState = ACTION_STATE_PATCH;
            break;
        default:
            actionState = ACTION_STATE_NONE;
            break;
        }

        return actionState;
    }

    CommandID CFrostEngine::OnPlanBegin(UInt32 cPackages)
    {    
        int retValue = pUX->OnPlanBegin(cPackages);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnPlanPackageBegin(String^ wzPackageId, PKG_REQUEST_STATE% reqState)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        REQUEST_STATE pkgState = GetReqState(reqState);

        int retValue = pUX->OnPlanPackageBegin(cwch, &pkgState);

        reqState = safe_cast<PKG_REQUEST_STATE>(safe_cast<int>(pkgState));

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    void CFrostEngine::OnPlanPackageComplete(String^ wzPackageId,HRESULTS hrStatus,CUR_PACKAGE_STATE state,PKG_REQUEST_STATE requested,PKG_ACTION_STATE execute,PKG_ACTION_STATE rollback)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        pUX->OnPlanPackageComplete(cwch, GetHRESULT(hrStatus), GetPkgState(state), GetReqState(requested), GetPkgActionState(execute), GetPkgActionState(rollback));
    }

    void CFrostEngine::OnPlanComplete(HRESULTS hrStatus)
    {
        pUX->OnPlanComplete(GetHRESULT(hrStatus));
    }


    CommandID CFrostEngine::OnApplyBegin()
    {
        int retValue = pUX->OnApplyBegin();

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnRegisterBegin()
    {
        int retValue = pUX->OnRegisterBegin();

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    void CFrostEngine::OnRegisterComplete(HRESULTS hrStatus)
    {
        pUX->OnRegisterComplete(GetHRESULT(hrStatus));
    }

    void CFrostEngine::OnUnregisterBegin()
    {
        pUX->OnUnregisterBegin();
    }

    void CFrostEngine::OnUnregisterComplete(HRESULTS hrStatus)
    {
        pUX->OnUnregisterComplete(GetHRESULT(hrStatus));
    }

    void CFrostEngine::OnCacheComplete(HRESULTS hrStatus)
    {
        pUX->OnCacheComplete(GetHRESULT(hrStatus));
    }

    CommandID CFrostEngine::OnExecuteBegin(UInt32 cExecutingPackages)
    {
        int retValue = pUX->OnExecuteBegin(cExecutingPackages);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnExecutePackageBegin(String^ wzPackageId, bool fExecute)
    {
        // TODO: This used to be disabled for some reason...
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        int retValue = pUX->OnExecutePackageBegin(cwch, fExecute);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnError(String^ wzPackageId, UInt32 dwCode, String^ wzError, UInt32 dwUIHint)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzError);

        pin_ptr<const wchar_t> cwch2 = PtrToStringChars(wzPackageId);

        int retValue = pUX->OnError(cwch2, dwCode, cwch, dwUIHint);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnProgress(UInt32 dwProgressPercentage, UInt32 dwOverallPercentage)
    {
        int retValue = pUX->OnProgress(dwProgressPercentage, dwOverallPercentage);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    CommandID CFrostEngine::OnExecuteMsiMessage(String^ wzPackageID, INSTALLMESSAGE mt, UInt32 uiFlags, String^ wzMessage)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageID);

        pin_ptr<const wchar_t> cwch2 = PtrToStringChars(wzMessage);

        int retValue = pUX->OnExecuteMsiMessage(cwch, mt, uiFlags, cwch2);

        CommandID cmdId = (CommandID)retValue;

        return cmdId;
    }

    void CFrostEngine::OnExecutePackageComplete(String^ wzPackageId, HRESULTS hrExitCode)
    {
        // TODO: This was disabled for some reason
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageId);

        pUX->OnExecutePackageComplete(cwch, GetHRESULT(hrExitCode));
    }

    void CFrostEngine::OnExecuteComplete(HRESULTS hrStatus)
    {
        pUX->OnExecuteComplete(GetHRESULT(hrStatus));
    }

    bool CFrostEngine::OnRestartRequired()
    {
        return (bool)pUX->OnRestartRequired();
    }

    void CFrostEngine::OnApplyComplete(HRESULTS hrStatus)
    {
        pUX->OnApplyComplete(GetHRESULT(hrStatus));
    }

    int CFrostEngine::ResolveSource(String^ wzPackageID, String^ wzPackageOrContainerPath)
    {
        pin_ptr<const wchar_t> cwch = PtrToStringChars(wzPackageID);
        pin_ptr<const wchar_t> cwch2 = PtrToStringChars(wzPackageOrContainerPath);

        int RetVal = pUX->ResolveSource(cwch,cwch2);

        return RetVal;
    }

    bool CFrostEngine::CanPackageBeDownloaded()
    {
        bool RetVal = pUX->CanPackagesBeDownloaded();
        return RetVal;
    }

    HRESULTS CFrostEngine::GetPackageCount(UInt32% numPackages)
    {
        HRESULTS hr;

        if (UXProxy != nullptr)
        {
            PackageCountEventArgs^ e = gcnew PackageCountEventArgs();
            UXProxy->GetPackageCountEvent(UXProxy, e);
            numPackages = e->PackageCount;
            hr = e->ResultToReturn;
        }
        else
        {
            numPackages = 0;
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::GetCommandLineParameters(String^% cmdLine, UInt32% pcchCommandLine)
    {
        HRESULTS hr;

        if (UXProxy != nullptr)
        {
            StringEventArgs^ e = gcnew StringEventArgs();
            UXProxy->GetCommandLineEvent(UXProxy, e);
            if (e->StringValue != nullptr)
            {
                cmdLine = e->StringValue;
            }
            else
            {
                cmdLine = String::Empty;
            }

            hr = e->ResultToReturn;
        }
        else
        {
            cmdLine = String::Empty;
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::GetPropertyNumeric(String^ propertyName, Int64% propertyValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            LongIntEventArgs^ e = gcnew LongIntEventArgs();
            e->StringValue = propertyName;

            UXProxy->GetVariableNumericEvent(UXProxy,e);
            propertyValue = e->Number;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::GetPropertyString(String^ propertyName, String^% propertyValue, UInt64% StringSize)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            StringVariableEventArgs^ e = gcnew StringVariableEventArgs();
            e->StringName = propertyName;

            UXProxy->GetVariableStringEvent(UXProxy,e);

            propertyValue = e->StringValue;
            StringSize = (UInt64)e->DWordValue;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::GetPropertyVersion(String^ propertyName, UInt64% propertyValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            LongIntEventArgs^ e = gcnew LongIntEventArgs();
            e->StringValue = propertyName;

            UXProxy->GetVariableVersionEvent(UXProxy,e);

            propertyValue = e->Number;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::SetPropertyNumeric(String^ propertyName, Int64 propertyValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            LongIntEventArgs^ e = gcnew LongIntEventArgs();
            e->StringValue = propertyName;
            e->Number = propertyValue;

            UXProxy->SetVariableNumericEvent(UXProxy,e);

            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::SetPropertyString(String^ propertyName, String^ propertyValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            StringVariableEventArgs^ e = gcnew StringVariableEventArgs();
            e->StringName = propertyName;
            e->StringValue = propertyValue;

            UXProxy->SetVariableStringEvent(UXProxy,e);

            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::SetPropertyVersion(String^ propertyName, UInt64 propertyValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            LongIntEventArgs^ e = gcnew LongIntEventArgs();
            e->StringValue = propertyName;
            e->Number = propertyValue;

            UXProxy->SetVariableVersionEvent(UXProxy,e);

            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::FormatPropertyString(String^ strIn, String^% strInOut, UInt64% StringSize)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            FormatStringEventArgs^ e = gcnew FormatStringEventArgs();
            e->InValue = strIn;
            e->InOutValue = strInOut;

            UXProxy->FormatStringEvent(UXProxy,e);

            strInOut = e->InOutValue;
            StringSize = strInOut->Length;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::EscapeString(String^ wzIn, String^% wzOut)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            FormatStringEventArgs^ e = gcnew FormatStringEventArgs();
            e->InValue = wzIn;
            
            UXProxy->EscapeStringEvent(UXProxy,e);

            wzOut = e->InOutValue;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::EvaluateCondition(String^ conditionName,  bool% conditionValue)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            ConditionalEventArgs^ e = gcnew ConditionalEventArgs();
            e->StringValue = conditionName;

            UXProxy->EvaluateConditionEvent(UXProxy,e);

            conditionValue = e->EvalResult;
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_S_FALSE;
        }

        return HRESULTS::HR_S_OK;
    }

    HRESULTS CFrostEngine::Elevate(/*__in_opt*/ IntPtr^ hwndParent)
    {
        // TODO: hwndParent IS IGNORED FOR NOW, NOT QUITE SURE HOW IT IS USED ANYHOW

        HRESULTS hr;

        if (UXProxy != nullptr)
        {
            ResultReturnArgs^ e = gcnew ResultReturnArgs();
            UXProxy->ElevateEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;    
    }

    HRESULTS CFrostEngine::Detect()
    {
        HRESULTS hr;
        if (UXProxy != nullptr)
        {
            ResultReturnArgs^ e = gcnew ResultReturnArgs();
            UXProxy->DetectEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;    
    }

    HRESULTS CFrostEngine::Plan(SETUP_ACTION action)
    {
        HRESULTS hr;
        if (UXProxy != nullptr)
        {
            SetupActionArgs^ e = gcnew SetupActionArgs(action);
            UXProxy->PlanEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;    
    }

    HRESULTS CFrostEngine::Apply(/*__in_opt*/ IntPtr^ hwndParent)
    {
        // TODO: hwndParent IS IGNORED FOR NOW, NOT QUITE SURE HOW IT IS USED ANYHOW

        HRESULTS hr;
        if (UXProxy != nullptr)
        {
            ResultReturnArgs^ e = gcnew ResultReturnArgs();
            UXProxy->ApplyEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;    
    }

    HRESULTS CFrostEngine::Suspend( IntPtr^ hwndParent)
    {
        //TODO: I'm copying the Apply method, so if it doesnt use the handle, take it out

        HRESULTS hr;
        if(UXProxy != nullptr)
        {
            ResultReturnArgs^ e = gcnew ResultReturnArgs();
            UXProxy->SuspendEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::Reboot( IntPtr^ hwndParent)
    {
        //TODO: I'm copying the Apply method, so if it doesnt use the handle, take it out

        HRESULTS hr;
        if(UXProxy != nullptr)
        {
            ResultReturnArgs^ e = gcnew ResultReturnArgs();
            UXProxy->RebootEvent(UXProxy, e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }
    
    HRESULTS CFrostEngine::SetSource(String^ wzSourcePath)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            StringEventArgs^ e = gcnew StringEventArgs();
            e->StringValue = wzSourcePath;
            UXProxy->SetSourceEvent(UXProxy,e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }

    HRESULTS CFrostEngine::Log(ENGINE_LOG_LEVEL Level, String^ Message)
    {
        HRESULTS hr;

        if(UXProxy != nullptr)
        {
            LogEventArgs^ e = gcnew LogEventArgs();
            e->StringValue = Message;
            e->MessageLogLevel = Level;
            UXProxy->LogEvent(UXProxy,e);
            hr = e->ResultToReturn;
        }
        else
        {
            hr = HRESULTS::HR_FAILURE;
        }

        return hr;
    }
}
}
}
}
}
