// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Runtime.InteropServices;

    using Microsoft.Tools.WindowsInstallerXml.Test.Frost.Core;

    public class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(Application_UnhandledException);

            try
            {
                Frost test = new Frost();

                test.StartUXInterface();
                test.StopUXInterface();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Console.WriteLine("Frost : " + e.Message);
        }
    }
}
