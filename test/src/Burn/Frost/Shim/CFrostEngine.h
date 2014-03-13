//-------------------------------------------------------------------------------------------------
// <copyright file="CFrostEngine.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    CFrostEngine defines the engine objects
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

#include "FrostEvents.h"
#include "IFrostUserExperience.h"

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

    value struct BurnCoreWrapper
    {
        IBurnCore* pBurnCore;
    };

    public ref class CFrostEngine : IFrostUserExperience
    {
    public:
        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting Detect from the engine")]
        static event DetectEventHandler^ DetectEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting Plan from the engine")]
        static event PlanEventHandler^ PlanEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting Plan from the engine")]
        static event ApplyEventHandler^ ApplyEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting a suspension of the process")]
        static event SuspendEventHandler^ SuspendEvent;

        //[Cagegory("Output")]
        //[Description("Event is fired when the UX is requesting a reboot")]
        static event RebootEventHandler^ RebootEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting the package count from the engine")]
        static event GetPackageCountEventHandler^ GetPackageCountEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting the command line from the engine")]
        static event GetStringEventHandler^ GetCommandLineEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting the numeric value of a variable")]
        static event GetVariableNumericEventHandler^ GetVariableNumericEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting the string value of a variable")]
        static event GetVariableStringEventHandler^ GetVariableStringEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting the version value of a variable")]
        static event GetVariableVersionEventHandler^ GetVariableVersionEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is setting the numeric value of a variable")]
        static event SetVariableNumericEventHandler^ SetVariableNumericEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is setting the string value of a variable")]
        static event SetVariableStringEventHandler^ SetVariableStringEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is setting the version value of a variable")]
        static event SetVariableVersionEventHandler^ SetVariableVersionEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is formatting a string with information from the engine")]
        static event FormatStringEventHandler^ FormatStringEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is ...?")]
        static event EscapeStringEventHandler^ EscapeStringEvent;
        
        //[Category("Output")]
        //[Description("Event is fired when the UX is requesting Elevation from the engine")]
        static event ElevateEventHandler^ ElevateEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is evaluating a conditional string")]
        static event EvaluateConditionEventHandler^ EvaluateConditionEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is writing to the logger")]
        static event LogEventHandler^ LogEvent;

        //[Category("Output")]
        //[Description("Event is fired when the UX is setting the Source path")]
        static event SetSourceEventHandler^ SetSourceEvent;

    private:
        static HMODULE UxModule = nullptr;
        static IBurnUserExperience* pUX = nullptr;
        static BurnCoreWrapper^ FrostCore = nullptr;
        static CFrostEngine^ UXProxy = nullptr;
        static GCHandle burnCoreGCHandle;

        CFrostEngine();
        HRESULT GetHRESULT(HRESULTS hrStatus);
        REQUEST_STATE GetReqState(PKG_REQUEST_STATE reqState);
        PACKAGE_STATE GetPkgState(CUR_PACKAGE_STATE state);
        ACTION_STATE GetPkgActionState(PKG_ACTION_STATE state);

    public:
        ~CFrostEngine();

        static HRESULTS CreateUX(Int32 appHwnd, SETUP_COMMAND cmd, IFrostUserExperience^% uxRef);

        // IBurnUserExperience
        virtual HRESULTS Initialize(Int32 nCmdShow, SETUP_RESUME ResumeState);
        virtual HRESULTS Run();
        virtual void Uninitialize();

        virtual CommandID OnDectectBegin(UInt32 cPackages);
        virtual CommandID OnDetectPackageBegin(String^ wzPackageId);
        virtual void OnDetectPackageComplete(String^ wzPackageId, HRESULTS hrStatus, CUR_PACKAGE_STATE state);
        virtual void OnDetectComplete(HRESULTS hrStatus);

        virtual CommandID OnPlanBegin(UInt32 cPackages);
        virtual CommandID OnPlanPackageBegin(String^ wzPackageId, PKG_REQUEST_STATE% reqState);
        virtual void OnPlanPackageComplete(String^ wzPackageId,HRESULTS hrStatus,CUR_PACKAGE_STATE state,PKG_REQUEST_STATE requested,PKG_ACTION_STATE execute,PKG_ACTION_STATE rollback);
        virtual void OnPlanComplete(HRESULTS hrStatus);

        virtual CommandID OnApplyBegin();
        virtual CommandID OnRegisterBegin();
        virtual void OnRegisterComplete(HRESULTS hrStatus);
        virtual void OnUnregisterBegin();
        virtual void OnUnregisterComplete(HRESULTS hrStatus);
        virtual void OnCacheComplete(HRESULTS hrStatus);
        virtual CommandID OnExecuteBegin(UInt32 cExecutingPackages);
        virtual CommandID OnExecutePackageBegin(String^ wzPackageId,bool fExecute);
        virtual CommandID OnError(String^ wzPackageId, UInt32 dwCode,String^ wzError,UInt32 dwUIHint);
        virtual CommandID OnProgress(UInt32 dwProgressPercentage,UInt32 dwOverallPercentage);
        virtual CommandID OnExecuteMsiMessage(String^ wzPackageID, INSTALLMESSAGE mt, UInt32 uiFlags, String^wzMessage);
        virtual void OnExecutePackageComplete(String^ wzPackageId,HRESULTS hrExitCode);
        virtual void OnExecuteComplete(HRESULTS hrStatus);
        virtual bool OnRestartRequired();
        virtual void OnApplyComplete(HRESULTS hrStatus);
        virtual int ResolveSource(String^ wzPackageID, String^ wzPackageOrContainerPath);
        virtual bool CanPackageBeDownloaded();

        // IBurnCore
        // class methods
        static HRESULTS GetPackageCount(UInt32% numPackages);
        static HRESULTS GetCommandLineParameters(String^% cmdLine,UInt32% pcchCommandLine);
        static HRESULTS GetPropertyNumeric(String^ propertyName, Int64% propertyValue);
        static HRESULTS GetPropertyString(String^ propertyName, String^% propertyValue, UInt64% StringSize);
        static HRESULTS GetPropertyVersion(String^ propertyName, UInt64% propertyValue);
        static HRESULTS SetPropertyNumeric(String^ propertyName, Int64 propertyValue);
        static HRESULTS SetPropertyString(String^ propertyName, String^ propertyValue);
        static HRESULTS SetPropertyVersion(String^ propertyName, UInt64 propertyValue);
        static HRESULTS FormatPropertyString(String^ strIn, String^% strInOut, UInt64% StringSize);
        static HRESULTS EscapeString(String^ wzIn, String^% wzOut);
        static HRESULTS EvaluateCondition(String^ conditionName, bool% conditionValue);
        static HRESULTS Elevate(/*__in_opt*/ IntPtr^ hwndParent);
        static HRESULTS Detect();
        static HRESULTS Plan(SETUP_ACTION action);
        static HRESULTS Apply( /*__in_opt*/ IntPtr^ hwndParent );
        static HRESULTS Suspend( IntPtr^ hwndParent );
        static HRESULTS Reboot( IntPtr^ hwndParent );
        static HRESULTS SetSource(String^ wzSourcePath);
        static HRESULTS Log(ENGINE_LOG_LEVEL Level, String^ Message);


        // object methods
        void TestObject() {/*System::Windows::Forms::MessageBox::Show("TestObject called");*/} 
    };

}
}
}
}
}