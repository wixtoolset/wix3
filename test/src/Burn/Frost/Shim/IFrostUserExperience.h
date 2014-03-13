//-------------------------------------------------------------------------------------------------
// <copyright file="IFrostUserExperience.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    IFrostUserExperience is the interface mapping to IBurnUserExperience
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

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
    public interface class IFrostUserExperience
    {
        HRESULTS Initialize(Int32 nCmdShow, SETUP_RESUME ResumeState);
        HRESULTS Run();
        void Uninitialize();

        CommandID OnDectectBegin(UInt32 cPackages);
        CommandID OnDetectPackageBegin(String^ wzPackageId);
        void OnDetectPackageComplete(String^ wzPackageId, HRESULTS hrStatus, CUR_PACKAGE_STATE state);
        void OnDetectComplete(HRESULTS hrStatus);

        CommandID OnPlanBegin(UInt32 cPackages);
        CommandID OnPlanPackageBegin(String^ wzPackageId, PKG_REQUEST_STATE% reqState);
        void OnPlanPackageComplete(
            String^ wzPackageId, 
            HRESULTS hrStatus,
            CUR_PACKAGE_STATE state,
            PKG_REQUEST_STATE requested,
            PKG_ACTION_STATE execute,
            PKG_ACTION_STATE rollback);
        void OnPlanComplete(HRESULTS hrStatus);

        CommandID OnApplyBegin();
        CommandID OnRegisterBegin();
        void OnRegisterComplete(HRESULTS hrStatus);
        void OnUnregisterBegin();
        void OnUnregisterComplete(HRESULTS hrStatus);
        void OnCacheComplete(HRESULTS hrStatus);
        CommandID OnExecuteBegin(UInt32 cExecutingPackages);
        CommandID OnExecutePackageBegin(String^ wzPackageId,bool fExecute);
        CommandID OnError(String^ wzPackageId, UInt32 dwCode,String^ wzError,UInt32 dwUIHint);
        CommandID OnProgress(UInt32 dwProgressPercentage,UInt32 dwOverallPercentage);
        CommandID OnExecuteMsiMessage(String^ wzPackageID, INSTALLMESSAGE mt, UInt32 uiFlags, String^wzMessage);
        void OnExecutePackageComplete(String^ wzPackageId,HRESULTS hrExitCode);
        void OnExecuteComplete(HRESULTS hrStatus);
        bool OnRestartRequired();
        void OnApplyComplete(HRESULTS hrStatus);
        int ResolveSource(String^ wzPackageID, String^ wzPackageOrContainerPath);
        bool CanPackageBeDownloaded();
    };
}
}
}
}
}
