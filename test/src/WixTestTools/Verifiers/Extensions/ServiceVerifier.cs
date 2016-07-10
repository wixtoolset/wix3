// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Verifiers.Extensions
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Xunit;

    /// <summary>
    /// Contains methods for Service verification
    /// </summary>
    public static class ServiceVerifier
    {
        /// <summary>
        /// verify a service has the expected failure action details
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <param name="resetPeriodInDays">the reset period in days</param>
        /// <param name="failureActions">an ordered list of the expected failure action types</param>
        public static void VerifyServiceInformation(string serviceName, int resetPeriodInDays, ServiceFailureActionType[] failureActions)
        {
            ServiceInformation service = GetSericeInformation(serviceName);
            string message = string.Empty;
            bool failed = false;

            if (resetPeriodInDays != service.ResetPeriodInDays)
            {
                failed = true;
                message += string.Format("Reset Period is incoorect. Actual: {0}, Expected: {1}.\r\n", service.ResetPeriodInDays, resetPeriodInDays);
            }

            for (int i = 0; i < failureActions.Length; i++)
            {
                if (failureActions[i] != service.Actions[i].Type)
                {
                    failed = true;
                    message += string.Format("FailureAction {0} is incorect. Actual: {1}, Expected: {2}. \r\n", i, service.Actions[i].Type.ToString(), failureActions[i].ToString());
                }
            }

            Assert.False(failed, message);
        }

        /// <summary>
        /// Checks if a service exists or not
        /// </summary>
        /// <param name="serviceName">The service to check for</param>
        /// <returns>True if the service exists, false otherwise</returns>
        public static bool ServiceExists(string serviceName)
        {
            IntPtr databaseHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
            if (databaseHandle == IntPtr.Zero)
            {
                throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot open ServiceManager. Last Error: {0}.", Marshal.GetLastWin32Error()));
            }

            IntPtr serviceHandle = OpenService(databaseHandle, serviceName, SERVICE_QUERY_CONFIG);
            int lastError = Marshal.GetLastWin32Error();
            if (IntPtr.Zero != serviceHandle)
            {
                return true;
            }
            else if (ERROR_SERVICE_DOES_NOT_EXIST == lastError)
            {
                return false;
            }
            else
            {
                throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot find Service {0}. Last Error: {1}.", serviceName, Marshal.GetLastWin32Error()));
            }
        }

        /// <summary>
        /// Look up the Failure action information for a service
        /// </summary>
        /// <param name="serviceName">The service to look up</param>
        /// <returns>Service Failure Action information</returns>
        public static ServiceInformation GetSericeInformation(string serviceName)
        {
            UInt32 dwBytesNeeded;

            // Get a valid service handle
            IntPtr serviceHandle = LookupServiceHandle(serviceName, scope.Query);

            // Determine the buffer size needed
            bool sucess = QueryServiceConfig2(serviceHandle, SERVICE_CONFIG_FAILURE_ACTIONS, IntPtr.Zero, 0, out dwBytesNeeded);

            IntPtr ptr = Marshal.AllocHGlobal((int)dwBytesNeeded);
            sucess = QueryServiceConfig2(serviceHandle, SERVICE_CONFIG_FAILURE_ACTIONS, ptr, dwBytesNeeded, out dwBytesNeeded);

            if (false == sucess)
            {
                throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot find  SERVICE_FAILURE_ACTIONS struct. Last Error: {0}.", Marshal.GetLastWin32Error()));
            }

            SERVICE_FAILURE_ACTIONS failureActions = new SERVICE_FAILURE_ACTIONS();
            Marshal.PtrToStructure(ptr, failureActions);

            ServiceInformation serviceConfigInformation = new ServiceInformation((UInt32)failureActions.dwResetPeriod, failureActions.lpRebootMsg, failureActions.lpCommand, failureActions.cActions);

            int offset = 0;
            for (int i = 0; i < failureActions.cActions; i++)
            {
                ServiceFailureAction action = new ServiceFailureAction();
                action.Type = (ServiceFailureActionType)Marshal.ReadInt32(failureActions.lpsaActions, offset);
                offset += sizeof(Int32);
                action.Delay = (UInt32)Marshal.ReadInt32(failureActions.lpsaActions, offset);
                offset += sizeof(Int32);
                serviceConfigInformation.Actions[i] = action;
            }

            // clean up
            Marshal.FreeHGlobal(ptr);
            if (serviceHandle != IntPtr.Zero)
            {
                CloseServiceHandle(serviceHandle);
            }

            return serviceConfigInformation;
        }

        /// <summary>
        /// Change service failure action information
        /// </summary>
        /// <param name="serviceName">the name of the service</param>
        /// <param name="resetPeriodInDays">the new reset period in days</param>
        /// <param name="failureActions">ordered list of failure action types to add</param>
        public static void SetServiceInformation(string serviceName, int resetPeriodInDays, ServiceFailureActionType[] failureActions)
        {
            SetServiceInformation(serviceName, resetPeriodInDays, string.Empty, string.Empty, failureActions);
        }

        /// <summary>
        /// Changes the service failure action information
        /// </summary>
        /// <param name="serviceName">The service to look for</param>
        /// <param name="resetPeriodInDays">The reset period (in days)</param>
        /// <param name="rebootMessage">The message displayed on reboot</param>
        /// <param name="commandLine">Program command line</param>
        /// <param name="actions">Ordered list of actions to add to the service configuration</param>
        public static void SetServiceInformation(string serviceName, int resetPeriodInDays, string rebootMessage, string commandLine, ServiceFailureActionType[] actions)
        {
            // Get a valid service handle
            IntPtr serviceHandle = LookupServiceHandle(serviceName, scope.Modify);

            // marshal the actions
            ServiceFailureAction action = new ServiceFailureAction();
            IntPtr lpsaActions = Marshal.AllocHGlobal(Marshal.SizeOf(action) * actions.Length);
            // Marshal.StructureToPtr(action, lpsaActions, false);
            IntPtr nextAction = lpsaActions;

            for (int i = 0; i < actions.Length; i++)
            {
                action = new ServiceFailureAction();
                action.Type = actions[i];
                action.Delay = (UInt32)TimeSpan.FromMinutes(1).TotalMilliseconds;

                Marshal.StructureToPtr(action, nextAction, false);
                nextAction = (IntPtr)(nextAction.ToInt64() + Marshal.SizeOf(action));
            }


            // now put it all in one struct
            SERVICE_FAILURE_ACTIONS failureActions = new SERVICE_FAILURE_ACTIONS();
            failureActions.dwResetPeriod = (int)TimeSpan.FromDays(resetPeriodInDays).TotalSeconds;
            failureActions.lpRebootMsg = rebootMessage;
            failureActions.lpCommand = commandLine;
            failureActions.cActions = actions.Length;
            failureActions.lpsaActions = lpsaActions;

            IntPtr lpInfo = Marshal.AllocHGlobal(Marshal.SizeOf(failureActions));
            Marshal.StructureToPtr(failureActions, lpInfo, true);

            // do the change
            bool success = ChangeServiceConfig2(serviceHandle, SERVICE_CONFIG_FAILURE_ACTIONS, lpInfo);
            //int errorcode = GetLatError();
            // clean up
            Marshal.FreeHGlobal(lpInfo);
            Marshal.FreeHGlobal(lpsaActions);
            if (serviceHandle != IntPtr.Zero)
            {
                CloseServiceHandle(serviceHandle);
            }

            if (false == success)
            {
                throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot set ServiceConfig. Last Error: {0}.", Marshal.GetLastWin32Error()));
            }
        }

        /// <summary>
        /// looks up a service and returns a valid service handle
        /// </summary>
        /// <param name="serviceName">name of the service</param>
        /// <returns> looks up a service handle</returns>
        private static IntPtr LookupServiceHandle(string serviceName, scope scope)
        {
            IntPtr databaseHandle = IntPtr.Zero;
            IntPtr serviceHandle = IntPtr.Zero;

            try
            {
                databaseHandle = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
                // int errorcode = GetLatError();
                if (databaseHandle == IntPtr.Zero)
                {
                    throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot open ServiceManager. Last Error: {0}.", Marshal.GetLastWin32Error()));
                }
                switch (scope)
                {
                    case scope.Query:
                        {
                            serviceHandle = OpenService(databaseHandle, serviceName, SERVICE_QUERY_CONFIG);
                            break;
                        }
                    case scope.Modify:
                        {
                            serviceHandle = OpenService(databaseHandle, serviceName, SERVICE_ALL_ACCESS);
                            break;
                        }
                }

                if (serviceHandle == IntPtr.Zero)
                {
                    throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot find Service {0}. Last Error: {1}.", serviceName, Marshal.GetLastWin32Error()));
                }
            }
            finally
            {
                if (IntPtr.Zero == databaseHandle)
                {
                    CloseServiceHandle(databaseHandle);
                }
                if (IntPtr.Zero == serviceHandle)
                {
                    CloseServiceHandle(serviceHandle);
                }
            }
            return serviceHandle;
        }

        #region P/Invoke declarations
        /// <summary>
        /// Declarations in this section are provided by pinvoke.net
        /// </summary>

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class SERVICE_FAILURE_ACTIONS
        {
            public int dwResetPeriod;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRebootMsg;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpCommand;
            public int cActions;
            public IntPtr lpsaActions;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(String lpMachineName, String lpDatabaseName, UInt32 dwDesiredAccess);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenService(IntPtr hSCManager, String lpServiceName, UInt32 dwDesiredAccess);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "QueryServiceConfig2W")]
        private static extern Boolean QueryServiceConfig2(IntPtr hService, UInt32 dwInfoLevel, IntPtr buffer, UInt32 cbBufSize, out UInt32 pcbBytesNeeded);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "ChangeServiceConfig2W")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2(IntPtr hService, UInt32 dwInfoLevel, IntPtr lpInfo);
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        private const Int32 SC_MANAGER_ALL_ACCESS = 0x000F003F;
        private const Int32 SERVICE_QUERY_CONFIG = 0x00000001;
        private const Int32 SERVICE_CHANGE_CONFIG = 0x00000002;
        private const UInt32 SERVICE_CONFIG_FAILURE_ACTIONS = 0x02;
        private const UInt32 DACL_SECURITY_INFORMATION = 0x00000004;
        private const UInt32 SERVICE_ALL_ACCESS = 0xF01FF;

        /// <summary>
        /// The specified service does not exist as an installed service.
        /// </summary>
        private const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;


        #endregion // P/Invoke declarations
    }

    /// <summary>
    /// Service Information
    /// </summary>
    public class ServiceInformation
    {
        public const UInt32 INFINITE = 0xFFFFFFFF;

        public UInt32 ResetPeriod;
        public string RebootMessage;
        public string ProgramCommandLine;
        public int ActionCount;
        public ServiceFailureAction[] Actions;

        public ServiceInformation(UInt32 resetPeriod, string rebootMessage, string programCommandLine, int actionCount)
        {
            this.ResetPeriod = resetPeriod;
            this.RebootMessage = rebootMessage;
            this.ProgramCommandLine = programCommandLine;
            this.ActionCount = actionCount;
            this.Actions = new ServiceFailureAction[this.ActionCount];
        }

        public UInt32 ResetPeriodInDays
        {
            get
            {
                if (ServiceInformation.INFINITE != this.ResetPeriod)
                {
                    return (this.ResetPeriod / (60 * 60 * 24));
                }
                else
                {
                    return ServiceInformation.INFINITE;
                }
            }
        }
    }

    /// <summary>
    /// Failure Action Types
    /// </summary>
    public enum ServiceFailureActionType : uint
    {
        None = 0,
        RestartService = 1,
        RebootComputer = 2,
        RunCommand = 3
    }
    public enum scope
    {
        Query,
        Modify

    }

    /// <summary>
    /// A Service failure action
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class ServiceFailureAction
    {
        public ServiceFailureActionType Type;
        public UInt32 Delay;
    }

    enum SE_OBJECT_TYPE
    {
        SE_UNKNOWN_OBJECT_TYPE,
        SE_FILE_OBJECT,
        SE_SERVICE,
        SE_PRINTER,
        SE_REGISTRY_KEY,
        SE_LMSHARE,
        SE_KERNEL_OBJECT,
        SE_WINDOW_OBJECT,
        SE_DS_OBJECT,
        SE_DS_OBJECT_ALL,
        SE_PROVIDER_DEFINED_OBJECT,
        SE_WMIGUID_OBJECT,
        SE_REGISTRY_WOW64_32KEY
    }

    enum SECURITY_INFORMATION
    {
        OWNER_SECURITY_INFORMATION = 1,
        GROUP_SECURITY_INFORMATION = 2,
        DACL_SECURITY_INFORMATION = 4,
        SACL_SECURITY_INFORMATION = 8,
    }

}
