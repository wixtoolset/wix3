//-----------------------------------------------------------------------
// <copyright file="Program.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Defines a Frost application.</summary>
//-----------------------------------------------------------------------

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
