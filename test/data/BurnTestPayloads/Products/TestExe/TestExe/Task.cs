// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using Microsoft.Win32;

namespace TestExe
{
    public abstract class Task
    {
        public string data;

        public Task(string Data)
        {
            data = Data;
        }

        public abstract void RunTask();

    }

    public class ExitCodeTask : Task
    {
        public ExitCodeTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            // this task does nothing.  Just stores data about what exit code to return.
        }
    }

    public class SleepTask : Task
    {
        public SleepTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            int milliseconds = int.Parse(this.data);
            Console.WriteLine("Starting to sleep for {0} milliseconds", milliseconds);
            System.Threading.Thread.Sleep(milliseconds);
        }
    }

    public class SleepRandomTask : Task
    {
        public SleepRandomTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            int low = int.Parse(data.Split(new string[] { ":" }, 2, StringSplitOptions.None)[0]);
            int high = int.Parse(data.Split(new string[] { ":" }, 2, StringSplitOptions.None)[1]);

            Random r = new Random();
            int milliseconds = r.Next(high - low) + low;
            Console.WriteLine("Starting to sleep for {0} milliseconds", milliseconds);
            System.Threading.Thread.Sleep(milliseconds);
        }
    }

    public class LogTask : Task
    {
        string[] argsUsed;
        public LogTask(string Data, string[] args)
            : base(Data)
        {
            argsUsed = args;
        }

        public override void RunTask()
        {
            string logFile = "";
            string argsUsedString = "";

            foreach (string a in argsUsed)
            {
                argsUsedString += a + " ";
            }

            try
            {
                logFile = System.Environment.ExpandEnvironmentVariables(data);
                Console.WriteLine("creating log file: " + logFile);
                StreamWriter textFile = File.CreateText(logFile);
                textFile.WriteLine("This is a log file created by TestExe.exe");
                textFile.WriteLine("Args used: " + argsUsedString);
                textFile.Close();
            }
            catch
            {
                Console.WriteLine("creating a log file failed for: {0}", logFile);
            }

        }
    }

    public class ProcessInfoTask : Task
    {
        public ProcessInfoTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            try
            {
                string processInfoXml = "";

                // Get information about the process and who is running it
                Process thisProc = Process.GetCurrentProcess();
                string username = thisProc.StartInfo.EnvironmentVariables["username"].ToString();

                int parentProcId = GetParentProcess(thisProc.Id);
                Process parentProc = Process.GetProcessById(parentProcId);
                string parentUsername = parentProc.StartInfo.EnvironmentVariables["username"].ToString();

                int grandparentProcId = GetParentProcess(parentProc.Id);
                Process grandparentProc = Process.GetProcessById(grandparentProcId);
                string grandparentUsername = grandparentProc.StartInfo.EnvironmentVariables["username"].ToString();

                processInfoXml += "<ProcessInfo>";
                processInfoXml += "  <ProcessName>" + thisProc.ProcessName + "</ProcessName>";
                processInfoXml += "  <Id>" + thisProc.Id.ToString() + "</Id>";
                processInfoXml += "  <SessionId>" + thisProc.SessionId.ToString() + "</SessionId>";
                processInfoXml += "  <MachineName>" + thisProc.MachineName + "</MachineName>";
                // this stuff isn't set since we didn't start the process and tell it what to use.  So don't bother 
                //processInfoXml += "  <StartInfo>";
                //processInfoXml += "    <FileName>" + thisProc.StartInfo.FileName + "</FileName>";
                //processInfoXml += "    <UserName>" + thisProc.StartInfo.UserName + "</UserName>";
                //processInfoXml += "    <WorkingDirectory>" + thisProc.StartInfo.WorkingDirectory + "</WorkingDirectory>";
                //processInfoXml += "    <Arguments>" + thisProc.StartInfo.Arguments + "</Arguments>";
                //processInfoXml += "  </StartInfo>";
                processInfoXml += "  <StartTime>" + thisProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "  <Username>" + username + "</Username>";
                processInfoXml += "  <ParentProcess>";
                processInfoXml += "    <ProcessName>" + parentProc.ProcessName + "</ProcessName>";
                processInfoXml += "    <Id>" + parentProc.Id.ToString() + "</Id>";
                processInfoXml += "    <StartTime>" + parentProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "    <Username>" + parentUsername + "</Username>";
                processInfoXml += "  </ParentProcess>";
                processInfoXml += "  <GrandparentProcess>";
                processInfoXml += "    <ProcessName>" + grandparentProc.ProcessName + "</ProcessName>";
                processInfoXml += "    <Id>" + grandparentProc.Id.ToString() + "</Id>";
                processInfoXml += "    <StartTime>" + grandparentProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "    <Username>" + grandparentUsername + "</Username>";
                processInfoXml += "  </GrandparentProcess>";
                processInfoXml += "</ProcessInfo>";

                string logFile = System.Environment.ExpandEnvironmentVariables(data);
                Console.WriteLine("Creating Process Info data file: " + logFile);
                StreamWriter textFile = File.CreateText(logFile);
                textFile.WriteLine(processInfoXml);
                textFile.Close();
            }
            catch (Exception eX)
            {
                Console.WriteLine("Creating Process Info data file failed");
                Console.WriteLine(eX.Message);
            }


        }

        private static int GetParentProcess(int Id)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + Id.ToString() + "'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return parentPid;
        }
    }

    public class FileExistsTask : Task
    {
        public FileExistsTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            string fileToExist = System.Environment.ExpandEnvironmentVariables(data);

            if (!String.IsNullOrEmpty(fileToExist))
            {
                Console.WriteLine("Waiting for this file to exist: \"" + fileToExist + "\"");
                while (!System.IO.File.Exists(fileToExist))
                {
                    System.Threading.Thread.Sleep(250);
                }
                Console.WriteLine("Found: \"" + fileToExist + "\"");
            }

        }
    }

    /// <summary>
    /// Task class that will create a registry key and write a name and value in it
    /// </summary>
    public class RegistryWriterTask : Task
    {
        private string hive;
        private string keyPath;
        private string[] keyPathArray;
        private string name;
        private RegistryValueKind regValueKind;
        private object value;

        public RegistryWriterTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            if (parseRegKeyNameTypeValue(System.Environment.ExpandEnvironmentVariables(data)))
            {
                RegistryKey rk = Registry.LocalMachine;

                if (hive == "HKCU") rk = Microsoft.Win32.Registry.CurrentUser;
                if (hive == "HKCC") rk = Microsoft.Win32.Registry.CurrentConfig;
                if (hive == "HKLM") rk = Microsoft.Win32.Registry.LocalMachine;

                foreach (string key in keyPathArray)
                {
                    rk = rk.CreateSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                rk.SetValue(name, value, regValueKind);
                Console.WriteLine("Created registry key: '{0}' name: '{1}' value: '{2}' of type: '{3}'",
                    hive + "\\" + keyPath,
                    name,
                    value.ToString(),
                    regValueKind.ToString());
            }
            else
            {
                Console.WriteLine("Unable to write registry key.");
            }

        }

        private bool parseRegKeyNameTypeValue(string delimittedData)
        {
            string[] splitString = delimittedData.Split(new string[] { "," }, StringSplitOptions.None);
            if (splitString.Length != 4)
            {
                Console.WriteLine("Invalid regkey. Unable to parse key,name,type,value from: \"" + delimittedData + "\"");
                return false;
            }
            else
            {
                keyPath = splitString[0];
                name = splitString[1];
                string datatype = splitString[2];
                if (datatype == "DWord")
                {
                    value = UInt32.Parse(splitString[3]);
                }
                else if (datatype == "QWord")
                {
                    value = UInt64.Parse(splitString[3]);
                }
                else
                {
                    value = splitString[3];
                }

                if (keyPath.ToUpper().StartsWith("HKLM\\"))
                {
                    hive = "HKLM";
                    keyPath = keyPath.Replace("HKLM\\", "");
                }
                else if (keyPath.ToUpper().StartsWith("HKCC\\"))
                {
                    hive = "HKCC";
                    keyPath = keyPath.Replace("HKCC\\", "");
                }
                else if (keyPath.ToUpper().StartsWith("HKCU\\"))
                {
                    hive = "HKCU";
                    keyPath = keyPath.Replace("HKCU\\", "");
                }
                else
                {
                    Console.WriteLine("Invalid regkey. Unable to determin hive.  regkey must start with either: [HKLM], [HKCU], or [HKCC]");
                    return false;
                }
                keyPathArray = keyPath.Split(new string[] { "\\" }, StringSplitOptions.None);

                try
                {
                    regValueKind = (RegistryValueKind)System.Enum.Parse(typeof(RegistryValueKind), datatype);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid datatype. It must be: String, DWord, or QWord (case sensitive)");
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Task class that will delete a registry key value or registry key and all of its children
    /// </summary>
    public class RegistryDeleterTask : Task
    {
        private string hive;
        private string keyPath;
        private string[] keyPathArray;
        private string name;

        public RegistryDeleterTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            if (parseRegKeyName(System.Environment.ExpandEnvironmentVariables(data)))
            {
                try
                {
                    RegistryKey rk = Registry.LocalMachine;

                    if (hive == "HKCU") rk = Microsoft.Win32.Registry.CurrentUser;
                    if (hive == "HKCC") rk = Microsoft.Win32.Registry.CurrentConfig;
                    if (hive == "HKLM") rk = Microsoft.Win32.Registry.LocalMachine;

                    RegistryKey rkParent = null;
                    foreach (string key in keyPathArray)
                    {
                        rkParent = rk;
                        rk = rk.OpenSubKey(key, true);
                    }

                    if (String.IsNullOrEmpty(name))
                    {
                        // delete the key and all of its children
                        string subkeyToDelete = keyPathArray[keyPathArray.Length - 1];
                        rkParent.DeleteSubKeyTree(subkeyToDelete);
                        Console.WriteLine("Deleted registry key: '{0}'", hive + "\\" + keyPath);
                    }
                    else
                    {
                        // just delete this value
                        rk.DeleteValue(name);
                        Console.WriteLine("Deleted registry key: '{0}' name: '{1}'", hive + "\\" + keyPath, name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to delete registry key: '{0}'", hive + "\\" + keyPath);
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Unable to delete registry key.");
            }

        }

        private bool parseRegKeyName(string delimittedData)
        {
            string[] splitString = delimittedData.Split(new string[] { "," }, StringSplitOptions.None);

            if (splitString.Length > 2)
            {
                Console.WriteLine("Unable to parse registry key and name.");
                return false;
            }

            keyPath = splitString[0];
            if (splitString.Length == 2)
            {
                name = splitString[1];
            }

            if (keyPath.ToUpper().StartsWith("HKLM\\"))
            {
                hive = "HKLM";
                keyPath = keyPath.Replace("HKLM\\", "");
            }
            else if (keyPath.ToUpper().StartsWith("HKCC\\"))
            {
                hive = "HKCC";
                keyPath = keyPath.Replace("HKCC\\", "");
            }
            else if (keyPath.ToUpper().StartsWith("HKCU\\"))
            {
                hive = "HKCU";
                keyPath = keyPath.Replace("HKCU\\", "");
            }
            else
            {
                Console.WriteLine("Invalid regkey. Unable to determine hive.  regkey must start with either: [HKLM], [HKCU], or [HKCC]");
                return false;
            }
            keyPathArray = keyPath.Split(new string[] { "\\" }, StringSplitOptions.None);
            return true;
        }
    }

    public class TaskParser
    {

        public static List<Task> ParseTasks(string[] args)
        {
            List<Task> tasks = new List<Task>();

            try
            {
                // for invalid args.  return empty list
                if (args.Length % 2 == 0)
                {
                    Task t;

                    for (int i = 0; i < args.Length; i += 2)
                    {
                        switch (args[i].ToLower())
                        {
                            case "/ec":
                                t = new ExitCodeTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/s":
                                t = new SleepTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/sr":
                                t = new SleepRandomTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/log":
                                t = new LogTask(args[i + 1], args);
                                tasks.Add(t);
                                break;
                            case "/pinfo":
                                t = new ProcessInfoTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/fe":
                                t = new FileExistsTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/regw":
                                t = new RegistryWriterTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/regd":
                                t = new RegistryDeleterTask(args[i + 1]);
                                tasks.Add(t);
                                break;

                            default:
                                Console.WriteLine("Error: Invalid switch specified.");
                                return new List<Task>();
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error: Invalid switch data specified.  Couldn't parse the data.");
                return new List<Task>();
            }

            return tasks;
        }
    }
}
