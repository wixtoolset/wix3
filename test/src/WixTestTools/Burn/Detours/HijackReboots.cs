// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using WixTest;

namespace WixTest.Burn.Detours
{
    /// <summary>
    /// Will add/remove a detour to burnstub.exe
    /// The detour will prevent all calls that burnstub.exe makes to shutdown
    /// the machine from actually shutting down the machine.  Calls to those 
    /// APIs are detoured to NoOp test functions.  This enables tests to exercise
    /// codepaths that would reboot a machine without actually rebooting the machine.
    /// </summary>
    public class HijackReboots : IDisposable
    {
        private static string burnstubExe = Path.Combine(Settings.WixToolsDirectory, "burnstub.exe");
        private static string setdllExe = System.Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\external\HijackShutdowns\setdll.exe");
        private static string detouredDll = System.Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\external\HijackShutdowns\detoured.dll");
        private static string shutdownWDll = System.Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\external\HijackShutdowns\shutdownW.dll");
        private static string hijackRebootsPath = System.Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\external\HijackShutdowns\HijackShutdowns");

        /// <summary>
        /// To detour reboot calls. This is used to test reboot without actually shutdowing the system
        /// </summary>
        public HijackReboots()
        {
            if (!File.Exists(shutdownWDll))
            {
                throw new Exception("The 'Detours' tool and shutdownW.dll is required to run this test.  This tool will allow you to redirect calls to Shutdown APIs so they don't actually reboot your machine and kill your test before it completes.");
            }
            AddDetour();
            UpdatePath();
        }

        public void CopyDependencies(string destination)
        {
            foreach (string srcFile in new string[] { detouredDll, shutdownWDll })
            {
                string destFile = Path.Combine(destination, Path.GetFileName(srcFile));
                LayoutManager.LayoutManager.CopyFile(srcFile, destFile);
            }
        }

        private void UpdatePath()
        {
            string currentPath = System.Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);
            if (!currentPath.ToLower().Contains(hijackRebootsPath.ToLower()))
            {
                string newPath = currentPath + ";" + hijackRebootsPath;
                System.Environment.SetEnvironmentVariable("Path", newPath, EnvironmentVariableTarget.Process);
            }
        }

        private void AddDetour()
        {
            string args = "/d:shutdownW.dll " + burnstubExe;
            RunSetDll(args);
        }

        private void RemoveDetour()
        {
            string args = "/r " + burnstubExe;
            RunSetDll(args);
        }

        private void RunSetDll(string args)
        {
            Process proc = new Process();
            proc.StartInfo.Arguments = args;
            proc.StartInfo.FileName = setdllExe;
            proc.StartInfo.WorkingDirectory = hijackRebootsPath;

            proc.Start();
            proc.WaitForExit();
        }



        #region IDisposable Members

        void IDisposable.Dispose()
        {
            RemoveDetour();
        }

        #endregion
    }
}
