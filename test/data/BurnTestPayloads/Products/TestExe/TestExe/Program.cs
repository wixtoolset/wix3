//-------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestExe
{
    class Program
    {
        static List<Task> tasks;
        static int exitCodeToReturn = 0;

        static int Main(string[] args)
        {
            Usage();
            tasks = TaskParser.ParseTasks(args);

            foreach (Task t in tasks)
            {
                // special case for the ExitCodeTask
                if (t.GetType() == typeof(ExitCodeTask))
                {
                    exitCodeToReturn = int.Parse(t.data);
                }
                else
                {
                    t.RunTask();
                }
            }

            Console.WriteLine("Exiting with ExitCode = {0}", exitCodeToReturn);
            return exitCodeToReturn;
        }

        static void Usage()
        {
            Console.WriteLine(@"TestExe.exe");
            Console.WriteLine(@"");
            Console.WriteLine(@"TestExe can be passed various switches to define how it will behave and what tasks it will perform.");
            Console.WriteLine(@"All switches are optional.");
            Console.WriteLine(@"Any # of switches can be combined in any order.");
            Console.WriteLine(@"Switches can be specified multiple times.");
            Console.WriteLine(@"The order of the switches listed is the order they will be processed.");
            Console.WriteLine(@"Info is written to stdout to describe what tasks are being performed as they are executed.");
            Console.WriteLine(@"");
            Console.WriteLine(@"Usage: TestExe.exe [tasks...]");
            Console.WriteLine(@"");
            Console.WriteLine(@"");
            Console.WriteLine(@"/ec #            Exit code to return.  Can only be specified once.  If not specified, 0 will be returned.  Example: “/ec 3010” would return 3010");
            Console.WriteLine(@"/s #             Milliseconds to sleep before continuing.  Example: “/s 5000” would sleep 5 seconds.");
            Console.WriteLine(@"/sr #-#          Random range of Milliseconds to sleep before continuing.  Example: “/sr 5000-10000” would sleep between 5-10 seconds.");
            Console.WriteLine(@"/log filename    Create a log file called filename.  Contents of the log are static text.  Example: “/log %temp%\test.log” would create a %temp%\test.log file.");
            Console.WriteLine(@"/Pinfo filename  Create an xml file containing information about the process: PID, start time, user running the process, etc.");
            Console.WriteLine(@"/fe filename     Wait for a file to exist before continuing.  Example: “/fe %temp%\cache\file.msi” would wait until %temp%\cache\file.msi exists.");
            Console.WriteLine(@"/regw regkey,name,type,value    (Re)writes a registry key with the specified value");
            Console.WriteLine(@"/regd regkey,[name]    Deletes registry key name or key and all of its children (subkeys and values)");
            Console.WriteLine(@"");
            Console.WriteLine(@"Example: ");
            Console.WriteLine(@"");
            Console.WriteLine(@"TestExe.exe /ec 1603 /Pinfo %temp%\Pinfo1.xml /s 1000 /log %temp%\log1.log /sr 5000-10000 /log %temp%\log2.log");
            Console.WriteLine(@""); 
            Console.WriteLine(@"This would result in the following execution:");
            Console.WriteLine(@" - Create an xml file with the current process info in it.");
            Console.WriteLine(@" - Sleep 1 seconds");
            Console.WriteLine(@" - Create log1.log");
            Console.WriteLine(@" - Sleep between 5-10 seconds");
            Console.WriteLine(@" - Create log2.log");
            Console.WriteLine(@" - Exit with 1603");
            Console.WriteLine(@"");
        }
    }
}
